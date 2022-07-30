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

			int whipDamage = (int)player.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(39); //39 is 66% of the base dmg of Polaris Leash
			bool PolarisExists = false;
			for (int i = 0; i < Main.maxProjectiles; i++)
			{
				if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<Projectiles.Summon.Whips.PolarisLeashPolaris>() && Main.projectile[i].owner == player.whoAmI)
				{
					PolarisExists = true;
					break;
				}
			}
			if (!PolarisExists)
			{
				Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.Center, Vector2.One, ModContent.ProjectileType<Projectiles.Summon.Whips.PolarisLeashPolaris>(), whipDamage, 1f, Main.myPlayer);
			}
		}
    }
}
