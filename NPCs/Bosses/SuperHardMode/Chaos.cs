using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    [AutoloadBossHead]
    class Chaos : ModNPC
    {
        public override void SetDefaults()
        {
            Main.npcFrameCount[NPC.type] = 8;
            NPC.width = 130;
            NPC.height = 170;
            NPC.aiStyle = 22;
            NPC.damage = 150;
            NPC.defense = 80;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.lifeMax = 400000;
            NPC.knockBackResist = 0;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.value = 600000;
            NPC.boss = true;
            NPC.lavaImmune = true;

            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            despawnHandler = new NPCDespawnHandler("Chaos tears you asunder...", Color.Yellow, DustID.GoldFlame);

        }

        int fireBreathDamage = 48;
        int iceStormDamage = 53;
        int greatFireballDamage = 49;
        int blazeBallDamage = 48;
        int purpleCrushDamage = 35;
        int meteorDamage = 55;
        int tornadoDamage = 45;
        int obscureSeekerDamage = 50;
        int crystalFireDamage = 50;
        int fireTrailsDamage = 35;
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.damage = (int)(NPC.damage / 2);
        }

        int chaosHealed = 0;
        bool chargeDamageFlag = false;
        int holdTimer = 0;

        int chargeTimer = 0;

        #region AI
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            Lighting.AddLight((int)NPC.position.X / 16, (int)NPC.position.Y / 16, 0.4f, 0f, 0f);

            if (holdTimer > 0)
            {
                holdTimer--;
            }
            if (Vector2.Distance(NPC.Center, Main.player[NPC.target].Center) > 1000)
            {
                NPC.defense = 9999;
                if (holdTimer <= 0 && Main.netMode != NetmodeID.Server)
                {
                    Main.NewText("Chaos is protected by unseen powers -- you're too far away!", 175, 75, 255);
                    holdTimer = 200;
                }
                else
                {
                    NPC.defense = 80;
                }
            }

            ClientSideAttacks();
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NonClientAttacks();
            }

            if (NPC.justHit)
            {
                NPC.ai[2] = 0f;
            }
            if (NPC.ai[2] >= 0f)
            {
                int num258 = 16;
                bool flag26 = false;
                bool flag27 = false;
                if (NPC.position.X > NPC.ai[0] - (float)num258 && NPC.position.X < NPC.ai[0] + (float)num258)
                {
                    flag26 = true;
                }
                else
                {
                    if ((NPC.velocity.X < 0f && NPC.direction > 0) || (NPC.velocity.X > 0f && NPC.direction < 0))
                    {
                        flag26 = true;
                    }
                }
                num258 += 24;
                if (NPC.position.Y > NPC.ai[1] - (float)num258 && NPC.position.Y < NPC.ai[1] + (float)num258)
                {
                    flag27 = true;
                }
                if (flag26 && flag27)
                {
                    NPC.ai[2] += 1f;
                    if (NPC.ai[2] >= 60f)
                    {
                        NPC.ai[2] = -200f;
                        NPC.direction *= -1;
                        NPC.velocity.X = NPC.velocity.X * -1f;
                        NPC.collideX = false;
                    }
                }
                else
                {
                    NPC.ai[0] = NPC.position.X;
                    NPC.ai[1] = NPC.position.Y;
                    NPC.ai[2] = 0f;
                }
            }
            else
            {
                NPC.ai[2] += 1f;
                if (Main.player[NPC.target].position.X + (float)(Main.player[NPC.target].width / 2) > NPC.position.X + (float)(NPC.width / 2))
                {
                    NPC.direction = -1;
                }
                else
                {
                    NPC.direction = 1;
                }
            }
            int num259 = (int)((NPC.position.X + (float)(NPC.width / 2)) / 16f) + NPC.direction * 2;
            int num260 = (int)((NPC.position.Y + (float)NPC.height) / 16f);
            if (NPC.position.Y > Main.player[NPC.target].position.Y)
            {
                NPC.velocity.Y -= .22f;
                if (NPC.velocity.Y < -2)
                {
                    NPC.velocity.Y = -2;
                }
            }
            if (NPC.position.Y < Main.player[NPC.target].position.Y)
            {
                NPC.velocity.Y += .22f;
                if (NPC.velocity.Y > 2)
                {
                    NPC.velocity.Y = 2;
                }
            }
            if (NPC.collideX)
            {
                NPC.velocity.X = NPC.oldVelocity.X * -0.4f;
                if (NPC.direction == -1 && NPC.velocity.X > 0f && NPC.velocity.X < 1f)
                {
                    NPC.velocity.X = 1f;
                }
                if (NPC.direction == 1 && NPC.velocity.X < 0f && NPC.velocity.X > -1f)
                {
                    NPC.velocity.X = -1f;
                }
            }
            if (NPC.collideY)
            {
                NPC.velocity.Y = NPC.oldVelocity.Y * -0.25f;
                if (NPC.velocity.Y > 0f && NPC.velocity.Y < 1f)
                {
                    NPC.velocity.Y = 1f;
                }
                if (NPC.velocity.Y < 0f && NPC.velocity.Y > -1f)
                {
                    NPC.velocity.Y = -1f;
                }
            }
            float num270 = 2.5f;
            if (NPC.direction == -1 && NPC.velocity.X > -num270)
            {
                NPC.velocity.X = NPC.velocity.X - 0.1f;
                if (NPC.velocity.X > num270)
                {
                    NPC.velocity.X = NPC.velocity.X - 0.1f;
                }
                else
                {
                    if (NPC.velocity.X > 0f)
                    {
                        NPC.velocity.X = NPC.velocity.X + 0.05f;
                    }
                }
                if (NPC.velocity.X < -num270)
                {
                    NPC.velocity.X = -num270;
                }
            }
            else
            {
                if (NPC.direction == 1 && NPC.velocity.X < num270)
                {
                    NPC.velocity.X = NPC.velocity.X + 0.1f;
                    if (NPC.velocity.X < -num270)
                    {
                        NPC.velocity.X = NPC.velocity.X + 0.1f;
                    }
                    else
                    {
                        if (NPC.velocity.X < 0f)
                        {
                            NPC.velocity.X = NPC.velocity.X - 0.05f;
                        }
                    }
                    if (NPC.velocity.X > num270)
                    {
                        NPC.velocity.X = num270;
                    }
                }
            }
            if (NPC.directionY == -1 && (double)NPC.velocity.Y > -2.5)
            {
                NPC.velocity.Y = NPC.velocity.Y - 0.04f;
                if ((double)NPC.velocity.Y > 2.5)
                {
                    NPC.velocity.Y = NPC.velocity.Y - 0.05f;
                }
                else
                {
                    if (NPC.velocity.Y > 0f)
                    {
                        NPC.velocity.Y = NPC.velocity.Y + 0.03f;
                    }
                }
                if ((double)NPC.velocity.Y < -2.5)
                {
                    NPC.velocity.Y = -2.5f;
                }
            }
            else
            {
                if (NPC.directionY == 1 && (double)NPC.velocity.Y < 2.5)
                {
                    NPC.velocity.Y = NPC.velocity.Y + 0.04f;
                    if ((double)NPC.velocity.Y < -2.5)
                    {
                        NPC.velocity.Y = NPC.velocity.Y + 0.05f;
                    }
                    else
                    {
                        if (NPC.velocity.Y < 0f)
                        {
                            NPC.velocity.Y = NPC.velocity.Y - 0.03f;
                        }
                    }
                    if ((double)NPC.velocity.Y > 2.5)
                    {
                        NPC.velocity.Y = 2.5f;
                    }
                }
            }
        }
        #endregion


        //Projectile spawning code must not run for every single multiplayer client
        void NonClientAttacks()
        {
            NPC.ai[1] += 0.35f;
            if (NPC.ai[1] >= 10f)
            {
                if (Main.rand.Next(90) == 1)
                {
                    Vector2 projTarget = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 9);
                    projTarget += Main.rand.NextVector2Circular(3, 3);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projTarget.X, projTarget.Y, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), fireBreathDamage, 0f, Main.myPlayer);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    NPC.ai[1] = 1f;
                }
                if (Main.rand.Next(500) == 1)
                {
                    Vector2 projTarget = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 8);
                    projTarget += Main.rand.NextVector2Circular(3, 3);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projTarget.X, projTarget.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellIcestormBall>(), iceStormDamage, 0f, Main.myPlayer);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    NPC.ai[1] = 1f;
                }
                if (Main.rand.Next(500) == 1)
                {
                    Vector2 projTarget = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 8);
                    projTarget += Main.rand.NextVector2Circular(3, 3);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projTarget.X, projTarget.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatFireballBall>(), greatFireballDamage, 0f, Main.myPlayer);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    NPC.ai[1] = 1f;
                }
                if (Main.rand.Next(1000) == 1)
                {
                    Vector2 projTarget = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 8);
                    projTarget += Main.rand.NextVector2Circular(3, 3);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projTarget.X, projTarget.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellBlazeBall>(), blazeBallDamage, 0f, Main.myPlayer);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    NPC.ai[1] = 1f;
                }
                if (Main.rand.Next(300) == 1)
                {
                    Vector2 projTarget = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 11);
                    projTarget += Main.rand.NextVector2Circular(3, 3);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projTarget.X, projTarget.Y, ModContent.ProjectileType<Projectiles.Enemy.CrazedPurpleCrush>(), purpleCrushDamage, 0f, Main.myPlayer);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    NPC.ai[1] = 1f;
                }

                if (Main.rand.Next(205) == 1)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), Main.player[NPC.target].position.X - 100 + Main.rand.Next(300), Main.player[NPC.target].position.Y - 530.0f, (float)(-40 + Main.rand.Next(80)) / 10, 14.9f, ModContent.ProjectileType<Projectiles.Enemy.EnemyMeteor>(), meteorDamage, 2.0f, Main.myPlayer);
                }

                if (Main.rand.Next(1200) == 1)
                {
                    Vector2 projTarget = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 4);
                    projTarget += Main.rand.NextVector2Circular(3, 3);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projTarget.X, projTarget.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellTornado>(), tornadoDamage, 0f, Main.myPlayer, NPC.target);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    NPC.ai[1] = 1f;
                }
                if (Main.rand.Next(220) == 1)
                {
                    Vector2 projTarget = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 8);
                    projTarget += Main.rand.NextVector2Circular(3, 3);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projTarget.X, projTarget.Y, ModContent.ProjectileType<Projectiles.Enemy.ObscureSeeker>(), obscureSeekerDamage, 0f, Main.myPlayer);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    NPC.ai[1] = 1f;
                }
                if (Main.rand.Next(50) == 1)
                {
                    Vector2 projTarget = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 12);
                    projTarget += Main.rand.NextVector2Circular(3, 3);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projTarget.X, projTarget.Y, ModContent.ProjectileType<Projectiles.Enemy.PoisonCrystalFire>(), crystalFireDamage, 0f, Main.myPlayer);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    NPC.ai[1] = 1f;
                }
                if (Main.rand.Next(120) == 1)
                {
                    Vector2 projTarget = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 5);
                    projTarget += Main.rand.NextVector2Circular(3, 3);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projTarget.X, projTarget.Y, ModContent.ProjectileType<Projectiles.Enemy.FireTrails>(), fireTrailsDamage, 0f, Main.myPlayer);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    NPC.ai[1] = 1f;
                }
            }
        }

        //Healing and dashing code must be deterministic and must run on every client
        void ClientSideAttacks()
        {
            chargeTimer++;
            if (chargeTimer >= 480)
            {
                chargeTimer = 0;
                chargeDamageFlag = true;
                NPC.velocity = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 14) + Main.player[NPC.target].velocity;
            }
            if (chargeDamageFlag == true)
            {
                NPC.damage = 120;
            }
            if (Math.Abs(NPC.Center.X - Main.player[NPC.target].Center.X) < 20)
            {
                chargeDamageFlag = false;
                NPC.damage = 80;
            }

            if (NPC.life <= NPC.lifeMax / 3)
            {
                if (chaosHealed >= 1 && chaosHealed <= 3)
                {
                    if (Main.rand.Next(500) == 1)
                    {
                        if (chaosHealed == 0)
                        {
                            UsefulFunctions.BroadcastText("Chaos has begun to heal itself...", Color.Yellow);
                        }
                        else
                        {
                            UsefulFunctions.BroadcastText("Chaos continues to heal itself...", Color.Yellow);
                        }
                        NPC.life += NPC.lifeMax / 6;
                        if (NPC.life > NPC.lifeMax) NPC.life = NPC.lifeMax;
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, 0, 0, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellEffectHealing>(), 0, 0f, Main.myPlayer);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, NPC.Center); NPC.netUpdate = true;
                        chaosHealed += 1;
                    }
                }
            }
        }
        #region Frames
        public override void FindFrame(int currentFrame)
        {
            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            if (NPC.velocity.X < 0)
            {
                NPC.spriteDirection = -1;
            }
            else
            {
                NPC.spriteDirection = 1;
            }
            NPC.rotation = NPC.velocity.X * 0.08f;
            NPC.frameCounter += 1.0;
            if (NPC.frameCounter >= 4.0)
            {
                NPC.frame.Y = NPC.frame.Y + num;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type])
            {
                NPC.frame.Y = 0;
            }
        }
        #endregion

        #region Gore
        public override void OnKill()
        {
            Projectile.NewProjectile(NPC.GetSource_FromThis(), (int)NPC.position.X, (int)NPC.position.Y, 0, 0, ModContent.ProjectileType<Projectiles.Enemy.ChaosDeathAnimation>(), 0, 0f, Main.myPlayer);
            if (!Main.expertMode)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Armors.PowerArmorNUHelmet>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Armors.PowerArmorNUTorso>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Armors.PowerArmorNUGreaves>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Magic.FlareTome>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Ranged.ElfinBow>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.HolyWarElixir>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.DarkSoul>(), 3000);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.GuardianSoul>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.SoulOfChaos>(), 3);
            }
        }
        #endregion
        public override bool CheckActive()
        {
            return false;
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.ChaosBag>()));
        }

        #region Magic Defense
        public int MagicDefenseValue()
        {
            return 65;
        }
        #endregion
    }
}