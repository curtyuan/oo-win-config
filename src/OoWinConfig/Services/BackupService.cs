using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using OoWinConfig.Models;

namespace OoWinConfig.Services;

public sealed class BackupService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly PathTokenResolver _pathTokenResolver;
    private readonly SourcePathOverrideService _sourcePathOverrideService;
    private readonly string _backupsDirectory;

    public BackupService(string dataDirectory, PathTokenResolver pathTokenResolver, SourcePathOverrideService sourcePathOverrideService)
    {
        _pathTokenResolver = pathTokenResolver;
        _sourcePathOverrideService = sourcePathOverrideService;
        _backupsDirectory = Path.Combine(dataDirectory, "backups");
    }

    public BackupManifest CreateBackup(SoftwareProfile profile, IEnumerable<ConfigItem> selectedItems, string label, string comment, string category = "manual")
    {
        var createdAtUtc = DateTime.UtcNow;
        var snapshotId = createdAtUtc.ToLocalTime().ToString("yyyyMMdd-HHmmss");
        var snapshotDirectory = Path.Combine(_backupsDirectory, profile.Id, snapshotId);
        Directory.CreateDirectory(snapshotDirectory);

        var manifest = new BackupManifest
        {
            SnapshotId = snapshotId,
            SoftwareId = profile.Id,
            ProfileName = profile.Name,
            Category = category,
            Label = string.IsNullOrWhiteSpace(label) ? snapshotId : label.Trim(),
            Comment = comment.Trim(),
            CreatedAtUtc = createdAtUtc
        };

        foreach (var item in selectedItems)
        {
            var sourcePath = ResolveSourcePath(profile.Id, item);
            if (item.Kind.Equals("directory", StringComparison.OrdinalIgnoreCase))
            {
                CopyDirectory(item, sourcePath, Path.Combine(snapshotDirectory, item.BackupPath), manifest);
            }
            else
            {
                CopyFile(item, sourcePath, Path.Combine(snapshotDirectory, item.BackupPath), manifest);
            }
        }

        manifest.FileCount = manifest.Files.Count;
        manifest.TotalSize = manifest.Files.Sum(file => file.Size);

        var manifestPath = Path.Combine(snapshotDirectory, "manifest.json");
        using var stream = File.Create(manifestPath);
        JsonSerializer.Serialize(stream, manifest, JsonOptions);

        return manifest;
    }

    public IReadOnlyList<BackupSnapshotRow> LoadBackups(string softwareId)
    {
        var profileBackupsDirectory = Path.Combine(_backupsDirectory, softwareId);
        if (!Directory.Exists(profileBackupsDirectory))
        {
            return [];
        }

        var rows = new List<BackupSnapshotRow>();
        foreach (var manifestPath in Directory.EnumerateFiles(profileBackupsDirectory, "manifest.json", SearchOption.AllDirectories))
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

    public BackupSnapshotRow? FindLatestBackupWithItem(string softwareId, string configItemId)
    {
        return LoadBackups(softwareId)
            .FirstOrDefault(row => row.Manifest.Files.Any(file => string.Equals(file.ConfigItemId, configItemId, StringComparison.OrdinalIgnoreCase)));
    }

    public bool RestoreConfigItem(SoftwareProfile profile, ConfigItem item, BackupSnapshotRow snapshot)
    {
        var snapshotDirectory = Path.GetDirectoryName(snapshot.ManifestPath);
        if (string.IsNullOrWhiteSpace(snapshotDirectory))
        {
            return false;
        }

        var backupPath = Path.Combine(snapshotDirectory, item.BackupPath);
        var destinationPath = ResolveSourcePath(profile.Id, item);

        if (item.Kind.Equals("directory", StringComparison.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(backupPath))
            {
                return false;
            }

            if (Directory.Exists(destinationPath))
            {
                Directory.Delete(destinationPath, recursive: true);
            }

            CopyDirectoryToDestination(backupPath, destinationPath);
            return true;
        }

        if (!File.Exists(backupPath))
        {
            return false;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.Copy(backupPath, destinationPath, overwrite: true);
        return true;
    }

    private string ResolveSourcePath(string profileId, ConfigItem item)
    {
        var path = _sourcePathOverrideService.GetSourcePath(profileId, item.Id, item.DefaultSourcePath);
        return _pathTokenResolver.Resolve(path);
    }

    private static void CopyFile(ConfigItem item, string sourcePath, string destinationPath, BackupManifest manifest)
    {
        if (!File.Exists(sourcePath))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.Copy(sourcePath, destinationPath, overwrite: true);
        AddFileEntry(item, sourcePath, destinationPath, item.BackupPath, manifest);
    }

    private static void CopyDirectory(ConfigItem item, string sourceDirectory, string destinationDirectory, BackupManifest manifest)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            return;
        }

        foreach (var sourceFile in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            var destinationPath = Path.Combine(destinationDirectory, relativePath);
            var backupPath = Path.Combine(item.BackupPath, relativePath).Replace('\\', '/');
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.Copy(sourceFile, destinationPath, overwrite: true);
            AddFileEntry(item, sourceFile, destinationPath, backupPath, manifest);
        }
    }

    private static void CopyDirectoryToDestination(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);
        foreach (var sourceFile in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            var destinationPath = Path.Combine(destinationDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.Copy(sourceFile, destinationPath, overwrite: true);
        }
    }

    private static void AddFileEntry(ConfigItem item, string sourcePath, string destinationPath, string backupPath, BackupManifest manifest)
    {
        var sourceInfo = new FileInfo(sourcePath);
        manifest.Files.Add(new BackupFileEntry
        {
            ConfigItemId = item.Id,
            SourcePath = sourcePath,
            BackupPath = backupPath,
            Size = sourceInfo.Length,
            LastWriteTimeUtc = sourceInfo.LastWriteTimeUtc,
            Sha256 = ComputeSha256(destinationPath)
        });
    }

    private static string ComputeSha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
