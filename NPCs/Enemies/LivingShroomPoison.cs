using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    class LivingShroomPoison : ModNPC // Renewable souce of mushrooms
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Poisonous Living Shroom");
            Main.npcFrameCount[npc.type] = 8;
        }
		public override void SetDefaults()
		{
            npc.CloneDefaults(NPCID.CorruptBunny);
            aiType = NPCID.CorruptBunny;
            npc.width = 10;
            npc.height = 16;
            npc.HitSound = SoundID.NPCHit33;
            npc.DeathSound = SoundID.NPCDeath29;
            npc.knockBackResist = .75f;
            npc.damage = 10;
            npc.lifeMax = 14;
            npc.defense = 6;
            animationType = NPCID.CorruptBunny;
            npc.value = 0;
        }
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			float chance = 0;

			if (Main.dayTime && NPC.CountNPCS(mod.NPCType("LivingShroomPoison")) < 2 && TileID.Sets.Conversion.Grass[spawnInfo.spawnTileType])
			{
				return 0.1f;
			}
			return chance;
		}
		public override void OnHitPlayer(Player target, int damage, bool crit)
		{

			target.AddBuff(BuffID.Poisoned, 300); //why is it dubleing in duration?
			npc.life = 0;
		}
			

	}
}
