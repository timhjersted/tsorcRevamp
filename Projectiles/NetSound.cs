using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles;

class NetSound : ModProjectile
{

    public override void SetDefaults()
    {
        Projectile.hide = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
    }
    public override void AI()
    {
        /* 1.4 Porting Note: This doesn't work anymore, because you need a SoundStyle instead of just ints!
         * I don't think we ever used this anyway though?? :?
        Terraria.Audio.SoundEngine.PlaySound((int)Projectile.velocity.X,
            (int)Main.player[Projectile.owner].position.X,
            (int)Main.player[Projectile.owner].position.Y,
            (int)Projectile.velocity.Y);
        Projectile.Kill();*/
    }
}
