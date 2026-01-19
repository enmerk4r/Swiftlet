Swiftlet is a Grasshopper plugin for making web requests to any arbitrary API. It is, in essence, cURL for Grasshopper - a general-purpose HTTP client. It also provides server-side capabilities for receiving HTTP requests and implementing MCP (Model Context Protocol) servers.

## Grasshopper Menu Structure

- **1. Auth**: Components for constructing auth headers (Basic Auth, Bearer Token, API Key)
- **2. Request**: Components for constructing request bodies, query params, custom headers, JSON payloads, multipart forms
- **3. Send**: Components implementing HTTP methods (GET, POST, PUT, DELETE, PATCH, HEAD, TRACE, CONNECT, OPTIONS) + Throttle + Deconstruct components (Response, Body, URL, Header, QueryParam)
- **4. Read JSON**: Components for parsing, querying, and manipulating JSON
- **5. Read HTML**: Components for parsing and querying HTML documents
- **6. Utilities**: Utility components (compression, base64, QR codes, file I/O, CSV, bitmaps)
- **7. Listen**: HTTP and socket listener components, plus Server Input/Response for building HTTP APIs
- **8. MCP**: Model Context Protocol server components for exposing Grasshopper tools to AI clients

## Project Structure

```
Swiftlet/                 # Main Grasshopper plugin
├── Components/           # Grasshopper component implementations
│   ├── 1_Auth/          # Authentication components
│   ├── 2_Request/       # Request building
│   ├── 3_Send/          # HTTP method implementations
│   ├── 4_ReadJson/      # JSON parsing components
│   ├── 5_ReadHtml/      # HTML parsing components
│   ├── 6_Utilities/     # Utility components
│   ├── 7_Serve/         # HTTP/socket listeners and server components
│   ├── 8_Mcp/           # MCP server components
│   └── Archived/        # Legacy/deprecated components
├── DataModels/
│   ├── Interfaces/      # Core abstractions (IRequestBody, IQueryParam, IHttpHeader, etc.)
│   └── Implementations/ # Concrete data models
├── Goo/                 # Grasshopper-specific type wrappers
├── Params/              # Custom Grasshopper parameter types
├── Util/                # Utility classes
├── EventArgs/           # Custom event argument types
└── Properties/          # Assembly info and embedded resources (icons)

SwiftletBridge/           # MCP stdio-to-HTTP bridge (separate project)
├── Program.cs           # Bridge implementation
└── SwiftletBridge.csproj

Yak/                      # Package distribution
├── Build-YakPackage.ps1 # Build script for Yak packages
├── manifest-template.yml # Package manifest template
└── dist-X.X.X/          # Distribution folders (created by build)
    ├── Swiftlet.gha
    ├── *.dll            # Dependencies
    └── mcp/             # MCP bridge executables
        ├── SwiftletBridge.exe          # Windows
        ├── SwiftletBridge-macos-x64    # macOS Intel
        └── SwiftletBridge-macos-arm64  # macOS Apple Silicon
```

## Build System

**Multi-Rhino Support:**
- Rhino 6: .NET Framework 4.6.2 (`RHINO6` define)
- Rhino 7: .NET Framework 4.8 (`RHINO7` define)
- Rhino 8: .NET 7.0-windows (`RHINO8` define)

**Dependencies:** RhinoCommon, Grasshopper, Newtonsoft.Json, HtmlAgilityPack, QRCoder

**Post-Build:** Assembly renamed from `.dll` to `.gha` (Grasshopper Assembly)

**Compatibility Notes:**
- Avoid using .NET Core/.NET 5+ only APIs (e.g., `ToHashSet()`) - use constructors instead (e.g., `new HashSet<T>(enumerable)`)
- All code must compile for .NET Framework 4.6.2

## Key Patterns

### Component Base Classes
- **`GH_Component`**: Standard base for most components
- **`BaseRequestComponent`**: Abstract base for HTTP request components with URL validation and IP blacklist checking
- **`IGH_VariableParameterComponent`**: Interface for components with dynamic inputs/outputs (used by ServerInputComponent, McpServerComponent)

### Data Flow
All custom types use the Goo wrapper pattern for Grasshopper integration:
- Data model in `DataModels/` → Goo wrapper in `Goo/` → Parameter type in `Params/`

