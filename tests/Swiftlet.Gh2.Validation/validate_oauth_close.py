"""Repeat a waiting OAuth active-canvas close in fresh, explicitly owned Rhino processes."""
import argparse, json, os, pathlib, socket, subprocess, sys
ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tests/Swiftlet.Cases"))
import cases
ap=argparse.ArgumentParser()
ap.add_argument("--slot-script", required=True)
ap.add_argument("--count", type=int, default=10)
ap.add_argument("--report", required=True)
a=ap.parse_args()
report=pathlib.Path(a.report).resolve(); report.parent.mkdir(parents=True,exist_ok=True)
rows=[]
for i in range(a.count):
    stop=report.parent / f"{report.stem}-{i+1}.stop"
    env=dict(os.environ); env.pop("DOTNET_ROOT",None)
    process=subprocess.Popen([sys.executable,"-u",a.slot_script,"--stop-file",str(stop)],stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,env=env)
    row={"iteration":i+1,"routerLog":str(stop)+".router.log"}
    try:
        while True:
            line=process.stdout.readline()
            if not line: raise RuntimeError("slot driver exited before SLOT")
            if line.startswith("SLOT "): break
        slot=json.loads(line[5:]); row.update(slot)
        port=int(slot["port"]); pid=int(slot["pid"])
        cases.stage_and_load(port)
        with socket.socket() as s: s.bind(("127.0.0.1",0)); callback=s.getsockname()[1]
        script=pathlib.Path(__file__).with_name("CloseOAuth.csx").read_text().replace("__CANVAS__","true").replace("__PORT__",str(callback))
        output=cases.run_csharp(port,script)
        assert f"PASS waiting OAuth document close PID={pid}" in output,output
        alive=cases.run_csharp(port,'Console.WriteLine("ALIVE:"+System.Environment.ProcessId);')
        assert f"ALIVE:{pid}" in alive,alive
        with socket.socket() as s: s.bind(("127.0.0.1",callback))
        row["passed"]=True
        print(f"PASS fresh close {i+1}/{a.count} PID={pid} API alive, callback released",flush=True)
    except Exception as error:
        row.update(passed=False,error=repr(error)); raise
    finally:
        rows.append(row); report.write_text(json.dumps(rows,indent=2))
        stop.touch()
        try: process.wait(timeout=75)
        except subprocess.TimeoutExpired: print("Driver did not stop; retaining it for diagnosis",flush=True)
print(f"{sum(r['passed'] for r in rows)}/{a.count} passed",flush=True)
