using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles;

class FireBombBall : ModProjectile
{

    public override string Texture => "tsorcRevamp/Projectiles/GreatFireballBall";
    public override void SetDefaults()
    {
        Projectile.aiStyle = 9;
        Projectile.friendly = true;
        Projectile.height = 16;
        Projectile.width = 16;
        Projectile.light = 0.8f;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = 1;
        Projectile.tileCollide = true;
    }

    public override void AI()
    {
        if (Projectile.soundDelay == 0 && Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y) > 2f)
        {
            Projectile.soundDelay = 10;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9, Projectile.Center);
        }

        int thisDust = Dust.NewDust(new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y - Projectile.velocity.Y), Projectile.width, Projectile.height, 15, 0, 0, 100, default, 2f);
        Main.dust[thisDust].noGravity = true;

        Projectile.rotation += 0.3f;
    }

    public override void Kill(int timeLeft)
    {
        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);
        for (int i = 0; i < 20; i++)
        {
            int thisDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 15, 0, 0, 100, default, 2f);
            Main.dust[thisDust].noGravity = true;
            thisDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 15, 0, 0, 100, default, 2f);
            Main.dust[thisDust].noGravity = true;
        }

        Projectile.position -= Projectile.Size / 2;

        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width), Projectile.position.Y + (float)(Projectile.height), 0, 0, ModContent.ProjectileType<FireField>(), Projectile.damage, 3f, Projectile.owner);
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width * 4), Projectile.position.Y + (float)(Projectile.height), 0, 0, ModContent.ProjectileType<FireField>(), Projectile.damage, 3f, Projectile.owner);
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width * -2), Projectile.position.Y + (float)(Projectile.height), 0, 0, ModContent.ProjectileType<FireField>(), Projectile.damage, 3f, Projectile.owner);
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width), Projectile.position.Y + (float)(Projectile.height * 4), 0, 0, ModContent.ProjectileType<FireField>(), Projectile.damage, 3f, Projectile.owner);
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width * 4), Projectile.position.Y + (float)(Projectile.height * 4), 0, 0, ModContent.ProjectileType<FireField>(), Projectile.damage, 3f, Projectile.owner);
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width * -2), Projectile.position.Y + (float)(Projectile.height * 4), 0, 0, ModContent.ProjectileType<FireField>(), Projectile.damage, 3f, Projectile.owner);
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width), Projectile.position.Y + (float)(Projectile.height * -2), 0, 0, ModContent.ProjectileType<FireField>(), Projectile.damage, 3f, Projectile.owner);
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width * 4), Projectile.position.Y + (float)(Projectile.height * -2), 0, 0, ModContent.ProjectileType<FireField>(), Projectile.damage, 3f, Projectile.owner);
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width * -2), Projectile.position.Y + (float)(Projectile.height * -2), 0, 0, ModContent.ProjectileType<FireField>(), Projectile.damage, 3f, Projectile.owner);
       
    }

}
