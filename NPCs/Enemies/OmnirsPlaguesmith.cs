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
	public class OmnirsPlaguesmith : ModNPC
	{
        float customAi1;
        float customspawn1;
        int OTimeLeft = 2000;
        int movedTowards = 0;
        int num94 = 0;
        int num95 = 0;
        int noJump = 0;
        bool walkAndShoot = true;

        bool canDrown = true;
        int drownTimerMax = 2500;
        int drownTimer = 2500;
        int drowningRisk = 500;

        float npcAcSPD = 0.9f; //How fast they accelerate.
        float npcSPD = 2.3f; //Max speed

        float npcEnrAcSPD = 1.1f; //How fast they accelerate.
        float npcEnrSPD = 4f; //Max speed

        bool tooBig = true;
        bool lavaJumping = false;
        bool thruWalls = true;
        int oNPCNoReach = 0;
        bool phaseThruWalls = false;
        bool oPhasing1 = false;
        bool oPhasing2 = false;
        bool oDigSound = false;
        int oAtt = 50;
        int oDef = 35;
        public override void SetDefaults()
		{
			
			
			NPC.width = 50;
			NPC.height = 80;
			NPC.damage = 50;
			NPC.defense = 35;
			NPC.lifeMax = 8250;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 25500f;
			NPC.npcSlots = 100;
			NPC.knockBackResist = 0.1f;
			Main.npcFrameCount[NPC.type] = 9;
			AnimationType = -1;
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
            int tile = (int)Main.tile[x, y].TileType;
            Player p = s.Player;
            if (Main.pumpkinMoon || Main.snowMoon || !Main.hardMode || p.ZoneDungeon || p.ZoneMeteor)
            {
                return 0f;
            }
            if (oUnderworld)
            {
                if (p.townNPCs <= 2f && Main.rand.Next(800) == 1) return 1f;
                else if (p.townNPCs <= 2f && (x < Main.maxTilesX * 0.2f || x > Main.maxTilesX * 0.8f) && Main.rand.Next(150) == 1) return 1f;
                else if (Main.bloodMoon && Main.rand.Next(100) == 1) return 1f;
                else if (NPC.downedPlantBoss && Main.rand.Next(15) == 1) return 1f;
                return 0f;
            }
            return 0f;
        }
        //Spawns in Hardmode Underworld, mostly under 2/10th and after 8/10th (Width) when there are no Town NPCs (Unless a Blood Moon).

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
            #region Ogre/custom frames
            int num = 1;
            if (!Main.dedServ)
            {
                num = 106;
            }
            if (NPC.velocity.Y == 0f)
            {
                if (NPC.direction == 1)
                {
                    NPC.spriteDirection = 1;
                }
                if (NPC.direction == -1)
                {
                    NPC.spriteDirection = -1;
                }
                if (NPC.velocity.X == 0f)
                {
                    if (NPC.type == 140)
                    {
                        NPC.frame.Y = num;
                        NPC.frameCounter = 0.0;
                    }
                    else
                    {
                        NPC.frame.Y = 0;
                        NPC.frameCounter = 0.0;
                    }
                }
                else
                {
                    NPC.frameCounter += (double)(Math.Abs(NPC.velocity.X) * 2f);
                    NPC.frameCounter += 1.0;
                    if (NPC.frameCounter > 6.0)
                    {
                        NPC.frame.Y = NPC.frame.Y + num;
                        NPC.frameCounter = 0.0;
                    }
                    if (NPC.frame.Y / num >= Main.npcFrameCount[NPC.type])
                    {
                        NPC.frame.Y = num * 2;
                    }
                }
            }
            else
            {
                NPC.frameCounter = 0.0;
                NPC.frame.Y = num;
                NPC.frame.Y = 0;
            }
            #endregion
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
            #region "New" shooting code
            //if (distance < 600 && Main.netMode != 1 && !Main.player[NPC.target].dead)
            //{
            //    NPC.localAI[3] += (Main.rand.Next(2, 5) * 0.1f) * NPC.scale;
            //    if (NPC.localAI[3] >= 10f)
            //    {
            //        NPC.TargetClosest(true);
            //        //if (spawnLimit < 7 && Main.rand.Next(3500)==1)
            //        //{
            //        //	int spawn = NPC.NewNPC(NPC.GetSource_FromAI(), (int) NPC.position.X+(NPC.width/2), (int) NPC.position.Y+(NPC.height/2), 104, 0);
            //        //	Main.npc[spawn].velocity.Y = -8;
            //        //	Main.npc[spawn].velocity.X = Main.rand.Next(-10,10)/10;
            //        //	spawnLimit += 1f;
            //        //	if (Main.netMode == 2) NetMessage.SendData(23, -1, -1, "OmnirsFireElemental", spawn, 0f, 0f, 0f, 0);
            //        //}
            //        if ((angle.X < 0f && NPC.velocity.X < 0f) || (angle.X > 0f && NPC.velocity.X > 0f))
            //        {
            //            if (Main.rand.Next(800) == 1)
            //            {
            //                angle *= 8f;
            //                int damage = 100;
            //                int type = ModContent.ProjectileType<Projectiles.OmnirsEnemyGreatEnergyBeam1>();
            //                int proj = Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center.X, NPC.Center.Y, angle.X, angle.Y, type, damage, 0f, Main.myPlayer);
            //                Main.projectile[proj].timeLeft = 0;
            //                Main.projectile[proj].aiStyle = 1;
            //                SoundEngine.PlaySound(SoundID.Item11, NPC.Center);
            //                NPC.localAI[3] = 1f;
            //                NPC.netUpdate = true;
            //            }
            //        }
            //    }
            //}
            //if (Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height))
            //{
            //    if (Main.rand.Next(70) == 1)
            //    {
            //        angle *= 8f;
            //        int damage = 100;
            //        int type = ModContent.ProjectileType<Projectiles.OmnirsFireBreath>();
            //        int proj = Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center.X, NPC.Center.Y, angle.X, angle.Y, type, damage, 0f, Main.myPlayer);
            //        Main.projectile[proj].timeLeft = 100;
            //        Main.projectile[proj].aiStyle = 23;
            //        SoundEngine.PlaySound(SoundID.Item11, NPC.Center);
            //        NPC.localAI[3] = 1f;
            //        NPC.netUpdate = true;
            //    }
            //    if (Main.rand.Next(200) == 1)
            //    {
            //        angle *= 8f;
            //        int damage = 70;
            //        int type = ModContent.ProjectileType<Projectiles.OmnirsGreatFireball1>();
            //        int proj = Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center.X, NPC.Center.Y, angle.X, angle.Y, type, damage, 0f, Main.myPlayer);
            //        Main.projectile[proj].timeLeft = 100;
            //        Main.projectile[proj].aiStyle = 23;
            //        SoundEngine.PlaySound(SoundID.Item11, NPC.Center);
            //        NPC.localAI[3] = 1f;
            //        NPC.netUpdate = true;
            //    }
            //    if (Main.rand.Next(140) == 1)
            //    {
            //        angle *= 8f;
            //        int damage = 150;
            //        int type = ModContent.ProjectileType<Projectiles.OmnirsGreatAttack>();
            //        int proj = Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center.X, NPC.Center.Y, angle.X, angle.Y, type, damage, 0f, Main.myPlayer);
            //        Main.projectile[proj].timeLeft = 10;
            //        Main.projectile[proj].aiStyle = 1;
            //        SoundEngine.PlaySound(SoundID.Item11, NPC.Center);
            //        NPC.localAI[3] = 1f;
            //        NPC.netUpdate = true;
            //    }
            //}
            #endregion
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
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsPlaguesmithGore1").Type, 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsPlaguesmithGore2").Type, 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsPlaguesmithGore3").Type, 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsPlaguesmithGore4").Type, 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsPlaguesmithGore5").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsPlaguesmithGore6").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsPlaguesmithGore6").Type, 1f);
            if (Main.rand.Next(6) == 0)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsGoldenArmor"));
            }
            if (Main.rand.Next(12) == 0)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsDragonScaleMail"));
            }
            if (Main.rand.Next(20) == 0)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsWarhammer"));
            }
            if (Main.rand.Next(1) == 0)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsSuddenDeathRune"), Main.rand.Next(3, 9));
            }
            if (Main.rand.Next(1) == 0)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsPoisonBombRune"), Main.rand.Next(3, 9));
            }
        }
    }
}
