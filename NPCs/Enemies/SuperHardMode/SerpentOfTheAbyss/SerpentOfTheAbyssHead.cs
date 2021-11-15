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
            npc.npcSlots = 30;
            npc.width = 42;
            npc.height = 81;
            npc.aiStyle = 6;
            npc.defense = 260;
            npc.timeLeft = 22500;
            npc.damage = 170;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath5;
            npc.lifeMax = 60000;
            npc.knockBackResist = 0;
            npc.lavaImmune = true;
            npc.scale = 1;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.behindTiles = true;
            npc.value = 37500;
            banner = npc.type;
            bannerItem = ModContent.ItemType<Banners.SerpentOfTheAbyssBanner>();
        }


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
                    else return 0.05f; //underworld
                }
            }
            return 0; //outside shm
        }

        public override void AI() {
            if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[0] == 0f) {
                npc.ai[2] = npc.whoAmI;
                npc.realLife = npc.whoAmI;
                int whoAmI = npc.whoAmI;
                for (int i = 0; i < 44; i++) {
                    int npcType = ModContent.NPCType<SerpentOfTheAbyssBody>();
                    switch (i) {
                        case 43:
                            npcType = ModContent.NPCType<SerpentOfTheAbyssTail>();
                            break;
                    }
                    int bodyPart = NPC.NewNPC((int)(npc.position.X + npc.width / 2), (int)(npc.position.Y + (float)npc.height), npcType, npc.whoAmI);
                    Main.npc[bodyPart].ai[2] = npc.whoAmI;
                    Main.npc[bodyPart].realLife = npc.whoAmI;
                    Main.npc[bodyPart].ai[1] = whoAmI;
                    Main.npc[whoAmI].ai[0] = bodyPart;
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, bodyPart);
                    whoAmI = bodyPart;
                }
            }
            Player nT = Main.player[npc.target];

            //if (Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))           
            //		{
            if (Main.rand.Next(90) == 0) {
                breath = true;
                Main.PlaySound(SoundID.Item, -1, -1, 20);
                npc.netUpdate = true;
            }
            //}


            if (breath) {

                float rotation = (float)Math.Atan2(npc.Center.Y - Main.player[npc.target].Center.Y, npc.Center.X - Main.player[npc.target].Center.X);
                int num54 = Projectile.NewProjectile(npc.Center.X, npc.Center.Y + (20f * npc.direction), npc.velocity.X * 3f + (float)Main.rand.Next(-2, 3), npc.velocity.Y * 3f + (float)Main.rand.Next(-2, 3), ModContent.ProjectileType<CursedDragonsBreath>(), 40, 0f, Main.myPlayer); //cursed dragons breath
                Main.projectile[num54].timeLeft = 50;
                npc.netUpdate = true;


                breathCD--;

            }
            if (breathCD <= 0) {
                breath = false;
                breathCD = 120;
                Main.PlaySound(SoundID.Item, -1, -1, 20);
            }
            if (Main.rand.Next(940) == 0) {
                npc.netUpdate = true;
                for (int pcy = 0; pcy < 10; pcy++) {
                    Projectile.NewProjectile((float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 400f, (float)(-80 + Main.rand.Next(160)) / 10, 10.9f, ModContent.ProjectileType<PoisonFlames>(), 47, 2f, Main.myPlayer); //9.9f was 14.9f
                    Main.PlaySound(SoundID.Item, -1, -1, 20);

                }
            }
            if (Main.rand.Next(2760) == 0) {
                npc.netUpdate = true;
                for (int pcy = 0; pcy < 10; pcy++) {
                    Projectile.NewProjectile((float)nT.position.X - 100 + Main.rand.Next(1600), (float)nT.position.Y - 300f, (float)(-40 + Main.rand.Next(80)) / 10, 9.5f, ModContent.ProjectileType<DragonMeteor>(), 50, 2f, Main.myPlayer); //dragon meteor
                    Main.PlaySound(SoundID.Item, -1, -1, 20);
                }
            }
            if (Main.rand.Next(60) == 0) {
                npc.netUpdate = true;
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
                target.AddBuff(ModContent.BuffType<Buffs.SlowedLifeRegen>(), 3600);
            }
        }
    }
}
