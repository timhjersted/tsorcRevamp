# Enemy Ability Porting Guide

How to port a FighterAI/ArcherAI enemy to the shared **poise/stagger**, **attack-phase tagging** (telegraph →
hyper-armor), **evasion**, and positioning systems. Two enemy shapes need different work:

- **AddAttack enemies** (their attacks come from `UsefulFunctions.AddAttack` → `SimpleProjectile`) — mostly
  automatic. *Examples: Dworc casters, BasiliskWalker, the AddAttack ghosts.*
- **Hand-rolled enemies** (attacks driven by `ai[]` / `localAI[]` / a custom timer field) — manual work.
  *Examples: BasiliskShifter (ported), BasiliskHunter (TODO), the Red Knight family.*

Term: **"attack-phase tagging"** = giving each attack its `AttackTelegraphing` (interruptible wind-up) and
`AttackCommitted` (hyper-armor) windows so the poise system knows when the enemy can be staggered vs. shrugs hits.
The committed spans are the **"hyper-armor windows."**

---

## The systems you're plugging into (quick reference)

All on `tsorcRevampGlobalNPC` unless noted.

| System | Field(s) / hook | Effect |
|---|---|---|
| Poise / stagger | `PoiseMax` (set via the central table, see below) | Magic/physical hits build poise; a break = stagger (launch + ~2s freeze + cancel wind-up) |
| Attack phases | `AttackTelegraphing`, `AttackCommitted`, derived `InAttack` | `AttackCommitted` = hyper-armor (no poise, no knockback). Telegraph = interruptible. |
| Auto-tagging | `AddAttack(..., commitFraction)` | AddAttack enemies set the phase flags automatically from `commitFraction` |
| Wind-up cancel | `IStaggerable.OnStagger(npc)` (hand-rolled) / automatic (AddAttack) | A stagger resets the attack timer → cancels a telegraphing shot |
| Evasion on hit | `EvasiveProfile.X(globalNPC)` + `OnHitBy* → tsorcRevampAIs.EvasiveOnHit(NPC, melee)` | Hop/dash/leap/quick-step/teleport/retreat reactions |
| Evasive cloak | `EvasiveProfile.EvasiveCloak(globalNPC, cloakChance, threatRange)` | Threat-reactive invisibility (caster ghosts) |
| Pre-attack jump | `CanJumpBeforeAttack`, `JumpBeforeAttackChance/Power/Delay`, `SuppressPreAttackJump` | Launches at commit so the shot fires mid-air |
| Ranged kiting | `KiteRangeMin`, `KiteRangeMax`, `KiteLooseness` | Holds a preferred distance band from a same-level player |
| White poise bar | (automatic) | Orange = vulnerable, **white** = hyper-armor/recovery i-frames, yellow = staggered |

### `commitFraction` (how the telegraph splits)
Fraction of the **telegraph window** (flash → fire) that stays *telegraphing* (cancellable) before it COMMITS:
- `0` (default) = committed the instant it flashes ("after the flash it's committed").
- `0.5` = first half of the tell is cancellable, second half is hyper-armor (e.g. a shrinking magic-ring tell).
- `1` = cancellable right up to the shot.
Pair with `telegraphTime` to size the window (e.g. `telegraphTime: 40, commitFraction: 0.5f`).

### Evasion flags (set directly or via a profile)
`EvasiveRetreatJump`, `EvasiveRetreatDash`, `EvasiveTeleportAway`, `EvasiveLeapForward`, `EvasiveRunningDash`,
`EvasiveRetreatAndShoot`, `EvasiveQuickStep`. Each carries a baked-in melee/ranged/both affinity inside
`EvasiveOnHit`. `CanJumpToEvade` is a separate AI-tick pre-jump (dodge incoming projectiles).

---

## Poise values live in ONE place — `PopulatePoiseProfiles()`

`tsorcRevampGlobalNPC.PopulatePoiseProfiles()` (GlobalNPC.cs) is the central table: `Add<MyEnemy>(knockBackResist,
poiseMax)`. It is applied LAST in `SetDefaults`, so it **overrides** any in-file `PoiseMax`/`knockBackResist`.

