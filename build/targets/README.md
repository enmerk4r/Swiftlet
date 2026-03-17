# Targets

This folder contains target metadata for the staged packaging pipeline.

## Current Targets

- `rhino8.json`
  - Rhino major version
  - plugin project and artifact names
  - bridge project and runtime publish targets
  - package platform tag
  - version source project

The packaging scripts read this metadata instead of hard-coding Rhino-specific paths directly in project post-build targets.
