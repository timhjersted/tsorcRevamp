using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Potions;

namespace tsorcRevamp.NPCs.Enemies.Basilisk
{
    class BasiliskShifter : ModNPC, IStaggerable
    {
        //HARD MODE VARIANT 
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 12;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 2;
            AnimationType = 28;
            NPC.aiStyle = 3;
            NPC.damage = 30;
            NPC.defense = 55;
            NPC.height = 46;
            NPC.width = 38;
            NPC.lifeMax = 350;
            NPC.HitSound = SoundID.NPCHit20;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 1750; // health / 2 : was 233
            NPC.lavaImmune = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.BasiliskShifterBanner>();

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.MaxJumpPower = 10f;
            globalNPC.MaxJumpBoost = 6f;
            globalNPC.NavSearchRadius = 80; // Phase 2: SmartFighter4AI movement
            globalNPC.CanUseRopes = true;
            // Step 6 beast levers: lurk (Wander) around where it lost the player when it gives up.
            globalNPC.PatrolMode = NPCs.PatrolMode.Wander;
            globalNPC.PatrolAnchorSource = NPCs.PatrolAnchorSource.GiveUpLocation;
            // Evasive on-hit: hop/dash away or i-frame quick-step.
            EvasiveProfile.Basilisk(globalNPC);
            // Re-adds the old main-attack vertical hop and low-HP rising-hover final hop.
            EvasiveProfile.BasiliskShifterAttackJumps(globalNPC);
            globalNPC.EvasiveBasiliskShifterCloseBackhop = true;
            globalNPC.EvasiveBasiliskShifterFarForwardHop = true;
            globalNPC.KiteRangeMin = 9f;
            globalNPC.KiteRangeMax = 18f;
            globalNPC.KiteLooseness = 0.15f;
        }

        float breathTimer = 60;

        // Deterministic projectile-attack timing (decide → dust telegraph → colored flash at commit → fire). The
        // attack is rolled+locked once at AtkDecide so the flash colour and the shot match. See attack-phase tagging.
        private const int AtkDecide = 60;    // roll + lock the attack; dust telegraph (interruptible) starts
        private const int AtkFlash = 85;     // colored TelegraphFlash spawns = the commit instant (hyperarmor begins)
        private const int AtkFire = 110;     // projectile(s) launch
        private const int AtkSprayEnd = 140; // lob sprays AtkFire..AtkSprayEnd; single shots reset ~10t after AtkFire
        private const int PostAttackDowntime = 120;
        private const float BreathTrackingHalfCone = 1.3962634f; // 80 degrees, for a 160-degree breath tracking cone.
        private const float DisrupterMinSpacing = 250f;
        private const int DisrupterShotInterval = 30;
        private const int DisrupterShotCount = 3;
        private int lockedAttack = -1;       // -1 none; 0 lob(purple), 1 spit(green), 2 final(green,low-HP), 3 disrupter(purple)
        private Vector2 lockedTargetPosition = Vector2.Zero;
        private int lockedFacingDirection = 1;
        private int disrupterPattern = 0;
        private bool disrupterVerticalTargets = false;

        float shotTimer;
        int chargeDamage = 0;
        bool chargeDamageFlag = false;
        int cursedBreathDamage = 13;
        int hypnoticDisruptorDamage = 18;
        int bioSpitDamage = 18;
        int leechTongueDamage = 10;
        int leechTongueTimer;


        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player; //These are mostly redundant with the new zone definitions, but it still works.
            bool Meteor = P.ZoneMeteor;
            bool Jungle = P.ZoneJungle;
            bool Dungeon = P.ZoneDungeon;
            bool Corruption = (P.ZoneCorrupt || P.ZoneCrimson);
            bool Hallow = P.ZoneHallow;
            bool AboveEarth = P.ZoneOverworldHeight;
            bool InBrownLayer = P.ZoneDirtLayerHeight;
            bool InGrayLayer = P.ZoneRockLayerHeight;
            bool InHell = P.ZoneUnderworldHeight;
            bool FrozenOcean = spawnInfo.SpawnTileX > (Main.maxTilesX - 800);
            bool Ocean = spawnInfo.SpawnTileX < 800 || FrozenOcean;
            // P.townNPCs > 0f // is no town NPCs nearby

