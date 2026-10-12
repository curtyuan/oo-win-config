using System.IO;
using System.Text.Json;

namespace OoWinConfig.Services;

public sealed class SourcePathOverrideService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;
    private Dictionary<string, string> _overrides = new(StringComparer.OrdinalIgnoreCase);

    public SourcePathOverrideService(string dataDirectory)
    {
        _filePath = Path.Combine(dataDirectory, "settings", "source-path-overrides.json");
        Load();
    }

    public string GetSourcePath(string profileId, string configItemId, string defaultSourcePath)
    {
        return _overrides.TryGetValue(GetKey(profileId, configItemId), out var value)
            ? value
            : defaultSourcePath;
    }

    public void SetSourcePath(string profileId, string configItemId, string sourcePath)
    {
        _overrides[GetKey(profileId, configItemId)] = sourcePath;
        Save();
    }

    public void RestoreDefault(string profileId, string configItemId)
    {
        if (_overrides.Remove(GetKey(profileId, configItemId)))
        {
            Save();
        }
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
        {
            return;
        }

        using var stream = File.OpenRead(_filePath);
        _overrides = JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? new(StringComparer.OrdinalIgnoreCase);
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        using var stream = File.Create(_filePath);
        JsonSerializer.Serialize(stream, _overrides, JsonOptions);
    }

    private static string GetKey(string profileId, string configItemId) => $"{profileId}:{configItemId}";
}
