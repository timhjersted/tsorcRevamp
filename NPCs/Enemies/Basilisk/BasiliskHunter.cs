using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.NPCs.Enemies.Basilisk
{
    class BasiliskHunter : ModNPC, IStaggerable
    {
        int cursedBreathDamage = 20;
        int cursedFlamesDamage = 20;
        int darkExplosionDamage = 45;
        int disruptDamage = 43;
        int bioSpitDamage = 45;
        int bioSpitfinalDamage = 50;
        int leechTongueDamage = 20;
        int leechTongueTimer;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 12;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Venom] = true;
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 3;
            AnimationType = 28;
            NPC.damage = 48;
            NPC.defense = 80; //was 105
            NPC.height = 46;
            NPC.width = 38;
            NPC.lifeMax = 2500;
            NPC.HitSound = SoundID.NPCHit20;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 10000; // life / 2.5 in SHM : was 882
            NPC.lavaImmune = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.BasiliskHunterBanner>();

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.MaxJumpPower = 11f;
            globalNPC.MaxJumpBoost = 7f;
            globalNPC.NavSearchRadius = 90; // Phase 2: SmartFighter4AI movement
            globalNPC.CanUseRopes = true;
            globalNPC.CanDoubleJump = true;
            globalNPC.DoubleJumpPower = 7f;
            // Step 6 beast levers: lurk (Wander) around where it lost the player when it gives up.
            globalNPC.PatrolMode = NPCs.PatrolMode.Wander;
            globalNPC.PatrolAnchorSource = NPCs.PatrolAnchorSource.GiveUpLocation;
            // Evasive on-hit: hop/dash away or i-frame quick-step.
            EvasiveProfile.Basilisk(globalNPC);
            // Poise (a stagger cancels the wind-up) + knockback flinch are tuned centrally in
            // tsorcRevampGlobalNPC.PopulatePoiseProfiles() (GlobalNPC.cs) - not here.
            EvasiveProfile.BasiliskHunterAttackJumps(globalNPC);
            globalNPC.KiteRangeMin = 9f;
            globalNPC.KiteRangeMax = 18f;
            globalNPC.KiteLooseness = 0.15f;
        }
        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
            cursedBreathDamage = (int)(cursedBreathDamage * tsorcRevampWorld.SHMScale);
            darkExplosionDamage = (int)(darkExplosionDamage * tsorcRevampWorld.SHMScale);
            disruptDamage = (int)(disruptDamage * tsorcRevampWorld.SHMScale);
            bioSpitDamage = (int)(bioSpitDamage * tsorcRevampWorld.SHMScale);
            bioSpitfinalDamage = (int)(bioSpitfinalDamage * tsorcRevampWorld.SHMScale);
            leechTongueDamage = (int)(leechTongueDamage * tsorcRevampWorld.SHMScale);
        }


        public Player player
        {
            get => Main.player[NPC.target];
        }


        //PROJECTILE HIT LOGIC
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

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player player = spawnInfo.Player;

            bool FrozenOcean = spawnInfo.SpawnTileX > (Main.maxTilesX - 800);
            bool Ocean = spawnInfo.SpawnTileX < 800 || FrozenOcean;
            float chance = 0;

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

            if (tsorcRevampWorld.SuperHardMode)
            {
                if (((player.ZoneMeteor || player.ZoneCorrupt || player.ZoneCrimson || player.ZoneUndergroundDesert) && (player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight)) && !player.ZoneDungeon)
                {
                    chance = 0.20f; // was .30
                }
                else
                {
                    if (player.ZoneOverworldHeight && (player.ZoneMeteor || player.ZoneCorrupt || player.ZoneCrimson || player.ZoneHallow) && !Ocean && !Main.dayTime)
                    {
                        chance = 0.10f;
                    }
                }
            }
            if (Main.bloodMoon)
            {
                chance *= 2;
            }

            return chance;
        }
        #endregion

        //float breathTimer = 0;
        //float projTimer = 0;
        //float disruptTimer = 0;
        //bool chargeDamageFlag;
        //int chargeDamage;
        bool breath;
        int breathCD = 120;

        //float boredResetT;
        //float boredTimer;
        //float bReset;
        //float customAi1;
        //float drowningRisk;
        //float drownTimer;
        //float drownTimerMax;
        //float tBored;

        private const int BreathWindupStart = 540;
        private const int BreathWindupEnd = 635;
        private const int BreathCommitTime = 636;
        private const int PostAttackDowntime = 90;
        private const int AtkDecide = 85;    // roll + lock the attack; dust telegraph (interruptible) starts
        private const int AtkFlash = 100;    // colored TelegraphFlash spawns = the commit instant (hyperarmor begins)
        private const int AtkFire = 115;     // projectile(s) launch
        private const int AtkSprayEnd = 155; // lob/final sprays AtkFire..AtkSprayEnd; single shots reset shortly after
        private const float DisrupterMinSpacing = 250f;
        private const int DisrupterShotInterval = 30;
        private const int DisrupterShotCount = 3;
        private int lockedAttack = -1;       // -1 none; 0 lob, 1 spit, 2 wounded spit, 3 desperate spray, 4 disrupter
        private Vector2 lockedTargetPosition = Vector2.Zero;
        private int lockedFacingDirection = 1;
        private int disrupterPattern = 0;
        private bool disrupterVerticalTargets = false;

        public void OnStagger(NPC npc)
        {
            NPC.localAI[1] = 0f;
            lockedAttack = -1;
            lockedTargetPosition = Vector2.Zero;
            leechTongueTimer = 0;
            if (!breath)
            {
                NPC.localAI[2] = 0f;
            }
        }



        public override void AI()  //  warrior ai
        {
            bool canStartLeechTongue = lockedAttack == -1 && !breath && NPC.localAI[2] < BreathWindupStart;
            if (BasiliskLeechTongueAttack.Update(NPC, ref leechTongueTimer, 500, leechTongueDamage, canStartLeechTongue))
            {
                return;
            }

            tsorcRevampAIs.FighterAI(NPC, 1, .03f, 0.2f, false, 10, false, SoundID.Mummy, 1000, 0.3f, 1.5f, true);

            Player player = Main.player[NPC.target];
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            bool desperate = NPC.life <= NPC.lifeMax / 5;
            bool wounded = NPC.life <= NPC.lifeMax / 5 * 2;

            if (Main.rand.NextBool(12) && NPC.velocity.Y <= -1f)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.4f, Pitch = 0.1f }, NPC.Center);
            }

            #region melee movement
            bool freeToFlourish = lockedAttack == -1 && !breath && NPC.localAI[2] < BreathWindupStart;

            //CHANCE TO JUMP FORWARDS 
            if (freeToFlourish && NPC.Distance(player.Center) > 250 && NPC.velocity.Y == 0f && Main.rand.NextBool(28) && !desperate)
            {
                int dust2 = Dust.NewDust(NPC.position, NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.Red);
                Main.dust[dust2].noGravity = true;

                NPC.TargetClosest(true);
                NPC.velocity.Y = Main.rand.NextFloat(-10f, -3f);
                NPC.velocity.X += NPC.direction * Main.rand.NextFloat(3f, 2f);
                NPC.netUpdate = true;
            }
            //CHANCE TO DASH STEP FORWARDS 
            else if (freeToFlourish && NPC.Distance(player.Center) > 370 && NPC.velocity.Y == 0f && Main.rand.NextBool(23) && !desperate)
            {
                int dust3 = Dust.NewDust(NPC.position, NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.Red);
                Main.dust[dust3].noGravity = true;

                NPC.velocity.Y = Main.rand.NextFloat(-10f, -3f);
                NPC.velocity.X += NPC.direction * Main.rand.NextFloat(6f, 5f);
                NPC.netUpdate = true;
            }
            #endregion

            #region Charge
            //CHARGE UNTIL DESPERATE PHASE
            if (Main.netMode != NetmodeID.MultiplayerClient && freeToFlourish && NPC.localAI[1] >= 95 && Main.rand.NextBool(30) && NPC.Distance(player.Center) > 250 && NPC.Distance(player.Center) < 500 && !desperate)
            {
                Lighting.AddLight(NPC.Center, Color.LightYellow.ToVector3() * 3f);
                int dust2 = Dust.NewDust(NPC.position, NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.Blue);
                Main.dust[dust2].noGravity = true;

                Vector2 vector8 = NPC.position + new Vector2(NPC.width * 0.5f, NPC.height / 2);
                float rotation = (float)Math.Atan2(vector8.Y - (player.position.Y + (player.height * 0.5f)), vector8.X - (player.position.X + (player.width * 0.5f)));
                NPC.velocity.X += NPC.direction * Main.rand.NextFloat(3f, 2f);
                NPC.velocity.Y = (float)(Math.Sin(rotation) * 10) * -1;
                NPC.ai[1] = 1f;
                NPC.damage = 80;
                NPC.localAI[1] = 1f;
                NPC.netUpdate = true;
            }
            #endregion

            #region Projectiles
            NPC.localAI[1]++;
            NPC.localAI[2]++;

            // ===== CURSED BREATH ATTACK - own clock: yellow/orange wind-up, then committed flame channel. =====
            bool breathTelegraph = NPC.localAI[2] >= BreathWindupStart + 1 && NPC.localAI[2] <= BreathWindupEnd && NPC.Distance(player.Center) > 20 && !desperate;
            if (breathTelegraph)
            {
                if (NPC.localAI[2] == BreathWindupStart + 1)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item6 with { Volume = 0.01f, Pitch = -0.5f }, NPC.Center);
                }

                NPC.velocity = Vector2.Zero;
                Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 3f);

                if (Main.rand.NextBool(2))
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 21, 0, 0, 50, Color.Yellow, 1f);
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 21, 0, 0, 50, Color.Yellow, 1f);
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 21, 0, 0, 50, Color.Yellow, 2f);
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 21, 0, 0, 50, Color.Yellow, 2f);
                }
            }
            if (NPC.localAI[2] == BreathCommitTime && !desperate)
            {
                breath = true;
                lockedAttack = -1;
                NPC.localAI[1] = 0f;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit30 with { Volume = 0.8f, Pitch = -0.3f }, NPC.Center);
            }
            if (breath)
            {
                NPC.velocity = Vector2.Zero;
                Lighting.AddLight(NPC.Center, Color.YellowGreen.ToVector3() * 3f);

                if (breathCD % 20 == 0)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.3f, Pitch = 0.1f }, NPC.Center);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + (5 * NPC.direction), NPC.Center.Y, NPC.velocity.X * 3f + Main.rand.Next(-2, 2), NPC.velocity.Y * 3f + Main.rand.Next(-2, 2), ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedBreath>(), cursedFlamesDamage, 0f, Main.myPlayer);
                }
                NPC.netUpdate = true;

                breathCD--;
                if (breathCD == 120 || breathCD == 60)
                {
                    BreathSideTeleport(player);
                }

                if (breathCD <= 0)
                {
                    breath = false;
                    breathCD = 160;
                    NPC.localAI[2] = -PostAttackDowntime;
                    NPC.localAI[1] = -PostAttackDowntime;
                    NPC.ai[3] = 0; //Reset bored counter. No teleporting after breath attack
                }
            }

            bool breathActive = breathTelegraph || breath;
            if (breathActive)
            {
                lockedAttack = -1;
            }
            else if (lockedAttack == -1 && NPC.localAI[1] >= AtkDecide)
            {
                lockedAttack = RollAttack(player, desperate, wounded);
                NPC.netUpdate = true;
            }

            bool atkActive = lockedAttack != -1;
            bool telegraphing = atkActive && NPC.localAI[1] >= AtkDecide && NPC.localAI[1] < AtkFlash;
            bool committed = atkActive && NPC.localAI[1] >= AtkFlash;
            bool purpleAttack = lockedAttack == 0 || lockedAttack == 4;
            Color telegraphColor = lockedAttack == 3 ? Color.OrangeRed : (purpleAttack ? Color.Purple : Color.GreenYellow);
            if (committed && lockedTargetPosition != Vector2.Zero)
            {
                NPC.direction = lockedFacingDirection;
                NPC.spriteDirection = lockedFacingDirection;
            }
            if (telegraphing && lockedAttack == 4)
            {
                CreateDisrupterSpacing(player);
            }

            if (telegraphing && Main.rand.NextBool(3))
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, purpleAttack ? DustID.CursedTorch : DustID.GemEmerald, NPC.velocity.X, NPC.velocity.Y);
                Dust.NewDust(NPC.position, NPC.width, NPC.height, lockedAttack == 3 ? DustID.Torch : DustID.GemEmerald, NPC.velocity.X, NPC.velocity.Y);
                Lighting.AddLight(NPC.Center, telegraphColor.ToVector3() * 0.6f);
            }

            if (atkActive && (int)NPC.localAI[1] == AtkFlash)
            {
                LockAttackAim(player);
                if (lockedAttack == 4)
                {
                    disrupterPattern = Main.rand.Next(4);
                    disrupterVerticalTargets = Main.rand.NextBool(2);
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(telegraphColor));
                }
            }

            if (committed && Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (lockedAttack == 0)
                {
                    // "Poison Lob" - high-arc drakin shot spray.
                    if (NPC.localAI[1] >= AtkFire && NPC.localAI[1] <= AtkSprayEnd && ((int)NPC.localAI[1] - AtkFire) % 8 == 0)
                    {
                        FireLob(player);
                    }
                }
                else if (lockedAttack == 3)
                {
                    // "Desperate Bio Spray" - erratic low-health spit volley.
                    if (NPC.localAI[1] >= AtkFire && NPC.localAI[1] <= AtkSprayEnd && ((int)NPC.localAI[1] - AtkFire) % 10 == 0)
                    {
                        FireFinalSpit(player);
                    }
                }
                else if (lockedAttack == 4 && NPC.localAI[1] >= AtkFire && NPC.localAI[1] <= AtkFire + DisrupterShotInterval * (DisrupterShotCount - 1) && ((int)NPC.localAI[1] - AtkFire) % DisrupterShotInterval == 0)
                {
                    FireDisrupter(player, ((int)NPC.localAI[1] - AtkFire) / DisrupterShotInterval);
                }
                else if ((int)NPC.localAI[1] == AtkFire)
                {
                    if (lockedAttack == 1) FireSpit(player, false);
                    else if (lockedAttack == 2) FireSpit(player, true);
                }
            }

            if (atkActive && NPC.localAI[1] >= GetAttackEndTime())
            {
                NPC.localAI[1] = -PostAttackDowntime;
                lockedAttack = -1;
                lockedTargetPosition = Vector2.Zero;
            }

            // Attack-phase flags -> poise (telegraph = stagger-cancellable; committed = hyperarmor).
            globalNPC.AttackTelegraphing = telegraphing || breathTelegraph;
            globalNPC.AttackCommitted = committed || breath;
            // The breath is a stationary channel - veto the pre-attack jump for it (projectile attacks still jump).
            globalNPC.SuppressPreAttackJump = breathActive;
            #endregion
        }

        private int RollAttack(Player player, bool desperate, bool wounded)
        {
            if (desperate)
            {
                return 3;
            }

            bool canDisrupt = NPC.Distance(player.Center) > 160 && Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1);
            if (wounded)
            {
                return Main.rand.Next(4) switch
                {
                    0 => 2,
                    1 or 2 when canDisrupt => 4,
                    _ => 0
                };
            }

            return Main.rand.Next(4) switch
            {
                0 => 0,
                1 => 1,
                2 or 3 when canDisrupt => 4,
                _ => 1
            };
        }

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

        private void FireSpit(Player player, bool wounded)
        {
            // "Bio Spit" / "Wounded Bio Spit" - aimed toxic glob.
            Vector2 targetPosition = GetLockedTargetPosition(player);
            if (!Collision.CanHitLine(NPC.Center, 0, 0, targetPosition, 0, 0)) return;

            Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, 10);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBioSpitBall>(), bioSpitDamage, 5f, Main.myPlayer);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = wounded ? 0.5f : -0.5f }, NPC.Center);
        }

        private void FireFinalSpit(Player player)
        {
            Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, GetLockedTargetPosition(player), 8);
            speed += Main.rand.NextVector2Circular(-6, -2);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBioSpitBall>(), bioSpitfinalDamage, 5f, Main.myPlayer);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.1f, Pitch = 0.2f }, NPC.Center);
        }

        private void FireDisrupter(Player player, int shotIndex)
        {
            Vector2 targetPosition = GetDisrupterTargetPosition(player, shotIndex);
            Vector2 projectileVelocity = GetDisrupterLaunchVelocity(shotIndex);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projectileVelocity, ModContent.ProjectileType<Projectiles.Enemy.HypnoticDisrupter>(), disruptDamage, 5f, Main.myPlayer, targetPosition.X, -targetPosition.Y);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.8f, Pitch = -0.2f }, NPC.Center);
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

            if (Main.netMode != NetmodeID.MultiplayerClient && NPC.velocity.Y == 0f && (int)NPC.localAI[1] == AtkDecide + 2)
            {
                NPC.velocity.X = retreatDirection * 4.5f;
                NPC.velocity.Y = -4.5f;
                NPC.netUpdate = true;
            }
        }

        private Vector2 GetDisrupterTargetPosition(Player player, int shotIndex)
        {
            Vector2 targetPosition = GetLockedTargetPosition(player);
            float offset = (shotIndex - 1) * 60f;

            return disrupterPattern switch
            {
                1 => disrupterVerticalTargets ? targetPosition + new Vector2(0f, offset) : targetPosition + new Vector2(offset, 0f),
                2 => targetPosition + new Vector2(offset * lockedFacingDirection, -Math.Abs(offset)),
                3 => targetPosition + new Vector2(offset * 1.5f, -80f),
                _ => targetPosition
            };
        }

        private Vector2 GetDisrupterLaunchVelocity(int shotIndex)
        {
            const float speed = 4f;
            float angle = disrupterPattern switch
            {
                0 => (lockedFacingDirection >= 0 ? -MathHelper.PiOver4 : -3f * MathHelper.PiOver4) + (shotIndex - 1) * MathHelper.ToRadians(10f),
                1 => lockedFacingDirection >= 0 ? MathHelper.ToRadians(60f) : MathHelper.ToRadians(120f),
                2 => (lockedFacingDirection >= 0 ? 0f : MathHelper.Pi) + (shotIndex - 1) * MathHelper.ToRadians(20f),
                3 => -MathHelper.PiOver2 + (shotIndex - 1) * MathHelper.ToRadians(18f),
                _ => 0f
            };
            return angle.ToRotationVector2() * speed;
        }

        private int GetAttackEndTime()
        {
            if (lockedAttack == 0 || lockedAttack == 3)
            {
                return AtkSprayEnd;
            }
            if (lockedAttack == 4)
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

        private void BreathSideTeleport(Player player)
        {
            int side = NPC.Center.X < player.Center.X ? 1 : -1;
            float[] distances = new float[] { 120f, 160f, 200f };
            int[] verticalOffsets = new int[] { 0, -3, 3, -6, 6 };

            for (int distanceIndex = 0; distanceIndex < distances.Length; distanceIndex++)
            {
                for (int verticalIndex = 0; verticalIndex < verticalOffsets.Length; verticalIndex++)
                {
                    Vector2 searchPoint = player.Center + new Vector2(side * distances[distanceIndex], verticalOffsets[verticalIndex] * 16f);
                    if (TryFindBreathTeleportPosition(searchPoint, player, out Vector2 teleportPosition))
                    {
                        SpawnBreathTeleportDust(NPC.Center);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                        NPC.position = teleportPosition;
                        NPC.velocity = Vector2.Zero;
                        NPC.TargetClosest(true);
                        NPC.netUpdate = true;
                        SpawnBreathTeleportDust(NPC.Center);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                        return;
                    }
                }
            }

            tsorcRevampAIs.TeleportImmediately(NPC, 15, true);
        }

        private bool TryFindBreathTeleportPosition(Vector2 searchPoint, Player player, out Vector2 teleportPosition)
        {
            int tileX = (int)(searchPoint.X / 16f);
            int startTileY = (int)(searchPoint.Y / 16f) - 8;

            for (int yOffset = 0; yOffset <= 18; yOffset++)
            {
                int groundTileY = startTileY + yOffset;
                if (!WorldGen.InWorld(tileX, groundTileY, 10) || !UsefulFunctions.IsTileReallySolid(tileX, groundTileY))
                {
                    continue;
                }

                Vector2 candidate = new Vector2(tileX * 16 - NPC.width / 2f, groundTileY * 16 - NPC.height);
                if (Collision.SolidCollision(candidate, NPC.width, NPC.height))
                {
                    continue;
                }

                if (!Collision.CanHit(candidate, NPC.width, NPC.height, player.position, player.width, player.height))
                {
                    continue;
                }

                teleportPosition = candidate;
                return true;
            }

            teleportPosition = Vector2.Zero;
            return false;
        }

        private void SpawnBreathTeleportDust(Vector2 center)
        {
            for (int i = 0; i < 18; i++)
            {
                Dust dust = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(NPC.width / 2f, NPC.height / 2f), DustID.CursedTorch, Main.rand.NextVector2Circular(3f, 3f), 100, Color.GreenYellow, 1.5f);
                dust.noGravity = true;
            }
        }

        #region FindFrame
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
            player.AddBuff(36, 600, false); //broken armor
            player.AddBuff(20, 1800, false); //poisoned
            player.AddBuff(ModContent.BuffType<CurseBuildup>(), 18000, false); //-20 life if counter hits 100

            if (Main.rand.NextBool(2))
            {

                player.AddBuff(ModContent.BuffType<BrokenSpirit>(), 1800, false);

            }


        }
        #endregion


        public override void OnKill()
        {
            if (NPC.life <= 0)
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

                    for (int i = 0; i < 10; i++)
                    {
                        Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Blood Splat").Type, 1.1f);
                    }
                }
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CursedSoul>(), 1, 3, 6));
            npcLoot.Add(new CommonDrop(ItemID.GreaterHealingPotion, 100, 1, 1, 8));
        }
    }
}
