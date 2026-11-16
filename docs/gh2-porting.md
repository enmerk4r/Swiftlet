# GH2 porting guide

This guide records patterns tested in the Swiftlet spike. It is not a claim that all components, migrations or release gates are complete.

## Boundaries and identities

Keep neutral behavior in Core/Runtime, UI/document services in Hosts.Rhino, and Grasshopper trees, parameters, scheduling and presentation in their respective shells. Use src/Swiftlet.Gh1 and src/Swiftlet.Gh2 across Rhino versions. Preserve GH1 assembly name Swiftlet.Gh.Rhino8 and all original component GUIDs. Give GH2 its own assembly GUID and stable IoIds, and record the explicit correspondence in docs/parity.json.

GH2 targets net8.0 and Grasshopper2 9.0.26300.9401 with runtime SDK assets excluded. Compile against that package's XML/API; installed 9.1 XML may expose newer symbols. Shared libraries remain net7.0. Rhino 9.1's active CLR is 10.0.2; target framework and runtime version are distinct.

The user-facing product/tab remains Swiftlet. Keep existing numbered section strings; GH2 renders their dots as middle dots. This host formatting does not require different product categories.

## Components and data

Start from CreateHttpHeaderComponent: Component + Nomen, IoId attribute, AddInputs/AddOutputs, Process(IDataAccess), and both empty and IReader constructors. Match input access and retrieval exactly. Never mutate shared input values. Copy mutable domain values in assistants.

Use plain domain types with TypeAssistant<T> and Parameter<T>. Public parameterless assistant constructors permit discovery. Parameter classes also need an IReader constructor. HttpHeader and QueryParameter serialize as a fixed two-string array. HttpResponseData serializes every field, including headers and exact response bytes. BitmapTypeAssistant stores dimensions and RGBA bytes. These assistants have actual persistent-parameter roundtrip checks, not just transient solve checks.

DescribePrimary provides the human-facing description (use the GH1 wording where relevant). Rhino AI graph snapshots currently sample raw ToString and may display the CLR type name. Compare typed fields and exact trees for behavior; do not change domain objects merely to make sampled strings look like GH1. Text conversion to another parameter is a separate conversion contract that may require ConversionRepository; DescribePrimary alone is not proof of every cast.

**Optional custom input rule:** `InputAdder.Add(parameter)` overwrites a pre-set parameter.Requirement with its default MustExist. Pass `inputs.Add(parameter, Requirement.MayBeMissing)` explicitly for optional custom pins (or set the requirement after adding). Inspect the constructed component, not only its initializer. This was the root cause of earlier zero-iteration solves. Do not add empty twigs, empty objects or one-null persistent values unless GH1 has that persistent default. Real network regressions prove optional Item (IRequestBody) and Twig (QueryParameter/HttpHeader) inputs execute with genuinely empty persistent trees once MayBeMissing is retained. `validate_send_http.py --clear-optional-defaults` explicitly verifies that absence. Missing bytes in UDP are mapped to empty bytes inside Process, matching GH1, rather than stored as a new default.

When GH1 lets an exception escape, it has empty output trees. When it reports an error and returns, outputs hold null slots. Preserve that distinction (throw versus AddError) in GH2; the strict parity runner detects it.

An output with no data appears as one null slot in the canonical tree, rather than an empty list. Item inputs with `.Set(default)` reproduce GH1 optional inputs with a persistent default.

HtmlNode and XmlNode keep their document context: their assistants return the same node from Copy; components must lock the owning document for every read. Never mutate these shared inputs.

After changing persistent data programmatically with Set, expire the input parameter and the owning component before solving. Expiring only the component can reuse cached input data.

## Network work and cancellation

Process is synchronous but runs on worker threads by default. GET calls the shared async executor with access.Solution.Token, waits there, and checks cancellation again before setting outputs. Do not wrap it in an untracked Task.Run or start a second request after every callback.

Shared HttpRequestExecutor previously swallowed caller cancellation as a status -1 response. It now propagates OperationCanceledException only when the caller token was canceled. Timeout/ordinary transport errors retain the existing failure-response contract. Core tests cover both paths. Audit found only one GH1 executor caller, HttpRequestComponentBase.Execute, which supplies no token; it does not depend on caller cancellation becoming a response.

