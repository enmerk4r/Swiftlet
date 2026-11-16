# Swiftlet: notes for coding agents

## Layout

| Path | What it is |
|---|---|
| `src/Swiftlet.Core`, `Swiftlet.Imaging`, `Swiftlet.HostAbstractions`, `Swiftlet.Hosts.*` | Domain and host services. No Grasshopper or Rhino references. |
| `src/Swiftlet.Runtime` | Host-neutral workflows (requests, servers, WebSockets, MCP, OAuth, utilities) shared by both shells. No Grasshopper or Rhino references. |
| `src/Swiftlet.Gh1` | Grasshopper 1 shell (`.gha`). Assembly name and namespace remain `Swiftlet.Gh.Rhino8`; component GUIDs must never change. |
| `src/Swiftlet.Gh2` | Grasshopper 2 shell (`.rhp`, Rhino 9+). |
| `src/SwiftletBridge` | Native bridge executable used by server, WebSocket and MCP components. |
| `build/targets/*.json`, `build/packaging` | Packaging targets and Yak scripts. Rhino-version differences belong here and in build properties, not in new source trees. |

The split is GH1 vs GH2, never per Rhino version. Logic belongs in Core/Runtime; shells stay thin adapters.
Every GH1 component must have a GH2 counterpart with the same behaviour (and vice versa). A missing counterpart is a blocker, not a note.

## Building

```
dotnet build Swiftlet.sln
dotnet run --project tests/Swiftlet.Core.Tests
dotnet run --project tests/Swiftlet.Integration.Tests
```

## Grasshopper 2 rules

Grasshopper 2 is not Grasshopper 1 with new names. Follow McNeel's six rules:

1. **Inputs are immutable.** The solver is multi-threaded by default; duplicate a value before modifying it.
2. **Long work must be cancellable.** Check `access.Solution.Token` in loops and pass it to async calls.
3. **Access level is a contract.** `Access.Item` / `Twig` / `Tree` must match the `GetItem` / `GetTwig` / `GetTree` (and `Set*`) calls; a mismatch compiles but fails at runtime.
4. **Not every type is RhinoCommon.** GH2 has its own types (`Colour`, `Angle`, ...). Check before assuming.
5. **Look the API up, do not guess.** Use the XML docs shipped with the `Grasshopper2` NuGet package or `C:\Program Files\Rhino 9\Plug-ins\Grasshopper2\net8.0\Grasshopper2.xml`. Compile against the pinned package version, not the installed Rhino.
6. **Every component and parameter needs both constructors**: the parameterless one and `(IReader reader) : base(reader)`. Without the second, save, copy/paste and undo break.

Also:

- Custom data uses the plain domain type plus a `TypeAssistant<T>` and `Parameter<T>`; there are no Goo wrappers in GH2.
- UI is Eto only. No WinForms, no `System.Drawing` UI.
- Never touch the Rhino document from `Process`.
- To recompute from a background thread (listeners, sockets, MCP): `Expire()` then `Document?.Solution.Start()`, or `Document.Solution.DelayedExpire(obj)` for bursts. Guard against the document having been closed.
- Each GH2 component carries its own `[IoId]`; the GH1 `ComponentGuid` it replaces is recorded in the migration table so GH1 files upgrade.
