using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Chaos
{
    ///<summary>
    ///The boundary Wing Buffet shoves you toward: a large ring of dark fire that fades in, holds at a FIXED
    ///radius, and fades out. Not an expanding wave — it does not move, so "safe" is learnable.
    ///
    ///Anchored where Chaos was when the gale began and left there (ShouldUpdatePosition false): a ring that
    ///followed the boss would make the safe zone unreadable. True annulus collision, so the middle is safe and
    ///the band itself is thin enough to dash through; the test is whether you resist the gust, not whether you
    ///can outrun a wall. Damage is armed only once it has fully faded in.
    ///
    ///ai[0] = radius in px. ai[1] = hold ticks between the fades.
    ///</summary>
    class ChaosGaleRing : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(Projectiles.InvisibleNothingProj));

        public const int FadeTicks = 40;
        const float RingHalfThickness = 26f;

        float Radius => Projectile.ai[0] > 0f ? Projectile.ai[0] : 700f;
        int HoldTicks => Projectile.ai[1] > 0f ? (int)Projectile.ai[1] : 180;
        int TotalTicks => FadeTicks * 2 + HoldTicks;
        int Age => TotalTicks - Projectile.timeLeft;

        ///<summary>0 while fading in or out, 1 while held. Drives both the dust density and whether it bites.</summary>
        float Presence
        {
            get
            {
                if (Age < FadeTicks)
                {
                    return Age / (float)FadeTicks;
                }

                if (Age >= FadeTicks + HoldTicks)
                {
                    return 1f - (Age - FadeTicks - HoldTicks) / (float)FadeTicks;
                }

                return 1f;
            }
        }

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = FadeTicks * 2 + 180;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = TotalTicks;

            //Broadphase has to span the whole ring; the real shape is decided in Colliding.
            Projectile.width = (int)(Radius * 2f);
            Projectile.height = (int)(Radius * 2f);
            Projectile.Center = Projectile.position + new Vector2(Projectile.width, Projectile.height) / 2f;
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        public override bool? CanDamage()
        {
            //Harmless while it is still forming or already dissolving — you can only be hit by a ring you can see.
            return Presence > 0.9f;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;

            if (Main.dedServ)
            {
                return;
            }

            //Dust laid around the circumference, thickening as the ring arms itself. Density is the tell for
            //whether it is live yet.
            float presence = Presence;
            int motes = 5 + (int)(presence * 13f);

            for (int i = 0; i < motes; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 spot = Projectile.Center + angle.ToRotationVector2() * (Radius + Main.rand.NextFloat(-RingHalfThickness, RingHalfThickness));

                int dustType = DustID.Shadowflame;
                if (Main.rand.NextBool(3))
                {
                    dustType = DustID.DemonTorch;
                }

                Dust mote = Dust.NewDustPerfect(spot, dustType, angle.ToRotationVector2() * 0.8f, 90, default, Main.rand.NextFloat(1.1f, 1.7f) * (0.5f + presence));
                mote.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            //True annulus: the band only, so standing in the middle is safe and a roll can cross it.
            //Hit when the hitbox's nearest point is inside the outer edge AND its farthest corner is outside
            //the inner edge — i.e. the box actually straddles the band.
            Vector2 center = Projectile.Center;

            Vector2 closest = new Vector2(
                MathHelper.Clamp(center.X, targetHitbox.Left, targetHitbox.Right),
                MathHelper.Clamp(center.Y, targetHitbox.Top, targetHitbox.Bottom));
            float closestDistance = Vector2.Distance(center, closest);

            float farthestX = MathHelper.Max(MathHelper.Distance(center.X, targetHitbox.Left), MathHelper.Distance(center.X, targetHitbox.Right));
            float farthestY = MathHelper.Max(MathHelper.Distance(center.Y, targetHitbox.Top), MathHelper.Distance(center.Y, targetHitbox.Bottom));
            float farthestDistance = (float)System.Math.Sqrt(farthestX * farthestX + farthestY * farthestY);

            return closestDistance <= Radius + RingHalfThickness && farthestDistance >= Radius - RingHalfThickness;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<Buffs.Debuffs.DarkInferno>(), 180);
        }
    }
}
