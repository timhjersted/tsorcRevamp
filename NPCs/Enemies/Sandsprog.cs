using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static tsorcRevamp.SpawnHelper;
using Terraria.GameContent.ItemDropRules;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    public class Sandsprog : ModNPC
    {
        bool whirling = false;
        int whirlingTimer;
        int knife2timer;

        // now has damage scaling
        public int knivesDamage = 13;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 12;
        }
        public override void SetDefaults()
        {
            NPC.knockBackResist = 0.65f;
            NPC.aiStyle = -1;
            NPC.damage = 24;
            NPC.defense = 10;
            NPC.height = 28;
            NPC.width = 20;
            NPC.lifeMax = 90;
            NPC.value = 40;

            tsorcRevampGlobalNPC sprogletGlobalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            sprogletGlobalNPC.Cowardice = 0.1f; //low cowardice


            if (Main.hardMode) 
            { 
                NPC.lifeMax = 200; 
                NPC.defense = 18; 
                NPC.damage = 40; 
                NPC.value = 100;
                knivesDamage = 20;
    }
            if (tsorcRevampWorld.SuperHardMode) 
            { 
                NPC.lifeMax = 900; 
                NPC.defense = 45; 
                NPC.damage = 60; 
                NPC.value = 3000;
                knivesDamage = 30;
            }

            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            //Banner = NPC.type;
            //BannerItem = ModContent.ItemType<Banners.HollowWarriorBanner>();

            NPC.buffImmune[BuffID.Confused] = true;
            
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            /*for (int i = 0; i < 10; i++)
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
            }*/
        }

        public override void AI()
        {
            if (!whirling) { tsorcRevampAIs.FighterAI(NPC, 2f, 0.07f, 0.2f, false, 4, false, null, 0, 0.5f, 2.5f, true, true, false); }
            else { tsorcRevampAIs.FighterAI(NPC, 4f, 0.07f, 0.3f, false, 4, false, null, 0, 0.5f, 6f, true, false, false); }

            Player player = Main.player[NPC.target];
            bool lineOfSight = Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height);

            #region check if standing on a solid tile

            int y_below_feet = (int)(NPC.position.Y + (float)NPC.height + 8f) / 16;
            bool standing_on_solid_tile = false;
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

            if (standing_on_solid_tile && !whirling)
            {
                if (NPC.position.Y > player.position.Y + 3 * 16 && Math.Abs(NPC.Center.X - player.Center.X) < 4f * 16 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0) && !whirling)
                {
                    NPC.velocity.Y = -6.9f; // jump if directly under player
                    NPC.netUpdate = true;
                }
            }

            //Main.NewText(whirlingTimer);
            //if (!standing_on_solid_tile) Main.NewText(NPC.velocity.Y);
            //if (whirlingTimer % 30 == 0) { Main.NewText(Math.Abs(NPC.velocity.X)); Main.NewText(NPC.defense); }


            if (whirlingTimer == 0) // Make sure it spawns in with the timer high, for ambush events
            {
                whirlingTimer += 300;
            }

            if (whirlingTimer > 15) 
            {
                knife2timer++;
            }

            if (whirlingTimer < 400)
            {
                whirlingTimer++;
            }
             
            if (NPC.HasValidTarget && whirlingTimer > 300 && Main.player[NPC.target].Distance(NPC.Center) < 350f && Main.player[NPC.target].position.Y + 16 >= NPC.position.Y && standing_on_solid_tile && lineOfSight && !whirling)
            {
                whirling = true;
                whirlingTimer = 1;
            }

            if (!whirling)
            {
                NPC.knockBackResist = 0.65f;
            }

            if (whirling)
            {
                NPC.knockBackResist = 0;

                if (whirlingTimer > 95 && whirlingTimer < 300)
                {
                    NPC.defense = 420;
                }
                else
                {
                    NPC.defense = 10;
                    if (Main.hardMode) { NPC.defense = 18; }
                    if (tsorcRevampWorld.SuperHardMode) { NPC.defense = 45; }
                }


                if (whirlingTimer < 300)
                {
                    NPC.velocity.Y = 0;
                }

                if (whirlingTimer < 95 || whirlingTimer > 300)
                {
                    NPC.velocity.X = 0;
                }

                if (whirlingTimer > 110 && whirlingTimer < 300 && whirlingTimer % 30 == 0)
                {
                    int knifeRight = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(NPC.velocity.X + Main.rand.NextFloat(4f, 6f), Main.rand.NextFloat(-7f, -2f)), ProjectileID.BoneDagger, knivesDamage, 5, Main.myPlayer);
                    Main.projectile[knifeRight].friendly = false;
                    Main.projectile[knifeRight].hostile = true;

                }
                if (whirlingTimer > 95 && whirlingTimer < 300 && knife2timer % 30 == 0)
                {
                    int knifeLeft = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(NPC.velocity.X + Main.rand.NextFloat(-6f, -4f), Main.rand.NextFloat(-7f, -2f)), ProjectileID.BoneDagger, knivesDamage, 5, Main.myPlayer);
                    Main.projectile[knifeLeft].friendly = false;
                    Main.projectile[knifeLeft].hostile = true;

                }

                if (NPC.collideX)
                {
                    whirlingTimer = 300;
                }

                if (whirlingTimer > 300 && whirlingTimer < 400)
                {
                    NPC.velocity.Y = 0;
                }

                if (whirlingTimer == 400)
                {
                    whirling = false;
                    whirlingTimer = 1;
                    knife2timer = 15; //So that the knives come out alternating sides
                }
            }
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            Texture2D whirlTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Enemies/Whirl");
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            if (whirling)
            {
                if (NPC.spriteDirection == -1)
                {
                    spriteBatch.Draw(whirlTexture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 44, 50), lightColor, NPC.rotation, new Vector2(22, 36), NPC.scale, effects, 0f);
                }
                else
                {
                    spriteBatch.Draw(whirlTexture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 44, 50), lightColor, NPC.rotation, new Vector2(22, 36), NPC.scale, effects, 0f);
                }
            }
        }

        #region Spawning
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            Player p = spawnInfo.Player;

            if (spawnInfo.Invasion || Sky(p) || spawnInfo.Player.ZoneSnow)
            {
                chance = 0;
                return chance;
            }

            if (spawnInfo.Player.ZoneDesert || spawnInfo.Player.ZoneUndergroundDesert) return 0.04f;

            return chance;
        }
        #endregion

        public override void ModifyNPCLoot(NPCLoot npcLoot) 
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.BoneDagger, 1, 3, 8));
        }

        #region Drawing and Animation

        public override void FindFrame(int frameHeight)
        {
            if (whirling && whirlingTimer > 95 && whirlingTimer < 300)
            {
                NPC.alpha = 255;
            }
            else { NPC.alpha = 0; }


            if (NPC.velocity.X != 0 && !whirling) //Walking
            {
                float framecountspeed = Math.Abs(NPC.velocity.X) * 2.2f;
                NPC.frameCounter += framecountspeed;
                NPC.spriteDirection = NPC.direction;

                if (NPC.frameCounter < 12)
                {
                    NPC.frame.Y = 0 * frameHeight;
                }
                else if (NPC.frameCounter < 24)
                {
                    NPC.frame.Y = 1 * frameHeight;
                }
                else if (NPC.frameCounter < 36)
                {
                    NPC.frame.Y = 2 * frameHeight;
                }
                else if (NPC.frameCounter < 48)
                {
                    NPC.frame.Y = 3 * frameHeight;
                }
                else if (NPC.frameCounter < 60)
                {
                    NPC.frame.Y = 4 * frameHeight;
                }
                else
                {
                    NPC.frameCounter = 0;
                }
            }

            if (NPC.velocity.Y != 0 && (!whirling)) //If falling/jumping
            {
                NPC.frame.Y = 5 * frameHeight;
            }

            if (whirling)
            {
                NPC.spriteDirection = NPC.direction;

                if (whirlingTimer < 30)
                {
                    NPC.frame.Y = 6 * frameHeight;
                }
                else if (whirlingTimer < 40)
                {
                    NPC.frame.Y = 7 * frameHeight;
                }
                else if (whirlingTimer < 49)
                {
                    NPC.frame.Y = 8 * frameHeight;
                }
                else if (whirlingTimer < 57)
                {
                    NPC.frame.Y = 9 * frameHeight;
                }
                else if (whirlingTimer < 64)
                {
                    NPC.frame.Y = 10 * frameHeight;
                }
                else if (whirlingTimer < 70)
                {
                    NPC.frame.Y = 11 * frameHeight;
                }
                else if (whirlingTimer < 76)
                {
                    NPC.frame.Y = 12 * frameHeight;
                }
                else if (whirlingTimer < 82)
                {
                    NPC.frame.Y = 13 * frameHeight;
                }
                else if (whirlingTimer < 88)
                {
                    NPC.frame.Y = 14 * frameHeight;
                }
                else if (whirlingTimer < 94)
                {
                    NPC.frame.Y = 11 * frameHeight;
                }
                else if (whirlingTimer < 98)
                {
                    NPC.frame.Y = 12 * frameHeight;
                }
                else if (whirlingTimer < 100)
                {
                    NPC.frame.Y = 13 * frameHeight;
                }



                else if (whirlingTimer > 300 && whirlingTimer < 304)
                {
                    NPC.frame.Y = 14 * frameHeight;
                }
                else if (whirlingTimer < 310)
                {
                    NPC.frame.Y = 13 * frameHeight;
                }
                else if (whirlingTimer < 316)
                {
                    NPC.frame.Y = 12 * frameHeight;
                }
                else if (whirlingTimer < 322)
                {
                    NPC.frame.Y = 11 * frameHeight;
                }
                else if (whirlingTimer < 328)
                {
                    NPC.frame.Y = 14 * frameHeight;
                }
                else if (whirlingTimer < 334)
                {
                    NPC.frame.Y = 13 * frameHeight;
                }
                else if (whirlingTimer < 340)
                {
                    NPC.frame.Y = 12 * frameHeight;
                }
                else if (whirlingTimer < 346)
                {
                    NPC.frame.Y = 11 * frameHeight;
                }
                else if (whirlingTimer < 352)
                {
                    NPC.frame.Y = 10 * frameHeight;
                }
                else if (whirlingTimer < 359)
                {
                    NPC.frame.Y = 9 * frameHeight;
                }
                else if (whirlingTimer < 367)
                {
                    NPC.frame.Y = 8 * frameHeight;
                }
                else if (whirlingTimer < 376)
                {
                    NPC.frame.Y = 7 * frameHeight;
                }
                else if (whirlingTimer < 400)
                {
                    NPC.frame.Y = 6 * frameHeight;
                }

                float framecountspeed = Math.Abs(NPC.velocity.X) * 2f;
                NPC.frameCounter += (int)framecountspeed;

                if (Math.Abs(NPC.velocity.X) > 0)
                {
                    NPC.frame.Y = 0 * frameHeight; //This prevents holes in the anim, not 100% sure why
                    //Main.NewText(NPC.frameCounter);

                    if (NPC.frameCounter < 12)
                    {
                        NPC.frame.Y = 0 * frameHeight;
                    }
                    else if (NPC.frameCounter < 24)
                    {
                        NPC.frame.Y = 1 * frameHeight;
                    }
                    else if (NPC.frameCounter < 36)
                    {
                        NPC.frame.Y = 2 * frameHeight;
                    }
                    else if (NPC.frameCounter < 48)
                    {
                        NPC.frame.Y = 3 * frameHeight;
                    }
                    else if (NPC.frameCounter < 60)
                    {
                        NPC.frame.Y = 4 * frameHeight;
                    }
                    else if (NPC.frameCounter < 72)
                    {
                        NPC.frame.Y = 5 * frameHeight;
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
