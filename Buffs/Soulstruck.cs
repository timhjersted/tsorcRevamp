using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Buffs;

public class Soulstruck : ModBuff
{
    //Generic texture since this buff is enemy-only
    public override string Texture => "tsorcRevamp/Buffs/ArmorDrug";

    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Soulstruck");
        Description.SetDefault("Will drop 10% more souls if killed while buff is active");
    }

    public override void Update(NPC npc, ref int buffIndex)
    {
        npc.GetGlobalNPC<tsorcRevampGlobalNPC>().Soulstruck = true;
    }
}
