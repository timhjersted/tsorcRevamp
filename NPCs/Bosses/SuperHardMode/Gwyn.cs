using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.Fiends;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    [AutoloadBossHead]
    class Gwyn : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.npcSlots = 10;
            Main.npcFrameCount[NPC.type] = 16;
            AnimationType = 28;
            NPC.aiStyle = 3;
            NPC.height = 40;
            NPC.width = 20;
            music = 12;
            NPC.damage = 105;
            NPC.defense = 90;
            NPC.lifeMax = 500000;
            NPC.knockBackResist = 0;
            NPC.boss = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 800000;
            bossBag = ModContent.ItemType<Items.BossBags.GwynBag>();
            despawnHandler = new NPCDespawnHandler("You have fallen before the Lord of Cinder...", Color.OrangeRed, 6);
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Gwyn, Lord of Cinder");
        }

        int deathBallDamage = 100;
        int phantomSeekerDamage = 45;
        int armageddonBallDamage = 85;
        int holdBallDamage = 35;
        int fireballBallDamage = 45;
        int blazeBallDamage = 55;
        int blackBreathDamage = 80;
        int purpleCrushDamage = 45;
        int fireBreathDamage = 50;
        int iceStormDamage = 33;
        int gravityBallDamage = 80;

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
            deathBallDamage = (int)(deathBallDamage / 2);
            phantomSeekerDamage = (int)(phantomSeekerDamage / 2);
            armageddonBallDamage = (int)(armageddonBallDamage / 2);
            holdBallDamage = (int)(holdBallDamage / 2);
            fireballBallDamage = (int)(fireballBallDamage / 2);
            blazeBallDamage = (int)(blazeBallDamage / 2);
            blackBreathDamage = (int)(blackBreathDamage / 2);
            purpleCrushDamage = (int)(purpleCrushDamage / 2);
            fireBreathDamage = (int)(fireBreathDamage / 2);
            iceStormDamage = (int)(iceStormDamage / 2);
            gravityBallDamage = (int)(gravityBallDamage / 2);
        }

        int chargeDamage = 0;
        bool chargeDamageFlag = false;

        float customAi1;
        float customspawn1;
        float customspawn2;
        float customspawn3;

        #region debuffs
        public override void OnHitPlayer(Player player, int damage, bool crit)
        {
            if (Main.rand.Next(2) == 0)
            {
                player.AddBuff(33, 7200, false); //weak
                player.AddBuff(36, 180, false); //broken armor
                player.AddBuff(24, 600, false); //on fire
                player.AddBuff(ModContent.BuffType<Buffs.SlowedLifeRegen>(), 1200, false); //slowed life regen
                player.AddBuff(ModContent.BuffType<Buffs.BrokenSpirit>(), 1800, false); //you lose knockback resistance
                player.AddBuff(ModContent.BuffType<Buffs.Crippled>(), 1200, false); //you lose flight
            }
        }
        #endregion

        #region AI
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {

            bool tooEarly = !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<Artorias>()) || !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<EarthFiendLich>()) || !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<FireFiendMarilith>()) || !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<WaterFiendKraken>()) || !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<Blight>()) || !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<Chaos>()) || !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<GhostWyvernMage.WyvernMageShadow>());
            if (tooEarly)
            {
                deathBallDamage = 10000;
                phantomSeekerDamage = 10000;
                armageddonBallDamage = 10000;
                holdBallDamage = 10000;
                fireballBallDamage = 10000;
                blazeBallDamage = 10000;
                blackBreathDamage = 10000;
                purpleCrushDamage = 10000;
                fireBreathDamage = 10000;
                iceStormDamage = 10000;
                gravityBallDamage = 10000;
                NPC.damage = 10000;
            }
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            int num58;
            int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.Red, 2f);
            Main.dust[dust].noGravity = true;

            bool flag2 = false;
            int num5 = 60;
            bool flag3 = true;
            if (NPC.velocity.Y == 0f && (NPC.velocity.X == 0f && NPC.direction < 0))
            {
                NPC.velocity.Y -= 8f;
                NPC.velocity.X -= 2f;
            }
            else if (NPC.velocity.Y == 0f && (NPC.velocity.X == 0f && NPC.direction > 0))
            {
                NPC.velocity.Y -= 8f;
                NPC.velocity.X += 2f;
            }
            if (NPC.velocity.Y == 0f && ((NPC.velocity.X > 0f && NPC.direction < 0) || (NPC.velocity.X < 0f && NPC.direction > 0)))
            {
                flag2 = true;
            }
            if (NPC.position.X == NPC.oldPosition.X || NPC.ai[3] >= (float)num5 || flag2)
            {
                NPC.ai[3] += 1f;
            }
            else
            {
                if ((double)Math.Abs(NPC.velocity.X) > 0.9 && NPC.ai[3] > 0f)
                {
                    NPC.ai[3] -= 1f;
                }
            }
            if (NPC.ai[3] > (float)(num5 * 10))
            {
                NPC.ai[3] = 0f;
            }
            if (NPC.justHit)
            {
                NPC.ai[3] = 0f;
            }
            if (NPC.ai[3] == (float)num5)
            {
                NPC.netUpdate = true;
            }
            if ((!Main.dayTime || (double)NPC.position.Y > Main.worldSurface * 16.0 || NPC.type == 26 || NPC.type == 27 || NPC.type == 28 || NPC.type == 31 || NPC.type == 47 || NPC.type == 67 || NPC.type == 73 || NPC.type == 77 || NPC.type == 78 || NPC.type == 79 || NPC.type == 80 || NPC.type == 110 || NPC.type == 111 || NPC.type == 120) && NPC.ai[3] < (float)num5)
            {
                if ((NPC.type == 3 || NPC.type == 21 || NPC.type == 31 || NPC.type == 77 || NPC.type == 110 || NPC.type == 132) && Main.rand.Next(1000) == 0)
                {
                    Terraria.Audio.SoundEngine.PlaySound(14, (int)NPC.position.X, (int)NPC.position.Y, 1);
                }
                if ((NPC.type == 78 || NPC.type == 79 || NPC.type == 80) && Main.rand.Next(500) == 0)
                {
                    Terraria.Audio.SoundEngine.PlaySound(26, (int)NPC.position.X, (int)NPC.position.Y, 1);
                }
            }
            else
            {
                if (NPC.velocity.X == 0f)
                {
                    if (NPC.velocity.Y == 0f)
                    {
                        NPC.ai[0] += 1f;
                        if (NPC.ai[0] >= 2f)
                        {
                            NPC.direction *= -1;
                            NPC.spriteDirection = NPC.direction;
                            NPC.ai[0] = 0f;
                        }
                    }
                }
                else
                {
                    NPC.ai[0] = 0f;
                }
                if (NPC.direction == 0)
                {
                    NPC.direction = 1;
                }
            }
            if (NPC.velocity.X < -1.5f || NPC.velocity.X > 1.5f)
            {
                if (NPC.velocity.Y == 0f)
                {
                    NPC.velocity *= 0.8f;
                }
            }
            else
            {
                if (NPC.velocity.X < 1.5f && NPC.direction == 1)
                {
                    NPC.velocity.X = NPC.velocity.X + 0.07f;
                    if (NPC.velocity.X > 1.5f)
                    {
                        NPC.velocity.X = 1.5f;
                    }
                }
                else
                {
                    if (NPC.velocity.X > -1.5f && NPC.direction == -1)
                    {
                        NPC.velocity.X = NPC.velocity.X - 0.07f;
                        if (NPC.velocity.X < -1.5f)
                        {
                            NPC.velocity.X = -1.5f;
                        }
                    }
                }
            }
            bool flag4 = false;
            if (NPC.velocity.Y == 0f)
            {
                int num29 = (int)(NPC.position.Y + (float)NPC.height + 8f) / 16;
                int num30 = (int)NPC.position.X / 16;
                int num31 = (int)(NPC.position.X + (float)NPC.width) / 16;
                for (int l = num30; l <= num31; l++)
                {
                    if (Main.tile[l, num29] == null)
                    {
                        return;
                    }
                    if (Main.tile[l, num29].HasTile && Main.tileSolid[(int)Main.tile[l, num29].TileType])
                    {
                        flag4 = true;
                        break;
                    }
                }
            }
            if (flag4)
            {
                int num32 = (int)((NPC.position.X + (float)(NPC.width / 2) + (float)(15 * NPC.direction)) / 16f);
                int num33 = (int)((NPC.position.Y + (float)NPC.height - 15f) / 16f);
                if (NPC.type == 109)
                {
                    num32 = (int)((NPC.position.X + (float)(NPC.width / 2) + (float)((NPC.width / 2 + 16) * NPC.direction)) / 16f);
                }
                if (Main.tile[num32, num33] == null)
                {
                    Main.tile[num32, num33].ClearTile();
                }
                if (Main.tile[num32, num33 - 1] == null)
                {
                    Main.tile[num32, num33 - 1].ClearTile();
                }
                if (Main.tile[num32, num33 - 2] == null)
                {
                    Main.tile[num32, num33 - 2].ClearTile();
                }
                if (Main.tile[num32, num33 - 3] == null)
                {
                    Main.tile[num32, num33 - 3].ClearTile();
                }
                if (Main.tile[num32, num33 + 1] == null)
                {
                    Main.tile[num32, num33 + 1].ClearTile();
                }
                if (Main.tile[num32 + NPC.direction, num33 - 1] == null)
                {
                    Main.tile[num32 + NPC.direction, num33 - 1].ClearTile();
                }
                if (Main.tile[num32 + NPC.direction, num33 + 1] == null)
                {
                    Main.tile[num32 + NPC.direction, num33 + 1].ClearTile();
                }
                if (Main.tile[num32, num33 - 1].HasTile && Main.tile[num32, num33 - 1].TileType == 10 && flag3)
                {
                    NPC.ai[2] += 1f;
                    NPC.ai[3] = 0f;
                    if (NPC.ai[2] >= 60f)
                    {
                        NPC.velocity.X = 0.5f * (float)(-(float)NPC.direction);
                        NPC.ai[1] += 1f;
                        NPC.ai[2] = 0f;
                        bool flag5 = false;
                        if (NPC.ai[1] >= 10f)
                        {
                            flag5 = true;
                            NPC.ai[1] = 10f;
                        }
                        WorldGen.KillTile(num32, num33 - 1, true, false, false);
                        if ((Main.netMode != 1 || !flag5) && flag5 && Main.netMode != 1)
                        {
                            if (NPC.type == 26)
                            {
                                WorldGen.KillTile(num32, num33 - 1, false, false, false);
                                if (Main.netMode == 2)
                                {
                                    NetMessage.SendData(17, -1, -1, null, 0, (float)num32, (float)(num33 - 1), 0f, 0);
                                }
                            }
                            else
                            {
                                bool flag6 = WorldGen.OpenDoor(num32, num33, NPC.direction);
                                if (!flag6)
                                {
                                    NPC.ai[3] = (float)num5;
                                    NPC.netUpdate = true;
                                }
                                if (Main.netMode == 2 && flag6)
                                {
                                    NetMessage.SendData(19, -1, -1, null, 0, (float)num32, (float)num33, (float)NPC.direction, 0);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if ((NPC.velocity.X < 0f && NPC.spriteDirection == -1) || (NPC.velocity.X > 0f && NPC.spriteDirection == 1))
                    {
                        if (Main.tile[num32, num33 - 2].HasTile && Main.tileSolid[(int)Main.tile[num32, num33 - 2].TileType])
                        {
                            if (Main.tile[num32, num33 - 3].HasTile && Main.tileSolid[(int)Main.tile[num32, num33 - 3].TileType])
                            {
                                NPC.velocity.Y = -8f;
                                NPC.netUpdate = true;
                            }
                            else
                            {
                                NPC.velocity.Y = -7f;
                                NPC.netUpdate = true;
                            }
                        }
                        else
                        {
                            if (Main.tile[num32, num33 - 1].HasTile && Main.tileSolid[(int)Main.tile[num32, num33 - 1].TileType])
                            {
                                NPC.velocity.Y = -6f;
                                NPC.netUpdate = true;
                            }
                            else
                            {
                                if (Main.tile[num32, num33].HasTile && Main.tileSolid[(int)Main.tile[num32, num33].TileType])
                                {
                                    NPC.velocity.Y = -5f;
                                    NPC.netUpdate = true;
                                }
                                else
                                {
                                    if (NPC.directionY < 0 && NPC.type != 67 && (!Main.tile[num32, num33 + 1].HasTile || !Main.tileSolid[(int)Main.tile[num32, num33 + 1].TileType]) && (!Main.tile[num32 + NPC.direction, num33 + 1].HasTile || !Main.tileSolid[(int)Main.tile[num32 + NPC.direction, num33 + 1].TileType]))
                                    {
                                        NPC.velocity.Y = -8f;
                                        NPC.velocity.X = NPC.velocity.X * 1.5f;
                                        NPC.netUpdate = true;
                                    }
                                    else
                                    {
                                        if (flag3)
                                        {
                                            NPC.ai[1] = 0f;
                                            NPC.ai[2] = 0f;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (flag3)
                {
                    NPC.ai[1] = 0f;
                    NPC.ai[2] = 0f;
                }
            }
            #endregion


            #region Charge
            if (Main.netMode != 1)
            {
                if (Main.rand.Next(80) == 1)
                {
                    chargeDamageFlag = true;
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    NPC.velocity.X = (float)(Math.Cos(rotation) * 10) * -1;
                    NPC.velocity.Y = (float)(Math.Sin(rotation) * 10) * -1;
                    NPC.ai[1] = 1f;
                    NPC.netUpdate = true;
                }
                if (chargeDamageFlag == true)
                {
                    NPC.damage = 150;
                    chargeDamage++;
                }
                if (chargeDamage >= 106)
                {
                    chargeDamageFlag = false;
                    NPC.damage = 105;
                    chargeDamage = 0;
                }

            }
            #endregion

            #region Projectiles
            if (Main.netMode != 1)
            {
                customAi1 += (Main.rand.Next(2, 5) * 0.1f) * NPC.scale;
                if (customAi1 >= 10f)
                {
                    if ((customspawn1 < 16) && Main.rand.Next(200) == 1)
                    {
                        int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), NPCID.Hellbat, 0);
                        Main.npc[Spawned].velocity.Y = -8;
                        Main.npc[Spawned].velocity.X = Main.rand.Next(-10, 10) / 10;
                        NPC.ai[0] = 20 - Main.rand.Next(80);
                        customspawn1 += 1f;
                        if (Main.netMode == 2)
                        {
                            NetMessage.SendData(23, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
                        }
                    }
                    if ((customspawn2 < 5) && Main.rand.Next(350) == 1)
                    {
                        int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<NPCs.Enemies.BlackKnight>(), 0);
                        Main.npc[Spawned].velocity.Y = -8;
                        Main.npc[Spawned].velocity.X = Main.rand.Next(-10, 10) / 10;
                        NPC.ai[0] = 20 - Main.rand.Next(80);
                        customspawn2 += 1f;
                        if (Main.netMode == 2)
                        {
                            NetMessage.SendData(23, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
                        }
                    }

                    if ((customspawn3 < 1) && Main.rand.Next(550) == 1)
                    {
                        int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<SwordOfLordGwyn>(), 0);
                        Main.npc[Spawned].velocity.Y = -8;
                        Main.npc[Spawned].velocity.X = Main.rand.Next(-10, 10) / 10;
                        NPC.ai[0] = 20 - Main.rand.Next(80);
                        customspawn3 += 1f;
                        if (Main.netMode == 2)
                        {
                            NetMessage.SendData(23, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
                        }
                    }

                    if (Main.rand.Next(100) == 1)
                    {
                        float num48 = 10f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathBall>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, deathBallDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 150;
                            Main.projectile[num54].aiStyle = 1;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            customAi1 = 1f;
                        }
                        NPC.netUpdate = true;
                    }

                    if (Main.rand.Next(200) == 1)
                    {
                        num58 = Projectile.NewProjectile(NPC.GetSource_FromThis(), this.NPC.position.X + 20, this.NPC.position.Y + 50, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), ModContent.ProjectileType<Projectiles.Enemy.BurningPhantomSeeker>(), phantomSeekerDamage, 0f, Main.myPlayer);
                        Main.projectile[num58].timeLeft = 460;
                        Main.projectile[num58].rotation = Main.rand.Next(700) / 100f;
                        Main.projectile[num58].ai[0] = this.NPC.target;


                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        customAi1 = 1f;

                        NPC.netUpdate = true;
                    }

                    if (Main.rand.Next(900) == 1)
                    {
                        float num48 = 8f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellArmageddonBall>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, armageddonBallDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 0;
                            Main.projectile[num54].aiStyle = -1;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            customAi1 = 1f;
                        }
                        NPC.netUpdate = true;
                    }

                    if (Main.rand.Next(220) == 1)
                    {
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                        NPC.velocity.X = (float)(Math.Cos(rotation) * 14) * -1;
                        NPC.velocity.Y = (float)(Math.Sin(rotation) * 14) * -1;
                        NPC.ai[1] = 1f;
                        NPC.netUpdate = true;
                    }

                    if (Main.rand.Next(250) == 1)
                    {
                        float num48 = 18f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y - 100 + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellHoldBall>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, holdBallDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 115;
                            Main.projectile[num54].aiStyle = 1;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            NPC.ai[1] = 1f;





                        }
                        NPC.netUpdate = true;
                    }

                    if (Main.rand.Next(500) == 1)
                    {
                        float num48 = 8f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathBall>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, deathBallDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 100;
                            Main.projectile[num54].aiStyle = 1;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            customAi1 = 1f;
                        }
                        NPC.netUpdate = true;
                    }

                    if (Main.rand.Next(150) == 1)
                    {
                        float num48 = 15f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-10, 20);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-10, 30);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatFireballBall>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, fireballBallDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 90;
                            Main.projectile[num54].aiStyle = 1;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            customAi1 = 1f;
                        }
                        NPC.netUpdate = true;
                    }

                    if (Main.rand.Next(150) == 1)
                    {
                        float num48 = 15f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-10, 20);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-10, 30);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatFireballBall>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, fireballBallDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 90;
                            Main.projectile[num54].aiStyle = 1;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            customAi1 = 1f;
                        }
                        NPC.netUpdate = true;
                    }

                    if (Main.rand.Next(60) == 1)
                    {
                        float num48 = 8f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellBlazeBall>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, blazeBallDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 0;
                            Main.projectile[num54].aiStyle = 1;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            NPC.ai[1] = 1f;
                        }
                        NPC.netUpdate = true;
                    }

                    if (Main.rand.Next(200) == 1)
                    {
                        float num48 = 8f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.BlackBreath>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, blackBreathDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 10;
                            Main.projectile[num54].aiStyle = 1;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            customAi1 = 1f;
                        }
                        NPC.netUpdate = true;
                    }

                    if (Main.rand.Next(400) == 1)
                    {
                        float num48 = 13f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y - 1000 + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.CrazedPurpleCrush>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, purpleCrushDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 600;
                            Main.projectile[num54].aiStyle = 1;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            customAi1 = 1f;
                        }
                        NPC.netUpdate = true;
                    }

                    if (Main.rand.Next(26) == 1)
                    {
                        float num48 = 14f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y - 400 + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.FireBreath>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, fireBreathDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 3000;
                            Main.projectile[num54].aiStyle = 23; //was 23
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            customAi1 = 1f;
                        }
                        NPC.netUpdate = true;
                    }

                    if (Main.rand.Next(66) == 1) //might remove
                    {
                        float num48 = 14f;
                        Vector2 vector8 = new Vector2(NPC.position.X - 1800 + (NPC.width * 0.5f), NPC.position.Y - 600 + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.FireBreath>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, fireBreathDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 200;
                            Main.projectile[num54].aiStyle = 23; //was 23
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            customAi1 = 1f;
                        }
                        NPC.netUpdate = true;
                    }

                    if (Main.rand.Next(66) == 1) // might remove
                    {
                        float num48 = 14f;
                        Vector2 vector8 = new Vector2(NPC.position.X + 1800 + (NPC.width * 0.5f), NPC.position.Y - 600 + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.FireBreath>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, fireBreathDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 200;
                            Main.projectile[num54].aiStyle = 23; //was 23
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            customAi1 = 1f;
                        }
                        NPC.netUpdate = true;
                    }

                    if (Main.rand.Next(250) == 1)
                    {
                        float num48 = 8f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellIcestormBall>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, iceStormDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 0;//was 70
                            Main.projectile[num54].aiStyle = 1;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            NPC.ai[1] = 1f;
                        }
                        NPC.netUpdate = true;
                    }



                    if (Main.rand.Next(305) == 1)
                    {
                        float num48 = 7f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.FireBreath>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, fireBreathDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 1900;
                            Main.projectile[num54].aiStyle = 23;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            customAi1 = 1f;
                        }
                        NPC.netUpdate = true;
                    }

                    if (Main.rand.Next(800) == 1)
                    {
                        float num48 = 12f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGravity4Ball>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, gravityBallDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 60;
                            Main.projectile[num54].aiStyle = 1;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            customAi1 = 1f;
                        }
                        NPC.netUpdate = true;
                    }
                }


            }
            #endregion
            #region Phase Through Walls
            if ((Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height)))
            {
                NPC.noTileCollide = false;
                NPC.noGravity = false;
            }
            if ((!Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height)))
            {
                NPC.noTileCollide = true;
                NPC.noGravity = true;
                NPC.velocity.Y = 0f;
                if (NPC.position.Y > Main.player[NPC.target].position.Y)
                {
                    NPC.velocity.Y -= 3f;
                }
                if (NPC.position.Y < Main.player[NPC.target].position.Y)
                {
                    NPC.velocity.Y += 8f;
                }
            }
            #endregion


            //if (!Main.bloodMoon)
            //{
            //	if (npc.timeLeft > 5)
            //	{
            //		Main.NewText("You have broken the Covenant of The Abyss...");
            //		for (int i = 0; i < 60; i++)
            //		{
            //			int dustID = Dust.NewDust(npc.position, npc.width, npc.height, 65, Main.rand.Next(-12, 12), Main.rand.Next(-12, 12), 150, default, 7f);
            //			Main.dust[dustID].noGravity = true;
            //		}
            //		npc.active = false;
            //		return;
            //	}
            //
            //}
        }
        public override bool CheckActive()
        {
            return false;
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        #region Gore
        public override void OnKill()
        {
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Hero of Lumelia Gore 1").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Hero of Lumelia Gore 2").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Hero of Lumelia Gore 3").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Hero of Lumelia Gore 2").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Hero of Lumelia Gore 3").Type, 1f);

            if (Main.expertMode)
            {
                NPC.DropBossBags();
            }
            else
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.GuardianSoul>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.DraxEX>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Epilogue>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.EssenceOfTerraria>());
            }

            tsorcRevampWorld.InitiateTheEnd();
        }
        #endregion
    }
}