- **Never** write `PoiseMax = X` in an enemy file — it'll be silently overwritten.
- To give an enemy poise, add it to that table (or confirm it's there). Tiers exist (boss 0.15/120 … light 0.5/18).
- In the enemy file, leave a pointer comment so readers know where it's tuned:
  ```csharp
  // Poise (a stagger cancels the wind-up) + knockback flinch are tuned centrally in
  // tsorcRevampGlobalNPC.PopulatePoiseProfiles() (GlobalNPC.cs) — not here.
  ```
- Ghosts are special: excluded from the table; they get `PoiseMax = 12` from `EnableMagicGhostPoise` on a **magic**
  hit only (magic-gated stagger). The attack-phase flags still apply.

---

## Path A — AddAttack enemies (easy)

These already auto-tag (SimpleProjectile sets `AttackTelegraphing`/`AttackCommitted` each tick) and auto-cancel on
stagger (it resets `ProjectileTimer`). You only:

1. **Name + tune each attack** in a comment, and set `commitFraction` (and maybe `telegraphTime`):
   ```csharp
   // "Bio Spit" — aimed acid glob.
   UsefulFunctions.AddAttack(NPC, 140, ModContent.ProjectileType<...EnemyBioSpitBall>(), dmg, 8, sound,
       telegraphColor: Color.GreenYellow, telegraphTime: 40, commitFraction: 0.5f);
   ```
2. **Confirm poise** — it's in `PopulatePoiseProfiles()` (add it if not).
3. **Add evasion**: an `EvasiveProfile.X(globalNPC)` call in SetDefaults + the two `OnHitBy*` overrides:
   ```csharp
   public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
       => tsorcRevampAIs.EvasiveOnHit(NPC, true);
   public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
       => tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
   ```
4. **Remove redundant inline `justHit` resets** (the old "reset ProjectileTimer when hit" / jump-back code) — the
   poise stagger + EvasiveOnHit replace them. **Exception:** keep them on ghosts (their only *physical* interrupt,
   since they're magic-stagger only).
5. Optional: `CanJumpBeforeAttack`, `EvasiveProfile.EvasiveCloak`, `KiteRangeMin/Max`.

---

## Path B — Hand-rolled enemies (the BasiliskShifter recipe → BasiliskHunter)

Hand-rolled enemies drive attacks off `ai[1]` / `localAI[1]/[2]` / a custom timer and fire on random rolls inside
timer bands. The goal: make them **deterministic** (decide → telegraph → flash/commit → fire), tag the phases, and
wire stagger-cancel. This is exactly what was done to **BasiliskShifter** (use it as the worked example).

### Step 0 — survey the enemy
- Find its attack timer(s) and every fire condition (the `ai[1] >= X && rand` blocks).
- Note: which attacks are HP-phase-gated, which need clear space / LOS, single-shot vs spray.
- Find its on-hit hook. Old enemies call `tsorcRevampAIs.FighterOnHit(NPC, melee)` (a pre-poise combined
  timer-reset + jump/teleport reaction — **BasiliskHunter does this**) — that gets replaced.

### Step 1 — class + fields
```csharp
class BasiliskHunter : ModNPC, IStaggerable   // <-- add the interface
{
    // Deterministic timing constants (tune to taste).
    private const int AtkDecide = 60;    // roll + LOCK the attack; dust telegraph starts (interruptible)
    private const int AtkFlash  = 85;    // colored TelegraphFlash = commit instant (hyper-armor begins)
    private const int AtkFire   = 110;   // projectile(s) launch
    private const int AtkSprayEnd = 140; // for spray attacks; single shots reset ~10t after AtkFire
    private int lockedAttack = -1;       // -1 none; 0..N = which attack this wind-up committed to
    // (the enemy's existing localAI[1] becomes the phase clock, or use a private float field)
```

### Step 2 — lock the attack once, drive phases each tick
Replace the per-tick `rand` attack selection with a single locked roll at `AtkDecide`, so the dust/flash COLOUR and
the fired shot match (variety comes from the roll, determinism after it):
```csharp
phaseTimer++;                              // localAI[1] or a private field
if (lockedAttack == -1 && phaseTimer >= AtkDecide /* && not mid-other-attack */)
    lockedAttack = RollAttackForCurrentHP();   // pick from the valid set for this HP phase

bool atkActive   = lockedAttack != -1;
bool telegraphing = atkActive && phaseTimer >= AtkDecide && phaseTimer < AtkFlash;
bool committed    = atkActive && phaseTimer >= AtkFlash;

if (telegraphing) { /* dust telegraph, colour by lockedAttack */ }
if (atkActive && (int)phaseTimer == AtkFlash && Main.netMode != NetmodeID.MultiplayerClient)
    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
        ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer,
        UsefulFunctions.ColorToFloat(flashColorFor(lockedAttack)));   // purple/green/etc.
if (committed && Main.netMode != NetmodeID.MultiplayerClient)
    FireLockedAttack();                    // single shot at AtkFire, or spray AtkFire..AtkSprayEnd
if (atkActive && phaseTimer >= (isSpray ? AtkSprayEnd : AtkFire + 10))
    { phaseTimer = 0; lockedAttack = -1; } // reset after the shot/spray

// Hand the phase windows to the poise system:
globalNPC.AttackTelegraphing = telegraphing /* || otherAttackTelegraph */;
globalNPC.AttackCommitted    = committed    /* || otherAttackCommit  */;
```
Pull the projectile spawns into small `FireX(player)` helpers (verbatim params from the original). Drop quirks like
"only fire when velocity-sign matches facing" — after commit it should always fire.

**Multiple clocks:** if an attack runs on a *separate* timer (e.g. the basilisk breath / "magic ring" on
`breathTimer`), keep it, tag it (`AttackTelegraphing` during its DustRing tell, `AttackCommitted` while firing), and
PAUSE the main projectile machine while it's active (`breathActive` guard).

### Step 3 — IStaggerable cancels the wind-up
```csharp
public void OnStagger(NPC npc)            // called by the poise system on a break
{
    phaseTimer = 0;
    lockedAttack = -1;
    // reset any other attack clocks (e.g. if (breathTimer > 0) breathTimer = 0;)
}
```
(`PoiseStaggerResetsAI` only resets `ai[1]` and won't help `localAI`/custom timers — that's why hand-rolled enemies
implement `IStaggerable`.)

### Step 4 — swap the on-hit hook + add evasion
```csharp
public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
    => tsorcRevampAIs.EvasiveOnHit(NPC, true);
public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
    => tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
// in SetDefaults:
EvasiveProfile.Basilisk(globalNPC);       // or the right profile / inline flags
```
Delete the old `FighterOnHit` call and any inline `justHit` timer-reset / jump-back blocks (now handled by stagger +
EvasiveOnHit). The old offensive-jump flourishes can come back via the global `CanJumpBeforeAttack` variation.

### Step 5 — knockback + poise cleanup
Delete any inline `NPC.knockBackResist = ...` per-HP-phase conditional — the central table manages knockback now.
Confirm the enemy is in `PopulatePoiseProfiles()` and add the pointer comment.

### Step 6 — verify
- Braces balanced; `using Terraria.ModLoader;` present (for `DamageClass`).
- In-game (tModLoader builds in-game): cast through a hit once committed (hyper-armor → **white** bar), get
  cancelled by a stagger during the telegraph, evasion fires in neutral. Tune `AtkDecide/Flash/Fire`, the dust/flash
  colours, and the spray spacing for feel.

---

## Attack naming + flash colours (convention)
- Give every attack a `// "Name"` comment (e.g. `"Poison Storm"`, `"Magic Ring"`, `"Shadow Shot"`).
- Flash/dust colour by family: green = poison/bio, purple = magic/disrupter, orange = thrown/physical, blue = ice.
  Keep an AddAttack enemy's `telegraphColor` matching its hand-rolled cousin so a family reads consistently.

## Profiles (`NPCs/EvasiveProfile.cs`)
A profile is just a documented bundle of flag assignments — one method per enemy/family that shares it
(`RedKnight`, `Basilisk`, `DworcShaman`, `DworcSniper`, `Ghost`). One-off enemies set flags inline. Add a new
`EvasiveProfile.X` when ≥2 enemies share a kit.
