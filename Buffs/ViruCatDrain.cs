using tsorcRevamp.NPCs;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
	public class ViruCatDrain : ModBuff
	{
		public override bool Autoload(ref string name, ref string texture)
		{
			// NPC only buff so we'll just assign it a useless buff icon.
			texture = "tsorcRevamp/Buffs/ArmorDrug";
			return base.Autoload(ref name, ref texture);
		}

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
