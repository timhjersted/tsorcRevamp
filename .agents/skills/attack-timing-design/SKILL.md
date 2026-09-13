---
name: attack-timing-design
description: Design a new PuppetNPC melee attack or combo - or rework an existing one - so it has weight, nuance and fair counterplay from the first build. Covers the player's counterplay budget (22-tick roll i-frames, 30-tick roll cooldown, 40-tick post-hit immunity, stamina per roll) and the fairness rules derived from it, the arc-size policy (default ~180° live sweep / ~220° envelope, matching the player's broadsword), the timing sheet (arc poses, wind-up, strike speed curve, follow-through, hit window, pauses, recovery), chaining swings without snaps, dodge-through re-facing, whiff-triggered gap-closers, leap finishers and punish-window recoveries, a checklist for writing or reviewing a moveset proposal, plus the preview measurements that prove it before playtest. Use when designing, adding, rebuilding or retuning any puppet boss/invader/enemy melee move or combo, when evaluating an attack or moveset proposal, when checking whether an attack is fair/dodgeable/reactable, when a move feels weightless, floaty, stiff, snappy, janky or too fast/slow, when swings are too narrow or end too soon, or when asked to bring an old move up to the Wrath Flurry standard.
---

# Attack timing design

A recipe for melee moves that feel heavy and readable, distilled from rebuilding Gwyn's **Wrath
Flurry** (`NPCs/Bosses/SuperHardMode/Gwyn.cs`, the reference implementation). Use it for new moves
and to audit old ones.

**Related skills:**
- Start with `enemy-upgrade-audit` to see what the enemy already has.
- Use `enemy-redesign` for the moveset, state machine and stats.
- Use `puppet-swing-tuning` for how the SwingPreview tool and the combo engine work: the three ease
  gates, V2 clips, and where the preview is not the game.
- Use `attack-quality-pass` for everything that isn't swing timing: projectile lifecycles,
  telegraph props and dust, grips, reach feasibility, move variety, copies and VFX. Its spec card
  and checklist apply to every attack, melee included.

This skill is the design layer on top of those.

## 1. Weight is contrast

Every move is four beats. Each gets its own ticks, and **the strike must be the fastest thing in
the move**:

| Beat | Job | Wrath Flurry value |
|---|---|---|
| **Tell** | Anticipation the player can read and react to | 44t: 25 easing up into the cocked pose, 19 dropping to the start |
| **Strike** | Short acceleration into a fast sweep | 10t ease-in, peak ~19°/tick |
| **Follow-through** | The blade shedding momentum; harmless | 45t exponential decay, near-still for the last half |
| **Recovery** | The punish window | 14t planted landing beat, then sheathed slow walk for 180t |

These are the failure modes we hit, and what they looked like in `--profile`:
- **No contrast.** Everything ran at 9–10°/tick, or the Smooth ease stopped dead.
- **Constant speed.** Linear gives 0 jerk and looks the worst.
- **Snaps between phases.** A 100°+ `first` jump into a step.
- **A creeping blade that still deals damage.** Armed ticks extended through a long, slow settle.

## 2. The player's counterplay budget

