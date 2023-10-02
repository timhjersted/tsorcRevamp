using Terraria;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Potions;
using static tsorcRevamp.SpawnHelper;


namespace tsorcRevamp.NPCs.Enemies
{
    public class MountedSandsprog : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = Main.npcFrameCount[NPCID.GiantWalkingAntlion];
        }
        public override void SetDefaults()
        {
            NPC.CloneDefaults(NPCID.GiantWalkingAntlion);
            AIType = NPCID.GiantWalkingAntlion;
            NPC.height = 44;
            NPC.damage = 33;
            NPC.lifeMax = 75;
            NPC.defense = 18;
            NPC.value = 0;
            NPC.knockBackResist = 0.3f;
            AnimationType = NPCID.GiantWalkingAntlion;
            NPC.DeathSound = SoundID.DD2_KoboldHurt;

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

            if (spawnInfo.Player.ZoneDesert || spawnInfo.Player.ZoneUndergroundDesert) return 0.05f;

            return chance;
        }
        #endregion

        int boostTimer;
        public override void AI()
        {
            if (boostTimer == 0) // Make sure it spawns in with the timer high, for ambush events
            {
                boostTimer += 180;
            }

            if (NPC.velocity.X > 3.5f || NPC.velocity.X < -3.5f)
            {
                NPC.damage = 80;
                NPC.knockBackResist = 0;
            }
            else
            {
                NPC.damage = 66;
                NPC.knockBackResist = 0.3f;

            }

            if (boostTimer < 300)
            {
                boostTimer++;
            }

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

            if (!standing_on_solid_tile && Math.Abs(NPC.velocity.X) > 3.5f && Main.player[NPC.target].Distance(NPC.Center) > 400f)
            {
                if (NPC.velocity.X > 0f) { NPC.velocity.X -= 0.5f; }
                if (NPC.velocity.X < 0f) { NPC.velocity.X += 0.5f; }
            }

            if (Math.Abs(NPC.velocity.X) > 7f) //speed limit
            {
                if (NPC.velocity.X > 0f) { NPC.velocity.X -= 0.2f; }
                if (NPC.velocity.X < 0f) { NPC.velocity.X += 0.2f; }
            }

            //Main.NewText(NPC.velocity.X);

            if (NPC.direction == 1)
            {
                if (boostTimer > 270)
                {
                    int dust = Dust.NewDust(new Vector2(NPC.Center.X + 12, NPC.Center.Y - 3), 8, 8, 183, Main.rand.NextFloat(-1f, 0f), Main.rand.Next(-2, 0), 0, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                }

                if (NPC.velocity.X > 3.5f && Main.player[NPC.target].Distance(NPC.Center) < 400f)
                {
                    Dust.NewDust(new Vector2(NPC.BottomLeft.X, NPC.BottomLeft.Y - 50), 20, 50, 16, Main.rand.Next(-3, -1), Main.rand.Next(-2, 0), 0, default(Color), 1f);
                }

                if (NPC.HasValidTarget && lineOfSight && Main.player[NPC.target].Distance(NPC.Center) < 400f && Main.player[NPC.target].position.X > NPC.position.X && NPC.velocity.X > 1f)
                {
                    if (NPC.velocity.X > 3.5f)
                    {
                        int dust = Dust.NewDust(new Vector2(NPC.Center.X + 12, NPC.Center.Y - 10), 8, 8, 183, Main.rand.NextFloat(-1f, 0f), Main.rand.Next(-2, 0), 0, default(Color), 1f);
                        Main.dust[dust].noGravity = true;
                    }
                    if (boostTimer == 300 && standing_on_solid_tile)
                    {
                        NPC.velocity.X += 4f;
                        for (int i = 0; i < 30; i++) Dust.NewDust(new Vector2(NPC.BottomLeft.X, NPC.BottomLeft.Y - 50), 20, 50, 16, Main.rand.Next(-3, -1), Main.rand.Next(-2, 0), 0, default(Color), 1f);
                        boostTimer = 1;
                    }
                }
            }
            else 
            {
                if (boostTimer > 270)
                {
                    int dust = Dust.NewDust(new Vector2(NPC.Center.X - 22, NPC.Center.Y - 3), 8, 8, 183, Main.rand.NextFloat(0f, 1f), Main.rand.NextFloat(-2f, 0f), 0, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                }
                if (NPC.velocity.X < -3.5f)
                {
                    Dust.NewDust(new Vector2(NPC.BottomRight.X - 10, NPC.BottomRight.Y - 50), 20, 50, 16, Main.rand.Next(1, 3), Main.rand.Next(-2, 0), 0, default(Color), 1f);
                }

                if (NPC.HasValidTarget && lineOfSight && Main.player[NPC.target].Distance(NPC.Center) < 400f && Main.player[NPC.target].position.X < NPC.position.X && NPC.velocity.X < -1f)
                {
                    if (NPC.velocity.X < -3.5f)
                    {
                        int dust = Dust.NewDust(new Vector2(NPC.Center.X - 22, NPC.Center.Y - 10), 8, 8, 183, Main.rand.NextFloat(0f, 1f), Main.rand.NextFloat(-2f, 0f), 0, default(Color), 1f);
                        Main.dust[dust].noGravity = true;
                    }

                    if (boostTimer == 300)
                    {
                        NPC.velocity.X -= 4f;
                        for (int i = 0; i < 30; i++) Dust.NewDust(new Vector2(NPC.BottomRight.X - 10, NPC.BottomRight.Y - 50), 20, 50, 16, Main.rand.Next(1, 3), Main.rand.Next(-2, 0), 0, default(Color), 1f);
                        boostTimer = 1;
                    }
                }
            }

            base.AI();

        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {

            Texture2D armTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Enemies/Sandsprog_Mounted_Knife_Arm");
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            if (Main.player[NPC.target].Distance(NPC.Center) < 450f && (boostTimer > 270 || Math.Abs(NPC.velocity.X) > 3.5f))
            {
                if (NPC.spriteDirection == -1)
                {
                    spriteBatch.Draw(armTexture, NPC.Center - Main.screenPosition + new Vector2(0f, NPC.gfxOffY), new Rectangle(NPC.frame.X, NPC.frame.Y, 84, 66), drawColor, NPC.rotation, new Vector2(40, 38), NPC.scale, effects, 0);
                }
                else
                {
                    spriteBatch.Draw(armTexture, NPC.Center - Main.screenPosition + new Vector2(0f, NPC.gfxOffY), new Rectangle(NPC.frame.X, NPC.frame.Y, 84, 66), drawColor, NPC.rotation, new Vector2(44, 38), NPC.scale, effects, 0);
                }
            }

            return true;
        }

        public override void OnKill()
        {

            NPC.NewNPC(NPC.GetSource_FromAI(), (int)(NPC.position.X + (float)(NPC.width / 2) + NPC.velocity.X), (int)(NPC.position.Y + (float)(NPC.height) + NPC.velocity.Y), NPCID.GiantWalkingAntlion);
            NPC.NewNPC(NPC.GetSource_FromAI(), (int)(NPC.position.X + (float)(NPC.width / 2) + NPC.velocity.X), (int)(NPC.position.Y + (float)(NPC.height + 20) + NPC.velocity.Y - 40), ModContent.NPCType<Sandsprog>());

            base.OnKill();
        }
    }
}
