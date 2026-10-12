using OoWinConfig.Models;

namespace OoWinConfig.ViewModels;

public sealed class SoftwareProfileRow : ViewModelBase
{
    private bool _isSelectedForBackup;
    private bool _isSelectedForSnapshotMap;
    private bool _isSnapshotMapMode;

    public required SoftwareProfile Profile { get; init; }
    public bool IsSelectedForBackup
    {
        get => _isSelectedForBackup;
        set => SetProperty(ref _isSelectedForBackup, value);
    }

    public bool IsSelectedForSnapshotMap
    {
        get => _isSelectedForSnapshotMap;
        set => SetProperty(ref _isSelectedForSnapshotMap, value);
    }

    public bool IsSnapshotMapMode
    {
        get => _isSnapshotMapMode;
        set => SetProperty(ref _isSnapshotMapMode, value);
    }

    public string Id => Profile.Id;
    public string Name => Profile.Name;
    public string Type => Profile.Type;
}
