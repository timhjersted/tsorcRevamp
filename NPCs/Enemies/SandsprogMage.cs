using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Projectiles.Ranged;
using static tsorcRevamp.SpawnHelper;

namespace tsorcRevamp.NPCs.Enemies
{
    public class SandsprogMage : ModNPC
    {
        bool channeling = false;
        int channelingTimer;

        // now has damage scaling
        public int projDamage = 13;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 7;
        }
        public override void SetDefaults()
        {
            NPC.knockBackResist = 0.75f;
            NPC.aiStyle = -1;
            NPC.damage = 16;
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
                NPC.damage = 26;
                NPC.value = 100;
                projDamage = 24;
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 900;
                NPC.defense = 45;
                NPC.damage = 34;
                NPC.value = 3000;
                projDamage = 36;
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

            if (!channeling) { tsorcRevampAIs.FighterAI(NPC, 1.2f, 0.05f, 0.2f, false, 4, false, null, 0, 0.5f, 2.3f, true, true, false); }
            else { tsorcRevampAIs.FighterAI(NPC, 1.2f, 0.05f, 0.2f, false, 4, false, null, 0, 0f, 2.5f, true, false, false); }

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

            if (standing_on_solid_tile && !channeling)
            {
                if (NPC.position.Y > player.position.Y + 3 * 16 && Math.Abs(NPC.Center.X - player.Center.X) < 4f * 16 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0) && !channeling)
                {
                    NPC.velocity.Y = -6.9f; // jump if directly under player
                    NPC.netUpdate = true;
                }
            }

            //Main.NewText(channelingTimer);
            //if (!standing_on_solid_tile) Main.NewText(NPC.velocity.Y);
            //if (channelingTimer % 30 == 0) { Main.NewText(Math.Abs(NPC.velocity.X)); Main.NewText(NPC.defense); }


            if (channelingTimer == 0) // Make sure it spawns in with the timer high, for ambush events
            {
                channelingTimer += 250;
            }

            if (channelingTimer < 250)
            {
                channelingTimer++;
            }

            if (NPC.life < NPC.lifeMax / 2)
            {
                channelingTimer++;
            }

            if (NPC.HasValidTarget && channelingTimer >= 250 && Main.player[NPC.target].Distance(NPC.Center) < 450f && standing_on_solid_tile && lineOfSight && !channeling)
            {
                channeling = true;
                channelingTimer = 1;
            }

            if (!channeling)
            {
                NPC.knockBackResist = 0.75f;
            }

            if (channeling)
            {
                NPC.knockBackResist = 0;
                NPC.velocity.X = 0;

                int dustQuantity = channelingTimer / 6;
                for (int i = 0; i < dustQuantity; i++)
                {
                    if (Main.rand.NextBool(10) && channelingTimer < 121)
                    {
                        int dust = Dust.NewDust(new Vector2(NPC.Center.X - 10, NPC.Center.Y - 30), 10, 10, 226, 0, 0, 100, default(Color), .4f);
                        Main.dust[dust].noGravity = true;
                    }
                }


                if (channelingTimer == 120 || channelingTimer == 121) //If it works, it works ;)
                {
                    float num48 = 5f;
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X);
                    float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y);


                    float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                    num51 = num48 / num51;
                    speedX *= num51;
                    speedY *= num51;
                    int type = ModContent.ProjectileType<PulsarShot>();
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int shot1 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 30, speedX, speedY, type, projDamage, 0f, Main.myPlayer, 0, 0);
                        Main.projectile[shot1].friendly = false;
                        Main.projectile[shot1].hostile = true;
                    }
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Item/PulsarShot") with { Volume = 0.6f, PitchVariance = 0.3f }, NPC.Center);

                }

                if (channelingTimer > 150)
                {
                    channeling = false;
                    channelingTimer = 1;
                }
            }
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            /*Texture2D whirlTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Enemies/Whirl");
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            if (channeling)
            {
                if (NPC.spriteDirection == -1)
                {
                    spriteBatch.Draw(whirlTexture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 44, 50), lightColor, NPC.rotation, new Vector2(22, 36), NPC.scale, effects, 0f);
                }
                else
                {
                    spriteBatch.Draw(whirlTexture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 44, 50), lightColor, NPC.rotation, new Vector2(22, 36), NPC.scale, effects, 0f);
                }
            }*/
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
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<StarlightShard>(), 4, 1));
        }
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Sandsprog_Gore_1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Sandsprog_Gore_1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Sandsprog_Mage_Gore_1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Sandsprog_Mage_Gore_2").Type, 1f);
            }
        }

        #region Drawing and Animation

        public override void FindFrame(int frameHeight)
        {

            if (NPC.velocity.X != 0 && !channeling) //Walking
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

            if (NPC.velocity.Y != 0 && (!channeling)) //If falling/jumping
            {
                NPC.frame.Y = 5 * frameHeight;
            }

            if (channeling)
            {
                NPC.spriteDirection = NPC.direction;

                NPC.frame.Y = 6 * frameHeight;

            }
        }

        #endregion

    }
}
