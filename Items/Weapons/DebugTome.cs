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
using tsorcRevamp.UI;
using System.Collections.Generic;

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

		float radius = 0;
		List<Vector2> activeTiles;
		List<Vector2> nextTiles;
		Rectangle arena = new Rectangle(1557, 1639, 467, 103);
		public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack) {
			tsorcRevampWorld.SuperHardMode = true;
			Main.NewText(player.position / 16);


			float radiusSquared = radius * radius;
			Vector2 centerOver16 = player.Center / 16;
			float circumference = 2 * MathHelper.Pi * radius;



			radius++;
			Main.NewText("Radius: " + radius);
			if (radius == 1)
			{
				activeTiles = new List<Vector2>();
				nextTiles = new List<Vector2>();
				activeTiles.Add(centerOver16);
			}
			else
            {
				Vector2 a = new Vector2(0, 1);
				Vector2 b = new Vector2(1, 0);
				Vector2 c = new Vector2(-1, 0);
				Vector2 d = new Vector2(0, -1);

				for (int i = 0; i < activeTiles.Count; i++)
                {
					Main.tile[(int)activeTiles[i].X, (int)activeTiles[i].Y].liquid = 255;
					if (!nextTiles.Contains(activeTiles[i] + a) && Main.tile[(int)(activeTiles[i] + a).X, (int)(activeTiles[i] + a).Y].liquid != 255)
                    {
						nextTiles.Add(activeTiles[i] + a);
                    }
					if (!nextTiles.Contains(activeTiles[i] + b) && Main.tile[(int)(activeTiles[i] + b).X, (int)(activeTiles[i] + b).Y].liquid != 255)
					{
						nextTiles.Add(activeTiles[i] + b);
					}
					if (!nextTiles.Contains(activeTiles[i] + c) && Main.tile[(int)(activeTiles[i] + c).X, (int)(activeTiles[i] + c).Y].liquid != 255)
					{
						nextTiles.Add(activeTiles[i] + c);
					}
					if (!nextTiles.Contains(activeTiles[i] + d) && Main.tile[(int)(activeTiles[i] + d).X, (int)(activeTiles[i] + d).Y].liquid != 255)
					{
						nextTiles.Add(activeTiles[i] + d);
					}
				}

				activeTiles = nextTiles;
				nextTiles = new List<Vector2>();
			}















			/*if (radius < 10)
			{
				for (int x = arena.Left; x < arena.Right; x++)
				{
					for (int y = arena.Height; y < arena.Bottom; y++)
					{
						Vector2 point = new Vector2(x, y);
						if (Vector2.DistanceSquared(centerOver16, point) < radiusSquared)
						{
							if (!UsefulFunctions.IsTileReallySolid(point))
							{
								Tile thisTile = Main.tile[(int)point.X, (int)point.Y];
								if (thisTile != null)
								{
									thisTile.liquid = 255;
								}
							}
						}
					}
				}
			}*/
			//for (int i = 0; i < circumference; i++)
			//{

			//}
			return false;
		}
        public override bool CanRightClick()
        {
			return true;
        }
        public override void RightClick(Player player)
        {
			radius = 0;
			Main.NewText("Radius Reset!");
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