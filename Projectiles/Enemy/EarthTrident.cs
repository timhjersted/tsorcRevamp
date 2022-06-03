using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles.Enemy
{
    class EarthTrident : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.aiStyle = 1;
            Projectile.hostile = true;
            Projectile.height = 16;
            Projectile.light = 0.5f;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.scale = 0.8f;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.width = 16;
            Projectile.tileCollide = false;
        }



        public override bool PreKill(int timeleft)
        {
            Projectile.type = ProjectileID.WoodenArrowHostile;

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 7, 0, 0, 0, default, 1f);
            }
            return true;
        }
    }
}