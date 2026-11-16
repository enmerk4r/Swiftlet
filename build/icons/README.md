# Icon pipeline

`Assets/ComponentIcons/Icons.ai` (Illustrator, saved PDF-compatible) is the design source. This
folder turns it into vector masters that Grasshopper 2 can load, plus 24 px PNGs for Grasshopper 1.

```
python -m venv .venv && .venv/Scripts/python -m pip install -r build/icons/requirements.txt
.venv/Scripts/python build/icons/build_icons.py --sheet icons-review.png
```

Outputs:

| Path | What |
|---|---|
| `Assets/Icons/svg/<slug>.svg` | Vector master per icon |
| `Assets/Icons/png24/<slug>.png` | 24 px PNG rendered from the master (GH1) |
| `Assets/Icons/icons.json` | slug → artboard number, legacy PNG, component/param classes (from `ShellIcons.cs`), conversion stats |

## Why the masters look the way they do

Grasshopper 2's `SvgIcon` (`Grasshopper2.UI.Icon.Vector.SvgIcon`) is a small, deliberate subset parser:
no `transform`, `clipPath`, `mask`, filters, gradients, group opacity or arc commands, and each
sub-path is filled on its own, so holes cannot be cut out. `convert.py` therefore writes a flat list of
`<path d="…" fill="#rrggbb">` elements in a `0 0 24 24` viewBox using only absolute `M L C Z`, and:

- converts text to outlines and splits any shape with holes (letter counters) into hole-free pieces;
- converts strokes to filled outlines;
- replaces Illustrator's blurred, masked shadow images with flat polygons coloured with the shade
  measured from the original artwork;
- draws parameter icons' hexagon (a placed bitmap in the `.ai`) as a faceted vector shape (`HEX`).

Fill colours are fixed hex values (not `gh:` theme roles), so the origami palette is preserved exactly.

## Checking in Grasshopper 2

`gh2_render_script.py` builds a `run_csharp` payload for the Rhino MCP server that loads each master
through GH2's own `SvgIcon.FromXml` and saves `DrawToBitmap` renders on light and dark backgrounds:

```
python build/icons/gh2_render_script.py Assets/Icons/svg <outdir> slug-a,slug-b,...
# then send <outdir>/render_args.json to the run_csharp tool of a Rhino 9 instance with GH2 started
```

## Adding or changing an icon

Edit `Icons.ai`, export the artboard's 24 px PNG into `Assets/ComponentIcons` as before, map it in
`ShellIcons.cs`, and rerun `build_icons.py`. Artboards are matched to PNGs by pixel comparison; add
an entry to `OVERRIDES` in `build_icons.py` if two artboards are near-identical.
