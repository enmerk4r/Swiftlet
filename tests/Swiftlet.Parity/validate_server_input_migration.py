import argparse,json
from pathlib import Path
from parity import RhinoMcp

p=argparse.ArgumentParser(description="Check saved ServerInput routes, port and nickname through the native migrator.")
p.add_argument("--port",type=int,required=True);p.add_argument("--gh1",type=Path,required=True);p.add_argument("--report",type=Path,required=True);a=p.parse_args()
c=RhinoMcp(a.port);c.call("g1_start");c.call("g2_start")
script=(Path(__file__).resolve().parents[2]/"src/Swiftlet.Gh2/Validation/ValidateServerInputMigration.csx").read_text()
script=script.replace("__GH1_PATH_JSON__",json.dumps(str(a.gh1.resolve(strict=True))))
result=c.call("run_csharp",{"script":script})
if result.get("stderr") or "PASS ServerInput migration" not in result.get("stdout",""):raise RuntimeError(result)
a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(result,indent=2));print(result["stdout"])
