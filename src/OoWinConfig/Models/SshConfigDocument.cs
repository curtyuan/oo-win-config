namespace OoWinConfig.Models;

public sealed class SshConfigDocument
{
    public SshConfigSource Source { get; init; } = new();
    public List<string> PreambleLines { get; init; } = [];
    public List<SshHostBlock> HostBlocks { get; init; } = [];
    public List<string> IncludeLines { get; init; } = [];
    public List<string> GlobalDirectiveLines { get; init; } = [];
    public DateTime LastWriteTimeUtc { get; init; }
}

public sealed class SshHostBlock
{
    public int OriginalIndex { get; init; }
    public List<string> RawLines { get; init; } = [];
    public string HostAlias { get; init; } = string.Empty;
    public string HostName { get; init; } = string.Empty;
    public string User { get; init; } = string.Empty;
    public string Port { get; init; } = string.Empty;
    public string IdentityFile { get; init; } = string.Empty;
    public string ProxyJump { get; init; } = string.Empty;
    public string ForwardAgent { get; init; } = string.Empty;
    public string ServerAliveInterval { get; init; } = string.Empty;
    public string StrictHostKeyChecking { get; init; } = string.Empty;
    public List<string> PreservedDirectiveLines { get; init; } = [];
}
