---
name: enemy-redesign
description: Rebuild a legacy tsorcRevamp enemy or boss into a Souls-style fight — bespoke attack state machine, telegraphs and recoveries, poise and hyper-armor, dust-driven spectacle, reusable projectile patterns, and stats picked from tier neighbours. Covers FighterAI ground enemies (the Gigas Method) and Invader/PuppetNPC bosses (the Gwyn Method), including the checklist for wiring a new puppet AttackPhase. Use when reworking, redesigning or adding attacks to any NPC or boss, designing a new enemy's moveset, choosing its stats, or when a new attack phase compiles but misbehaves.
---

# Enemy Redesign — the Gigas Method (+ the Gwyn Method for Invader bosses)

How to take a legacy tsorcRevamp enemy and rebuild it into a polished, Souls-style mini-boss with a
bespoke attack state machine, proper telegraphs/recoveries, hyper-armor rules, dust-driven spectacle,
and (optionally) a mirrored player weapon. **Part 1** = FighterAI ground enemies (the Gigas Method).
**Part 2** = full bosses on the Invader puppet system (the Gwyn Method). **Part 3** = cross-cutting
lessons that apply to everything. Distilled from the July 2026 overhaul push (both Gigases +
MinotaurMage + Necromancer + QuaraHydromancer + Parasprite + the Gwyn final-boss rebuild), so the next
rework starts from what those learned rather than re-deriving it. **When you do the next rework, add
what you learn to Part 3.**

Companion data in this folder: [EnemyStatInventory.md](EnemyStatInventory.md) — spawn-time stats for
every NPC, for picking a new enemy's numbers by tier neighbour. Before starting, run the
`enemy-upgrade-audit` skill to see what the target enemy already has and who to copy from.

**Reference implementations (read these alongside this guide):**
- `NPCs/Enemies/Gigas.cs` — Holy Gigas: gold/holy *caster* giant, one-time teleport surprise
- `NPCs/Enemies/IceGigas.cs` — Ice Gigas: frost *terrain-controller*, combo chains + fake death
- `NPCs/Enemies/MinotaurMage.cs` — common-enemy scale, 5 attacks, fire-staff caster (the simplest full example)
- `NPCs/Enemies/Necromancer.cs` — kiting ritual caster (SpellboundGhoul summons, sacrifice heal, soul siphon)
- `NPCs/Enemies/QuaraHydromancer.cs` — water-mage with a Wet→ink status combo; layered sprite (tsunami)
- `NPCs/Enemies/Parasprite.cs` — swarm gnat: color=role, latch-on mechanic, front/back dodge rules
- `NPCs/Enemies/FrozenGigasStatue.cs` / `SpellboundGhoul.cs` — helper/trap NPC pattern (texture reuse, detonate-on-death)
- `NPCs/Bosses/SuperHardMode/Gwyn.cs` + `NPCs/Puppets/PuppetNPC.cs` — **the Invader boss (Part 2)**
- `Projectiles/Enemy/{Gigas,IceGigas,Minotaur,Necromancer,Quara,Gwyn}/` — the projectile toolbox
- `Items/Weapons/Magic/HeartOfWinter.cs` + `WrathOfGold.cs` — the 4-cast tome pattern

---

## Phase 00 — The DESIGN REVIEW (do this FIRST, before touching code)

Before any rework, you diagnose the enemy as a game designer would. The user usually asks you to
"look at enemy X and see how it can be improved" — that is a *design* question, and the deliverable is
a written brainstorm of specific flaws + specific reworks for approval, NOT immediate code. This is the
step that made the Parasprite/Necromancer/etc. reworks good. The process:

**1. Read everything the enemy actually is.** Its NPC class, its projectile(s), and the shared systems
it leans on (AddAttack rotation, CanHealAllies, aiStyle). Note what it drops and its death effect —
those often reveal the *intended* fantasy (Minotaur's commented-out fire drops + torch-dust death = it
was always meant to be a fire mage; Necromancer's ghoul-summon + heal = a true necromancer).

**2. Name the enemy's intended fantasy, then ask if the player can SEE it.** The core question is
legibility: is the enemy's identity expressed in something the player can perceive and react to? The
recurring failure is an identity that exists only in hidden state.

**3. Hunt these specific, recurring flaw types** (all real finds):
- **Invisible core mechanic.** Parasprite's *entire identity* — multiplication — happened with zero
  telegraph/effect/sound; a new gnat just appeared. The fix is always: give the signature mechanic a
  ceremony (a telegraph you can interrupt) — breeders visibly *bloat* for a second before splitting.
- **Hidden state that should be visible — and often already has unused art for it.** Parasprite had
  three color variants doing nothing AND a hidden "can-breed" coin-flip. Binding role to color (pink =
  breeder) turned a coin-flip into a *decision* (kill the pink ones first) for free.
- **No counterplay / no player agency.** Necromancer's heal was instant `+150` on a timer — nothing
  the player could contest. The fix gives it a window (a channel you can interrupt) or a *resource* the
  player can deny (sacrifice a ghoul to heal → so killing the ghouls denies the heal).
- **No interaction beyond contact damage.** A 48-damage gnat that only bumps you is texture-less. Give
  it a real verb — the Parasprite *latches* and bleeds you, with front/back dodge rules.
- **Bugs masquerading as design.** A 30 HP gnat with `knockBackResist = 0` (immune to knockback);
  a swarm counter looping hardcoded `0..200` instead of `Main.maxNPCs`; `spriteColor` rolled per-client
  = MP desync; `justHit` zeroing the cast timer = trivially stunlockable casting. Call these out
  separately from design changes.
- **Pre-method cruft** the enemy predates (the old AddAttack rotation with no channels; ancient
  `MNPC.teleporterAI`/`fighterAI` library calls in MinotaurMage).

**4. Apply the designer's lens — every threat needs these four to be fair AND interesting:**
- **Legibility** — you can see it coming (a telegraph: dust, a pose, a sound, a color).
- **Counterplay** — a specific answer (dodge direction, jump, break LOS, kill the summon, keep moving,
  destroy the pillar, deny the resource). Vary the *type* of answer across a kit so it isn't one dodge
  repeated.
- **Agency** — the player's actions change outcomes (interrupt the channel, pop the breeder mid-bloat,
  shatter the statue early, move fast to escape the freeze).
- **Identity** — the fantasy reads at a glance from the enemy's element/behavior.
  Then layer **surprise/memorability** (the fake death, the trap-statue, the "don't turn your back"
  punish) and **decision texture** (swarm triage, kill-order, resource denial) on top.

