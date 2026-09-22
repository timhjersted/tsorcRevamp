using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    public class EnemyMeteorStormMeteor : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(Meteor));

        // Ground warning mark: the spawner stores the impact point it aimed at in ai[0]/ai[1] (world
        // coords). Every peer draws an intensifying ring there for MarkWarnTicks — a plain per-client
        // local counter (localAI[0]), never synced, since it's cosmetic only and every peer counts up
        // from the same spawn state. This replaces the old "falls with no ground telegraph" behavior:
        // the mark shows where impact lands for the whole flight, brightening as it nears.
        private const float MarkWarnTicks = 60f;

        public override void SetDefaults()
        {
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.height = 48;
            Projectile.width = 48;
            Projectile.light = 0.85f;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 200;
            Projectile.extraUpdates = 1;
            Projectile.scale = 1.2f;
        }

        public override void AI()
        {
            int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 293, Projectile.velocity.X / 2, Projectile.velocity.Y / 2, 160, default, 3.2f);
            Main.dust[dust].noGravity = true;
            dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 130, Projectile.velocity.X / 2, Projectile.velocity.Y / 2, 220, default, 1.05f);
            Main.dust[dust].noGravity = true;

            Projectile.localAI[0] = System.Math.Min(Projectile.localAI[0] + 1f, MarkWarnTicks);
            float markProgress = Projectile.localAI[0] / MarkWarnTicks;
            Vector2 markPos = new Vector2(Projectile.ai[0], Projectile.ai[1]);
            // Sparse and dim early, denser and brighter as impact nears - the "it's about to land here" beat.
            if (Main.rand.NextBool(markProgress < 1f ? 3 : 1))
            {
                Vector2 ringSpot = markPos + Main.rand.NextVector2CircularEdge(20f, 8f);
                Dust mark = Dust.NewDustPerfect(ringSpot, DustID.Torch, Vector2.Zero, 120, default, 1.0f + markProgress);
                mark.noGravity = true;
                mark.fadeIn = 0.6f;
            }
            Lighting.AddLight(markPos, 0.3f * markProgress, 0.15f * markProgress, 0.05f * markProgress);
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 30; i++)
            {
                int dust = Dust.NewDust(new Vector2(Projectile.position.X - Projectile.width / 2f, Projectile.position.Y - Projectile.height / 2f), Projectile.width, Projectile.height, 293, Main.rand.Next(-10, 10) + Projectile.velocity.X, Main.rand.Next(-10, 10) + Projectile.velocity.Y, 160, default, 3f);
                Main.dust[dust].noGravity = true;
                dust = Dust.NewDust(new Vector2(Projectile.position.X - Projectile.width / 2f, Projectile.position.Y - Projectile.height / 2f), Projectile.width, Projectile.height, 130, Main.rand.Next(-10, 10) + Projectile.velocity.X, Main.rand.Next(-10, 10) + Projectile.velocity.Y, 160, default, 1.5f);
                Main.dust[dust].noGravity = true;
            }

            Projectile.penetrate = 20;
            Vector2 oldCenter = Projectile.Center;
            Projectile.width = 200;
            Projectile.height = 200;
            Projectile.position = oldCenter - new Vector2(Projectile.width / 2f, Projectile.height / 2f);
            Projectile.damage /= 2;
            Projectile.Damage();
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire3, 180);
        }
    }
}
