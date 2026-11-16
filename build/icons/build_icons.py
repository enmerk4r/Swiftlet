"""Build the Swiftlet icon set from Assets/ComponentIcons/Icons.ai.

Outputs (paths relative to the repository root):
  Assets/Icons/svg/<slug>.svg     vector masters (GH2 SvgIcon-safe, see convert.py)
  Assets/Icons/png24/<slug>.png   24x24 PNGs rendered from the masters (for GH1)
  Assets/Icons/icons.json         slug -> artboard, legacy PNG, component/param class names
  src/Swiftlet.Runtime/IconSlugs.g.cs   class name -> slug lookup shared by the GH1 and GH2 shells

The artboard for each legacy PNG is found by pixel comparison (the .ai has no usable artboard
names), with explicit overrides where two artboards are near-identical. Class names are kept in
icons.json (originally bootstrapped from the GH1 icon switch in ShellIcons.cs); edit them there.

Usage:  python build/icons/build_icons.py [--repo PATH] [--sheet OUT.png] [--map-only]
        --map-only regenerates IconSlugs.g.cs from the existing icons.json without reconverting.
"""
import argparse, io, json, os, re, sys
import numpy as np, pymupdf, resvg_py
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import convert  # noqa: E402

# Artboards that the pixel matcher cannot tell apart (index is 0-based page number in Icons.ai).
OVERRIDES = {
    "Icons_create_multipart_form_data_named_24x24.png": 81,    # "BM" with N superscript
    "Icons_create_multipart_form_data_unnamed_24x24.png": 82,  # "BM" with U superscript
}
# Icons that come from other artwork files: slug -> (source .ai, page index, legacy 24px PNG, classes).
EXTRA = {
    "swiftlet-logo": ("Assets/Logo/Logo.ai", 0, "Assets/Logo/Logo_24x24.png", ["SwiftletPlugin"]),
}
# Legacy PNGs that are not referenced anywhere and duplicate another icon.
IGNORE = {"Icons-102.png"}
SLUG_FIXES = {"feild": "field"}


def slug_for(png):
    s = png.strip()
    s = re.sub(r"\.png$", "", s).strip()
    s = re.sub(r"^Icons_", "", s)
    s = re.sub(r"_24x24$", "", s.strip())
    for a, b in SLUG_FIXES.items():
        s = s.replace(a, b)
    return s.replace("_", "-").lower()


def parse_shell_icons(path):
    src = open(path, encoding="utf-8").read()
    pairs = re.findall(r'"(\w+)"\s*=>\s*"([^"]+\.png)"', src)
    return pairs


def on_white(img):
    img = img.convert("RGBA")
    bg = Image.new("RGBA", img.size, (255, 255, 255, 255))
    bg.alpha_composite(img)
    return np.asarray(bg.convert("RGB"), dtype=float)


def render_page(page, px):
    pix = page.get_pixmap(clip=pymupdf.Rect(0, 0, 24, 24), matrix=pymupdf.Matrix(px / 24, px / 24), alpha=True)
    return Image.frombytes("RGBA", (pix.width, pix.height), pix.samples)


