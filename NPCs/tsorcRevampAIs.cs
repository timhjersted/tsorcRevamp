using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using tsorcRevamp.Buffs;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Buffs.Runeterra.Magic;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Buffs.Weapons;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Buffs.Weapons.Summon.WhipDebuffs;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Accessories.Defensive;
using tsorcRevamp.Items.Accessories.Damage;
using tsorcRevamp.Items.Armors.Melee;
using tsorcRevamp.Items.Debug;
using tsorcRevamp.Items.ItemCrates;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.VanillaItems;
using tsorcRevamp.Items.Weapons.Magic.Runeterra;
using tsorcRevamp.Items.Weapons.Ranged;
using tsorcRevamp.Items.Weapons.Ranged.Runeterra;
using tsorcRevamp.Items.Weapons.Ranged.Specialist;
using tsorcRevamp.Items.Weapons.Summon;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.Items.Weapons.Summon.Whips;
using tsorcRevamp.Items.Weapons.Enemy;
using tsorcRevamp.Items.Weapons.Throwing;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;
using tsorcRevamp.Projectiles.Ranged;
using tsorcRevamp.Projectiles.Summon;
using tsorcRevamp.Projectiles.Summon.Archer;
using tsorcRevamp.Projectiles.Summon.SamuraiBeetle;
using tsorcRevamp.Projectiles.Summon.Whips;
using tsorcRevamp.Projectiles.Summon.Whips.Dominatrix;
using tsorcRevamp.Projectiles.Summon.Whips.EnchantedWhip;
using tsorcRevamp.Projectiles.Summon.Whips.PolarisLeash;
using tsorcRevamp.Projectiles.VFX;
using tsorcRevamp.Utilities;
using tsorcRevamp;

namespace tsorcRevamp.NPCs
{
    public static class tsorcRevampAIs
    {
        private const int SmokeFireTeleportCloudTicks = 60;
        private const int SmokeFireTeleportSnapTicks = 30;
        private const float TeleportMistVisualScale = 1.25f;
        private const float FireTeleportFlameSpeed = 5.6f;
        private const int FireTeleportFlameCount = 12;
        private const float FireTeleportFlameDamageMultiplier = 0.25f;

        ///<summary> 
        ///Walking AI that walks toward the player. Can be used with SimpleProjectile to fire projectiles, or LeapAtPlayer to leap when the player is close
        ///</summary>
        ///<param name="npc">The npc itself this function will run on</param>
        ///<param name="topSpeed">The max speed it can run at</param>
        ///<param name="acceleration">How quickly it can speed up</param>
        ///<param name="brakingPower">How quickly it can slow down</param>
        ///<param name="canTeleport">Lets it teleport near the player when it gets bored instead of walking around randomly</param>
        ///<param name="doorBreakingDamage">Setting this above 0 lets the npc break doors, and sets much damage should it deal when it hits them. Doors have 10 "health"</param>
        ///<param name="hatesLight">Should it run away during daylight?</param>
        ///<param name="randomSound">What sound should it randomly play?</param>
        ///<param name="soundFrequency">How often does it play its sound?</param>
        ///<param name="enragePercent">Accelerates twice as fast when below this % health</param> 
        ///<param name="enrageTopSpeed">Its new top speed when enraged</param>
        ///<param name="lavaJumping">Lets it hop around in lava</param>
        public static void FighterAI(NPC npc, float topSpeed = 1f, float acceleration = .07f, float brakingPower = .2f, bool canTeleport = false, int doorBreakingDamage = 4, bool hatesLight = false, SoundStyle? randomSound = null, int soundFrequency = 1000, float enragePercent = 0, float enrageTopSpeed = 0, bool lavaJumping = false, bool canDodgeroll = true, bool canPounce = true, int minSurfaceWidth = 0, bool canWalkBackwards = false)
        {
            npc.aiStyle = -1;
            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.MinSurfaceWidth = minSurfaceWidth; // > 0 => RequiresFlatGround (large enemies; avoid slopes/narrow ledges)
            globalNPC.CanWalkBackwards = canWalkBackwards;
            BasicAI(npc, topSpeed, acceleration, brakingPower, false, canTeleport, doorBreakingDamage, hatesLight, randomSound, soundFrequency, enragePercent, enrageTopSpeed, lavaJumping, canDodgeroll, canPounce);
        }

        ///<summary> 
        ///Special version of the fighter ai, stopping to shoot when the player is within range. Gets bored if it doesn't have line of sight to the player, and if it can teleport it will attempt to warp to a position with a clean shot.
        ///Uses npc.ai[2] to control aim direction!! Do not set it yourself if an NPC uses ArcherAI
        ///</summary>         
        ///<param name="npc">The npc itself this function will run on</param>
        ///<param name="projectileType">The ID of the projectile you want to shoot</param>
        ///<param name="projectileDamage">Damage of the projectile. Multiplied by 2 by default, and then 2 again in expert mode</param>
        ///<param name="projectileVelocity">Speed of the projectile</param>
        ///<param name="projectileCooldown">Sets the delay (in ticks) between shots</param>
        ///<param name="topSpeed">The max speed it can run at</param>
        ///<param name="acceleration">How quickly it can speed up</param>
        ///<param name="brakingPower">How quickly it can slow down</param>
        ///<param name="canTeleport">Lets it teleport near the player when it gets bored instead of walking around randomly</param>
        ///<param name="hatesLight">Should it run away during daylight? (UNIMPLEMENTED!)</param>
        ///<param name="shootSound">What sound should it play?</param>
        ///<param name="soundFrequency">How often does it play its sound?</param>
        ///<param name="enragePercent">Below this percent health, doubles speed and acceleration</param>
        ///<param name="lavaJumping">Lets it hop around in lava</param>
        ///<param name="projectileGravity">How much is the projectile's y velocity reduced each tick? Set 0 for projectiles with no gravity. If your projectile has custom gravity dropoff, stick that here.</param>
        ///<param name="shootSound">The type of sound to play when it shoots. Defaults to bow.</param>
        ///<param name="telegraphTicks">How many ticks before firing to flash the telegraph and lock aim.</param>
        public static void ArcherAI(NPC npc, int projectileType, int projectileDamage, float projectileVelocity, int projectileCooldown, float topSpeed = 1f, float acceleration = .07f, float brakingPower = .2f, bool canTeleport = false, int doorBreakingDamage = 4, bool hatesLight = false, SoundStyle? randomSound = null, int soundFrequency = 1000, float enragePercent = 0, float enrageTopSpeed = 0, bool lavaJumping = false, float projectileGravity = 0.035f, SoundStyle? shootSound = null, bool canDodgeroll = true, bool canPounce = false, Color? telegraphColor = null, int telegraphTicks = 15)
        {
            BasicAI(npc, topSpeed, acceleration, brakingPower, true, canTeleport, doorBreakingDamage, hatesLight, randomSound, soundFrequency, enragePercent, enrageTopSpeed, lavaJumping, canDodgeroll, false);
            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();

            if (telegraphColor == null)
            {
                telegraphColor = Color.Gray;
            }
            telegraphTicks = Math.Max(1, telegraphTicks);

            //Set default shoot sound
            if (shootSound == null)
            {
                shootSound = SoundID.Item5;
            }

            //Apply scaling to SHM enemies
            if (npc.ModNPC != null && npc.ModNPC.Mod == ModLoader.GetMod("tsorcRevamp"))
            {
                if (!npc.boss)
                {
                    if (npc.ModNPC.GetType().Namespace.Contains("SuperHardMode"))
                    {
                        projectileDamage = (int)(tsorcRevampWorld.SHMScale * projectileDamage);
                        projectileVelocity = (int)(tsorcRevampWorld.SubtleSHMScale * projectileVelocity);
                    }
                }
            }

            npc.aiStyle = -1;
            if (npc.confused)
            {
                globalNPC.ArcherAimDirection = 0f; // won't try to stop & aim if confused
            }
            else
            {
                int scaledProjectileCooldown = Math.Max(telegraphTicks * 2, (int)(projectileCooldown * globalNPC.CastingSpeed));
                int fireTick = scaledProjectileCooldown / 2;
                int telegraphTick = fireTick + telegraphTicks;

                if (globalNPC.ProjectileTimer > 0f)
                    globalNPC.ProjectileTimer -= 1f; // decrement fire & reload counter

                // Don't let airborne state abort a shot once the telegraph has already fired.
                bool inTelegraphWindow = globalNPC.ProjectileTimer <= telegraphTick && globalNPC.ProjectileTimer > fireTick;
                if (npc.justHit || (npc.velocity.Y != 0f && !inTelegraphWindow) || globalNPC.ProjectileTimer <= 0f)
                {
                    globalNPC.ProjectileTimer = scaledProjectileCooldown; //Reset firing time
                    globalNPC.ArcherAimDirection = 0f; //Not aiming
                    // If standing-fire has remaining shots and we're only resetting due to cooldown,
                    // immediately re-enter aiming state for the next volley shot.
                    if (!npc.justHit && globalNPC.FighterRangedStandShotsRemaining > 0)
                        globalNPC.ArcherAimDirection = 3f;
                }

                //Check if we're in range of and can hit the player
                if (!globalNPC.CanPassThroughWalls && Vector2.Distance(npc.Center, Main.player[npc.target].Center) < 700f && Collision.CanHit(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1) && Collision.CanHitLine(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1) && npc.velocity.Y == 0)
                {
                    //If it's not aiming yet, then slow down, aim, and start its cooldown
                    if (globalNPC.ArcherAimDirection == 0)
                    {
                        //Aim at them, and start the shot cooldown
                        npc.velocity.X *= 0.5f;
                        globalNPC.ArcherAimDirection = 3f;
                        globalNPC.ProjectileTimer = scaledProjectileCooldown;
                        // Clear any stale lock from a previous aim cycle. The lock before the shot
                        // only sets LockedShotVector while grounded; if this cycle's lock frame is missed (e.g.
                        // the archer is airborne then), the fire-time fallback below re-aims instead of firing a
                        // stale/zero vector (which spawned a zero-velocity arrow that just dropped — the Assassin bug).
                        globalNPC.LockedShotVector = Vector2.Zero;

                        // Standing-fire roll: tier-2 NPCs may plant their feet and fire N shots
                        // before resuming pursuit. High Aggression skips this; high Patience adds shots.
                        if (globalNPC.CanStopToFire && globalNPC.FighterRangedStandShotsRemaining == 0
                            && Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float stopBeforeChance = GetStandingFireChance(globalNPC, 0.1f);
                            if (Main.rand.NextFloat() < stopBeforeChance)
                            {
                                // 1 shot at low Patience, up to 3 shots at high Patience
                                globalNPC.FighterRangedStandShotsRemaining = 1 + Main.rand.Next(0, 1 + (int)globalNPC.Patience);
                            }
                        }
                    }

                    // Standing-fire: fully pin velocity so the NPC holds position and
                    // shows a standing animation frame rather than a walk/jump frame.
                    if (globalNPC.FighterRangedStandShotsRemaining > 0)
                    {
                        npc.velocity.X = 0f;
                        npc.velocity.Y = 0f;
                    }
                    else
                    {
                        npc.velocity.X *= 0.9f; // decelerate to stop & shoot
                        npc.velocity.Y = 0f;    // suppress jump-frame animation while aiming
                    }
                    npc.spriteDirection = npc.direction; // match animation to facing

                    // Telegraph fires before the shot: lock the aim direction now so
                    // a dodge-roll behind the enemy can't redirect the incoming projectile.
                    if (globalNPC.ProjectileTimer == telegraphTick)
                    {
                        globalNPC.LockedShotVector = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].Center, projectileVelocity, projectileGravity);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 spawnPosition = npc.position;
                            if (npc.direction == 1)
                            {
                                spawnPosition.X += npc.width;
                            }
                            Projectile.NewProjectileDirect(npc.GetSource_FromThis(), spawnPosition, npc.velocity, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(telegraphColor.Value));
                        }
                    }

