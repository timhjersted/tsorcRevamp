using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class DyingStarHoldout : ModProjectile
    {

        public override string Texture => "tsorcRevamp/Projectiles/GlaiveBeamHoldout";


        // This value controls how many frames it takes for the Prism to reach "max charge". 60 frames = 1 second.
        public static float MaxCharge = 60f;


        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dying Star Holdout");

            // Signals to Terraria that this projectile requires a unique identifier beyond its index in the projectile array.
            // This prevents the issue with the vanilla Last Prism where the beams are invisible in multiplayer.
            ProjectileID.Sets.NeedsUUID[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            // Use CloneDefaults to clone all basic projectile statistics from the vanilla Last Prism.
            Projectile.CloneDefaults(ProjectileID.LastPrism);
            Projectile.DamageType = DamageClass.Magic;
            Projectile.friendly = false;
            Projectile.hostile = false;
        }

        int charge = 0;
        int altFunctionTimer = 0;
        bool spawnedLaser = false;
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
                        Dust.NewDustPerfect(dustPos, DustID.MagicMirror, Vector2.Zero, 200, default, 0.75f).noGravity = true;
                    }                   
                }
                else
                {
                    charge = 0;
                    Vector2 collision = UsefulFunctions.GetFirstCollision(player.Center, Projectile.velocity);

                    Vector2 diff = collision - player.Center;
                    float length = diff.Length();
                    diff.Normalize();
                    Vector2 offset = Vector2.Zero;

                    for(int i = 0; i < 50; i++)
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
                        if (Main.rand.Next(2) == 0)
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
                                Dust.NewDustPerfect(player.Center + dustPoint, DustID.MagicMirror, (diff * 2).RotatedBy(Main.rand.NextFloat(MathHelper.Pi / -3, MathHelper.Pi / 3)) + (diff * 5), 200, default, 1f).noGravity = true;
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
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), collision, Vector2.Zero, ModContent.ProjectileType<Projectiles.FireballInferno2>(), Projectile.damage, 0, default);

                    //Drain BotC players stamina
                    if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
                    {
                        player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent = 0;
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
                    Dust.NewDustPerfect(dustPos, DustID.MagicMirror, dustVel, 200, default, 0.75f).noGravity = true;
                }

                Vector2 velocity = Projectile.velocity;
                velocity.Normalize();
                velocity *= 30;
                player.statMana -= (int)(10 * player.manaCost);
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), player.Center, velocity, ModContent.ProjectileType<Projectiles.Fireball3>(), Projectile.damage / 30, 0, default).rotation = velocity.ToRotation() + MathHelper.PiOver2;
            }
            


            // Update the Prism's position in the world and relevant variables of the player holding it.
            UpdatePlayerVisuals(player, rrp);

            // Update the Prism's behavior: project beams on frame 1, consume mana, and despawn if out of mana.
            if (Projectile.owner == Main.myPlayer)
            {
                // Slightly re-aim the Prism every frame so that it gradually sweeps to point towards the mouse.
                UpdateAim(rrp, player.HeldItem.shootSpeed);

                // The Prism immediately stops functioning if the player is Cursed (player.noItems) or "Crowd Controlled", e.g. the Frozen debuff.
                // player.channel indicates whether the player is still holding down the mouse button to use the item.
                bool stillInUse = player.channel || player.altFunctionUse == 2;                

                if(player.altFunctionUse == 2 && altFunctionTimer > 32)
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

                // If the Prism cannot continue to be used, then destroy it immediately.
                if (!stillInUse)
                {
                    Projectile.Kill();
                }
            }

            // This ensures that the Prism never times out while in use.
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
            // Place the Prism directly into the player's hand at all times.
            Vector2 origin = GetOrigin();
            Projectile.Center = new Vector2(origin.X + (-25 * player.direction), origin.Y - 15);
            // The beams emit from the tip of the Prism, not the side. As such, rotate the sprite by pi/2 (90 degrees).
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.spriteDirection = Projectile.direction;

            // The Prism is a holdout projectile, so change the player's variables to reflect that.
            // Constantly resetting player.itemTime and player.itemAnimation prevents the player from switching items or doing anything else.
            player.ChangeDir(Projectile.direction);
            //player.heldProj = projectile.whoAmI;
            player.itemTime = 2;
            player.itemAnimation = 2;

            // If you do not multiply by projectile.direction, the player's hand will point the wrong direction while facing left.
            player.itemRotation = (Projectile.velocity * Projectile.direction).ToRotation();
        }


        private void UpdateAim(Vector2 source, float speed)
        {
            // Get the player's current aiming direction as a normalized vector.
            Vector2 aim = Vector2.Normalize(Main.MouseWorld - source);
            if (aim.HasNaNs())
            {
                aim = -Vector2.UnitY;
            }

            // Change a portion of the Prism's current velocity so that it points to the mouse. This gives smooth movement over time.
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
