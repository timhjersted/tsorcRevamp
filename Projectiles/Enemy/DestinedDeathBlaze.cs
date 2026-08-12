using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>
    /// Flame sprite used decoratively by Red Knight standards and as a damaging radial projectile
    /// (<see cref="Weapons.RedKnightGroundWave"/>), which spawns these at an increasing rate as it
    /// travels. Standard callers pass zero damage, while Furnace Herald supplies damage and uses
    /// the compact contact hitbox plus this sprite's normal Destined Death debuff. Decorative mode
    /// does not expand the parent
    /// attack's damage footprint (§39).
    ///
    /// Sheet is 86x410 = FrameCount frames of 86x82 — MEASURED, not assumed: scanning for content
    /// bands puts them at y 32-53, 112-133, 186-221, 264-311 and 340-395, which lands one band
    /// inside each 82px slice. 410 does not divide by 4, so the "4-frame" description was wrong.
    ///
    /// ai[0] = travel direction (-1 / +1)
    /// ai[1] = 0 for a ground-hugging blaze, 1 for one that lifts off near the end of the wave's run,
    ///         and 2 for a spinning Furnace Herald projectile
    /// ai[2] = exact travel distance for Furnace Herald; unused by the decorative modes
    /// </summary>
    public class DestinedDeathBlaze : ModProjectile
    {
        const int FrameCount = 5;
        const int TicksPerFrame = 5;
        const int Lifetime = 46;

        int Direction => Projectile.ai[0] < 0f ? -1 : 1;
        bool Lifting => Projectile.ai[1] > 0.5f;
        bool HeraldWave => Projectile.ai[1] > 1.5f;
        float HeraldTravelDistance => Projectile.ai[2];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = FrameCount;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.scale = 1f;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<Buffs.Debuffs.DestinedDeath>(), 600);
        }

        public override void OnSpawn(IEntitySource source)
        {
            // Start on a random frame and a random scale so a cluster never reads as one stamp
            // repeated (vfx-shader-tips §33) — these spawn several at a time along the wave.
            Projectile.frame = Main.rand.Next(FrameCount);
            Projectile.frameCounter = Main.rand.Next(TicksPerFrame);
            Projectile.scale = Main.rand.NextFloat(0.42f, 0.78f);
            Projectile.rotation = Main.rand.NextFloat(-0.16f, 0.16f);
            if (HeraldWave)
            {
                Vector2 center = Projectile.Center;
                Projectile.width = 24;
                Projectile.height = 24;
                Projectile.Center = center;
                Projectile.scale = Main.rand.NextFloat(0.72f, 1.02f);
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            }
            else if (Lifting)
            {
                Projectile.velocity.Y -= Main.rand.NextFloat(1.4f, 3.1f);
            }
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= TicksPerFrame)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % FrameCount;
            }

            if (HeraldWave)
            {
                Projectile.rotation += Direction * 0.14f;
                float remaining = Math.Max(0f, HeraldTravelDistance - Projectile.localAI[0]);
                float speed = Projectile.velocity.Length();
                if (remaining <= 0f || speed <= 0.001f)
                {
                    Projectile.velocity = Vector2.Zero;
                }
                else
                {
                    if (speed > remaining)
                    {
                        Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * remaining;
                    }
                    Projectile.localAI[0] += Projectile.velocity.Length();
                }
            }
            else if (Lifting)
            {
                // Tip backward, away from the direction of travel.
                Projectile.rotation -= Direction * 0.026f;
                Projectile.velocity = Projectile.velocity.RotatedBy(0.04f);
                Projectile.velocity *= 0.98f;
            }
            else
            {
                Projectile.rotation -= Direction * 0.026f;
                // Keep ground blazes hugging terrain and moving locked with the ground wave
                float groundY = Weapons.PuppetGroundDustWave.FindGroundY(Projectile.Center.X, Projectile.Center.Y + 6f);
                Projectile.Center = new Vector2(Projectile.Center.X, groundY);
                Projectile.velocity.Y = 0f;
                Projectile.velocity.X *= 0.985f;
            }

            // Fire-like particles: DustID.Blood (5) and DustID.Wraith (54) rising from the flames
            if (!Main.dedServ && Main.rand.NextBool(HeraldWave ? 5 : 2))
            {
                int dustCount = HeraldWave ? 1 : 2;
                for (int i = 0; i < dustCount; i++)
                {
                    int dustType = Main.rand.NextBool(2) ? DustID.Blood : DustID.Wraith;
                    Vector2 dustPos = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-14f, 14f) * Projectile.scale, Main.rand.NextFloat(-18f, -2f) * Projectile.scale);
                    Vector2 dustVel = new Vector2(Projectile.velocity.X * 0.2f + Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-2.8f, -1.0f));
                    Dust dust = Dust.NewDustPerfect(dustPos, dustType, dustVel, 100, default, Main.rand.NextFloat(0.9f, 1.4f));
                    dust.noGravity = true;
                }
            }

            Lighting.AddLight(Projectile.Center, new Vector3(0.42f, 0.03f, 0.04f) * Projectile.scale);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / FrameCount;
            Rectangle frame = new(0, Projectile.frame * frameHeight, texture.Width, frameHeight);

            // Smooth in and smooth out — no hard pop at either end.
            float life = 1f - Projectile.timeLeft / (float)Lifetime;
            float fade = Utils.Clamp(life * 4.5f, 0f, 1f) * Utils.Clamp((1f - life) * 2.2f, 0f, 1f);

            // AlphaBlend: renders solid opaque black and rich crimson pixels matching the authored PNG texture
            Vector2 origin = HeraldWave ? frame.Size() * 0.5f : new Vector2(frame.Width * 0.5f, frame.Height);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame,
                Color.White * fade, Projectile.rotation,
                origin, Projectile.scale,
                Direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
            return false;
        }
    }
}
