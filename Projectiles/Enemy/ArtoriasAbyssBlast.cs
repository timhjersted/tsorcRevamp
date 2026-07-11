using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Projectiles.Enemy
{
    // Circular purple AOE blast (~150px across) that lingers briefly, then bursts into 6
    // ArtoriasFlameOrb seekers fanning out evenly in every direction.
    class ArtoriasAbyssBlast : ModProjectile
    {
        const float Radius = 75f;
        const int Lifetime = 20;
        const int OrbCount = 6;
        const float OrbSpeed = 4f;
        const int OrbDamage = 40;

        public override string Texture => "tsorcRevamp/NPCs/Invaders/InvaderPlaceholder";

        public override void SetDefaults()
        {
            Projectile.width = (int)(Radius * 2f);
            Projectile.height = (int)(Radius * 2f);
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
            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 1.25f);

            float t = 1f - Projectile.timeLeft / (float)Lifetime;
            if (Projectile.timeLeft % 2 == 0)
            {
                UsefulFunctions.DustRingPrecise(Projectile.Center, Radius * t, DustID.PurpleTorch, 24, alpha: 40, scale: 1.4f);
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

            for (int i = 0; i < 32; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(6f, 6f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.PurpleTorch, vel.X, vel.Y, 60, default, 1.6f);
                Main.dust[dust].noGravity = true;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            for (int i = 0; i < OrbCount; i++)
            {
                Vector2 vel = (MathHelper.TwoPi * i / OrbCount).ToRotationVector2() * OrbSpeed;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel,
                    ModContent.ProjectileType<ArtoriasFlameOrb>(), OrbDamage, 0f, Main.myPlayer);
            }
        }
    }
}
