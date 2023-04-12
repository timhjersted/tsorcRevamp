using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class GreatMagicWeapon : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Great Magic Weapon");
            // Description.SetDefault("Your weapon is imbued with powerful magic!");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<tsorcRevampPlayer>().GreatMagicWeapon = true;
        }
    }
}