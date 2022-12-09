using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Triplets
{
    class HomingStar : ModProjectile
    {
        
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Seeking Star");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
        }
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.scale = 1.1f;
            Projectile.timeLeft = 200;
            Projectile.hostile = true;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.friendly = false;
        }
        
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(BuffID.OnFire, 300);
        }


        float[] trailRotations = new float[6] { 0, 0, 0, 0, 0, 0 };
        bool playedSound = false;
        public override void AI()
        {
            if (!playedSound)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item43 with { Volume = 0.5f}, Projectile.Center);
                playedSound = true;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            for (int i = 5; i > 0; i--)
            {
                trailRotations[i] = trailRotations[i - 1];
            }
            trailRotations[0] = Projectile.rotation;

            Vector2 dustOffset = Projectile.velocity;
            dustOffset.Normalize();
            //dustOffset *= 30;
            //for (int i = 0; i < 3; i++)
            {
                Main.dust[Dust.NewDust(Projectile.position + dustOffset, Projectile.width, Projectile.height, DustID.FireworkFountain_Blue, Projectile.velocity.X, Projectile.velocity.Y)].noGravity = true;
                Dust.NewDustPerfect(Projectile.Center + dustOffset, DustID.GemSapphire, Vector2.Zero, 160, default).noGravity = true;
            }

            float homingAcceleration = 0.2f;
            if (Projectile.ai[0] == 1)
            {
                Projectile.timeLeft = 100;
                if (Projectile.velocity.Y < 7f)
                {
                    Projectile.velocity.Y += 1f;
                    homingAcceleration = 0;
                }
                else
                {
                    homingAcceleration = 0.01f;
                }
            }


            Player target = UsefulFunctions.GetClosestPlayer(Projectile.Center);
            if (target != null)
            {
                UsefulFunctions.SmoothHoming(Projectile, target.Center, homingAcceleration, 30, target.velocity, false);
            }
        }

        public override void Kill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit3 with { Volume = 0.5f}, Projectile.Center);

            // create glowing red embers that fill the explosion's radius
            for (int i = 0; i < 20; i++)
            {
                Main.dust[Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.FireworkFountain_Blue, Projectile.velocity.X, Projectile.velocity.Y)].noGravity = true;
                Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(4, 4), DustID.GemSapphire, Projectile.velocity + Main.rand.NextVector2Circular(4, 4), 160, default).noGravity = true;
            }
        }

        Texture2D texture;
        Texture2D starTexture;
        float starRotation;
        public override bool PreDraw(ref Color lightColor)
        {
            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(Texture, ReLogic.Content.AssetRequestMode.ImmediateLoad);
            }
            if (starTexture == null || starTexture.IsDisposed)
            {
                starTexture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Triplets/HomingStarStar", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            }

            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Rectangle starSourceRectangle = new Rectangle(0, 0, starTexture.Width, starTexture.Height);
            Vector2 origin = sourceRectangle.Size() / 2f;
            Vector2 starOrigin = starSourceRectangle.Size() / 2f;


            //Draw shadow trails
            for (float i = ProjectileID.Sets.TrailCacheLength[Projectile.type] - 1; i >= 0; i--)
            {
                Main.spriteBatch.Draw(texture, Projectile.oldPos[(int)i] - Main.screenPosition, sourceRectangle, Color.Cyan * ((6 - i) / 6), trailRotations[(int)i] + MathHelper.Pi, origin, Projectile.scale * ((6 - i) / 6), SpriteEffects.None, 0);
            }

            Main.spriteBatch.Draw(texture, Projectile.position - Main.screenPosition, sourceRectangle, Color.White, Projectile.rotation + MathHelper.Pi, origin, Projectile.scale, SpriteEffects.None, 0);

            Vector2 starOffset = Projectile.velocity;
            starOffset.Normalize();
            starOffset *= 30;
            Main.spriteBatch.Draw(starTexture, Projectile.position - Main.screenPosition + starOffset, starSourceRectangle, Color.White, Projectile.rotation + starRotation, starOrigin, Projectile.scale * 0.75f, SpriteEffects.None, 0);
            starRotation += 0.05f;

            return false;
        }

        Vector2 dustPos()
        {
            return Main.rand.NextVector2Circular(Projectile.width / 6, Projectile.height / 6) + Projectile.Center;
        }
        Vector2 dustVel()
        {
            return Main.rand.NextVector2Circular(7, 7);
        }
    }
}
