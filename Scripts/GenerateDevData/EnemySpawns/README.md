# Enemy spawn report generator

Reads the spawn registry in `Systems/EnemySpawns.cs` with Roslyn and writes `DevData/EnemySpawns/`:

- `enemy_spawns.html` — a self-contained page (no network needed). Tabs: spawns **by biome** for the
  picked world stage, **by enemy** (every registry line with its per-stage weights, `max` / `stop`
  flags and the comment above it), **exclusive zones** with each enemy's share, **other lists**
  (no-spawn enemies, enemies that keep their own `SpawnChance`, enemies the registry never mentions),
  a **glossary** of every named test and shared rule set, and the **vanilla pool edits** still made in
  `tsorcRevampGlobalNPC.EditSpawnPool`. Search filters every tab.
- `enemy_spawns.csv` — one row per registry line, for spreadsheets.

The page layout lives in `report.html` next to this file. The generator only replaces the
`/*__SPAWN_DATA__*/null` placeholder in it with the data as JSON, so styling and views are edited there.

It requires the .NET 10 SDK. Roslyn is loaded from the installed SDK, so nothing is downloaded.

From the repository root:

```powershell
.\Scripts\GenerateDevData\EnemySpawns\generate.cmd
```

To verify the committed report matches the registry without rewriting it:

```powershell
.\Scripts\GenerateDevData\EnemySpawns\generate.cmd --check
```

## What it reads

- `Spawn(...)` and `SpawnNightBoosted(...)` lines (the latter expands into its base, night ×2 and blood
  moon ×4 lines, as the registry does), and `SpawnOnly(...)` exclusive zones.
- The comment block directly above a line becomes that enemy's (or zone's) note; a comment at the end
  of a line becomes that line's comment. A comment that is only a class name (`// Gigas`) marks a
  registered enemy with no natural spawn; `// Name: ...` is a note for an enemy outside the registry.
- Named tests (`Zone.Jungle`, `Depth.Rows(...)`) and shared rule sets (`Evil`, `HollowQuiet`) with the
  comment above each definition, for the glossary and chip tooltips.
- Weights may be literals, simple arithmetic, or a `const` on an NPC class (`Locust.NaturalSpawnChance`).
  Anything else stops the generator with the line number, rather than guessing.

A line goes in a biome bucket for every biome test it names un-negated, looking inside shared rule
sets; the named map locations (Catacombs, Machine Temple, Wyvern Fortress...) are buckets of their own.
To add a biome or a nicer chip label, edit `AtomBiomes`, `PlaceBiomes` or `AtomLabels` in `Program.cs`.
