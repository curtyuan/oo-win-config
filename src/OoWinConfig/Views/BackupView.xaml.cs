using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using OoWinConfig.Models;
using OoWinConfig.ViewModels;

namespace OoWinConfig.Views;

public partial class BackupView : UserControl
{
    private bool _isSnapshotMapMode;
    private bool _isTrashMapMode;
    private string _trashScope = "general";
    private string _snapshotTagFilter = "all";
    private readonly HashSet<string> _deletedSnapshotKeys = [];
    private bool _isSnapshotSidebarVisible = true;
    private Func<string, IReadOnlyList<BackupSnapshotRow>> _loadSnapshots = _ => [];
    private Func<string, string, IReadOnlyList<BackupSnapshotRow>> _loadTrashSnapshots = (_, _) => [];

    public BackupView()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler? CreateBackupRequested;
    public event RoutedEventHandler? BackupProfilesRequested;
    public event RoutedEventHandler? CompareToStoredConfigRequested;
    public event Action<ConfigItemRow>? RestoreConfigItemRequested;
    public event RoutedEventHandler? ProfileBackupSelectionChanged;
    public event RoutedEventHandler? SelectAllProfilesRequested;
    public event RoutedEventHandler? ClearAllProfilesRequested;
    public event RoutedEventHandler? SnapshotCompareRequested;
    public event Action<IReadOnlyList<string>>? DeleteSnapshotsRequested;
    public event SelectionChangedEventHandler? ProfileSelectionChanged;

    public void SetSnapshotProvider(Func<string, IReadOnlyList<BackupSnapshotRow>> loadSnapshots)
    {
        _loadSnapshots = loadSnapshots;
    }

    public void SetTrashSnapshotProvider(Func<string, string, IReadOnlyList<BackupSnapshotRow>> loadTrashSnapshots)
    {
        _loadTrashSnapshots = loadTrashSnapshots;
    }

