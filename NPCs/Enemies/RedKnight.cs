using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework.Graphics;

namespace tsorcRevamp.NPCs.Enemies
{
    class RedKnight : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Red Knight");
        }

        public int redKnightsSpearDamage = 70;
        public int redMagicDamage = 40;

        public override void SetDefaults()
        {
            npc.npcSlots = 5;
            Main.npcFrameCount[npc.type] = 16;
            animationType = 28;
            npc.aiStyle = 3;
            npc.height = 40;
            npc.width = 20;
            npc.damage = 149;
            npc.defense = 41;
            npc.lifeMax = 5000;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.value = 15110;
            npc.knockBackResist = 0.06f;
            npc.buffImmune[BuffID.Confused] = true;
            npc.lavaImmune = true;
            npc.buffImmune[BuffID.OnFire] = true;
            banner = npc.type;
            bannerItem = ModContent.ItemType<Banners.RedKnightofArtoriasBanner>();

            if (!Main.hardMode)
            {
                //npc.defense = 14;
                //npc.value = 3500;
                //npc.damage = 40;
                redKnightsSpearDamage = 28;
                redMagicDamage = 25;
                npc.boss = true;
            }
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = (int)(npc.lifeMax / 2);
            npc.damage = (int)(npc.damage / 2);
            redKnightsSpearDamage = (int)(redKnightsSpearDamage / 2);
            redMagicDamage = (int)(redMagicDamage / 2);
        }


        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.player;

            if (Main.hardMode && P.ZoneDungeon && !(P.ZoneCorrupt || P.ZoneCrimson) && Main.rand.Next(1200) == 1) return 1;

            if (Main.hardMode && P.ZoneMeteor && !(P.ZoneCorrupt || P.ZoneCrimson) && P.ZoneRockLayerHeight && Main.rand.Next(250) == 1) return 1;

            if (Main.hardMode && !Main.dayTime && P.ZoneDungeon && !(P.ZoneCorrupt || P.ZoneCrimson) && P.ZoneRockLayerHeight && Main.rand.Next(350) == 1) return 1;

            if (Main.hardMode && P.ZoneUnderworldHeight && Main.rand.Next(100) == 1) return 1;

            if (tsorcRevampWorld.SuperHardMode && P.ZoneDungeon && Main.rand.Next(5) == 1) return 1; //30 was 1 percent, 10 is 2.76%

            if (tsorcRevampWorld.SuperHardMode && P.ZoneUnderworldHeight && Main.rand.Next(3) == 1) return 1;

            return 0;
        }
        #endregion

        //PROJECTILE HIT LOGIC
        public override void OnHitByItem(Player player, Item item, int damage, float knockback, bool crit)
        {
            tsorcRevampAIs.RedKnightOnHit(npc, true);            
        }
        
        public override void OnHitByProjectile(Projectile projectile, int damage, float knockback, bool crit)
        {           
            tsorcRevampAIs.RedKnightOnHit(npc, projectile.melee);
        }

        public Player player
        {
            get => Main.player[npc.target];
        }

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(npc, 4, 0.07f, 0.2f, true, 10, false, 0, 1000, 0.5f, 6, true);

            if (Main.netMode != 1 && !Main.player[npc.target].dead) // can generalize this section to moving+projectile code 
            {
                npc.localAI[1]++;

                //CHANCE TO JUMP FORWARDS
                if (npc.Distance(player.Center) > 20 && npc.velocity.Y == 0f && Main.rand.Next(500) == 1)
                {
                    int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X - 6f, npc.velocity.Y, 150, Color.Red, 1f);
                    Main.dust[dust].noGravity = true;
                    npc.velocity.Y = -6f; //9             
                    npc.TargetClosest(true);
                    npc.velocity.X = npc.velocity.X + (float)npc.direction * 2f;  //was 2  accellerate fwd; can happen midair
                    if ((float)npc.direction * npc.velocity.X > 2)
                        npc.velocity.X = (float)npc.direction * 2;  //  but cap at top speed
                    npc.netUpdate = true;
                }

                //CHANCE TO DASH STEP FORWARDS 
                else if (npc.Distance(player.Center) > 80 && npc.velocity.Y == 0f && Main.rand.Next(300) == 1)
                {
                    int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X - 6f, npc.velocity.Y, 150, Color.Red, 1f);
                    Main.dust[dust].noGravity = true;
                    //npc.direction *= -1;

                    npc.ai[0] = 0f;
                    npc.velocity.Y = -4f;
                    //npc.TargetClosest(true);
                    npc.velocity.X = npc.velocity.X * 4f; // burst forward

                    if ((float)npc.direction * npc.velocity.X > 4)
                        npc.velocity.X = (float)npc.direction * 4;  //  but cap at top speed
                                                                    //npc.localAI[1] = 160f;


                    //CHANCE TO JUMP AFTER DASH
                    if (Main.rand.Next(14) == 1)
                    {
                        npc.TargetClosest(true);

                        //npc.spriteDirection = npc.direction;
                        npc.ai[0] = 0f;
                        Lighting.AddLight(npc.Center, Color.OrangeRed.ToVector3() * 0.5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                        if (Main.rand.Next(3) == 1)
                        {
                            Dust.NewDust(npc.position, npc.width, npc.height, DustID.PinkFlame, npc.velocity.X, npc.velocity.Y);
                            Dust.NewDust(npc.position, npc.width, npc.height, DustID.PinkFlame, npc.velocity.X, npc.velocity.Y);
                        }
                        npc.velocity.Y = -7f;


                        npc.localAI[1] = 175f;

                    }

                    npc.netUpdate = true;
                }


                //FIRE ATTACK
                if (npc.localAI[1] <= 100 && npc.Distance(player.Center) > 60)
                {

                    if (Main.rand.Next(500) == 0) //30 was cool for great red knight
                    {
                        //FIRE
                        for (int pcy = 0; pcy < 2; pcy++)
                        {
                            //Player nT = Main.player[npc.target];

                            //Projectile.NewProjectile((float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //ORIGINAL
                            Projectile.NewProjectile((float)player.position.X - 10 + Main.rand.Next(10), (float)player.position.Y - 400f, (float)(-10 + Main.rand.Next(10)) / 10, 4.1f, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), redMagicDamage, 2f, Main.myPlayer); //Hellwing 12 was 2, was 8.9f near 10, not sure what / 10, does   
                            Main.PlaySound(2, -1, -1, 5);
                            //int FireAttack = Projectile... 
                            //Main.projectile[FireAttack].timeLeft = 15296;
                            npc.netUpdate = true;
                        }

                    }
                }

                //if (Main.rand.Next(3) == 1) //30 was cool for great red knight
                //{

                //}

            }




            /*ULTIMATE DEATH ATTACK - BLANKET OF FIRE ABOVE PLAYER THAT CURSES
            Player player = Main.player[npc.target];
            if (npc.Distance(player.Center) > 20 && Main.rand.Next(3) == 0)
            {
                Player nT = Main.player[npc.target];
                if (Main.rand.Next(8) == 0)
                {
                    Main.NewText("Death!", 175, 75, 255);
                }

                for (int pcy = 0; pcy < 3; pcy++)
                {
                    //Projectile.NewProjectile((float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //ORIGINAL
                    Projectile.NewProjectile((float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 7.1f, ModContent.ProjectileType<Projectiles.Enemy.CursedDragonsBreath>(), redMagicDamage, 2f, Main.myPlayer); //was 8.9f near 10, not sure what / 10, does
                    Main.PlaySound(2, -1, -1, 5);
                    npc.netUpdate = true;
                }
            }



            //INSANE WHIP ATTACK
            if (npc.localAI[1] >= 180f) //180 (without 2nd condition) and 185 created an insane attack
            {
                npc.TargetClosest(true);
                if (Collision.CanHitLine(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1))
                {
                    Vector2 speed = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 11);

                    if (((speed.X < 0f) && (npc.velocity.X < 0f)) || ((speed.X > 0f) && (npc.velocity.X > 0f)))
                    {
                        Projectile.NewProjectile(npc.Center.X, npc.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.RedKnightsSpear>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                        Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);

                        npc.localAI[1] = 185f;
                    }
                }
                #endregion

            }
            */



            //OFFENSIVE JUMPS
            if (npc.localAI[1] >= 160 && npc.localAI[1] <= 161 && npc.Distance(player.Center) > 40)
            {
                //CHANCE TO JUMP 
                if (Main.rand.Next(10) == 1)
                {
                    Lighting.AddLight(npc.Center, Color.OrangeRed.ToVector3() * 0.5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                    if (Main.rand.Next(3) == 1)
                    {
                        Dust.NewDust(npc.position, npc.width, npc.height, DustID.TeleportationPotion, npc.velocity.X, npc.velocity.Y);

                    }
                    npc.velocity.Y = -9f; //9             
                    npc.TargetClosest(true);
                    npc.localAI[1] = 165;
                    npc.netUpdate = true;

                }
            }
            //SPEAR ATTACK
            if (npc.localAI[1] >= 180f && npc.localAI[1] <= 181f) //180 (without 2nd condition) and 185 created an insane attack
            {
                npc.TargetClosest(true);
                if (Collision.CanHitLine(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1))
                {
                    Vector2 speed = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 11);

                    if (((speed.X < 0f) && (npc.velocity.X < 0f)) || ((speed.X > 0f) && (npc.velocity.X > 0f)))
                    {
                        Projectile.NewProjectile(npc.Center.X, npc.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.RedKnightsSpear>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                        Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
                        //go to poison attack
                        npc.localAI[1] = 185f;

                        if (Main.rand.Next(3) == 1)
                        {
                            //or chance to reset
                            npc.localAI[1] = 1f;

                        }
                    }
                }

            }

            //POISON ATTACK DUST TELEGRAPH
            if (npc.localAI[1] >= 185 && npc.Distance(player.Center) > 160) //was 180
            {
                //if(Main.rand.Next(60) == 0)
                //{
                Lighting.AddLight(npc.Center, Color.Yellow.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                if (Main.rand.Next(2) == 1 && npc.Distance(player.Center) > 1)
                {
                    Dust.NewDust(npc.position, npc.width, npc.height, DustID.Teleporter, npc.velocity.X, npc.velocity.Y);
                    Dust.NewDust(npc.position, npc.width, npc.height, DustID.Teleporter, npc.velocity.X, npc.velocity.Y);
                }

                //POISON ATTACK
                if (npc.localAI[1] >= 250 && npc.Distance(player.Center) > 160) //30 was cool for great red knight
                {
                    npc.TargetClosest(true);
                    //if (Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height) && Vector2.Distance(npc.Center, Main.player[npc.target].Center) <= 500)
                    if (Collision.CanHitLine(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1))
                    {
                        Vector2 speed2 = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 11);

                        if (((speed2.X < 0f) && (npc.velocity.X < 0f)) || ((speed2.X > 0f) && (npc.velocity.X > 0f)))
                        {
                            Projectile.NewProjectile(npc.Center.X, npc.Center.Y, speed2.X, speed2.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), redMagicDamage, 0f, Main.myPlayer);
                            Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
                            npc.localAI[1] = 1f;
                        }
                    }
                }
            }
        }

        #region Gore
        public override void NPCLoot()
        {
            Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Red Knight Gore 1"), 1f);
            Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Red Knight Gore 2"), 1f);
            Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Red Knight Gore 3"), 1f);
            Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Red Knight Gore 2"), 1f);
            Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Red Knight Gore 3"), 1f);

            if (Main.hardMode && Main.rand.Next(99) < 5) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Melee.ForgottenPearlSpear>(), 1);
            if (Main.rand.Next(99) < 10) Item.NewItem(npc.getRect(), ItemID.GreaterHealingPotion, 1);
            if (Main.rand.Next(99) < 10) Item.NewItem(npc.getRect(), ItemID.IronskinPotion, 1);
            if (Main.rand.Next(99) < 10) Item.NewItem(npc.getRect(), ItemID.RegenerationPotion, 1);
            

            if (tsorcRevampWorld.SuperHardMode)
            {
                Item.NewItem(npc.getRect(), ModContent.ItemType<Items.RedTitanite>(), 1 + Main.rand.Next(1));
                if (Main.rand.Next(99) < 5) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.PurgingStone>(), 1);
            }
        }
        #endregion

        #region Debuffs
        public override void OnHitPlayer(Player player, int damage, bool crit)
        {
            player.AddBuff(BuffID.OnFire, 180, false);

            if (Main.rand.Next(5) == 0)
            {
                player.AddBuff(ModContent.BuffType<Buffs.Crippled>(), 180, false); // loss of flight mobility
                player.AddBuff(ModContent.BuffType<Buffs.GrappleMalfunction>(), 1800, false);
                player.AddBuff(BuffID.NightOwl, 30, false);

            }
        }
        #endregion

        static Texture2D spearTexture;
        public override void PostDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            if (spearTexture == null)
            {
                spearTexture = mod.GetTexture("Projectiles/Enemy/RedKnightsSpear");
            }
            if (npc.localAI[1] >= 120 && npc.localAI[1] <= 181f)
            {
                int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X - 6f, npc.velocity.Y, 150, Color.Red, 1f);
                Main.dust[dust].noGravity = true;

                SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                if (npc.spriteDirection == -1)
                {
                    spriteBatch.Draw(spearTexture, npc.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, -MathHelper.PiOver2, new Vector2(8, 38), npc.scale, effects, 0); // facing left (8, 38 work)
                }
                else
                {
                    spriteBatch.Draw(spearTexture, npc.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, MathHelper.PiOver2, new Vector2(8, 38), npc.scale, effects, 0); // facing right, first value is height, higher number is higher
                }
            }
        }
    }
}
