using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Potions;
using static tsorcRevamp.SpawnHelper;

namespace tsorcRevamp.NPCs.Enemies
{
    class FirebombHollow : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 14;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.knockBackResist = 0.5f;
            NPC.lifeMax = 60;
            NPC.damage = 16;
            NPC.value = 250;
            NPC.height = 40;
            NPC.width = 20;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.defense = 6;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.FirebombHollowBanner>();

            if (Main.hardMode)
            {
                NPC.lifeMax = 500;
                NPC.defense = 25;
                NPC.value = 1550;
                NPC.damage = 42;
                NPC.knockBackResist = 0.2f;
            }

            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 1520;
                NPC.defense = 80;
                NPC.value = 2250;
                NPC.damage = 64;
                NPC.knockBackResist = 0.05f;

            }

        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            bool FrozenOcean = spawnInfo.SpawnTileX > (Main.maxTilesX - 800);
            bool Ocean = spawnInfo.SpawnTileX < 800 || FrozenOcean;
            Player p = spawnInfo.Player;
            if (spawnInfo.Invasion)
            {
                chance = 0;
                return chance;
            }
            if (spawnInfo.Player.townNPCs > 1f) return 0f;

            if (spawnInfo.Water || Sky(p)) return 0f;
            if (spawnInfo.Player.ZoneGlowshroom) return 0f;

            //Hollow enemies
           
            if (!Main.hardMode && spawnInfo.Player.ZoneDungeon) return 0.1f;
            if (Main.hardMode && spawnInfo.Player.ZoneDungeon) return 0.05f;
            if (Main.hardMode && spawnInfo.Lihzahrd) return 0.2f;

            if (tsorcRevampWorld.SuperHardMode && !(Ocean || spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson || spawnInfo.Player.ZoneUnderworldHeight)) return 0.08f;
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneOverworldHeight && !(Ocean || spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson)) return 0.23f;
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneDesert && !Ocean) return 0.15f;
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneDungeon) return 0.27f; //.08% is 4.28% .16 is 8% .32 is 16% .64 is 32%

            if (Main.expertMode && Main.bloodMoon && spawnInfo.Player.ZoneOverworldHeight && !(Ocean || spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson || spawnInfo.Player.ZoneUnderworldHeight)) return chance = 0.1f;

            if (Main.expertMode && Main.bloodMoon && !spawnInfo.Player.ZoneUnderworldHeight && !(Ocean || spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson || spawnInfo.Player.ZoneUnderworldHeight)) return chance = 0.06f;

            if (((!Main.expertMode && (NPC.downedBoss1 || NPC.downedBoss2)) || Main.expertMode) && spawnInfo.Player.ZoneOverworldHeight && Main.dayTime && !(Ocean || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson)) return chance = 0.05f;
            if (((!Main.expertMode && (NPC.downedBoss1 || NPC.downedBoss2)) || Main.expertMode) && spawnInfo.Player.ZoneOverworldHeight && !Main.dayTime && !(Ocean || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson)) return chance = 0.08f;

            if (((!Main.expertMode && (NPC.downedBoss1 || NPC.downedBoss2)) || Main.expertMode) && (spawnInfo.Player.ZoneDirtLayerHeight || spawnInfo.Player.ZoneRockLayerHeight) && Main.dayTime && !(Ocean || spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneHallow || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson || spawnInfo.Player.ZoneSnow)) return chance = 0.06f;
            if (((!Main.expertMode && (NPC.downedBoss1 || NPC.downedBoss2)) || Main.expertMode) && (spawnInfo.Player.ZoneDirtLayerHeight || spawnInfo.Player.ZoneRockLayerHeight) && !Main.dayTime && !(Ocean || spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneHallow || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson || spawnInfo.Player.ZoneSnow)) return chance = 0.09f;


            if ((!Main.expertMode && (NPC.downedBoss1 || NPC.downedBoss2)) || Main.expertMode && !(Ocean || spawnInfo.Player.ZoneUnderworldHeight || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson)) return chance = 0.04f;

            return chance;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.SoulCoin>(), 1, 2, 4));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Weapons.Throwing.Firebomb>(), 1, 3, 6));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.FadingSoul>(), 15));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.CharcoalPineResin>(), 5));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<Lifegem>()));

            int[] armorIDs = new int[] {
                ModContent.ItemType<Items.Armors.Magic.RedClothHat>(),
                ModContent.ItemType<Items.Armors.Magic.RedClothTunic>(),
                ModContent.ItemType<Items.Armors.Magic.RedClothPants>(),
            };
            npcLoot.Add(new DropMultiple(armorIDs, 30, 1, !NPC.downedBoss1));
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 10; i++)
            {
                int DustType = 5;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X += Main.rand.Next(-50, 51) * 0.04f;
                dust.velocity.Y += Main.rand.Next(-50, 51) * 0.04f;
                dust.scale *= .8f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 80; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 54, 2.5f * hit.HitDirection, -1.5f, 70, default(Color), 1f);
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 5, 1.5f * hit.HitDirection, -2.5f, 50, default(Color), 1f);
                }
            }
        }


        #region AI

        private const int AI_State_Slot = 0;
        private const int AI_Universal_Timer_Slot = 1;
        private const int AI_State_Timer_1_Slot = 2;
        private const int AI_State_Timer_2_Slot = 3;

        private const int State_Pursuing = 0;
        private const int State_Firebombing = 1;


        public float AI_State
        {
            get => NPC.ai[AI_State_Slot];
            set => NPC.ai[AI_State_Slot] = value;
        }
        public float AI_Universal_Timer
        {
            get => NPC.ai[AI_Universal_Timer_Slot];
            set => NPC.ai[AI_Universal_Timer_Slot] = value;
        }

        public float AI_State_Timer_1
        {
            get => NPC.ai[AI_State_Timer_1_Slot];
            set => NPC.ai[AI_State_Timer_1_Slot] = value;
        }

        public float AI_State_Timer_2
        {
            get => NPC.ai[AI_State_Timer_2_Slot];
            set => NPC.ai[AI_State_Timer_2_Slot] = value;
        }



        public override void AI()
        {
            Player player = Main.player[NPC.target];


            int lifePercentage = (NPC.life * 100) / NPC.lifeMax;
            float acceleration = 0.02f;
            float top_speed = (lifePercentage * -0.015f) + 2f; //Increase speed the lower the enemy HP%
            float braking_power = 0.1f; //Breaking power to slow down after moving above top_speed


            //Debug utilities

            //Main.NewText(Math.Abs(npc.velocity.X));
            //Main.NewText("AI_State is " + AI_State);
            //Main.NewText("AI_Universal_Timer is " + AI_Universal_Timer);
            //Main.NewText("AI_State_Timer_1 is " + AI_State_Timer_1);
            //Main.NewText("AI_Timer is " + AI_Timer);
            //Main.NewText("npc.frameCounter is " + npc.frameCounter);
            //Main.NewText("Distance is " + npc.Distance(player.Center));
            //Main.NewText("knockbackresist is " + npc.knockBackResist);


            #region AI_State Independent


            #region Check if standing on a solid tile

            bool standing_on_solid_tile = false;
            int x_in_front = (int)((NPC.position.X + (float)(NPC.width / 2) + (float)(15 * NPC.direction)) / 16f); // 15 pix in front of center of mass
            int y_above_feet = (int)((NPC.position.Y + (float)NPC.height - 15f) / 16f); // 15 pix above feet
            int y_below_feet = (int)(NPC.position.Y + (float)NPC.height + 8f) / 16;
            if (NPC.velocity.Y == 0f) // no jump/fall
            {
                int x_left_edge = (int)NPC.position.X / 16;
                int x_right_edge = (int)(NPC.position.X + (float)NPC.width) / 16;
                for (int l = x_left_edge; l <= x_right_edge; l++) // check every block under feet
                {
                    if (Main.tile[l, y_below_feet] == null) // null tile means ??
                        return;

                    if (Main.tile[l, y_below_feet].HasTile && Main.tileSolid[(int)Main.tile[l, y_below_feet].TileType]) // tile exists and is solid
                    {
                        standing_on_solid_tile = true;
                        break; // one is enough so stop checking
                    }
                } // END traverse blocks under feet
            } // END no jump/fall

            #endregion


            if (AI_Universal_Timer < 80)
            {
                AI_Universal_Timer++;
            }

            /*if (AI_Universal_Timer >= 60 && AI_Universal_Timer <= 70)
            {
                if (npc.direction == 1)
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(npc.position.X + 9, npc.position.Y + 1), 4, 4, 183, npc.velocity.X, npc.velocity.Y, 180, default(Color), 1.5f)];
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += npc.velocity;
                }

                else
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(npc.position.X + 3, npc.position.Y + 1), 4, 4, 183, npc.velocity.X, npc.velocity.Y, 180, default(Color), 1.5f)];
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += npc.velocity;
                }

            }

            if (AI_Universal_Timer >= 70 && AI_Universal_Timer <= 80)
            {
                if (npc.direction == 1)
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(npc.position.X + 9, npc.position.Y + 1), 4, 4, 183, npc.velocity.X, npc.velocity.Y, 180, default(Color), 0.8f)];
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += npc.velocity;
                }

                else
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(npc.position.X + 3, npc.position.Y + 1), 4, 4, 183, npc.velocity.X, npc.velocity.Y, 180, default(Color), 0.8f)];
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += npc.velocity;
                }
            }*/


            #endregion


            // PURSUING
            if (AI_State == State_Pursuing)
            {

                #region Target player, turn if can't reach player


                if (AI_State_Timer_1 == 0)
                {
                    NPC.TargetClosest(true); //  Target the closest player & face him (If passed as a parameter, a bool will determine whether it should face the target or not)
                }

                if (NPC.velocity.X == 0)
                {
                    AI_State_Timer_1++;
                    if (AI_State_Timer_1 > 120 && NPC.velocity.Y == 0)
                    {
                        NPC.direction *= -1;
                        NPC.spriteDirection = NPC.direction;
                        AI_State_Timer_1 = 50;
                    }
                }

                if (Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                {
                    AI_State_Timer_1 = 0;
                }

                #endregion

                #region Melee Movement & Drop through platforms - but also sometimes the world :(

                if (Math.Abs(NPC.velocity.X) > top_speed && NPC.velocity.Y == 0)
                {
                    NPC.velocity *= (1f - braking_power); //breaking
                }

                else
                {
                    NPC.velocity.X += NPC.direction * acceleration; //accelerating
                }


                if (NPC.direction == 1) //breaking power after turning, to turn fast or to "slip"
                {
                    if (NPC.velocity.X > -top_speed)
                    {
                        NPC.velocity.X += 0.085f;
                    }
                }

                else
                {
                    if (NPC.velocity.X < top_speed)
                    {
                        NPC.velocity.X += -0.085f;
                    }
                }

                //Speed limits
                if (NPC.velocity.X > 4f) //hard limit of 4f
                {
                    NPC.velocity.X = 4f;
                }

                if (NPC.velocity.X < -4f)
                {
                    NPC.velocity.X = -4f;
                }


                if (Main.tile[(int)NPC.position.X / 16, y_below_feet].TileType == TileID.Platforms && Main.tile[(int)(NPC.position.X + (float)NPC.width) / 16, y_below_feet].TileType == TileID.Platforms && NPC.position.Y < (player.position.Y - 4 * 16))
                {
                    NPC.noTileCollide = true;
                }
                else { NPC.noTileCollide = false; }

                #endregion

                #region New Tile()s, jumping
                if (standing_on_solid_tile)  //  if standing on solid tile
                {
                    if (Main.tile[x_in_front, y_above_feet] == null)
                    {
                        Main.tile[x_in_front, y_above_feet].ClearTile();
                    }

                    if (Main.tile[x_in_front, y_above_feet - 1] == null)
                    {
                        Main.tile[x_in_front, y_above_feet - 1].ClearTile();
                    }

                    if (Main.tile[x_in_front, y_above_feet - 2] == null)
                    {
                        Main.tile[x_in_front, y_above_feet - 2].ClearTile();
                    }

                    if (Main.tile[x_in_front, y_above_feet - 3] == null)
                    {
                        Main.tile[x_in_front, y_above_feet - 3].ClearTile();
                    }

                    if (Main.tile[x_in_front, y_above_feet + 1] == null)
                    {
                        Main.tile[x_in_front, y_above_feet + 1].ClearTile();
                    }
                    //  create? 2 other tiles farther in front
                    if (Main.tile[x_in_front + NPC.direction, y_above_feet - 1] == null)
                    {
                        Main.tile[x_in_front + NPC.direction, y_above_feet - 1].ClearTile();
                    }

                    if (Main.tile[x_in_front + NPC.direction, y_above_feet + 1] == null)
                    {
                        Main.tile[x_in_front + NPC.direction, y_above_feet + 1].ClearTile();
                    }

                    else // standing on solid tile but not in front of a passable door
                    {
                        if ((NPC.velocity.X < 0f && NPC.spriteDirection == -1) || (NPC.velocity.X > 0f && NPC.spriteDirection == 1))
                        {  //  moving forward
                            if (Main.tile[x_in_front, y_above_feet - 2].HasTile && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet - 2].TileType])
                            { // 3 blocks above ground level(head height) blocked
                                if (Main.tile[x_in_front, y_above_feet - 3].HasTile && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet - 3].TileType])
                                { // 4 blocks above ground level(over head) blocked
                                    NPC.velocity.Y = -8f; // jump with power 8 (for 4 block steps)
                                    NPC.netUpdate = true;
                                }
                                else
                                {
                                    NPC.velocity.Y = -7f; // jump with power 7 (for 3 block steps)
                                    NPC.netUpdate = true;
                                }
                            } // for everything else, head height clear:
                            else if (Main.tile[x_in_front, y_above_feet - 1].HasTile && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet - 1].TileType])
                            { // 2 blocks above ground level(mid body height) blocked
                                NPC.velocity.Y = -6f; // jump with power 6 (for 2 block steps)
                                NPC.netUpdate = true;
                            }
                            else if (Main.tile[x_in_front, y_above_feet].HasTile && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet].TileType])
                            { // 1 block above ground level(foot height) blocked
                                NPC.velocity.Y = -5f; // jump with power 5 (for 1 block steps)
                                NPC.netUpdate = true;
                            }
                            else if (NPC.directionY < 0 && (!Main.tile[x_in_front, y_above_feet + 1].HasTile || !Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet + 1].TileType]) && (!Main.tile[x_in_front + NPC.direction, y_above_feet + 1].HasTile || !Main.tileSolid[(int)Main.tile[x_in_front + NPC.direction, y_above_feet + 1].TileType]))
                            { // rising? & jumps gaps & no solid tile ahead to step on for 2 spaces in front
                                NPC.velocity.Y = -8f; // jump with power 8
                                NPC.velocity.X = NPC.velocity.X * 1.5f; // jump forward hard as well; we're trying to jump a gap
                                NPC.netUpdate = true;
                            }

                        } // END moving forward, still: standing on solid tile but not in front of a passable door
                    }
                }

                #endregion

                if (Math.Abs(NPC.Center.X - player.Center.X) < 15 * 16 && NPC.velocity.Y == 0 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                {
                    NPC.velocity.X = 0;
                }

                if (AI_Universal_Timer == 80 && standing_on_solid_tile && Math.Abs(NPC.Center.X - player.Center.X) < 15 * 16 && NPC.velocity.Y == 0 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                {
                    AI_State = State_Firebombing;
                    AI_State_Timer_1 = 0;
                    NPC.frameCounter = 0;
                }
            }

            // FIREBOMBING
            if (AI_State == State_Firebombing)
            {
                AI_State_Timer_1 += 1f;

                if (!standing_on_solid_tile || NPC.velocity.Y != 0)
                {
                    AI_State = State_Pursuing;
                    AI_Universal_Timer = 40;
                    AI_State_Timer_1 = 0;
                    NPC.frameCounter = 0;
                }

                if (AI_State_Timer_1 < 0)
                {
                    if (NPC.velocity.X > 2f) //hard limit of 6f
                    {
                        NPC.velocity.X = 2f;
                    }
                    if (NPC.velocity.X < -2f) //both directions
                    {
                        NPC.velocity.X = -2f;
                    }
                }

                if (AI_State_Timer_1 <= 10)
                {
                    if (NPC.direction == 1 && NPC.velocity.Y > 0.5f) { NPC.velocity.X -= 0.15f; }
                    if (NPC.direction == -1 && NPC.velocity.Y < -0.5f) { NPC.velocity.X += 0.15f; }
                }
                if (AI_State_Timer_1 > 10 && NPC.velocity.Y == 0 && standing_on_solid_tile)
                {
                    NPC.velocity.X = 0;
                }
                if (AI_State_Timer_1 < 45)
                {
                    NPC.TargetClosest(true);
                }
                if (AI_State_Timer_1 == 45)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.3f }, player.Center);

                    Vector2 difference = Main.player[NPC.target].Center - NPC.Center; //Distance between player center and npc center
                    Vector2 velocity = new Vector2(0.1f, 0).RotatedBy(difference.ToRotation()); //Give it velocity so it can face the right direction
                    Vector2 throwpower = (Main.player[NPC.target].Center - NPC.Center) / 30;

                    throwpower.Y += Main.rand.Next(-3, -1);
                    velocity += throwpower;

                    if (throwpower.Y < -8)
                    {
                        throwpower.Y = -8;
                    }

                    if (throwpower.X > 8)
                    {
                        throwpower.X = 8;
                    }
                    if (throwpower.X < -8)
                    {
                        throwpower.X = -8;
                    }
                    if (throwpower.X < 3 && throwpower.X >= 0)
                    {
                        throwpower.X = 3;
                    }
                    if (throwpower.X > -3 && throwpower.X < 0)
                    {
                        throwpower.X = -3;
                    }

                    //Main.NewText(throwpower);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        if (!Main.hardMode)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -14), throwpower, ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), 20, 0f, Main.myPlayer);
                        }
                        else
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -14), throwpower, ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), 25, 0f, Main.myPlayer);
                        }
                    }
                }

                if (AI_State_Timer_1 > 65)
                {
                    AI_State = State_Pursuing;
                    AI_Universal_Timer = 0;
                    AI_State_Timer_1 = 0;
                }
            }
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            if (AI_State == State_Firebombing && AI_State_Timer_1 < 45)
            {
                AI_State_Timer_1 = -10;
            }
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (AI_State == State_Firebombing && AI_State_Timer_1 < 45)
            {
                AI_State_Timer_1 = 10;
            }
        }

        #endregion


        #region Drawing & Animation

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (AI_Universal_Timer > 60)
            {
                Texture2D firebombTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Enemies/FirebombHollow_Firebomb");
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                if (NPC.spriteDirection == -1)
                {
                    spriteBatch.Draw(firebombTexture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 64, 54), drawColor, NPC.rotation, new Vector2(32, 30), NPC.scale, effects, 0);
                }
                else
                {
                    spriteBatch.Draw(firebombTexture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 64, 54), drawColor, NPC.rotation, new Vector2(32, 30), NPC.scale, effects, 0);
                }
            }
        }

        public override void DrawEffects(ref Color drawColor)
        {
            if (Main.rand.NextBool(20) && AI_State == State_Pursuing && AI_Universal_Timer >= 60)
            {
                if (NPC.direction == 1)
                {
                    int dust = Dust.NewDust(new Vector2(NPC.Center.X + 2, NPC.position.Y + 22), 12, 12, 6, NPC.velocity.X * 0f, NPC.velocity.Y * 0f, 30, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                }

                else
                {
                    int dust = Dust.NewDust(new Vector2(NPC.Center.X - 16, NPC.position.Y + 22), 12, 12, 6, NPC.velocity.X * 0f, NPC.velocity.Y * 0f, 30, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                }
            }
        }

        public override void FindFrame(int frameHeight)
        {
            if (AI_State == State_Firebombing && NPC.velocity.X == 0)
            {
                NPC.spriteDirection = NPC.direction;

                if (AI_State_Timer_1 <= 25)
                {
                    NPC.frame.Y = 10 * frameHeight;
                }
                else if (AI_State_Timer_1 < 45)
                {
                    NPC.frame.Y = 11 * frameHeight;
                }
                else if (AI_State_Timer_1 <= 51)
                {
                    NPC.frame.Y = 12 * frameHeight;
                }
                else
                {
                    NPC.frame.Y = 13 * frameHeight;
                }
            }

            else
            {
                NPC.spriteDirection = NPC.direction;

                if (NPC.velocity.Y != 0) //If airborn
                {
                    NPC.frame.Y = 1 * frameHeight;
                }
                else if (NPC.velocity.X == 0)
                {
                    NPC.frame.Y = 0 * frameHeight;
                }

                else
                {
                    float framecountspeed = Math.Abs(NPC.velocity.X) * 2.2f;
                    NPC.frameCounter += framecountspeed;


                    if (NPC.frameCounter < 12)
                    {
                        NPC.frame.Y = 2 * frameHeight;
                    }
                    else if (NPC.frameCounter < 24)
                    {
                        NPC.frame.Y = 3 * frameHeight;
                    }
                    else if (NPC.frameCounter < 36)
                    {
                        NPC.frame.Y = 4 * frameHeight;
                    }
                    else if (NPC.frameCounter < 48)
                    {
                        NPC.frame.Y = 5 * frameHeight;
                    }
                    else if (NPC.frameCounter < 60)
                    {
                        NPC.frame.Y = 6 * frameHeight;
                    }
                    else if (NPC.frameCounter < 72)
                    {
                        NPC.frame.Y = 7 * frameHeight;
                    }
                    else if (NPC.frameCounter < 84)
                    {
                        NPC.frame.Y = 8 * frameHeight;
                    }
                    else if (NPC.frameCounter < 96)
                    {
                        NPC.frame.Y = 9 * frameHeight;
                    }
                    else
                    {
                        NPC.frameCounter = 0;
                    }
                }
            }
        }



        #endregion


    }
}