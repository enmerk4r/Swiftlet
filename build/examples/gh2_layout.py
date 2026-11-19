"""Inspect, check, edit and render the layout of a Grasshopper 2 example file (.ghz).

    python build/examples/gh2_layout.py dump   --port PORT FILE.ghz [--json OUT.json]
    python build/examples/gh2_layout.py check  --port PORT FILE.ghz
    python build/examples/gh2_layout.py render --port PORT FILE.ghz OUT.png [--solve] [--region X,Y,W,H]
    python build/examples/gh2_layout.py apply  --port PORT FILE.ghz OPS.json [--out OTHER.ghz]
    python build/examples/gh2_layout.py thumbnail --port PORT FILE.ghz      redraw the preview stored in the file

Needs a Rhino 9 slot (tests/Swiftlet.Cases/slot.py) with Swiftlet loaded. GH2 would solve a file as soon
as it is shown on the canvas, so the tool switches solving off while it runs (and back afterwards). No
example sends a request or opens a port while its layout is being worked on, unless --solve is given.

The rules that `check` enforces are written down in Examples/LAYOUT.md. `apply` takes a JSON list of
operations (see OPERATIONS below), saves the file, reopens it and refuses the result if any component,
parameter or wire went missing. Text and groups may be added and removed freely.

OPERATIONS (ids are the 8-character ids that `dump` prints; several ops take "ids": [...])
    {"op": "name",    "id": GROUP, "text": ""}                    GH2's own group header (centred); the standard uses text titles instead
    {"op": "delete",  "id": SCRIBBLE}                             text objects only
    {"op": "text",    "id": SCRIBBLE, "text": "..."}
    {"op": "style",   "id": SCRIBBLE, "role": "heading"|"note"|"flag", "wrap": 220}
    {"op": "colour",  "id": SCRIBBLE, "colour": "Red"}           text colour: an OpenColor family (Red, Orange, ...); "Flat" is the skin default
    {"op": "disable", "ids": [...]} / {"op": "enable", "ids": [...]}   save components disabled or enabled
    {"op": "move",    "id": ANY, "left": X, "top": Y}             either coordinate may be omitted
    {"op": "moveby",  "ids": [...], "dx": 0, "dy": 0}
    {"op": "below",   "id": SCRIBBLE, "of": ANY, "gap": 10}       left edges aligned, under the object
    {"op": "above",   "id": SCRIBBLE, "of": ANY, "gap": 10}       left edges aligned, over the object
    {"op": "title_over",   "id": SCRIBBLE, "of": GROUP}           group title: left edge on the group's, TITLE_GAP above
    {"op": "heading_over", "id": SCRIBBLE, "of": GROUP}           column heading: same, HEADING_GAP above the top group
    {"op": "fit_values"}                                          widen every value box (or "ids") to show its whole text
    {"op": "size",    "id": VALUE, "width": 140}                  set a value box's width by hand
    {"op": "size",    "id": TEXTINPUT, "width": 220, "lines": 4}  a text input: width of its text area, rows shown
    {"op": "add",     "role": "heading"|"note"|"flag", "text": "...", "left": X, "top": Y, "group": GROUP, "key": "n1", "colour": "Red"}
    {"op": "ungroup", "group": GROUP, "id": ANY} / {"op": "group", "group": GROUP, "id": ANY}
    {"op": "new_group", "ids": [...], "key": "g1", "colour": "Teal"}   a new group around those objects
    {"op": "delete_group", "id": GROUP}                           removes the group and keeps its members
    {"op": "label",   "id": OBJECT, "text": "Bake"}               the name GH2 shows under a component or parameter
An "add" or "new_group" with a "key" can be referred to by later ops as "@n1" / "@g1".
Anywhere an id is expected, "name:Some name" selects the one object whose user-given name is exactly that.
"""
import argparse, base64, json, pathlib, sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tests" / "Swiftlet.Cases"))
from cases import call, run_csharp  # noqa: E402

# ---- the layout standard, in canvas units (see Examples/LAYOUT.md) ----
TITLE_GAP = 8           # bottom of a group's title to the top edge of the group
HEADING_GAP = 56        # bottom of a column heading to the top edge of the first group under it
NOTE_GAP = (6, 18)      # allowed distance between a note and the object it is attached to
ALIGN_TOLERANCE = 1.0
MIN_GAP = 6             # closest two objects in a group may sit
GROUP_GAP = 16          # closest two groups may sit
# GH2 draws a component's or parameter's user-given name centred under it. That label is not part of the
# object's bounds, so its extent is estimated from the text.
LABEL_CHAR, LABEL_HEIGHT, LABEL_GAP = 6.6, 16.0, 3.0