def render_svg(svg, px):
    return Image.open(io.BytesIO(bytes(resvg_py.svg_to_bytes(svg_string=svg, width=px, height=px)))).convert("RGBA")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--repo", default=os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..")))
    ap.add_argument("--sheet", help="write a review contact sheet to this path")
    ap.add_argument("--map-only", action="store_true")
    a = ap.parse_args()
    R = a.repo
    if a.map_only:
        write_slug_map(R, json.load(open(os.path.join(R, "Assets", "Icons", "icons.json")))["icons"])
        return
    ai = os.path.join(R, "Assets", "ComponentIcons", "Icons.ai")
    legacy_dir = os.path.join(R, "Assets", "ComponentIcons")
    out_svg = os.path.join(R, "Assets", "Icons", "svg")
    out_png = os.path.join(R, "Assets", "Icons", "png24")
    os.makedirs(out_svg, exist_ok=True)
    os.makedirs(out_png, exist_ok=True)

    doc = pymupdf.open(ai)
    pages24 = [on_white(render_page(p, 24)) for p in doc]

    # Class lists live in icons.json (the source of truth since ShellIcons.cs switched to the generated
    # map). The original mapping was bootstrapped from the hand-written switch in ShellIcons.cs.
    classes_by_png = {}
    existing = os.path.join(R, "Assets", "Icons", "icons.json")
    if os.path.exists(existing):
        for m in json.load(open(existing))["icons"].values():
            classes_by_png.setdefault(m["legacyPng"], []).extend(m["classes"])
    else:
        for cls, png in parse_shell_icons(os.path.join(R, "src", "Swiftlet.Gh1", "ShellIcons.cs")):
            classes_by_png.setdefault(png, []).append(cls)

    legacy = sorted(f for f in os.listdir(legacy_dir) if f.lower().endswith(".png") and f not in IGNORE)
    manifest, problems = {}, []
    for png in legacy:
        if png in OVERRIDES:
            page, err = OVERRIDES[png], None
        else:
            img = on_white(Image.open(os.path.join(legacy_dir, png)).resize((24, 24)))
            errs = [float(np.abs(img - p).mean()) for p in pages24]
            page = int(np.argmin(errs)); err = round(errs[page], 2)
        slug = slug_for(png)
        if slug in manifest:
            problems.append(f"duplicate slug {slug} ({png} vs {manifest[slug]['legacyPng']})")
        classes = sorted(classes_by_png.get(png, []))
        if not classes:
            problems.append(f"{png}: no classes mapped")
        manifest[slug] = {"artboard": page + 1, "legacyPng": png, "matchError": err, "classes": classes}

    used_pages = {m["artboard"] - 1 for m in manifest.values()}
    for p in range(doc.page_count):
        if p not in used_pages:
            problems.append(f"artboard {p + 1} not matched to any legacy PNG")
    for png in classes_by_png:
        if png not in legacy and not png.startswith(("Logo_", "../")):
            problems.append(f"mapping references missing {png}")

    for slug, (src, page, png, classes) in EXTRA.items():
        manifest[slug] = {"source": src, "artboard": page + 1, "legacyPng": os.path.relpath(os.path.join(R, png), legacy_dir).replace(os.sep, "/"),
                          "matchError": None, "classes": classes}
    docs = {}
    for slug, m in sorted(manifest.items()):
        src = m.get("source", "Assets/ComponentIcons/Icons.ai")
        if src not in docs:
            docs[src] = pymupdf.open(os.path.join(R, src))
        svg, faces, shadows = convert.convert(docs[src][m["artboard"] - 1])
        open(os.path.join(out_svg, slug + ".svg"), "w", newline="\n").write(svg)
        render_svg(svg, 24).save(os.path.join(out_png, slug + ".png"))
        m["faces"], m["shadows"] = faces, shadows
        orig = on_white(Image.open(os.path.join(legacy_dir, m["legacyPng"])).resize((24, 24)))
        new = on_white(Image.open(os.path.join(out_png, slug + ".png")))
        m["diffVsLegacy"] = round(float(np.abs(orig - new).mean()), 2)
        print(f"{slug:50s} ab{m['artboard']:>4} faces {faces:>3} shadows {shadows:>2} diff {m['diffVsLegacy']:5.1f}")

    with open(os.path.join(R, "Assets", "Icons", "icons.json"), "w", newline="\n") as f:
        json.dump({"source": "Assets/ComponentIcons/Icons.ai", "icons": dict(sorted(manifest.items()))}, f, indent=2)
        f.write("\n")
    write_slug_map(R, manifest)
    print(f"\n{len(manifest)} icons written.")
    for p in problems:
        print("NOTE:", p)
    if a.sheet:
        contact_sheet(R, manifest, legacy_dir, out_svg, a.sheet)


def write_slug_map(R, manifest):
    pairs = sorted((c, slug) for slug, m in manifest.items() for c in m["classes"])
    lines = [
        "// <auto-generated>",
        "// Generated by build/icons/build_icons.py from Assets/Icons/icons.json. Do not edit by hand.",
        "// </auto-generated>",
        "#nullable enable",
        "namespace Swiftlet.Runtime;",
        "",
        "/// <summary>Maps Grasshopper component/parameter class names to icon slugs in Assets/Icons.</summary>",
        "internal static class IconSlugs",
        "{",
        "    private static readonly System.Collections.Generic.Dictionary<string, string> Map = new(System.StringComparer.Ordinal)",
        "    {",
    ]
    lines += [f'        ["{c}"] = "{s}",' for c, s in pairs]
    lines += [
        "    };",
        "",
        "    /// <summary>Icon slug for a class name, or null when the class has no icon.</summary>",
        "    public static string? ForClass(string className) => Map.TryGetValue(className, out var slug) ? slug : null;",
        "",
        "    /// <summary>All class names with an icon.</summary>",
        "    public static System.Collections.Generic.IEnumerable<string> Classes => Map.Keys;",
        "}",
        "",
    ]
    path = os.path.join(R, "src", "Swiftlet.Runtime", "IconSlugs.g.cs")
    with open(path, "w", newline="\n", encoding="utf-8") as f:
        f.write("\n".join(lines))
    print("wrote", path, len(pairs), "classes")


def contact_sheet(R, manifest, legacy_dir, out_svg, path):
    from PIL import ImageDraw
    Z, cols = 48, 4
    cell_w = Z * 4 + 12
    items = sorted(manifest.items())
    rows = (len(items) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * cell_w, rows * (Z + 14) + 20), "white")
    d = ImageDraw.Draw(sheet)
    d.text((4, 4), "per icon: legacy PNG | new 24px | new master on light | new master on dark (all scaled to 48px)", fill="black")
    for k, (slug, m) in enumerate(items):
        x, y = (k % cols) * cell_w, 20 + (k // cols) * (Z + 14)
        svg = open(os.path.join(out_svg, slug + ".svg")).read()
        legacy = Image.open(os.path.join(legacy_dir, m["legacyPng"])).convert("RGBA").resize((Z, Z), Image.NEAREST)
        new24 = render_svg(svg, 24).resize((Z, Z), Image.NEAREST)
        big = render_svg(svg, Z)
        for j, (im, bg) in enumerate([(legacy, "white"), (new24, "white"), (big, "white"), (big, (40, 40, 40))]):
            b = Image.new("RGBA", (Z, Z), bg); b.alpha_composite(im)
            sheet.paste(b.convert("RGB"), (x + j * Z, y))
        d.text((x, y + Z), slug[:34], fill="black")
    sheet.save(path)
    print("sheet:", path)


if __name__ == "__main__":
    main()
