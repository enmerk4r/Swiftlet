using Grasshopper2.Components;
using Grasshopper2.Parameters;
using GrasshopperIO;
using Swiftlet.Core.Http;
using Swiftlet.Gh2.Parameters;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Components;

// "8. Server": HTTP Listener, UDP Listener and the WebSocket components. Each one owns a live network
// resource; ListenerComponent supplies the lifecycle (single-threaded solves, gate, cleanup on delete /
// disable / close, coalesced recomputes from background threads).

[IoId(ComponentIds.HttpListener)]
public sealed class HttpListenerComponent : ListenerComponent
{
    private readonly Outbox<SimpleHttpListenerRequest> _requests = new();
    private readonly System.Collections.Concurrent.ConcurrentQueue<SimpleHttpListenerRequest> _arrived = new();
    private SimpleHttpListenerSession? _session;

    public HttpListenerComponent()
        : base("HTTP Listener", "A simple HTTP listener component that can receive HTTP requests. \nThis component is VERY ALPHA. Use at your own risk.", Sections.Server) { }
    public HttpListenerComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddInteger("Port", "Pt", "Port number to listen at between 0 and 65535");
        inputs.AddText("Route", "Rt", "Optional route prefix", Access.Item, Requirement.MayBeMissing);
        inputs.Add(new RequestBodyParameter("Response Body", "Bd", "Pre-canned response body that will be returned to the sender"), Requirement.MayBeMissing);
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddText("Method", "Mt", "Request Method");
        outputs.Add(new HttpHeaderParameter("Headers", "Hd", "Http Request Headers", Access.Twig));
        outputs.Add(new QueryParameterParameter("Query Params", "Qr", "Components of the request query string", Access.Twig));
        outputs.AddText("Content", "Ct", "Request Content");
    }

    protected override void ProcessListener(IDataAccess access, CancellationToken cancellation)
    {
        if (!access.GetItem(0, out int port))
            return;

        access.GetItem(1, out string route);
        access.GetItem(2, out IRequestBody responseBody);

        if (port < 0 || port > 65535)
            throw new Exception("Port number must be between 0 and 65535");

        route ??= string.Empty;
        if (route.Length > 0 && !route.EndsWith('/'))
            route += "/";

        try
        {
            _session ??= CreateSession();
            _session.ReconfigureAsync(port, route, responseBody, cancellation).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            access.AddError("Listener failed", ex.Message);
            return;
        }

        // Request data is published only by the solve that a request triggered; other solves publish
        // nothing. If several requests arrived before this solve, the most recent one is shown.
        SimpleHttpListenerRequest[] pending = _requests.Publish(access.Solution, _arrived.TryDequeue!);
        if (pending.Length == 0)
        {
            access.SetTwig(1, Array.Empty<HttpHeader>());
            access.SetTwig(2, Array.Empty<QueryParameter>());
            return;
        }

        SimpleHttpListenerRequest request = pending[^1];
        access.SetItem(0, request.Method);
        access.SetTwig(1, request.Headers.ToArray());
        access.SetTwig(2, request.QueryParameters.ToArray());
        access.SetItem(3, request.Content);
    }

    protected override void OnSolutionCommitted(Grasshopper2.Doc.SolutionId solution) => _requests.Commit(solution);

    protected override bool HasUnconfirmedOutput => _requests.HasUnconfirmed;

    protected override void StopResource()
    {
        if (_session is not { } session)
            return;

        _session = null;
        _arrived.Clear();
        _requests.Clear();
        session.RequestReceived -= OnRequestReceived;
        session.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private SimpleHttpListenerSession CreateSession()
    {
        var session = new SimpleHttpListenerSession();
        session.RequestReceived += OnRequestReceived;
        return session;
    }

    private void OnRequestReceived(object? sender, EventArgs e)
    {
        if (!ReferenceEquals(sender, _session) || _session?.LatestRequest is not { } request)
            return;

        _arrived.Enqueue(request);
        RequestRecompute();
    }
}

[IoId(ComponentIds.UdpListener)]
public sealed class UdpListenerComponent : ListenerComponent
{
    private UdpListenerSession? _session;
    private string? _fault;