CORE = r'''
using System; using System.Linq; using System.Collections; using System.Collections.Generic; using System.Reflection; using System.Text.Json; using System.Text.Json.Nodes;
var gh = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Grasshopper2");
dynamic editor = gh.GetType("Grasshopper2.UI.Editor").GetProperty("Instance").GetValue(null);
dynamic canvas = editor.Canvas;
var ioT = gh.GetType("Grasshopper2.Doc.DocumentIO"); var docT = gh.GetType("Grasshopper2.Doc.Document");
var scribbleT = gh.GetType("Grasshopper2.SpecialObjects.ScribbleObject"); var groupT = gh.GetType("Grasshopper2.SpecialObjects.GroupObject");
var fontT = gh.GetType("Grasshopper2.SpecialObjects.ScribbleFont"); var alignT = gh.GetType("Grasshopper2.SpecialObjects.ScribbleAlignment");
var openM = ioT.GetMethods().Single(m => m.Name == "Open" && m.GetParameters().Length == 3 && m.GetParameters()[0].ParameterType == typeof(string));
var drawM = ((object)canvas).GetType().GetMethods().First(m => m.Name == "DrawToBitmap" && m.GetParameters().Length == 6);

object Open(string path) {
  object blank = docT.GetMethod("NewInactiveDocument").Invoke(null, null);
  dynamic reader = Activator.CreateInstance(ioT, new object[] { blank, false, false, false, false });
  if (!(bool)openM.Invoke(reader, new object[] { path, Array.CreateInstance(docT, 0), null })) throw new Exception("could not open " + path);
  return reader.Document; }
List<object> All(object d) => ((IEnumerable)((dynamic)d).Objects.Forwards).Cast<object>().ToList();
bool IsScribble(object o) => scribbleT.IsInstanceOfType(o);
bool IsGroup(object o) => groupT.IsInstanceOfType(o);
string Id(object o) => ((Guid)((dynamic)o).InstanceId).ToString();
IEnumerable<object> Pins(object o, bool inputs) {
  var ps = o.GetType().GetProperty("Parameters");
  if (ps != null) { dynamic p = ps.GetValue(o); foreach (object x in (IEnumerable)(inputs ? p.Inputs : p.Outputs)) yield return x; }
  else if (o.GetType().GetProperty("Inputs") != null && !IsGroup(o) && !IsScribble(o)) yield return o; }
int Wires(object d) { int n = 0; foreach (var o in All(d)) foreach (dynamic p in Pins(o, true)) n += (int)p.Inputs.Count; return n; }
// Bounds are computed lazily and groups cache theirs, so invalidate everything and paint the whole canvas once.
void Show(object d) {
  canvas.Document = (dynamic)d;
  foreach (dynamic o in All(d)) { try { o.Attributes.InvalidateLayout(); } catch { } }
  drawM.Invoke((object)canvas, new object[] { new Eto.Drawing.RectangleF(-20000, -20000, 80000, 80000), 64, 64, false, false, false }); }
Eto.Drawing.RectangleF Extent(object d) {
  float x0 = 1e9f, y0 = 1e9f, x1 = -1e9f, y1 = -1e9f;
  foreach (dynamic o in All(d)) { Eto.Drawing.RectangleF b = o.Attributes.Bounds; x0 = Math.Min(x0, b.Left); y0 = Math.Min(y0, b.Top); x1 = Math.Max(x1, b.Right); y1 = Math.Max(y1, b.Bottom); }
  return new Eto.Drawing.RectangleF(x0, y0, x1 - x0, y1 - y0); }
bool HasLabel(object o) { if (IsGroup(o) || IsScribble(o)) return false; string n = o.GetType().Name; if (!(n.EndsWith("Component") || n.EndsWith("Parameter") || o.GetType().GetProperty("Parameters") != null)) return false;
  return !string.IsNullOrEmpty((string)((dynamic)o).UserName); }
// Bounds plus the name label drawn under the object, when it has one.
Eto.Drawing.RectangleF Full(object o) {
  Eto.Drawing.RectangleF b = ((dynamic)o).Attributes.Bounds; if (!HasLabel(o)) return b;
  float w = __LABEL_CHAR__f * ((string)((dynamic)o).UserName).Length, cx = b.Left + b.Width / 2;
  float x0 = Math.Min(b.Left, cx - w / 2), x1 = Math.Max(b.Right, cx + w / 2);
  return new Eto.Drawing.RectangleF(x0, b.Top, x1 - x0, b.Height + __LABEL_GAP__f + __LABEL_HEIGHT__f); }
JsonArray Rect(Eto.Drawing.RectangleF b) => new JsonArray(Math.Round(b.X, 2), Math.Round(b.Y, 2), Math.Round(b.Width, 2), Math.Round(b.Height, 2));
JsonArray Pt(Eto.Drawing.PointF p) => new JsonArray(Math.Round(p.X, 2), Math.Round(p.Y, 2));

// Components expose pin positions; free-floating parameters and special objects only have bounds.
Eto.Drawing.PointF Grip(object pin, bool inlet) {
  object att = ((dynamic)pin).Attributes; var prop = att.GetType().GetProperty(inlet ? "Inlet" : "Outlet");
  if (prop != null) return (Eto.Drawing.PointF)prop.GetValue(att);
  Eto.Drawing.RectangleF b = ((dynamic)att).Bounds; return new Eto.Drawing.PointF(inlet ? b.Left : b.Right, b.Top + b.Height / 2); }
// The 500 x 300 preview stored inside a .ghz. GH2 writes whatever the canvas view happens to show, which is
// blank for a file saved from a script, so the tool draws the whole graph and stores that instead.
void Thumbnail(object d, string png) {
  Show(d); var e = Extent(d); float pad = 40; float w = e.Width + 2 * pad, h = e.Height + 2 * pad;
  if (w / h < 500f / 300f) w = h * 500f / 300f; else h = w * 300f / 500f;
  var region = new Eto.Drawing.RectangleF(e.X + e.Width / 2 - w / 2, e.Y + e.Height / 2 - h / 2, w, h);
  var bmp = (Eto.Drawing.Bitmap)drawM.Invoke((object)canvas, new object[] { region, 500, 300, true, true, false });
  bmp.Save(png, Eto.Drawing.ImageFormat.Png); }
JsonObject Dump(object d) {
  var all = All(d); var arr = new JsonArray(); var owner = new Dictionary<object, object>();
  foreach (var o in all) { foreach (var p in Pins(o, true)) owner[p] = o; foreach (var p in Pins(o, false)) owner[p] = o; }
  foreach (var o in all) {
    dynamic x = o; var j = new JsonObject { ["id"] = Id(o), ["type"] = o.GetType().Name, ["name"] = (string)(x.UserName ?? "") };
    try { if (string.IsNullOrEmpty((string)j["name"])) j["name"] = (string)x.Nomen.Name; } catch { }
    j["kind"] = IsGroup(o) ? "group" : IsScribble(o) ? "text" : "object";
    j["bounds"] = Rect((Eto.Drawing.RectangleF)x.Attributes.Bounds); if (HasLabel(o)) j["extent"] = Rect(Full(o)); j["pivot"] = Pt((Eto.Drawing.PointF)x.Attributes.Pivot);
    if (o.GetType().Name == "ValueObject") j["value"] = (string)x.Text;
    if (IsScribble(o)) { j["text"] = (string)x.Text; j["font"] = x.TextFont.ToString(); j["style"] = x.TextStyle.ToString(); j["align"] = x.TextAlign.ToString(); j["wrap"] = (float)x.WrapWidth; j["colour"] = x.TextColour.ToString(); }
    else if (!IsGroup(o)) { try { j["enabled"] = x.Activity.ToString() != "Disabled"; } catch { } }
    if (IsGroup(o)) { var c = new JsonArray(); foreach (Guid g in (IEnumerable<Guid>)x.ContentIds) c.Add(g.ToString()); j["content"] = c; j["colour"] = x.GroupColour.ToString(); }
    var wires = new JsonArray();
    foreach (dynamic pin in Pins(o, true)) {
      Eto.Drawing.PointF inlet = Grip((object)pin, true);
      foreach (Guid src in (Guid[])pin.Inputs.ToArray()) {
        object sp = ((dynamic)d).Objects.FindParameter(src);
        if (sp == null) continue;
        Eto.Drawing.PointF outlet = Grip(sp, false);
        wires.Add(new JsonObject { ["from"] = Pt(outlet), ["to"] = Pt(inlet), ["source"] = owner.TryGetValue(sp, out var so) ? Id(so) : Id(sp) }); } }
    j["wires"] = wires; arr.Add(j); }
  return new JsonObject { ["objects"] = arr, ["wireCount"] = Wires(d) }; }
'''

