namespace OoWinConfig.Models;

public sealed class BackupManifest
{
    public int SchemaVersion { get; set; } = 1;
    public string SnapshotId { get; set; } = string.Empty;
    public string SoftwareId { get; set; } = string.Empty;
    public string ProfileName { get; set; } = string.Empty;
    public string Category { get; set; } = "manual";
    public string Label { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public int FileCount { get; set; }
    public long TotalSize { get; set; }
    public List<BackupFileEntry> Files { get; set; } = [];
}
