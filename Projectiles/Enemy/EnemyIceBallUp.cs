using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy;

class EnemyIceBallUp : ModProjectile
{
    public override string Texture => "tsorcRevamp/Projectiles/Ice1Ball";
    public override void SetDefaults()
    {
        Projectile.aiStyle = 4;
        Projectile.hostile = true;
        Projectile.height = 16;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = 1;
        Projectile.tileCollide = true;
        Projectile.width = 16;
    }
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Ice Spell");

    }
    public override void AI()
    {
        if (Projectile.ai[0] != 0) //Dark Elf Mage version
        {
            Projectile.timeLeft = 80;
            Projectile.aiStyle = 1;
        }

        if (Math.Abs(Main.player[(int)Projectile.ai[1]].Center.X - Projectile.Center.X) < 8)
        {
            Projectile.Kill();
            Projectile.active = false;
        }
    }

    public override void Kill(int timeLeft)
    {
        Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Item30 with { Volume = 0.2f, Pitch = 0.3f }, Projectile.Center); //ice materialize - good
                                                                                                                                  //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 10);
        int Icicle = ModContent.ProjectileType<EnemyIceIcicleUp>();
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width), Projectile.position.Y + (float)(Projectile.height), 0, -6, Icicle, Projectile.damage, 3f, Projectile.owner);
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width * 4), Projectile.position.Y + (float)(Projectile.height * 2), 0, -6, Icicle, Projectile.damage, 3f, Projectile.owner);
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width * -2), Projectile.position.Y + (float)(Projectile.height * 2), 0, -6, Icicle, Projectile.damage, 3f, Projectile.owner);
        Vector2 projectilePos = new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y * Projectile.velocity.Y);
        if (Main.rand.NextBool(2))
        {
            //int num41 = Dust.NewDust(projectilePos, Projectile.width, Projectile.height, DustID.AncientLight, 0f, 0f, 100, Color.LightSkyBlue, 4f);
            //Main.dust[num41].noGravity = false;
            //Main.dust[num41].velocity *= 1f;
            //Dust.NewDust(projectilePos, Projectile.width, Projectile.height, 15, 0f, 0f, 100, default, 1f);

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(90);
            Lighting.AddLight(Projectile.Center, Color.WhiteSmoke.ToVector3() * 2f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
            int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 6, Projectile.velocity.X, Projectile.velocity.Y, 200, Color.LightCyan, 3f);
            Main.dust[dust].noGravity = true;
        }

    }   
}
