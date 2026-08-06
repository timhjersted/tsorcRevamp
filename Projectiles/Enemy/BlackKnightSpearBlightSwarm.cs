using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>
    /// Purely decorative: six blight-cloud puffs (two instances each of BlightCloud1/2/3), rotating
    /// and smoothly fading in and out at random spots within the damaging radius of a Black Knight /
    /// Great Black Knight spear's on-hit plague cloud (PlagueTeleportCloud, spawned alongside this in
    /// BlackThrowingSpear.OnKill). Kept as its own class rather than folded into
    /// PlagueTeleportCloud.cs — that class is also used by every Plague-style teleport in the mod
    /// (Artorias, this knight's own teleport, CursedDragonInvader), and this effect should NOT show
    /// up there.
    /// </summary>
    public class BlackKnightSpearBlightSwarm : ModProjectile
    {
        const int PuffCount = 6;
        const int Lifetime = 180; // matches PlagueTeleportCloud.LifetimeTicks

        static Asset<Texture2D>[] textures;

        struct Puff
        {
            public int Variant;        // 0..2, indexes textures[]
            public Vector2 Offset;     // from Projectile.Center
            public float Rotation;
            public float RotationSpeed;
            public int CycleLength;    // ticks for one full fade in -> hold -> fade out loop
            public int CycleTimer;
            public float Scale;
        }

        Puff[] puffs;

        // Half the spear-hit cloud's own radius by default, so the swarm reads as scattered THROUGH
        // the damaging area rather than spilling past its edge.
        float Radius => Projectile.ai[0] > 0f ? Projectile.ai[0] : 50f;

        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override bool? CanDamage() => false;

        void LoadPuffs()
        {
            if (puffs != null)
            {
                return;
            }

            textures ??= new[]
            {
                ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/BlightCloud1"),
                ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/BlightCloud2"),
                ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/BlightCloud3"),
            };

            puffs = new Puff[PuffCount];
            for (int i = 0; i < PuffCount; i++)
            {
                // Two of each variant (0,1,2,0,1,2), each with its own random spot/timing.
                puffs[i] = NewPuff(i % 3);
                // Stagger the starting phase so all six don't pulse in lockstep.
                puffs[i].CycleTimer = Main.rand.Next(puffs[i].CycleLength);
            }
        }

        Puff NewPuff(int variant)
        {
            float distance = Main.rand.NextFloat(0.15f, 0.9f) * Radius;
            float angle = Main.rand.NextFloat(MathHelper.TwoPi);
            float spin = Main.rand.NextFloat(0.012f, 0.03f) * (Main.rand.NextBool() ? 1f : -1f);
            return new Puff
            {
                Variant = variant,
                Offset = angle.ToRotationVector2() * distance,
                Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
                RotationSpeed = spin,
                CycleLength = Main.rand.Next(50, 85),
                CycleTimer = 0,
                Scale = Main.rand.NextFloat(0.85f, 1.3f),
            };
        }

        public override void AI()
        {
            LoadPuffs();
            for (int i = 0; i < puffs.Length; i++)
            {
                puffs[i].Rotation += puffs[i].RotationSpeed;
                puffs[i].CycleTimer++;
                if (puffs[i].CycleTimer >= puffs[i].CycleLength)
                {
                    // Re-roll a fresh spot, variant and timing each time a puff completes its fade
                    // cycle — reads as gas billowing around the area rather than six fixed puffs
                    // breathing in place for the whole lifetime.
                    puffs[i] = NewPuff(Main.rand.Next(3));
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LoadPuffs();

            // Overall envelope: quick fade in, hold, fade out only near the very end of life — the
            // same "hold curve" the rest of this kit's lingering effects use, so the swarm doesn't
            // just blink out mid-pulse when the cloud's lifetime runs out.
            float lifeFraction = 1f - Projectile.timeLeft / (float)Lifetime;
            float envelope = MathHelper.Clamp(lifeFraction / 0.08f, 0f, 1f)
                * MathHelper.Clamp((1f - lifeFraction) / 0.15f, 0f, 1f);

            SpriteBatch spriteBatch = Main.spriteBatch;
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            foreach (Puff puff in puffs)
            {
                float t = MathHelper.Clamp(puff.CycleTimer / (float)puff.CycleLength, 0f, 1f);
                // sin(pi*t): 0 at both ends of the cycle, 1 at the midpoint — a smooth
                // appear -> hold -> disappear pulse, not a hard on/off blink.
                float pulse = (float)Math.Sin(MathHelper.Pi * t);
                float alpha = pulse * envelope;
                if (alpha <= 0.01f)
                {
                    continue;
                }

                Texture2D tex = textures[puff.Variant].Value;
                // Color * scalar scales R,G,B,A together — this is ordinary SpriteBatch vertex-color
                // tinting (not the premultiplied-alpha shader trap), so this is the correct way to
                // both tint the sprite's native blue toward plague-purple AND fade it in one step.
                Color drawColor = new Color(150, 80, 200) * alpha;
                Vector2 drawPosition = Projectile.Center + puff.Offset - Main.screenPosition;
                spriteBatch.Draw(tex, drawPosition, null, drawColor, puff.Rotation,
                    tex.Size() * 0.5f, puff.Scale, SpriteEffects.None, 0f);
            }

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