**5. Write the proposal with real specifics, not vibes.** For each proposed attack/change give:
concrete numbers (telegraph ticks, recovery ticks, damage, ranges), which existing sprite/pattern it
reuses (so it's cheap to build), and — crucially — *what counterplay it demands and what tactic it
answers*. State which items are novel-risk (a brief player-freeze, a grab that bypasses i-frames) so
the user can veto. Present it as options for approval; the user picks and tweaks (they cut the
snowball attack, changed heal numbers, renamed things). Only build after a yes.

**Prove the counterplay with arithmetic.** The player rolls with **22 ticks of i-frames**, can roll
again **30 ticks after starting** a roll (so each roll leaves an 8-tick gap with no protection), and
is immune for **40 ticks** after any hit. A claim like
"rolling away is caught" or "punishes early rolls" must survive that timeline, and so must every
follow-up. For any **melee** attack or combo, load `attack-timing-design`: its §2 has the full
budget and fairness rules, its §3 the per-move timing sheet the proposal must fill in (on-screen
ticks, strike curve, live window, tail, punish window, travel distance), and its §10 the review
checklist, including the failures a real proposal made.

This review step is itself a deliverable the user values — several sessions were "review enemy X" with
no code written that turn at all.

## Phase 0 — Study before writing anything

1. **The poise/stagger system** — `NPCs/GlobalNPC.cs`, the `#region Poise / Stagger` (~line 410).
   The whole redesign hangs off three fields on `tsorcRevampGlobalNPC`:
   - `AttackTelegraphing` — *windup*: poise builds normally, ordinary hits give a light flinch, and a
     poise break **cancels the attack**. Use for windows the player is allowed to interrupt.
   - `AttackCommitted` — *hyper-armor*: zero knockback, zero poise buildup. Use for everything else.
   - `InAttack` (derived) — gates `EvasiveOnHit`; evasion never fires mid-attack.
   Both flags must be **re-set every AI tick** (clear them at the top of `AI()`, set them in the
   active attack's Run method).
2. **`IStaggerable`** — `NPCs/IStaggerable.cs`. Implement it on any enemy with bespoke attack state;
   `OnStagger(NPC)` must clear *exactly* your state machine back to neutral (state, timers, combo
   counters) and can add flavor (dispersal dust, shed projectiles). The global `TriggerStagger`
   calls it and handles the freeze/launch/cooldown itself.
3. **`PoiseProfiles`** — `GlobalNPC.cs` `PopulatePoiseProfiles()`. The central (kbResist, poiseMax)
   table. Mini-boss tier = boss values: `(0.15f, 120f)`. Registering here is mandatory — content
   files alone don't opt into poise.
4. **FighterAI** — `NPCs/tsorcRevampAIs.cs` `FighterAI(...)`. The SF4 walking/nav base. It keeps its
   state in GlobalNPC fields, NOT `npc.ai[]`, so your state machine can own instance fields freely.
   Sync those via `SendExtraAI`/`ReceiveExtraAI` instead.
5. **Verify every vanilla ID against the decompiled source** at `../TerrariaDecompiled/` — a full
   `ilspycmd` decompile of `tModLoader.dll`, one level above the mod (its `README.md` has the regen
   command). Dust IDs, SoundIDs, vanilla projectile IDs and their `Main.projFrames` counts. Never
   trust recall for these; several different-sounding names alias the same index.
6. **Compile-check loop:** `dotnet build tsorcRevamp.csproj` from the repo root works and is fast.
   Build after every major file. (A rework that deletes legacy code should *reduce* the warning
   count — watch it as a sanity signal.)

## Phase 0.7 — The `tsorcRevampGlobalNPC` lever catalog (know these BEFORE inventing behavior)

Most "behaviors" you'd think to hand-code already exist as opt-in levers on `tsorcRevampGlobalNPC`, set
once in the enemy's `SetDefaults` via `NPC.GetGlobalNPC<tsorcRevampGlobalNPC>()`. Check this list
before writing movement/evasion code — half the time the lever already does it. (Poise/stagger levers
are their own thing — see Phase 0 items 1–3; they're not repeated here.)

**Navigation & jumping** (per-enemy tuning genuinely matters here — don't leave at defaults for a
big/heavy or nimble enemy):
- `NavSearchRadius` — A* window (0 = pure fighter). Larger = finds ledges to jump to; it's slow, keep modest (40–80).
- `MaxJumpPower` (default 9) / `MaxJumpBoost` (default 4) — vertical jump ceiling / horizontal gap boost.
  **Tune these per enemy:** a heavy giant wants a strong deliberate jump (9–9.5), a nimble assassin a
  higher one (10), a big beast that shouldn't hop much a lower one. The apex(tiles) ≈ power²/(2·grav·16).
- `CanDoubleJump` + `DoubleJumpPower` — mid-air second jump.
- `MinSurfaceWidth` (0 = off; ≥2 = require that many flat tiles) — keeps LARGE enemies off narrow
  ledges/slopes where the sprite hangs off. Set to the enemy's footprint in tiles (Gigases use 3).
- `CanWalkBackwards` — backpedal while facing the player (casters/kiters).

**Kiting / positioning** (for ranged enemies that shouldn't march into melee):
- `KiteRangeMin`/`KiteRangeMax` (TILES; Max 0 = kiting off) — the preferred distance band. Closer than
  Min → back off; in band → drift + fire; farther than Max → let pursuit close. Necromancer 8–18, Gigas 0–30.
- `KiteLooseness` (0–1) — chance to NOT back off, so melee can sometimes close (higher = more aggressive).
- `PatrolMode` (`Pace`/`Wander`) — behavior when it can't path to the player.
- `RemembersLastKnownPos` — walks to where it last saw you instead of idling (casters).

**Capability bools** (opt-in verbs — grep the field for its exact semantics before use):
- `CanTeleport` — blink near the player when bored/blocked (the FighterAI stuck-recovery teleport).
- `CanPassThroughWalls` — phases through terrain (ghosts).
- `CanGoInvisible` — fades out (assassins/ambushers).
- `CanStopToFire` — plants to shoot a standing volley (ArcherAI tier-2).
- `CanHealAllies` — retargets a heal projectile at wounded allies (Necromancer/Warlock/Dworcs).
- `CanJumpToEvade` — hops straight up to dodge an incoming aimed projectile.
- `CanJumpBeforeAttack` (+ the `JumpBeforeAttack*` family) — launches upward as an attack commits so
  the shot goes off mid-air; a big variation knob for casters/archers.
- `EvadesProjectiles` (PuppetNPC) — proactively dodges incoming shots.

**On-hit evasion** — don't hand-roll dodges; use `EvasiveProfile.*` bundles (`HeavyBeast`, `RedKnight`,
…) in `SetDefaults`, or the individual `Evasive*` capability flags (`EvasiveRetreatJump`,
`EvasiveQuickStep`, `EvasiveRunningDash`, `EvasiveRetreatAndShoot`, …). Gate the `EvasiveOnHit` call to
`State == None` so it never bails the enemy out of a recovery/punish window (see Part 1 Phase 2/3).

**Combat shape:** `Strength` (0.7–1.3 HP/size/damage) and `Agility` (0.2–0.6 dodge/jump frequency) are
coarse dials the base AI reads.

## Phase 1 — Strip the legacy enemy

Old Omnir-era enemies share the same cruft; delete all of it:
- ~20 dead fields at the top (`customAi1`, `num94/95`, drown timers, `oAtt/oDef`, ...)
- unused `teleport()` helpers, commented `CanSpawnLegacy`, dead aim math (`angle` computed, never used)
- the duplicated blind 1-in-550 charge blocks (they fire at 2× rate with zero telegraph)
- the enrage-speed branch (giants should *never run* — speed is the arena's job, not theirs)
- dead loot: commented `Item.NewItem` rolls in `OnKill`

**Keep:** the SF4 lever block in `SetDefaults` (`NavSearchRadius`, `MaxJumpPower/Boost`,
`EvasiveProfile.*`, kite band, `PatrolMode`, `minSurfaceWidth`), gore spawns, `SpawnChance` override.

## Phase 2 — The state machine skeleton

```
enum AttackState : byte { None = 0, AttackA, AttackB, ..., ComboRecovery, PhaseTransition, ... }
AttackState State; int AttackTimer; int AttackCooldown; AttackState LastAttack;
```
- **Instance fields + `SendExtraAI`/`ReceiveExtraAI`**, never `npc.ai[]` (FighterAI-compatible, and
  byte/short-packing keeps the payload small). `netUpdate = true` on every transition
  (`StartAttack`/`EndAttack`); timers tick locally on clients between syncs.
- `AI()` shape:
  1. one-shot `InitializeStats()` (world-flag stats — see Phase 6)
  2. clear `AttackTelegraphing`/`AttackCommitted`
  3. one-time interrupts (phase transitions, fake death) — server-rolled
  4. if staggered (`g.StaggerTimer > 0`): slumped `rotation` lean, falling motes, `return`
  5. `State == None`: call `FighterAI(...)`, footstep FX, cooldown countdown, server `PickAttack()`
     (grounded + player alive + dist gate)
  6. else `RunAttack()`: face player (except locked-direction moves — lock **after** tick 1 so the
     lock captures a fresh value, not last cast's stale one), `AttackTimer++`, dispatch.
- **Attack picking:** weighted pool (`Span<(state, weight)>`), weights gated on distance-in-tiles,
  same-level checks, phase flags, and situational conditions (e.g. a counter-stance only if hit in
  the last 90t). Halve `LastAttack`'s weight. Roll server-side only.
- **Walking casts:** some attacks *keep calling* `FighterAI` (pass `canDodgeroll: false,
  canPounce: false` so the cast reads stately) — great when the threat is at the player's position,
  not the body. Standing casts just do `velocity.X *= 0.8f` per tick.
- **Every exit path must restore what the attack changed** — `EndAttack` is the safety net (e.g.
  Heart of Winter restores `dontTakeDamage`/contact damage there, covering the player-died-mid-attack
  bail-out).

## Phase 3 — The poise contract (the user's standing spec)

- **High relative poise:** 120f (boss tier) in `PoiseProfiles`.
- **Most telegraphs are hyper-armored** — `AttackCommitted = true` from windup frame 1.
- **The one LONG channel per enemy is the stagger window:** `AttackTelegraphing` for its first
  **two-thirds**, then `AttackCommitted` for the tail, with an audible+visual **commit cue** at the
  flip (pitch-up chime + `Projectiles.VFX.TelegraphFlash` in the enemy's color) so "too late to
  interrupt" is readable. Normal stagger point — no special threshold.
  (Gigas: Wrath of Gold 66/100t. IceGigas: Absolute Zero 80/120t.)
- **Recovery windows have NO armor** — they are the deliberate punish windows. Gate `EvasiveOnHit`
  in `OnHitByItem/Projectile` to `State == None` so the on-hit evasion can't bail the enemy out of
  its own recovery.
- `OnStagger` also drops any in-progress combo and can make the punish window interesting
  (IceGigas sheds 3–4 damaging ice chunks — footwork inside the reward).

## Phase 4 — Attack design rules

**Timing conventions (60 ticks/sec):**
| Piece | Range | Notes |
|---|---|---|
| Telegraph | 20–45t (standard), 100–120t (the staggerable channel) | every attack has one; dust + a sound cue at t=1 |
| Active | instant → 90t | multi-hit sweeps use many small ticks, not one chunk |
| Recovery | 25–60t | scale with the attack's payoff; the scariest move gets the longest (Stampede: 60t) |
| Cooldown (`EndAttack`) | 270–480t + rand(60) | mini-boss pacing — walking happens between attacks |

- **The dust IS the telegraph.** Radius shown by dust = radius of the hit (slam geyser ring →
  shockwave range). Never hit outside what was drawn.
- **Tick variation inside one attack:** marching waves at different cadences, 3 bursts with
  different arcs/speeds, sequential fans (one icicle per 8t, each glinting 10t before *its own*
  launch), accelerating drop rhythms (gaps 30→10t). One attack should pose 2–3 dodge problems.
- **Counterplay variety:** position (rings are inside-safe via true annulus collision), motion
  (Winter's Grasp: moving fast at the crystallize tick = safe), timing (jump/roll the low sweep),
  attention (destroy the statue/pillar before it fires), restraint (Rime Ward stores the hits you
  land and throws them back).
- **Sub-phase machines** (jump→apex→dive→land) live in one Run method with a phase int; detect
  landings via `NPC.collideY`, beat the terminal-velocity clamp by re-setting `velocity.Y` each
  tick, hang at an apex with `velocity.Y = -0.3f` (≈ cancels the post-AI gravity add), and always
  include a bail-out (dive timer > 300 = fell in a pit → `EndAttack`).

**Combo chains (IceGigas pattern):** a fixed follow-up table (`ChainFollowUp(state) => next`),
chain roll in `EndAttack` (30% above the phase break, 55% below), chained moves get **half
telegraph** via a `Tel(baseTicks)` wrapper, max 2 moves early / 3–4 in the late phase (rolled per
chain), and every extra move adds **+15t of `ComboRecovery`** — an exhausted, fully punishable
stand. The table can swap targets by phase (Breath→Crown early, Breath→Undertow late).

## Phase 5 — Spectacle: dust, sound, draw

**Dust grammar (verified IDs):**
| Meaning | Pattern |
|---|---|
| "Big cast coming — get away" | dust spiralling/converging INWARD to the body |
| Inhale/vacuum | dust flowing toward the mouth from far away (inverted flow reads instantly) |
| Ground hit incoming | simmering patch on the floor, then eruption burst |
| "This one fires next" | a bright glint flash on the specific projectile (`IceTorch`/`IceRod`, `GoldCoin`) |
| Blast radius | geysers erupting outward exactly to the damage radius |
| Exhaustion/recovery | heavy mist sinking off the body, gravity dust |
Gold kit: `GoldFlame`(228), `GoldCoin`(246), `AncientLight`(261, white-hot cores). Ice kit:
`IceTorch`(135, the glowy workhorse), `Frost`(92, mist), `Ice`(80, shatter), `IceRod`(67, glints),
`Snow`(76). Budgets: 2–4 dust/tick telegraphs, 10–22/tick strikes, 30–50 one-shot bursts.

**Sound:** every telegraph gets a cue at t=1; the enemy keeps ONE cast-chime identity
(gold = `Item29`, ice = `Item30` at varied pitches). `Item27` = ice shatter/crystal, `Item72` =
projectile launch, `Item14` = boom, `Thunder` = judgment strikes, `DeerclopsStep` +
`DeerclopsRubbleAttack` = footfalls/slams, `Item8` = blink. Vary `Pitch`, keep `Volume` ≤ 1.

**Mod-wide cue assignments** — reuse these so the same action always sounds the same:

| Cue | SoundID | Where it is wired |
|---|---|---|
| Throw (star, flask, caltrop, bomb) | `Item7` / `Item18` / `Item19`, random | `PuppetNPC.PlayThrowSound()` |
| Poise break / stagger | `Item27` | `TriggerStagger` — global, every poise enemy |
| Weapon first appears in a ranged telegraph | `Item60` | `PuppetNPC.OnRangedBurstStarted` |
| Teleport departure | `Item79` | `tsorcRevampAIs.QueueTeleport`; arrival stays `Item8` |
| Lit fuse / bomb telegraph | `SoundID.BombFuse` | `UsefulFunctions.BombFuse` — one shared swap point |
| Smoke bomb detonation | `Item20` | `InvaderSmokeBomb.Explode` |
| Enemy ninja-star pop | `Item70` | `EnemyNinjaStarProj` |
| Fire-breath telegraph | `Item119` | `OmnirsMassacre` |
| Rain-of-fire cast | `Item170` | `OmnirsMassacre` |

`Item172` (a long 3-second fuse) is deliberately unused, held for a future larger bomb.

**Draw:** `NPCID.Sets.TrailCacheLength = 8; TrailingMode = 0;` + a `PreDraw` that draws `oldPos`
afterimages (bottom-center origin: `oldPos[k] + (width/2, height + gfxOffY + 4)`) tinted in the
enemy's color when diving/charging (`velocity.Length() > 8f` or the specific phase). Life-scaled
`Lighting.AddLight` aura + a brightness pulse in every telegraph's first 20t. Idle particle identity
(frost mist / rising sparkles) so the enemy reads its element at a glance.

**No new sprites needed:** walk/idle/jump frame sets are enough — the body plants (idle) or jumps,
the spell does the talking. Reuse: `tsorcRevamp/Projectiles/InvisibleProj` for dust-only
projectiles; vanilla textures via `Texture => "Terraria/Images/Projectile_" + ProjectileID.X`
(verify `projFrames` — Deerclops spike 961 and IceSpike 174 are single-frame); a "statue/echo" NPC
just draws the parent's sheet tinted translucent in `PreDraw` (return false, guard `Main.dedServ`
in `FindFrame` — TextureAssets aren't loaded on dedicated servers).

## Phase 6 — Projectile toolbox (copy these, don't reinvent)

- **True annulus (expanding ring):** override `Colliding` — hit iff closest-point distance ≤ r+22
  AND farthest-corner distance ≥ r−22. Broadphase: make width/height = 2×maxRadius (`NewProjectile`
  centers on the spawn point). Inside-safe, roll-through-able. (`GigasNovaRing`/`GigasFreezeRing`.)
- **Ground-snapped spike/patch:** scan tiles down (≤ N) for the first solid, place bottom on it;
  the eruption is delay-armed via `ai[0]`, `CanDamage()` false until erupted. Rise-clip draw:
  bottom-anchored `EntitySpriteDraw` with `scaleY = progress`. (`GigasIceSpike`.)
- **Ground-crawling wave:** move X, per-tick snap-to-ground with climb ≤3 / drop ≤5 tile limits,
  kill on tall walls/pits; arm after a delay so the landing dust leads the damage. (`GigasShockwave`.)
- **Force zones (pull/glaze):** hostile projectile with `CanDamage() => false`; manipulate
  `player.velocity` in `AI()` for **all** players in range — it runs on every client and each
  client is authoritative for its own player. Always cap (pull ≤ 6 velocity, slide ≤ 8) and leave
  an escape (holding away stalemates the pull; below 0.4 velocity the glaze releases).
- **Hover-then-strike:** telegraph phase with `CanDamage() false`, aim locked shortly *before*
  firing (store the direction in `velocity` at tiny magnitude — it syncs), `tileCollide` only once
  flying. (`GigasHeavenlySpear`, `GigasCrownIcicle`, canopy/spear tome variants.)
- **Extra params when ai[0]/ai[1] are full:** pack small ints into a fraction —
  `ai[1] = detachTick + slot/10f` (`GigasHaloSun`).
- **Reactive minions:** orbiting suns read the parent's `StaggerTimer` and scatter on poise break —
  systems interacting for free.

## Phase 7 — Multiplayer rules (every one of these bit us or nearly did)

- All rolls and `Projectile.NewProjectile`/`NPC.NewNPC` behind `Main.netMode != NetmodeID.MultiplayerClient`.
- Dust/sound run unguarded (clients need them); guard `Main.netMode == NetmodeID.Server` only for
  pure-visual helpers.
- World-flag-dependent stats (SHM) **cannot** be read in `SetDefaults` — apply on the first `AI()`
  tick (`statsInitialized` one-shot) and sync the flag in `SendExtraAI`.
- Sounds keyed to `AttackTimer == X` may be missed by clients that sync mid-window: acceptable,
  keep transitions netUpdated so it's rare.
- First-kill tracking: loot rules resolve **before** `ModNPC.OnKill`, so `OnKill` can register
  `tsorcRevampWorld.NewSlain` (+ `NetMessage.SendData(MessageID.WorldData)` on server) and the
  same-kill loot rules still see the pre-kill state. (Invader NPCs established this pattern.)

## Phase 8 — Stats, phases, economy

- **PICK STATS BY TIER-NEIGHBOR — consult [EnemyStatInventory.md](EnemyStatInventory.md) FIRST.** It's an
  auto-generated table of every enemy/boss's HP / Def / Contact / Value across PreHM / HM / SHM plus
  their projectile damage values (~260 rows). When setting stats for a new or reworked enemy, find the
  enemies that spawn in the same biome/progression tier and match the neighborhood — don't guess in a
  vacuum. It also documents the automatic SHM scaling (`lifeMax *= SHMScale`, `def/damage *=
  SubtleSHMScale` for `SuperHardMode`-namespaced classes) and the non-expert boss-HP /1.3, so you can
  see whether an enemy gets runtime scaling on top of its `SetDefaults` values.
- **Tiers used:** mid-HM boss event = 22000 HP / 90 contact / 32 def (IceGigas); late-HM = 32000 /
  110 / 38 (Holy Gigas). **SHM escalation = double HP, damage up a lot** (44000/150/50 and
  64000/200/55), applied in `InitializeStats`.
- **Projectile damage** constants are instance **properties** through
  `ScaleDamage(x) => SHM ? x * 1.3f * tsorcRevampWorld.SHMScale : x` (SHMScale ramps 1→1.5 with SHM
  bosses downed — the ArcherAI convention). Remember hostile projectiles deal **2× the passed value**
  in normal mode: author half-values.
- **Souls = `NPC.value / 25`** (expert; see `GlobalNPC.OnKill`). Tune value against the nearest
  boss: IceGigas 150000 (6000, under TheSorrow's 6800), Holy Gigas 200000 (8000, under TheHunter's 8800).
- **Phase structure:** a one-time transition state at 50% (crack event: pause, shatter cue, permanent
  shimmer, unlocks the phase attacks — set the flag *when starting* the transition so burst damage
  can't double-trigger), and optionally a one-time **surprise** below 30% (fake death → crawling
  pile → rebirth into the scariest move; the fairness tell = the real death drops gore/coins, the
  fake drops nothing). One-time flags all synced.
- **Drops:** unique weapon = guaranteed first kill + 25% repeats, never in SHM (SHM kills pay souls
  via value). Conditions: `NonSHMFirstKillRule` / `NonSHMRepeatKillRule` in `tsorcDropRules.cs`
  (both keyed on `NewSlain`), registered in `ModifyNPCLoot`, with the `OnKill` NewSlain write.

## Phase 9 — The polish checklist (do ALL of these)

- [ ] Footsteps: thud (`DeerclopsStep`) + dust puff + micro screen-shake (`UsefulFunctions.ScreenShake`,
      1.5f/8t/350px falloff) every ~26 ticks of grounded walking; element flavor every Nth step
- [ ] Life-scaled aura + telegraph pulse (first 20t of any attack)
- [ ] Idle element mist (1 dust / 3 ticks)
- [ ] Trail-cache afterimages on the fast moves
- [ ] Stagger: slumped `rotation` lean + falling motes (+ shed hazard if it fits)
- [ ] Death spectacle in `HitEffect` when `life <= 0` (shake + double sound + 50-dust burst),
      factored into a helper if a fake death reuses it
- [ ] Anti-camping answer (ledge-leap, or vertical-reach attacks)
- [ ] `ScreenShake` on every landing/detonation, strength 3–12 by weight
- [ ] Localization: hjson entries for every new NPC/projectile/item (keys = class names; projectiles
      under the flat `Projectiles` block, items as `Name: { DisplayName, Tooltip }`)
- [ ] `PoiseProfiles` registration updated
- [ ] Stats sanity-checked against `EnemyStatInventory.md` tier-neighbors (Phase 8)

### Final-polish question: the Boss Checklist / boss-hunting tome (bosses only)
For a new BOSS or invader, the last decision is whether to add it to the Boss Checklist (the in-game
boss-hunting tome). Registration is a `bossChecklist.Call("LogBoss", this, nameof(X), tier, downedFn,
npcTypes, dict{displayName, spawnInfo, spawnItems, headTexture, portrait})` in `tsorcRevamp.cs`
(~line 2636+). **The critical caveat:** the core bosses are ordered by a `tier` float and the *next
boss's clue depends on the previous boss's rarity/tier* — so the sequence is load-bearing. When adding
an OPTIONAL boss or invader:
- Give it a **higher tier number than the core sequence** (e.g. Soul of Cinder is 21.5f) so it slots
  after them; **do NOT renumber or wedge into the existing tiers** — that breaks the clue chain.
- Each boss can still carry its own "this boss" clue/spawn-info entry without being part of the
  progression clues.
- **Most optional bosses/invaders are NOT rematchable** and don't need to be — they drop all their loot
  on the first kill (see the unique-weapon drop rules above), so the checklist entry is mainly a
  bestiary/lore record. Only wire a repeatable spawn if the boss is deliberately farmable.
Also update `NPC.rarity` (the bestiary rarity star) and, for the mod's own progression gates, the
`NewSlain` registration in `OnKill`.

## Phase 10 — The mirrored weapon (tome pattern)

One item, four casts: **tap vs hold on each mouse button**, split by a held controller projectile
(`Item.channel = true`, `AltFunctionUse => true`, `Shoot` spawns the controller with mode in ai[0];
release within 18 ticks = tap, else hold). Hold-left = a channel (drain mana per interval, spawn
stream/orbiters), hold-right = a charge-then-release (converging dust = the boss's own channel
language, surcharge mana at release, fizzle if released early). Friendly conversions of the boss
projectiles: `friendly = true`, `DamageType = DamageClass.Magic`, `usesLocalNPCImmunity` (+
`localNPCHitCooldown` 15–30 for multi-tick, `-1` for hit-once waves), aim helpers only on
`Main.myPlayer == Projectile.owner`, `CanUseItem` blocks while a controller exists. Placeholder art:
a recolorable vanilla tome texture (`Item_` + WaterBolt / GoldenShower).

## Phase 11 — Verify

1. `dotnet build` clean; warning count should not grow.
2. **Self-review for the classic bugs** (all real finds from this rework):
   - facing/direction locks reading stale values on the attack's first tick
   - evasion or chains firing out of punish windows
   - helpers existing in one giant but not the other (`IsSolidAt`)
   - state exits that don't restore what the state changed (`dontTakeDamage`, contact damage)
   - `TextureAssets` access on dedicated servers (`FindFrame`)
   - namespace collisions with the `tsorcRevamp` Mod class (don't qualify, use the namespace walk)
3. Hand off the **risk watchlist** — the specific things most likely to be wrong in game, so the
   playtest looks at them first: landing detection on slopes, gravity cancels, ground-snap on
   stairs, ring-collision broadphase, chained-telegraph readability, escape-velocity thresholds vs
   the mod's dodge-roll speed.
4. Record what is **unfinished** — work deliberately left for later, not whether it has been
   playtested. Test status goes stale the moment someone plays without saying so; a list of what
   was never built does not.

*Treat the reference implementations as canonical for structure; their tuning values have been
iterated since and are not frozen.*

---
---

# Part 2 — Puppet-based BOSSES (the Gwyn Method)

Everything above is for **FighterAI ground enemies** that own their whole AI. A different, larger
class of work is rebuilding a **boss on the Puppet system** — a *puppet Player* that wears armor and
swings weapons through a shared combat framework. This is how Artorias was rebuilt and how **Gwyn,
Lord of Cinder** (the final boss, 22 attacks) was rebuilt July 2026. Read `NPCs/Bosses/SuperHardMode/
Gwyn.cs` + `NPCs/Puppets/PuppetNPC.cs` alongside this.

**When to use which:** if the enemy is a humanoid that should swing a real weapon and combo like a
Souls boss → PuppetNPC. If it's a beast/caster/giant that moves and casts → FighterAI (Part 1).

## G0 — What the Puppet base gives you for free (don't rebuild these)

`PuppetNPC` is a big, mature base. Before writing ANY attack, know that it already owns:
- **Puppet rendering** — you supply `HeadArmorItemType`/`BodyArmorItemType`/`LegsArmorItemType`
  (an `[AutoloadEquip]` armor set) + `MeleeWeaponItemType` (an enemy-copy weapon item in
  `Items/Weapons/Enemy/`) + `MeleeArchetype`. The base draws the armored body swinging the weapon.
- **The melee combo system** (`MeleeComboSystem.cs`) — data-driven combos of `ComboMotion` steps
  (`OverheadArc`, `UnderhandArc`, `HorizontalSweep`, `Thrust`, `JoustDash`, `Spin`, `GroundSlam`,
  `LeapSlam`, `ChargeChop`, `Feint`, …). Each archetype (Greatsword, Broadsword, Hammer…) has a
  default 5-combo table. Range-gated, HP-weighted selection.
- **Direction locking** — `LockComboDirection()` holds facing during each swing's active window
  (roll behind = safe), UNLOCKS during the inter-step pause + recovery (re-faces you). **This is the
  correct challenging-but-fair behavior; do NOT lock recovery** — that creates a free-backstab window.
- **Special-attack TEMPLATES** — Nova (charge→blast→recovery), AbyssShard (windup→fire-event
  sequence), HomingVolley (dodgeback→overhead swing→fire mid-swing→recovery), TendrilGrab
  (telegraph→launch reaching arm→reach→swing), Boomerang (overhead→hurl→recovery), Pierce,
  JumpSlash, FlipSlash. Each is `CanX`/`XChance`/`XCooldown`/timing virtuals + `DoX*` hooks.
- **Wings/flight** — `HasWings`, `WingsAccessoryItemType`, an `EnemyFlightController` (takeoff/hover/
  dive/land). `RandomTakeoffChance 0` + `FlightHeightTrigger 99999` disables autonomous flight so YOU
  command it (`Flight.RequestTakeoff()`/`RequestLand()`).
- **Range params** — `MeleeRange`, `StabRange`, `ComboMaxStartRange`. Set these to the weapon's reach
  (greatsword ≈ 110/180/340). The base won't swing into empty air; `ChargeChop` closes gaps first.
- **`SlowDownBeforeMelee`** — set **false** so it pursues through the windup (no walking out of the
  telegraph). The direction lock snaps on at swing commit, not through the whole windup.
- **`TryMeleeHit(reach)`** — spawn a melee hitbox; used by any bespoke swing/slash.
- **A DebugMode HUD** (`DrawPuppetAttackDebug` in `tsorcRevampSystems.cs`) reading `DebugPhaseName`/
  `DebugComboTag`.

## G0.5 — The Artorias catalog: what's ALREADY BUILT to draw from

`Artorias.cs` was the first Invader boss rework and is the richest existing example. Before inventing a
mechanic, check whether Artorias already implements it — most "new" boss moves are a reskin of one of
these. Each is a base template Gwyn also reused; Artorias shows a fully-tuned configuration + the
`DoX*` hook bodies to copy.

- **Piercing Dash** (`CanPierce`): 60t telegraph → 16-speed dash across the arena (700px range) →
  **50% chance to end in a STAB that IMPALES the player**. The impale is a genuinely reusable
  player-grab-and-throw system: it sets `tsorcRevampPlayer.ImpaleFreezeTimer` + `ImpaleWorldPosition`
  (via `GetSwordTipWorldPosition`), which FREEZES the player pinned to the sword tip, then RAISES them
  over `PierceStabRaiseTicks` (180) and flicks/throws them (`PierceStabFlickTicks` 20). If you want a
  "seize and hurl the player" mechanic, this already exists — don't rebuild it.
- **Umbral Echo Step** (`CanEchoStep`): afterimage-trail teleport that *rides along on* other attacks
  (armed from Piercing Dash + Forward Flip Slash via `TryArmEchoStep`). The model for "a mobility
  flourish attached to an existing attack" rather than a standalone move.
- **Jumping Downward Slash** (`CanJumpSlash`): dodgeback → rise → slam. The template for any
  leap-and-crash (Gwyn could map a flame version here instead of AI-summoning it).
- **Forward Flip Slash** (`CanFlipSlash`): acrobatic forward flip into a hit — the roll-catch gap-closer.
- **Abyss Slash** (`CanAbyssSlash`): a sequence of ranged sword-wave swipes (`DoAbyssSlashFire(index)`
  per swipe) — the template for "throw projectile crescents off a swing," i.e. a ranged melee.
- **Abyss Tendril Grab** (`CanTendrilGrab`): telegraph (arm dissolving into shadow dust) → launch a
  reaching `ArtoriasAbyssTendril` projectile → reach ticks → finishing swing. The reach-out-and-grab
  template (Gwyn's Lord's Grasp is this with a flame reskin).
- **Charge-up Nova** (`CanNova`): the HP-threshold set-piece done right — a `(hpFrac, radius, damage)[]`
  table with per-stage `_novaStageDone` one-shot flags, escalating 500/600/700px blasts at 50/20/10%.
  Copy this exact structure for any "escalating one-shot at HP gates."

**Takeaway:** the Invader base has ~10 special-attack templates, most piloted by Artorias with tuned
values and copyable hook bodies. A new boss's kit is mostly (a) picking which templates to enable with
what timing, (b) a bespoke reactive combo table, and (c) a few AI-summoned casts for the non-swing
attacks. Very little is truly from scratch.

## G0.7 — REGULAR (non-invasion) enemies on the puppet system (Cleric of Sorrow)

You can render an ordinary spawned enemy as a puppet (armored body + held weapon) WITHOUT it being an
"invader." What makes an PuppetNPC "an invader" is *only* the "INVADED BY ___" banner fired on its first
AI tick — there is no invader registry/classification/spawn-pool gate. So:
- Add (once, to the base) `protected virtual bool AnnounceInvasion => true;` and guard the banner with it.
  A regular puppet enemy overrides it **false** and defines a normal `ModNPC.SpawnChance`. That's the whole
  "not an invader" story.
- **Reuse a vanilla equip sprite with no art — CHECK ItemID FIRST.** Most vanilla armor draw slots
  (`ArmorIDs.Body.X`) DO have a real equippable `ItemID` behind them, even reskins/rare drops (e.g. the
  Lunar Cultist Robe body slot 181 is `ItemID.BlueLunaticRobe` — a real, buyable vanity item). Grep the
  decompiled `Item.cs` `SetDefaults` switch for the `bodySlot =`/`headSlot =`/`legSlot =` you want to find
  which real `ItemID` already produces it before writing any wrapper. **Only** if no vanilla item sets that
  slot (true dead draw-slots do exist) write a tiny vanity `ModItem` with `Item.bodySlot = ArmorIDs.Body.X`
  in SetDefaults and hand THAT as `BodyArmorItemType`. (Cost of skipping this check: a whole unnecessary
  ModItem file — caught only when the user happened to know the real item existed.)
- **Rendering is coupled to `base.AI()`** — the puppet's wings/flight anim, weapon cast-pose, and arm posing
  all read the base's PRIVATE `_flight`/`Phase`/`_weaponAnim`. So do NOT override AI() wholesale. Use the
  Gwyn pattern: `override AI() => base.AI() + injected ticks`, and override `RunMovementAI(speedMult)` for
  ground movement. Run weapon casts through the base **magic phase** (`DoMagicAttack` sets
  `_magicAttackTicksOverride` for channels; `DoMagicTick` emits per-tick) so the weapon animates + fires from
  the hand for free. Fire from the weapon tip via `PuppetHandPosition + aim*reach`.
- **Magic-only caster:** set Melee/Ranged weapon types = -1 so the base only enters the magic phase; make
  `DoMeleeAttack`/`DoRangedAttack` no-ops (they're abstract).
- **Set-pieces that are NOT attacks go on `AttackPhase.Custom`, not the magic phase.** Self-encases,
  ally-channels, HP-triggered death-bursts, self-buffs — anything without a melee/ranged/magic shape —
  should NOT masquerade as a magic cast (that produces a two-FSM smell + leaky `Phase == MagicAttack &&
  myEnumFlag` conjunctions). Use the base's generic scriptable phase: `StartCustomAttack(duration,
  poseWeaponItemType = -1, swingPose = false)` parks the puppet, fires `DoCustomAttack()` once, then
  `DoCustomTick(ticksRemaining)` each tick (last tick = 1), holding an optional weapon pose, and returns to
  Idle on expiry. Call it from your `AI()` override when `Phase` is Idle/CasualStroll (or unconditionally to
  interrupt). Keep the ranged offense on the magic phase and the set-pieces on Custom — that clean split is
  the whole point. (Cleric of Sorrow: Hail/Veil/Undertow = magic; Ice Shell/Communion/Last Rites = Custom.)
- **Amphibious FLYER instead of a swim animation:** give it `HasWings` + a vanilla wings item
  (Jim's Wings) — the wings double as the underwater hover AND grant real out-of-water flight, sidestepping
  the "attached hands/feet can't swim" problem entirely.
- **Overlay art on the puppet (an ice shell, etc.):** override the subclass `PreDraw`, draw the overlay
  BEFORE `base.PreDraw` (→ behind the body), then call `base.PreDraw(..., drawColor * 0.5f)` to render the
  puppet translucent. (VERIFY the base honors drawColor alpha for the player draw — playtest.)
- **MP netcode for the subclass's own state:** `PuppetNPC` does NOT sync `Phase`/`PhaseTimer` at all —
  every peer independently runs `PuppetAttackAI()` off already-synced inputs (position/life/target), which
  the whole system tolerates because damage/spawns are separately gated server-only throughout. A subclass
  with its own extra state (which spell/attack variant is active, a heal target, reactive cooldowns, a
  one-time flag) should: (1) add `SendExtraAI`/`ReceiveExtraAI` for that state (Necromancer/SpiritOfKhaios
  pattern); (2) gate any WEIGHTED RANDOM pick to `Main.netMode != NetmodeID.MultiplayerClient` and rely on
  the synced value on clients, so server and clients never independently choose different outcomes; (3)
  leave triggers driven by an already-synced vanilla field (like `NPC.life` crossing an HP threshold)
  UNGATED — every peer deterministically reaches the same trigger at roughly the same tick, which is
  *tighter* sync than forcing it server-only and waiting for the client's own next unforced cycle to catch
  up; (4) gate any mutation of an OTHER entity's field (healing an ally NPC, buffing nearby NPCs) to
  server-only, matching the Necromancer ghoul-consumption precedent — those fields are themselves
  separately-synced, so every peer writing them independently is redundant at best.

## G1 — Extend the base with SAFE additive virtuals (never fork it)

When the base is missing a hook, add a `protected virtual` with a **no-op / current-behavior default**
so every other invader is unaffected, then override it in the boss. Real examples added for Gwyn:
- `MeleeComboPoolOverride => null` (bespoke combo table instead of the archetype default).
- `ReactiveComboIndex(dist, band, ready[]) => -1` (fully-reactive combo pick from live player state;
  -1 falls through to the weighted roll). Wired into `TryStartMeleeCombo` before the roll.
- `DebugAttackLabel` (friendly current-attack name for the HUD, with yellow/white alternation on
  change — the single most useful playtest tool; see G7).
- `ShowWingsWhenGrounded => true` (Gwyn=false to hide folded wings and keep a clean cape silhouette).
- `Flight` accessor + wing-item **hot-swap** in `SyncPuppet` (so Gwyn can swap Angel→Flame wings
  below 30% HP).
Each was ~4 lines in the base + the override. This is the pattern for ALL Invader extension.

## G2 — Fully reactive combos (the "reads your dodge" feel)

The base picks combos by range-band + HP weight. To make a boss *read the player*, override
`ReactiveComboIndex`: inspect `player.GetModPlayer<tsorcRevampPlayer>().isDodging` + velocity + relative
position, and return the counter combo's index (only if `ready[idx] > 0`, i.e. off cooldown; else -1).
Gwyn's reads: player **launched+above** → LeapSlam catch; **rolling through at point-blank** → Spin;
**rolling away** → Roll-Catch (far) or Sliding-Thrust (near); else -1. Also a **queued-punish** idiom:
a set-piece (teleport-behind, gravity-pull) sets a `_pendingNudge` timer, and `ReactiveComboIndex`
converts it into a guaranteed follow-up combo the next time one can start.

## G3 — Bespoke combo tables

Supply `MeleeComboPoolOverride` = a `MeleeCombo[]` built with a local `GS(motion, telegraph, active,
pause, dmg, reach, push)` step helper. Give each combo `Name`/`BaseWeight`/`Preferred` band/
`InitialFlashColor` (fire = Orange bread-and-butter, Red heavy commits)/`CooldownAfterUse`/
`HeavyCommit` (HP-scaled up). Index consts (`CB_CLEAVE=0`, …) let `ReactiveComboIndex` name them.
Give swings reach past the blade with a `DoComboMeleeHit` override that spawns a short-lived fire
crescent projectile on the heavy swings (ReachMult ≥ ~1.15), plus fire dust on every swing.

## G4 — Set-pieces: two delivery mechanisms, pick per attack

1. **Map onto a base TEMPLATE** when the attack has a weapon MOTION (raise/throw/swing/grab/dash).
   Override `CanX => true` + timing + the `DoX*` hooks. You get the puppet animation for free.
   Gwyn: HomingVolley = lightning-spear throw; Nova = Cinder Nova ring; AbyssShard = spear volley;
   Tendril = flame-grasp; Boomerang = spinning greatsword.
2. **AI-summoned magic** when the attack is a *cast* with no swing (rain, meteor, pull, teleport,
   marched charge, aerial phase). Add a `TickX()` called from the boss's `AI()` override, gated on a
   cooldown field + `Phase == AttackPhase.Idle || CasualStroll` (the free/non-attacking phases;
   `Phase` is `protected` on the base) so it never fires mid-swing. To seize the body for a
   multi-second scripted move (march, aerial storm, gravity channel), **park the combat machine** with
   `EnterPhase(AttackPhase.NovaRecovery, duration)` and drive `NPC.velocity`/position yourself for the
   duration, then let it expire back to Idle. Gwyn: Firestorm, Descent meteor, Gravity well, Flash
   Step, Judgment teleport, Unbroken Advance, Winged Plunge, Spear Storm.

## G4.5 — Adding a NEW `AttackPhase` (the compiler catches none of this)

Prefer G4's two mechanisms — neither adds a phase. When an attack genuinely needs its own phases in
the base, know that `AttackPhase` has ~66 values and a phase's *behaviour* is decided by which of the
membership predicates below it appears in. Leave it out of one and it compiles clean, then misbehaves
in a way that looks like an unrelated bug. Both failure rows marked ✱ happened in a single session.

| Predicate (`PuppetNPC.cs`) | Add the phase when… | If you forget |
|---|---|---|
| `IsWeaponVisiblePhase` | the weapon should be drawn during it | the blade vanishes while the pose is still held |
| `IsMeleeSwingPosePhase` | `_weaponRotation` is a real swing, not a held aim | the composite arm stays static; the sword rotates alone |
| ✱ `IsWeaponRecoveryPhase` | it is the recovery that ends a weapon swing | the pose cuts on recovery frame one, ignoring the follow-through hold |
| ✱ `AttackOwnsDodgeIFrames` | the attack arms its **own** `DodgeTimer` (rolls, flips, leaps) | the dodge guard skips `PuppetAttackAI` — the attack freezes itself (Flip Slash advanced 1 tick in 6) |
| `IsMeleeComboPhase` | it is part of the shared combo pipeline | combo bookkeeping (step advance, locks) never sees it |
| `IsHoldingMeleeRecoveryFollowThrough` switch | you added to `IsWeaponRecoveryPhase` | the hold uses the generic `MeleeRecoveryTicks`, so it releases at the wrong moment |

Two more that behave differently:

- **`IsTelemetryAttackPhase` is an exclusion list.** New phases are logged automatically; only touch it
  when adding a *non-attack* phase (a new idle, heal or guard state) that should stay out of the logs.
- **`UseCompositeArmForAdditionalPhase`** is a per-boss virtual escape hatch for `IsMeleeSwingPosePhase`.
  Reach for it when one boss's bespoke phase needs the composite arm, rather than editing the shared
  list for everyone. Gwyn uses it for a single sequence (`IsDashGrabSequence`); Artorias lists ~27
  phases in it — effectively his whole bespoke kit, recoveries included. **That makes it a second,
  per-boss list to update**: a new phase on a boss that overrides this must be added there as well as
  to the shared predicates above.

Also check `EnterPhase`: it clears the attack facing lock only on `Idle`, `CasualStroll` and
`ClosingDistance`. A new *free* phase belongs in that list too, or the boss keeps facing the way its
last attack pointed.

After wiring, render the attack with the `puppet-swing-tuning` skill before playtesting — a missing
`IsMeleeSwingPosePhase` entry shows up immediately as an arm that never moves.

## G5 — Wings & flight

`HasWings => true` shows wings; gate autonomous flight OFF and command it from a set-piece:
`Flight?.RequestTakeoff()` then drive `NPC.velocity` yourself (the controller owns velocity while
airborne; hover self-refreshes), `Flight?.RequestLand()` to come down. Hot-swap wing style by making
`WingsAccessoryItemType` a conditional getter (the base now rebuilds the cached wing item when it
changes). Vanilla wing IDs verified: `AngelWings 493`, `FlameWings 821`. **Dome/ceiling awareness:**
any upward move needs a `FindCeilingY` scan — cap the apex at `max(defaultApex, ceiling + pad)` so it
doesn't clip an arena roof (the Gwyn arena is a dome with tight edge clearance).

## G6 — Keep-what's-good when reworking a boss

The user often wants specific *systems* preserved while the kit is scrapped. Gwyn kept three at their
original distances (defense ring 1000px, coward's ring 2000px, rain-of-death >600px) and the on-hit
debuff stack, but scrapped every attack and the guardian-sword-NPC mechanic. Port the kept systems as
clean `TickX()` methods first (Phase 1 foundation), get it compiling and spawnable, THEN build attacks
on top. Ask which systems to keep; don't assume.

## G7 — The DebugMode attack HUD (build this EARLY for playtesting)

The single highest-leverage playtest tool. A `DebugAttackLabel` string the boss sets in every attack
hook (`SetAttackLabel(name, ticks)` + a timer that clears it), surfaced by the lower-left HUD as a
prominent line that **alternates yellow/white each time the attack name changes** (track last-label +
toggle per whoAmI). Now a tester always knows what's firing and can tell consecutive attacks apart.
For testing, let all attacks **cycle freely** (no HP gating) first; batch them behind HP thresholds
only AFTER the feel is tuned.

---

# Part 3 — Cross-cutting lessons (learned the hard way, all enemies)

These bit repeatedly across the Gigas pair, MinotaurMage, Necromancer, QuaraHydromancer, Parasprite,
and Gwyn. Internalize them.

### The `tsorcRevamp` Mod-class / namespace collision (hit ~6 times — the #1 recurring bug)
`tsorcRevamp` is BOTH the root namespace AND the `Mod` subclass. In any **expression/generic-argument**
context (`ModContent.ItemType<tsorcRevamp.Items.X>()`, `tsorcRevamp.NPCs.PatrolMode.Wander`), the
identifier `tsorcRevamp` binds to the **Mod class**, so `tsorcRevamp.Items`/`.NPCs` fails with "type
name 'Items' does not exist in the type 'tsorcRevamp'". Fixes, in order of preference:
1. Drop the `tsorcRevamp.` prefix — rely on `using` + relative name (`Armors.FirelinkHelm`,
   `NPCs.PatrolMode`). Works when unambiguous.
2. If the short name is **also ambiguous** between two namespaces (e.g. two `SwordOfLordGwyn` classes),
   use `global::tsorcRevamp.Items.Weapons.…` — `global::` forces namespace resolution and bypasses both
   the Mod-class binding and the ambiguity.
This collides constantly with WIP files (see below). When you see CS0426 "does not contain a definition
for 'Items'/'NPCs'" or CS0104 ambiguity on a mod type, this is it.

### You share the repo with a live, concurrently-editing user
The user edits files *while you work* — renaming classes (LordGwyn→Gwyn), adding WIP files
(DualBladedAxe, GravelordNito, SoulOfCinder items, GwynRewardDrops, SwordOfLordGwyn), tweaking your
projectiles' dust/comments. Consequences and habits:
- **Your build will break on files you never touched.** Before assuming your code is wrong, read the
  error's file path — it's often a user WIP file with the Mod-class collision or a missing `using
  Terraria.ID;`. Fix it (it's usually one line), note it in the summary, move on. This unblocks your
  own compile-check and is genuinely helpful.
- **Re-read before editing** if a `<system-reminder>` says a file changed; your cached view is stale.
- **hjson edits can get clobbered** by the user's concurrent edits — re-grep the anchor and re-apply.
- Watch for **name/asset renames**: a class you referenced may have moved (DreamerGhoul→SpellboundGhoul
  once the user added the sprite). Grep to confirm the current name.

### Sprite reuse workflow (no new art needed, validated across ~8 enemies)
- **Measure frames first:** `Add-Type -AssemblyName System.Drawing; [Image]::FromFile(path)` in
  PowerShell (Bash `$()` mangles it — use the PowerShell tool). Sheet H / frame count = frame height.
- **Copy out of the build-excluded staging folder:** `Projectiles/Enemy/UnusedAssets/` (and the mod's
  other unported-asset areas) are `buildIgnore`'d + `<Compile Remove>`'d. To USE a sprite, `cp` it into
  the live projectile folder under the projectile's class name so it autoloads. Never reference a path
  inside UnusedAssets.
- **Reuse vanilla textures** via `Texture => "Terraria/Images/Projectile_" + ProjectileID.X` (verify
  `Main.projFrames`). Deerclops spike 961 & IceSpike 174 = single-frame; SnowBallHostile 109.
- **Layered rendering** (keep dust AND add a sprite): draw the sprite in `PreDraw` (dust always renders
  above projectiles, so the dust sculpture layers on top automatically). Gate it behind a
  `static readonly bool DrawSprite = true` so the pure-dust fallback is one flip away. Bottom-anchor
  ground sprites; a `Reverse` mirror sprite is redundant — use `SpriteEffects.FlipHorizontally`.
- **Tint via a translucent draw pass** (statue/echo NPCs) — cheaper than a recolored sheet; the user
  can drop a real sheet later by changing one texture path.

### Dust & sound ID gotchas
- **`MartianSaucerSpark` == `GoldFlame` == 228** — literally the same dust; "variety" with both is a
  no-op. For gold-vs-electric contrast use `DustID.Electric` (226). ALWAYS verify dust IDs in the
  decompile; several "different-sounding" names alias the same index.
- Sound identity per element is worth keeping consistent — see the cue table in the Gigas Method's
  sound section above.

### Reusable projectile PATTERNS (the real toolbox — copy these shapes)
- **True annulus ring** (expanding, inside-safe, roll-through): `Colliding()` returns hit iff
  closest-point-dist ≤ r+thick AND farthest-corner-dist ≥ r−thick; broadphase box = 2×maxRadius.
- **Ground-snap crawler** (shockwave/flame-wave/floor-lightning): move X, per-tick snap to ground with
  climb≤N/drop≤M limits, die on tall walls & pits; arm after a delay so the telegraph dust leads the
  hitbox. Shared by Gigas, Minotaur, Quara, Gwyn floor-spark.
- **Delay-armed ground telegraph** (spike/pillar/strike-mark): `CanDamage()` false until `Timer >
  telegraph`; draw a rune/mark decal that brightens; frames-as-eruption if the sprite animates growth
  (IceWave). `OnSpawn` sets `timeLeft = telegraph + active`.
- **Force zone** (pull/glaze/wind, no damage): `CanDamage()=>false`, mutate `player.velocity` for ALL
  players each tick (runs on every client; each is authoritative for its own player); ALWAYS cap the
  force and leave an escape (hold-away, roll, or a speed threshold).
- **Spawn-node telegraph** (orb → holds → fires a projectile → dissipates): the orb is the READ, not a
  hazard (`CanDamage` false); reuse for both a small volley and a big storm by varying count/rhythm.
- **Two-stage delayed payload** (lightning spear): thrown projectile explodes → spawns a delayed
  strike → which spawns floor-crawlers. Keep the whole chain in the projectiles; the NPC just throws.
- **Reactive minions** reading the parent's state (`parent.GetGlobalNPC<…>().StaggerTimer`) for free
  interactions; **trap NPCs** that detonate on death (SpellboundGhoul, FrozenGigasStatue) — fire the
  blast from `OnKill` so *both* being killed and being sacrificed trigger it.

### Combo-chain & reactive idioms
- **Combo chains** (FighterAI enemies): fixed follow-up table, chained move gets **half telegraph**
  (`Tel()` wrapper), max 2 moves early / 3–4 in the late phase, **+15t recovery per extra move**
  (`ComboRecovery` state). Stagger drops the chain.
- **Wet/status combos:** a cheap tag attack that sets up a payoff (Quara: bubbles apply Wet →
  Wet doubles the ink debuff durations). Verify the vanilla buff's real effect first — `Wet` is purely
  cosmetic on players (dripping), so its whole value is as a combo flag.

### Amphibious hover — water movement without a swim animation (Cleric of Sorrow)
A walking FighterAI enemy with attached hands/feet **can't** get a believable swim animation, but it can
still move well underwater: when `NPC.wet`, set `NPC.noGravity = true` and drive `NPC.velocity` yourself
each tick toward a kite-band X + a bobbing target Y (a `sin` bob above the player), lerped and speed-clamped,
with frost/element dust **jetting at the feet** to read as propulsion. On land, fall back to normal
`FighterAI` + `LeapAtPlayer` (`noGravity = false`). Reuse the *idle/walk frames* — the levitation + feet
dust do the selling. FighterAI leaves gravity/water physics to vanilla `NPC.Update`, so directly writing
velocity in `AI()` with `noGravity` dominates. **Watchlist:** jitter at the water surface, and flying up out
of the water into an air pocket (becomes `!wet` → ground mode mid-air → falls) — clamp the upward velocity.

### Curse/empower-allies aura (mirror PlaguesmithBuff)
To make an enemy's death (or a channel) **empower nearby enemies**, copy the PlaguesmithBuff pattern: a
`ModBuff` whose `Update(NPC)` sets a bool flag on `tsorcRevampGlobalNPC` (reset in `ResetEffects`), read in
`GlobalNPC.OnHitPlayer` to `AddBuff` a debuff on the player. Apply the buff to nearby enemies once (loop
`Main.maxNPCs`, `!friendly && whoAmI != self && Distance < radius`) with the aura duration = the buff time.
(Cleric of Sorrow's Last Rites → `SorrowfulCurseBuff` → Cursed Inferno on hit for 30s.)

### Stats / economy / MP (quick refs, expanded)
- Hostile projectiles deal **2× the passed damage** on hit (normal). Author half-values.
- Souls = `NPC.value / 25`. Unique-weapon drops: guaranteed first kill + 25% repeats, never SHM.
- World-flag stats (HM/SHM) are **unreliable in SetDefaults** — apply on the first `AI()` tick
  (`statsInitialized` one-shot), sync the flag in `SendExtraAI`.
- `[Autoload]`-hidden helper NPCs need `SpawnChance => 0`, `NPCBestiaryDrawModifiers { Hide = true }`,
  and value 0.
- Compiling proves structure, not feel. Always hand off a risk watchlist of what to look at first,
  and a list of what was left unbuilt.

*This whole document is a living record of the July 2026 enemy-and-boss overhaul push. When you do the
next one, ADD what you learn here.*
