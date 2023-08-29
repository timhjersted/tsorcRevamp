using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs;

class CrimsonDrain : ModBuff
{
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Crimson Drain");
        Description.SetDefault("Enemies within a ten tile radius receive Crimson Burn.");
        Main.debuff[Type] = false;
        Main.buffNoTimeDisplay[Type] = false;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        player.GetModPlayer<tsorcRevampPlayer>().CrimsonDrain = true;
    }

}
