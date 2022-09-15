/*using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ID;

namespace tsorcRevamp.Items.Weapons.Runeterra.Summon
{
	public class CotUBuff3 : ModBuff
	{
		public static bool hascrit3 = false;
		public static int critcounter = 0;
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Center of the Universe");
			Description.SetDefault("You are the Center of the Universe");

			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}
		public override void Update(Player player, ref int buffIndex)
		{
			int dmg = (int)(player.GetTotalDamage(DamageClass.Summon).ApplyTo(150)); //150 is the base damage of CotU

			if (Main.GameUpdateCount % 60 == 0)
			{
				CotUStar3.timer3--;
			}
			if (hascrit3)
			{
				CotUStar3.timer3 = 3;
				hascrit3 = false;
			}
			if (critcounter == 3)
			{
				Projectile.NewProjectile(Projectile.GetSource_None(), player.Center, Vector2.One, ModContent.ProjectileType<CotUEStar>(), dmg, 1f, Main.myPlayer);
				if (Main.GameUpdateCount % 1 == 0)
				{
					critcounter -= 3;
				}
			}
			// If the minions exist reset the buff time, otherwise remove the buff from the player
			if (player.ownedProjectileCounts[ModContent.ProjectileType<CotUStar3>()] > 0)
			{
				player.buffTime[buffIndex] = 18000;
			}
			else
			{
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}
}*/