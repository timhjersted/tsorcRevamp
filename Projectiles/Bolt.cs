using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles;

public class Bolt : ModProjectile
{

    public override string Texture => "tsorcRevamp/Items/Ammo/Bolt";
    public override void SetDefaults()
    {

        Projectile.aiStyle = 1;
        Projectile.friendly = true;
        Projectile.height = 10;
        Projectile.penetrate = 2;
        Projectile.damage = 500;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.scale = (float)1;
        Projectile.tileCollide = true;
        Projectile.width = 5;
        AIType = ProjectileID.WoodenArrowFriendly;
        Projectile.aiStyle = 1;
    }

    public override void Kill(int timeLeft)
    {
        Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
        Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
    }
}
