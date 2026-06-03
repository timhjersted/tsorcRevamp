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
        public static void FighterAI(NPC npc, float topSpeed = 1f, float acceleration = .07f, float brakingPower = .2f, bool canTeleport = false, int doorBreakingDamage = 4, bool hatesLight = false, SoundStyle? randomSound = null, int soundFrequency = 1000, float enragePercent = 0, float enrageTopSpeed = 0, bool lavaJumping = false, bool canDodgeroll = true, bool canPounce = true)
        {
            npc.aiStyle = -1;
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
                // Nav-tiered recovery states (waypoints / ledge run-up) need to keep counting
                // so the shaman can finish a pathing escape and still reach the telegraph window.
                bool inTelegraphWindow = globalNPC.ProjectileTimer <= (projectileCooldown / 2 + 15) && globalNPC.ProjectileTimer > (projectileCooldown / 2);
                bool pathRecoveryActive = globalNPC.NavigationTier >= 1 && (globalNPC.WaypointTimer > 0 || globalNPC.LedgeRunUpTimer > 0);
                if (npc.justHit || (npc.velocity.Y != 0f && !inTelegraphWindow && !pathRecoveryActive) || globalNPC.ProjectileTimer <= 0f)
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
                    //If so, set boredom to 0
                    globalNPC.BoredTimer = 0;

                    //If it's not aiming yet, then slow down, aim, and start its cooldown
                    if (globalNPC.ArcherAimDirection == 0)
                    {
                        //Aim at them, and start the shot cooldown
                        npc.velocity.X *= 0.5f;
                        globalNPC.ArcherAimDirection = 3f;
                        globalNPC.ProjectileTimer = (int)(projectileCooldown * globalNPC.CastingSpeed);

                        // Standing-fire roll: tier-2 NPCs may plant their feet and fire N shots
                        // before resuming pursuit. High Aggression skips this; high Patience adds shots.
                        if (globalNPC.CanStopToFire && globalNPC.NavigationTier >= 2 && globalNPC.FighterRangedStandShotsRemaining == 0
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
            if (globalNPC.WaypointSearchCooldown > 0)
            {
                globalNPC.WaypointSearchCooldown--;
            }
            if (globalNPC.NavBlockedDirectionTimer > 0)
            {
                globalNPC.NavBlockedDirectionTimer--;
                if (globalNPC.NavBlockedDirectionTimer == 0)
                {
                    globalNPC.NavBlockedDirection = 0;
                }
            }
            if (globalNPC.NavExploreTimer > 0)
            {
                globalNPC.NavExploreTimer--;
                if (globalNPC.NavExploreTimer == 0)
                {
                    globalNPC.NavExploreDirection = 0;
                }
            }
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

                globalNPC.Initialized = true;
            }

            // WeakTeleport bored walk — if the NPC gave up pursuing the player, briefly
            // disengage, pause, then hand control back to normal pursuit.
            // Teleport charges are intentionally not restored here; WeakTeleport is a
            // strict two-use-per-NPC fallback, not a pursuit-cycle resource.
            if (globalNPC.WeakTeleport && globalNPC.WeakTeleportBoredPhase > 0)
            {
                if (npc.justHit)
                {
                    // Player found us during our break — resume normal pursuit
                    globalNPC.WeakTeleportBoredPhase = 0;
                    globalNPC.WeakTeleportReachTimer = 0;
                    globalNPC.WeakTeleportCooldown = 0;
                }
                else
                {
                    npc.TargetClosest(false);
                    globalNPC.WeakTeleportBoredTimer--;

                    switch (globalNPC.WeakTeleportBoredPhase)
                    {
                        case 1: // Stand still (2 seconds = 120 frames)
                            npc.velocity.X *= 0.85f;
                            if (globalNPC.WeakTeleportBoredTimer <= 0)
                            {
                                globalNPC.WeakTeleportBoredPhase = 2;
                                globalNPC.WeakTeleportBoredTimer = 300; // walk away 5 s
                                npc.direction = Main.player[npc.target].Center.X < npc.Center.X ? 1 : -1;
                                npc.spriteDirection = npc.direction;
                            }
                            break;

                        case 2: // Walk away from the player (5 seconds = 300 frames)
                            if (npc.velocity.X < topSpeed && npc.direction == 1) npc.velocity.X += acceleration;
                            else if (npc.velocity.X > -topSpeed && npc.direction == -1) npc.velocity.X -= acceleration;
                            if (globalNPC.WeakTeleportBoredTimer <= 0)
                            {
                                globalNPC.WeakTeleportBoredPhase = 3;
                                globalNPC.WeakTeleportBoredTimer = 120; // pause 2 s
                            }
                            break;

                        case 3: // Pause (2 seconds = 120 frames)
                            npc.velocity.X *= 0.85f;
                            if (globalNPC.WeakTeleportBoredTimer <= 0)
                            {
                                globalNPC.WeakTeleportBoredPhase = 4;
                                globalNPC.WeakTeleportBoredTimer = 120; // walk back 2 s
                                npc.direction *= -1; // turn around (toward player)
                                npc.spriteDirection = npc.direction;
                            }
                            break;

                        case 4: // Walk back toward the player briefly (2 seconds = 120 frames)
                            if (npc.velocity.X < topSpeed && npc.direction == 1) npc.velocity.X += acceleration;
                            else if (npc.velocity.X > -topSpeed && npc.direction == -1) npc.velocity.X -= acceleration;
                            if (globalNPC.WeakTeleportBoredTimer <= 0)
                            {
                                // Resume pursuit without restoring spent weak teleport charges.
                                globalNPC.WeakTeleportBoredPhase = 0;
                                globalNPC.WeakTeleportReachTimer = 0;
                                globalNPC.WeakTeleportCooldown = 0;
                            }
                            break;
                    }
                    return; // skip all normal movement, attacking, and boredom tracking
                }
            }

            bool earlyLineOfSight = Main.player[npc.target].CanHit(npc);
            bool earlyDifferentFloor = earlyLineOfSight && Math.Abs(Main.player[npc.target].Center.Y - npc.Center.Y) > 48f;
            bool shouldRequestWaypoint = globalNPC.NavigationTier >= 1
                && globalNPC.WaypointTimer == 0
                && globalNPC.WaypointSearchCooldown == 0
                && globalNPC.TeleportCountdown == 0
                && globalNPC.DodgeTimer == 0
                && globalNPC.PounceTimer == 0
                && (!earlyLineOfSight || earlyDifferentFloor || globalNPC.BoredTimer > 0 || globalNPC.StuckTimer >= 12);

            if (shouldRequestWaypoint)
            {
                globalNPC.LastNavIntent = !earlyLineOfSight ? "early:no-los"
                    : earlyDifferentFloor ? "early:different-floor"
                    : globalNPC.BoredTimer > 0 ? "early:bored"
                    : "early:stuck";
                bool forceWaypoint = globalNPC.BoredTimer >= globalNPC.BoredomThreshold || globalNPC.StuckTimer >= 20;
                TrySetFighterWaypoint(npc, globalNPC, forceWaypoint);
            }

            //If it has at least one attack, perform it
            if (globalNPC.AttackList.Count > 0)
            {
                bool crossableGapTowardPlayer = HasCrossableGapTowardPlayer(npc, globalNPC, out int gapTravelDirection);
                if (crossableGapTowardPlayer)
                {
                    npc.direction = gapTravelDirection;
                    npc.spriteDirection = gapTravelDirection;
                    globalNPC.FighterRangedStandShotsRemaining = 0;
                    if (globalNPC.CurrentAttack.needsLineOfSight)
                    {
                        globalNPC.ProjectileTimer = 0f;
                    }
                }
                float committedAttackLeadTime = globalNPC.CurrentAttack.type == ModContent.ProjectileType<Projectiles.Enemy.EnemySpellPoisonStormBall>()
                    ? 90f
                    : ProjectileTelegraphTime;
                bool inCommittedAttack = globalNPC.ProjectileTimer > globalNPC.CurrentAttack.timerCap - committedAttackLeadTime;
                bool navigationNeedsControl = globalNPC.NavigationTier >= 1
                    && globalNPC.CurrentAttack.needsLineOfSight
                    && !inCommittedAttack
                    && (globalNPC.WaypointTimer > 0 || globalNPC.LedgeRunUpTimer > 0 || globalNPC.LedgeVaultTimer > 0 || globalNPC.StuckTimer >= 8);
                if (!crossableGapTowardPlayer && !navigationNeedsControl)
                {
                    SimpleProjectile(npc);
                }
                else if (navigationNeedsControl)
                {
                    globalNPC.ProjectileTimer = 0f;
                    globalNPC.FighterRangedStandShotsRemaining = 0;
                    globalNPC.ArcherAimDirection = 0f;
                }
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

            //Stop moving when teleporting, and handle the logic to execute it
            if (globalNPC.TeleportCountdown > 0)
            {
                globalNPC.BoredTimer = 0;
                npc.velocity.X = 0;
                globalNPC.TeleportCountdown--;
                if (globalNPC.TeleportCountdown == 0)
                {
                    ExecuteQueuedTeleport(npc);
                }
            }

            //Block firing and reset cooldowns if it's busy doing other things
            if (globalNPC.TeleportCountdown > 0 || globalNPC.BoredTimer < 0 || globalNPC.DodgeTimer > 0 || globalNPC.PounceTimer > 0)
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

            //If just hit, then it's not bored
            if (npc.justHit)
            {
                globalNPC.BoredTimer = 0;
                if (globalNPC.WeakTeleport)
                {
                    // Being hit also resets the reach timer so the NPC doesn't give up immediately
                    // when the player finds it before it found LOS.
                    globalNPC.WeakTeleportReachTimer = 0;
                }
            }

            //If fleeing, despawn as soon as it's offscreen (via timeLeft running out)
            if (globalNPC.Fleeing || (hatesLight && Main.dayTime && (npc.position.Y / 16f) < Main.worldSurface))
            {
                globalNPC.BoredTimer = -999;
                npc.timeLeft = 10;
            }

            //If bored, target the closest player it has line of sight to. If it doesn't have los to any, just target the closest one.
            if (globalNPC.BoredTimer != 0)
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

            // Compute line of sight early so waypoint cancel logic can use it before movement
            bool lineOfSight = Main.player[npc.target].CanHit(npc);
            bool playerOnDirectEngageFloor = lineOfSight && Math.Abs(npc.Center.Y - Main.player[npc.target].Center.Y) < 32f;
            if (playerOnDirectEngageFloor)
            {
                ClearFighterWaypoint(globalNPC);
                globalNPC.NavExploreTimer = 0;
                globalNPC.NavExploreDirection = 0;
                globalNPC.NavBlockedDirection = 0;
                globalNPC.NavBlockedDirectionTimer = 0;
                globalNPC.LedgeRunUpTimer = 0;
                globalNPC.LedgeRunUpDirection = 0;
            }

            // WeakTeleport: limited-use gap-closing teleport for non-teleporter enemies.
            // Up to 2 total charges for this NPC, 10-second cooldown between each, minimum 40-tile range.
            if (globalNPC.WeakTeleport)
            {
                if (globalNPC.WeakTeleportCooldown > 0)
                    globalNPC.WeakTeleportCooldown--;

                if (!lineOfSight &&
                    globalNPC.WeakTeleportUses > 0 &&
                    globalNPC.WeakTeleportCooldown == 0 &&
                    globalNPC.TeleportCountdown == 0 &&
                    globalNPC.WeakTeleportBoredPhase == 0 &&
                    npc.Distance(Main.player[npc.target].Center) > 640f && // 40 tiles minimum
                    Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 dest = FindWeakTeleportDestination(npc, Main.player[npc.target]);
                    if (dest != Vector2.Zero)
                    {
                        int telegraphTime = globalNPC.TeleportTelegraphTime;
                        globalNPC.TeleportCountdown = telegraphTime;
                        globalNPC.TeleportTelegraph = dest;
                        npc.velocity = Vector2.Zero;
                        globalNPC.WeakTeleportUses--;
                        globalNPC.WeakTeleportCooldown = 600; // 10 seconds
                        globalNPC.BoredTimer = 0;
                        globalNPC.WaypointTimer = 0;
                        globalNPC.WaypointAction = tsorcRevampGlobalNPC.NavActionType.None;
                        globalNPC.WaypointNoProgressTimer = 0;
                        globalNPC.LastWaypointDistance = 0f;
                        globalNPC.NavRouteIndex = 0;
                        globalNPC.NavRouteCount = 0;
                        globalNPC.LedgeRunUpTimer = 0;
                        globalNPC.LedgeRunUpDirection = 0;
                        globalNPC.WeakTeleportReachTimer = 0;
                        npc.netUpdate = true;

                        SoundEngine.PlaySound(SoundID.Item8, npc.Center);
                        Projectile.NewProjectileDirect(npc.GetSource_FromThis(), npc.Center, Vector2.Zero,
                            ModContent.ProjectileType<Projectiles.VFX.TeleportTelegraph>(), 0, 0, Main.myPlayer,
                            npc.whoAmI, telegraphTime);
                        Projectile.NewProjectileDirect(npc.GetSource_FromThis(), dest, Vector2.Zero,
                            ModContent.ProjectileType<Projectiles.VFX.TeleportTelegraph>(), 0, 0, Main.myPlayer,
                            ai1: telegraphTime);
                    }
                }
            }

            // Waypoint navigation: tick down and cancel if LOS restored or destination reached
            if (globalNPC.WaypointTimer > 0)
            {
                globalNPC.WaypointTimer--;
                if (globalNPC.WaypointTimer <= 0)
                {
                    globalNPC.WaypointTarget = Vector2.Zero;
                    globalNPC.WaypointAction = tsorcRevampGlobalNPC.NavActionType.None;
                    globalNPC.WaypointNoProgressTimer = 0;
                    globalNPC.LastWaypointDistance = 0f;
                }
                // Drop waypoints (BFS found a platform-drop path) have same X as the NPC;
                // don't cancel them on X proximity — let Y proximity signal completion instead.
                bool isDropWaypoint = globalNPC.WaypointTarget.Y > npc.Center.Y + 48f
                    && Math.Abs(globalNPC.WaypointTarget.X - npc.Center.X) < 32f;
                bool reachedX = Math.Abs(npc.Center.X - globalNPC.WaypointTarget.X) < 8f;
                bool reachedJumpTarget = Math.Abs(npc.Center.X - globalNPC.WaypointTarget.X) < 24f
                    && Math.Abs(npc.Center.Y - globalNPC.WaypointTarget.Y) < 40f;
                bool droppedToTarget = isDropWaypoint
                    && Math.Abs(npc.Center.Y - globalNPC.WaypointTarget.Y) < 32f;

                // Only cancel on LOS when the player is at roughly the same elevation.
                // If they're on a different floor (e.g. above a platform ceiling), the NPC has
                // LOS through the platform but can't actually engage — keep the waypoint so it
                // continues navigating to a staircase/ledge rather than spinning back to centre.
                // 32px (~2 tiles) — only cancel the waypoint when the player is truly on the
                // same floor and directly engageable.  64 was too wide: the different-floor
                // BFS would set a waypoint only for cancelOnLos to destroy it one tick later.
                bool playerOnSameFloor = Math.Abs(npc.Center.Y - Main.player[npc.target].Center.Y) < 32f;
                bool cancelOnLos = lineOfSight && playerOnSameFloor;

                bool reachedWaypoint = globalNPC.WaypointAction == tsorcRevampGlobalNPC.NavActionType.JumpTo
                    ? reachedJumpTarget
                    : reachedX && !isDropWaypoint;

                float waypointDistance = npc.Distance(globalNPC.WaypointTarget);
                if (globalNPC.LastWaypointDistance <= 0f || waypointDistance < globalNPC.LastWaypointDistance - 4f)
                {
                    globalNPC.LastWaypointDistance = waypointDistance;
                    globalNPC.WaypointNoProgressTimer = 0;
                }
                else
                {
                    globalNPC.WaypointNoProgressTimer++;
                }

                bool waypointStalled = globalNPC.WaypointNoProgressTimer > 90;

                if (cancelOnLos || reachedWaypoint || droppedToTarget || waypointStalled)
                {
                    bool routeStepComplete = !cancelOnLos && !waypointStalled && (reachedWaypoint || droppedToTarget)
                        && globalNPC.NavRouteCount > 0
                        && globalNPC.NavRouteIndex + 1 < globalNPC.NavRouteCount;
                    if (routeStepComplete)
                    {
                        globalNPC.NavRouteIndex++;
                        globalNPC.NavRouteTimer = 0;
                        globalNPC.NavRouteNoProgressTimer = 0;
                        globalNPC.WaypointTarget = globalNPC.NavRouteTargets[globalNPC.NavRouteIndex];
                        globalNPC.WaypointAction = globalNPC.NavRouteActions[globalNPC.NavRouteIndex];
                        globalNPC.WaypointTimer = 420;
                        globalNPC.WaypointNoProgressTimer = 0;
                        globalNPC.LastWaypointDistance = npc.Distance(globalNPC.WaypointTarget);
                        globalNPC.LastNavRouteDistance = globalNPC.LastWaypointDistance;
                        globalNPC.LastNavIntent = "route:advance";
                        globalNPC.LastWaypointResult = $"route:{globalNPC.NavRouteIndex + 1}/{globalNPC.NavRouteCount} {globalNPC.WaypointAction}";
                        npc.netUpdate = true;
                        goto afterWaypointState;
                    }

                    Vector2 stalledWaypoint = globalNPC.WaypointTarget;
                    globalNPC.WaypointTimer = 0;
                    globalNPC.WaypointTarget = Vector2.Zero;
                    globalNPC.WaypointAction = tsorcRevampGlobalNPC.NavActionType.None;
                    globalNPC.WaypointNoProgressTimer = 0;
                    globalNPC.LastWaypointDistance = 0f;
                    globalNPC.NavRouteIndex = 0;
                    globalNPC.NavRouteCount = 0;
                    globalNPC.NavRouteTimer = 0;
                    globalNPC.NavRouteNoProgressTimer = 0;
                    globalNPC.LastNavRouteDistance = 0f;
                    if (waypointStalled)
                    {
                        globalNPC.WaypointSearchCooldown = Math.Max(globalNPC.WaypointSearchCooldown, 30);
                        globalNPC.LastWaypointResult = "fail:waypoint-stalled";
                        int stalledDirection = Math.Abs(stalledWaypoint.X - npc.Center.X) < 8f
                            ? (npc.direction == 0 ? Math.Sign(Main.player[npc.target].Center.X - npc.Center.X) : npc.direction)
                            : Math.Sign(stalledWaypoint.X - npc.Center.X);
                        MarkNavDirectionBlocked(globalNPC, stalledDirection, 180);
                        StartNavExplore(npc, globalNPC, -stalledDirection, 180);
                    }

                    // Immediately chain to the next BFS step when a waypoint is completed
                    // but the player is still not reachable at the same floor level.
                    // Without this there is a ~2 s gap (bfsFallback interval) during which the
                    // NPC reverts to "face player center X" and walks the wrong way.
                    // Chain to the next BFS step whenever a waypoint is completed/reached but
                    // the player is still not on the same floor.  Don't require BoredTimer > 20
                    // here — the hard BoredTimer reset (LOS + close Y) keeps it at 0 for
                    // different-floor cases, so the old guard would silently skip chaining.
                    if (!cancelOnLos && !waypointStalled && globalNPC.NavigationTier >= 1)
                    {
                        globalNPC.LastNavIntent = "waypoint:chain";
                        TrySetFighterWaypoint(npc, globalNPC, true);
                    }
                }
                afterWaypointState: ;
            }

            // Face the active waypoint first. Player-facing has a dead zone to avoid jitter
            // when directly underneath the player, but waypoint steering must not inherit it.
            if (globalNPC.WaypointTimer > 0)
            {
                float waypointDeltaX = globalNPC.WaypointTarget.X - npc.Center.X;
                if (Math.Abs(waypointDeltaX) > 4f)
                {
                    npc.direction = waypointDeltaX < 0f ? -1 : 1;
                    npc.spriteDirection = npc.direction;
                }
            }
            else if (!playerOnDirectEngageFloor && globalNPC.NavExploreTimer > 0 && globalNPC.NavExploreDirection != 0)
            {
                npc.direction = globalNPC.NavExploreDirection;
                npc.spriteDirection = npc.direction;
            }
            else if (Math.Abs(Main.player[npc.target].Center.X - npc.Center.X) > 30)
            {
                int desiredDirection;
                if (Main.player[npc.target].Center.X <= npc.Center.X)
                {
                    desiredDirection = -1;
                }
                else
                {
                    desiredDirection = 1;
                }
                if (!playerOnDirectEngageFloor && globalNPC.NavBlockedDirectionTimer > 0 && desiredDirection == globalNPC.NavBlockedDirection)
                {
                    desiredDirection *= -1;
                    globalNPC.LastNavIntent = "avoid:blocked-direction";
                }
                npc.direction = desiredDirection;
                if (globalNPC.BoredTimer < 0)
                {
                    npc.direction *= -1;
                }
                npc.spriteDirection = npc.direction;
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
            if (globalNPC.CanStopToFire && !globalNPC.CanPassThroughWalls && globalNPC.NavigationTier >= 2 && (globalNPC.FighterPostAttackPauseTimer > 0 || globalNPC.FighterRangedStandShotsRemaining > 0) && lineOfSight && npc.velocity.Y == 0f && !globalNPC.Fleeing)
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
            bool navActionHandledJump = false;
            if (globalNPC.NavJumpCooldown > 0)
            {
                globalNPC.NavJumpCooldown--;
            }

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

            // NavigationTier 0 compatibility path: preserve the original FighterAI terrain
            // behavior exactly, with no waypoint/BFS/ledge-run-up leakage.
            if (globalNPC.NavigationTier < 1 && standing_on_solid_tile)
            {
                if ((npc.velocity.X < 0f && npc.spriteDirection == -1) || (npc.velocity.X > 0f && npc.spriteDirection == 1))
                {
                    if (UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 2))
                    {
                        if (UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 3))
                        {
                            npc.velocity.Y = -8f;
                            npc.netUpdate = true;
                        }
                        else
                        {
                            npc.velocity.Y = -7f;
                            npc.netUpdate = true;
                        }
                    }
                    else if (UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 1))
                    {
                        npc.velocity.Y = -6f;
                        npc.netUpdate = true;
                    }
                    else if (UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet))
                    {
                        npc.velocity.Y = -5f;
                        npc.netUpdate = true;
                    }
                    else if (npc.directionY < 0 && !UsefulFunctions.IsTileReallySolid(x_in_front, y_below_feet) && !UsefulFunctions.IsTileReallySolid(x_in_front + npc.direction, y_below_feet))
                    {
                        npc.velocity.Y = -8f;
                        npc.velocity.X += 4f * npc.direction;
                        npc.netUpdate = true;
                    }

                    if (UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 1) && Main.tile[x_in_front, y_above_feet - 1].TileType == 10 && (doorBreakingDamage > 0))
                    {
                        npc.velocity.Y = 0;
                        globalNPC.BoredTimer = 0;
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
                                    globalNPC.BoredTimer = 999;
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

            if (standing_on_solid_tile && globalNPC.NavigationTier >= 1 && globalNPC.NavJumpCooldown == 0
                && HasCrossableGapTowardPlayer(npc, globalNPC, out int immediateGapDirection))
            {
                float jumpPower = MathHelper.Clamp(globalNPC.MaxJumpPower * 0.9f, 6.5f, globalNPC.MaxJumpPower);
                float gapBoost = MathHelper.Clamp(Math.Max(topSpeed * 1.8f, globalNPC.MaxJumpBoost * 0.6f), 2.25f, Math.Max(3f, globalNPC.MaxJumpBoost));
                npc.direction = immediateGapDirection;
                npc.spriteDirection = immediateGapDirection;
                npc.velocity.X = immediateGapDirection * gapBoost;
                npc.velocity.Y = -jumpPower;
                globalNPC.StuckTimer = 0;
                globalNPC.LedgeRunUpTimer = 0;
                globalNPC.LedgeRunUpDirection = 0;
                globalNPC.NavJumpCooldown = 18;
                globalNPC.FighterRangedStandShotsRemaining = 0;
                globalNPC.ProjectileTimer = 0f;
                npc.netUpdate = true;
                navActionHandledJump = true;
            }

            bool playerDirectlyEngageableForJumping = lineOfSight && Math.Abs(Main.player[npc.target].Center.Y - npc.Center.Y) < 32f;

            if (!playerDirectlyEngageableForJumping && globalNPC.NavRouteCount == 0 && standing_on_solid_tile && !navActionHandledJump && globalNPC.NavigationTier >= 1
                && globalNPC.NavJumpCooldown == 0 && globalNPC.StuckTimer >= 8
                && Math.Abs(Main.player[npc.target].Center.X - npc.Center.X) > 32f)
            {
                int blockedDirection = npc.direction == 0
                    ? Math.Sign(Main.player[npc.target].Center.X - npc.Center.X)
                    : npc.direction;
                if (blockedDirection == 0)
                {
                    blockedDirection = 1;
                }

                MarkNavDirectionBlocked(globalNPC, blockedDirection, 210);
                StartNavExplore(npc, globalNPC, -blockedDirection, 150);
                npc.velocity.X = globalNPC.NavExploreDirection * Math.Max(topSpeed * 0.9f, 1.1f);
                globalNPC.StuckTimer = 0;
                globalNPC.LedgeRunUpTimer = 0;
                globalNPC.LedgeRunUpDirection = 0;
                globalNPC.LedgeVaultTimer = 0;
                globalNPC.LedgeVaultDirection = 0;
                globalNPC.NavJumpCooldown = 10;
                ClearFighterWaypoint(globalNPC);
                globalNPC.LastNavIntent = "local:block-memory";
                globalNPC.LastWaypointResult = "local:explore-away";
                npc.netUpdate = true;
                navActionHandledJump = true;
            }

            if (!standing_on_solid_tile && globalNPC.NavigationTier >= 1 && globalNPC.LedgeVaultTimer > 0)
            {
                int vaultDirection = globalNPC.LedgeVaultDirection == 0 ? npc.direction : globalNPC.LedgeVaultDirection;
                globalNPC.LedgeVaultTimer--;
                npc.direction = vaultDirection;
                npc.spriteDirection = vaultDirection;

                int vaultElapsed = 30 - globalNPC.LedgeVaultTimer;
                if (vaultElapsed < 7 && npc.velocity.Y < -1f)
                {
                    npc.velocity.X *= 0.65f;
                }
                else
                {
                    float vaultBoost = MathHelper.Clamp(Math.Max(topSpeed * 1.25f, globalNPC.MaxJumpBoost * 0.3f), 0.85f, 1.65f);
                    npc.velocity.X = vaultDirection * vaultBoost;
                }

                if (globalNPC.LedgeVaultTimer == 0 || npc.velocity.Y >= 0f)
                {
                    globalNPC.LedgeVaultTimer = 0;
                    globalNPC.LedgeVaultDirection = 0;
                }
            }

            if (standing_on_solid_tile && !navActionHandledJump && globalNPC.NavigationTier >= 1 && globalNPC.WaypointTimer > 0)
            {
                float waypointDeltaX = globalNPC.WaypointTarget.X - npc.Center.X;
                int waypointDir = Math.Abs(waypointDeltaX) < 4f
                    ? npc.direction
                    : Math.Sign(waypointDeltaX);

                if (globalNPC.WaypointAction == tsorcRevampGlobalNPC.NavActionType.JumpTo && globalNPC.NavJumpCooldown == 0)
                {
                    float waypointDeltaY = globalNPC.WaypointTarget.Y - npc.Center.Y;
                    bool tinyHeightJump = waypointDeltaY > -24f && Math.Abs(waypointDeltaX) < 96f;
                    if (waypointDeltaY > 8f || tinyHeightJump)
                    {
                        // Defensive guard: lower/small-height waypoints are walk/drop steering,
                        // not jump commands. Local gap logic handles true gap jumps separately.
                        npc.direction = waypointDir == 0 ? npc.direction : waypointDir;
                        npc.spriteDirection = npc.direction;
                        if (Math.Abs(npc.velocity.X) < topSpeed * 0.75f)
                        {
                            npc.velocity.X = npc.direction * topSpeed * 0.75f;
                        }
                        globalNPC.WaypointAction = tsorcRevampGlobalNPC.NavActionType.Walk;
                    }
                    else
                    {
                        float upwardTiles = Math.Max(0f, -waypointDeltaY / 16f);
                        float jumpPower = MathHelper.Clamp(4.8f + upwardTiles * 1.35f, 5.5f, Math.Max(globalNPC.MaxJumpPower, 8f));
                        bool mostlyVerticalJump = Math.Abs(waypointDeltaX) < 18f && waypointDeltaY < -24f;
                        float horizontalBoost = mostlyVerticalJump
                            ? 0f
                            : MathHelper.Clamp(Math.Abs(waypointDeltaX) / 24f, 0.75f, Math.Max(globalNPC.MaxJumpBoost, 2f));
                        if (mostlyVerticalJump)
                        {
                            waypointDir = Main.player[npc.target].Center.X < npc.Center.X ? -1 : 1;
                        }
                        npc.direction = waypointDir == 0 ? npc.direction : waypointDir;
                        npc.spriteDirection = npc.direction;
                        npc.velocity.X = npc.direction * horizontalBoost;
                        npc.velocity.Y = -jumpPower;
                        globalNPC.StuckTimer = 0;
                        globalNPC.LedgeRunUpTimer = 0;
                        globalNPC.LedgeRunUpDirection = 0;
                        if (mostlyVerticalJump)
                        {
                            globalNPC.LedgeVaultTimer = 26;
                            globalNPC.LedgeVaultDirection = npc.direction;
                        }
                        globalNPC.NavJumpCooldown = 24;
                        npc.netUpdate = true;
                        navActionHandledJump = true;
                    }
                }

                if (globalNPC.WaypointAction == tsorcRevampGlobalNPC.NavActionType.Drop || globalNPC.WaypointAction == tsorcRevampGlobalNPC.NavActionType.DropThroughPlatform)
                {
                    npc.direction = waypointDir == 0 ? npc.direction : waypointDir;
                    npc.spriteDirection = npc.direction;
                    if (Math.Abs(npc.velocity.X) < topSpeed * 0.75f)
                    {
                        npc.velocity.X = npc.direction * topSpeed * 0.75f;
                    }
                }
            }

            //If standing on solid tile
            if (standing_on_solid_tile && !navActionHandledJump && globalNPC.NavigationTier >= 1)
            {
                //Moving forward, or blocked and ready to let tiered navigation plan an escape.
                if (npc.velocity.X * npc.direction > 0f || (globalNPC.NavigationTier >= 1 && globalNPC.StuckTimer >= 3))
                {
                    // Jump power scaled by per-enemy MaxJumpPower (NavigationTier >= 1) or vanilla 8f
                    float jumpPower = globalNPC.NavigationTier >= 1 ? globalNPC.MaxJumpPower : 8f;

                    // ── Ledge run-up ──────────────────────────────────────────────
                    // When StuckTimer first reaches a low threshold (NPC has been stopped by the
                    // same obstacle for ~8 frames), initiate a back-up: reverse velocity
                    // until geometry shows usable headroom, then fire a ledge-clear jump.
                    // This solves the "stuck in a pit
                    // with a 1-tile ledge" case where the NPC is pressed too close to
                    // the wall to clear the ledge corner with a vertical-only jump.
                    int leftFrontX = (int)(npc.position.X / 16f) - 1;
                    int rightFrontX = (int)((npc.position.X + npc.width) / 16f);
                    bool obstacleLeft =
                        UsefulFunctions.IsTileReallySolid(leftFrontX, y_above_feet    ) ||
                        UsefulFunctions.IsTileReallySolid(leftFrontX, y_above_feet - 1) ||
                        UsefulFunctions.IsTileReallySolid(leftFrontX, y_above_feet - 2);
                    bool obstacleRight =
                        UsefulFunctions.IsTileReallySolid(rightFrontX, y_above_feet    ) ||
                        UsefulFunctions.IsTileReallySolid(rightFrontX, y_above_feet - 1) ||
                        UsefulFunctions.IsTileReallySolid(rightFrontX, y_above_feet - 2);
                    bool anyObstacleAhead = npc.direction == -1 ? obstacleLeft : obstacleRight;

                    // Tiered navigation should commit to a ledge escape quickly instead of
                    // spending several frames doing local wall jumps under the overhang.
                    if (!playerDirectlyEngageableForJumping && globalNPC.NavRouteCount == 0 && globalNPC.NavigationTier >= 1 && globalNPC.StuckTimer >= 6
                        && (anyObstacleAhead || obstacleLeft || obstacleRight) && globalNPC.LedgeRunUpTimer == 0)
                    {
                        int playerDirection = Main.player[npc.target].Center.X < npc.Center.X ? -1 : 1;
                        if (!anyObstacleAhead)
                        {
                            if (playerDirection == -1 && obstacleLeft)
                            {
                                npc.direction = -1;
                            }
                            else if (playerDirection == 1 && obstacleRight)
                            {
                                npc.direction = 1;
                            }
                            else if (obstacleLeft)
                            {
                                npc.direction = -1;
                            }
                            else if (obstacleRight)
                            {
                                npc.direction = 1;
                            }
                            npc.spriteDirection = npc.direction;
                        }

                        globalNPC.LedgeRunUpTimer = 18;
                        globalNPC.LedgeRunUpDirection = npc.direction == 0 ? 1 : npc.direction;
                        MarkNavDirectionBlocked(globalNPC, globalNPC.LedgeRunUpDirection, 150);
                    }

                    if (globalNPC.LedgeRunUpTimer > 0)
                    {
                        int ledgeDirection = globalNPC.LedgeRunUpDirection == 0 ? npc.direction : globalNPC.LedgeRunUpDirection;
                        if (ledgeDirection == 0)
                        {
                            ledgeDirection = 1;
                        }

                        if (globalNPC.LedgeRunUpTimer > 1)
                        {
                            int backoffDirection = -ledgeDirection;
                            npc.direction = backoffDirection;
                            npc.spriteDirection = backoffDirection;
                            npc.velocity.X = backoffDirection * Math.Max(topSpeed * 0.85f, 1.1f);
                            globalNPC.LedgeRunUpTimer--;
                            globalNPC.LastNavIntent = "ledge:backoff";
                            globalNPC.LastWaypointResult = "ledge:building-clearance";
                            goto skipNormalJumps;
                        }

                        npc.direction = ledgeDirection;
                        npc.spriteDirection = ledgeDirection;
                        if (globalNPC.NavJumpCooldown == 0)
                        {
                            // Player-like ledge vault: go mostly straight up first, then drift
                            // toward the ledge after the head has time to clear the overhang.
                            npc.velocity.X = 0f;
                            npc.velocity.Y = -(jumpPower * 1.08f);
                            globalNPC.StuckTimer = 0;
                            globalNPC.LedgeRunUpTimer = 0;
                            globalNPC.LedgeRunUpDirection = 0;
                            globalNPC.LedgeVaultTimer = 30;
                            globalNPC.LedgeVaultDirection = ledgeDirection;
                            globalNPC.NavJumpCooldown = 18;
                            globalNPC.LastNavIntent = "ledge:vault";
                            globalNPC.LastWaypointResult = "ledge:jump-after-backoff";
                            npc.netUpdate = true;
                        }
                        else
                        {
                            npc.velocity.X = 0f;
                            globalNPC.LedgeRunUpTimer = 1;
                        }
                        // Skip normal jump logic while the run-up is active
                        goto skipNormalJumps;
                    }

                    // Smart navigation enemies should avoid repeated desperation wall-jumps.
                    // Let BFS / ledge-run-up choose the escape instead of bouncing in place.
                    bool mayJump = globalNPC.NavigationTier < 1;
                    bool mayStepUpOneTile = globalNPC.NavigationTier >= 1
                        && globalNPC.WaypointTimer == 0
                        && globalNPC.StuckTimer < 6
                        && UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet)
                        && !UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 1)
                        && !UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 2);

                    //3 blocks above ground level (head height) blocked
                    if (mayJump && UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 2))
                    {
                        //4 blocks above ground level (over head) blocked
                        if (UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 3))
                        {
                            npc.velocity.Y = -jumpPower; //Jump with full power (for 4+ block steps)
                            npc.netUpdate = true;
                        }
                        else
                        {
                            npc.velocity.Y = -jumpPower * 0.875f; //Jump with 87.5% power (for 3 block steps)
                            npc.netUpdate = true;
                        }
                    }
                    //For everything else, head height clear:
                    else if (mayJump && UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 1))
                    {
                        //2 blocks above ground level (mid body height) blocked
                        npc.velocity.Y = -jumpPower * 0.75f; //Jump with 75% power (for 2 block steps)
                        npc.netUpdate = true;
                    }
                    else if ((mayJump || mayStepUpOneTile) && UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet))
                    {
                        // 1-tile step: smart-nav enemies should not visibly jump here.
                        // Use only a tiny lift plus forward pressure so this reads as a step.
                        if (npc.velocity.Y > -2.2f)
                            npc.velocity.Y = -2.2f;
                        float minHSpeed = Math.Max(topSpeed * 0.55f, 1.5f);
                        if (npc.direction ==  1 && npc.velocity.X < minHSpeed)  npc.velocity.X = minHSpeed;
                        if (npc.direction == -1 && npc.velocity.X > -minHSpeed) npc.velocity.X = -minHSpeed;
                        npc.netUpdate = true;
                    }
                    else
                    {
                        // No wall obstacle ahead — check whether the floor continues.
                        // If the tile directly ahead at foot level is missing, there's a gap or drop.
                        if (!UsefulFunctions.IsTileReallySolid(x_in_front, y_below_feet))
                        {
                            // Scan up to 8 tiles horizontally and 20 tiles deep for a landing.
                            // gapWidth = horizontal distance to the far edge (0 = pure step-down).
                            // dropDepth = how many tiles lower the landing is (0 = same elevation).
                            int gapWidth  = -1; // -1 until a landing is found
                            int dropDepth =  0;
                            const int maxLandingScanDepth = 20;
                            bool waypointWantsForwardTravel = globalNPC.WaypointTimer > 0
                                && (Math.Abs(globalNPC.WaypointTarget.X - npc.Center.X) <= 16f
                                    || Math.Sign(globalNPC.WaypointTarget.X - npc.Center.X) == npc.direction);

                            for (int scan = 0; scan <= 8; scan++)
                            {
                                if (gapWidth >= 0) break;
                                int cx = x_in_front + scan * npc.direction;
                                for (int dy = 0; dy <= maxLandingScanDepth; dy++)
                                {
                                    if (UsefulFunctions.IsTileReallySolid(cx, y_below_feet + dy))
                                    {
                                        gapWidth  = scan;
                                        dropDepth = dy;
                                        break;
                                    }
                                }
                            }

                            // Only drop toward a pit when the player is clearly lower (~4 tiles).
                            // Same-level and above cases: halt so jump/BFS logic handles traversal instead.
                            bool playerClearlyBelow = Main.player[npc.target].Center.Y > npc.Center.Y + 64f;

                            if (gapWidth < 0)
                            {
                                // No landing found within scan range — very deep or wide pit.
                                // Only walk off the edge when the player is clearly below.
                                if (!playerClearlyBelow && !waypointWantsForwardTravel)
                                {
                                    npc.velocity.X = 0f;
                                    if (globalNPC.NavigationTier >= 1 && globalNPC.BoredTimer == 0)
                                        globalNPC.BoredTimer = 60;
                                }
                            }
                            else if (gapWidth == 0)
                            {
                                // Pure step-down (floor at same X but lower Y).
                                // Small drops (≤ 3 tiles): let gravity handle it naturally.
                                // Large drops: halt unless player is clearly below.
                                if (dropDepth > 3 && !playerClearlyBelow && !waypointWantsForwardTravel)
                                {
                                    npc.velocity.X = 0f;
                                    if (globalNPC.NavigationTier >= 1 && globalNPC.BoredTimer == 0)
                                        globalNPC.BoredTimer = 60;
                                }
                            }
                            else
                            {
                                // Genuine horizontal gap: jump across if reachable, else halt.
                                // Base cap of 8 tiles so all enemies cross typical platform gaps;
                                // NavigationTier >= 1 enemies scale further with their MaxJumpBoost.
                                float maxJumpable = globalNPC.NavigationTier >= 1
                                    ? Math.Max(8f, globalNPC.MaxJumpBoost + 3f)
                                    : 8f;

                                if (gapWidth <= maxJumpable)
                                {
                                    // Boost just enough to clear the gap, proportional to width.
                                    // Capped at the NPC's jump boost (or 4f floor for tier-0 enemies).
                                    float boostCap = globalNPC.NavigationTier >= 1
                                        ? Math.Max(globalNPC.MaxJumpBoost, 4f)
                                        : 4f;
                                    float horizontalBoost = MathHelper.Clamp(gapWidth * 0.7f, 1.5f, boostCap);
                                    npc.velocity.Y  = -jumpPower;
                                    npc.velocity.X += horizontalBoost * npc.direction;
                                    npc.netUpdate = true;
                                }
                                else
                                {
                                    // Gap too wide to jump — halt so the NPC doesn't walk off.
                                    if (!waypointWantsForwardTravel)
                                    {
                                        npc.velocity.X = 0f;
                                        if (globalNPC.NavigationTier >= 1 && globalNPC.BoredTimer == 0)
                                            globalNPC.BoredTimer = 60;
                                    }
                                }
                            }
                        }
                    }

                    //Door breaking
                    //First, it checks if the tile in front of it is solid, a door, and the npc can break it
                    if (UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 1) && Main.tile[x_in_front, y_above_feet - 1].TileType == 10 && (doorBreakingDamage > 0))
                    {
                        npc.velocity.Y = 0;
                        globalNPC.BoredTimer = 0; // not bored if working on breaking a door
                        if (Main.GameUpdateCount % 60 == 0)  //  knock once per second
                        {
                            npc.velocity.X = 0.5f * -npc.direction; //  slight recoil from hitting it
                            globalNPC.DoorBreakProgress += doorBreakingDamage;  //  increase door damage counter
                            WorldGen.KillTile(x_in_front, y_above_feet - 1, true, true, false);  //  kill door ? when door not breaking too? can fail=true; effect only would make more sense, to make knocking sound
                            if (globalNPC.DoorBreakProgress >= 10f && Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                globalNPC.DoorBreakProgress = 0; //Reset counter

                                //Try to open door
                                if (!WorldGen.OpenDoor(x_in_front, y_above_feet, npc.direction))
                                {
                                    //If the door is stuck set the npc to bored
                                    globalNPC.BoredTimer = 999;
                                    npc.velocity.X = 0; // cancel recoil so boredom wall reflection can trigger
                                }
                                else if (Main.netMode == NetmodeID.Server)
                                {
                                    //If it didn't fail sync the door opening
                                    NetMessage.SendData(MessageID.ToggleDoorState, -1, -1, null, 0, (float)x_in_front, (float)y_above_feet, (float)npc.direction, 0); // ??
                                }
                            }
                        }
                    }
                    skipNormalJumps: ; // target for the ledge-run-up early-exit goto
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
            // BFS may route the NPC through a platform drop: waypoint Y is below current position.
            bool bfsWantsDrop = globalNPC.WaypointTimer > 0
                && globalNPC.WaypointTarget.Y > npc.Center.Y + 48f
                && Math.Abs(globalNPC.WaypointTarget.X - npc.Center.X) < 32f;
            bool navWantsPlatformDrop = globalNPC.WaypointTimer > 0
                && globalNPC.WaypointAction == tsorcRevampGlobalNPC.NavActionType.DropThroughPlatform;
            bool shouldDropPlatform = globalNPC.NavigationTier >= 1
                ? (playerIsBelow || bfsWantsDrop || navWantsPlatformDrop)
                : playerIsBelow && (globalNPC.BoredTimer > 60 || Math.Abs(npc.Center.X - Main.player[npc.target].Center.X) < 300);

            if (standing_on_platforms && atLeastOnePlatform && shouldDropPlatform && (globalNPC.NavigationTier < 1 || Math.Abs(npc.velocity.X) < 2f || navWantsPlatformDrop))
            {
                npc.noTileCollide = true;
            }

            // Reset double jump and wind down StuckTimer when standing (velocity.Y == 0)
            if (npc.velocity.Y == 0f)
            {
                globalNPC.UsedDoubleJump = false;
            }

            // StuckTimer: detect ground-level movement blockage and attempt an escape jump
            if (standing_on_solid_tile && globalNPC.NavigationTier >= 1)
            {
                bool tryingToMoveForward = (npc.direction == 1 && npc.velocity.X >= 0f) ||
                                           (npc.direction == -1 && npc.velocity.X <= 0f);
                if (tryingToMoveForward && Math.Abs(npc.velocity.X) < 0.5f)
                {
                    globalNPC.StuckTimer++;
                }
                else
                {
                    // Don't erase wall-contact progress too aggressively. Brief wiggles,
                    // tiny recoil, or overhang checks should not wipe the timer back to 0.
                    if (Math.Abs(npc.velocity.X) > topSpeed * 0.5f)
                        globalNPC.StuckTimer = Math.Max(0, globalNPC.StuckTimer - 1);
                    // NPC is moving freely — also drain the run-up timer so a previous
                    // stuck episode doesn't leave a stale back-up in progress.
                    if (globalNPC.LedgeRunUpTimer > 0 && Math.Abs(npc.velocity.X) > topSpeed * 0.5f
                        && Math.Sign(npc.velocity.X) == npc.direction)
                    {
                        globalNPC.LedgeRunUpTimer = 0;
                        globalNPC.LedgeRunUpDirection = 0;
                    }
                }

                if (globalNPC.StuckTimer > 30)
                {
                    int blockedDirection = npc.direction == 0 ? Math.Sign(npc.velocity.X) : npc.direction;
                    globalNPC.StuckTimer = 0;
                    globalNPC.LedgeRunUpTimer = 0; // cancel any pending run-up; BFS takes over
                    globalNPC.LedgeRunUpDirection = 0;
                    ClearFighterWaypoint(globalNPC);
                    MarkNavDirectionBlocked(globalNPC, blockedDirection, 240);
                    StartNavExplore(npc, globalNPC, -blockedDirection, 180);
                    // Run BFS immediately rather than via BoredTimer — rerouting must fire
                    // even when the NPC has LOS or an old waypoint is steering it into a wall.
                    if (globalNPC.NavigationTier >= 1)
                    {
                        globalNPC.LastNavIntent = "stuck:reroute";
                        TrySetFighterWaypoint(npc, globalNPC, true);
                    }
                    else if (globalNPC.BoredTimer < 21)
                    {
                        globalNPC.BoredTimer = 21;
                    }
                    npc.netUpdate = true;
                }
            }

            // Double jump: apex-triggered mid-air second jump for capable enemies
            if (globalNPC.CanDoubleJump && !globalNPC.UsedDoubleJump && globalNPC.NavigationTier >= 1)
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

            // "LOS but player significantly above/below" behaves like no-LOS for boredom.
            // Threshold aligned with the different-floor BFS trigger (48px = 3 tiles) so both
            // systems agree on what "different floor" means.
            bool playerOnDifferentLevel = lineOfSight && Math.Abs(Main.player[npc.target].Center.Y - npc.Center.Y) > 48f;

            if (globalNPC.BoredTimer >= 0)
            {
                //Increase boredom if it's stuck on a wall it can't pass through, walking back and forth above the player, or can teleport but can't see the player
                if (!lineOfSight || playerOnDifferentLevel)
                {
                    globalNPC.BoredTimer++;

                    //Time it takes to get bored scales with how long it takes to accelerate
                    if (globalNPC.BoredTimer > globalNPC.BoredomThreshold * globalNPC.Patience)
                    {
                        if (!canTeleport)
                        {
                            if (globalNPC.NavigationTier >= 1)
                            {
                                globalNPC.BoredTimer = globalNPC.BoredomThreshold;
                                globalNPC.LastNavIntent = "bored:path-retry";
                                TrySetFighterWaypoint(npc, globalNPC, true);
                            }
                            else
                            {
                                globalNPC.BoredTimer = -540;
                                if (globalNPC.WaypointTimer == 0)
                                    npc.direction *= -1;
                            }
                        }
                        else
                        {
                            //Try to teleport somewhere it has line of sight to the player
                            if (globalNPC.TeleportCountdown == 0)
                            {
                                QueueTeleport(npc, 50, true, globalNPC.TeleportTelegraphTime);
                            }
                        }
                    }

                    // BFS waypoint: trigger as soon as the NPC becomes bored, then keep a
                    // slower fallback rescan while it remains stuck/bored.
                    bool justBecameBored = globalNPC.BoredTimer == 1;
                    bool stuckRescan = globalNPC.StuckTimer >= 20 && globalNPC.StuckTimer % 60 == 0;
                    bool bfsFallback  = Main.GameUpdateCount % 120 == 0;

                    if (globalNPC.NavigationTier >= 1 &&
                        globalNPC.WaypointTimer == 0 &&
                        (justBecameBored || stuckRescan || bfsFallback))
                    {
                        globalNPC.LastNavIntent = justBecameBored ? "bored:first-frame"
                            : stuckRescan ? "bored:stuck-rescan"
                            : "bored:fallback-rescan";
                        TrySetFighterWaypoint(npc, globalNPC, justBecameBored || stuckRescan);
                    }
                }
                //If it's not stuck not and it's not bored decrease the boredom counter
                else if (globalNPC.BoredTimer > 0)
                {
                    globalNPC.BoredTimer -= 1;
                    if (globalNPC.BoredTimer < 0)
                    {
                        globalNPC.BoredTimer = 0;
                    }
                }
            }
            else
            {
                //Always increase it if it's negative (aka bored)
                globalNPC.BoredTimer++;
            }

            // Only hard-reset boredom when the player is truly on the same floor (32px = 2 tiles).
            // The old 80px threshold was killing BoredTimer for players one floor up, preventing
            // boredom BFS from ever firing in the most common stuck scenario.
            if (!globalNPC.Fleeing && lineOfSight && Math.Abs(Main.player[npc.target].Center.Y - npc.Center.Y) < 32f)
            {
                globalNPC.BoredTimer = 0;
            }

            // ── Different-floor BFS trigger ───────────────────────────────────────
            // When the NPC has LOS but the player is grounded on a meaningfully
            // different floor (> 3 tiles of vertical separation), BoredTimer is
            // constantly reset to 0 by the block above, so the standard BFS trigger
            // (BoredTimer > 20) never fires.  The NPC just paces left-right forever.
            // Fix: fire BFS independently every ~3 s when this condition persists.
            // Stagger the check by NPC id so all NPCs don't BFS on the same frame.
            if (globalNPC.NavigationTier >= 1 && globalNPC.WaypointTimer == 0 && lineOfSight
                && Math.Abs(Main.player[npc.target].Center.Y - npc.Center.Y) > 48f  // > 3 tiles apart vertically
                && Main.player[npc.target].velocity.Y == 0f                          // player is standing (not falling)
                && ((npc.whoAmI + (int)Main.GameUpdateCount) % 60 == 0))             // every ~1 s, staggered per NPC
            {
                globalNPC.LastNavIntent = "los:different-floor";
                TrySetFighterWaypoint(npc, globalNPC, true);
            }

            // WeakTeleport reach tracking: count how long the NPC has been unable to reach the player.
            // "Reached" means LOS within 600px (~38 tiles). After the configured threshold
            // without reaching, briefly disengage, then turn back and resume pursuit.
            if (globalNPC.NavigationTier < 1 && globalNPC.WeakTeleport && globalNPC.WeakTeleportBoredPhase == 0)
            {
                if (lineOfSight && npc.Distance(Main.player[npc.target].Center) < 600f)
                {
                    // NPC is engaging the player; stop counting toward the bored-walk fallback.
                    if (globalNPC.WeakTeleportReachTimer > 0) globalNPC.WeakTeleportReachTimer = 0;
                }
                else
                {
                    globalNPC.WeakTeleportReachTimer++;
                    if (globalNPC.WeakTeleportReachTimer >= globalNPC.WeakTeleportBoredThreshold)
                    {
                        // Start at phase 1 (standstill). The state machine sets direction when
                        // it transitions into phase 2 (walk-away).
                        globalNPC.WeakTeleportBoredPhase = 1;
                        globalNPC.WeakTeleportBoredTimer = 120; // stand still 2 s
                        globalNPC.WeakTeleportReachTimer = 0;
                    }
                }
            }

            LogFighterNavDebug(npc, globalNPC, lineOfSight);

            //Dodging
            if (globalNPC.BoredTimer == 0 && globalNPC.TeleportCountdown == 0 && globalNPC.DodgeCooldown == 0)
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





        /// <summary>
        /// Searches for a valid teleport landing spot near <paramref name="player"/> that has
        /// direct line of sight back to the player. Returns the world-space center position of
        /// the landing spot, or <see cref="Vector2.Zero"/> if no valid spot was found.
        /// </summary>
        private static Vector2 FindWeakTeleportDestination(NPC npc, Player player)
        {
            for (int attempt = 0; attempt < 60; attempt++)
            {
                // Random horizontal offset: 35-50 tiles from player, random side.
                // WeakTeleport should help the NPC re-enter pursuit, not appear on top of the player.
                float offsetX = Main.rand.NextFloat(35f, 50f) * 16f * (Main.rand.NextBool() ? 1f : -1f);
                // Small vertical scatter so the NPC can land on platforms above/below player
                float offsetY = Main.rand.NextFloat(-5f, 5f) * 16f;
                Vector2 candidate = player.Center + new Vector2(offsetX, offsetY);

                int tileX = (int)(candidate.X / 16f);
                int tileY = (int)(candidate.Y / 16f);

                // Skip positions inside solid walls
                if (UsefulFunctions.IsTileReallySolid(tileX, tileY) ||
                    UsefulFunctions.IsTileReallySolid(tileX, tileY - 1))
                    continue;

                // Find the first ground tile below (solid tile or platform, within 6 tiles)
                int groundY = -1;
                for (int dy = 0; dy <= 6; dy++)
                {
                    Tile t = Framing.GetTileSafely(tileX, tileY + dy);
                    if (UsefulFunctions.IsTileReallySolid(tileX, tileY + dy) ||
                        (t.HasTile && TileID.Sets.Platforms[t.TileType]))
                    {
                        groundY = tileY + dy;
                        break;
                    }
                }
                if (groundY == -1) continue;

                // World-space center of the NPC if it were standing on that tile
                Vector2 centerAtDest = new Vector2(tileX * 16f + 8f, groundY * 16f - npc.height / 2f);

                // Require clear LOS from landing spot center to player
                if (!Collision.CanHitLine(centerAtDest, 1, 1, player.Center, 1, 1))
                    continue;

                return centerAtDest;
            }
            return Vector2.Zero;
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

        private static bool HasCrossableGapTowardPlayer(NPC npc, tsorcRevampGlobalNPC globalNPC, out int travelDirection)
        {
            travelDirection = 0;
            if (globalNPC.NavigationTier < 1 || npc.velocity.Y != 0f)
            {
                return false;
            }

            float playerDeltaX = Main.player[npc.target].Center.X - npc.Center.X;
            if (Math.Abs(playerDeltaX) < 48f)
            {
                return false;
            }

            travelDirection = playerDeltaX < 0f ? -1 : 1;
            int aheadX = travelDirection == -1
                ? (int)(npc.position.X / 16f) - 1
                : (int)((npc.position.X + npc.width) / 16f);
            int belowFeetY = (int)(npc.position.Y + npc.height + 8f) / 16;

            const int maxLandingScanDepth = 6;
            const int maxGapStartScan = 3;
            float maxJumpable = Math.Max(4f, Math.Min(8f, globalNPC.MaxJumpBoost + 2f));

            for (int gapStart = 0; gapStart <= maxGapStartScan; gapStart++)
            {
                int gapX = aheadX + gapStart * travelDirection;
                if (tsorcRevampGlobalNPC.BfsCanStand(npc, gapX, belowFeetY))
                {
                    continue;
                }

                for (int scan = gapStart + 1; scan <= maxJumpable; scan++)
                {
                    int scanX = aheadX + scan * travelDirection;
                    for (int dy = 0; dy <= maxLandingScanDepth; dy++)
                    {
                        if (tsorcRevampGlobalNPC.BfsCanStand(npc, scanX, belowFeetY + dy))
                        {
                            // Only jump same-level gaps. One-tile drops/slopes should be walked
                            // down naturally instead of being treated like pits.
                            return dy == 0;
                        }
                    }
                }

                return false;
            }

            return false;
        }

        private static bool IsFighterStandableTile(int x, int y)
        {
            if (UsefulFunctions.IsTileReallySolid(x, y))
            {
                return true;
            }

            if (Main.tile.Width > x && Main.tile.Height > y && x >= 0 && y >= 0)
            {
                Tile tile = Main.tile[x, y];
                return tile.HasTile && !tile.IsActuated && TileID.Sets.Platforms[tile.TileType];
            }

            return false;
        }

        private static void ClearFighterWaypoint(tsorcRevampGlobalNPC globalNPC)
        {
            globalNPC.WaypointTimer = 0;
            globalNPC.WaypointTarget = Vector2.Zero;
            globalNPC.WaypointAction = tsorcRevampGlobalNPC.NavActionType.None;
            globalNPC.WaypointNoProgressTimer = 0;
            globalNPC.LastWaypointDistance = 0f;
            globalNPC.NavRouteIndex = 0;
            globalNPC.NavRouteCount = 0;
            globalNPC.NavRouteTimer = 0;
            globalNPC.NavRouteNoProgressTimer = 0;
            globalNPC.LastNavRouteDistance = 0f;
        }

        private static void MarkNavDirectionBlocked(tsorcRevampGlobalNPC globalNPC, int blockedDirection, int duration = 180)
        {
            if (blockedDirection == 0)
            {
                return;
            }

            globalNPC.NavBlockedDirection = Math.Sign(blockedDirection);
            globalNPC.NavBlockedDirectionTimer = Math.Max(globalNPC.NavBlockedDirectionTimer, duration);
        }

        private static void StartNavExplore(NPC npc, tsorcRevampGlobalNPC globalNPC, int preferredDirection, int duration = 180)
        {
            int direction = preferredDirection == 0
                ? Math.Sign(Main.player[npc.target].Center.X - npc.Center.X)
                : Math.Sign(preferredDirection);

            if (direction == 0)
            {
                direction = npc.direction == 0 ? 1 : npc.direction;
            }

            if (globalNPC.NavBlockedDirectionTimer > 0 && direction == globalNPC.NavBlockedDirection)
            {
                direction *= -1;
            }

            globalNPC.NavExploreDirection = direction;
            globalNPC.NavExploreTimer = Math.Max(globalNPC.NavExploreTimer, duration);
            globalNPC.FighterNoLosPursuitBoostTimer = Math.Max(globalNPC.FighterNoLosPursuitBoostTimer, 90);
            globalNPC.BoredTimer = Math.Max(globalNPC.BoredTimer, 1);
            npc.direction = direction;
            npc.spriteDirection = direction;
        }

        private static bool TrySetFighterWaypoint(NPC npc, tsorcRevampGlobalNPC globalNPC, bool force = false)
        {
            if (globalNPC.NavigationTier < 1 || globalNPC.WaypointTimer > 0)
            {
                globalNPC.LastWaypointResult = globalNPC.NavigationTier < 1 ? "skip:tier0" : "skip:active";
                return false;
            }
            if (!force && globalNPC.WaypointSearchCooldown > 0)
            {
                globalNPC.LastWaypointResult = $"skip:cooldown-{globalNPC.WaypointSearchCooldown}";
                return false;
            }

            Span<Vector2> routeTargets = stackalloc Vector2[tsorcRevampGlobalNPC.MaxNavRouteSteps];
            Span<tsorcRevampGlobalNPC.NavActionType> routeActions = stackalloc tsorcRevampGlobalNPC.NavActionType[tsorcRevampGlobalNPC.MaxNavRouteSteps];
            if (tsorcRevampGlobalNPC.BfsFindRoute(npc, globalNPC.MaxJumpPower, globalNPC.MaxJumpBoost, routeTargets, routeActions, out int routeCount))
            {
                int routeStart = 0;
                while (routeStart < routeCount - 1 && !IsUsefulFighterWaypoint(npc, routeTargets[routeStart], routeActions[routeStart], routeCount - routeStart))
                {
                    routeStart++;
                }

                Vector2 waypoint = routeTargets[routeStart];
                tsorcRevampGlobalNPC.NavActionType action = routeActions[routeStart];
                if (!IsUsefulFighterWaypoint(npc, waypoint, action))
                {
                    globalNPC.WaypointSearchFailures++;
                    globalNPC.LastWaypointResult = $"fail:useless-{action} x{globalNPC.WaypointSearchFailures}";
                    globalNPC.WaypointSearchCooldown = force ? 12 : 30;
                    bool directLos = Main.player[npc.target].CanHit(npc) && Math.Abs(Main.player[npc.target].Center.Y - npc.Center.Y) < 32f;
                    if (!directLos && (force || globalNPC.WaypointSearchFailures >= 3))
                    {
                        int preferredDirection = Math.Sign(Main.player[npc.target].Center.X - npc.Center.X);
                        StartNavExplore(npc, globalNPC, preferredDirection, 150);
                        globalNPC.LastNavIntent = "explore:useless-waypoint";
                    }
                    return false;
                }

                int copiedRouteCount = Math.Min(routeCount - routeStart, tsorcRevampGlobalNPC.MaxNavRouteSteps);
                for (int i = 0; i < copiedRouteCount; i++)
                {
                    globalNPC.NavRouteTargets[i] = routeTargets[routeStart + i];
                    globalNPC.NavRouteActions[i] = routeActions[routeStart + i];
                }
                globalNPC.NavRouteIndex = 0;
                globalNPC.NavRouteCount = copiedRouteCount;
                globalNPC.NavRouteTimer = 0;
                globalNPC.NavRouteNoProgressTimer = 0;
                globalNPC.LastNavRouteDistance = npc.Distance(waypoint);
                globalNPC.WaypointTarget = waypoint;
                globalNPC.WaypointTimer = 420;
                globalNPC.WaypointAction = action;
                globalNPC.LastWaypointDistance = npc.Distance(waypoint);
                globalNPC.WaypointNoProgressTimer = 0;
                globalNPC.NavExploreTimer = 0;
                globalNPC.NavExploreDirection = 0;
                globalNPC.BoredTimer = Math.Max(globalNPC.BoredTimer, 1);
                globalNPC.WaypointSearchFailures = 0;
                string skippedPrefix = routeStart > 0 ? $"skip{routeStart} " : "";
                globalNPC.LastWaypointResult = $"{skippedPrefix}route:{globalNPC.NavRouteCount} set:{action} ({waypoint.X / 16f:F1},{waypoint.Y / 16f:F1})";
                globalNPC.WaypointSearchCooldown = force ? 10 : 20;
                npc.netUpdate = true;
                return true;
            }

            globalNPC.WaypointSearchFailures++;
            globalNPC.LastWaypointResult = $"fail:bfs x{globalNPC.WaypointSearchFailures}";
            globalNPC.WaypointSearchCooldown = force ? 20 : 45;
            bool hasDirectLos = Main.player[npc.target].CanHit(npc) && Math.Abs(Main.player[npc.target].Center.Y - npc.Center.Y) < 32f;
            if (!hasDirectLos && (force || globalNPC.WaypointSearchFailures >= 3))
            {
                int preferredDirection = Math.Sign(Main.player[npc.target].Center.X - npc.Center.X);
                StartNavExplore(npc, globalNPC, preferredDirection, 180);
                globalNPC.LastNavIntent = "explore:bfs-failed";
            }
            return false;
        }

        private static bool IsUsefulFighterWaypoint(NPC npc, Vector2 waypoint, tsorcRevampGlobalNPC.NavActionType action, int remainingRouteSteps = 1)
        {
            Player player = Main.player[npc.target];
            Vector2 delta = waypoint - npc.Center;

            if (action == tsorcRevampGlobalNPC.NavActionType.Walk && Math.Abs(delta.X) < 18f && Math.Abs(delta.Y) < 18f)
            {
                return remainingRouteSteps > 1;
            }
            if (action == tsorcRevampGlobalNPC.NavActionType.Walk && Math.Abs(delta.Y) > 40f)
            {
                return false;
            }
            if (action == tsorcRevampGlobalNPC.NavActionType.JumpTo && Math.Abs(delta.X) < 16f && Math.Abs(delta.Y) < 24f)
            {
                return false;
            }

            bool playerClearlyAbove = player.Center.Y < npc.Center.Y - 48f;
            bool waypointBelowNpc = waypoint.Y > npc.Center.Y + 18f;
            if (playerClearlyAbove && waypointBelowNpc && action == tsorcRevampGlobalNPC.NavActionType.JumpTo)
            {
                return false;
            }

            float currentDistance = npc.Distance(player.Center);
            float waypointDistance = Vector2.Distance(waypoint, player.Center);
            if (waypointDistance > currentDistance + 160f && action != tsorcRevampGlobalNPC.NavActionType.Drop)
            {
                return false;
            }

            return true;
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
                || globalNPC.BoredTimer > 0
                || globalNPC.StuckTimer > 0
                || globalNPC.WaypointTimer > 0
                || globalNPC.WaypointSearchFailures > 0
                || globalNPC.NavExploreTimer > 0
                || globalNPC.NavBlockedDirectionTimer > 0
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
                string waypoint = globalNPC.WaypointTimer > 0
                    ? $"{globalNPC.WaypointAction}@({globalNPC.WaypointTarget.X / 16f:F1},{globalNPC.WaypointTarget.Y / 16f:F1})/{globalNPC.WaypointTimer}"
                    : "none";
                string route = globalNPC.NavRouteCount > 0
                    ? $"{globalNPC.NavRouteIndex + 1}/{globalNPC.NavRouteCount}"
                    : "none";
                string line = $"[{DateTime.Now:HH:mm:ss}] {npc.TypeName}#{npc.whoAmI} pos=({npc.Center.X / 16f:F1},{npc.Center.Y / 16f:F1}) player=({player.Center.X / 16f:F1},{player.Center.Y / 16f:F1}) vel=({npc.velocity.X:F2},{npc.velocity.Y:F2}) g={!airborne} cx={npc.collideX} cy={npc.collideY} dist={npc.Distance(player.Center):F0} los={lineOfSight} yDiff={player.Center.Y - npc.Center.Y:F0} tier={globalNPC.NavigationTier} bored={globalNPC.BoredTimer} stuck={globalNPC.StuckTimer} route={route} wp={waypoint} intent={globalNPC.LastNavIntent} result={globalNPC.LastWaypointResult} cd={globalNPC.WaypointSearchCooldown} wpNoProg={globalNPC.WaypointNoProgressTimer} blocked={globalNPC.NavBlockedDirection}/{globalNPC.NavBlockedDirectionTimer} explore={globalNPC.NavExploreDirection}/{globalNPC.NavExploreTimer} ledgeRun={globalNPC.LedgeRunUpTimer} vault={globalNPC.LedgeVaultTimer} jumpCd={globalNPC.NavJumpCooldown} stopFire={globalNPC.CanStopToFire}";
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
                    if (globalNPC.CanStopToFire && globalNPC.NavigationTier >= 2 && globalNPC.FighterRangedStandShotsRemaining == 0
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
            if (globalNPC.NavigationTier < 2)
            {
                return;
            }

            globalNPC.FighterAttacksSincePause++;
            if (globalNPC.FighterAttacksSincePause >= attacksBeforePause)
            {
                globalNPC.FighterAttacksSincePause = 0;
                globalNPC.FighterPostAttackPauseTimer = pauseTicks;
                globalNPC.BoredTimer = 0;
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
        public static Vector2? GenerateTeleportPosition(NPC npc, int range, bool requireLineofSight = true)
        {
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

        public static void QueueTeleport(NPC npc, int range, bool requireLineofSight = true, int TeleportTelegraphTime = 140)
        {
            Vector2? potentialNewPos;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 100; i++)
                {
                    potentialNewPos = GenerateTeleportPosition(npc, range, requireLineofSight);
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
                    globalNPC.BoredTimer = 0;
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
