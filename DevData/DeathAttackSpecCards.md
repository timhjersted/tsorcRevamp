# Death Attack Spec Cards

Developer reference for the current Death / Absolute Death implementation. Values are taken from
`NPCs/Bosses/DeathBossBase.cs`, `NPCs/Bosses/Death.cs`, and `NPCs/Special/TrueDeath.cs`.
No in-game verification has been recorded here yet.

`DeathSpawnWarning` is the pre-spawn controller for the large FlamingScythe only.

## Shared Rules

- Death and Absolute Death share `DeathBossBase`; Absolute Death overrides damage, movement, and
  projectile density per card.
- The containment ring is intentionally boss-following during neutral phase-two play. It is anchored
  to the Reaper formation center during strong move 2.
- When the containment ring appears, Death locks the world to midnight (`Main.time = 16240`).
- The containment ring draws a deep-purple `DominionFieldEven` shader fog outside its boundary;
  it keeps the cloud movement but omits the outward-increasing depth ramp. Players found
  outside the fully formed ring call `OnPlayerOutsideContainmentRing(player, distanceOutside)`.
- Players outside the fully formed ring gain 1 Blight buildup every 2 ticks.
- Direct Death body contact applies the standard 15 Blight buildup.
- Projectiles that ignore tiles do so deliberately so the arena boundary, not terrain, controls the
  fight.
- The large flaming scythe uses a reduced 0.8x raw-texture hitbox because its visual scale is much
  larger than the intended collision size.
- Death Reaper uses the vanilla Reaper sprite as a placeholder and keeps frame 0 as a static
  presentation.
- Death Reaper is damageable. Normal Death Reapers have 1200 HP, 30 defense, and 74 contact
  damage; Absolute Death Reapers have 33333 HP, 66 defense, and 2222 contact damage.
- Reaper HP and contact damage use the normal Expert (2x) and Master (3x) difficulty multipliers.
- Death Reaper always faces the target horizontally, matching the Death boss rather than rotating
  its sprite around the formation.
- Death Reaper facing supports `Player` and `IntentDirection` modes. Strong moves 2 and 3 use the
  intended firing direction; the player-facing mode remains available for later moves.
- `DeathSickleWeapon` is the held melee scythe. Its source art is rotated 45 degrees
  counter-clockwise, rendered at 7.2x scale, and uses the bottom handle point `(46, 95)` as its rotation origin.
- TrueDeath-held sickles use an additional 1.25x scale multiplier.
- Its custom two-rectangle collision is resolved manually; NPC default square contact damage is disabled.
- Held-sickle contact uses GiantScytheDamage rather than the smaller sickle projectile damage.
- When Death dies, every active Death Reaper is cleared immediately.
- Large-scythe warning beams are orange (`new Color(255, 120, 35)`); their lengths are 1800 px
  for weak move 2, 1500 px for weak move 3, and 1800 px for weak move 4.
- Death and Absolute Death use small dark body dust. Death is dark purple; Absolute Death is
  dark red. The body glow activates when the containment ring appears.

## Phase One

### PhaseOneSickleRain

- Weapon/prop: vanilla Death Sickle projectile placeholder.
- Tell: 15-tick opacity/speed fade-in on each sickle; no separate spawn telegraph.
- Projectile: DeathSickleProjectile; 5 px/tick default speed; 30-tick fade-in; 8-second lifetime;
  15-tick fade-in and 15-tick fade-out; damage is disabled during fade-in/out.
- Birth/life/death: randomly spawned 500-750 px from the target; aimed once; no homing; no tile
  collision; `OnKill` dust burst.
- Count/interval: 1 per 60 ticks for normal Death, 1 per 20 ticks for Absolute Death.
- Deliberate exception: passes through tiles.

### PhaseOneSpecialDash

- Tell: stop and spin for 60 ticks while facing the target.
- Movement: 3 dash steps; 20 px/tick dash speed; 60 ticks per dash; 15-tick gap; deceleration
  starts at tick 45.
- Projectile: DeathBolt. Base Death fires no bolt during this move; Absolute Death fires 7 bolts in
  a 15-degree fan at 30 px/tick.
- Projectile lifecycle: 30-tick acceleration; 5.5-second lifetime; final 30 ticks fade and cannot
  damage; no spawn warning.

## Phase Two

### PhaseTwoWeak1

- Movement: chase at 3 px/tick while facing the target.
- Random pressure: DeathSickleProjectile every 60 ticks for normal Death, every 20 ticks for
  Absolute Death.
- Body shot: DeathBolt aimed at the target. Speed 20 px/tick. Interval scales linearly from 60
  ticks at full health to 20 ticks near zero health.
- Bolt: direct spawn; no pre-spawn warning.
- Duration: 6 seconds.

### PhaseTwoStrong1FiveDash

- Tell: strong-move roar, stop, and spin for 30 ticks.
- Movement: 5 dashes; 40 ticks each; normal Death has a 15-tick gap and Absolute Death has none.
- Speed: normal Death 20 px/tick; Absolute Death 36 px/tick.
- Body shots: normal Death fires 1 DeathBolt every 10 ticks during a dash; Absolute Death fires 3
  DeathBolts in a 15-degree fan at 30 px/tick.
- Arena: the containment ring is anchored for the whole dash sequence and smoothly returns to Death over 60 ticks at the end.
- Bolt: direct spawn; no pre-spawn warning.
- Deliberate exception: the dash direction is locked on launch; the boss does not continue facing
  the target during the dash body.

### PhaseTwoStrong2ReaperFormation