Fairness is arithmetic against these numbers. Every counterplay claim ("roll through it", "rolling
away gets caught", "punishes early rolls") must be checked against them, not asserted. All are read
from code; re-check the sources if a number looks off.

| Fact | Value | Source |
|---|---|---|
| **Roll i-frames** | **22 ticks**: the whole roll (`DodgeTimeMax` 0.37 s re-asserts `Player.immune` every roll tick). Gear lengthens the immunity to ~27 (`DodgeImmuneTime` 18 base; Chloranthy +3/+6, Hollow Soldier +3/+3). Design against 22, sanity-check 27. | `Players/tsorcRevampPlayerDodgeRoll.cs` |
| **Roll travel** | 8 px/tick for the first 11 ticks, then decelerating: about 100–120 px | same |
| **Roll cooldown** | **30 ticks counted from the roll's START**: `dodgeCooldown` is an absolute `Timer` (`GameUpdateCount + 30`) set at roll entry, and a new roll also can't start mid-roll. So the next roll is possible **30 ticks after the last one started**, leaving an **8-tick unprotected gap** (ticks 22–30) with no i-frames and no roll. Gear: Chloranthy Ring 1 → 10 and Ring 2 → 0 (so the 22-tick roll becomes the limit); Icebound Mythril Aegis 35; Burden of Smough +10; Hollow Soldier Agility −20 (−22 on ground). | same, roll entry ~line 522 |
| **Roll input buffer** | A roll pressed up to 15 ticks early (`QueueDodgeroll` 0.25 s) fires the instant the cooldown ends, so an immediate re-roll at tick 30 needs no reaction time | same |
| **Roll cost** | 30 stamina (× tired multiplier). Endgame max is ~200, so about 6 rolls from full. A hit taken in stamina debt applies Stagger. | same, `tsorcRevampPlayerStamina.cs` |
| **Perfect dodge** | Rolling within 12 frames before a hit that would have connected refunds half the roll's stamina | `PerfectDodgeWindow` |
| **Post-hit immunity** | **40 ticks** after any hit (80 with Cross Necklace-type `longInvince`). Puppet blade hits spawn a hostile hitbox, so they use this general cooldown. | vanilla `Player.Hurt` (`TerrariaDecompiled`) |
| **Jump** | No i-frames. It clears low sweeps and ground waves, not overheads. | — |
| **Reaction time** | ~12–15 ticks (200–250 ms) to see a tell start and press a key | — |

**Rules that follow:**
1. **A live window under 22 ticks is fully rollable** by a well-timed roll. That is the Souls
   standard, so every normal swing should be.
2. **"Rolling away gets caught" needs two things:** the hit must still be live when the roll's
   i-frames end, so live > 22 ticks counted from the roll, and the hitbox must travel at least as
   far as the roll (~110 px). A 16-tick lunge fails both, so a roll away timed with it always
   escapes. What a normal lunge *can* catch is a roll made during the tell, because its dash is
   aimed at release.
3. **The post-roll gap trap.** A roll protects ticks 0–22 from its start, and the next roll is
   available at tick 30, so every roll leaves an 8-tick unprotected gap. The latest a player can
   roll a hit is at the end of its live window, so that late roller's gap sits ~22–30 ticks after
   the live window ENDS. A follow-up that goes live less than ~30 ticks after the previous live
   window ends can land in someone's gap. Every such follow-up must be one of:
   - conditional on a hit or on the player's position (`ShouldContinueMeleeCombo` sees
     `previousStepHit`; check position there, before the pause re-faces);
   - avoidable without a roll (jumpable, or out-spaceable);
   - ≥ 30 ticks after the previous live window ends.

   Wrath Flurry's live windows end 19 ticks into each swipe and the next goes live 65 ticks after
   the last started, 46 after it ended, so each swipe is separately rollable.
4. **Punishing an early roll is narrow, and only fair with a clear bait** (a distinct pose or flash).
   The roll's own i-frames cover the first 22 ticks, and a buffered re-roll is ready at 30. So the
   real hit only catches the early roll if it goes live inside that roll's 22–30 gap, 22–30 ticks
   after the bait provoked it. Any later and the player can simply roll again; the bait then only
   costs them 30 stamina per roll, which is a legitimate but weaker "tax", not a punish.
5. **Hits under 40 ticks apart can't both damage the same player:** if the first connects, post-hit
   immunity absorbs the second. Budget a combo's damage per hit accordingly. A fast two-hit meant
   to deal both hits on connect must space them ≥ 40 ticks apart.
