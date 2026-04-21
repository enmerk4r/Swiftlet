# Linux Port Architecture

Note: this document captures the original migration plan. The active repository direction is now a single modern Rhino 8+ shell on Windows, Mac, and Linux, with the old `Swiftlet/` legacy tree removed from the repository. Legacy paths mentioned below are historical context only.

## Goal

Make Swiftlet run on Linux with feature parity for the current Windows plugin, while supporting the modern Rhino 8+ line without turning the solution into an unmaintainable build matrix.

This plan is based on the original repository structure and code hotspots:

- Build and packaging were encoded into a single project: `Swiftlet/Swiftlet.csproj`
- Rhino version selection was encoded in solution configurations: `Swiftlet/Swiftlet.sln`
- Packaging is handled by the cross-platform publish pipeline: `build/packaging/Publish-Target.ps1`
- The plugin currently depends on Windows-only or Windows-biased APIs in several places:
  - `System.Drawing` for bitmap features and resource handling
  - `System.Windows.Forms` for clipboard and menu-related code
  - `rundll32.exe` for OAuth browser launch
  - `HttpListener` for HTTP server and MCP server features

## Architectural Decisions

### 1. Split by runtime family, not by every possible combination

Do not model `RhinoVersion x DotNetVersion x OS` as named build configurations.

Instead:

- Rhino 6 and 7 remain the legacy family:
  - Windows only
  - .NET Framework
- Rhino 8 and 9 become the modern family:
  - cross-platform
  - .NET Core / .NET 8+

The solution should express this through project boundaries, not through configuration name tricks.

### 2. Extract shared logic into SDK-agnostic libraries

The current plugin mixes three kinds of code in one assembly:

- pure Swiftlet logic
- Grasshopper/Rhino integration
- host/platform behavior

That has to be split.

## Proposed Target Layout

```text
Swiftlet.sln
src/
  Swiftlet.Core/
  Swiftlet.Imaging/
  Swiftlet.HostAbstractions/
  Swiftlet.Hosts.Desktop/
  Swiftlet.Hosts.Headless/
  Swiftlet.Gh.Shared/
  Swiftlet.Gh.Rhino6/
  Swiftlet.Gh.Rhino7/
  Swiftlet.Gh.Rhino8/
  Swiftlet.Gh.Rhino9/
  SwiftletBridge/
build/
  packaging/
  targets/
tests/
  Swiftlet.Core.Tests/
  Swiftlet.Imaging.Tests/
  Swiftlet.Integration.Tests/
docs/
```

## Project Responsibilities

### `Swiftlet.Core`

Pure logic only. No Rhino, Grasshopper, WinForms, WPF, or image backend code.

Contents:

- request/response DTOs
- JSON/XML/HTML parsing helpers
- MIME/content-type utilities
- URL validation and blacklist logic
- MCP message models
- OAuth flow state and URL generation
- component-domain logic that does not require Rhino APIs

### `Swiftlet.Imaging`

Cross-platform image model and operations.

This project replaces Swiftlet's direct dependence on `System.Drawing` for user-facing bitmap features.

Contents:

- `SwiftletImage` internal image type
- encode/decode from bytes
- QR rendering
- pixel sampling for mesh generation
- color conversion helpers

Recommended backend:

- SkiaSharp

Reason:

- mature cross-platform rendering
- good encode/decode support
- straightforward pixel access
- usable for QR generation and bitmap conversion

### `Swiftlet.HostAbstractions`

Interfaces for platform and host services.

Examples:

- `IBrowserLauncher`
- `IClipboardService`
- `INotificationService`
- `IFileDialogService`
- `IHttpServerFactory`
- `IHostCapabilities`
- `IBridgeLocator`

### `Swiftlet.Hosts.Desktop`

Interactive implementations for Rhino desktop environments.

Use Eto where possible rather than WinForms.

Examples:

- clipboard via `Eto.Forms.Clipboard`
- dialogs/messages via Eto or Rhino UI helpers
- browser launch via platform-aware process launch

### `Swiftlet.Hosts.Headless`

Implementations for Rhino.Compute or other non-interactive execution.

Examples:

- browser launch returns a structured "manual action required" result
- clipboard is unavailable
- dialogs/messages become logs/runtime messages
- MCP config generation returns text, not clipboard side effects

### `Swiftlet.Gh.Shared`

Shared Grasshopper component source and adapters.

This should stay as source shared across the Rhino-specific shells, not as a single compiled DLL that references one Rhino SDK version.

Use one of:

- a shared project (`.shproj`)
- linked source includes
- a `Directory.Build.props` include strategy

### `Swiftlet.Gh.Rhino6`

- thin shell
- `net462`
- Windows only
- legacy packaging only

### `Swiftlet.Gh.Rhino7`

- thin shell
- `net48`
- Windows only

### `Swiftlet.Gh.Rhino8`

Primary modern shell.

Targets:

- `net8.0`

