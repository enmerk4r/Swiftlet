using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;
using Swiftlet.Core.Auth;
using Swiftlet.Core.Mcp;
using Swiftlet.Gh.Rhino8;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.HostAbstractions;
using Swiftlet.Hosts.Desktop;
using Swiftlet.Hosts.Headless;
using Swiftlet.Imaging;

return await IntegrationTestRunner.RunAsync();

internal static class IntegrationTestRunner
{
    public static async Task<int> RunAsync()
    {
        List<(string Name, Func<Task> Test)> tests =
        [
            ("Image codec round-trips PNG pixel data", ImageCodecRoundTripsPngAsync),
            ("QR generation returns a raster image with both dark and light pixels", QrGenerationProducesRasterAsync),
            ("Utility CSV helpers preserve quoted delimiters and quotes", UtilityCsvHelpersRoundTripAsync),
            ("Utility compression round-trips bytes", UtilityCompressionRoundTripsAsync),
            ("Utility URL encoding preserves form encoding semantics", UtilityUrlEncodingProducesExpectedOutputAsync),
            ("JSON null goos remain valid and preserve null semantics", JsonNullGoosPreserveNullSemanticsAsync),
            ("Headless host services require manual browser and clipboard actions", HeadlessServicesRequireManualActionsAsync),
            ("Desktop host workflow exposes desktop capabilities", DesktopWorkflowExposesCapabilitiesAsync),
            ("Modern OAuth authorization flow tracks state through callback completion", OAuthAuthorizationFlowTracksStateAsync),
            ("OAuth callback listener completes a loopback authorization round-trip", OAuthLoopbackCallbackRoundTripAsync),
            ("OAuth callback listener accepts form-post authorization responses", OAuthLoopbackCallbackFormPostRoundTripAsync),
            ("OAuth callback listener completes fragment-style browser relay callbacks", OAuthLoopbackCallbackFragmentRelayRoundTripAsync),
            ("OAuth callback listener normalizes malformed file-style callback targets", OAuthLoopbackCallbackMalformedFileTargetRoundTripAsync),
            ("Modern MCP server handles initialize, tools/list, and tools/call over HTTP", ModernMcpServerHandlesJsonRpcOverHttpAsync),
            ("Modern MCP server session manages lifecycle and config export", ModernMcpServerSessionManagesLifecycleAsync),
            ("Modern MCP response workflow preserves deconstruct and one-shot semantics", ModernMcpResponseWorkflowPreservesSemanticsAsync),
            ("Modern HTTP server routes requests and completes one-shot responses", ModernServerRoutesRequestsAndCompletesResponsesAsync),
            ("Simple HTTP listener captures requests and returns configured content", SimpleHttpListenerCapturesRequestsAndReturnsConfiguredContentAsync),
            ("Modern WebSocket server and client exchange messages", ModernWebSocketServerAndClientExchangeMessagesAsync),
            ("MCP config generation resolves platform bridge artifacts", McpConfigGenerationResolvesBridgeArtifactsAsync),
            ("Bridge artifact resolution repairs Unix execute permissions for native binaries", BridgeArtifactResolutionRepairsUnixExecutePermissionsAsync),
            ("MCP export reports manual clipboard guidance through notifications", McpExportReportsManualClipboardGuidanceAsync),
        ];

        foreach ((string name, Func<Task> test) in tests)
        {
            await test().ConfigureAwait(false);
            Console.WriteLine($"PASS {name}");
        }

        Console.WriteLine($"Executed {tests.Count} integration tests.");
        return 0;
    }

    private static Task ImageCodecRoundTripsPngAsync()
    {
        var image = new SwiftletImage(
            2,
            1,
            [
                255, 0, 0, 255,
                0, 255, 0, 255,
            ]);

        byte[] encoded = ImageCodec.Save(image, SwiftletImageFormat.Png);
        SwiftletImage decoded = ImageCodec.Load(encoded);

        Assert.Equal(2, decoded.Width);
        Assert.Equal(1, decoded.Height);
        Assert.Equal(new SwiftletColor(255, 0, 0, 255), decoded.GetPixel(0, 0));
        Assert.Equal(new SwiftletColor(0, 255, 0, 255), decoded.GetPixel(1, 0));
        return Task.CompletedTask;
    }

