using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon;

	public class SamuraiBeetleBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Samurai Beetle");
			Description.SetDefault("The Beetle will fight for you");

			Main.buffNoSave[Type] = true; // This buff won't save when you exit the world
			Main.buffNoTimeDisplay[Type] = true; // The time remaining won't display on this buff
		}

		public override void Update(Player player, ref int buffIndex)
		{
			// If the minions exist reset the buff time, otherwise remove the buff from the player
			if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.SamuraiBeetle>()] > 0)
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