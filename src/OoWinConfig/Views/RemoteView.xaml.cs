using System.Windows.Controls;
using System.Windows;

namespace OoWinConfig.Views;

public partial class RemoteView : UserControl
{
    public RemoteView()
    {
        InitializeComponent();
    }

    private void NewRemote_OnClick(object sender, RoutedEventArgs e)
    {
        NewRemoteOverlay.Visibility = Visibility.Visible;
    }

    private void CloseNewRemote_OnClick(object sender, RoutedEventArgs e)
    {
        NewRemoteOverlay.Visibility = Visibility.Collapsed;
    }

    private void TestSsh_OnClick(object sender, RoutedEventArgs e)
    {
        NewRemoteOverlay.Visibility = Visibility.Collapsed;
        SshConfigPanel.Visibility = Visibility.Collapsed;
        SshColumn.Width = new GridLength(0);
        RemoteCollectionPanel.SetValue(Grid.ColumnProperty, 0);
        RemoteCollectionPanel.SetValue(Grid.ColumnSpanProperty, 2);
        RemoteDisconnectedPanel.Visibility = Visibility.Collapsed;
        RemoteConnectedPanel.Visibility = Visibility.Visible;
    }
}