    private static Task QrGenerationProducesRasterAsync()
    {
        SwiftletImage image = ImageCodec.GenerateQrCode(
            "https://swiftlet.example",
            4,
            new SwiftletColor(0, 0, 0, 255),
            new SwiftletColor(255, 255, 255, 255),
            drawQuietZones: true);

        Assert.True(image.Width > 0);
        Assert.True(image.Height > 0);

        bool sawDark = false;
        bool sawLight = false;
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                SwiftletColor pixel = image.GetPixel(x, y);
                if (pixel == new SwiftletColor(0, 0, 0, 255))
                {
                    sawDark = true;
                }
                else if (pixel == new SwiftletColor(255, 255, 255, 255))
                {
                    sawLight = true;
                }

                if (sawDark && sawLight)
                {
                    return Task.CompletedTask;
                }
            }
        }

        throw new InvalidOperationException("Expected QR output to contain both dark and light pixels.");
    }

    private static Task UtilityCsvHelpersRoundTripAsync()
    {
        Type utilityType = GetShellType("Swiftlet.Gh.Rhino8.UtilityCsv");
        string line = (string)utilityType.GetMethod("CreateLine")!.Invoke(null, [new[] { "alpha", "be,ta", "say \"hi\"" }, ","])!;
        IReadOnlyList<string> parsed = (IReadOnlyList<string>)utilityType.GetMethod("ParseLine")!.Invoke(null, [line, ","])!;

        Assert.Contains("\"be,ta\"", line);
        Assert.Contains("\"say \"\"hi\"\"\"", line);
        Assert.Equal(3, parsed.Count);
        Assert.Equal("alpha", parsed[0]);
        Assert.Equal("be,ta", parsed[1]);
        Assert.Equal("say \"hi\"", parsed[2]);
        return Task.CompletedTask;
    }

    private static Task UtilityCompressionRoundTripsAsync()
    {
        Type utilityType = GetShellType("Swiftlet.Gh.Rhino8.UtilityCompression");
        byte[] original = Encoding.UTF8.GetBytes("swiftlet utilities round trip");
        byte[] compressed = (byte[])utilityType.GetMethod("Compress")!.Invoke(null, [original])!;
        byte[] decompressed = (byte[])utilityType.GetMethod("Decompress", [typeof(byte[])])!.Invoke(null, [compressed])!;

        Assert.True(compressed.Length > 0);
        Assert.Equal(Encoding.UTF8.GetString(original), Encoding.UTF8.GetString(decompressed));
        return Task.CompletedTask;
    }

    private static Task UtilityUrlEncodingProducesExpectedOutputAsync()
    {
        Type utilityType = GetShellType("Swiftlet.Gh.Rhino8.UtilityUrlEncoding");
        string encoded = (string)utilityType.GetMethod("Encode")!.Invoke(null, ["cafe au lait+?", Encoding.UTF8])!;

        Assert.Equal("cafe+au+lait%2B%3F", encoded);
        return Task.CompletedTask;
    }

    private static Task JsonNullGoosPreserveNullSemanticsAsync()
    {
        JsonValueGoo jsonNullValue = JsonValueGoo.CreateJsonNull();
        JsonNodeGoo jsonNullNode = JsonNodeGoo.CreateJsonNull();

        Assert.True(jsonNullValue.IsValid);
        Assert.True(jsonNullNode.IsValid);
        Assert.Equal("JSON Value [null]", jsonNullValue.ToString());
        Assert.Equal("JSON Value [null]", jsonNullNode.ToString());

        JsonValueGoo duplicatedValue = (JsonValueGoo)jsonNullValue.Duplicate();
        JsonNodeGoo duplicatedNode = (JsonNodeGoo)jsonNullNode.Duplicate();

        Assert.True(duplicatedValue.RepresentsJsonNull);
        Assert.True(duplicatedNode.RepresentsJsonNull);
        Assert.Null(duplicatedValue.Value);
        Assert.Null(duplicatedNode.Value);
        return Task.CompletedTask;
    }

    private static async Task HeadlessServicesRequireManualActionsAsync()
    {
        IHostServices services = new HeadlessHostServices();

        HostActionResult browserResult = await services.BrowserLauncher.OpenUrlAsync("https://example.com").ConfigureAwait(false);
        HostActionResult clipboardResult = await services.ClipboardService.SetTextAsync("swiftlet").ConfigureAwait(false);

        Assert.False(services.Capabilities.CanLaunchBrowser);
        Assert.False(services.Capabilities.CanUseClipboard);
        Assert.True(browserResult.RequiresManualAction);
        Assert.True(clipboardResult.RequiresManualAction);
        await Assert.ThrowsAsync<NotSupportedException>(async () =>
        {
            await using ILocalHttpCallbackSession _ = await services.LocalCallbacks
                .StartAsync(new Uri("http://localhost:48123/callback/"))
                .ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    private static Task DesktopWorkflowExposesCapabilitiesAsync()
    {
        IHostServices services = new DesktopHostServices(
            browserLauncher: new StubBrowserLauncher(HostActionResult.Success("Browser launch simulated.")));

        Assert.True(services.Capabilities.CanLaunchBrowser);
        Assert.True(services.Capabilities.CanUseClipboard);
        Assert.True(services.Capabilities.CanAcceptLocalHttpCallbacks);
        Assert.True(ModernHostWorkflow.SupportsInteractiveOAuth(services));
        Assert.True(ModernHostWorkflow.SupportsClipboardExport(services));
        return Task.CompletedTask;
    }

    private static async Task OAuthLoopbackCallbackRoundTripAsync()
    {
        int port = GetAvailablePort();
        var notifications = new CollectingNotificationSink();
        var services = new DesktopHostServices(
            browserLauncher: new StubBrowserLauncher(HostActionResult.Success("Browser launch simulated.")),
            notifications: notifications);

        OAuthAuthorizationSession session = ModernOAuthWorkflow.CreateAuthorizationSession(
            "https://accounts.example.com/authorize",
            "client-id",
            $"http://localhost:{port}/callback/",
            ["openid", "profile"]);

        HostActionResult launchResult = await ModernOAuthWorkflow
            .LaunchAuthorizationUrlAsync(services, session)
            .ConfigureAwait(false);

        Assert.True(launchResult.IsSuccess);
        await using ILocalHttpCallbackSession callbackSession = await services.LocalCallbacks
            .StartAsync(new Uri(session.RedirectUri))
            .ConfigureAwait(false);

        Task<OAuthCallbackResult> callbackTask = ModernOAuthWorkflow.WaitForAuthorizationCodeAsync(callbackSession, session);

        string responseText = await SendCallbackAsync(
            port,
            $"/callback/?code=test-code&state={Uri.EscapeDataString(session.State)}").ConfigureAwait(false);

        OAuthCallbackResult result = await callbackTask.ConfigureAwait(false);

        Assert.True(result.IsSuccess);
        Assert.Equal("test-code", result.AuthorizationCode);
        Assert.Contains("Authorization Successful", responseText);
        Assert.Equal(0, notifications.Notifications.Count);
    }

    private static async Task OAuthAuthorizationFlowTracksStateAsync()
    {
        int port = GetAvailablePort();
        var services = new DesktopHostServices(
            browserLauncher: new StubBrowserLauncher(HostActionResult.Success("Browser launch simulated.")));

        await using var flow = new ModernOAuthAuthorizationFlow(services);
        HostActionResult launchResult = await flow.StartAsync(
            "https://accounts.example.com/authorize",
            "client-id",
            $"http://localhost:{port}/callback/",
            ["openid"]).ConfigureAwait(false);

        Assert.True(launchResult.IsSuccess);
        Assert.True(flow.IsWaiting);
        Assert.Equal("Waiting for authorization... (check your browser)", flow.StatusMessage);
        Assert.NotNull(flow.Session);

        Task<OAuthCallbackResult> completionTask = flow.WaitForCompletionAsync();
        _ = SendCallbackAsync(port, $"/callback/?code=flow-code&state={Uri.EscapeDataString(flow.Session!.State)}");

        OAuthCallbackResult result = await completionTask.ConfigureAwait(false);

        Assert.True(result.IsSuccess);
        Assert.True(flow.IsCompleted);
        Assert.False(flow.IsWaiting);
        Assert.Equal("flow-code", flow.AuthorizationCode);
        Assert.Equal("Authorization successful", flow.StatusMessage);

        await flow.ResetAsync().ConfigureAwait(false);
        Assert.False(flow.IsCompleted);
        Assert.False(flow.IsWaiting);
        Assert.Null(flow.AuthorizationCode);
    }

    private static async Task OAuthLoopbackCallbackFormPostRoundTripAsync()
    {
        int port = GetAvailablePort();
        var services = new DesktopHostServices(
            browserLauncher: new StubBrowserLauncher(HostActionResult.Success("Browser launch simulated.")));

        OAuthAuthorizationSession session = ModernOAuthWorkflow.CreateAuthorizationSession(
            "https://accounts.example.com/authorize",
            "client-id",
            $"http://localhost:{port}/callback/",
            ["openid", "profile"]);

        await using ILocalHttpCallbackSession callbackSession = await services.LocalCallbacks
            .StartAsync(new Uri(session.RedirectUri))
            .ConfigureAwait(false);

        Task<OAuthCallbackResult> callbackTask = ModernOAuthWorkflow.WaitForAuthorizationCodeAsync(callbackSession, session);
        string responseText = await SendFormPostCallbackAsync(
            port,
            "/callback/",
            $"code=form-post-code&state={Uri.EscapeDataString(session.State)}").ConfigureAwait(false);

        OAuthCallbackResult result = await callbackTask.ConfigureAwait(false);

        Assert.True(result.IsSuccess);
        Assert.Equal("form-post-code", result.AuthorizationCode);
        Assert.Contains("Authorization Successful", responseText);
    }

    private static async Task OAuthLoopbackCallbackFragmentRelayRoundTripAsync()
    {
        int port = GetAvailablePort();
        var services = new DesktopHostServices(
            browserLauncher: new StubBrowserLauncher(HostActionResult.Success("Browser launch simulated.")));

        OAuthAuthorizationSession session = ModernOAuthWorkflow.CreateAuthorizationSession(
            "https://accounts.example.com/authorize",
            "client-id",
            $"http://localhost:{port}/callback/",
            ["openid", "profile"]);

        await using ILocalHttpCallbackSession callbackSession = await services.LocalCallbacks
            .StartAsync(new Uri(session.RedirectUri))
            .ConfigureAwait(false);

        Task<OAuthCallbackResult> callbackTask = ModernOAuthWorkflow.WaitForAuthorizationCodeAsync(callbackSession, session);

        string continuationPage = await SendCallbackAsync(port, "/callback/").ConfigureAwait(false);
        Assert.Contains("Completing Authorization", continuationPage);

        string relayFailurePage = await SendFormPostCallbackAsync(
            port,
            "/callback/",
            $"swiftlet_relay=1&swiftlet_relay_status=relayed_hash_params&code=fragment-code&state={Uri.EscapeDataString(session.State)}").ConfigureAwait(false);

        OAuthCallbackResult result = await callbackTask.ConfigureAwait(false);

        Assert.True(result.IsSuccess);
        Assert.Equal("fragment-code", result.AuthorizationCode);
        Assert.Contains("Authorization Successful", relayFailurePage);
    }

    private static async Task OAuthLoopbackCallbackMalformedFileTargetRoundTripAsync()
    {
        int port = GetAvailablePort();
        var services = new DesktopHostServices(
            browserLauncher: new StubBrowserLauncher(HostActionResult.Success("Browser launch simulated.")));

        OAuthAuthorizationSession session = ModernOAuthWorkflow.CreateAuthorizationSession(
            "https://accounts.example.com/authorize",
            "client-id",
            $"http://localhost:{port}/callback/",
            ["openid", "profile"]);

        await using ILocalHttpCallbackSession callbackSession = await services.LocalCallbacks
            .StartAsync(new Uri(session.RedirectUri))
            .ConfigureAwait(false);

        Task<OAuthCallbackResult> callbackTask = ModernOAuthWorkflow.WaitForAuthorizationCodeAsync(callbackSession, session);
        string responseText = await SendRawCallbackAsync(
            port,
            "GET file:///callback/%3Fcode=file-target-code%26state=" + Uri.EscapeDataString(session.State) + " HTTP/1.1\r\n" +
            $"Host: localhost:{port}\r\n" +
            "Connection: close\r\n\r\n").ConfigureAwait(false);

        OAuthCallbackResult result = await callbackTask.ConfigureAwait(false);

        Assert.True(result.IsSuccess);
        Assert.Equal("file-target-code", result.AuthorizationCode);
        Assert.Contains("Authorization Successful", responseText);
    }

    private static Task McpConfigGenerationResolvesBridgeArtifactsAsync()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "SwiftletTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            string assemblyLocation = Path.Combine(tempDirectory, "Swiftlet.Gha");

            string dllPath = Path.Combine(tempDirectory, "SwiftletBridge.dll");
            File.WriteAllText(dllPath, string.Empty);

            string dllConfig = ModernMcpWorkflow.GenerateConfig(assemblyLocation, "Swiftlet", 3001);
            Assert.Contains("\"type\": \"stdio\"", dllConfig);
            Assert.Contains("\"command\": \"dotnet\"", dllConfig);
            Assert.Contains("SwiftletBridge.dll", dllConfig);
            Assert.Contains("http://localhost:3001/mcp/", dllConfig);

            File.Delete(dllPath);
            string exePath = Path.Combine(tempDirectory, OperatingSystem.IsWindows() ? "SwiftletBridge.exe" : "SwiftletBridge");
            File.WriteAllText(exePath, string.Empty);

            string nativeConfig = ModernMcpWorkflow.GenerateConfig(assemblyLocation, "Swiftlet", 3002);
            Assert.Contains("\"type\": \"stdio\"", nativeConfig);
            Assert.Contains("SwiftletBridge", nativeConfig);
            Assert.Contains("\"http://localhost:3002/mcp/\"", nativeConfig);

            string lmStudioConfig = ModernMcpWorkflow.GenerateConfig(
                assemblyLocation,
                "Swiftlet",
                3003,
                McpClientConfigTarget.LmStudio);
            Assert.Contains("\"mcpServers\"", lmStudioConfig);
            Assert.Contains("\"url\": \"http://localhost:3003/mcp/\"", lmStudioConfig);

            string vsCodeConfig = ModernMcpWorkflow.GenerateConfig(
                assemblyLocation,
                "Swiftlet",
                3004,
                McpClientConfigTarget.VsCode);
            Assert.Contains("\"servers\"", vsCodeConfig);
            Assert.Contains("\"type\": \"http\"", vsCodeConfig);
            Assert.Contains("\"url\": \"http://localhost:3004/mcp/\"", vsCodeConfig);

            string claudeCodeConfig = ModernMcpWorkflow.GenerateConfig(
                assemblyLocation,
                "Swiftlet",
                3005,
                McpClientConfigTarget.ClaudeCode);
            Assert.Contains("\"mcpServers\"", claudeCodeConfig);
            Assert.Contains("\"type\": \"http\"", claudeCodeConfig);
            Assert.Contains("\"url\": \"http://localhost:3005/mcp/\"", claudeCodeConfig);

            string codexConfig = ModernMcpWorkflow.GenerateConfig(
                assemblyLocation,
                "Swiftlet",
                3006,
                McpClientConfigTarget.Codex);
            Assert.Contains("[mcp_servers.\"Swiftlet\"]", codexConfig);
            Assert.Contains("url = \"http://localhost:3006/mcp/\"", codexConfig);

            return Task.CompletedTask;
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static Task BridgeArtifactResolutionRepairsUnixExecutePermissionsAsync()
    {
        if (OperatingSystem.IsWindows())
        {
            return Task.CompletedTask;
        }

        string tempDirectory = Path.Combine(Path.GetTempPath(), "SwiftletTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            string bridgePath = Path.Combine(tempDirectory, "SwiftletBridge");
            File.WriteAllText(bridgePath, string.Empty);
            File.SetUnixFileMode(
                bridgePath,
                UnixFileMode.UserRead |
                UnixFileMode.UserWrite |
                UnixFileMode.GroupRead |
                UnixFileMode.OtherRead);

            BridgeLaunchCommand command = new BridgeArtifactLocator().ResolveServerCommand(tempDirectory, 3010);
            UnixFileMode mode = File.GetUnixFileMode(bridgePath);

            Assert.Equal(bridgePath, command.Command);
            Assert.True(mode.HasFlag(UnixFileMode.UserExecute));
            Assert.True(mode.HasFlag(UnixFileMode.GroupExecute));
            Assert.True(mode.HasFlag(UnixFileMode.OtherExecute));

            return Task.CompletedTask;
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static async Task ModernMcpServerHandlesJsonRpcOverHttpAsync()
    {
        int port = GetAvailablePort();
        await using var session = new ModernMcpServerSession();
        await session.ReconfigureAsync(
            port,
            "Swiftlet",
            [
                new McpToolDefinition(
                    "echo",
                    "Echoes text.",
                    [new McpToolParameter("message", "string", "Message to echo")]),
            ]).ConfigureAwait(false);

        using var client = new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{port}/"),
        };

        using HttpResponseMessage initializeResponse = await client.PostAsync(
            "mcp/",
            JsonContent("""{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}""")).ConfigureAwait(false);

        string initializeBody = await initializeResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        string sessionId = initializeResponse.Headers.GetValues("Mcp-Session-Id").Single();

        Assert.Equal(HttpStatusCode.OK, initializeResponse.StatusCode);
        Assert.Contains("\"protocolVersion\":\"2024-11-05\"", initializeBody);
        Assert.Contains("\"name\":\"Swiftlet\"", initializeBody);

        using var initializedRequest = new HttpRequestMessage(HttpMethod.Post, "mcp/")
        {
            Content = JsonContent("""{"jsonrpc":"2.0","method":"initialized","params":{}}"""),
        };
        initializedRequest.Headers.Add("Mcp-Session-Id", sessionId);

        using HttpResponseMessage initializedResponse = await client.SendAsync(initializedRequest).ConfigureAwait(false);
        Assert.Equal(HttpStatusCode.Accepted, initializedResponse.StatusCode);

        using var listRequest = new HttpRequestMessage(HttpMethod.Post, "mcp/")
        {
            Content = JsonContent("""{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}"""),
        };
        listRequest.Headers.Add("Mcp-Session-Id", sessionId);

        using HttpResponseMessage listResponse = await client.SendAsync(listRequest).ConfigureAwait(false);
        string listBody = await listResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

        Assert.True(
            listResponse.StatusCode == HttpStatusCode.OK,
            $"Expected tools/list to return 200 OK but got {(int)listResponse.StatusCode} {listResponse.StatusCode}: {listBody}");
        Assert.Contains("\"name\":\"echo\"", listBody);
        Assert.Contains("\"required\":[\"message\"]", listBody);

        using var callRequest = new HttpRequestMessage(HttpMethod.Post, "mcp/")
        {
            Content = JsonContent("""{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"echo","arguments":{"message":"hello"}}}"""),
        };
        callRequest.Headers.Add("Mcp-Session-Id", sessionId);

        Task<HttpResponseMessage> callResponseTask = client.SendAsync(callRequest);

        ModernMcpToolCallContext pendingCall = await WaitForPendingCallAsync(session, "echo").ConfigureAwait(false);
        Assert.Equal("hello", pendingCall.Arguments["message"]!.GetValue<string>());

        Assert.True(pendingCall.TryRespondWithText("echo: hello"));

        using HttpResponseMessage callResponse = await callResponseTask.ConfigureAwait(false);
        string callBody = await callResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.OK, callResponse.StatusCode);
        Assert.Contains("\"text\":\"echo: hello\"", callBody);
    }

    private static async Task ModernMcpServerSessionManagesLifecycleAsync()
    {
        int port = GetAvailablePort();
        await using var session = new ModernMcpServerSession();

        await session.ReconfigureAsync(
            port,
            "Swiftlet Session",
            [
                new McpToolDefinition("echo", "Echoes text."),
            ]).ConfigureAwait(false);

        Assert.True(session.IsRunning);
        Assert.Equal(port, session.Port);
        Assert.Equal("Swiftlet Session", session.ServerName);
        Assert.Contains($"http://localhost:{port}/mcp", session.StatusMessage!);

        string tempDirectory = Path.Combine(Path.GetTempPath(), "SwiftletTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            string assemblyLocation = Path.Combine(tempDirectory, "Swiftlet.Gha");
            File.WriteAllText(Path.Combine(tempDirectory, OperatingSystem.IsWindows() ? "SwiftletBridge.exe" : "SwiftletBridge"), string.Empty);

            string config = session.GenerateConfig(assemblyLocation);
            Assert.Contains("\"Swiftlet Session\"", config);
            Assert.Contains("\"type\": \"stdio\"", config);
            Assert.Contains($"\"http://localhost:{port}/mcp/\"", config);

            string lmStudioConfig = session.GenerateConfig(assemblyLocation, McpClientConfigTarget.LmStudio);
            Assert.Contains("\"mcpServers\"", lmStudioConfig);
            Assert.Contains($"\"url\": \"http://localhost:{port}/mcp/\"", lmStudioConfig);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }

        await session.StopAsync().ConfigureAwait(false);
        Assert.False(session.IsRunning);
        Assert.Equal(0, session.Port);
        Assert.Equal("Stopped", session.StatusMessage);
    }

    private static async Task ModernMcpResponseWorkflowPreservesSemanticsAsync()
    {
        McpToolResult? sentResult = null;
        var request = new ModernMcpToolCallContext(
            "call-1",
            "echo",
            new JsonObject
            {
                ["message"] = "hello",
            },
            result =>
            {
                sentResult = result.Duplicate();
                return true;
            });

        (ModernMcpToolCallContext passThrough, string toolName, JsonObject arguments) = ModernMcpResponseWorkflow.Deconstruct(request);
        Assert.True(ReferenceEquals(request, passThrough));
        Assert.Equal("echo", toolName);
        Assert.Equal("hello", arguments["message"]!.GetValue<string>());

        bool firstSuccess = ModernMcpResponseWorkflow.TrySendResponse(
            request,
            new JsonObject
            {
                ["ok"] = true,
            },
            isError: false);

        Assert.True(firstSuccess);
        Assert.False(ModernMcpResponseWorkflow.TrySendResponse(request, JsonValue.Create("again"), isError: false));
        Assert.NotNull(sentResult);
        Assert.False(sentResult!.IsError);
        Assert.Equal("{\"ok\":true}", sentResult.Content[0].ToJson()["text"]!.GetValue<string>());

        McpToolResult? sentError = null;
        var errorRequest = new ModernMcpToolCallContext(
            "call-2",
            "echo",
            new JsonObject(),
            result =>
            {
                sentError = result.Duplicate();
                return true;
            });

        Assert.True(ModernMcpResponseWorkflow.TrySendResponse(errorRequest, JsonValue.Create("boom"), isError: true));
        Assert.NotNull(sentError);
        Assert.True(sentError!.IsError);
        Assert.Equal("boom", sentError.Content[0].ToJson()["text"]!.GetValue<string>());
    }

    private static async Task ModernServerRoutesRequestsAndCompletesResponsesAsync()
    {
        int port = GetAvailablePort();
        var server = new ModernServer(["/", "/api", "/api/v1"]);

        await using var session = new ModernServerSession(server: server);
        await session.ReconfigureAsync(port, ["/", "/api", "/api/v1"]).ConfigureAwait(false);

        using var client = new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{port}/"),
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/echo?name=swiftlet")
        {
            Content = new StringContent("hello", Encoding.UTF8, "text/plain"),
        };
        request.Headers.Add("X-Test", "123");

        Task<HttpResponseMessage> responseTask = client.SendAsync(request);

        ModernServerRequestContext pendingRequest = await WaitForPendingRequestAsync(server, "/api/v1").ConfigureAwait(false);
        Assert.Equal("POST", pendingRequest.Method);
        Assert.Equal("/api/v1/echo", pendingRequest.Path);
        Assert.Equal("/api/v1", pendingRequest.MatchedRoute);
        Assert.Equal("hello", Encoding.UTF8.GetString(pendingRequest.Body.ToByteArray()));
        Assert.True(pendingRequest.Headers.Any(static header => header.Key == "X-Test" && header.Value == "123"));
        Assert.True(pendingRequest.QueryParameters.Any(static parameter => parameter.Key == "name" && parameter.Value == "swiftlet"));

        bool firstResponse = pendingRequest.TrySetResponse(
            ModernServerHttpResponse.FromBody(
                201,
                new Swiftlet.Core.Http.RequestBodyText("text/plain", "echo: hello"),
                [new Swiftlet.Core.Http.HttpHeader("X-Reply", "done")]));
        bool secondResponse = pendingRequest.TrySetResponse(ModernServerHttpResponse.Empty());

        Assert.True(firstResponse);
        Assert.False(secondResponse);

        using HttpResponseMessage response = await responseTask.ConfigureAwait(false);
        string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("echo: hello", responseBody);
        Assert.Equal("done", response.Headers.GetValues("X-Reply").Single());
    }

    private static async Task ModernWebSocketServerAndClientExchangeMessagesAsync()
    {
        int port = GetAvailablePort();

        await using var serverSession = new ModernWebSocketServerSession();
        await serverSession.ReconfigureAsync(port).ConfigureAwait(false);

        await using var clientSession = new ModernWebSocketClientSession();
        await clientSession.ReconnectAsync($"ws://localhost:{port}/", parameters: null).ConfigureAwait(false);

        Assert.True(clientSession.IsConnected);
        Assert.NotNull(clientSession.Connection);
        Assert.True(serverSession.TryGetAnyOpenConnection(out ModernWebSocketConnection? serverConnectionBeforeMessage));
        Assert.NotNull(serverConnectionBeforeMessage);
        Assert.Equal("Connected", serverConnectionBeforeMessage!.GetStatusString());

        bool sentToServer = await clientSession.Connection!.SendMessageAsync("hello from client").ConfigureAwait(false);
        Assert.True(sentToServer);

        ModernWebSocketReceivedMessage serverMessage = await WaitForWebSocketServerMessageAsync(serverSession).ConfigureAwait(false);
        Assert.Equal("hello from client", serverMessage.Message);
        Assert.True(serverSession.ActiveClientCount > 0);
        Assert.Equal("Connected", serverMessage.Connection.GetStatusString());

        bool sentToClient = await serverMessage.Connection.SendMessageAsync("hello from server").ConfigureAwait(false);
        Assert.True(sentToClient);

        string clientMessage = await WaitForWebSocketClientMessageAsync(clientSession).ConfigureAwait(false);
        Assert.Equal("hello from server", clientMessage);
    }

    private static async Task SimpleHttpListenerCapturesRequestsAndReturnsConfiguredContentAsync()
    {
        int port = GetAvailablePort();
        await using var session = new SimpleHttpListenerSession();
        await session.ReconfigureAsync(
            port,
            "hooks",
            new Swiftlet.Core.Http.RequestBodyText("text/plain", "listener ok")).ConfigureAwait(false);

        using var client = new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{port}/"),
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "hooks/ping?source=test")
        {
            Content = new StringContent("payload", Encoding.UTF8, "text/plain"),
        };
        request.Headers.Add("X-Listener", "true");

        using HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
        string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        SimpleHttpListenerRequest latestRequest = await WaitForSimpleHttpListenerRequestAsync(session).ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("listener ok", responseBody);
        Assert.Equal("POST", latestRequest.Method);
        Assert.Equal("/hooks/ping", latestRequest.Path);
        Assert.Equal("payload", latestRequest.Content);
        Assert.True(latestRequest.Headers.Any(static header => header.Key == "X-Listener" && header.Value == "true"));
        Assert.True(latestRequest.QueryParameters.Any(static parameter => parameter.Key == "source" && parameter.Value == "test"));
    }

    private static async Task McpExportReportsManualClipboardGuidanceAsync()
    {
        var notifications = new CollectingNotificationSink();
        var services = new DesktopHostServices(
            browserLauncher: new StubBrowserLauncher(HostActionResult.Success("Browser launch simulated.")),
            clipboardService: new StubClipboardService(
                HostActionResult.Manual("Clipboard unavailable.", "Copy this MCP config manually.")),
            notifications: notifications);

        HostActionResult result = await ModernMcpWorkflow
            .ExportConfigAsync(services, "{ \"mcpServers\": {} }")
            .ConfigureAwait(false);

        Assert.True(result.RequiresManualAction);
        Assert.Equal(1, notifications.Notifications.Count);
        Assert.Equal(HostNotificationSeverity.Warning, notifications.Notifications[0].Severity);
        Assert.Contains("Copy this MCP config manually.", notifications.Notifications[0].Message);
    }

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private static async Task<string> SendCallbackAsync(int port, string pathAndQuery)
    {
        return await SendRawCallbackAsync(
            port,
            $"GET {pathAndQuery} HTTP/1.1\r\n" +
            $"Host: localhost:{port}\r\n" +
            "Connection: close\r\n\r\n").ConfigureAwait(false);
    }

    private static async Task<string> SendFormPostCallbackAsync(int port, string path, string body)
    {
        byte[] bodyBytes = Encoding.ASCII.GetBytes(body);
        string request =
            $"POST {path} HTTP/1.1\r\n" +
            $"Host: localhost:{port}\r\n" +
            "Content-Type: application/x-www-form-urlencoded\r\n" +
            $"Content-Length: {bodyBytes.Length}\r\n" +
            "Connection: close\r\n\r\n" +
            body;

        return await SendRawCallbackAsync(port, request).ConfigureAwait(false);
    }

    private static async Task<string> SendRawCallbackAsync(int port, string request)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port).ConfigureAwait(false);

        using NetworkStream stream = client.GetStream();
        byte[] requestBytes = Encoding.ASCII.GetBytes(request);
        await stream.WriteAsync(requestBytes).ConfigureAwait(false);
        await stream.FlushAsync().ConfigureAwait(false);

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }

    private static StringContent JsonContent(string json)
    {
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private static async Task<ModernMcpToolCallContext> WaitForPendingCallAsync(
        ModernMcpServerSession session,
        string toolName,
        int maxAttempts = 100)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (session.TryDequeuePendingCall(toolName, out ModernMcpToolCallContext? context) &&
                context is not null)
            {
                return context;
            }

            await Task.Delay(10).ConfigureAwait(false);
        }

        throw new InvalidOperationException($"Timed out waiting for pending MCP call '{toolName}'.");
    }

    private static async Task<ModernServerRequestContext> WaitForPendingRequestAsync(
        ModernServer server,
        string route,
        int maxAttempts = 100)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (server.TryDequeuePendingRequest(route, out ModernServerRequestContext? context) &&
                context is not null)
            {
                return context;
            }

            await Task.Delay(10).ConfigureAwait(false);
        }

        throw new InvalidOperationException($"Timed out waiting for pending server request '{route}'.");
    }

    private static async Task<ModernWebSocketReceivedMessage> WaitForWebSocketServerMessageAsync(
        ModernWebSocketServerSession session,
        int maxAttempts = 100)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (session.TryDequeueMessage(out ModernWebSocketReceivedMessage? message) &&
                message is not null)
            {
                return message;
            }

            await Task.Delay(10).ConfigureAwait(false);
        }

        throw new InvalidOperationException("Timed out waiting for a WebSocket server message.");
    }

    private static async Task<SimpleHttpListenerRequest> WaitForSimpleHttpListenerRequestAsync(
        SimpleHttpListenerSession session,
        int maxAttempts = 100)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (session.LatestRequest is { } request)
            {
                return request;
            }

            await Task.Delay(10).ConfigureAwait(false);
        }

        throw new InvalidOperationException("Timed out waiting for a simple HTTP listener request.");
    }

    private static async Task<string> WaitForWebSocketClientMessageAsync(
        ModernWebSocketClientSession session,
        int maxAttempts = 100)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (session.TryDequeueMessage(out string? message) && !string.IsNullOrEmpty(message))
            {
                return message;
            }

            await Task.Delay(10).ConfigureAwait(false);
        }

        throw new InvalidOperationException("Timed out waiting for a WebSocket client message.");
    }

    private static Type GetShellType(string fullName)
    {
        return typeof(ModernServer).Assembly.GetType(fullName)
               ?? throw new InvalidOperationException($"Unable to find shell type '{fullName}'.");
    }
}

