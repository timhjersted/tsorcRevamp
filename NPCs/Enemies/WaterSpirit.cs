using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.ItemDropRules;

namespace tsorcRevamp.NPCs.Enemies
{
    class WaterSpirit : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 4;
            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[] {
                    BuffID.Poisoned,
                    BuffID.OnFire,
                    BuffID.Confused
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            AnimationType = 60;
            NPC.width = 50;
            NPC.height = 50;
            NPC.damage = 38;
            NPC.defense = 18;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lifeMax = 600;
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
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {

            /**
			//if(Y >= Main.rockLayer) return false; //this is for being above the grey background
			if (Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].type != 53) return 0; //this means 'if the tile you spawn on is not sand , dont spawn'
			if (Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY - 1].LiquidAmount == 0) return 0; //this means if there is no water above , don't spawn
			if (Main.rand.NextBool(15))
			{
				NPC.NewNPC(NPC.GetSource_FromAI(), spawnInfo.SpawnTileX * 16 + 8, spawnInfo.SpawnTileY * 16, 65, 0);
			}
			if (Main.rand.NextBool(10))
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

        public override void ModifyNPCLoot(NPCLoot npcLoot) 
        {
            npcLoot.Add(new CommonDrop(ModContent.ItemType<Items.Weapons.Magic.GreatMagicShieldScroll>(), 100, 1, 1, 3));
        }
    }
}


