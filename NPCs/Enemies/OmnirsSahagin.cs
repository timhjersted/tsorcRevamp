using Terraria.Audio;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies{
    public class OmnirsSahagin : ModNPC //amazing help from Cheezemaniac
    {
        float customAi1;
        int OTimeLeft = 2000;
        bool walkAndShoot = false;

        bool canDrown = false;
        int drownTimerMax = 3500;
        int drownTimer = 3500;
        int drowningRisk = 2000;

        float npcAcSPD = 0.7f; //How fast they accelerate.
        float npcSPD = 2.3f; //Max speed

        bool tooBig = false;
        bool lavaJumping = false;

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
            bool oOcean = (x <= (Main.maxTilesX * .2f) && x < (Main.maxTilesX * 0.8f) && y < (Main.maxTilesY * 0.4f));
            int tile = (int)Main.tile[x, y].TileType;
            Player p = s.Player;
            if (Main.pumpkinMoon || Main.snowMoon || Main.hardMode || p.townNPCs > 0f || p.ZoneDungeon)
            {
                return 0f;
            }
            if (oOcean)
            {
                if ((oSurface) && Main.rand.Next(20) == 1) return 1f;
                else if (oUnderSurface && Main.rand.Next(10) == 1) return 1f;
                return 0f;
            }
            return 0f;
        }
        //Spawns in the Ocean, before the Cavern (Height). Does not spawn in Hardmode, or if there are Town NPCs.
        #endregion




        public override void SetDefaults()
        {
            
            NPC.lifeMax = 44;
            NPC.damage = 22;
            NPC.defense = 6;
            NPC.knockBackResist = 0.4f;
            NPC.width = 18;
            NPC.height = 40;
            NPC.aiStyle = 3; // Copied Zombie AI to get behavior.
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.value = Item.buyPrice(0, 0, 0, 75);
            Main.npcFrameCount[NPC.type] = 15;
            AnimationType = 21;
            //Banner = NPC.type;
            //BannerItem = 0;
        }

        public override void AI()
        {
            NPC.noGravity = false;
            NPC.spriteDirection = NPC.direction;
            #region shoot and walk
            if (walkAndShoot && Main.netMode != 1 && !Main.player[NPC.target].dead) // can generalize this section to moving+Projectile code // can generalize this section to moving+Projectile code
            {
            }
            #endregion
            #region drown // code by Omnir
            if (canDrown)
            {
                if (!NPC.wet)
                {
                    NPC.TargetClosest(true);
                    drownTimer = drownTimerMax;
                }
                if (NPC.wet)
                {
                    drownTimer--;
                }
                if (NPC.wet && drownTimer > drowningRisk)
                {
                    NPC.TargetClosest(true);
                }
                else if (NPC.wet && drownTimer <= drowningRisk)
                {
                    NPC.TargetClosest(false);
                    if (NPC.timeLeft > 10)
                    {
                        NPC.timeLeft = 10;
                    }
                    NPC.directionY = -1;
                    if (NPC.velocity.Y > 0f)
                    {
                        NPC.direction = 1;
                    }
                    NPC.direction = -1;
                    if (NPC.velocity.X > 0f)
                    {
                        NPC.direction = 1;
                    }
                }
                if (drownTimer <= 0)
                {
                    NPC.life--;
                    if (NPC.life <= 0)
                    {
                        SoundEngine.PlaySound(SoundID.NPCHit1, NPC.Center);
                        OnKill();
                        NPC.netUpdate = true;
                    }
                }
            }
            #endregion
            #region Too Big and Lava Jumping
            if (tooBig)
            {
                if (NPC.velocity.Y == 0f && (NPC.velocity.X == 0f && NPC.direction < 0))
                {
                    NPC.velocity.Y -= 8f;
                    NPC.velocity.X -= npcSPD;
                }
                else if (NPC.velocity.Y == 0f && (NPC.velocity.X == 0f && NPC.direction > 0))
                {
                    NPC.velocity.Y -= 8f;
                    NPC.velocity.X += npcSPD;
                }
            }
            if (lavaJumping)
            {
                if (NPC.lavaWet)
                {
                    NPC.velocity.Y -= 2;
                }
            }
            #endregion
        }
        #region Gore
        public override void OnKill()
        {
            Color color = new Color();
            Rectangle rectangle = new Rectangle((int)NPC.position.X, (int)(NPC.position.Y + ((NPC.height - NPC.width) / 2)), NPC.width, NPC.width);
            int count = 30;
            for (int i = 1; i <= count; i++)
            {
                int dust = Dust.NewDust(NPC.position, rectangle.Width, rectangle.Height, 6, 0, 0, 100, color, 1.5f);
                Main.dust[dust].noGravity = false;
            }
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsSahaginGore1").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsSahaginGore2").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsSahaginGore3").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsSahaginGore2").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsSahaginGore3").Type, 1f);
            if (Main.rand.Next(28) == 0)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsIce1Tome"));
            }
            if (Main.rand.Next(10) == 0)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsAxe"));
            }
            if (Main.rand.Next(55) == 0)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsHeal1Tome"));
            }
        }
        #endregion
    }
}