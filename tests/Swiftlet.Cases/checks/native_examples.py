"""Build, reopen and verify the two native GH2 examples against local receivers.
Use an exclusively owned Rhino 9 with the installed Swiftlet package and GH2 open.
    python tests/Swiftlet.Cases/checks/native_examples.py --port 10500
No GH1 extraction, external services, credentials or test-only components are used.
"""
import argparse
import importlib.util
import json
import pathlib
import sys
import threading
import time

ROOT = pathlib.Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tests/Swiftlet.Cases'))
from cases import run_csharp

PRE = r'''
using System; using System.Linq; using System.Collections; using System.Collections.Generic; using System.Reflection; using System.Text.Json;
var gh = AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Grasshopper2");
var plug = AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Swiftlet.Gh2");
dynamic editor = gh.GetType("Grasshopper2.UI.Editor").GetProperty("Instance").GetValue(null);
dynamic doc = editor.Canvas.Document;
dynamic Add(string cls, string key, float x, float y) {
 dynamic c=Activator.CreateInstance(plug.GetTypes().Single(t=>t.Name==cls));
 c.UserName=key; doc.Objects.Add(c,new Eto.Drawing.PointF(x,y)); return c;
}
dynamic Find(string key) {foreach(dynamic c in doc.Objects.Forwards) if((string)c.UserName==key) return c; throw new Exception(key);}
void Put(dynamic c,int i,object a) {dynamic p=c.Parameters.Input(i); p.Set((dynamic)a); p.Expire();c.Expire();}
void Wire(dynamic a,int output,dynamic b,int input) {
 object src=output<0?(object)a:(object)a.Parameters.Output(output);
 object dst=input<0?(object)b:(object)b.Parameters.Input(input);
 gh.GetType("Grasshopper2.Parameters.Connections").GetMethods().Single(m=>m.Name=="Connect"&&m.GetParameters().Length==3).Invoke(null,new object[]{src,dst,null});
}
void Panel(dynamic c,int output,string title,float x,float y) {
 dynamic p=Activator.CreateInstance(gh.GetType("Grasshopper2.Parameters.Special.DataPanelObject"));
 p.UserName=title; doc.Objects.Add(p,new Eto.Drawing.PointF(x,y)); Wire(c,output,p,-1);
}
void Note(string text,float x,float y) {dynamic n=Activator.CreateInstance(gh.GetType("Grasshopper2.SpecialObjects.ScribbleObject"));n.Text=text;doc.Objects.Add(n,new Eto.Drawing.PointF(x,y));}
void Enable(dynamic c,bool on) {var p=((object)c).GetType().GetProperty("Activity");p.SetValue((object)c,Enum.Parse(p.PropertyType,on?"Enabled":"Disabled"));c.Expire();}
object[] Out(dynamic c,int i) {var data=(object)c.State.Data; if(data==null)return Array.Empty<object>();
 dynamic tree=c.State.Data.Tree(c.Parameters.Output(i).InstanceId); return ((IEnumerable)tree.AllItems).Cast<object>().ToArray();}
string Text(dynamic c,int i)=>string.Join("|",Out(c,i).Select(x=>x?.ToString()??""));
void Healthy() { foreach(dynamic o in doc.Objects.Forwards) {
 if(o.State.FaultException!=null)throw new Exception(o.State.FaultException.Message);
 dynamic messages=o.State.Data?.Messages; if(messages!=null) for(int k=0;k<(int)messages.Count;k++)
 Check(messages[k].Level.ToString()!="Error",o.UserName+": "+messages[k].Text);
}}
void Render(string path) {
 float l=100000,t=100000,r=-100000,b=-100000;
 foreach(dynamic o in doc.Objects.Forwards){dynamic q=o.Attributes.Bounds;l=Math.Min(l,(float)q.Left);t=Math.Min(t,(float)q.Top);r=Math.Max(r,(float)q.Right);b=Math.Max(b,(float)q.Bottom);}
 var rect=new Eto.Drawing.RectangleF(l-30,t-30,r-l+60,b-t+60);
 var draw=((object)editor.Canvas).GetType().GetMethods().First(m=>m.Name=="DrawToBitmap"&&m.GetParameters().Length==6);
 using var bitmap=(Eto.Drawing.Bitmap)draw.Invoke((object)editor.Canvas,new object[]{rect,(int)(rect.Width*1.5),(int)(rect.Height*1.5),false,true,false});
 bitmap.Save(path,Eto.Drawing.ImageFormat.Png);
}
void Check(bool ok,string text) {if(!ok)throw new Exception("ASSERT: "+text);}
'''

