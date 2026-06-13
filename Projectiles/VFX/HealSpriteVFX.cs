using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.VFX
{
    // Stationary, non-damaging version of EnemySpellEffectHealing for use on healed NPCs.
    class HealSpriteVFX : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/EnemySpellEffectHealing";

        public override void SetDefaults()
        {
            Projectile.width = 52;
            Projectile.height = 44;
            Main.projFrames[Projectile.type] = 5;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.light = 1f;
            Projectile.timeLeft = 30;
        }

        public override void AI()
        {
            Projectile.velocity = Microsoft.Xna.Framework.Vector2.Zero;

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 5)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 5)
                Projectile.Kill();
        }

        public override bool? CanDamage() => false;
    }
}