6. **Count the rolls.** If a player standing their ground needs more than about 4–5 rolls to
   survive a combo, it must also be escapable by spacing, jumping or disengaging. Otherwise it's a
   stamina check, not a skill check.
7. **Tells** need at least ~20 ticks on screen for a close-range light move (reaction plus input),
   and 40+ for a heavy.

## 3. Fill in the timing sheet BEFORE writing code

Write it as a comment block above the combo, as Wrath Flurry does:

```
Poses:    start ____ rad -> end ____ rad  (envelope ___°; live ≈ 0.8 x envelope = ___°)
          aim bias ±0.35 added on top; identity reason if live < ~170°: ___
Tell:     ___t ON SCREEN = max(MinComboTelegraphTicks, authored x ComboTelegraphMultiplier)
          (settle-into-cock ___t via LogicalWindupSettleFraction)
Strike:   EaseInTicks ___  cruise ___  EaseOutTicks ___  decay k ___  -> peak ___°/t
Live:     armed while speed >= 30% of peak -> HitWindowEnd ___   (___t; < 22 = rollable)
Open:     tail ___t + recovery ___t = ___t punish window after the blade goes harmless
Chain:    PostStepPause ___  step linger ___  endpoints shared? y/n
          next live starts ___t after this live window ENDS (>= 30, or conditional: ___)
Move:     travels ___px = speed ___px/t x ___t   (combo push is constant TopSpeed x mult)
Select:   band ___  RangedStartOnly? ___  distance/HP gates ___  cooldown ___t
React:    re-face in pauses (automatic) / gap-close after ___ whiffs / leap strike range ___px
Counter:  roll through ___ / roll away ___ / jump ___ / spacing ___   rolls needed: ___
Finish:   finisher ___   recovery ___t (landing beat ___t, then sheathe? walk speed ___)
Reach:    hitbox ComboReachBase*0.7*ReachMult = ___px vs visual grip->tip = ___px
FX:       projectile / VFX spawn tick ___ (peak = tick EaseInTicks), crossing time ___t
```

### Poses (swing space; 0 = the sprite's resting 45° diagonal; world angle = rotation - 45°)
- `-1.62` = blade past vertical behind the head, the cocked pose.
- `1.61` = low-forward, 47° below level.
- `-0.30` = `HoldRotation`, the carry.
- **`[-1.85, 1.40]` is NOT a comfort limit.** It is only the V2 aim-correction guard band
  (`PuppetAttackRuntime.UpdateAim`), which stops target-height aiming from pushing an already-wide
  clip into a pose that folds the arm through the torso. It clamps nothing authored. Legacy motions
  already go past it: GroundSlam 1.5, Wrath Flurry 1.61, landing-timed impact ~2.1.
- The real warning sign is visual: the shoulder, elbow or hand crossing the torso at an extreme
  pose. It's most likely behind the head (below about −1.7) with upward aim bias. Check it in a
  `--body` render.
- Remember the ±0.35 aim bias (`UseAimAdaptiveArc`), computed once at combo start.

### Arc size: default to a 180° LIVE sweep

Narrow swings that "end too soon" were a recurring problem. Keep three measurements apart:

| Measure | What it is | Broadsword default |
|---|---|---|
| **Live sweep** | Blade travel while armed | **~180°** |
| **Pose envelope** | Start pose to end pose: what `ModifyMeleeArcEndpoints` sets | ~205–225° |
| **Total travel** | Envelope plus the tell's movement (dip, raise, reversals) | Can exceed 225°; that's fine |

**The player's baseline** is `QuickSlashMeleeAnimation.MeleeSwingRotation`.
- The swing runs from aim − 112.5° to aim + 90°: a 202.5° envelope. In puppet swing space that's
  about −1.18 → +2.36, so it ends pointing straight down.
- Damage is gated until 10% of the animation time (`itemAnimation ≤ 0.9 × max`). By then the curve
  has covered 10% of the arc, so **~182° is live**.
