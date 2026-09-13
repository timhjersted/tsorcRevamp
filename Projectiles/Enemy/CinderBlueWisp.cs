using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>Blue firebolt for Soul of Cinder's staff fan. Its flame base follows its travel direction.</summary>
    class CinderBlueWisp : ModProjectile
    {
        const int Frames = 4;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Weapons/BlueWisp";

        public override void SetStaticDefaults() => Main.projFrames[Type] = Frames;

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 26;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.light = 0.55f;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            if (++Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Frames;
            }

            if (Main.rand.NextBool(4))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Clentaminator_Cyan,
                    -Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.35f, 0.35f), 120,
                    new Color(95, 190, 255), 0.85f);
                dust.noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, new Vector3(0.10f, 0.45f, 0.85f));
        }
    }
}
