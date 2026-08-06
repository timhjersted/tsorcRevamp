using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class DragonMeteor : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.aiStyle = 1;
            AIType = 20;
            Projectile.width = 59;
            Projectile.height = 62;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 600;
            Projectile.light = 1;
            Projectile.tileCollide = true;
            Projectile.friendly = false;
            Projectile.hostile = true;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<DragonMeteorExplosion>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            }
        }

        public override bool PreAI()
        {
            int D = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 6, Projectile.velocity.X, Projectile.velocity.Y, 200, new Color(), 3f);
            Main.dust[D].noGravity = true;
            Projectile.rotation += 0.1f;
            Lighting.AddLight((int)((Projectile.position.X + (float)(Projectile.width / 2)) / 16f), (int)((Projectile.position.Y + (float)(Projectile.height / 2)) / 16f), 1f, 1f, 1f);
            return true;
        }
    }
}
