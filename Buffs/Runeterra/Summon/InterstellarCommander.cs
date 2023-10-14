using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.Projectiles.Summon.Runeterra;

namespace tsorcRevamp.Buffs.Runeterra.Summon
{
    public class InterstellarCommander : ModBuff
    {
        public override void SetStaticDefaults()
		{
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

            if (player.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost && player.statMana > 0)
            {
                player.statMana -= 1;
                player.manaRegenDelay = 10;
            }
            else if (player.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost && player.statMana == 0)
            {
                player.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost = false;
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/InterstellarVessel/BoostDeactivation") with { Volume = InterstellarVesselGauntlet.SoundVolume });
            }
        }
	}
}