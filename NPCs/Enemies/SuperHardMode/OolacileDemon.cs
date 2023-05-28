using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using Terraria.GameContent.ItemDropRules;
using Terraria.DataStructures;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class OolacileDemon : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 5;
            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[]
                {
                    BuffID.Confused
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            AnimationType = 62;
            NPC.width = 70;
            NPC.height = 70;
            NPC.aiStyle = 22;
            NPC.damage = 63;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.defense = 70;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.lavaImmune = true;
            NPC.DeathSound = new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/NPCKilled/Gaibon_Roar");
            NPC.lifeMax = 4500;
            NPC.scale = 1.1f;
            NPC.knockBackResist = 0.2f;
            NPC.noGravity = true;
            NPC.noTileCollide = false;
            NPC.value = 18750;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.OolacileDemonBanner>();
        }
        int cursedBreathDamage = 35;
        int bioSpitDamage = 40;
        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
            cursedBreathDamage = (int)(cursedBreathDamage * tsorcRevampWorld.SHMScale);
            bioSpitDamage = (int)(bioSpitDamage * tsorcRevampWorld.SHMScale);
        }

        //float customAi1;
        int breathCD = 60;
        bool breath = false;

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player; //this shortens our code up from writing this line over and over.

            bool Sky = spawnInfo.SpawnTileY <= (Main.rockLayer * 4);
            bool Meteor = P.ZoneMeteor;
            bool Jungle = P.ZoneJungle;
            bool Dungeon = P.ZoneDungeon;
            bool Corruption = (P.ZoneCorrupt || P.ZoneCrimson);
            bool Hallow = P.ZoneHallow;
            bool AboveEarth = spawnInfo.SpawnTileY < Main.worldSurface;
            bool InBrownLayer = spawnInfo.SpawnTileY >= Main.worldSurface && spawnInfo.SpawnTileY < Main.rockLayer;
            bool InGrayLayer = spawnInfo.SpawnTileY >= Main.rockLayer && spawnInfo.SpawnTileY < (Main.maxTilesY - 200) * 16;
            bool InHell = spawnInfo.SpawnTileY >= (Main.maxTilesY - 200) * 16;
            bool Ocean = spawnInfo.SpawnTileX < 3600 || spawnInfo.SpawnTileX > (Main.maxTilesX - 100) * 16;

            // these are all the regular stuff you get , now lets see......


            if (NPC.AnyNPCs(ModContent.NPCType<OolacileDemon>()))
            {
                return 0;
            }


            if (tsorcRevampWorld.SuperHardMode && !Main.dayTime && InHell && Main.rand.NextBool(13)) return 1;

            if (tsorcRevampWorld.SuperHardMode && Main.bloodMoon && InHell && Main.rand.NextBool(8)) return 1;

            if (tsorcRevampWorld.SuperHardMode && Main.dayTime && InHell && Main.rand.NextBool(20)) return 1;

            return 0;
        }
        #endregion

        #region AI
        public override void AI()
        {
            ////  can_teleport==true code uses boredom_time and ai[3] (boredom), but not mutually exclusive
            //	bool can_teleport=true;  //  tp around like chaos ele
            //	int boredom_time=60; // time until it stops targeting player if blocked etc, 60 for anything but chaos ele, 20 for chaos ele
            //	int boredom_cooldown=10*boredom_time; // boredom level where boredom wears off; usually 10*boredom_time
            //	double bored_speed=.2;  //  above this speed boredom decreases(if not already bored); usually .9

            //#region adjust boredom level
            //	if (npc.ai[2] <= 0f)  //  loop to set ai[3] (boredom)
            //	{
            //		if (npc.position.X == npc.oldPosition.X || npc.ai[3] >= (float)boredom_time || moonwalking)  //  stopped or bored or moonwalking
            //			npc.ai[3] += 1f; // increase boredom
            //		else if ((double)Math.Abs(npc.velocity.X) > bored_speed && npc.ai[3] > 0f)  //  moving fast and not bored
            //			npc.ai[3] -= 1f; // decrease boredom

            //		if (npc.justHit || npc.ai[3] > boredom_cooldown)
            //			npc.ai[3] = 0f; // boredom wears off if enough time passes, or if hit

            //		if (npc.ai[3] == (float)boredom_time)
            //			npc.netUpdate = true; // netupdate when state changes to bored
            //	}
            //#endregion

            //#region teleportation particle effects
            //	if (can_teleport)  //  chaos elemental type teleporter
            //	{
            //		if (npc.ai[3] == -120f)  //  boredom goes negative? I think this makes disappear/arrival effects after it just teleported
            //		{
            //			npc.velocity *= 0f; // stop moving
            //			npc.ai[3] = 0f; // reset boredom to 0
            //			Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 8);
            //			Vector2 vector = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f); // current location
            //			float num6 = npc.oldPos[2].X + (float)npc.width * 0.5f - vector.X; // direction to where it was 3 frames ago?
            //			float num7 = npc.oldPos[2].Y + (float)npc.height * 0.5f - vector.Y; // direction to where it was 3 frames ago?
            //			float num8 = (float)Math.Sqrt((double)(num6 * num6 + num7 * num7)); // distance to where it was 3 frames ago?
            //			num8 = 2f / num8; // to normalize to 2 unit long vector
            //			num6 *= num8; // direction to where it was 3 frames ago, vector normalized
            //			num7 *= num8; // direction to where it was 3 frames ago, vector normalized
            //			for (int j = 0; j < 20; j++) // make 20 dusts at current position
            //			{
            //				int num9 = Dust.NewDust(npc.position, npc.width, npc.height, 71, num6, num7, 200, default(Color), 2f);
            //				Main.dust[num9].noGravity = true; // floating
            //				Dust expr_19EE_cp_0 = Main.dust[num9]; // make a dust handle?
            //				expr_19EE_cp_0.velocity.X = expr_19EE_cp_0.velocity.X * 2f; // faster in x direction
            //			}
            //			for (int k = 0; k < 20; k++) // more dust effects at old position
            //			{
            //				int num10 = Dust.NewDust(npc.oldPos[2], npc.width, npc.height, 71, -num6, -num7, 200, default(Color), 2f);
            //				Main.dust[num10].noGravity = true;
            //				Dust expr_1A6F_cp_0 = Main.dust[num10];
            //				expr_1A6F_cp_0.velocity.X = expr_1A6F_cp_0.velocity.X * 2f;
            //			}
            //		} // END just teleported
            //	} // END can teleport
            //#endregion

            //#region check if standing on a solid tile
            //// warning: this section contains a return statement
            //	bool standing_on_solid_tile = false;
            //	if (npc.velocity.Y == 0f) // no jump/fall
            //	{
            //		int y_below_feet = (int)(npc.position.Y + (float)npc.height + 8f) / 16;
            //		int x_left_edge = (int)npc.position.X / 16;
            //		int x_right_edge = (int)(npc.position.X + (float)npc.width) / 16;
            //		for (int l = x_left_edge; l <= x_right_edge; l++) // check every block under feet
            //		{
            //			if (Main.tile[l, y_below_feet] == null) // null tile means ??
            //				return;

            //			if (Main.tile[l, y_below_feet].active() && Main.tileSolid[(int)Main.tile[l, y_below_feet].type]) // tile exists and is solid
            //			{
            //				standing_on_solid_tile = true;
            //				break; // one is enough so stop checking
            //			}
            //		} // END traverse blocks under feet
            //	} // END no jump/fall
            //#endregion


            //#region teleportation
            //	if (Main.netMode != 1 && can_teleport && npc.ai[3] >= (float)boredom_time) // is server & chaos ele & bored
            //	{
            //		int target_x_blockpos = (int)Main.player[npc.target].position.X / 16; // corner not center
            //		int target_y_blockpos = (int)Main.player[npc.target].position.Y / 16; // corner not center
            //		int x_blockpos = (int)npc.position.X / 16; // corner not center
            //		int y_blockpos = (int)npc.position.Y / 16; // corner not center
            //		int tp_radius = 15; // radius around target(upper left corner) in blocks to teleport into
            //		int tp_counter = 0;
            //		bool flag7 = false;
            //		if (Math.Abs(npc.position.X - Main.player[npc.target].position.X) + Math.Abs(npc.position.Y - Main.player[npc.target].position.Y) > 2000f)
            //		{ // far away from target; 2000 pixels = 125 blocks
            //			tp_counter = 100;
            //			flag7 = true; // no teleport
            //		}
            //		while (!flag7) // loop always ran full 100 time before I added "flag7 = true;" below
            //		{
            //			if (tp_counter >= 100) // run 100 times
            //				break; //return;
            //			tp_counter++;

            //			int tp_x_target = Main.rand.Next(target_x_blockpos - tp_radius, target_x_blockpos + tp_radius);  //  pick random tp point (centered on corner)
            //			int tp_y_target = Main.rand.Next(target_y_blockpos - tp_radius, target_y_blockpos + tp_radius);  //  pick random tp point (centered on corner)
            //			for (int m = tp_y_target; m < target_y_blockpos + tp_radius; m++) // traverse y downward to edge of radius
            //			{ // (tp_x_target,m) is block under its feet I think
            //				if ((m < target_y_blockpos - 6 || m > target_y_blockpos + 6 || tp_x_target < target_x_blockpos - 6 || tp_x_target > target_x_blockpos + 4) && (m < y_blockpos - 1 || m > y_blockpos + 1 || tp_x_target < x_blockpos - 1 || tp_x_target > x_blockpos + 1) && Main.tile[tp_x_target, m].active())
            //				{ // over 6 blocks distant from player & over 1 block distant from old position & tile active(to avoid surface? want to tp onto a block?)
            //					bool safe_to_stand = true;
            //					bool dark_caster = false; // not a fighter type AI...
            //					if (dark_caster && Main.tile[tp_x_target, m - 1].wall == 0) // Dark Caster & ?outdoors
            //						safe_to_stand = false;
            //					else if (Main.tile[tp_x_target, m - 1].lava()) // feet submerged in lava
            //							safe_to_stand = false;

            //					if (safe_to_stand && Main.tileSolid[(int)Main.tile[tp_x_target, m].type] && !Collision.SolidTiles(tp_x_target - 1, tp_x_target + 1, m - 4, m - 1))
            //					{ // safe enviornment & solid below feet & 3x4 tile region is clear; (tp_x_target,m) is below bottom middle tile
            //						npc.position.X = (float)(tp_x_target * 16 - npc.width / 2); // center x at target
            //						npc.position.Y = (float)(m * 16 - npc.height); // y so block is under feet
            //						npc.netUpdate = true;
            //						npc.ai[3] = -120f; // -120 boredom is signal to display effects & reset boredom next tick in section "teleportation particle effects"
            //						flag7 = true; // end the loop (after testing every lower point :/)
            //					}
            //				} // END over 6 blocks distant from player...
            //			} // END traverse y down to edge of radius
            //		} // END try 100 times
            //	} // END is server & chaos ele & bored
            //	#endregion






            //if(Main.netMode != 1)
            //{
            NPC.ai[1] += (Main.rand.Next(2, 5) * 0.1f) * NPC.scale;
            if (NPC.ai[1] >= 10f)
            {

                NPC.TargetClosest(true);
                if (Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height))
                {


                    //Player nT = Main.player[npc.target];
                    if (Main.rand.NextBool(1200))
                    {
                        breath = true;
                        //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                    }
                    if (breath)
                    {

                        float rotation = (float)Math.Atan2(NPC.Center.Y - Main.player[NPC.target].Center.Y, NPC.Center.X - Main.player[NPC.target].Center.X);
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y - 5, (float)((Math.Cos(rotation) * 25) * -1), (float)((Math.Sin(rotation) * 25) * -1), ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedBreath>(), cursedBreathDamage, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 30;
                        NPC.netUpdate = true;





                        breathCD--;
                        //}
                    }
                    if (breathCD <= 0)
                    {
                        breath = false;
                        breathCD = 60;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                    }



                    //if (Main.rand.NextBool(35)) 
                    //	{
                    //		int num65 = Projectile.NewProjectile(NPC.GetSource_FromThis(), npc.Center.X+Main.rand.Next(-500,500), npc.Center.Y+Main.rand.Next(-500,500), 0, 0, "Dark Explosion", 70, 0f, Main.myPlayer);
                    //	}

                    if (Main.rand.NextBool(40))
                    {
                        float num48 = 7f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.EnemyBioSpitBall>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, bioSpitDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 150;
                            Main.projectile[num54].aiStyle = 1;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);

                            //customAi1 = 1f;
                        }
                        NPC.netUpdate = true;
                    }


                }


            }
            //}
            //if (npc.justHit)
            //{
            //   npc.ai[2] = 0f;
            //}
            if (NPC.ai[2] >= 0f)
            {
                int num258 = 16;
                bool flag26 = false;
                bool flag27 = false;
                if (NPC.position.X > NPC.ai[0] - (float)num258 && NPC.position.X < NPC.ai[0] + (float)num258)
                {
                    flag26 = true;
                }
                else
                {
                    if ((NPC.velocity.X < 0f && NPC.direction > 0) || (NPC.velocity.X > 0f && NPC.direction < 0))
                    {
                        flag26 = true;
                    }
                }
                num258 += 24;
                if (NPC.position.Y > NPC.ai[1] - (float)num258 && NPC.position.Y < NPC.ai[1] + (float)num258)
                {
                    flag27 = true;
                }
                if (flag26 && flag27)
                {
                    NPC.ai[2] += 1f;
                    if (NPC.ai[2] >= 60f)
                    {
                        NPC.ai[2] = -200f;
                        NPC.direction *= -1;
                        NPC.velocity.X = NPC.velocity.X * -0.2f; //was -1f
                        NPC.collideX = false;
                    }
                }
                else
                {
                    NPC.ai[0] = NPC.position.X;
                    NPC.ai[1] = NPC.position.Y; //added -60
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
            int num259 = (int)((NPC.position.X + (float)(NPC.width / 2)) / 16f) + NPC.direction * 2;
            int num260 = (int)(((NPC.position.Y - 30) + (float)NPC.height) / 16f);
            if (NPC.position.Y > Main.player[NPC.target].position.Y)
            {
                //npc.velocity.Y += .1f;
                //if (npc.velocity.Y > +2)
                //{
                //	npc.velocity.Y = -2;
                //}

                NPC.velocity.Y -= 0.05f;
                if (NPC.velocity.Y < -1)
                {
                    NPC.velocity.Y = -1;
                }


            }
            if (NPC.position.Y < Main.player[NPC.target].position.Y)
            {
                NPC.velocity.Y += 0.05f;
                if (NPC.velocity.Y > 1)
                {
                    NPC.velocity.Y = 1;
                }


            }
            if (NPC.collideX)
            {
                NPC.velocity.X = NPC.oldVelocity.X * -0.1f;
                if (NPC.direction == -1 && NPC.velocity.X > 0f && NPC.velocity.X < 1f)
                {
                    NPC.velocity.X = 0.3f; //was 1f
                }
                if (NPC.direction == 1 && NPC.velocity.X < 0f && NPC.velocity.X > -1f)
                {
                    NPC.velocity.X = -0.3f; //was -1f
                }
            }
            if (NPC.collideY)
            {
                NPC.velocity.Y = NPC.oldVelocity.Y * -0.25f;
                if (NPC.velocity.Y > 0f && NPC.velocity.Y < 1f)
                {
                    NPC.velocity.Y = 1f;
                }
                if (NPC.velocity.Y < 0f && NPC.velocity.Y > -1f)
                {
                    NPC.velocity.Y = -1f;
                }
            }
            float num270 = 2.5f;
            if (NPC.direction == -1 && NPC.velocity.X > -num270)
            {
                NPC.velocity.X = NPC.velocity.X - 0.1f;
                if (NPC.velocity.X > num270)
                {
                    NPC.velocity.X = NPC.velocity.X - 0.1f;
                }
                else
                {
                    if (NPC.velocity.X > 0f)
                    {
                        NPC.velocity.X = NPC.velocity.X + 0.02f;
                    }
                }
                if (NPC.velocity.X < -num270)
                {
                    NPC.velocity.X = -num270;
                }
            }
            else
            {
                if (NPC.direction == 1 && NPC.velocity.X < num270)
                {
                    NPC.velocity.X = NPC.velocity.X + 0.1f;
                    if (NPC.velocity.X < -num270)
                    {
                        NPC.velocity.X = NPC.velocity.X + 0.1f;
                    }
                    else
                    {
                        if (NPC.velocity.X < 0f)
                        {
                            NPC.velocity.X = NPC.velocity.X - 0.02f;
                        }
                    }
                    if (NPC.velocity.X > num270)
                    {
                        NPC.velocity.X = num270;
                    }
                }
            }
            if (NPC.directionY == -1 && (double)NPC.velocity.Y > -2.5)
            {
                NPC.velocity.Y = NPC.velocity.Y - 0.04f;
                if ((double)NPC.velocity.Y > 2.5)
                {
                    NPC.velocity.Y = NPC.velocity.Y - 0.05f;
                }
                else
                {
                    if (NPC.velocity.Y > 0f)
                    {
                        NPC.velocity.Y = NPC.velocity.Y + 0.03f;
                    }
                }
                if ((double)NPC.velocity.Y < -2.5)
                {
                    NPC.velocity.Y = -2.5f;
                }
            }
            else
            {
                if (NPC.directionY == 1 && (double)NPC.velocity.Y < 2.5)
                {
                    NPC.velocity.Y = NPC.velocity.Y + 0.04f;
                    if ((double)NPC.velocity.Y < -2.5)
                    {
                        NPC.velocity.Y = NPC.velocity.Y + 0.05f;
                    }
                    else
                    {
                        if (NPC.velocity.Y < 0f)
                        {
                            NPC.velocity.Y = NPC.velocity.Y - 0.03f;
                        }
                    }
                    if ((double)NPC.velocity.Y > 2.5)
                    {
                        NPC.velocity.Y = 2.5f;
                    }
                }
            }
            return;
        }
        #endregion
        #region Debuffs
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {

            if (Main.rand.NextBool(2))
            {
                target.AddBuff(ModContent.BuffType<CurseBuildup>(), 600 * 60, false); //-20 HP curse
            }

            if (Main.rand.NextBool(4))
            {

                target.AddBuff(ModContent.BuffType<FracturingArmor>(), 60 * 60, false); //armor reduced on hit
                target.AddBuff(ModContent.BuffType<Frostbite>(), 5 * 60, false); //chilled

            }

            //if (Main.rand.NextBool(10) && player.statLifeMax > 20) 

            //{

            //			UsefulFunctions.BroadcastText("You have been cursed!");
            //	player.statLifeMax -= 20;
            //}
        }
        #endregion
        //-------------------------------------------------------------------
        #region Glowing Eye Effect
        //public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        //{
        //broken for now
        //			int spriteWidth=npc.frame.Width; //use same number as ini Main.npcFrameCount[npc.type]
        //			int spriteHeight = Main.npcTexture[Config.npcDefs.byName[npc.name].type].Height / Main.npcFrameCount[npc.type];

        //			int spritePosDifX = (int)(npc.frame.Width / 2);
        //			int spritePosDifY = npc.frame.Height; // was npc.frame.Height - 4;

        //			int frame = npc.frame.Y / spriteHeight;

        //			int offsetX = (int)(npc.position.X + (npc.width / 2) - Main.screenPosition.X - spritePosDifX + 0.5f);
        //			int offsetY = (int)(npc.position.Y + npc.height - Main.screenPosition.Y - spritePosDifY);

        //			SpriteEffects flop = SpriteEffects.None;
        //			if(npc.spriteDirection == 1){
        //				flop = SpriteEffects.FlipHorizontally;
        //			}


        //				//Glowing Eye Effect
        //				for(int i=0;i>-1;i--)
        //				{
        //						//draw 3 levels of trail
        //						int alphaVal=255-(0*i);
        //						Color modifiedColour = new Color((int)(alphaVal),(int)(alphaVal),(int)(alphaVal),alphaVal);
        //						spriteBatch.Draw(Main.goreTexture[Config.goreID["Oolacile Demon Glow"]],
        //						new Rectangle((int)(offsetX), (int)(offsetY), spriteWidth, spriteHeight),
        //						new Rectangle(0,npc.frame.Height * frame, spriteWidth, spriteHeight),
        //						modifiedColour,
        //						0,  //Just add this here I think was 0
        //						new Vector2(0,0),
        //						flop,
        //						0);  


        //				}	
        //}
        #endregion
        //-------------------------------------------------------------------
        #region gore
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gaibon Gore 1").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gaibon Gore 2").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gaibon Gore 3").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gaibon Gore 4").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gaibon Gore 2").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gaibon Gore 3").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gaibon Gore 4").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Blood Splat").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Blood Splat").Type, 0.9f);
            }
        }
        #endregion

        public override void ModifyNPCLoot(NPCLoot npcLoot) 
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.RedTitanite>(), 1, 1, 2));
        }
    }
}