    public UdpListenerComponent()
        : base("UDP Listener", "A UDP listener component that receives UDP datagrams on a specified port.\nThis component is ALPHA. Use at your own risk.", Sections.Server) { }
    public UdpListenerComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddInteger("Port", "Pt", "Port number to listen on (0-65535)");
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new ByteArrayParameter("Data", "Dt", "Received data as byte array"));
        outputs.AddText("Remote", "Rm", "Remote endpoint (IP:Port) that sent the data");
    }

    protected override void ProcessListener(IDataAccess access, CancellationToken cancellation)
    {
        if (!access.GetItem(0, out int port))
            return;

        if (port < 0 || port > 65535)
        {
            access.AddError("Invalid port", "Port must be between 0 and 65535");
            return;
        }

        try
        {
            _session ??= CreateSession();
            _session.Start(port);
        }
        catch (Exception ex)
        {
            access.AddError("Failed to start listener", ex.Message);
        }

        if (Interlocked.Exchange(ref _fault, null) is { } fault)
            access.AddError("Listener stopped", fault);

        if (_session?.LatestData is { } data)
            access.SetItem(0, data);
        if (_session?.LatestRemoteEndpoint is { } remote)
            access.SetItem(1, remote);
    }

    protected override void StopResource()
    {
        if (_session is not { } session)
            return;

        _session = null;
        session.DataReceived -= OnDataReceived;
        session.Faulted -= OnFaulted;
        session.Dispose();
    }

    private UdpListenerSession CreateSession()
    {
        var session = new UdpListenerSession();
        session.DataReceived += OnDataReceived;
        session.Faulted += OnFaulted;
        return session;
    }

    private void OnDataReceived(object? sender, EventArgs e)
    {
        if (ReferenceEquals(sender, _session))
            RequestRecompute();
    }

    private void OnFaulted(object? sender, string message)
    {
        if (!ReferenceEquals(sender, _session))
            return;

        Volatile.Write(ref _fault, message);
        RequestRecompute();
    }
}

[IoId(ComponentIds.WebSocketClient)]
public sealed class WebSocketClientComponent : ListenerComponent
{
    private readonly Outbox<string> _received = new();
    private ModernWebSocketClientSession? _session;
    private string? _currentUrl;
    private bool _run;
    private string? _lastMessage;

