# Remote Config Collection Todo

## Idea

Add a `Remote` tab for collecting configuration files from remote machines over SSH, starting with Kali/Linux and Windows targets.

The remote collection output should use the same general shape as local backups: software profiles define which config files to collect, and the collected result becomes a portable config package.

## Goal

Use SSH to collect config from a remote machine, then organize it into:

1. Profile-based config files.
2. A backup/package manifest.
3. Setup scripts for applying or installing related configuration later.

## Initial Scope

1. Add a `Remote` tab.
2. Support Linux targets first, especially Kali.
3. Support Windows targets as a first-class target type as well.
4. Reuse the same profile concept as local config backup.
5. Define remote software profiles separately from local profiles when needed.
6. Collect files via SSH/SFTP or equivalent SSH-based file transfer.
7. Store collected output in the same package/snapshot style as local backups.

## Current UI Skeleton

The first UI skeleton has three columns:

1. Remote targets: saved SSH targets such as Kali/Linux or Windows machines.
2. Remote collection plan: profile-driven list of config groups to collect.
3. SSH / Package: connection fields and package output actions.

This skeleton is non-functional for now. SSH connection, credential handling, profile execution, and package creation are future implementation work.

## Connected State Direction

After SSH connection succeeds, the Remote tab should behave similarly to the local Backup tab:

1. Show detected/available remote software profiles for that target.
2. Show selected remote profile config items in the center.
3. Use remote source paths from remote profile definitions.
4. Allow selecting config items to collect.
5. Use `Collect selected` to copy remote files into a local profile-based package/snapshot.
6. Keep target metadata in the manifest, such as host alias, OS type, collected time, and profile ID.

This keeps local backup and remote collection conceptually aligned while preserving separate actions: local `Create backup` vs remote `Collect selected`.

Current UI direction:

1. `New remote` opens an in-window overlay instead of a persistent right-side settings column.
2. `Test SSH` currently simulates success.
3. After connection succeeds, the SSH config column is hidden.
4. The connected state expands to a Backup-like layout with remote profiles on the left and selected remote config items on the right.

## Profile Direction

Remote targets should still be driven by developer-defined software profiles.

Examples to discuss later:

1. Kali shell config.
2. Kali terminal tools.
3. Neovim config on Linux.
4. SSH config.
5. Windows remote config profiles.

## Open Questions

1. Whether remote profiles should live beside local profiles or under a separate folder such as `data/remote-profiles`.
2. How to represent target OS differences in profile JSON.
3. Whether remote collection should produce a normal backup snapshot or a separate remote package type.
4. How setup scripts should be generated and stored.
5. Whether setup scripts should be PowerShell, Bash, or both.
6. How credentials, SSH keys, hosts, and connection history should be stored or avoided.
7. Whether remote Windows should use SSH only or eventually support WinRM.

## Not Implemented Yet

This is a future feature note only. Implementation details will be discussed later.
