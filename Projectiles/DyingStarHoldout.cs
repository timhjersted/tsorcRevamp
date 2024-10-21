using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class DyingStarHoldout : ModProjectile
    {
        public static float MaxCharge = 60f;

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

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Daybreak, 300);
        }

        int charge = 0;
        int altFunctionTimer = 0;
        public override void AI()
        {
            altFunctionTimer++;
            Player player = Main.player[Projectile.owner];
            Vector2 rrp = player.RotatedRelativePoint(player.MountedCenter, true);
            float trueChargeTime = (MaxCharge * (player.HeldItem.useTime / 5f));
            charge++;
            //If the charge is negative, that means we're in the "firing" state
            if (player.altFunctionUse != 2)
            {
                if (charge < trueChargeTime)
                {

                    float radius = (trueChargeTime - charge) / 3f;
                    radius = ((radius * radius) / 4) + 64;



                    for (int j = 0; j < 50f * (charge / trueChargeTime) * (charge / trueChargeTime); j++)
                    {
                        Vector2 dir = Main.rand.NextVector2CircularEdge(radius, radius);
                        Vector2 dustPos = player.Center + dir;
                        Vector2 dustVel = new Vector2(3, 0).RotatedBy(dir.ToRotation() + MathHelper.Pi);
                        Dust.NewDustPerfect(dustPos, DustID.InfernoFork, Vector2.Zero, 200, default, 0.75f).noGravity = true;
                    }
                    for (int j = 0; j < 5f * (charge / trueChargeTime) * (charge / trueChargeTime); j++)
                    {
                        Vector2 dir = Main.rand.NextVector2CircularEdge(radius, radius);
                        Vector2 dustPos = player.Center + dir;
                        Vector2 dustVel = new Vector2(3, 0).RotatedBy(dir.ToRotation() + MathHelper.Pi);
                        Dust.NewDustPerfect(dustPos, DustID.Flare, Vector2.Zero, 200, default, 0.75f).noGravity = true;
                    }
                }
                else
                {
                    charge = 0;
                    Vector2 collision = UsefulFunctions.GetFirstCollision(player.Center, Projectile.velocity, 5000, true);

                    Vector2 diff = collision - player.Center;
                    float length = diff.Length();
                    diff.Normalize();
                    Vector2 offset = Vector2.Zero;

                    for (int i = 0; i < 50; i++)
                    {
                        Dust.NewDustPerfect(player.Center, DustID.InfernoFork, Main.rand.NextVector2Circular(6, 6), 200, default, 2f).noGravity = true;
                    }

                    for (int i = 0; i < 50; i++)
                    {
                        Dust.NewDustPerfect(collision, DustID.InfernoFork, Main.rand.NextVector2Circular(6, 6), 200, default, 2f).noGravity = true;
                    }

                    for (int i = 0; i < length - 64; i++)
                    {
                        offset += diff;
                        if (Main.rand.NextBool(2))
                        {
                            Vector2 dustPoint = offset;
                            //dustPoint.X += Main.rand.NextFloat(-16, 16);
                            //dustPoint.Y += Main.rand.NextFloat(-16, 16);
                            if (Main.rand.NextBool())
                            {
                                Dust.NewDustPerfect(player.Center + dustPoint, DustID.InfernoFork, (diff * 2).RotatedBy(Main.rand.NextFloat(MathHelper.Pi / -3, MathHelper.Pi / 3)) + (diff * 5), 200, default, 1.8f).noGravity = true;
                            }
                            else
                            {
                                Dust.NewDustPerfect(player.Center + dustPoint, DustID.Flare, (diff * 2).RotatedBy(Main.rand.NextFloat(MathHelper.Pi / -3, MathHelper.Pi / 3)) + (diff * 5), 200, default, 1f).noGravity = true;
                            }
                        }
                    }

                    for (int i = 0; i < 30; i++)
                    {
                        float dist = Main.rand.NextFloat(0, 32);
                        bool above = Main.rand.NextBool();
                        Vector2 dustOffset = new Vector2(dist, Main.rand.NextFloat(-12, 12));
                        dustOffset = dustOffset.RotatedBy(diff.ToRotation());

                        Dust.NewDustPerfect(player.Center + dustOffset, DustID.InfernoFork, diff, 200, default, 1.8f).noGravity = true;
                    }

                    player.statMana -= (int)(50 * player.manaCost);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45);
                    if (Main.myPlayer == Projectile.owner)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), collision, Vector2.Zero, ModContent.ProjectileType<Projectiles.FireballInferno2>(), Projectile.damage, 0, Projectile.owner);
                    }

                    //Drain BotC players stamina
                    if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
                    {
                        player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= 30;
                    }
                }
            }
            //If not, move up the animation sheet as the weapon charges
            else if (player.altFunctionUse == 2 && altFunctionTimer % 4 == 0 && player.statMana >= 5)
            {
                float radius = 64;

                for (int j = 0; j < 50f; j++)
                {
                    Vector2 dir = Main.rand.NextVector2CircularEdge(radius, radius);
                    Vector2 dustPos = player.Center + dir;
                    Vector2 dustVel = new Vector2(0.4f, 0).RotatedBy(dir.ToRotation() + MathHelper.Pi);
                    Dust.NewDustPerfect(dustPos, DustID.InfernoFork, dustVel, 200, default, 0.75f).noGravity = true;
                }
                for (int j = 0; j < 5f; j++)
                {
                    Vector2 dir = Main.rand.NextVector2CircularEdge(radius, radius);
                    Vector2 dustPos = player.Center + dir;
                    Vector2 dustVel = new Vector2(0.4f, 0).RotatedBy(dir.ToRotation() + MathHelper.Pi);
                    Dust.NewDustPerfect(dustPos, DustID.Flare, dustVel, 200, default, 0.75f).noGravity = true;
                }

                Vector2 velocity = Projectile.velocity;
                velocity.Normalize();
                velocity *= 30;
                player.statMana -= (int)(5 * player.manaCost);
                if (Main.myPlayer == Projectile.owner)
                {
                    Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), player.Center, velocity, ModContent.ProjectileType<Projectiles.Fireball3>(), Projectile.damage / 30, 0, Projectile.owner).rotation = velocity.ToRotation() + MathHelper.PiOver2;
                }

                //Drain BotC players stamina
                if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
                {
                    player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= 6;
                }
            }



            UpdatePlayerVisuals(player, rrp);

            if (Projectile.owner == Main.myPlayer)
            {
                UpdateAim(rrp, player.HeldItem.shootSpeed);

                bool stillInUse = player.channel || player.altFunctionUse == 2;

                if (player.altFunctionUse == 2 && altFunctionTimer > 32)
                {
                    stillInUse = false;
                }

                if (player.altFunctionUse == 2 && player.statMana <= (int)(5 * player.manaCost))
                {
                    stillInUse = false;
                }
                if (player.altFunctionUse != 2 && player.statMana <= (int)(50 * player.manaCost))
                {
                    stillInUse = false;
                }

                if (player.noItems || player.CCed)
                {
                    stillInUse = false;
                }

                if (!stillInUse)
                {
                    Projectile.Kill();
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
            Projectile.Center = new Vector2(origin.X + (-25 * player.direction), origin.Y - 15);
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
