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
        }

        float breathTimer = 60;

        // Deterministic projectile-attack timing (decide → dust telegraph → colored flash at commit → fire). The
        // attack is rolled+locked once at AtkDecide so the flash colour and the shot match. See attack-phase tagging.
        private const int AtkDecide = 60;    // roll + lock the attack; dust telegraph (interruptible) starts
        private const int AtkFlash = 85;     // colored TelegraphFlash spawns = the commit instant (hyperarmor begins)
        private const int AtkFire = 110;     // projectile(s) launch
        private const int AtkSprayEnd = 140; // lob sprays AtkFire..AtkSprayEnd; single shots reset ~10t after AtkFire
        private const int PostAttackDowntime = 120;
        private int lockedAttack = -1;       // -1 none; 0 lob(purple), 1 spit(green), 2 final(green,low-HP), 3 disrupter(purple)

        float shotTimer;
        int chargeDamage = 0;
        bool chargeDamageFlag = false;
        int cursedBreathDamage = 13;
        int darkExplosionDamage = 18;
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
            tsorcRevampAIs.EvasiveOnHit(NPC, true);
        }
        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
        }

        // Stagger (poise break) cancels a telegraphing attack — reset the timers to neutral. A committed attack is
        // hyperarmored, so a stagger can't happen during one; this only ever fires during a telegraph/neutral.
        public void OnStagger(NPC npc)
        {
            shotTimer = 0f;
            lockedAttack = -1;
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
            if (breathTimer > 480 && Main.rand.NextBool(2) && shotTimer >= 0f && shotTimer <= 99f && !lowHP && lockedAttack == -1)
            {
                breathTimer = -60;
                shotTimer = -60f; // pause the projectile machine while the breath fires
            }
            if (breathTimer < 0) // committed: spew breath
            {
                NPC.velocity.X = 0f;
                if ((int)breathTimer % 30 == 0)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.3f, Pitch = 0.1f }, NPC.Center);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 breathVel = UsefulFunctions.Aim(NPC.Center, player.Center, 9) + Main.rand.NextVector2Circular(-1.5f, 1.5f);
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
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, 0, 0, ModContent.ProjectileType<Projectiles.Enemy.DarkExplosion>(), darkExplosionDamage, 0f, Main.myPlayer);
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
                    lockedAttack = lowHP ? (Main.rand.NextBool(4) ? 3 : 2)
                                         : Main.rand.Next(3) switch { 0 => 0, 1 => 1, _ => 3 };
                }
            }
            bool atkActive = lockedAttack != -1;
            bool purpleAttack = lockedAttack == 0 || lockedAttack == 3; // lob + disrupter = purple; spit + final = green
            bool telegraphing = atkActive && shotTimer >= AtkDecide && shotTimer < AtkFlash;
            bool committed = atkActive && shotTimer >= AtkFlash;

            // Dust telegraph (interruptible) — colour matches the locked attack.
            if (telegraphing && Main.rand.NextBool(3))
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, purpleAttack ? DustID.CursedTorch : DustID.GemEmerald, NPC.velocity.X, NPC.velocity.Y);
                Lighting.AddLight(NPC.Center, (purpleAttack ? Color.Purple : Color.GreenYellow).ToVector3() * 0.6f);
            }
            // Colored flash at the commit instant — hyperarmor begins here.
            if (atkActive && (int)shotTimer == AtkFlash && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(purpleAttack ? Color.Purple : Color.GreenYellow));
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
                else if ((int)shotTimer == AtkFire)
                {
                    if (lockedAttack == 1) FireSpit(player);
                    else if (lockedAttack == 2) FireFinal(player);
                    else if (lockedAttack == 3) FireDisrupter(player);
                }
            }
            // Reset when the shot/spray is done (single shots keep a short committed recovery tail).
            if (atkActive && shotTimer >= (lockedAttack == 0 ? AtkSprayEnd : AtkFire + 10))
            {
                shotTimer = -PostAttackDowntime;
                lockedAttack = -1;
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
            Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, player.Center, 5);
            speed.Y += Main.rand.NextFloat(-2f, -6f);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.DD2DrakinShot, bioSpitDamage, 0f, Main.myPlayer);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);
        }

        private void FireSpit(Player player)
        {
            Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, player.Center, 9);
            int p = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBioSpitBall>(), bioSpitDamage, 0f, Main.myPlayer);
            Main.projectile[p].timeLeft = 300;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);
        }

        private void FireFinal(Player player)
        {
            Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, player.Center, 10);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBioSpitBall>(), bioSpitDamage, 0f, Main.myPlayer);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.1f }, NPC.Center);
        }

        private void FireDisrupter(Player player)
        {
            Vector2 vel = UsefulFunctions.BallisticTrajectory(NPC.Center, player.Center, 4f, 1.06f, true, true);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, ModContent.ProjectileType<Projectiles.Enemy.HypnoticDisrupter>(), hypnoticDisruptorDamage, 5f, Main.myPlayer);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.6f, Pitch = -0.5f }, player.Center);
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
