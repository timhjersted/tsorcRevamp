using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Items.Tools;

namespace tsorcRevamp.Buffs
{
    public class MagicShield : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense += MagicShieldScroll.DefenseIncrease;
            if (player.whoAmI == Main.myPlayer)
            {
                Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.Center, player.velocity, ModContent.ProjectileType<Projectiles.MagicShield>(), 0, 0, player.whoAmI);
            }
        }
    }
}