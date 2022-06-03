using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

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
            NPC.npcSlots = 5;
            Main.npcFrameCount[NPC.type] = 16;
            AnimationType = 28;
            NPC.aiStyle = 3;
            NPC.height = 40;
            NPC.width = 20;
            NPC.damage = 149;
            NPC.defense = 41;
            NPC.lifeMax = 5000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 15110;
            NPC.knockBackResist = 0.06f;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.lavaImmune = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.RedKnightofArtoriasBanner>();
            despawnHandler = new NPCDespawnHandler("The Red Knight has slain you...", Color.Red, DustID.RedTorch);

            if (!Main.hardMode)
            {
                //npc.defense = 14;
                //npc.value = 3500;
                //npc.damage = 40;
                redKnightsSpearDamage = 28;
                redMagicDamage = 25;
                NPC.boss = true;
            }
        }

        NPCDespawnHandler despawnHandler;

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
            redKnightsSpearDamage = (int)(redKnightsSpearDamage / 2);
            redMagicDamage = (int)(redMagicDamage / 2);
        }


        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player;

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
            tsorcRevampAIs.RedKnightOnHit(NPC, true);
        }

        public override void OnHitByProjectile(Projectile projectile, int damage, float knockback, bool crit)
        {
            tsorcRevampAIs.RedKnightOnHit(NPC, projectile.DamageType == DamageClass.Melee);
        }

        public Player player
        {
            get => Main.player[NPC.target];
        }

        public override void AI()
        {
            if (!Main.hardMode)
            {
                despawnHandler.TargetAndDespawn(NPC.whoAmI);
            }
            tsorcRevampAIs.FighterAI(NPC, 2, 0.05f, 0.2f, true, 10, false, null, 1000, 0.5f, 4, true);


            if (Main.netMode != 1 && !Main.player[NPC.target].dead)
            {
                NPC.localAI[1]++;

                //play creature sounds
                if (Main.rand.Next(1500) == 1)
                {

                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/ominous-creature2") with { Volume = 0.5f }, NPC.Center);
                    //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 69, 0.6f, 0.0f); //earth staff rough fireish
                }


                //CHANCE TO JUMP FORWARDS
                if (NPC.Distance(player.Center) > 60 && NPC.velocity.Y == 0f && Main.rand.Next(500) == 1 && NPC.localAI[1] <= 166f)
                {
                    int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.Red, 1f);
                    Main.dust[dust].noGravity = true;
                    NPC.velocity.Y = -6f; //9             
                    NPC.TargetClosest(true);
                    NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * 2f;  //was 2  accellerate fwd; can happen midair
                    if ((float)NPC.direction * NPC.velocity.X > 2)
                        NPC.velocity.X = (float)NPC.direction * 2;  //  but cap at top speed
                    NPC.netUpdate = true;
                }

                //CHANCE TO DASH STEP FORWARDS 
                if (NPC.Distance(player.Center) > 100 && NPC.velocity.Y == 0f && Main.rand.Next(300) == 1 && NPC.localAI[1] <= 166f)
                {
                    int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.Red, 1f);
                    Main.dust[dust].noGravity = true;
                    //npc.direction *= -1;

                    //npc.ai[0] = 0f;
                    NPC.velocity.Y = -4f;
                    //npc.TargetClosest(true);
                    NPC.velocity.X = NPC.velocity.X * 4f; // burst forward

                    if ((float)NPC.direction * NPC.velocity.X > 4)
                        NPC.velocity.X = (float)NPC.direction * 4;  //  but cap at top speed
                                                                    //npc.localAI[1] = 160f;


                    //CHANCE TO JUMP AFTER DASH
                    if (Main.rand.Next(14) == 1 && NPC.localAI[1] <= 166f)
                    {



                        //npc.ai[0] = 0f;
                        Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 0.5f);
                        if (Main.rand.Next(3) == 1)
                        {
                            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.PinkTorch, NPC.velocity.X, NPC.velocity.Y);
                            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.PinkTorch, NPC.velocity.X, NPC.velocity.Y);
                        }
                        NPC.velocity.Y = -7f;


                        NPC.localAI[1] = 170f;

                    }

                    NPC.netUpdate = true;
                }


                //OFFENSIVE JUMP
                if (NPC.localAI[1] == 165 && NPC.velocity.Y <= 0f && Main.rand.Next(5) == 1) // && npc.localAI[1] <= 161 && npc.Distance(player.Center) > 80; 5 was 10
                {
                    Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 0.5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                    if (Main.rand.Next(3) == 1)
                    {
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.TeleportationPotion, NPC.velocity.X, NPC.velocity.Y);
                    }
                    NPC.velocity.Y = -10f; //9             
                    NPC.TargetClosest(true);
                    //npc.localAI[1] = 165;
                    NPC.netUpdate = true;
                }

                //SPEAR ATTACK
                if (NPC.localAI[1] == 180f) // 
                {
                    NPC.TargetClosest(true);
                    //if (Collision.CanHitLine(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1))
                    //{
                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 11, fallback: true); //0.4f, true, true																								
                    //speed += Main.player[npc.target].velocity;
                    speed += Main.rand.NextVector2Circular(-4, -2);

                    //if (((speed.X < 0f) && (npc.velocity.X < 0f)) || ((speed.X > 0f) && (npc.velocity.X > 0f)))
                    //{
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.RedKnightsSpear>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.1f }, NPC.Center); //Play swing-throw sound

                    //go to poison attack
                    NPC.localAI[1] = 200f;

                    //or chance to reset
                    if (Main.rand.Next(3) == 1)
                    {

                        NPC.localAI[1] = 1f;

                    }


                    //}


                }


                //POISON ATTACK DUST TELEGRAPH
                if (NPC.localAI[1] >= 250) //was 180; && npc.Distance(player.Center) > 160
                {

                    Lighting.AddLight(NPC.Center, Color.Yellow.ToVector3() * 1f);
                    if (Main.rand.Next(2) == 1 && NPC.Distance(player.Center) > 1)
                    {
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Teleporter, NPC.velocity.X, NPC.velocity.Y);
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Teleporter, NPC.velocity.X, NPC.velocity.Y);
                    }

                    //POISON ATTACK
                    if (NPC.localAI[1] >= 320)//&& npc.Distance(player.Center) > 160
                    {
                        NPC.TargetClosest(true);
                        //if (Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height) && Vector2.Distance(npc.Center, Main.player[npc.target].Center) <= 500)
                        if (Collision.CanHitLine(NPC.Center, 1, 1, Main.player[NPC.target].Center, 1, 1))
                        {
                            //Vector2 speed2 = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 11);
                            Vector2 speed2 = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 11, 1.1f, highAngle: true, fallback: true); //0.4f, true, true																								
                            speed2 += Main.player[NPC.target].velocity / 4;

                            if (((speed2.X < 0f) && (NPC.velocity.X < 0f)) || ((speed2.X > 0f) && (NPC.velocity.X > 0f)))
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed2.X, speed2.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), redMagicDamage, 0f, Main.myPlayer);


                                //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 42, 0.6f, 0f); //flaming wood, high pitched air going out
                                //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 43, 0.6f, 0f); //staff magic cast, low sound
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.6f, Pitch = 0.7f }, NPC.Center); //inferno fork, almost same as fire (works)
                                                                                                                                       //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 48, 0.6f, 0.7f); // mine snow, tick sound
                                                                                                                                       //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 60, 0.6f, 0.0f); //terra beam
                                                                                                                                       //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 69, 0.6f, 0.0f); //earth staff rough fireish

                                //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 81, 0.6f, 0f); //spawn slime mount, more like thunder flame burn
                                //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 88, 0.6f, 0f); //meteor staff more bass and fire
                                //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 100, 0.6f, 0f); // cursed flame wall, lasts a bit longer than flame
                                //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 101, 0.6f, 0f); // crystal vilethorn - breaking crystal
                                //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 103, 0.6f, 0f); //shadowflame hex (little beasty)
                                //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 104, 0.6f, 0f); //shadowflame 
                                //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 106, 0.6f, 0f); //flask throw tink sound

                                //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 109, 0.6f, 0.0f); //crystal serpent fire
                                //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 110, 0.6f, 0.0f); //crystal serpent split, paper, thud, faint high squeel

                                //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 125, 0.3f, .2f); //phantasmal bolt fire 2
                                NPC.localAI[1] = 1f;
                            }
                        }

                    }
                }


                //FIRE ATTACK FROM THE AIR
                if (NPC.localAI[1] <= 100 && NPC.Distance(player.Center) > 60)
                {

                    if (Main.rand.Next(500) == 0) //30 was cool for great red knight
                    {
                        //FIRE
                        for (int pcy = 0; pcy < 2; pcy++)
                        {


                            //Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //ORIGINAL
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)player.position.X - 10 + Main.rand.Next(10), (float)player.position.Y - 400f, (float)(-10 + Main.rand.Next(10)) / 10, 4.1f, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), redMagicDamage, 2f, Main.myPlayer); //Hellwing 12 was 2, was 8.9f near 10, not sure what / 10, does   

                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.01f }, NPC.Center);

                            NPC.netUpdate = true;
                        }

                    }
                }

                /*ULTIMATE DEATH ATTACK - BLANKET OF FIRE ABOVE PLAYER THAT CURSES
            Player player = Main.player[npc.target];
            if (npc.Distance(player.Center) > 20 && Main.rand.Next(3) == 0)
            {
                Player nT = Main.player[npc.target];
                if (Main.rand.Next(8) == 0)
                {
                    UsefulFunctions.BroadcastText("Death!", 175, 75, 255);
                }

                for (int pcy = 0; pcy < 3; pcy++)
                {
                    //Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //ORIGINAL
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 7.1f, ModContent.ProjectileType<Projectiles.Enemy.CursedDragonsBreath>(), redMagicDamage, 2f, Main.myPlayer); //was 8.9f near 10, not sure what / 10, does
                    Terraria.Audio.SoundEngine.PlaySound(2, -1, -1, 5);
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
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), npc.Center.X, npc.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.RedKnightsSpear>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);

                        npc.localAI[1] = 185f;
                    }
                }
                #endregion

                }
                */










            }




        }

        #region Gore
        public override void OnKill()
        {
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Red Knight Gore 1").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Red Knight Gore 2").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Red Knight Gore 3").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Red Knight Gore 2").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Red Knight Gore 3").Type, 1f);

            if (Main.hardMode && Main.rand.Next(99) < 5) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Melee.ForgottenPearlSpear>(), 1);
            if (Main.rand.Next(99) < 10) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.GreaterHealingPotion, 1);
            if (Main.rand.Next(99) < 10) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.IronskinPotion, 1);
            if (Main.rand.Next(99) < 10) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.RegenerationPotion, 1);


            if (tsorcRevampWorld.SuperHardMode)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.RedTitanite>(), 1 + Main.rand.Next(1));
                if (Main.rand.Next(99) < 5) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.PurgingStone>(), 1);
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
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (spearTexture == null)
            {
                spearTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/RedKnightsSpear");
            }
            if (NPC.localAI[1] >= 120 && NPC.localAI[1] <= 180f)
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.Red, 1f);
                Main.dust[dust].noGravity = true;

                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                if (NPC.spriteDirection == -1)
                {
                    spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, -MathHelper.PiOver2, new Vector2(8, 38), NPC.scale, effects, 0); // facing left (8, 38 work)
                }
                else
                {
                    spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, MathHelper.PiOver2, new Vector2(8, 38), NPC.scale, effects, 0); // facing right, first value is height, higher number is higher
                }
            }
        }
    }
}
