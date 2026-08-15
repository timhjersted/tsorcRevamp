using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    // Lobbed poison globule used by Eland's spit combos. Manual gravity (Projectile.ai[0] holds the
    // constant used to compute the launch velocity via UsefulFunctions.BallisticTrajectory, so the
    // per-tick integration here must match) instead of a vanilla aiStyle, so the arc lands exactly on
    // the frozen aim point regardless of the projectile's own physics quirks.
    public class PoisonSpitBall : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/EnemySpellGreatPoisonStrikeBall";

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.light = 0.5f;
            Projectile.timeLeft = 180;
        }

        public override void AI()
        {
            Projectile.velocity.Y += Projectile.ai[0];
            Projectile.rotation = Projectile.velocity.ToRotation();

            if (Main.rand.NextBool(3))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Poisoned, 0f, 0f, 100, default, 1f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.3f;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Poisoned, 6 * 60, false);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Short + fat: was 54x18, which read as a thin green line rather than a blob of poison liquid.
            EnemyVFX.DrawElandVenomProjectile(Projectile.Center, Projectile.velocity, new Vector2(30f, 24f));
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            EnemyShaderBurst.Spawn(Projectile.GetSource_Death(), Projectile.Center, EnemyVFXBurstKind.ElandVenomImpact);
            // The splat visual is 92px wide but used to deal no damage whatsoever. Give it a matching
            // hitbox so the whole visible splash actually connects.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                // ai[1] carries Combo B's 60-tick cloud damage window. Other volleys leave it at
                // zero and retain ElandVenomSplash's short impact-only damage timing.
                Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<ElandVenomSplash>(), Projectile.damage, 0f, Projectile.owner,
                    0f, Projectile.ai[1]);
            }
            for (int i = 0; i < 8; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Poisoned, 0f, 0f, 100, default, 1.3f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 1.5f;
            }
        }
    }
}