Optional only if needed for Rhino 8 on Windows running .NET Framework:

- `net48`

This is the shell that should own Linux compatibility first.

### `Swiftlet.Gh.Rhino9`

Do not implement now.

Prepare for it by keeping Rhino-version-specific code isolated in thin shells so Rhino 9 becomes:

- a new shell project
- a new package version mapping
- small compatibility shims only

### `SwiftletBridge`

Keep as a separate executable project.

Publish per RID:

- `win-x64`
- `linux-x64`
- optionally `linux-arm64`
- optionally `osx-arm64`

Do not publish it from inside the plugin project as a post-build side effect.

## Build Strategy

### Keep only `Debug` and `Release`

Replace custom solution configurations like `Debug-Rhino6` with standard configurations and explicit MSBuild properties.

Examples:

```powershell
dotnet build src/Swiftlet.Gh.Rhino7/Swiftlet.Gh.Rhino7.csproj -c Release
dotnet build src/Swiftlet.Gh.Rhino8/Swiftlet.Gh.Rhino8.csproj -c Release -p:TargetFramework=net8.0
```

### Centralize target metadata

Add a single target metadata file under `build/targets/` describing:

- Rhino major version
- TFM
- platform tag
- package layout
- bridge RID set

That metadata should drive CI and packaging, instead of hand-coded branching spread across project files and PowerShell.

### Separate build from packaging

Current state couples:

- build
- `.gha` rename
- bridge publish
- yak package creation

These should become separate steps:

1. compile plugin
2. stage output
3. publish bridge binaries
4. assemble yak layout
5. build package

## Packaging Model

### Rhino 6

- package/distribution: `rh6-win`
- no Linux target

### Rhino 7

- package/distribution: `rh7-win`
- no Linux target

### Rhino 8

Primary package:

- distribution: `rh8-any`

Package layout:

```text
swiftlet-x.y.z-rh8-any.yak
  manifest.yml
  net8.0/
    Swiftlet.gha
    ...
```

If you need Rhino 8 on Windows running .NET Framework:

```text
swiftlet-x.y.z-rh8-any.yak
  manifest.yml
  net48/
    Swiftlet.gha
    ...
  net8.0/
    Swiftlet.gha
    ...
```

### Rhino 9

Likely another `rh9-any` package with a `net8.0` or newer subfolder when McNeel finalizes the runtime story.

## Feature-Complete Linux Strategy

Linux parity cannot mean "retarget the existing project and hope". The current Windows implementation uses several APIs that are either unsupported or incorrect for Linux.

### 1. Bitmap and image features

Current Windows-sensitive surface:

- `BitmapGoo`
- `BitmapParam`
- `ByteArrayToBitmap`
- `BitmapToByteArray`
- `BitmapToMesh`
- `GenerateQRCode`
- color and image helpers

#### Decision

Keep the public Swiftlet feature set, but replace the internal image representation.

Introduce:

- `SwiftletImage`

Suggested shape:

- encoded bytes
- width
- height
- pixel format
- lazy decoded backend image handle if needed

Then:

- `BitmapGoo` becomes a wrapper around `SwiftletImage`
- `BitmapParam` remains the public Grasshopper parameter type name
- component GUIDs and nicknames stay unchanged

This preserves user-facing behavior while removing hard reliance on `System.Drawing.Bitmap` for component data flow.

#### Why this matters

If image data continues to flow through the plugin as `System.Drawing.Bitmap`, Linux support will remain brittle or blocked.

### 2. QR code generation

Current code uses `QRCoder` and emits a `Bitmap`.

#### Decision

Split QR generation into two steps:

- payload generation
- rendering through `Swiftlet.Imaging`

Do not let QR code generation be the reason the whole plugin remains tied to GDI+.

### 3. OAuth browser launch

Current code launches the browser using `rundll32.exe`, which is Windows-only.

#### Decision

Replace with `IBrowserLauncher`.

Implementations:

- Windows desktop: shell-open URL
- Linux desktop: `xdg-open`
- future macOS: `open`
- headless: no launch, return URL for manual authorization

#### Important refinement

For Linux Compute or other headless hosts, opening a browser is not a valid assumption.

So the OAuth component should support two execution modes:

- interactive mode: launch browser automatically
- manual mode: output the authorization URL and wait for callback/pasted code

That is how you keep parity in practice for server environments.

### 4. Clipboard and message dialogs

Current code uses:

- `Clipboard.SetText`
- `MessageBox.Show`

#### Decision

Replace with host services:

- `IClipboardService`
- `INotificationService`

Desktop implementations should use Eto.

Headless implementations should:

- log
- return text
- set runtime messages

Clipboard should be treated as a convenience layer, not the only way to access generated data.

### 5. HTTP listener and MCP server

Current code relies on `HttpListener`.

That is not an acceptable long-term server abstraction for Linux because Microsoft documents `HttpListener` as being built on `HTTP.sys`, which is Windows-specific.

