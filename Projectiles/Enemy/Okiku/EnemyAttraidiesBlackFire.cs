using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy.Okiku
{
    class EnemyAttraidiesBlackFire : Projectiles.VFX.DynamicTrail
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/EnemyBlackFire";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Black Fire");

        }
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.timeLeft = 600;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;

            trailWidth = 25;
            trailPointLimit = 300;
            trailYOffset = 30;
            trailMaxLength = 300;
            NPCSource = false;
            collisionPadding = 0;
            collisionEndPadding = 1;
            collisionFrequency = 2;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/BlackFireball", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        public override void AI()
        {
            if (Projectile.timeLeft == 487)
            {
                Main.NewText(Projectile.timeLeft);
            }
            base.AI();
            Lighting.AddLight(Projectile.Center, Color.Purple.ToVector3());

            if ((int)Projectile.ai[0] < 0)
            {
                trailMaxLength = 700;
                //Let them move slow for a second to telegraph before accelerating off
                if (Projectile.timeLeft < 570)
                {
                    if (Projectile.velocity.Length() < 40)
                    {
                        Projectile.velocity *= 1.05f;
                    }
                }
            }
            else
            {
                if (Main.player[(int)Projectile.ai[0]].position.Y > Projectile.position.Y)
                {
                    Projectile.tileCollide = false;
                }
                else
                {
                    Projectile.tileCollide = true;
                }

                // Determine projectile behavior
                // Apply half-gravity & clamp downward speed
                Projectile.velocity.Y = Projectile.velocity.Y > 16f ? 16f : Projectile.velocity.Y + 0.1f;

                if (Projectile.velocity.X < 0f)
                {    // Dampen left-facing horizontal velocity & clamp to minimum speed
                    Projectile.velocity.X = Projectile.velocity.X > -1f ? -1f : Projectile.velocity.X + 0.01f;
                }
                else if (Projectile.velocity.X > 0f)
                {    // Dampen right-facing horizontal velocity & clamp to minimum speed
                    Projectile.velocity.X = Projectile.velocity.X < 1f ? 1f : Projectile.velocity.X - 0.01f;
                }

                // Align projectile facing with velocity normal
                Projectile.rotation = (float)Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X) - 2.355f;

                if (Main.rand.NextBool(5))
                {
                    // Render fire particles [every frame]
                    int particle = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 54, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 160, default(Color), 3f);
                    Main.dust[particle].noGravity = true;
                    Main.dust[particle].velocity *= 1.4f;
                    int lol = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 58, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 160, default(Color), 3f);
                    Main.dust[lol].noGravity = true;
                    Main.dust[lol].velocity *= 1.4f;
                }

                // Render smoke particles [every other frame]
                if (Projectile.timeLeft % 4 == 0)
                {
                    int particle2 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 1, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f - 1f, 180, default(Color), 1f + (float)Main.rand.Next(2));
                    Main.dust[particle2].noGravity = true;
                    Main.dust[particle2].noLight = true;
                    Main.dust[particle2].fadeIn = 3f;
                }
            }            
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<DarkInferno>(), 240, false);
        }

        public override bool PreKill(int timeLeft)
        {
            if (!Projectile.active)
            {
                return true;
            }
            Projectile.timeLeft = 0;
            //projectile.AI(false);

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

            float len = 4f;
            int flam = ModContent.ProjectileType<EnemyBlackFirelet>();
            Vector2 dir = new Vector2(1f, 0f);

            // determine how many flamelets to spew (5~8)
            int children = Main.rand.Next(5) + 3;

            // set the angle offset by the number of flamelets
            int offset = 180 / (children - 1);

            // rotate theta by a random angle between 0 and 90 degrees
            int preOffset = Main.rand.Next(90);
            dir = new Vector2((float)Math.Cos(preOffset) * dir.X - (float)Math.Sin(preOffset) * dir.Y, (float)Math.Sin(preOffset) * dir.X + (float)Math.Cos(preOffset) * dir.Y);

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

                //if(projectile.owner == Main.myPlayer) Projectile.NewProjectile(Projectile.GetSource_FromThis(), projectile.position.X + (float)(projectile.width-5), projectile.position.Y + (float)(projectile.height-5), 0, 0, flam, 72, 0, projectile.owner);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X, Projectile.position.Y, velX, velY, flam, Projectile.damage, 0, Projectile.owner);
            }


            // create glowing red embers that fill the explosion's radius
            for (int i = 0; i < 30; i++)
            {
                float velX = 2f - ((float)Main.rand.Next(20)) / 5f;
                float velY = 2f - ((float)Main.rand.Next(20)) / 5f;
                velX *= 4f;
                velY *= 4f;
                int p = Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width >> 1), Projectile.position.Y - (float)(Projectile.height >> 1)), Projectile.width, Projectile.height, 54, velX, velY, 160, default(Color), 1.5f);
                int p2 = Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width >> 1), Projectile.position.Y - (float)(Projectile.height >> 1)), Projectile.width, Projectile.height, 58, velX, velY, 160, default(Color), 1.5f);
            }

            // terminate projectile
            Projectile.active = false;
            return true;
        }

        
        public override float CollisionWidthFunction(float progress)
        {
            return 12;
        }

        float timeFactor = 0;
        public override void SetEffectParameters(Effect effect)
        {
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/BlackFireball", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            effect = ModContent.Request<Effect>("tsorcRevamp/Effects/BlackFireball", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            collisionEndPadding = trailPositions.Count / 3;
            collisionPadding = trailPositions.Count / 8;
            visualizeTrail = false;
            timeFactor++;
            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.tNoiseTextureTurbulent);
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);

            Color shaderColor = Color.DarkViolet;
            shaderColor = UsefulFunctions.ShiftColor(shaderColor, timeFactor, 0.03f);
            effect.Parameters["shaderColor"].SetValue(shaderColor.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
    }
}