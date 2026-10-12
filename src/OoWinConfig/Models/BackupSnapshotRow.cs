namespace OoWinConfig.Models;

public sealed class BackupSnapshotRow
{
    public required BackupManifest Manifest { get; init; }
    public required string ManifestPath { get; init; }

    public string DisplayName => $"{Manifest.CreatedAtUtc.ToLocalTime():yyyy-MM-dd HH:mm} - {Manifest.Label}";
    public string Category => Manifest.Category;
    public string Summary => $"{Manifest.FileCount} files / {FormatSize(Manifest.TotalSize)}";

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        var kb = bytes / 1024d;
        if (kb < 1024)
        {
            return $"{kb:0.#} KB";
        }

        return $"{kb / 1024d:0.#} MB";
    }
}
