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
	public class OmnirsHydra : ModNPC
	{
        float customAi1;
        int movedTowards = 0;
        int num94 = 0;
        int num95 = 0;
        int noJump = 0;
        int OTimeLeft = 2000;
        bool walkAndShoot = true;

        bool canDrown = true;
        int drownTimerMax = 10000;
        int drownTimer = 10000;
        int drowningRisk = 3000;

        float npcAcSPD = 0.6f; //How fast they accelerate.
        float npcSPD = 2.2f; //Max speed

        float npcEnrAcSPD = .9f; //How fast they accelerate.
        float npcEnrSPD = 5f; //Max speed

        bool tooBig = true;
        bool lavaJumping = true;
        bool thruWalls = true;
        int oNPCNoReach = 0;
        bool phaseThruWalls = false;
        bool oPhasing1 = false;
        bool oPhasing2 = false;
        bool oDigSound = false;
        int oAtt = 50;
        int oDef = 10;

        public override void SetDefaults()
		{
			
			
			NPC.width = 170;
			NPC.height = 130;
			NPC.damage = 50;
			NPC.defense = 10;
			NPC.lifeMax = 2350;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 2400f;
			NPC.npcSlots = 100;
            NPC.scale = 1.1f;
			NPC.knockBackResist = 0.1f;
			Main.npcFrameCount[NPC.type] = 16;
			AnimationType = 28;
			NPC.lavaImmune = true;
			NPC.buffImmune[BuffID.Venom] = true;
			NPC.buffImmune[BuffID.Confused] = true;
			NPC.buffImmune[BuffID.CursedInferno] = true;
			NPC.buffImmune[BuffID.OnFire] = true;
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
            bool oBorders = (y < (Main.maxTilesY * 0.03f) || x < (Main.maxTilesX * 0.03f) || y > (Main.maxTilesY * 0.97f) || x > (Main.maxTilesX * 0.97f));
            int tile = (int)Main.tile[x, y].TileType;
            Player p = s.Player;
            if ((p.townNPCs > 2f && !Main.bloodMoon) || Main.pumpkinMoon || Main.snowMoon || !p.ZoneJungle || oUnderworld || oBorders)
            {
                return 0f;
            }
            if (oSurface || oUnderSurface || oUnderground || oCavern)
            {
                if (Main.rand.Next(12000) == 1) return 1f;
                else if (Main.hardMode && Main.rand.Next(50) == 1) return 1f;
                else if ((oUnderground || oCavern) && Main.rand.Next(800) == 1) return 1f;
                else if (Main.hardMode && (oUnderground || oCavern) && Main.rand.Next(30) == 1) return 1f;
                else if (Main.bloodMoon && Main.rand.Next(120) == 1) return 1f;
                return 0f;
            }
            return 0f;
        }
        //Spawns in the Jungle, mostly Underground and in the Cavern.

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
			MNPC.teleporterAI
			(
				NPC, 
				ref NPC.ai, 
				false, 		// immobile		Whether or not this NPC should move while its teleporting.
				20, 		// tpRadius		Radius around the player where the NPC will try to move.
				13,			// distToPlayer	Minimum distance to keep from the player as the NPC teleports.
				60,			// tpInterval	How often the NPC will try to teleport, tied to NPC.ai[3].
				true, 		// aerial		Whether or not an NPC will try to move to an airborne position.
				teleport	// tpEffect		The effect that the NPC will create as it moves.
			);
			MNPC.fighterAI
			(
				NPC, 
				ref NPC.ai,
				false,		// nocturnal  	If true, flees when it is daytime.
				true,		// focused 		If true, NPC wont get interrupted when hit or confused.
				60, 		// boredom 		The amount of ticks until the NPC gets 'bored' following a target.
				2, 			// knockPower 	0 == do not interact with doors, attempt to open the doors by this value, negative numbers will break instead
				accel, 		// accel 		The rate velocity X increases by when moving.
				topSpeed,	// topSpeed 	the maximum velocity on the X axis.
				2, 			// leapReq 		-1 NPC wont jump over gaps, more than 0 NPC will leap at players
				5, 			// leapSpeed	The max tiles it can jump across and over, horizontally. 
				9, 			// leapHeight 	The max tiles it can jump across and over, vertically. 
				100,		// leapRangeX 	The distance from a player before the NPC initiates leap, horizontally. 
				50,			// leapRangeY 	The distance from a player before the NPC initiates leap, vertically. 
				0, 			// shotType 	If higher than 0, allows an NPC to fire a Projectile, archer style.
				40,			// shotRate 	The rate of fire of the Projectile, if there is one.
				70,			// shotPow 		The Projectile's damage, if -1 it will use the Projectile's default.
				14			// shotSpeed	The Projectile's velocity.
			);
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
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsHydraGore1").Type, 1.1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsHydraGore2").Type, 1.1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsHydraGore3").Type, 1.1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsHydraGore2").Type, 1.1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsHydraGore3").Type, 1.1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsHydraGore1").Type, 1.1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsHydraGore1").Type, 1.1f);
            //if (Main.rand.Next(10) == 0)
            //{
            //    // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsEnergyBeamScroll"));
            //}
            //if (Main.rand.Next(7) == 0)
            //{
            //    // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsRoyalHelmet"));
            //}
            //if (Main.rand.Next(33) == 0)
            //{
            //    // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsDragonHorn"));
            //}
            //if (Main.rand.Next(5) == 0)
            //{
            //    // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsBootsofHaste"));
            //}
            //    // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsDragonHam"));
        }
    }
}
