using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class DarkKnight : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.npcSlots = 3;
            Main.npcFrameCount[NPC.type] = 20;
            AnimationType = 110;
            NPC.width = 18;
            NPC.height = 48;

            NPC.timeLeft = 750;
            NPC.damage = 105;
            NPC.lavaImmune = true;
            NPC.defense = 30;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lifeMax = 5600;
            NPC.knockBackResist = 0f;
            NPC.value = 9680;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.DarkKnightBanner>();

            //NPC.buffImmune[BuffID.Poisoned] = true;
            //NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
        }

        int stormWaveDamage = 35;
        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            stormWaveDamage = (int)(stormWaveDamage * tsorcRevampWorld.SubtleSHMScale);
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player player = spawnInfo.Player;
            bool FrozenOcean = spawnInfo.SpawnTileX > (Main.maxTilesX - 800);
            bool Ocean = spawnInfo.SpawnTileX < 800 || FrozenOcean;

            
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

            if (tsorcRevampWorld.SuperHardMode && player.townNPCs < 1f && (player.ZoneCorrupt || player.ZoneCrimson || player.ZoneDungeon) && !player.ZoneMeteor && !player.ZoneJungle && !player.ZoneUnderworldHeight && !player.ZoneHallow && !Ocean)
            {
                chance = 0.2f;
            }
            if (!Main.dayTime)
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
            tsorcRevampAIs.ArcherAI(NPC, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssStormWave>(), stormWaveDamage, 14, 90, 1.4f, 0.04f, 0.04f, true, lavaJumping: true);
        }

        #region Gore
        public override void OnKill()
        {
            if (NPC.life <= 0)
            {
                for (int num36 = 0; num36 < 50; num36++)
                {
                    {
                        Color color = new Color();
                        int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 2f);
                        Main.dust[dust].noGravity = true;
                        dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 2f);
                        Main.dust[dust].noGravity = true;
                        dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 3f);
                        Main.dust[dust].noGravity = true;
                        dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 62, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                        Main.dust[dust].noGravity = false;

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
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.WhiteTitanite>(), 1, 3, 5));
        }
        #endregion
    }
}