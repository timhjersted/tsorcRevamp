using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Accessories.Defensive.Rings;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Shortswords;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    [AutoloadBossHead]
    class Artorias : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 15;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Ichor] = true;
        }
        public override void SetDefaults()
        {
            NPC.knockBackResist = 0;
            NPC.damage = 84;
            NPC.defense = 25;
            NPC.height = 40;
            NPC.width = 30;
            NPC.lifeMax = 75000;
            NPC.scale = 1f;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.value = 700000;
            NPC.boss = true;
            NPC.lavaImmune = true;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.Artorias.DespawnHandler"), Color.Gold, DustID.GoldFlame);
        }

        public int poisonStrikeDamage = 60;
        public int redKnightsSpearDamage = 76;
        public int redMagicDamage = 64;
        public int burningSphereDamage = 231;

        int darkBeadDamage = 72;


        public int blackBreathDamage = 108;
        public int phantomSeekerDamage = 112;

        //This attack does damage equal to 25% of your max health no matter what, so its damage stat is irrelevant and only listed for readability.
        public int gravityBallDamage = 0;

        bool defenseBroken = false;

        public float DarkBeadShotTimer;
        public float DarkBeadShotCounter;
        public float poisonTimer = 0;
        public float poisonTimer2 = 0;

        //chaos
        int holdTimer = 0;

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
            poisonStrikeDamage = (int)(poisonStrikeDamage * tsorcRevampWorld.SHMScale);
            redKnightsSpearDamage = (int)(redKnightsSpearDamage * tsorcRevampWorld.SHMScale);
            redMagicDamage = (int)(redMagicDamage * tsorcRevampWorld.SHMScale);
            darkBeadDamage = (int)(darkBeadDamage * tsorcRevampWorld.SHMScale);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(ModContent.BuffType<FracturingArmor>(), 300 * 60, false);
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.BrokenArmor, 3 * 60, false);
                target.AddBuff(BuffID.Poisoned, 60 * 60, false);
                target.AddBuff(ModContent.BuffType<CurseBuildup>(), 300 * 60, false);
            }
        }
        float customAi1;

        float customspawn2;
        NPCDespawnHandler despawnHandler;


        //PROJECTILE HIT LOGIC
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            if (NPC.justHit && Main.rand.NextBool(12))
            {
                tsorcRevampAIs.TeleportImmediately(NPC, 20, true);
                poisonTimer = 1f;
                DarkBeadShotCounter = 0;
                DarkBeadShotTimer = 0;
            }
            if (NPC.justHit && NPC.Distance(player.Center) < 350 && Main.rand.NextBool(7))//
            {
                NPC.velocity.Y = Main.rand.NextFloat(-5f, -3f); //was 6 and 3
                float v = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(-9f, -6f);
                NPC.velocity.X = v;
                DarkBeadShotCounter = 0;
                DarkBeadShotTimer = 0;
                NPC.netUpdate = true;
            }

        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextBool(8))
            {

                NPC.velocity.Y = Main.rand.NextFloat(-9f, -3f);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(2f, 4f);
                poisonTimer = 1f;
                DarkBeadShotCounter = 0;
                DarkBeadShotTimer = 0;
                NPC.netUpdate = true;

            }

            if (NPC.justHit && Main.rand.NextBool(25))
            {
                tsorcRevampAIs.TeleportImmediately(NPC, 20, true);
                poisonTimer = 30f;
            }
        }
        //public static Texture2D spearTexture;
        public static Texture2D texture;
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(NPC.ModNPC.Texture);
            }
            if (!defenseBroken)
            {
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                ArmorShaderData data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.LivingOceanDye), Main.LocalPlayer);
                data.Apply(null);
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                Vector2 origin = NPC.frame.Size() / 2f;
                Vector2 offset = new Vector2(16, 20);
                spriteBatch.Draw(texture, NPC.position - Main.screenPosition + offset, NPC.frame, Color.White, NPC.rotation, origin, 1.1f, effects, 0f);
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }
        }

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 1.1f, canTeleport: true, enragePercent: 0.3f, enrageTopSpeed: 1.6f);

            Player player = Main.player[NPC.target];

            //announce magical barrier warning once
            if (holdTimer > 1)
            {
                holdTimer--;
            }
            //proximity debuff and warning
            if (Vector2.Distance(NPC.Center, Main.player[NPC.target].Center) < 1200)
            {

                player.AddBuff(ModContent.BuffType<TornWings>(), 60, false);

                if (holdTimer <= 0)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Artorias.Protection"), 175, 75, 255);
                    holdTimer = 12000;
                }

            }

            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            if (NPC.HasBuff(ModContent.BuffType<Buffs.DispelShadow>()))
            {
                defenseBroken = true;
            }


            int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 32, NPC.velocity.X - 3f, NPC.velocity.Y, 150, Color.Yellow, 1f);
            Main.dust[dust].noGravity = true;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {

                //DARK BEAD ATTACK
                DarkBeadShotTimer++;

                //Counts up each tick. Used to space out shots
                if (DarkBeadShotTimer <= 81)
                {
                    Lighting.AddLight(NPC.Center, Color.WhiteSmoke.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                    if (Main.rand.NextBool(2))
                    {
                        int pink2 = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CrystalSerpent, NPC.velocity.X, NPC.velocity.Y, Scale: 1.5f);

                        Main.dust[pink2].noGravity = true;
                    }

                }
                if (DarkBeadShotTimer == 55)
                {
                    if (NPC.Distance(player.Center) >= 221)
                    {
                        NPC.velocity.Y = Main.rand.NextFloat(-12f, -3f); //was 6 and 3 && NPC.velocity.Y == 0
                        float v = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(-9f, 9f);
                        NPC.velocity.X = v;

                    }
                    if (NPC.Distance(player.Center) <= 220)
                    {
                        NPC.velocity.Y = Main.rand.NextFloat(-10f, -3f); //was 6 and 3 && NPC.velocity.Y == 0
                        float v = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(-16f, -11f);
                        NPC.velocity.X = v;
                    }
                }

                if (DarkBeadShotTimer >= 82 && DarkBeadShotCounter < 2)
                {
                    poisonTimer = 1f;
                    Vector2 projVelocity = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 7f);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVelocity.X, projVelocity.Y, ModContent.ProjectileType<Projectiles.Enemy.ArtoriasDarkBead>(), darkBeadDamage, 0f, Main.myPlayer);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item80 with { Volume = 0.4f, Pitch = 0.1f }, NPC.Center); //acid flame

                    if (DarkBeadShotCounter <= 1)
                    {
                        DarkBeadShotTimer = 72;
                    }

                    DarkBeadShotCounter++;

                }


                //DEBUFFS
                if (NPC.Distance(player.Center) < 600)
                {
                    player.AddBuff(ModContent.BuffType<TornWings>(), 60, false);
                    player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 60, false);
                }


                poisonTimer++; ;


                //TELEGRAPH DUSTS
                if (poisonTimer >= 150 && poisonTimer <= 179)
                {
                    Lighting.AddLight(NPC.Center, Color.YellowGreen.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                    if (Main.rand.NextBool(2))
                    {
                        int dust3 = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.Red, 0.5f);
                        Main.dust[dust3].noGravity = true;
                    }
                }


                //POISON FROM ABOVE ATTACK
                if (poisonTimer <= 100 && NPC.Distance(player.Center) > 250)
                {

                    if (Main.rand.NextBool(130)) //30 was cool for great red knight
                    {
                        //POISON
                        for (int pcy = 0; pcy < 5; pcy++)
                        {
                            //Player nT = Main.player[npc.target];

                            //Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //ORIGINAL
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)player.position.X - 10 + Main.rand.Next(10), (float)player.position.Y - 300f, (float)(-10 + Main.rand.Next(10)) / 10, 2.1f, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), redMagicDamage, 2f, Main.myPlayer); //Hellwing 12 was 2, was 8.9f near 10, not sure what / 10, does   
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.01f }); //fire

                            //int FireAttack = Projectile... 
                            //Main.projectile[FireAttack].timeLeft = 15296;
                            //DarkBeadShotCounter = 0;
                            NPC.netUpdate = true;
                        }

                    }
                }


                //FIRE ATTACK 2

                if (poisonTimer <= 100)
                {
                    Player nT = Main.player[NPC.target];
                    if (Main.rand.NextBool(330)) //30 was cool for great red knight
                    {
                        //FIRE
                        for (int pcy = 0; pcy < 2; pcy++)
                        {
                            int spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X - 100 + Main.rand.Next(200), (int)NPC.Center.Y - 500, NPCID.BurningSphere, 0);
                            Main.npc[spawned].damage = burningSphereDamage;
                            Main.npc[spawned].velocity += Main.player[NPC.target].velocity;
                            Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/GaibonSpit2") with { Volume = 0.4f }, NPC.Center);

                            if (Main.netMode == NetmodeID.Server)
                            {
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawned, 0f, 0f, 0f, 0);
                            }
                        }


                        /*//HELLWING ATMOSPHERE BUT DOES NO DAMAGE YET
                        for (int pcy = 0; pcy < 2; pcy++)
                        {
                            //Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //ORIGINAL
                            int FireAttack = Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 300f, (float)(-50 + Main.rand.Next(100)) / 10, 5.1f, ProjectileID.Hellwing, redMagicDamage, 12f, Main.myPlayer); //Hellwing 12 was 2, was 8.9f near 10, not sure what / 10, does   ModContent.ProjectileType<Projectiles.Enemy.DragonsBreath>()
                            Terraria.Audio.SoundEngine.PlaySound(2, -1, -1, 5);
                            //Main.projectile[FireAttack].timeLeft = 15296;
                            npc.netUpdate = true;
                        }
                        */
                    }

                    if (Main.rand.NextBool(220))
                    {
                        for (int pcy = 0; pcy < 3; pcy++)
                        {
                            int spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X - 600 + Main.rand.Next(600), (int)NPC.Center.Y - 500, NPCID.BurningSphere, 0);
                            Main.npc[spawned].damage = burningSphereDamage;
                            Main.npc[spawned].velocity += Main.player[NPC.target].velocity;
                            Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/GaibonSpit2") with { Volume = 0.4f }, NPC.Center);

                            if (Main.netMode == NetmodeID.Server)
                            {
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawned, 0f, 0f, 0f, 0);
                            }
                        }
                    }

                    if (Main.rand.NextBool(450))
                    {
                        for (int pcy = 0; pcy < 7; pcy++)
                        {
                            int spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X - 500 + Main.rand.Next(500), (int)NPC.Center.Y - 700, NPCID.BurningSphere, 0);
                            Main.npc[spawned].damage = burningSphereDamage;
                            Main.npc[spawned].velocity += Main.player[NPC.target].velocity;
                            Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/GaibonSpit2") with { Volume = 0.4f }, NPC.Center);

                            if (Main.netMode == NetmodeID.Server)
                            {
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawned, 0f, 0f, 0f, 0);
                            }
                        }
                    }
                }

                /*ULTIMATE DEATH ATTACK - BLANKET OF FIRE ABOVE PLAYER THAT CURSES
                  Player player = Main.player[npc.target];
                  if (npc.Distance(player.Center) > 20 && Main.rand.NextBool(3))
                  {
                      Player nT = Main.player[npc.target];
                      if (Main.rand.NextBool(8))
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

                  */

                if (Main.rand.NextBool(10) && NPC.life <= NPC.lifeMax / 2)
                {

                    //ULTIMATE DEATH ATTACK - BLANKET OF FIRE ABOVE PLAYER THAT CURSES
                    //Player player = Main.player[npc.target];
                    if (NPC.Distance(player.Center) > 200 && Main.rand.NextBool(3))
                    {
                        Player nT = Main.player[NPC.target];

                        if (Main.rand.NextBool(30))
                        {
                            UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Artorias.Open"), 75, 75, 255);
                        }
                        for (int pcy = 0; pcy < 3; pcy++)
                        {
                            //Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //ORIGINAL
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 540f, (float)(-50 + Main.rand.Next(100)) / 10, 7.1f, ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedBreath>(), poisonStrikeDamage, 2f, Main.myPlayer); //was 8.9f near 10, not sure what / 10, does
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.2f, Pitch = 0.01f }); //flamethrower
                            NPC.netUpdate = true;
                        }
                    }
                }






                //OFFENSIVE JUMPS
                if (poisonTimer >= 160 && poisonTimer <= 161 && NPC.Distance(player.Center) > 400)
                {
                    //CHANCE TO JUMP 
                    if (Main.rand.NextBool(20))
                    {
                        Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 0.5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                        if (Main.rand.NextBool(3))
                        {
                            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.TeleportationPotion, NPC.velocity.X, NPC.velocity.Y);

                        }
                        NPC.velocity.Y = -10f; //9             
                        NPC.TargetClosest(true);
                        DarkBeadShotCounter = 0;
                        DarkBeadShotTimer = 20;
                        NPC.netUpdate = true;

                    }
                }
                //SPEAR ATTACK

                if (poisonTimer == 180f) //180 (without 2nd condition) and 185 created an insane attack && poisonTimer <= 181f
                {
                    NPC.TargetClosest(true);
                    /*
                    //if (Collision.CanHitLine(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1))
                    //{
                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 12); //0.4f, true, true																								
                    speed += Main.player[NPC.target].velocity;
                    //speed += Main.rand.NextVector2Circular(-4, -2);

                    if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.
                    
                    sSpear>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.6f, Pitch = 0.3f }, NPC.position); //Play swing-throw sound
                                                                                                                                //go to poison attack
                        poisonTimer = 185f;

                        if (Main.rand.NextBool(3))
                        {
                            //or chance to reset
                            DarkBeadShotCounter = 0;
                            poisonTimer = 1f;

                        }

                    }
                    */


                    int spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, NPCID.BurningSphere, 0);
                    Main.npc[spawned].damage = burningSphereDamage;
                    Main.npc[spawned].velocity += Main.player[NPC.target].velocity;
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/GaibonSpit2") with { Volume = 0.4f }, NPC.Center);
                    poisonTimer = 185f;

                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawned, 0f, 0f, 0f, 0);
                    }


                }


                //POISON ATTACK DUST TELEGRAPH
                if (poisonTimer >= 310 && NPC.life >= NPC.lifeMax / 2) //was 180
                {
                    //if(Main.rand.NextBool(60))
                    //{
                    Lighting.AddLight(NPC.Center, Color.Yellow.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                    if (Main.rand.NextBool(2) && NPC.Distance(player.Center) > 10)
                    {
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Teleporter, NPC.velocity.X, NPC.velocity.Y);
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Teleporter, NPC.velocity.X, NPC.velocity.Y);
                    }

                    //POISON ATTACK
                    if (poisonTimer >= 350 && Main.rand.NextBool(2)) //30 was cool for great red knight
                    {
                        NPC.TargetClosest(true);
                        //if (Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height) && Vector2.Distance(npc.Center, Main.player[npc.target].Center) <= 500)
                        if (Collision.CanHitLine(NPC.Center, 1, 1, Main.player[NPC.target].Center, 1, 1))
                        {
                            Vector2 speed2 = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 9); //0.4f, true, true																								
                            speed2 += Main.player[NPC.target].velocity / 2;

                            if (((speed2.X < 0f) && (NPC.velocity.X < 0f)) || ((speed2.X > 0f) && (NPC.velocity.X > 0f)))
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed2.X, speed2.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), redMagicDamage, 0f, Main.myPlayer);
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item125 with { Volume = 0.3f, Pitch = 0.0f }); //phantasmal bolt fire 2
                            }



                            if (poisonTimer >= 255)
                            {

                                if (Main.rand.NextBool(3))
                                {
                                    DarkBeadShotCounter = 0;
                                    DarkBeadShotTimer = 0;
                                }
                                poisonTimer = 1f;
                            }
                            //}
                        }
                    }

                }

                //DD2DrakinShot FINAL ATTACK
                if (poisonTimer >= 186f && NPC.life <= NPC.lifeMax / 2)
                {
                    bool clearSpace = true;
                    for (int i = 0; i < 10; i++)
                    {
                        if (UsefulFunctions.IsTileReallySolid((int)NPC.Center.X / 16, ((int)NPC.Center.Y / 16) - i))
                        {
                            clearSpace = false;
                        }
                    }

                    if (clearSpace)
                    {
                        Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 5);

                        speed.Y += Main.rand.NextFloat(-2f, -6f);
                        //speed += Main.rand.NextVector2Circular(-10, -8);
                        if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                        {
                            int lob = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.DD2DrakinShot, poisonStrikeDamage, 0f, Main.myPlayer);

                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);

                        }

                        if (poisonTimer >= 230f)
                        {
                            poisonTimer = 1f;
                        }
                    }
                }




                //old code
                customAi1 += (Main.rand.Next(2, 5) * 0.1f) * NPC.scale;
                if (customAi1 >= 10f)
                {

                    if ((customspawn2 < 27) && Main.rand.NextBool(1500))
                    {
                        int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<Enemies.LothricBlackKnight>(), 0); // Spawns Lothric Black Knight
                        Main.npc[Spawned].velocity.Y = -8;
                        Main.npc[Spawned].velocity.X = Main.rand.Next(-10, 10) / 10;
                        NPC.ai[0] = 20 - Main.rand.Next(80);
                        customspawn2 += 1f;
                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
                        }
                    }
                }


            }
        }

        /*
        if ((Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height)))
        {
            NPC.noTileCollide = false;
            NPC.noGravity = false;
        }
        if ((!Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height)))
        {
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.velocity.Y = 0f;
            if (NPC.position.Y > Main.player[NPC.target].position.Y)
            {
                NPC.velocity.Y -= 3f;
            }
            if (NPC.position.Y < Main.player[NPC.target].position.Y)
            {
                NPC.velocity.Y += 8f;
            }
         }
        
        */


        /*
         if (spearTexture == null || texture.IsDisposed)
         {
             spearTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/ArtoriasGreatsword");
         }
         if (poisonTimer >= 120 && poisonTimer <= 180f)
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
         */
        public override void SendExtraAI(BinaryWriter writer)
        {
            if (NPC.HasBuff(ModContent.BuffType<Buffs.DispelShadow>()))
            {
                defenseBroken = true;
            }
            if (defenseBroken)
            {
                writer.Write(true);
            }
            else
            {
                writer.Write(false);
            }
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            bool recievedBrokenDef = reader.ReadBoolean();
            if (recievedBrokenDef == true)
            {
                defenseBroken = true;
                NPC.defense = 0;
            }
        }

        int textCooldown;
        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (//item.type == ModContent.ItemType<Items.Weapons.Melee.Shortswords.BarrowBlade>() doesn't work since Barrow Blade only damages with its projectile now, put that into its projectile below
                item.type == ModContent.ItemType<Items.Weapons.Melee.Broadswords.ForgottenGaiaSword>())
            {
                defenseBroken = true;
            }
            if (!defenseBroken)
            {
                if (textCooldown == 0)
                {
                    //Only a fabled blade can break this shield!
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Artorias.BarrowBladeHint"));
                    textCooldown = 5;
                }
                else
                {
                    textCooldown--;
                }
                CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.Artorias.Immune"), true, false);
                modifiers.SetMaxDamage(1);
            }
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (projectile.type == ModContent.ProjectileType<BarrowBladeProjectile>())
            {
                defenseBroken = true;
            }
            if (!defenseBroken)
            {
                if (textCooldown == 0)
                {
                    //Only a fabled blade can break this shield!
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Artorias.BarrowBladeHint"));
                    textCooldown = 5;
                }
                else
                {
                    textCooldown--;
                }
                CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.Artorias.Immune"), true, false);
                modifiers.SetMaxDamage(1);
            }
        }


        public override bool CheckActive()
        {
            return false;
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.ArtoriasBag>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.AdventureModeRule, ItemID.LargeAmethyst));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<GuardianSoul>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<WolfRing>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<SoulOfArtorias>(), 1, 2, 4));
            npcLoot.Add(notExpertCondition);
        }

        #region Gore
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Easterling Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Easterling Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Easterling Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Easterling Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Easterling Gore 3").Type, 1f);
            }
        }
        #endregion

        public override void FindFrame(int currentFrame)
        {
            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
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
                    NPC.frame.Y = 0;
                    NPC.frameCounter = 0.0;
                }
                else
                {
                    NPC.frameCounter += (double)(Math.Abs(NPC.velocity.X) * .5f);
                    //npc.frameCounter += 1.0;
                    if (NPC.frameCounter > 2)
                    {
                        NPC.frame.Y = NPC.frame.Y + num;
                        NPC.frameCounter = 0;
                    }
                    if (NPC.frame.Y / num >= Main.npcFrameCount[NPC.type])
                    {
                        NPC.frame.Y = num * 1;
                    }
                }
            }
            else
            {
                NPC.frameCounter = 1.5;
                NPC.frame.Y = num;
                NPC.frame.Y = 0;
            }
        }


    }
}
