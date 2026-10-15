using System.Windows;
using System.Windows.Controls;

namespace OoWinConfig.Views;

public partial class RemoteView : UserControl
{
    public event EventHandler? OpenSshConfigRequested;

    public RemoteView()
    {
        InitializeComponent();
    }

    private void OpenSshConfig_OnClick(object sender, RoutedEventArgs e)
    {
        OpenSshConfigRequested?.Invoke(this, EventArgs.Empty);
    }
}
