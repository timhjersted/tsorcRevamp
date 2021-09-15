using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Pets {
    public abstract class BonfireProjectiles : ModProjectile {

        public abstract int ChestType { get; }
        public abstract int ItemType { get; }
        public abstract void SetWhoAmI(tsorcRevampPlayer player, int value);

        public abstract LegacySoundStyle UseSound { get; }

        public override void AI() {
            if (projectile.ai[0] == 0f) {
                if (projectile.velocity.Length() < 0.1) {
                    projectile.velocity.X = 0f;
                    projectile.velocity.Y = 0f;
                    projectile.ai[0] = 1f;
                    projectile.ai[1] = 45f;
                    return;
                }

                projectile.velocity *= 0.94f;
                if (projectile.velocity.X < 0f) {
                    projectile.direction = -1;
                }
                else {
                    projectile.direction = 1;
                }

                projectile.spriteDirection = projectile.direction;
                return;
            }

            if (Main.player[projectile.owner].Center.X < projectile.Center.X) {
                projectile.direction = -1;
            }
            else
                projectile.direction = 1;

            projectile.spriteDirection = projectile.direction;
            projectile.ai[1] += 1f;
            float acceleration = 0.005f;
            if (projectile.ai[1] > 0f) {
                projectile.velocity.Y -= acceleration;
            }
            else {
                projectile.velocity.Y += acceleration;
            }

            if (projectile.ai[1] >= 90f) {
                projectile.ai[1] *= -1f;
            }

            StorageProjectileAI(projectile);
        }

        internal void StorageProjectileAI(Projectile proj) {
            Player p = Main.LocalPlayer;
            if (Main.gamePaused && !Main.gameMenu) {
                return;
            }
            Vector2 projPosRelative = proj.position - Main.screenPosition;
            if (!(Main.mouseX > projPosRelative.X) || !(Main.mouseX < projPosRelative.X + proj.width) || !(Main.mouseY > projPosRelative.Y) || !(Main.mouseY < projPosRelative.Y + proj.height)) {
                return;
            }

            int pTilePosX = (int)(p.Center.X / 16.0);
            int pTilePosY = (int)(p.Center.Y / 16.0);
            int projTilePosX = (int)proj.Center.X / 16;
            int projTilePosY = (int)proj.Center.Y / 16;
            int lastTileRangeX = p.lastTileRangeX;
            int lastTileRangeY = p.lastTileRangeY;
            if (pTilePosX < projTilePosX - lastTileRangeX || pTilePosX > projTilePosX + lastTileRangeX + 1 || pTilePosY < projTilePosY - lastTileRangeY || pTilePosY > projTilePosY + lastTileRangeY + 1) {
                return;
            }


            p.noThrow = 2;
            p.showItemIcon = true;
            p.showItemIcon2 = ItemType;
            if (PlayerInput.UsingGamepad) {
                p.GamepadEnableGrappleCooldown();
            }
            if (!Main.mouseRight || !Main.mouseRightRelease || Player.StopMoneyTroughFromWorking != 0) {
                return;
            }
            Main.mouseRightRelease = false;
            p.tileInteractAttempted = true;
            p.tileInteractionHappened = true;
            p.releaseUseTile = false;
            if (p.chest == ChestType) {
                Main.PlaySound(UseSound);
                p.chest = -1;
                SetWhoAmI(p.GetModPlayer<tsorcRevampPlayer>(), -1);
                Recipe.FindRecipes();
                return;
            }

            SetWhoAmI(p.GetModPlayer<tsorcRevampPlayer>(), proj.whoAmI);

            p.chest = ChestType;
            p.chestX = projTilePosX;
            p.chestY = projTilePosY;
            p.talkNPC = -1;
            Main.npcShop = 0;
            Main.playerInventory = true;
            Main.PlaySound(UseSound);
            Recipe.FindRecipes();


        }
    }
    public class SafeProjectile : BonfireProjectiles {
        public override int ChestType => -3;
        public override int ItemType => ItemID.Safe;
        public override LegacySoundStyle UseSound => SoundID.Item37;

        public override void SetWhoAmI(tsorcRevampPlayer player, int value) => player.chestBank = value;

        public override void SetDefaults() {
            projectile.width = 24;
            projectile.height = 24;
            projectile.tileCollide = false;
            projectile.timeLeft = 10800;
        }

    }
    public class PiggyBankProjectile : BonfireProjectiles {
        public override int ChestType => -2;
        public override int ItemType => ItemID.PiggyBank;
        public override LegacySoundStyle UseSound => SoundID.Item59;

        public override void SetWhoAmI(tsorcRevampPlayer player, int value) => player.chestPiggy = value;

        public override void SetDefaults() {
            projectile.height = 16;
            projectile.width = 24;
            projectile.tileCollide = false;
            projectile.timeLeft = 10800;
        }
    }
}
