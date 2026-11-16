"""Generate the "gh2" blocks of docs/parity.json from the built GH2 assembly and check GH1/GH2 parity.

    python tests/Swiftlet.Cases/cases.py --port N --load          # fresh slot, current GH1 + GH2 builds
    python build/parity/build_parity_gh2.py --port N --write       # refresh docs/parity.json gh2 blocks
    python build/parity/build_parity_gh2.py --port N               # check only (release gate)

The GH2 side is read at runtime (gh2_catalog.cs via the Rhino AI run_csharp tool): [IoId], Nomen, every
pin's name, access, requirement and persistent default, and every migration rule together with the GH2
class it actually produces when run on a fresh GH1 object. Nothing in the gh2 blocks is hand-edited
except "notes", which is preserved.

Per GH1 entry the status is one of
  ported       a rule migrates it to a GH2 class and every checked property matches
  mismatch     a rule and a GH2 class exist, but pins/name/section differ (see "diffs")
  no-rule      a GH2 class of the same name exists but no migration rule targets it
  missing      no GH2 counterpart at all
  archived     archived GH1 component; "migration" says where it goes, or null if nowhere
Checked: name, section and palette place (GH1 exposure row -> GH2 Nomen slot, hidden -> Rank.Hidden) for
components and parameters; for components also input/output count and order, pin names, access (item/list/tree
vs Item/Twig/Tree), optionality (GH1 Optional vs GH2 Requirement) and input defaults (colours compared by
value). Descriptions and nicknames are warnings only (GH2 uses two-letter nicknames by convention). A GH1
optional input with a default that GH2 makes required with the same default is a warning (it differs only
when a connected wire carries no data); --strict makes it a mismatch.

Exit status is non-zero when any non-archived entry is not "ported" or a rule is broken.
"""
import argparse, json, pathlib, re, sys

REPO = pathlib.Path(__file__).resolve().parent.parent.parent
sys.path.insert(0, str(REPO / "tests" / "Swiftlet.Cases"))
from cases import run_csharp  # noqa: E402

ACCESS = {"item": "Item", "list": "Twig", "tree": "Tree"}
PALETTE_ORDER = ["primary", "secondary", "tertiary", "quarternary", "quinary", "senary", "septenary", "last"]
OPTIONAL = {"MayBeMissing", "MayBeNull"}


def gh2_catalog(port):
    out = run_csharp(port, (pathlib.Path(__file__).parent / "gh2_catalog.cs").read_text(encoding="utf-8"))
    line = next((l for l in out.splitlines() if l.startswith("SWIFTLET_GH2_CATALOG:")), None)
    if line is None:
        sys.exit("gh2_catalog.cs produced no result:\n" + out[:4000])
    return json.loads(line[len("SWIFTLET_GH2_CATALOG:"):])


def norm_defaults(values):
    return [v for v in (values or []) if v is not None]


COLOUR_NAMES = {"Black": (0, 0, 0), "White": (255, 255, 255)}


def norm_value(v):
    """Compare colour defaults by value: GH1 prints names or 'R,G,B', GH2 prints RGB(r g b) in 0..1."""
    if v in COLOUR_NAMES:
        return "#%02x%02x%02x" % COLOUR_NAMES[v]
    m = re.fullmatch(r"RGB\(([\d.]+) ([\d.]+) ([\d.]+)\)", v or "")
    if m:
        return "#%02x%02x%02x" % tuple(round(float(x) * 255) for x in m.groups())
    return v


STRICT_OPTIONAL = False


