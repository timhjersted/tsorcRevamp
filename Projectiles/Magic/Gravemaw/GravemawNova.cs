using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Magic.Gravemaw
{
    ///<summary>Reliquary Nova: a friendly expanding true-annulus ring (the player's version of the boss's
    ///Soul Nova). Colliding() only hits the current radius band, so it sweeps outward once. ai[0]=max radius.</summary>
    class GravemawNova : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float ExpandSpeed = 9f;
        const float RingHalfThickness = 24f;

        float MaxRadius => Projectile.ai[0] > 0f ? Projectile.ai[0] : 360f;
        float Radius => Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 800;
            Projectile.height = 800;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // hit each NPC once
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source) => Projectile.timeLeft = (int)(MaxRadius / ExpandSpeed) + 2;

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0] += ExpandSpeed;
            for (int i = 0; i < 36; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = Projectile.Center + angle.ToRotationVector2() * (Radius + Main.rand.NextFloat(-8f, 8f));
                int type = Main.rand.NextBool(3) ? DustID.Shadowflame : DustID.PurpleTorch;
                int d = Dust.NewDust(pos, 4, 4, type, 0f, 0f, 80, default, Main.rand.NextFloat(1.2f, 1.7f));
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity = angle.ToRotationVector2() * 2.5f;
            }
            Lighting.AddLight(Projectile.Center, 0.6f, 0.15f, 0.8f);
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
    }
}
