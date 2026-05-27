using System.Drawing;
using Rhino.Display;
using Swiftlet.Imaging;

namespace Swiftlet.Gh.Rhino8;

internal static class RhinoViewportCapture
{
    public static bool TryCaptureActiveViewport(out SwiftletImage? image, out string? errorMessage)
    {
        return TryCapture(
            doc => doc.Views.ActiveView,
            out image,
            out errorMessage);
    }

    public static bool TryCaptureViewport(string viewportName, out SwiftletImage? image, out string? errorMessage)
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
            out errorMessage);
    }

    private static bool TryCapture(
        Func<Rhino.RhinoDoc, RhinoView?> resolveView,
        out SwiftletImage? image,
        out string? errorMessage)
    {
        ArgumentNullException.ThrowIfNull(resolveView);

        SwiftletImage? capturedImage = null;
        string? capturedError = null;
        using var done = new ManualResetEventSlim(false);

        Rhino.RhinoApp.InvokeOnUiThread((Action)(() =>
        {
            try
            {
                Rhino.RhinoDoc? doc = Rhino.RhinoDoc.ActiveDoc;
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
        }));

        done.Wait();

        image = capturedImage;
        errorMessage = capturedError;
        return image is not null;
    }
}
