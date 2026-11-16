"""Live native authentication checks, including exact GH1/GH2 typed output parity."""
import argparse, base64, json, re, sys
from pathlib import Path
HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(HERE.parents[2]/"tests"/"Swiftlet.Parity"))
from parity import RhinoMcp
p=argparse.ArgumentParser(description=__doc__)
p.add_argument("--port",required=True,type=int)
p.add_argument("--gh1",required=True,type=Path)
a=p.parse_args();client=RhinoMcp(a.port)
client.call("g2_start");client.call("g1_start")
ids=dict(re.findall(r'const string (\w+) = "([^"]+)"',(HERE.parent/"Ids"/"ComponentIds.Auth.cs").read_text()))
fixtures=[("BasicAuth",["alice","p:ass"],["Authorization","Basic YWxpY2U6cDphc3M="]),
 ("BasicAuth",["?","?"],["Authorization","Basic Pzo/"]),
 ("BearerTokenAuth",["abc.?"],["Authorization","Bearer abc.?"]),
 ("ApiKeyAuth",["X-Api-Key","a & ?"],["X-Api-Key","a & ?"])]
for name,inputs,expected in fixtures:
 target=client.call("g2_place_component",{"selector":ids[name],"solve":False})["id"]
 try:
  setup='''var gh=AppDomain.CurrentDomain.GetAssemblies().Single(x=>x.GetName().Name=="Grasshopper2");
  dynamic editor=gh.GetType("Grasshopper2.UI.Editor").GetProperty("Instance").GetValue(null);
  foreach(dynamic c in (System.Collections.IEnumerable)editor.Canvas.Document.Objects.Forwards) if(c.InstanceId.ToString()=="__TARGET_ID__") {
   string[] values=__INPUTS_JSON__;
   for(int i=0;i<values.Length;i++){c.Parameters.Input(i).Set(new string[]{values[i]});c.Parameters.Input(i).Expire();}c.Expire();
  }'''
  replacements={"__TARGET_ID__":target,"__CLASS__":name+"Component","__INPUTS_JSON__":"new string[]"+json.dumps(inputs,ensure_ascii=False).replace("[","{",1).rsplit("]",1)[0]+"}",
   "__GH1_PATH_JSON__":json.dumps(str(a.gh1.resolve(strict=True))),
   "__EXPECTED_HEADER__":"new string[]{"+",".join(json.dumps(s,ensure_ascii=False) for s in expected)+"}",
   "__EXPECTED_QUERY__":"new string[]{"+",".join(json.dumps(s,ensure_ascii=False) for s in expected)+"}"}
  def run(source):
   for k,v in replacements.items():source=source.replace(k,v)
   result=client.call("run_csharp",{"script":source})
   if result.get("stderr"):raise RuntimeError(result["stderr"])
   return result.get("stdout","")
  run(setup)
  result=client.call("g2_solve_canvas")
  if not result["solved"] or result["errors"] or result["warnings"]:raise RuntimeError(result)
  evidence=run((HERE/"ValidateAuth.csx").read_text())
  if "PASS " not in evidence:raise RuntimeError("Missing Auth evidence")
  print(evidence,end="")
 finally:client.call("g2_delete_component",{"selector":target,"solve":False})
print("PASS basic authentication suite")
