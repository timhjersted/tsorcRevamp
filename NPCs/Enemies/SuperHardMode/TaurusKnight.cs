using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
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
            npc.npcSlots = 5;
            Main.npcFrameCount[npc.type] = 16;
            animationType = 28;
            npc.height = 40;
            npc.width = 20;
            npc.damage = 145;
            npc.defense = 50;
            npc.timeLeft = 22000;
            npc.lifeMax = 14200;
            npc.scale = 1.1f;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.value = 62000;
            npc.knockBackResist = 0.01f;
            npc.lavaImmune = true;
            banner = npc.type;
            bannerItem = ModContent.ItemType<Banners.TaurusKnightBanner>();

            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.OnFire] = true;
            npc.buffImmune[BuffID.Confused] = true;
        }

        int breathDamage = 20;
        int tridentDamage = 25;
        int crystalFireDamage = 50;

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = (int)(npc.lifeMax / 2);
            npc.damage = (int)(npc.damage / 2);
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
            Player P = spawnInfo.player; //this shortens our code up from writing this line over and over.
            bool Meteor = P.ZoneMeteor;
            bool Jungle = P.ZoneJungle;
            bool Dungeon = P.ZoneDungeon;
            bool Corruption = (P.ZoneCorrupt || P.ZoneCrimson);
            bool Hallow = P.ZoneHoly;
            bool AboveEarth = spawnInfo.spawnTileY < Main.worldSurface;
            bool InBrownLayer = spawnInfo.spawnTileY >= Main.worldSurface && spawnInfo.spawnTileY < Main.rockLayer;
            bool InGrayLayer = spawnInfo.spawnTileY >= Main.rockLayer && spawnInfo.spawnTileY < (Main.maxTilesY - 200) * 16;
            bool InHell = spawnInfo.spawnTileY >= (Main.maxTilesY - 200) * 16;
            bool Ocean = spawnInfo.spawnTileX < 3600 || spawnInfo.spawnTileX > (Main.maxTilesX - 100) * 16;

            // these are all the regular stuff you get , now lets see......


            if (tsorcRevampWorld.SuperHardMode && Dungeon && Main.rand.Next(60) == 1)

            {
                UsefulFunctions.BroadcastText("A Taurus Knight is close... ", 175, 75, 255);
                return 1;
            }

            if (tsorcRevampWorld.SuperHardMode && InHell && Main.rand.Next(30) == 1)

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
            npc.aiStyle = -1;
            tsorcRevampAIs.FighterAI(npc, 0.6f, 0.07f, 0.1f, true, enragePercent: 0.5f, enrageTopSpeed: 5);

            bool clearLineofSight = Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height);
            tsorcRevampAIs.SimpleProjectile(npc, ref tridentTimer, 136, ModContent.ProjectileType<Projectiles.Enemy.EarthTrident>(), tridentDamage, 14, clearLineofSight, true, 2, 17);
            if (tridentTimer == 125f)
            {
                if (Main.rand.Next(2) == 1)
                {
                    npc.velocity.Y = -8f;
                }
            }

            projectileTimer++;
            if(clearLineofSight)
            {
                //Fire rain and disruptor
                if (projectileTimer > 225)
                {
                    Main.PlaySound(2, -1, -1, 20);
                    if (Main.rand.NextBool())
                        for (int pcy = 0; pcy < 10; pcy++)
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(Main.player[npc.target].position.X - 200 + Main.rand.Next(500), Main.player[npc.target].position.Y - 400f, (float)(-50 + Main.rand.Next(100)) / 5, 4.1f, ModContent.ProjectileType<Projectiles.Enemy.PoisonCrystalFire>(), crystalFireDamage, 2f, Main.myPlayer); //was 8.9f near 10, tried Main.rand.Next(2, 5)
                            }
                        }
                    else
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(npc.Center.X + Main.rand.Next(-500, 500), npc.Center.Y + Main.rand.Next(-500, 500), 0, 0, ModContent.ProjectileType<Projectiles.Enemy.HypnoticDisrupter>(), disrupterDamage, 0f, Main.myPlayer);
                        }
                    }
                    projectileTimer = 0;
                }

                if (npc.life < npc.lifeMax * 0.5f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        // charge forward code 
                        if (Main.rand.Next(50) == 1)
                        {
                            chargeDamageFlag = true;
                            npc.velocity = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 11);
                            npc.netUpdate = true;
                        }
                    }

                    //This bit won't work in multiplayer, chargeDamageFlag needs to be synced. TODO.
                    if (chargeDamageFlag == true)
                    {
                        npc.damage = 125;
                        chargeDamage++;
                    }
                    if (chargeDamage >= 125)
                    {
                        chargeDamageFlag = false;
                        npc.damage = 115;
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
                            Vector2 breathVel = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 12);
                            breathVel += Main.rand.NextVector2Circular(-1.5f, 1.5f); if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(npc.Center.X + (5 * npc.direction), npc.Center.Y, breathVel.X, breathVel.Y, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), breathDamage, 0f, Main.myPlayer);
                            }
                            npc.ai[3] = 0; //Reset bored counter. No teleporting mid-breath attack
                        }
                    }
                    if (breathTimer > 180)
                    {
                        UsefulFunctions.DustRing(npc.Center, (int)(48 * ((280f - breathTimer) / 100f)), DustID.Fire, 48, 4);
                        Lighting.AddLight(npc.Center, Color.Orange.ToVector3() * 5);
                    }
                }

            }
        }
    

        static Texture2D spearTexture;
        public override void PostDraw(SpriteBatch spriteBatch, Color drawColor)
        {

            if (spearTexture == null)
            {
                spearTexture = mod.GetTexture("Projectiles/Enemy/EarthTrident");
                //spearTexture = ModContent.GetTexture("Terraria/Projectile_508");
            }

            //TELEGRAPH DUSTS
            if (tridentTimer >= 117 && Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))  
            {
                Lighting.AddLight(npc.Center, Color.Yellow.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                if (Main.rand.Next(2) == 1)
                {
                    Dust.NewDust(npc.position, npc.width / 2, npc.height / 2, DustID.GoldFlame, npc.velocity.X, npc.velocity.Y);
                    Dust.NewDust(npc.position, npc.width / 2, npc.height / 2, DustID.GoldFlame, npc.velocity.X, npc.velocity.Y); //CrystalPulse
                }

                //if (npc.ai[2] >= 150)
                //{
                
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

        #region Gore
        public override void NPCLoot()
        {
            if (npc.life <= 0)
            {
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Red Knight Gore 1"), 1f);
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Red Knight Gore 2"), 1f);
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Red Knight Gore 3"), 1f);
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Red Knight Gore 2"), 1f);
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Red Knight Gore 3"), 1f);
                for (int i = 0; i < 6; i++)
                {
                    Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Blood Splat"), 0.9f);
                }
            }

            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Humanity>(), 5);
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.RedTitanite>(), 7);
        }
        #endregion
    }
}