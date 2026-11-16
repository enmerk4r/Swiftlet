"""Run basic authentication parity through the shared wired-source GH1/GH2 runner."""
import argparse, subprocess, sys
from pathlib import Path
root=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser(description=__doc__)
p.add_argument("--port",required=True,type=int)
p.add_argument("--report")
a=p.parse_args()
command=[sys.executable,str(root/"tests/Swiftlet.Cases/cases.py"),"--port",str(a.port),str(root/"tests/Swiftlet.Cases/cases/auth.json")]
if a.report:command += ["--report",a.report]
raise SystemExit(subprocess.call(command))
