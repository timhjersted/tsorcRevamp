using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class WaterTrail : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.penetrate = 6;
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.hostile = true;
        }
        public override void AI()
        {
            if (Projectile.ai[0] == 1)
            {
                Projectile.tileCollide = false;
            }
            Projectile.rotation += 4f;
            if (Main.rand.NextBool(4))
            {
                int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 29, 0, 0, 50, Color.Blue, 2.0f);
                Main.dust[dust].noGravity = false;
            }
            Lighting.AddLight((int)(Projectile.position.X / 16f), (int)(Projectile.position.Y / 16f), 0.4f, 0.1f, 0.1f);

            if (Projectile.velocity.X <= 4 && Projectile.velocity.Y <= 4 && Projectile.velocity.X >= -4 && Projectile.velocity.Y >= -4)
            {
                float accel = 2f + (Main.rand.Next(10, 30) * 0.5f);
                Projectile.velocity.X *= accel;
                Projectile.velocity.Y *= accel;
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        { //allow the projectile to bounce
            Projectile.penetrate--;
            if (Projectile.penetrate == 0)
            {
                Projectile.Kill();
            }
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10, Projectile.position);
            if (Projectile.velocity.X != oldVelocity.X)
            {
                Projectile.velocity.X = -oldVelocity.X;
            }
            if (Projectile.velocity.Y != oldVelocity.Y)
            {
                Projectile.velocity.Y = -oldVelocity.Y;
            }
            return false;
        }
    }
}
