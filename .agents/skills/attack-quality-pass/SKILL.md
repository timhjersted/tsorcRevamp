---
name: attack-quality-pass
description: Quality-control standard for every enemy/boss attack that is not just a sword swing - projectiles, staff/magic casts, thrown weapons, lunges and leaps, summoned copies/phantoms, arena mechanics, player grabs/throws and attack VFX. Covers the full projectile lifecycle (spawn telegraph, travel dusts/rotation/animation, deliberate death with kill effects - never silently vanishing), telegraph standards (weapon out early, raise/aim pose, projectile forming at the tip, telegraph lines, colour-matched dusts), held-prop correctness (right weapon per attack, grip point, fire from the tip, aim tracking), reach and range feasibility, density and pacing, move-selection variety, spawned copies, player-affecting effects, shader/VFX polish, renames, and a per-attack spec card plus pre-handoff checklist. Use when designing, building, reworking, reviewing or handing off ANY boss or enemy attack, projectile, telegraph, staff cast, summon, clone or arena effect; when writing or evaluating an attack proposal; or when an attack looks cheap, unfinished, placeholder-y, pops in/out, misses, lands short, or is rarely seen.
---

# Attack quality pass

Almost every piece of attack feedback in this project falls into the categories below: a
projectile that pops in and vanishes, a staff that isn't in the hand, a leap that lands short, an
attack nobody ever sees. None of them are hard to get right. They get missed because nobody checked
for them.

**Treat this as the definition of done for an attack.** Fill in the spec card (§12) before
building, and run the checklist (§13) before handing off. If a requirement is deliberately broken
(a projectile that passes through walls and flies off screen, say), write the choice down in a
code comment and in the handoff. An undocumented exception reads as a bug.

**Related skills:**
- Melee swing timing, arcs and fairness maths (roll i-frames etc.) live in `attack-timing-design`.
  This skill does not repeat them.
- Dust IDs, counts and layering: `vfx-dust-tips`.
- Shaders: `vfx-pipeline` and `vfx-shader-tips`, which include `ShaderPreview`.
- Sword render and grip: `puppet-swing-tuning`.
- Whole moveset redesign, state machines and stats: `enemy-redesign`.

## 1. Projectile lifecycle: birth, life, death

Every projectile has three authored phases. Missing any of them is the most common complaint.

### Birth: never pop into existence
- **Telegraph the spawn point.** Dust converging on or forming at the exact spawn position for a
  stated number of ticks before the projectile appears. 30–40 ticks is the usual range. The dust
  colour matches the projectile's sprite.
- **Weapon-fired shots form at the weapon tip.** The real projectile, or a preview of it, appears at
  the tip, follows the tip until release, and ideally grows from small to full scale over the
  charge. It is released from the tip, never from `NPC.Center`.
- **Spawn distance is a fairness value.** A projectile that spawns around the player must spawn far
  enough away that `distance / speed ≥ ~20 ticks` of reaction time (see `attack-timing-design` §2).
  "Spawns too close" is a design bug, not a tuning nit.

### Life: it must look like it's doing something
- **Orientation.** State which side of the sprite leads, then set rotation to match:
  `Projectile.rotation = velocity.ToRotation() + <sprite offset>`. For an animated sheet, record
  which edge of each frame faces the target. For example: "BlueWisp: bottom of frame faces the
  player".
- **Animation.** A multi-frame sheet needs `Main.projFrames[Type]` and a frame counter. Check the
  frame count against the PNG height.
- **Spin.** Orbs, stars and shards usually want inner rotation independent of their travel
  direction. Set the spin direction deliberately; mirrored pairs spin in mirrored directions.
- **Travel dust.** A few non-gravity dusts per N ticks, tinted to the sprite. Sparse is fine;
  none reads as placeholder.
- **Speed.** Something the player can read, stated in px/tick and checked against the arena size.

### Death: every ending is authored
| How it ends | What it must do |
|---|---|
| Hits a tile | `tileCollide = true`; `OnTileCollide` returns true (or bounces, deliberately) → kill effect |
| Hits the player | Kill effect, unless it's deliberately piercing |
| Lifetime runs out on screen | **Fade out** over 10–20 ticks (alpha or scale) plus dust. Never vanish at full opacity |
| Deliberately passes through walls or leaves the screen | `tileCollide = false`, enough `timeLeft` to clear the screen, and a comment saying it's deliberate |

