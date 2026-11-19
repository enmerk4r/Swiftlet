using System.Net;
using System.Text;
using Swiftlet.Runtime.Transfers;

internal static class FileTransferRestartTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public static async Task RunAsync()
    {
        await LateCallbacksAsync(reset: true, restart: true);
        await LateCallbacksAsync(reset: false, restart: true);
        await LateCallbacksAsync(reset: true, restart: false);
        await DownloadRestartAsync(blockBody: false);
        await DownloadRestartAsync(blockBody: true);
        await UploadRestartAsync(multipart: false);
        await UploadRestartAsync(multipart: true);
    }

    private static async Task LateCallbacksAsync(bool reset, bool restart)
    {
        using var job = new ControlledJob();
        var oldRun = new Worker();
        var newRun = new Worker();
        int changes = 0;
        job.Changed += _ => Interlocked.Increment(ref changes);
        Task oldTask = job.Start(oldRun, "old");
        await oldRun.Started.Task.WaitAsync(Timeout);
        if (reset)
            job.Reset();
        Task newTask = restart ? job.Start(newRun, "new") : Task.CompletedTask;
        try
        {
            if (restart)
                await newRun.Started.Task.WaitAsync(Timeout);
            TransferSnapshot expected = job.Snapshot;
            oldRun.Release.SetResult();
            await oldTask.WaitAsync(Timeout);
            Assert.True(oldRun.Cancelled, "Reset or replacement must cancel the old worker.");
            Assert.False(oldRun.Published, "A superseded worker must not publish its download.");
            Assert.Equal(expected, job.Snapshot);
            Assert.Equal(0, changes);
            if (restart)
            {
                newRun.Release.SetResult();
                await newTask.WaitAsync(Timeout);
                Assert.True(newRun.Published);
                Assert.True(job.Snapshot.Success);
                Assert.Equal("new", job.Snapshot.Content);
                Assert.Equal(201, job.Snapshot.StatusCode);
                Assert.Equal(2, changes); // current progress and completion only
            }
            else
                Assert.Equal(TransferSnapshot.Idle, job.Snapshot);
        }
        finally
        {
            oldRun.Release.TrySetResult();
            newRun.Release.TrySetResult();
            await Task.WhenAll(oldTask, newTask).WaitAsync(Timeout);
        }
    }

    private static async Task DownloadRestartAsync(bool blockBody)
    {
        string directory = CreateDirectory();
        string path = Path.Combine(directory, "download.txt");
        await File.WriteAllTextAsync(path, "existing file");
        var started = Signal();
        var release = Signal();
        using var handler = new Handler(async (request, token) =>
        {
            if (request.RequestUri!.AbsolutePath == "/old")
            {
                if (blockBody)
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StreamContent(new DeferredFailureStream(started, release)),
                    };
                started.SetResult();
                await release.Task.ConfigureAwait(false); // fail only after the replacement has finished
                throw new HttpRequestException("Late old response failure");
            }
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("replacement bytes") };
        });
        using var client = new HttpClient(handler);
        using var job = new FileDownloadJob(client);
        int changes = 0;
        job.Changed += _ => Interlocked.Increment(ref changes);
        Task oldTask = job.Start("https://transfer.test/old", path, []);
        try
        {
            await started.Task.WaitAsync(Timeout);
            job.Reset();
            await job.Start("https://transfer.test/new", path, []).WaitAsync(Timeout);
            Assert.True(job.Snapshot.Success, job.Snapshot.Error);
            TransferSnapshot expected = job.Snapshot;
            int expectedChanges = changes;
            release.SetResult();
            await oldTask.WaitAsync(Timeout); // includes the old worker's catch and file cleanup
            Assert.Equal(expected, job.Snapshot);
            Assert.Equal(expectedChanges, changes);
            Assert.Equal("replacement bytes", await File.ReadAllTextAsync(path));
            Assert.Equal(1, Directory.GetFiles(directory).Length); // no abandoned staging files
        }
        finally
        {
            release.TrySetResult();
            await oldTask.WaitAsync(Timeout);
            DeleteDirectory(directory);
        }
    }

    private static async Task UploadRestartAsync(bool multipart)
    {
        string directory = CreateDirectory();
        string path = Path.Combine(directory, "upload.txt");
        await File.WriteAllTextAsync(path, "upload bytes");
        long length = new FileInfo(path).Length;
        var started = Signal();
        var release = Signal();
        using var handler = new Handler(async (request, token) =>
        {
            if (request.RequestUri!.AbsolutePath == "/old")
            {
                started.SetResult();
                await release.Task.ConfigureAwait(false);
                // A transport can finish an already-started read after cancellation. Exercise the
                // real ProgressReadStream callback as well as the late exception/completion path.
                await request.Content!.CopyToAsync(Stream.Null, CancellationToken.None).ConfigureAwait(false);
                throw new HttpRequestException("Late old upload failure");
            }
            await request.Content!.CopyToAsync(Stream.Null, token).ConfigureAwait(false);
            return new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("replacement result") };
        });
        using var client = new HttpClient(handler);
        using FileTransferJob job = multipart ? new FileMultipartUploadJob(client) : new FileUploadJob(client);
        Task Start(string name) => job is FileUploadJob upload
            ? upload.Start("https://transfer.test/" + name, path, "POST", "text/plain", [], length)
            : ((FileMultipartUploadJob)job).Start("https://transfer.test/" + name, path, "file", "upload.txt", "text/plain", [], [], length);
        int changes = 0;
        job.Changed += _ => Interlocked.Increment(ref changes);
        Task oldTask = Start("old");
        try
        {
            await started.Task.WaitAsync(Timeout);
            job.Reset();
            await Start("new").WaitAsync(Timeout);
            Assert.True(job.Snapshot.Success, job.Snapshot.Error);
            Assert.Equal("replacement result", job.Snapshot.Content);
            Assert.Equal(202, job.Snapshot.StatusCode);
            TransferSnapshot expected = job.Snapshot;
            int expectedChanges = changes;
            release.SetResult();
            await oldTask.WaitAsync(Timeout);
            Assert.Equal(expected, job.Snapshot);
            Assert.Equal(expectedChanges, changes);
            Assert.Equal("upload bytes", await File.ReadAllTextAsync(path));
        }
        finally
        {
            release.TrySetResult();
            await oldTask.WaitAsync(Timeout);
            DeleteDirectory(directory);
        }
    }

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static string CreateDirectory() => Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "swiftlet-transfer-" + Guid.NewGuid().ToString("N"))).FullName;
    private static void DeleteDirectory(string directory)
    {
        foreach (string file in Directory.GetFiles(directory))
            File.Delete(file);
        Directory.Delete(directory);
    }

    private sealed class Worker
    {
        public readonly TaskCompletionSource Started = Signal();
        public readonly TaskCompletionSource Release = Signal();
        public bool Cancelled;
        public bool Published;
    }

    private sealed class ControlledJob : FileTransferJob
    {
        public Task Start(Worker worker, string name) => Begin(100, name, async (generation, token) =>
        {
            worker.Started.SetResult();
            await worker.Release.Task.ConfigureAwait(false); // deliberately late cancellation handling
            worker.Cancelled = token.IsCancellationRequested;
            SetStatus(generation, 201);
            ReportProgress(generation, 50, 100);
            Complete(generation, true, null, name, setProgress: true, commit: () => worker.Published = true);
        });
    }

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }

    private sealed class DeferredFailureStream(TaskCompletionSource started, TaskCompletionSource release) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            started.TrySetResult();
            await release.Task.ConfigureAwait(false);
            throw new IOException("Late old download read failure");
        }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
