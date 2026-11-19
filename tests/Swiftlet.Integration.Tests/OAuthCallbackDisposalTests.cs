using System.Net;
using System.Net.Sockets;
using System.Text;
using Swiftlet.Hosts.Desktop;
using Swiftlet.HostAbstractions;

internal static class OAuthCallbackDisposalTests
{
    public static async Task RunAsync()
    {
        var factory = new LoopbackHttpCallbackListenerFactory();
        int port = FreePort();
        // Disposal alone owns shutdown; a caller cancellation must not be required.
        for (int i = 0; i < 32; i++)
        {
            await using var session = await factory.StartAsync(new Uri($"http://localhost:{port}/callback/"));
            using var cleanup = new CancellationTokenSource();
            Task<LocalHttpCallbackRequest> pending = session.WaitForCallbackAsync(cleanup.Token);
            try
            {
                await Task.WhenAll(session.DisposeAsync().AsTask(), session.DisposeAsync().AsTask()).WaitAsync(TimeSpan.FromSeconds(3));
                AssertPortReleased(port);
                Assert.True(pending.IsCompleted, "Dispose must await its pending accept/read task.");
            }
            finally { cleanup.Cancel(); try { await pending; } catch { } }
        }
        // A connected browser can stall before completing the request headers.
        await using (var session = await factory.StartAsync(new Uri($"http://localhost:{port}/callback/")))
        {
            Task<LocalHttpCallbackRequest> pending = session.WaitForCallbackAsync();
            using var browser = new TcpClient();
            await browser.ConnectAsync(IPAddress.Loopback, port);
            await browser.GetStream().WriteAsync(Encoding.ASCII.GetBytes("GET /callback/ HTTP/1.1\r\nHost: localhost\r\n"));
            await Task.WhenAll(session.DisposeAsync().AsTask(), session.DisposeAsync().AsTask()).WaitAsync(TimeSpan.FromSeconds(3));
            Assert.True(pending.IsCompleted, "Dispose must unblock an incomplete HTTP request.");
            try { await pending; } catch { }
            AssertPortReleased(port);
        }
    }
    public static async Task IgnoreNonCallbacksAsync()
    {
        var factory = new LoopbackHttpCallbackListenerFactory();
        foreach (string kind in new[] { "empty", "favicon", "malformed", "idle" })
        {
            int port = FreePort();
            await using var session = await factory.StartAsync(new Uri($"http://localhost:{port}/callback/"));
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var pending = session.WaitForCallbackAsync(timeout.Token);
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(9) };
            using var idle = kind == "idle" ? new TcpClient() : null;
            using var idle2 = kind == "idle" ? new TcpClient() : null;
            if (idle is not null) await idle.ConnectAsync(IPAddress.Loopback, port);
            if (idle2 is not null) await idle2.ConnectAsync(IPAddress.Loopback, port);
            if (kind == "favicon")
            {
                using var favicon = await http.GetAsync($"http://localhost:{port}/favicon.ico");
                Assert.Equal(HttpStatusCode.NotFound, favicon.StatusCode);
            }
            else if (kind != "idle")
            {
                using var probe = new TcpClient();
                await probe.ConnectAsync(IPAddress.Loopback, port);
                if (kind == "malformed")
                    await probe.GetStream().WriteAsync(Encoding.ASCII.GetBytes("not-http\r\n\r\n"));
            }
            var elapsed = System.Diagnostics.Stopwatch.StartNew();
            var response = http.GetAsync($"http://localhost:{port}/callback/?code=valid&state=expected");
            var request = await pending.WaitAsync(timeout.Token);
            Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(2), "An idle preconnect must not delay the real callback until its five-second read deadline.");
            Assert.Equal("/callback/", request.RequestUri.AbsolutePath);
            Assert.Equal("valid", request.GetQueryParameter("code"));
            await session.SendResponseAsync(LocalHttpCallbackResponse.Html(200, "done"), timeout.Token);
            using var completed = await response;
            Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        }
    }
    private static int FreePort()
    { var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); int port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop(); return port; }
    private static void AssertPortReleased(int port)
    { var listener = new TcpListener(IPAddress.Loopback, port); listener.Start(); listener.Stop(); }
}
