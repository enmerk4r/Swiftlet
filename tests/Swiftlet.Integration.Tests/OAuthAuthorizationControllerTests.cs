using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Web;
using Swiftlet.HostAbstractions;
using Swiftlet.Hosts.Desktop;
using Swiftlet.Runtime;

internal static class OAuthAuthorizationControllerTests
{
    public static async Task RunAsync()
    {
        var browser = new Browser();
        await using var controller = new OAuthAuthorizationController(new DesktopHostServices(browserLauncher: browser));
        OAuthAuthorizationChangedEventArgs? lastChange = null;
        controller.Changed += (_, args) => lastChange = (OAuthAuthorizationChangedEventArgs)args;
        int port = FreePort();
        var ready = await controller.EvaluateAsync("https://auth.example.test/authorize", "client", ["openid"], port, false);
        Assert.False(ready.IsWaiting); Assert.Equal(0, browser.Count);
        var waiting = await controller.EvaluateAsync("https://auth.example.test/authorize", "client", ["openid"], port, true);
        Assert.True(waiting.IsWaiting); Assert.NotNull(waiting.Verifier); Assert.Equal(1, browser.Count);
        await controller.EvaluateAsync("https://auth.example.test/authorize", "client", ["openid"], port, true);
        Assert.Equal(1, browser.Count);
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        await client.GetStringAsync($"http://localhost:{port}/callback/?code=bad-code&state=wrong");
        await UntilAsync(() => !controller.Snapshot.IsWaiting);
        Assert.Null(controller.Snapshot.Code); Assert.Contains("State mismatch", controller.Snapshot.Error!);
        Assert.False(controller.Snapshot.ReportError, "Provider callback failures preserve the GH1 status-output contract.");
        await UntilAsync(() => lastChange is not null);
        var finishedGeneration = lastChange!.Generation;
        await controller.ResetAsync();
        Assert.True(finishedGeneration != controller.Generation, "Reset must invalidate queued notifications from the old attempt.");
        Assert.Null(controller.Snapshot.Code);
        AssertPortReleased(port);
        await controller.EvaluateAsync("https://auth.example.test/authorize", "client", ["openid"], port, true);
        string state = HttpUtility.ParseQueryString(new Uri(browser.Url!).Query)["state"]!;
        await client.GetStringAsync($"http://localhost:{port}/callback/?code=good-code&state={Uri.EscapeDataString(state)}");
        await UntilAsync(() => controller.Snapshot.Code == "good-code");
        Assert.Equal("Authorization successful", controller.Snapshot.Status);
        AssertPortReleased(port);
        await controller.ResetAsync();
        await controller.EvaluateAsync("https://auth.example.test/authorize", "client", [], port, true);
        Assert.True(controller.Snapshot.IsWaiting);
        await controller.ResetAsync();
        Assert.False(controller.Snapshot.IsWaiting); Assert.Null(controller.Snapshot.Code);
        AssertPortReleased(port);
        // Reusing the same controller after reset creates a fresh verifier/state, never the canceled result.
        string oldUrl = browser.Url!;
        await controller.EvaluateAsync("https://auth.example.test/authorize", "client", [], port, true);
        Assert.True(oldUrl != browser.Url);
        await controller.ResetAsync(); AssertPortReleased(port);
    }
    private static int FreePort()
    { var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); int port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop(); return port; }
    private static void AssertPortReleased(int port)
    { var listener = new TcpListener(IPAddress.Loopback, port); listener.Start(); listener.Stop(); }
    private static async Task UntilAsync(Func<bool> predicate)
    { using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5)); while (!predicate()) await Task.Delay(10, timeout.Token); }
    private sealed class Browser : IBrowserLauncher
    {
        public int Count; public string? Url;
        public Task<HostActionResult> OpenUrlAsync(string url, CancellationToken cancellationToken = default)
        { Count++; Url = url; return Task.FromResult(HostActionResult.Success("Browser launch simulated")); }
    }
}
