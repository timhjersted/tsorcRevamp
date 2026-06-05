using Terraria.Audio;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tsorcRevamp.NPCs.Enemies{
    public class OmnirsEvilEye : ModNPC
    {
        float customAi1;
        int OTimeLeft = 2000;
        bool walkAndShoot = true;

        float npcAcSPD = 0.6f; //How fast they accelerate.
        float npcSPD = 2.5f; //Max speed

        bool tooBig = false;
        bool lavaJumping = false;

        int num;
        int oProAmnt = 4;

        public override void SetDefaults()
        {
            
            
            NPC.width = 80;
            NPC.height = 120;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 900f;
            NPC.npcSlots = 100;
            NPC.scale = 1f;
            NPC.aiStyle = -1;//22;
            NPC.knockBackResist = 0.1f;
            Main.npcFrameCount[NPC.type] = 3;
            AnimationType = -1;
            NPC.damage = 50;
            NPC.defense = 5;
            NPC.lifeMax = 400;
        }

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo) { return 0f; }
public float CanSpawnLegacy(NPCSpawnInfo s)
        {
            int x = s.SpawnTileX;
            int y = s.SpawnTileY;
            bool oSky = (y < (Main.maxTilesY * 0.1f));
            bool oSurface = (y >= (Main.maxTilesY * 0.1f) && y < (Main.maxTilesY * 0.2f));
            bool oUnderSurface = (y >= (Main.maxTilesY * 0.2f) && y < (Main.maxTilesY * 0.3f));
            bool oUnderground = (y >= (Main.maxTilesY * 0.3f) && y < (Main.maxTilesY * 0.4f));
            bool oCavern = (y >= (Main.maxTilesY * 0.4f) && y < (Main.maxTilesY * 0.6f));
            bool oMagmaCavern = (y >= (Main.maxTilesY * 0.6f) && y < (Main.maxTilesY * 0.8f));
            bool oUnderworld = (y >= (Main.maxTilesY * 0.8f));
            bool oBorders = (y < (Main.maxTilesY * 0.03f) || x < (Main.maxTilesX * 0.03f) || y > (Main.maxTilesY * 0.97f) || x > (Main.maxTilesX * 0.97f));
            int tile = (int)Main.tile[x, y].TileType;
            Player p = s.Player;
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Enemies.OmnirsEvilEye>()))
            {
                return 0f;
            }
            if (y > Main.maxTilesY - 200 && y < Main.maxTilesY - 190) return 1f;
            return 0f;
        }
        // Spawns in Hardmode Corruption/Crimson (with no NPCS when not a Blood Moon)
        #endregion


        public void teleport(bool pre)
        {
            if (!Main.expertMode)
            {
                if (Main.netMode != 2)
                {
                    SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                    for (int m = 0; m < 25; m++)
                    {
                        int dustID = Dust.NewDust(NPC.position, NPC.width, NPC.height, 6, 0, 0, 100, Color.White, 2f);
                        Main.dust[dustID].noGravity = true;
                        Main.dust[dustID].velocity = new Vector2(MathHelper.Lerp(-1f, 1f, (float)Main.rand.NextDouble()), MathHelper.Lerp(-1f, 1f, (float)Main.rand.NextDouble()));
                        Main.dust[dustID].velocity *= 7f;
                    }
                }
            }
        }

        public override void AI()  //  Floater ai
        {
            #region Ogre/custom frames
            int num = 1;
            if (!Main.dedServ)
            {
                num = Terraria.GameContent.TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            if (NPC.direction == 1)
            {
                NPC.spriteDirection = 1;
            }
            if (NPC.direction == -1)
            {
                NPC.spriteDirection = -1;
            }
            NPC.rotation = NPC.velocity.X * 0.08f;
            NPC.frameCounter += 1.0;
            if (NPC.frameCounter >= 8.0)
            {
                NPC.frame.Y = NPC.frame.Y + num;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type])
            {
                NPC.frame.Y = 0;
            }
            #endregion
            bool flag19 = false;
            if (NPC.justHit)
            {
                NPC.ai[2] = 0f;
            }
            if (NPC.ai[2] >= 0f)
            {
                int num283 = 16;
                bool flag21 = false;
                bool flag22 = false;
                if (NPC.position.X > NPC.ai[0] - (float)num283 && NPC.position.X < NPC.ai[0] + (float)num283)
                {
                    flag21 = true;
                }
                else if ((NPC.velocity.X < 0f && NPC.direction > 0) || (NPC.velocity.X > 0f && NPC.direction < 0))
                {
                    flag21 = true;
                }
                num283 += 24;
                if (NPC.position.Y > NPC.ai[1] - (float)num283 && NPC.position.Y < NPC.ai[1] + (float)num283)
                {
                    flag22 = true;
                }
                if (flag21 & flag22)
                {
                    NPC.ai[2] += 1f;
                    if (NPC.ai[2] >= 30f && num283 == 16)
                    {
                        flag19 = true;
                    }
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
                NPC.TargetClosest(true);
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
            int num284 = (int)((NPC.position.X + (float)(NPC.width / 2)) / 16f) + NPC.direction * 2;
            int num285 = (int)((NPC.position.Y + (float)NPC.height) / 16f);
            bool flag23 = true;
            bool flag24 = false;
            int num286 = 3;
            if (NPC.justHit)
            {
                NPC.ai[3] = 0f;
                NPC.localAI[1] = 0f;
            }
            float num287 = 7f;
            Vector2 vector33 = new Vector2(NPC.position.X + (float)NPC.width * 0.5f, NPC.position.Y + (float)NPC.height * 0.5f);
            float num288 = Main.player[NPC.target].position.X + (float)(Main.player[NPC.target].width / 2) - vector33.X;
            float num289 = Main.player[NPC.target].position.Y + (float)(Main.player[NPC.target].height / 2) - vector33.Y;
            float num290 = (float)Math.Sqrt((double)(num288 * num288 + num289 * num289));
            num290 = num287 / num290;
            num288 *= num290;
            num289 *= num290;

            num286 = 8;
            if (NPC.ai[3] > 0f)
            {
                NPC.ai[3] += 1f;
                if (NPC.ai[3] >= 64f)
                {
                    NPC.ai[3] = 0f;
                }
            }
            if (Main.netMode != 1 && NPC.ai[3] == 0f)
            {
                NPC.localAI[1] += 1f;
                if (NPC.localAI[1] > 120f && Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height) && !Main.player[NPC.target].npcTypeNoAggro[NPC.type])
                {
                    NPC.localAI[1] = 0f;
                    NPC.ai[3] = 1f;
                    NPC.netUpdate = true;
                }
            }

            for (int num309 = num285; num309 < num285 + num286; num309 = num + 1)
            {
                
                if ((Main.tile[num284, num309].HasUnactuatedTile && Main.tileSolid[(int)Main.tile[num284, num309].TileType]) || Main.tile[num284, num309].LiquidAmount > 0)
                {
                    if (num309 <= num285 + 1)
                    {
                        flag24 = true;
                    }
                    flag23 = false;
                    break;
                }
                num = num309;
            }
            if (Main.player[NPC.target].npcTypeNoAggro[NPC.type])
            {
                bool flag25 = false;
                for (int num310 = num285; num310 < num285 + num286 - 2; num310 = num + 1)
                {
                    
                    if ((Main.tile[num284, num310].HasUnactuatedTile && Main.tileSolid[(int)Main.tile[num284, num310].TileType]) || Main.tile[num284, num310].LiquidAmount > 0)
                    {
                        flag25 = true;
                        break;
                    }
                    num = num310;
                }
                NPC.directionY = (!flag25).ToDirectionInt();
            }
            if (flag19)
            {
                flag24 = false;
                flag23 = true;
            }
            if (flag23)
            {
                NPC.velocity.Y = NPC.velocity.Y + 0.1f;
                if (NPC.velocity.Y > 6f)
                {
                    NPC.velocity.Y = 6f;
                }

            }
            else
            {
                if (NPC.directionY < 0 && NPC.velocity.Y > 0f)
                {
                    NPC.velocity.Y = NPC.velocity.Y - 0.1f;
                }
                if (NPC.velocity.Y < -7f)
                {
                    NPC.velocity.Y = -7f;
                }
            }
            if (NPC.collideX)
            {
                NPC.velocity.X = NPC.oldVelocity.X * -0.4f;
                if (NPC.direction == -1 && NPC.velocity.X > 0f && NPC.velocity.X < 1f)
                {
                    NPC.velocity.X = 2f;
                }
                if (NPC.direction == 1 && NPC.velocity.X < 0f && NPC.velocity.X > -1f)
                {
                    NPC.velocity.X = -2f;
                }
            }
            if (NPC.collideY)
            {
                NPC.velocity.Y = NPC.oldVelocity.Y * -0.25f;
                if (NPC.velocity.Y > 0f && NPC.velocity.Y < 1f)
                {
                    NPC.velocity.Y = 2f;
                }
                if (NPC.velocity.Y < 0f && NPC.velocity.Y > -1f)
                {
                    NPC.velocity.Y = -2f;
                }
            }
            float num312 = 2.5f;
            if (NPC.direction == -1)
            {
                NPC.velocity.X = NPC.velocity.X - 0.2f;
                if (NPC.velocity.X > num312)
                {
                    NPC.velocity.X = NPC.velocity.X - 0.2f;
                }
                else if (NPC.velocity.X > 0f)
                {
                    NPC.velocity.X = NPC.velocity.X + 0.05f;
                }
                if (NPC.velocity.X < -num312)
                {
                    NPC.velocity.X = -num312;
                }
            }
            else if (NPC.direction == 1)
            {
                NPC.velocity.X = NPC.velocity.X + 0.2f;
                if (NPC.velocity.X < -num312)
                {
                    NPC.velocity.X = NPC.velocity.X + 0.2f;
                }
                else if (NPC.velocity.X < 0f)
                {
                    NPC.velocity.X = NPC.velocity.X - 0.05f;
                }
                if (NPC.velocity.X > num312)
                {
                    NPC.velocity.X = num312;
                }
            }
            num312 = 1.2f;
            if (NPC.direction == -1)
            {
                NPC.velocity.Y = NPC.velocity.Y - 0.04f;
                if (NPC.velocity.Y > num312)
                {
                    NPC.velocity.Y = NPC.velocity.Y - 0.05f;
                }
                else if (NPC.velocity.Y > 0f)
                {
                    NPC.velocity.Y = NPC.velocity.Y + 0.03f;
                }
                if (NPC.velocity.Y < -num312)
                {
                    NPC.velocity.Y = -num312;
                }
            }
            else if (NPC.direction == 1)
            {
                NPC.velocity.Y = NPC.velocity.Y + 0.04f;
                if (NPC.velocity.Y < -num312)
                {
                    NPC.velocity.Y = NPC.velocity.Y + 0.05f;
                }
                else if (NPC.velocity.Y < 0f)
                {
                    NPC.velocity.Y = NPC.velocity.Y - 0.03f;
                }
                if (NPC.velocity.Y > num312)
                {
                    NPC.velocity.Y = num312;
                }
            }
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.TargetClosest(true);
            Player player = Main.player[NPC.target];
            //for (int m = NPC.oldPos.Length - 1; m > 0; m--)
            //{
            //    NPC.oldPos[m] = NPC.oldPos[m - 1];
            //}
            //NPC.oldPos[0] = NPC.position;
            bool seeplayer = NPC.ai[0] >= 200 || Collision.CanHit(NPC.position, NPC.width, NPC.height, player.position, player.width, player.height);
            //public static void AIFlier(NPC NPC, ref float[] ai, bool sporadic = true, float moveIntervalX = 0.1f, float moveIntervalY = 0.04f, float maxSpeedX = 4f, float maxSpeedY = 1.5f, bool canBeBored = true, int timeUntilBoredom = 300)
            //NPC.rotation = NPC.velocity.X * 0.08f;
        }
        public override void OnKill()
        {
            if (Main.rand.Next(5) == 0)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsCoralSword"));
            }
            if (Main.rand.Next(50) == 0)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsJeweloftheDepths"));
            }
            if (Main.rand.Next(3) == 0)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsDezoneTome"));
            }
            if (Main.rand.Next(10) == 0)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsEscapeTome"));
            }
            if (Main.rand.Next(5) == 0)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsGreatSword"));
            }
        }
    }
}
