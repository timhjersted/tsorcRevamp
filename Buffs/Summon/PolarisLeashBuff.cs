using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon
{
	public class PolarisLeashBuff : ModBuff
	{
        public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Polaris");
			Description.SetDefault("Polaris will fight for you");
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = false;
		}

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                int whipDamage = (int)player.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(39); //39 is 66% of the base dmg of Polaris Leash
                if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.Whips.PolarisLeashPolaris>()] == 0)
                {
                    Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.Center, Vector2.One, ModContent.ProjectileType<Projectiles.Summon.Whips.PolarisLeashPolaris>(), whipDamage, 1f, Main.myPlayer);
                }
            }
        }
    }
}
