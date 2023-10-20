using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Ranged
{
    class PhoenixFlame : ModProjectile
    {
        //Like the DD2 Phoenix projectile, but bigger, and leaves a damaging prim trail of fire behind it
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.penetrate = 1;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 0.3f;
            Projectile.knockBack = 0f;
        }
        public override void AI()
        {

        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, 150);
        }
    }

}
