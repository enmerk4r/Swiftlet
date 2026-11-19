"""Build equivalent numeric-source graphs in GH1/GH2 through Rhino MCP; compare full trees."""
from __future__ import annotations

import argparse
import json
import math
import sys
import urllib.request
import uuid
from pathlib import Path

HERE = Path(__file__).resolve().parent


class RhinoMcp:
    def __init__(self, port: int, timeout: float = 120):
        if not 1024 <= port <= 65535:
            raise ValueError("Expected the loopback port of a dedicated Rhino test slot")
        self.endpoint = f"http://127.0.0.1:{port}/"
        self.timeout = timeout
        self.calls = []
        self.http = urllib.request.build_opener(urllib.request.ProxyHandler({}))

    def call(self, tool: str, arguments: dict | None = None):
        arguments = arguments or {}
        request = {"jsonrpc": "2.0", "id": str(uuid.uuid4()), "method": "tools/call",
                   "params": {"name": tool, "arguments": arguments}}
        req = urllib.request.Request(self.endpoint, json.dumps(request).encode(),
                                     {"Content-Type": "application/json", "Accept": "application/json, text/event-stream"})
        with self.http.open(req, timeout=self.timeout) as response:
            raw = response.read().decode()
        if raw.lstrip().startswith(("event:", "data:")):
            raw = next(line[5:].strip() for line in raw.splitlines() if line.startswith("data:"))
        envelope = json.loads(raw)
        if "error" in envelope:
            raise RuntimeError(f"{tool}: {envelope['error']}")
        result = envelope["result"]
        content = result.get("content", [])
        text = "\n".join(block["text"] for block in content if block.get("type") == "text")
        try:
            payload = json.loads(text)
        except ValueError:
            payload = text
        self.calls.append({"tool": tool, "arguments": arguments, "result": result})
        if result.get("isError"):
            raise RuntimeError(f"{tool}: {payload}")
        return payload


def compare_outputs(left, right, *, abs_tol=1e-9, rel_tol=0.0):
    """Names differ between hosts; compare corresponding pins' exact paths and typed values."""
    differences = []
    if len(left) != len(right):
        return [f"output count: GH1={len(left)}, GH2={len(right)}"]
    for pin, (one, two) in enumerate(zip(left, right)):
        a, b = one["branches"], two["branches"]
        if len(a) != len(b):
            differences.append(f"output {pin}: branch count {len(a)} != {len(b)}")
            continue
        for branch, (x, y) in enumerate(zip(a, b)):
            location = f"output {pin}, branch {branch}"
            if x["path"] != y["path"]:
                differences.append(f"{location}: path {x['path']} != {y['path']}")
            if len(x["values"]) != len(y["values"]):
                differences.append(f"{location}: item count {len(x['values'])} != {len(y['values'])}")
                continue
            for index, (v, w) in enumerate(zip(x["values"], y["values"])):
                if v["kind"] != w["kind"]:
                    differences.append(f"{location}, item {index}: type {v['kind']} != {w['kind']}")
                elif v["kind"] == "number":
                    n, m = float(v["value"]), float(w["value"])
                    equal = (math.isnan(n) and math.isnan(m)) or n == m or math.isclose(n, m, rel_tol=rel_tol, abs_tol=abs_tol)
                    if not equal:
                        differences.append(f"{location}, item {index}: {v['value']} != {w['value']}")
                elif v["value"] != w["value"]:
                    differences.append(f"{location}, item {index}: {v['value']!r} != {w['value']!r}")
    return differences


def numeric_inputs(fixture):
    inputs = fixture.get("inputs")
    if not isinstance(inputs, list) or not inputs:
        raise ValueError("inputs must be a non-empty list of finite numeric scalar inputs")
    for value in inputs:
        if isinstance(value, bool) or not isinstance(value, (int, float)) or not math.isfinite(value):
            raise ValueError("This initial graph builder supports numeric scalars only; add a typed source adapter for other fixtures")
        if abs(value) > 1e9 or round(value, 12) != value:
            raise ValueError("Numeric slider fixtures support magnitude <= 1e9 and at most 12 decimal places")
    return inputs


def resolve_component(client, generation, selector):
    try:
        return str(uuid.UUID(selector))
    except ValueError:
        matches = client.call(f"g{generation}_search_components", {"query": selector, "limit": 100})
        exact = [item for item in matches if item["name"].casefold() == selector.casefold()]
        if len(exact) != 1:
            raise ValueError(f"GH{generation}: expected one exact component name {selector!r}, found {len(exact)}; specify a GUID")
        return exact[0]["guid"]


