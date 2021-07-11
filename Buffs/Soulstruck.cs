using tsorcRevamp.NPCs;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
	public class Soulstruck : ModBuff
	{
		public override bool Autoload(ref string name, ref string texture)
		{
			// NPC only buff so we'll just assign it a useless buff icon.
			texture = "tsorcRevamp/Buffs/ArmorDrug";
			return base.Autoload(ref name, ref texture);
		}

		public override void SetDefaults()
		{
			DisplayName.SetDefault("Soulstruck");
			Description.SetDefault("Will drop 10% more souls if killed while buff is active");
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			npc.GetGlobalNPC<tsorcRevampGlobalNPC>().Soulstruck = true;
		}
	}
}
