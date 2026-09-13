# Enemy Upgrade Matrix

Which enemies have which combat/animation improvements, and who to copy from when bringing one up.

The table below is **generated from the source** — regenerate it rather than editing it by hand:

```bash
bash .agents/tools/enemy-upgrade-matrix.sh --write
```

Covers the `PuppetNPC` melee/animation axes only. For the FighterAI/ArcherAI side — poise, hyper-armor
windows, evasion, kiting, pre-attack jumps — see [EnemyAbilityPortingGuide.md](EnemyAbilityPortingGuide.md);
that guide is the equivalent checklist for non-puppet enemies.

---

## Coverage

`Y` opted in · `n` explicitly opted **out** (a decision, not an oversight) · `Y*` on via an archetype
default, not written in the file · `.` off by default, never considered

<!-- GENERATED:BEGIN -->

| Enemy | archetype | arm | 2hnd | ease | clock | tele | mirr | aimC | aimA | flip | arcs | over | link | foll | land | pool | clip |
|---|---|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
| Program | — | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . | Y |
| PuppetProfile | — | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . |
| HeroofLumelia | Broadsword | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . |
| Artorias | Greatsword | Y | . | Y | Y | Y | Y | . | . | . | Y | Y | Y | Y | Y | . | . |
| Gwyn | Greatsword | Y | Y | Y | Y | Y | Y | . | Y | n | Y | Y | Y | Y | Y | Y | Y |
| SoulOfCinder | Broadsword | Y | . | Y | Y | Y | Y | . | Y | Y | . | . | Y | Y | . | Y | . |
| ClericOfSorrow | — | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . |
| DarkBloodKnight | Broadsword | Y | . | Y | Y | Y | Y | . | . | . | Y | . | Y | Y | . | Y | . |
| DarkKnight | Broadsword | Y | . | Y | Y | Y | Y | . | . | . | Y | . | Y | Y | Y | Y | . |
| AbyssalNinjaInvader | — | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . |
| BlackNinja | Flail | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . |
| Blaidd | Greatsword | . | . | Y | . | . | Y | . | Y | . | . | . | . | . | . | Y | . |
| CursedDragonInvader | Halberd | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . |
| DreadWraith | — | Y | . | Y | . | Y | Y | . | Y | . | . | . | . | . | . | Y | . |
| Kahlrun | Broadsword | Y | . | Y | . | Y | Y | . | Y | . | . | . | . | . | . | Y | Y |
| Khaios | — | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . |
| OwlFatherInvader | Axe | Y | . | Y | Y* | Y | Y | Y* | Y | n | Y | . | Y | Y | Y | Y | Y |
| ShadowNinja | Rapier | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . |
| SpiritOfKhaios | — | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . |
| StuddedLeatherWarrior | Axe | Y | . | . | Y* | Y | Y | Y* | . | . | . | . | Y | Y | Y | Y | Y |

<!-- GENERATED:END -->

---

## What each axis is

