# Changelog

## 0.4.0 - Unreleased

### Grasshopper 2

- Adds a native Rhino 9 Grasshopper 2 version covering all 115 current components and 19 custom parameters, alongside the existing Grasshopper 1 version.
- Uses one `swiftlet` Yak package: Rhino 8 carries GH1; Rhino 9 carries GH1 and GH2 with the same shared libraries. [cd5cda1](https://github.com/enmerk4r/Swiftlet/commit/cd5cda1b5932e566387f4495faa5e5bd026d9835)
- Converts current Swiftlet components and floating parameters in GH1 definitions to native GH2 objects, including saved HTTP timeouts, text-body content types and server output names.
- Converts archived Create Text Body to the current component and archived three-input MCP Tool Response to a hidden native compatibility component with its original result format. [42fe962](https://github.com/enmerk4r/Swiftlet/commit/42fe962ec715dbd0b5e1ed351713350d07196d9a) [402882c](https://github.com/enmerk4r/Swiftlet/commit/402882c63f3aa12301148b7a8e0efa0384c72232)
- Leaves archived Socket Listener and Create Multipart Form Body Named/Unnamed on GH1 interop hosts; third-party and scripting components may also need GH1 hosting. [8c5805d](https://github.com/enmerk4r/Swiftlet/commit/8c5805d3f0ae15d3a34a6a9f57560c39e55e8625)
- Adds 15 `.ghz` example counterparts, retaining the original `.gh` files and recreating their notes; the OpenAI example was reopened but not executed without a key. [c3d55bb](https://github.com/enmerk4r/Swiftlet/commit/c3d55bb38f2a865610b6a1007f3668df2b271cc6)
- Keeps GH1's optional-input behavior for HTML Recursive, CSV Delimiter, Byte Array To Text Encoding and the four QR settings, including fallback values when their pin data is cleared. [65baefe](https://github.com/enmerk4r/Swiftlet/commit/65baefeb9c75c9d8e7403ccfb4d7bf8ab07ace4d)
- Uses vector icons and palette groups matching GH1's exposure rows. [d731741](https://github.com/enmerk4r/Swiftlet/commit/d731741bd924810768678b3d7e90e15125853a19) [f54cddc](https://github.com/enmerk4r/Swiftlet/commit/f54cddc104f72c8fb067ae1852a24f10a404856c)

### Changes affecting Grasshopper 1

These changes also apply to GH2 where the implementation is shared; the GH1 assembly name and component GUIDs are retained.

- Icons now use PNGs generated from the shared vector masters, including flat origami shadows and redrawn parameter hexagons. [a25f129](https://github.com/enmerk4r/Swiftlet/commit/a25f129a6c1e2f3dc7357fabb5906edd9d6f9422)
- Create CSV Line handles an empty cell list and removes an entire trailing delimiter, including multi-character delimiters. [5e0982f](https://github.com/enmerk4r/Swiftlet/commit/5e0982f9ae0bbe3dbe6dbafc48b0703217422535)
- Read JSON Array and Read JSON Object preserve JSON null elements as valid JSON-null values. [42fe962](https://github.com/enmerk4r/Swiftlet/commit/42fe962ec715dbd0b5e1ed351713350d07196d9a)
- HTTP requests no longer deadlock when a synchronous caller occupies the context needed by their asynchronous continuation; this was reproduced with a programmatically solved GH1 graph. [83e0599](https://github.com/enmerk4r/Swiftlet/commit/83e05998fcb6d4d10ff1af9cb1050665cec6f16a)
- Viewport capture executes directly when already on Rhino's UI thread instead of dispatching back to that thread and waiting. [a4513b3](https://github.com/enmerk4r/Swiftlet/commit/a4513b3ef26aa37521fff582e4f1df8aa2a95f8b)
- HTTP Listener accepts ports through 65535 instead of rejecting ports above the previous typo, 65353. [c49296f](https://github.com/enmerk4r/Swiftlet/commit/c49296f8dcbb087fef6d14d6f77eb76ea1534f30)
- Server Input retains an unanswered request across recomputes and advances its route queue when that request receives a response. [58b28dd](https://github.com/enmerk4r/Swiftlet/commit/58b28ddfa3b74b48c574e1ee7678f4d77b508fc2)
- Server Input finishes removed-route or stopped-server requests with HTTP 503 and marks canceled response waits complete with HTTP 408. [58b28dd](https://github.com/enmerk4r/Swiftlet/commit/58b28ddfa3b74b48c574e1ee7678f4d77b508fc2)
- Server Input keeps replies attached to their original bridge connection across restarts and closes pending waits when that bridge stops. [58b28dd](https://github.com/enmerk4r/Swiftlet/commit/58b28ddfa3b74b48c574e1ee7678f4d77b508fc2)
- Server Input reconnects its notifications after being added back to a document and prevents inserting a route before Root. [58b28dd](https://github.com/enmerk4r/Swiftlet/commit/58b28ddfa3b74b48c574e1ee7678f4d77b508fc2)
- Server Input ignores callbacks queued before its removal or document closure. [e9c1cf1](https://github.com/enmerk4r/Swiftlet/commit/e9c1cf1b55d15a62569a89883940c6474817d837)
- Download File, Upload File and Upload File Multipart ignore completion from a canceled job after Run was reset, allowing the next run to start without a stale failure. [6728044](https://github.com/enmerk4r/Swiftlet/commit/672804441460f91fab66d2650efe1a06fc12a819)
- OAuth Token no longer reuses an already redeemed authorization code when Refresh changes from true back to false. [6d708ea](https://github.com/enmerk4r/Swiftlet/commit/6d708ea2251edf7344e600d537476d34472e81cd)
- OAuth Token cancels requests on component removal or document closure and allows a canceled refresh to be retried without losing its trigger. [6d708ea](https://github.com/enmerk4r/Swiftlet/commit/6d708ea2251edf7344e600d537476d34472e81cd)
- OAuth network continuations no longer depend on resuming on the calling UI context. [6d708ea](https://github.com/enmerk4r/Swiftlet/commit/6d708ea2251edf7344e600d537476d34472e81cd)
- OAuth Authorize ignores queued completion notifications from a superseded or stopped authorization attempt. [9597f5d](https://github.com/enmerk4r/Swiftlet/commit/9597f5d0b93a2b20441f73cb4ab4af6d714ec99d)
- OAuth reset and disposal now wait for callback listeners and incomplete request readers to stop before returning, so their port can be reused immediately. [3f2521c](https://github.com/enmerk4r/Swiftlet/commit/3f2521c5e0a52d630c0a466d80b43e7311bece00)
- OAuth ignores empty or malformed connections, returns 404 for unrelated paths, and reads simultaneous clients independently so idle browser preconnects do not consume or delay the real callback. [fa8de0a](https://github.com/enmerk4r/Swiftlet/commit/fa8de0ab261a0e46171c7f3153ddd4657647da6f)
- MCP Server retains unanswered calls across recomputes and advances each tool's queue after a successful response. [402882c](https://github.com/enmerk4r/Swiftlet/commit/402882c63f3aa12301148b7a8e0efa0384c72232)
- MCP Server completes pending calls with an error when their tool is removed or the server stops instead of silently discarding them. [402882c](https://github.com/enmerk4r/Swiftlet/commit/402882c63f3aa12301148b7a8e0efa0384c72232)
- MCP Server isolates replies from old bridge instances, serializes tool removal against incoming calls, and dispatches notifications without blocking bridge shutdown on the UI. [402882c](https://github.com/enmerk4r/Swiftlet/commit/402882c63f3aa12301148b7a8e0efa0384c72232)
- MCP Server restores notifications when added back to a document and ignores delayed callbacks after shutdown. [402882c](https://github.com/enmerk4r/Swiftlet/commit/402882c63f3aa12301148b7a8e0efa0384c72232)
- Shared DLLs now carry the Swiftlet release version instead of an indistinguishable `1.0.0.0` identity. [49f1ba3](https://github.com/enmerk4r/Swiftlet/commit/49f1ba3b1dce5869bca000fd2ae937b38125f657)

For callers of the shared HTTP API, explicit caller cancellation now propagates as cancellation rather than an HTTP failure response; existing GH1 request components do not supply that token, and timeout responses remain unchanged. [8ee85ff](https://github.com/enmerk4r/Swiftlet/commit/8ee85ff5c8bb2e30f3d8aa6a62b980741c17b484)

### Compatibility and verification notes

- Windows Rhino 9.1 checks cover 239 GH1/GH2 cases, 134 migration rules, and whole-file conversion of 21 examples; two examples retain only the archived Swiftlet components listed above as GH1 hosts. [4350fd9](https://github.com/enmerk4r/Swiftlet/commit/4350fd92e266c9bcf754a5c98dc41ac31a12016b) [8c5805d](https://github.com/enmerk4r/Swiftlet/commit/8c5805d3f0ae15d3a34a6a9f57560c39e55e8625)
- Whole-file checks report missing third-party objects and non-Swiftlet wire differences; they are not a promise that arbitrary third-party definitions convert losslessly. [8c5805d](https://github.com/enmerk4r/Swiftlet/commit/8c5805d3f0ae15d3a34a6a9f57560c39e55e8625)
- In both versions, undoing removal of an MCP tool restores its output but loses downstream wires; this existing limitation is recorded for review. [805d157](https://github.com/enmerk4r/Swiftlet/commit/805d157f4113b241de2c42493c040eb36c8d2d0b)
- GH2 Create Text Body applies a detected JSON/XML/HTML content type to that output without changing the saved picker value, whereas GH1 changes its saved choice. [42fe962](https://github.com/enmerk4r/Swiftlet/commit/42fe962ec715dbd0b5e1ed351713350d07196d9a)
- Download File retains its existing URL-validation behavior and 32-bit Size output, which can wrap above 2 GB. [6728044](https://github.com/enmerk4r/Swiftlet/commit/672804441460f91fab66d2650efe1a06fc12a819)
- Programmatic settings and undo checks passed; pointer/focus checks, macOS runtime checks, and final combined network stress remain separate release checks. [805d157](https://github.com/enmerk4r/Swiftlet/commit/805d157f4113b241de2c42493c040eb36c8d2d0b)

This entry describes the release branch, not a published release.
