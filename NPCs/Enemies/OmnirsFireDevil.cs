using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
//using Microsoft.Xna.Framework.Graphics;
//using Microsoft.Xna.Framework.Input;
//using System.Linq;
//using System.Text;

namespace tsorcRevamp.NPCs.Enemies
{
	public class OmnirsFireDevil : ModNPC
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
        int drownTimerMax = 1200;
        int drownTimer = 1200;
        int drowningRisk = 1200;

        float npcAcSPD = 0.6f; //How fast they accelerate.
        float npcSPD = 2.5f; //Max speed

        float npcEnrAcSPD = .70f; //How fast they accelerate when enraged.
        float npcEnrSPD = 4.0f; //Max speed when enraged

        bool tooBig = false;
        bool lavaJumping = true;
        bool thruWalls = false;

        public override void SetDefaults()
		{
			//npc.name = "Fire Lurker";
			npc.displayName = "Fire Lurker";
			npc.width = 20;
			npc.height = 40;
			npc.damage = 65;
			npc.defense = 2;
			npc.lifeMax = 200;
            npc.scale = 1f;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath5;
            npc.value = 350f;
			npc.npcSlots = 20;
			npc.knockBackResist = 0.1f;
			Main.npcFrameCount[npc.type] = 15;
			animationType = 21;
			npc.lavaImmune = true;
			npc.buffImmune[BuffID.Venom] = false;
			npc.buffImmune[BuffID.Confused] = true;
			npc.buffImmune[BuffID.CursedInferno] = false;
			npc.buffImmune[BuffID.OnFire] = true;
		}
        public override float CanSpawn(NPCSpawnInfo)
        {
            int x = s.spawnTileX;
            int y = s.spawnTileY;
            bool oSky = (y < (Main.maxTilesY * 0.1f));
            bool oSurface = (y >= (Main.maxTilesY * 0.1f) && y < (Main.maxTilesY * 0.2f));
            bool oUnderSurface = (y >= (Main.maxTilesY * 0.2f) && y < (Main.maxTilesY * 0.3f));
            bool oUnderground = (y >= (Main.maxTilesY * 0.3f) && y < (Main.maxTilesY * 0.4f));
            bool oCavern = (y >= (Main.maxTilesY * 0.4f) && y < (Main.maxTilesY * 0.6f));
            bool oMagmaCavern = (y >= (Main.maxTilesY * 0.6f) && y < (Main.maxTilesY * 0.8f));
            bool oUnderworld = (y >= (Main.maxTilesY * 0.8f));
            int tile = (int)Main.tile[x, y].type;
            Player p = s.player;
            if (Main.pumpkinMoon || Main.snowMoon || Main.hardMode || p.ZoneDungeon || p.ZoneMeteor)
            {
                return 0f;
            }
            if (oUnderworld && !Main.hardMode)
            {
                if (x < Main.maxTilesX * 0.25f && Main.rand.Next(10) == 1) return 1f;
                else if (Main.bloodMoon && Main.rand.Next(15) == 1) return 1f;
                return 0f;
            }
            return 0f;
        }
        //Spawns in the Lower Cavern into the Underworld. Does not spawn in Hardmode.
        public void teleport(bool pre)
		{
			if (Main.netMode != 2)
			{
				Main.PlaySound(2, (int)npc.Center.X, (int)npc.Center.Y, 8);
				for (int m = 0; m < 25; m++)
				{
					int dustID = Dust.NewDust(npc.position, npc.width, npc.height, 6, 0, 0, 100, Color.White, 2f);
					Main.dust[dustID].noGravity = true;
					Main.dust[dustID].velocity = new Vector2(MathHelper.Lerp(-1f, 1f, (float)Main.rand.NextDouble()), MathHelper.Lerp(-1f, 1f, (float)Main.rand.NextDouble()));
					Main.dust[dustID].velocity *= 7f;
				}
			}
		}
        public override void AI()
        {
            npc.noGravity = false;
            npc.lavaImmune = true;
            npc.spriteDirection = npc.direction;
            bool enraged = (npc.life < (float)npc.lifeMax * .2f);  //  speed up at low life
            int shotRate = enraged ? 100 : 70;
            float accel = enraged ? npcEnrAcSPD : npcAcSPD;  //  how fast it can speed up
            float topSpeed = enraged ? npcEnrSPD : npcSPD;  //  max walking speed, also affects jump length
            for (int j = 0; j < 2; j++)
            {
                int dustID = Dust.NewDust(npc.position, npc.width, npc.height, 6, npc.velocity.X * 0.025f, npc.velocity.Y * 0.025f, 100, default(Color), 2.2f);
                Main.dust[dustID].noGravity = true;
                Main.dust[dustID].velocity.Y--;
            }
            MNPC.teleporterAI
            (
                npc,
                ref npc.ai,
                false,      // immobile		Whether or not this NPC should move while its teleporting.
                20,         // tpRadius		Radius around the player where the NPC will try to move.
                13,         // distToPlayer	Minimum distance to keep from the player as the NPC teleports.
                60,         // tpInterval	How often the NPC will try to teleport, tied to npc.ai[3].
                true,       // aerial		Whether or not an NPC will try to move to an airborne position.
                teleport    // tpEffect		The effect that the NPC will create as it moves.
            );
            MNPC.fighterAI
            (
                npc,
                ref npc.ai,
                false,      // nocturnal  	If true, flees when it is daytime.
                true,       // focused 		If true, npc wont get interrupted when hit or confused.
                60,         // boredom 		The amount of ticks until the npc gets 'bored' following a target.
                2,          // knockPower 	0 == do not interact with doors, attempt to open the doors by this value, negative numbers will break instead
                accel,      // accel 		The rate velocity X increases by when moving.
                topSpeed,   // topSpeed 	the maximum velocity on the X axis.
                2,          // leapReq 		-1 npc wont jump over gaps, more than 0 npc will leap at players
                5,          // leapSpeed	The max tiles it can jump across and over, horizontally. 
                9,          // leapHeight 	The max tiles it can jump across and over, vertically. 
                100,        // leapRangeX 	The distance from a player before the npc initiates leap, horizontally. 
                50,         // leapRangeY 	The distance from a player before the npc initiates leap, vertically. 
                0,          // shotType 	If higher than 0, allows an npc to fire a projectile, archer style.
                40,         // shotRate 	The rate of fire of the projectile, if there is one.
                70,         // shotPow 		The projectile's damage, if -1 it will use the projectile's default.
                14          // shotSpeed	The projectile's velocity.
            );
            Vector2 angle = Main.player[npc.target].Center - npc.Center;
            angle.Y = angle.Y - (Math.Abs(angle.X) * .1f);
            angle.X += (float)Main.rand.Next(-20, 21);
            angle.Y += (float)Main.rand.Next(-20, 21);
            angle.Normalize();
            if (npc.lavaWet) npc.velocity.Y -= 2;
            float distance = npc.Distance(Main.player[npc.target].Center);
            #region shoot and walk
            if (walkAndShoot && Main.netMode != 1 && !Main.player[npc.target].dead) // can generalize this section to moving+projectile code // can generalize this section to moving+projectile code
            {
                if (npc.justHit)
                    npc.ai[2] = 0f; // reset throw countdown when hit
                #region Projectiles
                customAi1 += (Main.rand.Next(2, 5) * 0.1f) * npc.scale;
                if (customAi1 >= 10f)
                {
                    npc.TargetClosest(true);
                    if (Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
                    {
                        if (Main.rand.Next(200) == 1)
                        {

                            float num48 = 8f;
                            Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                            float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                            float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                            if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
                            {
                                Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 5);
                                float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                                num51 = num48 / num51;
                                speedX *= num51;
                                speedY *= num51;
                                int damage = 10;
                                int type = mod.ProjectileType("OmnirsEnemySpellGreatFireballBall");
                                int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, damage, 0f, Main.myPlayer);
                                Main.projectile[num54].timeLeft = 300;
                                Main.projectile[num54].aiStyle = 1;
                                customAi1 = 1f;
                            }
                            npc.netUpdate = true;
                        }
                        if (Main.rand.Next(1000) == 1)
                        {
                            float num48 = 8f;
                            Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                            float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                            float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                            if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
                            {
                                Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 5);
                                float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                                num51 = num48 / num51;
                                speedX *= num51;
                                speedY *= num51;
                                int damage = 30;
                                int type = mod.ProjectileType("OmnirsEnemySpellExplosionBall");
                                int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, 0, 0, type, damage, 0f, Main.myPlayer);
                                Main.projectile[num54].timeLeft = 100;
                                Main.projectile[num54].aiStyle = 1;
                                customAi1 = 1f;
                            }
                            npc.netUpdate = true;
                        }
                    }
                }
                #endregion
                #region Charge
                if (npc.velocity.Y == 0f && Main.rand.Next(550) == 1)
                {
                    Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
                    npc.velocity.X = (float)(Math.Cos(rotation) * 7) * -1;
                    npc.velocity.Y = (float)(Math.Sin(rotation) * 7) * -1;
                    npc.ai[1] = 1f;
                    npc.netUpdate = true;
                }
                #endregion
            }
            #endregion
            #region drown // code by Omnir
            if (canDrown)
            {
                if (!npc.wet)
                {
                    npc.TargetClosest(true);
                    drownTimer = drownTimerMax;
                }
                if (npc.wet)
                {
                    drownTimer--;
                }
                if (npc.wet && drownTimer > drowningRisk)
                {
                    npc.TargetClosest(true);
                }
                else if (npc.wet && drownTimer <= drowningRisk)
                {
                    npc.TargetClosest(false);
                    if (npc.timeLeft > 10)
                    {
                        npc.timeLeft = 10;
                    }
                    npc.directionY = -1;
                    if (npc.velocity.Y > 0f)
                    {
                        npc.direction = 1;
                    }
                    npc.direction = -1;
                    if (npc.velocity.X > 0f)
                    {
                        npc.direction = 1;
                    }
                }
                if (drownTimer <= 0)
                {
                    npc.life--;
                    if (npc.life <= 0)
                    {
                        Main.PlaySound(4, (int)npc.position.X, (int)npc.position.Y, 1);
                        npc.NPCLoot();
                        npc.netUpdate = true;
                    }
                }
            }
            #endregion
            #region Too Big and Lava Jumping
            if (tooBig)
            {
                if (npc.velocity.Y == 0f && (npc.velocity.X == 0f && npc.direction < 0))
                {
                    npc.velocity.Y -= 8f;
                    npc.velocity.X -= npcSPD;
                }
                else if (npc.velocity.Y == 0f && (npc.velocity.X == 0f && npc.direction > 0))
                {
                    npc.velocity.Y -= 8f;
                    npc.velocity.X += npcSPD;
                }
            }
            if (lavaJumping)
            {
                if (npc.lavaWet)
                {
                    npc.velocity.Y -= 2;
                }
            }
            #endregion
        }

        public override void NPCLoot()
        {
            Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/OmnirsFireDevilGore1"), 1f);
            Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/OmnirsFireDevilGore2"), 1f);
            Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/OmnirsFireDevilGore3"), 1f);
            Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/OmnirsFireDevilGore2"), 1f);
            Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/OmnirsFireDevilGore3"), 1f);
            //if (Main.rand.Next(2) == 0)
            //{
            //    Item.NewItem((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height, mod.ItemType("OmnirsExplosionRune"),Main.rand.Next(1, 5));
            //}
            //if (Main.rand.Next(2) == 0)
            //{
            //    Item.NewItem((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height, mod.ItemType("OmnirsFireFieldRune"),Main.rand.Next(1, 3));
            //}
            //if (Main.rand.Next(10) == 0)
            //{
            //    Item.NewItem((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height, mod.ItemType("OmnirsFireBombRune"),Main.rand.Next(1, 2));
            //}
            //if (Main.rand.Next(50) == 0)
            //{
            //    Item.NewItem((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height, mod.ItemType("OmnirsFireWaveScroll"));
            //}
            //if (Main.rand.Next(20) == 0)
            //{
            //    Item.NewItem((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height, mod.ItemType("OmnirsGuardianShield"));
            //}
        }
    }
}
