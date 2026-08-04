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

        // Per-puff quad rotation, rolled once on spawn. The fog shader samples its noise in local UV
        // space, so without this every puff samples the identical pattern and a row of them reads as
        // the same stamp repeated - which is exactly why the trail looked like separate pieces rather
        // than one gas cloud. Rotating the quad rotates the sampled noise; the fog is radially
        // symmetric so nothing else changes. Stored in ai[1] so it survives multiplayer sync.
        float PuffRotation => Projectile.ai[1];

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (Projectile.ai[1] == 0f)
            {
                Projectile.ai[1] = Main.rand.NextFloat(MathHelper.TwoPi);
                Projectile.netUpdate = true;
            }
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

            // Gentle rise + sway so the puff is never static; neighbouring puffs drift apart slightly
            // and their soft edges knit together instead of sitting as a rigid row of discs.
            Projectile.position.Y -= 0.09f;
            Projectile.position.X += (float)System.Math.Sin((Projectile.whoAmI * 0.7f)
                + Main.GlobalTimeWrappedHourly * 0.8f) * 0.10f;

            Lighting.AddLight(Projectile.Center, 0.05f, 0.3f, 0.05f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Divisor must match the 6*60 lifetime set in SetDefaults. It was 180f, so progress
            // started at -1 and the shader's fade curve rendered the cloud completely INVISIBLE for
            // its first ~1.5s - it only became visible during its back half.
            float progress = 1f - Projectile.timeLeft / (6f * 60f);
            // Drawn well wider than the 48px hitbox so consecutive puffs physically overlap and blend
            // into a continuous bank of gas; the shader's circular cutoff keeps the extra size soft.
            EnemyVFX.DrawElandToxicField(Projectile.Center, Vector2.One * 92f, progress, true, PuffRotation);
            return true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Poisoned, 12 * 60, false);
        }
    }
}