### Core Interfaces
- `IRequestBody` - Request body with `ToHttpContent()` and `ToByteArray()`
- `IQueryParam` - Key-value for query parameters
- `IHttpHeader` - Key-value for HTTP headers
- `IHttpResponseDTO` - HTTP response metadata and content

### Goo Types
| Goo Type | Wraps | Purpose |
|----------|-------|---------|
| `RequestBodyGoo` | `IRequestBody` | Request bodies |
| `HttpHeaderGoo` | `HttpHeader` | HTTP headers |
| `QueryParamGoo` | `QueryParam` | Query parameters |
| `HttpWebResponseGoo` | `HttpResponseDTO` | HTTP responses |
| `JTokenGoo` / `JObjectGoo` / `JArrayGoo` / `JValueGoo` | Newtonsoft.Json types | JSON handling |
| `ByteArrayGoo` | `byte[]` | Binary data |
| `BitmapGoo` | `Bitmap` | Images |
| `HtmlNodeGoo` | `HtmlNode` | HTML DOM nodes |
| `MultipartFieldGoo` | `MultipartField` | Multipart form fields |
| `ListenerRequestGoo` | `HttpListenerContext` | Pending HTTP request/response context |
| `McpToolParameterGoo` | `McpToolParameter` | MCP tool parameter definition |
| `McpToolDefinitionGoo` | `McpToolDefinition` | MCP tool definition |
| `McpToolCallRequestGoo` | `McpToolCallContext` | Pending MCP tool call request |
| `WebSocketConnectionGoo` | `WebSocketConnection` | Open WebSocket connection for bidirectional messaging |

## Server Components (7_Serve)

Components for building HTTP APIs within Grasshopper:

### ServerInputComponent
- Listens for HTTP requests on a specified port
- Implements `IGH_VariableParameterComponent` for dynamic route outputs
- Default "/" output, users can add more routes via ZUI (e.g., "/compute/geometry")
- Outputs `ListenerRequestGoo` when a request matches a route
- Connection stays open until ServerResponseComponent sends response

### DeconstructRequestComponent
- Extracts data from `ListenerRequestGoo`: Method, Route, Headers, Query Params, Body (as `RequestBodyGoo`)
- Passes through the original request unchanged for downstream response

### ServerResponseComponent
- Takes `ListenerRequestGoo` + response data (body, status code, headers)
- Writes HTTP response and closes the connection
- Outputs success boolean

**Usage Flow:**
```
[Server Input] → [Deconstruct Request] → [Your Logic] → [Server Response]
      │                    │                                    │
      └── ListenerRequestGoo passes through unchanged ──────────┘
```

## WebSocket Components (7_Serve)

Components for bidirectional WebSocket communication. Uses a pass-through context pattern similar to HTTP server components.

### WebSocketClientComponent
- Connects to a WebSocket server as a client
- Inputs: URL, Query Params, Run (boolean)
- Outputs: Connection (`WebSocketConnectionGoo`), Message, Messages (list), Status
- The Connection output is the key to bidirectional communication

### WebSocketServerComponent
- Accepts WebSocket connections from clients
- Inputs: Port, Run (boolean)
- Outputs: Connection (`WebSocketConnectionGoo`), Message, Messages (list), Clients (count), Status
- Supports multiple simultaneous client connections
- Each received message includes the Connection for that specific client

### WebSocketSendComponent
- Sends a message through an open WebSocket connection
- Inputs: Connection (`WebSocketConnectionGoo`), Message, Send (boolean)
- Outputs: Success, Status
- Works with connections from both Client and Server components

### WebSocketConnection Data Model
- Wraps the underlying WebSocket instance
- Thread-safe `SendMessage()` method for concurrent access
- Tracks connection state, remote endpoint, and connection ID
- Used by WebSocketSendComponent to send through open connections

**Usage Flow (Client connecting to external service):**
```
[WebSocket Client] ──► Connection ──► [Your Logic] ──► [WebSocket Send]
         │                                                    ↑
         └── Message ──► (process/decide) ───────────────────┘
```

**Usage Flow (GH-to-GH communication):**
```
Instance A:                              Instance B:
[WebSocket Server] ◄─────────────────── [WebSocket Client]
         │                                        │
    Connection ──► [Logic] ──► [WS Send]    Connection ──► [Logic] ──► [WS Send]
         │              │           │              │              │           │
    Message ────────────┘           └──────────► Message ─────────┘           │
                                                                              │
         └─────────────────────────────────────────────────────────◄──────────┘
```

