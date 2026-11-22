using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy.Weapons
{
    /// <summary>
    /// The Great Black Knight's Skyfall Flail landing: a 96px Blight burst at his feet, live for 10 ticks (rollable).
    /// Dust-only (no sprite): on its first tick a ring of Blight geysers erupts outward along the floor exactly to the
    /// damage radius, so what is drawn is what hits. Spawned server-side at NPC.Bottom - 8px.
    /// </summary>
    public class GreatBlackKnightSkyfallBurst : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public const float Radius = 96f;
        const int LiveTicks = 10;

        public override void SetDefaults()
        {
            Projectile.width = (int)(Radius * 2f);
            Projectile.height = (int)(Radius * 2f);
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = LiveTicks;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            if (Projectile.localAI[0] > 0f || Main.dedServ)
            {
                return;
            }
            Projectile.localAI[0] = 1f;

            // Geysers: columns of Wraith rising at even steps out to the radius on both sides, a yellow ember in each.
            for (float offset = -Radius; offset <= Radius; offset += 12f)
            {
                Vector2 basePosition = Projectile.Center + new Vector2(offset, 0f);
                float edge = 1f - Math.Abs(offset) / Radius;
                for (int i = 0; i < 3; i++)
                {
                    Vector2 rise = new Vector2(offset * 0.02f, -Main.rand.NextFloat(2f, 5f) * (0.5f + edge));
                    Dust geyser = Dust.NewDustPerfect(basePosition, DustID.Wraith, rise, 100, Color.Black, Main.rand.NextFloat(1.2f, 1.8f));
                    geyser.noGravity = true;
                }
                Dust ember = Dust.NewDustPerfect(basePosition, DustID.YellowTorch, new Vector2(0f, -Main.rand.NextFloat(1f, 3f)), 100, default, 1.1f);
                ember.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = Vector2.Clamp(Projectile.Center,
                new Vector2(targetHitbox.Left, targetHitbox.Top),
                new Vector2(targetHitbox.Right, targetHitbox.Bottom));
            return Vector2.DistanceSquared(Projectile.Center, closest) <= Radius * Radius;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            BlightBuildup.Apply(target);
        }
    }
}
