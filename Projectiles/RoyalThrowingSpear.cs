using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class RoyalThrowingSpear : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Ranged/Thrown/RoyalThrowingSpear";
        public override void SetDefaults()
        {
            Projectile.aiStyle = 1;
            Projectile.friendly = true;
            Projectile.height = 14;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.scale = 0.9f;
            Projectile.tileCollide = true;
            Projectile.width = 14;
        }

        public override void Kill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 7, 0f, 0f, 0, default, 1f);
            }
        }
    }
}
