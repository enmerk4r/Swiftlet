using System.Net.Sockets;

namespace Swiftlet.Runtime;

/// <summary>
/// Receives UDP datagrams on a port and remembers the most recent one. Shared by the Grasshopper 1
/// and Grasshopper 2 "UDP Listener" components.
/// </summary>
public sealed class UdpListenerSession : IDisposable
{
    private readonly object _sync = new();
    private UdpClient? _client;
    private CancellationTokenSource? _cancellation;
    private byte[]? _latestData;
    private string? _latestRemoteEndpoint;

    /// <summary>Raised on a background thread after a datagram has been stored.</summary>
    public event EventHandler? DataReceived;

    /// <summary>Raised on a background thread when the receive loop stops because of an unexpected error.</summary>
    public event EventHandler<string>? Faulted;

    public int Port { get; private set; }

    public bool IsRunning
    {
        get
        {
            lock (_sync)
            {
                return _client is not null;
            }
        }
    }

    /// <summary>The most recent datagram, or null when nothing has been received yet.</summary>
    public byte[]? LatestData
    {
        get
        {
            lock (_sync)
            {
                return _latestData;
            }
        }
    }

    /// <summary>Sender of the most recent datagram as "address:port".</summary>
    public string? LatestRemoteEndpoint
    {
        get
        {
            lock (_sync)
            {
                return _latestRemoteEndpoint;
            }
        }
    }

    /// <summary>
    /// Listens on <paramref name="port"/>. Does nothing when already listening on that port; otherwise
    /// the previous socket is closed first. Received data is kept across a port change, as before.
    /// </summary>
    public void Start(int port)
    {
        lock (_sync)
        {
            if (_client is not null && Port == port)
            {
                return;
            }

            StopCore();
            Port = port;

            var cancellation = new CancellationTokenSource();
            var client = new UdpClient(port);
            _cancellation = cancellation;
            _client = client;
            _ = Task.Run(() => ReceiveLoopAsync(client, cancellation.Token));
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            StopCore();
        }
    }

    public void Dispose() => Stop();

    private void StopCore()
    {
        try
        {
            _cancellation?.Cancel();
            _client?.Close();
            _client?.Dispose();
        }
        catch
        {
            // Closing a socket that is already gone is not an error worth surfacing.
        }
        finally
        {
            _client = null;
            _cancellation?.Dispose();
            _cancellation = null;
        }
    }

    private async Task ReceiveLoopAsync(UdpClient client, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                UdpReceiveResult result = await client.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                lock (_sync)
                {
                    // A datagram that arrives while this socket is being replaced belongs to the old listener.
                    if (!ReferenceEquals(client, _client))
                    {
                        return;
                    }

                    _latestData = result.Buffer;
                    _latestRemoteEndpoint = $"{result.RemoteEndPoint.Address}:{result.RemoteEndPoint.Port}";
                }

                DataReceived?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (SocketException)
        {
        }
        catch (Exception ex)
        {
            Faulted?.Invoke(this, ex.Message);
        }
    }
}
