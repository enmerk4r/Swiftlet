# Packaging

Modern packaging lives here.

## Entry Point

Use `build/packaging/Publish-Target.ps1` from the repo root:

```powershell
powershell -ExecutionPolicy Bypass -File .\build\packaging\Publish-Target.ps1 -Target rhino8 -Configuration Release
powershell -ExecutionPolicy Bypass -File .\build\packaging\Publish-Target.ps1 -Target rhino9 -Configuration Release
```

## One-Command Release Build

Use `build/packaging/Build-YakPackage.ps1` when you want the local machine to drive the whole release packaging loop:

```powershell
powershell -ExecutionPolicy Bypass -File .\build\packaging\Build-YakPackage.ps1 -Target rhino8 -Configuration Release
```

The script:

- requires a clean working tree by default
- verifies `origin/<current-branch>` points at the local `HEAD`
- optionally runs `dotnet test Swiftlet.sln -c Release`
- manually triggers `.github/workflows/bridge-aot-artifacts.yml` with `gh workflow run`
- waits for that build-only workflow to complete
- downloads the Linux and Apple Silicon bridge artifacts
- verifies the uploaded SHA-256 checksums
- extracts them into `artifacts/prebuilt-bridge`
- invokes `Publish-Target.ps1` with `-PrebuiltBridgeRoot`

This keeps CI manual and build-only. The workflow still has read-only repository permissions and no Yak credentials or publishing secrets. Publishing to Yak remains a separate local step.

Prerequisites:

- GitHub CLI (`gh`) installed and authenticated
- `git`, `dotnet`, `tar`, and Yak available locally
- the branch pushed before running the script

Useful options:

```powershell
powershell -ExecutionPolicy Bypass -File .\build\packaging\Build-YakPackage.ps1 -Target rhino8 -SkipTests
powershell -ExecutionPolicy Bypass -File .\build\packaging\Build-YakPackage.ps1 -Target rhino8 -Branch your-branch-name
```

For release packaging, provide prebuilt CI bridge artifacts for the RIDs that cannot be built on the local host:

```powershell
powershell -ExecutionPolicy Bypass -File .\build\packaging\Publish-Target.ps1 -Target rhino8 -Configuration Release -PrebuiltBridgeRoot .\artifacts\prebuilt-bridge
```

The prebuilt root may contain subfolders named by bridge `id` or RID, for example:

```text
artifacts/prebuilt-bridge/
├── linux-x64/SwiftletBridge
└── osx-arm64/SwiftletBridge
```

If the machine already has the required NuGet packages restored and you want to avoid hitting NuGet again, use:

```powershell
powershell -ExecutionPolicy Bypass -File .\build\packaging\Publish-Target.ps1 -Target rhino8 -Configuration Release -NoRestore
```

If you already built the plug-in in Visual Studio and want to package from that existing output directly, use:

```powershell
powershell -ExecutionPolicy Bypass -File .\build\packaging\Publish-Target.ps1 -Target rhino8 -Configuration Debug -NoRestore -PluginOutputDir .\src\Swiftlet.Gh.Rhino8\bin\Debug\net7.0
```

The script:

- builds the target shell into a staged plugin folder
- publishes `SwiftletBridge` by RID, or copies matching prebuilt bridge artifacts from `-PrebuiltBridgeRoot`
- builds one local `.yak` package for `any`
- assembles a Linux Compute package layout with setup docs and an MCP config template
- stages a Rhino.Compute install helper that installs the local `.yak` for the `rhino-compute` user
- copies the bridge binaries into `bridge/<rid>/` inside the packaged plugin payload
- writes a machine-readable `artifact-manifest.json`

## Optional Release Packaging

`src/Swiftlet.Gh.Rhino8` can invoke `Publish-Target.ps1` after a Windows `Release` build, but this is disabled by default because release packaging now expects Linux and macOS ARM bridge artifacts from the build-only CI workflow.

- It packages from the existing build output via `-PluginOutputDir`, so it does not rebuild the plug-in.
- It passes `-NoRestore`, so it reuses the already-restored packages from the build machine.
- To opt in for a specific build, pass `/p:AutoPackageOnReleaseBuild=true`.
- If CI bridge artifacts are needed, also pass `/p:PackagingPrebuiltBridgeRoot=<path>`.

## Current Target Model

- `rhino8`
  - builds from `src/Swiftlet.Gh.Rhino8/Swiftlet.Gh.Rhino8.csproj`
  - represents the active Rhino 8+ line
  - currently packages bridge artifacts for `win-x64`, `linux-x64`, `osx-arm64`, and `osx-x64`
  - uses Native AOT for `win-x64`, `linux-x64`, and `osx-arm64`
  - keeps `osx-x64` as a self-contained single-file bridge
  - produces a single `rh8-any` Yak package because Yak compatibility is derived from the referenced RhinoCommon SDK
- `rhino9`
  - builds from the same `src/Swiftlet.Gh.Rhino8/Swiftlet.Gh.Rhino8.csproj`
  - stages the same binary for Rhino 9 discovery
  - re-tags the generated `any` package to `rh9-*` after `yak build`

## Output Layout

Artifacts are written to:

```text
artifacts/publish/<target>/<version>/
```

For the current `rhino8` target that means:

```text
artifacts/publish/rhino8/<version>/
```

Key folders:

- `plugin/any`
  - cross-platform plugin payload used for all Yak distribution
  - includes `Swiftlet.Gh.Rhino8.gha`
  - includes `bridge/win-x64/SwiftletBridge.exe`
  - includes `bridge/linux-x64/SwiftletBridge`
  - includes `bridge/osx-arm64/SwiftletBridge`
  - includes `bridge/osx-x64/SwiftletBridge`
- `linux/compute`
  - Linux-facing package layout for Rhino.Compute and other headless hosts
  - includes `packages/*.yak`
  - includes `install-compute-plugin.sh`
  - includes `bridge/SwiftletBridge`
  - includes `install.sh`
  - includes `examples/claude-desktop.mcp.template.json`
- `bridge/win-x64`
  - Native AOT Windows bridge publish
- `bridge/linux-x64`
  - Native AOT Linux bridge publish, usually provided by the CI artifact workflow
- `bridge/osx-arm64`
  - Native AOT macOS Apple Silicon bridge publish, usually provided by the CI artifact workflow
- `bridge/osx-x64`
  - self-contained macOS Intel bridge publish
- `.github/workflows/bridge-aot-artifacts.yml`
  - manually triggered, build-only workflow for `linux-x64` and `osx-arm64` Native AOT bridge artifacts
  - uses read-only repository permissions and no release secrets
- `yak/any`
  - cross-platform Yak staging folder for Windows, Mac, and Linux installs
- `artifact-manifest.json`
  - summary of the staged outputs

## Notes

- `build/packaging/Publish-Rhino8.ps1` remains as a compatibility wrapper that forwards to `Publish-Target.ps1 -Target rhino8`.
- The `rhino9` target is a packaging-layer compatibility target. It does not change the compiled shell project.
