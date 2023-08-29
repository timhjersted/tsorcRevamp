using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs;

class DarkInferno : ModBuff
{
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Dark Inferno");
        Description.SetDefault("The black flames eat away at your skin");
        Main.debuff[Type] = true;
        Main.pvpBuff[Type] = true;
        Main.buffNoSave[Type] = true;
        BuffID.Sets.LongerExpertDebuff[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        player.GetModPlayer<tsorcRevampPlayer>().DarkInferno = true;
    }
    public override void Update(NPC npc, ref int buffIndex)
    {
        npc.GetGlobalNPC<NPCs.tsorcRevampGlobalNPC>().DarkInferno = true;
    }
}
