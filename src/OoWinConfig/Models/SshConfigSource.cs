namespace OoWinConfig.Models;

public sealed class SshConfigSource
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;

    public string DisplayName => string.IsNullOrWhiteSpace(Label) ? Path : $"{Label} - {Path}";

    public override string ToString()
    {
        return DisplayName;
    }
}
