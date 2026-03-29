using System.Net;
using System.Net.Http;
using Swiftlet.Core.Auth;
using Swiftlet.Core.Http;
using Swiftlet.Core.Json;
using Swiftlet.Core.Mcp;
using Swiftlet.Core.Security;
using System.Text.Json.Nodes;

return TestRunner.Run();

internal static class TestRunner
{
    public static int Run()
    {
        List<(string Name, Action Test)> tests =
        [
            ("ContentTypes.ToDisplayName maps known and custom types", ContentTypesMapDisplayNames),
            ("MimeTypeMap resolves known and unknown extensions", MimeTypeMapResolvesExtensions),
            ("UrlBuilder appends encoded query parameters", UrlBuilderAppendsQueryParameters),
            ("RequestBodyFormUrlEncoded serializes form values", RequestBodyFormUrlEncodedSerializesValues),
            ("MultipartField preserves text and bytes in duplicates", MultipartFieldDuplicatesSafely),
            ("RequestBodyMultipartForm compiles multipart content", RequestBodyMultipartFormCompiles),
            ("MultipartBodyParser preserves text newlines and binary payloads", MultipartBodyParserPreservesContent),
            ("MultipartBodyParser keeps byte fields without filenames as bytes", MultipartBodyParserKeepsAnonymousByteFieldsBinary),
            ("MultipartBodyParser ignores boundary-like data inside part content", MultipartBodyParserIgnoresBoundaryLikePayloadData),
            ("HttpResponseData duplicates and preserves headers", HttpResponseDataDuplicates),
            ("HttpResponseData preserves legacy header surface from HttpResponseMessage", HttpResponseDataFromResponseMessagePreservesLegacyHeaders),
            ("OAuth PKCE produces URL-safe verifier, challenge, and state", OAuthPkceGeneratesUrlSafeValues),
            ("OAuth authorization request builds expected URL", OAuthAuthorizationRequestBuildsUrl),
            ("OAuth authorization session bundles verifier, state, and URL", OAuthAuthorizationSessionBuildsState),
            ("OAuth token request serializes authorization code flow", OAuthTokenRequestSerializesAuthorizationCodeFlow),
            ("OAuth token request serializes refresh token flow", OAuthTokenRequestSerializesRefreshFlow),
            ("OAuth token client parses successful token responses", OAuthTokenClientParsesSuccessfulResponses),
            ("HttpRequestDefinition builds full URL and preserves headers", HttpRequestDefinitionBuildsRequestData),
            ("HttpRequestExecutor returns response data from handler", HttpRequestExecutorReturnsResponseData),
            ("JSON object merger recursively merges nested objects", JsonObjectMergerRecursivelyMergesNestedObjects),
            ("JSON object merger supports null-aware conflict modes", JsonObjectMergerSupportsNullAwareConflictModes),
            ("JSON object merger treats arrays and type mismatches as conflicts", JsonObjectMergerTreatsArraysAndTypeMismatchesAsConflicts),
            ("JSON object merger parses valid and invalid modes", JsonObjectMergerParsesModes),
            ("MCP tool definition builds expected input schema", McpToolDefinitionBuildsInputSchema),
            ("MCP client config builder serializes command and args", McpClientConfigBuilderSerializesLaunchCommand),
            ("IpBlacklist matches configured IPv4 and IPv6 ranges", IpBlacklistMatchesConfiguredRanges),
            ("IpBlacklist falls back to block-all for invalid environment configuration", IpBlacklistBlocksAllOnInvalidEnvironmentConfig),
            ("UrlValidator rejects malformed input and blacklisted hosts", UrlValidatorRejectsInvalidAndBlacklistedUrls),
        ];

        foreach ((string name, Action test) in tests)
        {
            test();
            Console.WriteLine($"PASS {name}");
        }

        Console.WriteLine($"Executed {tests.Count} core tests.");
        return 0;
    }

