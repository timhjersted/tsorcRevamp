using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy.Gwyn
{
    class BulletHellShot : ModProjectile
    {

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Gwyn/Petal";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Lightning Spear");
        }
        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 225;
            Projectile.tileCollide = false;
            Projectile.aiStyle = -1;
            Projectile.light = 0.7f;
        }
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<FracturingArmor>(), 1500);
        }
    }
}
