using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    class MindflayerKingServant : ModNPC
    {
        public override void SetDefaults()
        {
            Main.npcFrameCount[NPC.type] = 3;
            AnimationType = 29;
            NPC.aiStyle = 8;
            NPC.damage = 75;
            NPC.defense = 15;
            NPC.height = 44;
            NPC.lifeMax = 400;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lavaImmune = true;
            AIType = 32;
            NPC.value = 800;
            NPC.width = 28;
            NPC.knockBackResist = 0.2f;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.MindflayerKingServantBanner>();
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
        }

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (!spawnInfo.Player.ZoneDungeon && !spawnInfo.Player.ZoneJungle && !spawnInfo.Player.ZoneMeteor && spawnInfo.Player.position.Y < ((Main.rockLayer * 25.0)) && spawnInfo.Player.position.Y > ((Main.worldSurface * 0.44999998807907104)))
            {
                if (spawnInfo.Player.position.Y > ((Main.rockLayer * 15.0)) && spawnInfo.Player.position.X < ((Main.rockLayer * 60.0)) && Main.rand.Next(2000) == 1) return 1;
                if (spawnInfo.Player.position.Y > ((Main.rockLayer * 15.0)) && spawnInfo.Player.position.X > ((Main.rockLayer * 145.0)) && Main.rand.Next(2000) == 1) return 1;
            }
            return 0;
        }
        #endregion

        #region Gore
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Piscodemon Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Piscodemon Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Piscodemon Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Piscodemon Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Piscodemon Gore 3").Type, 1f);
            }
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Heart, 1);
        }
        #endregion
    }
}