/*using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Runeterra.Summon
{
	public class CotUBuff2 : ModBuff
	{
		public static bool hascrit2 = false;
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Center of the Mechanism");
			Description.SetDefault("A mechanism is centered around you");

			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}
		public override void Update(Player player, ref int buffIndex)
		{
			if (Main.GameUpdateCount % 60 == 0)
			{
				CotUStar2.timer2--;
			}

			if (hascrit2)
            {
				CotUStar2.timer2 = 3;
				hascrit2 = false;
            }

			// If the minions exist reset the buff time, otherwise remove the buff from the player
			if (player.ownedProjectileCounts[ModContent.ProjectileType<CotUStar2>()] > 0)
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