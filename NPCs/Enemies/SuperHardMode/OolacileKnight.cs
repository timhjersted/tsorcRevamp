using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class OolacileKnight : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Oolacile Knight");
        }
        public override void SetDefaults()
        {
            npc.npcSlots = 2;
            Main.npcFrameCount[npc.type] = 16;
            animationType = 28;
            npc.height = 40;
            npc.width = 20;
            music = 12;
            npc.damage = 125;
            npc.defense = 70;
            npc.lavaImmune = true;
            npc.lifeMax = 18000;
            npc.scale = 1.1f;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.value = 28750;
            npc.knockBackResist = 0f;
            banner = npc.type;
            bannerItem = ModContent.ItemType<Banners.OolacileKnightBanner>();
            npc.lavaImmune = true;
        }

        int dragonsBreathDamage = 39;
        int darkExplosionDamage = 37;
        int earthTridentDamage = 35;

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = (int)(npc.lifeMax / 2);
            npc.damage = (int)(npc.damage / 2);
            dragonsBreathDamage = (int)(dragonsBreathDamage * tsorcRevampWorld.SubtleSHMScale);
            darkExplosionDamage = (int)(darkExplosionDamage * tsorcRevampWorld.SubtleSHMScale);
            earthTridentDamage = (int)(earthTridentDamage * tsorcRevampWorld.SubtleSHMScale);
        }



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
            bool FrozenOcean = spawnInfo.spawnTileX > (Main.maxTilesX - 800);
            bool Ocean = spawnInfo.spawnTileX < 800 || FrozenOcean;

            // these are all the regular stuff you get , now lets see......

            if (spawnInfo.water) return 0f;

            if (NPC.AnyNPCs(ModContent.NPCType<OolacileKnight>()))
            {
                return 0;
            }

            if (Jungle && tsorcRevampWorld.SuperHardMode && AboveEarth && !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<OolacileKnight>()) && Main.rand.Next(20) == 1)

            {
                Main.NewText("An ancient warrior has come to banish you from existence...", 175, 75, 255);
                return 1;
            }

            if (Dungeon && Main.bloodMoon && tsorcRevampWorld.SuperHardMode && tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<OolacileKnight>()) && Main.rand.Next(15) == 1)

            {
                Main.NewText("You are being hunted...", 175, 75, 255);
                return 1;
            }

            if (Meteor && Main.bloodMoon && tsorcRevampWorld.SuperHardMode && tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<OolacileKnight>()) && Main.rand.Next(20) == 1)

            {
                Main.NewText("You are being hunted...", 175, 75, 255);
                return 1;
            }

            if (Dungeon && tsorcRevampWorld.SuperHardMode && tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<OolacileKnight>()) && Main.rand.Next(30) == 1)

            {
                Main.NewText("You are being hunted...", 175, 75, 255);
                return 1;
            }

            return 0;
        }
        #endregion

        int hitCounter;
        float tridentTimer;
        float breathTimer;

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(npc, 1.5f, 0.03f, canTeleport: true, soundType: 26, soundFrequency: 2000, enragePercent: 0.5f, enrageTopSpeed: 4);
            tsorcRevampAIs.LeapAtPlayer(npc, 7, 4, 1.5f, 128);
            tsorcRevampAIs.SimpleProjectile(npc, ref tridentTimer, 150, ModContent.ProjectileType<Projectiles.Enemy.EarthTrident>(), earthTridentDamage, 11, Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height), true, 2, 17);

            breathTimer++;
            if (breathTimer > 500)
            {
                breathTimer = -90;
            }

            if (breathTimer < 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 breathVel = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 12);
                    breathVel += Main.rand.NextVector2Circular(-1.5f, 1.5f);
                    Projectile.NewProjectile(npc.Center.X + (5 * npc.direction), npc.Center.Y, breathVel.X, breathVel.Y, ModContent.ProjectileType<Projectiles.Enemy.CursedDragonsBreath>(), dragonsBreathDamage, 0f, Main.myPlayer);
                    npc.ai[3] = 0; //Reset bored counter. No teleporting mid-breath attack
                }
            }

            if (breathTimer > 360)
            {
                UsefulFunctions.DustRing(npc.Center, (int)(48 * ((500 - breathTimer) / 120)), DustID.Fire, 48, 4);
                Lighting.AddLight(npc.Center, Color.GreenYellow.ToVector3() * 5);
            }

            if (breathTimer == 0)
            {
                Projectile.NewProjectile(npc.Center.X, npc.Center.Y, 0, 0, ModContent.ProjectileType<Projectiles.Enemy.DarkExplosion>(), darkExplosionDamage, 0f, Main.myPlayer);
            }


            Player player = Main.player[npc.target];
            //when close to enemy, grapple and mobility hindered
            if (npc.Distance(player.Center) < 600)
            {
                player.AddBuff(ModContent.BuffType<Buffs.GrappleMalfunction>(), 2);
            }
            if (Main.hardMode && npc.Distance(player.Center) < 100)
            {
                player.AddBuff(ModContent.BuffType<Buffs.Crippled>(), 60, false);
            }


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

                if (Main.rand.Next(1000) == 0)
                {
                    int Spawned = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<NPCs.Enemies.SuperHardMode.OolacileDemon>(), 0);

                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(23, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
                    }
                }
            }
        }

        


        #region Debuffs
        public override void OnHitPlayer(Player player, int target, bool crit) 
        {

            if (Main.rand.Next(2) == 0)
            {
                player.AddBuff(ModContent.BuffType<Buffs.CurseBuildup>(), 36000, false); //-20 HP curse
            }

            if (Main.rand.Next(4) == 0)
            {

                player.AddBuff(36, 600, false); //broken armor
                player.AddBuff(23, 300, false); //cursed

            }

            //if (Main.rand.Next(10) == 0 && player.statLifeMax > 20) 

            //{

            //			Main.NewText("You have been cursed!");
            //	player.statLifeMax -= 20;
            //}
        }
        #endregion

        public static Texture2D spearTexture;
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
            int spriteWidth = npc.frame.Width; //use same number as ini Main.npcFrameCount[npc.type]
            int spriteHeight = Main.npcTexture[ModContent.NPCType<OolacileKnight>()].Height / Main.npcFrameCount[npc.type];

            int spritePosDifX = (int)(npc.frame.Width / 2);
            int spritePosDifY = npc.frame.Height; // was npc.frame.Height - 4;

            int frame = npc.frame.Y / spriteHeight;

            int offsetX = (int)(npc.position.X + (npc.width / 2) - Main.screenPosition.X - spritePosDifX + 0.5f);
            int offsetY = (int)(npc.position.Y + npc.height - Main.screenPosition.Y - spritePosDifY);

            SpriteEffects flop = SpriteEffects.None;
            if (npc.spriteDirection == 1)
            {
                flop = SpriteEffects.FlipHorizontally;
            }


            //Glowing Eye Effect
            for (int i = 15; i > -1; i--)
            {
                //draw 3 levels of trail
                int alphaVal = 255 - (15 * i);
                Color modifiedColour = new Color((int)(alphaVal), (int)(alphaVal), (int)(alphaVal), alphaVal);
                spriteBatch.Draw(Main.goreTexture[mod.GetGoreSlot("Gores/Oolacile Knight Glow")],
                    new Rectangle((int)(offsetX - npc.velocity.X * (i * 0.5f)), (int)(offsetY - npc.velocity.Y * (i * 0.5f)), spriteWidth, spriteHeight),
                    new Rectangle(0, npc.frame.Height * frame, spriteWidth, spriteHeight),
                    modifiedColour,
                    0,
                    new Vector2(0, 0),
                    flop,
                    0);
            }
        }
        

        #region gore
        public override void NPCLoot()
        {
            Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Oolacile Knight Gore 1"), 0.9f);
            Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Oolacile Knight Gore 2"), 0.9f);
            Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Oolacile Knight Gore 3"), 0.9f);
            Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Oolacile Knight Gore 4"), 0.9f);
            Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Oolacile Knight Gore 2"), 0.9f);
            Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Oolacile Knight Gore 3"), 0.9f);
            for (int i = 0; i < 8; i++)
            {
                Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Blood Splat"), 0.9f);
            }

            if (Main.rand.Next(99) < 30) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Humanity>());
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.RedTitanite>(), 2);
        }
        #endregion

    }
}