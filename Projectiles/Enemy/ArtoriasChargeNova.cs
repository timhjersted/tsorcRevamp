using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    // One-shot giant AOE detonation for Artorias's health-threshold charge-up nova. Radius is set
    // per-use via ai[0] (500/600/700px across the three HP-threshold triggers) since a single fixed
    // size can't cover all three call sites.
    class ArtoriasChargeNova : ModProjectile
    {
        const int Lifetime = 18;

        float Radius => Projectile.ai[0];

        public override string Texture => "tsorcRevamp/NPCs/Invaders/InvaderPlaceholder";

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.timeLeft = Lifetime;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Vector2 center = Projectile.Center;
            Projectile.width = Projectile.height = (int)(Radius * 2f);
            Projectile.Center = center;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 1.25f);

            for (int i = 0; i < 6; i++)
            {
                Color tint = Main.rand.NextBool(3) ? (Main.rand.NextBool() ? Color.Black : Color.White) : default;
                Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(Radius, Radius);
                Dust d = Dust.NewDustPerfect(pos, DustID.PurpleTorch, Vector2.Zero, 40, tint, Main.rand.NextFloat(1.4f, 2.2f));
                d.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Vector2.Distance(Projectile.Center, targetHitbox.Center.ToVector2()) <= Radius;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 80; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(9f, 9f);
                Color tint = Main.rand.NextBool() ? (Main.rand.NextBool() ? Color.Black : Color.White) : default;
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.PurpleTorch, vel.X, vel.Y, 60, tint, 2.2f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
