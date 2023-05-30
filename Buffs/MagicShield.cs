using Terraria;
using Terraria.ModLoader;

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
            player.statDefense += 10;
            Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.Center, player.velocity, ModContent.ProjectileType<Projectiles.MagicShield>(), 0, 0, player.whoAmI);
        }
    }
}