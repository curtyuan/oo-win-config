using System.IO;
using System.Text.Json;
using OoWinConfig.Models;

namespace OoWinConfig.Services;

public sealed class UiSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _filePath;

    public UiSettingsService(string dataDirectory)
    {
        _filePath = Path.Combine(dataDirectory, "settings", "ui-settings.json");
    }

    public UiSettings Load()
    {
        if (!File.Exists(_filePath))
        {
            return new UiSettings();
        }

        using var stream = File.OpenRead(_filePath);
        return JsonSerializer.Deserialize<UiSettings>(stream, JsonOptions) ?? new UiSettings();
    }

    public void Save(UiSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        using var stream = File.Create(_filePath);
        JsonSerializer.Serialize(stream, settings, JsonOptions);
    }
}
