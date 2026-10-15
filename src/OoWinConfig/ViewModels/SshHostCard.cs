using System.IO;
using OoWinConfig.Models;
using OoWinConfig.Services;

namespace OoWinConfig.ViewModels;

public sealed class SshHostCard : ViewModelBase
{
    private string _hostAlias = string.Empty;
    private string _hostName = string.Empty;
    private string _user = string.Empty;
    private string _port = string.Empty;
    private string _identityFile = string.Empty;
    private string _proxyJump = string.Empty;
    private string _forwardAgent = string.Empty;
    private string _serverAliveInterval = string.Empty;
    private string _strictHostKeyChecking = string.Empty;
    private string _preservedDirectives = string.Empty;
    private string _testStatus = "Not tested";
    private bool _isDirty;
    private bool _isPendingDelete;

    public int? OriginalIndex { get; set; }
    public string SourcePath { get; set; } = string.Empty;

    public string HostAlias
    {
        get => _hostAlias;
        set => SetEditableProperty(ref _hostAlias, value);
    }

    public string HostName
    {
        get => _hostName;
        set => SetEditableProperty(ref _hostName, value);
    }

    public string User
    {
        get => _user;
        set => SetEditableProperty(ref _user, value);
    }

    public string Port
    {
        get => _port;
        set => SetEditableProperty(ref _port, value);
    }

    public string IdentityFile
    {
        get => _identityFile;
        set => SetEditableProperty(ref _identityFile, value);
    }

    public string ProxyJump
    {
        get => _proxyJump;
        set => SetEditableProperty(ref _proxyJump, value);
    }

    public string ForwardAgent
    {
        get => _forwardAgent;
        set => SetEditableProperty(ref _forwardAgent, value);
    }

    public string ServerAliveInterval
    {
        get => _serverAliveInterval;
        set => SetEditableProperty(ref _serverAliveInterval, value);
    }

    public string StrictHostKeyChecking
    {
        get => _strictHostKeyChecking;
        set => SetEditableProperty(ref _strictHostKeyChecking, value);
    }

    public string PreservedDirectives
    {
        get => _preservedDirectives;
        set => SetEditableProperty(ref _preservedDirectives, value);
    }

    public string TestStatus
    {
        get => _testStatus;
        set => SetProperty(ref _testStatus, value);
    }

    public bool IsDirty
    {
        get => _isDirty;
        set
        {
            if (SetProperty(ref _isDirty, value))
            {
                OnPropertyChanged(nameof(StateLabel));
                OnPropertyChanged(nameof(DeleteActionLabel));
            }
        }
    }

    public bool IsPendingDelete
    {
        get => _isPendingDelete;
        set
        {
            if (SetProperty(ref _isPendingDelete, value))
            {
                IsDirty = true;
                OnPropertyChanged(nameof(StateLabel));
                OnPropertyChanged(nameof(DeleteActionLabel));
            }
        }
    }

    public string StateLabel => IsPendingDelete ? "Pending delete" : IsDirty ? "Dirty" : "Clean";
    public string DeleteActionLabel => IsPendingDelete ? "Undo delete" : "Mark delete";
    public string KeyStatus => string.IsNullOrWhiteSpace(IdentityFile) ? "No key" : File.Exists(ExpandIdentityFile()) ? "Key exists" : "Key missing";
    public bool IsDefaultsCard => string.Equals(HostAlias.Trim(), "*", StringComparison.Ordinal);

    public static SshHostCard FromBlock(SshConfigSource source, SshHostBlock block)
    {
        return new SshHostCard
        {
            OriginalIndex = block.OriginalIndex,
            SourcePath = source.Path,
            _hostAlias = block.HostAlias,
            _hostName = block.HostName,
            _user = block.User,
            _port = block.Port,
            _identityFile = block.IdentityFile,
            _proxyJump = block.ProxyJump,
            _forwardAgent = block.ForwardAgent,
            _serverAliveInterval = block.ServerAliveInterval,
            _strictHostKeyChecking = block.StrictHostKeyChecking,
            _preservedDirectives = string.Join(Environment.NewLine, block.PreservedDirectiveLines),
            _isDirty = false
        };
    }

    public static SshHostCard CreateNew(string sourcePath)
    {
        return new SshHostCard
        {
            SourcePath = sourcePath,
            _hostAlias = "new-host",
            _port = "22",
            _isDirty = true
        };
    }

    public SshHostCard Duplicate()
    {
        return new SshHostCard
        {
            SourcePath = SourcePath,
            _hostAlias = HostAlias + "-copy",
            _hostName = HostName,
            _user = User,
            _port = Port,
            _identityFile = IdentityFile,
            _proxyJump = ProxyJump,
            _forwardAgent = ForwardAgent,
            _serverAliveInterval = ServerAliveInterval,
            _strictHostKeyChecking = StrictHostKeyChecking,
            _preservedDirectives = PreservedDirectives,
            _isDirty = true
        };
    }

    public void AcceptChanges()
    {
        OriginalIndex ??= -1;
        IsDirty = false;
        IsPendingDelete = false;
        OnPropertyChanged(nameof(KeyStatus));
        OnPropertyChanged(nameof(IsDefaultsCard));
    }

    private bool SetEditableProperty(ref string field, string value)
    {
        if (!SetProperty(ref field, value))
        {
            return false;
        }

        IsDirty = true;
        OnPropertyChanged(nameof(KeyStatus));
        OnPropertyChanged(nameof(IsDefaultsCard));
        return true;
    }

    private string ExpandIdentityFile()
    {
        return SshConfigService.ExpandHome(IdentityFile.Trim().Trim('"'));
    }
}
