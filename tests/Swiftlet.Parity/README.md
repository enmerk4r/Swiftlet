# Live GH1/GH2 parity

This harness builds the same small graph on both canvases in a dedicated Rhino 9 MCP slot. It uses `g1_*` and `g2_*` tools for discovery, placement, wiring and solving, then C# script adapters to read every output value and exact tree path. It does not compare the MCP graph summary's sampled strings. Requires Python 3.10+ (standard library only), Rhino 9 with RhinoAI and Grasshopper 1/2, and a running dedicated MCP slot.

```powershell
python tests/Swiftlet.Parity/parity.py --port 10500 --fixture tests/Swiftlet.Parity/fixtures/addition.json --report artifacts/parity/addition.json
python tests/Swiftlet.Parity/parity.py --port 10500 --component Addition --inputs '[2.25,3.5]' --report artifacts/parity/custom.json
python -m unittest discover -s tests/Swiftlet.Parity -p test_parity.py
```

Use the loopback port returned by **your** `spawn_slot(version="9")`. Never target a colleague's canvas. The script appends graphs and deletes only the objects it created, including on ordinary failures; it never clears a canvas. `--keep-graphs` leaves its graphs for inspection. Existing documents and unrelated canvas objects are retained. A process crash or transport failure can prevent cleanup; the report records cleanup failures. Use an isolated test instance with no user work.

The report includes the fixture, selectors, instance IDs, full output trees, diagnostics, tolerances, every MCP call/result, and cleanup status. Exit status is 0 only for a successful comparison and cleanup. A matching pair that is wrong can be caught with a fixture's `expected_scalars` oracle; the included Addition fixture expects 5.75.

## Fixture contract and current coverage

`component` is an exact name, or a GUID. `inputs` is an ordered list of finite numeric scalars (magnitude at most 1e9, up to 12 decimal places). This first source adapter uses numeric sliders. Text, boolean, tree, custom-data and geometry **input fixtures are not supported yet** and fail before graph changes. Outputs support floating numbers, exact integers, text, booleans, nulls, and bytes. Unknown output types fail rather than being compared as display strings; add a shared explicit adapter in `canonical.cs` before testing custom types.

Host-specific selector/pin changes are explicit:

```json
{
  "name": "addition",
  "component": "Addition",
  "inputs": [2.25, 3.5],
  "gh1": {"component": "a0d62394-a118-422d-abb3-6af115c75b25", "input_pins": [0, 1]},
  "gh2": {"component": "00000000-a9e2-4ef6-a4d8-a8f87f93757f", "input_pins": [0, 1]},
  "expected_scalars": [5.75]
}
```

Outputs correspond by index. Host-specific output labels are recorded but not equated. Branch count, exact integer path arrays, branch order, item count, null positions and canonical type must match. Floating values use absolute tolerance 1e-9 and relative tolerance 0 by default; integers/booleans/text/bytes remain exact. Tolerances can be set with `--abs-tol` and `--rel-tol` and appear in the report. Two NaNs match; signed infinities match only the same sign.

**Observed host difference:** Addition with inputs `[2,3]` produces integer 5 in GH1 and double 5 in GH2. This harness deliberately reports that representation difference. The default floating fixture proves strict parity without hiding it. Any numeric storage equivalence policy must be a deliberate fixture/port decision, not a blanket coercion.

**Null handling:** GH2 `ItemCount` excludes nulls; `LeafCount` includes null slots. The GH2 extractor uses `LeafCount` and `NullAt` so nulls cannot disappear from a passing result.

This is a synchronous graph harness. HTTP/listener/MCP/websocket races, cancellation, persistence, lifecycle, geometry and migration require additional fixture adapters and lifecycle tests; this smoke check does not certify them. Do not mark a missing adapter or unsupported fixture as passed. The higher-level catalog is `docs/parity.json`.

## Load a local GH2 plugin

```powershell
python tests/Swiftlet.Parity/load_plugin.py --port 10500 --plugin C:/path/to/bin/Debug/net8.0/Swiftlet.Gh2.rhp
```

The loader first copies the complete build output to a unique temporary directory, so Rhino cannot lock the developer build folder. Keep that copy until the test Rhino is closed.

`load-gh2-plugin.cs` uses reflection against the running Grasshopper2 assembly, so the Script Editor needs no `#r`. It calls public `PluginServer.ScopePlugin(path)` then `LoadPlugin(path, out FailureInfo)`, checks the result and reports failure details. Verified on Rhino 9.1.26314.09001 with the Swiftlet.Gh2 spike compiled against Grasshopper2 9.0.26300.9401; Astra independently verified discovery/placement after loading. This does **not** persist a global custom plugin or alter package-manager folders. Start a fresh Rhino slot to load a rebuilt assembly; this helper intentionally treats already-loaded/conflicting versions as failures rather than claiming a hot reload.

For persistent developer loading, McNeel's documented route is `GH2Plugins` > Install, selecting the .rhp. `PluginServer.InstallCustomPlugin` is its public API counterpart; it records the file for future starts. We did not change that shared user setting during multi-agent development. `RHINO_PACKAGE_DIRS` and Yak layouts belong to the separate cold-install packaging tests; this process-local proof is not proof of package auto-discovery.

Reference: [McNeel testing/loading guidance](https://developer.rhino3d.com/guides/grasshopper2/getting-started/), installed `Grasshopper2.xml` PluginServer/ITree/ITwig docs, and [McNeel's graph inspector implementation](https://github.com/mcneel/RhinoAI/blob/rhino-9.x/rhino/plugin/Tools/GH2/GH2_GetCanvasGraphTool.ai.cs).

## Server Input lifecycle spike

After loading a build with ServerInput and staging SwiftletBridge beside its shared Runtime DLL, run `python tests/Swiftlet.Parity/validate_server_input.py --port YOUR_PORT --report artifacts/parity/server-input.json`. It asserts actual HTTP routing, identity retained across recompute, eight simultaneous same-route requests, variable-route archive cloning, deletion during a request, port changes, and document-close cleanup. It closes the scratch GH2 document as its final case, so use a dedicated slot with no user work. This script creates/mutates only its test components and cleans up its component on ordinary failure.

Shared GH1/GH2 changes: a claimed request survives repeated/canceled solutions until its one-shot response completes; completion schedules the next pending request; removed routes finish queued requests with 503; canceled request waiters become completed 408 handles; bridge shutdown cancels pending waits and old responses retain the old bridge writer. GH1 no longer silently drains pending requests during an unrelated solve, guards queued expiry after removal/close, reattaches its events after undo/re-add, and prevents inserting a new route before the protected Root output.
