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
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire3] = true;
        }
        public override void SetDefaults()
        {
            NPC.knockBackResist = 0;
            NPC.damage = 55;
            NPC.defense = 25;
            NPC.height = 40;
            NPC.width = 30;
            NPC.lifeMax = 90000;
            NPC.scale = 1f;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.value = 750000;
            NPC.rarity = 39;
            NPC.boss = true;
            NPC.lavaImmune = true;
            NPC.coldDamage = true;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.Artorias.DespawnHandler"), Color.Gold, DustID.GoldFlame);

            tsorcRevampGlobalNPC artoriasGlobalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            artoriasGlobalNPC.Agility = 0.1f;
        }

        public int attackPatternCounter = 0;

        public int poisonStrikeDamage = 30;
        public int redKnightsSpearDamage = 30;
        public int redMagicDamage = 34;
        public int burningSphereDamage = 36;

        int darkBeadDamage = 30;


        public int blackBreathDamage = 32;
        public int phantomSeekerDamage = 32;

        //This attack does damage equal to 25% of your max health no matter what, so its damage stat is irrelevant and only listed for readability.
        public int gravityBallDamage = 0;

        bool defenseBroken = false;

        public float DarkBeadShotTimer;
        public float DarkBeadShotCounter;
        public float poisonTimer = 0;
        public float poisonTimer2 = 0;



        // Track which thresholds have already spawned their NPCs
        private bool spawned90 = false;
        private bool spawned60 = false;
        private int spawned40 = 0; // Spawn two at 40%
        private int spawned20 = 0; // Spawn 3 at 20%


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
            if (NPC.justHit && Main.rand.NextBool(13))
            {
                tsorcRevampAIs.TeleportImmediately(NPC, 20, true);
                poisonTimer = 1f;
                DarkBeadShotCounter = 0;
                DarkBeadShotTimer = 0;
            }
            if (NPC.justHit && NPC.Distance(player.Center) < 350 && Main.rand.NextBool(12))//
            {
                NPC.velocity.Y = Main.rand.NextFloat(-8f, -5f); //was 6 and 3
                float v = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(-6f, -9f);
                NPC.velocity.X = v;
                DarkBeadShotCounter = 0;
                DarkBeadShotTimer = 0;
                NPC.netUpdate = true;
            }
            if (NPC.justHit && NPC.Distance(player.Center) > 350 && Main.rand.NextBool(8))//
            {
                NPC.velocity.Y = Main.rand.NextFloat(-8f, -5f); //was 6 and 3
                float v = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(3f, 5f);
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

                NPC.velocity.Y = Main.rand.NextFloat(-3f, -6f);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(3f, 5f);
                poisonTimer = 1f;
                DarkBeadShotCounter = 0;
                DarkBeadShotTimer = 0;
                NPC.netUpdate = true;

            }

            if (NPC.justHit && Main.rand.NextBool(20))
            {
                tsorcRevampAIs.TeleportImmediately(NPC, 20, true);
                poisonTimer = 0f;
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


        void SpawnNPC()
        {
            int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<Enemies.LothricBlackKnight>(), 0); // Spawns Lothric Black Knight
            Main.npc[Spawned].velocity.Y = -8;
            Main.npc[Spawned].velocity.X = Main.rand.Next(-10, 10) / 10;
            NPC.ai[0] = 20 - Main.rand.Next(80);
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
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


            // Spawn NPC at 90% health
            if (!spawned90 && NPC.life <= NPC.lifeMax * 0.9)
            {
                SpawnNPC();
                spawned90 = true;
            }
            // Spawn NPC at 60% health
            else if (!spawned60 && NPC.life <= NPC.lifeMax * 0.6)
            {
                SpawnNPC();
                spawned60 = true;
            }
            // Spawn two NPCs at 40% health
            else if (spawned40 < 2 && NPC.life <= NPC.lifeMax * 0.4)
            {
                SpawnNPC();
                spawned40++;
            }
            // Spawn NPC at 20% health
            else if (spawned20 < 3 && NPC.life <= NPC.lifeMax * 0.2)
            {
                SpawnNPC();
                spawned20++;
            }


            if (Main.netMode != NetmodeID.MultiplayerClient)
            {

                //DARK BEAD ATTACK
                DarkBeadShotTimer++;

                //Counts up each tick. Used to space out shots
                if (DarkBeadShotTimer <= 81)
                {
                    Lighting.AddLight(NPC.Center, Color.WhiteSmoke.ToVector3() * 3f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                    if (Main.rand.NextBool(2))
                    {
                        int pink2 = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CrystalSerpent, NPC.velocity.X, NPC.velocity.Y, Scale: 1.4f);

                        Main.dust[pink2].noGravity = true;
                    }

                }
                if (DarkBeadShotTimer == 65)
                {
                    if (NPC.Distance(player.Center) >= 221)
                    {
                        NPC.velocity.Y = Main.rand.NextFloat(-3f, -9f); //was 6 and 3 && NPC.velocity.Y == 0
                        float v = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(2f, 6f);
                        NPC.velocity.X = v;

                    }
                    if (NPC.Distance(player.Center) <= 220)
                    {
                        NPC.velocity.Y = Main.rand.NextFloat(-3f, -9f); //was 6 and 3 && NPC.velocity.Y == 0
                        float v = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(-3f, -9f);
                        NPC.velocity.X = v;
                    }
                }

                if (DarkBeadShotTimer >= 85 && DarkBeadShotCounter < 2)
                {
                    //poisonTimer = 1f;
                    Vector2 projVelocity = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 5f);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVelocity.X, projVelocity.Y, ModContent.ProjectileType<Projectiles.Enemy.ArtoriasDarkBead>(), darkBeadDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item80 with { Volume = 0.4f, Pitch = 0.1f }, NPC.Center); //acid flame

                    if (DarkBeadShotCounter <= 1)
                    {
                        DarkBeadShotTimer = 80;
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
                // SHADOW SHOT FROM ABOVE ATTACK
                if (poisonTimer == 200)
                { 
                        for (int pcy = 0; pcy < 2; pcy++)
                        {
                           

                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Vector2 projVelocity = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 0.5f);

                                // Number of projectiles in the shotgun blast
                                int numProjectiles = 3;
                                // Spread angle in degrees
                                float spreadAngle = 3f;

                                for (int i = 0; i < numProjectiles; i++)
                                {
                                    // Calculate the horizontal velocity component
                                    float horizontalVelocity = (i - (numProjectiles - 1) / 2f) * spreadAngle / (numProjectiles - 1);

                                    // Introduce a slight random variation in speed
                                    float speedVariation = 1.0f + Main.rand.NextFloat(-0.1f, 0.1f);  // Varies speed by ±10%

                                    // Calculate the final velocity
                                    Vector2 finalVelocity = new Vector2(horizontalVelocity / 10, 3.1f) * speedVariation;

                                    // Create the projectile
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(),
                                                             player.position.X - 5 + Main.rand.Next(10),
                                                             player.position.Y - 400f,
                                                             finalVelocity.X,
                                                             finalVelocity.Y,
                                                             ModContent.ProjectileType<Projectiles.Enemy.ShadowShot>(),
                                                             redMagicDamage,
                                                             2f,
                                                             Main.myPlayer);
                                }
                            }

                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.01f }); //fire

                          
                            NPC.netUpdate = true;
                        }
                        
                        if (DarkBeadShotTimer >= 86)
                        {
                            DarkBeadShotCounter = 0;
                            DarkBeadShotTimer = 0;
                        }           
                }


                // SHADOW SHOT ATTACKS x 3

                if (poisonTimer == 400)
                {
                    Player nT = Main.player[NPC.target];


                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            // Determine the attack pattern
                            switch (attackPatternCounter)
                            {
                                case 0: // Curtain Call
                                {
                                        int numProjectiles = 20;
                                        float spacing = 100f;

                                        for (int i = 0; i < numProjectiles; i++)
                                        {
                                            Vector2 position = new Vector2(player.position.X - 500 + i * spacing, player.position.Y - 400f);
                                            Vector2 velocity = UsefulFunctions.Aim(position, player.Center, 1.3f);

                                            Projectile.NewProjectile(NPC.GetSource_FromThis(),
                                                                     position.X,
                                                                     position.Y,
                                                                     velocity.X,
                                                                     velocity.Y,
                                                                     ModContent.ProjectileType<Projectiles.Enemy.ShadowShot>(),
                                                                     redMagicDamage,
                                                                     2f,
                                                                     Main.myPlayer);
                                        }
                                        break;
                                }
                                case 1: // Shotgun blast on two Sides
                                {
                                        int numProjectiles = 6;
                                        float spreadAngle = 2f;

                                        for (int side = -1; side <= 1; side += 2)
                                        {
                                            for (int i = 0; i < numProjectiles; i++)
                                            {
                                                float horizontalVelocity = (i - (numProjectiles - 1) / 2f) * spreadAngle;
                                                float speedVariation = 1.0f + Main.rand.NextFloat(-0.1f, 0.1f);
                                                Vector2 finalVelocity = new Vector2(horizontalVelocity, -1.1f) * speedVariation;

                                                Projectile.NewProjectile(NPC.GetSource_FromThis(),
                                                                            player.position.X + 200 * side,
                                                                            player.position.Y + 300f,
                                                                            finalVelocity.X,
                                                                            finalVelocity.Y,
                                                                            ModContent.ProjectileType<Projectiles.Enemy.ShadowShot>(),
                                                                            redMagicDamage,
                                                                            2f,
                                                                            Main.myPlayer);
                                            }
                                        }
                                        break;
                                }
                                case 2: // Bottom Up
                                {
                                    int numProjectiles = 25;
                                    float spacing = 100f;

                                    for (int i = 0; i < numProjectiles; i++)
                                    {
                                        Vector2 position = new Vector2(player.position.X + 400 - i * spacing, player.position.Y + 500);
                                        Vector2 velocity = UsefulFunctions.Aim(position, player.Center, 1.3f);

                                        Projectile.NewProjectile(NPC.GetSource_FromThis(),
                                                                    position.X,
                                                                    position.Y,
                                                                    velocity.X,
                                                                    velocity.Y,
                                                                    ModContent.ProjectileType<Projectiles.Enemy.ShadowShot>(),
                                                                    redMagicDamage,
                                                                    2f,
                                                                    Main.myPlayer);
                                    }
                                    break;
                                }
                            }

                            // Increment the attack pattern counter, wrapping around if necessary
                            attackPatternCounter = (attackPatternCounter + 1) % 3;
                        }


                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.01f }); //fire

                           
                            NPC.netUpdate = true;
                        
                        poisonTimer = 0f;
                    

                    
                }

               

                if (Main.rand.NextBool(10) && NPC.life <= NPC.lifeMax / 2)
                {

                    //ULTIMATE DEATH ATTACK - BLANKET OF FIRE ABOVE PLAYER THAT CURSES
                   
                    if (NPC.Distance(player.Center) > 200 && Main.rand.NextBool(4))
                    {
                        Player nT = Main.player[NPC.target];

                        if (Main.rand.NextBool(30))
                        {
                            UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Artorias.Open"), 75, 75, 255);
                        }
                        for (int pcy = 0; pcy < 3; pcy++)
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                  Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 540f, (float)(-50 + Main.rand.Next(100)) / 10, 7.1f, ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedBreath>(), poisonStrikeDamage, 2f, Main.myPlayer); //was 8.9f near 10, not sure what / 10, does
                            }
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.2f, Pitch = 0.01f }); //flamethrower
                            NPC.netUpdate = true;
                        }
                    }
                }


                //OFFENSIVE JUMPS
                if (poisonTimer == 160 && NPC.Distance(player.Center) > 200)
                {
                    //CHANCE TO JUMP 
                    if (Main.rand.NextBool(2))
                    {
                        Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 0.5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                        if (Main.rand.NextBool(3))
                        {
                            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.TeleportationPotion, NPC.velocity.X, NPC.velocity.Y);

                        }
                        NPC.velocity.Y = Main.rand.NextFloat(-3f, -6f);
                        NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(3f, 5f);
                        NPC.TargetClosest(true);
                        DarkBeadShotCounter = 0;
                        DarkBeadShotTimer = 40;
                        NPC.netUpdate = true;

                    }
                }
                

                //DD2DrakinShot FINAL ATTACK
                if (poisonTimer >= 300f && NPC.life <= NPC.lifeMax / 2)
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
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                int lob = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.DD2DrakinShot, poisonStrikeDamage, 0f, Main.myPlayer);
                            }
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);

                        }

                        if (poisonTimer >= 340f)
                        {
                            poisonTimer = 1f;
                        }
                    }
                }



            }
        }

        
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
