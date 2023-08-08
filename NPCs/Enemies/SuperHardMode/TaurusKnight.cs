using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.ItemDropRules;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class TaurusKnight : ModNPC
    {
        int breathDamage = 20;
        int tridentDamage = 25;
        int crystalFireDamage = 50;
        public int disrupterDamage = 65;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 16;
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            AnimationType = 28;
            NPC.height = 40;
            NPC.width = 20;
            NPC.damage = 73;
            NPC.defense = 50;
            NPC.timeLeft = 22000;
            NPC.lifeMax = 5000; // was 7100, toning down to increase spawn rates, now also in desert
            NPC.scale = 1.1f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 40000; // life / 1.25 bc rare : was 6200 with 7100 life
            NPC.knockBackResist = 0.01f;
            NPC.lavaImmune = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.TaurusKnightBanner>();
            UsefulFunctions.AddAttack(NPC, 136, ModContent.ProjectileType<Projectiles.Enemy.EarthTrident>(), tridentDamage, 14, SoundID.Item17);
        }
        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
            breathDamage = (int)(breathDamage * tsorcRevampWorld.SHMScale);
            tridentDamage = (int)(tridentDamage * tsorcRevampWorld.SHMScale);
            crystalFireDamage = (int)(crystalFireDamage * tsorcRevampWorld.SHMScale);
            disrupterDamage = (int)(disrupterDamage * tsorcRevampWorld.SHMScale);
        }

        int chargeDamage = 0;
        bool chargeDamageFlag = false;

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player; //this shortens our code up from writing this line over and over.
            bool Meteor = P.ZoneMeteor;
            bool Desert = (P.ZoneDesert || P.ZoneUndergroundDesert);
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

            //Ensuring it can't spawn if one already exists.
            int count = 0;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if (Main.npc[i].type == NPC.type)
                {
                    count++;
                    if (count > 0)
                    {
                        return 0;
                    }
                }
            }

            if (tsorcRevampWorld.SuperHardMode && Desert && Main.rand.NextBool(45))

            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.TaurusKnight.Nearby"), 175, 75, 255);
                return 1;
            }

            if (tsorcRevampWorld.SuperHardMode && Dungeon && Main.rand.NextBool(50))

            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.TaurusKnight.Nearby"), 175, 75, 255);
                return 1;
            }

            if (tsorcRevampWorld.SuperHardMode && InHell && Main.rand.NextBool(30))

            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.TaurusKnight.Nearby"), 175, 75, 255);
                return 1;
            }


            return 0;
        }
        #endregion

        int fireBreathTimer = 0;
        int breathTimer = 0;
        public override void AI()
        {
            NPC.aiStyle = -1;

            tsorcRevampAIs.FighterAI(NPC, 0.6f, 0.07f, 0.1f, canTeleport: true, enragePercent: 0.5f, enrageTopSpeed: 5, canDodgeroll: true, lavaJumping: true);

            Lighting.AddLight(NPC.Center, Color.Yellow.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%

            bool clearLineofSight = Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height);
            
            if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer == 125f)
            {
                if (Main.rand.NextBool(2))
                {
                    NPC.velocity.Y = -8f;
                    NPC.netUpdate = true;
                }
            }

            fireBreathTimer++;
            if (clearLineofSight)
            {
                //TELEGRAPH DUSTS
                if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer >= 117)
                {
                    if (Main.rand.NextBool(2))
                    {
                        Dust.NewDust(NPC.position, NPC.width / 2, NPC.height / 2, DustID.GoldFlame, NPC.velocity.X, NPC.velocity.Y);
                        Dust.NewDust(NPC.position, NPC.width / 2, NPC.height / 2, DustID.GoldFlame, NPC.velocity.X, NPC.velocity.Y); //CrystalPulse
                    }
                }

                if (fireBreathTimer == 200)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, NPC.velocity, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.OrangeRed));
                    }
                }

                //Fire rain and disruptor
                if (fireBreathTimer > 225)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                    if (Main.rand.NextBool())
                        for (int pcy = 0; pcy < 10; pcy++)
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), Main.player[NPC.target].position.X - 200 + Main.rand.Next(500), Main.player[NPC.target].position.Y - 400f, (float)(-50 + Main.rand.Next(100)) / 5, 4.1f, ModContent.ProjectileType<Projectiles.Enemy.PoisonCrystalFire>(), crystalFireDamage, 2f, Main.myPlayer); //was 8.9f near 10, tried Main.rand.Next(2, 5)
                            }
                        }
                    else
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + Main.rand.Next(-500, 500), NPC.Center.Y + Main.rand.Next(-500, 500), 0, 0, ModContent.ProjectileType<Projectiles.Enemy.HypnoticDisrupter>(), disrupterDamage, 0f, Main.myPlayer);
                        }
                    }
                    fireBreathTimer = 0;
                }

                if (NPC.life < NPC.lifeMax / 2)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        // charge forward code 
                        if (Main.rand.NextBool(50))
                        {
                            chargeDamageFlag = true;
                            NPC.velocity = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 11);
                            NPC.netUpdate = true;
                        }
                    }

                    //This bit won't work in multiplayer, chargeDamageFlag needs to be synced. TODO.
                    if (chargeDamageFlag == true)
                    {
                        NPC.damage = 125;
                        chargeDamage++;
                    }
                    if (chargeDamage >= 125)
                    {
                        chargeDamageFlag = false;
                        NPC.damage = 115;
                        chargeDamage = 0;
                    }

                    //Fire breath
                    breathTimer++;
                    if (breathTimer > 280)
                    {
                        breathTimer = -30;
                    }
                    if (breathTimer < 0)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 breathVel = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 12);
                            breathVel += Main.rand.NextVector2Circular(-1.5f, 1.5f); if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + (5 * NPC.direction), NPC.Center.Y, breathVel.X, breathVel.Y, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), breathDamage, 0f, Main.myPlayer);
                            }
                            NPC.ai[3] = 0; //Reset bored counter. No teleporting mid-breath attack
                        }
                    }
                    if (breathTimer > 180)
                    {
                        UsefulFunctions.DustRing(NPC.Center, (int)(48 * ((280f - breathTimer) / 100f)), DustID.Torch, 48, 4);
                        Lighting.AddLight(NPC.Center, Color.Orange.ToVector3() * 5);
                    }
                }

            }
        }


        public static Texture2D spearTexture;
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            UsefulFunctions.EnsureLoaded(ref spearTexture, "tsorcRevamp/Projectiles/Enemy/EarthTrident");

            if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer >= 117 && Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height))
            {
                float rotation = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 1).ToRotation() + MathHelper.PiOver2;
             
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                if (NPC.spriteDirection == -1)
                {
                    spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, rotation, new Vector2(8, 38), NPC.scale, effects, 0); // facing left (8, 38 work)
                }
                else
                {
                    spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, rotation, new Vector2(8, 38), NPC.scale, effects, 0); // facing right, first value is height, higher number is higher
                }
            }
        }

        #region Gore
        public override void OnKill()
        {
            if (NPC.life <= 0)
            {
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 3").Type, 1f);
                    for (int i = 0; i < 6; i++)
                    {
                        Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Blood Splat").Type, 0.9f);
                    }
                }
            }
        }
        #endregion

        public override void ModifyNPCLoot(NPCLoot npcLoot) 
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Humanity>(), 1, 1, 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RedTitanite>(), 1, 3, 6));
        }
    }
}