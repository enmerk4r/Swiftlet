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
    private static int FreePort()
    { var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); int port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop(); return port; }
    private static void AssertPortReleased(int port)
    { var listener = new TcpListener(IPAddress.Loopback, port); listener.Start(); listener.Stop(); }
}
