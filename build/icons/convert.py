"""Convert one Icons.ai artboard into a normalised SVG master that Grasshopper 2's SvgIcon can load.

GH2 SvgIcon is a deliberately small parser (see Grasshopper2.xml, T:Grasshopper2.UI.Icon.Vector.SvgIcon):
no transforms, clips, masks, filters, gradients, group opacity or arcs, and every sub-path is filled on
its own (so holes cannot be subtracted). The masters therefore are a flat list of <path> elements with
absolute M/L/C/Z data in a 0 0 24 24 viewBox and an inline fill colour each:

* text is converted to outlines; glyphs/faces with holes are cut into hole-free pieces;
* strokes are converted to filled outlines;
* Illustrator's blurred, masked shadow images are replaced by flat polygons coloured with the shade
  observed in the original artwork (measured by rendering the artboard with and without them);
* parameter icons, which use a placed 24px bitmap hexagon, get a faceted vector hexagon instead.
"""
import io, sys, re, math
import numpy as np, cv2, pymupdf, resvg_py
from PIL import Image
from svgelements import SVG, Path, Shape, Move, Line, CubicBezier, QuadraticBezier, Arc, Close, Image as SvgImage, Use
from shapely.geometry import Polygon, MultiPolygon
from shapely.ops import unary_union

SS = 16          # supersampling for shadow analysis (24*16 = 384 px)
N = 24

def hexcol(c):
    return "#%02x%02x%02x" % tuple(int(round(v)) for v in c[:3])

def fmt(v):
    s = f"{v:.3f}".rstrip("0").rstrip(".")
    return "0" if s in ("-0", "") else s

def path_to_d(p):
    out = []
    for seg in p.segments():
        if isinstance(seg, Move):
            out.append(f"M{fmt(seg.end.x)} {fmt(seg.end.y)}")
        elif isinstance(seg, Line):
            out.append(f"L{fmt(seg.end.x)} {fmt(seg.end.y)}")
        elif isinstance(seg, CubicBezier):
            out.append("C" + " ".join(f"{fmt(q.x)} {fmt(q.y)}" for q in (seg.control1, seg.control2, seg.end)))
        elif isinstance(seg, QuadraticBezier):
            s0, c, e = seg.start, seg.control, seg.end
            c1 = (s0.x + 2 / 3 * (c.x - s0.x), s0.y + 2 / 3 * (c.y - s0.y))
            c2 = (e.x + 2 / 3 * (c.x - e.x), e.y + 2 / 3 * (c.y - e.y))
            out.append(f"C{fmt(c1[0])} {fmt(c1[1])} {fmt(c2[0])} {fmt(c2[1])} {fmt(e.x)} {fmt(e.y)}")
        elif isinstance(seg, Arc):
            for cb in seg.as_cubic_curves():
                out.append("C" + " ".join(f"{fmt(q.x)} {fmt(q.y)}" for q in (cb.control1, cb.control2, cb.end)))
        elif isinstance(seg, Close):
            out.append("Z")
    return "".join(out)

def path_polys(p, scale, closed_only=True):
    """Flatten a reified path into polylines (one per subpath)."""
    polys, cur = [], []
    mn = 3 if closed_only else 2
    for seg in p.segments():
        if isinstance(seg, Move):
            if len(cur) >= mn: polys.append(cur)
            cur = [(seg.end.x * scale, seg.end.y * scale)]
        elif isinstance(seg, Close):
            if cur and not closed_only: cur.append(cur[0])
            if len(cur) >= mn: polys.append(cur)
            cur = []
        elif seg.start is not None:
            n = 1 if isinstance(seg, Line) else 24
            for t in np.linspace(0, 1, n + 1)[1:]:
                q = seg.point(t); cur.append((q.x * scale, q.y * scale))
    if len(cur) >= mn: polys.append(cur)
    return polys

def evenodd_fix_d(p):
    """Re-express an even-odd fill as nonzero by polygonising with even-odd semantics."""
    g = None
    for q in path_polys(p, 1.0):
        pg = Polygon(q).buffer(0)
        g = pg if g is None else g.symmetric_difference(pg)
    return shapely_to_d(g) if g is not None else ""

def face_geom(kind, obj):
    if kind == "stroke": return obj
    g = None
    for q in path_polys(obj, 1.0):
        if len(q) < 3: continue
        pg = Polygon(q).buffer(0)
        g = pg if g is None else g.symmetric_difference(pg)
    return g if g is not None else Polygon()

def has_holes(g):
    gs = g.geoms if hasattr(g, "geoms") else [g]
    return any(hasattr(p, "interiors") and len(p.interiors) for p in gs)

