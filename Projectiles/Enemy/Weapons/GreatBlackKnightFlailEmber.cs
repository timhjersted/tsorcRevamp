using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    /// <summary>
    /// One of the embers flicked off GreatBlackKnightFlail's tip on an empowered throw's release. No dedicated
    /// blue-flame sprite yet, so it's dust-only (like GreatBlackKnightFlailPulse) — travels under gravity from a
    /// share of the flail's own launch velocity, then sticks to whatever surface it hits and keeps burning there as
    /// a GreatBlackKnightFlailEmberLingering. Impact + surface-following logic mirrors EnemyFireFlask via the
    /// shared SurfaceFlameHelper.
    /// </summary>
    public class GreatBlackKnightFlailEmber : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        private const float Gravity = 0.2f;
        private const int SpreadTiles = 1; // a small cluster per ember, not a full FireFlask-sized spread

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 90;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.light = 0.3f;
            Projectile.hide = false;
        }

        public override void AI()
        {
            Projectile.velocity.Y += Gravity;
            if (Projectile.velocity.Y > 16f)
            {
                Projectile.velocity.Y = 16f;
            }

            if (Main.rand.NextBool(5))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.IceTorch, Projectile.velocity * 0.2f, 100, default, 1f);
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            EnemyVFX.DrawGreatBlackKnightEmber(Projectile.Center, Projectile.velocity, new Vector2(48f, 16f), 0.84f);
            return false;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Vector2 normal = Vector2.UnitY * -1f;
            if (Projectile.velocity.X != oldVelocity.X && System.Math.Abs(oldVelocity.X) > System.Math.Abs(oldVelocity.Y))
            {
                normal = oldVelocity.X > 0f ? -Vector2.UnitX : Vector2.UnitX;
            }
            else if (Projectile.velocity.Y != oldVelocity.Y)
            {
                normal = oldVelocity.Y > 0f ? -Vector2.UnitY : Vector2.UnitY;
            }

            Projectile.ai[0] = normal.X;
            Projectile.ai[1] = normal.Y;
            Projectile.Kill();
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Frostburn, 6 * 60);
            Projectile.ai[0] = 0f;
            Projectile.ai[1] = -1f;
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(2f, 2f);
                Dust.NewDustPerfect(Projectile.Center, DustID.IceTorch, velocity, 100, default, Main.rand.NextFloat(1f, 1.6f)).noGravity = true;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Vector2 normal = new Vector2(Projectile.ai[0], Projectile.ai[1]);
            if (normal == Vector2.Zero)
            {
                normal = -Vector2.UnitY;
            }
            normal.Normalize();
            SurfaceFlameHelper.SpawnSurfaceFlames(Projectile, Projectile.Center, normal, SpreadTiles, ModContent.ProjectileType<GreatBlackKnightFlailEmberLingering>(), Projectile.damage);
        }
    }

    /// <summary>The 9-second stuck-burning half of an ember — sits wherever it landed and applies Frostburn.</summary>
    public class GreatBlackKnightFlailEmberLingering : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 9 * 60;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.light = 0.5f;
            Projectile.hide = false;
        }

        public override void AI()
        {
            Vector2 normal = new Vector2(Projectile.ai[0], Projectile.ai[1]);
            if (normal == Vector2.Zero)
            {
                normal = -Vector2.UnitY;
            }
            normal.Normalize();
            Projectile.velocity = Vector2.Zero;

            if (Main.rand.NextBool(6))
            {
                Vector2 dustPos = Projectile.Center - normal * 4f + Main.rand.NextVector2Circular(6f, 6f);
                Vector2 dustVel = normal * Main.rand.NextFloat(0.4f, 1.2f) + Main.rand.NextVector2Circular(0.3f, 0.3f);
                Dust.NewDustPerfect(dustPos, DustID.IceTorch, dustVel, 100, default, Main.rand.NextFloat(1f, 1.8f)).noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 normal = new Vector2(Projectile.ai[0], Projectile.ai[1]);
            if (normal == Vector2.Zero)
            {
                normal = -Vector2.UnitY;
            }
            EnemyVFX.DrawGreatBlackKnightEmber(Projectile.Center, normal, new Vector2(34f, 20f), 0.68f);
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Frostburn, 6 * 60);
        }
    }
}
