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
            NPC.npcSlots = 3;
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
            NPC.lifeMax = 3500;
            NPC.scale = 0.9f;
            NPC.knockBackResist = 0;
            NPC.value = 7230;

            NPC.buffImmune[BuffID.Poisoned] = true;
            //NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;

            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.CrystalKnightBanner>();
        }

        int crystalBoltDamage = 30;
        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            crystalBoltDamage = (int)(crystalBoltDamage * tsorcRevampWorld.SubtleSHMScale);
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player player = spawnInfo.Player;
            bool FrozenOcean = spawnInfo.SpawnTileX > (Main.maxTilesX - 800);
            float chance = 0;

            //Ensuring it can't spawn if one already exists.
            int count = 0;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if (Main.npc[i].type == NPC.type)
                {
                    count++;
                    if (count > 0)
                    {
                        return 0;
                    }
                }
            }

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
            tsorcRevampAIs.ArcherAI(NPC, ModContent.ProjectileType<Projectiles.Enemy.EnemyCrystalKnightBolt>(), crystalBoltDamage, 14, 100, 2, 0.07f, canTeleport: true, lavaJumping: true, shootSound: SoundID.Item30 with { Pitch = 1.1f });
        }


        public override void OnKill()
        {
            if (NPC.life <= 0)
            {
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Crystal Knight Gore 1").Type, 1.1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Crystal Knight Gore 2").Type, 1.1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Crystal Knight Gore 2").Type, 1.1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Crystal Knight Gore 2").Type, 1.1f);
                }
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
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.BlueTitanite>(), 1, 3, 5));
        }

    }
}