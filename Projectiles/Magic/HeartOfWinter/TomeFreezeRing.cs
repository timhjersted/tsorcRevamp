using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Magic
{
    ///<summary>
    ///Heart of Winter right hold: the charged release — an expanding annular freeze wave centered on
    ///the player, the friendly mirror of the boss's Absolute Zero ring. True ring collision; each
    ///NPC is hit once as the wave passes through it. ai[0] = max radius (px).
    ///</summary>
    class TomeFreezeRing : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float ExpandSpeed = 9f;
        const float RingHalfThickness = 22f;

        float MaxRadius => Projectile.ai[0] > 0f ? Projectile.ai[0] : 260f;
        float Radius => Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.width = 560;
            Projectile.height = 560;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; //one hit per NPC as the wave passes
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 40;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = (int)(MaxRadius / ExpandSpeed) + 2;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0] += ExpandSpeed;

            int points = 32;
            for (int i = 0; i < points; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = Projectile.Center + angle.ToRotationVector2() * (Radius + Main.rand.NextFloat(-8f, 8f));
                int dust = Dust.NewDust(pos, 4, 4, DustID.IceTorch, 0f, 0f, 60, default, Main.rand.NextFloat(1.2f, 1.8f));
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = angle.ToRotationVector2() * 2.5f;
            }
            if (Main.rand.NextBool(2))
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = Projectile.Center + angle.ToRotationVector2() * Radius;
                int crystal = Dust.NewDust(pos, 4, 4, DustID.IceRod, 0f, 0f, 40, default, 1f);
                Main.dust[crystal].noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.5f, 0.8f, 1.1f);
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

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn, 4 * 60);
        }
    }
}
