using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class BoulderDropLeft : ModProjectile //for use in events. Boulder will roll left
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Ranged/ThrowingSpear"; //it's invis

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Explosion");
        }

        public override void SetDefaults()
        {
            projectile.aiStyle = -1;
            projectile.alpha = 255; //invis
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.height = 6;
            projectile.width = 6;
            projectile.scale = 1f;
            projectile.tileCollide = false;
            projectile.timeLeft = 4;
        }

        public override void AI()
        {
            projectile.velocity.X = 0;
            projectile.velocity.Y = 0;
        }

        public override bool PreKill(int timeLeft)
        {
            projectile.type = ProjectileID.GrenadeI;
            Projectile.NewProjectile(projectile.Center, new Vector2(0, 0), ProjectileID.Boulder, 70, 1, Main.myPlayer);

            return true;
        }


    }
}