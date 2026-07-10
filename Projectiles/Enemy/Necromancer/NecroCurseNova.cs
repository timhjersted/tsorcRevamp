using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Curse nova: the cornered caster's answer — a small expanding ring of shadow that shoves the
    ///player away and dims their sight. True annulus collision (inside-safe, roll through it).
    ///ai[0] = max radius (px).
    ///</summary>
    class NecroCurseNova : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float ExpandSpeed = 7f;
        const float RingHalfThickness = 18f;

        float MaxRadius => Projectile.ai[0] > 0f ? Projectile.ai[0] : 112f;
        float Radius => Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 260;
            Projectile.height = 260;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 20;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = (int)(MaxRadius / ExpandSpeed) + 2;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0] += ExpandSpeed;

            for (int i = 0; i < 20; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = Projectile.Center + angle.ToRotationVector2() * (Radius + Main.rand.NextFloat(-6f, 6f));
                int dust = Dust.NewDust(pos, 4, 4, DustID.Shadowflame, 0f, 0f, 60, default, Main.rand.NextFloat(1.1f, 1.6f));
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = angle.ToRotationVector2() * 2f;
            }
            Lighting.AddLight(Projectile.Center, 0.4f, 0.2f, 0.6f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = new Vector2(
                MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right),
                MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            float distClosest = Vector2.Distance(Projectile.Center, closest);
            float distFarthest = 0f;
            distFarthest = System.Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Left, targetHitbox.Top)));
            distFarthest = System.Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Right, targetHitbox.Top)));
            distFarthest = System.Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Left, targetHitbox.Bottom)));
            distFarthest = System.Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Right, targetHitbox.Bottom)));
            return distClosest <= Radius + RingHalfThickness && distFarthest >= Radius - RingHalfThickness;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Darkness, 5 * 60);
            target.AddBuff(BuffID.Slow, 2 * 60);
        }
    }
}
