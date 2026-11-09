# Developer Data

`DevData` contains developer-facing inventories, spreadsheets, and Markdown references extracted
from the mod. These files document source data; they are not runtime assets loaded by tModLoader.

Subject-specific data belongs in a named subdirectory such as `EnemySpawns/` or `Weapons/`.
Generated subdirectories should identify their generator and should be updated through that tool
rather than edited by hand.

The enemy spawn report (`EnemySpawns/enemy_spawns.html`, plus a one-row-per-rule CSV) is regenerated
by `Scripts/GenerateDevData/EnemySpawns` from the spawn registry in `Systems/EnemySpawns.cs`. Open the
HTML in a browser: it browses spawns by biome and stage, by enemy, by exclusive zone, and explains every
condition name the registry uses.
