"""Rebuild server_demo.ghz with native GH2 geometry and verify both HTTP routes.
Run against an exclusively owned Rhino 9 with installed Swiftlet and GH1/GH2 open.
"""
import argparse,json,pathlib,sys,urllib.request
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parent))
from native_examples import ROOT,PRE,ROUNDTRIP
from cases import call,run_csharp

CONVERT=r'''
var gh1=AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Grasshopper");
var ghio=AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="GH_IO");
dynamic archive=Activator.CreateInstance(ghio.GetType("GH_IO.Serialization.GH_Archive"));
Check((bool)archive.ReadFromFile(@"__SOURCE__"),"read source");
dynamic source=Activator.CreateInstance(gh1.GetType("Grasshopper.Kernel.GH_Document"));
Check((bool)archive.ExtractObject(source,"Definition"),"extract source");
foreach(dynamic o in source.Objects)if(o.GetType().Name=="GH_Panel" && ((string)o.UserText).Trim()=="1900")o.UserText="18744";
var convert=gh.GetType("Grasshopper2.Doc.DocumentIO").GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static).Single(m=>m.Name=="Convert1");
dynamic converted=convert.Invoke(null,new[]{Activator.CreateInstance(gh.GetType("Grasshopper2.Interop.IGH_Document"),new object[]{(object)source}),Enum.Parse(gh.GetType("Grasshopper2.Doc.Migration.MigrationLevel"),"Total")});
foreach(dynamic o in converted.Objects.Forwards)if(o.GetType().Name=="ServerInputComponent") {Enable(o,false);o.UserName="Start server";}
foreach(dynamic o in ((IEnumerable)doc.Objects.Forwards).Cast<object>().ToArray())doc.Objects.Remove(o);
editor.Canvas.Document=converted;
Console.WriteLine("converted source without solving");
'''
REWIRE=r'''
var list=((IEnumerable)doc.Objects.Forwards).Cast<object>().ToArray();var connections=gh.GetType("Grasshopper2.Parameters.Connections");
int replaced=0;
foreach(dynamic old in list.Where(o=>o.GetType().Name.StartsWith("GH1Interop"))) {
 string target=(string)old.Grasshopper1Name=="Populate 3D"?"Populate Box":"Voronoi 3D";
 Check((string)old.Grasshopper1Name=="Populate 3D"||(string)old.Grasshopper1Name=="Voronoi 3D","known interop");
 dynamic fresh=list.First(o=>!o.GetType().Name.StartsWith("GH1Interop")&&(string)((dynamic)o).Nomen.Name==target&&string.IsNullOrEmpty((string)((dynamic)o).UserName));
 fresh.UserName=target;fresh.Attributes.Pivot=old.Attributes.Pivot;
 for(int i=0;i<2;i++)connections.GetMethod("CopyAllInputs").Invoke(null,new object[]{old.Parameters.Input(i),fresh.Parameters.Input(i),null});
 connections.GetMethod("CopyAllOutputs").Invoke(null,new object[]{old.Parameters.Output(0),fresh.Parameters.Output(0),null});
 doc.Objects.Remove(old);replaced++;
}
Check(replaced==4,"four native geometry replacements");
float left=100000,top=100000;foreach(dynamic o in doc.Objects.Forwards){left=Math.Min(left,(float)o.Attributes.Bounds.Left);top=Math.Min(top,(float)o.Attributes.Bounds.Top);}
Note("GH2 HTTP GEOMETRY SERVER\nEnable Start server, then POST JSON to localhost:18744/voronoi-volume or /voronoi-area.\nExample body: {\"num_points\":8,\"box_size\":10}\nNative Populate Box and mesh-based Voronoi 3D replace the GH1 geometry nodes.\nGeometry/numeric results differ from GH1. See server-gh2.md. Disable Start server when finished.",left+500,top-180);
Console.WriteLine("replaced 4 geometry nodes; server saved disabled");
'''

