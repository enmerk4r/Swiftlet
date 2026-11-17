# GH2 settings and native undo gates

Programmatic checks use each component's real `AppendToInputPanel`, materialise its actual Eto control, change the DropDown selection (firing its normal event), and invoke native document Undo/Redo. They do not replace settings with direct property assignments. Tests wait for the solve triggered by that action and read all output trees from one `component.State.Data` snapshot. Reading output-parameter states separately can mix independently cached solution results. No mouse or keyboard automation is used.

| Component / action | Result | Evidence |
|---|---|---|
| GET, POST, PUT, PATCH, DELETE, HTTP timeout | Works | 100 seconds ? 1 ? Undo 100 ? Redo 1. A 1.5-second local endpoint alternates status 200 / timeout -1 / 200 / -1. Property and archive clone agree. |
| Create Text Body content type | Works | JSON ? Text ? Undo JSON ? Redo Text; emitted body's ContentType and archive agree. |
| Save Web Response mode | Works | Binary ? Text ? Undo Binary ? Redo Text; exact file bytes and byte-count output alternate 3 / 12 / 3 / 12. Archive agrees. |
| Server Input add/remove route | Works | Real DoCreateParameter/DoRemoveParameter and native history preserve pin IDs, route names, downstream wire and archive. Real requests switch between root and /new-route as outputs change. |
| MCP Server Port / Server Name | Works | Native PersistentDataAction + Undo/Redo restore live session configuration; the active port accepts connections. |
| MCP tool-driven output removal / Undo | Works | Undo/Redo/Undo with native document history: the output returns with its original pin ID and downstream wire, and is removed again on Redo. GH1's native wire undo gives the same result (`validate_gh1_mcp_undo.py`). |
| MCP copy-config buttons | No saved option to undo | These export configuration; they do not change document settings. The panel has no saved options picker. Configuration generation/export has separate MCP tests. |

Run against a freshly staged build in an explicitly owned Rhino slot:

```
python tests/Swiftlet.Cases/cases.py --port PORT --load
python tests/Swiftlet.Gh2.Validation/validate_settings.py --port PORT --report artifacts/ui-settings.json
python tests/Swiftlet.Gh2.Validation/validate_variable_settings.py --port PORT --report artifacts/ui-variable.json
python tests/Swiftlet.Gh2.Validation/validate_gh1_mcp_undo.py --port PORT --report artifacts/ui-gh1-baseline.json
```

`validate_variable_settings.py` deliberately exits nonzero while MCP output-wire restoration fails. It preserves an explicit failing repro for the shared GH1/GH2 limitation. The coordinator has deferred a behaviour change for pigac to review; it is separate from the passing GH2 parity gates. The GH1 baseline uses two real Define Tool components, records the native wire action before disconnecting the second tool, and then invokes GH1 document Undo. It avoids serialising custom tool goos into a generic persistent-data undo record.

## Mouse-and-keyboard check: incomplete

A real desktop check was attempted on Windows on 2026-11-17 against the installed
Swiftlet 0.4.0 package built from `441ca1c`, in a separately owned Rhino 9 scratch
process. Only a disabled GET component with a loopback URL was prepared; no request
was sent and no repository example was overwritten.

The first attempt stopped before any input because Windows was locked. After the
user unlocked it, a right-click on the GET component opened its settings popup and
visibly showed **Timeout: 100 seconds**. This confirms that the popup can be opened
by pointer input; it does not establish that changing the control or undo works.

The automation could not continue reliably: the window inventory exposes Rhino's
parent window, while GH2 and its popup appear only as owned windows in captures.
An accessibility click on the Timeout control was rejected as being over GH2 rather
than the selected parent Rhino window. After refreshing and retrying with the
popup's screenshot coordinates, input activation dismissed the popup and the
resulting capture showed the GH2 canvas instead of an expanded list. No setting
change was observed. A second available window inventory also exposed only the
parent Rhino window. This is an automation targeting limitation, not evidence of a
Swiftlet control defect.

Screenshots of the GET popup and its 100-second value were captured during the attempt. The
scratch Rhino was closed and mouse/keyboard control released afterwards.

The following visible-UI gates remain pending and need a person at the machine (about ten
minutes): changing all five request timeouts,
content type and save mode; Undo/Redo for those controls; Server Input route
add/remove and wire restoration; MCP Port/Server Name edits and copy-config buttons;
and Tab/Enter/Esc focus handling. The programmatic event checks above remain valid,
but must not be described as having passed these mouse-and-keyboard gates.
