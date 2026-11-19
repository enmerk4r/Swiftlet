# Changelog

## 0.4.0 - 2026-11-18

### Added

- Native Grasshopper 2 version for Rhino 9: all 115 components and 19 parameters, with vector icons. One `swiftlet` package installs Grasshopper 1 on Rhino 8 and both versions on Rhino 9.
- Grasshopper 1 definitions convert to native Grasshopper 2 components. The deprecated Socket Listener and Create Multipart Form Body Named/Unnamed stay hosted Grasshopper 1 objects.
- A Grasshopper 2 `.ghz` file for every example. Examples that send a request or start a server on opening are saved disabled and marked ENABLE ME!

### Changed (both versions)

- Create Text Body keeps the content type chosen in its menu.
- ImageSharp 3.2.0, fixing six security advisories.
- `BLOCKED_SUBNETS` accepts IPv6 ranges starting with `::`; a malformed value now blocks IPv6 as well as IPv4.
- WebSocket Client stops trying to connect after 10 seconds and reports why.
- Icons redrawn from shared vector masters.

### Fixed (both versions)

- HTTP Listener accepts ports up to 65535; listeners and servers reject port 0.
- Server Input and MCP Server keep unanswered requests across recomputes, and answer them with an error when a route or tool is removed or the server stops.
- Undoing the removal of an MCP tool restores its output and wires.
- OAuth, file transfer and HTTP requests behave correctly when cancelled, repeated or run concurrently.
- Read JSON Array/Object keep JSON nulls; Create CSV Line handles empty input and multi-character delimiters.

### Not yet tested

- Inside Rhino for Mac, and the macOS Intel bridge. Linux and macOS arm64 are covered by CI only.