    private static void ContentTypesMapDisplayNames()
    {
        Assert.Equal("JSON", ContentTypes.ToDisplayName(ContentTypes.ApplicationJson));
        Assert.Equal("HTML", ContentTypes.ToDisplayName(ContentTypes.TextHtml));
        Assert.Equal("Custom", ContentTypes.ToDisplayName("application/custom"));
        Assert.Equal("Custom", ContentTypes.ToDisplayName(null));
    }

    private static void MimeTypeMapResolvesExtensions()
    {
        Assert.Equal("application/json", MimeTypeMap.GetMimeType("payload.json"));
        Assert.Equal("image/jpeg", MimeTypeMap.GetMimeType("photo.JPG"));
        Assert.Equal(ContentTypes.ApplicationOctetStream, MimeTypeMap.GetMimeType("README"));
        Assert.Equal(ContentTypes.ApplicationOctetStream, MimeTypeMap.GetMimeType(null));
    }

    private static void UrlBuilderAppendsQueryParameters()
    {
        string fullUrl = UrlBuilder.AddQueryParameters(
            "https://example.com/resource",
            [
                new QueryParameter("first name", "Ada Lovelace"),
                new QueryParameter("city", "New York"),
            ]);

        Assert.Equal(
            "https://example.com/resource?first%20name=Ada%20Lovelace&city=New%20York",
            fullUrl);
    }

    private static void RequestBodyFormUrlEncodedSerializesValues()
    {
        var body = new RequestBodyFormUrlEncoded(
            [
                new KeyValuePair<string, string>("client_id", "swiftlet"),
                new KeyValuePair<string, string>("redirect_uri", "http://localhost:3001/callback"),
            ]);

        Assert.Equal(
            "client_id=swiftlet&redirect_uri=http%3A%2F%2Flocalhost%3A3001%2Fcallback",
            body.ToString());
        Assert.Equal(
            "client_id=swiftlet&redirect_uri=http%3A%2F%2Flocalhost%3A3001%2Fcallback",
            System.Text.Encoding.UTF8.GetString(body.ToByteArray()));
    }

    private static void MultipartFieldDuplicatesSafely()
    {
        var textField = new MultipartField("note", "hello", ContentTypes.TextPlain);
        MultipartField duplicateTextField = textField.Duplicate();

        Assert.True(duplicateTextField.IsText);
        Assert.Equal("hello", duplicateTextField.Text);
        Assert.Equal("note", duplicateTextField.Name);

        var byteField = new MultipartField("file", [1, 2, 3], "data.bin", ContentTypes.ApplicationOctetStream);
        MultipartField duplicateByteField = byteField.Duplicate();

        Assert.False(duplicateByteField.IsText);
        Assert.Equal("data.bin", duplicateByteField.FileName);
        Assert.Equal(3, duplicateByteField.Bytes.Length);
    }

    private static void RequestBodyMultipartFormCompiles()
    {
        var body = new RequestBodyMultipartForm(
            [
                new MultipartField("message", "hello"),
                new MultipartField("payload", [1, 2, 3], "payload.bin", ContentTypes.ApplicationOctetStream),
            ]);

        Assert.True(body.ContentType.StartsWith("multipart/form-data;", StringComparison.OrdinalIgnoreCase));
        Assert.True(body.ToByteArray().Length > 0);
        Assert.Equal(2, body.Fields.Count);
    }

    private static void MultipartBodyParserPreservesContent()
    {
        byte[] payload = [0, 255, 13, 10, 45, 45, 98, 111, 117, 110, 100, 97, 114, 121, 45, 108, 105, 107, 101];
        var body = new RequestBodyMultipartForm(
            [
                new MultipartField("message", "hello\r\n", ContentTypes.TextPlain),
                new MultipartField("payload", payload, "payload.bin", ContentTypes.ApplicationOctetStream),
            ]);

        List<MultipartField> fields = MultipartBodyParser.Parse(body.ToByteArray(), body.ContentType);

        Assert.Equal(2, fields.Count);
        Assert.True(fields[0].IsText);
        Assert.Equal("hello\r\n", fields[0].Text);
        Assert.Equal(ContentTypes.TextPlain, fields[0].ContentType);
        Assert.False(fields[1].IsText);
        Assert.Equal("payload.bin", fields[1].FileName);
        Assert.True(payload.SequenceEqual(fields[1].Bytes));
    }

