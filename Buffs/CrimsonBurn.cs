using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Buffs
{
    class CrimsonBurn : ModBuff
    {

        //Generic texture since this buff is enemy-only
        public override string Texture => "tsorcRevamp/Buffs/ArmorDrug";


        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crimson Burn");
            Description.SetDefault("Your flesh is burning");
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<tsorcRevampGlobalNPC>().CrimsonBurn = true;
        }
    }
}
