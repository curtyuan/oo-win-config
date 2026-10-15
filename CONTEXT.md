# Context

## Glossary

### SSH Config Tab

A top-level tab dedicated to managing SSH config files. It reads, displays, edits, and writes SSH `Host` blocks. It does not show remote files, software profiles, snapshots, restore actions, or trash.

### SSH Config Source

An SSH config file selected by the user, such as `C:\Users\User\.ssh\config` or `D:\ssh\work.config`. Windows users may have multiple sources, and the selected source is the source of truth for the host cards shown in the SSH Config tab.

### SSH Host Card

A card representation of one SSH `Host` block from the selected SSH Config Source. Editing the card changes that `Host` block and can be written back to the config source.

### Remote Profile Management

A separate future workflow for using SSH connections to collect or restore software profile files on remote machines. It can reference SSH Host Cards later, but it is not part of the SSH Config Tab.
