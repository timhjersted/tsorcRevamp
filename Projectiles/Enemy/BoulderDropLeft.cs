using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class BoulderDropLeft : ModProjectile //for use in events. Boulder will roll left
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Ranged/Thrown/ThrowingSpear"; //it's invis

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Explosion");
        }

        public override void SetDefaults()
        {
            Projectile.aiStyle = -1;
            Projectile.alpha = 255; //invis
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.height = 6;
            Projectile.width = 6;
            Projectile.scale = 1f;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 4;
        }

        public override void AI()
        {
            Projectile.velocity.X = 0;
            Projectile.velocity.Y = 0;
        }

        public override bool PreKill(int timeLeft)
        {
            Projectile.type = ProjectileID.GrenadeI;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, new Vector2(0, 0), ProjectileID.Boulder, 70, 1, Main.myPlayer);

            return true;
        }


    }
}