MULTIPART = r'''
foreach(dynamic item in ((IEnumerable)doc.Objects.Forwards).Cast<object>().ToArray()) doc.Objects.Remove(item);
Note("MULTIPART FORM DATA / GH2\n1. Run: python multipart_receiver.py\n2. Enable POST Request to send to 127.0.0.1:18741.\n3. Status = 200; response lists title and upload fields.\nPOST is saved disabled. Disable it again before closing.\nSelect a component to edit its persistent input values.",40,30);
var text=Add("CreateMultipartFieldTextComponent","Title field",160,250);
Put(text,0,new[]{"title"});Put(text,1,new[]{"Swiftlet origami"});Put(text,2,new[]{"text/plain"});
var bytes=Add("TextToByteArrayComponent","UTF-8 file contents",160,450);
Put(bytes,0,new[]{"Fold me.\n"});Put(bytes,1,new[]{"UTF8"});
var file=Add("CreateMultipartFieldBytesComponent","Upload field",440,450);
Put(file,0,new[]{"upload"});Put(file,2,new[]{"origami.txt"});Put(file,3,new[]{"text/plain"});Wire(bytes,0,file,1);
var body=Add("CreateMultipartFormBodyComponent","Combine both fields",730,320);Wire(text,0,body,0);Wire(file,0,body,0);
var post=Add("PostRequestComponent","POST Request",1030,320);Put(post,0,new[]{"http://127.0.0.1:18741/multipart"});Wire(body,0,post,1);Enable(post,false);
Panel(post,0,"HTTP status",1300,250);Panel(post,1,"Receiver JSON",1300,450);
Note("title = Swiftlet origami\nupload = origami.txt\nUTF-8 content = Fold me. + newline (9 bytes)",440,600);
doc.Solution.StartWait();
Console.WriteLine("BUILT multipart");
'''

WEBSOCKET = r'''
foreach(dynamic item in ((IEnumerable)doc.Objects.Forwards).Cast<object>().ToArray()) doc.Objects.Remove(item);
Note("WEBSOCKET LOOPBACK / GH2\nLocal replacement for the archived Socket Listener example.\n1. Set Local server Run=true.\n2. Set Client Run=true; Status becomes Connected.\n3. Set Send to server Send=true, then false. Server receives hello from Swiftlet.\n4. Set Reply to client Send=true, then false. Client receives subscribed: origami.\n5. Set Client Run=false, then Local server Run=false to stop.\nAll Run/Send flags are saved false. No external service is needed.",40,20);
var server=Add("WebSocketServerComponent","Local server",140,330);Put(server,0,new[]{18742});Put(server,1,new[]{false});
var client=Add("WebSocketClientComponent","Client",140,650);Put(client,0,new[]{"ws://localhost:18742/"});Put(client,2,new[]{false});
var send=Add("WebSocketSendComponent","Send to server",500,650);Put(send,1,new[]{"hello from Swiftlet"});Put(send,2,new[]{false});Wire(client,0,send,0);
var reply=Add("WebSocketSendComponent","Reply to client",500,330);Put(reply,1,new[]{"subscribed: origami"});Put(reply,2,new[]{false});Wire(server,0,reply,0);
Panel(server,1,"Server received",870,330);Panel(client,1,"Client received",870,650);
Panel(server,4,"Server status",1150,330);Panel(client,3,"Client status",1150,650);
Note("The old On Open subscription is now an explicit Send.\nFor a real service, send its subscription text after Connected.\nSend is level-triggered: leaving it true can resend on recompute.\nUse a free port; change both Port and URL if 18742 is occupied.",40,900);
doc.Solution.StartWait();Console.WriteLine("BUILT websocket");
'''

