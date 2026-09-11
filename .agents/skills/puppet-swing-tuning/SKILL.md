---
name: puppet-swing-tuning
description: Preview, measure and tune a PuppetNPC melee swing offline — arc shape, easing, telegraph/recovery timing, follow-through hold, armor-sheet layout and composite-arm sprite geometry — without launching the game. Use when working on any puppet boss's swings or animation, when a swing "feels janky / stiff / snappy / floaty", when authoring or changing SwingEaseStyle values or combo step timings, when a shoulder or arm renders wrong mid-swing, when player skin shows through a puppet's armor, when authoring or scaffolding armor sheets for a puppet, or when asked to render/see/judge an attack animation.
---

# Puppet swing tuning

Two offline tools remove the five-minute game round-trip: `SwingPreview` runs the mod's **real**
swing maths headless and renders the puppet's whole body per tick; `swing-analyze.ps1` turns the
telemetry into charts plus a metrics verdict. Both live in `.agents/tools/`, both write to
`tsorcDocs/SwingReports/`.

This skill is the tool and engine reference. For **designing or retuning a move** (the timing
sheet, speed-curve maths, chaining rules and audit table), use `attack-timing-design`, which drives
this tool. Its main instrument is `--profile`: per-phase sweep, peak °/tick, the first-frame jump
into each phase, armed ticks, and the deg/tick of every frame.

## 1. Build, then run

`SwingPreview` references the mod DLL, so the mod must be compiled first. **`-t:Compile` writes to
`obj/`, not `bin/`** — and it works while tModLoader is open, which a full build does not.

```bash
dotnet build tsorcRevamp.csproj -t:Compile
cd .agents/tools/SwingPreview && dotnet build
```

```bash
dotnet run -- --list                                     # every combo the harness can see
dotnet run -- --puppet Gwyn                              # readout of all 12 combos, no rendering
dotnet run -- --puppet Gwyn --combo "Wrath Flurry" --body                  # the chained combo, animated
dotnet run -- --puppet Gwyn --combo "Wrath Flurry" --step-linger 3 --body  # try a between-hits hold
dotnet run -- --puppet Gwyn --combo "Wrath Flurry" --roll-through 4 --body # player rolls behind before swipe 4
dotnet run -- --archetype Greatsword --combo "Heavy Chop" --compare-eases --clock on
dotnet run -- --motion OverheadArc --compare-eases       # prototype a move that does not exist yet
```

A table combo renders **chained, the way the game plays it**: one telegraph, then each step straight
out of the previous step's pause, then one recovery. `--per-step` gives the old isolated view (each
step with its own telegraph and recovery). Clean per-arc metrics come from that view, but the game
never plays it.

`--body` adds the rendered animation: a grid sheet, a labelled contact sheet, and a self-contained
scrubbable HTML player. The player has a phase timeline: windup, swing, armed swing, pause and
recovery, with a white tick at each step boundary. Click the timeline to seek. Add `--airborne` for
the jump pose. Then chart it:

```bash
powershell -File .agents/tools/swing-analyze.ps1 -LogFile "$env:TEMP\swing-preview.jsonl" -Top 4
```

## 2. The puppet's real flags are read from the mod, not passed by hand

`--puppet <ClassName>` builds a `PuppetProfile` (`PuppetProfile.cs`) by reflecting getters off an
**uninitialized** instance of that class (no constructor, no `SetDefaults`). It reads:

- the pool (`MeleeComboPoolOverride`, else the archetype table);
- the three gate inputs (`UseAuthoredComboSwingClock`, `UseSwingEasing`, `AimSwingActive`);
- `UseLogicalMeleeTelegraphs`, `OverheadWindupOvershoot`;
- the telegraph formula (`MinComboTelegraphTicks`, `ComboTelegraphMultiplier`);
- both lingers and `MeleeRecoveryTicks`;
- `ModifyMeleeArcEndpoints` and `CustomizeMeleeCombo` (so Artorias's widened arcs, 30-tick pauses and
  Ground Pound retarget all show up);
- the private consts `HoldRotation` / `DefaultWeaponAnimMax`;
- the weapon's `useAnimation`, found by scanning the IL of the `MeleeWeaponItemType` getter and of
  the item's `SetDefaults`.

Before each combo, `PuppetProfile.Bind` writes that combo into the instance's `_activeMeleeCombo`
and sets `Phase = MeleeComboAttack`. That matters because bosses key per-combo tuning on
`ActiveMeleeComboName` plus the phase. For example, Gwyn's Wrath Flurry widens its arcs and holds
through its pauses, and without the bind the preview would read "no combo" and miss both. This is
also why the lingers print per run.

