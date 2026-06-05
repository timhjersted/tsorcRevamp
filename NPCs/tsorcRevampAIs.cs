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
        public static void FighterAI(NPC npc, float topSpeed = 1f, float acceleration = .07f, float brakingPower = .2f, bool canTeleport = false, int doorBreakingDamage = 4, bool hatesLight = false, SoundStyle? randomSound = null, int soundFrequency = 1000, float enragePercent = 0, float enrageTopSpeed = 0, bool lavaJumping = false, bool canDodgeroll = true, bool canPounce = true, bool sizeMatters = false, int minSurfaceWidth = 2, bool canWalkBackwards = false)
        {
            npc.aiStyle = -1;
            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.SizeMatters = sizeMatters;
            globalNPC.MinSurfaceWidth = minSurfaceWidth;
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
        public static void ArcherAI(NPC npc, int projectileType, int projectileDamage, float projectileVelocity, int projectileCooldown, float topSpeed = 1f, float acceleration = .07f, float brakingPower = .2f, bool canTeleport = false, int doorBreakingDamage = 4, bool hatesLight = false, SoundStyle? randomSound = null, int soundFrequency = 1000, float enragePercent = 0, float enrageTopSpeed = 0, bool lavaJumping = false, float projectileGravity = 0.035f, SoundStyle? shootSound = null, bool canDodgeroll = true, bool canPounce = false, Color? telegraphColor = null)
        {
            BasicAI(npc, topSpeed, acceleration, brakingPower, true, canTeleport, doorBreakingDamage, hatesLight, randomSound, soundFrequency, enragePercent, enrageTopSpeed, lavaJumping, canDodgeroll, false);
            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();

            if (telegraphColor == null)
            {
                telegraphColor = Color.Gray;
            }

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
                if (globalNPC.ProjectileTimer > 0f)
                    globalNPC.ProjectileTimer -= 1f; // decrement fire & reload counter

                // Don't let airborne state abort a shot once the telegraph has already fired.
                bool inTelegraphWindow = globalNPC.ProjectileTimer <= (projectileCooldown / 2 + 15) && globalNPC.ProjectileTimer > (projectileCooldown / 2);
                if (npc.justHit || (npc.velocity.Y != 0f && !inTelegraphWindow) || globalNPC.ProjectileTimer <= 0f)
                {
                    globalNPC.ProjectileTimer = (int)(projectileCooldown * globalNPC.CastingSpeed); //Reset firing time
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
                        globalNPC.ProjectileTimer = (int)(projectileCooldown * globalNPC.CastingSpeed);

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

                    // Telegraph fires 15 ticks before the shot: lock the aim direction now so
                    // a dodge-roll behind the enemy can't redirect the incoming projectile.
                    if (globalNPC.ProjectileTimer - 15 == (projectileCooldown / 2))
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
                    if (globalNPC.ProjectileTimer == (projectileCooldown / 2))
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
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
            if (npc.target < 0 || npc.target >= Main.maxPlayers || !Main.player[npc.target].active || Main.player[npc.target].dead)
            {
                npc.TargetClosest(false);
            }
            topSpeed *= globalNPC.Swiftness;
            acceleration *= globalNPC.Swiftness;
            if (globalNPC.FighterNoLosPursuitBoostTimer > 0)
            {
                globalNPC.FighterNoLosPursuitBoostTimer--;
                topSpeed *= 1.25f;
                acceleration *= 1.35f;
            }

            if (!globalNPC.Initialized)
            {
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

            //If it has at least one attack, perform it
            if (globalNPC.AttackList.Count > 0)
            {
                SimpleProjectile(npc);
            }

            if (globalNPC.PounceTimer > 0)
            {
                globalNPC.PounceTimer--;

                if (globalNPC.PounceTimer % 5 == 0)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 spawnPosition = npc.position;
                        spawnPosition.Y += npc.height;
                        spawnPosition.X += Main.rand.NextFloat(npc.width);
                        Projectile.NewProjectileDirect(npc.GetSource_FromThis(), spawnPosition, new Vector2(0, 2), ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer);
                    }
                }

                if (globalNPC.PounceTimer == 0)
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
            }
            else if (globalNPC.PounceCooldown > 0)
            {
                globalNPC.PounceCooldown--;
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

            // Fleeing: a low-HP flee (set on hit) or a hatesLight enemy caught in daylight above ground.
            // Replaces the old BoredTimer = -999 sentinel; gates firing + reverses facing below.
            bool fleeing = globalNPC.Fleeing || (hatesLight && Main.dayTime && (npc.position.Y / 16f) < Main.worldSurface);

            //Stop moving when teleporting, and handle the logic to execute it
            if (globalNPC.TeleportCountdown > 0)
            {
                npc.velocity.X = 0;
                globalNPC.TeleportCountdown--;
                if (globalNPC.TeleportCountdown == 0)
                {
                    ExecuteQueuedTeleport(npc);
                }
            }

            //Block firing and reset cooldowns if it's busy doing other things (incl. fleeing)
            if (globalNPC.TeleportCountdown > 0 || fleeing || globalNPC.DodgeTimer > 0 || globalNPC.PounceTimer > 0)
            {
                globalNPC.ProjectileTimer = 0;
                globalNPC.ArcherAimDirection = 0;
            }

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

                // Being hit forces a re-acquire (mirrors the old BoredTimer = 0 on justHit).
                if (npc.justHit)
                {
                    globalNPC.PursuitState = PursuitState.Pursue;
                    globalNPC.DisengageTimer = 0;
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

                PursuitState fsmState = NavBehavior.UpdateState(npc, globalNPC, fsmPlayer, fsmLos, fsmProgress, FighterAggroRange);

                if (globalNPC.TeleportCooldownTimer > 0) globalNPC.TeleportCooldownTimer--;

                // Disengage resolver (4b): blink to re-acquire when the give-up condition for this style is met.
                // Charges + per-style cooldown gate it; out of charges / on cooldown / no valid spot → falls
                // through to Patrol. After a blink the NPC has LOS → FSM → Pursue.
                if (globalNPC.CanTeleport && globalNPC.TeleportCountdown == 0
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
                    if (fireTeleport && !TryTeleportReacquire(npc, globalNPC))
                    {
                        // No valid spot this attempt — throttle the (100×100) search so it doesn't run every
                        // frame while the NPC patrols; it'll retry shortly or fall through to Patrol.
                        globalNPC.TeleportCooldownTimer = 30;
                    }
                }

                if (fsmState == PursuitState.Patrol)
                {
                    // Mid-blink (a teleport was just queued, or is counting down) → hold; otherwise patrol.
                    if (globalNPC.TeleportCountdown == 0)
                    {
                        NavBehavior.RunPatrol(npc, globalNPC, topSpeed, acceleration);
                        if (!npc.noTileCollide && !npc.noGravity) AutoStepUp(npc);
                    }
                    // Don't pursue, attack, dodge, or pounce while patrolling.
                    globalNPC.ProjectileTimer = 0f;
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

            // Compute line of sight early (used by movement + boredom logic below).
            bool lineOfSight = Main.player[npc.target].CanHit(npc);

            // Face the player (dead zone avoids jitter when nearly aligned). Fleeing walks away.
            if (Math.Abs(Main.player[npc.target].Center.X - npc.Center.X) > 30)
            {
                npc.direction = Main.player[npc.target].Center.X <= npc.Center.X ? -1 : 1;
                if (fleeing) // walk away from the player
                {
                    npc.direction *= -1;
                }
                npc.spriteDirection = npc.direction;

                if (globalNPC.CanWalkBackwards && !fleeing)
                {
                    float playerDist = npc.Distance(Main.player[npc.target].Center);
                    if (playerDist < 180f)
                    {
                        // Reverse physical direction to walk backwards
                        npc.direction *= -1;
                        // Keep sprite facing the player
                        npc.spriteDirection = Main.player[npc.target].Center.X <= npc.Center.X ? -1 : 1;
                    }
                }
            }

            //If moving more than max speed, then slow down
            if (globalNPC.PounceCooldown <= 240)
            {
                if (npc.velocity.X > topSpeed)
                {
                    npc.velocity.X -= brakingPower;
                    if (npc.velocity.X < 0)
                    {
                        npc.velocity.X = 0;
                    }
                }
                if (npc.velocity.X < -topSpeed)
                {
                    npc.velocity.X += brakingPower;
                    if (npc.velocity.X > 0)
                    {
                        npc.velocity.X = 0;
                    }
                }
            }

            // Post-attack standoff: tier 2 enemies may briefly hold position after a few attacks,
            // but ordinary LOS should not stop pursuit.
            bool inStandoff = false;
            if (globalNPC.FighterPostAttackPauseTimer > 0)
            {
                globalNPC.FighterPostAttackPauseTimer--;
            }
            if (globalNPC.CanStopToFire && !globalNPC.CanPassThroughWalls && (globalNPC.FighterPostAttackPauseTimer > 0 || globalNPC.FighterRangedStandShotsRemaining > 0) && lineOfSight && npc.velocity.Y == 0f && !globalNPC.Fleeing)
            {
                inStandoff = true;
                if (globalNPC.FighterRangedStandShotsRemaining > 0)
                    npc.velocity.X = 0f; // hard stop: hold position while firing a burst
                else
                    npc.velocity.X *= 0.8f; // gradual stop: post-attack breather
            }

            //Accelerate in the direction they are facing (unless the npc is an aiming archer)
            if ((!isArcher || globalNPC.ArcherAimDirection == 0) && !inStandoff)
            {
                if (npc.velocity.X < topSpeed && npc.direction == 1)
                {
                    npc.velocity.X += acceleration;
                    if (npc.velocity.X > topSpeed)
                    {
                        npc.velocity.X = topSpeed;
                    }
                }
                else
                {
                    if (npc.velocity.X > -topSpeed && npc.direction == -1)
                    {
                        npc.velocity.X -= acceleration;
                        if (npc.velocity.X < -topSpeed)
                        {
                            npc.velocity.X = -topSpeed;
                        }
                    }
                }
            }


            // Ledge-halt: optionally stop before a significant drop when we already have LOS.
            // Only halts if dropping would put the NPC meaningfully lower than the player —
            // tiny drops and same-elevation crossings are left alone so jump/gap logic can handle them.
            // A hard cap of 180 frames prevents indefinite ledge-camping.
            if (!globalNPC.CanPassThroughWalls && globalNPC.HaltAtLedge && lineOfSight && npc.velocity.Y == 0f && !globalNPC.Fleeing)
            {
                int aheadX = npc.direction == -1
                    ? (int)(npc.position.X / 16f) - 1
                    : (int)((npc.position.X + npc.width) / 16f);
                int belowY = (int)(npc.position.Y + npc.height + 8f) / 16;

                if (!UsefulFunctions.IsTileReallySolid(aheadX, belowY))
                {
                    // Scan downward for solid ground (ignores water/air). Cap at 10 tiles.
                    int dropDepth = 10;
                    for (int dy = 1; dy <= 10; dy++)
                    {
                        if (UsefulFunctions.IsTileReallySolid(aheadX, belowY + dy))
                        {
                            dropDepth = dy;
                            break;
                        }
                    }

                    // Where would we land, in world-space Y pixels?
                    float landingWorldY = (belowY + dropDepth) * 16f;
                    float playerWorldY = Main.player[npc.target].Center.Y;

                    // Halt only if landing would put us more than 3 tiles below the player
                    // (losing elevation), AND the drop is at least 4 tiles deep (not a tiny step).
                    bool wouldLoseElevation = landingWorldY > playerWorldY + 48f;
                    bool dropIsSignificant = dropDepth >= 4;
                    bool shouldHalt = wouldLoseElevation && dropIsSignificant && globalNPC.LedgeHaltTimer < 180;

                    if (shouldHalt)
                    {
                        npc.velocity.X = 0f;
                        globalNPC.LedgeHaltTimer++;
                    }
                    else
                    {
                        globalNPC.LedgeHaltTimer = 0;
                    }
                }
                else
                {
                    globalNPC.LedgeHaltTimer = 0;
                }
            }
            else
            {
                globalNPC.LedgeHaltTimer = 0;
            }

            //Jumping and platform falling code, copied and edited from Firebomb Hollow
            int x_in_front;
            if (npc.direction == -1)
            {
                x_in_front = (int)(npc.position.X / 16f) - 1;
            }
            else
            {
                x_in_front = (int)((npc.position.X + npc.width) / 16f);
            }

            int y_above_feet = (int)((npc.position.Y + (float)npc.height - 15f) / 16f); // 15 pix above feet
            //Dust.DrawDebugBox(new Rectangle(x_in_front * 16, y_above_feet * 16, 16, 16));
            int y_below_feet = (int)(npc.position.Y + (float)npc.height + 8f) / 16;
            bool standing_on_solid_tile = false;

            //Check if standing on a solid tile
            int x_left_edge = (int)npc.position.X / 16;
            int x_right_edge = (int)(npc.position.X + (float)npc.width) / 16;
            if (npc.velocity.Y == 0)
            {
                for (int l = x_left_edge; l <= x_right_edge; l++) // check every block under feet
                {
                    if (UsefulFunctions.IsTileReallySolid(l, y_below_feet)) // tile exists and is solid
                    {
                        standing_on_solid_tile = true;
                    }
                }
            }

            // SizeMatters Navigation check
            if (globalNPC.SizeMatters)
            {
                int minWidth = globalNPC.MinSurfaceWidth;
                int centerTileX = (int)(npc.Center.X / 16f);
                
                bool currentValid = UsefulFunctions.IsPartOfValidSurface(centerTileX, y_below_feet, minWidth);
                bool targetValid = UsefulFunctions.IsPartOfValidSurface(x_in_front, y_below_feet, minWidth) 
                                || UsefulFunctions.IsPartOfValidSurface(x_in_front, y_below_feet - 1, minWidth);
                                
                if (!currentValid || !targetValid)
                {
                    if (standing_on_solid_tile)
                    {
                        npc.velocity.Y = -globalNPC.MaxJumpPower * 0.8f;
                        npc.velocity.X = globalNPC.MaxJumpBoost * npc.direction;
                        npc.netUpdate = true;
                    }
                    else
                    {
                        if (!targetValid)
                        {
                            npc.velocity.X *= 0.8f;
                        }
                    }
                }
            }

            // Terrain handling: height-aware wall climbs (scaled to MaxJumpPower) + gap jump + door breaking.
            if (standing_on_solid_tile)
            {
                if ((npc.velocity.X < 0f && npc.spriteDirection == -1) || (npc.velocity.X > 0f && npc.spriteDirection == 1))
                {
                    // Step 4a — height-aware climb scaled to THIS enemy's jump power. Sense the full wall
                    // height ahead, work out how high its own MaxJumpPower (default 8f = vanilla) can actually
                    // reach given gravity, and jump only walls within that reach AND with clearance above the
                    // top to land on (headroom). Walls taller than it can clear, or capped by a ceiling, are
                    // NOT jumped — it stands pressed and the position-immobility anti-stuck below disengages it
                    // (→ Patrol/teleport) instead of bouncing forever (see the RedKnight wall-bounce log).
                    // This also wires the per-enemy MaxJumpPower / MaxJumpBoost levers into the dumb (tier-0)
                    // path for the first time (they were tier-≥1-only before).
                    float jumpPower = globalNPC.MaxJumpPower;
                    float grav = npc.gravity > 0.01f ? npc.gravity : 0.3f;
                    bool wallAhead = UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 1)
                                  || UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 2);
                    if (wallAhead)
                    {
                        // Contiguous wall height ahead (solid tiles stacked up from the step row).
                        int wallTiles = 0;
                        while (wallTiles < 12 && UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - wallTiles))
                            wallTiles++;
                        int topY = y_above_feet - wallTiles; // first open row above the wall

                        // ~2 tiles of headroom above the wall top at both the wall and the landing column.
                        bool headroom = !UsefulFunctions.IsTileReallySolid(x_in_front, topY)
                                     && !UsefulFunctions.IsTileReallySolid(x_in_front, topY - 1)
                                     && !UsefulFunctions.IsTileReallySolid(x_in_front + npc.direction, topY)
                                     && !UsefulFunctions.IsTileReallySolid(x_in_front + npc.direction, topY - 1);

                        // Apex height (tiles) this jump can reach; 0.9 margin so it clears the corner, not just
                        // grazes the apex.
                        int reachableTiles = (int)((jumpPower * jumpPower) / (2f * grav) / 16f * 0.9f);

                        if (wallTiles >= 2 && wallTiles <= reachableTiles && headroom)
                        {
                            // Jump just hard enough to clear (wallTiles + 1) tiles, capped at MaxJumpPower.
                            float neededV = (float)Math.Sqrt(2f * grav * (wallTiles + 1) * 16f);
                            npc.velocity.Y = -Math.Min(neededV, jumpPower);
                            npc.netUpdate = true;
                        }
                        // wallTiles == 1 -> AutoStepUp glide handles it; too tall / capped / no headroom -> no
                        // jump, stand and give up.
                    }
                    // (1-tile step removed: the smooth AutoStepUp glide below replaces the old -5f hop so
                    //  tier-0 walkers no longer visibly bounce over every single-tile bump.)
                    else if (npc.directionY < 0 && !UsefulFunctions.IsTileReallySolid(x_in_front, y_below_feet) && !UsefulFunctions.IsTileReallySolid(x_in_front + npc.direction, y_below_feet))
                    {
                        // Cross a gap toward an above player — vertical from MaxJumpPower, horizontal from MaxJumpBoost.
                        npc.velocity.Y = -jumpPower;
                        npc.velocity.X += globalNPC.MaxJumpBoost * npc.direction;
                        npc.netUpdate = true;
                    }

                    if (UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 1) && Main.tile[x_in_front, y_above_feet - 1].TileType == 10 && (doorBreakingDamage > 0))
                    {
                        npc.velocity.Y = 0;
                        if (Main.GameUpdateCount % 60 == 0)
                        {
                            npc.velocity.X = 0.5f * -npc.direction;
                            globalNPC.DoorBreakProgress += doorBreakingDamage;
                            WorldGen.KillTile(x_in_front, y_above_feet - 1, true, true, false);
                            if (globalNPC.DoorBreakProgress >= 10f && Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                globalNPC.DoorBreakProgress = 0;
                                if (!WorldGen.OpenDoor(x_in_front, y_above_feet, npc.direction))
                                {
                                    // Door is jammed (e.g. blocked above): stop ramming. The position-immobility
                                    // anti-stuck disengages it to Patrol/teleport after ~1.5s.
                                    npc.velocity.X = 0;
                                }
                                else if (Main.netMode == NetmodeID.Server)
                                {
                                    NetMessage.SendData(MessageID.ToggleDoorState, -1, -1, null, 0, (float)x_in_front, (float)y_above_feet, (float)npc.direction, 0);
                                }
                            }
                        }
                    }
                }
            }



            //Can fall through platforms
            bool standing_on_platforms = true;
            bool atLeastOnePlatform = false;
            if (npc.velocity.Y == 0)
            {
                for (int l = x_left_edge; l <= x_right_edge; l++) // check every block under feet
                {
                    if (TileID.Sets.Platforms[Main.tile[l, y_below_feet].TileType])
                    {
                        atLeastOnePlatform = true;
                    }
                    else
                    {
                        if (Main.tile[l, y_below_feet].HasTile)
                        {
                            standing_on_platforms = false;
                        }
                    }
                }
            }

            // Drop through platforms when player is below.
            // Threshold is 64px (4 tiles) to avoid accidental drops on tiny height differences.
            // Gate on low horizontal speed: noTileCollide disables ALL tile collision, so a fast-moving
            // NPC would clip through walls. Only drop when nearly stopped horizontally.
            bool playerIsBelow = Main.player[npc.target].Center.Y > npc.Center.Y + 32f;
            // Drop when the player is below AND either roughly overhead, or we've failed to make progress for
            // ~1s (DisengageTimer) — the FSM-clean replacement for the old "BoredTimer > 60" fallback.
            bool shouldDropPlatform = playerIsBelow && (globalNPC.DisengageTimer > 60 || Math.Abs(npc.Center.X - Main.player[npc.target].Center.X) < 300);

            if (standing_on_platforms && atLeastOnePlatform && shouldDropPlatform)
            {
                npc.noTileCollide = true;
            }

            // Smoothly glide up 1-tile steps instead of the removed -5f hop. Skip while platform-dropping
            // (noTileCollide) so the lift doesn't fight the drop.
            if (!npc.noTileCollide && !npc.noGravity)
            {
                AutoStepUp(npc);
            }

            // Reset double jump when standing (velocity.Y == 0)
            if (npc.velocity.Y == 0f)
            {
                globalNPC.UsedDoubleJump = false;
            }

            // Double jump: apex-triggered mid-air second jump for capable enemies.
            // Gated purely by the CanDoubleJump bool (default false) — tier-independent.
            if (globalNPC.CanDoubleJump && !globalNPC.UsedDoubleJump)
            {
                // Fire when clearly falling (player is still above us) — velocity.Y > 1.5f avoids
                // triggering on the first few frames after stepping off a ledge
                if (!standing_on_solid_tile && npc.velocity.Y > 1.5f && npc.directionY < 0)
                {
                    npc.velocity.Y = -globalNPC.DoubleJumpPower;
                    globalNPC.UsedDoubleJump = true;
                    npc.netUpdate = true;
                }
            }

            // Refresh after movement phase — tile state may have changed (platform drop, etc.)
            lineOfSight = Main.player[npc.target].CanHit(npc);

            // Step 4a LOS fix: boredom is now "no LOS" only — the old `playerOnDifferentLevel`
            // (|Δy| > 48f) qualifier is dropped, since with the FSM a vertical offset no longer means
            // "can't reach" (the give-up clock + anti-stuck handle genuinely unreachable players).

            // ── Tier-0 position-immobility anti-stuck (Step 4a) ──────────────────────────────────────
            // A visible-but-walled player must not trap the NPC forever, so this fires regardless of LOS.
            // It is keyed on the NPC's OWN position (not velocity / distance), so the repeated wall-jump
            // bounce — which keeps velocity.Y != 0 and makes the player distance oscillate — can no longer
            // hide the stuck state (see the RedKnight wall-bounce log). If the NPC hasn't moved ~2 tiles for
            // ~1.5s while unable to engage, hand off to the FSM give-up (→ Search/Patrol/teleport). Adjacent
            // melee and aiming archers reset the timer so legitimate "stand and fight" doesn't read as stuck.
            if (globalNPC.PursuitState != PursuitState.Patrol)
            {
                bool canEngage = lineOfSight && npc.Distance(Main.player[npc.target].Center) < 64f;
                bool aiming = globalNPC.ArcherAimDirection != 0f || globalNPC.FighterRangedStandShotsRemaining > 0;
                if (canEngage || aiming || npc.Distance(globalNPC.StuckCheckPos) > 32f)
                {
                    globalNPC.StuckCheckPos = npc.Center;
                    globalNPC.StuckTimer = 0;
                }
                else
                {
                    globalNPC.StuckTimer++;
                    if (globalNPC.StuckTimer > 90) // ~1.5s without moving 2 tiles and unable to engage
                    {
                        globalNPC.StuckTimer = 0;
                        globalNPC.StuckCheckPos = npc.Center;
                        NavBehavior.ForceDisengage(npc, globalNPC);
                    }
                }
            }

            LogFighterNavDebug(npc, globalNPC, lineOfSight);

            //Dodging — only while actively pursuing (not searching/patrolling/fleeing/teleporting)
            if (globalNPC.PursuitState == PursuitState.Pursue && !fleeing && globalNPC.TeleportCountdown == 0 && globalNPC.DodgeCooldown == 0)
            {
                if (canDodgeroll && npc.Distance(Main.player[npc.target].Center) > 160)
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
                                //Randomly choose whether to roll or jump
                                if (Main.rand.NextBool() && heightToJump)
                                {
                                    npc.velocity.Y -= 8;
                                }
                                else
                                {
                                    globalNPC.DodgeTimer = 30;
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
            }
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
        private static void AutoStepUp(NPC npc)
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
            if (globalNPC.SizeMatters)
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
                string logPath = logDir + separator + "tsorcRevamp-nav.log";
                Player player = Main.player[npc.target];
                string line = $"[{DateTime.Now:HH:mm:ss}] {npc.TypeName}#{npc.whoAmI} pos=({npc.Center.X / 16f:F1},{npc.Center.Y / 16f:F1}) player=({player.Center.X / 16f:F1},{player.Center.Y / 16f:F1}) vel=({npc.velocity.X:F2},{npc.velocity.Y:F2}) g={!airborne} cx={npc.collideX} cy={npc.collideY} dist={npc.Distance(player.Center):F0} los={lineOfSight} yDiff={player.Center.Y - npc.Center.Y:F0} pursuit={globalNPC.PursuitState} disengage={globalNPC.DisengageTimer}/{globalNPC.NavGiveUpTicks} immobile={globalNPC.StuckTimer} tp={(globalNPC.CanTeleport ? $"{globalNPC.TeleportStyle}/ch{(globalNPC.TeleportChargesRemaining == int.MaxValue ? "inf" : globalNPC.TeleportChargesRemaining.ToString())}/cd{globalNPC.TeleportCooldownTimer}/cnt{globalNPC.TeleportCountdown}" : "off")} stopFire={globalNPC.CanStopToFire}";
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
            if (globalNPC.CurrentAttack.needsLineOfSight && !Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
            {
                actuallyFire = false;
            }

            //If the color was not set, use white
            if (globalNPC.CurrentAttack.color == null)
            {
                globalNPC.CurrentAttack.color = Color.White;
            }

            //Increment the timer. Stop increasing it once we reach the telegraph time. Only continue once it is actually firing. Once it is actually firing do not stop incrementing the timer, so that it can not stop firing after telegraphing a shot.
            if (globalNPC.ProjectileTimer < globalNPC.CurrentAttack.timerCap - ProjectileTelegraphTime || actuallyFire || globalNPC.ProjectileTimer > globalNPC.CurrentAttack.timerCap - ProjectileTelegraphTime)
            {
                globalNPC.ProjectileTimer++;

                //Spawn a telegraph flash once the telegraph time is reached
                if (globalNPC.ProjectileTimer == 1 + globalNPC.CurrentAttack.timerCap - ProjectileTelegraphTime)
                {
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

            //If it's supposed to stop moving when firing, then do so
            if (globalNPC.CanStopToFire && globalNPC.CurrentAttack.stopBefore && !globalNPC.CanPassThroughWalls)
            {
                bool inTelegraphWindow = globalNPC.ProjectileTimer > globalNPC.CurrentAttack.timerCap - ProjectileTelegraphTime;
                float stopBeforeChance = GetStandingFireChance(globalNPC, globalNPC.CurrentAttack.stopBeforeChance);

                if (inTelegraphWindow && Main.rand.NextFloat() < stopBeforeChance)
                {
                    npc.velocity.X = 0;
                    npc.velocity.Y = 0f; // suppress jump-frame animation while aiming

                    // Standing-fire roll: on the first frame of the telegraph window, tier-2 NPCs
                    // may commit to firing N shots in a row without resuming movement.
                    // Aggression lowers the chance to stand; Patience raises the burst count.
                    if (globalNPC.CanStopToFire && globalNPC.FighterRangedStandShotsRemaining == 0
                        && globalNPC.ProjectileTimer == globalNPC.CurrentAttack.timerCap - ProjectileTelegraphTime + 1
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
                    Vector2 projectileVector = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].Center + globalNPC.CurrentAttack.overshoot.Value, globalNPC.CurrentAttack.velocity, globalNPC.CurrentAttack.gravity);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center.X, npc.Center.Y, projectileVector.X, projectileVector.Y, globalNPC.CurrentAttack.type, globalNPC.CurrentAttack.damage, 0f, Main.myPlayer, globalNPC.CurrentAttack.ai0, globalNPC.CurrentAttack.ai1);
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

            public ProjectileData(int projectileType, int timerCap, int projectileDamage, float projectileVelocity, SoundStyle? shootSound = null, float projectileGravity = 0.035f, float ai0 = 0, float ai1 = 0, Vector2? overshoot = null, Color? telegraphColor = null, bool stopBeforeFiring = true, bool needsLineOfSight = true, float weight = 1, Func<NPC, bool> condition = null, float stopBeforeChance = 0.1f)
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
        public static Vector2? GenerateTeleportPosition(NPC npc, int range, bool requireLineofSight = true, bool preferHighGround = false)
        {
            int playerTileY = (int)(Main.player[npc.target].Center.Y / 16f);
            //Do not teleport if the player is way way too far away (stops enemies following you home if you mirror away)
            if (Math.Abs(npc.position.X - Main.player[npc.target].position.X) + Math.Abs(npc.position.Y - Main.player[npc.target].position.Y) > 2000f)
            { // far away from target; 2000 pixels = 125 blocks
                return null;
            }

            //Try 100 times at most
            for (int i = 0; i < 100; i++)
            {
                //Pick a random point to target. Make sure it's at least 11 blocks away from the player to avoid cheap hits.
                Vector2 teleportTarget = Vector2.Zero;
                if (range < 13)
                {
                    range = 13;
                }
                teleportTarget.X = Main.rand.Next(11, range);
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

            QueueTeleport(npc, 50, true, globalNPC.TeleportTelegraphTime, globalNPC.PrefersHighGround);

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

        public static void QueueTeleport(NPC npc, int range, bool requireLineofSight = true, int TeleportTelegraphTime = 140, bool preferHighGround = false)
        {
            Vector2? potentialNewPos;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 100; i++)
                {
                    potentialNewPos = GenerateTeleportPosition(npc, range, requireLineofSight, preferHighGround);
                    if (potentialNewPos.HasValue && (!requireLineofSight || (Collision.CanHit(potentialNewPos.Value, 1, 1, Main.player[npc.target].Center, 1, 1) && Collision.CanHitLine(potentialNewPos.Value, 1, 1, Main.player[npc.target].Center, 1, 1))))
                    {
                        npc.GetGlobalNPC<tsorcRevampGlobalNPC>().TeleportCountdown = TeleportTelegraphTime;
                        npc.GetGlobalNPC<tsorcRevampGlobalNPC>().TeleportTelegraph = potentialNewPos.Value;
                        SoundEngine.PlaySound(SoundID.Item8, npc.Center);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectileDirect(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.TeleportTelegraph>(), 0, 0, Main.myPlayer, npc.whoAmI, TeleportTelegraphTime);
                            Projectile.NewProjectileDirect(npc.GetSource_FromThis(), potentialNewPos.Value, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.TeleportTelegraph>(), 0, 0, Main.myPlayer, ai1: TeleportTelegraphTime);
                        }

                        break;
                    }
                }
            }
        }

        private static void SpawnTeleportMist(Vector2 position, Vector2 direction, int width, int height, tsorcRevampGlobalNPC globalNPC)
        {
            for (int i = 0; i < globalNPC.TeleportDustCount; i++)
            {
                Vector2 randomVelocity = direction * Main.rand.NextFloat(2.5f, 5.5f)
                    + Main.rand.NextVector2Circular(1.6f, 1.6f);
                Dust dust = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(width * 0.4f, height * 0.4f),
                    globalNPC.TeleportDustType, randomVelocity, 150, globalNPC.TeleportDustColor, globalNPC.TeleportDustScale);
                dust.noGravity = true;
                dust.fadeIn = 0.45f;
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

            SpawnTeleportMist(npc.Center, diff, npc.width, npc.height, globalNPC);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectileDirect(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 350, 20);
                Projectile.NewProjectileDirect(npc.GetSource_FromThis(), globalNPC.TeleportTelegraph, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 350, 20);
            }

            npc.Center = globalNPC.TeleportTelegraph;

            SpawnTeleportMist(npc.Center, -diff, npc.width, npc.height, globalNPC);
        }

        public static void FighterOnHit(NPC npc, bool melee)
        {
            if (melee)
            {
                npc.localAI[1] = 80f; // was 100
                npc.knockBackResist = 0.09f;
                // Abort any standing-fire burst — the NPC will be knocked airborne anyway
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().FighterRangedStandShotsRemaining = 0;

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
            if (!melee && Main.rand.NextBool())
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
        #region Red Knight Hit AI
        public static void RedKnightOnHit(NPC npc, bool melee) //ref int stunlockBreak
        {
            /*
            // Ensure that the stunlockBreak timer is always decreasing
            stunlockBreak--;

            // Increment the stunlockBreak timer
            stunlockBreak += 600;

            // Check if the stunlockBreak timer is greater than or equal to 3000
            if (stunlockBreak >= 2000)
            {
                
                // Set knockback to 0 and decrement the stunlockBreak timer
                npc.knockBackResist = 0;
                
            }
 
            if (stunlockBreak < 0)
            {
                stunlockBreak = 0;
            }
            */
            if (melee)
            {
                // Ensures melee can't interrupt attack once the flash telegraph triggers
                if ((npc.ai[1] < 155f) || (npc.ai[1] > 180f && npc.ai[1] < 300f) || (npc.ai[1] > 325f && npc.ai[1] < 900f) || npc.ai[1] > 925f)
                {
                    int randomChoice = Main.rand.Next(10);

                    switch (randomChoice)
                    {
                        case 0:
                            npc.ai[1] = 0f;
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
                            if (Main.rand.NextBool(4))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -9f;
                                npc.velocity.X = -9f * npc.direction;
                                npc.ai[1] = 140f;
                                npc.netUpdate = true;
                            }
                            else
                            {
                                npc.ai[1] = 0f;
                            }
                            break;
                        case 5:
                            // Small dash back - Bomb
                            if (Main.rand.NextBool(4))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -6f;
                                npc.velocity.X = -8f * npc.direction;
                                npc.ai[1] = 860f;
                                npc.netUpdate = true;


                            }
                            // Alt dash - Bomb
                            else if (Main.rand.NextBool(4))
                            {
                                npc.ai[1] = 850f;
                                npc.TargetClosest(true);
                                npc.velocity.Y = -4f;
                                npc.velocity.X = -9f * npc.direction;
                            }
                            break;
                        case 6:
                            // Big dash back - Bomb
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
                                npc.spriteDirection = npc.direction;
                                TeleportImmediately(npc, 22, true);
                                npc.netUpdate = true;
                            }
                            else if (Main.rand.NextBool(4))
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
                            if (Main.rand.NextBool(4))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -3f;
                                npc.velocity.X = -7f * npc.direction;
                                npc.ai[1] = 130f;
                                npc.netUpdate = true;
                            }
                            else if (Main.rand.NextBool(2))
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
                            if (Main.rand.NextBool(4))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -6f;
                                npc.velocity.X = -9f * npc.direction;
                                npc.ai[1] = 280f;
                                npc.netUpdate = true;
                            }
                            else if (Main.rand.NextBool(4))
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
                    //npc.knockBackResist = 0;
                }

                //npc.knockBackResist = 0.4f; //was 0.9            
            }

            if (!melee)
            {
                tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
                if (globalNPC.FighterRangedHitInterruptedPause || globalNPC.FighterPostAttackPauseTimer > 0 || globalNPC.FighterRangedStandShotsRemaining > 0)
                {
                    globalNPC.FighterRangedHitInterruptedPause = false;
                    globalNPC.FighterPostAttackPauseTimer = 0;
                    globalNPC.FighterRangedStandShotsRemaining = 0;
                    npc.TargetClosest(true);
                    float distance = npc.Distance(Main.player[npc.target].Center);
                    if (distance > 320f)
                    {
                        npc.ai[1] = Main.rand.NextBool() ? 90f : 830f;
                    }
                    else
                    {
                        npc.velocity.Y = -5f;
                        npc.velocity.X += npc.direction * 5f;
                    }
                    npc.netUpdate = true;
                    return;
                }

                // Ensures ranged can't interrupt attack once the flash telegraph triggers
                if ((npc.ai[1] < 155f) || (npc.ai[1] > 180f && npc.ai[1] < 300f) || (npc.ai[1] > 325f && npc.ai[1] < 900f) || npc.ai[1] > 925f)
                {
                    int randomChoice = Main.rand.Next(9);

                    switch (randomChoice)
                    {
                        case 0:
                            // Burst forward
                            if (Main.rand.NextBool(5))
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
                            if (npc.Distance(Main.player[npc.target].Center) > 400 && Main.rand.NextBool(4))
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
                            if (Main.rand.NextBool(4))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -6f;
                                npc.velocity.X = 6f * npc.direction;
                                npc.ai[1] = 860f;
                                npc.netUpdate = true;
                            }
                            // Alt dash backwards - Bomb
                            if (Main.rand.NextBool(4))
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
                            if (Main.rand.NextBool(4))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -3f;
                                npc.velocity.X = -7f * npc.direction;
                                npc.ai[1] = 140f;
                                npc.netUpdate = true;
                            }
                            else if (Main.rand.NextBool(4))
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
