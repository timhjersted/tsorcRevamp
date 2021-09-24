using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.Seath
{
    [AutoloadBossHead]
    class SeathTheScalelessHead : ModNPC
    {
        public override void SetDefaults()
        {
            npc.netAlways = true;
            npc.npcSlots = 101;
            npc.width = 32;
            npc.height = 32;
            drawOffsetY = 60;
            npc.aiStyle = 6;
            npc.knockBackResist = 0;
            npc.timeLeft = 22500;
            npc.damage = 130;
            npc.defense = 50;
            npc.HitSound = SoundID.NPCHit7;
            npc.DeathSound = SoundID.NPCDeath8;
            npc.lifeMax = 170000;
            music = 12;
            npc.boss = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.value = 500000;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.Confused] = true;
            npc.buffImmune[BuffID.OnFire] = true;
            npc.buffImmune[BuffID.CursedInferno] = true;
            bossBag = ModContent.ItemType<Items.BossBags.SeathBag>();
            despawnHandler = new NPCDespawnHandler("Seath consumes your soul...", Color.Cyan, DustID.BlueFairy);
        }


        int breathDamage = 33;
        int frozenTearDamage = 44;
        int meteorDamage = 50;
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = (int)(npc.lifeMax / 2);
            npc.damage = (int)(npc.damage / 2);
            breathDamage = (int)(breathDamage / 2);
            frozenTearDamage = (int)(frozenTearDamage / 2);
            meteorDamage = (int)(meteorDamage / 2);
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Seath the Scaleless");
        }


        int breathCD = 110;
        bool breath = false;
        //bool tailD = false;

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.player;

            bool Sky = P.position.Y <= (Main.rockLayer * 4);
            bool Meteor = P.ZoneMeteor;
            bool Jungle = P.ZoneJungle;
            bool Dungeon = P.ZoneDungeon;
            bool Corruption = (P.ZoneCorrupt || P.ZoneCrimson);
            bool Hallow = P.ZoneHoly;
            bool AboveEarth = P.position.Y < Main.worldSurface;
            bool InBrownLayer = P.position.Y >= Main.worldSurface && P.position.Y < Main.rockLayer;
            bool InGrayLayer = P.position.Y >= Main.rockLayer && P.position.Y < (Main.maxTilesY - 200) * 16;
            bool InHell = P.position.Y >= (Main.maxTilesY - 200) * 16;
            bool Ocean = P.position.X < 3600 || P.position.X > (Main.maxTilesX - 100) * 16;
            bool FrozenOcean = P.position.X > (Main.maxTilesX - 100) * 16;


            if (tsorcRevampWorld.SuperHardMode && (Sky || AboveEarth) && !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<SeathTheScalelessHead>()) && FrozenOcean && Main.rand.Next(100) == 1) return 1;

            if (Main.hardMode && P.townNPCs > 2f && tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<Artorias>()) && !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<SeathTheScalelessHead>()) && !Main.dayTime && Main.rand.Next(1000) == 1)
            {
                Main.NewText("The village is under attack!", 175, 75, 255);
                Main.NewText("Seath the Scaleless has come to destroy all...", 175, 75, 255);
                return 1;
            }
            return 0;
        }
        #endregion



        #region AI
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(npc.whoAmI);


            Player nT = Main.player[npc.target];
            if (Main.rand.Next(255) == 0)
            {
                breath = true;
                Main.PlaySound(15, -1, -1, 0);
            }
            if (breath)
            {

                Projectile.NewProjectile(npc.position.X + (float)npc.width / 2f, npc.position.Y + (float)npc.height / 2f, npc.velocity.X * 3f + (float)Main.rand.Next(-2, 3), npc.velocity.Y * 3f + (float)Main.rand.Next(-2, 3), ModContent.ProjectileType<Projectiles.Enemy.FrozenDragonsBreath>(), breathDamage, 1.2f, 255);
                Main.PlaySound(2, -1, -1, 20);
                npc.netUpdate = true; //new
                breathCD--;

            }
            if (breathCD <= 0)
            {
                breath = false;
                breathCD = 110;
                //Main.PlaySound(2, -1, -1, 20);
            }
            if (Main.rand.Next(820) == 0)
            {
                for (int pcy = 0; pcy < 10; pcy++)
                {
                    Projectile.NewProjectile((float)nT.position.X - 800 + Main.rand.Next(1600), (float)nT.position.Y - 500f, (float)(-40 + Main.rand.Next(80)) / 10, 10.1f, ModContent.ProjectileType<Projectiles.Enemy.FrozenTear>(), frozenTearDamage, 2f, 255); //10.1f was 14.9f is speed
                    Main.PlaySound(2, -1, -1, 20);
                    npc.netUpdate = true; //new

                }
            }
            if (Main.rand.Next(1560) == 0)
            {
                for (int pcy = 0; pcy < 10; pcy++)
                {
                    Projectile.NewProjectile((float)nT.position.X - 500 + Main.rand.Next(1000), (float)nT.position.Y - 500f, (float)(-100 + Main.rand.Next(200)) / 10, 11.5f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, 255); //9.5f was 14.9f
                    Main.PlaySound(2, -1, -1, 20);
                    npc.netUpdate = true; //new
                }
            }
            if (Main.rand.Next(2) == 0)
            {
                int d = Dust.NewDust(npc.position, npc.width, npc.height, DustID.BlueFairy, npc.velocity.X / 4f, npc.velocity.Y / 4f, 100, default(Color), 1f);
                Main.dust[d].noGravity = true;
            }

            npc.boss = true;
            npc.noTileCollide = true;
            npc.noGravity = true;
            npc.behindTiles = true;
            int[] bodyTypes = new int[] { ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessLegs>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessLegs>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody2>(), ModContent.NPCType<SeathTheScalelessBody3>() };
            tsorcRevampGlobalNPC.AIWorm(npc, ModContent.NPCType<SeathTheScalelessHead>(), bodyTypes, ModContent.NPCType<SeathTheScalelessTail>(), 17, 6f, 10f, 0.17f, true, false, true, false, false);


            //this makes the head always stay in the same position even when it flips upside down
            //if (npc.velocity.X < 0f){ npc.spriteDirection = 1; } else  //both -1 is correct for looking right with no flipping, or 1 and -1 with flipping
            //if (npc.velocity.X > 0f){ npc.spriteDirection = -1; }


           



            //new
            //if (!Main.npc[(int)npc.ai[1]].active) 
            //{
            //	npc.life = 0;
            //	npc.HitEffect(0, 10.0);
            //	NPCLoot();
            //	npc.active = false;
            //}

        }
        #endregion
        public override bool CheckActive()
        {
            return false;
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void NPCLoot()
        {
            Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
            if (npc.life <= 0)
            {
                //Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), "Seath the Scaleless Head Gore", 1f, -1);
                Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Seath the Scaleless Head Gore"), 1f);
                //Main.gore[a].timeLeft = 1800; int a = Gore.New..etc

                if (Main.expertMode)
                {
                    npc.DropBossBags();
                }
                else
                {
                    Item.NewItem(npc.getRect(), ModContent.ItemType<Items.DarkSoul>(), 7000);
                    Item.NewItem(npc.getRect(), ModContent.ItemType<Items.BequeathedSoul>(), 2);
                    Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Accessories.BlueTearstoneRing>());
                    Item.NewItem(npc.getRect(), ModContent.ItemType<Items.PurgingStone>());
                    Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Accessories.DragonWings>());
                }
                //npc.netUpdate = true;
            }
        }
    }
}