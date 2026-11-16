using System.Drawing;
using global::Rhino.Display;
using Swiftlet.Imaging;

namespace Swiftlet.Hosts.Rhino;

internal static class RhinoViewportCapture
{
    public static bool TryCaptureActiveViewport(
        out SwiftletImage? image,
        out string? errorMessage,
        CancellationToken cancellationToken = default)
    {
        return TryCapture(
            doc => doc.Views.ActiveView,
            out image,
            out errorMessage,
            cancellationToken);
    }

    public static bool TryCaptureViewport(
        string viewportName,
        out SwiftletImage? image,
        out string? errorMessage,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(viewportName))
        {
            image = null;
            errorMessage = "Viewport name is required.";
            return false;
        }

        return TryCapture(
            doc => doc.Views.Find(viewportName, false),
            out image,
            out errorMessage,
            cancellationToken);
    }

    private static bool TryCapture(
        Func<global::Rhino.RhinoDoc, RhinoView?> resolveView,
        out SwiftletImage? image,
        out string? errorMessage,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resolveView);

        SwiftletImage? capturedImage = null;
        string? capturedError = null;

        // Not disposed on purpose: after a cancelled wait the queued UI action may still call Set().
        var done = new ManualResetEventSlim(false);

        Action capture = () =>
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    capturedError = "Viewport capture was cancelled.";
                    return;
                }

                global::Rhino.RhinoDoc? doc = global::Rhino.RhinoDoc.ActiveDoc;
                if (doc is null)
                {
                    capturedError = "No active Rhino document.";
                    return;
                }

                RhinoView? view = resolveView(doc);
                if (view is null)
                {
                    capturedError = "Viewport was not found.";
                    return;
                }

                using Bitmap? bitmap = view.CaptureToBitmap();
                if (bitmap is null)
                {
                    capturedError = "Viewport capture failed.";
                    return;
                }

                capturedImage = SystemDrawingBitmapConverter.ToSwiftletImage(bitmap);
            }
            catch (Exception ex)
            {
                capturedError = ex.Message;
            }
            finally
            {
                done.Set();
            }
        };

        if (global::Rhino.RhinoApp.InvokeRequired)
        {
            // Called from a solver thread (Grasshopper 2). Run on the UI thread, but stop waiting
            // if the solution is cancelled while the UI thread is busy.
            global::Rhino.RhinoApp.InvokeOnUiThread(capture);
            try
            {
                done.Wait(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                image = null;
                errorMessage = "Viewport capture was cancelled.";
                return false;
            }
        }
        else
        {
            capture();
        }

        image = capturedImage;
        errorMessage = capturedError;
        return image is not null;
    }
}
