using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class CrystalKnight : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.npcSlots = 2;
            Main.npcFrameCount[NPC.type] = 20;
            AnimationType = 110;
            NPC.width = 18;
            NPC.height = 48;
            NPC.timeLeft = 750;
            NPC.damage = 125;
            NPC.defense = 30;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lavaImmune = true;
            NPC.lifeMax = 6000;
            NPC.scale = 0.9f;
            NPC.knockBackResist = 0;
            NPC.value = 7930;

            banner = NPC.type;
            bannerItem = ModContent.ItemType<Banners.CrystalKnightBanner>();
        }

        int crystalBoltDamage = 30;
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            crystalBoltDamage = (int)(crystalBoltDamage * tsorcRevampWorld.SubtleSHMScale);
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player player = spawnInfo.Player;
            bool FrozenOcean = spawnInfo.SpawnTileX > (Main.maxTilesX - 800);

            // these are all the regular stuff you get , now lets see......
            float chance = 0;

            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneOverworldHeight && (FrozenOcean || player.ZoneHallow))
            {
                chance = 0.2f;
            }
            if (tsorcRevampWorld.SuperHardMode && !spawnInfo.Player.ZoneOverworldHeight && (FrozenOcean || player.ZoneHallow))
            {
                chance = 0.36f;
            }
            if (FrozenOcean && player.ZoneHallow)
            {
                chance *= 2;
            }

            if (Main.bloodMoon)
            {
                chance *= 2;
            }

            return chance;
        }


        public override void AI()
        {
            // ArcherAI(NPC npc, int projectileType, int projectileDamage, float projectileVelocity, int projectileCooldown, float topSpeed = 1f, float acceleration = .07f, float brakingPower = .2f, bool canTeleport = false, bool hatesLight = false, int passiveSound = 0, int soundFrequency = 1000, float enragePercent = 0, float enrageTopSpeed = 0, bool lavaJumping = false, float projectileGravity = 0.035f, int soundType = 2, int soundStyle = 5)
            tsorcRevampAIs.ArcherAI(NPC, ModContent.ProjectileType<Projectiles.Enemy.EnemyCrystalKnightBolt>(), crystalBoltDamage, 14, 70, 2, 0.07f, canTeleport: true, lavaJumping: true, soundType: 2, soundStyle: 30);
        }


        public override void OnKill()
        {
            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
            if (NPC.life <= 0)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gores/Crystal Knight Gore 1").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gores/Crystal Knight Gore 2").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gores/Crystal Knight Gore 2").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gores/Crystal Knight Gore 2").Type, 1.1f);

                for (int num36 = 0; num36 < 50; num36++)
                {
                    {
                        Color color = new Color();
                        int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 1f);
                        Main.dust[dust].noGravity = true;
                        dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 1f);
                        Main.dust[dust].noGravity = true;
                        dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 1f);
                        Main.dust[dust].noGravity = true;
                        dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 1f);
                        Main.dust[dust].noGravity = true;

                        Dust.NewDust(NPC.position, NPC.height, NPC.width, 14, 0.2f, 0.2f, 100, default(Color), 2f);
                        Dust.NewDust(NPC.position, NPC.height, NPC.width, 14, 0.2f, 0.2f, 100, default(Color), 1f);
                        Dust.NewDust(NPC.position, NPC.height, NPC.width, 14, 0.2f, 0.2f, 100, default(Color), 1f);
                        Dust.NewDust(NPC.position, NPC.height, NPC.width, 14, 0.2f, 0.2f, 100, default(Color), 1f);
                        Dust.NewDust(NPC.position, NPC.height, NPC.width, 14, 0.2f, 0.2f, 100, default(Color), 1f);
                        Dust.NewDust(NPC.position, NPC.height, NPC.width, 14, 0.2f, 0.2f, 100, default(Color), 1f);
                    }


                }

                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.BlueTitanite>(), 3 + Main.rand.Next(2));
            }
        }

    }
}