- **Kill effects live in `OnKill`,** so every death path (tile, player, timeout, being cleared)
  plays them. The feedback standard for a "real" projectile is a burst of ~70 dusts in its colour.
- **"Travel until it hits something"** is the default for aimed shots. A projectile that stops or
  disappears mid-air before reaching a wall or the player is the classic failure.
- **Children.** A projectile that spawns more on impact (shards, a return bolt) gives each child
  the same full lifecycle and states its damage explicitly.

Netcode: spawn projectiles only when `Main.netMode != NetmodeID.MultiplayerClient`. Spawn dust only
when `!Main.dedServ`.

## 2. Telegraph standards

- **The weapon is out before the telegraph starts.** Show the weapon about 30 ticks before the
  tell begins, so it isn't conjured on frame one of the attack.
- **Every attack has a tell, with its own pose.** A staff cast raises or aims the staff (for
  example, raised to 45° for a lob, pointed at the player for an aimed shot). A tell made only of a
  timer is not a tell.
- **Build-up dust** at the tip or spawn point, colour-matched. For multi-shot sequences it continues
  through the whole sequence, not just the first shot.
- **Aimed shots get telegraph lines** when the direction matters. Don't drop an existing aim line in
  a rework; that's a regression.
- **Reuse proven telegraphs** before inventing one. If a boss already has a good tell for the same
  kind of attack (Juggernaut's flame-breath dust, for example), copy that code, positioned on this
  attacker's hand.
- **No borrowed props.** A bomb sprite as a staff telegraph, or a spinning meteor staff, is
  placeholder behaviour. The telegraph uses the actual weapon for the attack.
- **An attack's effect must be observable.** A stance or buff with no visible result ("Riposte
  stance doesn't seem to do anything") needs both a tell and a visible payoff. If you can't say what
  the player should see, the attack isn't designed yet.

## 3. Held weapons and props

- **The right weapon per attack.** Sword attacks show the sword; staff attacks show the staff. Check
  every phase of a multi-phase attack: leaps, somersaults and recoveries inherit whatever was last
  set. `SetDisplayWeapon` and the per-phase held item are where this goes wrong.
- **Grip point is measured, not guessed.** Find the prop's butt and tip pixels in the PNG, then pick
  the grip as a fraction along that line. Staves are held about 30% up from the butt, not near the
  gem. For puppets that fraction becomes `MagicGripNorm` (magic), `MeleeHandleNorm` (melee) or
  `SpearBaseNorm`/`SpearHeadNorm` (spears).
- **"In hand" is verified from a render, not from code.** A prop drawn 16–20px off the hand is the
  typical symptom of a wrong grip norm or a wrong hand anchor. For melee use the `SwingPreview --body`
  render. For magic and ranged props, which the preview doesn't draw yet, take an in-game screenshot
  and use the puppet debug overlay (`DebugHandPos`, `DebugOrigin`). Report the measured pixel error.
- **Fire from the tip.** The tip position is the hand plus the weapon direction times the
  grip-to-tip distance at draw scale. Every projectile and every tip dust uses that same position.
- **Aim tracks the shot.** A sweeping volley (for example, 6–12 shots across 180°) rotates the prop
  to each shot's angle as it fires.
- **Hitbox matches the visual** for any weapon that deals damage (see `attack-timing-design` §9).

## 4. Reach and range feasibility

"Lands short of the player" and "doesn't reach" are feasibility failures, not tuning. **State the
intended reach, then prove it at three distances: close, typical, and the stated maximum.**

- **Leaps and jumps:** check that the launch solver can reach the maximum distance. `BeginLeapAttack`
  caps horizontal speed, so a "reach the player from 40 tiles" requirement (640px) needs the cap,
  the height or the airtime raised, or the attack gated to its real range.
- **Lunges and dashes:** combo push is a constant `TopSpeed × mult`, which is short. A lunge meant
  to pierce needs a distance-solved dash that aims to **overshoot** the target (for example 200px
  past), so an on-time hit is guaranteed and the player has to dodge rather than wait.
- **Lobs and arcs:** solve the launch velocity for gravity so shells land on the player *and* beyond
  them (for example 50–100px past). Aiming at the player's current position makes most shots fall
  short.
- **Returning weapons:** state the out-distance (for example "at least 300px past the target")
  before the return.
- **Ground-travelling effects** (lightning along the floor, waves) continue for a stated distance or
  until a solid tile, whichever comes first.
- **Grabs and homing:** "reach at any distance" means speed scales with distance, up to a stated cap.
- **Anti-whiff:** don't start a melee attack or a copy's swing when the target is clearly out of
  reach; close the distance first (for example with a running jump sized to the needed trajectory).

## 5. Density, spread and pacing

- **Counts match the fantasy.** A "raining shotgun lob" is ~30 shells, not 4. When a rework lowers
  a count, say why.
- **Spread** is stated as a total width in px or an angle in degrees, and checked against the
  player's movement options.
- **Sequences have steps:** appear with dust, pause, then act. State every interval in ticks (for
  example "12 shots, 5-tick delay", "3-shot fan, 60-tick pause between fans"). "Happens way too fast"
  means an interval was 0.
- **Speed changes are explicit percentages** of the current value (for example "25% slower, starts
  25% farther away").
- **Fades:** appear and disappear over stated ticks (for example 15-tick fade-out, 90-tick ghost
  fade-in). Nothing pops.
- **Multiplayer:** a per-player attack (a fan aimed at the player) repeats for every valid player,
  not just `NPC.target`.

## 6. Move-selection variety

"Some moves I rarely see" and "too many repeats" are selection-system bugs.

- **Use a bag, not independent rolls.** Deal attacks from a shuffled bag without replacement and
  refill when it's empty; never allow an immediate repeat. Reference: `ChooseFromBag` in
  `NPCs/RedKnightAttackController.cs`. Weighted rolls with per-attack cooldowns still starve
  low-weight moves.
- **Gates narrow the bag; they don't replace it.** Range, health-phase and cooldown gates filter
  which attacks are eligible, then the bag picks. An attack whose gate is almost never true is
  effectively cut; check the gate against real fight distances.
- **Phase changes are explicit:** say which attacks become more frequent after 50% HP, and how
  (weight, extra bag entries, shorter cooldown).
- **Verify with counts.** Log picks over a long fight, or simulate N picks. Every attack should
  appear within roughly one bag-length of the last time it appeared.

## 7. Spawned copies, phantoms and summoned bosses

For ghost copies of the boss, soul-summoned past bosses, or echo clones:
- **Arrival:** fade in with themed dust over a stated time, then a telegraph delay (for example 90
  ticks) before acting.
- **Grounded:** spawn on the ground or snap to it. A copy stuck mid-air, drifting down, is the known
  failure.
- **Movement is the source's real movement AI**, not a simplified float. If the copy is "Artorias,
  melee only", it moves exactly like Artorias.
- **Its attacks are the source's attacks,** with the same anti-whiff range awareness as normal NPCs.
  Attack speed for copies is stated, not left at the default.
- **Exit:** when done, walk away and fade out with dust over a stated time.
- **Owner behaviour while a copy is active:** state it. For example: the owner idles 120 ticks, then
  releases its attack lock.
- **Frequency by phase** is stated.
- **Reference implementations:** the puppet echo-step system (`EchoStep*` virtuals in
  `PuppetNPC.cs`) and Artorias's spectral copies. Read them first, and fix their known issues in
  the copy you build rather than inheriting them.

## 8. Effects on the player

- **Throws, flicks and knockback respect tiles.** Apply velocity that solid tiles stop, and don't
  push the player through blocks.
- **Timing matches the visual.** Launch at the moment the effect shows it (for example 20 ticks into
  an explosion), with a stated velocity.
- **Tethers and pulls:** state the attach duration (for example 30 ticks) and the pull target
  ("into sword range", a distance in px).
- **Debuffs** have a duration tied to the attack and a visual on the player.
- **Enemy i-frames and blinking:** an attack's landing shouldn't make the *enemy* immune or blink
  unless that's the design. `DodgeTimer`-style immunity should never leak into a normal landing
  frame.

## 9. Shader and VFX polish

- **No visible primitive shapes.** A solid circle, rectangle or straight quad edge reads as
  unfinished. Mask with noise and falloff (`vfx-shader-tips`), and preview it in `ShaderPreview`
  before building.
- **Pixel filter** on new shaders, matching the mod's style.
- **Opacity changes via draw colour or alpha first** (for example 50% less opaque), before touching
  the `.fx`; that avoids a shader rebuild.
- **Remove leftovers.** A secondary trail or glow nobody asked for is noise; make each layer earn
  its place.
- **Layering is a requirement:** state what draws behind tiles, over the player, or behind a
  projectile (for example a silhouette behind an orb).
- **VFX attach to their source:** a slash follows the actual blade position, size and duration. Half
  the sword's size, or ending early, is a bug.
- **Size and hitbox relation** is stated, for example "explosion visual overlaps 25% outside the
  100px hitbox".
- **Reuse** an existing effect (explosion, silhouette, debuff haze) when the feedback names one.

## 10. Arena and boss-state mechanics

- **Anchor to the spawn point,** not the boss's current position, for rings and zones. A moving
  anchor makes "safe" unlearnable.
- **Timed changes restore:** a ring that shrinks states its shrink speed, hold time (for example 12
  seconds) and restore. It never stays shrunk for the rest of a phase by accident.
- **Mirrored effects:** if a boundary effect animates on one side of a ring, say whether the other
  side mirrors it.
- **Boss hygiene:** bosses shouldn't inherit ordinary-enemy behaviour such as fleeing to heal or
  drinking Estus. Disable it explicitly (Gwyn: `EstusChargesMax => 0`).

## 11. Renames and assets

- **A rename is a set of changes:**
  - the display name in every `Localization/*.hjson` (en-US, ru-RU, zh-Hans);
  - the class and file renamed together with `git mv`, plus any matching `.png`;
  - every reference, including combo `Name` strings compared elsewhere;
  - the debug attack labels.
- **No placeholder art ships silently.** A copied sprite carries a
  `// PLACEHOLDER sprite (copy of X)` comment and is listed in the handoff (CLAUDE.md convention).
- **A new sprite's metadata is recorded where it's used:** frame count, frame size, facing
  direction, and which edge leads.

## 12. Attack spec card: fill in per attack before building

A proposal or plan without this is incomplete. Leave nothing blank; write `n/a` or `deliberate: …`.

```
Attack:        name (display) / enum or combo name / file
Weapon/prop:   sprite ___  shown ___t before tell  grip norm ___ (measured butt/tip px ___)
Tell:          pose ___  duration ___t  dust ___ (colour-matched)  aim line y/n  sound ___
Projectile:    sprite ___ frames ___ leading edge ___  rotation/spin ___  scale ___
  birth:       where ___  pre-spawn dust ___t  forms at tip y/n  grows ___t
  life:        speed ___px/t  count ___  interval ___t  spread ___°/___px  trail dust ___  homing ___
  death:       tile ___  player ___  timeout (fade ___t) ___  kill effect ___  pass-through? deliberate: ___
  children:    ___ (full lifecycle each)
Reach:         intended ___px; proven at close ___ / typical ___ / max ___   overshoot ___px
Player effect: throw/pull/debuff ___  tile-stopped y/n  timing ___t  velocity ___
Copies:        arrival ___ delay ___ grounded y/n  movement AI ___ exit ___ owner does ___
VFX/shader:    layers ___ draw order ___ pixel filter y/n  opacity ___ previewed y/n
Selection:     gates ___  bag entries/weight ___  phase change ___
Fairness:      reaction time ___t  rollable/jumpable ___  (attack-timing-design §2)
Multiplayer:   per-player y/n
Damage:        ___ (const, relative to tier)
```

## 13. Pre-handoff checklist

Run it in game with the debug HUD attack label on. Watch each attack at least twice: once standing
still, once at max range.

- [ ] Weapon correct and in hand for every phase (screenshot); projectiles leave from the tip
- [ ] Tell has a pose and colour-matched dust; the weapon appears before the tell
- [ ] No projectile pops in: spawn telegraph or tip-forming visible
- [ ] No projectile pops out: every death path shows its kill effect or a fade
- [ ] Rotation, animation and leading edge are correct in both facing directions
- [ ] Reach proven at close, typical and max distance; nothing lands short
- [ ] Counts, intervals, spread and speeds match the spec card
- [ ] Every multi-player targeting case handled
- [ ] Copies grounded, move like the source, fade in and out
- [ ] Player throws stopped by tiles; debuff visuals present
- [ ] Shaders show no primitive shapes, have a pixel filter, and correct layering
- [ ] Selection: every attack seen within a bag-length over a long fight
- [ ] Renames complete (localization, `git mv`, references); placeholders listed
- [ ] Melee parts carry the `SwingPreview --profile` numbers and a `--body` render (`attack-timing-design`)

Say plainly in the handoff which items were verified in game, which were only compiled, and which
exceptions are deliberate.
