"""Build a run_csharp payload that renders SVG masters through GH2's own SvgIcon (via reflection).

Usage: python gh2_render_script.py <svg dir> <out dir> <comma-separated slugs>
Writes <out dir>/render_args.json; renders land in <out dir> as <slug>_<px>_<light|dark>.png.
"""
import json, sys, os
svgdir, outdir, names = sys.argv[1], sys.argv[2], sys.argv[3].split(",")
script = r'''
using System; using System.IO; using System.Linq; using System.Reflection;
using Eto.Drawing;
var gh2 = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Grasshopper2");
Console.WriteLine("GH2 " + gh2.GetName().Version + " " + gh2.Location);
var svgType = gh2.GetType("Grasshopper2.UI.Icon.Vector.SvgIcon", true);
var fromXml = svgType.GetMethod("FromXml", BindingFlags.Public | BindingFlags.Static, null, new[]{typeof(string)}, null);
var svgdir = @"%SVG%"; var outdir = @"%OUT%";
foreach (var n in new[]{%NAMES%}) {
  try {
    var xml = File.ReadAllText(Path.Combine(svgdir, n + ".svg"));
    var icon = fromXml.Invoke(null, new object[]{ xml });
    var draw = icon.GetType().GetMethod("DrawToBitmap", new[]{typeof(Size), typeof(int), typeof(Color)});
    foreach (var px in new[]{24, 96}) {
      foreach (var bg in new[]{("light", Colors.White), ("dark", Color.FromArgb(40,40,40))}) {
        var bmp = (Bitmap)draw.Invoke(icon, new object[]{ new Size(px, px), 0, bg.Item2 });
        bmp.Save(Path.Combine(outdir, $"{n}_{px}_{bg.Item1}.png"), ImageFormat.Png);
      }
    }
    Console.WriteLine("OK " + n + " " + icon.GetType().FullName);
  } catch (Exception e) { var x = e.InnerException ?? e; Console.WriteLine("FAIL " + n + " " + x.GetType().Name + ": " + x.Message); }
}
'''
script = script.replace("%SVG%", svgdir).replace("%OUT%", outdir).replace("%NAMES%", ",".join('"%s"' % n for n in names))
json.dump({"script": script}, open(os.path.join(outdir, "render_args.json"), "w"))
