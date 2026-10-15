using System.IO;
using System.Text.Json;
using OoWinConfig.Models;
using OoWinConfig.ViewModels;

namespace OoWinConfig.Services;

public sealed class SshConfigService
{
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public SshConfigService(string dataDirectory)
    {
        _settingsPath = Path.Combine(dataDirectory, "settings", "ssh-config-sources.json");
    }

    public SshConfigSourceSettings LoadSourceSettings()
    {
        if (File.Exists(_settingsPath))
        {
            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<SshConfigSourceSettings>(json, _jsonOptions);
            if (settings is not null && settings.Sources.Count > 0)
            {
                return settings;
            }
        }

        var defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh", "config");
        return new SshConfigSourceSettings
        {
            SelectedSourceId = "default",
            Sources =
            [
                new SshConfigSource
                {
                    Id = "default",
                    Label = "Default",
                    Path = defaultPath
                }
            ]
        };
    }

    public void SaveSourceSettings(SshConfigSourceSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, _jsonOptions));
    }

    public SshConfigDocument LoadDocument(SshConfigSource source)
    {
        var filePath = ExpandHome(source.Path);
        if (!File.Exists(filePath))
        {
            return new SshConfigDocument { Source = source };
        }

        var lines = File.ReadAllLines(filePath).ToList();
        var preamble = new List<string>();
        var blocks = new List<SshHostBlock>();
        var currentBlock = new List<string>();
        var blockIndex = 0;

        foreach (var line in lines)
        {
            if (IsDirective(line, "Host"))
            {
                if (currentBlock.Count > 0)
                {
                    blocks.Add(ParseHostBlock(currentBlock, blockIndex++));
                }
                else if (blocks.Count == 0)
                {
                    // First Host starts after the preamble.
                }

                currentBlock = [line];
                continue;
            }

            if (currentBlock.Count > 0)
            {
                currentBlock.Add(line);
            }
            else
            {
                preamble.Add(line);
            }
        }

        if (currentBlock.Count > 0)
        {
            blocks.Add(ParseHostBlock(currentBlock, blockIndex));
        }

        var includeLines = lines.Where(line => IsDirective(line, "Include")).ToList();
        var globalDirectiveLines = preamble
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Where(line => !line.TrimStart().StartsWith('#'))
            .Where(line => !IsDirective(line, "Include"))
            .ToList();

        return new SshConfigDocument
        {
            Source = source,
            PreambleLines = preamble,
            HostBlocks = blocks,
            IncludeLines = includeLines,
            GlobalDirectiveLines = globalDirectiveLines,
            LastWriteTimeUtc = File.GetLastWriteTimeUtc(filePath)
        };
    }

    public void EnsureSourceExists(SshConfigSource source)
    {
        var filePath = ExpandHome(source.Path);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, string.Empty);
        }
    }

    public void SaveDocument(SshConfigDocument document, IReadOnlyList<SshHostCard> cards)
    {
        var filePath = ExpandHome(document.Source.Path);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        if (File.Exists(filePath))
        {
            var backupPath = filePath + ".oo-win-config.bak";
            File.Copy(filePath, backupPath, overwrite: true);
        }

        var originalCards = cards
            .Where(card => card.OriginalIndex is not null)
            .ToDictionary(card => card.OriginalIndex!.Value);
        var newCards = cards.Where(card => card.OriginalIndex is null && !card.IsPendingDelete).ToList();
        var output = new List<string>(document.PreambleLines);

        foreach (var block in document.HostBlocks)
        {
            if (!originalCards.TryGetValue(block.OriginalIndex, out var card))
            {
                output.AddRange(block.RawLines);
                continue;
            }

            if (card.IsPendingDelete)
            {
                continue;
            }

            output.AddRange(card.IsDirty ? BuildHostBlock(card) : block.RawLines);
        }

        foreach (var card in newCards)
        {
            if (output.Count > 0 && !string.IsNullOrWhiteSpace(output[^1]))
            {
                output.Add(string.Empty);
            }

            output.AddRange(BuildHostBlock(card));
        }

        File.WriteAllLines(filePath, output);
    }

    public static string ExpandHome(string path)
    {
        if (path.StartsWith("~/", StringComparison.Ordinal) || path.StartsWith("~\\", StringComparison.Ordinal))
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path[2..]);
        }

        return Environment.ExpandEnvironmentVariables(path);
    }

    private static SshHostBlock ParseHostBlock(List<string> rawLines, int blockIndex)
    {
        var hostAlias = GetDirectiveValue(rawLines[0], "Host");
        var preserved = new List<string>();
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["HostName"] = string.Empty,
            ["User"] = string.Empty,
            ["Port"] = string.Empty,
            ["IdentityFile"] = string.Empty,
            ["ProxyJump"] = string.Empty,
            ["ForwardAgent"] = string.Empty,
            ["ServerAliveInterval"] = string.Empty,
            ["StrictHostKeyChecking"] = string.Empty
        };

        foreach (var line in rawLines.Skip(1))
        {
            if (TrySplitDirective(line, out var key, out var value) && values.ContainsKey(key))
            {
                values[key] = value;
            }
            else
            {
                preserved.Add(line);
            }
        }

        return new SshHostBlock
        {
            OriginalIndex = blockIndex,
            RawLines = rawLines,
            HostAlias = hostAlias,
            HostName = values["HostName"],
            User = values["User"],
            Port = values["Port"],
            IdentityFile = values["IdentityFile"],
            ProxyJump = values["ProxyJump"],
            ForwardAgent = values["ForwardAgent"],
            ServerAliveInterval = values["ServerAliveInterval"],
            StrictHostKeyChecking = values["StrictHostKeyChecking"],
            PreservedDirectiveLines = preserved
        };
    }

    private static List<string> BuildHostBlock(SshHostCard card)
    {
        var lines = new List<string> { $"Host {card.HostAlias}" };
        AddDirective(lines, "HostName", card.HostName);
        AddDirective(lines, "User", card.User);
        AddDirective(lines, "Port", card.Port);
        AddDirective(lines, "IdentityFile", card.IdentityFile);
        AddDirective(lines, "ProxyJump", card.ProxyJump);
        AddDirective(lines, "ForwardAgent", card.ForwardAgent);
        AddDirective(lines, "ServerAliveInterval", card.ServerAliveInterval);
        AddDirective(lines, "StrictHostKeyChecking", card.StrictHostKeyChecking);

        foreach (var line in SplitLines(card.PreservedDirectives))
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                lines.Add(line.StartsWith(' ') || line.StartsWith('\t') ? line : $"  {line}");
            }
        }

        return lines;
    }

    private static void AddDirective(List<string> lines, string key, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            lines.Add($"  {key} {value.Trim()}");
        }
    }

    private static IEnumerable<string> SplitLines(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
    }

    private static bool IsDirective(string line, string directive)
    {
        var trimmed = line.TrimStart();
        return !trimmed.StartsWith('#')
            && trimmed.StartsWith(directive, StringComparison.OrdinalIgnoreCase)
            && trimmed.Length > directive.Length
            && char.IsWhiteSpace(trimmed[directive.Length]);
    }

    private static string GetDirectiveValue(string line, string directive)
    {
        return IsDirective(line, directive) ? line.TrimStart()[directive.Length..].Trim() : string.Empty;
    }

    private static bool TrySplitDirective(string line, out string key, out string value)
    {
        key = string.Empty;
        value = string.Empty;
        var trimmed = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
        {
            return false;
        }

        var separator = trimmed.IndexOfAny([' ', '\t']);
        if (separator <= 0)
        {
            return false;
        }

        key = trimmed[..separator];
        value = trimmed[separator..].Trim();
        return true;
    }
}
