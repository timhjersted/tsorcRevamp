using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.Projectiles.Summon.Runeterra;

namespace tsorcRevamp.Buffs.Runeterra
{
	public class InterstellarCommander : ModBuff
    {
        public static bool hascrit2 = false;
        public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Interstellar Commander");
			Description.SetDefault("You're the commander of these ships!");

			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}
		public override void Update(Player player, ref int buffIndex)
		{
      // If the minions exist reset the buff time, otherwise remove the buff from the player
      if (player.ownedProjectileCounts[ModContent.ProjectileType<InterstellarVesselShip>()] > 0)
			{
				// update projectiles
				InterstellarVesselControls.ReposeProjectiles(player);
				player.buffTime[buffIndex] = 18000;
			}
			else
			{
				player.DelBuff(buffIndex);
				buffIndex--;
            }
            if (Main.GameUpdateCount % 60 == 0)
            {
                InterstellarVesselShip.timer2--;
            }

            if (hascrit2)
            {
                InterstellarVesselShip.timer2 = 3;
                hascrit2 = false;
            }
        }
	}
}