The live slow-request check cancels a ten-second local endpoint after 300 ms. Observed completion was approximately 303 ms with Phase=Cancelled. GH2 ObjectSolutionState.Data intentionally retains the most recent successful result, even after cancellation. CurrentId identifies that cached result; ProcessId identifies the newer canceled/faulted computation. Require a successful phase/current solution before reading outputs. The shared executor also uses ConfigureAwait(false): a GH1 graph solved programmatically on Rhino's UI thread deadlocked when the synchronous Execute adapter awaited a continuation on that blocked context. The live graph completes after this fix, and a controlled no-pump-context Core test fails on the old code and passes with it. Manual canvas-click reproduction has not been tested.

The cancellation test verifies the previous successful-data ID is unchanged, rather than incorrectly requiring the cache to be empty.

DNS validation currently uses the shared synchronous UrlValidator; token checks surround validation, but DNS resolution itself is not interruptible by this adapter. Do not describe cancellation as instantaneous for every network stage.

## Settings, undo and persistence

GET stores TimeoutSeconds explicitly through Store/IReader and in CustomValues for its UI setting. Store must call base.Store, reader constructors must call base(reader). The Eto input-panel timeout picker adds CustomValueAction before changing the value, then expires and solves. Never use WinForms in the GH2 shell.

A component can compile, register and solve while still failing persistence. Test component serialization, persistent custom parameters, and document archives. GrasshopperIO.IO.ReadStorableFromFile<Document> is required when reading the document archive used by the spike; the untyped overload cannot infer Document from an IoId. These archive tests do not replace actual UI save/open, copy/paste, undo/redo, or a cold restart.

## OAuth sessions and lifecycle

Both shells use Runtime.OAuthTokenSession and OAuthAuthorizationController. Token requests serialize through a gate, suppress repeated one-use authorization codes, and refresh on the rising Refresh edge. Lowering Refresh must not replay the original authorization code. A canceled refresh retains the last successful token and can be retried. OAuthTokenClient awaits without capturing the caller context; GH1 schedules completion asynchronously, while GH2 waits on its worker with the solution/lifetime cancellation token.

Authorization attempts own their callback listener, cancellation source and generation. Reset, deletion, disable and document close cancel and release that listener. Every queued completion carries its attempt generation; both adapters recheck it on the UI thread, and GH1 checks again in its scheduled solution callback. An old completion must not expire a new or removed component. There is only one completion notification per attempt, so no cross-attempt notification coalescing is needed.

Live credentials, codes and verifiers are transient. GH1 Write/Read and GH2 archive duplication must produce fresh sessions, rather than restore cached credentials. The controller preserves GH1's distinction between setup validation errors (runtime errors) and provider callback failures (Status output).

`Validation/validate_oauth.py --port <slot>` checks both hosts against local token/callback fixtures: deduplication, refresh edges, real callbacks, cancellation, deletion, disable/re-enable, credential-free archives, and active document close with post-close same-PID/API liveness. It uses an injected manual browser service and never authenticates to an external provider. The four strict static parity cases are in `tests/Swiftlet.Cases/cases/oauth.json`.

`Validation/validate_oauth_close.py --slot-script tests/Swiftlet.Cases/slot.py --count 10 --report <json>` repeats the waiting-authorizer active-canvas close in fresh owned processes, checks listener port release and same-PID API liveness, and retains each router log. Start routers with machine-wide .NET; do not inherit DOTNET_ROOT pointing to a user-local .NET 8-only installation. Real interactive browser-provider login, copy/paste and undo remain separate release checks.

## Migration

RequestMigrationTable (formerly HeaderMigrationTable) demonstrates a simple Gh1MigrationRule.Identity pin map. This is sufficient only for stateless one-to-one cases.

GetRequestMigration first delegates pin transfer to the standard identity rule, then reads the protected TimeoutSeconds property from the restored GH1 source instance using the interop UnderlyingObject. This avoids referencing Grasshopper.dll in the native GH2 shell. The live test writes a GH1 GET with timeout 30 through its real GH_IO Write, reads a fresh GH1 instance, and migrates it; the result retains 30 and all three inputs/outputs. Fail explicitly if a required saved property cannot be recovered. Do not silently reset legacy state or drop connected pins.

TransferOutputs preserves wiring, but does not restore GH1 NickName-based settings. Transfer those explicitly before maintaining variable parameters (ServerInput demonstrates saved route names).

A direct migrator test does not yet prove an entire GH1 file's wires/custom data convert correctly. Opening representative saved GH1 definitions, including nondefault settings and custom persistent data, remains a full migration gate.

## Icons and viewport work

Every component/parameter uses an IconInternal override returning SwiftletIcons.For(GetType()) with the base icon as fallback. The shared generated IconSlugs mapping comes from Assets/Icons/icons.json; Parameter-to-Param suffix mapping is supported. New names outside that convention need explicit map entries. SVGs are embedded by the GH2 project.

