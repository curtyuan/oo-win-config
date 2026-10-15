namespace OoWinConfig.Models;

public sealed class SshConfigSourceSettings
{
    public List<SshConfigSource> Sources { get; set; } = [];
    public string SelectedSourceId { get; set; } = string.Empty;
}
