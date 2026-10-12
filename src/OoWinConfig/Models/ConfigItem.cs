namespace OoWinConfig.Models;

public sealed class ConfigItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string DefaultSourcePath { get; set; } = string.Empty;
    public string[] CandidateSourcePaths { get; set; } = [];
    public string BackupPath { get; set; } = string.Empty;
    public bool Required { get; set; }
    public string? Description { get; set; }
}
