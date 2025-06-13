using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Tools;

namespace tsorcRevamp.Buffs
{
    public class MagicShield : ModBuff
    {
        public override LocalizedText Description => base.Description.WithFormatArgs(MagicShieldScroll.DefenseIncrease);
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<tsorcRevampPlayer>().magicDefense += MagicShieldScroll.DefenseIncrease;
            if (player.whoAmI == Main.myPlayer)
            {
                Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.Center, player.velocity, ModContent.ProjectileType<Projectiles.MagicShield>(), 0, 0, player.whoAmI);
            }
        }
    }
}