using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy.IceGigas
{
    // Breath-material release, with one stream per ~16px of the source perimeter.
    // ai[0]: travel beyond the sprite (32/64/160px); ai[1]: buildup once per victim;
    // ai[2]: spike height scale, or -1 for the statue's 128x164px oval.
    // A synced controller owns the moving frost, so dust-budget exhaustion cannot remove hits.
    // Each visible stream and its swept collision use the same deterministic points.
    class GigasFrostBurst : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));
        const int Lifetime = 32;
        bool touchedLocalPlayer;
        readonly List<FrostMote> frostDust = new List<FrostMote>();
        readonly struct FrostMote
        {
            public readonly Dust Dust;
            public readonly Vector2 Origin, Direction;
            public readonly float BirthTravel, MaximumTravel;
            public FrostMote(Dust dust, Vector2 origin, Vector2 direction, float birthTravel, float maximumTravel)
            {
                Dust = dust;
                Origin = origin;
                Direction = direction;
                BirthTravel = birthTravel;
                MaximumTravel = maximumTravel;
            }
        }
        float[] clearTravel;
        int Age => Lifetime - Projectile.timeLeft;
        Vector2 Radii => Projectile.ai[2] < 0f ? new Vector2(64f, 82f) : new Vector2(15.4f, 27f * Projectile.ai[2]);
        int StreamCount => (int)Math.Ceiling(MathHelper.TwoPi * MathF.Sqrt((Radii.X * Radii.X + Radii.Y * Radii.Y) * 0.5f) / 16f);

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override bool? CanDamage() => false; // Frost buildup only; no hidden explosion damage.
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (Main.dedServ) return;
            if (Age == 0)
            {
                // ai[2] < 0 = the statue's single big shatter. ai[0] >= 64 = a Glacial Slam spike: 126 of those
                // burst in a rolling wave, so each is near-silent to keep the wave from rattling.
                float burstVolume = 0.28f;

                if (Projectile.ai[2] < 0f)
                {
                    burstVolume = 0.85f;
                }
                else if (Projectile.ai[0] >= 64f)
                {
                    burstVolume = 0.08f;
                }

                SoundEngine.PlaySound(SoundID.Item27 with { Volume = burstVolume, Pitch = 0.15f }, Projectile.Center);
            }

            float travel = Travel(Age);
            Player player = Main.LocalPlayer;
            Vector2 radii = Radii;
            int streams = StreamCount;
            if (clearTravel == null)
            {
                clearTravel = new float[streams];
                for (int i = 0; i < streams; i++)
                {
                    float angle = MathHelper.TwoPi * i / streams;
                    Vector2 surface = new Vector2(MathF.Cos(angle) * radii.X, MathF.Sin(angle) * radii.Y);
                    Vector2 origin = Projectile.Center + surface;
                    Vector2 direction = surface.SafeNormalize(Vector2.UnitY);
                    clearTravel[i] = -1f;
                    for (float distance = 0f; distance <= Projectile.ai[0]; distance += 4f)
                    {
                        Vector2 sample = origin + direction * distance;
                        if (Collision.SolidCollision(sample - new Vector2(4f), 8, 8)
                            || !Collision.CanHitLine(origin, 1, 1, sample, 1, 1)) break;
                        clearTravel[i] = distance;
                    }
                }
            }
            float fade = MathHelper.Clamp(Projectile.timeLeft / 8f, 0f, 1f);
            foreach (FrostMote mote in frostDust)
            {
                Dust dust = mote.Dust;
                if (!dust.active || dust.customData != this) continue;
                dust.position = mote.Origin + mote.Direction * (Math.Min(travel, mote.MaximumTravel) - mote.BirthTravel);
                dust.velocity = Vector2.Zero;
                dust.alpha = 80 + (int)(175f * (1f - fade));
            }
            for (int i = 0; i < streams; i++)
            {
                if (clearTravel[i] < 0f) continue;
                float angle = MathHelper.TwoPi * i / streams;
                Vector2 surface = new Vector2(MathF.Cos(angle) * radii.X, MathF.Sin(angle) * radii.Y);
                Vector2 outward = surface.SafeNormalize(Vector2.UnitY);
                Vector2 origin = Projectile.Center + surface;
                Vector2 current = origin + outward * Math.Min(travel, clearTravel[i]);

                if (!touchedLocalPlayer && player.active && !player.dead)
                {
                    float collisionPoint = 0f;
                    // Snow lingers along each emitted stream until the shared final fade, so the
                    // whole visible trail remains cold, with only one application per explosion.
                    if (Collision.CheckAABBvLineCollision(player.position, player.Size, origin, current, 12f, ref collisionPoint)
                        && Collision.CanHitLine(player.Center, 1, 1, current, 1, 1))
                    {
                        touchedLocalPlayer = true;
                        FrostBuildup.Apply(player, (int)Projectile.ai[1]);
                    }
                }

                // The statue is a dense all-direction breath blast; overlapping ground spikes
                // use half the emission cadence to keep their combined dust count bounded.
                if (Projectile.timeLeft > 8 && (Projectile.ai[2] < 0f || Age % 2 == 0))
                {
                    // Birth at the silhouette, then travel along the stream. Clamp each stream's
                    // total range at terrain so neither its particles nor its frost cross walls.
                    float streamTravel = Math.Min(travel, clearTravel[i]);
                    Dust snow = GigasFrostBreathPuff.SpawnFrostDust(origin, Vector2.Zero, 0.85f, true);
                    snow.customData = this;
                    frostDust.Add(new FrostMote(snow, origin, outward, streamTravel, clearTravel[i]));
                    if (Projectile.ai[2] < 0f)
                    {
                        Dust mist = GigasFrostBreathPuff.SpawnFrostDust(origin, Vector2.Zero, 0.9f, false);
                        mist.customData = this;
                        frostDust.Add(new FrostMote(mist, origin, outward, streamTravel, clearTravel[i]));
                    }
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            foreach (FrostMote mote in frostDust)
                if (mote.Dust.customData == this) mote.Dust.active = false;
        }

        float Travel(int age)
        {
            float progress = MathHelper.Clamp(age / (Lifetime - 1f), 0f, 1f);
            return Projectile.ai[0] * (1f - (1f - progress) * (1f - progress));
        }
    }
}
