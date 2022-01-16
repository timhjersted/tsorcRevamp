using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;
using tsorcRevamp.Projectiles.Enemy;
using static tsorcRevamp.SpawnHelper;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode.SerpentOfTheAbyss {
    class SerpentOfTheAbyssHead : ModNPC {

        int breathCD = 110;
        bool breath = false;
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Serpent of the Abyss");
        }
        public override void SetDefaults() {
            npc.netAlways = true;
            npc.npcSlots = 2;
            npc.width = 42;
            npc.height = 81;
            npc.aiStyle = 6;
            npc.defense = 200;
            npc.timeLeft = 22500;
            npc.damage = 170;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath5;
            npc.lifeMax = 20000; //20k, down from 120k
            npc.knockBackResist = 0;
            npc.lavaImmune = true;
            npc.scale = 1;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.behindTiles = true;
            npc.value = 25500;
            banner = npc.type;
            bannerItem = ModContent.ItemType<Banners.SerpentOfTheAbyssBanner>();

            bodyTypes = new int[33];
            int bodyID = ModContent.NPCType<SerpentOfTheAbyssBody>();
            for (int i = 0; i < 33; i++)
            {
                bodyTypes[i] = bodyID;
            }
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = (int)(npc.lifeMax / 2);
            npc.damage = (int)(npc.damage / 2);
        }
        int[] bodyTypes;

        public override float SpawnChance(NPCSpawnInfo spawnInfo) {
            Player p = spawnInfo.player;
            Point pTile = p.Center.ToTileCoordinates();
            bool worldEdge = (pTile.X < Main.maxTilesX * 0.3f) || (pTile.X > Main.maxTilesX * 0.7f); //thanks i hate it
            if (tsorcRevampWorld.SuperHardMode) {
                if (p.ZoneUnderworldHeight) {
                    if (worldEdge) {
                        if (Main.bloodMoon) { return 0.2f; } //blood moon, underworld, edge
                        else return 0.067f; //not blood moon, underworld, edge
                    }
                    else if (Main.bloodMoon) { return 0.067f; } //blood moon, underworld
                    else return 0.02f; //underworld
                }
            }
            return 0; //outside shm
        }

        public override void AI() {

            tsorcRevampGlobalNPC.AIWorm(npc, ModContent.NPCType<SerpentOfTheAbyssHead>(), bodyTypes, ModContent.NPCType<SerpentOfTheAbyssTail>(), 35, .8f, 17, 0.25f, false, false, false, true, true);


            Player nT = Main.player[npc.target];
            //190 was 90
            if (Main.rand.Next(190) == 0) {
                breath = true;
                Main.PlaySound(SoundID.Item, -1, -1, 20);
                npc.netUpdate = true;
            }


            if (breath) {

                float rotation = (float)Math.Atan2(npc.Center.Y - Main.player[npc.target].Center.Y, npc.Center.X - Main.player[npc.target].Center.X);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int num54 = Projectile.NewProjectile(npc.Center.X, npc.Center.Y + (20f * npc.direction), npc.velocity.X * 3f + (float)Main.rand.Next(-2, 3), npc.velocity.Y * 3f + (float)Main.rand.Next(-2, 3), ModContent.ProjectileType<CursedDragonsBreath>(), 40, 0f, Main.myPlayer); //cursed dragons breath
                    Main.projectile[num54].timeLeft = 50;
                }

                breathCD--;

            }
            if (breathCD <= 0) {
                breath = false;
                breathCD = 120;
                Main.PlaySound(SoundID.Item, -1, -1, 20);
            }
            if (Main.rand.Next(940) == 0) {
                for (int pcy = 0; pcy < 10; pcy++) {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile((float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 400f, (float)(-80 + Main.rand.Next(160)) / 10, 10.9f, ModContent.ProjectileType<PoisonFlames>(), 47, 2f, Main.myPlayer); //9.9f was 14.9f
                    }
                }
                Main.PlaySound(SoundID.Item, -1, -1, 20);
            }
            if (Main.rand.Next(2760) == 0) {
                for (int pcy = 0; pcy < 10; pcy++)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile((float)nT.position.X - 100 + Main.rand.Next(1600), (float)nT.position.Y - 300f, (float)(-40 + Main.rand.Next(80)) / 10, 9.5f, ModContent.ProjectileType<DragonMeteor>(), 50, 2f, Main.myPlayer); //dragon meteor
                    } 
                }
                Main.PlaySound(SoundID.Item, -1, -1, 20);
            }
            if (Main.rand.Next(60) == 0) {
                int d = Dust.NewDust(npc.position, npc.width, npc.height, 6, npc.velocity.X / 4f, npc.velocity.Y / 4f, 100, default(Color), 1f);
                Main.dust[d].noGravity = true;
            }
        }

        public override void NPCLoot() {
            Item.NewItem(npc.getRect(), ModContent.ItemType<Humanity>(), 2);

            if (Main.rand.Next(12) == 0) {
                Item.NewItem(npc.getRect(), ModContent.ItemType<Humanity>(), Main.rand.Next(3, 6));
            }

        }

        public override void HitEffect(int hitDirection, double damage) {
            if (npc.life <= 0) {
                Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Lich King Serpent Head Gore"));
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) {
            if (Main.rand.Next(2) == 0) {
                target.AddBuff(BuffID.CursedInferno, 600);
                target.AddBuff(ModContent.BuffType<Buffs.SlowedLifeRegen>(), 1200);
            }
        }
    }
}
