using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    // A marked glyph EvilEye drops on the player's position: harmless while it closes in on
    // itself, then briefly becomes a damaging hitbox at the moment it detonates.
    public class EvilEyeGroundBlast : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Ice1Ball";

        const float TelegraphTicks = 35f;
        const float DetonationWindow = 3f;

        public override void SetDefaults()
        {
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 60;
            Projectile.damage = 0;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.knockBack = 4f;
        }

        public override void AI()
        {
            Projectile.ai[0]++;
            Lighting.AddLight(Projectile.Center, 0.15f, 0.25f, 0.6f);

            if (Projectile.ai[0] < TelegraphTicks)
            {
                float t = Projectile.ai[0] / TelegraphTicks;
                if (Main.rand.NextFloat() < 0.35f + t * 0.3f)
                {
                    Vector2 edge = Main.rand.NextVector2CircularEdge(30f, 30f) * (1f - t * 0.5f);
                    int dust = Dust.NewDust(Projectile.Center + edge, 2, 2, DustID.BlueTorch, 0f, 0f, 150, default, 1f + t);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity = -edge * 0.06f;
                }
            }
            else if (Projectile.ai[0] == TelegraphTicks)
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.8f, Pitch = -0.2f }, Projectile.Center);
                UsefulFunctions.DustRingPrecise(Projectile.Center, 32f, DustID.BlueTorch, 24, 3f, 150, 1.4f);
                Lighting.AddLight(Projectile.Center, 0.6f, 0.8f, 1.6f);
                Projectile.damage = 30;
            }
            else if (Projectile.ai[0] > TelegraphTicks + DetonationWindow)
            {
                Projectile.Kill();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = MathHelper.Clamp(Projectile.ai[0] / TelegraphTicks, 0f, 1f);
            bool active = Projectile.ai[0] >= TelegraphTicks;
            EnemyVFX.DrawEvilEyeGroundGlyph(Projectile.Center, progress, active);
            return true;
        }
    }
}
