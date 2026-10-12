# Snapshot Map Redesign

## Goal

Redesign Snapshot map mode so it feels like a timeline map, not a nested placeholder panel.

Snapshot map remains inside Backup page and reuses the left profile list.

## Current Problems

1. Snapshot map still has background color mismatch around the timeline area.
2. The layout has too many nested margins/paddings between the Snapshot map host, the `Timeline` group, and the inner timeline widget.
3. The current timeline area is a placeholder grid, not a true software-by-date map.
4. Profile selection still visually inherits Backup mode behavior too much, including checkbox-oriented interaction.

## Desired Interaction

1. Enter Snapshot map from the button beside `Selected profile config items`.
2. Left column title changes from `Softwares` to `Profiles`.
3. Left profile cards do not show checkboxes in Snapshot map mode.
4. Single-clicking a profile card toggles whether it appears in the map.
5. Selected profile cards have a green outline.
6. Clicking a selected profile again removes the green outline and removes its row from the map.
7. The map updates immediately after profile selection changes.

## Desired Timeline Layout

1. Y axis: software profiles, one horizontal row per selected software/profile.
2. X axis: snapshot dates.
3. Older snapshots are on the left.
4. Newer snapshots are on the right.
5. After map refresh/search, horizontal scroll defaults to the far right so the most recent snapshots are visible first.
6. Snapshot date headers are pulled out into a top timeline row.
7. Date headers and snapshot cards use the same fixed column width so the same date aligns vertically across software rows.

## Snapshot Card Shape

Each snapshot card should be compact and show exactly three visual text rows:

1. `name`
2. `date`
3. `tag`

Cards should be clickable. They should not show checkboxes.

Snapshot/config cards must fit the dark theme. Avoid light default WPF backgrounds, high-contrast system colors, or borders that look disconnected from the rest of the dark UI.

## Styling Direction

1. Remove the extra outer background layer between Snapshot map mode and the timeline widget.
2. Remove the extra inner background layer inside `Timeline`.
3. Avoid stacked margins where Snapshot map host, `Timeline`, and the inner widget each add spacing.
4. Prefer one clear spacing boundary at the main content edge.
5. Timeline rows and cards should sit directly inside the map surface.
6. Snapshot cards should use the app's dark panel/card colors and green accent only for selected states.
7. Right-side controls should be separate widgets, with `Actions` above `Tags`.

## Tag And Action Behavior

1. `Actions` contains `Compare` and any compare helper text.
2. `Filter` contains tag/category filters.
3. `All` shows all placeholder snapshot cards.
4. `Manual` filters to manual snapshots.
5. `Batch` filters to batch snapshots.
6. Changing the filter refreshes the map and scrolls back to the newest visible date.
7. Compare remains enabled only when exactly two visible snapshot cards are selected.
8. `Delete snapshot` is enabled when at least one visible snapshot card is selected.
9. Delete removes selected placeholder snapshot cards from the current map data.
10. Actions/Tags are presented as a right sidebar that can be hidden and shown.
11. In Snapshot map mode, left-side `All` / `Clear` select or clear map profiles, not backup profiles.
12. Sidebar can be toggled by clicking the sidebar rail arrow or pressing `Ctrl+R`.
13. Sidebar arrow points right when expanded and left when collapsed.

## Trash Behavior

1. `Delete snapshot` moves snapshot directories to `data/trash/general/{profileId}/{snapshotId}/`.
2. It does not permanently delete files.
3. Trash is viewable through a `Trash` map mode entry beside Snapshot map.
4. Trash mode supports `General` and `Remote` scopes.
5. Trash mode uses the same left profile selection model as Snapshot map.

## Theme Settings

Settings should eventually offer a light background/theme option, but the default app theme remains dark.

## Implementation Plan

1. Split Backup left profile item template into two modes: Backup mode with checkbox, Snapshot map mode without checkbox.
2. Add a `IsSelectedForSnapshotMap` state to the profile row view model or a local selection set keyed by profile ID.
3. In Snapshot map mode, handle `SelectionChanged` or item click as toggle instead of standard single selection.
4. Apply a green border style to selected profile cards in Snapshot map mode.
5. Replace the current placeholder `UniformGrid` with an `ItemsControl` of profile timeline rows.
6. Each profile timeline row renders profile name at the left edge or row header, then a horizontal list of snapshot cards sorted by date ascending.
7. Wrap the timeline body in a horizontal `ScrollViewer` and call `ScrollToRightEnd()` after map data refresh.
8. Keep right-side `Tags / Actions` for now, but remove unnecessary nested margin layers.
9. Keep Compare enabled only when exactly two snapshot cards are selected.

## Open Design Choices

1. Whether timeline row headers should be sticky while horizontal scrolling.
2. Whether selected snapshot cards should use the same green outline as selected profiles or a different accent.
3. Whether profiles should appear in map order by left list order or by most recent snapshot date.

Initial recommendation: row headers do not need to be sticky for this pass, selected snapshot cards can use the same green accent, and profile row order should follow the left list order.
