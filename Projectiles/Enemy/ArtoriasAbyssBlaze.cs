using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    // Travels away from the slam point (spawned in a left/right pair) hugging the ground, growing
    // from a low flicker to a full 80px-tall wall over GrowTicks, then holds full size until it has
    // traveled TravelDistance and dissipates. Uses the 5-frame EnemyAbyssBlaze sprite (86x82/frame) -
    // the frame sequence itself depicts the flame growing, so frame index is driven by growth progress.
    class ArtoriasAbyssBlaze : ModProjectile
    {
        const int GrowTicks = 30;
        const float TravelDistance = 400f;
        const float Speed = 5f;
        const int FinalHeight = 80;
        const int FinalWidth = 32;
        const int StartHeight = 10;
        const int StartWidth = 12;

        float _spawnX;
        float _groundY;
        int _elapsed;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/EnemyAbyssBlaze";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = StartWidth;
            Projectile.height = StartHeight;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.light = 0.7f;
            Projectile.timeLeft = 200; // safety cap; normally ends via TravelDistance
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            // Velocity is set by the spawning code (one instance per side, opposite signs);
            // just lock in the reference point to grow/travel from.
            _spawnX = Projectile.Center.X;
            _groundY = Projectile.position.Y + Projectile.height; // feet stay pinned to this Y as it grows
        }

        public override void AI()
        {
            _elapsed++;
            float growT = MathHelper.Clamp(_elapsed / (float)GrowTicks, 0f, 1f);

            int newHeight = (int)MathHelper.Lerp(StartHeight, FinalHeight, growT);
            int newWidth = (int)MathHelper.Lerp(StartWidth, FinalWidth, growT);
            float centerX = Projectile.Center.X;
            Projectile.width = newWidth;
            Projectile.height = newHeight;
            Projectile.position.X = centerX - newWidth / 2f;
            Projectile.position.Y = _groundY - newHeight;

            Projectile.frame = (int)(growT * (Main.projFrames[Type] - 1));

            Lighting.AddLight(Projectile.Center, new Vector3(0.6f, 0.15f, 0.85f) * (0.4f + 0.6f * growT));

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Vector2 dustVel = new Vector2(Projectile.velocity.X * 0.15f, Main.rand.NextFloat(-1.5f, -0.3f));
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, dustVel, 50, default, Main.rand.NextFloat(1f, 1.4f));
                d.noGravity = true;
            }

            if (Math.Abs(Projectile.Center.X - _spawnX) >= TravelDistance)
            {
                Projectile.Kill();
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }
            for (int i = 0; i < 16; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(4f, 4f) + new Vector2(0f, -2f);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, vel, 60, new Color(230, 120, 220), 1.3f);
                d.noGravity = true;
            }
        }
    }
}
