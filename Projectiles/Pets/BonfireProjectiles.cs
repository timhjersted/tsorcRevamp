using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Pets {
    internal static class BonfireProjectiles {
		internal static void PiggyBankAI(Projectile proj, int itemType, int chestType, ref int playerBank, Player p, tsorcRevampPlayer modPlayer) {
			if (Main.gamePaused && !Main.gameMenu) {
				return;
			}
			Vector2 projPosRelative = proj.position - Main.screenPosition;
			int pTilePosX = (int)(p.Center.X / 16.0);
			int pTilePosY = (int)(p.Center.Y / 16.0);
			int projTilePosX = (int)proj.Center.X / 16;
			int projTilePosY = (int)proj.Center.Y / 16;
			int lastTileRangeX = p.lastTileRangeX;
			int lastTileRangeY = p.lastTileRangeY;
			if (pTilePosX < projTilePosX - lastTileRangeX || pTilePosX > projTilePosX + lastTileRangeX + 1 || pTilePosY < projTilePosY - lastTileRangeY || pTilePosY > projTilePosY + lastTileRangeY + 1) {
				if (playerBank == proj.whoAmI) {
					playerBank = -1;
					modPlayer.chests = false;
				}
			}
			else {
				if (Main.mouseX <= projPosRelative.X || Main.mouseX >= projPosRelative.X + proj.width || Main.mouseY <= projPosRelative.Y || Main.mouseY >= projPosRelative.Y + proj.height) {
					return;
				}
				p.noThrow = 2;
				p.showItemIcon = true;
				p.showItemIcon2 = itemType;
				if (PlayerInput.UsingGamepad) {
					p.GamepadEnableGrappleCooldown();
				}
				if (!Main.mouseRight || !Main.mouseRightRelease || Player.StopMoneyTroughFromWorking != 0) {
					return;
				}
				Main.mouseRightRelease = false;
				if (p.chest == chestType) {
					Main.PlaySound(p.chest == -2 ? SoundID.Item59 : new Terraria.Audio.LegacySoundStyle(SoundID.CoinPickup, 0));
					p.chest = -1;
					Recipe.FindRecipes();
					return;
				}
				bool flag = false;
				pTilePosX = ((p.SpawnX == -1) ? Main.spawnTileX : p.SpawnX);
				pTilePosY = ((p.SpawnY == -1) ? Main.spawnTileY : p.SpawnY);
				if (!SolidTile(projTilePosX, projTilePosY)) {
					for (int i = 0; i < Main.maxTilesX; i++) {
						for (int j = 0; j < Main.maxTilesY; j++) {
							if (pTilePosX - i > 40 && pTilePosY + j < Main.maxTilesY - 40 && SolidTile(pTilePosX - i, pTilePosY + j)) {
								projTilePosX = pTilePosX - i;
								projTilePosY = pTilePosY + j;
								flag = true;
								break;
							}
							if (pTilePosX + i < Main.maxTilesX - 40 && pTilePosY + j < Main.maxTilesY - 40 && SolidTile(pTilePosX + i, pTilePosY + j)) {
								projTilePosX = pTilePosX + i;
								projTilePosY = pTilePosY + j;
								flag = true;
								break;
							}
							if (pTilePosX + i < Main.maxTilesX - 40 && pTilePosY - j > 40 && SolidTile(pTilePosX + i, pTilePosY - j)) {
								projTilePosX = pTilePosX + i;
								projTilePosY = pTilePosY - j;
								flag = true;
								break;
							}
							if (pTilePosX - i > 40 && pTilePosY - j > 40 && SolidTile(pTilePosX - i, pTilePosY - j)) {
								projTilePosX = pTilePosX - i;
								projTilePosY = pTilePosY - j;
								flag = true;
								break;
							}
						}
						if (flag) {
							break;
						}
					}
				}
				playerBank = proj.whoAmI;
				modPlayer.chests = true;
				p.chest = chestType;
				p.chestX = projTilePosX;
				p.chestY = projTilePosY;
				p.talkNPC = -1;
				Main.npcShop = 0;
				Main.playerInventory = true;
				Main.PlaySound(p.chest == -2? SoundID.Item59 : new Terraria.Audio.LegacySoundStyle(SoundID.CoinPickup, 0));
				Recipe.FindRecipes();
			}
		}
		internal static bool SolidTile(int x, int y) {
			Tile tile = Main.tile[x, y];
			if (!(tile == null) && tile.active() && Main.tileSolid[tile.type] && !Main.tileSolidTop[tile.type] && !tile.halfBrick() && tile.slope() == 0) {
				return !tile.inActive();
			}
			return false;
		}
	}
	public class SafeProjectile : ModProjectile {
		public override void SetStaticDefaults() => DisplayName.SetDefault("");

        public override void SetDefaults() {
            projectile.width = 32;
            projectile.height = 32;
			projectile.aiStyle = 97;
			projectile.tileCollide = false;
			projectile.timeLeft = 10800;
		}
		public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI) {
			drawCacheProjsOverWiresUI.Add(projectile.whoAmI);
		}
		public override void PostAI() {
			if (Main.netMode != NetmodeID.Server) {
				Player player = Main.player[Main.myPlayer];
				tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
				BonfireProjectiles.PiggyBankAI(projectile, ItemID.Safe, -3, ref modPlayer.safe, player, modPlayer);
			}
		}
	}
	public class PiggyBankProjectile : ModProjectile {
		public override void SetStaticDefaults() => DisplayName.SetDefault("");

		public override void SetDefaults() {
			projectile.width = 32;
			projectile.height = 32;
			projectile.aiStyle = 97;
			projectile.tileCollide = false;
			projectile.timeLeft = 10800;
		}
		public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI) {
            drawCacheProjsOverWiresUI.Add(projectile.whoAmI);
        }
        public override void PostAI() {
            if (Main.netMode != NetmodeID.Server) {
                Player player = Main.player[Main.myPlayer];
                tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
                BonfireProjectiles.PiggyBankAI(projectile, ItemID.PiggyBank, -2, ref modPlayer.safe, player, modPlayer);
            }
        }
    }
}
