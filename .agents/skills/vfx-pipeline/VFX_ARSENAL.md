# Reusable VFX Arsenal

The first-pass arsenal lives under `VFX/` and is deliberately independent from gameplay collision.
NPCs and projectiles still own authoritative damage; these helpers render the corresponding client-only
beam, flame, smoke, ring, impact, and soul effects.

## Runtime architecture

- `VFX/VFXSystem.cs`
  - One shared primitive-strip render pass.
  - One alpha particle pass for smoke.
  - One additive particle pass for light, sparks, and souls.
  - Hard limits of 2,400 particles and 256 strips prevent accidental runaway effects.
  - No visual entity is networked.
- `VFX/VFX.cs`
  - High-level recipes intended for boss and projectile code.
- `Utilities/VFXShowcaseCommand.cs`
  - Debug-only visual test command.

The primitive material presets reuse textures that the mod already loads:

| Material | Texture | Good for |
|---|---|---|
| `Solid` | Magic pixel | White-hot cores and exact telegraphs |
| `Turbulent` | `TurbulentNoise` | Flame bodies, unstable beams, eruptions |
| `Wavy` | `WavyNoise` | Wide auras, souls, magic ribbons |
| `Splotchy` | `SplotchyNoise` | Stone, ichor, corroded or fossil surfaces |

## Public recipes

```csharp
using VFXApi = global::tsorcRevamp.VFX.VFX;

VFXApi.Beam(start, end, 120f, Color.Cyan, Color.White, lifetime: 30);
VFXApi.FlamePillar(basePosition, 700f, 150f, Color.GreenYellow, Color.White, 90);
VFXApi.Ring(center, 40f, 520f, 20f, Color.Gold, 45);
VFXApi.SmokeVent(position, Color.Gray, count: 8);
VFXApi.GroundEruption(position, new Color(70, 55, 45), Color.Gold);
VFXApi.SoulSpiral(center, Color.Purple, count: 18);
VFXApi.WorldspineArc(start, end, 420f, 100f, Color.Bisque, Color.Purple, 120);
```

Call visual recipes on clients at deterministic attack ticks. Keep `Projectile.NewProjectile` and
`NPC.NewNPC` server-authoritative as usual. A visual's apparent width should match the separately authored
collision width.

## In-game showcase

Enable Debug Mode, then use:

```text
/vfxshowcase all
/vfxshowcase beam
/vfxshowcase pillar
/vfxshowcase earth
/vfxshowcase worldspine
```

The showcase is intentionally non-damaging and does not modify the Earth Fiend encounter.

## Earth Fiend visual recipes

### Faultline Litany

1. Draw the ground-crawler's exact future route with a narrow solid sulfur-gold strip.
2. Add a wider splotchy strip at low opacity.
3. On eruption, call `GroundEruption` at each armed ground segment.
4. Keep collision in the ground-crawler projectile.

### Dying Earth

1. During the long channel, call `SoulSpiral` with shrinking radius.
2. Draw three thin preview rings using `Solid` so their collision radii are unambiguous.
3. At commit, add `TelegraphFlash`, then replace previews with expanding turbulent rings.
4. Use `ScreenShake` only at the committed detonation.

### Lava Exhumation

1. Use a solid orange floor strip as the warning.
2. Spawn `FlamePillar` only when the damage projectile arms.
3. Feed `SmokeVent` at a low cadence near the top of the pillar rather than spawning a huge smoke burst.
4. Use white cores only during the damaging portion.

### Worldspine Breach

`WorldspineArc` is a cheap abstract preview, not the final creature renderer. The final helper should draw
its segment sprites along the same sampled curve, with the wavy soul strip behind it and a separate simple
hitbox moving along the telegraphed route.

## Source-art pack

Generated source art belongs in `Textures/VFX/Source/` and concept material in
`tsorcDocs/VFXConcepts/`. Source masks should stay grayscale on black so additive materials can use
them without requiring native transparency.

Planned masks:

- soft circular bloom
- hard beam core with tapered cap
- turbulent flame silhouette
- vertical flame lick
- smoke puff A and B
- branching fault crack
- radial impact star
- soul wisp
- ichor splash
- fossil/basalt breakup mask

These are source material, not automatically loadable content. Crop, normalize, and test a mask before
moving it into a runtime texture path.

---
# Shader authoring — moved

The `.fx` → `.xnb` toolchain, `ps_2_0` traps, texture traps, the effect parameter contract and the
"techniques worth copying" reference now live in **`.agents/skills/vfx-pipeline/SKILL.md`**, under
"Shader authoring reference".

They moved because they are agent guidance, not project documentation, and describing one toolchain
in two files let the two copies drift — neither recorded that the standalone and effect compilers
disagree about the slot budget, which cost an hour every time it was rediscovered.

This file now documents only the reusable `VFX/` arsenal above.

- **`.agents/skills/vfx-pipeline/SKILL.md`** — build/compile/register a shader in this repo.
- **`.agents/skills/vfx-shader-tips/SKILL.md`** — transferable HLSL technique (§1–30) and field notes
  on how effects here actually broke (§31–50).

---

## Build and playtest watchlist (arsenal runtime)


- Run tModLoader Build + Reload; this repository is not verified with standalone `dotnet build`.
- Confirm `PostDrawTiles` ordering places the environmental effects behind NPCs as intended.
- Check the BasicEffect world transform at non-default zoom levels.
- Check alpha smoke for dark fringes and whether the reused cloud sprites tint cleanly.
- Stress `/vfxshowcase all` repeatedly and confirm the particle/strip caps prevent runaway allocations.
- Verify the dedicated server never requests textures or creates `BasicEffect`.
- Verify beam, ring, and pillar visuals do not imply a larger collision area than their gameplay projectile.

