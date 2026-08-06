---
name: vfx-dust-tips
description: Dust-particle guide for Terraria tModLoader VFX — a curated DustID table for fire/smoke/ember/magic/blood/ice/poison/electric/stone/sparkle, the count-vs-scale "chunkiness" tradeoff, layered burst architecture, NewDust vs NewDustPerfect vs NewDustDirect, what fadeIn/noGravity/noLight actually do, and a symptoms → fix table.
---

# ✨ Dust Particle Tips for Terraria (tModLoader)

Shaders get all the attention, but most of what a player actually *sees* in this mod is dust. It is
also where the cheapest wins are: an effect that reads as "blocky", "sparse", "flat" or "it just
stops" is usually one number away from being fixed, and unlike a shader you can change it without a
recompile.

> **Start here if an effect looks BLOCKY:** §3 (count buys density, scale does not) and §6
> (`fadeIn` is a grow-to-scale target, not an alpha ramp — over half its uses in this repo are
> silent no-ops). Those two account for most of the "why does my fire look like orange LEGO" time.

## Related Skills & References
- **[VFX Pipeline & Shader Development Guide](file:///.agents/skills/vfx-pipeline/SKILL.md)**: the
  end-to-end `.fx` → `.xnb` → C# pipeline, compiler disagreements, and the draw-helper audit list.
- **[VFX Shader Tips & HLSL Best Practices](file:///.agents/skills/vfx-shader-tips/SKILL.md)**: HLSL
  technique, the field notes (§31–41), the offline preview harness (§42–48). §20 there (the 3-layer
  particle architecture) and §23 (easing curves) are the shader-side companions to §4 and §7 below.
- **[Documentation/VFX_ARSENAL.md](file:///Documentation/VFX_ARSENAL.md)**: the reusable `VFX/`
  particle/beam/ring library. Check it before hand-rolling a particle system.

---

## 1. The Curated DustID Table

IDs verified against `Terraria.ID.DustID` in the decompiled source
(`ModSources/TerrariaDecompiled/Terraria.ID/DustID.cs`) and cross-checked against
<https://terraria.wiki.gg/wiki/Dust_IDs>. Prefer the named constant (`DustID.Torch`) over the raw
number — a lot of old code in this repo still passes bare `6` / `31` and it is unreadable.

| Need | First pick | ID | Alternates | Notes |
|---|---|---|---|---|
| **Fire (general)** | `DustID.Torch` | 6 | `OrangeTorch` 158, `RedTorch` 60 | The workhorse. 173 uses in this repo — if you are unsure, this is the answer. |
| **Fire (cooler / trailing)** | `DustID.OrangeTorch` | 158 | `YellowTorch` 64 | Good as a *second* layer behind `Torch` so a flame is not one flat hue. |
| **Fire (evil / cursed)** | `DustID.CursedTorch` | 75 | `Shadowflame` 27, `DemonTorch` 65 | Green-black and purple-black respectively. `Shadowflame` is this mod's default "dark" partner to `RedTorch`. |
| **Smoke** | `DustID.Smoke` | 31 | `Cloud` 16, `Asphalt` | Tint it — untinted `Smoke` is a flat mid-grey. See §5. |
| **Embers / sparks** | `DustID.Torch` (small scale) | 6 | `SparkForLightDisc` 306, `Firework_Red` | An "ember" is not a different dust, it is `Torch` at scale 0.4–0.8 with high velocity. |
| **Magic (holy / gold)** | `DustID.GoldFlame` | 228 | `AncientLight` 261, `SilverFlame` 279 | `GoldFlame` (118 uses here) is the mod's Gwyn/sunlight signature. |
| **Magic (arcane / void)** | `DustID.ShadowbeamStaff` | 173 | `Shadowflame` 27, `PurpleTorch` 62, `DemonTorch` 65 | `ShadowbeamStaff` (108 uses) is the abyss signature. |
| **Blood** | `DustID.Blood` | 5 | `GreenBlood` 273, `CrimsonSpray` | **Gore-gated**: with Blood and Gore off, vanilla silently replaces 5 and 273 with clouds. Never rely on their colour for a mechanic tell. |
| **Ice / frost** | `DustID.Frost` | 92 | `IceTorch` 135, `Ice` 80, `IceRod` | `Frost` reads coldest; `IceTorch` is the blue-flame variant. |
| **Poison / toxic** | `DustID.Poisoned` | 46 | `PoisonStaff` 163, `ToxicBubble`, `VenomStaff` | `Poisoned` (46) has a special `fadeIn` rule: it grows at **+0.1/tick**, ten times the normal rate. |
| **Electricity** | `DustID.Electric` | 226 | `MartianSaucerSpark` 228-adjacent, `WitherLightning` 272 | `Electric` and `MartianSaucerSpark` both read as arcing sparks; use short lifetimes and high speed. |
| **Stone / debris** | `DustID.Stone` | 1 | `Dirt` 0, `Asphalt`, ore dusts (`Copper`, `Titanium`, `Adamantite`) | These are the few you usually want **with** gravity. |
| **Sparkle / glint** | `DustID.AncientLight` | 261 | `YellowStarDust` 292, `GemDiamond` 91, `MagicMirror` 15 | The §4 "foreground" layer. Small, fast, short-lived, `noGravity`. |
| **Lava** | *(avoid — see below)* | 35 | use `Torch` + orange tint | See the trap in §9. |

**Picking by colour instead of by name.** Most torch dusts accept a `newColor` tint that multiplies
the sprite, so you often want a *neutral bright* dust plus a tint rather than hunting for an ID that
is already the right hue. `DustID.TintableDustLighted` (43) exists precisely for this.

---

## 2. `NewDust` vs `NewDustPerfect` vs `NewDustDirect`

Three entry points, and the repo uses all three (2359 / 496 / 257 call sites). They are not
interchangeable:

```csharp
// Scatters randomly inside a RECTANGLE. Returns an int INDEX. Use when you want an area fill and
// do not care exactly where each mote lands — e.g. a projectile's own hitbox.
int i = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                     DustID.Torch, speedX, speedY, alpha, color, scale);
Main.dust[i].noGravity = true;

// Same, but returns the Dust OBJECT. Strictly better than NewDust when you are going to set fields
// on it — no Main.dust[i] round-trip, no index juggling.
Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch);

// EXACT position, EXACT velocity, no rectangle scatter. Use whenever you are computing the
// position yourself (rings, arcs, cones, converging suction) — which is most good VFX.
Dust d = Dust.NewDustPerfect(center + offset, DustID.Torch, velocity, alpha, color, scale);
```

**Rules of thumb**
- Computing an offset yourself? `NewDustPerfect`. Anything else double-scatters and blurs your shape.
- Not computing an offset, but setting fields? `NewDustDirect`.
- `NewDust` returning `6000` means the dust budget is exhausted; `Main.dust[6000]` is a dummy, so
  writing to it is harmless. That is why almost nobody guards the index — but it *does* mean your
  effect silently thins out under load. Do not build a mechanic tell out of dust alone.

**Hidden scale jitter.** All three apply `dust.scale = (1 ± 0.20) * Scale` before your value lands.
So a `Scale` of 2f really means **1.6–2.4**, and any multiplier you apply afterwards multiplies the
jitter too. When you are reasoning about a maximum size, always compute it as
`1.2 × Scale × (everything you multiply afterwards)`.

---

## 3. Chunkiness: count buys density, scale does not

**The single most common dust bug in this repo.** A dust sprite is ~8×8 px. At `scale = 2` it is a
16px square — the same size as a Terraria tile. At `scale = 4` it is a 32px square, and the effect
stops reading as particles and starts reading as masonry.

The instinct when an effect looks thin is to raise `scale`. That is backwards: scale makes each mote
more *obvious as a square*, while count makes the cloud more *continuous*. Trade one for the other:

```csharp
// BEFORE — 1 mote at up to scale 5.4 (a ~43px block). Reads as an orange brick.
// AFTER  — 2 motes at up to scale 4.05, plus a fine pass at 0.5-0.95.
```

Worked example: `Projectiles/Enemy/FireBreath.cs`. Vanilla `aiStyle 23` spawned **one**
`DustID.Torch` per tick and then multiplied its scale by `3.0` and again by `1.5`, so with the ±20%
jitter the ceiling was **5.40**. That one number is why every fire-breath in the mod looked like a
column of LEGO. The rewrite caps the body at 4.05, doubles the count, and adds a fine foreground
pass — same visual mass, no blocks.

Second example: `Projectiles/VFX/TeleportMistLinger.cs` fire style went from 5 motes/frame at up to
scale 2.25 to 7 motes/frame at up to 1.70, plus an ember pass.

**Calibration numbers that work here**
| Layer | Scale band | Count |
|---|---|---|
| Background atmosphere (smoke, fog) | 0.5–0.9 spawn, growing via `fadeIn` to 1.3–1.7 | few, long-lived |
| Midground body (the fire itself) | 0.75–1.7 | the bulk |
| Foreground detail (sparks, glints) | 0.4–0.95 | ~⅓ of the body count |

Anything above ~2.0 needs a reason. Anything above ~3.0 is almost certainly a bug.

---

## 4. Layered bursts (the 3-pass architecture, in dust)

This is [vfx-shader-tips §20](file:///.agents/skills/vfx-shader-tips/SKILL.md) applied to particles.
One pass of identical motes always reads as one stamp; three passes with genuinely different
parameters read as an event. The three layers are **different in every axis**, not the same loop at
three sizes:

```csharp
// MIDGROUND — the body. Medium, medium speed, the recognisable colour.
for (int i = 0; i < 64; i++)
{
    Vector2 direction = Main.rand.NextVector2Unit();
    bool soot = i % 4 == 0;                                  // a minority dark mote gives contrast
    Dust ember = Dust.NewDustPerfect(
        Projectile.Center + direction * Main.rand.NextFloat(4f, 68f),
        soot ? DustID.Shadowflame : DustID.RedTorch,
        direction * Main.rand.NextFloat(1.6f, 6.8f),
        soot ? 150 : 90,
        soot ? new Color(16, 3, 8) : new Color(214, 22, 42),
        Main.rand.NextFloat(0.75f, 1.5f));
    ember.noGravity = true;
}

// FOREGROUND — smallest, FASTEST, shortest-lived. Gives the blast a sharp leading edge.
for (int i = 0; i < 22; i++) { /* scale 0.45-0.85, speed 7-12.5, noGravity */ }

// BACKGROUND — slowest, darkest, longest-lived, and it BILLOWS (see §6). Outlives the fire so the
// effect does not end on a hard cut.
for (int i = 0; i < 16; i++) { /* spawn scale 0.5-0.8, fadeIn 1.3-1.7, speed 0.6-2.2 */ }
```

Real implementation: `Projectiles/Enemy/EnemyFirebomb.cs` `OnKill`, and
`Projectiles/Enemy/FireBreath.cs` `SpawnBreathDust`.

**Rings and multi-ring bursts.** If you emit two rings, vary *every* parameter between them —
count, speed, radius, angular phase, lifetime — or you have drawn one ring twice.
`tsorcRevampAIs.SpawnFireTeleportBurst` offsets its second ring by a half-step
(`+ MathHelper.Pi / count`) plus an irrational nudge so its members land in the first ring's gaps.
This is the particle form of [vfx-shader-tips §33](file:///.agents/skills/vfx-shader-tips/SKILL.md).

---

## 5. Colour tinting

`newColor` **multiplies** the dust sprite; it does not replace it. So:
- Tinting a bright dust darker works (`DustID.Smoke` + `new Color(38, 30, 32)` → sooty smoke).
- Tinting a dark dust brighter does **nothing**. If you need a brighter result, pick a brighter ID.
- `default` / `Color.White` means "no tint" — that is what the overwhelming majority of calls pass.

`alpha` (the `int` parameter, 0–255) is **transparency, inverted**: `0` is fully opaque, `255` is
invisible. Higher alpha = fainter. Typical bands in this repo: `60–90` for hot sparks you want to
punch, `100–150` for body fire, `170–200` for background smoke.

Watch out for the same trap as [vfx-shader-tips §48](file:///.agents/skills/vfx-shader-tips/SKILL.md):
a pale desaturated tint over a bright dust is white. If a flame is reading pastel, lower the `alpha`
(make it more opaque) rather than raising the tint's brightness.

---

## 6. `fadeIn` is a GROW-TO-SCALE target, not an alpha ramp

**Read this before setting `fadeIn` again.** From `Terraria/Dust.cs` `UpdateDust`:

```csharp
if (dust.fadeIn > 0f && dust.fadeIn < 100f)
{
    dust.scale += 0.03f;                       // (+0.1f for types 46 / 213 / 260)
    if (dust.scale > dust.fadeIn) dust.fadeIn = 0f;
}
else { dust.scale -= 0.01f; }                  // normal decay
```

So `fadeIn` means **"keep growing until you reach this scale, then start shrinking"**. It has
nothing to do with opacity. Two consequences:

1. **`fadeIn <= spawn scale` is a silent no-op.** The dust is already bigger than the target, so the
   flag clears on the first tick and nothing happens. This is very common in this repo —
   `EnemyVFX.cs:251` (`fadeIn = 0.4f` on a 0.9–1.5 dust), `tsorcRevampAIs.SpawnTeleportMist`
   (`fadeIn = 0.45f` on a scale-1.0 dust), and the old `TeleportMistLinger` fire pass
   (`fadeIn = 0.6f` on a 1.5–2.25 dust) were all doing nothing at all.
2. **To actually billow, spawn small and set `fadeIn` above it.** `Main.rand.NextFloat(0.5f, 0.8f)`
   spawn scale with `fadeIn = Main.rand.NextFloat(1.3f, 1.7f)` gives a smoke mote that visibly
   expands for ~30 ticks before it decays. That is the correct shape for smoke, dust clouds, and
   anything gaseous.
3. **`fadeIn` also suppresses tile-collision culling** for several dust types (`if (SolidCollision(...)
   && dust.fadeIn == 0f)`), so a growing dust survives inside walls where a normal one would be
   deleted. Occasionally useful, occasionally a bug.

`ChaosBlackFire.cs:102` (`fadeIn = 3f` on a scale 1–2 smoke) is a correct, deliberate use: those
motes triple in size. Copy that pattern, not the no-op one.

---

## 7. `noGravity`, `noLight`, velocity and lifetime

- **`noGravity = true`** is right for essentially all fire, smoke, magic and spark VFX. Without it
  the mote arcs downward and your radial burst turns into a fountain. Reach for it by default; leave
  gravity ON only for debris, blood spatter, and anything meant to fall. Note that `noGravity` also
  switches several dust types onto a **different scale/velocity branch** in `Dust.UpdateDust` (some
  drift-damp at `velocity *= 0.93f`, some accelerate below scale 0.7, `DustID.Lava` grows) — the
  behaviour is per-type, so if a dust does something odd, read its branch rather than assuming.
- **`noLight = true`** stops the dust contributing to the lighting engine. Use it on *dark* motes
  (soot, shadow) — otherwise a black smoke cloud paradoxically brightens the room.
- **`velocity`** is where the character is. A few patterns worth knowing by name:
  ```csharp
  Vector2 dir = Main.rand.NextVector2Unit();                    // uniform radial burst
  Vector2 dir = Main.rand.NextVector2Circular(r, r);            // filled disc offset
  Vector2 dir = Main.rand.NextVector2CircularEdge(r, r);        // ON the ring, not inside it
  Vector2 inward = (center - pos).SafeNormalize(Vector2.Zero);  // SUCTION — telegraphs a charge-up
  ```
  Suction (motes moving *inward*) is the standard anticipation cue before a big attack — see
  `RedKnightVFX.DrawStormHeraldGather` and
  [vfx-shader-tips §21](file:///.agents/skills/vfx-shader-tips/SKILL.md).
- **You cannot set a lifetime directly.** A dust dies when `scale` decays to ~0. You control its
  life through spawn scale, `fadeIn`, and `noGravity`. If you need a precise duration, spawn from a
  timed emitter projectile (see `Projectiles/VFX/TeleportMistLinger.cs`) rather than fighting it.
- **Ramp emission with the event.** A telegraph should emit more as it nears release:
  `int count = 1 + (int)(progress * 3f);` — `AbyssLurkerMeteor.cs:110`, `EnemyVFX.cs:243`.

---

## 8. Directional dust: cones, streams and trails

For a stream (a breath, a flamethrower, a wake) build the basis once and offset along it:

```csharp
Vector2 forward  = Projectile.velocity.SafeNormalize(Vector2.UnitX);
Vector2 sideways = new(-forward.Y, forward.X);        // perpendicular, no trig needed

Dust fine = Dust.NewDustPerfect(
    Projectile.Center + sideways * Main.rand.NextFloat(-9f, 9f),   // spread ACROSS the stream
    DustID.Torch,
    forward * Main.rand.NextFloat(1.4f, 3.6f)                       // travel ALONG it
        + sideways * Main.rand.NextFloat(-1.1f, 1.1f),              // slight divergence
    120, default, Main.rand.NextFloat(0.5f, 0.95f));
fine.noGravity = true;
```

`new Vector2(-v.Y, v.X)` is the cheap perpendicular — do not reach for `RotatedBy(MathHelper.PiOver2)`.

The particle analogue of
[vfx-shader-tips §45](file:///.agents/skills/vfx-shader-tips/SKILL.md) (anisotropic sampling) is:
**give a stream much more variance along its axis than across it.** Equal spread in both directions
makes a stream look like a ball.

---

## 9. Traps: dusts that do not do what their name says

| Dust | Trap |
|---|---|
| `DustID.Lava` (35) | **Not a general fire dust.** With `noGravity` it *grows* at +0.03 scale/tick (`if (dust.type == 35 && dust.noGravity) dust.scale += 0.03f;`), so a 60-tick mote balloons by +1.8 — the opposite of what you want for flame. Without `noGravity` it is a lava *bubble* that deletes itself the instant it is not inside a liquid (`if (!Collision.WetCollision(...)) dust.scale = 0f;`). It also increments the ambient `lavaBubbles` counter. Used **zero** times in this repo, correctly. Use `DustID.Torch`. |
| `DustID.Blood` (5), `GreenBlood` (273) | Silently replaced with cloud dust when the player has Blood and Gore disabled. Fine as flavour, never as a readability cue. |
| `DustID.Poisoned` (46) | Grows at **+0.1/tick** under `fadeIn`, not +0.03. A `fadeIn` value tuned on a normal dust will overshoot wildly here. |
| `DustID.Smoke` (31) untinted | Flat mid-grey that reads as "placeholder". Always tint it toward the effect's palette. |
| Vanilla `aiStyle` dust | If your `ModProjectile` uses a vanilla `aiStyle`, the vanilla AI spawns dust **before** `ModProjectile.AI()` runs, so you cannot tame it from there. Override `PreAI()` and return `false`, then reimplement — that is what `FireBreath.cs` does. |
| Dust budget | `Dust.NewDust` starts randomly refusing spawns past ~60% of `Main.maxDustToDraw`. A 200-mote burst does not reliably produce 200 motes. Budget accordingly and never gate gameplay on dust. |

---

## 10. Symptoms → fix

| Symptom | Likely cause | Fix |
|---|---|---|
| "Looks like orange/grey **blocks**" | `scale` above ~2 | §3 — halve the scale, double the count. Check for hidden multipliers and the ±20% jitter. |
| "Looks **sparse / thin**" | too few motes | Raise count. Do **not** raise scale — that is what caused the last bug. |
| "Reads as **one stamp repeated**" | every mote has identical parameters | §4 — three layers with genuinely different scale/speed/lifetime/colour; randomise per mote. |
| "**Pops in** abruptly" | spawn scale is already the final scale | §6 — spawn small, set `fadeIn` **above** the spawn scale. |
| "My `fadeIn` does nothing" | `fadeIn <= spawn scale` | §6 — it is a grow-to target, not an alpha ramp. |
| "Effect **ends on a hard cut**" | every layer has the same lifetime | §4 — add a slow background layer that outlives the body. |
| "Radial burst **droops** into a fountain" | `noGravity` not set | §7. |
| "Black smoke **lights up** the room" | dust contributes to lighting | §7 — `noLight = true`. |
| "Stream looks like a **ball**" | equal spread along and across | §8 — much more variance along the axis than across it. |
| "Colour tint has **no effect**" | tinting a dark dust brighter | §5 — `newColor` multiplies; pick a brighter `DustID`. |
| "Dust is **too faint**" | `alpha` too high | §5 — `alpha` is inverted; lower it toward 0. |
| "Dust **stops mid-effect**" | emission is gated by a condition that expired | Check the gate, not the dust — see [vfx-shader-tips §39](file:///.agents/skills/vfx-shader-tips/SKILL.md). |
| "Vanilla-`aiStyle` dust I can't change" | vanilla AI runs before `ModProjectile.AI()` | §9 — `PreAI()` returning `false` + reimplement. |
| "Count doesn't match what I asked for" | dust budget throttling | §2 / §9 — expected under load; don't rely on exact counts. |

---

## 11. Verification discipline

You cannot preview dust offline the way you can preview a shader
([vfx-shader-tips §42](file:///.agents/skills/vfx-shader-tips/SKILL.md)), so the discipline is
arithmetic instead:

- **Write the maximum scale down.** `1.2 (jitter) × Scale × every multiplier`. If that number is
  above 2, justify it in a comment.
- **State before → after counts and scale bands** in any change description. "Made it better" is not
  reviewable; "40 motes @ 0.9–1.9 → 64 @ 0.75–1.5 plus a 22-mote fine pass" is.
- **Check who else spawns the projectile you are editing.** `FireBreath` alone is fired by eight
  different NPCs plus the shared teleport system — a "demon fire-breath" tweak is a mod-wide change.
  Same rule as [vfx-shader-tips §35](file:///.agents/skills/vfx-shader-tips/SKILL.md).
- **Adding damaging particles is a balance change, not polish.** If a decorative layer needs to
  overlap a damaging one, spawn it with `hostile = false; friendly = false; damage = 0;` and say so
  — see the second ring in `tsorcRevampAIs.SpawnFireTeleportBurst`.
