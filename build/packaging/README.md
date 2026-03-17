# Packaging

Modern packaging for the Rhino 8 port lives here.

## Entry Point

Use `build/packaging/Publish-Rhino8.ps1` from the repo root:

```powershell
powershell -ExecutionPolicy Bypass -File .\build\packaging\Publish-Rhino8.ps1 -Configuration Release
```

If the machine already has the required NuGet packages restored and you want to avoid hitting NuGet again, use:

```powershell
powershell -ExecutionPolicy Bypass -File .\build\packaging\Publish-Rhino8.ps1 -Configuration Release -NoRestore
```

If you already built the Rhino 8 plug-in in Visual Studio and want to package from that existing output directly, use:

```powershell
powershell -ExecutionPolicy Bypass -File .\build\packaging\Publish-Rhino8.ps1 -Configuration Debug -NoRestore -PluginOutputDir .\src\Swiftlet.Gh.Rhino8\bin\Debug\net7.0
```

The script:

- builds the Rhino 8 Grasshopper shell into a staged Windows plugin folder
- publishes `SwiftletBridge` for `win-x64`
- publishes `SwiftletBridge` for `linux-x64`
- builds local `.yak` packages for both `win` and `any`
- assembles a Linux Compute package layout with setup docs and an MCP config template
- stages a Rhino.Compute install helper that installs the local `.yak` for the `rhino-compute` user
- copies the Windows bridge beside the `.gha` so MCP config generation works out of the box
- creates a built Windows Yak package
- creates a built cross-platform Yak package
- writes a machine-readable `artifact-manifest.json`

## Output Layout

Artifacts are written to:

```text
artifacts/publish/rhino8/<version>/
```

Key folders:

- `plugin/windows`
  - loadable Rhino 8 / Grasshopper payload
  - includes `Swiftlet.Gh.Rhino8.gha`
  - includes `SwiftletBridge.exe`
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
- `yak/win`
  - Windows Yak staging folder with generated `manifest.yml`
  - includes a built `swiftlet-<version>-rh8_0-win.yak`
- `yak/any`
  - cross-platform Yak staging folder for Linux/Compute installs
  - includes a built `swiftlet-<version>-rh8_0-any.yak`
- `artifact-manifest.json`
  - summary of the staged outputs

## Notes

- The legacy `Yak/Build-YakPackage.ps1` script is still for the old Windows-only plugin and should not be used for the Rhino 8 modern shell.
- The Rhino 8 `.gha` is still built from the Windows-hosted workflow, but the modern pipeline now also emits an `any` Yak package so the same plug-in payload can be installed into Linux Rhino.Compute.
- The Linux deliverable in the modern pipeline is the `linux/compute` package, which now includes both the self-contained `SwiftletBridge` publish and the local Swiftlet `.yak` for Rhino.Compute installation.
