using System.IO;
using System.Text.Json;
using OoWinConfig.Models;

namespace OoWinConfig.Services;

public sealed class ProfileLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public IReadOnlyList<SoftwareProfile> LoadProfiles(string profilesDirectory)
    {
        if (!Directory.Exists(profilesDirectory))
        {
            return [];
        }

        var profiles = new List<SoftwareProfile>();

        foreach (var file in Directory.EnumerateFiles(profilesDirectory, "*.json").OrderBy(Path.GetFileName))
        {
            using var stream = File.OpenRead(file);
            var profile = JsonSerializer.Deserialize<SoftwareProfile>(stream, JsonOptions);
            if (profile is not null)
            {
                profiles.Add(profile);
            }
        }

        return profiles;
    }
}