- The player's hit is an aimed 90° sector (`CheckRectangleVsArcCollision`), not the swept blade.
  Treat the player's arc as the visual language to match, not as a collision model.

**Converting live sweep to envelope.** With the §3 hit-window rule (armed while speed ≥ 30% of
peak), the armed part is about **80% of the envelope** for every curve we use: 0.80–0.82 across
10/45 k7, 7/26 k7, 8/32 k6 and 6/30 k8. So:

```
envelope ≈ live sweep / 0.8        180° live -> ~224° envelope;   185° envelope -> only ~149° live
```

**Wrath Flurry's 185° swipes are only live for about 149°.** It was tuned by envelope before this
rule existed. Widening it means extending the END pose.

**Arc policy for sword puppets** (live sweep):

| Kind | Live | Envelope |
|---|---|---|
| Ordinary committed cut | 175–185° | ~220–230° |
| Technical combo cut (vary the direction) | 170–185° | ~215–230° |
| Compact / fast swipe, *as a deliberate identity choice* | 155–170° | ~195–215° |
| Signature heavy | 185–200° | ~230–250° |

The tell's back-cock adds another 25–40° of harmless motion on top.

**Where to put the extra degrees: extend the END, not the start.** The player's own arc ends
straight down (~2.36), and follow-through below level reads naturally. Starting further back than
about −1.7 is where the arm folds through the torso. Narrow a move only because its identity calls
for a shorter cut, never to stay inside the V2 guard band.

### Strike speed: solve it, don't guess it
`SwingEaseStyle.Weighted` has a cubic ease-in, an optional constant cruise, and an exponential
decay. Top speed for a fixed sweep:

```
v = sweep / (in/3 + cruise + out * (1 - e^-k) / k)          (deg/tick if sweep is in degrees)
```

| 185° sweep, in / cruise / out, k | peak °/tick |
|---|---|
| 10 / 0 / 45, k 6 | 17.1 |
| **10 / 0 / 45, k 7 (Wrath Flurry)** | **19.0** |
| 10 / 0 / 45, k 8 | 20.7 |
| 6 / 0 / 45, k 7 | 22.0 |
| 10 / 0 / 25, k 7 | 26.8 |

Peak speed scales linearly with the envelope: the default ~224° envelope (180° live) at 10/0/45 k7
peaks at 23.0°/t.

Long ramps cap the peak: a cruise stretch *lowers* it, and a longer or softer ease-out lowers it.
To get a faster strike, raise k or shorten the ease-in. Adding cruise ticks won't do it.

### Hit window
Speed is `v·e^(-k·p)` through the ease-out, so it stays above 30% of peak for `out · ln(1/0.3) / k`
ticks. Set:

```
HitWindowEnd = (in + cruise + out * 1.204 / k) / AttackTicks
```

Wrath Flurry's value is 0.32. Gwyn's `OnMeleeComboAttackTick` also stops feeding the fire slash past
it, so no VFX advertises a dead blade.

## 4. Chaining rules

- **Share endpoints.** Each step should start exactly where the previous one ended. Alternating
  underhand/overhand between the same two poses does this for free. Overhead-into-overhead needs a
  re-raise, and that re-raise is a snap.
- **Hold through pauses.** At `MeleeComboInterStepLingerTicks = 0` the pause drifts toward a
  fraction of the *outgoing* arc, and the next step then snaps. That's what Gwyn's other combos do:
  103–184° first-frame jumps. Set a linger (3 is enough when endpoints are shared) so every pause
  is a pure hold.
- **Dodge-through re-facing is free.** Facing is locked while a step is live. `MeleeComboPause`
  re-faces the player and recaptures the lock before the next step. So a roll through the boss
  costs exactly the current swing, which is the Souls rule. The pause must be ≥ 1 tick; Wrath
  Flurry uses 10.
- **The last step goes straight to recovery**, and only step 1 gets a telegraph. Later steps start
  from the pause.

