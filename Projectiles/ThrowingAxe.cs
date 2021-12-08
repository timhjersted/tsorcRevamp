using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class ThrowingAxe : ModProjectile {

        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/ThrowingAxe";
        public override void SetDefaults() {
            projectile.aiStyle = 2;
            projectile.friendly = true;
            projectile.melee = true;
            projectile.tileCollide = true;
            projectile.height = 22;
            projectile.penetrate = 1;
            projectile.width = 22;
        }
        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Dig, (int)projectile.position.X, (int)projectile.position.Y, 1);
            for (int i = 0; i < 10; i++)
            {
                Vector2 projPosition = new Vector2(projectile.position.X, projectile.position.Y);
                Dust.NewDust(projPosition, projectile.width, projectile.height, 7, 0f, 0f, 0, default, 1f);
            }
        }
    }
}
