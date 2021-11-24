using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.HellkiteDragon
{
    [AutoloadBossHead]
    class HellkiteDragonHead : ModNPC
    {
        public override void SetDefaults()
        {
            npc.netAlways = true;
            npc.npcSlots = 10;
            npc.width = 60;
            npc.height = 60;
            drawOffsetY = 42;
            npc.aiStyle = 6;
            npc.knockBackResist = 0;
            npc.timeLeft = 22500;
            npc.damage = 100;
            npc.defense = 10;
            npc.HitSound = SoundID.NPCHit7;
            npc.DeathSound = SoundID.NPCDeath8;
            npc.lifeMax = 100000;
            music = 12;
            npc.boss = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.behindTiles = true;
            npc.value = 200000;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.Confused] = true;
            npc.buffImmune[BuffID.OnFire] = true;
            npc.buffImmune[BuffID.CursedInferno] = true;
            bossBag = ModContent.ItemType<Items.BossBags.HellkiteBag>();

            Color textColor = new Color(175, 75, 255);
            despawnHandler = new NPCDespawnHandler("The Hellkite Dragon claims its prey...", textColor, 174);
        }


        int breathDamage = 33;
        int flameRainDamage = 27;
        int meteorDamage = 33;
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.damage = (int)(npc.damage / 2);
            breathDamage = (int)(breathDamage / 2);
            flameRainDamage = (int)(flameRainDamage / 2);
            meteorDamage = (int)(meteorDamage / 2);
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Hellkite Dragon");
        }

        int breathCD = 90;
        //int previous = 0;
        bool breath = false;
        //bool tailD = false;
        public override bool CheckActive()
        {
            return false;
        }

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
            bool BeforeThreeAfterSeven = (P.position.X < Main.maxTilesX * 0.3f) || (P.position.X > Main.maxTilesX * 0.7f); //Before 3/10ths or after 7/10ths width (a little wider than ocean bool?)
            bool BeforeThree = P.position.X < Main.maxTilesX * 0.3f;
            // these are all the regular stuff you get , now lets see......

            if (tsorcRevampWorld.SuperHardMode && !BeforeThree && Main.bloodMoon && AboveEarth && Main.rand.Next(1000) == 1) return 1;

            if (tsorcRevampWorld.SuperHardMode && BeforeThree && !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<Artorias>()) && Main.bloodMoon && AboveEarth && Main.rand.Next(30) == 1) return 1;

            if (tsorcRevampWorld.SuperHardMode && BeforeThree && !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<Artorias>()) && Main.bloodMoon && Sky && Main.rand.Next(5) == 1) return 1;

            if (tsorcRevampWorld.SuperHardMode && !Main.dayTime && Sky && Main.rand.Next(500) == 1) return 1;

            if (tsorcRevampWorld.SuperHardMode && !Main.dayTime && Sky && Main.bloodMoon && Main.rand.Next(150) == 1) return 1;

            if (tsorcRevampWorld.SuperHardMode && Main.bloodMoon && Sky && !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<Artorias>()) && Main.rand.Next(20) == 1) return 1;

            if (tsorcRevampWorld.SuperHardMode && !Main.dayTime && InHell && Main.rand.Next(500) == 1) return 1;

            if (Main.hardMode && P.townNPCs > 2f && tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<TheHunter>()) && !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<HellkiteDragonHead>()) && !Main.dayTime && Main.rand.Next(200) == 1)

            {
                Main.NewText("The village is under attack!", 175, 75, 255);
                Main.NewText("A Hellkite Dragon has come to feed...", 175, 75, 255);
                return 1;
            }

            return 0;
        }
        #endregion




        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(npc.whoAmI);

            Player nT = Main.player[npc.target];
            if (Main.rand.Next(275) == 0)
            {
                breath = true;
                //Main.PlaySound(15, -1, -1, 0);
            }
            if (breath)
            {
                //while (breathCD > 0) {
                //for (int pcy = 0; pcy < 10; pcy++) {
                Projectile.NewProjectile(npc.position.X + (float)npc.width / 2f, npc.position.Y + (float)npc.height / 2f, npc.velocity.X * 3f + (float)Main.rand.Next(-2, 3), npc.velocity.Y * 3f + (float)Main.rand.Next(-2, 3), ModContent.ProjectileType<Projectiles.Enemy.DragonsBreath>(), breathDamage, 1.2f, Main.myPlayer);
                //}
                npc.netUpdate = true; //new
                breathCD--;
                //}
            }
            if (breathCD <= 0)
            {
                breath = false;
                breathCD = 90;
                Main.PlaySound(2, -1, -1, 20);
            }
            if (Main.rand.Next(833) == 0)
            {
                for (int pcy = 0; pcy < 10; pcy++)
                {
                    Projectile.NewProjectile((float)nT.position.X - 600 + Main.rand.Next(1200), (float)nT.position.Y - 500f, (float)(-40 + Main.rand.Next(80)) / 10, 6.5f, ModContent.ProjectileType<Projectiles.Enemy.FlameRain>(), flameRainDamage, 2f, Main.myPlayer);
                    Main.PlaySound(2, -1, -1, 20);
                    npc.netUpdate = true; //new
                }
            }
            if (Main.rand.Next(1460) == 0)
            {
                for (int pcy = 0; pcy < 10; pcy++)
                {
                    Projectile.NewProjectile((float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer);
                    Main.PlaySound(2, -1, -1, 20);
                    npc.netUpdate = true; //new
                }
            }
            if (Main.rand.Next(2) == 0)
            {
                int d = Dust.NewDust(npc.position, npc.width, npc.height, 6, npc.velocity.X / 4f, npc.velocity.Y / 4f, 100, default(Color), 1f);
                Main.dust[d].noGravity = true;
            }
            
            int[] bodyTypes = new int[] { ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonLegs>(), ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonLegs>(), ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonBody2>(), ModContent.NPCType<HellkiteDragonBody3>() };
            tsorcRevampGlobalNPC.AIWorm(npc, ModContent.NPCType<HellkiteDragonHead>(), bodyTypes, ModContent.NPCType<HellkiteDragonTail>(), 12, .8f, 22, 0.25f, true, false, true, false, false); //30f was 10f

        }
        public static void SetImmune(Projectile projectile, NPC hitNPC)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC currentNPC = Main.npc[i];
                if (currentNPC.type == ModContent.NPCType<HellkiteDragonHead>() || currentNPC.type == ModContent.NPCType<HellkiteDragonBody>() || currentNPC.type == ModContent.NPCType<HellkiteDragonBody2>() || currentNPC.type == ModContent.NPCType<HellkiteDragonBody3>() || currentNPC.type == ModContent.NPCType<HellkiteDragonLegs>() || currentNPC.type == ModContent.NPCType<HellkiteDragonTail>())
                {
                    currentNPC.immune[projectile.owner] = 10;
                }
            }
        }
        public override void OnHitByProjectile(Projectile projectile, int damage, float knockback, bool crit)
        {
            SetImmune(projectile, npc);
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void NPCLoot()
        {
            npc.netUpdate = true;
            Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
            Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Hellkite Dragon Head Gore"), 1f);
            Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Blood Splat"), 0.9f);
            Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Blood Splat"), 0.9f);

            if (Main.expertMode)
            {
                npc.DropBossBags();
            }
            else
            {
                Item.NewItem(npc.getRect(), ModContent.ItemType<Items.DragonEssence>(), 22 + Main.rand.Next(6));
                Item.NewItem(npc.getRect(), ModContent.ItemType<Items.DarkSoul>(), 4000);
                if (Main.rand.Next(99) < 10) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Accessories.DragonStone>());
            }
        }
    }
}