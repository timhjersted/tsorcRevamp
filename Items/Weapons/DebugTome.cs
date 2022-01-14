using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using System;
using System.Net;
using Newtonsoft.Json;
using System.Threading;

namespace tsorcRevamp.Items.Weapons {
	public class DebugTome : ModItem {
		public override void SetStaticDefaults() {
			Tooltip.SetDefault("You should not have this" +
				"\nDev item used for testing purposes only" +
				"\nUsing this may cause irreversible effects on your world");
		}
		
		public override void SetDefaults() {
			item.damage = 999999;
			item.knockBack = 4;
			item.crit = 4;
			item.width = 30;
			item.height = 30;
			item.useTime = 20;
			item.useAnimation = 20;
			item.UseSound = SoundID.Item11;
			item.useTurn = true;
			item.noMelee = true;
			item.magic = true;
			item.autoReuse = true;
			item.value = 10000;
			item.useStyle = ItemUseStyleID.HoldingOut;
			item.rare = ItemRarityID.Green;
			item.shootSpeed = 24f;
			item.shoot = ModContent.ProjectileType<Projectiles.BlackFirelet>();
		}

		public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack) {
			tsorcRevampWorld.SuperHardMode = true;
			Main.NewText(player.position / 16);

			new TestCutscene(player).Start();
			
			for (float i = 0.1f; i < 19; i *= 1.01f)
			{
				Vector2 trajectory = UsefulFunctions.BallisticTrajectory(player.Center, Main.MouseWorld, i, (9.8f / 60), false, false);
				if(trajectory != Vector2.Zero)
				{
					trajectory += player.velocity;
					Projectile.NewProjectile(player.Center, trajectory, ModContent.ProjectileType<Projectiles.IdealArrow>(), damage, knockBack, Main.myPlayer);
					i++; //Just to keep this from getting out of hand
				}
				trajectory = UsefulFunctions.BallisticTrajectory(player.Center, Main.MouseWorld, i, (9.8f / 60), true, false);
				if (trajectory != Vector2.Zero)
				{
					trajectory += player.velocity;
					Projectile.NewProjectile(player.Center, trajectory, ModContent.ProjectileType<Projectiles.IdealArrow>(), damage, knockBack, Main.myPlayer);
					i++;
				}
			}
			return false;
		}

		//For multiplayer testing, because I only have enough hands for one keyboard. Makes the player holding it float vaguely near the next other player.
		public override void UpdateInventory(Player player)
		{
			if (player.name == "MPTestDummy")
			{
				if (player.whoAmI == 0)
				{
					if (Main.player[1] != null && Main.player[1].active)
					{
						player.position = Main.player[1].position;
						player.position.X += 300;
						player.position.Y += 300;
					}
				}
				else
				{
					if (Main.player[0] != null && Main.player[0].active)
					{
						player.position = Main.player[0].position;
						player.position.X += 300;
						player.position.Y += 300;
					}
				}
			}
        }

        public override bool CanUseItem(Player player)
        {
			if (player.name == "Zeodexic" || player.name.Contains("Sam") || player.name == "Chroma TSORC test")
			{
				return true;
			}
			return false;
		}
	}
}