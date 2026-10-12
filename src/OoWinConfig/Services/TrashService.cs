using System.IO;
using System.Text.Json;
using OoWinConfig.Models;

namespace OoWinConfig.Services;

public sealed class TrashService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _trashDirectory;

    public TrashService(string dataDirectory)
    {
        _trashDirectory = Path.Combine(dataDirectory, "trash");
    }

    public void MoveToTrash(string manifestPath)
    {
        var snapshotDirectory = Path.GetDirectoryName(manifestPath);
        if (string.IsNullOrWhiteSpace(snapshotDirectory) || !Directory.Exists(snapshotDirectory))
        {
            return;
        }

        using var stream = File.OpenRead(manifestPath);
        var manifest = JsonSerializer.Deserialize<BackupManifest>(stream, JsonOptions);
        if (manifest is null)
        {
            return;
        }

        var destinationDirectory = Path.Combine(_trashDirectory, "general", manifest.SoftwareId, manifest.SnapshotId);
        destinationDirectory = GetAvailableDirectory(destinationDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationDirectory)!);
        Directory.Move(snapshotDirectory, destinationDirectory);
    }

    public IReadOnlyList<BackupSnapshotRow> LoadTrash(string scope, string softwareId)
    {
        var profileTrashDirectory = Path.Combine(_trashDirectory, scope, softwareId);
        if (!Directory.Exists(profileTrashDirectory))
        {
            return [];
        }

        var rows = new List<BackupSnapshotRow>();
        foreach (var manifestPath in Directory.EnumerateFiles(profileTrashDirectory, "manifest.json", SearchOption.AllDirectories))
        {
            using var stream = File.OpenRead(manifestPath);
            var manifest = JsonSerializer.Deserialize<BackupManifest>(stream, JsonOptions);
            if (manifest is not null)
            {
                rows.Add(new BackupSnapshotRow { Manifest = manifest, ManifestPath = manifestPath });
            }
        }

        return rows.OrderByDescending(row => row.Manifest.CreatedAtUtc).ToList();
    }

    private static string GetAvailableDirectory(string destinationDirectory)
    {
        if (!Directory.Exists(destinationDirectory))
        {
            return destinationDirectory;
        }

        var suffix = 1;
        while (Directory.Exists($"{destinationDirectory}-{suffix}"))
        {
            suffix++;
        }

        return $"{destinationDirectory}-{suffix}";
    }
}
