"""Build and verify the GH2 Iris scatter-plot example against its local Python model."""
import argparse,json,pathlib,sys,time,urllib.request
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parent))
from native_examples import ROOT,PRE,ROUNDTRIP
from cases import call,run_csharp

BUILD=r'''
Note("IRIS CLASSIFICATION / GH2\nStart the bundled Python server, then enable Predict Iris.\n150 original samples; POST is saved disabled.\n3D plot: X = sepal length, Y = sepal width, Z = petal length (cm).\nPetal width is also sent to the model. See iris-gh2.md.",40,20);
var samples=Add("ParseJsonStringComponent","150 Iris samples",120,260);Put(samples,0,new[]{__JSON__});
var body=Add("CreateTextBodyComponent","JSON request body",420,260);Wire(samples,0,body,0);
var post=Add("PostRequestComponent","Predict Iris",720,260);Put(post,0,new[]{"http://127.0.0.1:8000/predict-iris"});Wire(body,0,post,1);Enable(post,false);
Panel(post,0,"HTTP status",1020,120);
var response=Add("ParseJsonStringComponent","Parse predictions",1020,260);Wire(post,1,response,0);
var array=Add("ReadJsonArrayComponent","Prediction list",1300,260);Wire(response,0,array,0);
var labels=Add("ReadJsonValueComponent","Class labels",1560,260);Wire(array,0,labels,0);Panel(labels,0,"Predictions",1840,140);
var rows=Add("ReadJsonArrayComponent","Sample rows",120,660);Wire(samples,0,rows,0);
string[] keys={"sepal_length_cm","sepal_width_cm","petal_length_cm"};
for(int k=0;k<3;k++) {var key=Add("GetJsonObjectKeyComponent",keys[k],420,550+k*180);Put(key,1,new[]{keys[k]});Wire(rows,0,key,0);var value=Add("ReadJsonValueComponent","Coordinate "+k,720,550+k*180);Wire(key,0,value,0);Wire(value,1,Find("Measurement points"),k);}
string[] names={"Iris Setosa","Iris Versicolour","Iris Virginica"};
var colourT=gh.GetType("Grasshopper2.Types.Colour.Colour");double[][] rgb={new[]{0.2,0.65,0.95},new[]{0.95,0.55,0.1},new[]{0.65,0.3,0.8}};
for(int k=0;k<3;k++) {dynamic eq=Find("Compare "+k),split=Find("Class points "+k),paint=Find("Class colour "+k);Wire(labels,0,eq,0);Put(eq,1,new[]{names[k]});Wire(Find("Measurement points"),0,split,0);Wire(eq,0,split,1);Wire(split,0,paint,0);var colour=colourT.GetMethod("FromRgb").Invoke(null,new object[]{rgb[k][0],rgb[k][1],rgb[k][2],1f});var colours=Array.CreateInstance(colourT,1);colours.SetValue(colour,0);Put(paint,1,colours);Note(names[k]+" / "+new[]{"blue","orange","purple"}[k],1810,500+k*180);}
Note("The original flower geometry is replaced by a native scatter plot.\nEach coloured point represents one prediction. Coincident measurements overlap.\nSelect 150 Iris samples to edit the JSON input; keep all four feature keys.\nThe bundled model normalises each submitted batch; class counts depend on the batch.",40,1080);
doc.Solution.StartWait();
'''

def main():
 ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--port',required=True,type=int);a=ap.parse_args()
 def cs(code):return run_csharp(a.port,PRE+code,timeout=60)
 call(a.port,'g2_start',{})
 samples=json.loads((ROOT/'Examples/swiftlet-iris-prediction/iris-samples.json').read_text())
 cs('doc.Solution.Stop();')
 cs('foreach(dynamic o in ((IEnumerable)doc.Objects.Forwards).Cast<object>().ToArray())doc.Objects.Remove(o);')
 natives=[('Point XYZ','Measurement points',1020,730)]
 for k in range(3):natives += [('Text Equality','Compare '+str(k),1300,550+180*k),('Dispatch','Class points '+str(k),1560,550+180*k),('00000000-c6c7-4dc9-b89a-579fdfe6bcd5','Class colour '+str(k),1840,550+180*k)]
 try:
  for selector,key,x,y in natives:
   result,text=call(a.port,'g2_place_component',{'selector':selector,'x':x,'y':y});assert not result.get('isError'),text
   ident=json.loads(text)['id'];cs('foreach(dynamic o in doc.Objects.Forwards)if(o.InstanceId.ToString()=="'+ident+'")o.UserName='+json.dumps(key)+';')
  cs(BUILD.replace('__JSON__',json.dumps(json.dumps(samples,separators=(',',':')))))
  report={'roundtrip':cs(ROUNDTRIP.replace('__PATH__',str(ROOT/'Examples/swiftlet-iris-prediction/iris-3d.ghz')))}
  cs('Check(Find("Predict Iris").Activity.ToString()=="Disabled","saved disabled");Enable(Find("Predict Iris"),true);doc.Solution.Start();')
  end=time.monotonic()+30
  while 'True' in cs('Console.WriteLine(doc.Solution.State.CurrentlyRunning!=null);'):
   if time.monotonic()>end:raise TimeoutError('Iris solve did not settle')
   time.sleep(.2)
  cs('Check(Text(Find("Predict Iris"),0)=="200","HTTP 200");Healthy();')
  labels=json.loads(cs('Console.WriteLine(JsonSerializer.Serialize(Out(Find("Class labels"),0)));'))
  req=urllib.request.Request('http://127.0.0.1:8000/predict-iris',data=json.dumps(samples).encode(),headers={'Content-Type':'application/json'})
  with urllib.request.urlopen(req,timeout=30) as response:assert json.load(response)==labels
  assert len(labels)==len(samples)==150
  names=['Iris Setosa','Iris Versicolour','Iris Virginica'];assert set(labels)<=set(names)
  raw=json.loads(cs('Console.WriteLine(JsonSerializer.Serialize(Out(Find("Measurement points"),0).Select(x=>{dynamic p=x;return new[]{(double)p.X,(double)p.Y,(double)p.Z};})));'))
  keys=['sepal_length_cm','sepal_width_cm','petal_length_cm'];assert raw==[[r[k] for k in keys] for r in samples]
  counts=[]
  for k,name in enumerate(names):
   coords=json.loads(cs('Console.WriteLine(JsonSerializer.Serialize(Out(Find("Class colour '+str(k)+'"),0).Select(x=>{dynamic p=x;return new[]{(double)p.X,(double)p.Y,(double)p.Z};})));'))
   assert coords==[point for point,label in zip(raw,labels) if label==name]
   counts.append(len(coords))
  report.update(samples=150,class_counts=counts,model_matches_direct_request=True,all_classified_coordinates_verified=True)
  cs('Render(@"'+str(ROOT/'artifacts/native-examples/iris.png')+'");')
  (ROOT/'artifacts/native-examples/iris-report.json').write_text(json.dumps(report,indent=2)+'\n')
  print(json.dumps(report,indent=2));print('PASS: 150 predictions, coordinates and all three classified point sets verified.')
 finally:
  cs('doc.Solution.Stop();')
  cs('foreach(dynamic o in ((IEnumerable)doc.Objects.Forwards).Cast<object>().ToArray())doc.Objects.Remove(o);')
if __name__=='__main__':main()
