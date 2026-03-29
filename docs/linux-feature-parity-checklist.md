# Linux Feature Parity Checklist

Note: the active repository direction is now Rhino 8+ only. Older Rhino references below are historical cleanup items from the original migration plan.

This checklist translates the current Windows implementation into explicit cross-platform work items.

## Build and Packaging

- [ ] Replace custom solution configurations (`Debug-Rhino6`, `Release-Rhino8`, etc.) with standard `Debug` and `Release`.
- [ ] Move Rhino-version selection into project boundaries and build metadata.
- [ ] Remove Yak packaging from plugin post-build steps.
- [ ] Publish `SwiftletBridge` independently by RID.
- [ ] Add Linux staging output for Rhino 8+ package generation.
- [ ] Remove legacy Rhino 6 and Rhino 7 from the active development path.

## Core Extraction

- [ ] Move request, response, DTO, parsing, and validation logic into `Swiftlet.Core`.
- [ ] Keep RhinoCommon and Grasshopper references out of `Swiftlet.Core`.
- [ ] Add tests for URL validation, JSON/XML/HTML parsing, and blacklist behavior.

## Imaging

### Current Windows-sensitive surface

- `Swiftlet/Goo/BitmapGoo.cs`
- `Swiftlet/Params/BitmapParam.cs`
- `Swiftlet/Components/7_Utilities/ByteArrayToBitmap.cs`
- `Swiftlet/Components/7_Utilities/BitmapToByteArray.cs`
- `Swiftlet/Components/7_Utilities/BitmapToMesh.cs`
- `Swiftlet/Components/7_Utilities/GenerateQRCode.cs`

### Required work

- [ ] Introduce a cross-platform image model (`SwiftletImage`).
- [ ] Replace `System.Drawing.Bitmap` as the internal bitmap data contract.
- [ ] Port encode/decode logic to a cross-platform backend.
- [ ] Port QR rendering to the new image backend.
- [ ] Preserve current component GUIDs and parameter identities.
- [ ] Add image round-trip tests.
- [ ] Add QR snapshot tests or byte-level output tests.
- [ ] Add mesh-generation tests based on known image input.

## OAuth

### Current Windows-sensitive surface

- `Swiftlet/Components/1_Auth/OAuthAuthorizeComponent.cs`

### Required work

- [ ] Replace `rundll32.exe` browser launch with `IBrowserLauncher`.
- [ ] Support desktop launch on Windows.
- [ ] Support desktop launch on Linux.
- [ ] Add a headless/manual authorization mode for Rhino.Compute.
- [ ] Move callback listener behind a server abstraction.
- [ ] Add tests for PKCE verifier/challenge generation.
- [ ] Add tests for auth URL construction.

## MCP Server

### Current Windows-sensitive surface

- `Swiftlet/Components/9_Mcp/McpServerComponent.cs`
- `SwiftletBridge/Program.cs`

### Required work

- [ ] Move MCP protocol logic into `Swiftlet.Core`.
- [ ] Replace direct clipboard dependency with a service.
- [ ] Make config generation accessible without clipboard.
- [ ] Replace `HttpListener` in the modern path.
- [ ] Publish `SwiftletBridge` for Linux as well as Windows.
- [ ] Add MCP request/response contract tests.

## HTTP Server Features

### Current Windows-sensitive surface

- `Swiftlet/Components/8_Serve/HttpListener.cs`
- `Swiftlet/Components/8_Serve/ServerInputComponent.cs`
- `Swiftlet/Components/8_Serve/ServerResponseComponent.cs`
- `Swiftlet/Components/8_Serve/DeconstructRequestComponent.cs`
- `Swiftlet/EventArgs/RequestReceivedEventArgs.cs`
- `Swiftlet/Goo/ListenerRequestGoo.cs`

### Required work

- [ ] Replace `HttpListener` with a cross-platform server abstraction for Rhino 8+.
- [ ] Preserve current request/response component semantics.
- [ ] Validate localhost binding behavior on Linux.
- [ ] Add request routing and response tests.

## WebSockets and UDP

### Current surface

- `Swiftlet/Components/8_Serve/WebSocketClientComponent.cs`
- `Swiftlet/Components/8_Serve/WebSocketServerComponent.cs`
- `Swiftlet/Components/8_Serve/WebSocketSendComponent.cs`
- `Swiftlet/Components/8_Serve/UdpListener.cs`
- `Swiftlet/Components/3_Send/UdpStreamComponent.cs`

### Required work

- [ ] Confirm WebSocket client behavior on Linux.
- [ ] Confirm WebSocket server behavior after replacing `HttpListener`.
- [ ] Confirm UDP listener/send behavior on Linux.
- [ ] Add integration tests for WebSocket send/receive.
- [ ] Add integration tests for UDP send/receive.

## Host Interaction and UI

### Current Windows-sensitive surface

- `Swiftlet/Components/9_Mcp/McpServerComponent.cs`
- `Swiftlet/Components/3_Send/BaseRequestComponent.cs`
- `Swiftlet/Components/2_Request/CreateTextBody.cs`
- `Swiftlet/Components/7_Utilities/SaveWebResponse.cs`

### Required work

- [ ] Replace clipboard access with `IClipboardService`.
- [ ] Replace dialogs/messages with Eto-backed notifications where needed.
- [ ] Distinguish desktop-capable and headless-capable host behavior.
- [ ] Avoid making clipboard or dialogs the only path to a feature.

## Grasshopper Shell Compatibility

- [ ] Keep component GUIDs unchanged.
- [ ] Keep existing nicknames and categories unchanged.
- [ ] Keep existing parameter names unchanged.
- [ ] Keep component wire semantics unchanged.
- [ ] Preserve old Windows behavior where the host supports it.

## Testing and Validation

- [ ] Add unit tests for extracted core logic.
- [ ] Add imaging tests.
- [ ] Add MCP contract tests.
- [ ] Add OAuth tests.
- [ ] Add Linux CI leg for non-Rhino unit tests.
- [ ] Validate Rhino 8 desktop on Windows with the modern shell.
- [ ] Validate Rhino.Compute on Linux with the modern shell.

## Exit Criteria

Linux parity is complete only when all of the following are true:

- [ ] Swiftlet builds as a Rhino 8+ modern shell without project-wide WinForms/WPF assumptions.
- [ ] Bitmap and QR components work without `System.Drawing` as the internal image pipeline.
- [ ] OAuth works in both interactive desktop mode and headless/manual mode.
- [ ] MCP server works on Linux.
- [ ] HTTP server components work on Linux.
- [ ] Bridge binaries are packaged for Linux.
- [ ] Existing Grasshopper definitions continue to load without component identity breakage.
