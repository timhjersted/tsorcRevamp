using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class SlograII : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Slogra");
        }
        public override void SetDefaults()
        {
            npc.npcSlots = 3;
            Main.npcFrameCount[npc.type] = 16;
            npc.width = 38;
            npc.height = 32;
            animationType = 104;
            npc.aiStyle = 26;
            //npc.timeLeft = 750;
            npc.damage = 116;
            npc.defense = 50;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = mod.GetLegacySoundSlot(SoundType.NPCKilled, "Sounds/NPCKilled/Gaibon_Roar");
            npc.lifeMax = 9200; //was 18k
            npc.knockBackResist = 0f;
            npc.value = 6000;
            banner = npc.type;
            bannerItem = ModContent.ItemType<Banners.SlograIIBanner>();
        }

        int tridentDamage = 50;

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = (int)(npc.lifeMax / 2);
            npc.damage = (int)(npc.damage / 2);
            tridentDamage = (int)(tridentDamage / 2);
        }

        float tridentTimer;

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.player; //this shortens our code up from writing this line over and over.

            bool Sky = spawnInfo.spawnTileY <= (Main.rockLayer * 4);
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

            if (tsorcRevampWorld.SuperHardMode && !Main.dayTime && !Dungeon && Jungle && AboveEarth && Main.rand.Next(50) == 1) return 1;

            if (tsorcRevampWorld.SuperHardMode && !Main.dayTime && !Dungeon && Jungle && InBrownLayer && Main.rand.Next(32) == 1) return 1;

            if (tsorcRevampWorld.SuperHardMode && !Main.dayTime && !Dungeon && Jungle && InGrayLayer && Main.rand.Next(40) == 1) return 1;

            if (tsorcRevampWorld.SuperHardMode && !Main.dayTime && !Dungeon && Jungle && Main.bloodMoon && Main.rand.Next(5) == 1) return 1;


            return 0;
        }
        #endregion


        public override void OnHitByItem(Player player, Item item, int damage, float knockback, bool crit)
        {
            base.OnHitByItem(player, item, damage, .09f, crit);


            //Insert whatever you want to happen on-hit here
            if (npc.justHit)
            {
                tridentTimer = 100f;
                //npc.knockBackResist = 0.09f;

                //WHEN HIT, CHANCE TO JUMP BACKWARDS && npc.velocity.Y >= -1f
                if (Main.rand.Next(10) == 1)//was 12
                {

                    npc.TargetClosest(false);

                    npc.velocity.Y = -8f;
                    npc.velocity.X = -4f * npc.direction;

                    //if (Main.rand.Next(1) == 1)
                    //{ 
                    tridentTimer = 140f;
                    //}

                    npc.netUpdate = true;
                }

                //WHEN HIT, CHANCE TO DASH STEP BACKWARDS && npc.velocity.Y >= 1f
                else if (Main.rand.Next(8) == 1)//was 10
                {

                    //npc.TargetClosest(false);

                    npc.velocity.Y = -4f;
                    npc.velocity.X = -6f * npc.direction;

                    //npc.direction *= -1;
                    //npc.spriteDirection = npc.direction;
                    //npc.ai[0] = 0f;
                    //if (Main.rand.Next(2) == 1)
                    //{
                    tridentTimer = 140f;
                    //}

                    //CHANCE TO JUMP AFTER DASH
                    if (Main.rand.Next(4) == 1)
                    {
                        npc.TargetClosest(true);
                        npc.velocity.Y = -7f;
                        tridentTimer = 140f;

                    }

                    npc.netUpdate = true;
                }
            }
        }

        int hitCounter = 0;
        public override void AI()
        {
            tsorcRevampAIs.FighterAI(npc, 3, 0.09f, 0.2f, true, 0, false, 26, 2000, 0.1f, 4, true);
            tsorcRevampAIs.LeapAtPlayer(npc, 5, 4, 2, 128);

            bool clearLineofSight = Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height);
            tsorcRevampAIs.SimpleProjectile(npc, ref tridentTimer, 150, ModContent.ProjectileType<Projectiles.Enemy.EarthTrident>(), tridentDamage, 11, clearLineofSight, true, 2, 17);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (npc.justHit)
                {
                    hitCounter++;
                }

                if (hitCounter > 6 || (npc.life < 0.1 * npc.lifeMax && Main.rand.Next(400) == 1))
                {
                    npc.velocity = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 15);
                    npc.netUpdate = true;
                    hitCounter = 0;
                    for (int i = 0; i < 50; i++)
                    {
                        Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 4, 0, 0, 100, default, 2f);
                    }
                    for (int i = 0; i < 20; i++)
                    {
                        Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 18, 0, 0, 100, default, 2f);
                    }
                }

                if (Main.rand.Next(500) == 0)
                {
                    int Spawned = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<NPCs.Enemies.SuperHardMode.VampireBat>(), 0);

                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(23, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
                    }
                }
            }
        }

        #region gore
        public override void NPCLoot()
        {
            Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Slogra Gore 1"), 0.9f);
            Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Slogra Gore 2"), 0.9f);
            Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Slogra Gore 3"), 0.9f);
            Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Slogra Gore 2"), 0.9f);
            Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Slogra Gore 3"), 0.9f);
            for (int i = 0; i < 6; i++)
            {
                Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Blood Splat"), 0.9f);
            }

            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.FlameOfTheAbyss>());
        }
        #endregion

        #region Draw Spear
        static Texture2D spearTexture;
        public override void PostDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            if (spearTexture == null || spearTexture.IsDisposed)
            {
                spearTexture = mod.GetTexture("Projectiles/Enemy/EarthTrident");
            }
            if (tridentTimer >= 110)
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
        #endregion

    }
}