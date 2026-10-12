namespace OoWinConfig.Models;

public sealed class BackupFileEntry
{
    public string ConfigItemId { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string BackupPath { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime LastWriteTimeUtc { get; set; }
    public string Sha256 { get; set; } = string.Empty;
}