    private void ProfilesList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSnapshotMapMode)
        {
            return;
        }

        ProfileSelectionChanged?.Invoke(sender, e);
    }

    private void ProfileBackupSelection_OnChanged(object sender, RoutedEventArgs e) => ProfileBackupSelectionChanged?.Invoke(sender, e);
    private void SelectAll_OnClick(object sender, RoutedEventArgs e)
    {
        if (_isSnapshotMapMode)
        {
            foreach (var row in GetProfileRows())
            {
                row.IsSelectedForSnapshotMap = true;
            }

            UpdateSnapshotMapRows();
            return;
        }

        SelectAllProfilesRequested?.Invoke(sender, e);
    }

    private void ClearAll_OnClick(object sender, RoutedEventArgs e)
    {
        if (_isSnapshotMapMode)
        {
            foreach (var row in GetProfileRows())
            {
                row.IsSelectedForSnapshotMap = false;
            }

            UpdateSnapshotMapRows();
            return;
        }

        ClearAllProfilesRequested?.Invoke(sender, e);
    }
    private void BackupProfiles_OnClick(object sender, RoutedEventArgs e) => BackupProfilesRequested?.Invoke(sender, e);
    private void CreateBackup_OnClick(object sender, RoutedEventArgs e) => CreateBackupRequested?.Invoke(sender, e);
    private void CompareToStoredConfig_OnClick(object sender, RoutedEventArgs e) => CompareToStoredConfigRequested?.Invoke(sender, e);
    private void SnapshotCompare_OnClick(object sender, RoutedEventArgs e) => SnapshotCompareRequested?.Invoke(sender, e);

    private void RestoreConfigItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ConfigItemRow row })
        {
            RestoreConfigItemRequested?.Invoke(row);
        }
    }

    private void DeleteSnapshot_OnClick(object sender, RoutedEventArgs e)
    {
        var selectedSnapshots = GetSelectedSnapshotCards();
        DeleteSnapshotsRequested?.Invoke(selectedSnapshots
            .Where(snapshot => !string.IsNullOrWhiteSpace(snapshot.ManifestPath))
            .Select(snapshot => snapshot.ManifestPath)
            .ToList());

        foreach (var snapshot in selectedSnapshots)
        {
            _deletedSnapshotKeys.Add(snapshot.Key);
        }

        UpdateSnapshotMapRows();
    }

    private void OpenSnapshotInExplorer_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: SnapshotCard snapshot } || snapshot.IsPlaceholder)
        {
            return;
        }

        var snapshotDirectory = !string.IsNullOrWhiteSpace(snapshot.SnapshotDirectory)
            ? snapshot.SnapshotDirectory
            : Path.Combine(AppContext.BaseDirectory, "data", "backups", snapshot.ProfileId);
        if (!Directory.Exists(snapshotDirectory))
        {
            snapshotDirectory = Path.Combine(AppContext.BaseDirectory, "data", "backups");
        }

        Directory.CreateDirectory(snapshotDirectory);
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{snapshotDirectory}\"",
            UseShellExecute = true
        });
    }

    private void SnapshotSidebarToggle_OnClick(object sender, RoutedEventArgs e)
    {
        ToggleSnapshotSidebar();
    }

    public bool TryToggleSnapshotSidebar()
    {
        if (!_isSnapshotMapMode)
        {
            return false;
        }

        ToggleSnapshotSidebar();
        return true;
    }

    private void ToggleSnapshotSidebar()
    {
        _isSnapshotSidebarVisible = !_isSnapshotSidebarVisible;
        SnapshotSidebarPanel.Visibility = _isSnapshotSidebarVisible ? Visibility.Visible : Visibility.Collapsed;
        SnapshotSidebarCollapsedToggleButton.Visibility = _isSnapshotSidebarVisible ? Visibility.Collapsed : Visibility.Visible;
        SnapshotSidebarToggleButton.Content = ">";
        SnapshotSidebarCollapsedToggleButton.Content = "<";
        SnapshotSidebarColumn.Width = _isSnapshotSidebarVisible ? new GridLength(246) : new GridLength(38);

        Dispatcher.BeginInvoke(() => SnapshotTimelineScroll.ScrollToRightEnd(), System.Windows.Threading.DispatcherPriority.Background);
    }

    private void SnapshotTagAll_OnClick(object sender, RoutedEventArgs e) => SetSnapshotTagFilter("all");
    private void SnapshotTagManual_OnClick(object sender, RoutedEventArgs e) => SetSnapshotTagFilter("manual");
    private void SnapshotTagBatch_OnClick(object sender, RoutedEventArgs e) => SetSnapshotTagFilter("batch");

    private void SnapshotMapMode_OnClick(object sender, RoutedEventArgs e)
    {
        if (_isSnapshotMapMode && !_isTrashMapMode)
        {
            SetSnapshotMapMode(false, trashMode: false);
            return;
        }

        SetSnapshotMapMode(true, trashMode: false);
    }

    private void TrashMapMode_OnClick(object sender, RoutedEventArgs e)
    {
        SetSnapshotMapMode(true, trashMode: !_isTrashMapMode);
    }

    private void TrashScopeGeneral_OnClick(object sender, RoutedEventArgs e)
    {
        _trashScope = "general";
        UpdateSnapshotMapRows();
    }

    private void TrashScopeRemote_OnClick(object sender, RoutedEventArgs e)
    {
        _trashScope = "remote";
        UpdateSnapshotMapRows();
    }

    public void ShowSnapshotMapMode()
    {
        SetSnapshotMapMode(true, trashMode: false);
    }

    private void SetSnapshotMapMode(bool enabled, bool trashMode)
    {
        _isSnapshotMapMode = enabled;
        _isTrashMapMode = enabled && trashMode;
        SnapshotMapPanel.Visibility = _isSnapshotMapMode ? Visibility.Visible : Visibility.Collapsed;
        ConfigItemsPanel.Visibility = _isSnapshotMapMode ? Visibility.Collapsed : Visibility.Visible;
        TrashScopePanel.Visibility = _isTrashMapMode ? Visibility.Visible : Visibility.Collapsed;
        SnapshotSidebarColumn.Width = _isSnapshotMapMode ? (_isSnapshotSidebarVisible ? new GridLength(246) : new GridLength(38)) : new GridLength(0);
        SnapshotSidebarPanel.Visibility = _isSnapshotMapMode && _isSnapshotSidebarVisible ? Visibility.Visible : Visibility.Collapsed;
        SnapshotSidebarCollapsedToggleButton.Visibility = _isSnapshotMapMode && !_isSnapshotSidebarVisible ? Visibility.Visible : Visibility.Collapsed;
        BackupProfilesButton.Visibility = _isSnapshotMapMode ? Visibility.Collapsed : Visibility.Visible;
        ProfileBulkActionsPanel.Visibility = Visibility.Visible;
        ProfilesHeaderText.Text = _isSnapshotMapMode ? "Profiles" : "Softwares";
        UpdateModeHeader();

        foreach (var row in GetProfileRows())
        {
            row.IsSnapshotMapMode = _isSnapshotMapMode;
            if (!_isSnapshotMapMode)
            {
                row.IsSelectedForSnapshotMap = false;
            }
        }

        if (_isSnapshotMapMode && !GetProfileRows().Any(row => row.IsSelectedForSnapshotMap) && ProfilesList.SelectedItem is SoftwareProfileRow selectedRow)
        {
            selectedRow.IsSelectedForSnapshotMap = true;
        }

        if (_isSnapshotMapMode)
        {
            ProfilesList.SelectedItem = null;
        }

        UpdateSnapshotMapRows();
    }

    private void UpdateModeHeader()
    {
        if (!_isSnapshotMapMode)
        {
            SnapshotMapModeButton.Content = "Snapshot map";
            TrashMapModeButton.Content = "Trash";
            ConfigHeaderText.Text = "Selected profile config items";
            return;
        }

        SnapshotMapModeButton.Content = "Config items";
        TrashMapModeButton.Content = _isTrashMapMode ? "Snapshot map" : "Trash";
        ConfigHeaderText.Text = _isTrashMapMode ? "Trash map" : "Snapshot map";
    }

    private void ProfilesListItem_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isSnapshotMapMode || sender is not ListBoxItem { DataContext: SoftwareProfileRow row })
        {
            return;
        }

        row.IsSelectedForSnapshotMap = !row.IsSelectedForSnapshotMap;
        ProfilesList.SelectedItem = null;
        UpdateSnapshotMapRows();
        e.Handled = true;
    }

    private void UpdateSnapshotMapRows()
    {
        var rows = GetProfileRows()
            .Where(row => row.IsSelectedForSnapshotMap)
            .Select(row => SnapshotMapRow.FromProfile(row, LoadRowsForMode(row.Id), _snapshotTagFilter, _deletedSnapshotKeys))
            .ToList();

        var dateKeys = rows
            .SelectMany(row => row.Snapshots)
            .Select(snapshot => snapshot.Date)
            .Distinct()
            .Order()
            .ToList();

        foreach (var row in rows)
        {
            row.AlignTo(dateKeys);
        }

        var hasSelectedProfiles = rows.Count > 0;
        var hasSnapshots = dateKeys.Count > 0;
        SnapshotEmptyText.Visibility = hasSelectedProfiles && !hasSnapshots ? Visibility.Visible : Visibility.Collapsed;
        SnapshotTimelineScroll.Visibility = hasSnapshots ? Visibility.Visible : Visibility.Collapsed;

        SnapshotDateColumnsItems.ItemsSource = CreateDateColumns(dateKeys);
        SnapshotRowsItems.ItemsSource = rows;
        SnapshotRowHeadersItems.ItemsSource = rows;
        UpdateSnapshotActionButtons();

        Dispatcher.BeginInvoke(() => SnapshotTimelineScroll.ScrollToRightEnd(), System.Windows.Threading.DispatcherPriority.Background);
    }

    private IReadOnlyList<BackupSnapshotRow> LoadRowsForMode(string profileId)
    {
        return _isTrashMapMode
            ? _loadTrashSnapshots(_trashScope, profileId)
            : _loadSnapshots(profileId);
    }

    private void SnapshotTimelineScroll_OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (Math.Abs(e.VerticalChange) > 0)
        {
            SnapshotRowHeaderScroll.ScrollToVerticalOffset(e.VerticalOffset);
        }
    }

    private void SetSnapshotTagFilter(string filter)
    {
        _snapshotTagFilter = filter;
        SnapshotTagFilterText.Text = $"Showing: {GetSnapshotTagFilterLabel(filter)}";
        UpdateSnapshotMapRows();
    }

    private static string GetSnapshotTagFilterLabel(string filter)
    {
        return filter switch
        {
            "manual" => "Manual",
            "batch" => "Batch",
            _ => "All"
        };
    }

    private static List<SnapshotDateColumn> CreateDateColumns(IReadOnlyList<string> dateKeys)
    {
        var columns = new List<SnapshotDateColumn>();
        foreach (var dateKey in dateKeys)
        {
            var date = DateOnly.Parse(dateKey);
            var year = date.Year.ToString();
            var month = $"{date.Month}月";
            columns.Add(new SnapshotDateColumn
            {
                Date = dateKey,
                YearLabel = year,
                MonthLabel = month
            });
        }

        return columns;
    }

    private IEnumerable<SoftwareProfileRow> GetProfileRows()
    {
        return ProfilesList.ItemsSource?.Cast<SoftwareProfileRow>() ?? [];
    }

    private void SnapshotCompareCard_OnChanged(object sender, RoutedEventArgs e)
    {
        UpdateSnapshotActionButtons();
    }

    private void UpdateSnapshotActionButtons()
    {
        var selectedCount = CountCheckedSnapshotCards();
        SnapshotCompareButton.IsEnabled = selectedCount == 2;
        DeleteSnapshotButton.IsEnabled = selectedCount > 0;
    }

    private List<SnapshotCard> GetSelectedSnapshotCards()
    {
        return FindVisualChildren<ToggleButton>(SnapshotMapPanel)
            .Where(button => button.IsChecked == true)
            .Select(button => button.DataContext)
            .OfType<SnapshotCard>()
            .ToList();
    }

    private int CountCheckedSnapshotCards()
    {
        return FindVisualChildren<ToggleButton>(SnapshotMapPanel).Count(button => button.IsChecked == true);
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

    private sealed class SnapshotMapRow
    {
        public required string ProfileName { get; init; }
        public required string ProfileId { get; init; }
        public required List<SnapshotCard> Snapshots { get; set; }

        public void AlignTo(IReadOnlyList<string> dateKeys)
        {
            var byDate = Snapshots.ToDictionary(snapshot => snapshot.Date, StringComparer.OrdinalIgnoreCase);
            Snapshots = dateKeys
                .Select(date => byDate.TryGetValue(date, out var snapshot)
                    ? snapshot
                    : SnapshotCard.CreatePlaceholder(ProfileId, date))
                .ToList();
        }

        public static SnapshotMapRow FromProfile(SoftwareProfileRow profile, IReadOnlyList<BackupSnapshotRow> backupRows, string tagFilter, HashSet<string> deletedSnapshotKeys)
        {
            var snapshots = backupRows
                .Select(row => SnapshotCard.FromBackup(profile.Id, row))
                .Where(snapshot => MatchesTagFilter(snapshot, tagFilter))
                .Where(snapshot => !deletedSnapshotKeys.Contains(snapshot.Key))
                .OrderBy(snapshot => snapshot.Date)
                .ToList();

            return new SnapshotMapRow
            {
                ProfileName = profile.Name,
                ProfileId = profile.Id,
                Snapshots = snapshots
            };
        }

        private static bool MatchesTagFilter(SnapshotCard snapshot, string tagFilter)
        {
            return tagFilter == "all" || string.Equals(snapshot.Tag, tagFilter, StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class SnapshotCard
    {
        public string ProfileId { get; set; } = string.Empty;
        public required string Name { get; init; }
        public required string Date { get; init; }
        public required string Tag { get; init; }
        public string ManifestPath { get; init; } = string.Empty;
        public string SnapshotDirectory { get; init; } = string.Empty;
        public bool IsSelected { get; set; }
        public bool IsPlaceholder { get; init; }
        public bool IsSnapshot => !IsPlaceholder;
        public string ShortDate => IsPlaceholder ? string.Empty : DateOnly.Parse(Date).ToString("M/d");
        public string Key => $"{ProfileId}|{Date}|{Tag}";

        public static SnapshotCard FromBackup(string profileId, BackupSnapshotRow row)
        {
            var createdAt = row.Manifest.CreatedAtUtc.ToLocalTime();
            return new SnapshotCard
            {
                ProfileId = profileId,
                Name = string.IsNullOrWhiteSpace(row.Manifest.Label) ? row.Manifest.SnapshotId : row.Manifest.Label,
                Date = DateOnly.FromDateTime(createdAt).ToString("yyyy-MM-dd"),
                Tag = string.IsNullOrWhiteSpace(row.Manifest.Category) ? "manual" : row.Manifest.Category,
                ManifestPath = row.ManifestPath,
                SnapshotDirectory = Path.GetDirectoryName(row.ManifestPath) ?? string.Empty
            };
        }

        public static SnapshotCard CreatePlaceholder(string profileId, string date)
        {
            return new SnapshotCard
            {
                ProfileId = profileId,
                Name = "....",
                Date = date,
                Tag = string.Empty,
                IsPlaceholder = true
            };
        }
    }

    private sealed class SnapshotDateColumn
    {
        public required string Date { get; init; }
        public required string YearLabel { get; init; }
        public required string MonthLabel { get; init; }
    }
}