def decompose(g, overlap=0.02):
    """SvgIcon fills each subpath independently, so holes (letter counters) cannot be expressed.
    Cut every holed polygon with a vertical line through each hole until no holes remain."""
    from shapely.geometry import LineString
    from shapely.ops import split
    todo = list(g.geoms) if hasattr(g, "geoms") else [g]
    done = []
    while todo:
        p = todo.pop()
        if p.is_empty or not hasattr(p, "exterior"): continue
        if not p.interiors:
            done.append(p); continue
        h = p.interiors[0]
        x = h.centroid.x
        minx, miny, maxx, maxy = p.bounds
        parts = split(p, LineString([(x, miny - 1), (x, maxy + 1)]))
        todo.extend(q for q in parts.geoms if q.area > 1e-6)
    # tiny overlap hides anti-aliasing seams between the pieces
    return [q.buffer(overlap, join_style="mitre") for q in done]

def vector_elements(page):
    raw = page.get_svg_image(text_as_path=True)
    # drop the shadow layers entirely (any group with a mask) before parsing
    raw = re.sub(r'<g mask="url\(#mask_\d+\)">.*?</g>\s*</g>', "", raw, flags=re.S)
    # artboards larger than 24pt (e.g. the logo) are scaled into the 24-unit icon space via the viewport
    raw = re.sub(r'(<svg[^>]*?)\swidth="[^"]*"\s+height="[^"]*"', lambda m: m.group(1) + ' width="%d" height="%d"' % (N, N), raw, count=1)
    svg = SVG.parse(io.StringIO(raw), reify=True)
    els = []
    for e in svg.elements():
        if isinstance(e, SvgImage):
            continue
        if isinstance(e, Shape) and not isinstance(e, SVG):
            p = Path(e); p.reify()
            if len(p) == 0:
                continue
            gop = float(e.values.get("opacity", 1.0)) if "opacity" in e.values else 1.0
            bb = p.bbox()
            is_backdrop = bb is not None and bb[2] - bb[0] >= N * 0.98 and bb[3] - bb[1] >= N * 0.98
            if is_backdrop and p.fill is not None and p.fill.value is not None and (p.fill.red, p.fill.green, p.fill.blue) == (255, 255, 255):
                continue  # full-artboard white backdrop (present in Logo.ai)
            if p.fill is not None and p.fill.value is not None:
                op = (p.fill.opacity if p.fill.opacity is not None else 1.0) * gop
                evenodd = str(e.values.get("fill-rule", "")).lower() == "evenodd"
                els.append(("fill", p, (p.fill.red, p.fill.green, p.fill.blue), op, evenodd))
            if p.stroke is not None and p.stroke.value is not None and (p.stroke_width or 0) > 0:
                op = (p.stroke.opacity if p.stroke.opacity is not None else 1.0) * gop
                cap = str(e.values.get("stroke-linecap", "butt"))
                join = str(e.values.get("stroke-linejoin", "miter"))
                els.append(("stroke", stroke_outline(p, p.stroke_width, cap, join), (p.stroke.red, p.stroke.green, p.stroke.blue), op, False))
    return els

def stroke_outline(p, width, cap, join):
    """Outline a stroked path into a shapely (Multi)Polygon in SVG units."""
    from shapely.geometry import LineString
    capst = {"round": "round", "square": "square"}.get(cap, "flat")
    joinst = {"round": "round", "bevel": "bevel"}.get(join, "mitre")
    geoms = []
    for poly in path_polys(p, 1.0, closed_only=False):
        if len(poly) >= 2:
            geoms.append(LineString(poly).buffer(width / 2, cap_style=capst, join_style=joinst, mitre_limit=4, quad_segs=8))
    return unary_union(geoms)

def shapely_to_d(g):
    from shapely.geometry.polygon import orient
    gs = g.geoms if hasattr(g, "geoms") else [g]
    out = []
    for pg in gs:
        if pg.is_empty or not hasattr(pg, "exterior"): continue
        pg = orient(pg.simplify(0.01), 1.0)  # exterior CCW, holes CW -> nonzero-safe
        for ring in [pg.exterior, *pg.interiors]:
            pts = list(ring.coords)[:-1]
            out.append("M" + "L".join(f"{fmt(x)} {fmt(y)}" for x, y in pts) + "Z")
    return "".join(out)

def render_pdf(page, scale):
    k = scale * N / page.rect.width
    pix = page.get_pixmap(clip=page.rect, matrix=pymupdf.Matrix(k, k), alpha=True)
    return np.asarray(Image.frombytes("RGBA", (pix.width, pix.height), pix.samples)).astype(float)

def render_svg(svg_text, px):
    png = bytes(resvg_py.svg_to_bytes(svg_string=svg_text, width=px, height=px))
    return np.asarray(Image.open(io.BytesIO(png)).convert("RGBA")).astype(float)

def build_svg(items):
    body = []
    for d, col, op in items:
        a = f' fill-opacity="{fmt(op)}"' if op < 0.999 else ""
        body.append(f'<path d="{d}" fill="{hexcol(col)}"{a}/>')
    return ('<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24">\n'
            + "\n".join(body) + "\n</svg>\n")

def over_white(rgba):
    a = rgba[..., 3:4] / 255.0
    return rgba[..., :3] * a + 255 * (1 - a)

