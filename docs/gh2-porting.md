# GH2 porting guide

Implementation and test notes for the 0.4.0 branch; release status and user-visible changes are in [CHANGELOG.md](../CHANGELOG.md). Native event/undo results are in [gh2-ui-gates.md](gh2-ui-gates.md).

## Boundaries, SDK and identities

Keep domain behavior in Core/Runtime, Rhino services in Hosts.Rhino, and Grasshopper data trees, scheduling and UI in `src/Swiftlet.Gh1` / `src/Swiftlet.Gh2`; Rhino-version differences belong in build targets. Preserve the GH1 assembly name `Swiftlet.Gh.Rhino8` and its GUIDs. GH2 has stable, separate IoIds, mapped in [parity.json](parity.json).

Compile GH2 against pinned `Grasshopper2 9.0.26300.9401`, targeting net8.0 and excluding runtime SDK assets. Shared libraries target net7.0. Tests here used Windows Rhino 9.1 with CLR 10.0.2: target framework and loaded runtime are different. Look up signatures in the pinned NuGet XML; installed Rhino XML may describe newer APIs.

Keep the product/tab named Swiftlet and preserve numbered section names; GH2 renders their dots as middle dots.

Every component and parameter needs both its normal constructor and `(IReader reader) : base(reader)`. Use `TypeAssistant<T>` plus plain domain values, not GH1 Goo wrappers. Assistants must preserve exact fields/bytes through archives. Public parameterless assistant constructors permit discovery. `DescribePrimary` supplies display text, not a general text-conversion contract; register conversions separately when needed. Inputs are immutable; duplicate mutable values before changing them. HTML/XML nodes retain their document owner, so their readers lock that owner instead of modifying shared nodes.

## Pins and parity

- Match access (`Item`, `Twig`, `Tree`) to both Get and Set calls; a mismatch can compile and fail only during solving.
- **Pass optionality to Add:** `inputs.Add(parameter, Requirement.MayBeMissing)`; `Add(parameter)` overwrites a pre-set requirement with its default `MustExist`.
- Do not invent empty values or null-item defaults to make optional pins execute; truly empty optional custom Item/Twig inputs were verified with live HTTP/UDP fixtures.
- Preserve optionality even when a pin has a persistent default. Ten pins across HTML search, CSV, Byte Array To Text and QR retain `MayBeMissing` plus their defaults; `Validation/ValidateOptionalDefaults.csx` clears the defaults and verifies all seven components still produce the expected fallback output. The previous build was not tested with this check, so this is verified alignment, not a demonstrated before/after failure.
- Preserve failure output shape: a GH1 exception gives empty output trees, whereas an error message followed by return can leave null slots; choose GH2 throw versus `AddError` accordingly.
- After programmatically changing persistent data, expire the input parameter and component before solving.

Compare typed values, tree paths, null slots and diagnostics; sampled Rhino AI `ToString()` output is not a parity oracle. Read a coherent set of output trees from one `component.State.Data` snapshot, keyed by output InstanceId. Independently cached output-parameter states can refer to different solves. Cancellation retains the last successful data: inspect phase/solution IDs before interpreting it as current output.

`build/parity/build_parity_gh2.py` reflects names, sections, pins, access, requirements, defaults, migration targets and palette placement. Duplicate migration rules are errors. The generator reports optional-with-default versus required-with-default differences as warnings (errors with `--strict`); the ten known differences are now aligned. A warning classification is not evidence that missing-input behavior matches.

## Cancellation and background delivery

Process is synchronous and normally runs on a worker. Pass `access.Solution.Token` into long work and check it before publishing outputs. Shared HTTP caller cancellation propagates; timeout/transport failures retain their response contract. Synchronous GH1 adapters use `ConfigureAwait(false)` inside awaited workflows to avoid waiting for a continuation on a blocked caller context. DNS validation remains synchronous, so cancellation is not immediate at every network stage.

`ListenerComponent` serializes Process, reconfiguration and shutdown with a gate. Cancel its lifetime before waiting for the gate. Release resources on deletion, disable, document replacement and close; recreate them on re-enable or undo. UI callbacks recheck document state, activity and generation so work from an old session cannot expire a new one.

