# Packaging

Modern packaging lives here.

## Entry Point

Use `build/packaging/Publish-Target.ps1` from the repo root:

```powershell
powershell -ExecutionPolicy Bypass -File .\build\packaging\Publish-Target.ps1 -Target rhino8 -Configuration Release
powershell -ExecutionPolicy Bypass -File .\build\packaging\Publish-Target.ps1 -Target rhino9 -Configuration Release
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
- publishes `SwiftletBridge` by RID
- builds one local `.yak` package for `any`
- assembles a Linux Compute package layout with setup docs and an MCP config template
- stages a Rhino.Compute install helper that installs the local `.yak` for the `rhino-compute` user
- copies the bridge binaries into `bridge/<rid>/` inside the packaged plugin payload
- writes a machine-readable `artifact-manifest.json`

## Automatic Release Packaging

`src/Swiftlet.Gh.Rhino8` now invokes `Publish-Target.ps1` automatically after a Windows `Release` build.

- It packages from the existing build output via `-PluginOutputDir`, so it does not rebuild the plug-in.
- It passes `-NoRestore`, so it reuses the already-restored packages from the build machine.
- To disable this behavior for a specific build, pass `/p:AutoPackageOnReleaseBuild=false`.

## Current Target Model

- `rhino8`
  - builds from `src/Swiftlet.Gh.Rhino8/Swiftlet.Gh.Rhino8.csproj`
  - represents the active Rhino 8+ line
  - currently publishes bridge artifacts for `win-x64`, `linux-x64`, `osx-arm64`, and `osx-x64`
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
  - self-contained Windows bridge publish
- `bridge/linux-x64`
  - self-contained Linux bridge publish
- `bridge/osx-arm64`
  - self-contained macOS Apple Silicon bridge publish
- `bridge/osx-x64`
  - self-contained macOS Intel bridge publish
- `yak/any`
  - cross-platform Yak staging folder for Windows, Mac, and Linux installs
- `artifact-manifest.json`
  - summary of the staged outputs

## Notes

- `build/packaging/Publish-Rhino8.ps1` remains as a compatibility wrapper that forwards to `Publish-Target.ps1 -Target rhino8`.
- The `rhino9` target is a packaging-layer compatibility target. It does not change the compiled shell project.
