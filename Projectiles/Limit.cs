using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles;

class Limit : ModProjectile
{

    public override string Texture => "tsorcRevamp/Items/Weapons/Melee/Limit";
    public override void SetDefaults()
    {
        Projectile.width = 58;
        Projectile.height = 58;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.extraUpdates = 1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 8; //this thing is way stronger than it looks
        Projectile.penetrate = -1;
        Projectile.timeLeft = 90;
        Projectile.light = 0.6f;
    }
    public override void AI()
    {
        int toEdge = Projectile.height / 4;
        Projectile.ai[0]++;
        Projectile.rotation -= 0.225f * Projectile.direction;
        if (Projectile.ai[0] == 45)
        {
            Projectile.velocity *= 20;
        }
        if (Projectile.ai[0] > 60)
        {
            Projectile.alpha += 25;
        }
        if (Projectile.alpha > 250)
        {
            Projectile.Kill();
        }
    }
}
