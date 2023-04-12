using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class BlasterShot : ModProjectile
    {
        public override void SetDefaults()
        {

            // while the sprite is actually bigger than 15x15, we use 15x15 since it lets the projectile clip into tiles as it bounces. It looks better.
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.aiStyle = 0;
            Projectile.penetrate = 2;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 38;
            //These 2 help the projectile hitbox be centered on the projectile sprite.
            DrawOffsetX = -2;
            DrawOriginOffsetY = -2;
        }

        public override void AI()
        {

            // Rotation increased by velocity.X 
            Projectile.rotation += Projectile.velocity.X * 0.08f;
            //projectile.rotation = projectile.velocity.ToRotation(); // projectile faces sprite right

            if (Projectile.localAI[0] == 0f)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item91 with { Volume = 0.7f, Pitch = -0.5f }, Projectile.Center);
                Projectile.localAI[0] += 1f;
            }

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

        public override void Kill(int timeLeft)
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

    }
}