Every run prints this profile. Treat any `- ... base default used` line as a fidelity hole: that getter
needed live NPC state. `--archetype` alone uses the `PuppetNPC` base defaults, which means no clock,
Linear and no lingers. Combine it with `--puppet` to swing a generic table with a boss's flags.
`--clock`, `--step-linger`, `--linger`, `--useanim` and `--overshoot` override individual values.

## 3. Three gates decide whether `step.Ease` does anything

An ease you author can compile clean and change nothing. Check all three before believing it took.

**Gate 1 — the clock flag.** `ApplySwingEase` in `PuppetNPC.cs`:

```csharp
return UseAuthoredComboSwingClock ? SwingEase.Apply(a0, a1, t, step.Ease)
                                  : SwingEase.Apply(a0, a1, t, UseSwingEasing);  // true → Smooth, false → Linear
```

`UseAuthoredComboSwingClock` defaults to `AimSwingActive` (true for every Axe-archetype puppet). When
it is off, `step.Ease` is ignored and the puppet's **`UseSwingEasing`** decides. The fallback is
Smooth or Linear, *not* a plain lerp. Artorias and Gwyn override the flag to `true`.

**Gate 2 — the motion.** These motions route their attack through `ApplySwingEase` and read
`step.Ease`:
- `OverheadArc`, `UnderhandArc`, `HorizontalSweep`, `VerticalChop`
- `GroundSlam`, `IaidoDraw`, `JoustDash`, `LeapThrust`
- `LeapSlam`, unless `UseLandingTimedLeapSlam`

The middle four were routed on 2026-09-10; before that they ignored `step.Ease`. The motions that
never read it: `Spin` (a fixed +0.28 rad per tick in **every** phase, telegraph and pause included),
`Thrust`, and the carry/pose motions (`LowAxeRun`, `RisingUppercutLeap`, `ChargeChop`, `Feint`,
`BackstepRaise`, `ThrownWeaponRetrieve`).

**Gate 3 — V2 clips.** A combo with `RuntimeV2Clip` set is driven by its `PuppetAttackClip`;
`TickWeaponAnim` returns early and the combo steps never touch the blade. Their `Ease` values are dead
data; the clip's own `swingEase` is what plays. Gwyn's Cleave, Cinderfall, Guillotine and Backhand
Step are V2. The preview runs these through the mod's real `PuppetAttackRuntime`.

**The sweep clock is a separate trap.** The authored clock resizes the 0→1 sweep to
`AttackTicks / SwingSpeedMult`, but **only for `IsArcSwingMotion` steps**: the Overhead, Underhand,
HorizontalSweep, VerticalChop, GroundSlam, IaidoDraw and DoubleSpinSlam arcs. Every other motion,
and every motion when the clock is off, sweeps over the **weapon's `item.useAnimation`**, while the
step still ends after `AttackTicks`. When `useAnimation > AttackTicks`, the step ends partway through
its arc. Gwyn's 32-tick sword stops the Wrath Flurry JoustDash at t=0.44, Sliding Thrust at 0.50 and
the leaps at about 0.7. Artorias's JoustDash steps stop at 0.47–0.53. The run readout prints
`-> reaches t=…` whenever this happens.

> **How this was learned (2026-09-10):** the preview reported every Gwyn combo as `Linear` because
> `GS(...)` never set `Ease`, and I concluded the game swung Linear. It did not — `UseSwingEasing`
> made it Smooth all along. The preview then read `step.Ease` directly; it now models all three gates
> and prints the effective ease per step.

## 4. Reading the metrics

| Metric | What it actually tells you |
|---|---|
| `active fraction` | Souls melee sits ~8–20%. Much higher reads as a hitbox that lingers. |
| `sweep net/total` | Greatsword overhead wants ~150–200° net. Net ≪ total means the arc doubled back. |
| `peak ang. vel` **timing** | Heavy weapons peak late (50–70%). Early peaks read light and snappy. |
| `telegraph motion` | 0° travelled = no readable tell, regardless of how many ticks it lasts. |
| `telegraph handoff` | >6° jump on the first swing frame is a visible snap between phases. |
| `end hold` | 0 = follow-through cut off. See §6. |
| `recovery motion` | Fast/large = the follow-through is thrown away rather than settled. |

**Do not read `max jerk` as a quality score.** Jerk tracks **sweep density (deg/tick)**, not ease
choice: in the preview, 57°/18t scored 3.8 and clean, 132°/22t scored 8.5, 132°/16t scored 9.2 (the maths holds
for any curve, but two of those were V2-clip combos, so they describe the step data, not the game).
`Linear` scores a perfect **0.0 jerk by construction** — constant velocity has zero acceleration
anywhere — which makes the worst-looking option win the metric. If a swing spikes under *every*
ease, the step is too short for the arc it covers; lengthen `AttackTicks` instead of re-easing.