    private static void MultipartBodyParserKeepsAnonymousByteFieldsBinary()
    {
        byte[] payload = [1, 2, 3, 4, 5];
        var body = new RequestBodyMultipartForm(
            [
                new MultipartField("payload", payload, contentType: ContentTypes.ApplicationOctetStream),
            ]);

        List<MultipartField> fields = MultipartBodyParser.Parse(body.ToByteArray(), body.ContentType);

        Assert.Equal(1, fields.Count);
        Assert.False(fields[0].IsText);
        Assert.True(payload.SequenceEqual(fields[0].Bytes));
    }

    private static void MultipartBodyParserIgnoresBoundaryLikePayloadData()
    {
        const string boundary = "test-boundary";
        byte[] payload = System.Text.Encoding.ASCII.GetBytes("line one\r\n--test-boundary-extra\r\nline two");
        byte[] prefix = System.Text.Encoding.ASCII.GetBytes(
            "--test-boundary\r\n"
            + "Content-Disposition: form-data; name=\"payload\"; filename=\"payload.bin\"\r\n"
            + "Content-Type: application/octet-stream\r\n\r\n");
        byte[] suffix = System.Text.Encoding.ASCII.GetBytes("\r\n--test-boundary--\r\n");
        byte[] body = [.. prefix, .. payload, .. suffix];

        List<MultipartField> fields = MultipartBodyParser.Parse(body, $"multipart/form-data; boundary={boundary}");

        Assert.Equal(1, fields.Count);
        Assert.False(fields[0].IsText);
        Assert.True(payload.SequenceEqual(fields[0].Bytes));
    }

    private static void HttpResponseDataDuplicates()
    {
        var response = new HttpResponseData(
            "1.1",
            200,
            "OK",
            [new HttpHeader("X-Test", "abc")],
            true,
            "{\"ok\":true}",
            [1, 2, 3]);

        HttpResponseData duplicate = response.Duplicate();

        Assert.Equal("1.1", duplicate.Version);
        Assert.Equal(200, duplicate.StatusCode);
        Assert.Equal("abc", duplicate.Headers[0].Value);
        Assert.Equal(3, duplicate.Bytes.Length);
    }

    private static void HttpResponseDataFromResponseMessagePreservesLegacyHeaders()
    {
        using var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("body"),
        };
        responseMessage.Headers.Add("X-Test", "abc");
        responseMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

        HttpResponseData response = HttpResponseData.FromHttpResponseMessage(responseMessage);

