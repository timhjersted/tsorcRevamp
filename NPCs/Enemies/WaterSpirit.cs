using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    class WaterSpirit : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            Main.npcFrameCount[NPC.type] = 4;
            AnimationType = 60;
            NPC.width = 50;
            NPC.height = 50;
            NPC.damage = 75;
            NPC.defense = 18;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lifeMax = 1200;
            NPC.scale = 1;
            NPC.friendly = false;
            NPC.noTileCollide = true;
            NPC.lavaImmune = true;
            NPC.noGravity = true;
            NPC.knockBackResist = 0;
            NPC.alpha = 100;
            NPC.value = 1600;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.WaterSpiritBanner>();


            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.Confused] = true;
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {

            /**
			//if(Y >= Main.rockLayer) return false; //this is for being above the grey background
			if (Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].type != 53) return 0; //this means 'if the tile you spawn on is not sand , dont spawn'
			if (Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY - 1].LiquidAmount == 0) return 0; //this means if there is no water above , don't spawn
			if (Main.rand.Next(15) == 0)
			{
				NPC.NewNPC(NPC.GetSource_FromAI(), spawnInfo.SpawnTileX * 16 + 8, spawnInfo.SpawnTileY * 16, 65, 0);
			}
			if (Main.rand.Next(10) == 0)
			{
				NPC.NewNPC(NPC.GetSource_FromAI(), spawnInfo.SpawnTileX * 16 + 8, spawnInfo.SpawnTileY * 16, 67, 0);
			}
			else
			{
				NPC.NewNPC(NPC.GetSource_FromAI(), spawnInfo.SpawnTileX * 16 + 8, spawnInfo.SpawnTileY * 16, 64, 0);
			}
			**/
            return 0;
        }

        public override void OnKill()
        {
            if (Main.rand.Next(100) < 3) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Magic.GreatMagicShieldScroll>());
        }
    }
}


