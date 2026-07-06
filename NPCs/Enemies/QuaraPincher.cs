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
	// Sprite by Omnir, from Omnir's Nostalgia Pack: https://forums.terraria.org/index.php?threads/omnirs-nostalgia-pack.11875/
	public class QuaraPincher : ModNPC
	{
        float customAi1;
        int OTimeLeft = 2000;
        bool walkAndShoot = false;

        bool canDrown = false;
        int drownTimerMax = 2000;
        int drownTimer = 2000;
        int drowningRisk = 1200;

        float npcAcSPD = 0.8f; //How fast they accelerate.
        float npcSPD = 1.5f; //Max speed

        bool tooBig = true;
        bool lavaJumping = false;

        float npcEnrAcSPD = 1.0f; //How fast they accelerate.
        float npcEnrSPD = 2.0f; //Max speed

        public override void SetDefaults()
		{
			
			
			NPC.width = 18;
			NPC.height = 45;
			NPC.damage = 56;
			NPC.defense = 12;
			NPC.lifeMax = 1800;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 2600f;
			NPC.npcSlots = 100;
            NPC.scale = 1.2f;
			NPC.knockBackResist = 0.8f;
			Main.npcFrameCount[NPC.type] = 15;
			AnimationType = 21;
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
            bool oOcean = (x <= (Main.maxTilesX * .2f) && x < (Main.maxTilesX * 0.8f) && y < (Main.maxTilesY * 0.4f));
            int tile = (int)Main.tile[x, y].TileType;
            Player p = s.Player;
            if (Main.pumpkinMoon || Main.snowMoon || p.townNPCs > 0f || p.ZoneDungeon)
            {
                return 0f;
            }
            if (oOcean)
            {
                if (oSurface || oUnderSurface || oUnderground)
                {
                    if (Main.rand.Next(600) == 1) return 1f;
                    else if ((oUnderSurface || oUnderground) && Main.rand.Next(175) == 1) return 1f;
                    if (Main.hardMode)
                    {
                        if (Main.rand.Next(31) == 1) return 1f;
                        else if ((oUnderSurface || oUnderground) && Main.rand.Next(19) == 1) return 1f;
                        return 0f;
                    }
                    return 0f;
                }
                return 0f;
            }
            return 0f;
        }
        //Spawns in the Ocean down into the Underground. Does not spawn in the Dungeon, Meteor, or if there are Town NPCs.

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
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("QuaraPincherGore1").Type, 1.2f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("QuaraPincherGore2").Type, 1.2f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("QuaraPincherGore2").Type, 1.2f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("QuaraPincherGore3").Type, 1.2f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("QuaraPincherGore3").Type, 1.2f);

            //if (Main.rand.Next(3) == 0)
            //{
            //    // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsGreatFireballRune"), Main.rand.Next(1, 20));
            //}
            if (Main.rand.Next(8) == 0)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsWarriorHelmet"));
            }
            if (Main.rand.Next(8) == 0)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsKnightArmor"));
            }
            if (Main.rand.Next(8) == 0)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsKnightGreaves"));
            }
        }
        #endregion
    }
}
