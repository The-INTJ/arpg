# Run History Snippet: Converting The Code-Built Version Into A Panel-Editable UI

The current implementation intentionally uses code-built containers so we could land the feature quickly while keeping it reusable. The reusable piece is `scenes/RunHistorySnippet.tscn`, and both end screens instance that scene instead of owning the score layout directly.

## What Exists Right Now

- `scripts/GameState.cs` tracks the current run and stores completed run summaries in memory for the current app session.
- `scripts/RunScoreEntry.cs` defines the data shape the UI renders.
- `scenes/RunHistorySnippet.tscn` is the reusable snippet scene.
- `scripts/RunHistorySnippet.cs` currently builds each card, row, and label in code with `VBoxContainer`, `GridContainer`, and `PanelContainer`.

## Goal Of The Conversion

Move the visual structure out of `RunHistorySnippet.cs` and into editor-authored scenes so you can:

- tweak spacing, fonts, and nesting in the Godot editor
- give designers a real panel/card scene to edit
- reuse the same card layout in other screens without touching code

## Recommended Refactor Shape

Create two small UI scenes:

1. `scenes/ui/RunScoreCard.tscn`
2. `scenes/ui/RunStatRow.tscn`

Keep `scenes/RunHistorySnippet.tscn` as the host scene that owns the scroll area and instantiates card scenes.

## Step-By-Step

1. Create `scenes/ui/RunStatRow.tscn` with a simple root such as `HBoxContainer`.
   Add two `Label` children named `NameLabel` and `ValueLabel`.

2. Create `scripts/RunStatRow.cs`.
   Add a method like `Apply(RunStatLine line)` that writes into those labels.

3. Create `scenes/ui/RunScoreCard.tscn` with a `PanelContainer` root.
   Inside it, build the exact hierarchy you want in the editor: title label, meta label, score grid area, stats list area, separators, icons, background panels, whatever you want.

4. Create `scripts/RunScoreCard.cs`.
   Give it typed references to the important child nodes and an `Apply(RunScoreEntry entry, bool isLatest)` method.

5. Add an exported `PackedScene` field to `RunHistorySnippet.cs` for the card scene.
   Instantiate `RunScoreCard.tscn` instead of manually calling `CreateRunCard(...)`.

6. Inside `RunScoreCard.cs`, add another exported `PackedScene` for `RunStatRow.tscn`.
   Use that for the stats list instead of building labels in code.

7. Move as much styling as possible from C# into the scene itself.
   Keep `Palette` for colors, but let the scene own margins, structure, and panel nesting.

## Suggested Ownership Split

- `RunHistorySnippet.cs`
  - owns data source selection
  - owns scrolling container
  - instantiates one card per run

- `RunScoreCard.cs`
  - owns one run summary card
  - decides how a run is presented visually

- `RunStatRow.cs`
  - owns one name/value row

## Nice Follow-Up Improvements

- Add a `MaxEntries` export on `RunHistorySnippet.cs`
- Add a `ShowOnlyLatest` export for compact placements
- Add icon slots or outcome badges in `RunScoreCard.tscn`
- Move repeated font/spacing overrides into a shared theme resource

## Why This Structure Works Well

It keeps the feature reusable as a scene snippet while separating:

- data capture in `GameState`
- presentation data in `RunScoreEntry`
- screen composition in `RunHistorySnippet`
- editor-owned visuals in future card/row scenes

That gives you panel-editable UI without losing the "drop this into any screen" workflow that the new snippet already establishes.