ROUNDTRIP = r'''
var ioT=gh.GetType("Grasshopper2.Doc.DocumentIO");var docT=gh.GetType("Grasshopper2.Doc.Document");
int Count(object d)=>((IEnumerable)((dynamic)d).Objects.Forwards).Cast<object>().Count();
int Wires(object d){int count=0;foreach(dynamic o in ((dynamic)d).Objects.Forwards){
 var ps=((object)o).GetType().GetProperty("Parameters");
 if(ps!=null){dynamic p=ps.GetValue(o);foreach(dynamic i in p.Inputs)count+=(int)i.Inputs.Count;}
 else {var prop=((object)o).GetType().GetProperty("Inputs");if(prop!=null)count+=(int)((dynamic)prop.GetValue(o)).Count;}}
 return count;}
int before=Count(doc),wires=Wires(doc);
Check(before>0,"refuse to overwrite an example with an empty document");
dynamic io=Activator.CreateInstance(ioT,new object[]{(object)doc,false,false,false,false});
var saved=ioT.GetMethod("Save",new[]{typeof(string),gh.GetType("Grasshopper2.Doc.FileContents")}).Invoke(io,new object[]{@"__PATH__",Enum.Parse(gh.GetType("Grasshopper2.Doc.FileContents"),"All")});
object back=docT.GetMethod("NewInactiveDocument").Invoke(null,null);
dynamic reader=Activator.CreateInstance(ioT,new object[]{back,false,false,false,false});
var open=ioT.GetMethods().Single(m=>m.Name=="Open"&&m.GetParameters().Length==3&&m.GetParameters()[0].ParameterType==typeof(string));
Check((bool)open.Invoke(reader,new object[]{@"__PATH__",Array.CreateInstance(docT,0),null}),"read saved file");
back=reader.Document;Check(Count(back)==before&&Wires(back)==wires,"roundtrip objects/wires");
Check(!((IEnumerable)((dynamic)back).Objects.Forwards).Cast<object>().Any(o=>o.GetType().Name.StartsWith("GH1Interop")),"no GH1 interop");
foreach(dynamic item in ((IEnumerable)doc.Objects.Forwards).Cast<object>().ToArray()) doc.Objects.Remove(item);editor.Canvas.Document=(dynamic)back;
Console.WriteLine("ROUNDTRIP "+JsonSerializer.Serialize(new {objects=before,wires}));
'''


def save_example(port, cs, path, layout_key):
    """Save, lay out with the shared standard, then reopen the saved graph for its fixture checks."""
    result = cs(ROUNDTRIP.replace('__PATH__', str(path)))
    sys.path.insert(0, str(ROOT / 'build/examples'))
    from native_layout import apply
    _, problems = apply(port, layout_key)
    if problems:
        raise AssertionError('Layout check failed: ' + '; '.join(problems))
    cs(r'''var dt=gh.GetType("Grasshopper2.Doc.Document");var it=gh.GetType("Grasshopper2.Doc.DocumentIO");
    dynamic reader=Activator.CreateInstance(it,new object[]{dt.GetMethod("NewInactiveDocument").Invoke(null,null),false,false,false,false});
    var open=it.GetMethods().Single(m=>m.Name=="Open"&&m.GetParameters().Length==3&&m.GetParameters()[0].ParameterType==typeof(string));
    Check((bool)open.Invoke(reader,new object[]{@"__PATH__",Array.CreateInstance(dt,0),null}),"read laid-out example");
    editor.Canvas.Document=reader.Document;'''.replace('__PATH__', str(path)))
    return result