- Tell: strong-move roar; Reapers fade in over 60 ticks.
- Reaper movement: formation at radius 300 px, idle to 90 ticks, orbit from 90-330 ticks, freeze
  from 330-420 ticks, spiral from 420-540 ticks, fade out from 540-600 ticks.
- Reaper count: normal Death uses 6; Absolute Death uses 9.
- Reaper attack: both variants currently use DeathSickleProjectile.
- Orbit volleys: normal Death fires 4 shots every 60 ticks; Absolute Death keeps 3 shots every 30 ticks.
- Freeze and spiral volleys: both variants fire a 2-sickle, 20-degree fan. Normal Death fires every
  10 ticks; Absolute Death fires every 5 ticks.
- Body bolts: direct spawn; no pre-spawn warning.
- Body shots: normal Death begins firing only after orbit ends at tick 330; Absolute Death begins
  at tick 120. Body shots stop at tick 540; interval 60 ticks.
- Body shot pattern: normal Death fires 2 bolts with 60-degree spacing; Absolute Death fires 5
  bolts with 45-degree spacing.
- Arena: containment ring is anchored to the formation center for this move.
- Duration: 10 seconds.

### PhaseTwoStrong3ScatterReapers

- Tell: strong-move roar; 6 Reapers split into inner and outer rings.
- Currently excluded from the random strong-move bag. The implementation remains available for manual use.
- Formation: inner radius 70 px; outer radius 120 px; Reapers arrive during the first second.
- Attack window: from tick 120/150 until tick 600; Reapers fire sickle fans and perform short
  turn bursts.
- Normal Death: inner Reapers start at tick 120, outer at tick 150; Reaper fans use 3 sickles at
  15 degrees; body bolts are disabled.
- Absolute Death: Reaper sickle speed 18 px/tick; body bolts are enabled; body bolt ring uses 6
  bolts at 60-degree spacing.
- Fade: Reapers fade from tick 600 to tick 660.
- Duration: 11 seconds.

### PhaseTwoWeak2FlamingScythe

- Movement: stationary and continuously facing the target.
- Tell: warning beams track for 120 ticks and lock for the final 30 ticks.
- Warning beams are 1800 px long and orange.
- Pre-spawn warning: flaming scythes gather dust from tick 126 and release at tick 150.
- Throw: at tick 150, the first warning releases the vanilla Death Sickle use sound and all warnings release their flaming scythes.
- Normal Death: 3 scythes, 15-degree adjacent spread.
- Absolute Death: 12 scythes evenly distributed around the full circle.
- Scythe movement: 800 px outbound over 60 ticks, then returns over 60 ticks; projectile rotates at
  18 degrees/tick; infinite penetration; 0.8x raw-texture hitbox; 8-tick fade-in and 10-tick
  fade-out; damage is disabled during both fades.
- Duration: 300 ticks.

### PhaseTwoWeak3StraightScythes

- Warning: normal Death creates 6 fixed beams 1500 px behind it, spaced 600 px apart.
- Warning beams are 1500 px long and orange.
- Phantom scythes lead the release by 90 ticks.
- Absolute Death also creates 5 reverse beams 1500 px in front, spaced 600 px apart.
- Timing: 60 ticks of warning, 30 ticks of weapon spin, release at tick 90.
- Projectile: one giant flaming scythe per beam in straight mode; speed 30 px/tick.
- Straight mode: 20-tick fade-in, 10-tick fade-out, damage disabled during both.
- Duration: 120 ticks.

### PhaseTwoWeak4InwardScythes

- Warning beams are 1800 px long and orange.
- Placement: warnings are created on the 1500 px boundary and point inward.
- Normal Death: six evenly distributed directions, one volley.
- Absolute Death: twelve evenly distributed directions, four volleys one second apart.
- Projectile: straight-mode DeathFlamingScythe at 30 px/tick.

### PhaseTwoWeak5SickleRush

- Ring: anchored when the move starts, then smoothly returns to Death during the finisher.
- Death teleports 600 px behind and below the player's facing direction, 45 degrees from straight back.
- Sickle: end-pivot mode, 30-tick fade-in, 15-tick 45-degree wind-up, 45-tick hold.
- Warning: a fixed 1200 px beam appears for 60 ticks before the rush.
- Rush: three full rotations over 2000 px. One rotation matches one 16-tick swing animation cycle.
- Finisher: sickle completes its half rotation over 8 ticks, while the ring returns smoothly over 90 ticks.
- During the 90-tick recovery, Death uses smooth acceleration/deceleration to return to the player-orbit circle and settle there.
- Sickle damage is disabled during fade-in and fade-out.

### PhaseTwoStrong4SickleDash

- Tell: strong-move roar, ring anchored, sickle fades in over 60 ticks on a 100 px orbit.
- Sickle uses middle-pivot mode while orbiting and dashing.
- Dashes: 6 dashes, 60 ticks each, 18-tick gaps, locked player aim, smoothed 900-1600 px travel.
- Rotation: random clockwise/counter-clockwise per dash; gaps preserve damped motion.
- Body: Death uses the existing player-orbit motion behavior.
- Strong move 4 currently fires no bolt and no warning beam.
- Exit: 30-tick sickle fade-out, ring return, then a 90-tick wait.

## Open Verification Items

- Record in-game close/typical/max-range checks for each active move.
- Confirm the warning-line and large-scythe visuals at both normal and Absolute Death scales.
- Confirm multiplayer late-join behavior for DeathReaper local timers and warning-beam age.
