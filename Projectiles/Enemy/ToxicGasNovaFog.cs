using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    // The lingering fog left behind when Eland's Toxic Gas Nova detonates — fills the entire blast
    // radius with drifting poison dust for a few seconds after the initial burst. Dust-only (no drawn
    // sprite, like PoisonTrailCloud); collision is overridden to a true circle (Colliding below) rather
    // than the square Projectile hitbox, so it only actually hits someone genuinely inside the ring.
    public class ToxicGasNovaFog : ModProjectile
    {
        // Dust-only (hide = true below) - reuse the shared invisible placeholder rather than new art,
        // same convention as PoisonTrailCloud.cs/ArtoriasAbyssBlast.cs/BlindingPulse.cs.
        public override string Texture => "tsorcRevamp/NPCs/Puppets/PuppetPlaceholder";

        public const int Diameter = 800; // 2x Eland's 400px nova radius

        public override void SetDefaults()
        {
            Projectile.width = Diameter;
            Projectile.height = Diameter;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.hide = false; //transparent placeholder; shader is drawn in PreDraw
            Projectile.light = 0.4f;
            Projectile.timeLeft = 4 * 60;
        }

        public override void AI()
        {
            float radius = Projectile.width / 2f;
            for (int i = 0; i < 1; i++)
            {
                Vector2 spot = Projectile.Center + Main.rand.NextVector2Circular(radius, radius);
                int dust = Dust.NewDust(spot, 1, 1, DustID.Poisoned, 0f, 0f, 150, default, 1.6f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = Main.rand.NextVector2Circular(0.4f, 0.4f);
                if (Projectile.timeLeft < 60)
                {
                    Main.dust[dust].alpha = 255 - (int)(255 * (Projectile.timeLeft / 60f));
                }
            }

            Lighting.AddLight(Projectile.Center, 0.1f, 0.4f, 0.1f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = 1f - Projectile.timeLeft / (4f * 60f);
            EnemyVFX.DrawElandToxicField(Projectile.Center, Vector2.One * Diameter, progress, true, true);
            return false;
        }

        // True circular hit-test instead of the square AABB, so only players genuinely inside the
        // 400px blast radius (not just inside its bounding box) take the hit.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float radius = Projectile.width / 2f;
            Vector2 closest = Vector2.Clamp(Projectile.Center,
                new Vector2(targetHitbox.Left, targetHitbox.Top),
                new Vector2(targetHitbox.Right, targetHitbox.Bottom));
            return Vector2.DistanceSquared(Projectile.Center, closest) <= radius * radius;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Poisoned, 12 * 60, false);
        }
    }
}