SCRIPT = CORE + r'''
string path = @"__PATH__"; string mode = "__MODE__";
// GH2 solves a document as soon as it becomes the canvas document, which would send the example's requests
// and open its ports. Solving is a process-wide switch: turn it off for the duration and restore it after.
var solving = gh.GetType("Grasshopper2.Doc.SolutionServer").GetProperty("EnableSolutions");
bool solvingBefore = (bool)solving.GetValue(null);
solving.SetValue(null, false);
try {
object doc = Open(path); Show(doc);
if (mode == "dump") { Console.WriteLine("JSON:" + Dump(doc).ToJsonString()); }
else if (mode == "thumbnail") { Thumbnail(doc, @"__OUT__"); Console.WriteLine("thumbnail drawn"); }
else if (mode == "render") {
  if (__SOLVE__) { solving.SetValue(null, true); ((dynamic)doc).Solution.StartWait(); solving.SetValue(null, false); Show(doc); }
  var e = Extent(doc); float pad = 60; var region = new Eto.Drawing.RectangleF(e.X - pad, e.Y - pad, e.Width + 2 * pad, e.Height + 2 * pad);
  float[] only = new float[] { __REGION__ }; if (only.Length == 4) region = new Eto.Drawing.RectangleF(only[0], only[1], only[2], only[3]);
  float scale = Math.Min(2f, 4000f / region.Width);
  var bmp = (Eto.Drawing.Bitmap)drawM.Invoke((object)canvas, new object[] { region, (int)(region.Width * scale), (int)(region.Height * scale), false, true, false });
  var page = new Eto.Drawing.Bitmap(bmp.Width, bmp.Height, Eto.Drawing.PixelFormat.Format32bppRgba);
  using (var g = new Eto.Drawing.Graphics(page)) { g.Clear(Eto.Drawing.Colors.White); g.DrawImage(bmp, 0, 0); }
  page.Save(@"__OUT__", Eto.Drawing.ImageFormat.Png); Console.WriteLine("rendered " + bmp.Width + "x" + bmp.Height); }
else if (mode == "apply") {
  // Text and groups may be added or removed; components, parameters and wires may not.
  bool Counts(object o) => !IsScribble(o) && !IsGroup(o);
  int objectsBefore = All(doc).Count(Counts), wiresBefore = Wires(doc);
  var ops = JsonNode.Parse(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("__OPS__"))).AsArray();
  var keyed = new Dictionary<string, object>();
  object Find(string id) {
    if (id.StartsWith("@")) return keyed[id.Substring(1)];
    if (id.StartsWith("name:")) { var named = All(doc).Where(o => (string)((dynamic)o).UserName == id.Substring(5)).ToList();
      if (named.Count != 1) throw new Exception("name '" + id.Substring(5) + "' matches " + named.Count + " objects"); return named[0]; }
    var hits = All(doc).Where(o => Id(o).StartsWith(id, StringComparison.OrdinalIgnoreCase)).ToList();
    if (hits.Count != 1) throw new Exception("id '" + id + "' matches " + hits.Count + " objects"); return hits[0]; }
  // A group caches its outline and does not refresh it while its members are moved from a script, so the
  // outline is worked out the way GH2 draws it: the union of the members' bounds plus a margin. Name labels
  // under members are not part of it, which is why a long label can stick out of its group.
  Eto.Drawing.RectangleF B(object o) {
    if (!IsGroup(o)) return (Eto.Drawing.RectangleF)((dynamic)o).Attributes.Bounds;
    float x0 = 1e9f, y0 = 1e9f, x1 = -1e9f, y1 = -1e9f;
    foreach (object member in (IEnumerable)((dynamic)o).Content) { if (member == null || IsGroup(member)) continue; Eto.Drawing.RectangleF b = ((dynamic)member).Attributes.Bounds;
      x0 = Math.Min(x0, b.Left); y0 = Math.Min(y0, b.Top); x1 = Math.Max(x1, b.Right); y1 = Math.Max(y1, b.Bottom); }
    const float margin = 14f; return new Eto.Drawing.RectangleF(x0 - margin, y0 - margin, x1 - x0 + 2 * margin, y1 - y0 + 2 * margin); }
  void MoveBy(object o, float dx, float dy) { dynamic a = ((dynamic)o).Attributes; Eto.Drawing.PointF p = a.Pivot; a.Pivot = new Eto.Drawing.PointF(p.X + dx, p.Y + dy); try { a.InvalidateLayout(); } catch { } }
  void Relayout() { Show(doc); }
  void MoveTo(object o, float? left, float? top) { Relayout(); var b = B(o); MoveBy(o, left.HasValue ? left.Value - b.Left : 0, top.HasValue ? top.Value - b.Top : 0); Relayout(); }
  void Style(object o, string role, float? wrap) {
    scribbleT.GetProperty("TextFont").SetValue(o, Enum.Parse(fontT, "Sans"));
    scribbleT.GetProperty("TextStyle").SetValue(o, Enum.Parse(typeof(Eto.Drawing.FontStyle), role == "note" ? "Italic" : "Bold"));
    scribbleT.GetProperty("TextAlign").SetValue(o, Enum.Parse(alignT, "Left"));
    ((dynamic)o).TextAngle = 0; if (wrap.HasValue) ((dynamic)o).WrapWidth = wrap.Value; }
  // Objects snapped onto a component (a toggle on an input, say) travel with it when it moves, so every
  // placement is replayed once after everything has moved.
  void Colour(object o, string family) {
    var c = scribbleT.GetProperty("TextColour"); c.SetValue(o, Enum.Parse(c.PropertyType, family)); }
  var absolute = new HashSet<string> { "move", "moveby", "below", "above", "heading_over", "title_over" };
  var destinations = new Dictionary<JsonNode, List<(object, float, float)>>();
  var plan = ops.Select(n => n).ToList(); plan.AddRange(ops.Where(n => absolute.Contains((string)n["op"])).ToList());
  int index = 0;
  foreach (var node in plan) {
    bool replay = index++ >= ops.Count;
    var op = node.AsObject(); string kind = (string)op["op"];
    float? F(string k) => op[k] == null ? (float?)null : (float)op[k].GetValue<double>();
    var targets = op["ids"] != null ? op["ids"].AsArray().Select(i => Find((string)i)).ToList() : op["id"] != null ? new List<object> { Find((string)op["id"]) } : new List<object>();
    switch (kind) {
      case "name": foreach (dynamic g in targets) { if (!IsGroup(g)) throw new Exception("name: not a group"); g.UserName = (string)op["text"]; } break;
      case "delete": if (replay) break; foreach (var t in targets) { if (!IsScribble(t)) throw new Exception("delete: only text objects may be deleted"); ((dynamic)doc).Objects.Remove((dynamic)t); } break;
      case "text": foreach (dynamic t in targets) t.Text = (string)op["text"]; break;
      case "style": foreach (var t in targets) Style(t, (string)op["role"], F("wrap")); Relayout(); break;
      case "colour": foreach (var t in targets) { if (!IsScribble(t)) throw new Exception("colour: text objects only"); Colour(t, (string)op["colour"]); } Relayout(); break;
      case "disable": case "enable": foreach (dynamic t in targets) { if (IsGroup(t) || IsScribble(t)) throw new Exception(kind + ": components and parameters only");
          var act = ((object)t).GetType().GetProperty("Activity"); act.SetValue(t, Enum.Parse(act.PropertyType, kind == "disable" ? "Disabled" : "Enabled")); } break;
      case "move": foreach (var t in targets) MoveTo(t, F("left"), F("top")); break;
      case "moveby": {
        // Destinations are fixed when the op is first met, so the replay puts snapped children back where they belong.
        if (!destinations.TryGetValue(node, out var where)) { Relayout(); where = targets.Select(t => { var b = B(t); return (t, b.Left + (F("dx") ?? 0), b.Top + (F("dy") ?? 0)); }).ToList(); destinations[node] = where; }
        foreach (var (t, left, top) in where) MoveTo(t, left, top); break; }
      case "below": case "above": { Relayout(); var of = Find((string)op["of"]); var anchor = B(of); float gap = F("gap") ?? 10;
        float under = IsGroup(of) ? anchor.Bottom : Full(of).Bottom;   // clear of the name label, if any
        foreach (var t in targets) { var b = B(t); MoveTo(t, anchor.Left, kind == "below" ? under + gap : anchor.Top - gap - b.Height); } break; }
      case "title_over": case "heading_over": { Relayout(); var anchor = B(Find((string)op["of"])); float gap = kind == "title_over" ? __TITLE_GAP__ : __HEADING_GAP__;
        foreach (var t in targets) { var b = B(t); MoveTo(t, anchor.Left, anchor.Top - gap - b.Height); } break; }
      case "fit_values": {
        // Value boxes cut their text off when it is longer than the box. Make each wide enough for its text,
        // keeping the left edge where it was.
        var boxes = targets.Count > 0 ? targets : All(doc).Where(o => o.GetType().Name == "ValueObject").ToList();
        foreach (var t in boxes) { if (t.GetType().Name != "ValueObject") continue; Relayout(); var before = B(t); dynamic a = ((dynamic)t).Attributes; Eto.Drawing.SizeF z = a.Size;
          float width = Math.Max(60f, 8f * ((string)((dynamic)t).Text ?? "").Length + 34f);
          if (width > z.Width + 0.5f) { a.Size = new Eto.Drawing.SizeF(width, z.Height); MoveTo(t, before.Left, before.Top); } }
        break; }
      case "size": foreach (var t in targets) { Relayout(); var before = B(t); object att = ((dynamic)t).Attributes;
          PropertyInfo Deep(string name) { for (var c = att.GetType(); c != null; c = c.BaseType) { var found = c.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly); if (found != null) return found; } return null; }
          var hosting = Deep("HostingWidth"); var units = Deep("VerticalUnits");
          if (hosting != null && units != null) {
            // text inputs and similar: "width" is the width of the text area, "lines" its height in rows
            if (F("width").HasValue) hosting.SetValue(att, F("width").Value);
            if (op["lines"] != null) units.SetValue(att, (int)op["lines"].GetValue<double>()); }
          else { dynamic a = att; Eto.Drawing.SizeF z = a.Size; a.Size = new Eto.Drawing.SizeF(F("width") ?? z.Width, F("height") ?? z.Height); }
          MoveTo(t, before.Left, before.Top); } break;
      case "add": { object n = Activator.CreateInstance(scribbleT); ((dynamic)n).Text = (string)op["text"]; Style(n, (string)op["role"], F("wrap"));
        if (op["colour"] != null) Colour(n, (string)op["colour"]);
        ((dynamic)doc).Objects.Add((dynamic)n, new Eto.Drawing.PointF(F("left") ?? 0, F("top") ?? 0)); MoveTo(n, F("left"), F("top"));
        if (op["group"] != null) ((dynamic)Find((string)op["group"])).AddContent(((dynamic)n).InstanceId);
        if (op["key"] != null) keyed[(string)op["key"]] = n; break; }
      case "new_group": { if (replay) break; object g = Activator.CreateInstance(groupT);
        foreach (var t in targets) ((dynamic)g).AddContent(((dynamic)t).InstanceId);
        ((dynamic)doc).Objects.Add((dynamic)g, new Eto.Drawing.PointF(0, 0));
        if (op["colour"] != null) groupT.GetProperty("GroupColour").SetValue(g, Enum.Parse(groupT.GetProperty("GroupColour").PropertyType, (string)op["colour"]));
        if (op["key"] != null) keyed[(string)op["key"]] = g; break; }
      case "label": foreach (dynamic t in targets) { if (IsGroup(t) || IsScribble(t)) throw new Exception("label: use name/text for groups and text"); t.UserName = (string)op["text"]; } Relayout(); break;
      case "delete_group": if (replay) break; foreach (var t in targets) { if (!IsGroup(t)) throw new Exception("delete_group: not a group");
        foreach (Guid member in ((IEnumerable<Guid>)((dynamic)t).ContentIds).ToArray()) ((dynamic)t).RemoveContent(member);
        ((dynamic)doc).Objects.Remove((dynamic)t); } break;
      case "group": ((dynamic)Find((string)op["group"])).AddContent(((dynamic)targets[0]).InstanceId); break;
      case "ungroup": ((dynamic)Find((string)op["group"])).RemoveContent(((dynamic)targets[0]).InstanceId); break;
      default: throw new Exception("unknown op " + kind); } }
  Relayout();
  if (All(doc).Count(Counts) != objectsBefore || Wires(doc) != wiresBefore) throw new Exception("objects or wires changed; nothing saved");
  dynamic io = Activator.CreateInstance(ioT, new object[] { doc, false, false, false, false });
  ioT.GetMethod("Save", new[] { typeof(string), gh.GetType("Grasshopper2.Doc.FileContents") }).Invoke((object)io, new object[] { @"__OUT__", Enum.Parse(gh.GetType("Grasshopper2.Doc.FileContents"), "All") });
  object back = Open(@"__OUT__");
  if (All(back).Count(Counts) != objectsBefore || Wires(back) != wiresBefore) throw new Exception("saved file lost objects or wires: " + All(back).Count(Counts) + " objects, " + Wires(back) + " wires");
  Thumbnail(back, @"__OUT__" + ".canvas.png");
  Console.WriteLine("saved " + objectsBefore + " objects, " + wiresBefore + " wires, " + All(back).Count(IsGroup) + " groups, " + All(back).Count(IsScribble) + " text objects"); }
} finally {
  // leave the canvas on an empty document so a later tool call does not act on the example
  canvas.Document = (dynamic)docT.GetMethod("NewInactiveDocument").Invoke(null, null);
  solving.SetValue(null, solvingBefore);
}
'''


