namespace OoWinConfig.Models;

public sealed class SoftwareProfile
{
    public int SchemaVersion { get; set; }
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "software";
    public ConfigItem[] ConfigItems { get; set; } = [];
}
