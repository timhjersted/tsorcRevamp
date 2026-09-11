---
name: puppet-swing-tuning
description: Preview, measure and tune a PuppetNPC melee swing offline — arc shape, easing, telegraph/recovery timing, follow-through hold, armor-sheet layout and composite-arm sprite geometry — without launching the game. Use when working on any puppet boss's swings or animation, when a swing "feels janky / stiff / snappy / floaty", when authoring or changing SwingEaseStyle values or combo step timings, when a shoulder or arm renders wrong mid-swing, when player skin shows through a puppet's armor, when authoring or scaffolding armor sheets for a puppet, or when asked to render/see/judge an attack animation.
---

# Puppet swing tuning

Two offline tools remove the five-minute game round-trip: `SwingPreview` runs the mod's **real**
swing maths headless and renders the puppet's whole body per tick; `swing-analyze.ps1` turns the
telemetry into charts plus a metrics verdict. Both live in `.agents/tools/`, both write to
`tsorcDocs/SwingReports/`.

## 1. Build, then run

`SwingPreview` references the mod DLL, so the mod must be compiled first. **`-t:Compile` writes to
`obj/`, not `bin/`** — and it works while tModLoader is open, which a full build does not.

```bash
dotnet build tsorcRevamp.csproj -t:Compile
cd .agents/tools/SwingPreview && dotnet build
```

```bash
dotnet run -- --list                      # every combo the harness can see
dotnet run -- --archetype Greatsword --combo "Heavy Chop" --body --zoom 3
dotnet run -- --puppet Gwyn --combo "Wrath Flurry" --body --linger 14
dotnet run -- --motion OverheadArc --compare-eases       # prototype a move that does not exist yet
```

`--body` adds the rendered animation: a grid sheet, a labelled contact sheet, and a self-contained
scrubbable HTML player. Add `--airborne` for the jump pose. Then chart it:

```bash
powershell -File .agents/tools/swing-analyze.ps1 -LogFile "$env:TEMP\swing-preview.jsonl" -Top 4
```

## 2. `--archetype` and `--puppet` read DIFFERENT tables

`--archetype` reads the shared `WeaponArchetypeTables`. A boss with its own moveset keeps it in a
**private static `MeleeCombo[]` on its own class**, exposed only through the *protected*
`MeleeComboPoolOverride` — unreachable across the assembly boundary, so `Options.TableFor` reflects
any static `MeleeCombo[]` off a `PuppetNPC` subclass instead. Reach those with `--puppet <ClassName>`.

Gwyn's twelve combos are invisible to `--archetype` entirely. `--list` shows both kinds.

## 3. The gate that silently makes every ease inert

```csharp
// PuppetNPC.cs — swing angle
return UseAuthoredComboSwingClock ? SwingEase.Apply(a0, a1, t, step.Ease) : <plain lerp>;
```

`UseAuthoredComboSwingClock` defaults to `AimSwingActive` (`UseAimCenteredSwing && AimSwingMasterEnable`).
**A puppet that does not set one of those ignores `step.Ease` completely** — you can author a full
per-step ease pass, compile clean, and change nothing. Artorias and Gwyn override it to `true`.

Turning it on also selects the swing clock, but that is usually a no-op: `GetMeleeSwingTicks(n)`
returns `n` unchanged for any positive `n`, and a step with no `SwingSpeedMult` falls to `1f`, so
both branches land on `step.AttackTicks`. Check those two things before assuming it is free.

**Also check the step helper takes an ease at all.** Gwyn's local `GS(...)` had no `ease` parameter,
so all twelve combos silently used `SwingEaseStyle.Linear` — enum value 0, the uninitialized default.
The shared `S(...)` in `MeleeComboSystem` defaults to `Smooth`; a bespoke helper should match it.

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
choice: measured on Gwyn, 57°/18t scored 3.8 and clean, 132°/22t scored 8.5, 132°/16t scored 9.2.
`Linear` scores a perfect **0.0 jerk by construction** — constant velocity has zero acceleration
anywhere — which makes the worst-looking option win the metric. If a swing spikes under *every*
ease, the step is too short for the arc it covers; lengthen `AttackTicks` instead of re-easing.

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

`MeleeRecoveryLingerTicks` holds the finished pose for N ticks after the swing, keeps the blade
drawn for exactly that long, then eases back to the carry angle and lets the rest of recovery play as
the ordinary unarmed idle. **It defaults to 0**, which cuts the weapon on recovery frame one.

Current values: Artorias 30, Gwyn 14, StuddedLeatherWarrior 6, OwlFatherInvader 6, everyone else 0.

## 7. Where the preview is NOT the game

Say so when reporting results; a preview that quietly flatters is worse than none.

- **`--linger` is not read from the puppet.** `MeleeRecoveryLingerTicks` is protected, so the harness
  defaults to 0 like the base class and you must pass the boss's real value by hand.
- **`Spin` reports 0° sweep.** The angle extraction does not model it. Not a game bug.
- **Shoulder cells are fixed** (`ColFrontShoulder=0, RowShoulder=1`) rather than vanilla's per-frame
  `pt`/`pt2`.
- **Weapon anchor is approximate** — grip point, origin and offsets are a calibration knob here.
- Skin is never drawn; vanilla player skin ships as packed `.xnb`.

Anything the preview cannot see — draw layer order against other mods, lighting, dust, shader passes,
actual hitbox registration — still needs the game.