| Col | Virtual | What it buys you | What it looks like without it |
|---|---|---|---|
| `arm` | `UseCompositeArmSwing` | The vanilla composite front arm rotates continuously with the blade | The arm snaps between 4 fixed Use1–Use4 poses under a smoothly-moving sword |
| `2hnd` | `UseTwoHandedCompositeSwing` | Back arm reaches a second hilt point and follows the same swing | A greatsword swung one-handed |
| `ease` | `UseSwingEasing` | Arcs run through `SwingEase` instead of a bare `Lerp` | Constant angular velocity, dead stop on the last frame |
| `clock` | `UseAuthoredComboSwingClock` | The step's authored `AttackTicks` *is* the 0→1 swing clock | Swing timing drifts from the weapon's `useAnimation` |
| `tele` | `UseLogicalMeleeTelegraphs` | Wind-up settles to the opposite end, then raises — and **lands exactly** on the swing's start angle | Exponential `Lerp` toward the start angle that never arrives, so the swing snaps on frame one |
| `mirr` | `MirrorMeleeSwingRotationByFacing` | Blade geometry mirrors correctly when facing left | Blade reversed / hilt-first on one facing |
| `aimC` | `UseAimCenteredSwing` | Whole arc reorients toward the player's actual pitch (up/level/down) | Fixed arc; swings over a player standing above or below |
| `aimA` | `UseAimAdaptiveArc` | Cheaper version: biases the arc endpoints toward the target | as above, milder |
| `flip` | `UseAlternateFlip` | Alternating swings reverse the arc so repeats don't look identical | Every chop is the same animation |
| `arcs` | `ModifyMeleeArcEndpoints` | Per-motion hand-tuned start/end angles for this weapon's proportions | Generic archetype angles on a sprite they weren't authored for |
| `over` | `OverheadWindupOvershoot` | Wind-up goes past the arc start, so the swing has runway | Swing begins from a cramped pose |
| `link` | `MeleeComboInterStepLingerTicks` | Inter-step pause holds the contact pose, then eases into the **next** step's start angle | ~2 rad snap between combo steps |
| `foll` | `MeleeRecoveryLingerTicks` | Recovery parks the finished pose before easing back to the carry | Follow-through cut off on the frame damage ends |
| `land` | `UseLandingTimedLeapSlam` | `LeapSlam` resolves on real ground contact (`OnLeapSlamLanded`, `UpdateLeapSlamPose`) | Slam resolves on a tick countdown; the impact doesn't match the feet |
| `pool` | `MeleeComboPoolOverride` | A bespoke combo table for this enemy | The shared `WeaponArchetypeTables` list for its archetype |
| `clip` | `RuntimeV2Clip` | `PuppetAttackClip` single-clock executor (windup/active/recovery as one authored clip) | The legacy multi-phase rotation switch |

---

## Reference implementations — who to copy

Ranked by how much of the above they actually carry. **Recency is not maturity**: most of these files
share a handful of bulk commit dates, so `git log` tells you nothing useful here. Count capabilities.

1. **`OwlFatherInvader`** — the current high-water mark (13 of 16). Bespoke combo table with per-step
   easing, V2 clips, landing-timed slam, hand-tuned arcs, both linger windows. **Start here.**
2. **`StuddedLeatherWarrior`** — the composite-arm and V2-clip A/B pilot; the cleanest small file to
   read. Its `Axe` table is the best-authored archetype table in the repo (per-combo `MoveBrake`,
   mixed `SwingEaseStyle`s). Copy the *table* even for non-axes.
3. **`Gwyn`** — the only two-handed composite swing, and the only aim-adaptive greatsword. Copy from
   here for great-weapon posing specifically.
4. **`Artorias`** — deepest bespoke *attack set*, but on the legacy animation path (no bespoke combo
   table, no V2 clips, no aim adaptation).

Everything at or below `Blaidd` in the table is on the base path and will look noticeably stiffer.

---

## Upgrade ladder

Cheapest-first. Each step is independent; stop wherever the enemy is good enough.

1. **`MirrorMeleeSwingRotationByFacing`** — one line, fixes a reversed blade. Always do this first.
2. **`UseSwingEasing` + `UseLogicalMeleeTelegraphs`** — two lines, removes the constant-velocity sweep
   and the telegraph→swing snap. Biggest quality-per-character in the file.
3. **`MeleeRecoveryLingerTicks` / `MeleeComboInterStepLingerTicks`** — two numbers, adds weight and
   removes the inter-step snap. Greatswords want ~30/~15; light weapons ~6.
4. **`UseCompositeArmSwing`** — the arm stops being a flipbook. Verify the grip visually.
5. **`MeleeComboPoolOverride`** — give the enemy its own table. This is where "hand-tuned swing arcs"
   actually lives: per-step `Ease`, `SwingSpeedMult`, `MoveBrake`, `PostStepPause`, `ReachMult`.
   Model it on `WeaponArchetypeTables.Axe`, not on `Greatsword`/`Broadsword` (see below).
6. **`ModifyMeleeArcEndpoints`** — retune the arc extremes to the actual sprite.
7. **`RuntimeV2Clip`** — port the signature swings onto authored clips.

### Known content gaps (not per-enemy — fix once, everyone benefits)

- **The shared `Greatsword` and `Broadsword` archetype tables author zero per-step `ease:` and zero
  `MoveBrake`** — they predate both features. Only the `Axe` table was written after them. Every
  greatsword puppet without a `pool` override inherits that flatness, which is why they feel stiffer
  than the axe users regardless of their own flags.
