using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Items.VanillaItems;

namespace tsorcRevamp.Buffs
{
    public class MagicWeapon : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<tsorcRevampPlayer>().MagicWeapon = true;
            player.GetModPlayer<tsorcRevampPlayer>().ReboundProjectile = true;
        }
    }
}