# Vector redraw of the raster parameter hexagon (Assets/ComponentIcons/Special/ParamBase.png),
# faceted to match the origami look: black rim, dark body, lighter upper-left facet, rim highlights.
HEX = [
    ("M0 11.5L6 1L18 1L24 11.5L18 22L6 22Z", (0, 0, 0)),
    ("M0.8 11.5L6.45 1.7L17.55 1.7L23.2 11.5L17.55 21.3L6.45 21.3Z", (0x24, 0x24, 0x24)),
    ("M0.8 11.5L6.45 1.7L17.55 1.7L4.4 17.6Z", (0x3a, 0x3a, 0x3a)),
    ("M0.8 11.5L6.45 1.7L17.55 1.7L17.1 2.5L6.9 2.5L1.7 11.5Z", (0x9a, 0x9a, 0x9a)),
    ("M6.45 21.3L17.55 21.3L23.2 11.5L22.3 11.5L17.1 20.5L6.9 20.5Z", (0x50, 0x50, 0x50)),
]

def is_param_page(page):
    for im in page.get_image_info():
        b = im["bbox"]
        if im["width"] <= 48 and b[2] - b[0] > 23 and b[3] - b[1] > 23:
            return True
    return False

def convert(page, shadow_thresh=0.06, min_area_px=40):
    els = vector_elements(page)
    if is_param_page(page):
        from svgelements import Path as _P
        hex_els = []
        for d, col in HEX:
            hp = _P(d); hex_els.append(("fill", hp, col, 1.0, False))
        els = hex_els + els
    param = is_param_page(page)
    items = []
    geoms = [face_geom(k, o) for k, o, *_ in els]
    for (kind, obj, col, op, eo), g in zip(els, geoms):
        if has_holes(g):
            d = "".join(shapely_to_d(q) for q in decompose(g))
        elif kind == "stroke" or eo: d = shapely_to_d(g)
        else: d = path_to_d(obj)
        items.append((d, col, op))
    base_svg = build_svg(items)
    px = N * SS
    full = render_pdf(page, SS)
    flat = render_svg(base_svg, px)
    F, V = over_white(full), over_white(flat)
    lumF = F.mean(axis=2) + 1; lumV = V.mean(axis=2) + 1
    dark = np.clip(1 - lumF / lumV, 0, 1)

    # topmost-face label map in draw order
    label = np.full((px, px), -1, np.int32)
    from shapely import affinity
    for i, g in enumerate(geoms):
        m = np.zeros((px, px), np.uint8)
        gs = g.geoms if hasattr(g, "geoms") else [g]
        for pg in gs:
            if pg.is_empty or not hasattr(pg, "exterior"): continue
            pg = affinity.scale(pg, SS, SS, origin=(0, 0))
            cv2.fillPoly(m, [np.round(np.array(pg.exterior.coords)).astype(np.int32)], 1)
            for h in pg.interiors: cv2.fillPoly(m, [np.round(np.array(h.coords)).astype(np.int32)], 0)
        label[m > 0] = i

    shadows = {}  # face index -> list of (d, colour)
    for i in ([] if param else range(len(els))):
        face = label == i
        if face.sum() < min_area_px: continue
        m = (face & (dark > shadow_thresh)).astype(np.uint8)
        m = cv2.morphologyEx(m, cv2.MORPH_OPEN, np.ones((5, 5), np.uint8))
        if m.sum() < min_area_px: continue
        cnts, _ = cv2.findContours(m, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        from shapely import affinity as _af
        facepoly = _af.scale(geoms[i], SS, SS, origin=(0, 0))
        for c in cnts:
            if cv2.contourArea(c) < min_area_px: continue
            ap = cv2.approxPolyDP(cv2.convexHull(c), 0.35 * SS, True).reshape(-1, 2)
            if len(ap) < 3: continue
            sp = Polygon(ap).buffer(0).intersection(facepoly.buffer(0.5))
            if sp.is_empty: continue
            region = np.zeros_like(m); cv2.fillPoly(region, [ap.astype(np.int32)], 1)
            sel = (region > 0) & face
            colour = np.median(F[sel], axis=0) if sel.any() else F[m > 0].mean(axis=0)
            spg = sp.geoms if isinstance(sp, MultiPolygon) else [sp]
            for g in spg:
                if g.area < min_area_px or not hasattr(g, "exterior"): continue
                pts = list(g.simplify(0.5).exterior.coords)[:-1]
                d = "M" + "L".join(f"{fmt(x / SS)} {fmt(y / SS)}" for x, y in pts) + "Z"
                shadows.setdefault(i, []).append((d, colour, 1.0))
    # Translucent faces (e.g. the logo's pale wings) blend with whatever is under them in ways the
    # flat master cannot reproduce exactly; bake each one to the colour observed in the original.
    for i, (d, col, op) in enumerate(items):
        if op < 0.999:
            sel = (label == i) & (dark <= shadow_thresh)
            if sel.sum() >= min_area_px:
                items[i] = (d, np.median(F[sel], axis=0), 1.0)
    out = []
    for i, it in enumerate(items):
        out.append(it)
        out.extend(shadows.get(i, []))
    return build_svg(out), len(els), sum(len(v) for v in shadows.values())
