using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;
using tsorcRevamp.Projectiles.Enemy;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode.SerpentOfTheAbyss
{
    class SerpentOfTheAbyssHead : ModNPC
    {

        int breathCD = 110;
        bool breath = false;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Serpent of the Abyss");
        }
        public override void SetDefaults()
        {
            NPC.netAlways = true;
            NPC.npcSlots = 2;
            NPC.width = 42;
            NPC.height = 81;
            NPC.aiStyle = 6;
            NPC.defense = 200;
            NPC.timeLeft = 22500;
            NPC.damage = 170;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.lifeMax = 20000;
            NPC.knockBackResist = 0;
            NPC.lavaImmune = true;
            NPC.scale = 1;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.value = 25500;
            banner = NPC.type;
            bannerItem = ModContent.ItemType<Banners.SerpentOfTheAbyssBanner>();

            bodyTypes = new int[33];
            int bodyID = ModContent.NPCType<SerpentOfTheAbyssBody>();
            for (int i = 0; i < 33; i++)
            {
                bodyTypes[i] = bodyID;
            }
        }

        int cursedBreathDamage = 35;
        int poisonFlamesDamage = 39;
        int dragonMeteorDamage = 41;

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
            cursedBreathDamage = (int)(cursedBreathDamage * tsorcRevampWorld.SubtleSHMScale);
            poisonFlamesDamage = (int)(poisonFlamesDamage * tsorcRevampWorld.SubtleSHMScale);
            dragonMeteorDamage = (int)(dragonMeteorDamage * tsorcRevampWorld.SubtleSHMScale);
        }
        int[] bodyTypes;

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player p = spawnInfo.Player;
            Point pTile = p.Center.ToTileCoordinates();
            bool worldEdge = (pTile.X < Main.maxTilesX * 0.3f) || (pTile.X > Main.maxTilesX * 0.7f); //thanks i hate it
            if (tsorcRevampWorld.SuperHardMode)
            {
                if (p.ZoneUnderworldHeight)
                {
                    if (worldEdge)
                    {
                        if (Main.bloodMoon) { return 0.2f; } //blood moon, underworld, edge
                        else return 0.067f; //not blood moon, underworld, edge
                    }
                    else if (Main.bloodMoon) { return 0.067f; } //blood moon, underworld
                    else return 0.02f; //underworld
                }
            }
            return 0; //outside shm
        }

        public override void AI()
        {

            tsorcRevampGlobalNPC.AIWorm(NPC, ModContent.NPCType<SerpentOfTheAbyssHead>(), bodyTypes, ModContent.NPCType<SerpentOfTheAbyssTail>(), 35, .8f, 17, 0.25f, false, false, false, true, true);


            Player nT = Main.player[NPC.target];
            //190 was 90
            if (Main.rand.Next(190) == 0)
            {
                breath = true;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20);
                NPC.netUpdate = true;
            }


            if (breath)
            {

                float rotation = (float)Math.Atan2(NPC.Center.Y - Main.player[NPC.target].Center.Y, NPC.Center.X - Main.player[NPC.target].Center.X);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y + (20f * NPC.direction), NPC.velocity.X * 3f + (float)Main.rand.Next(-2, 3), NPC.velocity.Y * 3f + (float)Main.rand.Next(-2, 3), ModContent.ProjectileType<CursedDragonsBreath>(), cursedBreathDamage, 0f, Main.myPlayer); //cursed dragons breath
                    Main.projectile[num54].timeLeft = 50;
                }

                breathCD--;

            }
            if (breathCD <= 0)
            {
                breath = false;
                breathCD = 120;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20);
            }
            if (Main.rand.Next(940) == 0)
            {
                for (int pcy = 0; pcy < 10; pcy++)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 400f, (float)(-80 + Main.rand.Next(160)) / 10, 10.9f, ModContent.ProjectileType<PoisonFlames>(), poisonFlamesDamage, 2f, Main.myPlayer); //9.9f was 14.9f
                    }
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20);
            }
            if (Main.rand.Next(2760) == 0)
            {
                for (int pcy = 0; pcy < 10; pcy++)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(1600), (float)nT.position.Y - 300f, (float)(-40 + Main.rand.Next(80)) / 10, 9.5f, ModContent.ProjectileType<DragonMeteor>(), dragonMeteorDamage, 2f, Main.myPlayer); //dragon meteor
                    }
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20);
            }
            if (Main.rand.Next(60) == 0)
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, 6, NPC.velocity.X / 4f, NPC.velocity.Y / 4f, 100, default(Color), 1f);
                Main.dust[d].noGravity = true;
            }
        }

        public override void OnKill()
        {
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Humanity>(), 2);

            if (Main.rand.Next(12) == 0)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Humanity>(), Main.rand.Next(3, 6));
            }

        }

        public override void HitEffect(int hitDirection, double damage)
        {
            if (NPC.life <= 0)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gores/Lich King Serpent Head Gore").Type);
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (Main.rand.Next(2) == 0)
            {
                target.AddBuff(BuffID.CursedInferno, 600);
                target.AddBuff(ModContent.BuffType<Buffs.SlowedLifeRegen>(), 1200);
            }
        }
    }
}
