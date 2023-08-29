using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs;

class CrystalMagicWeapon : ModBuff
{
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Crystal Magic Weapon");
        Description.SetDefault("Your weapon is imbued with crystalline magic!");
        Main.debuff[Type] = false;
        Main.buffNoTimeDisplay[Type] = false;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        player.GetModPlayer<tsorcRevampPlayer>().CrystalMagicWeapon = true;
    }
}