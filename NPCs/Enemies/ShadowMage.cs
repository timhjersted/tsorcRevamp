using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    class ShadowMage : ModNPC
    {
        public override void SetDefaults()
        {
            Main.npcFrameCount[NPC.type] = 2;
            AnimationType = 29;
            NPC.aiStyle = 0;
            NPC.damage = 50;
            NPC.defense = 28;
            NPC.height = 44;
            NPC.timeLeft = 22500;
            NPC.lifeMax = 900;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.lavaImmune = true;
            NPC.value = 3000;
            NPC.width = 28;
            NPC.knockBackResist = 0.3f;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.ShadowMageBanner>();

            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.Poisoned] = true;
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
            oracleDamage = (int)(oracleDamage / 2);
            ice3Damage = (int)(ice3Damage / 2);
        }

        int oracleDamage = 25;
        int ice3Damage = 35;

        //float customAi1;

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            bool underworld = spawnInfo.Player.ZoneUnderworldHeight;
            if (Main.hardMode && spawnInfo.Player.ZoneDungeon || Main.hardMode && underworld)
            {
                if (Main.rand.Next(15) == 0)
                {
                    return 1;
                }
            }
            return 0;
        }


        #region AI
        public override void AI()
        {



            #region check if standing on a solid tile
            // warning: this section contains a return statement
            //bool standing_on_solid_tile = false;
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
                        //standing_on_solid_tile = true;
                        break; // one is enough so stop checking
                    }
                } // END traverse blocks under feet
            } // END no jump/fall
            #endregion



            NPC.netUpdate = true; //was false.. wha?
            NPC.ai[0]++; // Timer Scythe
            NPC.ai[1]++; // Timer Teleport
                         // npc.ai[2]++; // Shots

            if (NPC.life > 50)
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.CursedTorch, NPC.velocity.X, NPC.velocity.Y, 200, default, 1f);
                Main.dust[dust].noGravity = true;
            }
            else if (NPC.life <= 50)
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.CursedTorch, NPC.velocity.X, NPC.velocity.Y, 140, default, 2f);
                Main.dust[dust].noGravity = true;
            }

            //if (Main.netMode != 2)
            //{
            if (NPC.ai[0] >= 12 && NPC.ai[2] < 5)
            {
                float num48 = 3f;
                Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                int type = ModContent.ProjectileType<Projectiles.Enemy.TheOracle>();
                float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, (float)((Math.Cos(rotation) * num48) * -1), (float)((Math.Sin(rotation) * num48) * -1), type, oracleDamage, 0f, Main.myPlayer);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                NPC.ai[0] = 0;
                NPC.ai[2]++;
                NPC.netUpdate = true; //new
            }

            //}

            if (NPC.ai[1] >= 60)
            {
                NPC.velocity.X *= 0.77f;
                NPC.velocity.Y *= 0.37f;
            }

            if ((NPC.ai[1] >= 290 && NPC.life > 50) || (NPC.ai[1] >= 120 && NPC.life <= 50))
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                for (int num36 = 0; num36 < 10; num36++)
                {
                    int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, NPC.velocity.X + Main.rand.Next(-10, 10), NPC.velocity.Y + Main.rand.Next(-10, 10), 200, Color.Red, 2f);
                    Main.dust[dust].noGravity = false;
                }
                NPC.ai[3] = (float)(Main.rand.Next(360) * (Math.PI / 180));
                NPC.ai[2] = 0;
                NPC.ai[1] = 0;
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
                    if (Math.Abs(NPC.position.X - Main.player[NPC.target].position.X) + Math.Abs(NPC.position.Y - Main.player[NPC.target].position.Y) > 8000f)
                    { // far away from target; 4000 pixels = 250 blocks
                        tp_counter = 100;
                        flag7 = false; // always teleport, use true for no teleport when far away
                    }
                    while (!flag7) // loop always ran full 100 time before I added "flag7 = true" below
                    {
                        if (tp_counter >= 100) // run 100 times
                            break; //return;
                        tp_counter++;

                        int tp_x_target = Main.rand.Next(target_x_blockpos - tp_radius, target_x_blockpos + tp_radius);  //  pick random tp point (centered on corner)
                        int tp_y_target = Main.rand.Next((target_y_blockpos - tp_radius) - 40, (target_y_blockpos + tp_radius) - 20);  //  pick random tp point (centered on corner)
                        for (int m = tp_y_target; m < target_y_blockpos + tp_radius; m++) // traverse y downward to edge of radius
                        { // (tp_x_target,m) is block under its feet I think
                            if ((m < target_y_blockpos - 18 || m > target_y_blockpos + 18 || tp_x_target < target_x_blockpos - 18 || tp_x_target > target_x_blockpos + 18) && (m < y_blockpos - 5 || m > y_blockpos + 5 || tp_x_target < x_blockpos - 5 || tp_x_target > x_blockpos + 5) && Main.tile[tp_x_target, m].HasTile)
                            { // over 18 blocks distant from player & over 5 block distant from old position & tile active(to avoid surface? want to tp onto a block?)
                                bool safe_to_stand = true;
                                bool dark_caster = false; // not a fighter type AI...
                                if (dark_caster && Main.tile[tp_x_target, m - 1].WallType == 0) // Dark Caster & ?outdoors
                                    safe_to_stand = false;
                                else if (Main.tile[tp_x_target, m - 1].LiquidType) // feet submerged in lava
                                    safe_to_stand = false;

                                if (safe_to_stand && Main.tileSolid[(int)Main.tile[tp_x_target, m].TileType] && !Collision.SolidTiles(tp_x_target - 1, tp_x_target + 1, m - 4, m - 1))
                                { // safe enviornment & solid below feet & 3x4 tile region is clear; (tp_x_target,m) is below bottom middle tile

                                    NPC.TargetClosest(true);
                                    NPC.position.X = (float)(tp_x_target * 16 - NPC.width / 2); // center x at target
                                    NPC.position.Y = (float)(m * 16 - NPC.height); // y so block is under feet
                                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                                    NPC.velocity.X = (float)(Math.Cos(rotation) * 12) * -1;
                                    NPC.velocity.Y = (float)(Math.Sin(rotation) * 12) * -1;


                                    // npc.position.X = (float)(tp_x_target * 16 - npc.width / 2); // center x at target
                                    // npc.position.Y = (float)(m * 16 - npc.height); // y so block is under feet
                                    NPC.netUpdate = true;

                                    //npc.ai[3] = -120f; // -120 boredom is signal to display effects & reset boredom next tick in section "teleportation particle effects"
                                    flag7 = true; // end the loop (after testing every lower point :/)
                                    NPC.ai[1] = 0;
                                }
                            } // END over 18 blocks distant from player...
                        } // END traverse y down to edge of radius
                    } // END try 100 times


                }
            }


            //end region teleportation

            //beginning of Omnir's Ultima Weapon projectile code

            NPC.ai[3]++;

            if (NPC.ai[3] >= 30) //how often the crystal attack can happen in frames per second
            {
                if (Main.rand.Next(2) == 0) //1 in 2 chance boss will use attack when it flies down on top of you
                {
                    float num48 = 4f;
                    Vector2 vector9 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y - 520 + (NPC.height / 2));
                    float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector9.X) + Main.rand.Next(-20, 0x15);
                    float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector9.Y) + Main.rand.Next(-20, 0x15);
                    if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                    {
                        float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        //int damage = 50;//(int) (14f * npc.scale);
                        int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellIce3Ball>();//44;//0x37; //14;
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector9.X, vector9.Y, speedX, speedY, type, ice3Damage, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 160;
                        Main.projectile[num54].aiStyle = 4;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item25, NPC.Center);
                        NPC.ai[3] = 0; ;
                    }

                    //npc.netUpdate = true; //new


                    if (Main.rand.Next(35) == 0) //1 in 20 chance boss will summon an NPC
                    {
                        int Random = Main.rand.Next(80);
                        int Paraspawn = 0;
                        if (Random == 0) Paraspawn = NPC.NewNPC(NPC.GetSource_FromAI(), (int)Main.player[this.NPC.target].position.X - 736 - this.NPC.width / 2, (int)Main.player[this.NPC.target].position.Y - 16 - this.NPC.width / 2, NPCID.CursedSkull, 0);
                        if (Random == 0) Paraspawn = NPC.NewNPC(NPC.GetSource_FromAI(), (int)Main.player[this.NPC.target].position.X + 700 - this.NPC.width / 2, (int)Main.player[this.NPC.target].position.Y - this.NPC.width / 2, NPCID.CursedSkull, 0);
                        if (Random == 15 && Main.hardMode) Paraspawn = NPC.NewNPC(NPC.GetSource_FromAI(), (int)Main.player[this.NPC.target].position.X - 700 - this.NPC.width / 2, (int)Main.player[this.NPC.target].position.Y - this.NPC.width / 2, NPCID.Wraith, 0);
                        if (Random == 15 && Main.hardMode) Paraspawn = NPC.NewNPC(NPC.GetSource_FromAI(), (int)Main.player[this.NPC.target].position.X + 750 - this.NPC.width / 2, (int)Main.player[this.NPC.target].position.Y - this.NPC.width / 2, NPCID.Wraith, 0);

                        Main.npc[Paraspawn].velocity.X = NPC.velocity.X;
                        NPC.active = true;

                    }

                    NPC.netUpdate = true; //new


                }
            }

            NPC.ai[3] += 1; // my attempt at adding the timer that switches back to the shadow orb
            if (NPC.ai[3] >= 600)
            {
                if (NPC.ai[1] == 0) NPC.ai[1] = 1;
                else NPC.ai[1] = 0;
            }
        }

        #endregion

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            Texture2D glowTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Enemies/ShadowMage_Glowmask");
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            if (NPC.spriteDirection == 1)
            {
                spriteBatch.Draw(glowTexture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 56, 54), lightColor, NPC.rotation, new Vector2(28, 27), NPC.scale, effects, 0f);
            }
            else
            {
                spriteBatch.Draw(glowTexture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 56, 54), lightColor, NPC.rotation, new Vector2(28, 27), NPC.scale, effects, 0f);
            }

        }
        public override void FindFrame(int frameHeight)
        {

            if ((NPC.velocity.X != 0 || NPC.velocity.Y != 0) || (NPC.ai[0] >= 12 && NPC.ai[2] < 5))
            {
                NPC.frame.Y = 1 * frameHeight;
            }
            else
            {
                NPC.frame.Y = 0 * frameHeight;
            }
        }

        #region Gore
        public override void OnKill()
        {
            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
            Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Undead Caster Gore 1").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Undead Caster Gore 2").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Undead Caster Gore 2").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Undead Caster Gore 3").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Undead Caster Gore 3").Type, 1f);

            if (Main.rand.Next(99) < 40) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.ManaRegenerationPotion, 1);
            if (Main.rand.Next(99) < 20) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.GreaterHealingPotion, 1);
        }
        #endregion
    }
}