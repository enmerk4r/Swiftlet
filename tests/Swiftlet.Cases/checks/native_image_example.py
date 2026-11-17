"""Build and test the GH2 image example using loopback fixtures only; no API key or OpenAI call."""
import argparse,importlib.util,json,pathlib,sys,threading
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parent))
from native_examples import ROOT,PRE,ROUNDTRIP
from cases import call,run_csharp
BUILD=r'''
foreach(dynamic o in ((IEnumerable)doc.Objects.Forwards).Cast<object>().ToArray())doc.Objects.Remove(o);
Note("IMAGE RESPONSE / GH2 / OFFLINE FIXTURE\n1. Run python image_fixture.py in this example directory.\n2. Enable Image request: status 200, width/height 2, four coloured pixels.\nThe fixture returns a test pattern; it does not generate an image.\nPOST is saved disabled. No key is stored or required.\nSee image-gh2.md before adapting this graph to a live Images API.",40,20);
var json=Add("ParseJsonStringComponent","Request JSON",120,300);Put(json,0,new[]{"{\"model\":\"gpt-image-2.5-sunburst\",\"prompt\":\"An origami swiftlet on a white background\",\"n\":1,\"size\":\"1024x1024\",\"output_format\":\"png\"}"});
var body=Add("CreateTextBodyComponent","JSON body",410,300);Wire(json,0,body,0);
var post=Add("PostRequestComponent","Image request",700,300);Put(post,0,new[]{"http://127.0.0.1:18745/v1/images/generations"});Wire(body,0,post,1);Enable(post,false);Panel(post,0,"HTTP status",980,120);
var parsed=Add("ParseJsonStringComponent","Response JSON",980,300);Wire(post,1,parsed,0);
var data=Add("GetJsonObjectKeyComponent","data",1260,300);Put(data,1,new[]{"data"});Wire(parsed,0,data,0);
var items=Add("ReadJsonArrayComponent","Images",1540,300);Wire(data,0,items,0);
var key=Add("GetJsonObjectKeyComponent","b64_json",120,600);Put(key,1,new[]{"b64_json"});Wire(items,0,key,0);
var value=Add("ReadJsonValueComponent","Base64 text",410,600);Wire(key,0,value,0);
var bytes=Add("Base64ToByteArrayComponent","PNG bytes",700,600);Wire(value,0,bytes,0);
var bitmap=Add("ByteArrayToBitmapComponent","Decoded bitmap",980,600);Wire(bytes,0,bitmap,0);
var mesh=Add("BitmapToMeshComponent","Image mesh",1260,600);Wire(bitmap,0,mesh,0);Panel(mesh,2,"Width",1540,600);Panel(mesh,3,"Height",1540,820);
Note("Local fixture only: 2x2 red/green/blue/white test pattern.\nThe same response path handles data[].b64_json from GPT image models.\nLive generation is not verified and may incur API charges.\nA real image mesh has one vertex per pixel; resize large images before meshing.",700,1080);
doc.Solution.StartWait();
'''

def main():
 ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--port',required=True,type=int);a=ap.parse_args()
 spec=importlib.util.spec_from_file_location('image_fixture',ROOT/'Examples/swiftlet-openai-api/image_fixture.py');fixture=importlib.util.module_from_spec(spec);spec.loader.exec_module(fixture)
 server=fixture.ThreadingHTTPServer(('127.0.0.1',18745),fixture.Handler);worker=threading.Thread(target=server.serve_forever,daemon=True);worker.start()
 def cs(code):return run_csharp(a.port,PRE+code,timeout=60)
 try:
  call(a.port,'g2_start',{});cs(BUILD)
  report={'roundtrip':cs(ROUNDTRIP.replace('__PATH__',str(ROOT/'Examples/swiftlet-openai-api/swiftlet-dalle-example.ghz')))}
  cs('Check(Find("Image request").Activity.ToString()=="Disabled","saved disabled");Enable(Find("Image request"),true);doc.Solution.StartWait();Healthy();Check(Text(Find("Image request"),0)=="200","HTTP 200");')
  import base64
  got=cs('Console.WriteLine(Convert.ToBase64String((byte[])Out(Find("PNG bytes"),0).Single()));').strip();assert base64.b64decode(got)==fixture.fixture_png()
  pixels=json.loads(cs('dynamic b=Out(Find("Decoded bitmap"),0).Single();Check((int)b.Width==2&&(int)b.Height==2,"bitmap dimensions");var pixels=new List<int[]>();for(int y=0;y<2;y++)for(int x=0;x<2;x++){dynamic c=b.GetPixel(x,y);pixels.Add(new[]{(int)c.R,(int)c.G,(int)c.B,(int)c.A});}Console.WriteLine(JsonSerializer.Serialize(pixels));'))
  assert pixels==[[255,0,0,255],[0,255,0,255],[0,0,255,255],[255,255,255,255]]
  cs('dynamic m=Out(Find("Image mesh"),0).Single();Check((int)m.Vertices.Count==4&&(int)m.Faces.Count==1,"mesh");Check(Text(Find("Image mesh"),2)=="2"&&Text(Find("Image mesh"),3)=="2","reported dimensions");Render(@"'+str(ROOT/'artifacts/native-examples/image.png')+'");')
  assert len(fixture.Handler.requests)==1
  report.update(http_status=200,requests=1,png_bytes_exact=True,pixels=pixels,mesh_vertices=4,mesh_faces=1,live_service_verified=False)
  (ROOT/'artifacts/native-examples/image-report.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
 finally:
  try:cs('doc.Solution.Stop();foreach(dynamic o in ((IEnumerable)doc.Objects.Forwards).Cast<object>().ToArray())doc.Objects.Remove(o);')
  finally:server.shutdown();server.server_close();worker.join()
if __name__=='__main__':main()
