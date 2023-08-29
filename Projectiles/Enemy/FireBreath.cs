using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy;

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
        Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Item34 with { Volume = 0.1f, Pitch = -0.2f }, Projectile.Center); //flamethrower
        return true;
    }

}