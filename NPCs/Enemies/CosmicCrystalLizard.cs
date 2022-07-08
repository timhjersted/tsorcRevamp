using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    public class CosmicCrystalLizard : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cosmic Crystal Lizard");
            Main.npcFrameCount[NPC.type] = 29;
        }

        public override void SetDefaults()
        {
            NPC.width = 28;
            NPC.height = 20;
            NPC.aiStyle = -1;
            NPC.damage = 0;
            NPC.knockBackResist = 0.6f;
            NPC.defense = 9999;
            NPC.lifeMax = Main.rand.Next(13, 21);
            NPC.HitSound = SoundID.NPCHit42;
            NPC.DeathSound = SoundID.NPCDeath32;
            NPC.value = 0;
            NPC.rarity = 5;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.Venom] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
            NPC.buffImmune[BuffID.Frostburn] = true;
            NPC.buffImmune[BuffID.Frostburn2] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.ShadowFlame] = true;
            NPC.buffImmune[ModContent.BuffType<Buffs.CrescentMoonlight>()] = true;
            NPC.buffImmune[ModContent.BuffType<Buffs.DarkInferno>()] = true;
            NPC.buffImmune[ModContent.BuffType<Buffs.CrimsonBurn>()] = true;
            NPC.buffImmune[ModContent.BuffType<Buffs.ToxicCatDrain>()] = true;
            NPC.buffImmune[ModContent.BuffType<Buffs.ViruCatDrain>()] = true;
            NPC.buffImmune[ModContent.BuffType<Buffs.BiohazardDrain>()] = true;
            NPC.buffImmune[ModContent.BuffType<Buffs.ElectrocutedBuff>()] = true;
            NPC.buffImmune[ModContent.BuffType<Buffs.PolarisElectrocutedBuff>()] = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.CosmicCrystalLizardBanner>();
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;

            if (NPC.CountNPCS(Mod.Find<ModNPC>("CosmicCrystalLizard").Type) < 1 && (spawnInfo.Player.ZoneRockLayerHeight || spawnInfo.Player.ZoneDirtLayerHeight) && !spawnInfo.Water && !Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].IsHalfBlock && !Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].RightSlope && !Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].LeftSlope && !spawnInfo.Player.ZoneJungle)
            {
                return 0.02f;
            }
            if (NPC.CountNPCS(Mod.Find<ModNPC>("CosmicCrystalLizard").Type) < 1 && (spawnInfo.Player.ZoneRockLayerHeight || spawnInfo.Player.ZoneDirtLayerHeight) && !spawnInfo.Water && !Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].IsHalfBlock && !Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].RightSlope && !Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].LeftSlope && spawnInfo.Player.ZoneJungle)
            {
                return 0.02f;
            }
            if (NPC.CountNPCS(Mod.Find<ModNPC>("CosmicCrystalLizard").Type) < 1 && spawnInfo.Player.ZoneOverworldHeight && !spawnInfo.Water && !Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].IsHalfBlock && !Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].RightSlope && !Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].LeftSlope && (Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY - 2].WallType == WallID.DirtUnsafe || Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY - 2].WallType == WallID.MudUnsafe || Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY - 2].WallType == WallID.Planked))
            {
                return 0.02f;
            }
            return chance;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            Texture2D texture = TextureAssets.Npc[NPC.type].Value;
            Texture2D textureglow = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Enemies/CosmicCrystalLizard_Glow");
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            if (NPC.spriteDirection == -1)
            {
                spriteBatch.Draw(textureglow, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 94, 88), Color.White, NPC.rotation, new Vector2(70, 74), NPC.scale, effects, 0f);
                spriteBatch.Draw(texture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 94, 88), lightColor, NPC.rotation, new Vector2(70, 74), NPC.scale, effects, 0.1f);
            }
            else
            {
                spriteBatch.Draw(textureglow, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 94, 88), Color.White, NPC.rotation, new Vector2(24, 74), NPC.scale, effects, 0f);
                spriteBatch.Draw(texture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 94, 88), lightColor, NPC.rotation, new Vector2(24, 74), NPC.scale, effects, 0.1f);
            }

            return false; //whether to not to draw the base sprite
        }


        #region AI

        private const int AI_State_Slot = 0;
        private const int AI_Timer_Slot = 1;

        private const int State_Idle = 0;
        private const int State_Jump = 2;
        private const int State_Fleeing = 3;
        private const int State_PeaceOut = 4;

        public float AI_State
        {
            get => NPC.ai[AI_State_Slot];
            set => NPC.ai[AI_State_Slot] = value;
        }

        public float AI_Timer
        {
            get => NPC.ai[AI_Timer_Slot];
            set => NPC.ai[AI_Timer_Slot] = value;
        }

        public int spawntimer = 0;
        public int peaceouttimer = 0;
        public int idleframe = 1;
        public int immuneframe = 0;

        public override void AI()
        {

            NPC.netUpdate = false;
            immuneframe++;

            if (immuneframe > 1)
            {
                NPC.immortal = false;
                NPC.defense = 9999;
            }

            if (AI_State == State_Idle)
            {
                Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/CosmicSparkle"), NPC.Center);

                NPC.TargetClosest(true);
                AI_Timer++;

                if (NPC.velocity.Y == 0)
                {
                    NPC.velocity = new Vector2(0f, 0f);
                }

                if (((NPC.HasValidTarget && Main.player[NPC.target].Distance(NPC.Center) < 250f) || NPC.life < NPC.lifeMax) && NPC.collideY)
                {
                    AI_State = State_Fleeing;
                    AI_Timer = 0;
                }

            }
            else if (AI_State == State_Jump)
            {
                AI_Timer++;
                peaceouttimer++;
                if (AI_Timer == 1)
                {
                    NPC.velocity = new Vector2(NPC.direction * -2.7f, -3.6f);
                }
                else if (AI_Timer == 10)
                {
                    AI_State = State_Fleeing;
                    AI_Timer = 0;
                }
            }
            else if (AI_State == State_Fleeing)
            {
                peaceouttimer++;
                NPC.TargetClosest(true);
                if (NPC.direction == 1) //FACING LEFT - vel to move left
                {
                    if (NPC.velocity.X > -2.7f)
                    {
                        NPC.velocity += new Vector2(-.1f, 0); //breaking power after turn
                    }
                    else if (NPC.velocity.X < -4f) //max vel
                    {
                        NPC.velocity += new Vector2(.04f, 0); //slowdown after knockback
                    }
                    else if ((NPC.velocity.X <= -2.7f) && (NPC.velocity.X > -4f))
                    {
                        NPC.velocity += new Vector2(-.03f, 0); //running accel.
                    }
                }

                if (NPC.direction == -1) //FACING RIGHT + vel to move right
                {
                    if (NPC.velocity.X < 2.7f)
                    {
                        NPC.velocity += new Vector2(.1f, 0); //breaking power
                    }
                    else if (NPC.velocity.X > 4f) //max vel
                    {
                        NPC.velocity += new Vector2(-.04f, 0); //slowdown after knockback
                    }
                    else if ((NPC.velocity.X >= 2.7f) && (NPC.velocity.X < 4f))
                    {
                        NPC.velocity += new Vector2(.03f, 0); //running accel.
                    }
                }
                if (NPC.collideX && NPC.collideY)
                {
                    // NPC has stopped upon hitting a block
                    AI_State = State_Jump;
                    peaceouttimer += 1;
                    AI_Timer = 0;
                }
                if (NPC.velocity.X == 0 && NPC.velocity.Y == 0)
                {
                    AI_State = State_Jump;
                }
            }
            else if (AI_State == State_PeaceOut)
            {
                AI_Timer++;

                NPC.noGravity = true;
                NPC.velocity = new Vector2(0, 0);

                if (AI_Timer == 37)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item82, NPC.Center);
                }
                if (AI_Timer == 128)
                {
                    NPC.life = 0;
                }
            }
            if ((peaceouttimer > 150 && Main.rand.NextBool(100)) || peaceouttimer > 420 && (NPC.collideY))
            {
                AI_State = State_PeaceOut;
                AI_Timer = 0;
                peaceouttimer = 0;
            }

            if (AI_State == State_Idle || AI_State == State_Fleeing || AI_State == State_Jump) //Dusts
            {
                if (Main.rand.NextBool(10)) //Yellow
                {
                    Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 170, NPC.velocity.X, NPC.velocity.Y, 100, default(Color), .4f)];
                    dust.velocity *= 0f;
                    dust.noGravity = true;
                    dust.velocity += NPC.velocity;
                    dust.fadeIn = 1f;
                }
                if (Main.rand.NextBool(10)) //Pink
                {
                    Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 272, NPC.velocity.X, NPC.velocity.Y, 100, default(Color), .5f)]; //223, 255, 272
                    dust.velocity *= 0f;
                    dust.noGravity = true;
                    dust.velocity += NPC.velocity;
                    dust.fadeIn = 1f;
                }
                if (Main.rand.NextBool(10)) //Blue
                {
                    Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 185, NPC.velocity.X, NPC.velocity.Y, 100, default(Color), .4f)];
                    dust.velocity *= 0f;
                    dust.noGravity = true;
                    dust.velocity += NPC.velocity;
                    dust.fadeIn = 1f;
                }
                if (Main.rand.NextBool(10)) //Green
                {
                    Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 107, NPC.velocity.X, NPC.velocity.Y, 100, default(Color), .4f)];
                    dust.velocity *= 0f;
                    dust.noGravity = true;
                    dust.velocity += NPC.velocity;
                    dust.fadeIn = 1f;
                }
            }
        }

        #endregion

        #region Animation

        //Idle
        private const int Frame_Idle_1 = 0;
        private const int Frame_Idle_2 = 1;
        private const int Frame_Idle_3 = 2;

        //Fleeing
        private const int Frame_Fleeing_1 = 3;
        private const int Frame_Fleeing_2 = 4;
        private const int Frame_Fleeing_3 = 5;
        private const int Frame_Fleeing_4 = 6;
        private const int Frame_Fleeing_5 = 7;
        private const int Frame_Fleeing_6 = 8;
        private const int Frame_Fleeing_7 = 9;
        private const int Frame_Fleeing_8 = 10;
        private const int Frame_Fleeing_9 = 11;
        private const int Frame_Fleeing_10 = 12;

        //PeaceOut
        private const int Frame_PeaceOut_1 = 13;
        private const int Frame_PeaceOut_2 = 14;
        private const int Frame_PeaceOut_3 = 15;
        private const int Frame_PeaceOut_4 = 16;
        private const int Frame_PeaceOut_5 = 17;
        private const int Frame_PeaceOut_6 = 18;
        private const int Frame_PeaceOut_7 = 19;
        private const int Frame_PeaceOut_8 = 20;
        private const int Frame_PeaceOut_9 = 21;
        private const int Frame_PeaceOut_10 = 22;
        private const int Frame_PeaceOut_11 = 23;
        private const int Frame_PeaceOut_12 = 24;
        private const int Frame_PeaceOut_13 = 25;
        private const int Frame_PeaceOut_14 = 26;
        private const int Frame_PeaceOut_15 = 27;
        private const int Frame_PeaceOut_16 = 28;

        public override void FindFrame(int frameHeight)
        {

            // For the most part, our animation matches up with our states.
            if (AI_State == State_Idle)
            {
                // Cycle through all idle frames
                NPC.spriteDirection = NPC.direction;
                NPC.frameCounter += Main.rand.Next(1, 4);
                if (NPC.frameCounter < 250 && idleframe == 1)
                {
                    NPC.frame.Y = Frame_Idle_1 * frameHeight;
                }
                if ((idleframe == 1 && NPC.frameCounter > 250) || (idleframe == 2 && NPC.frameCounter > 20) || (idleframe == 3 && NPC.frameCounter > 200) || (idleframe == 4 && NPC.frameCounter > 20))
                {
                    NPC.frameCounter = 0;
                    idleframe++;
                }
                else if (NPC.frameCounter < 20 && idleframe == 2)
                {
                    NPC.frame.Y = Frame_Idle_2 * frameHeight;
                }
                else if (NPC.frameCounter < 200 && idleframe == 3)
                {
                    NPC.frame.Y = Frame_Idle_3 * frameHeight;
                }
                else if (NPC.frameCounter < 20 && idleframe == 4)
                {
                    NPC.frame.Y = Frame_Idle_2 * frameHeight;
                }
                else if (idleframe == 5)
                {
                    NPC.frameCounter = 0;
                    idleframe = 1;
                }
            }

            else if (AI_State == State_Jump)
            {
                NPC.frame.Y = Frame_Fleeing_3 * frameHeight;
            }

            else if (AI_State == State_Fleeing)
            {
                // Cycle through all running frames
                NPC.spriteDirection = NPC.direction;
                NPC.frameCounter++;
                if (NPC.frameCounter < 3)
                {
                    NPC.frame.Y = Frame_Fleeing_1 * frameHeight;
                }
                else if (NPC.frameCounter < 6)
                {
                    NPC.frame.Y = Frame_Fleeing_2 * frameHeight;
                }
                else if (NPC.frameCounter < 9)
                {
                    NPC.frame.Y = Frame_Fleeing_3 * frameHeight;
                }
                else if (NPC.frameCounter < 12)
                {
                    NPC.frame.Y = Frame_Fleeing_4 * frameHeight;
                }
                else if (NPC.frameCounter < 15)
                {
                    NPC.frame.Y = Frame_Fleeing_5 * frameHeight;
                }
                else if (NPC.frameCounter < 18)
                {
                    NPC.frame.Y = Frame_Fleeing_6 * frameHeight;
                }
                else if (NPC.frameCounter < 21)
                {
                    NPC.frame.Y = Frame_Fleeing_7 * frameHeight;
                }
                else if (NPC.frameCounter < 24)
                {
                    NPC.frame.Y = Frame_Fleeing_8 * frameHeight;
                }
                else if (NPC.frameCounter < 27)
                {
                    NPC.frame.Y = Frame_Fleeing_9 * frameHeight;
                }
                else if (NPC.frameCounter < 30)
                {
                    NPC.frame.Y = Frame_Fleeing_10 * frameHeight;
                }
                else
                {
                    NPC.frameCounter = 0;
                }
            }

            else if (AI_State == State_PeaceOut)
            {
                //play despawn frames once
                NPC.spriteDirection = NPC.direction;
                NPC.frameCounter++;

                if (NPC.frameCounter < 15)
                {
                    NPC.frame.Y = Frame_Fleeing_10 * frameHeight;
                    if (NPC.direction == -1)
                    {
                        if (Main.rand.NextBool(10)) //Yellow
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 170, -1, -1, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Pink
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 272, -1, -1, 100, default(Color), .5f)]; //223, 255, 272
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Blue
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 185, -1, -1, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Green
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 107, -1, -1, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                    }
                    if (NPC.direction == 1)
                    {
                        if (Main.rand.NextBool(10)) //Yellow
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 170, 1, -1, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Pink
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 272, 1, -1, 100, default(Color), .5f)]; //223, 255, 272
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Blue
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 185, 1, -1, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Green
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 107, 1, -1, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                    }
                }

                else if (NPC.frameCounter < 27)
                {
                    NPC.frame.Y = Frame_PeaceOut_1 * frameHeight;
                    if (NPC.direction == -1)
                    {
                        Lighting.AddLight(((int)NPC.position.X - 24) / 16, ((int)NPC.position.Y - 36) / 16, 0.2f, 0.2f, 0.2f); //when facing right
                        if (Main.rand.NextBool(10)) //Yellow
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 170, -1, -1, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Pink
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 272, -1, -1, 100, default(Color), .5f)]; //223, 255, 272
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Blue
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 185, -1, -1, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Green
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 107, -1, -1, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                    }
                    if (NPC.direction == 1)
                    {
                        Lighting.AddLight(((int)NPC.position.X + 50) / 16, ((int)NPC.position.Y - 36) / 16, 0.2f, 0.2f, 0.2f); //when facing left
                        if (Main.rand.NextBool(10)) //Yellow
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 170, 1, -1, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Pink
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 272, 1, -1, 100, default(Color), .5f)]; //223, 255, 272
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Blue
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 185, 1, -1, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Green
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 107, 1, -1, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                    }
                }

                else if (NPC.frameCounter < 37)
                {
                    NPC.dontTakeDamage = true;
                    NPC.frame.Y = Frame_PeaceOut_2 * frameHeight;
                    if (NPC.direction == -1)
                    {
                        Lighting.AddLight(((int)NPC.position.X - 24) / 16, ((int)NPC.position.Y - 36) / 16, 0.25f, 0.25f, 0.25f);

                        if (Main.rand.NextBool(10)) //Yellow
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 170, -2, -2, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Pink
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 272, -2, -2, 100, default(Color), .5f)]; //223, 255, 272
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Blue
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 185, -2, -2, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Green
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 107, -2, -2, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                    }
                    if (NPC.direction == 1)
                    {
                        Lighting.AddLight(((int)NPC.position.X + 50) / 16, ((int)NPC.position.Y - 36) / 16, 0.25f, 0.25f, 0.25f);
                        if (Main.rand.NextBool(10)) //Yellow
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 170, 2, -2, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Pink
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 272, 2, -2, 100, default(Color), .5f)]; //223, 255, 272
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Blue
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 185, 2, -2, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Green
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 107, 2, -2, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                    }

                }

                else if (NPC.frameCounter < 45)
                {
                    NPC.dontTakeDamage = true;
                    NPC.frame.Y = Frame_PeaceOut_3 * frameHeight;
                    if (NPC.direction == -1)
                    {
                        Lighting.AddLight(((int)NPC.position.X - 24) / 16, ((int)NPC.position.Y - 36) / 16, 0.3f, 0.3f, 0.3f);

                        if (Main.rand.NextBool(8)) //Yellow
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 170, -2, -2, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(8)) //Pink
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 272, -2, -2, 100, default(Color), .5f)]; //223, 255, 272
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(8)) //Blue
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 185, -2, -2, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(8)) //Green
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 107, -2, -2, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                    }
                    if (NPC.direction == 1)
                    {
                        Lighting.AddLight(((int)NPC.position.X + 50) / 16, ((int)NPC.position.Y - 36) / 16, 0.3f, 0.3f, 0.3f);

                        if (Main.rand.NextBool(8)) //Yellow
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 170, 2, -2, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(8)) //Pink
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 272, 2, -2, 100, default(Color), .5f)]; //223, 255, 272
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(8)) //Blue
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 185, 2, -2, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(8)) //Green
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 2), 28, 18, 107, 2, -2, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                    }

                }

                else if (NPC.frameCounter < 53)
                {
                    NPC.dontTakeDamage = true;
                    NPC.frame.Y = Frame_PeaceOut_4 * frameHeight;
                    if (NPC.direction == -1)
                    {
                        Lighting.AddLight(((int)NPC.position.X - 24) / 16, ((int)NPC.position.Y - 36) / 16, 0.35f, 0.35f, 0.35f);

                        if (Main.rand.NextBool(6)) //Yellow
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 4), 28, 18, 170, -2, -2, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(6)) //Pink
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 4), 28, 18, 272, -2, -2, 100, default(Color), .5f)]; //223, 255, 272
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(6)) //Blue
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 4), 28, 18, 185, -2, -2, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(6)) //Green
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 4), 28, 18, 107, -2, -2, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                    }
                    if (NPC.direction == 1)
                    {
                        Lighting.AddLight(((int)NPC.position.X + 50) / 16, ((int)NPC.position.Y - 36) / 16, 0.35f, 0.35f, 0.35f);

                        if (Main.rand.NextBool(6)) //Yellow
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 4), 28, 18, 170, 2, -2, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(6)) //Pink
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 4), 28, 18, 272, 2, -2, 100, default(Color), .5f)]; //223, 255, 272
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(6)) //Blue
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 4), 28, 18, 185, 2, -2, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(6)) //Green
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y - 4), 28, 18, 107, 2, -2, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                    }
                }

                else if (NPC.frameCounter < 61)
                {
                    NPC.dontTakeDamage = true;
                    NPC.frame.Y = Frame_PeaceOut_5 * frameHeight;
                    if (NPC.direction == -1)
                    {
                        Lighting.AddLight(((int)NPC.position.X - 24) / 16, ((int)NPC.position.Y - 36) / 16, 0.4f, 0.4f, 0.4f);

                        if (Main.rand.NextBool(10)) //Yellow
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X - 2, NPC.position.Y - 6), 28, 18, 170, -3, -3, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Pink
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X - 2, NPC.position.Y - 6), 28, 18, 272, -3, -3, 100, default(Color), .5f)]; //223, 255, 272
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Blue
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X - 2, NPC.position.Y - 6), 28, 18, 185, -3, -3, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Green
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X - 2, NPC.position.Y - 6), 28, 18, 107, -3, -3, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                    }
                    if (NPC.direction == 1)
                    {
                        Lighting.AddLight(((int)NPC.position.X + 50) / 16, ((int)NPC.position.Y - 36) / 16, 0.4f, 0.4f, 0.4f);

                        if (Main.rand.NextBool(10)) //Yellow
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X - 2, NPC.position.Y - 6), 28, 18, 170, 3, -3, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Pink
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X - 2, NPC.position.Y - 6), 28, 18, 272, 3, -3, 100, default(Color), .5f)]; //223, 255, 272
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Blue
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X - 2, NPC.position.Y - 6), 28, 18, 185, 3, -3, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Green
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X - 2, NPC.position.Y - 6), 28, 18, 107, 3, -3, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                    }
                }

                else if (NPC.frameCounter < 69)
                {
                    if (NPC.direction == -1)
                    {
                        Lighting.AddLight(((int)NPC.position.X - 24) / 16, ((int)NPC.position.Y - 36) / 16, 0.4f, 0.4f, 0.4f);

                        if (Main.rand.NextBool(10)) //Yellow
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X - 2, NPC.position.Y - 6), 28, 18, 170, -3, -3, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Pink
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X - 2, NPC.position.Y - 6), 28, 18, 272, -3, -3, 100, default(Color), .5f)]; //223, 255, 272
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Blue
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X - 2, NPC.position.Y - 6), 28, 18, 185, -3, -3, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Green
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X - 2, NPC.position.Y - 6), 28, 18, 107, -3, -3, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                    }
                    if (NPC.direction == 1)
                    {
                        Lighting.AddLight(((int)NPC.position.X + 50) / 16, ((int)NPC.position.Y - 36) / 16, 0.4f, 0.4f, 0.4f);

                        if (Main.rand.NextBool(10)) //Yellow
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X - 2, NPC.position.Y - 6), 28, 18, 170, 3, -3, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Pink
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X - 2, NPC.position.Y - 6), 28, 18, 272, 3, -3, 100, default(Color), .5f)]; //223, 255, 272
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Blue
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X - 2, NPC.position.Y - 6), 28, 18, 185, 3, -3, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                        if (Main.rand.NextBool(10)) //Green
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.position.X - 2, NPC.position.Y - 6), 28, 18, 107, 3, -3, 100, default(Color), .4f)];
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                        }
                    }
                    NPC.dontTakeDamage = true;
                    NPC.frame.Y = Frame_PeaceOut_6 * frameHeight;
                }
                else if (NPC.frameCounter < 77)
                {
                    if (NPC.direction == -1)
                    {
                        Lighting.AddLight(((int)NPC.position.X - 24) / 16, ((int)NPC.position.Y - 36) / 16, 0.45f, 0.45f, 0.45f);
                    }
                    if (NPC.direction == 1)
                    {
                        Lighting.AddLight(((int)NPC.position.X + 50) / 16, ((int)NPC.position.Y - 36) / 16, 0.45f, 0.45f, 0.45f);
                    }
                    NPC.frame.Y = Frame_PeaceOut_7 * frameHeight;
                    NPC.dontTakeDamage = true;
                }
                else if (NPC.frameCounter < 85)
                {
                    if (NPC.direction == -1)
                    {
                        Lighting.AddLight(((int)NPC.position.X - 24) / 16, ((int)NPC.position.Y - 36) / 16, 0.4f, 0.4f, 0.4f);
                    }
                    if (NPC.direction == 1)
                    {
                        Lighting.AddLight(((int)NPC.position.X + 50) / 16, ((int)NPC.position.Y - 36) / 16, 0.4f, 0.4f, 0.4f);
                    }
                    NPC.frame.Y = Frame_PeaceOut_8 * frameHeight;
                    NPC.dontTakeDamage = true;
                }
                else if (NPC.frameCounter < 93)
                {
                    if (NPC.direction == -1)
                    {
                        Lighting.AddLight(((int)NPC.position.X - 24) / 16, ((int)NPC.position.Y - 36) / 16, 0.35f, 0.35f, 0.35f);
                    }
                    if (NPC.direction == 1)
                    {
                        Lighting.AddLight(((int)NPC.position.X + 50) / 16, ((int)NPC.position.Y - 36) / 16, 0.35f, 0.35f, 0.35f);
                    }
                    NPC.frame.Y = Frame_PeaceOut_9 * frameHeight;
                    NPC.dontTakeDamage = true;
                }
                else if (NPC.frameCounter < 100)
                {
                    if (NPC.direction == -1)
                    {
                        Lighting.AddLight(((int)NPC.position.X - 24) / 16, ((int)NPC.position.Y - 36) / 16, 0.3f, 0.3f, 0.3f);
                    }
                    if (NPC.direction == 1)
                    {
                        Lighting.AddLight(((int)NPC.position.X + 50) / 16, ((int)NPC.position.Y - 36) / 16, 0.3f, 0.3f, 0.3f);
                    }
                    NPC.frame.Y = Frame_PeaceOut_10 * frameHeight;
                    NPC.dontTakeDamage = true;
                }
                else if (NPC.frameCounter < 107)
                {
                    if (NPC.direction == -1)
                    {
                        Lighting.AddLight(((int)NPC.position.X - 24) / 16, ((int)NPC.position.Y - 36) / 16, 0.2f, 0.2f, 0.2f);
                    }
                    if (NPC.direction == 1)
                    {
                        Lighting.AddLight(((int)NPC.position.X + 50) / 16, ((int)NPC.position.Y - 36) / 16, 0.2f, 0.2f, 0.2f);
                    }
                    NPC.frame.Y = Frame_PeaceOut_11 * frameHeight;
                    NPC.dontTakeDamage = true;
                }
                else if (NPC.frameCounter < 114)
                {
                    if (NPC.direction == -1)
                    {
                        Lighting.AddLight(((int)NPC.position.X - 24) / 16, ((int)NPC.position.Y - 36) / 16, 0.15f, 0.15f, 0.15f);
                    }
                    if (NPC.direction == 1)
                    {
                        Lighting.AddLight(((int)NPC.position.X + 50) / 16, ((int)NPC.position.Y - 36) / 16, 0.15f, 0.15f, 0.15f);
                    }
                    NPC.frame.Y = Frame_PeaceOut_12 * frameHeight;
                    NPC.dontTakeDamage = true;
                }
                else if (NPC.frameCounter < 121)
                {
                    if (NPC.direction == -1)
                    {
                        Lighting.AddLight(((int)NPC.position.X - 24) / 16, ((int)NPC.position.Y - 36) / 16, 0.1f, 0.1f, 0.1f);
                    }
                    if (NPC.direction == 1)
                    {
                        Lighting.AddLight(((int)NPC.position.X + 50) / 16, ((int)NPC.position.Y - 36) / 16, 0.1f, 0.1f, 0.1f);
                    }
                    NPC.frame.Y = Frame_PeaceOut_13 * frameHeight;
                    NPC.dontTakeDamage = true;
                }
                else if (NPC.frameCounter < 123)
                {
                    NPC.frame.Y = Frame_PeaceOut_14 * frameHeight;
                    NPC.dontTakeDamage = true;
                }
                else if (NPC.frameCounter < 126)
                {
                    NPC.frame.Y = Frame_PeaceOut_15 * frameHeight;
                    NPC.dontTakeDamage = true;
                }
                else if (NPC.frameCounter < 128)
                {
                    NPC.frame.Y = Frame_PeaceOut_16 * frameHeight;
                    NPC.dontTakeDamage = true;
                }
            }
        }
        #endregion


        public override void HitEffect(int hitDirection, double damage)
        {
            for (int i = 0; i < 5; i++)
            {
                int DustType = 191;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];

                dust.scale *= .70f + Main.rand.Next(-30, 31) * 0.01f;
                dust.velocity.Y = Main.rand.Next(-2, 0);
                dust.noGravity = false;
                dust.alpha = 0;
            }
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 191, 0, Main.rand.Next(-2, 0), 0, default(Color), .75f);
                }
            }
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (Main.rand.NextBool(2) && immuneframe >= 1 && !crit)
            {
                NPC.immortal = true;
                immuneframe = 0;
            }
            else
            {
                damage = 1;
            }
            if (crit && immuneframe >= 1)
            {
                damage = 2;
                NPC.defense = 0;
                immuneframe = 0;
            }
        }
        public override void ModifyHitByItem(Player player, Item item, ref int damage, ref float knockback, ref bool crit)
        {
            if (crit && immuneframe >= 1)
            {
                damage = 2;
                NPC.defense = 0;
                immuneframe = 0;
            }
        }
        public override void OnKill()
        {
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), Mod.Find<ModItem>("DarkSoul").Type, 400);
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), Mod.Find<ModItem>("EternalCrystal").Type); //always drops 1

            if (Main.rand.NextFloat() >= 0.2f) // 80% chance
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.EndurancePotion);
            }
            if (Main.rand.NextFloat() >= 0.2f) // 80% chance
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), Mod.Find<ModItem>("SoulSiphonPotion").Type);
            }


            if (NPC.lifeMax == 20 || NPC.lifeMax == 19) //the higher the npc.lifeMax, the higher the chance of getting a second crystal
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), Mod.Find<ModItem>("EternalCrystal").Type);
            }
            else if (NPC.lifeMax == 18 && Main.rand.NextFloat() >= 0.15f)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), Mod.Find<ModItem>("EternalCrystal").Type);
            }
            else if (NPC.lifeMax == 17 && Main.rand.NextFloat() >= 0.3f)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), Mod.Find<ModItem>("EternalCrystal").Type);
            }
            else if (NPC.lifeMax == 16 && Main.rand.NextFloat() >= 0.45f)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), Mod.Find<ModItem>("EternalCrystal").Type);
            }
            else if (NPC.lifeMax == 15 && Main.rand.NextFloat() >= 0.6f)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), Mod.Find<ModItem>("EternalCrystal").Type);
            }
            else if (NPC.lifeMax == 14 && Main.rand.NextFloat() >= 0.75f)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), Mod.Find<ModItem>("EternalCrystal").Type);
            }
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.15f, (float)Main.rand.Next(-30, 31) * 0.15f), Mod.Find<ModGore>("CosmicCrystalLizard_Gore1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.15f, (float)Main.rand.Next(-30, 31) * 0.15f), Mod.Find<ModGore>("CosmicCrystalLizard_Gore2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.15f, (float)Main.rand.Next(-30, 31) * 0.15f), Mod.Find<ModGore>("CosmicCrystalLizard_Gore3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.15f, (float)Main.rand.Next(-30, 31) * 0.15f), Mod.Find<ModGore>("CosmicCrystalLizard_Gore4").Type, 1f);
            }
        }
    }
}
