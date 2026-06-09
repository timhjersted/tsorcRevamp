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
	public class OmnirsGigas : ModNPC
	{
        float customAi1;
        int movedTowards = 0;
        int num94 = 0;
        int num95 = 0;
        int noJump = 0;
        int OTimeLeft = 2000;
        bool walkAndShoot = true;

        bool canDrown = true;
        int drownTimerMax = 3500;
        int drownTimer = 3500;
        int drowningRisk = 2000;

        float npcAcSPD = 0.5f; //How fast they accelerate.
        float npcSPD = 1.0f; //Max speed

        float npcEnrAcSPD = .6f; //How fast they accelerate.
        float npcEnrSPD = 1.4f; //Max speed

        bool tooBig = true;
        bool lavaJumping = false;
        bool thruWalls = true;
        int oNPCNoReach = 0;
        bool phaseThruWalls = false;
        bool oPhasing1 = false;
        bool oPhasing2 = false;
        bool oDigSound = false;
        int oAtt = 50;
        int oDef = 13;

        public override void SetDefaults()
		{
			
			
			NPC.width = 52;
			NPC.height = 110;
			NPC.damage = 50;
			NPC.defense = 13;
			NPC.lifeMax = 750;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 2000f;
			NPC.npcSlots = 100;
            NPC.scale = 1.1f;
			NPC.knockBackResist = 0.1f;
			Main.npcFrameCount[NPC.type] = 16;
			AnimationType = 28;
		}
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
            int tile = (int)Main.tile[x, y].TileType;
            Player p = s.Player;
            if (Main.pumpkinMoon || Main.snowMoon || p.townNPCs > 2f || p.ZoneDungeon || p.ZoneMeteor)
            {
                return 0f;
            }

            if (!Main.hardMode && (oCavern || oMagmaCavern))
            {
                if (x < Main.maxTilesX * 0.35f && Main.rand.Next(250) == 1) return 1f;
                else if (x > Main.maxTilesX * 0.65f && Main.rand.Next(250) == 1) return 1f;
                return 0f;
            }
            else if (Main.hardMode && oUnderground)
            {
                if (Main.rand.Next(50) == 1) return 1f;
                return 0f;
            }

            return 0f;
        }
        //Spawns in the Cavern before 3.5/10th and after 6.5/10th of the world (Width) in normal mode. In Hardmode, spawns in the underground. Does not spawn in the dungeon.

        public void teleport(bool pre)
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
		public override void AI()  //  warrior ai
		{


            bool enraged = (NPC.life < (float)NPC.lifeMax*.2f);  //  speed up at low life
			int shotRate = enraged?100:70;
			float accel=enraged? npcEnrAcSPD:npcAcSPD;  //  how fast it can speed up
			float topSpeed=enraged? npcEnrSPD:npcSPD;  //  max walking speed, also affects jump length
            tsorcRevampAIs.FighterAI(NPC, topSpeed: topSpeed, acceleration: accel, canTeleport: false, minSurfaceWidth: 2, canWalkBackwards: true);
            Vector2 angle = Main.player[NPC.target].Center - NPC.Center;
            angle.Y = angle.Y - (Math.Abs(angle.X) * .1f);
            angle.X += (float)Main.rand.Next(-20, 21);
            angle.Y += (float)Main.rand.Next(-20, 21);
            angle.Normalize();
            if (NPC.lavaWet) NPC.velocity.Y-=2;
			float distance = NPC.Distance(Main.player[NPC.target].Center);
            #region shoot and walk
            if (Main.netMode != 1 && !Main.player[NPC.target].dead) // can generalize this section to moving+Projectile code // can generalize this section to moving+Projectile code
            {
                if (NPC.justHit)
                    NPC.ai[2] = 0f; // reset throw countdown when hit

                #region Charge
                if (NPC.velocity.Y == 0f && Main.rand.Next(550) == 1)
                {
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    NPC.velocity.X = (float)(Math.Cos(rotation) * 7) * -1;
                    NPC.velocity.Y = (float)(Math.Sin(rotation) * 7) * -1;
                    NPC.ai[1] = 1f;
                    NPC.netUpdate = true;
                }
                #endregion
            }
            #endregion
            if (NPC.velocity.Y == 0f && Main.rand.Next(550) == 1)
            {
                Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                NPC.velocity.X = (float)(Math.Cos(rotation) * 7) * -1;
                NPC.velocity.Y = (float)(Math.Sin(rotation) * 7) * -1;
                NPC.localAI[3] = 1f;
                NPC.netUpdate = true;
            }
		}
		public override void OnKill()
		{
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsGigasGore1").Type, 1.1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsGigasGore2").Type, 1.1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsGigasGore3").Type, 1.1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsGigasGore2").Type, 1.1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsGigasGore3").Type, 1.1f);
            if (Main.rand.Next(9) == 0)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsGigantAxe"));
            }
            if (Main.rand.Next(9) == 0)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsGreatBow"));
            }
            if (Main.rand.Next(12) == 0)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsMythrilGlove"));
            }
            if (Main.rand.Next(25) == 0)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsWereBuster"));
            }
            if (Main.rand.Next(15) == 0)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsGreatAxe"));
            }
            if (Main.rand.Next(8) == 0)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsPlateArmor"));
            }
            if (Main.rand.Next(8) == 0)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsPlateHelmet"));
            }
            if (Main.rand.Next(8) == 0)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsPlateGreaves"));
            }
        }
    }
}
