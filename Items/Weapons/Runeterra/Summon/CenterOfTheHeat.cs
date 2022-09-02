using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Runeterra.Summon
{
	public class CenterOfTheHeat : ModBuff
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Center of the Heat");
			Description.SetDefault("You're in a hot center");

			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}
		public override void Update(Player player, ref int buffIndex)
		{
      // If the minions exist reset the buff time, otherwise remove the buff from the player
      if (player.ownedProjectileCounts[ModContent.ProjectileType<ScorchingPointStar>()] > 0)
			{
				// update projectiles
				ScorchingPoint.ReposeProjectiles(player);
				player.buffTime[buffIndex] = 18000;
			}
			else
			{
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}
}