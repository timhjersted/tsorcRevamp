using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.GhostWyvernMage
{
    [AutoloadBossHead]
    class WyvernMageShadow : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 3;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn2] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 10;
            AnimationType = 29;
            NPC.aiStyle = 0;
            NPC.damage = 0;
            NPC.defense = 56;
            NPC.height = 56;
            NPC.scale = 1.05f;
            NPC.timeLeft = 22500;
            NPC.lifeMax = 250000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.noGravity = true;
            NPC.noTileCollide = false;
            NPC.boss = true;
            NPC.lavaImmune = true;
            NPC.value = 660000;
            NPC.rarity = 41;
            NPC.width = 35;
            NPC.knockBackResist = 0f;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.WyvernMageShadow.DespawnHandler"), Color.DarkCyan, DustID.Demonite);
            nextWarpPoint = Main.rand.NextVector2CircularEdge(320, 320);
        }

        int frozenSawDamage = 50;
        int lightningDamage = 62;
        int plasmaDamage = 50;
        int lifeTimer = 0;
        int holdTimer = 0;
        int desperationTimer = 0;
        int lightningDelayTimer = -1;
        Vector2 delayedTargetPosition;
        int deathTimer = 0;
        int mageShadowTimer = 0;

        // When this hits 5, the boss fires an orb and resets it back to 0. Only happens right at the start of its teleport.
        public int OrbTimer
        {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }

        // When this hits 200 (120 if dragon is dead) the boss teleports
        public int TeleportTimer
        {
            get => (int)NPC.ai[1];
            set => NPC.ai[1] = value;
        }

        // Counts up each time the boss fires an orb.
        public int ShotCount
        {
            get => (int)NPC.ai[2];
            set => NPC.ai[2] = value;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.WitheredWeapon, 3 * 60, false);
            target.AddBuff(BuffID.WitheredArmor, 3 * 60, false);
        }

        #region AI
        NPCDespawnHandler despawnHandler;
        bool dragonAliveLastFrame = true;
        int phaseTransitionTimer = 0;
        float spawnDelay = 60;
        float zapTimer = 45;
        bool initialized = false;
        int normalAttackCounter = 0;
        Vector2 nextWarpPoint;
        int teleportCooldown = 180;
        int nextAttackType = 0;
        float auraBonus;
        int expandRadius = 0;
        public static Effect effect;

        public override void AI()
        {
            bool dragonAlive = NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonHead>());
            
            Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 2f);
            if (!dragonAlive)
            {
                Main.dayTime = false;
                Main.time = 3000;
            }
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            if (!initialized)
            {
                nextWarpPoint = Main.rand.NextVector2CircularEdge(640, 440) + NPC.Center;
                initialized = true;
            }

            if (deathTimer > 0)
            {
                deathTimer++;
                NPC.noGravity = true;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int rand = Main.rand.Next(10);
                    if (rand == 0)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center + Main.rand.NextVector2Circular(10, 10), Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.LightRay>(), 0, 0, Main.myPlayer, 3, UsefulFunctions.ColorToFloat(Color.OrangeRed));
                    }
                    if (rand == 1)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center + Main.rand.NextVector2Circular(10, 10), Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.LightRay>(), 0, 0, Main.myPlayer, 3, UsefulFunctions.ColorToFloat(Color.Red));
                    }
                    if (rand == 2)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center + Main.rand.NextVector2Circular(10, 10), Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.LightRay>(), 0, 0, Main.myPlayer, 3, UsefulFunctions.ColorToFloat(new Color(0, 30, 255)));
                    }
                }

                if (deathTimer == 120)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(0.7f, 0.7f).RotatedBy(MathHelper.PiOver2 * Main.rand.Next(0, 4)), ModContent.ProjectileType<Projectiles.Enemy.WyvernMage.RedLightning>(), 0, 0f, Main.myPlayer);
                    }
                }

                if (deathTimer > 180)
                {
                    NPC.StrikeNPC(NPC.CalculateHitInfo(999999, 1, true, 0), false, false);
                }

                return;
            }

            Player player = Main.player[NPC.target];
            if (holdTimer > 1)
            {
                holdTimer--;
            }
            if (desperationTimer > 1)
            {
                desperationTimer--;
            }

            if (NPC.life < NPC.lifeMax / 2)
            {
                player.AddBuff(BuffID.Chilled, 30, false);
                player.AddBuff(BuffID.Ichor, 30, false);
                player.AddBuff(BuffID.Darkness, 30, false);

                if (holdTimer <= 0)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.WyvernMageShadow.ShadowWave"), 235, 23, 220);
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Lotr/Darkness"), NPC.Center);
                    holdTimer = 12000;
                }
            }

            //Desperation phase, player gets obstructed
            if (NPC.life < NPC.lifeMax / 15)
            {
                player.AddBuff(BuffID.Blackout, 30, false); player.AddBuff(BuffID.Obstructed, 30, false);
                if (desperationTimer <= 0)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.WyvernMageShadow.FinalShadowWave"), 175, 23, 200);
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Lotr/Darkness"), NPC.Center);
                    desperationTimer = 15000;
                }
            }

            OrbTimer++;
            TeleportTimer++;

            if (dragonAlive)
            {
                NPC.defense = 142;
            }

            if (dragonAliveLastFrame && !dragonAlive)
            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.WyvernMageShadow.GhostDragonDead"), 210, 23, 23);
                phaseTransitionTimer = 1;
            }
            dragonAliveLastFrame = dragonAlive;

            if (phaseTransitionTimer > 0 && phaseTransitionTimer < 360)
            {
                if (phaseTransitionTimer == 300)
                {
                    Vector2 spawnVec = new Vector2(500, 0);
                    for (int i = 0; i < 4; i++)
                    {
                        auraBonus = 1.8f;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            spawnVec = spawnVec.RotatedBy(MathHelper.PiOver2);
                            Vector2 aimVec = -Vector2.Normalize(spawnVec);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + spawnVec, aimVec, ModContent.ProjectileType<Projectiles.Enemy.WyvernMage.RedLightning>(), frozenSawDamage, 0f, Main.myPlayer);
                        }
                    }
                }
                phaseTransitionTimer++;
                OrbTimer++;
                return;
            }

            int transparency = 100;
            if (!dragonAlive)
            {
                transparency += 50;
            }
            int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.Wraith, NPC.velocity.X, NPC.velocity.Y, transparency, Color.Black, 1f);
            Main.dust[dust].noGravity = true;

            if (nextAttackType == 0)
            {
                if ((OrbTimer >= 4 && ShotCount < 5) || (ShotCount < 7 && !dragonAlive))
                {
                    auraBonus = 1;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 startPos = NPC.Center;
                        float speed;
                        float startRotation;

                        if (dragonAlive)
                        {
                            speed = 4;
                            startRotation = MathHelper.ToRadians(-15);
                        }
                        else
                        {
                            speed = 3f;
                            startRotation = MathHelper.ToRadians(-60);
                        }

                        Vector2 projVelocity = UsefulFunctions.Aim(startPos, Main.player[NPC.target].Center, speed);
                        projVelocity = projVelocity.RotatedBy(startRotation + MathHelper.ToRadians(30) * ShotCount);

                        Projectile.NewProjectile(NPC.GetSource_FromThis(), startPos.X, startPos.Y, projVelocity.X, projVelocity.Y, ModContent.ProjectileType<Projectiles.Enemy.WyvernMage.PurpleMagicProj>(), frozenSawDamage, 0f, Main.myPlayer);
                    }
                    OrbTimer = 0;
                    ShotCount++;
                }

                if (TeleportTimer % 6 == 0 && TeleportTimer < 180 && TeleportTimer > 60 && !dragonAlive)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        {
                            Vector2 spawnPos = Main.player[NPC.target].Center;
                            spawnPos.Y += -550;
                            spawnPos.X += Main.rand.Next(-50, 50);
                            Vector2 projVel = UsefulFunctions.Aim(spawnPos, Main.player[NPC.target].Center, 1);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, projVel, ModContent.ProjectileType<Projectiles.Enemy.WyvernMage.SmallRedLightning>(), frozenSawDamage, 0f, Main.myPlayer);
                        }
                    }
                }
            }
            else if (nextAttackType == 1)
            {
                if (TeleportTimer == 35)
                {
                    auraBonus = 1;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 projVel = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 1);
                        if (dragonAlive)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projVel, ModContent.ProjectileType<Projectiles.Enemy.WyvernMage.SmallRedLightning>(), frozenSawDamage, 0f, Main.myPlayer);
                        }
                        else
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projVel.RotatedBy(-0.15f) * 6, ModContent.ProjectileType<Projectiles.Enemy.WyvernMage.PurpleMagicProj>(), frozenSawDamage, 0f, Main.myPlayer, 1);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projVel, ModContent.ProjectileType<Projectiles.Enemy.WyvernMage.RedLightning>(), frozenSawDamage, 0f, Main.myPlayer);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projVel.RotatedBy(0.15f) * 6, ModContent.ProjectileType<Projectiles.Enemy.WyvernMage.PurpleMagicProj>(), frozenSawDamage, 0f, Main.myPlayer, 1);
                        }
                    }
                }

                if (TeleportTimer % 45 == 0 && !dragonAlive)
                {
                    auraBonus = 0.5f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            Vector2 projVel = new Vector2(Main.rand.NextFloat(-.3f, .3f), 8);
                            Vector2 spawnPos = Main.player[NPC.target].Center;
                            spawnPos.Y += -600;
                            spawnPos.X += -800 + (250 * i);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, projVel, ModContent.ProjectileType<Projectiles.Enemy.WyvernMage.RedRainProj>(), frozenSawDamage, 0f, Main.myPlayer);
                        }
                    }
                }
                if (TeleportTimer % 150 == 0 && !dragonAlive)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 projVel = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 1);
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projVel, ModContent.ProjectileType<Projectiles.Enemy.WyvernMage.RedLightning>(), frozenSawDamage, 0f, Main.myPlayer);
                        }
                    }
                }
            }
                else if (nextAttackType == 2)
                {
                    zapTimer--;
                    Vector2 spawnPos = Main.rand.NextVector2CircularEdge(1300, 1300) + NPC.Center;
                    Vector2 projVel = UsefulFunctions.Aim(spawnPos, NPC.Center, 10);
                    projVel = projVel.RotatedBy(-.25 + 2f * (teleportCooldown - TeleportTimer) / 600);
                    if (TeleportTimer < 400)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, projVel, ModContent.ProjectileType<Projectiles.Ice5Icicle>(), frozenSawDamage, 0f, Main.myPlayer, 1);
                        }
                    }

                    if (zapTimer <= 0 && TeleportTimer > 90)
                    {
                        auraBonus = .8f;
                        zapTimer = Main.rand.Next(35, 60);
                        projVel = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 1);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projVel, ModContent.ProjectileType<Projectiles.Enemy.WyvernMage.RedLightning>(), frozenSawDamage, 0f, Main.myPlayer);
                        }
                    }
                }

            if (TeleportTimer >= 10)
            {
                NPC.velocity.X *= 0.77f;
                NPC.velocity.Y *= 0.27f;
            }

            if (TeleportTimer >= teleportCooldown)
            {
                WyvernMageTeleport();
            }

            mageShadowTimer++;
            if (mageShadowTimer >= 1250 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int Paraspawn = NPC.NewNPC(NPC.GetSource_FromAI(), (int)Main.player[NPC.target].position.X - 676 - NPC.width / 2, (int)Main.player[NPC.target].position.Y - 16 - NPC.width / 2, ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.MageShadow>(), 0);
                Main.npc[Paraspawn].velocity.X = NPC.velocity.X;
                Paraspawn = NPC.NewNPC(NPC.GetSource_FromAI(), (int)Main.player[NPC.target].position.X + 676 - NPC.width / 2, (int)Main.player[NPC.target].position.Y - 16 - NPC.width / 2, ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.MageShadow>(), 0);
                Main.npc[Paraspawn].velocity.X = NPC.velocity.X;

                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Paraspawn, 0f, 0f, 0f, 0);
                }

                mageShadowTimer = 0;
            }
        }

        #endregion

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(nextWarpPoint);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            nextWarpPoint = reader.ReadVector2();
        }

        public void WyvernMageTeleport()
        {
            if (nextWarpPoint != Vector2.Zero)
            {
                Vector2 diff = nextWarpPoint - NPC.Center;
                float length = diff.Length();
                diff.Normalize();
                Vector2 offset = Vector2.Zero;

                for (int i = 0; i < length; i++)
                {
                    offset += diff;
                    if (Main.rand.NextBool(2))
                    {
                        Vector2 dustPoint = offset;
                        dustPoint.X += Main.rand.NextFloat(-NPC.width / 2, NPC.width / 2);
                        dustPoint.Y += Main.rand.NextFloat(-NPC.height / 2, NPC.height / 2);
                        if (Main.rand.NextBool())
                        {
                            Dust.NewDustPerfect(NPC.Center + dustPoint, 71, diff * 5, 200, default, 0.8f).noGravity = true;
                        }
                        else
                        {
                            Dust.NewDustPerfect(NPC.Center + dustPoint, DustID.FireworkFountain_Red, diff * 5, 200, default, 0.8f).noGravity = true;
                        }
                    }
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 350, 20);
                }

                NPC.Center = nextWarpPoint;
            }

            NPC.velocity = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 13);

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
            TeleportTimer = 0;
            ShotCount = 0;

            nextAttackType = Main.rand.Next(2);

            if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonHead>()))
            {
                teleportCooldown = 210;

                nextAttackType = Main.rand.Next(100);
                if (nextAttackType < 40)
                {
                    nextAttackType = 0;
                    normalAttackCounter++;
                }
                if (nextAttackType >= 40 && nextAttackType < 80)
                {
                    nextAttackType = 1;
                    normalAttackCounter++;
                }
                if (nextAttackType >= 80 || normalAttackCounter > 5)
                {
                    normalAttackCounter = 0;
                    nextAttackType = 2;
                    teleportCooldown = 600;
                }
            }

            for (int i = 0; i < 10; i++)
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.Wraith, NPC.velocity.X + Main.rand.Next(-10, 10), NPC.velocity.Y + Main.rand.Next(-10, 10), 200, Color.Red, 4f);
                Main.dust[dust].noGravity = false;
            }

            int warpHorizontalMax = 640;
            int warpVerticalMax = 440;

            if (nextAttackType == 2)
            {
                warpHorizontalMax = 400;
                warpVerticalMax = 10;
            }

            int checks = 0;
            while (checks < 15)
            {
                checks++;
                nextWarpPoint = Main.rand.NextVector2CircularEdge(warpHorizontalMax, warpVerticalMax);
                if (Collision.CanHit(Main.player[NPC.target].Center + nextWarpPoint, 1, 1, Main.player[NPC.target].Center, 1, 1) || Collision.CanHitLine(Main.player[NPC.target].Center + nextWarpPoint, 1, 1, Main.player[NPC.target].Center, 1, 1))
                {
                    nextWarpPoint = Main.player[NPC.target].Center + nextWarpPoint;
                    break;
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), nextWarpPoint, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.TeleportTelegraph>(), 0, 0, Main.myPlayer, ai1: teleportCooldown);
            }

            NPC.netUpdate = true;
        }

        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonHead>()))
            {
                modifiers.FinalDamage *= 0.25f;
            }
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonHead>()))
            {
                modifiers.FinalDamage *= 0.25f;
            }
        }

        public override bool CheckDead()
        {
            if (deathTimer == 0)
            {
                SoundEngine.PlaySound(SoundID.Shatter);
                deathTimer++;
                NPC.life = 1;
                NPC.dontTakeDamage = true;
            }

            if (deathTimer > 180)
            {
                return true;
            }
            return false;
        }

        public override void FindFrame(int currentFrame)
        {
            int frameHeight = !Main.dedServ ? TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type] : 1;

            if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonHead>()))
            {
                NPC.frame.Y = 2 * frameHeight; 
                NPC.frameCounter = 0; 
                if (NPC.position.X > Main.player[NPC.target].position.X)
                {
                    NPC.spriteDirection = -1;
                }
                else
                {
                    NPC.spriteDirection = 1;
                }
                return;
            }
            if ((NPC.velocity.X > -9 && NPC.velocity.X < 9) && (NPC.velocity.Y > -9 && NPC.velocity.Y < 9))
            {
                NPC.frameCounter = 0;
                NPC.frame.Y = 0;
                if (NPC.position.X > Main.player[NPC.target].position.X)
                {
                    NPC.spriteDirection = -1;
                }
                else
                {
                    NPC.spriteDirection = 1;
                }
            }

            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            if ((NPC.velocity.X > -2 && NPC.velocity.X < 2) && (NPC.velocity.Y > -2 && NPC.velocity.Y < 2))
            {
                NPC.frameCounter = 0;
                NPC.frame.Y = 0;
            }
            else
            {
                NPC.frameCounter += 1.0;
            }
            if (NPC.frameCounter >= 1.0)
            {
                NPC.frame.Y = NPC.frame.Y + num;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type])
            {
                NPC.frame.Y = 0;
            }
        }

        public override bool CheckActive()
        {
            return false;
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBagByCondition(new GhostWyvernMageDropCondition(), ModContent.ItemType<Items.BossBags.WyvernMageShadowBag>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.ByCondition(new GhostWyvernMageDropCondition(), ModContent.ItemType<HolyWarElixir>()));
            notExpertCondition.OnSuccess(ItemDropRule.ByCondition(new GhostWyvernMageDropCondition(), ModContent.ItemType<GhostWyvernSoul>(), 1, 3, 6));
            npcLoot.Add(notExpertCondition);
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShard>(), 1, 2, 4));
        }

        public override void OnKill()
        {
            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Wyvern Mage Shadow Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Wyvern Mage Shadow Gore 2").Type, 1f);
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.BossDeath>(), 0, 0, Main.myPlayer, 3, UsefulFunctions.ColorToFloat(Color.Purple));
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            auraBonus *= 0.9f;

            Color rgbColor = Color.Red;
            if (nextAttackType == 0)
            {
                expandRadius = 0;
            }
            if (nextAttackType == 1)
            {
                expandRadius = 0;
                rgbColor = Color.OrangeRed;
            }
            if (nextAttackType == 2)
            {
                if (expandRadius < 1000)
                {
                    expandRadius += 2;
                }
                rgbColor = new Color(255, 30, 255);
            }

            if (dragonAliveLastFrame)
            {
                rgbColor = Color.Purple;
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            float effectIntensity = 0;
            if (!dragonAliveLastFrame)
            {
                effectIntensity = 1;
                int effectTimer = 1;
                if (effectTimer > 540)
                {
                    effectIntensity = 1f - ((effectTimer - 540f) / 60f);
                }

                if (effectTimer < 20)
                {
                    effectIntensity = effectTimer / 20f;
                }
            }

            float timeFactor = 1;
            float scaleFactor = 4;

            if (effect == null)
            {
                effect = ModContent.Request<Effect>("tsorcRevamp/Effects/CatAura", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            float colorIntensity = 0.25f;
            if (phaseTransitionTimer > 1)
            {
                colorIntensity = (.6f * ((float)phaseTransitionTimer) / 360f) + auraBonus;
                rgbColor = Color.Lerp(Color.Gray, rgbColor, colorIntensity);
            }

            Rectangle sourceRectangle = new Rectangle(0, 0, 150 + expandRadius / 4 + (int)(70 * (effectIntensity + auraBonus)), 150 + expandRadius / 4 + (int)(70 * (effectIntensity + auraBonus)));
            Vector2 origin = sourceRectangle.Size() / 2f;

            effect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseVoronoi.Width);
            effect.Parameters["effectSize"].SetValue(sourceRectangle.Size());
            effect.Parameters["effectColor"].SetValue(rgbColor.ToVector4() * colorIntensity * 0.5f * (6 + (expandRadius / 500f)));
            effect.Parameters["ringProgress"].SetValue(0.4f + .25f * effectIntensity + (expandRadius / 2500f));
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * timeFactor);

            effect.Parameters["scaleFactor"].SetValue(scaleFactor);

            effect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseVoronoi, NPC.Center - Main.screenPosition, sourceRectangle, Color.White, 0, origin, NPC.scale, SpriteEffects.None, 0);

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);

            return true;
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
        }

        public static Texture2D texture;
    }

    public class GhostWyvernMageDropCondition : IItemDropRuleCondition
    {
        public bool CanDrop(Terraria.GameContent.ItemDropRules.DropAttemptInfo info)
        {
            return !NPC.AnyNPCs(ModContent.NPCType<GhostDragonHead>());
        }

        public bool CanShowItemDropInUI()
        {
            return false;
        }

        public string GetConditionDescription()
        {
            return LangUtils.GetTextValue("NPCs.WyvernMageShadow.Condition");
        }
    }
}