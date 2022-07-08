using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class TaurusKnight : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Taurus Knight");
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            Main.npcFrameCount[NPC.type] = 16;
            AnimationType = 28;
            NPC.height = 40;
            NPC.width = 20;
            NPC.damage = 145;
            NPC.defense = 50;
            NPC.timeLeft = 22000;
            NPC.lifeMax = 14200;
            NPC.scale = 1.1f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 62000;
            NPC.knockBackResist = 0.01f;
            NPC.lavaImmune = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.TaurusKnightBanner>();

            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.Confused] = true;
        }

        int breathDamage = 20;
        int tridentDamage = 25;
        int crystalFireDamage = 50;

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
            breathDamage = (int)(breathDamage * tsorcRevampWorld.SubtleSHMScale);
            tridentDamage = (int)(tridentDamage * tsorcRevampWorld.SubtleSHMScale);
            crystalFireDamage = (int)(crystalFireDamage * tsorcRevampWorld.SubtleSHMScale);
        }

        public int disrupterDamage = 65;
        int chargeDamage = 0;
        bool chargeDamageFlag = false;



        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
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


            if (tsorcRevampWorld.SuperHardMode && Dungeon && Main.rand.NextBool(60))

            {
                UsefulFunctions.BroadcastText("A Taurus Knight is close... ", 175, 75, 255);
                return 1;
            }

            if (tsorcRevampWorld.SuperHardMode && InHell && Main.rand.NextBool(30))

            {
                UsefulFunctions.BroadcastText("A Taurus Knight is close... ", 175, 75, 255);
                return 1;
            }


            return 0;
        }
        #endregion

        float tridentTimer = 0;
        int projectileTimer = 0;
        int breathTimer = 0;
        public override void AI()
        {
            NPC.aiStyle = -1;
            tsorcRevampAIs.FighterAI(NPC, 0.6f, 0.07f, 0.1f, true, enragePercent: 0.5f, enrageTopSpeed: 5);

            bool clearLineofSight = Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height);
            tsorcRevampAIs.SimpleProjectile(NPC, ref tridentTimer, 136, ModContent.ProjectileType<Projectiles.Enemy.EarthTrident>(), tridentDamage, 14, clearLineofSight, true, SoundID.Item17);
            if (tridentTimer == 125f)
            {
                if (Main.rand.NextBool(2))
                {
                    NPC.velocity.Y = -8f;
                }
            }

            projectileTimer++;
            if (clearLineofSight)
            {
                //Fire rain and disruptor
                if (projectileTimer > 225)
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
                    projectileTimer = 0;
                }

                if (NPC.life < NPC.lifeMax * 0.5f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        // charge forward code 
                        if (Main.rand.NextBool(50))
                        {
                            chargeDamageFlag = true;
                            NPC.velocity = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 11);
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
                            Vector2 breathVel = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 12);
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


        static Texture2D spearTexture;
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {

            if (spearTexture == null)
            {
                spearTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/EarthTrident");
                //spearTexture = (Texture2D)ModContent.Request<Texture2D>("Terraria/Images/Projectile_508");
            }

            //TELEGRAPH DUSTS
            if (tridentTimer >= 117 && Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height))
            {
                Lighting.AddLight(NPC.Center, Color.Yellow.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                if (Main.rand.NextBool(2))
                {
                    Dust.NewDust(NPC.position, NPC.width / 2, NPC.height / 2, DustID.GoldFlame, NPC.velocity.X, NPC.velocity.Y);
                    Dust.NewDust(NPC.position, NPC.width / 2, NPC.height / 2, DustID.GoldFlame, NPC.velocity.X, NPC.velocity.Y); //CrystalPulse
                }

                //if (npc.ai[2] >= 150)
                //{

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

            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Humanity>(), 5);
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.RedTitanite>(), 7);
        }
        #endregion
    }
}