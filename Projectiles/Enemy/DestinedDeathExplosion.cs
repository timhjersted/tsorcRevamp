using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>
    /// Hardmode-only kill effect for the Red Knight family's thrown spears: the DestinedDeathExplosion
    /// sheet played once, with red and black dust thrown on top of it.
    ///
    /// Purely decorative — the spear's own damage has already resolved by the time this spawns.
    /// It does NOT replace the pre-hardmode effect: callers gate on <c>Main.hardMode</c> and fall
    /// through to whatever they did before, so early-game impacts are untouched.
    ///
    /// Sheet is 52x260 = FrameCount frames of 52x52 (verified by scanning for the fully-transparent
    /// separator rows at y = 50/51, 102/103, 154/155, 206/207, 258/259 — the filename says nothing
    /// about frame count, so it was measured).
    /// </summary>
    public class DestinedDeathExplosion : ModProjectile
    {
        const int FrameCount = 5;
        const int TicksPerFrame = 4;
        const int Lifetime = FrameCount * TicksPerFrame;

        /// <summary>Caller-supplied size multiplier; 0 means 1x.</summary>
        float Scale => Projectile.ai[0] > 0f ? Projectile.ai[0] : 1f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = FrameCount;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<Buffs.Debuffs.DestinedDeath>(), 600);
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (Main.dedServ)
            {
                return;
            }

            // Red and black dust "on top of" the sprite — spawned once at birth rather than per
            // tick so the burst is a single impact, not a lingering emitter.
            for (int i = 0; i < 22; i++)
            {
                bool soot = i % 3 == 0;
                Vector2 velocity = Main.rand.NextVector2Circular(4.2f, 4.2f) * Scale;
                Dust mote = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f) * Scale,
                    soot ? DustID.Shadowflame : DustID.RedTorch, velocity,
                    soot ? 160 : 90,
                    soot ? new Color(18, 4, 10) : new Color(206, 16, 34),
                    Main.rand.NextFloat(0.75f, 1.35f) * Scale);
                mote.noGravity = true;
                mote.fadeIn = soot ? 0.5f : 0f;
            }
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= TicksPerFrame)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
            }
            Lighting.AddLight(Projectile.Center,
                new Vector3(0.62f, 0.05f, 0.06f) * (Projectile.timeLeft / (float)Lifetime));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / FrameCount;
            Rectangle frame = new(0, Utils.Clamp(Projectile.frame, 0, FrameCount - 1) * frameHeight,
                texture.Width, frameHeight);

            // Fade in fast, out slow, so it never pops at either end.
            float life = 1f - Projectile.timeLeft / (float)Lifetime;
            float fade = Utils.Clamp(life * 6f, 0f, 1f) * Utils.Clamp((1f - life) * 2.4f, 0f, 1f);

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame,
                Color.White * fade, Projectile.rotation, frame.Size() * 0.5f,
                Scale, SpriteEffects.None, 0f);
            return false;
        }

        /// <summary>
        /// Spawn helper so the two spear classes cannot drift apart. Returns false when it declined
        /// (not hardmode / on a client), which is the caller's cue to run its old effect instead.
        /// </summary>
        internal static bool TrySpawn(Terraria.DataStructures.IEntitySource source, Vector2 position, float scale)
        {
            if (!Main.hardMode)
            {
                return false;
            }
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(source, position, Vector2.Zero,
                    ModContent.ProjectileType<DestinedDeathExplosion>(), 0, 0f, Main.myPlayer, scale);
            }
            return true;
        }
    }
}