#### Decision

Move modern server features to a Kestrel-based implementation behind `IHttpServerFactory`.

Affected features:

- HTTP listener/server components
- MCP server component
- OAuth callback listener
- any future embedded server capability

Legacy Rhino 6/7 may continue to use the existing implementation until the modern stack is complete, but the shared architecture should no longer depend on `HttpListener`.

### 6. Headless vs desktop capability

Linux support needs to cover both:

- future desktop Rhino/Grasshopper on Linux if McNeel goes there
- Rhino.Compute/headless execution

That requires explicit capability checks.

Introduce:

- `IHostCapabilities`

Minimum flags:

- `CanLaunchBrowser`
- `CanUseClipboard`
- `CanShowDialogs`
- `CanAcceptLocalHttpCallbacks`

Components with interactive behavior must branch through host capabilities instead of assuming a desktop session.

## Concrete Migration Phases

### Phase 0. Freeze behavior and add tests

Before moving files around:

- add tests for URL validation
- add tests for OAuth URL generation and PKCE
- add tests for image conversions and QR outputs
- add tests for MCP JSON-RPC formatting

### Phase 1. Create the new solution layout

- create `src/`, `tests/`, and `build/`
- move `SwiftletBridge` under `src/`
- create empty projects:
  - `Swiftlet.Core`
  - `Swiftlet.Imaging`
  - `Swiftlet.HostAbstractions`
  - `Swiftlet.Hosts.Desktop`
  - `Swiftlet.Hosts.Headless`
  - `Swiftlet.Gh.Rhino6`
  - `Swiftlet.Gh.Rhino7`
  - `Swiftlet.Gh.Rhino8`

Do not move component code yet.

### Phase 2. Extract pure logic into `Swiftlet.Core`

Move first:

- utilities with no Rhino dependencies
- request/response models
- JSON/XML/HTML helpers
- URL validation and blacklist logic
- MCP protocol message construction

### Phase 3. Build the imaging layer

- introduce `SwiftletImage`
- move bitmap conversion logic to `Swiftlet.Imaging`
- port QR generation to the new imaging layer
- adapt Grasshopper bitmap goo/param to the new model

This is the most important prerequisite for Linux parity.

### Phase 4. Replace host interaction code

- browser launch abstraction
- clipboard abstraction
- notification abstraction
- file dialog abstraction if needed

Move `McpServerComponent` and `OAuthAuthorizeComponent` off direct WinForms calls.

### Phase 5. Replace `HttpListener` in the modern family

- implement Kestrel-based server layer
- migrate:
  - MCP server
  - HTTP listener/server components
  - OAuth callback listener

Leave Rhino 6/7 on legacy server code if necessary during transition.

### Phase 6. Split Rhino shells

- create thin Rhino 6 shell
- create thin Rhino 7 shell
- create thin Rhino 8 shell
- wire shared source includes

At this point the old all-in-one project can be retired.

### Phase 7. Rebuild packaging

- remove post-build packaging from plugin projects
- generate stage folders under `artifacts/`
- build Yak packages from staged outputs
- include bridge binaries by RID

### Phase 8. Linux validation

Validate in this order:

1. unit tests on Windows
2. Rhino 8 desktop on Windows using `net8.0`
3. Rhino.Compute on Linux
4. Yak packaging and install verification

## Compatibility Rules

To avoid breaking existing users and Grasshopper files:

- keep component GUIDs unchanged
- keep component names and nicknames unchanged
- keep parameter names and nicknames unchanged
- keep wire-level data semantics unchanged
- preserve old Windows convenience behavior when the host supports it

Internal implementation can change. Public component identity cannot.

## Immediate Next Steps

1. Create the new solution skeleton beside the current project, not in-place.
2. Extract `Swiftlet.Core` first.
3. Build `Swiftlet.Imaging` before touching Linux packaging.
4. Replace `HttpListener` in the Rhino 8 path before claiming Linux support.
5. Add headless-aware OAuth behavior instead of only porting browser launch.

## External References

- Rhino 8 moved to .NET Core and Rhino 8.20+ defaults to .NET 8:
  - https://developer.rhino3d.com/en/guides/rhinocommon/moving-to-dotnet-core/
- RhinoCommon is the cross-platform SDK and Rhino recommends Eto for cross-platform UI:
  - https://developer.rhino3d.com/en/guides/rhinocommon/what-is-rhinocommon/
  - https://developer.rhino3d.com/guides/eto/what-is-eto/
  - https://developer.rhino3d.com/guides/eto/clipboard/
- Yak supports multi-targeted packages for Rhino 8+:
  - https://developer.rhino3d.com/guides/yak/the-anatomy-of-a-package/
- Microsoft documents `HttpListener` as being built on `HTTP.sys`:
  - https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-net-httplistener
- Microsoft documents `System.Drawing.Common` as Windows-only on modern .NET:
  - https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/system-drawing-common-windows-only
