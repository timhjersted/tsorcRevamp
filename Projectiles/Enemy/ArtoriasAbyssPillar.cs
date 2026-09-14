using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    // Tall narrow abyss eruption at Flip Slash's landing point, spawned bottom-anchored at Artorias's feet. The hitbox is
    // the eruption shader's solid core: 78% of the quad's width, rising from the ground as the shader reveals it.
    class ArtoriasAbyssPillar : ModProjectile
    {
        const int Lifetime = 16;
        internal const int Width = 150;
        internal const int Height = 420;
        // The shader's rise mask reveals a pixel once Progress >= (height fraction from the bottom) * 0.72.
        const float EruptionRiseProgress = 0.72f;
        // ArtoriasAbyssEruption.fx exactCore: |uv.x - 0.5| < 0.39.
        const float EruptionCoreHalfWidth = 0.39f;
        // PreDraw fades over the last FadeTicks; damage ends when the fade starts.
        const int FadeTicks = 5;

        public override string Texture => "tsorcRevamp/NPCs/Puppets/PuppetPlaceholder";

        public override void SetDefaults()
        {
            Projectile.width = Width;
            Projectile.height = Height;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.timeLeft = Lifetime;
        }

        public override bool? CanDamage() => Projectile.timeLeft > FadeTicks;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float progress = 1f - Projectile.timeLeft / (float)Lifetime;
            float riseFraction = MathHelper.Clamp(progress / EruptionRiseProgress, 0f, 1f);
            float coreHalfWidth = Width * EruptionCoreHalfWidth;
            float risenHeight = Height * riseFraction;
            Rectangle core = new Rectangle((int)(Projectile.Center.X - coreHalfWidth), (int)(Projectile.Bottom.Y - risenHeight),
                (int)(coreHalfWidth * 2f), (int)risenHeight);
            return core.Intersects(targetHitbox);
        }

        public override void AI()
        {
            if (Main.dedServ)
            {
                return;
            }

            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 1.25f);

            if (Projectile.timeLeft % 2 == 0)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 pos = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-Width * 0.4f, Width * 0.4f), 0f);
                    Vector2 vel = new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-18f, -6f));
                    Dust d = Dust.NewDustPerfect(pos, DustID.PurpleTorch, vel, 40, new Color(200, 90, 220), 1.5f);
                    d.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = 1f - Projectile.timeLeft / (float)Lifetime;
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            ArtoriasVFX.DrawGroundRift(Projectile.Bottom - new Vector2(0f, 5f),
                new Vector2(Width * 1.56f, 102f), progress, 0.72f * fade);
            ArtoriasVFX.DrawEruption(Projectile.Center, Projectile.Size, progress, 0.92f * fade);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }
            for (int i = 0; i < 24; i++)
            {
                Vector2 position = Projectile.Bottom - new Vector2(Main.rand.NextFloat(-Width * 0.4f, Width * 0.4f),
                    Main.rand.NextFloat(0f, Height));
                Vector2 vel = Main.rand.NextVector2Circular(5f, 5f);
                Dust d = Dust.NewDustPerfect(position, DustID.PurpleTorch, vel, 60, new Color(230, 120, 220), 1.4f);
                d.noGravity = true;
            }
        }
    }
}
