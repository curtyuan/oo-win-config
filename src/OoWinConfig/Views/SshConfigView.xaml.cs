using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using OoWinConfig.Models;
using OoWinConfig.Services;
using OoWinConfig.ViewModels;

namespace OoWinConfig.Views;

public partial class SshConfigView : UserControl, INotifyPropertyChanged
{
    private SshConfigService? _service;
    private SshConfigDocument? _document;
    private SshConfigSource? _selectedSource;
    private SshHostCard? _selectedHost;
    private string _selectedSourcePath = string.Empty;
    private string _includeDirectivesText = "No Include directives found.";
    private string _globalDirectivesText = "No global directives found.";

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<string>? StatusChanged;

    public ObservableCollection<SshConfigSource> Sources { get; } = [];
    public ObservableCollection<SshHostCard> Hosts { get; } = [];

    public SshConfigSource? SelectedSource
    {
        get => _selectedSource;
        set
        {
            if (_selectedSource == value)
            {
                return;
            }

            _selectedSource = value;
            OnPropertyChanged(nameof(SelectedSource));
            LoadSelectedSource();
        }
    }

    public SshHostCard? SelectedHost
    {
        get => _selectedHost;
        set
        {
            if (_selectedHost == value)
            {
                return;
            }

            _selectedHost = value;
            OnPropertyChanged(nameof(SelectedHost));
        }
    }

    public string SelectedSourcePath
    {
        get => _selectedSourcePath;
        set
        {
            _selectedSourcePath = value;
            OnPropertyChanged(nameof(SelectedSourcePath));
        }
    }

    public string IncludeDirectivesText
    {
        get => _includeDirectivesText;
        set
        {
            _includeDirectivesText = value;
            OnPropertyChanged(nameof(IncludeDirectivesText));
        }
    }

    public string GlobalDirectivesText
    {
        get => _globalDirectivesText;
        set
        {
            _globalDirectivesText = value;
            OnPropertyChanged(nameof(GlobalDirectivesText));
        }
    }

    public SshConfigView()
    {
        InitializeComponent();
        DataContext = this;
    }

    public void Initialize(string dataDirectory)
    {
        _service = new SshConfigService(dataDirectory);
        var settings = _service.LoadSourceSettings();
        Sources.Clear();
        foreach (var source in settings.Sources)
        {
            Sources.Add(source);
        }

        SelectedSource = Sources.FirstOrDefault(source => source.Id == settings.SelectedSourceId) ?? Sources.FirstOrDefault();
    }

    private void LoadSelectedSource()
    {
        if (_service is null || SelectedSource is null)
        {
            return;
        }

        _document = _service.LoadDocument(SelectedSource);
        SelectedSourcePath = SelectedSource.Path;
        Hosts.Clear();

        foreach (var block in _document.HostBlocks)
        {
            Hosts.Add(SshHostCard.FromBlock(SelectedSource, block));
        }

        SelectedHost = Hosts.FirstOrDefault();
        IncludeDirectivesText = _document.IncludeLines.Count == 0
            ? "No Include directives found."
            : string.Join(Environment.NewLine, _document.IncludeLines);
        GlobalDirectivesText = _document.GlobalDirectiveLines.Count == 0
            ? "No global directives found."
            : string.Join(Environment.NewLine, _document.GlobalDirectiveLines);
        SaveSourceSelection();
        StatusChanged?.Invoke(this, File.Exists(SshConfigService.ExpandHome(SelectedSource.Path))
            ? $"Loaded {Hosts.Count} SSH host card(s) from {SelectedSource.Path}."
            : $"SSH config source does not exist yet: {SelectedSource.Path}");
    }

