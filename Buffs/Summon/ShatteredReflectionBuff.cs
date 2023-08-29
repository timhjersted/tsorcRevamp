using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon;

	public class ShatteredReflectionBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Shattered Reflection");
			Description.SetDefault("Fragments of your soul haunt your enemies");

			Main.buffNoSave[Type] = true; // This buff won't save when you exit the world
			Main.buffNoTimeDisplay[Type] = true; // The time remaining won't display on this buff
		}

		public override void Update(Player player, ref int buffIndex)
		{
			// If the minions exist reset the buff time, otherwise remove the buff from the player
			if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.ShatteredReflectionProjectile>()] > 0)
			{
				player.buffTime[buffIndex] = 18000;
				player.GetModPlayer<tsorcRevampPlayer>().SetAuraState(tsorcAuraState.Nebula);
			}
			else
			{
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}