                    //Fire at halfway through: first half of delay is aim, 2nd half is cooldown
                    if (globalNPC.ProjectileTimer == fireTick)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            // Fallback: if the early lock was skipped this cycle (archer airborne at the lock
                            // frame), LockedShotVector is still Zero — re-aim now so the shot fires properly
                            // instead of spawning with zero velocity and dropping. When the lock DID happen the
                            // value is non-zero and is preserved (the anti-dodgeroll aim-lock stays intact).
                            if (globalNPC.LockedShotVector == Vector2.Zero)
                            {
                                globalNPC.LockedShotVector = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].Center, projectileVelocity, projectileGravity);
                            }

                            // Spawn the shot just in front of the NPC rather than at its center. These projectiles
                            // are spawned player-owned (friendly) and flipped hostile a frame later, so a spawn
                            // overlapping the shooter's own hitbox lets the still-friendly projectile damage it.
                            Vector2 shotDir = globalNPC.LockedShotVector.SafeNormalize(new Vector2(npc.direction, 0f));
                            Vector2 shotSpawn = npc.Center + shotDir * (npc.width / 2f + 10f);
                            Projectile.NewProjectile(npc.GetSource_FromThis(), shotSpawn.X, shotSpawn.Y, globalNPC.LockedShotVector.X, globalNPC.LockedShotVector.Y, projectileType, projectileDamage, 0f, Main.myPlayer);
                        }

                        SoundEngine.PlaySound(shootSound.Value);

                        // Consume one standing-fire charge per shot
                        if (globalNPC.FighterRangedStandShotsRemaining > 0)
                        {
                            if (--globalNPC.FighterRangedStandShotsRemaining == 0)
                            {
                                // All charges spent — exit standing-fire and resume pursuit
                                globalNPC.ArcherAimDirection = 0f;
                                npc.TargetClosest(true);
                            }
                        }
                    }

                    // Only track the player visually while we haven't yet committed to a shot direction
                    if (!inTelegraphWindow)
                    {
                        Vector2 aimVector = UsefulFunctions.Aim(npc.Center, Main.player[npc.target].Center, projectileVelocity);

                        if (Math.Abs(aimVector.Y) > Math.Abs(aimVector.X) * 2f) // target steeply above/below NPC
                        {
                            if (aimVector.Y > 0f)
                                globalNPC.ArcherAimDirection = 1f; // aim downward
                            else
                                globalNPC.ArcherAimDirection = 5f; // aim upward
                        }
                        else if (Math.Abs(aimVector.X) > Math.Abs(aimVector.Y) * 2f) // target on level with NPC
                            globalNPC.ArcherAimDirection = 3f;  //  aim straight ahead
                        else if (aimVector.Y > 0f) // target is below NPC
                            globalNPC.ArcherAimDirection = 2f;  //  aim slight downward
                        else // target is not below NPC
                            globalNPC.ArcherAimDirection = 4f;  //  aim slight upward
                    }
                }
                //If we're out of range of the player, don't aim at them
                else
                {
                    globalNPC.ArcherAimDirection = 0;
                    globalNPC.FighterRangedStandShotsRemaining = 0; // abort standing-fire if target leaves range
                }
            }

            npc.ai[2] = globalNPC.ArcherAimDirection;
        }



        //Todo:
        //Upgrade gap-jumping code to scale jump x and  y velocity with gap size, up to a limit
        //Upgrade wall-jumping code to scale jump height with how tall the wall in front of it is. Also let it recognize walls with gaps in them.
        //More complex "bored" check than simple velocity. Right now it can get bored if it takes too long doing things that require it to move slow.
        private static void BasicAI(NPC npc, float topSpeed, float acceleration, float brakingPower, bool isArcher, bool canTeleport = false, int doorBreakingDamage = 0, bool hatesLight = false, SoundStyle? randomSound = null, int soundFrequency = 1000, float enragePercentage = 0, float enrageTopSpeed = 0, bool lavaJumping = false, bool canDodgeroll = true, bool canPounce = true)
        {
            npc.noTileCollide = false;

            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.RunningCustomFighterAI = true; // mark for PostAI's confusion handling
            globalNPC.CanDodgeroll = canDodgeroll;   // expose for the on-hit wall-pin escape
            if (globalNPC.FighterEvasionCooldown > 0)
            {
                globalNPC.FighterEvasionCooldown--;
            }
            // Poise enemies: restore the captured SetDefaults knockback resist every tick so it stays the flinch dial even
            // if the enemy's own AI zeroes knockBackResist during its attacks (those lines run after this and still apply).
            if (globalNPC.PoiseMax > 0f && globalNPC.BaseKnockBackResist >= 0f)
            {
                npc.knockBackResist = globalNPC.BaseKnockBackResist;
            }
            if (npc.target < 0 || npc.target >= Main.maxPlayers || !Main.player[npc.target].active || Main.player[npc.target].dead)
            {
                npc.TargetClosest(false);
            }
            if (globalNPC.HealthScaledSpeedBase >= 0)
                topSpeed = (npc.life / (float)npc.lifeMax) * globalNPC.HealthScaledSpeedMultiplier + globalNPC.HealthScaledSpeedBase;
            topSpeed *= globalNPC.Swiftness;
            acceleration *= globalNPC.Swiftness;
            if (globalNPC.FighterNoLosPursuitBoostTimer > 0)
            {
                globalNPC.FighterNoLosPursuitBoostTimer--;
                topSpeed *= 1.25f;
                acceleration *= 1.35f;
            }
            // RunningDash burst: a grounded sprint toward the player. Driven as a top-speed multiplier (NOT a velocity
            // override) so whichever mover is active stays in control of pursuit + terrain. Telegraph phase excluded.
            if (globalNPC.InSustainedEvasion && globalNPC.CurrentEvasion == EvasiveBehavior.RunningDash && !globalNPC.EvasiveTelegraphing)
            {
                topSpeed *= globalNPC.EvasiveDashSpeedMult;
                acceleration *= globalNPC.EvasiveDashSpeedMult;
            }

            if (!globalNPC.Initialized)
            {
                // Capture the pristine SetDefaults knockback resist before any AI tick mutates it — the poise flinch and
                // attack-state restores read this so the per-enemy SetDefaults value stays the source of truth.
                globalNPC.BaseKnockBackResist = npc.knockBackResist;

                //Make damage and health scale with strength
                npc.damage = (int)(npc.damage * globalNPC.Strength);
                npc.life = (int)(npc.life * globalNPC.Strength);
                npc.lifeMax = (int)(npc.lifeMax * globalNPC.Strength);
                npc.scale *= (float)Math.Pow(globalNPC.Strength, 0.5f); //Make 'scale' only increase with the square root of strength, to make it change less dramatically

                //Make low-frequency attacks somewhat more likely
                foreach (ProjectileData data in globalNPC.AttackList)
                {
                    data.timerCap = (int)(data.timerCap * globalNPC.CastingSpeed);
                    if (data.weight < 1)
                    {
                        data.weight += (1 - data.weight) * globalNPC.Adeptness;
                    }
                }

                // Legacy `canTeleport: true` AI param → unlimited Normal blink (≈ its current feel). Enemies may
                // instead set CanTeleport / TeleportStyle / TeleportMaxCharges directly in SetDefaults (those win).
                if (!globalNPC.CanTeleport && canTeleport)
                {
                    globalNPC.CanTeleport = true;
                }

                globalNPC.Initialized = true;
            }

            // Seed the charge pool once (after SetDefaults + the legacy migration above have set MaxCharges).
            if (!globalNPC.TeleportChargesInitialized)
            {
                globalNPC.TeleportChargesRemaining = globalNPC.TeleportMaxCharges < 0 ? int.MaxValue : globalNPC.TeleportMaxCharges;
                globalNPC.TeleportChargesInitialized = true;
            }

            // Fleeing: a low-HP flee (set on hit) or a hatesLight enemy caught in daylight above ground.
            // Replaces the old BoredTimer = -999 sentinel; gates firing + reverses facing below.
            // (Hoisted above the combat layer so RunFighterCombatExec can read it; nothing in attacks/
            //  pounce/dodge depends on it, so this move is order-safe.)
            bool fleeing = globalNPC.Fleeing || (hatesLight && Main.dayTime && (npc.position.Y / 16f) < Main.worldSurface);

            // ── Combat layer (Phase 2 Step 1: combat/movement separation) ────────────────────────────
            // Combat is being pulled out of this god-method so the movement half can later be swapped for
            // SmartFighter4AI. RunFighterCombatExec runs the start-of-frame combat: fire attacks, advance an
            // in-progress pounce/dodge, the teleport countdown, and block-firing-while-busy. It fills `intent`
            // (SeizesBody / HoldForAttack). NOTE: the movement code below does NOT yet read `intent` — wiring
            // movement to honor it is Step 3; it's populated now only so the contract surface exists.
            // See Documentation/CombatMovementSeparation_Plan.md.
            FighterCombatIntent intent = default;
            RunFighterCombatExec(npc, globalNPC, topSpeed, fleeing, ref intent);

            //Apply scaling to SHM enemies
            if (npc.ModNPC != null && npc.ModNPC.Mod == ModLoader.GetMod("tsorcRevamp"))
            {
                if (!npc.boss)
                {
                    if (npc.ModNPC.GetType().Namespace.Contains("SuperHardMode"))
                    {
                        topSpeed *= tsorcRevampWorld.SHMScale;
                        acceleration *= tsorcRevampWorld.SubtleSHMScale;
                        enrageTopSpeed *= tsorcRevampWorld.SHMScale;
                    }
                }
            }


            //If it has a sound to play, roll a chance for playing it
            if (randomSound != null && Main.rand.Next(soundFrequency) <= 0)
            {
                SoundEngine.PlaySound(randomSound.Value, npc.Center);
            }

            //If we can enrage, do that
            if (npc.life < (float)npc.lifeMax * enragePercentage)
            {
                acceleration *= 2;
                topSpeed = enrageTopSpeed;
            }

            //If it can jump in lava and is in lava, do that
            if (lavaJumping && npc.lavaWet)
            {
                npc.velocity.Y -= 2;
            }

            //If fleeing, despawn as soon as it's offscreen (via timeLeft running out)
            if (fleeing)
            {
                npc.timeLeft = 10;
            }

            // ── Patrol/Pursue FSM + unified teleport (Phase 1 Steps 4a/4b — tier-0 path only) ────────
            // Advance the shared macro-state machine; when it gives up the chase, either blink to re-acquire
            // (the styled disengage resolver) or take over with patrol locomotion. Fleeing enemies are left to
            // the despawn path above. See Documentation/PatrolPursue_and_NavTier_Removal.md.
            if (!fleeing)
            {
                const float FighterAggroRange = 2000f; // generous: an LOS sighting re-aggros like the old BoredTimer reset
                Player fsmPlayer = Main.player[npc.target];

                // Being hit forces a re-acquire (mirrors the old BoredTimer = 0 on justHit). For a beast this also
                // breaks it out of a stale wander (RegisterHitForBeast already cleared it from the hit hook; clearing
                // here too keeps the re-engage local so the stale overlay below can't re-force Patrol this frame).
                if (npc.justHit)
                {
                    globalNPC.PursuitState = PursuitState.Pursue;
                    globalNPC.DisengageTimer = 0;
                    globalNPC.BeastStale = false;
                    globalNPC.BeastUnreachableFrames = 0;
                }

                // TRUE line of sight: Player.CanHit (== Collision.CanHit) is the permissive trajectory check that
                // tolerates stepping over obstacles and reports "LOS" through complex terrain (e.g. straight up
                // through a cave). For the give-up / re-acquire decision we need the strict straight-line check,
                // so pair it with Collision.CanHitLine exactly like the archer's "can I shoot?" gate. Solids
                // block; platforms (solidTop) don't.
                bool fsmLos = FighterHasLineOfSight(npc, fsmPlayer);
                float fsmDist = npc.Distance(fsmPlayer.Center);
                // Progress = closing distance toward the target since last frame (tier-0 has no path to
                // count as an in-progress "real move"; the anti-stuck detector below handles the
                // visible-but-walled case where LOS would otherwise keep resetting the give-up clock).
                bool fsmProgress = globalNPC.LastPursuitDist <= 0f || fsmDist < globalNPC.LastPursuitDist - 0.5f;
                globalNPC.LastPursuitDist = fsmDist;

                // Large-beast stale-wander overlay: a giant that can't reach the player (BeastUnreachableFrames,
                // maintained by SF4's positioner) and hasn't been hit for ~BeastStaleWanderTicks loses interest and
                // wanders — even WITH line of sight. While BeastStale it ignores LOS re-aggro (so it actually wanders
                // off); a hit clears BeastStale + forces Pursue (RegisterHitForBeast). Only beasts; everyone else
                // runs the normal FSM untouched.
                PursuitState fsmState;
                if (globalNPC.RequiresFlatGround && globalNPC.BeastStale)
                {
                    if (globalNPC.PursuitState != PursuitState.Patrol) NavBehavior.EnterPatrol(npc, globalNPC);
                    globalNPC.PursuitState = PursuitState.Patrol;
                    fsmState = PursuitState.Patrol;
                }
                else
                {
                    fsmState = NavBehavior.UpdateState(npc, globalNPC, fsmPlayer, fsmLos, fsmProgress, FighterAggroRange);
                    if (globalNPC.RequiresFlatGround
                        && globalNPC.BeastUnreachableFrames > 120
                        && globalNPC.FramesSinceHit > globalNPC.BeastStaleWanderTicks)
                    {
                        globalNPC.BeastStale = true;
                        NavBehavior.EnterPatrol(npc, globalNPC);
                        fsmState = PursuitState.Patrol;
                    }
                }

                if (globalNPC.TeleportCooldownTimer > 0) globalNPC.TeleportCooldownTimer--;

                // Lava escape: ANY teleporter caught in lava blinks to safe ground near the player — survival for
                // non-lava-immune enemies, repositioning for immune ones (mirrors the old RingedKnight lava-escape).
                // Bypasses the FSM give-up gate below so it fires even mid-pursuit, and uses its own short cooldown.
                if (globalNPC.CanTeleport && npc.lavaWet && globalNPC.TeleportCountdown == 0
                    && globalNPC.TeleportAppearanceTimer == 0 && globalNPC.TeleportChargesRemaining > 0
                    && globalNPC.TeleportCooldownTimer == 0)
                {
                    if (!TryTeleportOutOfLava(npc, globalNPC))
                        globalNPC.TeleportCooldownTimer = 30; // no safe spot this attempt — throttle the retry
                }

                // Disengage resolver (4b): blink to re-acquire when the give-up condition for this style is met.
                // Charges + per-style cooldown gate it; out of charges / on cooldown / no valid spot → falls
                // through to Patrol. After a blink the NPC has LOS → FSM → Pursue.
                if (globalNPC.CanTeleport && globalNPC.TeleportCountdown == 0 && globalNPC.TeleportAppearanceTimer == 0
                    && globalNPC.TeleportChargesRemaining > 0 && globalNPC.TeleportCooldownTimer == 0)
                {
                    bool fireTeleport;
                    switch (globalNPC.TeleportStyle)
                    {
                        case TeleportStyle.Aggressive:
                            // Blink the moment LOS is lost (short debounce so a pillar flicker doesn't spam it),
                            // bypassing Search/Patrol entirely.
                            fireTeleport = !fsmLos && globalNPC.DisengageTimer >= 30;
                            break;
                        case TeleportStyle.Relaxed:
                            // Give up, wander a few seconds, THEN blink.
                            fireTeleport = fsmState == PursuitState.Patrol && globalNPC.PatrolElapsed >= 300;
                            break;
                        default: // Normal — blink at the disengage point instead of patrolling.
                            fireTeleport = fsmState == PursuitState.Patrol || fsmState == PursuitState.Search;
                            break;
                    }
                    // A stale beast deliberately lost interest (can't reach you + un-hit ~10s) — don't let the
                    // reacquire-teleport yank it straight back; let it actually wander. A hit clears BeastStale and
                    // re-enables this. (Lava-escape teleport above is separate and still fires.)
                    if (globalNPC.RequiresFlatGround && globalNPC.BeastStale) fireTeleport = false;
                    if (fireTeleport && !TryTeleportReacquire(npc, globalNPC))
                    {
                        // No valid spot this attempt — throttle the (100×100) search so it doesn't run every
                        // frame while the NPC patrols; it'll retry shortly or fall through to Patrol.
                        globalNPC.TeleportCooldownTimer = 30;
                    }
                }

                if (fsmState == PursuitState.Patrol)
                {
                    // Mid-blink (a teleport was just queued, counting down, or appearance is pending) → hold; otherwise patrol.
                    if (globalNPC.TeleportCountdown == 0 && globalNPC.TeleportAppearanceTimer == 0)
                    {
                        if (globalNPC.CanPassThroughWalls)
                        {
                            // Ghost enemies always drift toward the player even in "patrol" — wandering away from
                            // a wall they can't pathfind through means TryGhostWallTeleport never fires.
                            // By maintaining forward velocity they will walk into the wall, build GhostWallTimer,
                            // and phase through via the wall teleport as designed.
                            float dirX = Math.Sign(fsmPlayer.Center.X - npc.Center.X);
                            if (dirX != 0f)
                            {
                                npc.direction = (int)dirX;
                                npc.spriteDirection = npc.direction;
                            }
                            npc.velocity.X = MathHelper.Lerp(npc.velocity.X, npc.direction * topSpeed, acceleration / topSpeed);
                            if (!npc.noTileCollide && !npc.noGravity) AutoStepUp(npc);
                        }
                        else
                        {
                            NavBehavior.RunPatrol(npc, globalNPC, topSpeed, acceleration);
                            if (!npc.noTileCollide && !npc.noGravity) AutoStepUp(npc);
                        }
                    }
                    // Don't advance an LOS-REQUIRING attack while patrolling (it gave up / can't see the target).
                    // Attacks that don't need LOS keep charging + firing (so AOEs like the poison storm aren't frozen
                    // when the player ducks out of sight). Aim direction always clears (aiming needs LOS).
                    if (globalNPC.AttackList.Count == 0 || globalNPC.CurrentAttack.needsLineOfSight)
                    {
                        globalNPC.ProjectileTimer = 0f;
                    }
                    globalNPC.ArcherAimDirection = 0f;
                    LogFighterNavDebug(npc, globalNPC, fsmLos);
                    return;
                }
            }

            //If not actively engaging (lost LOS / searching) or fleeing, retarget the closest player it can see.
            if (globalNPC.PursuitState != PursuitState.Pursue || fleeing)
            {
                float distance = 9999999;
                int target = -1;
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    if (Main.player[i].active && !Main.player[i].dead)
                    {
                        if (Main.player[i].CanHit(npc))
                        {
                            float playerDistance = Main.player[i].Distance(npc.Center);
                            if (playerDistance < distance)
                            {
                                distance = playerDistance;
                                target = i;
                            }
                        }
                    }
                    if (target != -1)
                    {
                        npc.target = target;
                    }
                    else
                    {
                        npc.TargetClosest(false);
                    }
                }
            }

            // ── Combat sense + pluggable movement substrate (Phase 2 Step 2/3/4: combat/movement separation) ─
            // Compute LOS once, let the combat layer DECIDE the stop-and-fire (SenseHoldForAttack →
            // intent.HoldForAttack), then run ONE of two interchangeable movers and refresh LOS for the log +
            // triggers. The enemy's "smartness" is the single NavSearchRadius lever:
            //   NavSearchRadius == 0  → LocalMover (cheap reactive; keeps interleaving with pounce/dodge).
            //   NavSearchRadius  > 0  → SmartFighter4AI A* pathing in movementOnly mode (FSM + attacks already
            //                           done here, so it ONLY moves). Combat (attacks/pounce/dodge/teleport/aim)
            //                           is unchanged either way — it lives above this line.
            bool lineOfSight = Main.player[npc.target].CanHit(npc);
            intent.HoldForAttack = SenseHoldForAttack(npc, globalNPC, lineOfSight);
            if (globalNPC.NavSearchRadius > 0)
            {
                // SF4 has no isArcher accel-gate of its own, so fold the archer-aim hold into holdForAttack HERE
                // (only at the SF4 call-site, leaving the verified LocalMover path untouched).
                bool sf4Hold = intent.HoldForAttack || (isArcher && globalNPC.ArcherAimDirection != 0f);
                // SeizesBody guard: combat/teleport owns the body this frame, so don't let A* pathing fight it.
                if (!intent.SeizesBody)
                    SmartFighter4AI.Run(npc, topSpeed, acceleration, doorBreakingDamage, 700f, movementOnly: true, holdForAttack: sf4Hold, brakingPower: brakingPower);
                lineOfSight = Main.player[npc.target].CanHit(npc); // refresh for the log + triggers
            }
            else
            {
                lineOfSight = LocalMover.Run(npc, globalNPC, topSpeed, acceleration, brakingPower, isArcher, doorBreakingDamage, fleeing, lineOfSight, ref intent);
            }

            // SF4 enemies already logged this frame's movement via the smart log (LogFrame) on the Pursue/Search
            // path, so only emit the FSM-layer line here when the SF4 mover was SKIPPED (combat seized the body —
            // pounce/dodge/teleport). LocalMover enemies (radius 0) always log. Patrol frames are logged via the
            // early-return path above. This keeps SF4 enemies to a single, non-duplicated shared log file.
            if (globalNPC.NavSearchRadius == 0 || intent.SeizesBody)
                LogFighterNavDebug(npc, globalNPC, lineOfSight);

            // Combat triggers (Step 1): the end-of-frame decision to START a dodge or pounce. Runs after the
            // movement phase refreshed `lineOfSight`; the timers it sets are executed at the top of the NEXT
            // frame by RunFighterCombatExec.
            RunFighterCombatTriggers(npc, globalNPC, fleeing, lineOfSight, canDodgeroll, canPounce);
        }





        // The combat→movement hand-off struct `FighterCombatIntent` now lives at namespace scope in
        // NPCs/LocalMover.cs (shared by this combat layer + both movers). Referenced here by simple name.

        // Start-of-frame combat execution, extracted verbatim from BasicAI's top (pure refactor, same order):
        // fire attacks, advance an in-progress pounce/dodge (incl. their cooldown ticks), run the teleport
        // countdown, and block firing while busy. Fills `intent.SeizesBody`. `topSpeed` is the PRE-enrage value
        // (this code runs before enrage scaling), preserving the original pounceSpeed = topSpeed * 5.
        private static void RunFighterCombatExec(NPC npc, tsorcRevampGlobalNPC globalNPC, float topSpeed, bool fleeing, ref FighterCombatIntent intent)
        {
            //If it has at least one attack, perform it
            if (globalNPC.AttackList.Count > 0)
            {
                SimpleProjectile(npc);
            }

            if (globalNPC.PounceTimer > 0)
            {
                globalNPC.PounceTimer--;

                SpawnHighArcPounceTelegraph(npc, globalNPC);

                if (globalNPC.PounceStyle == PounceStyle.DirectPounce)
                {
                    RunDirectPounce(npc, globalNPC, topSpeed);
                }
                else if (globalNPC.PounceTimer == 0)
                {
                    LaunchHighArcPounce(npc, topSpeed);
                }
            }
            else if (globalNPC.PounceCooldown > 0)
            {
                globalNPC.PounceCooldown--;
            }

            if (globalNPC.DirectPounceAfterimageTimer > 0)
            {
                if (globalNPC.DirectPounceAfterimages && globalNPC.DirectPounceAfterimageTimer % 2 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    SpawnDirectPounceAfterimage(npc);
                }

                globalNPC.DirectPounceAfterimageTimer--;
            }

            if (globalNPC.DirectPounceRecoveryTimer > 0)
            {
                if (globalNPC.DirectPounceAfterimageTimer == 0 && npc.velocity.Y == 0f)
                {
                    npc.velocity.X *= 0.85f;
                }

                globalNPC.DirectPounceRecoveryTimer--;
            }

            if (globalNPC.DodgeTimer > 0)
            {
                npc.rotation += MathHelper.TwoPi / 30f * npc.direction;
                npc.velocity.X = 5 * npc.direction;

                globalNPC.DodgeTimer--;
                if (globalNPC.DodgeTimer == 0)
                {
                    npc.velocity.X = 0;
                }
            }
            else
            {
                npc.rotation = 0;

                if (globalNPC.DodgeCooldown > 0)
                {
                    globalNPC.DodgeCooldown--;
                }
            }

            // Quick step: a standing i-frame dash WITHOUT sprite rotation. The
            // i-frames + pass-through are handled by the CanBeHit*/CanHitPlayer hooks reading QuickStepTimer.
            if (globalNPC.QuickStepTimer > 0)
            {
                npc.velocity.X = globalNPC.QuickStepSpeed * globalNPC.QuickStepDir;
                globalNPC.QuickStepTimer--;
                if (globalNPC.QuickStepTimer == 0)
                {
                    npc.velocity.X = 0;
                    globalNPC.QuickStepRecoveryTimer = 30;
                }
            }
            else if (globalNPC.QuickStepRecoveryTimer > 0)
            {
                npc.velocity.X *= 0.4f;
                if (Math.Abs(npc.velocity.X) < 0.2f)
                {
                    npc.velocity.X = 0;
                }
                globalNPC.QuickStepRecoveryTimer--;
            }

            // Advance the multi-tick evasion windows (RunningDash telegraph→burst, RetreatAndShoot back-off).
            UpdateEvasion(npc, globalNPC);

            //Stop moving when teleporting, and handle the logic to execute it
            if (globalNPC.TeleportCountdown > 0)
            {
                npc.velocity *= 0.1f;
                if (npc.velocity.LengthSquared() < 0.01f)
                {
                    npc.velocity = Vector2.Zero;
                }

                // Non-Default styles hide the NPC inside its departure cloud for the full telegraph.
                if (globalNPC.TeleportVisualStyle != TeleportVisualStyle.Default)
                    npc.alpha = 255;

                globalNPC.TeleportCountdown--;
                if (globalNPC.TeleportCountdown == 0)
                {
                    ExecuteQueuedTeleport(npc);
                }
            }

            // Smoke/fire/plague: NPC is still hidden during snap delay; revealed at destination.
            if (globalNPC.TeleportAppearanceTimer > 0)
            {
                npc.velocity *= 0.1f;
                if (npc.velocity.LengthSquared() < 0.01f)
                {
                    npc.velocity = Vector2.Zero;
                }

                if (globalNPC.TeleportVisualStyle != TeleportVisualStyle.Default)
                    npc.alpha = 255;

                globalNPC.TeleportAppearanceTimer--;
                if (globalNPC.TeleportAppearanceTimer == 0)
                {
                    npc.Center = globalNPC.TeleportTelegraph;
                    globalNPC.TeleportTelegraph = Vector2.Zero;
                    npc.alpha = 0; // reveal at destination
                    npc.netUpdate = true;
                }
            }

            // Busy states can optionally reset attack timing after SimpleProjectile has run. Evasive moves choose
            // that policy in ShouldEvasionResetProjectileTimer, so individual moves can keep or reset shot progress.
            if (globalNPC.TeleportCountdown > 0 || globalNPC.TeleportAppearanceTimer > 0 || fleeing || globalNPC.DodgeTimer > 0 || globalNPC.PounceTimer > 0 || globalNPC.DirectPounceAfterimageTimer > 0 || globalNPC.DirectPounceRecoveryTimer > 0 || globalNPC.QuickStepTimer > 0 || globalNPC.QuickStepRecoveryTimer > 0 || globalNPC.InSustainedEvasion)
            {
                bool nonEvasiveBusy = globalNPC.TeleportCountdown > 0 || globalNPC.TeleportAppearanceTimer > 0 || fleeing || globalNPC.DodgeTimer > 0 || globalNPC.PounceTimer > 0 || globalNPC.DirectPounceAfterimageTimer > 0 || globalNPC.DirectPounceRecoveryTimer > 0;
                if (nonEvasiveBusy || ShouldEvasionResetProjectileTimer(globalNPC))
                {
                    globalNPC.ProjectileTimer = 0;
                }
                globalNPC.ArcherAimDirection = 0;
            }

            // Combat/teleport owns the body this frame (it set velocity above). Populated here so movement no-ops.
            bool teleportBusy = globalNPC.TeleportCountdown > 0 || globalNPC.TeleportAppearanceTimer > 0;
            if (teleportBusy)
            {
                npc.velocity *= 0.1f;
                if (npc.velocity.LengthSquared() < 0.01f)
                {
                    npc.velocity = Vector2.Zero;
                }
            }

            // Quick step drives its own velocity; the running-dash TELEGRAPH holds the enemy still while it flashes
            // (the burst itself does NOT seize the body — it runs via the topSpeed multiplier so the mover pursues).
            bool dashTelegraphHold = globalNPC.InSustainedEvasion && globalNPC.CurrentEvasion == EvasiveBehavior.RunningDash && globalNPC.EvasiveTelegraphing;
            intent.SeizesBody = teleportBusy || globalNPC.PounceTimer > 0 || globalNPC.DodgeTimer > 0 || globalNPC.DirectPounceAfterimageTimer > 0 || globalNPC.DirectPounceRecoveryTimer > 0 || globalNPC.QuickStepTimer > 0 || globalNPC.QuickStepRecoveryTimer > 0 || dashTelegraphHold || globalNPC.EvasiveRetreating;
        }

        private static bool ShouldEvasionResetProjectileTimer(tsorcRevampGlobalNPC globalNPC)
        {
            if (globalNPC.QuickStepTimer > 0 || globalNPC.QuickStepRecoveryTimer > 0)
            {
                return false;
            }

            if (!globalNPC.InSustainedEvasion)
            {
                return false;
            }

            switch (globalNPC.CurrentEvasion)
            {
                case EvasiveBehavior.RetreatAndShoot:
                    return false;
                default:
                    return true;
            }
        }

        private static void SpawnHighArcPounceTelegraph(NPC npc, tsorcRevampGlobalNPC globalNPC)
        {
            if (globalNPC.PounceTimer % 5 != 0 || Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Vector2 spawnPosition = npc.position;
            spawnPosition.Y += npc.height;
            spawnPosition.X += Main.rand.NextFloat(npc.width);
            Projectile.NewProjectileDirect(npc.GetSource_FromThis(), spawnPosition, new Vector2(0, 2), ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer);
        }

        private static void LaunchHighArcPounce(NPC npc, float topSpeed)
        {
            float pounceSpeed = topSpeed * 5;
            bool hasTrajectory = false;
            while (!hasTrajectory)
            {
                Vector2 trajectory = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].Center + new Vector2(0, -100), pounceSpeed, npc.gravity, false, false);
                if (trajectory == Vector2.Zero)
                {
                    pounceSpeed += topSpeed * 2;

                    //If it requires more than 20 units of speed to make it to the player, give up and just launch normally instead of using a ballistic trajectory
                    if (pounceSpeed > 20)
                    {
                        npc.velocity = UsefulFunctions.Aim(npc.Center, Main.player[npc.target].Center + new Vector2(0, -100), 20);
                        npc.netUpdate = true;
                        break;
                    }
                }
                else
                {
                    hasTrajectory = true;
                    npc.velocity = trajectory;
                    npc.netUpdate = true;
                }
            }
        }

        private static void RunDirectPounce(NPC npc, tsorcRevampGlobalNPC globalNPC, float topSpeed)
        {
            if (globalNPC.PounceTimer > 0)
            {
                int direction = Math.Sign(globalNPC.PounceTarget.X - npc.Center.X);
                if (direction == 0)
                {
                    direction = npc.direction == 0 ? 1 : npc.direction;
                }

                npc.direction = direction;
                npc.spriteDirection = direction;

                if (npc.velocity.Y == 0f)
                {
                    float runSpeed = MathHelper.Clamp(topSpeed * 1.35f, 1.5f, 4.5f);
                    npc.velocity.X = MathHelper.Lerp(npc.velocity.X, direction * runSpeed, 0.35f);
                }

                if (globalNPC.PounceTimer % 8 == 0)
                {
                    SoundEngine.PlaySound(SoundID.Run, npc.Center);
                }

                return;
            }

            LaunchDirectPounce(npc, globalNPC);
        }

        private static void LaunchDirectPounce(NPC npc, tsorcRevampGlobalNPC globalNPC)
        {
            if (globalNPC.PounceTarget == Vector2.Zero)
            {
                globalNPC.PounceTarget = Main.player[npc.target].Center;
            }

            float aggressionCurve = GetPounceAggressionCurve(globalNPC);
            float dashSpeed = MathHelper.Lerp(10f, 12.5f, aggressionCurve);
            int direction = Math.Sign(globalNPC.PounceTarget.X - npc.Center.X);
            if (direction == 0)
            {
                direction = npc.direction == 0 ? 1 : npc.direction;
            }

            Vector2 velocity = UsefulFunctions.Aim(npc.Center, globalNPC.PounceTarget, dashSpeed);
            if (velocity == Vector2.Zero)
            {
                velocity = new Vector2(direction * dashSpeed, -2.5f);
            }
            else if (velocity.Y > -2f)
            {
                velocity.Y = -2f;
            }

            npc.velocity = velocity;
            globalNPC.DirectPounceAfterimageTimer = globalNPC.DirectPounceAfterimages ? 8 : 0;
            globalNPC.DirectPounceRecoveryTimer = 18;
            globalNPC.PounceTarget = Vector2.Zero;
            npc.netUpdate = true;
        }

        private static void SpawnDirectPounceAfterimage(NPC npc)
        {
            float encodedFrame = npc.spriteDirection < 0 ? -(npc.frame.Y + 1) : npc.frame.Y + 1;
            Projectile afterimage = Projectile.NewProjectileDirect(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.NPCAfterimage>(), 0, 0, Main.myPlayer, npc.type, encodedFrame);
            afterimage.Center = npc.Center;
            afterimage.netUpdate = true;
        }

        // Stop-to-fire DECISION (Step 3): should the mover pin position so the combat layer can fire? This is the
        // standoff condition that used to live mid-BasicAI as `inStandoff`, lifted into the combat layer so BOTH
        // movers (LocalMover now, SmartFighter4AI in Step 4) can honor it via intent.HoldForAttack without knowing
        // anything about post-attack pauses / standing-fire bursts. Also ticks the post-attack pause timer (its
        // single owner now). Computed in BasicAI right after retarget, where LOS + grounded state match the old
        // in-mover decision point exactly — so behavior is unchanged.
        private static bool SenseHoldForAttack(NPC npc, tsorcRevampGlobalNPC globalNPC, bool lineOfSight)
        {
            if (globalNPC.FighterPostAttackPauseTimer > 0) globalNPC.FighterPostAttackPauseTimer--;
            return globalNPC.CanStopToFire && !globalNPC.CanPassThroughWalls
                && (globalNPC.FighterPostAttackPauseTimer > 0 || globalNPC.FighterRangedStandShotsRemaining > 0)
                && lineOfSight && npc.velocity.Y == 0f && !globalNPC.Fleeing;
        }

        // End-of-frame combat triggers, extracted verbatim from BasicAI's tail (pure refactor): decide whether
        // to START a dodge (vs an incoming projectile) or a pounce. Reads the post-movement `lineOfSight`; the
        // timers it sets here are executed next frame by RunFighterCombatExec.
        private static void RunFighterCombatTriggers(NPC npc, tsorcRevampGlobalNPC globalNPC, bool fleeing, bool lineOfSight, bool canDodgeroll, bool canPounce)
        {
            //Dodging — only while actively pursuing (not searching/patrolling/fleeing/teleporting)
            if (globalNPC.PursuitState == PursuitState.Pursue && !fleeing && globalNPC.TeleportCountdown == 0 && globalNPC.TeleportAppearanceTimer == 0 && globalNPC.DodgeCooldown == 0)
            {
                // Roll OR jump out of the way of an incoming aimed projectile. canDodgeroll grants both options (the
                // original roll-or-jump 50/50); CanJumpToEvade is an additive opt-in so a non-rolling enemy can still pre-jump.
                if ((canDodgeroll || globalNPC.CanJumpToEvade) && npc.Distance(Main.player[npc.target].Center) > 160)
                {
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        //If a projectile is within 100 units of the NPC and is within 0.3 radian angle of being aimed at them, then try to dodge
                        if (Main.projectile[i].active && Main.projectile[i].friendly && Main.projectile[i].damage > 0 && Main.projectile[i].DistanceSQ(npc.Center) < 40000 && UsefulFunctions.CompareAngles(Main.projectile[i].velocity, UsefulFunctions.Aim(Main.projectile[i].Center, npc.Center, 1)) < 0.3f)
                        {
                            if (Main.rand.NextFloat() < globalNPC.Agility)
                            {
                                bool heightToJump = true;
                                for (int j = 0; j < 8; j++)
                                {
                                    if (UsefulFunctions.IsTileReallySolid(npc.Center + new Vector2(0, -j)))
                                    {
                                        heightToJump = false;
                                        break;
                                    }
                                }
                                if (canDodgeroll)
                                {
                                    //Randomly choose whether to roll or jump (jump needs headroom, else roll) — unchanged
                                    if (Main.rand.NextBool() && heightToJump)
                                    {
                                        npc.velocity.Y -= 8;
                                    }
                                    else
                                    {
                                        globalNPC.DodgeTimer = 30;
                                    }
                                }
                                else if (heightToJump) //CanJumpToEvade-only: jump when there's room, otherwise hold
                                {
                                    npc.velocity.Y -= 8;
                                }

                                globalNPC.DodgeCooldown = (int)(300 * (1 - globalNPC.Agility));
                            }

                            npc.netUpdate = true;
                            break;
                        }
                    }
                }


                //Pouncing
                if (canPounce && globalNPC.PounceCooldown == 0 && lineOfSight)
                {
                    switch (globalNPC.PounceStyle)
                    {
                        case PounceStyle.DirectPounce:
                            TryStartDirectPounce(npc, globalNPC);
                            break;
                        case PounceStyle.HighArcPounce:
                            TryStartHighArcPounce(npc, globalNPC);
                            break;
                    }
                }
            }
        }

        private static void TryStartHighArcPounce(NPC npc, tsorcRevampGlobalNPC globalNPC)
        {
            if (npc.DistanceSQ(Main.player[npc.target].Center) > 40000 / globalNPC.Aggression)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextFloat() * 180 < globalNPC.Aggression)
                {
                    globalNPC.PounceTimer = 30;
                    globalNPC.PounceCooldown = 300;
                    npc.netUpdate = true;
                }
            }
        }

        private static void TryStartDirectPounce(NPC npc, tsorcRevampGlobalNPC globalNPC)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || npc.velocity.Y != 0f || SmartFighter4AI.HasActiveMovementPlan(npc))
            {
                return;
            }

            Player player = Main.player[npc.target];
            float distance = npc.Distance(player.Center);
            float aggressionCurve = GetPounceAggressionCurve(globalNPC);
            float minDistance = MathHelper.Lerp(340f, 260f, aggressionCurve);
            float maxDistance = MathHelper.Lerp(760f, 1100f, aggressionCurve);

            if (distance < minDistance || distance > maxDistance)
            {
                return;
            }

            int direction = Math.Sign(player.Center.X - npc.Center.X);
            if (direction == 0)
            {
                direction = npc.direction == 0 ? 1 : npc.direction;
            }

            float overshoot = MathHelper.Lerp(48f, 96f, aggressionCurve);
            overshoot = Math.Max(overshoot, (npc.width + player.width) * 0.35f);

            globalNPC.PounceTarget = player.Center + new Vector2(direction * overshoot, -8f);
            globalNPC.PounceTimer = (int)MathHelper.Lerp(36f, 24f, aggressionCurve);
            globalNPC.PounceCooldown = (int)MathHelper.Lerp(420f, 180f, aggressionCurve);
            npc.netUpdate = true;
        }

        private static float GetPounceAggressionCurve(tsorcRevampGlobalNPC globalNPC)
        {
            float aggression01 = MathHelper.Clamp(globalNPC.Aggression / 2.5f, 0f, 1f);
            return (float)Math.Sqrt(aggression01);
        }

        // Strict geometric line of sight (solids block, platforms don't). Pairs the permissive Collision.CanHit
        // (trajectory/step check) with the strict Collision.CanHitLine — the same combo the archer uses to decide
        // it can shoot. Plain Player.CanHit / Collision.CanHit alone reports LOS through complex terrain.
        private static bool FighterHasLineOfSight(NPC npc, Player player)
            => Collision.CanHit(npc.Center, 1, 1, player.Center, 1, 1)
            && Collision.CanHitLine(npc.Center, 1, 1, player.Center, 1, 1);

        // Smooth vanilla-style 1-tile step-up (ported from SmartFighter4AI / MNPC.cs). When the NPC walks
        // into a <=1-tile step or half-block, lift its position onto the step and add to gfxOffY so the
        // SPRITE glides up instead of snapping/jumping. Call each grounded, non-rope/non-platform-drop frame.
        // This replaces the old desperation 1-tile wall-jump for tier-0 walkers.
        // internal (not private) so the extracted LocalMover class can share this single copy.
        internal static void AutoStepUp(NPC npc)
        {
            if (npc.velocity.Y < 0f) return; // only while grounded / descending, like vanilla
            int offset = 0;
            if (npc.velocity.X < 0f) offset = -1;
            else if (npc.velocity.X > 0f) offset = 1;
            if (offset == 0) return;

            Vector2 pos = npc.position;
            pos.X += npc.velocity.X;
            int tileX = (int)((pos.X + (npc.width / 2) + ((npc.width / 2 + 1) * offset)) / 16f);
            int tileY = (int)((pos.Y + npc.height - 1f) / 16f);
            if (!WorldGen.InWorld(tileX, tileY, 5)) return;

            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (globalNPC.RequiresFlatGround)
            {
                if (!UsefulFunctions.IsPartOfValidSurface(tileX, tileY, globalNPC.MinSurfaceWidth) &&
                    !UsefulFunctions.IsPartOfValidSurface(tileX, tileY - 1, globalNPC.MinSurfaceWidth))
                {
                    return; // abort step-up!
                }
            }

            Tile t = Main.tile[tileX, tileY];
            Tile tU1 = Main.tile[tileX, tileY - 1];
            Tile tU2 = Main.tile[tileX, tileY - 2];
            Tile tU3 = Main.tile[tileX, tileY - 3];
            Tile tU4 = Main.tile[tileX, tileY - 4];
            Tile tBackU3 = Main.tile[tileX - offset, tileY - 3];

            bool stepBlock = (t.HasUnactuatedTile && !t.TopSlope && !tU1.TopSlope && Main.tileSolid[t.TileType] && !Main.tileSolidTop[t.TileType])
                             || (tU1.IsHalfBlock && tU1.HasUnactuatedTile);
            bool clearU1 = !tU1.HasUnactuatedTile || !Main.tileSolid[tU1.TileType] || Main.tileSolidTop[tU1.TileType]
                           || (tU1.IsHalfBlock && (!tU4.HasUnactuatedTile || !Main.tileSolid[tU4.TileType] || Main.tileSolidTop[tU4.TileType]));
            bool clearU2 = !tU2.HasUnactuatedTile || !Main.tileSolid[tU2.TileType] || Main.tileSolidTop[tU2.TileType];
            bool clearU3 = !tU3.HasUnactuatedTile || !Main.tileSolid[tU3.TileType] || Main.tileSolidTop[tU3.TileType];
            bool clearBehind = !tBackU3.HasUnactuatedTile || !Main.tileSolid[tBackU3.TileType];

            if ((float)(tileX * 16) < pos.X + npc.width && (float)(tileX * 16 + 16) > pos.X
                && stepBlock && clearU1 && clearU2 && clearU3 && clearBehind)
            {
                float tileWorldY = tileY * 16f;
                if (t.IsHalfBlock) tileWorldY += 8f;
                if (tU1.IsHalfBlock) tileWorldY -= 8f;
                if (tileWorldY < pos.Y + npc.height)
                {
                    float tileWorldYHeight = pos.Y + npc.height - tileWorldY;
                    if (tileWorldYHeight <= 16.1f)
                    {
                        npc.gfxOffY += npc.position.Y + npc.height - tileWorldY;
                        npc.position.Y = tileWorldY - npc.height;
                        npc.stepSpeed = tileWorldYHeight >= 9.0f ? 2f : 1f;
                    }
                }
            }
        }

        //AI snippits go here! Simply call these in the npc's main AI function to add them
        #region AI Snippets

        public static int ProjectileTelegraphTime = 25;

        private static float GetStandingFireChance(tsorcRevampGlobalNPC globalNPC, float baseChance)
        {
            float aggressionMultiplier = 1f;
            if (globalNPC.Aggression >= 0f)
            {
                aggressionMultiplier = MathHelper.Clamp(1f - globalNPC.Aggression / 2.5f, 0f, 1f);
            }

            return MathHelper.Clamp(baseChance * aggressionMultiplier, 0f, 1f);
        }

        private static void LogFighterNavDebug(NPC npc, tsorcRevampGlobalNPC globalNPC, bool lineOfSight)
        {
            // Now covers ALL tiers (was tier >= 1 only) so the tier-0 baseline AI — the one with the
            // stuck-jumping / falling-into-pits problems we want to fix — is diagnosable too.
            if (!ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
            {
                return;
            }
            if (npc.target < 0 || npc.target >= Main.maxPlayers || !Main.player[npc.target].active)
            {
                return;
            }
            // Keep the log focused on active engagements near the player.
            if (npc.Distance(Main.player[npc.target].Center) > 1600f)
            {
                return;
            }

            // Log when something behaviorally relevant is happening — crucially, for the baseline AI, ANY time
            // it's airborne (jumping/falling) or shoved against a wall: that's exactly when the stuck-jumping and
            // pit-falling show up.
            bool airborne = npc.velocity.Y != 0f;
            bool interesting = airborne
                || npc.collideX
                || globalNPC.StuckTimer > 0
                || globalNPC.PursuitState != PursuitState.Pursue
                || !lineOfSight
                || Math.Abs(Main.player[npc.target].Center.Y - npc.Center.Y) > 48f;
            if (!interesting)
            {
                return;
            }

            int now = (int)Main.GameUpdateCount;
            // Sample finer while actively maneuvering (airborne / against a wall / stuck) so fast stuck-jump
            // oscillation is actually captured between lines; coarser when idle/walking to keep it readable.
            int interval = (airborne || npc.collideX || globalNPC.StuckTimer > 0) ? 6 : 30;
            if (now - globalNPC.LastNavDebugLogTick < interval)
            {
                return;
            }
            globalNPC.LastNavDebugLogTick = now;

            try
            {
                string separator = Path.DirectorySeparatorChar.ToString();
                string logDir = Main.SavePath + separator + "Logs";
                Directory.CreateDirectory(logDir);
                // SF4-routed enemies write into the SAME file as the smart log so one shared log holds the full
                // timeline (Pursue/Search nav lines from LogFrame + these FSM-layer lines for Patrol / combat-seize
                // frames where the SF4 mover is skipped). The `[fsm]` tag distinguishes the two line formats.
                string logPath = logDir + separator + (globalNPC.NavSearchRadius > 0 ? "tsorcRevamp-smartfighter4.log" : "tsorcRevamp-nav.log");
                Player player = Main.player[npc.target];
                string line = $"[{DateTime.Now:HH:mm:ss}] {npc.TypeName}#{npc.whoAmI} [fsm] pos=({npc.Center.X / 16f:F1},{npc.Center.Y / 16f:F1}) player=({player.Center.X / 16f:F1},{player.Center.Y / 16f:F1}) vel=({npc.velocity.X:F2},{npc.velocity.Y:F2}) g={!airborne} cx={npc.collideX} cy={npc.collideY} dist={npc.Distance(player.Center):F0} los={lineOfSight} yDiff={player.Center.Y - npc.Center.Y:F0} pursuit={globalNPC.PursuitState} disengage={globalNPC.DisengageTimer}/{globalNPC.NavGiveUpTicks} immobile={globalNPC.StuckTimer} tp={(globalNPC.CanTeleport ? $"{globalNPC.TeleportStyle}/ch{(globalNPC.TeleportChargesRemaining == int.MaxValue ? "inf" : globalNPC.TeleportChargesRemaining.ToString())}/cd{globalNPC.TeleportCooldownTimer}/cnt{globalNPC.TeleportCountdown}" : "off")} stopFire={globalNPC.CanStopToFire}";
                File.AppendAllText(logPath, line + Environment.NewLine);
            }
            catch
            {
                // Debug logging should never affect NPC AI.
            }
        }


        public static bool SimpleProjectile(NPC npc)
        {
            return SimpleProjectile(npc, true);
        }

        ///<summary> 
        ///Fires a projectile with various parameters. Uses any timer variable you give it, and goes in the npc's AI() function
        ///</summary>
        ///<param name="npc">The npc itself this function will run on</param>
        ///<param name="actuallyFire">This lets you use a condition to block the projectile from firing unless it is true (such as having line of sight to the player)</param>
        public static bool SimpleProjectile(NPC npc, bool actuallyFire = true)
        {
            //Get the globalnpc for this NPC, which holds important data
            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();

            //This should only not equal -1 on the frame an attack successfully fires. This resets it afterward.
            globalNPC.AttackSucceeded = -1;

            //Do not fire if it needs line of sight and does not have it
            // True (hitscan) LOS: CanHitLine is the strict straight-line check (blocked by solids, ignores platforms),
            // i.e. "is there a clear shot?" — NOT the permissive CanHit that tolerates stepping over terrain. So a
            // needsLineOfSight attack (a terrain-killed projectile: spear, bomb, etc.) only fires with a real clear path.
            if (globalNPC.CurrentAttack.needsLineOfSight && !Collision.CanHitLine(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1))
            {
                actuallyFire = false;
            }

            //If the color was not set, use white
            if (globalNPC.CurrentAttack.color == null)
            {
                globalNPC.CurrentAttack.color = Color.White;
            }

            //Increment the timer. Stop increasing it once we reach the telegraph time. Only continue once it is actually firing. Once it is actually firing do not stop incrementing the timer, so that it can not stop firing after telegraphing a shot.
            if (globalNPC.ProjectileTimer < globalNPC.CurrentAttack.timerCap - globalNPC.CurrentAttack.telegraphTime || actuallyFire || globalNPC.ProjectileTimer > globalNPC.CurrentAttack.timerCap - globalNPC.CurrentAttack.telegraphTime)
            {
                globalNPC.ProjectileTimer++;

                //Spawn a telegraph flash once the telegraph time is reached
                if (globalNPC.ProjectileTimer == 1 + globalNPC.CurrentAttack.timerCap - globalNPC.CurrentAttack.telegraphTime)
                {
                    if (globalNPC.CurrentAttack.overshoot == null)
                    {
                        globalNPC.CurrentAttack.overshoot = Vector2.Zero;
                    }
                    if (globalNPC.CurrentAttack.lockAimAtTelegraph)
                    {
                        globalNPC.LockedShotTargetPosition = Main.player[npc.target].Center + globalNPC.CurrentAttack.overshoot.Value;
                        globalNPC.LockedShotFacingDirection = globalNPC.LockedShotTargetPosition.X >= npc.Center.X ? 1 : -1;
                        npc.direction = globalNPC.LockedShotFacingDirection;
                        npc.spriteDirection = globalNPC.LockedShotFacingDirection;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            npc.netUpdate = true;
                        }
                    }
                    Vector2 spawnPosition = npc.position;
                    if (npc.direction == 1)
                    {
                        spawnPosition.X += npc.width;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectileDirect(npc.GetSource_FromThis(), spawnPosition, npc.velocity, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(globalNPC.CurrentAttack.color.Value));
                    }
                }
            }

            // Poise flags (Level 2): the telegraph window is the last `telegraphTime` ticks before the shot. Split it at
            // commitFraction — TELEGRAPHING (poise builds, a stagger cancels it) until the commit point, then COMMITTED
            // (hyper-armor, fires through). Before the flash the enemy is neutral (free to evade); a stagger there still
            // cancels the pending shot via the ProjectileTimer reset in TriggerStagger.
            int flashTick = globalNPC.CurrentAttack.timerCap - globalNPC.CurrentAttack.telegraphTime;
            int commitTick = flashTick + (int)Math.Round(globalNPC.CurrentAttack.telegraphTime * globalNPC.CurrentAttack.commitFraction);
            bool inTell = globalNPC.ProjectileTimer > flashTick;
            globalNPC.AttackTelegraphing = inTell && globalNPC.ProjectileTimer <= commitTick;
            globalNPC.AttackCommitted = inTell && globalNPC.ProjectileTimer > commitTick;
            if (globalNPC.CurrentAttack.lockAimAtTelegraph && globalNPC.AttackCommitted && globalNPC.LockedShotTargetPosition != Vector2.Zero)
            {
                npc.direction = globalNPC.LockedShotFacingDirection;
                npc.spriteDirection = globalNPC.LockedShotFacingDirection;
            }

            //If it's supposed to stop moving when firing, then do so
            if (globalNPC.CanStopToFire && globalNPC.CurrentAttack.stopBefore && !globalNPC.CanPassThroughWalls)
            {
                bool inTelegraphWindow = globalNPC.ProjectileTimer > globalNPC.CurrentAttack.timerCap - globalNPC.CurrentAttack.telegraphTime;
                float stopBeforeChance = GetStandingFireChance(globalNPC, globalNPC.CurrentAttack.stopBeforeChance);

                if (inTelegraphWindow && Main.rand.NextFloat() < stopBeforeChance)
                {
                    npc.velocity.X = 0;
                    npc.velocity.Y = 0f; // suppress jump-frame animation while aiming

                    // Standing-fire roll: on the first frame of the telegraph window, tier-2 NPCs
                    // may commit to firing N shots in a row without resuming movement.
                    // Aggression lowers the chance to stand; Patience raises the burst count.
                    if (globalNPC.CanStopToFire && globalNPC.FighterRangedStandShotsRemaining == 0
                        && globalNPC.ProjectileTimer == globalNPC.CurrentAttack.timerCap - globalNPC.CurrentAttack.telegraphTime + 1
                        && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float aggressionFraction = Math.Clamp(globalNPC.Aggression / 2.5f, 0f, 1f);
                        if (Main.rand.NextFloat() > aggressionFraction)
                        {
                            globalNPC.FighterRangedStandShotsRemaining = 1 + Main.rand.Next(0, 1 + (int)globalNPC.Patience);
                        }
                    }
                }
            }

            if (globalNPC.ProjectileTimer >= globalNPC.CurrentAttack.timerCap)
            {
                globalNPC.ProjectileTimer = 0;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (globalNPC.CurrentAttack.overshoot == null)
                    {
                        globalNPC.CurrentAttack.overshoot = Vector2.Zero;
                    }
                    Vector2 targetPosition = globalNPC.CurrentAttack.lockAimAtTelegraph && globalNPC.LockedShotTargetPosition != Vector2.Zero
                        ? globalNPC.LockedShotTargetPosition
                        : Main.player[npc.target].Center + globalNPC.CurrentAttack.overshoot.Value;
                    Vector2 projectileVector = UsefulFunctions.BallisticTrajectory(npc.Center, targetPosition, globalNPC.CurrentAttack.velocity, globalNPC.CurrentAttack.gravity);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center.X, npc.Center.Y, projectileVector.X, projectileVector.Y, globalNPC.CurrentAttack.type, globalNPC.CurrentAttack.damage, 0f, Main.myPlayer, globalNPC.CurrentAttack.ai0, globalNPC.CurrentAttack.ai1);
                    globalNPC.LockedShotTargetPosition = Vector2.Zero;
                }
                if (globalNPC.CurrentAttack.sound != null)
                {
                    SoundEngine.PlaySound(globalNPC.CurrentAttack.sound.Value, npc.Center);
                }

                globalNPC.AttackSucceeded = globalNPC.AttackIndex;
                RegisterFighterAttack(npc);
                globalNPC.AttackIndex = globalNPC.NextAttackIndex;
                globalNPC.NextAttackIndex = WeightedRandomAttackSelection(globalNPC);

                // Consume one standing-fire charge. When exhausted, exit standing mode.
                if (globalNPC.FighterRangedStandShotsRemaining > 0)
                {
                    if (--globalNPC.FighterRangedStandShotsRemaining == 0)
                    {
                        npc.TargetClosest(true); // resume pursuit
                    }
                }
            }

            return false;
        }

        public static void RegisterFighterAttack(NPC npc, int attacksBeforePause = 4, int pauseTicks = 60)
        {
            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
            // Post-attack pauses only matter for stop-to-fire enemies (gated by the CanStopToFire bool).
            if (!globalNPC.CanStopToFire)
            {
                return;
            }

            globalNPC.FighterAttacksSincePause++;
            if (globalNPC.FighterAttacksSincePause >= attacksBeforePause)
            {
                globalNPC.FighterAttacksSincePause = 0;
                globalNPC.FighterPostAttackPauseTimer = pauseTicks;
            }
        }

        /// <summary>
        /// Picks a random attack from AttackList based on the weight of each entry
        /// </summary>
        /// <param name="globalNPC">The NPC being operated on</param>
        /// <returns></returns>
        public static int WeightedRandomAttackSelection(tsorcRevampGlobalNPC globalNPC)
        {
            if (globalNPC.AttackList.Count == 0 || globalNPC.AttackList.Count == 1)
            {
                return 0;
            }
            float weightMax = 0;
            foreach (ProjectileData data in globalNPC.AttackList)
            {
                weightMax += data.weight;
            }

            float randomVal = Main.rand.NextFloat(weightMax);

            float runningTotal = 0;
            for (int i = 0; i < globalNPC.AttackList.Count; i++)
            {
                runningTotal += globalNPC.AttackList[i].weight;
                if (randomVal < runningTotal)
                {
                    return i;
                }
            }

            return 0;
        }

        /// <summary>
        /// Simple class which holds all the data relevant to firing a projectile
        /// </summary>
        public class ProjectileData
        {
            public int timerCap;
            public int type;
            public int damage;
            public float velocity;
            public SoundStyle? sound;
            public float gravity;
            public float ai0;
            public float ai1;
            public Vector2? overshoot;
            public Color? color;
            public bool stopBefore;
            public bool needsLineOfSight;
            public float weight;
            public Func<NPC, bool> condition;
            public float stopBeforeChance;
            public int telegraphTime;
            public bool lockAimAtTelegraph;
            // Fraction of the telegraph window (flash→fire) that is still TELEGRAPHING (cancellable) before the attack
            // COMMITS to hyper-armor. 0 = committed the instant it flashes ("after the flash it's committed"); 0.5 =
            // first half of the tell is cancellable, second half committed (e.g. a shrinking magic ring); 1 = cancellable
            // right up to the shot. Drives AttackTelegraphing/AttackCommitted in SimpleProjectile → the poise system.
            public float commitFraction;

            public ProjectileData(int projectileType, int timerCap, int projectileDamage, float projectileVelocity, SoundStyle? shootSound = null, float projectileGravity = 0.035f, float ai0 = 0, float ai1 = 0, Vector2? overshoot = null, Color? telegraphColor = null, bool stopBeforeFiring = true, bool needsLineOfSight = false, float weight = 1, Func<NPC, bool> condition = null, float stopBeforeChance = 0.1f, int? telegraphTime = null, float commitFraction = 0f, bool lockAimAtTelegraph = false)
            {
                type = projectileType;
                this.timerCap = timerCap;
                damage = projectileDamage;
                velocity = projectileVelocity;
                sound = shootSound;
                gravity = projectileGravity;
                this.ai0 = ai0;
                this.ai1 = ai1;
                this.overshoot = overshoot;
                color = telegraphColor;
                stopBefore = stopBeforeFiring;
                this.needsLineOfSight = needsLineOfSight;
                this.weight = weight;
                this.condition = condition;
                this.stopBeforeChance = stopBeforeChance;
                this.telegraphTime = telegraphTime ?? ProjectileTelegraphTime;
                this.commitFraction = commitFraction;
                this.lockAimAtTelegraph = lockAimAtTelegraph;
            }
        }

        ///<summary> 
        ///Lets the npc leap at players who are close, does not use any ai slots, and goes in an npc's ai function
        ///</summary>
        ///<param name="npc">The npc itself this function will run on</param>
        ///<param name="hopSpeedX">How fast it leaps horizontally</param>
        ///<param name="hopSpeedY">How fast it leaps vertically</param>
        ///<param name="minimumSpeed">How fast it has to be running to be allowed to hop</param>
        ///<param name="hopRange">It leaps at the player when it is this close to them</param>
        public static void LeapAtPlayer(NPC npc, float hopSpeedX, float hopSpeedY, float minimumSpeed, float hopRange = 64)
        {
            //If the player is within range and if the npc is moving fast enough to be allowed to hop, then hop
            if (npc.velocity.Y == 0f && Math.Abs(npc.Center.X - Main.player[npc.target].Center.X) < hopRange && Math.Abs(npc.Center.Y - Main.player[npc.target].Center.Y) < hopRange && ((npc.direction > 0 && npc.velocity.X >= minimumSpeed) || (npc.direction < 0 && npc.velocity.X <= -minimumSpeed)))
            {
                npc.velocity.X = hopSpeedX * npc.direction;
                npc.velocity.Y = -hopSpeedY;
                npc.netUpdate = true;
            }
        }

        ///<summary> 
        ///Calculates a position to teleport the NPC to. Returns null if there is no valid position.
        ///</summary>
        ///<param name="npc">The npc itself this function will run on</param>
        ///<param name="range">The max range from the player it can teleport. Minimum is 12 blocks.</param>
        ///<param name="requireLineofSight">Try to teleport somewhere that has line of sight to the player</param>
        public static Vector2? GenerateTeleportPosition(NPC npc, int range, bool requireLineofSight = true, bool preferHighGround = false, int minRange = 11)
        {
            int playerTileY = (int)(Main.player[npc.target].Center.Y / 16f);
            //Do not teleport if the player is way way too far away (stops enemies following you home if you mirror away)
            if (Math.Abs(npc.position.X - Main.player[npc.target].position.X) + Math.Abs(npc.position.Y - Main.player[npc.target].position.Y) > 2000f)
            { // far away from target; 2000 pixels = 125 blocks
                return null;
            }

            // Clamp minRange so it's always strictly less than range.
            minRange = Math.Max(1, Math.Min(minRange, range - 1));

            //Try 100 times at most
            for (int i = 0; i < 100; i++)
            {
                //Pick a random point to target. Make sure it's at least minRange blocks away from the player to avoid cheap hits.
                Vector2 teleportTarget = Vector2.Zero;
                if (range < 13)
                {
                    range = 13;
                }
                teleportTarget.X = Main.rand.Next(minRange, range);
                if (Main.rand.NextBool())
                {
                    teleportTarget.X *= -1;
                }

                //Move teleportTarget up a few blocks, since in the next step the algorithm will search downward from this point to find a valid landing spot
                teleportTarget.Y -= 12;

                //Add the player's position to it to convert it to an actual tile coordinate
                teleportTarget += Main.player[npc.target].position / 16;

                //Starting from the point we picked, go down one block at a time until we find hit a solid block
                bool odd = false;
                for (int y = 0; Math.Abs(y) < range / 2;)
                {
                    if (odd)
                    {
                        y *= -1;
                        y++;
                        odd = !odd;
                    }
                    else
                    {
                        y *= -1;
                        odd = !odd;
                    }
                    if (UsefulFunctions.IsTileReallySolid((int)teleportTarget.X, (int)teleportTarget.Y + y))
                    {
                        //Skip to the next tile if any of the following is true:

                        // If there are solid blocks in the way, leaving no room to teleport to
                        if (Collision.SolidTiles((int)teleportTarget.X - 1, (int)teleportTarget.X + 1, (int)teleportTarget.Y + y - 4, (int)teleportTarget.Y + y - 1))
                        {
                            //Main.NewText("Fail 1");
                            continue;
                        }

                        //If it requires line of sight, and there is not a clear path, and it has not tried at least 50 times, then skip to the next try
                        else if (requireLineofSight && !(Collision.CanHit(new Vector2(teleportTarget.X, (int)teleportTarget.Y + y), 2, 2, Main.player[npc.target].Center / 16, 2, 2) && Collision.CanHitLine(new Vector2(teleportTarget.X, (int)teleportTarget.Y + y), 2, 2, Main.player[npc.target].Center / 16, 2, 2)))
                        {
                            //Main.NewText("Fail 3");
                            continue;
                        }

                        // High-ground preference (4b): for the first ~70 tries, reject spots more than 2 tiles
                        // BELOW the player so the search favours elevated vantage points (archers / hunters).
                        // After that, accept any valid spot so it never fails to teleport for lack of a perch.
                        else if (preferHighGround && i < 70 && ((int)teleportTarget.Y + y) > playerTileY + 2)
                        {
                            continue;
                        }

                        //If the selected tile has lava above it, and the npc isn't immune
                        else if (Main.tile[(int)teleportTarget.X, (int)teleportTarget.Y + y - 1].LiquidType == LiquidID.Lava && !npc.lavaImmune)
                        {
                            //Main.NewText("Fail 4");
                            continue;
                        }

                        //Then teleport and return
                        teleportTarget.X = ((int)teleportTarget.X * 16 - npc.width / 2); //Center npc at target
                        teleportTarget.Y = (((int)teleportTarget.Y + y) * 16 - npc.height); //Subtract npc.height from y so block is under feet
                        npc.TargetClosest(true);
                        npc.netUpdate = true;

                        if(teleportTarget.Length() < 400)
                        {
                            UsefulFunctions.BroadcastText("Teleport error!");
                            UsefulFunctions.BroadcastText("NPC Name: " + npc.GivenOrTypeName);
                            UsefulFunctions.BroadcastText("Target coordinates: " + teleportTarget);
                            UsefulFunctions.BroadcastText("Please report this to our discord!");
                        }
                        return teleportTarget;
                    }
                }
            }

            return null;
        }


        ///<summary> 
        ///Teleports the NPC to a random position within a specified range around the player, includes effects. Does not teleport the enemy if no safe location exists.
        ///Will not teleport enemies right next to the player. Teleports enemies somewhere with line of sight to the player by default.
        ///</summary>
        ///<param name="npc">The npc itself this function will run on</param>
        ///<param name="range">The max range from the player it can teleport. Minimum is 12 blocks.</param>
        ///<param name="requireLineofSight">Try to teleport somewhere that has line of sight to the player</param>
        public static void TeleportImmediately(NPC npc, int range, bool requireLineofSight = true)
        {
            QueueTeleport(npc, range, requireLineofSight, 60);
            ExecuteQueuedTeleport(npc);
        }

        /// <summary>
        /// Unified-teleport disengage resolver (4b): try to blink to a spot with LOS to the player to
        /// re-acquire the chase. Reuses <see cref="QueueTeleport"/> for the smoke telegraph + warp. On a
        /// successful queue, consumes a charge (unless unlimited) and starts the per-style cooldown. Returns
        /// true if a blink was queued.
        /// </summary>
        public static bool TryTeleportReacquire(NPC npc, tsorcRevampGlobalNPC globalNPC)
        {
            if (globalNPC.TeleportCountdown != 0)
            {
                return false; // already mid-blink
            }

            // minRange: 5 tiles so the blink can land inside small rooms (default 11 is too large for tight spaces).
            QueueTeleport(npc, 50, true, globalNPC.TeleportTelegraphTime, globalNPC.PrefersHighGround, minRange: 5);

            // QueueTeleport only sets TeleportCountdown when it actually found a valid destination.
            if (globalNPC.TeleportCountdown <= 0)
            {
                return false; // no valid spot this attempt — caller falls through to Patrol
            }

            if (globalNPC.TeleportChargesRemaining != int.MaxValue && globalNPC.TeleportChargesRemaining > 0)
            {
                globalNPC.TeleportChargesRemaining--; // limited charges do not recharge
            }

            // Per-style cooldown; Aggressive shortens when wounded.
            globalNPC.TeleportCooldownTimer = globalNPC.TeleportStyle switch
            {
                TeleportStyle.Relaxed => 3600,                                                 // 60s
                TeleportStyle.Aggressive => npc.life < npc.lifeMax / 2 ? 300 : 600,            // 10s, 5s when wounded
                _ => 0,                                                                        // Normal: none (preserves legacy canTeleport feel — blink whenever it gives up)
            };
            return true;
        }

        /// <summary>
        /// Lava-escape blink: a <see cref="tsorcRevampGlobalNPC.CanTeleport"/> enemy caught in lava warps to safe
        /// ground near the player. Line of sight is NOT required (the goal is getting OUT of the lava, not
        /// re-acquiring a clean line) — <see cref="GenerateTeleportPosition"/> still rejects lava-topped tiles for
        /// non-lava-immune enemies, so they land somewhere dry. Uses a short fixed cooldown so an enemy that keeps
        /// sliding back into lava isn't left to cook waiting on the (much longer) per-style cooldown. Returns true
        /// if a blink was queued.
        /// </summary>
        public static bool TryTeleportOutOfLava(NPC npc, tsorcRevampGlobalNPC globalNPC)
        {
            if (globalNPC.TeleportCountdown != 0)
            {
                return false; // already mid-blink
            }

            QueueTeleport(npc, 50, requireLineofSight: false, globalNPC.TeleportTelegraphTime, globalNPC.PrefersHighGround, minRange: 5);

            // QueueTeleport only sets TeleportCountdown when it actually found a valid destination.
            if (globalNPC.TeleportCountdown <= 0)
            {
                return false;
            }

            if (globalNPC.TeleportChargesRemaining != int.MaxValue && globalNPC.TeleportChargesRemaining > 0)
            {
                globalNPC.TeleportChargesRemaining--; // limited charges do not recharge
            }

            globalNPC.TeleportCooldownTimer = 90; // short (1.5s): responsive re-escape without spamming
            return true;
        }

        public static void QueueTeleport(NPC npc, int range, bool requireLineofSight = true, int TeleportTelegraphTime = 140, bool preferHighGround = false, int minRange = 11)
        {
            Vector2? potentialNewPos;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 100; i++)
                {
                    potentialNewPos = GenerateTeleportPosition(npc, range, requireLineofSight, preferHighGround, minRange);
                    if (potentialNewPos.HasValue && (!requireLineofSight || (Collision.CanHit(potentialNewPos.Value, 1, 1, Main.player[npc.target].Center, 1, 1) && Collision.CanHitLine(potentialNewPos.Value, 1, 1, Main.player[npc.target].Center, 1, 1))))
                    {
                        npc.GetGlobalNPC<tsorcRevampGlobalNPC>().TeleportCountdown = TeleportTelegraphTime;
                        npc.GetGlobalNPC<tsorcRevampGlobalNPC>().TeleportTelegraph = potentialNewPos.Value;
                        SoundEngine.PlaySound(SoundID.Item79 with { Volume = 0.6f, PitchVariance = 0.1f }, npc.Center); // exit/departure cue

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            var visualStyle = npc.GetGlobalNPC<tsorcRevampGlobalNPC>().TeleportVisualStyle;
                            if (visualStyle == TeleportVisualStyle.Default)
                            {
                                Projectile.NewProjectileDirect(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.TeleportTelegraph>(), 0, 0, Main.myPlayer, npc.whoAmI, TeleportTelegraphTime);
                                Projectile.NewProjectileDirect(npc.GetSource_FromThis(), potentialNewPos.Value, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.TeleportTelegraph>(), 0, 0, Main.myPlayer, ai1: TeleportTelegraphTime);
                            }
                            else
                            {
                                if (visualStyle == TeleportVisualStyle.Plague)
                                {
                                    // Plague telegraph cloud must outlast the full countdown so there's no gap
                                    // before ExecuteQueuedTeleport spawns the real clouds. Cap at LifetimeTicks
                                    // so we don't exceed the projectile's intended maximum duration.
                                    int plagueTelegraphLife = Math.Min(TeleportTelegraphTime, PlagueTeleportCloud.LifetimeTicks);
                                    var srcCloud = Projectile.NewProjectileDirect(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.PlagueTeleportCloud>(), 0, 0, Main.myPlayer, 0f, PlagueTeleportCloud.MaxCloudRadius);
                                    srcCloud.timeLeft = plagueTelegraphLife;
                                    var dstCloud = Projectile.NewProjectileDirect(npc.GetSource_FromThis(), potentialNewPos.Value, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.PlagueTeleportCloud>(), 0, 0, Main.myPlayer, 0f, PlagueTeleportCloud.MaxCloudRadius);
                                    dstCloud.timeLeft = plagueTelegraphLife;
                                }
                                else
                                {
                                    float mistStyle = visualStyle == TeleportVisualStyle.Fire ? 1f : 0f;
                                    float radius = Math.Max(npc.width, npc.height) * 0.5f * TeleportMistVisualScale;
                                    var srcMist = Projectile.NewProjectileDirect(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.TeleportMistLinger>(), 0, 0, Main.myPlayer, mistStyle, radius);
                                    srcMist.timeLeft = TeleportTelegraphTime;
                                    var dstMist = Projectile.NewProjectileDirect(npc.GetSource_FromThis(), potentialNewPos.Value, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.TeleportMistLinger>(), 0, 0, Main.myPlayer, mistStyle, radius);
                                    dstMist.timeLeft = TeleportTelegraphTime;

                                    if (visualStyle == TeleportVisualStyle.Fire)
                                    {
                                        Projectile.NewProjectileDirect(npc.GetSource_FromThis(), npc.Center, Vector2.Zero,
                                            ModContent.ProjectileType<Projectiles.VFX.FireTeleportBlast>(), 0, 0, Main.myPlayer);
                                        Projectile.NewProjectileDirect(npc.GetSource_FromThis(), potentialNewPos.Value, Vector2.Zero,
                                            ModContent.ProjectileType<Projectiles.VFX.FireTeleportBlast>(), 0, 0, Main.myPlayer);
                                    }
                                }
                            }
                        }

                        break;
                    }
                }
            }
        }

        private static void SpawnTeleportMist(Vector2 position, Vector2 direction, int width, int height, tsorcRevampGlobalNPC globalNPC)
        {
            int dustCount = (int)Math.Ceiling(globalNPC.TeleportDustCount * TeleportMistVisualScale);
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 randomVelocity = direction * Main.rand.NextFloat(2.5f, 5.5f)
                    + Main.rand.NextVector2Circular(1.6f, 1.6f);
                Dust dust = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(width * 0.4f * TeleportMistVisualScale, height * 0.4f * TeleportMistVisualScale),
                    globalNPC.TeleportDustType, randomVelocity, 150, globalNPC.TeleportDustColor, globalNPC.TeleportDustScale * TeleportMistVisualScale);
                dust.noGravity = true;
                dust.fadeIn = 0.45f;
            }
        }

        private static void SpawnFireTeleportBurst(NPC npc, Vector2 position)
        {
            int damage = Math.Max(1, (int)(npc.damage * FireTeleportFlameDamageMultiplier));
            float rotationOffset = Main.rand.NextFloat(MathHelper.TwoPi);
            int flameLifetime = npc.type == ModContent.NPCType<Enemies.Basilisk.BasiliskHunter>() && npc.life <= npc.lifeMax / 2 ? 45 : 30;

            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.4f }, position);

            for (int i = 0; i < FireTeleportFlameCount; i++)
            {
                Vector2 velocity = (rotationOffset + MathHelper.TwoPi * i / FireTeleportFlameCount).ToRotationVector2() * FireTeleportFlameSpeed;
                Projectile flame = Projectile.NewProjectileDirect(npc.GetSource_FromThis(), position, velocity,
                    ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), damage, 5f, Main.myPlayer);
                flame.timeLeft = flameLifetime;
            }
        }

        public static void ExecuteQueuedTeleport(NPC npc)
        {
            if (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().TeleportTelegraph == Vector2.Zero)
            {
                return;
            }
            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();

            SoundEngine.PlaySound(SoundID.Item8, npc.Center);

            Vector2 diff = globalNPC.TeleportTelegraph - npc.Center;
            float length = diff.Length();
            if (length > 0f)
                diff /= length;

            if (globalNPC.TeleportVisualStyle == TeleportVisualStyle.Default)
            {
                SpawnTeleportMist(npc.Center, diff, npc.width, npc.height, globalNPC);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectileDirect(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 350, 20);
                    Projectile.NewProjectileDirect(npc.GetSource_FromThis(), globalNPC.TeleportTelegraph, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 350, 20);
                }

                npc.Center = globalNPC.TeleportTelegraph;
                SpawnTeleportMist(npc.Center, -diff, npc.width, npc.height, globalNPC);
            }
            else
            {
                if (globalNPC.TeleportVisualStyle == TeleportVisualStyle.Plague)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        var exitCloud = Projectile.NewProjectileDirect(npc.GetSource_FromThis(), npc.Center, Vector2.Zero,
                            ModContent.ProjectileType<Projectiles.VFX.PlagueTeleportCloud>(), 0, 0, Main.myPlayer, 1f, PlagueTeleportCloud.MaxCloudRadius);
                        exitCloud.timeLeft = PlagueTeleportCloud.LifetimeTicks;

                        var entryCloud = Projectile.NewProjectileDirect(npc.GetSource_FromThis(), globalNPC.TeleportTelegraph, Vector2.Zero,
                            ModContent.ProjectileType<Projectiles.VFX.PlagueTeleportCloud>(), 0, 0, Main.myPlayer, 0f, PlagueTeleportCloud.MaxCloudRadius);
                        entryCloud.timeLeft = PlagueTeleportCloud.LifetimeTicks;
                    }
                }
                else
                {
                    bool isFireTeleport = globalNPC.TeleportVisualStyle == TeleportVisualStyle.Fire;
                    float style = isFireTeleport ? 1f : 0f;
                    float radius = Math.Max(npc.width, npc.height) * 0.5f * TeleportMistVisualScale;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        // Both clouds start simultaneously. NPC moves halfway through the 1s cloud.
                        var exitMist = Projectile.NewProjectileDirect(npc.GetSource_FromThis(), npc.Center, Vector2.Zero,
                            ModContent.ProjectileType<Projectiles.VFX.TeleportMistLinger>(), 0, 0, Main.myPlayer, style, radius);
                        exitMist.timeLeft = SmokeFireTeleportCloudTicks;

                        var entryMist = Projectile.NewProjectileDirect(npc.GetSource_FromThis(), globalNPC.TeleportTelegraph, Vector2.Zero,
                            ModContent.ProjectileType<Projectiles.VFX.TeleportMistLinger>(), 0, 0, Main.myPlayer, style, radius);
                        entryMist.timeLeft = SmokeFireTeleportCloudTicks;

                        if (isFireTeleport)
                        {
                            SpawnFireTeleportBurst(npc, npc.Center);
                            SpawnFireTeleportBurst(npc, globalNPC.TeleportTelegraph);
                        }
                    }
                }

                // Position snaps to destination after 0.5s (30 frames), handled by FighterAI.
                globalNPC.TeleportAppearanceTimer = SmokeFireTeleportSnapTicks;
            }
        }

        public static void FighterOnHit(NPC npc, bool melee)
        {
            if (melee)
            {
                npc.localAI[1] = 80f; // was 100
                npc.knockBackResist = 0.09f;
                // Abort any standing-fire burst — the NPC will be knocked airborne anyway
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().FighterRangedStandShotsRemaining = 0;

                if (!Main.rand.NextBool(2))
                {
                    return;
                }

                //TELEPORT MELEE
                if (Main.rand.NextBool(18))
                {
                    TeleportImmediately(npc, 25, true);
                }
                //WHEN HIT, CHANCE TO JUMP BACKWARDS 
                else if (Main.rand.NextBool(8))
                {
                    //npc.TargetClosest(false);
                    npc.velocity.Y = -8f;
                    npc.velocity.X = -4f * npc.direction;
                    npc.localAI[1] = 150f;
                    npc.netUpdate = true;
                }
                //WHEN HIT, CHANCE TO DASH STEP BACKWARDS 
                else if (Main.rand.NextBool(8))
                {
                    npc.velocity.Y = -4f;
                    npc.velocity.X = -7f * npc.direction;
                    npc.localAI[1] = 150f;
                    npc.netUpdate = true;
                }
                else if (Main.rand.NextBool(4))
                {
                    npc.TargetClosest(true);
                    npc.velocity.Y = -7f;
                    npc.velocity.X = -10f * npc.direction;
                    npc.localAI[1] = 150f;
                    npc.netUpdate = true;
                }

            }
            if (!melee && Main.rand.NextBool(4))
            {
                if (Main.rand.NextBool(4))
                {

                    int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X - 6f, npc.velocity.Y, 150, Color.Red, 1f);
                    Main.dust[dust].noGravity = true;

                    npc.velocity.Y = -9f;
                    npc.velocity.X = 4f * npc.direction;
                    npc.TargetClosest(true);

                    if ((float)npc.direction * npc.velocity.X > 4)
                    {
                        npc.velocity.X = (float)npc.direction * 4;
                    }
                    npc.netUpdate = true;
                }
                if (Main.rand.NextBool(6))
                {

                    npc.ai[0] = 0f;
                    npc.velocity.Y = -5f;
                    npc.velocity.X *= 4f; // burst forward
                    npc.TargetClosest(true);

                    npc.velocity.X += (float)npc.direction * 5f;  //  accellerate fwd; can happen midair
                    if ((float)npc.direction * npc.velocity.X > 5)
                    {
                        npc.velocity.X = (float)npc.direction * 5;  //  but cap at top speed
                    }
                    //CHANCE TO JUMP AFTER DASH
                    if (Main.rand.NextBool(8))
                    {
                        npc.TargetClosest(true);
                        npc.spriteDirection = npc.direction;
                        npc.ai[0] = 0f;
                        npc.velocity.Y = -6f;
                    }
                    npc.netUpdate = true;
                }
                if (npc.Distance(Main.player[npc.target].Center) > 300 && Main.rand.NextBool(24))
                {
                    TeleportImmediately(npc, 20, false);
                }
            }

        }

        #region Reactive Shield (shared by the shield enemies — see ShieldProfile)
        /// <summary>
        /// Pre-emptive block: call once per AI tick from a shield enemy that is in NEUTRAL (not already guarding or
        /// mid-attack). When a threat is detected — a friendly projectile heading at it, or the player within
        /// <see cref="tsorcRevampGlobalNPC.ShieldThreatRange"/> — it rolls <see cref="tsorcRevampGlobalNPC.PreemptiveBlockChance"/>
        /// and, on success, sets <see cref="tsorcRevampGlobalNPC.ReactiveBlockTimer"/> so the enemy raises its guard
        /// BEFORE the hit lands. Returns true if it triggered a block this tick.
        /// </summary>
        public static bool TryPreemptiveBlock(NPC npc, tsorcRevampGlobalNPC globalNPC, int holdTicks = 75)
        {
            if (globalNPC.PreemptiveBlockChance <= 0f || globalNPC.ReactiveBlockTimer > 0)
            {
                return false;
            }
            if (!ShieldThreatIncoming(npc, globalNPC.ShieldThreatRange))
            {
                return false;
            }
            if (Main.rand.NextFloat() >= globalNPC.PreemptiveBlockChance)
            {
                return false;
            }
            globalNPC.ReactiveBlockTimer = holdTicks;
            return true;
        }

        /// <summary>
        /// On-hit block: call from a shield enemy's OnHitBy* hooks. Rolls <see cref="tsorcRevampGlobalNPC.OnHitBlockChance"/>
        /// to snap the guard up the instant it's hit, so it can catch the rest of a combo. Returns true if it blocked.
        /// </summary>
        public static bool TryOnHitBlock(NPC npc, tsorcRevampGlobalNPC globalNPC, bool melee, int holdTicks = 75)
        {
            if (globalNPC.OnHitBlockChance <= 0f || Main.rand.NextFloat() >= globalNPC.OnHitBlockChance)
            {
                return false;
            }
            globalNPC.ReactiveBlockTimer = holdTicks;
            return true;
        }

        /// <summary>
        /// True if a friendly projectile is heading roughly at this NPC, or the player is within <paramref name="meleeRange"/>
        /// px. Mirrors GlobalNPC's private EvasiveThreatNearby predicate (the dodge/jump-to-evade scan).
        /// </summary>
        public static bool ShieldThreatIncoming(NPC npc, int meleeRange)
        {
            if (meleeRange > 0 && npc.HasValidTarget &&
                npc.DistanceSQ(Main.player[npc.target].Center) < meleeRange * meleeRange)
            {
                return true;
            }

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.friendly && proj.damage > 0 && proj.DistanceSQ(npc.Center) < 40000 &&
                    UsefulFunctions.CompareAngles(proj.velocity, UsefulFunctions.Aim(proj.Center, npc.Center, 1)) < 0.3f)
                {
                    return true;
                }
            }

            return false;
        }
        #endregion

        #region Evasive On-Hit Reaction
        // Reusable scratch buffer for building the weighted behavior pool (single-threaded AI, so a shared static is safe).
        private static readonly List<EvasiveBehavior> EvasionPool = new List<EvasiveBehavior>(4);

        /// <summary>
        /// Occasional evasive reaction when an enemy is hit in neutral: it builds a weighted pool from whichever
        /// <c>Evasive*</c> capability flags the enemy has enabled (see <see cref="EvasiveProfile"/>) and samples one.
        /// PURELY repositioning — it never cancels an attack (the poise stagger does that) and only fires in pure
        /// neutral, never during a windup/attack (InAttack) or while staggered. If pinned against a wall it escapes
        /// TOWARD the player instead. <paramref name="melee"/> selects which behaviors are eligible (each carries its
        /// own <see cref="EvasiveHitSource"/>). See project_poise_stagger_system.
        /// </summary>
        public static void EvasiveOnHit(NPC npc, bool melee)
        {
            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();

            // Never react while attacking (windup or committed), staggered, or already mid-evasion (don't let a hit
            // restart/override an in-progress dash/retreat/quick-step) — interruption is the poise stagger's job.
            if (globalNPC.InAttack || globalNPC.StaggerTimer > 0 || globalNPC.InEvasion)
            {
                return;
            }

            // Getting shot out of a stand-and-fire / post-attack pause clears those pause flags so the react-roll
            // below can reposition the archer, instead of a separate 50%-gated snap-out reaction.
            if (!melee && (globalNPC.FighterRangedHitInterruptedPause || globalNPC.FighterPostAttackPauseTimer > 0 || globalNPC.FighterRangedStandShotsRemaining > 0))
            {
                globalNPC.FighterRangedHitInterruptedPause = false;
                globalNPC.FighterPostAttackPauseTimer = 0;
                globalNPC.FighterRangedStandShotsRemaining = 0;
            }

            // Rate-limit so an enemy doesn't react to every hit of a fast combo.
            if (globalNPC.FighterEvasionCooldown > 0)
            {
                return;
            }

            // If pinned against a wall, escape toward the player rather than evading into it.
            if (TryWallEscape(npc, globalNPC))
            {
                return;
            }

            // Build the weighted pool from this enemy's enabled behaviors that match this hit type. Weights reproduce
            // the original 5-case spread: RetreatJump (3 cosmetic variants) 3/5, RetreatDash 1/5, TeleportAway 1/5.
            EvasionPool.Clear();
            AddEvasion(globalNPC.EvasiveRetreatJump, EvasiveHitSource.Both, 3, EvasiveBehavior.RetreatJump, melee);
            AddEvasion(globalNPC.EvasiveRetreatDash, EvasiveHitSource.Both, 1, EvasiveBehavior.RetreatDash, melee);
            AddEvasion(globalNPC.EvasiveTeleportAway, EvasiveHitSource.Both, 1, EvasiveBehavior.TeleportAway, melee);
            AddEvasion(globalNPC.EvasiveLeapForward, EvasiveHitSource.Ranged, 1, EvasiveBehavior.LeapForward, melee);
            AddEvasion(globalNPC.EvasiveRunningDash, EvasiveHitSource.Both, 1, EvasiveBehavior.RunningDash, melee);
            AddEvasion(globalNPC.EvasiveRetreatAndShoot, EvasiveHitSource.Melee, 1, EvasiveBehavior.RetreatAndShoot, melee);
            AddEvasion(globalNPC.EvasiveQuickStep, EvasiveHitSource.Both, 2, EvasiveBehavior.QuickStep, melee);
            float targetDistance = npc.Distance(Main.player[npc.target].Center);
            AddEvasion(globalNPC.EvasiveBasiliskWalkerCloseBackhop && targetDistance < 150f, EvasiveHitSource.Both, 1, EvasiveBehavior.BasiliskWalkerCloseBackhop, melee);
            AddEvasion(globalNPC.EvasiveBasiliskWalkerFarScrambleHop && targetDistance > 150f, EvasiveHitSource.Both, 1, EvasiveBehavior.BasiliskWalkerFarScrambleHop, melee);
            AddEvasion(globalNPC.EvasiveBasiliskShifterCloseBackhop && targetDistance < 150f, EvasiveHitSource.Both, 1, EvasiveBehavior.BasiliskShifterCloseBackhop, melee);
            AddEvasion(globalNPC.EvasiveBasiliskShifterFarForwardHop && targetDistance > 150f, EvasiveHitSource.Both, 1, EvasiveBehavior.BasiliskShifterFarForwardHop, melee);
            if (EvasionPool.Count == 0)
            {
                return;
            }

            // Only react to a fraction of hits — keeps the enemy slippery without spasming.
            if (!Main.rand.NextBool(5))
            {
                return;
            }

            npc.TargetClosest(true);
            ExecuteEvasion(npc, globalNPC, EvasionPool[Main.rand.Next(EvasionPool.Count)], melee);

            globalNPC.FighterEvasionCooldown = 40; // ~0.66s before another reaction
            npc.netUpdate = true;
        }

        // Adds an enabled behavior to the pool `weight` times, but only if its hit-source matches this hit.
        private static void AddEvasion(bool enabled, EvasiveHitSource source, int weight, EvasiveBehavior behavior, bool melee)
        {
            if (!enabled)
            {
                return;
            }
            if (source == EvasiveHitSource.Melee && !melee)
            {
                return;
            }
            if (source == EvasiveHitSource.Ranged && melee)
            {
                return;
            }
            for (int i = 0; i < weight; i++)
            {
                EvasionPool.Add(behavior);
            }
        }

        // Executes one instantaneous evasive behavior. Assumes npc.direction already faces the player (TargetClosest).
        private static void ExecuteEvasion(NPC npc, tsorcRevampGlobalNPC globalNPC, EvasiveBehavior behavior, bool melee)
        {
            int away = -npc.direction;
            bool grounded = npc.velocity.Y == 0f;

            switch (behavior)
            {
                case EvasiveBehavior.RetreatJump: // hop / big leap / high-arc drift (was switch cases 0,1,3)
                    switch (Main.rand.Next(3))
                    {
                        case 0: // small hop back
                            npc.velocity.Y = -6f;
                            npc.velocity.X = 7f * away;
                            break;
                        case 1: // big leap back (toned down)
                            npc.velocity.Y = -8f;
                            npc.velocity.X = 7f * away;
                            break;
                        case 2: // jump high, drifting back
                            npc.velocity.Y = -11f;
                            npc.velocity.X = 4f * away;
                            break;
                    }
                    break;
                case EvasiveBehavior.RetreatDash: // low dash back (was case 2)
                    npc.velocity.X = 5f * away;
                    if (grounded)
                    {
                        npc.velocity.Y = -3f;
                    }
                    break;
                case EvasiveBehavior.TeleportAway: // teleport away if able, else leap back (was case 4)
                    if (globalNPC.CanTeleport)
                    {
                        TeleportImmediately(npc, 20, melee);
                    }
                    else
                    {
                        npc.velocity.Y = -6f;
                        npc.velocity.X = 8f * away;
                    }
                    break;
                case EvasiveBehavior.LeapForward: // lunge TOWARD the player to close the gap (instant, arced spread)
                    npc.velocity.X = -away * Main.rand.NextFloat(9f, 12f); // -away = toward the player
                    npc.velocity.Y = Main.rand.NextFloat(-4f, -7f);
                    break;
                case EvasiveBehavior.RunningDash: // arm the flash telegraph → grounded speed burst (sustained)
                    globalNPC.InSustainedEvasion = true;
                    globalNPC.CurrentEvasion = EvasiveBehavior.RunningDash;
                    globalNPC.EvasiveTelegraphing = true;
                    globalNPC.EvasiveTimer = globalNPC.EvasiveDashTelegraphTicks;
                    break;
                case EvasiveBehavior.RetreatAndShoot: // arm a forced-flee back-off window (sustained)
                    globalNPC.InSustainedEvasion = true;
                    globalNPC.CurrentEvasion = EvasiveBehavior.RetreatAndShoot;
                    globalNPC.EvasiveTelegraphing = false;
                    globalNPC.EvasiveTimer = globalNPC.EvasiveRetreatTicks;
                    break;
                case EvasiveBehavior.QuickStep: // arm an i-frame standing dash step (no rotation)
                    Player target = Main.player[npc.target];
                    const int maxForwardQuickStepTicks = 30;
                    float crossDistance = Math.Abs(target.Center.X - npc.Center.X) + (target.width + npc.width) * 0.5f + 16f;
                    bool canCrossThrough = crossDistance <= globalNPC.QuickStepSpeed * maxForwardQuickStepTicks;
                    bool stepForward = canCrossThrough && Main.rand.NextBool();
                    globalNPC.QuickStepDir = stepForward ? -away : away;
                    globalNPC.QuickStepRecoveryTimer = 0;
                    if (stepForward)
                    {
                        globalNPC.QuickStepTimer = Math.Clamp((int)Math.Ceiling(crossDistance / globalNPC.QuickStepSpeed), globalNPC.QuickStepTicks, maxForwardQuickStepTicks);
                    }
                    else
                    {
                        globalNPC.QuickStepTimer = globalNPC.QuickStepTicks;
                    }
                    break;
                case EvasiveBehavior.BasiliskWalkerCloseBackhop:
                    npc.velocity.Y = Main.rand.NextFloat(-6f, -3f);
                    npc.velocity.X += npc.direction * Main.rand.NextFloat(-5f, -3f);
                    break;
                case EvasiveBehavior.BasiliskWalkerFarScrambleHop:
                    npc.velocity.Y = Main.rand.NextFloat(-5f, -2f);
                    npc.velocity.X += npc.direction * Main.rand.NextFloat(-5f, 3f);
                    break;
                case EvasiveBehavior.BasiliskShifterCloseBackhop:
                    npc.velocity.Y = Main.rand.NextFloat(-6f, -4f);
                    npc.velocity.X += npc.direction * Main.rand.NextFloat(-7f, -4f);
                    break;
                case EvasiveBehavior.BasiliskShifterFarForwardHop:
                    npc.velocity.Y = Main.rand.NextFloat(-10f, -3f);
                    npc.velocity.X += npc.direction * Main.rand.NextFloat(3f, 7f);
                    break;
            }
        }

        // Drives the multi-tick evasion windows each AI tick. RunningDash: hold + flash during the telegraph, then a
        // hyper-armored grounded burst (the actual speed comes from the topSpeed multiplier in BasicAI; the mover keeps
        // pursuing). RetreatAndShoot: seizes the body and drives a grounded back-off away from the player. Whether it
        // preserves or resets shot progress is controlled by ShouldEvasionResetProjectileTimer. QuickStep has its own
        // exec block above and is not handled here.
        // Internal (not private) so the SmartFighter4 mover can tick the same sustained-evasion
        // windows FighterAI does — evasion is mover-agnostic state on the GlobalNPC, but until now
        // only FighterAI advanced it.
        internal static void UpdateEvasion(NPC npc, tsorcRevampGlobalNPC globalNPC)
        {
            if (!globalNPC.InSustainedEvasion)
            {
                return;
            }

            if (globalNPC.EvasiveTimer > 0)
            {
                globalNPC.EvasiveTimer--;
            }

            switch (globalNPC.CurrentEvasion)
            {
                case EvasiveBehavior.RunningDash:
                    if (globalNPC.EvasiveTelegraphing)
                    {
                        if (globalNPC.EvasiveTimer % 6 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            SpawnEvasiveTelegraph(npc);
                        }
                        if (globalNPC.EvasiveTimer == 0) // telegraph done → start the burst
                        {
                            globalNPC.EvasiveTelegraphing = false;
                            globalNPC.EvasiveTimer = globalNPC.EvasiveDashTicks;
                            npc.TargetClosest(true);
                            npc.netUpdate = true;
                        }
                    }
                    else
                    {
                        npc.knockBackResist = 0f; // hyper-armor: no knockback for the duration of the burst
                        if (globalNPC.EvasiveTimer == 0)
                        {
                            EndSustainedEvasion(npc, globalNPC);
                        }
                    }
                    break;
                case EvasiveBehavior.RetreatAndShoot:
                    // Drive a grounded back-off directly (SeizesBody makes the mover yield).
                    npc.TargetClosest(true);
                    npc.velocity.X = globalNPC.EvasiveRetreatSpeed * -npc.direction; // -direction = away from the player
                    if (globalNPC.EvasiveTimer == 0)
                    {
                        EndSustainedEvasion(npc, globalNPC);
                    }
                    break;
                default:
                    EndSustainedEvasion(npc, globalNPC);
                    break;
            }
        }

        private static void EndSustainedEvasion(NPC npc, tsorcRevampGlobalNPC globalNPC)
        {
            // RunningDash zeroed knockBackResist for hyper-armor; restore the captured SetDefaults value. Poise enemies
            // also get this back at the top of BasicAI each tick, but non-poise enemies rely on this restore.
            if (globalNPC.CurrentEvasion == EvasiveBehavior.RunningDash && globalNPC.BaseKnockBackResist >= 0f)
            {
                npc.knockBackResist = globalNPC.BaseKnockBackResist;
            }
            globalNPC.InSustainedEvasion = false;
            globalNPC.EvasiveTelegraphing = false;
            globalNPC.EvasiveTimer = 0;
        }

        private static void SpawnEvasiveTelegraph(NPC npc)
        {
            Vector2 spawnPosition = npc.Center + new Vector2(Main.rand.NextFloat(-npc.width / 2f, npc.width / 2f), Main.rand.NextFloat(-npc.height / 2f, npc.height / 2f));
            Projectile.NewProjectileDirect(npc.GetSource_FromThis(), spawnPosition, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer);
        }

        /// <summary>
        /// If the enemy is pinned against a wall on the side knockback pushes it (away from the player), break the pin by
        /// moving TOWARD the player: dodgeroll if able (rolls toward the player), else an optional teleport, else a jump.
        /// Returns true if it escaped (and sets the shared evasion cooldown). See project_poise_stagger_system.
        /// </summary>
        private static bool TryWallEscape(NPC npc, tsorcRevampGlobalNPC globalNPC)
        {
            npc.TargetClosest(true);
            int toward = npc.direction; // toward the player = the open side
            int away = -toward;         // the side knockback pushes it / where a pinning wall would be

            // Probe an 8px column just past the NPC's edge on the wall side, spanning its body height.
            float edgeX = npc.Center.X + away * (npc.width / 2f);
            Vector2 probePos = new Vector2(away > 0 ? edgeX : edgeX - 8f, npc.Center.Y - npc.height / 2f + 2f);
            if (!Collision.SolidCollision(probePos, 8, npc.height - 4))
            {
                return false; // not pinned
            }

            bool grounded = npc.velocity.Y == 0f;
            if (globalNPC.CanDodgeroll && grounded)
            {
                npc.spriteDirection = npc.direction;
                globalNPC.DodgeTimer = 30; // BasicAI rolls at 5 * npc.direction → toward the player, out of the pin
                globalNPC.DodgeCooldown = (int)(300 * (1 - globalNPC.Agility));
            }
            else if (globalNPC.CanTeleport && Main.rand.NextBool(2))
            {
                TeleportImmediately(npc, 18, false);
            }
            else
            {
                npc.velocity.Y = -8f;        // jump toward the player to clear the wall
                npc.velocity.X = 5f * toward;
            }

            globalNPC.FighterEvasionCooldown = 45;
            npc.netUpdate = true;
            return true;
        }

        #endregion

        #region Gwyn Hit AI
        public static void GwynOnHit(NPC npc, bool melee) //ref int stunlockBreak
        {

            if (melee)
            {
                // Ensures melee can't interrupt attack once the flash telegraph triggers
                if ((npc.ai[1] < 155f) || (npc.ai[1] > 180f && npc.ai[1] < 300f) || (npc.ai[1] > 325f && npc.ai[1] < 900f) || npc.ai[1] > 925f)
                {
                    int randomChoice = Main.rand.Next(10);

                    switch (randomChoice)
                    {
                        case 0:
                            npc.ai[1] = 50f;
                            break;

                        case 1:
                            npc.ai[1] = 700f;
                            break;

                        case 2:
                            npc.ai[1] = 200f;
                            break;

                        case 3:
                            npc.ai[1] = 800f;
                            break;
                        case 4:
                            // Big jump back - Spear
                            if (Main.rand.NextBool(2))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -9f;
                                npc.velocity.X = -9f * npc.direction;
                                npc.ai[1] = 140f;
                                npc.netUpdate = true;
                            }
                            else
                            {
                                npc.ai[1] = 50f;
                            }
                            break;
                        case 5:
                            // Small dash back - Bomb
                            if (Main.rand.NextBool(2))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -6f;
                                npc.velocity.X = -8f * npc.direction;
                                npc.ai[1] = 860f;
                                npc.netUpdate = true;


                            }
                            // Alt dash - Bomb
                            else
                            {
                                npc.ai[1] = 850f;
                                npc.TargetClosest(true);
                                npc.velocity.Y = -4f;
                                npc.velocity.X = -9f * npc.direction;
                            }
                            break;
                        case 6:
                            // Big dash back - Bomb
                            if (Main.rand.NextBool(2))
                            {
                                npc.TargetClosest(true);
                                npc.ai[1] = 880f;
                                npc.velocity.Y = -8f;
                                npc.velocity.X = -11f * npc.direction;
                                npc.netUpdate = true;
                            }
                            else
                            {
                                npc.TargetClosest(true);
                                npc.ai[1] = 50f;
                            }
                            break;
                        case 7:
                            // Teleport
                            if (Main.rand.NextBool(2))
                            {
                                npc.spriteDirection = npc.direction;
                                TeleportImmediately(npc, 22, true);
                                npc.netUpdate = true;
                            }
                            else
                            {
                                // Poison TP
                                npc.spriteDirection = npc.direction;
                                TeleportImmediately(npc, 22, true);
                                npc.TargetClosest(true);
                                npc.velocity.Y = -10f;
                                npc.velocity.X = -6f * npc.direction;
                                npc.ai[1] = 260f;
                            }
                            break;
                        case 8:
                            //Small dash back - Spear
                            if (Main.rand.NextBool(2))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -3f;
                                npc.velocity.X = -7f * npc.direction;
                                npc.ai[1] = 130f;
                                npc.netUpdate = true;
                            }
                            else
                            {
                                // Jump high
                                npc.TargetClosest(true);
                                npc.velocity.Y = -11f;
                                npc.velocity.X = -7f * npc.direction;
                                npc.ai[1] = 130f;

                            }
                            break;
                        case 9:
                            // Dash back - Poison
                            if (Main.rand.NextBool(2))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -6f;
                                npc.velocity.X = -9f * npc.direction;
                                npc.ai[1] = 280f;
                                npc.netUpdate = true;
                            }
                            else
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -10f;
                                npc.velocity.X = -7f * npc.direction;
                                npc.ai[1] = 280f;
                            }
                            break;


                    }
                    npc.netUpdate = true;
                }
                else
                {
                    npc.knockBackResist = 0;
                }

                npc.knockBackResist = 0.4f; //was 0.9            
            }

            if (!melee)
            {
                // Ensures ranged can't interrupt attack once the flash telegraph triggers
                if ((npc.ai[1] < 155f) || (npc.ai[1] > 180f && npc.ai[1] < 300f) || (npc.ai[1] > 325f && npc.ai[1] < 900f) || npc.ai[1] > 925f)
                {
                    int randomChoice = Main.rand.Next(9);

                    switch (randomChoice)
                    {
                        case 0:
                            // Burst forward
                            if (Main.rand.NextBool(4))
                            {
                                npc.velocity.Y = -9f;
                                npc.velocity.X = 4f * npc.direction;
                                npc.TargetClosest(true);

                                if ((float)npc.direction * npc.velocity.X > 4)
                                {
                                    npc.velocity.X = (float)npc.direction * 3;  //  3 was 4 - this caps the top speed
                                }
                                npc.netUpdate = true;
                            }
                            break;

                        case 1:
                            // Burst forward
                            if (Main.rand.NextBool(6))
                            {
                                npc.velocity.Y = -6f;
                                npc.velocity.X *= 4f; // burst forward
                                npc.TargetClosest(true);

                                npc.velocity.X += (float)npc.direction * 5f;  //  accellerate fwd; can happen midair
                                if ((float)npc.direction * npc.velocity.X > 5)
                                {
                                    npc.velocity.X = (float)npc.direction * 5;  //  but cap at top speed
                                }

                                // Chance to jump after dash
                                if (Main.rand.NextBool(6))
                                {
                                    npc.TargetClosest(true);
                                    npc.spriteDirection = npc.direction;
                                    npc.velocity.Y = -6f;
                                }

                                npc.netUpdate = true;
                            }
                            break;

                        case 2:
                            // Teleport
                            if (npc.Distance(Main.player[npc.target].Center) > 400 && Main.rand.NextBool(3))
                            {
                                TeleportImmediately(npc, 15, false);
                            }
                            break;

                        case 3:
                            // Dash backwards - Poison
                            if (Main.rand.NextBool(4))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -6f;
                                npc.velocity.X = -9f * npc.direction;
                                npc.ai[1] = 290f;
                                npc.netUpdate = true;
                            }
                            else if (Main.rand.NextBool(4))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -10f;
                                npc.velocity.X = -7f * npc.direction;
                                npc.ai[1] = 290f;
                            }
                            break;
                        case 4:
                            // Chance to big jump backwards - Spear
                            if (Main.rand.NextBool(4))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -9f;
                                npc.velocity.X = -9f * npc.direction;
                                npc.ai[1] = 140f;
                                npc.netUpdate = true;
                            }
                            break;
                        case 5:
                            // Small dash backwards - Bomb
                            if (Main.rand.NextBool(2))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -6f;
                                npc.velocity.X = 6f * npc.direction;
                                npc.ai[1] = 860f;
                                npc.netUpdate = true;
                            }
                            // Alt dash backwards - Bomb
                            else
                            {
                                npc.ai[1] = 850f;
                                npc.TargetClosest(true);
                                npc.velocity.Y = -4f;
                                npc.velocity.X = -9f * npc.direction;
                            }
                            break;
                        case 6:
                            // Big dash backwards - Bomb
                            if (Main.rand.NextBool(4))
                            {
                                npc.TargetClosest(true);
                                npc.ai[1] = 880f;
                                npc.velocity.Y = -8f;
                                npc.velocity.X = -11f * npc.direction;
                                npc.netUpdate = true;
                            }
                            break;
                        case 7:
                            // Teleport
                            if (Main.rand.NextBool(4))
                            {
                                TeleportImmediately(npc, 20, true);
                                npc.netUpdate = true;
                            }
                            else if (Main.rand.NextBool(4))
                            // Poision Teleport
                            {
                                TeleportImmediately(npc, 20, true);
                                npc.TargetClosest(true);
                                npc.velocity.Y = -10f;
                                npc.velocity.X = -5f * npc.direction;
                                npc.ai[1] = 250f;
                            }
                            break;
                        case 8:
                            // Small dash backwards - Spear
                            if (Main.rand.NextBool(3))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -3f;
                                npc.velocity.X = -7f * npc.direction;
                                npc.ai[1] = 140f;
                                npc.netUpdate = true;
                            }
                            else
                            // Jump high, slightly forward
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -10f;
                                npc.velocity.X = 3f * npc.direction;
                                npc.ai[1] = 130f;
                                npc.netUpdate = true;
                            }
                            break;
                        case 9:
                            // Attack interrupt; for Great Red Knight it cycles to DD2 attack at 1/2 health
                            npc.ai[1] = 700f;
                            break;
                    }
                    npc.netUpdate = true;
                }
            }
        }
        #endregion
        #endregion
    }
}
