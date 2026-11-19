using System.Net;
using System.Net.Http;
using Swiftlet.Core.Auth;
using Swiftlet.Runtime;

internal static class OAuthTokenSessionTests
{
    public static async Task RunAsync()
    {
        int requests = 0;
        bool slow = false;
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var handler = new Handler(async (request, token) =>
        {
            Interlocked.Increment(ref requests);
            string form = await request.Content!.ReadAsStringAsync(token).ConfigureAwait(false);
            if (slow) { started.TrySetResult(true); await Task.Delay(Timeout.Infinite, token).ConfigureAwait(false); }
            if (form.Contains("code=bad")) return new HttpResponseMessage(HttpStatusCode.BadRequest)
                { Content = new StringContent("{\"error\":\"invalid_grant\"}") };
            bool refresh = form.Contains("grant_type=refresh_token");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(refresh ? "{\"access_token\":\"refreshed\",\"expires_in\":60}"
                    : "{\"access_token\":\"original\",\"refresh_token\":\"refresh-1\",\"expires_in\":60}")
            };
        });
        using var client = new HttpClient(handler);
        var session = new OAuthTokenSession(new OAuthTokenClient(client));
        var input = new OAuthTokenInputs("https://oauth.example.test/token", "client", "one-use-code", "verifier", "http://localhost/callback/", "", false);
        var results = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => session.EvaluateAsync(input)));
        Assert.Equal(1, requests); Assert.True(results.All(r => r.AccessToken == "original"));
        var refreshed = await session.EvaluateAsync(input with { Refresh = true });
        Assert.Equal("refreshed", refreshed.AccessToken); Assert.Equal("refresh-1", refreshed.RefreshToken); Assert.Equal(2, requests);
        await session.EvaluateAsync(input with { Refresh = true }); Assert.Equal(2, requests);
        // Dropping the refresh trigger must retain the new tokens, not reuse the one-use code.
        Assert.Equal("refreshed", (await session.EvaluateAsync(input)).AccessToken); Assert.Equal(2, requests);
        slow = true;
        using var cancel = new CancellationTokenSource();
        var pending = session.EvaluateAsync(input with { Refresh = true }, cancel.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5)); cancel.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await pending);
        Assert.Equal("refreshed", session.Cached.AccessToken); Assert.Equal(3, requests);
        slow = false;
        await session.EvaluateAsync(input with { Refresh = true }); Assert.Equal(4, requests);
        var failed = await session.EvaluateAsync(input with { Code = "bad" });
        Assert.Contains("invalid_grant", failed.Error!); Assert.Equal(5, requests);
        await session.EvaluateAsync(input with { Code = "bad" }); Assert.Equal(5, requests);
        var invalid = await session.EvaluateAsync(input with { TokenUrl = "" });
        Assert.Equal("Token URL is required", invalid.Diagnostic); Assert.Equal(5, requests);
    }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