    private void SourceComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SaveSourceSelection();
    }

    private void AddSource_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select SSH config source",
            Filter = "SSH config files|config;*.config;*.conf;*.*|All files|*.*",
            CheckFileExists = false
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var label = Path.GetFileName(dialog.FileName);
        if (string.IsNullOrWhiteSpace(label))
        {
            label = "Custom";
        }

        var source = new SshConfigSource
        {
            Id = Guid.NewGuid().ToString("N"),
            Label = label,
            Path = dialog.FileName
        };
        Sources.Add(source);
        SelectedSource = source;
        SaveSourceSelection();
    }

    private void CreateSourceFile_OnClick(object sender, RoutedEventArgs e)
    {
        if (_service is null || SelectedSource is null)
        {
            return;
        }

        _service.EnsureSourceExists(SelectedSource);
        LoadSelectedSource();
    }

    private void Reload_OnClick(object sender, RoutedEventArgs e)
    {
        LoadSelectedSource();
    }

    private void OpenSourceFile_OnClick(object sender, RoutedEventArgs e)
    {
        if (SelectedSource is null)
        {
            return;
        }

        var path = SshConfigService.ExpandHome(SelectedSource.Path);
        if (!File.Exists(path))
        {
            StatusChanged?.Invoke(this, $"Source file does not exist: {SelectedSource.Path}");
            return;
        }

        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    private void NewHost_OnClick(object sender, RoutedEventArgs e)
    {
        if (SelectedSource is null)
        {
            return;
        }

        var card = SshHostCard.CreateNew(SelectedSource.Path);
        Hosts.Add(card);
        SelectedHost = card;
        HostsList.ScrollIntoView(card);
    }

    private void SaveAll_OnClick(object sender, RoutedEventArgs e)
    {
        if (_service is null || _document is null || SelectedSource is null)
        {
            return;
        }

        var sourcePath = SshConfigService.ExpandHome(SelectedSource.Path);
        if (File.Exists(sourcePath)
            && _document.LastWriteTimeUtc != default
            && File.GetLastWriteTimeUtc(sourcePath) != _document.LastWriteTimeUtc)
        {
            var result = MessageBox.Show(
                Window.GetWindow(this),
                "The selected SSH config changed on disk since it was loaded. Save anyway?",
                "SSH config changed",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        _service.SaveDocument(_document, Hosts.ToList());
        LoadSelectedSource();
        StatusChanged?.Invoke(this, $"Saved SSH config source: {SelectedSource.Path}");
    }

    private void EditHost_OnClick(object sender, RoutedEventArgs e)
    {
        SelectHostFromButton(sender);
    }

    private async void TestHost_OnClick(object sender, RoutedEventArgs e)
    {
        if (SelectHostFromButton(sender) is { } host)
        {
            await TestHostAsync(host);
        }
    }

    private async void TestSelectedHost_OnClick(object sender, RoutedEventArgs e)
    {
        if (SelectedHost is not null)
        {
            await TestHostAsync(SelectedHost);
        }
    }

    private void DuplicateHost_OnClick(object sender, RoutedEventArgs e)
    {
        if (SelectHostFromButton(sender) is not { } host)
        {
            return;
        }

        var duplicate = host.Duplicate();
        Hosts.Add(duplicate);
        SelectedHost = duplicate;
        HostsList.ScrollIntoView(duplicate);
    }

    private void ToggleDeleteHost_OnClick(object sender, RoutedEventArgs e)
    {
        if (SelectHostFromButton(sender) is { } host)
        {
            host.IsPendingDelete = !host.IsPendingDelete;
        }
    }

    private void ToggleSelectedDelete_OnClick(object sender, RoutedEventArgs e)
    {
        if (SelectedHost is not null)
        {
            SelectedHost.IsPendingDelete = !SelectedHost.IsPendingDelete;
        }
    }

    private void HostsList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(SelectedHost));
    }

    private SshHostCard? SelectHostFromButton(object sender)
    {
        if (sender is FrameworkElement { DataContext: SshHostCard host })
        {
            SelectedHost = host;
            return host;
        }

        return SelectedHost;
    }

    private async Task TestHostAsync(SshHostCard host)
    {
        if (SelectedSource is null || string.IsNullOrWhiteSpace(host.HostAlias))
        {
            return;
        }

        host.TestStatus = "Testing...";
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ssh",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            process.StartInfo.ArgumentList.Add("-F");
            process.StartInfo.ArgumentList.Add(SshConfigService.ExpandHome(SelectedSource.Path));
            process.StartInfo.ArgumentList.Add("-o");
            process.StartInfo.ArgumentList.Add("BatchMode=yes");
            process.StartInfo.ArgumentList.Add("-o");
            process.StartInfo.ArgumentList.Add("ConnectTimeout=5");
            process.StartInfo.ArgumentList.Add(host.HostAlias);
            process.StartInfo.ArgumentList.Add("exit");

            process.Start();
            var completed = await Task.Run(() => process.WaitForExit(8000));
            if (!completed)
            {
                process.Kill(entireProcessTree: true);
                host.TestStatus = "Test timed out";
                return;
            }

            host.TestStatus = process.ExitCode == 0 ? "Test passed" : "Test failed";
        }
        catch (Exception ex)
        {
            host.TestStatus = "Test error";
            StatusChanged?.Invoke(this, ex.Message);
        }
    }

    private void SaveSourceSelection()
    {
        if (_service is null || SelectedSource is null || Sources.Count == 0)
        {
            return;
        }

        _service.SaveSourceSettings(new SshConfigSourceSettings
        {
            SelectedSourceId = SelectedSource.Id,
            Sources = Sources.ToList()
        });
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
