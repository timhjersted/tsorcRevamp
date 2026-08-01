using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.VesselOfSouls
{
    /// <summary>Non-damaging, short-lived implosion/explosion used by Vessel set pieces.</summary>
    class VesselSoulRuptureVFX : ModProjectile
    {
        float Radius => Projectile.ai[0] > 0f ? Projectile.ai[0] : 120f;
        bool Exploding => Projectile.ai[1] >= 0.5f;
        int Duration => Projectile.ai[2] > 0f ? (int)Projectile.ai[2] : 24;

        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 24;
            Projectile.netImportant = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.timeLeft = Duration;
        }

        public override bool? CanDamage() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = 1f - Projectile.timeLeft / (float)Duration;
            float fade = 1f - MathHelper.Clamp((progress - 0.70f) / 0.30f, 0f, 1f);
            VesselVFX.DrawRupture(Projectile.Center, Radius, progress, Exploding, 0.92f * fade);
            return false;
        }
    }
}
