using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    // The mark StickySpitBall leaves behind. Reuses EnemySpellGreatPoisonStrike's 5-frame sprite, but
    // (unlike that projectile, which plays the animation once over ~15 ticks and dies) this loops the
    // cycle continuously for its whole 10-second lifetime and keeps refreshing Poisoned on anyone
    // standing in it.
    public class StickySpitCloud : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/EnemySpellGreatPoisonStrike";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 0.7f;
            Projectile.timeLeft = 10 * 60;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
            }

            // Was 2 grey DustID.Web every single tick at scale 1.2 - a constant grey haze that fought
            // the green sprite. Now poison-green, roughly half the size, and gated so it emits at
            // about a sixth of the old rate.
            if (Main.rand.NextBool(3))
            {
                Dust drip = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    DustID.Poisoned, Main.rand.NextVector2Circular(0.22f, 0.22f),
                    120, default, Main.rand.NextFloat(0.5f, 0.75f));
                drip.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Divisor must match the 10*60 lifetime set in SetDefaults (was 480f, so progress
            // started negative and the cloud faded in late instead of landing at full strength).
            float progress = 1f - Projectile.timeLeft / (10f * 60f);
            EnemyVFX.DrawElandToxicField(Projectile.Center, Vector2.One * 44f, progress, true);
            return true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Poisoned, 10 * 60, false);
        }
    }
}
