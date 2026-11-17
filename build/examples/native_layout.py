"""Apply the shared layout tool to generated examples using stable object names.

The specifications contain positions and annotations only. Component data and wires
are preserved by gh2_layout.apply; IDs are resolved afresh on every generation.
"""
import argparse
import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "build/examples"))
import gh2_layout as layout


def apply(port, key):
    spec = json.loads((ROOT / "build/examples/native_layouts" / (key + ".json")).read_text())
    path = ROOT / spec["file"]
    data = layout.dump(port, path)
    things = [o for o in data["objects"] if o["kind"] == "object"]
    def find(name):
        base, sep, index = name.rpartition("#")
        if not sep:
            base, index = name, 0
        hits = ([o for o in things if o.get("value") == base[6:]] if base.startswith("value:")
                else [o for o in things if o["name"] in (base, spec.get("labels", {}).get(base, base))])
        hits.sort(key=lambda o: (o["bounds"][1], o["bounds"][0]))
        if not hits or (not sep and len(hits) != 1):
            raise ValueError(f"Ambiguous or missing object: {name}")
        return hits[int(index)]["id"]
    ops = [{"op": "delete", "id": o["id"]} for o in data["objects"] if o["kind"] == "text"]
    ops += [{"op": "delete_group", "id": o["id"]} for o in data["objects"] if o["kind"] == "group"]
    ops.append({"op": "fit_values"})
    for name, (x, y) in spec["positions"].items():
        ops.append({"op": "move", "id": find(name), "left": x, "top": y})
    for name, text in spec.get("labels", {}).items():
        ops.append({"op": "label", "id": find(name), "text": text})
    memberships = {}
    for k, group in enumerate(spec["groups"]):
        ids = [o["id"] for o in things] if group["members"] == "*" else [find(n) for n in group["members"]]
        memberships.update({ident: f"@g{k}" for ident in ids})
        ops.append({"op": "new_group", "ids": ids, "key": f"g{k}"})
    for k, note in enumerate(spec["notes"]):
        anchor = find(note["of"])
        ops += [{"op": "add", "role": "note", "text": note["text"], "wrap": 260, "left": 0, "top": 0, "key": f"n{k}", "group": memberships[anchor]},
                {"op": note.get("where", "above"), "id": f"@n{k}", "of": anchor, "gap": 10}]
    for k, group in enumerate(spec["groups"]):
        ops += [{"op": "add", "role": "heading", "text": group["title"], "left": 0, "top": 0, "key": f"t{k}"},
                {"op": "title_over", "id": f"@t{k}", "of": f"@g{k}"}]
    print(layout.run(port, "apply", path, out=path, ops=ops).strip())
    result = layout.dump(port, path)
    if spec.get("columns"):
        groups = [o for o in result["objects"] if o["kind"] == "group"]
        top = min(g["bounds"][1] for g in groups)
        stack_ops = []
        for column in spec["columns"]:
            left = min(groups[i]["bounds"][0] for i in column)
            y = top
            for i in column:
                g = groups[i]
                title = next(o for o in result["objects"] if o.get("text") == spec["groups"][i]["title"])
                dx, dy = left - g["bounds"][0], y - g["bounds"][1]
                stack_ops.append({"op": "moveby", "ids": g["content"] + [title["id"]], "dx": dx, "dy": dy})
                y += g["bounds"][3] + 56
        print(layout.run(port, "apply", path, out=path, ops=stack_ops).strip())
        result = layout.dump(port, path)
    problems = layout.check(result)
    out = ROOT / "artifacts/layout-review"
    out.mkdir(parents=True, exist_ok=True)
    (out / (key + "-check.json")).write_text(json.dumps({"problems": problems, "data": result}, indent=2))
    print(f"{key}: {len(problems)} layout problems")
    for p in problems:
        print(p)
    return path, problems


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("key")
    parser.add_argument("--port", type=int, required=True)
    args = parser.parse_args()
    _, problems = apply(args.port, args.key)
    sys.exit(bool(problems))
