"""Render one PNG per current Swiftlet component as it looks on the Grasshopper 2 canvas.

The wiki shows each component next to its Grasshopper 1 screenshot, so the files are written under the
same names as those (component-images/gh1/<n>_<name>.png -> component-images/gh2/<same>.png).

    python build/wiki/gh2_component_images.py --port PORT --wiki C:\\path\\to\\Swiftlet.wiki [--only NAME ...]

Needs a Rhino 9 slot (tests/Swiftlet.Cases/slot.py) with Swiftlet loaded, either installed as a package
or through `cases.py --load`. Each component is placed on the active GH2 canvas, drawn with
Canvas.DrawToBitmap (no screen capture), and removed again. Nothing is solved, so no component opens a
port, starts a download or launches a browser; that also means status labels that only appear after a
solve are not shown, matching the unsolved Grasshopper 1 screenshots.
"""
import argparse, json, pathlib, re, sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tests" / "Swiftlet.Cases"))
from cases import call, run_csharp  # noqa: E402

# Wiki file names that are not "<panel number>_<component name>".
SPECIAL = {"api_key": "1_auth_api_key_xs"}
PADDING = 40      # canvas units around the component
SCALE = 2         # output pixels per canvas unit

RENDER = r'''
using System; using System.Linq; using System.Collections;
var gh2 = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Grasshopper2");
dynamic editor = gh2.GetType("Grasshopper2.UI.Editor").GetProperty("Instance").GetValue(null);
dynamic canvas = editor.Canvas; dynamic doc = canvas.Document;
object target = null;
foreach (object o in (IEnumerable)doc.Objects.Forwards) if (((dynamic)o).InstanceId.ToString() == "__ID__") target = o;
if (target == null) throw new Exception("placed object not found");
dynamic bounds = ((dynamic)target).Attributes.Bounds;
float pad = __PAD__f;
var region = new Eto.Drawing.RectangleF((float)bounds.X - pad, (float)bounds.Y - pad, (float)bounds.Width + 2 * pad, (float)bounds.Height + 2 * pad);
var draw = ((object)canvas).GetType().GetMethods().First(m => m.Name == "DrawToBitmap" && m.GetParameters().Length == 6);
// region, width, height, drawBackground, drawWires, drawMessages
var bitmap = (Eto.Drawing.Bitmap)draw.Invoke((object)canvas, new object[] { region, (int)(region.Width * __SCALE__), (int)(region.Height * __SCALE__), false, false, false });
bitmap.Save(@"__PATH__", Eto.Drawing.ImageFormat.Png);
doc.Objects.Remove((dynamic)target);
Console.WriteLine(bitmap.Width + "x" + bitmap.Height + " left=" + ((IEnumerable)doc.Objects.Forwards).Cast<object>().Count());
'''


def slug(name):
    return re.sub(r"[^a-z0-9]+", "_", name.lower()).strip("_")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--port", type=int, required=True)
    ap.add_argument("--wiki", required=True, help="path to the Swiftlet.wiki checkout")
    ap.add_argument("--only", nargs="*", help="component names to render (default: all)")
    args = ap.parse_args()

    wiki = pathlib.Path(args.wiki)
    existing = {p.stem for p in (wiki / "component-images" / "gh1").glob("*.png")}
    by_key = {re.sub(r"_(xs|sm)$", "", re.sub(r"^\d+_", "", stem)): stem for stem in existing}
    out_dir = wiki / "component-images" / "gh2"
    out_dir.mkdir(parents=True, exist_ok=True)

    parity = json.loads((ROOT / "docs" / "parity.json").read_text(encoding="utf-8"))
    entries = [e for e in parity["entries"]
               if e["kind"] != "param" and not e["archived"] and (e.get("gh2") or {}).get("status") == "ported"]
    if args.only:
        wanted = {slug(n) for n in args.only}
        entries = [e for e in entries if slug(e["name"]) in wanted]

    call(args.port, "g2_start", {})
    failures, done = [], 0
    for entry in sorted(entries, key=lambda e: (e["panel"], e["name"])):
        key = slug(entry["name"])
        stem = SPECIAL.get(key) or by_key.get(key)
        if stem is None:
            failures.append(f"{entry['name']}: no matching wiki image name")
            continue
        try:
            _, text = call(args.port, "g2_place_component", {"selector": entry["gh2"]["ioId"], "x": 400, "y": 300})
            match = re.search(r"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", text)
            if not match:
                raise RuntimeError("place failed: " + text[:200])
            script = (RENDER.replace("__ID__", match.group(0)).replace("__PAD__", str(PADDING))
                      .replace("__SCALE__", str(SCALE)).replace("__PATH__", str(out_dir / (stem + ".png"))))
            result = run_csharp(args.port, script).strip()
            if not result.endswith("left=0"):
                raise RuntimeError("canvas not empty afterwards: " + result)
            done += 1
            print(f"ok   {stem}.png  {result.split()[0]}", flush=True)
        except Exception as error:  # keep going; report at the end
            failures.append(f"{entry['name']}: {error}")
            print(f"FAIL {entry['name']}: {error}", flush=True)

    print(f"\n{done} image(s) written to {out_dir}")
    if failures:
        print(f"{len(failures)} failure(s):")
        for failure in failures:
            print("  " + failure)
        sys.exit(1)


if __name__ == "__main__":
    main()