LAYOUT=r'''
// Make migrated scalar values readable, then preserve vertical grouping while moving consumers right.
foreach(dynamic o in doc.Objects.Forwards)if(o.GetType().Name=="ValueObject"){dynamic a=o.Attributes;a.Size=new Eto.Drawing.SizeF(Math.Max(140f,(float)a.Size.Width),(float)a.Size.Height);}
var objects=((IEnumerable)doc.Objects.Forwards).Cast<object>().ToArray();
var owners=new Dictionary<Guid,object>();foreach(dynamic o in objects)foreach(dynamic pin in o.EntireFamily)owners[(Guid)pin.InstanceId]=o;
var before=new Dictionary<object,HashSet<object>>();foreach(dynamic o in objects){var incoming=new HashSet<object>();var property=((object)o).GetType().GetProperty("Parameters");var pins=property==null?new[]{(object)o}:((IEnumerable)((dynamic)property.GetValue(o)).Inputs).Cast<object>().ToArray();foreach(dynamic pin in pins){var inputProperty=((object)pin).GetType().GetProperty("Inputs");if(inputProperty==null)continue;foreach(Guid id in ((dynamic)inputProperty.GetValue(pin)).Forwards)if(owners.TryGetValue(id,out var src)&&!ReferenceEquals(src,(object)o))incoming.Add(src);}before[o]=incoming;}
var done=new HashSet<object>();var visiting=new HashSet<object>();
void Place(object obj){if(done.Contains(obj))return;Check(visiting.Add(obj),"acyclic layout");dynamic o=obj;float x=(float)o.Attributes.Pivot.X;foreach(var src in before[obj]){Place(src);x=Math.Max(x,(float)((dynamic)src).Attributes.Bounds.Right+40-((float)o.Attributes.Bounds.Left-(float)o.Attributes.Pivot.X));}o.Attributes.Pivot=new Eto.Drawing.PointF(x,(float)o.Attributes.Pivot.Y);visiting.Remove(obj);done.Add(obj);}
foreach(var obj in objects)Place(obj);
foreach(dynamic o in objects)if(o.GetType().Name=="ValueObject"){var targets=((IEnumerable)o.Outputs.Forwards).Cast<Guid>().Where(id=>owners.ContainsKey(id)).Select(id=>owners[id]).ToArray();if(targets.Length!=1)continue;dynamic target=targets[0];float x=(float)target.Attributes.Bounds.Left-40-((float)o.Attributes.Bounds.Right-(float)o.Attributes.Pivot.X);float y=target.GetType().Name=="CreateJsonObjectComponent"?(float)target.Attributes.Pivot.Y-75:(float)o.Attributes.Pivot.Y;o.Attributes.Pivot=new Eto.Drawing.PointF(x,y);}
'''


def main():
 ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--port',required=True,type=int);a=ap.parse_args()
 def cs(code):
  out=run_csharp(a.port,PRE+code);print(out.strip(),flush=True);return out
 call(a.port,'g1_start',{});call(a.port,'g2_start',{})
 try:
  cs(CONVERT.replace('__SOURCE__',str(ROOT/'Examples/swiftlet-server-components/server_demo.gh')))
  for name in ['Populate Box','Populate Box','Voronoi 3D','Voronoi 3D']:
   result,text=call(a.port,'g2_place_component',{'selector':name,'x':100,'y':100});assert not result.get('isError'),text
  cs(REWIRE)
  cs(LAYOUT)
  path=ROOT/'Examples/swiftlet-server-components/server_demo.ghz'
  report={'roundtrip':cs(ROUNDTRIP.replace('__PATH__',str(path))),'routes':{}}
  cs('Check(Find("Start server").Activity.ToString()=="Disabled","server saved disabled");Enable(Find("Start server"),true);doc.Solution.StartWait();')
  for route,key in [('voronoi-volume','volume'),('voronoi-area','area')]:
   data=json.dumps({'num_points':8,'box_size':10}).encode()
   req=urllib.request.Request('http://localhost:18744/'+route,data=data,headers={'Content-Type':'application/json'})
   with urllib.request.urlopen(req,timeout=20) as r:
    assert r.status==200;response=json.load(r)
   assert set(response)=={key} and isinstance(response[key],(float,int)) and 0<response[key]<100000,response
   report['routes'][route]=response
  out=cs(r'''Healthy();var meshCounts=new List<int>();foreach(dynamic o in doc.Objects.Forwards)if(o.GetType().FullName=="MeshComponents.Voronoi3d") {var cells=Out(o,0);Check(cells.Length==0||cells.Length==8,"zero or eight Voronoi meshes per route");Check(cells.All(x=>x.GetType().Name.Contains("Mesh")),"native mesh outputs");meshCounts.Add(cells.Length);}Check(meshCounts.Count==2 && meshCounts.Contains(8),"active route has eight mesh cells");Console.WriteLine("MESHES:"+JsonSerializer.Serialize(meshCounts));''')
  report['geometry']=out.strip()
  cs('Render(@"'+str(ROOT/'artifacts/native-examples/server.png')+'");Enable(Find("Start server"),false);')
  (ROOT/'artifacts/native-examples/server-report.json').write_text(json.dumps(report,indent=2))
  print('PASS native server: both HTTP routes return geometry-derived JSON and the active route has 8 mesh cells.')
 finally:
  cs('foreach(dynamic o in ((IEnumerable)doc.Objects.Forwards).Cast<object>().ToArray())doc.Objects.Remove(o);')
if __name__=='__main__':main()