def main():
    ap=argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--port',type=int,required=True)
    ap.add_argument('--report',type=pathlib.Path)
    ap.add_argument('--only',choices=['multipart','websocket'])
    args=ap.parse_args()
    def cs(code):
        result=run_csharp(args.port,PRE+code)
        print(result.strip(),flush=True)
        return result
    def wait(code, expected):
        end=time.monotonic()+12
        while time.monotonic()<end:
            value=cs('Console.WriteLine("VALUE:"+('+code+'));')
            if 'VALUE:'+expected in value:return
            time.sleep(.2)
        raise AssertionError('Timed out waiting for '+expected)
    spec=importlib.util.spec_from_file_location('receiver',ROOT/'Examples/swiftlet-simple-examples/multipart_receiver.py')
    receiver=importlib.util.module_from_spec(spec);spec.loader.exec_module(receiver)
    server=receiver.make_server()
    thread=threading.Thread(target=server.serve_forever,daemon=True);thread.start()
    report={}
    (ROOT/'artifacts/native-examples').mkdir(parents=True,exist_ok=True)
    try:
        cs('Console.WriteLine("PID "+Environment.ProcessId+"; "+plug.Location);')
        if args.only in (None,'multipart'):
            cs(MULTIPART)
            path=ROOT/'Examples/swiftlet-simple-examples/multipart-form-data.ghz'
            report['multipart_roundtrip']=save_example(args.port,cs,path,'multipart')
            cs('Check(Find("POST Request").Activity.ToString()=="Disabled","POST saved disabled");Enable(Find("POST Request"),true);doc.Solution.StartWait();Check(Text(Find("POST Request"),0)=="200","HTTP 200");Console.WriteLine("BODY:"+Text(Find("POST Request"),1));')
            expected={'fields':[{'name':'title','filename':None,'content_type':'text/plain','text':'Swiftlet origami','bytes':16}, {'name':'upload','filename':'origami.txt','content_type':'text/plain','text':'Fold me.\n','bytes':9}]}
            assert server.received==[expected],server.received
            response=cs('Console.WriteLine("JSON:"+Text(Find("POST Request"),1));')
            assert json.loads(next(l[5:] for l in response.splitlines() if l.startswith('JSON:')))==expected
            report['multipart_received']=server.received
            cs('Healthy();Render(@"'+str(ROOT/'artifacts/native-examples/multipart.png')+'");')
            cs('Enable(Find("POST Request"),false);foreach(dynamic item in ((IEnumerable)doc.Objects.Forwards).Cast<object>().ToArray()) doc.Objects.Remove(item);')
        if args.only in (None,'websocket'):
            cs(WEBSOCKET)
            path=ROOT/'Examples/swiftlet-listeners/swiftlet-websocket-loopback.ghz'
            report['websocket_roundtrip']=cs(ROUNDTRIP.replace('__PATH__',str(path)))
            cs('Put(Find("Local server"),1,new[]{true});doc.Solution.StartWait();')
            wait('Text(Find("Local server"),4)','Listening on ws://localhost:18742/')
            cs('Put(Find("Client"),2,new[]{true});doc.Solution.StartWait();')
            wait('Text(Find("Client"),3)','Connected')
            cs('Put(Find("Send to server"),2,new[]{true});doc.Solution.StartWait();Check(Text(Find("Send to server"),0)=="True","client sent");Put(Find("Send to server"),2,new[]{false});doc.Solution.StartWait();')
            wait('Text(Find("Local server"),1)','hello from Swiftlet')
            cs('Put(Find("Reply to client"),2,new[]{true});doc.Solution.StartWait();Check(Text(Find("Reply to client"),0)=="True","server sent");Put(Find("Reply to client"),2,new[]{false});doc.Solution.StartWait();')
            wait('Text(Find("Client"),1)','subscribed: origami')
            cs('Healthy();Render(@"'+str(ROOT/'artifacts/native-examples/websocket.png')+'");')
            cs('Put(Find("Client"),2,new[]{false});doc.Solution.StartWait();Put(Find("Local server"),1,new[]{false});doc.Solution.StartWait();Check(Text(Find("Local server"),4)=="Stopped","server stopped");')
            report['websocket']={'client_to_server':'hello from Swiftlet','server_to_client':'subscribed: origami','stopped':True}
        print('PASS: selected saved files reopened and their network outputs verified.',flush=True)
        if args.report:
            args.report.parent.mkdir(parents=True,exist_ok=True)
            args.report.write_text(json.dumps(report,indent=2),encoding='utf-8')
    finally:
        try:
            cs('foreach(dynamic item in ((IEnumerable)doc.Objects.Forwards).Cast<object>().ToArray()) doc.Objects.Remove(item);')
        finally:
            server.shutdown();server.server_close();thread.join(timeout=5)

if __name__=='__main__':main()



