using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Ranged.Ammo;

namespace tsorcRevamp.Projectiles.Ranged
{
    class Coin : ModProjectile
    {
        public override void SetDefaults()
        {

            // while the sprite is actually bigger than 15x15, we use 15x15 since it lets the projectile clip into tiles as it bounces. It looks better.
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.aiStyle = 1;
            Projectile.penetrate = 2;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 600;
        }

        public override void AI()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (i != Projectile.whoAmI && Main.projectile[i].active && Main.projectile[i].type != Projectile.type)
                {
                    //Check if any projectiles hit the coin

                    Rectangle otherHitbox = Main.projectile[i].Hitbox;
                    int hitboxMultiplier = 1;
                    float reflectionSpeed = 20;
                    if (Main.projectile[i].type == ModContent.ProjectileType<AntiMaterialRound>())
                    {
                        reflectionSpeed = 10;
                        hitboxMultiplier = 10;
                    }

                    int hitboxShiftX = otherHitbox.Width;
                    int hitboxShiftY = otherHitbox.Height;

                    //Shift the hitbox
                    otherHitbox.Width *= hitboxMultiplier;
                    otherHitbox.Height *= hitboxMultiplier;

                    hitboxShiftX = otherHitbox.Width - hitboxShiftX;
                    hitboxShiftY = otherHitbox.Height - hitboxShiftY;
                    otherHitbox.X -= hitboxShiftX;
                    otherHitbox.Y -= hitboxShiftY;

                    if (Main.projectile[i].Colliding(otherHitbox, Projectile.Hitbox))
                    {
                        //Check if the projectile in question physically hit the coin (its hitbox intersects).
                        //, then the projectile has weird custom collision and will pass through the coin (but still fire the deflection)
                        //If Colliding() was true and this is true, the projectile has normal collision and will be deflected
                        //If this is false the other projectile has weird collision and will pass through the coin, but still fire a deflected laser.
                        if (otherHitbox.Intersects(Projectile.Hitbox))
                        {
                            Main.projectile[i].friendly = true;
                            Main.projectile[i].hostile = false;
                            Main.projectile[i].velocity = UsefulFunctions.Aim(Main.projectile[i].Center, GetTarget(), reflectionSpeed);
                            Main.projectile[i].CritChance = 100;
                            Main.projectile[i].damage *= 2;
                        }
                        else
                        {
                            float ai1 = 0;
                            //Special case handling
                            if (Main.projectile[i].type == ModContent.ProjectileType<RedLaserBeam>())
                            {
                                ai1 = 1;
                            }
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, UsefulFunctions.Aim(Projectile.Center, GetTarget(), 1), Main.projectile[i].type, Main.projectile[i].damage * 2, Main.projectile[i].knockBack, Projectile.owner, 0, ai1);
                        }

                        if (Main.netMode != NetmodeID.Server)
                        {
                            int option = Main.rand.Next(3);
                            if (option == 0)
                            {
                                Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/RicochetUno") with { Volume = 0.6f, PitchVariance = .3f }, Projectile.Center);
                            }
                            else if (option == 1)
                            {
                                Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/RicochetDos") with { Volume = 0.6f, PitchVariance = .3f }, Projectile.Center);
                            }
                            else if (option == 2)
                            {
                                Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/RicochetTres") with { Volume = 0.6f, PitchVariance = .3f }, Projectile.Center);
                            }
                        }

                        Projectile.Kill();
                    }
                }
            }

            Projectile.rotation += 0.12f;

            Lighting.AddLight(Projectile.position, 0.0452f, 0.24f, 0.24f);

            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft <= 6)
            {
                Projectile.alpha += 25;

                if (Projectile.alpha > 255)
                {
                    Projectile.alpha = 225;
                }
            }
        }

        Vector2 GetTarget()
        {
            //If more than one coin is in the air, target the *furthest* other coin (to maximize ricochet potential)
            float coinDistance = 0;
            Vector2 coinCenter = Vector2.Zero;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].type == Projectile.type && i != Projectile.whoAmI)
                {
                    float distance = Projectile.DistanceSQ(Main.projectile[i].Center);
                    if (distance > coinDistance && Collision.CanHit(Main.projectile[i], Projectile))
                    {
                        coinDistance = distance;
                        coinCenter = Main.projectile[i].Center;
                    }
                }
            }
            if (coinCenter != Vector2.Zero)
            {
                return coinCenter;
            }

            //If there are no other coins in the air, target the closest enemy


            int? closestNPC = null;
            float NPCdistance = float.MaxValue;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && !Main.npc[i].friendly && !Main.npc[i].dontTakeDamage && !NPCID.Sets.CountsAsCritter[Main.npc[i].type] && Main.npc[i].lifeMax > 1 && Collision.CanHit(Main.npc[i], Projectile))
                {
                    float newDistance = Vector2.DistanceSquared(Projectile.Center, Main.npc[i].Center);
                    if (newDistance < NPCdistance)
                    {
                        NPCdistance = newDistance;
                        closestNPC = i;
                    }
                }
            }

            if (closestNPC != null)
            {
                return Main.npc[closestNPC.Value].Center;
            }
            else
            {
                //If there are no other coins or enemies, bounce in a random direction
                return Projectile.Center + Main.rand.NextVector2Circular(10, 10);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.8f, Pitch = -0.25f }, Projectile.Center);
            for (int d = 0; d < 30; d++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 111, Projectile.velocity.X * 1f, Projectile.velocity.Y * 1f, 30, default(Color), .7f);
                Main.dust[dust].velocity.X = +Main.rand.Next(-50, 51) * 0.085f;
                Main.dust[dust].velocity.Y = +Main.rand.Next(-50, 51) * 0.085f;
                Main.dust[dust].noGravity = true;
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.8f, Pitch = -0.25f }, Projectile.Center);
            for (int d = 0; d < 30; d++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 111, Projectile.velocity.X * 1f, Projectile.velocity.Y * 1f, 30, default(Color), .7f);
                Main.dust[dust].velocity.X = +Main.rand.Next(-50, 51) * 0.085f;
                Main.dust[dust].velocity.Y = +Main.rand.Next(-50, 51) * 0.085f;
                Main.dust[dust].noGravity = true;

            }
            return true;

        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.4f, Pitch = -0.25f }, Projectile.Center);
            for (int d = 0; d < 30; d++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 111, Projectile.velocity.X * 1.2f, Projectile.velocity.Y * 1.2f, 30, default(Color), .7f);
                Main.dust[dust].velocity.X = +Main.rand.Next(-50, 51) * 0.085f;
                Main.dust[dust].velocity.Y = +Main.rand.Next(-50, 51) * 0.085f;
                Main.dust[dust].noGravity = true;

            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Color.White;
            return base.PreDraw(ref lightColor);
        }
    }
}
