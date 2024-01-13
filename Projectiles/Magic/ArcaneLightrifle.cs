using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Magic
{
    class ArcaneLightrifle : ModProjectile
    {
        public static float MaxCharge = 45f;

        public override void SetStaticDefaults()
        {
            //Signals to Terraria that this projectile requires a unique identifier beyond its index in the projectile array.
            ProjectileID.Sets.NeedsUUID[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.LastPrism);
            Projectile.DamageType = DamageClass.Magic;
            Projectile.friendly = false;
            Projectile.hostile = false;
        }

        int charge = 0;
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            //Stop the BotC player from using the Glaive Beam if they have either 120 stamina or are full (ensures they can still use it even if they don't have stamina vessels)
            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < 30)
            {
                player.channel = false;

                Projectile.Kill();
                return;
            }
            Vector2 rrp = player.RotatedRelativePoint(player.MountedCenter, true);

            if (Projectile.owner == Main.myPlayer)
            {
                UpdateAim(rrp, player.HeldItem.shootSpeed);

                bool stillInUse = player.channel;

                if (player.noItems || player.CCed || player.statMana < (int)(50 * player.manaCost))
                {
                    stillInUse = false;
                }

                if (!stillInUse)
                {
                    Projectile.Kill();
                    return;
                }
            }


            MaxCharge = 10;
            float trueChargeTime = (MaxCharge * (player.HeldItem.useTime / 5f));
            charge++;

            UpdatePlayerVisuals(player, rrp);



            //If the charge is negative, that means we're in the "firing" state
            if (charge < trueChargeTime)
            {
                float radius = (trueChargeTime - charge) / 3f;
                radius = ((radius * radius) / 4) + 64;

                for (int j = 0; j < 50f * (charge / trueChargeTime) * (charge / trueChargeTime); j++)
                {
                    Vector2 dir = Projectile.velocity;
                    dir.Normalize();
                    dir *= radius;
                    dir = dir.RotatedBy(Main.rand.NextFloat(-MathHelper.PiOver2 / 6f, MathHelper.PiOver2 / 6f));
                    Vector2 dustPos = player.Center + dir;
                    Vector2 dustVel = new Vector2(3, 0).RotatedBy(dir.ToRotation() + MathHelper.Pi);
                    Dust d = Dust.NewDustPerfect(dustPos, 174, dustVel, 200, default, 2f);
                    d.noGravity = true;
                    d.shader = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.CyanGradientDye), Main.LocalPlayer);
                }
                for (int j = 0; j < 5f * (charge / trueChargeTime) * (charge / trueChargeTime); j++)
                {
                    Vector2 dir = Main.rand.NextVector2CircularEdge(radius, radius);
                    Vector2 dustPos = player.Center + dir;
                    Vector2 dustVel = new Vector2(3, 0).RotatedBy(dir.ToRotation() + MathHelper.Pi);
                    Dust.NewDustPerfect(dustPos, DustID.MagicMirror, Vector2.Zero, 200, default, 0.75f).noGravity = true;
                }
            }
            else
            {
                //Consume mana etc
                charge = 0;
                player.statMana -= (int)(50 * player.manaCost);
                if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
                {
                    player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= 30;
                    if (player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < 0)
                    {
                        player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent = 0;
                    }
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45);

                //Calculate its collision point, then spawn a laser with the length from the projectiles center to that collision point
                Vector2 collision1 = UsefulFunctions.GetFirstCollision(player.Center, Projectile.velocity, 5000, true, true);
                Vector2 colVel1 = collision1 - player.Center;
                colVel1.Normalize();

                if (Main.myPlayer == Projectile.owner)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), player.Center, colVel1, ModContent.ProjectileType<LightrifleFire>(), Projectile.damage, 0, Projectile.owner, colVel1.Length(), 0);
                }

                Vector2 testCol1 = Vector2.Zero;
                Vector2 testCol2 = Vector2.Zero;

                Vector2 reflectionA = colVel1;
                reflectionA.Y *= -1;
                Vector2 reflectionB = colVel1;
                reflectionB.X *= -1;

                //Calcuate how the lasers should reflect
                //If it's aiming up right or down left
                if (colVel1.X >= 0 == colVel1.Y >= 0)
                {
                    //Run test collisions aiming up left and down right to see which direction is longer
                    testCol1 = UsefulFunctions.GetFirstCollision(collision1, reflectionA, 5000, true, true);
                    testCol2 = UsefulFunctions.GetFirstCollision(collision1, reflectionB, 5000, true, true);
                }
                //And vice versa with quadrants 2/4 and 1/3
                else if (colVel1.X < 0 == colVel1.Y > 0)
                {
                    testCol1 = UsefulFunctions.GetFirstCollision(collision1, reflectionA, 5000, true, true);
                    testCol2 = UsefulFunctions.GetFirstCollision(collision1, reflectionB, 5000, true, true);
                }

                Vector2 collision2;
                if (collision1.DistanceSQ(testCol1) > collision1.DistanceSQ(testCol2))
                {
                    collision2 = testCol1;
                }
                else
                {
                    collision2 = testCol2;
                }

                Vector2 colVel2 = collision2 - collision1;
                colVel2.Normalize();
                if (Main.myPlayer == Projectile.owner)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), collision1, colVel2, ModContent.ProjectileType<LightrifleFire>(), (int)(Projectile.damage * 2f), 0, Projectile.owner, colVel2.Length(), 1);
                }

                //And do it again for the next reflection:
                Vector2 reflectionC = colVel2;
                reflectionC.Y *= -1;
                Vector2 reflectionD = colVel2;
                reflectionD.X *= -1;

                if (colVel2.X >= 0 == colVel2.Y >= 0)
                {
                    //Run test collisions aiming up left and down right to see which direction is longer
                    testCol1 = UsefulFunctions.GetFirstCollision(collision2, reflectionC, 5000, true, true);
                    testCol2 = UsefulFunctions.GetFirstCollision(collision2, reflectionD, 5000, true, true);
                }
                //And vice versa with quadrants 2/4 and 1/3
                else if (colVel2.X < 0 == colVel2.Y > 0)
                {
                    testCol1 = UsefulFunctions.GetFirstCollision(collision2, reflectionC, 5000, true, true);
                    testCol2 = UsefulFunctions.GetFirstCollision(collision2, reflectionD, 5000, true, true);
                }

                Vector2 collision3;
                if (collision2.DistanceSQ(testCol1) > collision2.DistanceSQ(testCol2))
                {
                    collision3 = testCol1;
                }
                else
                {
                    collision3 = testCol2;
                }

                Vector2 colVel3 = collision3 - collision2;
                colVel3.Normalize();

                if (Main.myPlayer == Projectile.owner)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), collision2, colVel3, ModContent.ProjectileType<LightrifleFire>(), (int)(Projectile.damage * 3f), 0, Projectile.owner, colVel3.Length(), 2);
                }
            }




            Projectile.timeLeft = 2;
        }



        public override bool? CanDamage()
        {
            return false;
        }

        //Moves the centerpoint of the beam so it's always aligned with the sprite
        //Was there a better way to do this? Probably. This works though.
        private Vector2 GetOrigin()
        {
            Vector2 origin = Main.player[Projectile.owner].Center;
            origin.X -= 5 * Main.player[Projectile.owner].direction;
            origin.Y -= 15;

            if (Main.player[Projectile.owner].itemRotation < 0)
            {
                //If aiming in the upper right quadrant
                if (Main.player[Projectile.owner].direction == 1)
                {
                    origin.X -= 10 * (float)Math.Cos(Math.Abs(Main.player[Projectile.owner].itemRotation));
                    origin.Y -= -10 * (float)Math.Sin(Math.Abs(Main.player[Projectile.owner].itemRotation));
                }
                //Bottom left
                else
                {
                    origin.X -= 5 + -10 * (float)Math.Cos(Math.Abs(Main.player[Projectile.owner].itemRotation));
                    origin.Y += -10 * (float)Math.Sin(Math.Abs(Main.player[Projectile.owner].itemRotation));
                }
            }
            else
            {
                //Bottom right
                if (Main.player[Projectile.owner].direction == 1)
                {
                    origin.X += 6 + -11 * (float)Math.Cos(Math.Abs(Main.player[Projectile.owner].itemRotation));
                    origin.Y += -10 * (float)Math.Sin(Math.Abs(Main.player[Projectile.owner].itemRotation));
                }
                //Upper left
                else
                {
                    origin.X += 10 * (float)Math.Cos(Math.Abs(Main.player[Projectile.owner].itemRotation));
                    origin.Y -= -10 * (float)Math.Sin(Math.Abs(Main.player[Projectile.owner].itemRotation));
                }
            }
            return origin;
        }

        private void UpdatePlayerVisuals(Player player, Vector2 playerHandPos)
        {
            Vector2 origin = GetOrigin();
            Projectile.Center = new Vector2(origin.X + (205 * player.direction), origin.Y - 15);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.spriteDirection = Projectile.direction;

            player.ChangeDir(Projectile.direction);
            //player.heldProj = projectile.whoAmI;

            if (player.altFunctionUse == 2)
            {
                player.itemTime = 2;
                player.itemAnimation = 2;
            }
            player.itemRotation = (Projectile.velocity * Projectile.direction).ToRotation();
        }


        private void UpdateAim(Vector2 source, float speed)
        {
            Vector2 aim = Vector2.Normalize(Main.MouseWorld - source);
            if (aim.HasNaNs())
            {
                aim = -Vector2.UnitY;
            }

            aim = Vector2.Normalize(Vector2.Lerp(Vector2.Normalize(Projectile.velocity), aim, 1));
            aim *= speed;

            if (aim != Projectile.velocity)
            {
                Projectile.netUpdate = true;
            }
            Projectile.velocity = aim;
        }

        Vector2 dustVel(Vector2 source)
        {

            //Pick an angle in the first quadrant (0 - 90 degrees)
            float angle = Main.rand.NextFloat(0, MathHelper.PiOver2);

            //Modify the speed of the projectile based on it
            float speed = Math.Abs((angle / (MathHelper.PiOver4)) - 1f);

            //Since this pattern is symmetrical on both axises, we can just have a 50% chance to flip it on the x-axis
            if (Main.rand.NextBool())
            {
                angle += MathHelper.PiOver2;
            }

            //And another 50% chance to flip it on the y-axis
            if (Main.rand.NextBool())
            {
                angle += MathHelper.Pi;
            }
            if (Main.rand.NextBool(2))
            {

                //Add some variation
                if (Main.rand.NextBool())
                {
                    speed = Main.rand.NextFloat(0, speed);
                }
            }

            //Create the second smaller loop
            else
            {
                angle += MathHelper.PiOver4;
                speed /= 1.6f;
            }

            //Smooth out the curves slightly
            speed = (float)Math.Pow(speed, 0.9f);

            return new Vector2(speed * 5, 0).RotatedBy(angle);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }

}
