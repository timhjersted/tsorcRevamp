using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
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

        struct FlameTongue
        {
            public float X;
            public int Age;
            public float Seed;
            public float DistanceProgress;
        }

        readonly List<FlameTongue> _wake = new();

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

            if (!Main.dedServ)
            {
                for (int i = _wake.Count - 1; i >= 0; i--)
                {
                    FlameTongue tongue = _wake[i];
                    tongue.Age++;
                    if (tongue.Age > 58)
                        _wake.RemoveAt(i);
                    else
                        _wake[i] = tongue;
                }

                // Ancient Demon's breath reads as fire because it is a stream of separately aging
                // flame sprites. Keep that principle here, but cache the decorative tongues locally
                // so the authoritative moving projectile and its damage volume remain unchanged.
                if (_elapsed % 4 == 0)
                {
                    float distanceProgress = MathHelper.Clamp(
                        Math.Abs(Projectile.Center.X - _spawnX) / TravelDistance, 0f, 1f);
                    _wake.Add(new FlameTongue
                    {
                        X = Projectile.Center.X,
                        Age = 0,
                        Seed = (Projectile.identity * 0.173f + _elapsed * 0.071f) % 1f,
                        DistanceProgress = distanceProgress
                    });
                }
            }

            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * (0.65f + 0.6f * growT));

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Vector2 dustVel = new Vector2(Projectile.velocity.X * Main.rand.NextFloat(-0.05f, 0.12f),
                    Main.rand.NextFloat(-2.4f, -0.45f));
                int type = Main.rand.NextBool(5) ? DustID.SilverFlame
                    : Main.rand.NextBool(3) ? DustID.ShadowbeamStaff : DustID.PurpleTorch;
                Dust d = Dust.NewDustPerfect(Projectile.Bottom
                    + new Vector2(Main.rand.NextFloat(-Projectile.width, Projectile.width), Main.rand.NextFloat(-36f, -5f)),
                    type, dustVel, 80, new Color(170, 46, 226), Main.rand.NextFloat(0.68f, 1.08f));
                d.noGravity = true;
            }

            if (Math.Abs(Projectile.Center.X - _spawnX) >= TravelDistance)
            {
                Projectile.Kill();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float grow = MathHelper.Clamp(_elapsed / (float)GrowTicks, 0f, 1f);
            float direction = Projectile.velocity.X < 0f ? -1f : 1f;
            float travel = MathHelper.Clamp(Math.Abs(Projectile.Center.X - _spawnX) / TravelDistance, 0f, 1f);
            float fade = 1f - MathHelper.Clamp((travel - 0.88f) / 0.12f, 0f, 1f);
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            SpriteEffects effects = direction < 0f
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            // Low, fading tongues remain behind the leading hazard. Their height increases with
            // distance from the slam, producing the requested low-center / tall-edge fire profile.
            for (int i = 0; i < _wake.Count; i++)
            {
                FlameTongue tongue = _wake[i];
                float ageFade = 1f - MathHelper.Clamp((tongue.Age - 34f) / 24f, 0f, 1f);
                if (ageFade <= 0f)
                    continue;

                float shapeT = MathHelper.SmoothStep(0f, 1f, tongue.DistanceProgress);
                int wakeFrame = Math.Clamp(1 + (int)(shapeT * 3f), 1, 4);
                Rectangle wakeSource = texture.Frame(1, Main.projFrames[Type], 0, wakeFrame);
                float height = MathHelper.Lerp(18f, 78f, shapeT)
                    * MathHelper.Lerp(0.82f, 1.12f, tongue.Seed);
                float width = MathHelper.Lerp(16f, 46f, shapeT);
                float rise = tongue.Age * MathHelper.Lerp(0.05f, 0.20f, shapeT);
                float sway = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5.6f
                    + tongue.Seed * MathHelper.TwoPi) * 0.07f * direction;
                Vector2 scale = new Vector2(width / wakeSource.Width, height / wakeSource.Height);
                Vector2 bottom = new Vector2(tongue.X, _groundY - rise);
                Color wakeColor = Color.Lerp(new Color(82, 24, 142),
                    new Color(224, 60, 210), shapeT) * (ageFade * 0.62f * fade);
                Main.EntitySpriteDraw(texture, bottom - Main.screenPosition, wakeSource,
                    wakeColor, sway, new Vector2(wakeSource.Width * 0.5f, wakeSource.Height),
                    scale, effects, 0f);
            }

            Rectangle source = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 flameSize = new(
                MathHelper.Lerp(28f, 76f, grow),
                MathHelper.Lerp(34f, 118f, grow));
            Vector2 bottomCenter = new(Projectile.Center.X, _groundY);
            Vector2 shaderCenter = bottomCenter - new Vector2(0f, flameSize.Y * 0.5f);
            ArtoriasVFX.DrawFloorFlame(texture, source, shaderCenter, flameSize,
                direction, grow, fade, Projectile.identity * 0.071f);

            // Crisp pixel-art core on top of the moving shader envelope. Explicit mirroring fixes
            // the old problem where both left/right projectiles visibly curled to the right.
            Vector2 leadScale = new(flameSize.X / source.Width, flameSize.Y / source.Height);
            Main.EntitySpriteDraw(texture, bottomCenter - Main.screenPosition, source,
                Color.White * fade, 0f, new Vector2(source.Width * 0.5f, source.Height),
                leadScale, effects, 0f);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }
            for (int i = 0; i < 6; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(4f, 4f) + new Vector2(0f, -2f);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, vel, 60, new Color(230, 120, 220), 1.3f);
                d.noGravity = true;
            }
        }
    }
}
