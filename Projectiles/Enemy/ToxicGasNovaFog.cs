using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    // The lingering fog left behind when Eland's Toxic Gas Nova detonates. It is no longer a
    // fully-formed cloud that simply fades: it EXPANDS out of Eland over four seconds, holds at full
    // size for four more, then dissipates over three - and it stops damaging one second before it
    // finishes fading, so the tail end of the visual is safe to walk through. That growth window is
    // the counterplay: you can see the edge coming and outrun it.
    //
    // Collision is a true circle against the *current animated* radius (Colliding below) rather than
    // the square Projectile hitbox, so it only ever hits someone genuinely inside the visible gas.
    public class ToxicGasNovaFog : ModProjectile
    {
        // Dust-only (hide = true below) - reuse the shared invisible placeholder rather than new art,
        // same convention as PoisonTrailCloud.cs/ArtoriasAbyssBlast.cs/BlindingPulse.cs.
        public override string Texture => "tsorcRevamp/NPCs/Puppets/PuppetPlaceholder";

        public const int Diameter = 800; // 2x Eland's 400px nova radius
        const float MaxRadius = Diameter / 2f;

        const int GrowTicks = 4 * 60;      // expand out of Eland
        const int HoldTicks = 4 * 60;      // full size
        const int FadeTicks = 3 * 60;      // dissipate
        const int DamageStopTicks = GrowTicks + HoldTicks + 2 * 60; // harmless for the last second
        const int TotalTicks = GrowTicks + HoldTicks + FadeTicks;

        int Age => TotalTicks - Projectile.timeLeft;

        /// <summary>Animated radius: eases out of Eland to full size, then holds.</summary>
        float CurrentRadius
        {
            get
            {
                float grow = MathHelper.Clamp(Age / (float)GrowTicks, 0f, 1f);
                // Ease-out so it bursts outward quickly then settles, rather than creeping linearly.
                grow = grow * (2f - grow);
                return MathHelper.Lerp(24f, MaxRadius, grow);
            }
        }

        public override void SetDefaults()
        {
            // Kept at the maximum extent for broadphase; the real hit-test is the circle below.
            Projectile.width = Diameter;
            Projectile.height = Diameter;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.hide = false; //transparent placeholder; shader is drawn in PreDraw
            Projectile.light = 0.4f;
            Projectile.timeLeft = TotalTicks;
        }

        public override void AI()
        {
            float radius = CurrentRadius;
            // Dust density scales with area so the cloud looks equally thick at every size.
            int puffs = 1 + (int)(radius / 170f);
            for (int i = 0; i < puffs; i++)
            {
                Vector2 spot = Projectile.Center + Main.rand.NextVector2Circular(radius, radius);
                int dust = Dust.NewDust(spot, 1, 1, DustID.Poisoned, 0f, 0f, 150, default, 1.6f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = new Vector2(Main.rand.NextFloat(-0.2f, 0.2f), Main.rand.NextFloat(-0.4f, -0.1f));
            }

            Lighting.AddLight(Projectile.Center, 0.1f, 0.4f, 0.1f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = Age / (float)TotalTicks;
            // Drawn at the animated diameter so the shader's fog grows with the hitbox. The shader's
            // hold curve fades from progress 0.72 (~tick 475) to 1.0, which lines up with the fade
            // window starting at tick 480.
            EnemyVFX.DrawElandToxicField(Projectile.Center, Vector2.One * CurrentRadius * 2f, progress, true);
            return true;
        }

        // Harmless for the final second of the dissipate, so the last wisps can't chip you.
        public override bool? CanDamage() => Age < DamageStopTicks;

        // True circular hit-test against the CURRENT radius instead of the square AABB, so only
        // players genuinely inside the visible gas (not just inside its bounding box) take the hit.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float radius = CurrentRadius;
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
