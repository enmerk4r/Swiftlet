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

                capturedImage = ToSwiftletImage(bitmap);
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

    private static SwiftletImage ToSwiftletImage(Bitmap bitmap)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;
        var pixels = new byte[width * height * 4];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = bitmap.GetPixel(x, y);
                int offset = ((y * width) + x) * 4;
                pixels[offset] = pixel.R;
                pixels[offset + 1] = pixel.G;
                pixels[offset + 2] = pixel.B;
                pixels[offset + 3] = pixel.A;
            }
        }

        return new SwiftletImage(width, height, pixels);
    }
}
