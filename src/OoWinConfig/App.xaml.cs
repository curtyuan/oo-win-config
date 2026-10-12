using System.Windows;
using System.Windows.Threading;

namespace OoWinConfig;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();

        if (e.Args.Contains("--capture-ui"))
        {
            var viewArg = GetArgumentValue(e.Args, "--view");
            mainWindow.ApplyAutomationView(viewArg);
            var exitAfterCapture = e.Args.Contains("--exit-after-capture");
            _ = CaptureUiAfterRender(mainWindow, exitAfterCapture);
        }
    }

    private static string? GetArgumentValue(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private async Task CaptureUiAfterRender(MainWindow mainWindow, bool exitAfterCapture)
    {
        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        await Task.Delay(300);

        mainWindow.CaptureUiScreenshot();

        if (exitAfterCapture)
        {
            Shutdown();
        }
    }
}

