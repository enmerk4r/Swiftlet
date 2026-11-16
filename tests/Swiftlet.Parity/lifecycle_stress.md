# Combined GH2 lifecycle stress

Use a dedicated Rhino 9 slot with both current Swiftlet shells loaded (`cases.py --load`). The harness creates and closes its own GH2 scratch documents; it must never run on a user's working canvas.

```powershell
python tests/Swiftlet.Parity/lifecycle_stress.py --port <Rhino-AI-port> --pid <Rhino-PID> --seconds 600 --cycles 25 --report artifacts/lifecycle/stress.json
```

The run continues until both the duration and minimum cycle count are reached. Start Rhino through the logged `tests/Swiftlet.Cases/slot.py` with `DOTNET_ROOT` unset. Confirm the slot PID before loading or running tests. No keyboard or mouse input is used.

One canvas contains Server Input, MCP Server, HTTP Listener, WebSocket Server and OAuth Authorize, plus real native response components and their wires. Ordinary concurrent bursts require exact HTTP/MCP response bodies and all eight WebSocket echo messages. Each cycle also completes a real OAuth callback and restarts authorization on the same port. OAuth uses the public host-injection constructor with a manual browser launcher, so no browser windows or external endpoints are used.

Mutations rotate through:

- Disabling/re-enabling all five services on their existing ports.
- Changing all five ports, including a fresh OAuth authorization attempt.
- Deleting all five via `Document.Methods.DeleteObjects`, recording native undo actions, then undo/redo/undo and verifying restored traffic, wires and variable outputs.
- Stopping/restarting WebSocket Server with its Run input.
- Closing/recreating the scratch document while requests are in flight.

Deletion archives OAuth with Authorize=false. After undo, the fixture reinjects its manual browser dependency before setting Authorize=true; it uses the real native component and callback listener throughout. This is a test-host substitution, not a product setting.

Ports are inspected through OS LISTEN state, without connecting: a probe connection could itself consume a one-shot callback accept. SwiftletBridge processes are counted with Windows CIM, restricted to the owned Rhino parent PID. Four bridges are expected while all services run, three while WebSocket Run=false, and zero after disable/delete/close. Each command checks Rhino's PID and requires DOTNET_ROOT to remain unset. Final cleanup checks every previously used port owned by Rhino, zero owned bridges, and another successful Rhino AI call.

Transport errors and explicit server-stop errors from requests deliberately interrupted by a lifecycle change are counted separately. Assertion failures, lost ordinary traffic, faults, unexpected listening ports, wrong bridge counts or loss of Rhino liveness fail the run. The JSON report is updated after every cycle and retained on failure, including cleanup status.
