using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class FireBreath : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fire Breath");

        }
        public override void SetDefaults()
        {
            Projectile.aiStyle = 23;
            Projectile.hostile = true;
            Projectile.height = 20;
            Projectile.light = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 10;
            Projectile.scale = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 3000;
            Projectile.width = 28;
        }
        public override bool PreKill(int timeLeft)
        {
            //projectile.type = 102; //makes cool explosion dust but also annoying exploding sound :/
            Terraria.Audio.SoundEngine.PlaySound(2, (int)Projectile.position.X, (int)Projectile.position.Y, 34, 0.1f, -0.2f); //flamethrower
            return true;
        }

    }
}