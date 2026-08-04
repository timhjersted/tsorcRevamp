using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    // One puff of Eland's poison trail: a floating, dust-only (no drawn sprite) gas patch dropped
    // behind it while the trail attack is emitting. Stationary and ignores tiles — it hangs in the
    // air where it was dropped and fades out after its lifetime.
    public class PoisonTrailCloud : ModProjectile
    {
        // Dust-only (hide = true below) - reuse the shared invisible placeholder rather than new art,
        // same convention as ArtoriasAbyssBlast.cs/BlindingPulse.cs.
        public override string Texture => "tsorcRevamp/NPCs/Puppets/PuppetPlaceholder";

        public const int MinSize = 48; // 3 tiles

        public override void SetDefaults()
        {
            Projectile.width = MinSize;
            Projectile.height = MinSize;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.hide = false; //transparent placeholder; shader is drawn in PreDraw
            Projectile.light = 0.4f;
            Projectile.timeLeft = 6 * 60;
        }

        public override void AI()
        {
            // Fill the box with lazily-drifting toxic dust; fades in/out with remaining lifetime.
            if (Main.rand.NextBool(5))
            {
                for (int i = 0; i < 2; i++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Poisoned, 0f, 0f, 100, default, 2f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= 0.3f;
                }
            }

            Lighting.AddLight(Projectile.Center, 0.05f, 0.3f, 0.05f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = 1f - Projectile.timeLeft / 180f;
            EnemyVFX.DrawElandToxicField(Projectile.Center, Vector2.One * 36f, progress, true, false);
            return true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Poisoned, 12 * 60, false);
        }
    }
}