WebSocket Server/Client and HTTP Listener use `ListenerComponent.Outbox<T>` to retain received items across abandoned processing. The integrated rule currently waits for the publishing solution to complete and requests recomputation after cancellation/fault. Controlled cancellation found that a publisher can remain Completed while downstream work is canceled, then be consumed by a later solution without rerunning the publisher. Whole-solution completion is therefore too coarse to decide delivery: the correction must distinguish a retained component result from a discarded one. That correction and burst/lifecycle checks for both missing and duplicate messages are still in progress; the current rule is not a validated delivery guarantee.

For burst callbacks, `Document.Solution.DelayedExpire(this)` lets GH2 schedule recomputation without immediately canceling a running solution. MCP Server uses it. Migration of the remaining listener/OAuth callbacks to that scheduling pattern is under validation; do not claim it has landed until the integrated source does so.

Server Input and MCP calls are one-shot live handles, not copyable snapshots: assistants retain identity and do not restore live connections from archives. Shared Runtime state retains one unanswered request per route/tool across recomputes and advances after response. Removing a route/tool completes inaccessible calls, and each response stays tied to its originating bridge writer.

## OAuth ownership

Both shells use `OAuthTokenSession` and `OAuthAuthorizationController`. Exchange is serialized; duplicate one-use codes are suppressed; refresh is edge-triggered; cancellation preserves the last successful token and permits retry. Session credentials, authorization codes and verifiers are transient, not archive state.

The Desktop callback session owns its accept loop and concurrent client readers, with a five-second deadline per incomplete connection. Empty/malformed clients are ignored, unrelated paths receive 404, and the first request targeting the callback path wins; callback state validation remains in the OAuth workflow. Disposal cancels and joins all accepts/readers before returning. Queued notifications carry an attempt generation, checked again on the UI thread and in GH1's scheduled callback.

Tests cover wrong state, empty/malformed/favicon traffic, two held idle connections followed by a prompt callback, immediate reset/rebind, partial HTTP reads, token refresh/cancellation, archives and both hosts' lifecycle. Fixtures use a manual browser service and loopback servers; they do not prove real provider login. The earlier unexplained Rhino exit was not reproduced in ten consecutive final-build close checks with same-PID/API liveness; it was not diagnosed as fixed.

## Settings and undo

Store settings in CustomValues and through Store/IReader, calling the base methods. Eto picker handlers record `CustomValueAction` before changing a value. Native control events plus Undo/Redo were verified for six HTTP timeouts, text-body content type and Save Web Response mode, including archive clones and actual outputs/files.

Server Input's real create/remove-parameter actions preserve pin identity and downstream wires through undo, including live route output. MCP Port/Name persistent-data undo restores the live session configuration. Automatic MCP tool removal instead recreates a new output on undo and loses its downstream wire in **both** GH1 and GH2; this is a documented existing limitation. Config-copy buttons are export actions, not saved options. Programmatic event tests do not prove mouse hit targets or popup focus.

## Migration and archived components

Use identity rules only for stateless one-to-one mappings. Stateful rules first recover saved GH1 settings from the interop source, establish the complete variable pin set, then transfer pins/wires. `TransferOutputs` alone does not restore GH1 NickName-based route/tool settings. Missing required state must fail explicitly.

| Archived GH1 object | GH2 disposition |
|---|---|
| Create Text Body | Current native component, with saved content type |
| Three-input MCP Tool Response | Native compatibility adapter marked `[Obsolete]`, hidden in the palette; original JSON-as-text/plain-error behavior and ordered wires retained |
| Socket Listener | GH1 interop host |
| Multipart Form Body Named / Unnamed | GH1 interop hosts |

`tests/Swiftlet.Cases/checks/migrate_files.py` reads GH1 archives without solving, converts through `DocumentIO.Convert1(..., MigrationLevel.Total)`, classifies native/interop objects and compares wires into Swiftlet objects. It processes one file per call. Missing third-party objects are dropped **in the test's extraction copy** and reported because GH1 extraction can otherwise wait indefinitely on a hidden missing-plugin prompt; original files are untouched.

