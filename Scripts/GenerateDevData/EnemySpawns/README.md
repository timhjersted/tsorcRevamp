# Enemy spawn developer-data generator

This tool reads the mod's C# syntax and regenerates the enemy spawn reference files in
`DevData/EnemySpawns/`.

The detailed per-enemy inventory is a standalone responsive HTML report. Open
`enemy_spawns_inventory.html` in a browser to use its full-width layout and enemy search.
`biome_spawn_pools.html` is the compact overview: progression phase, biome, enemy, and gate-adjusted
average weight. The original weight and `NextBool` divisor remain visible beneath adjusted values.

It requires the .NET 10 SDK. Roslyn is loaded from the installed SDK, so regeneration does not
download parser packages.

From the repository root:

```powershell
.\Scripts\GenerateDevData\EnemySpawns\generate.cmd
```

To verify that committed reports match the source without rewriting them:

```powershell
.\Scripts\GenerateDevData\EnemySpawns\generate.cmd --check
```

The generator intentionally excludes abstract implementation bases such as `PuppetNPC`, while
following inheritance through them so concrete invader NPCs remain in the inventory. Unknown or
complex conditions are preserved verbatim instead of being silently assigned to a guessed biome.

The tool runs embedded regression checks for conditional weights, biome exclusions, Underworld
rules, random gates, pool replacement operations, and Adventure Mode block rules before it reads
the repository.
