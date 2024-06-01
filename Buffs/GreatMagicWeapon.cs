using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Items.VanillaItems;

namespace tsorcRevamp.Buffs
{
    public class GreatMagicWeapon : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<tsorcRevampPlayer>().GreatMagicWeapon = true;
            player.GetModPlayer<tsorcRevampPlayer>().ReboundProjectile = true;
        }
    }
}