        Assert.Equal(1, response.Headers.Count);
        Assert.Equal("X-Test", response.Headers[0].Key);
        Assert.Equal("abc", response.Headers[0].Value);
    }

    private static void OAuthPkceGeneratesUrlSafeValues()
    {
        string verifier = OAuthPkce.GenerateCodeVerifier();
        string challenge = OAuthPkce.GenerateCodeChallenge(verifier);
        string state = OAuthPkce.GenerateState();

        Assert.True(verifier.Length > 0);
        Assert.True(challenge.Length > 0);
        Assert.True(state.Length > 0);
        Assert.False(verifier.Contains('+'));
        Assert.False(verifier.Contains('/'));
        Assert.False(verifier.Contains('='));
        Assert.False(challenge.Contains('+'));
        Assert.False(challenge.Contains('/'));
        Assert.False(challenge.Contains('='));
    }

    private static void OAuthAuthorizationRequestBuildsUrl()
    {
        var request = new OAuthAuthorizationRequest(
            "https://accounts.example.com/oauth2/authorize",
            "swiftlet-client",
            "http://localhost:3001/callback/",
            ["read", "write"],
            "state-123",
            "challenge-456");

        string url = request.BuildUrl();

        Assert.Equal(
            "https://accounts.example.com/oauth2/authorize?response_type=code&client_id=swiftlet-client&redirect_uri=http%3A%2F%2Flocalhost%3A3001%2Fcallback%2F&state=state-123&code_challenge=challenge-456&code_challenge_method=S256&scope=read%20write",
            url);
    }

    private static void OAuthAuthorizationSessionBuildsState()
    {
        var session = new OAuthAuthorizationSession(
            "https://accounts.example.com/oauth2/authorize",
            "swiftlet-client",
            "http://localhost:3001/callback/",
            ["read"]);

        Assert.True(session.CodeVerifier.Length > 0);
        Assert.True(session.State.Length > 0);
        Assert.True(session.AuthorizationUrl.Contains("code_challenge=", StringComparison.Ordinal));
        Assert.True(session.AuthorizationUrl.Contains("state=", StringComparison.Ordinal));
    }

    private static void OAuthTokenRequestSerializesAuthorizationCodeFlow()
    {
        OAuthTokenRequest request = OAuthTokenRequest.ForAuthorizationCode(
            "https://accounts.example.com/oauth2/token",
            "swiftlet-client",
            "code-123",
            "http://localhost:3001/callback/",
            "verifier-456",
            "secret-789");

        string body = request.ToRequestBody().ToString();

        Assert.Equal(
            "client_id=swiftlet-client&grant_type=authorization_code&code=code-123&redirect_uri=http%3A%2F%2Flocalhost%3A3001%2Fcallback%2F&code_verifier=verifier-456&client_secret=secret-789",
            body);
    }

    private static void OAuthTokenRequestSerializesRefreshFlow()
    {
        OAuthTokenRequest request = OAuthTokenRequest.ForRefreshToken(
            "https://accounts.example.com/oauth2/token",
            "swiftlet-client",
            "refresh-123",
            "secret-789");

        string body = request.ToRequestBody().ToString();

        Assert.Equal(
            "client_id=swiftlet-client&grant_type=refresh_token&refresh_token=refresh-123&client_secret=secret-789",
            body);
    }

    private static void OAuthTokenClientParsesSuccessfulResponses()
    {
        var handler = new FakeHttpMessageHandler(_ =>
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"access_token\":\"at-123\",\"refresh_token\":\"rt-456\",\"expires_in\":3600,\"token_type\":\"Bearer\"}"),
            };
        });

        var client = new OAuthTokenClient(new HttpClient(handler));
        OAuthTokenResponse response = client.Exchange(
            OAuthTokenRequest.ForAuthorizationCode(
                "https://accounts.example.com/oauth2/token",
                "swiftlet-client",
                "code-123",
                "http://localhost:3001/callback/"));

        Assert.Equal("at-123", response.AccessToken);
        Assert.Equal("rt-456", response.RefreshToken);
        Assert.Equal(3600, response.ExpiresIn);
        Assert.Equal("Bearer", response.TokenType);
    }

    private static void HttpRequestDefinitionBuildsRequestData()
    {
        var definition = new HttpRequestDefinition(
            "https://api.example.com/resource",
            "post",
            new RequestBodyText(ContentTypes.ApplicationJson, "{\"ok\":true}"),
            [new QueryParameter("page", "1")],
            [new HttpHeader("X-Test", "abc")],
            30);

        Assert.Equal("https://api.example.com/resource?page=1", definition.BuildUrl());
        Assert.Equal("post", definition.Method);
        Assert.Equal("abc", definition.Headers[0].Value);
        Assert.Equal(30, definition.TimeoutSeconds);
    }

    private static void HttpRequestExecutorReturnsResponseData()
    {
        var handler = new FakeHttpMessageHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                Content = new StringContent("{\"status\":\"ok\"}"),
            };
            response.Headers.Add("X-Test", "abc");
            return response;
        });

        var executor = new HttpRequestExecutor(new HttpClient(handler));
        var definition = new HttpRequestDefinition("https://api.example.com", "GET");

        HttpResponseData response = executor.Execute(definition);

        Assert.Equal(202, response.StatusCode);
        Assert.Equal("{\"status\":\"ok\"}", response.Content);
        Assert.Equal("abc", response.Headers.Single(header => header.Key == "X-Test").Value);
    }

    private static void JsonObjectMergerRecursivelyMergesNestedObjects()
    {
        JsonObject objectA = ParseJsonObject("""
            {
              "name": "base",
              "settings": {
                "enabled": true,
                "theme": "light"
              },
              "count": 1
            }
            """);
        JsonObject objectB = ParseJsonObject("""
            {
              "settings": {
                "theme": "dark",
                "timeout": 30
              },
              "count": 2,
              "extra": "value"
            }
            """);

        JsonObject merged = JsonObjectMerger.Merge(objectA, objectB, JsonObjectMergeConflictMode.PreferA);

        AssertJsonEqual(
            ParseJsonObject("""
                {
                  "name": "base",
                  "settings": {
                    "enabled": true,
                    "theme": "light",
                    "timeout": 30
                  },
                  "count": 1,
                  "extra": "value"
                }
                """),
            merged);
    }

    private static void JsonObjectMergerSupportsNullAwareConflictModes()
    {
        JsonObject objectA = ParseJsonObject("""
            {
              "keepA": "a",
              "preferBWhenANull": null,
              "nested": {
                "preferBWhenANull": null,
                "keepA": "left"
              }
            }
            """);
        JsonObject objectB = ParseJsonObject("""
            {
              "keepA": "b",
              "preferBWhenANull": "filled",
              "nested": {
                "preferBWhenANull": 99,
                "keepA": "right"
              }
            }
            """);

        JsonObject preferAUnlessNull = JsonObjectMerger.Merge(objectA, objectB, JsonObjectMergeConflictMode.PreferAUnlessNull);
        AssertJsonEqual(
            ParseJsonObject("""
                {
                  "keepA": "a",
                  "preferBWhenANull": "filled",
                  "nested": {
                    "preferBWhenANull": 99,
                    "keepA": "left"
                  }
                }
                """),
            preferAUnlessNull);

        JsonObject preferBUnlessNull = JsonObjectMerger.Merge(
            ParseJsonObject("""{ "value": "left", "other": 1 }"""),
            ParseJsonObject("""{ "value": null, "other": 2 }"""),
            JsonObjectMergeConflictMode.PreferBUnlessNull);

        AssertJsonEqual(
            ParseJsonObject("""{ "value": "left", "other": 2 }"""),
            preferBUnlessNull);
    }

    private static void JsonObjectMergerTreatsArraysAndTypeMismatchesAsConflicts()
    {
        JsonObject objectA = ParseJsonObject("""
            {
              "items": [1, 2],
              "shape": {
                "kind": "circle"
              }
            }
            """);
        JsonObject objectB = ParseJsonObject("""
            {
              "items": [3],
              "shape": "square"
            }
            """);

        JsonObject merged = JsonObjectMerger.Merge(objectA, objectB, JsonObjectMergeConflictMode.PreferB);

        AssertJsonEqual(
            ParseJsonObject("""
                {
                  "items": [3],
                  "shape": "square"
                }
                """),
            merged);
    }

    private static void JsonObjectMergerParsesModes()
    {
        Assert.True(JsonObjectMerger.TryParseConflictMode(0, out JsonObjectMergeConflictMode preferA));
        Assert.Equal(JsonObjectMergeConflictMode.PreferA, preferA);

        Assert.True(JsonObjectMerger.TryParseConflictMode(3, out JsonObjectMergeConflictMode preferBUnlessNull));
        Assert.Equal(JsonObjectMergeConflictMode.PreferBUnlessNull, preferBUnlessNull);

        Assert.False(JsonObjectMerger.TryParseConflictMode(42, out JsonObjectMergeConflictMode fallback));
        Assert.Equal(JsonObjectMerger.DefaultConflictMode, fallback);
    }

    private static void McpClientConfigBuilderSerializesLaunchCommand()
    {
        string json = McpClientConfigBuilder.Build(
            "Swiftlet",
            new BridgeLaunchCommand("dotnet", ["/tmp/SwiftletBridge.dll", "http://localhost:3001/mcp/"]));

        Assert.Contains("\"command\": \"dotnet\"", json);
        Assert.Contains("\"Swiftlet\"", json);
        Assert.Contains("\"http://localhost:3001/mcp/\"", json);
    }

    private static void McpToolDefinitionBuildsInputSchema()
    {
        var definition = new McpToolDefinition(
            "fetch_data",
            "Fetches data.",
            [
                new McpToolParameter("url", "string", "Endpoint URL"),
                new McpToolParameter("timeout", "integer", "Timeout in seconds", required: false),
            ]);

        string json = definition.ToJson().ToJsonString();

        Assert.Contains("\"name\":\"fetch_data\"", json);
        Assert.Contains("\"description\":\"Fetches data.\"", json);
        Assert.Contains("\"required\":[\"url\"]", json);
        Assert.Contains("\"timeout\":{\"type\":\"integer\"", json);
    }

    private static void IpBlacklistMatchesConfiguredRanges()
    {
        IpBlacklist blacklist = new(["127.0.0.0/8", "2001:db8::/32"]);

        Assert.True(blacklist.IsAddressBlacklisted(IPAddress.Parse("127.0.0.1")));
        Assert.True(blacklist.IsAddressBlacklisted(IPAddress.Parse("2001:db8::1")));
        Assert.False(blacklist.IsAddressBlacklisted(IPAddress.Parse("8.8.8.8")));
    }

    private static void IpBlacklistBlocksAllOnInvalidEnvironmentConfig()
    {
        const string variableName = "SWIFTLET_TEST_BLOCKED_SUBNETS";
        string? original = Environment.GetEnvironmentVariable(variableName);

        try
        {
            Environment.SetEnvironmentVariable(variableName, "127.0.0.0/8;not-a-cidr");
            IpBlacklist blacklist = IpBlacklist.FromEnvironment(variableName);

            Assert.True(blacklist.IsAddressBlacklisted(IPAddress.Parse("8.8.8.8")));
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, original);
        }
    }

    private static void UrlValidatorRejectsInvalidAndBlacklistedUrls()
    {
        IIpBlacklist blacklist = new IpBlacklist(["10.0.0.0/8"]);

        UrlValidationResult missingScheme = UrlValidator.ValidateHttpUrl("example.com");
        Assert.False(missingScheme.IsValid);
        Assert.Equal("URL must include a scheme (http:// or https://)", missingScheme.ErrorMessage);

        UrlValidationResult queryInBaseUrl = UrlValidator.ValidateHttpUrl("https://example.com?x=1");
        Assert.False(queryInBaseUrl.IsValid);
        Assert.Equal("Please do not include query parameters in your URL. Use the Params (P) input instead.", queryInBaseUrl.ErrorMessage);

        UrlValidationResult blacklistedDns = UrlValidator.ValidateHttpUrl(
            "https://internal.example",
            blacklist,
            _ => new IPHostEntry { AddressList = [IPAddress.Parse("10.1.2.3")] });

        Assert.False(blacklistedDns.IsValid);
        Assert.Equal("The given hostname or IP address is blacklisted.", blacklistedDns.ErrorMessage);

        UrlValidationResult allowedDns = UrlValidator.ValidateHttpUrl(
            "https://public.example",
            blacklist,
            _ => new IPHostEntry { AddressList = [IPAddress.Parse("8.8.8.8")] });

        Assert.True(allowedDns.IsValid);
        Assert.Null(allowedDns.ErrorMessage);
    }

    private static JsonObject ParseJsonObject(string json)
    {
        return JsonNode.Parse(json)?.AsObject() ?? throw new InvalidOperationException("Failed to parse JSON object.");
    }

    private static void AssertJsonEqual(JsonObject expected, JsonObject actual)
    {
        if (!JsonNode.DeepEquals(expected, actual))
        {
            throw new InvalidOperationException($"Expected '{expected.ToJsonString()}' but got '{actual.ToJsonString()}'.");
        }
    }
}

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        _responseFactory = responseFactory;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_responseFactory(request));
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
}