## 5. Scope per-combo tuning to the combo

Most of these knobs are per-puppet virtuals. Key them on the live combo, and **always also check
the phase**: `ActiveMeleeComboName` is never cleared, so a stale name would leak into later one-shot
swings.

```csharp
bool WrathFlurrySwinging =>
    ActiveMeleeComboName == WrathFlurryName
    && (Phase == AttackPhase.MeleeComboTelegraph
        || Phase == AttackPhase.MeleeComboAttack
        || Phase == AttackPhase.MeleeComboPause);
```

| Knob | What it shapes |
|---|---|
| `ModifyMeleeArcEndpoints(motion, ref a0, ref a1)` | The arc poses |
| `MeleeComboInterStepLingerTicks` | Hold in the pauses |
| `LogicalWindupSettleFraction` | How long the tell eases into the cocked pose |
| `UseLandingTimedLeapSlam` + `LeapSlamCarryRotation` / `LeapSlamImpactRotation` | Leap carry pose and slam |
| Step `LeapStrikeRange` | Swing mid-air once past the apex with the target in range (swept hit, no second hit on landing) |
| `WeaponSheathed` | Put the weapon away mid-recovery: hides it AND drops the arm pose |
| `RunMovementAI(speedMult)` | Slow walk during a recovery |
| `ShouldContinueMeleeCombo` (sees `previousStepHit`) + `ModifyNextMeleeComboStep` | Reactive swaps, e.g. whiff streak → leap |

Write one small helper per step type when a combo repeats it; `FlurrySwipe` and `FlurryLeap` are
the pattern. Keep every number in named constants at the top of the tuning block.

## 6. Engine gates: authored values silently do nothing unless

Read `puppet-swing-tuning` §3 for the detail. The short version:

- `UseAuthoredComboSwingClock` must be on. Without it, `Ease` is ignored and `Weighted` is not
  applied.
- The motion must read `Ease`. **Spin never does**: it is a fixed 0.28 rad/tick, and its recovery
  rewinds nearly a full turn.
- Only `IsArcSwingMotion` steps get the authored sweep clock. **JoustDash, leaps and the others
  sweep over the weapon's `useAnimation`**, and get cut off early when that exceeds `AttackTicks`.
- A `RuntimeV2Clip` combo ignores its steps entirely. Tune the clip instead.
  `PuppetAttackClip.SwingEase` has no `Weighted` (it falls back to Smooth), so a move that needs
  this curve must be a legacy MeleeCombo.

The preview prints all of this per step, so read its readout before believing anything.

## 7. Workflow

1. **Audit** with `enemy-upgrade-audit`, then run the readout for the puppet.
2. **Fill in the timing sheet** (§3). Solve the peak speed and the hit window, and check every
   counterplay claim against §2.
3. **Implement**: named constants, a step helper, and combo-scoped overrides (§5). Compile with
   `dotnet build tsorcRevamp.csproj -t:Compile`.
4. **Measure** in `.agents/tools/SwingPreview`, after building it with `dotnet build`:
   ```bash
   dotnet run -- --puppet Gwyn --combo "Wrath Flurry" --profile          # numbers
   dotnet run -- --puppet Gwyn --combo "Wrath Flurry" --body --zoom 2    # HTML player, send it
   dotnet run -- --puppet Gwyn --combo "Wrath Flurry" --roll-through 4 --body
   ```
   Pass criteria in `--profile`:
   - Every Attack row's `first` is ≲ 10°.
   - Pause rows sweep 0°.
   - The strike peak is clearly the fastest thing in the move.
   - The deg/tick row shows a short ramp, a peak, and a long decaying tail.
   - Armed ticks end where the tail gets slow.
   - Recovery never sweeps > 180°. If it does, that's the Spin rewind.
5. **Hand off** with the render and the list of things the preview can't show: real leap airtime,
   in-range timing, walking, and hit registration.