def build_graph(client, generation, fixture, created):
    config = fixture.get(f"gh{generation}", {})
    selector = resolve_component(client, generation, config.get("component", fixture["component"]))
    values = numeric_inputs(fixture)
    pins = config.get("input_pins", list(range(len(values))))
    if len(pins) != len(values):
        raise ValueError("input_pins must match the input value count")
    sliders, wires = [], []
    for index, value in enumerate(values):
        key = f"input{index}"
        slider = {"Key": key, "Min": min(0, value - 1), "Value": value, "Max": max(10, value + 1),
                  "Name": f"Parity {index}", "X": 100, "Y": 100 + index * 80}
        slider["Type" if generation == 1 else "Decimals"] = "float" if generation == 1 else 12
        sliders.append(slider)
        wires.append({"SrcKey": key, "Src": "0", "DstKey": "target", "Dst": str(pins[index])})
    result = client.call(f"g{generation}_apply_graph", {"sliders": sliders,
                         "components": [{"Key": "target", "Selector": selector, "X": 400, "Y": 100}],
                         "wires": wires, "solve": True})
    created.extend(item["id"] for item in result.get("placed", []))
    if result.get("placeErrors") or result.get("wiresOk") != len(wires):
        raise RuntimeError(f"GH{generation}: graph placement/wiring failed: {result}")
    if generation == 2 and not result.get("solve", {}).get("solved"):
        raise RuntimeError(f"GH2: solution failed: {result.get('solve')}")
    target = next(item["id"] for item in result["placed"] if item["key"] == "target")
    graph = client.call(f"g{generation}_get_canvas_graph", {"include_data": False})
    instance = next(item for item in graph["objects"] if item["id"] == target)
    # Warnings are visible in the report; errors/faults are never accepted as parity.
    if any(message.get("level", "").lower() in ("error", "fault") for message in instance.get("messages", [])):
        raise RuntimeError(f"GH{generation}: component diagnostics: {instance['messages']}")
    source = (HERE / "canonical.cs").read_text() + "\n" + (HERE / f"extract-gh{generation}.cs").read_text()
    source = source.replace("__TARGET_ID__", str(uuid.UUID(target)))
    extracted = client.call("run_csharp", {"script": source})
    if extracted.get("stderr"):
        raise RuntimeError(f"GH{generation} extraction: {extracted['stderr']}")
    marker = "SWIFTLET_PARITY_JSON:"
    line = next((line for line in extracted.get("stdout", "").splitlines() if line.startswith(marker)), None)
    if line is None:
        raise RuntimeError(f"GH{generation} extractor produced no complete result: {extracted}")
    return {"component": selector, "target": target, "diagnostics": instance.get("messages", []),
            "outputs": json.loads(line[len(marker):])}


def expected_differences(outputs, expected, abs_tol, rel_tol):
    for output in outputs:
        branches = output["branches"]
        if len(branches) != 1 or branches[0]["path"] != [0] or len(branches[0]["values"]) != 1:
            return ["expected_scalars requires each output to contain one value at path {0}"]
    wanted = [{"name": "expected", "branches": [{"path": [0], "values": [{"kind": "number", "value": str(value)}]}]}
              for value in expected]
    return compare_outputs(outputs, wanted, abs_tol=abs_tol, rel_tol=rel_tol)


def run(client, fixture, *, keep_graphs=False, abs_tol=1e-9, rel_tol=0.0):
    numeric_inputs(fixture)
    created = {1: [], 2: []}
    report = {"fixture": fixture, "endpoint": client.endpoint, "absolute_tolerance": abs_tol,
              "relative_tolerance": rel_tol, "status": "failed", "differences": [], "cleanup_errors": []}
    try:
        for generation in (1, 2):
            client.call(f"g{generation}_start")
            report[f"gh{generation}"] = build_graph(client, generation, fixture, created[generation])
        report["differences"] = compare_outputs(report["gh1"]["outputs"], report["gh2"]["outputs"], abs_tol=abs_tol, rel_tol=rel_tol)
        if "expected_scalars" in fixture:
            for generation in (1, 2):
                report["differences"].extend(f"GH{generation}: {item}" for item in expected_differences(
                    report[f"gh{generation}"]["outputs"], fixture["expected_scalars"], abs_tol, rel_tol))
        if not report["differences"]:
            report["status"] = "passed"
    except Exception as error:
        report["error"] = str(error)
    finally:
        if not keep_graphs:
            for generation in (2, 1):
                for object_id in reversed(created[generation]):
                    try:
                        client.call(f"g{generation}_delete_component", {"selector": object_id, "solve": False})
                    except Exception as error:
                        report["cleanup_errors"].append(str(error))
        if report["cleanup_errors"]:
            report["status"] = "failed"
        report["kept_graphs"] = keep_graphs
        report["calls"] = client.calls
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--port", type=int, required=True, help="Loopback port of YOUR dedicated Rhino 9 slot")
    parser.add_argument("--fixture", type=Path)
    parser.add_argument("--component", help="Exact component name or GUID shared by both hosts")
    parser.add_argument("--inputs", help="JSON numeric scalar array, e.g. '[2,3]'")
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--keep-graphs", action="store_true")
    parser.add_argument("--abs-tol", type=float, default=1e-9)
    parser.add_argument("--rel-tol", type=float, default=0.0)
    args = parser.parse_args()
    if args.fixture and (args.component or args.inputs):
        parser.error("Use --fixture or --component with --inputs")
    if args.abs_tol < 0 or args.rel_tol < 0 or not math.isfinite(args.abs_tol + args.rel_tol):
        parser.error("Tolerances must be finite and nonnegative")
    try:
        fixture = json.loads(args.fixture.read_text(encoding="utf-8-sig")) if args.fixture else {
            "name": args.component, "component": args.component, "inputs": json.loads(args.inputs or "null")}
        if not fixture.get("component"):
            parser.error("Supply --fixture or --component and --inputs")
        report = run(RhinoMcp(args.port), fixture, keep_graphs=args.keep_graphs, abs_tol=args.abs_tol, rel_tol=args.rel_tol)
    except Exception as error:
        report = {"status": "failed", "error": str(error)}
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(report, indent=2, allow_nan=False), encoding="utf-8")
    print(f"{report['status'].upper()}: {args.report}")
    if report.get("error"):
        print(report["error"], file=sys.stderr)
    for difference in report.get("differences", []):
        print(difference, file=sys.stderr)
    return 0 if report["status"] == "passed" else 1


if __name__ == "__main__":
    raise SystemExit(main())
