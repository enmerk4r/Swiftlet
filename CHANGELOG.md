# Changelog

## 0.4.0 - 2026-11-18

### Added

- Swiftlet for Grasshopper 2 on Rhino 9, alongside the Grasshopper 1 version. One `swiftlet` package installs Grasshopper 1 on Rhino 8 and both versions on Rhino 9.
- Grasshopper 1 definitions opened in Grasshopper 2 convert to native Swiftlet components. The deprecated Socket Listener and Create Multipart Form Body Named/Unnamed have no Grasshopper 2 version and keep running as Grasshopper 1 components.
- A Grasshopper 2 `.ghz` file for every example. Examples that send a request or start a server are saved disabled and marked ENABLE ME!

### Changed

- Create Text Body keeps the content type chosen in its menu.
- ImageSharp 3.2.0, which fixes six security advisories.
- `BLOCKED_SUBNETS` accepts IPv6 ranges starting with `::`.
- WebSocket Client stops trying to connect after 10 seconds and reports why.
- New icons.

### Fixed

- HTTP Listener accepts ports up to 65535.
- Server Input and MCP Server no longer lose unanswered requests on recompute.
- Undoing the removal of an MCP tool restores its output and wires.
- OAuth, file transfers and HTTP requests behave correctly when cancelled or restarted.
- Read JSON Array/Object keep JSON nulls.

### Not yet tested

- Inside Rhino for Mac. If you try it there, please open an issue.