## 8. Auditing an existing move

Run `dotnet run -- --puppet <Class> --profile` and look for:

| Readout symptom | Fix |
|---|---|
| Attack row `first` 50–185° | Share endpoints, add a step linger |
| Pause row sweeping tens of degrees | Linger 0 drifting toward the outgoing arc; add a linger |
| `reaches t=0.4x before the step ends` | Non-arc motion on `useAnimation`; `AttackTicks` ≥ it, or change the motion |
| `authored X ignored` | A gate is off; see §6 |
| Heavy weapon peaking < ~12°/t with no tail | Convert to `Weighted`; solve the peak |
| Armed for a long, slow tail | Add `HitWindowEnd` |
| Attack row sweep × 0.8 well under ~170° (swing "ends too soon") | Widen the envelope toward ~220°, extending the END pose first (§3 arc policy) |
| Recovery sweeping ~340° | Spin rewind: wrap the angle or use an angle lerp |
| V2 clip | Tune the clip's windup/active/recovery/ease, or convert it to a MeleeCombo if it needs `Weighted` |

## 9. Also check

- **Hitbox vs visual reach.**
  - The visual reach is grip to farthest texture corner: `MeleeHandleNorm` × texture size ×
    `MeleeWeaponDrawScale`.
  - The hitbox reach is `ComboReachBase × 0.7 × ReachMult`.
  - Gwyn at draw scale 1.0: 117px visual vs 96px hitbox. Keep them within ~10%, or the sword hits
    air or misses what it visibly cuts.
- **Grips.** Judge grips only from renders made on or after 2026-09-11; earlier preview renders
  pinned the texture corner, not `MeleeHandleNorm`.
- **Length.** Wrath Flurry runs 12.4s. A long commit needs the punish window at the end, and a
  gap-closer so a retreating player can't simply wait it out.

## 10. Writing or reviewing a moveset proposal

Use this whenever a moveset arrives as a table, whether from another AI, a design doc, or the user,
and whenever you write one yourself. A proposal that only says "tell / live / recovery" is a
starting point, not a design. Before anything is built, every move must have a complete §3 timing
sheet and pass the checks below.

### Per-move checks
- **Ticks are on-screen ticks.** Authored telegraphs are multiplied: the default is
  `max(30, tel × 1.35)`, so "24t" plays as 32. Either set the puppet's `ComboTelegraphMultiplier`
  to 1 and `MinComboTelegraphTicks` to ~20, or author `tel = desired / multiplier`.
- **Only step 1 telegraphs, and always for at least `MinComboTelegraphTicks`.** A move whose first
  motion IS the tell (a backstep, a feint, a sprint) still gets that telegraph in front of it. Keep
  the minimum short for such combos, or the reveal is spoiled.
- **All four beats are present.** "Live" is only the armed window. Ask for the tail (the harmless
  follow-through) and state the full punish window as tail + recovery.
- **Arcs are stated as live sweep AND envelope,** defaulting to ~180° live (~220° envelope). Anything
  under ~170° live needs a stated identity reason. Don't narrow an arc to respect the `[-1.85, 1.40]`
  V2 guard band; it isn't a limit for authored swings (§3).
- **Movement is feasible.** Combo push is a constant `TopSpeed × ForwardPushMult` for the whole
  step, which is about 3 px/tick on a typical knight. Work out `px = speed × ticks`. A lunge that
  "closes 7–14 tiles" needs a distance-solved dash: leaps have one (`BeginLeapAttack` solves the
  horizontal speed); a ground dash doesn't exist yet. Constant speed also has no contrast: a dash
  should accelerate and decay like a swing.
- **Motion and engine path:**
  - `Weighted` needs a legacy MeleeCombo with the authored clock, not a V2 clip.
  - JoustDash and leaps sweep over the weapon's `useAnimation`, not `AttackTicks`.
  - Spin ignores `Ease`.