**Chained runs** span every step, so the analyzer's swing window runs from the first attack frame
to the last, pauses included. That makes the between-step handoffs count toward jerk and sweep:
useful for judging the chain, useless for judging a single arc. Use `--per-step` for per-arc numbers.
The number that matters for a chain is the **first-frame jump into each later step**. At step linger
0, Gwyn's Under-Over and 3-Hit overheads open with a 103–105° single-tick jump, and Roll-Catch's
GroundSlam with 184°. A 3-tick linger brings all three under 20°.

`SwingEaseStyle.Weighted` is authored in **ticks**. Set the step's `EaseInTicks` and `EaseOutTicks`
(the full-speed stretch is whatever remains of `AttackTicks`). It needs the authored clock.
- **The curve:** built like a real swing. A cubic ease-in (speed grows with t²), then an optional
  constant top speed, then an exponential decay onto the end pose. Velocity is continuous at both
  joins, unlike `Trapezoidal`, whose ramps are sized in degrees and jump to cruise speed.
- **The contrast knob:** `EaseOutDecay`, in 1/e-foldings over the ease-out (0 means 6). Higher values
  cover the fixed arc sooner, so the strike is faster and the settle stays near-still for longer.
- **The speed cap:** top speed v solves `v·(in/3 + cruise + out·(1−e^−k)/k) = sweep`. Gwyn's 185°
  swipes (10 in, 0 cruise, 45 out, k = 7) peak at 19°/tick and are down to 21% of that 10 ticks
  after the peak. The first pass (quadratic in, cubic out, 6-tick cruise) capped at 9.6°/tick;
  that was the "not enough contrast" complaint.
- **The hit window:** pair it with `HitWindowEnd`, the fraction of the attack phase after which the
  blade disarms. That way a slowly settling blade isn't a live hitbox. Gwyn keeps the blade armed
  while speed is at least 30% of peak: `ln(1/0.3)/k` of the ease-out.

`LogicalWindupSettleFraction` (virtual, default 0.25) sets how much of a logical wind-up is spent
easing from the carry pose up to the arc's far end before the drop to the attack start.

**Landing-timed leaps.** Opt in with `UseLandingTimedLeapSlam`; Artorias uses it globally, and Gwyn
only inside Wrath Flurry.
- The blade is carried at `LeapSlamCarryRotation` through the flight. It then SmoothSteps to
  `LeapSlamImpactRotation` over the 10 ticks before the projected landing.
- Both rotations are virtual, so a combo can carry its own cocked pose.
- A step's `LeapStrikeRange` > 0 starts the downswing in the air: once past the apex with the target
  that close, the blade sweeps for real and the landing adds no second hit.

`SwingEaseStyle`: `Linear` (0, usually accidental), `Smooth` (heavy, eases both ends), `Snap`
(front-loaded, lands early), `Whip` (back-loaded, holds the apex — right for baiting punishes),
`Trapezoidal` (**needs >55 ticks**, and reads real ticks rather than a 0–1 fraction).

## 5. Puppet armor sheets and composite-arm geometry

A puppet renders a real `Player` wearing armour items, so its art is ordinary tModLoader equip sheets:

| Sheet | Size | Layout |
|---|---|---|
| Head | `40×1120` | 20 vertical frames of `40×56` |
| Legs | `40×1120` | 20 vertical frames of `40×56` |
| **Body** | `360×224` | **9 × 4 grid** of `40×56` cells |

**Hide the player's skin**, or arms, hands and legs show through the armour on the puppet. Copy the
flags from `Items/Armors/LordGwynArmor.cs` / `LordGwynLeggings.cs`, which set them in
`SetStaticDefaults` against `EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body)` (or `.Legs`):

- Body: `ArmorIDs.Body.Sets.HidesTopSkin`, **`HidesArms`**, `HidesHands`
- Legs: `ArmorIDs.Legs.Sets.HidesTopSkin`, `HidesBottomSkin`

`HidesArms` matters most here: the composite arm is drawn from the armour's own arm cells, and without
it the player's skin arm renders underneath. `HidesTopSkin` exists on **both** `Body.Sets` and
`Legs.Sets` — set whichever the piece covers. The scaffolding tool below additionally sets
`ArmorIDs.Head.Sets.DrawHead = false` for full-face helms; no shipped armor in the mod uses it, and
Gwyn's helm renders correctly without it.

