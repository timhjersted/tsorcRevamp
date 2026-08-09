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
    /// Decorative flame sprite that rides the Red Knight standards' ground shockwave
    /// (<see cref="Weapons.RedKnightGroundWave"/>), which spawns these at an increasing rate as it
    /// travels. Same "spawn a short-lived sprite alongside the damaging projectile" pattern the
    /// knight's other secondary VFX use; it carries no hitbox of its own so it cannot change the
    /// attack's damage footprint (§39).
    ///
    /// Sheet is 86x410 = FrameCount frames of 86x82 — MEASURED, not assumed: scanning for content
    /// bands puts them at y 32-53, 112-133, 186-221, 264-311 and 340-395, which lands one band
    /// inside each 82px slice. 410 does not divide by 4, so the "4-frame" description was wrong.
    ///
    /// ai[0] = travel direction (-1 / +1)
    /// ai[1] = 0 for a ground-hugging blaze, 1 for one that lifts off near the end of the wave's run
    /// </summary>
    public class DestinedDeathBlaze : ModProjectile
    {
        const int FrameCount = 5;
        const int TicksPerFrame = 5;
        const int Lifetime = 46;

        int Direction => Projectile.ai[0] < 0f ? -1 : 1;
        bool Lifting => Projectile.ai[1] > 0.5f;

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
            if (Lifting)
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

            // Tip BACKWARD, away from the direction of travel
            Projectile.rotation -= Direction * 0.026f;

            if (Lifting)
            {
                Projectile.velocity = Projectile.velocity.RotatedBy(0.04f);
                Projectile.velocity *= 0.98f;
            }
            else
            {
                // Keep ground blazes hugging terrain and moving locked with the ground wave
                float groundY = Weapons.PuppetGroundDustWave.FindGroundY(Projectile.Center.X, Projectile.Center.Y + 6f);
                Projectile.Center = new Vector2(Projectile.Center.X, groundY);
                Projectile.velocity.Y = 0f;
                Projectile.velocity.X *= 0.985f;
            }

            // Fire-like particles: DustID.Blood (5) and DustID.Wraith (54) rising from the flames
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                for (int i = 0; i < 2; i++)
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
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame,
                Color.White * fade, Projectile.rotation,
                new Vector2(frame.Width * 0.5f, frame.Height), Projectile.scale,
                Direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
            return false;
        }
    }
}
