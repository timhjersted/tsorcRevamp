using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles.Magic.Runeterra.LudensTempest
{
    class LudensTempestFire : Projectiles.VFX.DynamicTrail
    {
        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.scale = 1.5f;
            Projectile.alpha = 50;
            Projectile.timeLeft = 360;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.damage = 45;
            Projectile.knockBack = 9;


            trailCollision = true;
            trailWidth = 25;
            trailPointLimit = 150;
            trailYOffset = 30;
            trailMaxLength = 150;
            NPCSource = false;
            collisionPadding = 0;
            collisionEndPadding = 1;
            collisionFrequency = 2;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/BlackFireball", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        Vector2 destination = new Vector2(0f, 0f);
        int channeling = 0;

        public override void AI()
        {
            base.AI();

            Projectile.tileCollide = true;
            // Apply half-gravity & clamp downward speed
            Projectile.velocity.Y = Projectile.velocity.Y > 16f ? 16f : Projectile.velocity.Y + 0.1f;

            if (Projectile.velocity.X < 0f)
            {   // Dampen left-facing horizontal velocity & clamp to minimum speed
                Projectile.velocity.X = Projectile.velocity.X > -1f ? -1f : Projectile.velocity.X + 0.01f;
            }
            else if (Projectile.velocity.X > 0f)
            {  // Dampen right-facing horizontal velocity & clamp to minimum speed
                Projectile.velocity.X = Projectile.velocity.X < 1f ? 1f : Projectile.velocity.X - 0.01f;
            }

            // Align projectile facing with velocity normal
            Projectile.rotation = (float)Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X) - 2.355f;
        }

        public override void Kill(int timeLeft)
        {
            if (!Projectile.active)
            {
                return;
            }
            Projectile.timeLeft = 0;

            SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Magic/LudensTempestBoom") with { Volume = 0.25f }, Projectile.Center);

            float len = 4f;
            int flam = ModContent.ProjectileType<LudensTempestFirelet>();
            int damg = Projectile.damage / 2;
            Vector2 dir = new Vector2(1f, 0f);

            // determine how many flamelets to spew (5~8)
            int children = Main.rand.Next(5) + 3;

            // set the angle offset by the number of flamelets
            int offset = 180 / (children - 1);

            // rotate theta by a random angle between 0 and 90 degrees
            int preOffset = Main.rand.Next(90);
            dir = new Vector2((float)Math.Cos(preOffset) * dir.X - (float)Math.Sin(preOffset) * dir.Y, (float)Math.Sin(preOffset) * dir.X + (float)Math.Cos(preOffset) * dir.Y);

            Vector2 averageCenter = Vector2.Zero;
            for (int i = 0; i < trailPositions.Count; i++)
            {
                averageCenter += trailPositions[i] / trailPositions.Count;
            }
            if (Projectile.ai[0] == 0)
            {
                // create the flaming shrapnel-like projectiles
                for (int i = 0; i < children; i++)
                {
                    float velX = (float)Math.Cos(offset) * dir.X - (float)Math.Sin(offset) * dir.Y;
                    float velY = (float)Math.Sin(offset) * dir.X + (float)Math.Cos(offset) * dir.Y;

                    dir.X = velX;
                    dir.Y = velY;

                    float var = (float)(Main.rand.Next(10) / 10);

                    velX *= len + var;
                    velY *= len + var;

                    if (Projectile.owner == Main.myPlayer)
                    {
                        Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), averageCenter, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 10, 0, Main.myPlayer, 30, 10);
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), averageCenter.X, averageCenter.Y, velX, velY, flam, damg, 0, Projectile.owner);
                    }
                }

                // setup projectile for explosion
                Projectile.damage = Projectile.damage * 2;
                Projectile.penetrate = 20;
                Projectile.width = Projectile.width << 3;
                Projectile.height = Projectile.height << 3;

                Projectile.Damage();
            } else

            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), averageCenter, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 10, 0, Main.myPlayer, 120, 15);
            }

            // create glowing red embers that fill the explosion's radius
            for (int i = 0; i < 30; i++)
            {
                float velX = 2f - ((float)Main.rand.Next(20)) / 5f;
                float velY = 2f - ((float)Main.rand.Next(20)) / 5f;
                velX *= 4f;
                velY *= 4f;
                Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width / 2), Projectile.position.Y - (float)(Projectile.height / 2)), Projectile.width, Projectile.height, 54, velX, velY, 160, default, 1.5f);
                Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width / 2), Projectile.position.Y - (float)(Projectile.height / 2)), Projectile.width, Projectile.height, 58, velX, velY, 160, default, 1.5f);
            }

            // terminate projectile
            Projectile.active = false;
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (target.immune[Projectile.owner] > 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public override float CollisionWidthFunction(float progress)
        {
            return 12;
        }

        float timeFactor = 0;
        public override void SetEffectParameters(Effect effect)
        {
            collisionEndPadding = trailPositions.Count / 3;
            collisionPadding = trailPositions.Count / 8;
            visualizeTrail = false;
            timeFactor++;
            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.NoiseTurbulent);
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);

            Color shaderColor = Color.BlueViolet;
            shaderColor = UsefulFunctions.ShiftColor(shaderColor, timeFactor, 0.03f);
            effect.Parameters["shaderColor"].SetValue(shaderColor.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
    }
}
