using tsorcRevamp.NPCs;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
	public class ViruCatDrain : ModBuff
	{
		//Generic texture since this buff is enemy-only
		public override string Texture => "tsorcRevamp/Buffs/ArmorDrug";

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Virulent Catalyzer Drain");
			Description.SetDefault("Losing life");
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ViruCatDrain = true;
		}
	}
}
