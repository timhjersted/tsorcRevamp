using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Summon.Tetsujin
{
    class FriendlyDragonsBreath : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
        }

        public override void SetDefaults()
        {

            Projectile.width = 6;
            Projectile.height = 6;
            AIType = 1; //what's 85? ignores time left. trying 1
            Projectile.aiStyle = 23;
            Projectile.timeLeft = 3600; //3600 does't even matter
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 3;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }
    }
}
