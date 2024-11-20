using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class ThrowingAxe : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.aiStyle = 2;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = true;
            Projectile.width = 26;
            Projectile.height = 46;
            Projectile.penetrate = 1;
        }
        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            for (int i = 0; i < 10; i++)
            {
                Vector2 projPosition = new Vector2(Projectile.position.X, Projectile.position.Y);
                Dust.NewDust(projPosition, Projectile.width, Projectile.height, 7, 0f, 0f, 0, default, 1f);
            }
        }
    }
}