internal sealed class StubBrowserLauncher : IBrowserLauncher
{
    private readonly HostActionResult _result;

    public StubBrowserLauncher(HostActionResult result)
    {
        _result = result;
    }

    public Task<HostActionResult> OpenUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_result);
    }
}

internal sealed class StubClipboardService : IClipboardService
{
    private readonly HostActionResult _result;

    public StubClipboardService(HostActionResult result)
    {
        _result = result;
    }

    public Task<HostActionResult> SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_result);
    }
}

internal static class Assert
{
    public static void True(bool condition, string? message = null)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message ?? "Expected condition to be true.");
        }
    }

    public static void False(bool condition, string? message = null)
    {
        if (condition)
        {
            throw new InvalidOperationException(message ?? "Expected condition to be false.");
        }
    }

    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected '{expected}' but got '{actual}'.");
        }
    }

    public static void NotNull(object? value)
    {
        if (value is null)
        {
            throw new InvalidOperationException("Expected value to be non-null.");
        }
    }

    public static void Null(object? value)
    {
        if (value is not null)
        {
            throw new InvalidOperationException($"Expected null but got '{value}'.");
        }
    }

    public static void Contains(string expectedSubstring, string actual)
    {
        if (!actual.Contains(expectedSubstring, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected '{actual}' to contain '{expectedSubstring}'.");
        }
    }

    public static async Task ThrowsAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected exception of type '{typeof(TException).Name}'.");
    }
}
