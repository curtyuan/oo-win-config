# SSH Config Manager Tab

## User Requirement

oo-win-config will eventually manage remote machine profiles through SSH, but this feature is not that remote file/profile manager.

This feature is an independent SSH settings manager.

The SSH tab should only serve SSH configuration:

1. Select which SSH config file is being managed.
2. Read SSH host entries from that config file.
3. Show each machine/host entry as a card.
4. Let the user edit cards.
5. Write card changes back to the selected SSH config file.

Remote file browsing, remote profile backup, snapshot map, and restore UI do not belong in this tab.

## Product Boundary

### In Scope

```text
SSH Config Tab
  Manages SSH config files.
  Shows Host blocks as editable cards.
  Reads from and writes to the selected SSH config file.
```

### Out Of Scope For This Tab

```text
Remote profile backup
Remote file browsing
Remote config item cards
Remote snapshot map
Remote restore actions
Remote trash actions
```

Those features can use SSH config entries later, but they should live in a different workflow or tab.

## Domain Terms

### SSH Config Source

An SSH config file path selected by the user.

Examples:

```text
%USERPROFILE%\.ssh\config
D:\ssh\work.config
C:\Users\User\.ssh\config
```

Windows users may not keep SSH config at `~/.ssh/config`, so the tab needs a config source selector.

### SSH Host Card

A visual representation of one `Host` block in the selected SSH config source.

Example SSH config block:

```sshconfig
Host kali-dev
  HostName 192.168.1.20
  User curtyuan
  Port 22
  IdentityFile ~/.ssh/id_ed25519
```

The card should be editable and write back to the same block.

### SSH Directive

A key/value line inside a `Host` block, such as `HostName`, `User`, `Port`, or `IdentityFile`.

Unknown directives must be preserved.

## UI Spec

### Navigation

Add a dedicated top-level tab:

```text
SSH Config
```

This tab is separate from:

```text
Backup
Remote
Settings
```

The tab should not show software profiles or remote backup config items.

### Layout

Recommended layout:

```text
+-------------------------------------------------------------+
| SSH Config                                                  |
| [Config source dropdown] [Add source]                       |
|                                                             |
| Selected file: C:\Users\User\.ssh\config                  |
| [Reload] [Save all] [Open file] [Backup config]             |
|                                                             |
| +------------------+ +------------------+ +----------------+ |
| | Host card        | | Host card        | | New host card  | |
| +------------------+ +------------------+ +----------------+ |
|                                                             |
| Edit panel / popup for selected Host card                   |
+-------------------------------------------------------------+
```

### Config Source Selector

The user can choose which SSH config file is active using a dropdown.

Different config files are first-class selectable sources, but the UI should stay compact because most users will usually work with one active source at a time.

Required source actions:

1. Add config source.
2. Remove config source from app list without deleting the file.
3. Reload selected source from disk.
4. Open selected source in Explorer/editor.
5. Create selected source if it does not exist.

Suggested source labels:

```text
Default
Work
Lab
Custom
```

App metadata for sources can be stored in:

```text
data/settings/ssh-config-sources.json
```

Example metadata:

```json
{
  "sources": [
    {
      "id": "default",
      "label": "Default",
      "path": "C:/Users/User/.ssh/config"
    },
    {
      "id": "work",
      "label": "Work",
      "path": "D:/ssh/work.config"
    }
  ],
  "selectedSourceId": "default"
}
```

This metadata stores which config files the app should show. It is not the source of truth for SSH host settings.

## SSH Host Card UI

Each card represents one `Host` block from the selected config source.

Card summary fields:

1. `Host` alias.
2. `HostName`.
3. `User`.
4. `Port`.
5. `IdentityFile`.
6. Key path exists/missing status.
7. Dirty/clean state.
8. Last test result.

Card actions:

1. Edit.
2. Test SSH.
3. Duplicate.
4. Mark host block for deletion.
5. Save card, if dirty.

Delete does not immediately write to disk. It marks the card as pending deletion and the deletion is applied when the user clicks `Save all`.

Do not include `Use for backup` in this tab for the first design pass. That action belongs to the future remote/profile workflow, not the SSH settings tab.

## Edit Card UI

The edit UI can be an inline expanded card or an overlay panel.

Common editable fields:

```text
Host
HostName
User
Port
IdentityFile
ProxyJump
ForwardAgent
ServerAliveInterval
StrictHostKeyChecking
```

Also show an advanced section for directives the app does not model directly:

```text
Advanced / preserved directives
```

Rules:

1. Unknown directives remain attached to the same Host block.
2. Editing common fields does not delete unknown directives.
3. Unrelated Host blocks are not modified.
4. Comments should be preserved when feasible.
5. If exact comment preservation is not possible, warn before rewriting that block.

## Read And Write Rules

### Read

When a config source is selected:

1. Read the file.
2. Parse global directives separately from Host blocks.
3. Convert each Host block into one SSH Host Card.
4. Preserve raw lines needed to write the block back safely.
5. Detect `Include` directives, but do not expand included files in the first implementation.

### Include Directives

SSH config can include other config files:

```sshconfig
Include ~/.ssh/conf.d/*.conf
Include D:/ssh/work/*.config
```

First implementation strategy:

1. Do not automatically expand `Include` files.
2. Show detected `Include` directives in a read-only source details section.
3. Let the user add an included file manually as another SSH Config Source in the dropdown.
4. Do not write through wildcard includes.

Reason:

Included files create ambiguity about where edits should be written, especially with wildcard includes and Windows paths. Treating included files as explicit config sources avoids accidental edits to the wrong file.

### Global Directives

Global directives are SSH config directives outside any `Host` block:

```sshconfig
ServerAliveInterval 30
IdentityAgent ~/.ssh/agent.sock
AddKeysToAgent yes

Host kali-dev
  HostName 192.168.1.20
  User curtyuan
```

First implementation strategy:

1. Show global directives in a read-only advanced section for the selected source.
2. Do not edit global directives through Host cards.
3. Preserve global directives when saving Host card changes.
4. Treat `Host *` as a Host card, but visually label it as defaults rather than a machine.

### Write

Before writing:

1. Detect whether the file changed on disk since last read.
2. Create a backup copy.
3. Apply pending card edits and pending card deletions.
4. Write only changed blocks when feasible.
5. Preserve unrelated blocks and global directives.

Suggested backup path:

```text
{configPath}.oo-win-config.bak
```

If the file changed on disk, prompt the user before overwriting.

## Test SSH

The SSH Config tab may test a card's connection settings.

Test SSH behavior:

1. Uses the selected config source and Host alias.
2. Does not require remote profile backup.
3. Displays success/failure on the card.
4. Stores last test status in app metadata if useful.

The test command should be equivalent to:

```text
ssh -F <selected-config-path> <host-alias>
```

Implementation can use a library later, but the UI model should remain config-source plus host-alias.

## Relationship To Remote Backup

Remote backup can later reference SSH Host Cards as endpoints.

That later workflow can decide how to map:

```text
SSH Host Card -> Remote endpoint -> Profile backup/snapshot
```

But this tab should not show remote files, software profiles, config item cards, baseline snapshot actions, restore actions, or trash actions.

The only durable bridge this tab needs is stable identity:

```text
config source id + Host alias
```

If remote backup stores endpoint metadata in snapshot manifests later, it can derive that metadata from the selected SSH Host Card at the time of backup.

## Open Decisions

1. Should the SSH Config tab keep a side panel for source details, or should source details live in a compact dropdown flyout?
2. Should `Host *` cards be editable in the first pass, or read-only defaults?
3. Which SSH directives should be promoted to first-class edit fields beyond the initial common set?
