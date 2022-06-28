using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class Abysswalker : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            Main.npcFrameCount[NPC.type] = 15;
            AnimationType = 21;
            NPC.knockBackResist = 0;
            NPC.aiStyle = 3;
            NPC.damage = 105;
            NPC.defense = 72;
            NPC.height = 40;
            NPC.lifeMax = 5000;
            NPC.scale = 1;
            NPC.HitSound = SoundID.NPCHit29;
            NPC.DeathSound = SoundID.NPCDeath31;
            NPC.value = 12500;
            NPC.width = 18;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.AbysswalkerBanner>();
        }


        int poisonBallDamage = 27;
        int stormBallDamage = 30;

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
            poisonBallDamage = (int)(poisonBallDamage * tsorcRevampWorld.SubtleSHMScale);
            stormBallDamage = (int)(stormBallDamage * tsorcRevampWorld.SubtleSHMScale);
        }


        //Spawns in the Jungle Underground and in the Cavern.
        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player player = spawnInfo.Player;

            // these are all the regular stuff you get , now lets see......
            float chance = 0;

            if ((player.ZoneMeteor || player.ZoneJungle) && tsorcRevampWorld.SuperHardMode && !player.ZoneDungeon && !(player.ZoneCorrupt || player.ZoneCrimson))
            {
                chance = 0.25f;
            }
            if (player.ZoneDirtLayerHeight)
            {
                chance *= 1.5f;
            }
            if (player.ZoneRockLayerHeight)
            {
                chance *= 1.5f;
            }
            if (Main.bloodMoon)
            {
                chance *= 2;
            }

            return chance;
        }
        #endregion

        float poisonStrikeTimer = 0;
        float poisonStormTimer = 0;
        int poisonStormTimerCap = 400;
        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 2f, 0.05f, 0.2f, true, enragePercent: 0.3f, enrageTopSpeed: 3);

            bool clearLineofSight = Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height);

            tsorcRevampAIs.SimpleProjectile(NPC, ref poisonStrikeTimer, 120, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), poisonBallDamage, 9, clearLineofSight, true, SoundID.Item20);

            if (tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<Bosses.Fiends.EarthFiendLich>()))
            {
                poisonStormTimerCap = 250;
            }

            tsorcRevampAIs.SimpleProjectile(NPC, ref poisonStormTimer, poisonStormTimerCap, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssStormBall>(), stormBallDamage, 0, clearLineofSight, true, SoundID.Item100);

            if (poisonStrikeTimer >= 60)
            {
                Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Ichor, 0, 0).noGravity = true;
            }
            if (poisonStormTimer >= poisonStormTimerCap - 90)
            {
                UsefulFunctions.DustRing(NPC.Center, (poisonStormTimerCap + 5) - poisonStormTimer, DustID.BlueCrystalShard, 12, 4);
                Lighting.AddLight(NPC.Center, Color.Orange.ToVector3() * 5);
                if (Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height))
                {
                    NPC.velocity = Vector2.Zero;
                }
            }
            

            //Transparency. Higher alpha = more invisible
            if (NPC.justHit)
            {
                NPC.alpha = 0;
            }
            if (Main.rand.Next(200) == 1)
            {
                NPC.alpha = 0;
            }
            if (Main.rand.Next(50) == 1)
            {
                NPC.alpha = 210;
            }
            if (Main.rand.Next(250) == 1)
            {
                NPC.life += 5;
                if (NPC.life > NPC.lifeMax) NPC.life = NPC.lifeMax;
                NPC.netUpdate = true;
            }
        }

        #region Gore
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Voodoomaster Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 3").Type, 1f);
            }
            if (Main.rand.Next(99) < 10) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.FlameOfTheAbyss>(), 4 + Main.rand.Next(3));
        }
        #endregion


    }
}