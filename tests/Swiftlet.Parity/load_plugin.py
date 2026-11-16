"""Load one locally built GH2 .rhp for this process only; restart Rhino before loading a rebuild."""
import argparse
import json
import shutil
import tempfile
from pathlib import Path

from parity import HERE, RhinoMcp

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--port", required=True, type=int)
parser.add_argument("--plugin", required=True, type=Path)
args = parser.parse_args()
path = args.plugin.resolve(strict=True)
if path.suffix.casefold() != ".rhp":
    parser.error("Expected a compiled .rhp")
stage = Path(tempfile.mkdtemp(prefix="swiftlet-gh2-load-"))
shutil.copytree(path.parent, stage, dirs_exist_ok=True)
path = stage / path.name
print(f"Staged plugin and dependencies at {path}")
client = RhinoMcp(args.port)
client.call("g2_start")
source = (HERE / "load-gh2-plugin.cs").read_text().replace("__PLUGIN_PATH_JSON__", json.dumps(str(path)))
result = client.call("run_csharp", {"script": source})
if result.get("stderr"):
    raise RuntimeError(result["stderr"])
print(result.get("stdout", ""), end="")
