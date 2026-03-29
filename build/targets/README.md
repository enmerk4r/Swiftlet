# Targets

This folder contains target metadata for the staged packaging pipeline.

## Current Targets

- `rhino8.json`
  - minimum Rhino major supported by the package
  - Rhino majors expected to load the same binary
  - Yak compatibility major inferred from the referenced RhinoCommon SDK
  - plugin project and artifact names
  - bridge project and runtime publish targets
  - shared version source file
- `rhino9.json`
  - packages the same Rhino 8 shell for Rhino 9 discovery on Yak
  - re-tags the generated package files to `rh9-*` after `yak build`
  - keeps the shell/build project unchanged while Rhino 8 binaries remain compatible in Rhino 9

The packaging scripts read this metadata instead of hard-coding Rhino-specific paths directly in project post-build targets.

## Notes

- The current active shell is `src/Swiftlet.Gh.Rhino8/Swiftlet.Gh.Rhino8.csproj`.
- That shell is intended to support Rhino 8 and newer while the binary remains compatible.
- The `rhino9` packaging target is a packaging-layer retag of the same built binary for Yak discovery.
- If Rhino 9 ever requires a different binary, `rhino9.json` should switch to a real Rhino 9 shell/project instead of retagging the Rhino 8 build.
