using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class CernosPrimeHeld : ModProjectile {

        private int charge;
        private int chargeTimer;

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Bow Holdout");
            Main.projFrames[projectile.type] = 7;
            ProjectileID.Sets.NeedsUUID[projectile.type] = true;
        }

        public override void SetDefaults() {
            projectile.CloneDefaults(ProjectileID.LastPrism); //so the visual bow does no damage
            projectile.width = 48;
            projectile.height = 24;
            projectile.friendly = false;
        }

        public override Color? GetAlpha(Color lightColor) {
            return Color.White;
        }

        public override void AI() {
            Player player = Main.player[projectile.owner];
            Vector2 playerHandPos = player.RotatedRelativePoint(player.MountedCenter);
            //update character visuals while idle
            {
                projectile.Center = playerHandPos;
                projectile.rotation = projectile.velocity.ToRotation() + (float)Math.PI / 2f;
                player.heldProj = projectile.whoAmI;
                player.itemRotation = (projectile.velocity * projectile.direction).ToRotation();
                player.ChangeDir(projectile.direction);
            }
            if (projectile.owner == Main.myPlayer) {
                //update character visuals while aiming
                {
                    Vector2 aimVector = Vector2.Normalize(Main.MouseWorld - playerHandPos);
                    aimVector = Vector2.Normalize(Vector2.Lerp(Vector2.Normalize(projectile.velocity), aimVector, 0.2f)); //taken straight from RedLaserBeam, thanks past me!
                    aimVector *= 18; //projectile max velocity
                    if (aimVector != projectile.velocity) {
                        projectile.netUpdate = true; //update the bow visually to other players when we change aim
                    }
                    projectile.velocity = aimVector;
                }
                bool charging = player.channel && !player.noItems && !player.CCed; //not cursed or frozen, and holding lmb

                if (charging) {
                    chargeTimer++;
                    if ((chargeTimer % 8 == 0) && (chargeTimer <= 48)) { //gain one "charge" every 8 frames, up to max of 6
                        projectile.frame++;
                        charge++;
                    }
                }
                else { 
                    chargeTimer = 0;
                    Vector2 bowVelocity = Vector2.Normalize(projectile.velocity);

                    if (charge != 0) { //dont fire zero-velocity arrows, it looks silly

                        int ammoLocation = 0;
                        int ammoProjectileType = 0;

                        for (int k = 54; k < 58; k++) {
                            if (player.inventory[k].ammo == AmmoID.Arrow && player.inventory[k].stack > 0) {
                                ammoLocation = k;
                                ammoProjectileType = player.inventory[k].shoot;
                                break;
                            }
                        }
                        if (ammoLocation == 0) {
                            for (int j = 0; j < 54; j++) {
                                if (player.inventory[j].ammo == AmmoID.Arrow && player.inventory[j].stack > 0) {
                                    ammoLocation = j;
                                    ammoProjectileType = player.inventory[j].shoot;
                                    break;
                                }
                            }
                        }

                        for (int i = 0; i < 3; i++) {
                            Vector2 inaccuracy = new Vector2(bowVelocity.X, bowVelocity.Y).RotatedByRandom(MathHelper.ToRadians((float)16f - (charge) * 2.5f)); //more accurate when charged

                            Vector2 projectileVelocity = inaccuracy * (18f - (3 * (6 - charge))); //faster arrows when charged, 3 velocity per point of charge

                            

                            if ((ammoLocation != 0) && (player.inventory[ammoLocation].stack > 0)) {
                                Projectile.NewProjectile(projectile.Center, projectileVelocity, ammoProjectileType, projectile.damage, projectile.knockBack, projectile.owner);
                                player.inventory[ammoLocation].stack--;
                                if (player.inventory[ammoLocation].stack == 0) {
                                    player.inventory[ammoLocation].TurnToAir();
                                }
                            }
                        }
                        Main.PlaySound(SoundID.Item5.WithVolume(0.8f), player.position);
                    }
                    charge = 0; //reset the charge
                    projectile.Kill(); //and kill the bow so we dont keep shooting
                }
            }
        }

    }
}
