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

            if (Main.rand.NextBool(6))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Poisoned, 0f, 0f, 150, default, 1f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.2f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = 1f - Projectile.timeLeft / (10f * 60f);
            EnemyVFX.DrawElandToxicField(Projectile.Center, new Vector2(Projectile.width, Projectile.height), progress, true, false);
            return true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Poisoned, 10 * 60, false);
        }
    }
}
