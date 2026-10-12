using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace OoWinConfig.Views;

public partial class SnapshotMapView : UserControl
{
    public SnapshotMapView()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler? CompareRequested;
    public event TextChangedEventHandler? SearchTextChanged;

    public string SearchText => SearchBox.Text;

    private void SearchBox_OnTextChanged(object sender, TextChangedEventArgs e) => SearchTextChanged?.Invoke(sender, e);
    private void Compare_OnClick(object sender, RoutedEventArgs e) => CompareRequested?.Invoke(sender, e);

    private void SnapshotCompareCard_OnChanged(object sender, RoutedEventArgs e)
    {
        CompareButton.IsEnabled = CountCheckedSnapshotCards() == 2;
    }

    private int CountCheckedSnapshotCards()
    {
        return FindVisualChildren<ToggleButton>(this).Count(button => button.IsChecked == true);
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                yield return typedChild;
            }

            foreach (var descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }
}
