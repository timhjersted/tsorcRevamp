using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.Projectiles.Summon.Runeterra;
using static Humanizer.In;

namespace tsorcRevamp.Buffs.Runeterra.Summon
{
	public class InterstellarCommander : ModBuff
    {
        public override void SetStaticDefaults()
		{
			// DisplayName.SetDefault("Interstellar Commander");
			// Description.SetDefault("You're the commander of these ships!");

			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}
		public override void Update(Player player, ref int buffIndex)
        {
            if (player.HeldItem.type == ModContent.ItemType<ScorchingPoint>())
            {
                player.maxMinions += 1;
            }
            if (player.ownedProjectileCounts[ModContent.ProjectileType<InterstellarVesselShip>()] > 0)
			{
				InterstellarVesselGauntlet.ReposeProjectiles(player);
				player.buffTime[buffIndex] = 18000;
			}
			else
			{
				player.DelBuff(buffIndex);
				buffIndex--;
            }
            if (player.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost)
            {
                player.statMana -= 1;
                player.manaRegenDelay = 10;
            }
        }
	}
}