    public WebSocketClientComponent()
        : base("WebSocket Client", "Connects to a WebSocket server and receives messages.\nOutputs a Connection that can be used with WebSocket Send to send messages.", Sections.Server) { }
    public WebSocketClientComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("URL", "Ur", "URL of WebSocket server (ws:// or wss://)");
        var query = new QueryParameterParameter("Params", "Pr", "Query parameters to append to the URL", Access.Twig);
        query.Set(Array.Empty<QueryParameter>());
        inputs.Add(query, Requirement.MayBeMissing);
        inputs.AddBoolean("Run", "Rn", "Set to true to connect, false to disconnect").Set(false);
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new WebSocketConnectionParameter("Connection", "Cn", "WebSocket connection for sending messages via WebSocket Send component"));
        outputs.AddText("Message", "Ms", "Most recent message received from the server");
        outputs.AddText("Messages", "Mm", "All messages received since last solve", Access.Twig);
        outputs.AddText("Status", "St", "Connection status");
    }

    protected override void ProcessListener(IDataAccess access, CancellationToken cancellation)
    {
        access.GetItem(0, out string url);
        access.GetTwig<QueryParameter>(1, out var query);
        if (!access.GetItem(2, out bool run))
            run = false;

        if (string.IsNullOrWhiteSpace(url))
        {
            access.AddError("Missing URL", "URL cannot be empty");
            access.SetItem(3, "Error: No URL");
            return;
        }

        if (!url.StartsWith("ws://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
        {
            access.AddError("Invalid URL", "URL must start with ws:// or wss://");
            access.SetItem(3, "Error: Invalid URL scheme (use ws:// or wss://)");
            return;
        }

        QueryParameter[] parameters = query?.NonNullItems.ToArray() ?? Array.Empty<QueryParameter>();
        string fullUrl = UrlBuilder.AddQueryParameters(url, parameters);
        ModernWebSocketClientSession session = _session ??= CreateSession();

        try
        {
            if (run && !_run)
            {
                _lastMessage = null;
                session.ReconnectAsync(url, parameters, cancellation).GetAwaiter().GetResult();
                _currentUrl = fullUrl;
            }
            else if (!run && _run)
            {
                session.DisconnectAsync().GetAwaiter().GetResult();
                _currentUrl = null;
                _lastMessage = null;
            }
            else if (run && !string.Equals(_currentUrl, fullUrl, StringComparison.Ordinal) && session.IsConnected)
            {
                _lastMessage = null;
                session.ReconnectAsync(url, parameters, cancellation).GetAwaiter().GetResult();
                _currentUrl = fullUrl;
            }
            else if (run && !session.IsConnected)
            {
                session.ReconnectAsync(url, parameters, cancellation).GetAwaiter().GetResult();
                _currentUrl = fullUrl;
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            access.AddError("Connection failed", ex.Message);
        }

        _run = run;

        // Messages stay in the outbox until this solve is confirmed complete, so a cancelled solve loses none.
        string[] messages = _received.Publish(access.Solution, (out string item) =>
        {
            bool found = session.TryDequeueMessage(out string? next) && !string.IsNullOrEmpty(next);
            item = next ?? string.Empty;
            return found;
        });
        if (messages.Length > 0)
            _lastMessage = messages[^1];

        if (session.Connection is not null)
            access.SetItem(0, session.Connection);

        string status = session.LastError is not null
            ? $"Error: {session.LastError}"
            : session.Connection?.GetStatusString() ?? "Disconnected";

        if (_lastMessage is not null)
            access.SetItem(1, _lastMessage);
        access.SetTwig(2, messages);
        access.SetItem(3, status);
    }

    protected override void OnSolutionCommitted(Grasshopper2.Doc.SolutionId solution) => _received.Commit(solution);

    protected override bool HasUnconfirmedOutput => _received.HasUnconfirmed;

    protected override void StopResource()
    {
        _run = false;
        _currentUrl = null;
        _received.Clear();
        if (_session is not { } session)
            return;

        _session = null;
        session.StateChanged -= OnStateChanged;
        session.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private ModernWebSocketClientSession CreateSession()
    {
        var session = new ModernWebSocketClientSession();
        session.StateChanged += OnStateChanged;
        return session;
    }

    private void OnStateChanged(object? sender, EventArgs e) => RequestRecompute();
}

[IoId(ComponentIds.WebSocketServer)]
public sealed class WebSocketServerComponent : ListenerComponent
{
    private readonly Outbox<ModernWebSocketReceivedMessage> _received = new();
    private ModernWebSocketServerSession? _session;
    private string? _lastMessage;
    private ModernWebSocketConnection? _lastConnection;

    public WebSocketServerComponent()
        : base("WebSocket Server", "Accepts WebSocket connections from clients. Outputs Connection for each message received, use with WebSocket Send to respond.", Sections.Server) { }
    public WebSocketServerComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddInteger("Port", "Pt", "Port number to listen on (0-65535)").Set(8181);
        inputs.AddBoolean("Run", "Rn", "Set to true to start the server, false to stop").Set(false);
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.Add(new WebSocketConnectionParameter("Connection", "Cn", "Connection context for the client that sent the message (use with WebSocket Send)"));
        outputs.AddText("Message", "Ms", "Most recent message received");
        outputs.AddText("Messages", "Mm", "All messages received since last solve", Access.Twig);
        outputs.AddInteger("Clients", "Nc", "Number of connected clients");
        outputs.AddText("Status", "St", "Server status");
    }

    protected override void ProcessListener(IDataAccess access, CancellationToken cancellation)
    {
        if (!access.GetItem(0, out int port))
            port = 8181;
        if (!access.GetItem(1, out bool run))
            run = false;

        if (port < 0 || port > 65535)
        {
            access.AddError("Invalid port", "Port must be between 0 and 65535");
            access.SetItem(4, "Error: Invalid port");
            return;
        }

        ModernWebSocketServerSession session = _session ??= CreateSession();
        try
        {
            if (run)
                session.ReconfigureAsync(port, cancellation).GetAwaiter().GetResult();
            else if (session.IsRunning)
                session.StopAsync().GetAwaiter().GetResult();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            access.AddError("Failed to start server", ex.Message);
            access.SetItem(4, $"Error: {ex.Message}");
            return;
        }

        // Messages stay in the outbox until this solve is confirmed complete, so a cancelled solve loses none.
        ModernWebSocketReceivedMessage[] received = _received.Publish(access.Solution, (out ModernWebSocketReceivedMessage item) =>
        {
            bool found = session.TryDequeueMessage(out ModernWebSocketReceivedMessage? next) && next is not null;
            item = next!;
            return found;
        });
        string[] messages = received.Select(static message => message.Message).ToArray();
        if (received.Length > 0)
        {
            _lastMessage = received[^1].Message;
            _lastConnection = received[^1].Connection;
        }

        if (_lastConnection is null || !_lastConnection.IsOpen)
            session.TryGetAnyOpenConnection(out _lastConnection);

        if (_lastConnection is not null)
            access.SetItem(0, _lastConnection);
        if (_lastMessage is not null)
            access.SetItem(1, _lastMessage);
        access.SetTwig(2, messages);
        access.SetItem(3, session.ActiveClientCount);
        access.SetItem(4, run ? $"Listening on ws://localhost:{port}/" : "Stopped");
    }

    protected override void OnSolutionCommitted(Grasshopper2.Doc.SolutionId solution) => _received.Commit(solution);

    protected override bool HasUnconfirmedOutput => _received.HasUnconfirmed;

    protected override void StopResource()
    {
        _lastConnection = null;
        _received.Clear();
        if (_session is not { } session)
            return;

        _session = null;
        session.StateChanged -= OnStateChanged;
        session.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private ModernWebSocketServerSession CreateSession()
    {
        var session = new ModernWebSocketServerSession();
        session.StateChanged += OnStateChanged;
        return session;
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (ReferenceEquals(sender, _session))
            RequestRecompute();
    }
}

/// <summary>Sends a text message over a connection produced by WebSocket Client or WebSocket Server.</summary>
[IoId(ComponentIds.WebSocketSend)]
public sealed class WebSocketSendComponent : SwiftletComponent
{
    public WebSocketSendComponent()
        : base("WebSocket Send", "Sends a message through an open WebSocket connection. Use with WebSocket Client or WebSocket Server components.", Sections.Server) { }
    public WebSocketSendComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new WebSocketConnectionParameter("Connection", "Cn", "WebSocket connection from WebSocket Client or WebSocket Server"));
        inputs.AddText("Message", "Ms", "Message to send");
        inputs.AddBoolean("Send", "Sn", "Set to true to send the message").Set(false);
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddBoolean("Success", "Sc", "True if the message was sent successfully");
        outputs.AddText("Status", "St", "Connection status or error message");
    }

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out ModernWebSocketConnection connection);
        access.GetItem(1, out string message);
        if (!access.GetItem(2, out bool send))
            send = false;

        if (connection is null)
        {
            access.SetItem(0, false);
            access.SetItem(1, "No connection provided");
            return;
        }

        if (!connection.IsOpen)
        {
            access.SetItem(0, false);
            access.SetItem(1, $"Connection not open: {connection.GetStatusString()}");
            return;
        }

        if (!send)
        {
            access.SetItem(0, false);
            access.SetItem(1, "Ready to send (set Send to true)");
            return;
        }

        if (string.IsNullOrEmpty(message))
        {
            access.SetItem(0, false);
            access.SetItem(1, "Message is empty");
            access.AddWarning("Nothing to send", "Message is empty");
            return;
        }

        try
        {
            bool success = connection.SendMessageAsync(message, access.Solution.Token).GetAwaiter().GetResult();
            access.SetItem(0, success);
            access.SetItem(1, success ? "Message sent" : "Failed to send message");
            if (!success)
                access.AddError("Send failed", "Failed to send message");
        }
        catch (OperationCanceledException) when (access.Solution.Token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            access.SetItem(0, false);
            access.SetItem(1, $"Error: {ex.Message}");
            access.AddError("Send failed", ex.Message);
        }
    }
}