The 21-example audit found 19 fully native Swiftlet conversions and two retaining only the unsupported archived Swiftlet objects above, with their wires intact. Google Maps also has unavailable third-party objects and one missing non-Swiftlet wire; do not generalize that result to lossless conversion. Standard conversion drops GH1 scribbles. `make_gh2_examples.py` recreates notes with their text/position and GH2 default styling, saves through DocumentIO.Save and reopens to compare counts. Fifteen `.ghz` examples were generated; the key-dependent OpenAI example was reopened only, and two others reported unavailable external services when solved.

## Icons, palette and viewport services

`Assets/Icons/icons.json` drives shared `IconSlugs.g.cs`; SVG masters feed GH2 vector icons and generated 24px GH1 PNGs. The pipeline outlines text/strokes, flattens transforms and shadows, and splits compound glyph holes for GH2's SVG parser. Native renders were compared against reference renders. Keep the original origami colors and flat shaded faces.

Run `python build/parity/build_palette.py` after catalog changes. `Palette.Entry` uses generated class mappings to translate GH1 exposure rows into Nomen slots 0-7 and hidden rank; it does not change IoIds. Stored objects keep their serialized Nomen, so regenerate example files when testing new palette naming. Current constructors match GH1 names/sections; pin short-name style is a separate user-facing choice.

Viewport captures go through `Hosts.Rhino.RhinoViewportCapture`, which runs inline on the UI thread or dispatches from workers with cancellation. Do not access or modify the Rhino document directly from Process.

## Reproducible local checks

Use an isolated worktree and an explicitly owned Rhino slot. `tests/Swiftlet.Cases/slot.py --stop-file PATH` starts Rhino 9 and retains `<PATH>.router.log`; creating the stop file closes its slot. Clear an inherited `DOTNET_ROOT` that points to a .NET 8-only installation: Rhino 9's helpers need the machine-wide .NET 10 runtime. Never mix old Rhino 8 routers into the shared slot store, and verify PID ownership rather than assuming a reused port is yours.

```
dotnet build Swiftlet.sln
dotnet run --project tests/Swiftlet.Core.Tests
dotnet run --project tests/Swiftlet.Integration.Tests
python tests/Swiftlet.Cases/cases.py --port PORT --load
$caseFiles = (Get-ChildItem tests/Swiftlet.Cases/cases/*.json).FullName
python tests/Swiftlet.Cases/cases.py --port PORT @caseFiles
python tests/Swiftlet.Cases/cases.py --port PORT --migrations
python build/parity/build_parity_gh2.py --port PORT
python tests/Swiftlet.Cases/checks/migrate_files.py --port PORT --report migration.json
```

The loader stages GH1/GH2 together in one temporary folder; never load build output directly, and restart Rhino after changing binaries. If the machine lacks a net7 runtime, use `DOTNET_ROLL_FORWARD=Major` for the net7 test executables, not a .NET 8-only DOTNET_ROOT override.

The 239-case suite compares exact GH1/GH2 results; network, transfers, OAuth, archives and settings have separate live harnesses under `tests/Swiftlet.Cases/checks`, `tests/Swiftlet.Parity` and `src/Swiftlet.Gh2/Validation`. Close tests must check listener/bridge cleanup **and** a post-close call to the same Rhino PID. Use OS LISTEN-state inspection for OAuth port checks: a bare TCP probe is itself a client connection.

The Rhino 9 Yak payload must contain both shells and one consistent set of versioned shared DLLs. Packaging checks assembly identities; cold-start checks point `RHINO_PACKAGE_DIRS` at the package contents and exercise its bundled bridge. Windows cold start and packaged network checks passed; actual user install/update/uninstall, other platforms, pointer/focus checks and the final combined stress result remain separate release evidence. Do not infer them from a successful compile or component archive clone.

Primary API references: [GH2 getting started](https://developer.rhino3d.com/guides/grasshopper2/getting-started/) and [GH2 migration](https://developer.rhino3d.com/guides/grasshopper2/migrating-components-to-gh2/); use the pinned NuGet XML and tested repository code for exact signatures.