To scaffold a new set, `ModSources/SpriteTools/ArmorSpriteTool.ps1` — **outside this repo**, one level
up — generates correctly sized sheets, grid guide images and the `ModItem` classes with those flags
set. `-UseReferenceSheets -ReferencePrefix <existing set>` copies a working animated set so the output
has valid walk and arm-use frames on day one; repaint over it using the guides. It will not solve
side-facing frames from a front-facing concept — it produces validated targets for the art pass. It
also emits an `EnemySpriteRenderer` snippet; ignore it, that is the pre-`PuppetNPC` path and only
`RedKnightTest` still uses it.

### Torso and shoulder-cap cells

| Cell | Code (0-idx) | Sheet position (art tool) | Draw layer |
|---|---|---|---|
| Torso | `pt3 = (0,0)` | col 1, row 1 | Torso — **under** the front arm |
| Jump torso | `pt3 = (1,0)` | col 2, row 1 | Torso |
| **Front shoulder cap** | `pt2 = (0,1)` | **col 1, row 2** | ArmOverItem — **over** the front arm |
| Back shoulder cap | `pt = (1,1)` | col 2, row 2 | Skin, with the back arm |

**Where a pauldron is painted decides whether the arm covers it.** Painted into the torso cell, it sits
under the swinging arm. Painted into the front-cap cell, it sits over it — which is what you want, and
what `PuppetShoulderCapLayer` forces by pinning `compShoulderOverFrontArm = true`. Gwyn's sheet
(2026-09-10) has the pauldron in the **torso** cell (44 gold px at cell y22–29) and **both cap cells
empty**, so his arm covers it. The fix is pixel-only: move that block to col 1, row 2 at the same
coordinates. SwingPreview draws the front cap from exactly (0,1), last, so check it there first.

Cap cells are **hidden in the jump pose** (body row 5) unless the armour sets
`ArmorIDs.Body.Sets.showsShouldersWhileJumping[slot] = true` — the vanilla default is false for modded
armour. Without it, a pauldron moved into the cap cell vanishes mid-air.

### Composite-arm cells

`CreateCompositeFrameRect(pt)` = `Rectangle(pt.X*40, pt.Y*56, 40, 56)`.

| | Column (0-idx) | Column as art tools show it | Row |
|---|---|---|---|
| **Front arm** (holds the weapon) | `X = 7` | **8** | stretch |
| **Back arm** | `X = 8` | **9** | stretch |

Row is the stretch amount: `Full 0`, `ThreeQuarters 1`, `Quarter 2`, `None 3`. When a composite arm
is enabled the frame is **fully overridden** — whatever walk/jump frame was selected is discarded.

- Pivots in art space: front `(15, 28)`, back `(26, 30)`. Facing-independent, because vanilla flips
  the offset sign so the same texel stays the pivot.
- **The arm cells are gender-independent.** Vanilla's `if (!Male) { pt.Y += 2; pt2.Y += 2; pt3.Y += 2; }`
  shifts back shoulder, front shoulder and torso only. There is no female arm variant.
- **Shoulder pads belong in the shoulder cells, not the arm cells**, unless the puppet sets
  `SuppressCompositeShoulderCaps`. A pad baked into column 8 or 9 draws *in addition to* the
  separately-drawn cap — that is the classic "duplicate pauldron", and it also makes the arm art
  taller than the joint, which reads as a mispositioned pivot when it is not.

### Shoulder draw order flips mid-swing

`compShoulderOverFrontArm` is chosen from the **body frame ROW**, not the armour: rows **1, 2 and 5**
draw the arm OVER the shoulder cap; every other row draws it behind. `BodyRowFromWeaponRotation`
walks rows 1→4 as the weapon pitches down (pitch >0.95→1, >0.70→2, >0.30→3, else 4), and airborne is
row 5 — so an unfixed swing shows the arm in front of the pauldron for its high-pitch half.
`PuppetShoulderCapLayer` now forces the flag `true` for every puppet. Render the old behaviour with
`--vanilla-shoulder-order` to compare.

## 6. Follow-through and when the weapon stops drawing

`MeleeRecoveryLingerTicks` holds the finished pose for N ticks after the swing, then the blade eases
back to the carry angle (`Lerp` 0.10 per tick toward `HoldRotation` = -0.30). **It defaults to 0**,
which starts that ease on recovery frame one.

Whether the weapon is still **drawn** depends on the phase. `IsWeaponVisiblePhase` has listed
`MeleeComboRecovery` unconditionally since June 2026, so combo recoveries show the blade for the
whole recovery. Plain `MeleeRecovery` shows it for the whole recovery when linger > 0. Only the
bespoke recoveries (JumpSlash, Stab, Tendril, HomingVolley, …) stop drawing it once the hold ends.
Hiding the sword partway through a combo recovery would need a new rule; no setting does it today.

