using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.MantassinSorcery
{
    // The Mantassin's floor sigil: the Encircling Nails convergence mark and the Glintstone Blink destination.
    // Harmless; it is the telegraph. Centre ON the floor surface. ai[0] = countdown ticks, ai[1] = width in px.
    // The ring tightens as the countdown runs, then the sigil fades over FadeTicks.
    // MP: spawned by the server with its countdown in ai[]; the local age starts on the same tick everywhere.
    public class MantassinRune : ModProjectile
    {
        private const int FadeTicks = 12;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private int CountdownTicks => (int)Projectile.ai[0];
        private float Width => Projectile.ai[1];

        private int Age
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 300;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Age++;
            if (Age == 1)
            {
                Projectile.timeLeft = CountdownTicks + FadeTicks;
            }
            Projectile.velocity = Vector2.Zero;
            Lighting.AddLight(Projectile.Center, EnemyVFX.MantassinMid.ToVector3() * 0.4f);

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                // Glints rising off the sigil, denser as the countdown runs out.
                float halfWidth = Width * 0.45f;
                Vector2 point = Projectile.Center + new Vector2(Main.rand.NextFloat(-halfWidth, halfWidth), 0f);
                Dust glint = Dust.NewDustPerfect(point, DustID.TintableDustLighted,
                    new Vector2(0f, -Main.rand.NextFloat(0.5f, 1.6f)), 50, EnemyVFX.MantassinMid, 0.7f);
                glint.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float countdown = MathHelper.Clamp(Age / (float)System.Math.Max(1, CountdownTicks), 0f, 1f);
            float opacity = MathHelper.Clamp(Age / 10f, 0f, 1f);
            int fadeAge = Age - CountdownTicks;
            if (fadeAge > 0)
            {
                opacity *= 1f - MathHelper.Clamp(fadeAge / (float)FadeTicks, 0f, 1f);
            }

            EnemyVFX.DrawMantassinRune(Projectile.Center, Width, countdown, opacity);
            return false;
        }
    }
}