- **Selection:** a move meant to start at mid-range must be `RangedStartOnly`, with a
  `CanSelectMeleeCombo` distance gate. Combos otherwise start only inside `MeleeEngageRange`, so the
  move would only ever fire at point-blank. That's how Gwyn's gap-closers ended up with 8% of picks.
  HP-gated moves go in `CanSelectMeleeCombo` too.
- **Every counterplay claim is proved against §2.** Write out the roll timeline, measured from the
  roll's start: i-frames end at 22, the next roll is available at 30 (from the START, not the end),
  and each live window falls somewhere on that line. Check "roll away gets caught" against rule 2,
  every follow-up against the rule 3 gap, early-roll punishes against rule 4, and multi-hit damage
  against rule 5.
- **Reactive branches name their exact condition and where it's evaluated.**
  `ShouldContinueMeleeCombo` runs at step end, before the pause re-faces the player, so "in front
  and close" is checkable there. `ModifyNextMeleeComboStep` runs at the pause's end.
- **Projectiles and VFX are tied to a beat.** Spawn on the peak tick (tick `EaseInTicks`), not at
  mid-step, which on a `Weighted` curve is already the harmless tail. A projectile must cross the
  player within the 22-tick roll to be rollable.
- **Poise:** say where hyper armor is on (heavy tells and strikes) and where it's off (recoveries).
- **Reach:** the hitbox matches the visible blade (§9).

### Kit-level checks
- **Every distance band has an answer:** close, mid and long. Don't delete an enemy's only ranged
  option in a melee rework; the Dark Knight proposal dropped its storm-wave spell, which leaves a
  player beyond 14 tiles free to kite it forever.
- **Rhythm:** tells and recoveries should spread out (Gwyn's clips run 20/26/34/44 and 12/16/40/48)
  rather than cluster. The heaviest tell should front the fastest strike.
- **Pose compatibility:** note which moves end where the next can start (an overhead ends low, an
  underhand starts low), so the AI's picks can flow into each other.
- **The identity move is deterministic.** A reactive dodge or feint triggers on a readable
  condition (for example, 2 hits taken within 60 ticks), not a dice roll.
- **Tier pacing:** a normal enemy gets shorter tails and fewer long commits than a boss. Save
  multi-second strings and 180-tick recoveries for bosses.

### What the Dark Knight review caught (2026-09-11)
GPT's six-move table had good bones: varied tells, stated counterplay, a hit-gated string, and an
HP-gated heavy. These were its failures, in the order to check for them:
1. No follow-through beat.
2. Tells written as authored, not on-screen.
3. A 7–14-tile lunge on a constant 3 px/tick push.
4. "Rolling away is caught" claimed for a 16-tick window.
5. An unconditional follow-up inside the post-roll gap: the return cut went live ~23 ticks after
   the first cut's live window ended, so a late roller couldn't roll again. Its own hit-or-close
   condition was the fix, but it was stated as flavour, not as the requirement it is.
6. A backstep "tell" preceded by a mandatory telegraph.
7. VFX timed to mid-step.
8. No ranged answer.

The first review of that table also got one thing wrong, which is worth knowing when you check your
own work. It said False Retreat's early-roll punish "works" on the assumption that the roll cooldown
ran 30 ticks *after* the roll ended. It runs from the roll's start. An early roller at the backstep
has a buffered re-roll ready at 30, 10 ticks before the slash goes live at ~40. So as written, False
Retreat only taxes stamina; to actually punish, the slash must go live 22–30 ticks after the
backstep provokes the roll. Read the source for every number in §2 rather than recalling it.

It also set the light cuts to 160°, by treating the `[-1.85, 1.40]` V2 aim guard band as a
composite-arm comfort limit. It's neither. The default is a ~180° live sweep, ~220° envelope (§3
arc policy); 160° is a deliberate compact-cut choice, not a default.