## MCP Components (8_Mcp)

Components for implementing Model Context Protocol servers, allowing AI clients like Claude to call Grasshopper-defined tools.

### DefineToolParameterComponent
- Creates parameter definitions for MCP tools
- Inputs: Name, Type (string/number/integer/boolean/object/array), Description, Required
- Outputs: `McpToolParameterGoo`

### DefineToolComponent
- Creates tool definitions from parameters
- Inputs: Name, Description, list of Parameters
- Outputs: `McpToolDefinitionGoo` with auto-generated JSON Schema

### McpServerComponent
- Main MCP server handling protocol lifecycle
- Inputs: Port, list of Tool definitions, Server Name
- Outputs: One `McpToolCallRequestGoo` per tool (dynamic based on tool definitions)
- Handles: initialize, initialized, tools/list, tools/call
- Protocol: JSON-RPC 2.0 over HTTP

### DeconstructToolCallComponent
- Extracts data from `McpToolCallRequestGoo`: Tool name, Arguments (JObject)
- Passes through request for response

### McpToolResponseComponent
- Sends response back to MCP client
- Inputs: Request, Content (JToken), IsError flag
- Outputs: Success boolean

**Usage Flow:**
```
[Define Tool Parameter] ──► [Define Tool] ──► [MCP Server]
                                                    │
                                              tool_name (output)
                                                    │
                                                    ▼
                                        [Deconstruct Tool Call]
                                                    │
                                         Arguments ─┼─► [Your Logic]
                                                    │         │
                                           Request ─┼─────────┘
                                                    │         │
                                                    ▼         ▼
                                           [MCP Tool Response]
```

**MCP Protocol Details:**
- Endpoint: `http://localhost:{port}/mcp/`
- Session management via `Mcp-Session-Id` header
- Protocol version: 2024-11-05
- Tools-only capability (Resources and Prompts can be added later)

## SwiftletBridge (MCP Bridge)

SwiftletBridge is a standalone executable that bridges Claude Desktop's stdio-based MCP protocol to Swiftlet's HTTP-based MCP server. This allows Claude Desktop to communicate with Grasshopper definitions without requiring Node.js or any other runtime.

**How it works:**
1. Claude Desktop spawns SwiftletBridge as a subprocess
2. Claude sends JSON-RPC messages via stdin
3. SwiftletBridge forwards requests to the HTTP MCP server
4. Responses are written back to stdout for Claude

**Claude Desktop Configuration** (`claude_desktop_config.json`):

Windows:
```json
{
  "mcpServers": {
    "Swiftlet": {
      "command": "C:\\path\\to\\mcp\\SwiftletBridge.exe",
      "args": ["http://localhost:3001/mcp/"]
    }
  }
}
```

macOS (Apple Silicon):
```json
{
  "mcpServers": {
    "Swiftlet": {
      "command": "/path/to/mcp/SwiftletBridge-macos-arm64",
      "args": ["http://localhost:3001/mcp/"]
    }
  }
}
```

macOS (Intel):
```json
{
  "mcpServers": {
    "Swiftlet": {
      "command": "/path/to/mcp/SwiftletBridge-macos-x64",
      "args": ["http://localhost:3001/mcp/"]
    }
  }
}
```

**Building the Bridge:**
The bridge is automatically built and published for all platforms (win-x64, osx-x64, osx-arm64) during the Yak package build process. The executables are placed in the `mcp/` subdirectory of the dist folder.

**Manual build:**
```bash
# Windows
dotnet publish SwiftletBridge/SwiftletBridge.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true

# macOS Intel
dotnet publish SwiftletBridge/SwiftletBridge.csproj -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true

# macOS Apple Silicon
dotnet publish SwiftletBridge/SwiftletBridge.csproj -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true
```

## Utility Classes

- **`ContentTypeUtility`**: MIME type constants and mapping
- **`UrlUtility`**: URL building with query parameter encoding
- **`NamingUtility`**: Category/subcategory names for all components (AUTH, REQUEST, SEND, READ_JSON, READ_HTML, UTILITIES, LISTEN, MCP)
- **`CompressionUtility`**: GZIP compression/decompression
- **`IpBlacklistUtil`**: URL validation against IP blacklists

## Adding New Components