- **`UseAimCenteredSwing` defaults on only for `WeaponArchetype.Axe`.** No other archetype has been
  piloted. Enabling it elsewhere needs an arc-endpoint re-check (`MaxAimPitch` clamps the arm).

---

## Gotcha: defaults

Do not audit this by grepping for an identifier. Several axes resolve differently depending on
archetype, and an explicit `=> false` is a deliberate opt-out that reads identically to an opt-in
under a naive grep:

- `UseAimCenteredSwing` → `true` for `Axe`, `false` otherwise.
- `UseAuthoredComboSwingClock` → follows `AimSwingActive`, so `true` for `Axe`.
- `Gwyn` and `OwlFatherInvader` set `UseAlternateFlip => false` **on purpose**; `Artorias` has simply
  never set it. Same grep hit, opposite meaning.
- Poise is not per-file: puppets missing from `PopulatePoiseProfiles()` get a boss/non-boss fallback
  in `GlobalNPC.SetDefaults`, so absence from that table is usually fine, not a gap.

The generator script resolves all of these. Trust it over a grep.

---

## Evaluating a swing (not just its flags)

The matrix says whether a feature is *enabled*. To judge whether a swing actually reads well:

```
# in game
/swingarm log            # start telemetry, fight, then /swingarm log again to stop

# out of game
powershell -File .agents/tools/swing-analyze.ps1 -Attack Cinderfall -Top 3
```

It picks up the newest `tsorcRevamp-puppet-attack-*.jsonl` from the tModLoader Logs folder and writes a
chart per attack run into `tsorcDocs/SwingReports/`:

- **A** — onion-skin blade arc in NPC-local space, one blade line per frame, coloured windup / active
  hitbox / recovery. A gap between the grey and red fans is a telegraph→swing snap.
- **B/C/D** — blade angle, angular velocity, angular acceleration. Spikes in D are visible snapping.

Angle comes from the logged hand→tip geometry, not the raw rotation field, so it already folds in
facing, mirroring and every draw-time offset — it is what the player sees.

Metrics printed alongside: windup/active/recovery in ticks and ms, active fraction, net and total
sweep, peak angular velocity and *when* in the swing it peaks, max jerk and spike count, end-hold, and
facing flips. The reference bands are Souls-melee discussion heuristics, not rules — a boss built to
feel deliberately wrong will fail them, and that can be the right call.

**Coverage caveat:** a frame is only logged when the weapon is actually drawn (`RecordRender` sits
inside the weapon draw path), so `IsWeaponVisiblePhase` bounds what the log can see. Phases that hide
the weapon produce no samples at all.

### Previewing a swing without launching the game

`.agents/tools/SwingPreview` runs the mod's **real** swing maths headless and writes the same
telemetry schema, so `swing-analyze.ps1` renders a previewed swing exactly like a logged one.

```bash
dotnet build tsorcRevamp.csproj -t:Compile                      # refresh obj\ (works with tML open)
cd .agents/tools/SwingPreview && dotnet build
dotnet run -- --list                                            # every archetype table and its steps
dotnet run -- --archetype Greatsword --combo "Heavy Chop" --compare-eases --overshoot 0.18
dotnet run -- --motion OverheadArc --telegraph 45 --attack 26 --ease Whip   # prototype a new move
```

It references `tsorcRevamp.dll` rather than re-implementing anything, so `SwingEase`, the archetype
tables and `WeaponArchetypeTables.SwingArcEndpoints` cannot drift from the game — which is the
failure mode the VFX preview harness documents for its hand-ported HLSL.

**Reference the assembly in `obj\Debug\net8.0`, not `bin\`.** `-t:Compile` is the build that works
while tModLoader is open, and it only writes `obj\`; `bin\` still holds whatever the last full build
produced, so pointing at it silently previews stale code.

`--compare-eases` emits the same swing once per `SwingEaseStyle`, which is the fastest way to pick a
curve: on Heavy Chop it showed Whip as the only style with zero jerk spikes, and Trapezoidal
producing a ~112°/tick single-frame jump at that tick budget (see `SwingEase.ApplyTrapezoidal`).

**What it cannot show:** sprites, the composite arm pose, terrain, AI decisions, hit registration.
It previews the motion curve and its timing. Still playtest before shipping.
