using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon
{
	public class FrigidEnchantmentBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Polar Star");
			Description.SetDefault("The Polar Star will fight for you");
			Main.debuff[Type] = false;
			Main.buffNoTimeDisplay[Type] = false;
		}
        public override void Update(Player player, ref int buffIndex)
        {
			int whipDamage = (int)player.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(52); //48 is the base dmg of the Enchanted Whip
			bool CoolWhipProjectileExists = false;
			for (int i = 0; i < Main.maxProjectiles; i++)
			{
				if (Main.projectile[i].active && Main.projectile[i].type == ProjectileID.CoolWhipProj && Main.projectile[i].owner == player.whoAmI)
				{
					CoolWhipProjectileExists = true;
					break;
				}
			}

			if (!CoolWhipProjectileExists)
			{
				Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.Center, Vector2.One, ProjectileID.CoolWhipProj, whipDamage, 1f, Main.myPlayer, ProjAIStyleID.CoolFlake, ProjAIStyleID.CoolFlake);
			}
		}
    }
}
