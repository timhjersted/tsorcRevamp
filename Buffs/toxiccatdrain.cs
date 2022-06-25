using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Buffs
{
    public class ToxicCatDrain : ModBuff
    {
        //Generic texture since this buff is enemy-only
        public override string Texture => "tsorcRevamp/Buffs/ArmorDrug";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Toxic Catalyzer Drain");
            Description.SetDefault("Losing life");
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ToxicCatDrain = true;
        }
    }
}