V2 clips ignore the linger entirely: their `Recovery` stage SmoothSteps from the end pose to
`RecoveryEndRotation` on the clip's own clock.

**Known defect, Spin recovery:** Spin accumulates rotation up to 2π, and the recovery ease is a plain
`Lerp` rather than an angle lerp. After Sunspin or the Wrath Flurry finisher, the blade therefore
**rewinds nearly a full turn backwards** to reach -0.30. The chained render shows it (Wrath Flurry
t119–161).

Current values: Artorias 30, Gwyn 14, StuddedLeatherWarrior 6, OwlFatherInvader 6, everyone else 0.
Between-step hold (`MeleeComboInterStepLingerTicks`): Artorias 15, StuddedLeatherWarrior 3,
OwlFatherInvader 3, everyone else (Gwyn included) 0.

## 7. Where the preview is NOT the game

Say so when reporting results; a preview that quietly flatters is worse than none.

What it models faithfully (fixed 2026-09-10; before that it got all of these wrong):
- the three ease gates;
- V2 clips, run through the real `PuppetAttackRuntime`;
- the chained timeline;
- the sweep clock;
- the telegraph formula, `HoldRotation`, both lingers, the per-motion pause drift, and `Spin`.

What still diverges:

- **The combo pose switch is a hand-port.** `Program.ComboPose` copies the combo branch of
  `PuppetNPC.TickWeaponAnim`, which is private and reads live NPC state. The same goes for
  `StepStartRotation` (`ComboStepStartRotation`) and `LogicalSwingWindup`. Edit the game's switch
  and re-sync these, or the preview drifts.
- **Runtime step swaps are not simulated.** `ModifyNextMeleeComboStep` and `ShouldContinueMeleeCombo`
  never run, so Wrath Flurry's two-whiff leap never appears in a render.
- **Leap landing is approximate.**
  - Landing-timed LeapSlam lands after flat-ground airtime, `2·LeapAttackUpSpeed/0.3` = 63 ticks.
  - An in-air `LeapStrikeRange` strike is shown at the latest it could happen, the last 10 ticks,
    because there is no player position.
  - The base carry and impact getters need `FrontHandWeapon`, so they fall back to a zero weapon
    `RotationOffset`; Gwyn's flurry overrides resolve exactly.
  - Other leaps run their full timer.
- **Other timers:** `LowAxeRun` also runs its full timer instead of ending on reach, and
  `ApexDiveCleave` shows only the ascent.
- **Aim is always level**: no aim bias, and V2 aim correction stays 0.
- **Facing and flip:** facing is right unless `--roll-through N` flips it in the pause before step
  N, as `MeleeComboPause` re-faces the target. A left-facing frame is rendered as a whole-body mirror
  of the right-facing pose. Per-layer mirroring got the weapon's rotate/mirror order wrong, and the
  sword pointed away from the arm; nobody noticed because nothing rendered left-facing until
  2026-09-11. `UseAlternateFlip` never flips.
  `ShouldContinueMeleeCombo` and `ModifyNextMeleeComboStep` are not called, so hit-confirm branches
  always continue.
- **Armed frames:** legacy steps are armed for the whole attack phase, as `DoComboMeleeHit` does.
  Leaps are armed on their last frame. Actual collision is not simulated.
- **Shoulder cells are fixed** (`ColFrontShoulder=0, RowShoulder=1`) rather than vanilla's per-frame
  `pt`/`pt2`.
- **The weapon draw now matches the game's** (fixed 2026-09-11). `MeleeHandleNorm` is the texel
  pinned to the hand, and `MeleeWeaponDrawScale` is the scale relative to the body. Both are read
  from the puppet, as `DrawWeaponToLayer` uses them, and the frame padding grows to fit the blade.
  Until then the preview pinned the texture's bottom-left corner (past the pommel) at a guessed 0.45
  scale. Every render looked like the puppet held the very end of its sword; the game never did.
  **Judge grips only from renders made after that date.** The hand position itself is still
  vanilla's `GetFrontHandPosition` maths.
- **Sheathed frames** (`WeaponSheathed`) draw no weapon and hang the arms straight down, standing in
  for the natural walk draw. The walking itself is not simulated.
- Skin is never drawn; vanilla player skin ships as packed `.xnb`.

Anything the preview cannot see — draw layer order against other mods, lighting, dust, shader passes,
actual hitbox registration — still needs the game.
