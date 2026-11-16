# GH2 settings and native undo gates

Programmatic checks use each component's real `AppendToInputPanel`, materialise its actual Eto control, change the DropDown selection (firing its normal event), and invoke native document Undo/Redo. They do not replace settings with direct property assignments. Tests wait for the solve triggered by that action and read all output trees from one `component.State.Data` snapshot. Reading output-parameter states separately can mix independently cached solution results. No mouse or keyboard automation is used.

| Component / action | Result | Evidence |
|---|---|---|
| GET, POST, PUT, PATCH, DELETE, HTTP timeout | Works | 100 seconds ? 1 ? Undo 100 ? Redo 1. A 1.5-second local endpoint alternates status 200 / timeout -1 / 200 / -1. Property and archive clone agree. |
| Create Text Body content type | Works | JSON ? Text ? Undo JSON ? Redo Text; emitted body's ContentType and archive agree. |
| Save Web Response mode | Works | Binary ? Text ? Undo Binary ? Redo Text; exact file bytes and byte-count output alternate 3 / 12 / 3 / 12. Archive agrees. |
| Server Input add/remove route | Works | Real DoCreateParameter/DoRemoveParameter and native history preserve pin IDs, route names, downstream wire and archive. Real requests switch between root and /new-route as outputs change. |
| MCP Server Port / Server Name | Works | Native PersistentDataAction + Undo/Redo restore live session configuration; the active port accepts connections. |
| MCP tool-driven output removal / Undo | Fails on tested baseline | Output name/count returns, but with a new pin ID; the downstream wire remains disconnected. GH1's native wire-undo baseline also restores only the output. This is shared pre-existing behaviour, not a GH2-only regression. |
| MCP copy-config buttons | No saved option to undo | These export configuration; they do not change document settings. The panel has no saved options picker. Configuration generation/export has separate MCP tests. |

Run against a freshly staged build in an explicitly owned Rhino slot:

```
python tests/Swiftlet.Cases/cases.py --port PORT --load
python tests/Swiftlet.Gh2.Validation/validate_settings.py --port PORT --report artifacts/ui-settings.json
python tests/Swiftlet.Gh2.Validation/validate_variable_settings.py --port PORT --report artifacts/ui-variable.json
python tests/Swiftlet.Gh2.Validation/validate_gh1_mcp_undo.py --port PORT --report artifacts/ui-gh1-baseline.json
```

`validate_variable_settings.py` deliberately exits nonzero while MCP output-wire restoration fails. It preserves an explicit failing repro for the shared GH1/GH2 limitation. The coordinator has deferred a behaviour change for pigac to review; it is separate from the passing GH2 parity gates. The GH1 baseline uses two real Define Tool components, records the native wire action before disconnecting the second tool, and then invokes GH1 document Undo. It avoids serialising custom tool goos into a generic persistent-data undo record.

## Human click check still needed

Programmatic event checks do not prove popup focus, pointer hit targets or keyboard handling. Desktop automation was stopped by the user, so these remain **needs pigac to click**: open each component's input panel, select the values in the table, use GH2 Undo and Redo, and confirm the displayed choice and resulting output. For Server Input, use its actual add/remove-output UI and confirm the connected route wire returns after Undo. No permission request is needed for the completed programmatic checks.
