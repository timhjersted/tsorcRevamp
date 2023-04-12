using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class MagicWeapon : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Magic Weapon");
            // Description.SetDefault("Your weapon is imbued with magic!");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<tsorcRevampPlayer>().MagicWeapon = true;
        }
    }
}