def run(port, mode, path, out="", ops=None, solve=False, region=""):
    script = (SCRIPT.replace("__PATH__", str(pathlib.Path(path).resolve())).replace("__MODE__", mode)
              .replace("__OUT__", str(pathlib.Path(out).resolve()) if out else "")
              .replace("__OPS__", base64.b64encode(json.dumps(ops or []).encode()).decode())
              .replace("__SOLVE__", "true" if solve else "false").replace("__REGION__", ", ".join(f"{float(v)}f" for v in region.split(",")) if region else "").replace("__HEADING_GAP__", str(HEADING_GAP)).replace("__TITLE_GAP__", str(TITLE_GAP))
              .replace("__LABEL_CHAR__", str(LABEL_CHAR)).replace("__LABEL_GAP__", str(LABEL_GAP)).replace("__LABEL_HEIGHT__", str(LABEL_HEIGHT)))
    call(port, "g2_start", {})
    return run_csharp(port, script)


def store_thumbnail(ghz, png):
    """Replace canvas.png inside the .ghz with the given image, leaving every other entry byte for byte."""
    import os, zipfile
    ghz, png = pathlib.Path(ghz), pathlib.Path(png)
    image = png.read_bytes()
    temp = ghz.with_suffix(".ghz.tmp")
    with zipfile.ZipFile(ghz) as source, zipfile.ZipFile(temp, "w") as target:
        for item in source.infolist():
            data = image if item.filename == "canvas.png" else source.read(item.filename)
            target.writestr(item, data, compress_type=item.compress_type)
        if "canvas.png" not in source.namelist():
            target.writestr("canvas.png", image, compress_type=zipfile.ZIP_DEFLATED)
    os.replace(temp, ghz)
    png.unlink()


