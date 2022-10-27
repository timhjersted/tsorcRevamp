using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class OolacileSorcerer : ModNPC
    {
        public override void SetDefaults()
        {

            NPC.npcSlots = 2;
            Main.npcFrameCount[NPC.type] = 3;
            AnimationType = 29;
            NPC.aiStyle = 0;
            NPC.damage = 96;
            NPC.defense = 92;
            NPC.height = 44;
            NPC.timeLeft = 22500;
            NPC.lifeMax = 12800;
            NPC.scale = 1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.lavaImmune = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.OolacileSorcererBanner>();

            NPC.value = 10500;
            NPC.width = 28;
            NPC.knockBackResist = 0.1f;

            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.Confused] = true;
        }

        int darkBeadDamage = 40;
        int darkOrbDamage = 64;
        int seekerDamage = 49;

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
            darkBeadDamage = (int)(darkBeadDamage * tsorcRevampWorld.SubtleSHMScale);
            darkOrbDamage = (int)(darkOrbDamage * tsorcRevampWorld.SubtleSHMScale);
            seekerDamage = (int)(seekerDamage * tsorcRevampWorld.SubtleSHMScale);
        }


        //float customAi1;


        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Enemies.SuperHardMode.OolacileSorcerer>()))
            {
                return 0;
            }

            Player P = spawnInfo.Player; //this shortens our code up from writing this line over and over.
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

            if (tsorcRevampWorld.SuperHardMode && Jungle && !Dungeon && !Corruption && AboveEarth && !Ocean && Main.rand.NextBool(90)) return 1;

            if (tsorcRevampWorld.SuperHardMode && Main.bloodMoon && Jungle && !Dungeon && !Corruption && !AboveEarth && !Ocean && Main.rand.NextBool(120)) return 1;

            if (tsorcRevampWorld.SuperHardMode && Dungeon && Main.rand.NextBool(30)) return 1;

            if (tsorcRevampWorld.SuperHardMode && !Main.dayTime && Dungeon && !Corruption && !AboveEarth && Main.rand.NextBool(15)) return 1;

            if (tsorcRevampWorld.SuperHardMode && Main.bloodMoon && Dungeon && Main.rand.NextBool(8)) return 1;


            return 0;
        }
        #endregion


        #region AI
        public override void AI()
        {


            #region check if standing on a solid tile
            // warning: this section contains a return statement
            if (NPC.velocity.Y == 0f) // no jump/fall
            {
                int y_below_feet = (int)(NPC.position.Y + (float)NPC.height + 8f) / 16;
                int x_left_edge = (int)NPC.position.X / 16;
                int x_right_edge = (int)(NPC.position.X + (float)NPC.width) / 16;
                for (int l = x_left_edge; l <= x_right_edge; l++) // check every block under feet
                {
                    if (Main.tile[l, y_below_feet] == null) // null tile means ??
                        return;

                    if (Main.tile[l, y_below_feet].HasTile && Main.tileSolid[(int)Main.tile[l, y_below_feet].TileType]) // tile exists and is solid
                    {
                        break; // one is enough so stop checking
                    }
                } // END traverse blocks under feet
            } // END no jump/fall
            #endregion


            NPC.netUpdate = false;
            NPC.ai[0]++; // Timer Scythe
            NPC.ai[1]++; // Timer Teleport
                         // npc.ai[2]++; // Shots

            if (NPC.life > 900)
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, NPC.velocity.X, NPC.velocity.Y, 210, Color.Black, 2f);
                Main.dust[dust].noGravity = true;
            }
            else if (NPC.life <= 900)
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, NPC.velocity.X, NPC.velocity.Y, 140, Color.Black, 3f);
                Main.dust[dust].noGravity = true;
            }

            if (Main.netMode != 2)
            {
                if (NPC.ai[0] >= 12 && NPC.ai[2] < 5) //2 was 12
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
                        int type = ModContent.ProjectileType<Projectiles.Enemy.OolacileDarkBead>();//44;//0x37; //14;
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, darkBeadDamage, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 550;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        NPC.ai[0] = 0;
                        NPC.ai[2]++;
                    }
                    NPC.netUpdate = true;
                }
            }

            if (NPC.ai[1] >= 40) //20 was 35 for longer move time
            {
                NPC.velocity.X *= 0.37f;
                NPC.velocity.Y *= 0.17f;
            }

            if ((NPC.ai[1] >= 280 && NPC.life > 300) || (NPC.ai[1] >= 120 && NPC.life <= 300)) //300 increases the time between teleports
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                for (int num36 = 0; num36 < 10; num36++)
                {
                    int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 27, NPC.velocity.X + Main.rand.Next(-10, 10), NPC.velocity.Y + Main.rand.Next(-10, 10), 200, Color.Purple, 1f);
                    Main.dust[dust].noGravity = true;
                }
                NPC.ai[3] = (float)(Main.rand.Next(360) * (Math.PI / 180));
                NPC.ai[2] = 0;
                //npc.ai[1] = 0;
                if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
                {
                    NPC.TargetClosest(true);
                }
                if (Main.player[NPC.target].dead)
                {
                    NPC.position.X = 0;
                    NPC.position.Y = 0;
                    if (NPC.timeLeft > 10)
                    {
                        NPC.timeLeft = 10;
                        return;
                    }
                }
                else
                {
                    //end of W1k's Death code
                    //region teleportation - can't believe I got this to work.. yayyyyy :D lol
                    int target_x_blockpos = (int)Main.player[NPC.target].position.X / 16; // corner not center
                    int target_y_blockpos = (int)Main.player[NPC.target].position.Y / 16; // corner not center
                    int x_blockpos = (int)NPC.position.X / 16; // corner not center
                    int y_blockpos = (int)NPC.position.Y / 16; // corner not center
                    int tp_radius = 30; // radius around target(upper left corner) in blocks to teleport into
                    int tp_counter = 0;
                    bool flag7 = false;
                    if (Math.Abs(NPC.position.X - Main.player[NPC.target].position.X) + Math.Abs(NPC.position.Y - Main.player[NPC.target].position.Y) > 9000000f)
                    { // far away from target; 4000 pixels = 250 blocks
                        tp_counter = 100;
                        flag7 = false; // always telleport was true for no teleport
                    }
                    while (!flag7) // loop always ran full 100 time before I added "flag7 = true;" below
                    {
                        if (tp_counter >= 100) // run 100 times
                            break; //return;
                        tp_counter++;

                        int tp_x_target = Main.rand.Next(target_x_blockpos - tp_radius, target_x_blockpos + tp_radius);  //  pick random tp point (centered on corner)
                        int tp_y_target = Main.rand.Next((target_y_blockpos - tp_radius) - 62, (target_y_blockpos + tp_radius) - 26);  //  pick random tp point (centered on corner)
                        for (int m = tp_y_target; m < target_y_blockpos + tp_radius; m++) // traverse y downward to edge of radius
                        { // (tp_x_target,m) is block under its feet I think
                            if ((m < target_y_blockpos - 21 || m > target_y_blockpos + 10 || tp_x_target < target_x_blockpos - 18 || tp_x_target > target_x_blockpos + 18) && (m < y_blockpos - 8 || m > y_blockpos + 8 || tp_x_target < x_blockpos - 8 || tp_x_target > x_blockpos + 8) && !Main.tile[tp_x_target, m].HasTile)
                            { // over 21 blocks distant from player & over 5 block distant from old position & tile active(to avoid surface? want to tp onto a block?)
                                bool safe_to_stand = true;
                                bool dark_caster = false; // not a fighter type AI...
                                if (dark_caster && Main.tile[tp_x_target, m - 1].WallType == 0) // Dark Caster & ?outdoors
                                    safe_to_stand = false;
                                else if (Main.tile[tp_x_target, m - 1].LiquidType == LiquidID.Lava) // feet submerged in lava
                                    safe_to_stand = false;

                                if (safe_to_stand && !Collision.SolidTiles(tp_x_target - 1, tp_x_target + 1, m - 4, m - 1))
                                { //  3x4 tile region is clear; (tp_x_target,m) is below bottom middle tile
                                  // safe_to_stand && Main.tileSolid[(int)Main.tile[tp_x_target, m].type] && // removed safe enviornment && solid below feet

                                    NPC.TargetClosest(true);
                                    NPC.position.X = (float)(tp_x_target * 16 - NPC.width / 2); // center x at target
                                    NPC.position.Y = (float)(m * 16 - NPC.height); // y so block is under feet			
                                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y - 5 + (NPC.height / 2));
                                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                                    NPC.velocity.X = (float)(Math.Cos(rotation) * 4) * -1;
                                    NPC.velocity.Y = (float)(Math.Sin(rotation) * 2) * -1;







                                    NPC.netUpdate = true;

                                    //npc.ai[3] = -120f; // -120 boredom is signal to display effects & reset boredom next tick in section "teleportation particle effects"
                                    flag7 = true; // end the loop (after testing every lower point :/)
                                    NPC.ai[1] = 0;
                                }
                            } // END over 17 blocks distant from player...
                        } // END traverse y down to edge of radius
                    } // END try 100 times


                }
            }



            //beginning of Omnir's Ultima Weapon projectile code


            NPC.ai[3]++;
            if (NPC.ai[3] >= 60) //how often the crystal attack can happen in frames per second
            {

                if (Main.rand.NextBool(40)) //1 in 2 chance boss will use attack when it flies down on top of you
                {

                    float num48 = 2f;
                    Vector2 vector9 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y - 520 + (NPC.height / 2));
                    float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector9.X) + Main.rand.Next(-20, 0x15);
                    float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector9.Y) + Main.rand.Next(-20, 0x15);
                    if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                    {
                        float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        int type = ModContent.ProjectileType<Projectiles.Enemy.OolacileDarkOrb>();//44;//0x37; //14;
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector9.X, vector9.Y, speedX, speedY, type, darkOrbDamage, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 600;
                        Main.projectile[num54].aiStyle = 4;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item25, NPC.Center);
                        NPC.ai[3] = 0;
                    }

                    NPC.netUpdate = true;


                }

                if (Main.rand.NextBool(30))
                {

                    float num48 = 8f;
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                    float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                    if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                    {
                        float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        int type = ModContent.ProjectileType<Projectiles.Enemy.OolacileSeeker>();//44;//0x37; //14;
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, seekerDamage, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 750;
                        //Main.projectile[num54].aiStyle=1;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        NPC.ai[1] = 1f;
                    }
                    NPC.netUpdate = true;
                }






                if (Main.rand.NextBool(205)) //1 in 205 chance boss will summon an NPC
                {
                    int Random = Main.rand.Next(80);
                    int Paraspawn = 0;
                    if (Random == 0) Paraspawn = NPC.NewNPC(NPC.GetSource_FromAI(), (int)Main.player[this.NPC.target].position.X - 636 - this.NPC.width / 2, (int)Main.player[this.NPC.target].position.Y - 16 - this.NPC.width / 2, ModContent.NPCType<Parasprite>(), 0);
                    Main.npc[Paraspawn].velocity.X = NPC.velocity.X;
                    NPC.active = true;
                }
            }

            NPC.ai[3] += 1; // my attempt at adding the timer that switches back to the shadow orb
            if (NPC.ai[3] >= 600)
            {
                NPC.ai[3] = 0;
                if (NPC.ai[1] == 0) NPC.ai[1] = 1;
                else NPC.ai[1] = 0;
            }
        }

        #endregion

        public override void FindFrame(int currentFrame)
        {

            if ((NPC.velocity.X > -9 && NPC.velocity.X < 9) && (NPC.velocity.Y > -9 && NPC.velocity.Y < 9))
            {
                NPC.frameCounter = 0;
                NPC.frame.Y = 0;
                if (NPC.position.X > Main.player[NPC.target].position.X)
                {
                    NPC.spriteDirection = -1;
                }
                else
                {
                    NPC.spriteDirection = 1;
                }
            }

            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            if ((NPC.velocity.X > -1 && NPC.velocity.X < 1) && (NPC.velocity.Y > -1 && NPC.velocity.Y < 1))
            {
                NPC.frameCounter = 0;
                NPC.frame.Y = 0;
            }
            else
            {
                NPC.frameCounter += 1.0;
            }
            if (NPC.frameCounter >= 1.0)
            {
                NPC.frame.Y = NPC.frame.Y + num;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type])
            {
                NPC.frame.Y = 0;
            }
        }

        #region Gore
        public override void OnKill()
        {
            if (NPC.life <= 0)
            {
                UsefulFunctions.BroadcastText("The sorcerer of Oolacile has been defeated...", 150, 150, 150);
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Oolacile Sorcerer Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Oolacile Sorcerer Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Oolacile Sorcerer Gore 3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Oolacile Sorcerer Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Oolacile Sorcerer Gore 3").Type, 1f);
                }
            }
        }
        #endregion

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Potions.HealingElixir>()));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.RedTitanite>()));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.PurgingStone>(), 30));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.CursedSoul>(), 2));
            npcLoot.Add(new Terraria.GameContent.ItemDropRules.CommonDrop(ModContent.ItemType<Items.Potions.CrimsonPotion>(), 100, 1, 1, 5));
            npcLoot.Add(new Terraria.GameContent.ItemDropRules.CommonDrop(ModContent.ItemType<Items.Potions.StrengthPotion>(), 100, 1, 1, 5));
            npcLoot.Add(new Terraria.GameContent.ItemDropRules.CommonDrop(ModContent.ItemType<Items.Potions.ShockwavePotion>(), 100, 1, 1, 5));
            npcLoot.Add(new Terraria.GameContent.ItemDropRules.CommonDrop(ModContent.ItemType<Items.Potions.BattlefrontPotion>(), 100, 1, 1, 5));
            npcLoot.Add(new Terraria.GameContent.ItemDropRules.CommonDrop(ModContent.ItemType<Items.Potions.AttractionPotion>(), 100, 1, 1, 5));
        }
    }
}