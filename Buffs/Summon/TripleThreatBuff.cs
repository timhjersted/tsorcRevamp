using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon
{
	public class TripleThreatBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Triple Threat");
			Description.SetDefault("The Triad will fight for you");

			Main.buffNoSave[Type] = true; // This buff won't save when you exit the world
			Main.buffNoTimeDisplay[Type] = true; // The time remaining won't display on this buff
		}

		public override void Update(Player player, ref int buffIndex)
		{
			// If the minions exist reset the buff time, otherwise remove the buff from the player
			if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.FriendlyRetinazer>()] > 0
				|| player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.FriendlySpazmatism>()] > 0
				|| player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.FriendlyCataluminance>()] > 0)
			{
				player.GetModPlayer<tsorcRevampPlayer>().SetAuraState(tsorcAuraState.Light);
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