1. Create file in appropriate `Components/X_Category/` folder
2. Inherit from `GH_Component` (or `BaseRequestComponent` for HTTP, or implement `IGH_VariableParameterComponent` for dynamic params)
3. Implement `RegisterInputParams()`, `RegisterOutputParams()`, `SolveInstance()`
4. Set unique `ComponentGuid`
5. Add icon to `Properties/Resources.resx`
6. Use category/subcategory from `NamingUtility`

## Adding New Custom Types

1. Create data model in `DataModels/Implementations/`
2. Create interface in `DataModels/Interfaces/` if needed
3. Create Goo wrapper in `Goo/` with `CastTo<Q>()` and `CastFrom()` methods
4. Create parameter type in `Params/`
5. Always set icons to `null` for new components. Those will be added later.

## Archiving/Retiring Components

**IMPORTANT**: Never delete components that have been released. Deleting components breaks existing Grasshopper definitions that use them. Instead, archive them following this procedure:

1. **Rename the class** to `OriginalName_ARCHIVED` (e.g., `SocketListener` → `SocketListener_ARCHIVED`)
2. **Rename the file** to match: `OriginalName_ARCHIVED.cs`
3. **Add the `[Obsolete]` attribute** with a message pointing to the replacement:
   ```csharp
   [Obsolete("Use NewComponent instead. This component is kept for backwards compatibility.")]
   public class OldComponent_ARCHIVED : GH_Component
   ```
4. **Hide from the component palette** by overriding `Exposure`:
   ```csharp
   public override GH_Exposure Exposure => GH_Exposure.hidden;
   ```
5. **Update the description** to indicate deprecation:
   ```csharp
   : base("Component Name", "ABBR",
       "[DEPRECATED - Use NewComponent instead]\nOriginal description...",
       NamingUtility.CATEGORY, NamingUtility.SUBCATEGORY)
   ```
6. **Add a summary comment** at the top of the class:
   ```csharp
   /// <summary>
   /// ARCHIVED: This component has been replaced by NewComponent.
   /// Kept for backwards compatibility with existing Grasshopper definitions.
   /// </summary>
   ```
7. **NEVER change the ComponentGuid** - this is what links existing .gh files to the component
8. **Give the replacement component a NEW unique GUID** - don't reuse the old one

**Currently archived components:**
- `SocketListener_ARCHIVED` → replaced by `WebSocketClientComponent`
- `CreateMultipartFormBodyNamed_ARCHIVED` → replaced by Create Multipart Form Body with Multipart Field
- `CreateMultipartFormBodyUnnamed_ARCHIVED` → replaced by Create Multipart Form Body with Multipart Field
- `OAuthRefreshComponent_ARCHIVED` → replaced by `OAuthTokenComponent` with Refresh input

## Async Listener Pattern

For components that listen for external events (HTTP requests, WebSocket messages, etc.):

1. Use `Task.Run()` in `AfterSolveInstance()` to start async listener
2. Create custom `EventHandler` and `EventArgs` for received events
3. Store received data in instance fields
4. Use `Grasshopper.Instances.ActiveCanvas.BeginInvoke()` or `Rhino.RhinoApp.InvokeOnUiThread()` to call `ExpireSolution(true)` from UI thread
5. Use a `_requestTriggeredSolve` flag to distinguish user-triggered vs event-triggered solves
6. Implement cleanup in `RemovedFromDocument()` and `DocumentContextChanged()`

## Key Files Reference

- `Components/3_Send/HttpRequestComponent.cs` - Core HTTP request implementation
- `Components/3_Send/BaseRequestComponent.cs` - Common HTTP validation logic
- `Components/7_Serve/ServerInputComponent.cs` - Variable parameter HTTP server example
- `Components/7_Serve/ServerResponseComponent.cs` - HTTP response sending
- `Components/7_Serve/WebSocketClientComponent.cs` - WebSocket client with pass-through connection
- `Components/7_Serve/WebSocketServerComponent.cs` - WebSocket server accepting multiple clients
- `Components/7_Serve/WebSocketSendComponent.cs` - Send messages through open WebSocket connections
- `Components/8_Mcp/McpServerComponent.cs` - MCP protocol implementation
- `DataModels/Implementations/McpToolCallContext.cs` - MCP response sending logic
- `DataModels/Implementations/WebSocketConnection.cs` - WebSocket connection context with send capability
- `Util/ContentTypeUtility.cs` - MIME type constants
- `Util/NamingUtility.cs` - Category/subcategory names
