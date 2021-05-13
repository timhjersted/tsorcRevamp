using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using Microsoft.Xna.Framework;


namespace tsorcRevamp.Projectiles
{
    public class ShulletBellDark : ModProjectile
    {
        public override void SetDefaults()
        {
            projectile.height = 6;
            projectile.width = 6;
            projectile.scale = 0.5f;
            projectile.hostile = false;
            projectile.friendly = true;
        }
        public override void AI()
        {
            float rotationsPerSecond = 1.2f;
            bool rotateClockwise = true;
            //The rotation is set here
            projectile.rotation += (rotateClockwise ? 1 : -1) * MathHelper.ToRadians(rotationsPerSecond * 6f);

            projectile.velocity.Y = projectile.velocity.Y + 0.1f; // 0.1f for arrow gravity, 0.4f for knife gravity
            if (projectile.velocity.Y > 16f) // This check implements "terminal velocity". We don't want the projectile to keep getting faster and faster. Past 16f this projectile will travel through blocks, so this check is useful.
            {
                projectile.velocity.Y = 16f;
            }
        }
    }
}