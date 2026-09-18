using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles;

namespace tsorcRevamp.Projectiles.Enemy.OolacileSorcerer
{
    /// <summary>
    /// Corruption Surge's expanding abyss ring: a TRUE ANNULUS, so standing inside it (next to the boss)
    /// is safe and rolling outward through the band is the other answer. Dust-only — the ring the player
    /// sees IS the hitbox, drawn at the same radius every tick.
    /// ai[0] = max radius (px, default 520). Each player is hit at most once.
    /// </summary>
    class OccultistAbyssRing : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        // Expansion is deliberately slower than a roll (8 px/tick for the first 11 ticks): the band can be
        // outrun outward as well as rolled through, so it never becomes an unavoidable wall.
        private const float ExpandSpeed = 6.5f;
        private const float RingHalfThickness = 26f;
        private const int FadeTicks = 16;

        private readonly bool[] _hitPlayers = new bool[Main.maxPlayers];

        private float MaxRadius => Projectile.ai[0] > 0f ? Projectile.ai[0] : 520f;
        private int Duration => (int)(MaxRadius / ExpandSpeed) + FadeTicks;
        private float Radius => Math.Min(MaxRadius, Projectile.localAI[0] * ExpandSpeed);

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.friendly = false;
            // Broadphase box must span the full damage diameter — NewProjectile centres on the spawn point.
            Projectile.width = 1120;
            Projectile.height = 1120;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 600;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = Duration;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;

            if (Main.dedServ)
            {
                return;
            }

            // Draw the band the collision actually uses: 44 motes scattered across the ring's thickness,
            // fading with the projectile so the hazard never lingers invisibly.
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            int moteCount = (int)(44f * fade);
            for (int i = 0; i < moteCount; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float bandOffset = Main.rand.NextFloat(-RingHalfThickness, RingHalfThickness);
                Vector2 position = Projectile.Center + angle.ToRotationVector2() * (Radius + bandOffset);
                Dust mote = Dust.NewDustPerfect(position, DustID.Shadowflame,
                    angle.ToRotationVector2() * 2.2f, 60, default, Main.rand.NextFloat(1.4f, 2.2f));
                mote.noGravity = true;
            }

            // A thin bright crest on the leading edge so the outer boundary — the part you have to clear —
            // reads separately from the trailing haze.
            for (int i = 0; i < 10; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 position = Projectile.Center + angle.ToRotationVector2() * (Radius + RingHalfThickness);
                Dust crest = Dust.NewDustPerfect(position, DustID.CrimsonSpray,
                    angle.ToRotationVector2() * 3f, 40, default, 1.5f * fade);
                crest.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, 0.9f, 0.15f, 0.25f);
        }

        // True annulus: hit only when the hitbox straddles the band — its closest point is inside the outer
        // edge AND its farthest corner is outside the inner edge. Standing in the middle stays safe.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 center = Projectile.Center;
            Vector2 closest = new Vector2(
                MathHelper.Clamp(center.X, targetHitbox.Left, targetHitbox.Right),
                MathHelper.Clamp(center.Y, targetHitbox.Top, targetHitbox.Bottom));
            float closestDistance = Vector2.Distance(center, closest);

            float farthestDistance = 0f;
            for (int corner = 0; corner < 4; corner++)
            {
                float cornerX = (corner & 1) == 0 ? targetHitbox.Left : targetHitbox.Right;
                float cornerY = (corner & 2) == 0 ? targetHitbox.Top : targetHitbox.Bottom;
                float distance = Vector2.Distance(center, new Vector2(cornerX, cornerY));
                if (distance > farthestDistance)
                {
                    farthestDistance = distance;
                }
            }

            return closestDistance <= Radius + RingHalfThickness
                && farthestDistance >= Radius - RingHalfThickness;
        }

        public override bool CanHitPlayer(Player target) => !_hitPlayers[target.whoAmI];

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            _hitPlayers[target.whoAmI] = true;
        }
    }
}