Use approved normalized vector masters; keep the origami colors and flat shaded faces. GH2 SVG parsing cannot faithfully handle arbitrary Illustrator transforms, clips, gradients or compound glyph holes. Opus's converter normalizes these, splits glyph holes, and validates actual native GH2 rendering. The logo has its own vector source.

Use Hosts.Rhino.RhinoViewportCapture with access.Solution.Token. It runs inline on the UI thread or dispatches to the UI and waits cancellably from a worker. Do not access or modify Rhino document state directly from Process. Both capture components and bitmap serialization were verified live by Fable; see their source for the shared service pattern.

## Listener and variable-parameter gate

ServerInput uses SingleThreaded only for its own iteration ordering, plus an explicit session gate for external callbacks, port changes and shutdown. Live HTTP checks passed background routing, identical unanswered request reuse across recompute, eight simultaneous same-route requests, variable-route archive clone, deletion while a request is pending, port change and document-close port release. The reusable script is tests/Swiftlet.Parity/validate_server_input.py; it closes its scratch document, so use a dedicated slot.

ServerInputRequestState in Runtime retains one active request per route until its response completes. Both shells use it; cancellation/recompute cannot silently discard a claimed request. A response completion schedules the next queued request. Runtime cancellation marks the handle completed and route removal finishes unreachable queued requests. Each bridge generation retains its own cancellation token and response writer, preventing late old-generation responses from crossing into a restarted bridge. Lifecycle callbacks cancel before waiting on the gate; background expiry callbacks recheck document/activity/closing state on the UI thread.

ListenerRequest is deliberately a shared one-shot response handle, as GH1 Duplicate was, not a mutable data snapshot to duplicate. Its TypeAssistant.Copy preserves identity; live connections are transient and are not restored from an archive. Variable output names/ports are stored by the normal component/parameter serialization. ServerInput migration creates the full variable output set before transferring pin identities, wiring, values and route names.

Remaining listener release checks include actual UI undo/redo, saved GH1 file migration with wires, activity/disabled behavior and long-running stress. Archive clone and deterministic request-state tests do not by themselves prove those UI transitions.

## Build, load and test

Build in an isolated worktree. Use tests/Swiftlet.Parity/load_plugin.py to copy the artifact/dependencies to a fresh temporary folder, start GH2, and ScopePlugin then LoadPlugin. Do not load directly from your build folder; Rhino locks loaded files. Restart the dedicated Rhino process before testing a rebuilt assembly. Coordinate slot ownership with the router owner; never use another agent's default canvas.

Use the current Rhino 9 router. Current-session legacy native MCP tool inventories may be stale; the direct loopback helper/harness reaches g2 tools. Always target the dedicated port.

Header checks: src/Swiftlet.Gh2/Validation/README.md.
HTTP bulk checks: `Validation/validate_send_http.py --port <slot> --clear-optional-defaults` checks exact loopback HTTP method/query/header/body values, all six saved HTTP timeout migrations, actual UDP datagrams from both shells, and Throttle cancellation. Auth and Send pure/data cases are in tests/Swiftlet.Cases/cases/{auth,send}.json.

GET checks: run Validation/validate_get.py --port <dedicated-port> --gh1 <built-gh1.gha> with the latest GH2 build already loaded. It creates/removes its own GET component and serves only a temporary loopback HTTP fixture. Assertions cover default optional lists, exact typed query/header handling, Unicode query escaping, response byte persistence, timeout persistence, cancellation, and original GH1 saved-state migration. The optional --output saves evidence.

The parity harness reads full tree paths and typed values and fails on unsupported types. Extend canonicalization deliberately per domain type; never fall back to ToString equality or sample-only comparison. Maintain exact pins/defaults/null semantics and behavior in docs/parity.json. Archived components need an explicit disposition, not silent omissions.

## Release evidence and remaining limits

The unified Rhino 9 payload must contain GH1 and GH2 plus exactly one consistent set of shared DLLs. Live coexistence tests showed first-loaded shared assemblies win; independently updated packages can silently bind to the other shell's copies. Fable has tested isolated cold startup with RHINO_PACKAGE_DIRS pointing directly at the package contents. Real install/update/uninstall, full bridge payloads, every component's parity, complete file migration, UI undo/copy/paste, and platform-specific tests remain release gates.

Primary references: [GH2 getting started](https://developer.rhino3d.com/guides/grasshopper2/getting-started/), [GH2 migration guide](https://developer.rhino3d.com/guides/grasshopper2/migrating-components-to-gh2/). For exact signatures prefer the pinned NuGet XML and compilable, tested code in this repository.

