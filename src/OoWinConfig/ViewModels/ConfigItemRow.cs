using System.IO;
using OoWinConfig.Models;
using OoWinConfig.Services;

namespace OoWinConfig.ViewModels;

public sealed class ConfigItemRow : ViewModelBase
{
    private bool _isSelected = true;
    private readonly PathTokenResolver _pathTokenResolver;

    public ConfigItemRow(string profileId, ConfigItem item, PathTokenResolver pathTokenResolver, SourcePathOverrideService sourcePathOverrideService)
    {
        ProfileId = profileId;
        Item = item;
        _pathTokenResolver = pathTokenResolver;
        SourcePathOverrideService = sourcePathOverrideService;
    }

    public string ProfileId { get; }
    public ConfigItem Item { get; }
    public SourcePathOverrideService SourcePathOverrideService { get; }
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string Name => Item.Name;
    public string Kind => Item.Kind;
    public string SourcePath => _pathTokenResolver.Resolve(SourcePathOverrideService.GetSourcePath(ProfileId, Item.Id, Item.DefaultSourcePath));
    public string BackupPath => Item.BackupPath;
    public string Required => Item.Required ? "Core" : "Optional";
    public string Status => Exists() ? "Found" : "Missing";
    public string StatusIcon => Exists() ? "✓" : "✕";
    public bool IsFound => Exists();
    public string RestoreActionLabel => Exists() ? "Replace current config" : "Place config";
    public string RequiredLabel => Item.Required ? "Core" : "Optional";

    private bool Exists()
    {
        return Item.Kind.Equals("directory", StringComparison.OrdinalIgnoreCase)
            ? Directory.Exists(SourcePath)
            : File.Exists(SourcePath);
    }
}
