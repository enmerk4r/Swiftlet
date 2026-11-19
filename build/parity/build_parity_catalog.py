"""Build docs/parity.json: the GH1 -> GH2 port checklist and the basis of the GH1 migration table.

Inputs:
  * a runtime dump of every GH1 document object (gh1_runtime.json), produced by loading the built
    Swiftlet.Gh.Rhino8 assembly inside Rhino and instantiating each component/param (see
    gh1_catalog_script.py; it runs through the Rhino MCP `run_csharp` tool);
  * the GH1 sources (src/Swiftlet.Gh1), scanned for behaviour that needs special care in GH2;
  * Assets/Icons/icons.json for the icon slug of each class.

Usage: python build/parity/build_parity_catalog.py <gh1_runtime.json> [--repo PATH]
"""
import argparse, json, os, re

# Source patterns that flag GH2 porting work, with the note recorded in the catalog.
FEATURES = [
    ("menu", r"AppendAdditionalMenuItems", "context menu items (GH2: AppendToInputPanel / presets)"),
    ("persistence", r"override\s+bool\s+(Write|Read)\s*\(", "custom Write/Read serialisation (GH2: CustomValues or Store + IReader ctor)"),
    ("taskCapable", r"GH_TaskCapableComponent", "GH_TaskCapableComponent (GH2: removed; solver is multi-threaded)"),
    ("variableParameters", r"IGH_VariableParameterComponent", "variable parameters (GH2: CanCreateParameter/DoCreateParameter/VariableParameterMaintenance)"),
    ("backgroundExpiry", r"ExpireSolution|ScheduleSolution", "expires/schedules solutions from code (GH2: Expire + Document.Solution.Start / DelayedExpire)"),
    ("backgroundWork", r"Task\.Run|CancellationToken", "background tasks / cancellation (GH2: access.Solution.Token, thread-safety)"),
    ("documentLifecycle", r"RemovedFromDocument|DocumentContextChanged|OnPingDocument", "document lifecycle hooks (cleanup on delete/close)"),
    ("runtimeMessage", r"\bMessage\s*=", "component Message text"),
    ("rhinoDisplay", r"Rhino\.Display|RhinoDoc", "Rhino display/document access (never from GH2 Process)"),
    ("windowsUi", r"System\.Windows\.Forms|Clipboard", "WinForms / clipboard (GH2: Eto only)"),
]


def class_sources(root):
    """Map class name -> list of source texts (partial classes may span files)."""
    out = {}
    for d, _, files in os.walk(root):
        if os.sep + "bin" in d or os.sep + "obj" in d:
            continue
        for f in files:
            if not f.endswith(".cs"):
                continue
            text = open(os.path.join(d, f), encoding="utf-8-sig").read()
            for m in re.finditer(r"\bclass\s+(\w+)", text):
                out.setdefault(m.group(1), []).append((os.path.relpath(os.path.join(d, f), root).replace(os.sep, "/"), text))
    return out


def base_of(texts, name):
    for _, t in texts:
        m = re.search(r"\bclass\s+%s\b[^:{]*:\s*([\w.]+)" % re.escape(name), t)
        if m:
            return m.group(1).split("<")[0]
    return None


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("runtime")
    ap.add_argument("--repo", default=os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..")))
    a = ap.parse_args()
    R = a.repo
    gh1 = os.path.join(R, "src", "Swiftlet.Gh1")
    runtime = json.load(open(a.runtime, encoding="utf-8-sig"))
    srcs = class_sources(gh1)
    icons = json.load(open(os.path.join(R, "Assets", "Icons", "icons.json")))["icons"]
    slug_by_class = {c: s for s, m in icons.items() for c in m["classes"]}

    # The "gh2" blocks are produced by build_parity_gh2.py; keep them when regenerating the GH1 side.
    existing = {}
    old_path = os.path.join(R, "docs", "parity.json")
    if os.path.exists(old_path):
        existing = {e["gh1Guid"]: e.get("gh2") for e in json.load(open(old_path, encoding="utf-8"))["entries"]}
    entries = []
    for o in runtime["objects"]:
        cls = o["class"]
        # collect this class and its in-repo base classes
        chain, files, name = [], [], cls
        while name and name in srcs and name not in chain:
            chain.append(name)
            files += srcs[name]
            name = base_of(srcs[name], name)
        text = "\n".join(t for _, t in files)
        feats = {k: note for k, pat, note in FEATURES if re.search(pat, text)}
        if o.get("variableParameters"):
            feats["variableParameters"] = dict((k, n) for k, _, n in FEATURES)["variableParameters"]
        archived = bool(o.get("obsolete")) or cls.endswith("_ARCHIVED")
        e = {
            "class": cls,
            "kind": o.get("kind"),
            "gh1Guid": o.get("guid"),
            "name": o.get("name"),
            "nickname": o.get("nickname"),
            "description": o.get("description"),
            "tab": o.get("category"),
            "panel": o.get("subcategory"),
            "exposure": o.get("exposure"),
            "archived": archived,
            "baseClasses": chain[1:] + ([name] if name and name not in chain else []),
            "sourceFiles": sorted({f for f, _ in files}),
            "icon": slug_by_class.get(cls),
        }
        if o.get("kind") == "component":
            e["inputs"] = o.get("inputs", [])
            e["outputs"] = o.get("outputs", [])
        else:
            e["typeName"] = o.get("typeName")
            e["access"] = o.get("access")
        e["portNotes"] = feats
        e["gh2"] = existing.get(o.get("guid")) or {"class": None, "ioId": None, "status": "todo" if not archived else "decide",
                                                     "migration": None, "notes": ""}
        entries.append(e)

    entries.sort(key=lambda e: (e["kind"] != "param", e["panel"] or "", e["archived"], e["name"] or ""))
    doc = {
        "about": "GH1 -> GH2 parity checklist. GH1 fields are generated from a runtime dump of the built "
                 "GH1 assembly plus a source scan (build/parity/build_parity_catalog.py). The 'gh2' blocks are "
                 "generated from the built GH2 assembly and its migration rules by build/parity/"
                 "build_parity_gh2.py (status, diffs); only 'notes' is edited by hand.",
        "gh1Assembly": "Swiftlet.Gh.Rhino8",
        "counts": {
            "components": sum(e["kind"] == "component" for e in entries),
            "params": sum(e["kind"] == "param" for e in entries),
            "archived": sum(e["archived"] for e in entries),
            "withoutIcon": [e["class"] for e in entries if not e["icon"]],
        },
        "entries": entries,
    }
    out = os.path.join(R, "docs", "parity.json")
    os.makedirs(os.path.dirname(out), exist_ok=True)
    with open(out, "w", newline="\n", encoding="utf-8") as f:
        json.dump(doc, f, indent=2, ensure_ascii=False)
        f.write("\n")
    print(json.dumps(doc["counts"], indent=1))
    agg = {}
    for e in entries:
        for k in e["portNotes"]:
            agg[k] = agg.get(k, 0) + 1
    print("port notes:", agg)


if __name__ == "__main__":
    main()
