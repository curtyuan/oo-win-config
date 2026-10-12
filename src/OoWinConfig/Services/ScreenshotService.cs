using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OoWinConfig.Services;

public sealed class ScreenshotService
{
    private readonly string _screenshotsDirectory;

    public ScreenshotService(string dataDirectory)
    {
        _screenshotsDirectory = Path.Combine(dataDirectory, "debug", "screenshots");
    }

    public string Capture(FrameworkElement element)
    {
        Directory.CreateDirectory(_screenshotsDirectory);

        var width = (int)Math.Ceiling(element.ActualWidth);
        var height = (int)Math.Ceiling(element.ActualHeight);
        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException("Cannot capture an element with no rendered size.");
        }

        var renderTarget = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        renderTarget.Render(element);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(renderTarget));

        var filePath = Path.Combine(_screenshotsDirectory, $"oo-win-config-{DateTime.Now:yyyyMMdd-HHmmss}.png");
        using var stream = File.Create(filePath);
        encoder.Save(stream);

        return filePath;
    }
}
