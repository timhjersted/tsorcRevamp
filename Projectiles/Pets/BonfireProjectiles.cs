using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Pets {
    internal static class BonfireProjectiles {
        internal static void PiggyBankAI(Projectile proj, int chestType, ref int playerBank, Player p, tsorcRevampPlayer modPlayer) {
            if (Main.gamePaused && !Main.gameMenu) {
                return;
            }
            int num = (int)(p.Center.X / 16.0);
            int num2 = (int)(p.Center.Y / 16.0);
            int num3 = (int)proj.Center.X / 16;
            int num4 = (int)proj.Center.Y / 16;
            int lastTileRangeX = p.lastTileRangeX;
            int lastTileRangeY = p.lastTileRangeY;
            if (num < num3 - lastTileRangeX || num > num3 + lastTileRangeX + 1 || num2 < num4 - lastTileRangeY || num2 > num4 + lastTileRangeY + 1) {
                if (playerBank == proj.whoAmI) {
                    playerBank = -1;
                    modPlayer.chests = false;
                }
            }
            if (p.chest != -1) {
                return;
            }
            else {
                p.noThrow = 2;
                if (PlayerInput.UsingGamepad) {
                    p.GamepadEnableGrappleCooldown();
                }
                int num5 = (int)proj.Center.X / 16;
                int num6 = (int)proj.Center.Y / 16;
                playerBank = proj.whoAmI;
                modPlayer.chests = true;
                p.chest = chestType;
                p.chestX = num5;
                p.chestY = num6;
                p.talkNPC = -1;
                Main.npcShop = 0;
                Recipe.FindRecipes();
            }
        }
    }
    public class SafeProjectile : ModProjectile {
        public override string Texture => "tsorcRevamp/Projectiles/none";
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("");
        }

        public override void SetDefaults() {
            projectile.width = 1;
            projectile.height = 1;
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
                BonfireProjectiles.PiggyBankAI(projectile, -3, ref modPlayer.safe, player, modPlayer);
            }
            if (Main.player[projectile.owner].Distance(projectile.Center) > 100f) {
                projectile.Kill();
            }
        }
    }
    public class PiggyBankProjectile : ModProjectile {
        public override string Texture => "tsorcRevamp/Projectiles/none";
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("");
        }

        public override void SetDefaults() {
            projectile.width = 1;
            projectile.height = 1;
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
                BonfireProjectiles.PiggyBankAI(projectile, -2, ref modPlayer.safe, player, modPlayer);
            }
            if (Main.player[projectile.owner].Distance(projectile.Center) > 100f) {
                projectile.Kill();
            }
        }
    }
}
