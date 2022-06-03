using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class SagittariusBowHeld : ModProjectile
    {

        private int charge;
        private int chargeTimer;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Bow Holdout");
            Main.projFrames[Projectile.type] = 7;
            ProjectileID.Sets.NeedsUUID[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.LastPrism); //so the visual bow does no damage
            Projectile.width = 50;
            Projectile.height = 12;
            Projectile.friendly = false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Color.White;
        }

        public override void AI()
        {
            const int MAX_CHARGE_COUNT = 6;
            Player player = Main.player[Projectile.owner];
            Vector2 playerHandPos = player.RotatedRelativePoint(player.MountedCenter);
            //update character visuals while idle
            {
                Projectile.Center = playerHandPos;
                Projectile.rotation = Projectile.velocity.ToRotation() + (float)Math.PI / 2f;
                player.heldProj = Projectile.whoAmI;
                player.itemRotation = (Projectile.velocity * Projectile.direction).ToRotation();
                player.ChangeDir(Projectile.direction);
            }
            if (Projectile.owner == Main.myPlayer)
            {
                //update character visuals while aiming
                {
                    Vector2 aimVector = Vector2.Normalize(Main.MouseWorld - playerHandPos);
                    aimVector = Vector2.Normalize(Vector2.Lerp(Vector2.Normalize(Projectile.velocity), aimVector, 0.6f)); //taken straight from RedLaserBeam, thanks past me!
                    aimVector *= 24;
                    if (aimVector != Projectile.velocity)
                    {
                        Projectile.netUpdate = true; //update the bow visually to other players when we change aim
                    }
                    Projectile.velocity = aimVector;
                }
                bool charging = player.channel && !player.noItems && !player.CCed; //not cursed or frozen, and holding lmb
                int maxChargeTime; //for modifying the max charge time based on prefix


                if ((player.HeldItem.useTime + 1) % MAX_CHARGE_COUNT == 0)
                { //for rounding up
                    maxChargeTime = player.HeldItem.useTime + 1;
                }
                else if ((player.HeldItem.useTime + 2) % MAX_CHARGE_COUNT == 0)
                { //for rounding up
                    maxChargeTime = player.HeldItem.useTime + 2;
                }
                else maxChargeTime = player.HeldItem.useTime - (player.HeldItem.useTime % MAX_CHARGE_COUNT); //round down if 3, 4, or 5

                int chargeInterval = maxChargeTime / MAX_CHARGE_COUNT;

                if (charging)
                {
                    chargeTimer++;
                    if ((chargeTimer % chargeInterval == 0) && (chargeTimer <= maxChargeTime))
                    { //gain one charge every chargeInterval frames, up to max of MAX_CHARGE_COUNT
                        Projectile.frame++;
                        charge++;
                    }
                }
                else
                {
                    chargeTimer = 0;
                    Vector2 bowVelocity = Vector2.Normalize(Projectile.velocity);

                    if (charge != 0)
                    { //dont fire zero-velocity arrows, it looks silly

                        int ammoLocation = 0;
                        int ammoProjectileType = 0;

                        FindAmmo(player, ref ammoLocation, ref ammoProjectileType);

                        for (int i = 0; i < 2; i++)
                        {
                            Vector2 inaccuracy = new Vector2(bowVelocity.X, bowVelocity.Y).RotatedByRandom(MathHelper.ToRadians((float)16f - (charge) * 2.5f)); //more accurate when charged

                            Vector2 projectileVelocity = inaccuracy * (1 + ((player.HeldItem.shootSpeed / 27) * (float)(Math.Pow((Math.Floor((double)charge)), 2))));

                            if ((ammoLocation != 0) && (player.inventory[ammoLocation].stack > 0))
                            {
                                //the projectile damage math has to be cast like this for it to work! it wont work if you just cast the result! do not change it! 
                                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, projectileVelocity, ammoProjectileType, (int)(Projectile.damage * ((float)charge / (float)MAX_CHARGE_COUNT)), Projectile.knockBack, Projectile.owner);
                                if (player.inventory[ammoLocation].type != ItemID.EndlessQuiver)
                                {
                                    player.inventory[ammoLocation].stack--;
                                    if (player.inventory[ammoLocation].stack == 0)
                                    {
                                        player.inventory[ammoLocation].TurnToAir();
                                    }
                                }
                            }
                        }
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.8f }, player.position);
                    }
                    charge = 0; //reset the charge
                    Projectile.Kill(); //and kill the bow so we dont keep shooting
                }
            }
        }


        private void FindAmmo(Player player, ref int ammoLocation, ref int ammoProjectileType)
        {
            for (int k = 54; k < 58; k++)
            {
                if (player.inventory[k].ammo == AmmoID.Arrow && player.inventory[k].stack > 0)
                {
                    ammoLocation = k;
                    ammoProjectileType = player.inventory[k].shoot;
                    break;
                }
            }
            if (ammoLocation == 0)
            {
                for (int j = 0; j < 54; j++)
                {
                    if (player.inventory[j].ammo == AmmoID.Arrow && player.inventory[j].stack > 0)
                    {
                        ammoLocation = j;
                        ammoProjectileType = player.inventory[j].shoot;
                        break;
                    }
                }
            }
        }
    }
}