def compare_pins(side, gh1_pins, gh2_pins, inputs):
    diffs, warnings = [], []
    if len(gh1_pins) != len(gh2_pins):
        diffs.append(f"{side}: {len(gh1_pins)} pins in GH1, {len(gh2_pins)} in GH2")
    for i, (a, b) in enumerate(zip(gh1_pins, gh2_pins)):
        tag = f"{side}[{i}] {a['name']}"
        if a["name"] != b["name"]:
            diffs.append(f"{tag}: name '{a['name']}' vs '{b['name']}'")
        if ACCESS.get(a["access"]) != b["access"]:
            diffs.append(f"{tag}: access {a['access']} vs {b['access']}")
        if inputs:
            gh2_optional = b["requirement"] in OPTIONAL
            d1 = [norm_value(v) for v in norm_defaults(a.get("default"))]
            d2 = [norm_value(v) for v in norm_defaults(b.get("default"))]
            if bool(a.get("optional")) != gh2_optional:
                msg = f"{tag}: optional {bool(a.get('optional'))} vs requirement {b['requirement']}"
                # GH1 optional-with-default ported as a required input with the same default (porting
                # guide pattern): identical unless a wire delivers no data, so a warning unless --strict.
                if a.get("optional") and d1 and d1 == d2 and not STRICT_OPTIONAL:
                    warnings.append(msg + " (same default)")
                else:
                    diffs.append(msg)
            if d1 != d2:
                diffs.append(f"{tag}: default {d1} vs {d2}")
        if (a.get("description") or "") != (b.get("info") or ""):
            warnings.append(f"{tag}: description differs")
    return diffs, warnings


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--port", type=int, required=True)
    ap.add_argument("--write", action="store_true", help="update the gh2 blocks in docs/parity.json")
    ap.add_argument("--report", help="write the raw GH2 catalog and evaluation here")
    ap.add_argument("--warnings", action="store_true", help="also print description/nickname warnings")
    ap.add_argument("--strict", action="store_true", help="treat GH1 optional-with-default vs GH2 required-with-default as a mismatch")
    a = ap.parse_args()
    global STRICT_OPTIONAL
    STRICT_OPTIONAL = a.strict

    cat = gh2_catalog(a.port)
    by_ioid = {o["ioId"]: o for o in cat["objects"] if o.get("ioId")}
    by_class = {o["class"]: o for o in cat["objects"]}
    rules_by_gh1 = {}
    broken = []
    for m in cat["migrations"]:
        if "error" in m:
            broken.append(f"rule {m['source']}/{m['ruleType']} ({m.get('gh1Class') or m.get('gh1Guid')}): {m['error']}")
        if m.get("gh1Guid"):
            rules_by_gh1.setdefault(m["gh1Guid"].lower(), []).append(m)

    doc_path = REPO / "docs" / "parity.json"
    doc = json.load(open(doc_path, encoding="utf-8"))
    problems, warnings, counts = [], [], {}
    for e in doc["entries"]:
        rules = [r for r in rules_by_gh1.get(e["gh1Guid"].lower(), []) if "error" not in r]
        rule = rules[0] if rules else None
        target = by_ioid.get(rule["gh2IoId"]) if rule and rule.get("gh2IoId") else None
        if target is None and rule and rule.get("gh2Class"):
            target = by_class.get(rule["gh2Class"])
        if target is None and not e["archived"]:
            # Same-named GH2 class without a rule (params: FooParam -> FooParameter).
            guess = e["class"][:-len("Param")] + "Parameter" if e["kind"] == "param" and e["class"].endswith("Param") else e["class"]
            target = by_class.get(guess)
        diffs, warns = [], []
        if len(rules) > 1:
            diffs.append(f"{len(rules)} migration rules for this GH1 GUID ({', '.join(r['source'] for r in rules)})")
        if target is not None and not e["archived"]:
            # Name, section and palette place apply to components and parameters alike.
            if e["name"] != target.get("name"):
                diffs.append(f"name '{e['name']}' vs '{target.get('name')}'")
            # GH2 displays the dot in numbered sections as a middle dot; the stored section is the same.
            if e["panel"] != (target.get("section") or "").replace("·", "."):
                diffs.append(f"section '{e['panel']}' vs '{target.get('section')}'")
            hidden = e["exposure"] == "hidden"
            want_rank, want_slot = ("Hidden", None) if hidden else ("Normal", PALETTE_ORDER.index(e["exposure"]))
            if target.get("rank") != want_rank or (want_slot is not None and target.get("slot") != want_slot):
                diffs.append(f"palette: GH1 exposure {e['exposure']} vs GH2 slot {target.get('slot')} rank {target.get('rank')}")
        if target is not None and e["kind"] == "component" and target.get("kind") == "component" and not e["archived"]:
            if e["description"] != target.get("info"):
                warns.append("component description differs")
            for side, inputs in (("in", True), ("out", False)):
                d, w = compare_pins(side, e.get("inputs" if inputs else "outputs", []), target.get("inputs" if inputs else "outputs", []), inputs)
                diffs += d
                warns += w
        if target is not None and target.get("error"):
            diffs.append("GH2 class could not be instantiated: " + target["error"])

        if e["archived"]:
            status = "archived"
        elif target is None:
            status = "missing"
        elif rule is None:
            status = "no-rule"
        elif diffs:
            status = "mismatch"
        else:
            status = "ported"
        counts[status] = counts.get(status, 0) + 1
        old = e.get("gh2") or {}
        e["gh2"] = {
            "class": target["class"] if target else None,
            "ioId": target["ioId"] if target else None,
            "status": status,
            "migration": ({"rule": rule["ruleType"], "table": rule["source"], "target": rule.get("gh2Class")} if rule else None),
            "diffs": diffs,
            "notes": old.get("notes", ""),
        }
        if status not in ("ported", "archived"):
            problems.append((e, status, diffs))
        elif status == "archived" and rule is None:
            warnings.append(f"archived {e['class']} has no migration rule (no GH2 replacement)")
        if warns:
            warnings += [f"{e['class']}: {w}" for w in warns]

    mapped = {e["gh2"]["ioId"] for e in doc["entries"] if e["gh2"]["ioId"]}
    extra = [o["class"] for o in cat["objects"] if o.get("ioId") not in mapped and o.get("kind") in ("component", "param")]

    total = len(doc["entries"])
    print("status:", ", ".join(f"{k} {v}" for k, v in sorted(counts.items())), f"(of {total})")
    by_panel = {}
    for e, status, diffs in problems:
        by_panel.setdefault(e["panel"], []).append((e, status, diffs))
    for panel in sorted(by_panel):
        print(f"\n{panel}:")
        for e, status, diffs in by_panel[panel]:
            print(f"  {status.upper():8s} {e['kind']:9s} {e['class']}")
            for d in diffs:
                print(f"           - {d}")
    for b in broken:
        print("BROKEN", b)
    if extra:
        print("\nGH2 objects with no GH1 counterpart:", ", ".join(sorted(extra)))
    if a.warnings:
        for w in warnings:
            print("WARN", w)
    else:
        print(f"\n{len(warnings)} warning(s) (descriptions, archived without replacement); --warnings to list")

    if a.write:
        with open(doc_path, "w", newline="\n", encoding="utf-8") as f:
            json.dump(doc, f, indent=2, ensure_ascii=False)
            f.write("\n")
        print("updated", doc_path)
    if a.report:
        json.dump({"catalog": cat, "problems": [(e["class"], s, d) for e, s, d in problems], "warnings": warnings},
                  open(a.report, "w", encoding="utf-8"), indent=2)
    return 1 if problems or broken else 0


if __name__ == "__main__":
    sys.exit(main())
