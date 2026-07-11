using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    // Tall narrow AOE burst around the slam point - a plain rectangular hitbox (unlike
    // ArtoriasAbyssBlast's circular one), read as a pillar of dust erupting straight up before the
    // traveling flame walls (ArtoriasAbyssBlaze) peel off to either side.
    class ArtoriasAbyssPillar : ModProjectile
    {
        const int Lifetime = 16;

        public override string Texture => "tsorcRevamp/NPCs/Invaders/InvaderPlaceholder";

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 140;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            if (Main.dedServ)
            {
                return;
            }

            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 1.25f);

            for (int i = 0; i < 3; i++)
            {
                Vector2 pos = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-20f, 20f), 0f);
                Vector2 vel = new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-9f, -3f));
                Dust d = Dust.NewDustPerfect(pos, DustID.PurpleTorch, vel, 40, new Color(200, 90, 220), 1.3f);
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }
            for (int i = 0; i < 20; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(5f, 5f);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, vel, 60, new Color(230, 120, 220), 1.4f);
                d.noGravity = true;
            }
        }
    }
}
