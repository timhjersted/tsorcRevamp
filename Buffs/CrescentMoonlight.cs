using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Buffs
{
    public class CrescentMoonlight : ModBuff
    {
        //Generic texture since this buff is enemy-only
        public override string Texture => "tsorcRevamp/Buffs/ArmorDrug";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crescent Moonlight");
            Description.SetDefault("Losing life");
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<tsorcRevampGlobalNPC>().CrescentMoonlight = true;
        }
    }
}
