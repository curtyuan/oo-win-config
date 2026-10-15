using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using OoWinConfig.Models;
using OoWinConfig.Services;
using OoWinConfig.ViewModels;

namespace OoWinConfig;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly PathTokenResolver _pathTokenResolver = new();
    private readonly string _dataDirectory = Path.Combine(AppContext.BaseDirectory, "data");
    private readonly SourcePathOverrideService _sourcePathOverrideService;
    private readonly BackupService _backupService;
    private readonly TrashService _trashService;
    private readonly UiSettingsService _uiSettingsService;
    private readonly ScreenshotService _screenshotService;
    private List<SoftwareProfileRow> _profileRows = [];
    private bool _isLoadingUiSettings;
    private bool _isCompareScrollLinked = true;

    public MainWindow()
    {
        _sourcePathOverrideService = new SourcePathOverrideService(_dataDirectory);
        _backupService = new BackupService(_dataDirectory, _pathTokenResolver, _sourcePathOverrideService);
        _trashService = new TrashService(_dataDirectory);
        _uiSettingsService = new UiSettingsService(_dataDirectory);
        _screenshotService = new ScreenshotService(_dataDirectory);
        InitializeComponent();
        SshConfigView.Initialize(_dataDirectory);
        WireViewEvents();
        LoadUiSettings();
        LoadProfiles();
    }

    private void WireViewEvents()
    {
        BackupView.ProfileSelectionChanged += ProfilesList_OnSelectionChanged;
        BackupView.ProfileBackupSelectionChanged += ProfileBackupSelection_OnChanged;
        BackupView.SelectAllProfilesRequested += SelectAllProfiles_OnClick;
        BackupView.ClearAllProfilesRequested += ClearAllProfiles_OnClick;
        BackupView.BackupProfilesRequested += BackupProfiles_OnClick;
        BackupView.CreateBackupRequested += CreateBackup_OnClick;
        BackupView.CompareToStoredConfigRequested += CompareToStoredConfig_OnClick;
        BackupView.RestoreConfigItemRequested += RestoreConfigItem_OnRequested;
        BackupView.SnapshotCompareRequested += SnapshotMapCompare_OnClick;
        BackupView.DeleteSnapshotsRequested += DeleteSnapshots_OnRequested;
        BackupView.SetSnapshotProvider(softwareId => _backupService.LoadBackups(softwareId));
        BackupView.SetTrashSnapshotProvider((scope, softwareId) => _trashService.LoadTrash(scope, softwareId));
        RemoteView.OpenSshConfigRequested += (_, _) => SelectSshConfigTab();
        SshConfigView.StatusChanged += (_, message) => StatusText.Text = message;
    }

    private void DeleteSnapshots_OnRequested(IReadOnlyList<string> manifestPaths)
    {
        foreach (var manifestPath in manifestPaths)
        {
            _trashService.MoveToTrash(manifestPath);
        }

        StatusText.Text = $"Moved {manifestPaths.Count} snapshot(s) to trash.";
    }

    private void LoadProfiles()
    {
        var profilesDirectory = Path.Combine(_dataDirectory, "profiles");
        var profiles = new ProfileLoader().LoadProfiles(profilesDirectory);
        _profileRows = profiles.Select(profile => new SoftwareProfileRow { Profile = profile }).ToList();

        BackupView.ProfilesList.ItemsSource = _profileRows;
        StatusText.Text = $"Loaded {profiles.Count} profiles from {profilesDirectory}";

        if (_profileRows.Count > 0)
        {
            BackupView.ProfilesList.SelectedIndex = 0;
        }
    }

    private void ProfilesList_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (BackupView.ProfilesList.SelectedItem is not SoftwareProfileRow row)
        {
            BackupView.ConfigItemsList.ItemsSource = null;
            return;
        }

        var profile = row.Profile;
        BackupView.SetSelectedProfileHeader(profile.Name);
        BackupView.ConfigItemsList.ItemsSource = profile.ConfigItems
            .Select(item => new ConfigItemRow(profile.Id, item, _pathTokenResolver, _sourcePathOverrideService))
            .ToList();

    }

    private void CreateBackup_OnClick(object sender, RoutedEventArgs e)
    {
        if (BackupView.ProfilesList.SelectedItem is not SoftwareProfileRow row)
        {
            return;
        }

        var profile = row.Profile;

        var rows = BackupView.ConfigItemsList.ItemsSource?.Cast<ConfigItemRow>().ToList() ?? [];
        var selectedItems = rows
            .Where(row => row.IsSelected)
            .Select(row => row.Item)
            .ToList();

        if (selectedItems.Count == 0)
        {
            MessageBox.Show(this, "Select at least one config item.", "Create backup", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var label = $"manual-{DateTime.Now:yyyyMMdd-HHmmss}";
        var manifest = _backupService.CreateBackup(profile, selectedItems, label, comment: string.Empty);
        StatusText.Text = $"Created backup {manifest.SnapshotId} for {profile.Name}: {manifest.FileCount} files.";
    }

    private void ProfileBackupSelection_OnChanged(object sender, RoutedEventArgs e)
    {
        UpdateBackupProfilesButton();
    }

    private void SelectAllProfiles_OnClick(object sender, RoutedEventArgs e)
    {
        foreach (var row in GetProfileRows())
        {
            row.IsSelectedForBackup = true;
        }

        BackupView.ProfilesList.Items.Refresh();
        UpdateBackupProfilesButton();
    }

    private void ClearAllProfiles_OnClick(object sender, RoutedEventArgs e)
    {
        foreach (var row in GetProfileRows())
        {
            row.IsSelectedForBackup = false;
        }

        BackupView.ProfilesList.Items.Refresh();
        UpdateBackupProfilesButton();
    }

    private void BackupProfiles_OnClick(object sender, RoutedEventArgs e)
    {
        var selectedRows = GetProfileRows()
            .Where(row => row.IsSelectedForBackup)
            .ToList();

        if (selectedRows.Count == 0)
        {
            return;
        }

        var createdCount = 0;
        foreach (var selectedRow in selectedRows)
        {
            var profile = selectedRow.Profile;
            var label = $"batch-{DateTime.Now:yyyyMMdd-HHmmss}";
            _backupService.CreateBackup(profile, profile.ConfigItems, label, comment: string.Empty, category: "batch");
            createdCount++;
        }

        StatusText.Text = $"Created backups for {createdCount} profiles.";
    }

    private void RestoreConfigItem_OnRequested(ConfigItemRow itemRow)
    {
        var profileRow = GetProfileRows().FirstOrDefault(row => row.Id == itemRow.ProfileId);
        if (profileRow is null)
        {
            return;
        }

        var profile = profileRow.Profile;
        var latestBackup = _backupService.FindLatestBackupWithItem(profile.Id, itemRow.Item.Id);
        if (latestBackup is null)
        {
            var createBackup = MessageBox.Show(
                this,
                $"No backup contains '{itemRow.Name}'. Create a manual backup from the current config now?",
                "No backup available",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (createBackup == MessageBoxResult.Yes)
            {
                var label = $"manual-{DateTime.Now:yyyyMMdd-HHmmss}";
                var manifest = _backupService.CreateBackup(profile, [itemRow.Item], label, comment: string.Empty, category: "manual");
                StatusText.Text = $"Created manual backup {manifest.SnapshotId} for {profile.Name}: {manifest.FileCount} files.";
            }

            return;
        }

        var action = itemRow.IsFound ? "replace the current config with" : "place config from";
        var result = MessageBox.Show(
            this,
            $"This will {action} the latest backup:\n\n{latestBackup.DisplayName}\n\nCreate a manual backup of the current config before restoring?\n\nYes = back up then restore\nNo = restore without backup\nCancel = abort",
            itemRow.IsFound ? "Replace current config" : "Place config",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Cancel)
        {
            return;
        }

        if (result == MessageBoxResult.Yes)
        {
            var label = $"manual-{DateTime.Now:yyyyMMdd-HHmmss}";
            _backupService.CreateBackup(profile, [itemRow.Item], label, comment: "Before restore", category: "manual");
        }

        var restored = _backupService.RestoreConfigItem(profile, itemRow.Item, latestBackup);
        StatusText.Text = restored
            ? $"Restored {itemRow.Name} from {latestBackup.DisplayName}."
            : $"Could not restore {itemRow.Name}; backup content was missing.";
        BackupView.ConfigItemsList.Items.Refresh();
    }

    private void UpdateBackupProfilesButton()
    {
        BackupView.BackupProfilesButton.IsEnabled = GetProfileRows().Any(row => row.IsSelectedForBackup);
    }

    private IEnumerable<SoftwareProfileRow> GetProfileRows()
    {
        return BackupView.ProfilesList.ItemsSource?.Cast<SoftwareProfileRow>() ?? [];
    }

    private void BackupTab_OnClick(object sender, RoutedEventArgs e)
    {
        MainTabs.SelectedIndex = 0;
        SyncHeaderTabs();
    }

    private void RemoteTab_OnClick(object sender, RoutedEventArgs e)
    {
        MainTabs.SelectedIndex = 1;
        SyncHeaderTabs();
    }

    private void SshConfigTab_OnClick(object sender, RoutedEventArgs e)
    {
        SelectSshConfigTab();
    }

    private void SelectSshConfigTab()
    {
        MainTabs.SelectedIndex = 2;
        SyncHeaderTabs();
    }

    private void MainTabs_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        SyncHeaderTabs();
    }

    private void SyncHeaderTabs()
    {
        if (!IsInitialized)
        {
            return;
        }

        BackupNavButton.IsChecked = MainTabs.SelectedIndex == 0;
        RemoteNavButton.IsChecked = MainTabs.SelectedIndex == 1;
        SshConfigNavButton.IsChecked = MainTabs.SelectedIndex == 2;
    }

    private void CompareToStoredConfig_OnClick(object sender, RoutedEventArgs e)
    {
        CompareOverlayTitle.Text = "Compare to stored config";
        CompareOverlay.Visibility = Visibility.Visible;
    }

    private void SnapshotMapCompare_OnClick(object sender, RoutedEventArgs e)
    {
        CompareOverlayTitle.Text = "Compare selected snapshots";
        CompareOverlay.Visibility = Visibility.Visible;
    }

    private void CloseCompare_OnClick(object sender, RoutedEventArgs e)
    {
        CompareOverlay.Visibility = Visibility.Collapsed;
    }

    private void LinkScroll_OnClick(object sender, RoutedEventArgs e)
    {
        _isCompareScrollLinked = !_isCompareScrollLinked;
        LinkScrollButton.Content = _isCompareScrollLinked ? "Unlink scroll" : "Link scroll";
    }

    private void Window_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.OemComma && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            OpenSettingsOverlay();
            e.Handled = true;
        }

        if (e.Key == Key.R && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            e.Handled = BackupView.TryToggleSnapshotSidebar();
        }
    }

    private void Window_OnSourceInitialized(object? sender, EventArgs e)
    {
        WindowChromeService.TryEnableDarkTitleBar(this);
    }

    private void Settings_OnClick(object sender, RoutedEventArgs e)
    {
        OpenSettingsOverlay();
    }

    private void CloseSettings_OnClick(object sender, RoutedEventArgs e)
    {
        SettingsOverlay.Visibility = Visibility.Collapsed;
    }

    private void OpenSettingsOverlay()
    {
        SettingsOverlay.Visibility = Visibility.Visible;
    }

    private void UiScaleSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsInitialized)
        {
            return;
        }

        UiScaleTransform.ScaleX = UiScaleSlider.Value;
        UiScaleTransform.ScaleY = UiScaleSlider.Value;
        UiScaleValueText.Text = $"{UiScaleSlider.Value:P0}";

        if (!_isLoadingUiSettings)
        {
            SaveUiSettings();
        }
    }

    private void FontSizeSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsInitialized)
        {
            return;
        }

        ScaledRoot.SetValue(System.Windows.Controls.Control.FontSizeProperty, FontSizeSlider.Value);
        FontSizeValueText.Text = $"{FontSizeSlider.Value:0}px";

        if (!_isLoadingUiSettings)
        {
            SaveUiSettings();
        }
    }

    private void ResetUiSettings_OnClick(object sender, RoutedEventArgs e)
    {
        UiScaleSlider.Value = 1;
        FontSizeSlider.Value = 14;
        SelectBackgroundTheme("dark");
        SaveUiSettings();
    }

    public string CaptureUiScreenshot()
    {
        var filePath = _screenshotService.Capture(ScaledRoot);
        StatusText.Text = $"Captured UI screenshot: {filePath}";
        return filePath;
    }

    public void ApplyAutomationView(string? viewName)
    {
        if (string.Equals(viewName, "snapshot-map", StringComparison.OrdinalIgnoreCase))
        {
            MainTabs.SelectedIndex = 0;
            BackupView.ShowSnapshotMapMode();
            SyncHeaderTabs();
        }

        if (string.Equals(viewName, "ssh-config", StringComparison.OrdinalIgnoreCase))
        {
            MainTabs.SelectedIndex = 2;
            SyncHeaderTabs();
        }

        if (string.Equals(viewName, "remote", StringComparison.OrdinalIgnoreCase))
        {
            MainTabs.SelectedIndex = 1;
            SyncHeaderTabs();
        }

        if (string.Equals(viewName, "settings", StringComparison.OrdinalIgnoreCase))
        {
            OpenSettingsOverlay();
        }
    }

    private void LoadUiSettings()
    {
        _isLoadingUiSettings = true;
        var settings = _uiSettingsService.Load();
        UiScaleSlider.Value = Clamp(settings.UiScale, UiScaleSlider.Minimum, UiScaleSlider.Maximum);
        FontSizeSlider.Value = Clamp(settings.FontSize, FontSizeSlider.Minimum, FontSizeSlider.Maximum);
        SelectBackgroundTheme(settings.BackgroundTheme);
        _isLoadingUiSettings = false;

        ApplyUiSettings();
    }

    private void ApplyUiSettings()
    {
        UiScaleTransform.ScaleX = UiScaleSlider.Value;
        UiScaleTransform.ScaleY = UiScaleSlider.Value;
        ScaledRoot.SetValue(System.Windows.Controls.Control.FontSizeProperty, FontSizeSlider.Value);
        UiScaleValueText.Text = $"{UiScaleSlider.Value:P0}";
        FontSizeValueText.Text = $"{FontSizeSlider.Value:0}px";
        ApplyBackgroundTheme(GetSelectedBackgroundTheme());
    }

    private void SaveUiSettings()
    {
        _uiSettingsService.Save(new UiSettings
        {
            UiScale = UiScaleSlider.Value,
            FontSize = FontSizeSlider.Value,
            BackgroundTheme = GetSelectedBackgroundTheme()
        });
    }

    private void BackgroundThemeComboBox_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!IsInitialized)
        {
            return;
        }

        ApplyBackgroundTheme(GetSelectedBackgroundTheme());

        if (!_isLoadingUiSettings)
        {
            SaveUiSettings();
        }
    }

    private void SelectBackgroundTheme(string? theme)
    {
        var normalizedTheme = string.Equals(theme, "light", StringComparison.OrdinalIgnoreCase) ? "light" : "dark";
        foreach (var item in BackgroundThemeComboBox.Items.OfType<System.Windows.Controls.ComboBoxItem>())
        {
            item.IsSelected = string.Equals(item.Tag?.ToString(), normalizedTheme, StringComparison.OrdinalIgnoreCase);
        }
    }

    private string GetSelectedBackgroundTheme()
    {
        return BackgroundThemeComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem item
            ? item.Tag?.ToString() ?? "dark"
            : "dark";
    }

    private void ApplyBackgroundTheme(string theme)
    {
        var isLight = string.Equals(theme, "light", StringComparison.OrdinalIgnoreCase);
        var background = new SolidColorBrush(isLight ? Color.FromRgb(238, 241, 245) : Color.FromRgb(15, 19, 25));
        Background = background;
        ScaledRoot.Background = background;
    }

    private static double Clamp(double value, double minimum, double maximum)
    {
        return Math.Min(Math.Max(value, minimum), maximum);
    }
}