def dump(port, path):
    text = run(port, "dump", path)
    line = next(l for l in text.splitlines() if l.startswith("JSON:"))
    return json.loads(line[5:])


# ---------------------------------------------------------------- checks
def body(o):
    x, y, w, h = o["bounds"]
    return x, y, x + w, y + h


def rect(o):
    """What the object occupies on the canvas: its bounds plus the name label under it, if it has one."""
    x, y, w, h = o.get("extent") or o["bounds"]
    return x, y, x + w, y + h


def intersects(a, b, slack=0.0):
    return a[0] < b[2] - slack and b[0] < a[2] - slack and a[1] < b[3] - slack and b[1] < a[3] - slack


def contains(outer, inner):
    return outer[0] <= inner[0] and outer[1] <= inner[1] and outer[2] >= inner[2] and outer[3] >= inner[3]


def gap(a, b):
    dx = max(a[0] - b[2], b[0] - a[2], 0)
    dy = max(a[1] - b[3], b[1] - a[3], 0)
    return max(dx, dy) if (dx == 0 or dy == 0) else (dx * dx + dy * dy) ** 0.5


def wire_points(w, n=40):
    (x0, y0), (x1, y1) = w["from"], w["to"]
    d = max(abs(x1 - x0) * 0.5, 20)
    pts = []
    for i in range(n + 1):
        t = i / n
        a, b, c, e = (1 - t) ** 3, 3 * (1 - t) ** 2 * t, 3 * (1 - t) * t * t, t ** 3
        pts.append((a * x0 + b * (x0 + d) + c * (x1 - d) + e * x1, a * y0 + b * y0 + c * y1 + e * y1))
    return pts


