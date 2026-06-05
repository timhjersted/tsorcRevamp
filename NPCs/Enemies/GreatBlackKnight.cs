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
    public class GreatBlackKnight : ModNPC
    {

        float customAi1;
        float customspawn1;
        float customspawn2;
        int chargeDamage = 0;
        bool chargeDamageFlag = false;

        int OTimeLeft = 2000;
        int movedTowards = 0;
        int num94 = 0;
        int num95 = 0;
        int noJump = 0;
        bool walkAndShoot = true;

        bool canDrown = false;
        int drownTimerMax = 2000;
        int drownTimer = 2000;
        int drowningRisk = 1200;

        float npcAcSPD = 1.2f; //How fast they accelerate.
        float npcSPD = 4.5f; //Max speed

        float npcEnrAcSPD = 2.95f; //How fast they accelerate.
        float npcEnrSPD = 6.0f; //Max speed

        bool tooBig = true;
        bool lavaJumping = true;

        public override void SetDefaults()
        {
            
            NPC.lifeMax = 66000;
            NPC.damage = 350;
            NPC.defense = 3000;
            NPC.knockBackResist = 0.1f;
            NPC.width = 30;
            NPC.height = 40;
            NPC.aiStyle = -1;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath6;
            Main.npcFrameCount[NPC.type] = 16;
            NPC.value = 0f;
            AnimationType = 28;
            NPC.scale = 1.2f;
            NPC.boss = true;
            Banner = NPC.type;
            BannerItem = 0;
        }

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
            bool enraged = (NPC.life < (float)NPC.lifeMax * .2f);  //  speed up at low life
            int shotRate = enraged ? 100 : 70;
            float accel = enraged ? npcEnrAcSPD : npcAcSPD;  //  how fast it can speed up
            float topSpeed = enraged ? npcEnrSPD : npcSPD;  //  max walking speed, also affects jump length
            MNPC.teleporterAI
            (
                NPC,
                ref NPC.ai,
                false,      // immobile		Whether or not this NPC should move while its teleporting.
                20,         // tpRadius		Radius around the player where the NPC will try to move.
                13,         // distToPlayer	Minimum distance to keep from the player as the NPC teleports.
                60,         // tpInterval	How often the NPC will try to teleport, tied to NPC.ai[3].
                true,       // aerial		Whether or not an NPC will try to move to an airborne position.
                teleport    // tpEffect		The effect that the NPC will create as it moves.
            );
            MNPC.fighterAI
            (
                NPC,
                ref NPC.ai,
                false,      // nocturnal  	If true, flees when it is daytime.
                true,       // focused 		If true, NPC wont get interrupted when hit or confused.
                60,         // boredom 		The amount of ticks until the NPC gets 'bored' following a target.
                2,          // knockPower 	0 == do not interact with doors, attempt to open the doors by this value, negative numbers will break instead
                accel,      // accel 		The rate velocity X increases by when moving.
                topSpeed,   // topSpeed 	the maximum velocity on the X axis.
                2,          // leapReq 		-1 NPC wont jump over gaps, more than 0 NPC will leap at players
                5,          // leapSpeed	The max tiles it can jump across and over, horizontally. 
                9,          // leapHeight 	The max tiles it can jump across and over, vertically. 
                100,        // leapRangeX 	The distance from a player before the NPC initiates leap, horizontally. 
                50,         // leapRangeY 	The distance from a player before the NPC initiates leap, vertically. 
                0,          // shotType 	If higher than 0, allows an NPC to fire a Projectile, archer style.
                40,         // shotRate 	The rate of fire of the Projectile, if there is one.
                70,         // shotPow 		The Projectile's damage, if -1 it will use the Projectile's default.
                14          // shotSpeed	The Projectile's velocity.
            );
            Vector2 angle = Main.player[NPC.target].Center - NPC.Center;
            angle.Y = angle.Y - (Math.Abs(angle.X) * .1f);
            angle.X += (float)Main.rand.Next(-20, 21);
            angle.Y += (float)Main.rand.Next(-20, 21);
            angle.Normalize();
            float distance = NPC.Distance(Main.player[NPC.target].Center);
            #region shoot and walk
            if (Main.netMode != 1)
            {
                if (NPC.justHit)
                    NPC.ai[2] = 0f; // reset throw countdown when hit
                if (Main.rand.Next(200) == 1)
                {
                    chargeDamageFlag = true;
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    NPC.velocity.X = (float)(Math.Cos(rotation) * 14) * -1;
                    NPC.velocity.Y = (float)(Math.Sin(rotation) * 14) * -1;
                    NPC.ai[1] = 1f;
                    NPC.netUpdate = true;
                }
                if (chargeDamageFlag == true)
                {
                    NPC.damage = 220;
                    chargeDamage++;
                }
                if (chargeDamage >= 50)
                {
                    chargeDamageFlag = false;
                    NPC.damage = 50;
                    chargeDamage = 0;
                }
                #region Spawn
                /*\r\n                if ((customspawn1 < 8) && Main.rand.Next(250) == 1)\r\n                {\r\n                    int npcToSpawn = ModContent.NPCType<NPCs.Enemies.OmnirsNazgul>();\r\n                    int npcIndex = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, npcToSpawn, 0, 0f, 0f, 0f, 0f, 255);\r\n                    Main.npc[npcIndex].whoAmI = npcIndex;\r\n                    NetMessage.SendData(23, -1, -1, "", npcIndex, 0f, 0f, 0f, 0, 0, 0);\r\n                    Main.npc[npcIndex].BigMimicSpawnSmoke();\r\n                    customspawn1 += 1f;\r\n                }\r\n                */
                /*\r\n                if ((customspawn2 < 1) && Main.rand.Next(250) == 1)\r\n                {\r\n                    int npcToSpawn = ModContent.NPCType<NPCs.Bosses.SuperHardMode.Witchking>();\r\n                    int npcIndex = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, npcToSpawn, 0, 0f, 0f, 0f, 0f, 255);\r\n                    Main.npc[npcIndex].whoAmI = npcIndex;\r\n                    NetMessage.SendData(23, -1, -1, "", npcIndex, 0f, 0f, 0f, 0, 0, 0);\r\n                    Main.npc[npcIndex].BigMimicSpawnSmoke();\r\n                    customspawn2 += 1f;\r\n                }\r\n                */
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

        public override void OnKill()
        {
            if (Main.netMode == 2)
            {
                return;
            }
            //generate particle effect
            Color color = new Color();
            Rectangle rectangle = new Rectangle((int)NPC.position.X, (int)(NPC.position.Y + ((NPC.height - NPC.width) / 2)), NPC.width, NPC.width);//NPC.frame;
            int count = 50;
            float vectorReduce = .4f;
            for (int i = 1; i <= count; i++)
            {
                //int dust = Dust.NewDust(new Vector2((float) rectangle.X, (float) rectangle.Y), rectangle.Width, rectangle.Height, 6, (NPC.velocity.X * 0.2f) + (NPC.direction * 3), NPC.velocity.Y * 0.2f, 100, color, 1.9f);
                int dust = Dust.NewDust(NPC.position, rectangle.Width, rectangle.Height, 5, 0, 0, 100, color, 1.8f);
                Main.dust[dust].noGravity = false;
                Main.dust[dust].velocity.X = vectorReduce * (Main.dust[dust].position.X - (NPC.position.X + (NPC.width / 2)));
                Main.dust[dust].velocity.Y = vectorReduce * (Main.dust[dust].position.Y - (NPC.position.Y + (NPC.height / 2)));
            }
            int sLoot = (Main.rand.Next(3));
            int sLoot2 = (Main.rand.Next(4));
            int sLoot3 = (Main.rand.Next(4));
            if (sLoot == 0)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsSecondAgeElvenHelmet"));
                if (Main.rand.Next(3) == 0)
                {
                    // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsSauronsHelmet"));
                }
            }
            if (sLoot == 1)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsSecondAgeElvenArmor"));
                if (Main.rand.Next(3) == 0)
                {
                    // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsSauronsArmor"));
                }
            }
            if (sLoot == 2)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsSecondAgeElvenGreaves"));
                if (Main.rand.Next(3) == 0)
                {
                    // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsSauronsGreaves"));
                }
            }
            if (Main.rand.Next(10) == 0)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsSauronMace"));
            }
            if (Main.rand.Next(20) == 0)
            {
                // Item.NewItem((int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, Mod.ItemType("OmnirsTheOneRing"));
            }
        }
    }
}