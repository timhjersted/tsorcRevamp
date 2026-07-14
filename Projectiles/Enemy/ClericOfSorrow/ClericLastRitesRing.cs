using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.ClericOfSorrow
{
    ///<summary>
    ///Last Rites: the Cleric of Sorrow's dying curse-burst. An expanding green cursed-flame ring (the same
    ///true-annulus collision as GigasFreezeRing — inside is safe, roll-through-able) that erupts from the
    ///cleric's body at death below 5% HP. Hitting the player inflicts Cursed Inferno. The wider curse that
    ///empowers nearby enemies is applied by the NPC itself at the moment of detonation. ai[0] = max radius.
    ///</summary>
    class ClericLastRitesRing : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float ExpandSpeed = 7f;
        const float RingHalfThickness = 24f;

        float MaxRadius => Projectile.ai[0] > 0f ? Projectile.ai[0] : 260f;
        float Radius => Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 560;
            Projectile.height = 560;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 60;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = (int)(MaxRadius / ExpandSpeed) + 2;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0] += ExpandSpeed;

            int points = 40;
            for (int i = 0; i < points; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = Projectile.Center + angle.ToRotationVector2() * (Radius + Main.rand.NextFloat(-10f, 10f));
                int dust = Dust.NewDust(pos, 4, 4, DustID.CursedTorch, 0f, 0f, 80, default, Main.rand.NextFloat(1.4f, 2f));
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = angle.ToRotationVector2() * 2.5f;
            }
            Lighting.AddLight(Projectile.Center, 0.2f, 0.7f, 0.2f);
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
            target.AddBuff(BuffID.CursedInferno, 5 * 60);
        }
    }
}