def label(o):
    text = o.get("text") or o.get("name") or o["type"]
    return f'{o["id"][:8]} "{text.splitlines()[0][:40] if text else ""}"'


def is_flag(t):
    """A bold note in a colour of its own, such as the red ENABLE ME! under a disabled request."""
    return t["style"] == "Bold" and t.get("colour", "Flat") != "Flat"


def check(data):
    objs = data["objects"]
    by_id = {o["id"]: o for o in objs}
    groups = [o for o in objs if o["kind"] == "group"]
    texts = [o for o in objs if o["kind"] == "text"]
    things = [o for o in objs if o["kind"] == "object"]
    member = {}
    for g in groups:
        for cid in g["content"]:
            member.setdefault(cid, []).append(g)
    problems = []

    titled = set()
    for i, a in enumerate(groups):
        for b in groups[i + 1:]:
            ra, rb = rect(a), rect(b)
            if contains(ra, rb) or contains(rb, ra):
                continue
            if intersects(ra, rb):
                problems.append(f"groups overlap: {label(a)} and {label(b)}")
            elif gap(ra, rb) < GROUP_GAP:
                problems.append(f"groups closer than {GROUP_GAP}: {label(a)} and {label(b)} ({gap(ra, rb):.0f})")

    solid = things + texts
    for i, a in enumerate(solid):
        for b in solid[i + 1:]:
            ra, rb = rect(a), rect(b)
            if intersects(ra, rb, 0.5):
                problems.append(f"overlap: {label(a)} and {label(b)}")
            elif a["kind"] == "object" and b["kind"] == "object" and gap(ra, rb) < MIN_GAP:
                problems.append(f"closer than {MIN_GAP}: {label(a)} and {label(b)} ({gap(ra, rb):.1f})")

    for t in texts:
        rt = rect(t)
        inside = [g for g in groups if contains(rect(g), rt)]
        if t["font"] != "Sans" or t["align"] != "Left" or t.get("angle", 0):
            problems.append(f"text {label(t)} must be Sans and left-aligned (is {t['font']}, {t['align']})")
        for g in groups:
            if not contains(rect(g), rt) and intersects(rect(g), rt):
                problems.append(f"text {label(t)} straddles the edge of group {label(g)}")
        if inside and is_flag(t):
            # a flag: a bold coloured line directly under a component saved disabled, telling the reader to enable it
            if t["id"] not in member:
                problems.append(f"flag {label(t)} lies in a group it does not belong to; add it to the group")
            under = [o for o in things if abs(rt[0] - body(o)[0]) <= ALIGN_TOLERANCE and NOTE_GAP[0] <= rt[1] - rect(o)[3] <= NOTE_GAP[1]]
            if not any(o.get("enabled") is False for o in under):
                problems.append(f"flag {label(t)} is not directly under a disabled component (left edges aligned, {NOTE_GAP[0]}-{NOTE_GAP[1]} below it)")
        elif inside:
            if t["style"] != "Italic":
                problems.append(f"note {label(t)} must be italic (is {t['style']})")
            if t["id"] not in member:
                problems.append(f"note {label(t)} lies in a group it does not belong to; add it to the group")
            anchors = [o for o in things if any(g in inside for g in member.get(o["id"], []))]
            ok = False
            for o in anchors:
                ro = rect(o)
                aligned = abs(rt[0] - body(o)[0]) <= ALIGN_TOLERANCE
                below = NOTE_GAP[0] <= rt[1] - ro[3] <= NOTE_GAP[1]
                above = NOTE_GAP[0] <= ro[1] - rt[3] <= NOTE_GAP[1]
                if aligned and (below or above):
                    ok = True
                    break
            if not ok:
                problems.append(f"note {label(t)} is not attached to an object (left edges aligned, {NOTE_GAP[0]}-{NOTE_GAP[1]} above or below it)")
        else:
            # text outside every group is a group title (just above its group) or a column heading (above the title)
            if t["style"] != "Bold":
                problems.append(f"title {label(t)} must be bold (is {t['style']})")
            if "\n" in t["text"].strip() or t["bounds"][3] > 30:
                problems.append(f"title {label(t)} must be a single line")
            under = [g for g in groups if rect(g)[1] >= rt[3] - 1 and rect(g)[0] <= rt[2] and rect(g)[2] >= rt[0]]
            if not under:
                problems.append(f"title {label(t)} has no group under it")
            else:
                g = min(under, key=lambda g: rect(g)[1])
                rg = rect(g)
                distance = rg[1] - rt[3]
                if abs(rg[0] - rt[0]) > ALIGN_TOLERANCE:
                    problems.append(f"title {label(t)} is not flush with the left edge of {label(g)} (off by {rt[0] - rg[0]:.1f})")
                if abs(distance - TITLE_GAP) <= ALIGN_TOLERANCE:
                    if g["id"] in titled:
                        problems.append(f"group {label(g)} has two titles")
                    titled.add(g["id"])
                elif abs(distance - HEADING_GAP) <= ALIGN_TOLERANCE:
                    if t["text"] != t["text"].upper():
                        problems.append(f"column heading {label(t)} must be upper case")
                else:
                    problems.append(f"title {label(t)} is {distance:.1f} above {label(g)}; a group title sits {TITLE_GAP} above, a column heading {HEADING_GAP}")
    flags = [t for t in texts if is_flag(t)]
    for o in things:
        if o.get("enabled") is False and not any(
                abs(rect(t)[0] - body(o)[0]) <= ALIGN_TOLERANCE and NOTE_GAP[0] <= rect(t)[1] - rect(o)[3] <= NOTE_GAP[1] for t in flags):
            problems.append(f"{label(o)} is saved disabled but has no flag under it telling the reader to enable it")
        holders = [g for g in groups if contains(rect(g), body(o))]
        if not holders:
            problems.append(f"object {label(o)} is not in a group")
        elif not any(contains(rect(g), rect(o)) for g in holders):
            problems.append(f"the name label under {label(o)} sticks out of its group: shorten the name or move the object inwards")
    for g in groups:
        if g["id"] not in titled:
            problems.append(f"group {label(g)} has no title (bold text, flush left, {TITLE_GAP} above the group)")
        if g["name"].strip() and g["name"].strip() != "Group":
            problems.append(f"group {label(g)} uses the built-in header as well; clear its name")

    for o in objs:
        for w in o["wires"]:
            if w["to"][0] < w["from"][0] - 1:
                problems.append(f"wire runs right to left into {label(o)} from {label(by_id.get(w['source'], o))}")
            pts = wire_points(w)
            for other in solid:
                if other["id"] in (o["id"], w["source"]):
                    continue
                r = rect(other)
                if any(r[0] + 1 < x < r[2] - 1 and r[1] + 1 < y < r[3] - 1 for x, y in pts):
                    what = "text" if other["kind"] == "text" else "object"
                    problems.append(f"wire into {label(o)} crosses {what} {label(other)}")
    return sorted(set(problems))


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("mode", choices=["dump", "check", "render", "apply", "thumbnail"])
    ap.add_argument("--port", type=int, required=True)
    ap.add_argument("file")
    ap.add_argument("extra", nargs="?")
    ap.add_argument("--json")
    ap.add_argument("--out")
    ap.add_argument("--solve", action="store_true")
    ap.add_argument("--region", help="render only this canvas rectangle: x,y,width,height")
    a = ap.parse_args()

    if a.mode == "render":
        print(run(a.port, "render", a.file, out=a.extra, solve=a.solve, region=a.region).strip())
        return
    if a.mode == "apply":
        ops = json.loads(pathlib.Path(a.extra).read_text(encoding="utf-8"))
        out = a.out or a.file
        print(run(a.port, "apply", a.file, out=out, ops=ops).strip())
        store_thumbnail(out, str(pathlib.Path(out).resolve()) + ".canvas.png")
        return
    if a.mode == "thumbnail":
        png = str(pathlib.Path(a.file).resolve()) + ".canvas.png"
        print(run(a.port, "thumbnail", a.file, out=png).strip())
        store_thumbnail(a.file, png)
        return
    data = dump(a.port, a.file)
    if a.json:
        pathlib.Path(a.json).write_text(json.dumps(data, indent=1), encoding="utf-8")
    if a.mode == "dump":
        for o in sorted(data["objects"], key=lambda o: (o["kind"], o["bounds"][0], o["bounds"][1])):
            x, y, w, h = o["bounds"]
            extra = f' {o["font"]}/{o["style"]}/{o["align"]}' if o["kind"] == "text" else (f' [{len(o["content"])} members]' if o["kind"] == "group" else "")
            print(f'{o["kind"]:6} {o["id"][:8]} x={x:8.1f} y={y:8.1f} w={w:7.1f} h={h:6.1f} {o["type"]:28} {json.dumps((o.get("text") or o["name"])[:60])}{extra}')
        print(f'{len(data["objects"])} objects, {data["wireCount"]} wires')
        return
    problems = check(data)
    for p in problems:
        print("  " + p)
    print(f"{len(problems)} layout problem(s) in {a.file}")
    sys.exit(1 if problems else 0)


if __name__ == "__main__":
    main()
