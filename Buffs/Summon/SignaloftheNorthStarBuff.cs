using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon
{
	public class SignaloftheNorthStarBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("North Star");
			Description.SetDefault("The North Star will fight for you");
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = false;
		}

        public override void Update(Player player, ref int buffIndex)
        {

			int whipDamage = (int)player.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(66); //66 is 66% of the base dmg of Signal of the North Star
			bool NorthStarExists = false;
			for (int i = 0; i < Main.maxProjectiles; i++)
			{
				if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<Projectiles.Summon.Whips.SignaloftheNorthStarNorthStar>() && Main.projectile[i].owner == player.whoAmI)
				{
					NorthStarExists = true;
					break;
				}
			}
			if (!NorthStarExists)
			{
				Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.Center, Vector2.One, ModContent.ProjectileType<Projectiles.Summon.Whips.SignaloftheNorthStarNorthStar>(), whipDamage, 1f, Main.myPlayer);
			}
		}
    }
}