            //Ensuring it can't spawn if two already exists.
            int count = 0;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if (Main.npc[i].type == NPC.type)
                {
                    count++;
                    if (count > 1)
                    {
                        return 0;
                    }
                }
            }

            if (spawnInfo.Water) return 0f;

            //SPAWNS IN HM JUNGLE AT NIGHT ABOVE GROUND AFTER THE RAGE IS DEFEATED
            if (Main.hardMode && Jungle && !Corruption && !Main.dayTime && AboveEarth && !Ocean && P.townNPCs <= 0f && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheRage>())) && Main.rand.NextBool(30)) return 1;

            //SPAWNS IN HM METEOR UNDERGROUND AT NIGHT
            if (Main.hardMode && Meteor && !Main.dayTime && (InBrownLayer || InGrayLayer) && !spawnInfo.Water && Main.rand.NextBool(10)) return 1;

            if (Main.hardMode && Meteor && Main.dayTime && (InBrownLayer || InGrayLayer) && !spawnInfo.Water && Main.rand.NextBool(20)) return 1;

            //SPAWNS AGAIN IN CORRUPTION AND NOW CRIMSON
            if (Main.hardMode && Corruption && !Main.dayTime && !Ocean && (InBrownLayer || InGrayLayer) && !spawnInfo.Water && Main.rand.NextBool(20)) return 1;

            if (Main.hardMode && Corruption && Main.dayTime && !Ocean && (InBrownLayer || InGrayLayer) && !spawnInfo.Water && Main.rand.NextBool(30)) return 1;

            //SPAWNS IN DUNGEON AT NIGHT RARELY
            if (Main.hardMode && Dungeon && !Main.dayTime && (InBrownLayer || InGrayLayer) && Main.rand.NextBool(45)) return 1;

            //SPAWNS IN HM HALLOW RARELY
            if (Main.hardMode && (InBrownLayer || InGrayLayer) && Hallow && !Ocean && !spawnInfo.Water && Main.rand.NextBool(45)) return 1;

            //SPAWNS RARELY IN HM JUNGLE UNDERGROUND
            if (Main.hardMode && Jungle && InGrayLayer && !Ocean && !spawnInfo.Water && Main.rand.NextBool(60)) return 1;

            //BLOODMOON HIGH SPAWN IN METEOR OR JUNGLE
            if (Main.hardMode && !tsorcRevampWorld.SuperHardMode && (Meteor || Jungle) && !Dungeon && (AboveEarth || InBrownLayer || InGrayLayer) && !spawnInfo.Water && Main.bloodMoon && Main.rand.NextBool(5)) return 1;

            return 0;
        }
        #endregion

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            Projectiles.Enemy.BasiliskLeechTongue.NotifyOwnerHit(NPC, damageDone);
            tsorcRevampAIs.EvasiveOnHit(NPC, true);
        }
        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            Projectiles.Enemy.BasiliskLeechTongue.NotifyOwnerHit(NPC, damageDone);
            tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
        }

        // Stagger (poise break) cancels a telegraphing attack — reset the timers to neutral. A committed attack is
        // hyperarmored, so a stagger can't happen during one; this only ever fires during a telegraph/neutral.
        public void OnStagger(NPC npc)
        {
            shotTimer = 0f;
            lockedAttack = -1;
            lockedTargetPosition = Vector2.Zero;
            leechTongueTimer = 0;
            if (breathTimer > 0f) breathTimer = 0f; // drop a breath wind-up (leave a mid-fire breath alone)
        }

        public override void AI()
        {
            bool canStartLeechTongue = lockedAttack == -1 && breathTimer >= 0f && breathTimer <= 360f;
            if (BasiliskLeechTongueAttack.Update(NPC, ref leechTongueTimer, 430, leechTongueDamage, canStartLeechTongue))
            {
                return;
            }

            Player player = Main.player[NPC.target];
            tsorcRevampAIs.FighterAI(NPC, 1, 0.03f, canTeleport: false, randomSound: SoundID.Mummy, soundFrequency: 1000, enragePercent: 0.5f, enrageTopSpeed: 2);
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            bool lowHP = NPC.life < NPC.lifeMax / 2;

            //MAKE SOUND WHEN JUMPING/HOVERING
            if (Main.rand.NextBool(12) && NPC.velocity.Y <= -1f)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.2f, Pitch = 0.1f }, NPC.Center);
            }

            // ===== MAGIC RING (breath) ATTACK — own clock: shrinking DustRing "magic ring" telegraph 360→480, then
            //       committed cursed-breath fire (breathTimer<0). Stationary channel; no pre-attack jump (see below). =====
            breathTimer++;
            // At low HP the magic-ring breath is fully disabled (every breath branch below is gated !lowHP). Without
            // this cap breathTimer runs away past 480 and breathActive (breathTimer > 360) stays TRUE forever, which
            // BOTH freezes the projectile machine (shotTimer pinned to 0) AND blocks the leech tongue (canStart needs
            // breathTimer <= 360) — i.e. the Basilisk stops attacking entirely once below half health. Keep it neutral.
            if (lowHP && breathTimer > 360) breathTimer = 0;
            if (breathTimer > 480 && Main.rand.NextBool(2) && shotTimer >= 0f && shotTimer <= 99f && !lowHP && lockedAttack == -1)
            {
                LockAttackAim(player);
                breathTimer = -60;
                shotTimer = -60f; // pause the projectile machine while the breath fires
            }
            if (breathTimer < 0) // committed: spew breath
            {
                NPC.velocity.X = 0f;
                NPC.direction = lockedFacingDirection;
                NPC.spriteDirection = lockedFacingDirection;
                if ((int)breathTimer % 30 == 0)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.3f, Pitch = 0.1f }, NPC.Center);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 breathVel = GetConeLimitedBreathVelocity(player, 9f) + Main.rand.NextVector2Circular(-1.5f, 1.5f);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + (5 * NPC.direction), NPC.Center.Y, breathVel.X, breathVel.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedBreathCollides>(), cursedBreathDamage, 0f, Main.myPlayer);
                    NPC.ai[3] = 0; // no teleporting mid-breath
                }
            }
            if (breathTimer > 360 && breathTimer <= 480 && !lowHP) // shrinking-ring telegraph
            {
                UsefulFunctions.DustRing(NPC.Center, (int)(48 * ((480 - breathTimer) / 120)), DustID.CursedTorch, 48, 4);
                Lighting.AddLight(NPC.Center, Color.GreenYellow.ToVector3() * 5);
            }
            if (breathTimer == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                shotTimer = -PostAttackDowntime;
            }
            bool breathTelegraph = breathTimer > 360 && breathTimer <= 480 && !lowHP;
            bool breathCommitted = breathTimer < 0;

            // ===== PROJECTILE ATTACKS (decide → dust → flash → commit → fire) =====
            // The breath owns the body while it winds up (DustRing) or fires — pause + cancel the projectile machine.
            bool breathActive = breathTimer > 360 || breathTimer < 0;
            if (breathActive)
            {
                shotTimer = 0f;
                lockedAttack = -1;
            }
            else
            {
                shotTimer++;
                // Roll + LOCK the attack once when the wind-up begins, so the dust/flash colour and the fired shot match.
                if (lockedAttack == -1 && shotTimer >= AtkDecide)
                {
                    lockedAttack = lowHP ? (Main.rand.NextBool(2) ? 3 : 2)
                                         : Main.rand.Next(4) switch { 0 => 0, 1 => 1, _ => 3 };
                }
            }
            bool atkActive = lockedAttack != -1;
            bool purpleAttack = lockedAttack == 0 || lockedAttack == 3; // lob + disrupter = purple; spit + final = green
            bool telegraphing = atkActive && shotTimer >= AtkDecide && shotTimer < AtkFlash;
            bool committed = atkActive && shotTimer >= AtkFlash;
            if (committed && lockedTargetPosition != Vector2.Zero)
            {
                NPC.direction = lockedFacingDirection;
                NPC.spriteDirection = lockedFacingDirection;
            }
            if (telegraphing && lockedAttack == 3)
            {
                CreateDisrupterSpacing(player);
            }

            // Dust telegraph (interruptible) — colour matches the locked attack.
            if (telegraphing && Main.rand.NextBool(3))
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, purpleAttack ? DustID.CursedTorch : DustID.GemEmerald, NPC.velocity.X, NPC.velocity.Y);
                Lighting.AddLight(NPC.Center, (purpleAttack ? Color.Purple : Color.GreenYellow).ToVector3() * 0.6f);
            }
            // Colored flash at the commit instant — hyperarmor begins here.
            if (atkActive && (int)shotTimer == AtkFlash)
            {
                LockAttackAim(player);
                if (lockedAttack == 3)
                {
                    disrupterPattern = Main.rand.Next(2);
                    disrupterVerticalTargets = Main.rand.NextBool(2);
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(purpleAttack ? Color.Purple : Color.GreenYellow));
                }
            }
            // Fire (committed). Lob sprays AtkFire..AtkSprayEnd every 8t; the others are single shots at AtkFire.
            if (committed && Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (lockedAttack == 0)
                {
                    if (shotTimer >= AtkFire && shotTimer <= AtkSprayEnd && ((int)shotTimer - AtkFire) % 8 == 0)
                    {
                        FireLob(player);
                    }
                }
                else if (lockedAttack == 3 && shotTimer >= AtkFire && shotTimer <= AtkFire + DisrupterShotInterval * (DisrupterShotCount - 1) && ((int)shotTimer - AtkFire) % DisrupterShotInterval == 0)
                {
                    FireDisrupter(player, ((int)shotTimer - AtkFire) / DisrupterShotInterval);
                }
                else if ((int)shotTimer == AtkFire)
                {
                    if (lockedAttack == 1) FireSpit(player);
                    else if (lockedAttack == 2) FireFinal(player);
                }
            }
            // Reset when the shot/spray is done (single shots keep a short committed recovery tail).
            if (atkActive && shotTimer >= GetAttackEndTime())
            {
                shotTimer = -PostAttackDowntime;
                lockedAttack = -1;
                lockedTargetPosition = Vector2.Zero;
            }

            // Attack-phase flags → poise (telegraph = stagger-cancellable; committed = hyperarmor).
            globalNPC.AttackTelegraphing = telegraphing || breathTelegraph;
            globalNPC.AttackCommitted = committed || breathCommitted;
            // The magic-ring breath is a stationary channel — veto the pre-attack jump for it (projectile attacks still jump).
            globalNPC.SuppressPreAttackJump = breathActive;

            // ===== Shift-toward-player lunge (movement flourish; unchanged) =====
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (Main.rand.NextBool(80) && NPC.Distance(player.Center) > 200)
                {
                    Lighting.AddLight(NPC.Center, Color.Red.ToVector3() * 3f);
                    chargeDamageFlag = true;
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float rotation = (float)Math.Atan2(vector8.Y - (player.position.Y + (player.height * 0.5f)), vector8.X - (player.position.X + (player.width * 0.5f)));
                    NPC.velocity.X = (float)(Math.Cos(rotation) * 10) * -1;
                    NPC.velocity.Y = (float)(Math.Sin(rotation) * 10) * -1;
                    NPC.netUpdate = true;
                }
                if (chargeDamageFlag)
                {
                    Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 5f);
                    NPC.damage = 35;
                    chargeDamage++;
                }
                if (chargeDamage >= 70)
                {
                    chargeDamageFlag = false;
                    NPC.damage = 30;
                    chargeDamage = 0;
                }
            }
        }

        // Fire helpers — called only server-side from the committed window. The lob needs vertical clearance to arc.
        private void FireLob(Player player)
        {
            for (int i = 0; i < 15; i++)
            {
                if (UsefulFunctions.IsTileReallySolid((int)NPC.Center.X / 16, ((int)NPC.Center.Y / 16) - i)) return;
            }
            Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, GetLockedTargetPosition(player), 5);
            speed.Y += Main.rand.NextFloat(-2f, -6f);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.DD2DrakinShot, bioSpitDamage, 0f, Main.myPlayer);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);
        }

        private void FireSpit(Player player)
        {
            Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, GetLockedTargetPosition(player), 9);
            int p = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBioSpitBall>(), bioSpitDamage, 0f, Main.myPlayer);
            Main.projectile[p].timeLeft = 300;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);
        }

        private void FireFinal(Player player)
        {
            Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, GetLockedTargetPosition(player), 10);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBioSpitBall>(), bioSpitDamage, 0f, Main.myPlayer);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.1f }, NPC.Center);
        }

        private void FireDisrupter(Player player, int shotIndex)
        {
            Vector2 targetPosition = GetDisrupterTargetPosition(player, shotIndex);
            Vector2 velocity = GetDisrupterLaunchVelocity(targetPosition, shotIndex);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, velocity, ModContent.ProjectileType<Projectiles.Enemy.HypnoticDisrupter>(), hypnoticDisruptorDamage, 5f, Main.myPlayer, targetPosition.X, -targetPosition.Y);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.6f, Pitch = -0.5f }, player.Center);
        }

        private void CreateDisrupterSpacing(Player player)
        {
            if (NPC.Distance(player.Center) >= DisrupterMinSpacing)
            {
                return;
            }

            int faceDirection = player.Center.X >= NPC.Center.X ? 1 : -1;
            int retreatDirection = -faceDirection;
            NPC.direction = faceDirection;
            NPC.spriteDirection = faceDirection;
            NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, retreatDirection * 3.2f, 0.2f);

            if (Main.netMode != NetmodeID.MultiplayerClient && NPC.velocity.Y == 0f && (int)shotTimer == AtkDecide + 2)
            {
                NPC.velocity.X = retreatDirection * 4.5f;
                NPC.velocity.Y = -4.5f;
                NPC.netUpdate = true;
            }
        }

        private Vector2 GetDisrupterTargetPosition(Player player, int shotIndex)
        {
            Vector2 targetPosition = GetLockedTargetPosition(player);
            if (disrupterPattern == 0)
            {
                return targetPosition;
            }

            float offset = (shotIndex - 1) * 60f;
            return disrupterVerticalTargets ? targetPosition + new Vector2(0f, offset) : targetPosition + new Vector2(offset, 0f);
        }

        private Vector2 GetDisrupterLaunchVelocity(Vector2 targetPosition, int shotIndex)
        {
            const float speed = 4f;
            if (disrupterPattern == 0)
            {
                float baseAngle = lockedFacingDirection >= 0 ? -MathHelper.PiOver4 : -3f * MathHelper.PiOver4;
                float offset = (shotIndex - 1) * MathHelper.ToRadians(10f);
                return (baseAngle + offset).ToRotationVector2() * speed;
            }

            float angle = lockedFacingDirection >= 0 ? MathHelper.ToRadians(60f) : MathHelper.ToRadians(120f);
            return angle.ToRotationVector2() * speed;
        }

        private int GetAttackEndTime()
        {
            if (lockedAttack == 0)
            {
                return AtkSprayEnd;
            }
            if (lockedAttack == 3)
            {
                return AtkFire + DisrupterShotInterval * (DisrupterShotCount - 1) + 10;
            }
            return AtkFire + 10;
        }

        private void LockAttackAim(Player player)
        {
            lockedTargetPosition = player.Center;
            lockedFacingDirection = player.Center.X >= NPC.Center.X ? 1 : -1;
            NPC.direction = lockedFacingDirection;
            NPC.spriteDirection = lockedFacingDirection;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.netUpdate = true;
            }
        }

        private Vector2 GetLockedTargetPosition(Player player)
        {
            return lockedTargetPosition == Vector2.Zero ? player.Center : lockedTargetPosition;
        }

        private Vector2 GetConeLimitedBreathVelocity(Player player, float speed)
        {
            Vector2 toPlayer = player.Center - NPC.Center;
            if (toPlayer == Vector2.Zero)
            {
                toPlayer = Vector2.UnitX * lockedFacingDirection;
            }

            float facingAngle = lockedFacingDirection >= 0 ? 0f : MathHelper.Pi;
            float aimOffset = MathHelper.WrapAngle(toPlayer.ToRotation() - facingAngle);
            float clampedAimOffset = MathHelper.Clamp(aimOffset, -BreathTrackingHalfCone, BreathTrackingHalfCone);
            return (facingAngle + clampedAimOffset).ToRotationVector2() * speed;
        }

        #region Find Frame
        public override void FindFrame(int currentFrame)
        {
            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            if (NPC.velocity.Y == 0f)
            {
                if (NPC.direction == 1)
                {
                    NPC.spriteDirection = 1;
                }
                if (NPC.direction == -1)
                {
                    NPC.spriteDirection = -1;
                }
                if (NPC.velocity.X == 0f)
                {
                    NPC.frame.Y = 0;
                    NPC.frameCounter = 0.0;
                }
                else
                {
                    NPC.frameCounter += (double)(Math.Abs(NPC.velocity.X) * 2f);
                    NPC.frameCounter += 1.0;
                    if (NPC.frameCounter > 6.0)
                    {
                        NPC.frame.Y = NPC.frame.Y + num;
                        NPC.frameCounter = 0.0;
                    }
                    if (NPC.frame.Y / num >= Main.npcFrameCount[NPC.type])
                    {
                        NPC.frame.Y = num * 2;
                    }
                }
            }
            else
            {
                NPC.frameCounter = 0.0;
                NPC.frame.Y = num;
                NPC.frame.Y = 0;
            }
        }

        #endregion

        #region Debuffs
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(ModContent.BuffType<CurseBuildup>(), 18000, false); //-20 life if counter hits 100
            target.AddBuff(BuffID.Poisoned, 10 * 60, false);

            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.BrokenArmor, 10 * 60, false);
            }
            if (Main.rand.NextBool(4))
            {
                target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 300 * 60, false);
            }
        }
        #endregion

        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 1").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 2").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 3").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 2").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 1").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 3").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 2").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 3").Type, 1.1f);
                for (int i = 0; i < 10; i++)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Blood Splat").Type, 1.1f);
                }
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.GreaterHealingPotion, 25));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BloodredMossClump>(), 2, 1, 2));
            IItemDropRule hmCondition = new LeadingConditionRule(new Conditions.IsHardmode());
            hmCondition.OnSuccess(ItemDropRule.Common(ItemID.SoulofNight, 3));
            npcLoot.Add(hmCondition);
        }
    }
}
