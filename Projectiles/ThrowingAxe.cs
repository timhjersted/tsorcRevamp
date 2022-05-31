using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class ThrowingAxe : ModProjectile {

        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/ThrowingAxe";
        public override void SetDefaults() {
            Projectile.aiStyle = 2;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = true;
            Projectile.height = 22;
            Projectile.penetrate = 1;
            Projectile.width = 22;
        }
        public override void Kill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, (int)Projectile.position.X, (int)Projectile.position.Y, 1);
            for (int i = 0; i < 10; i++)
            {
                Vector2 projPosition = new Vector2(Projectile.position.X, Projectile.position.Y);
                Dust.NewDust(projPosition, Projectile.width, Projectile.height, 7, 0f, 0f, 0, default, 1f);
            }
        }
    }
}
