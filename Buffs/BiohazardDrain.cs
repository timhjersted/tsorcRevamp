using tsorcRevamp.NPCs;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
	public class BiohazardDrain : ModBuff
	{
		//Generic texture since this buff is enemy-only
		public override string Texture => "tsorcRevamp/Buffs/ArmorDrug";

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Biohazard Drain");
			Description.SetDefault("Rapidly losing life");
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			npc.GetGlobalNPC<tsorcRevampGlobalNPC>().BiohazardDrain = true;
		}
	}
}
