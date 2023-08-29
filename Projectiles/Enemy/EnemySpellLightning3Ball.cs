using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy;

class EnemySpellLightning3Ball : ModProjectile
{
    public override string Texture => "tsorcRevamp/Projectiles/Bolt1Ball";
    public override void SetDefaults()
    {
        //projectile.aiStyle = 4;
        Projectile.hostile = true;
        Projectile.height = 16;
        Projectile.penetrate = 1;
        Projectile.tileCollide = true;
        Projectile.width = 16;
        Projectile.timeLeft = 600;
    }
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Enemy Spell Lightning 3");

    }

    bool timeleftSet = false;
    public override void AI()
    {
        if (!timeleftSet)
        {
            Projectile.timeLeft = (int)Projectile.ai[0];
            timeleftSet = true;
        }
        if (Projectile.ai[1] != 0)
        {
            Projectile.aiStyle = 1;
        }
        if (Projectile.soundDelay == 0 && Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y) > 2f)
        {
            Projectile.soundDelay = 10;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9, Projectile.Center);
        }
        int num47 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.MagicMirror, 0f, 0f, 100, default, 2f);
        Main.dust[num47].velocity *= 0.3f;
        Main.dust[num47].position.X = Projectile.position.X + (float)(Projectile.width / 2) + 4f + (float)Main.rand.Next(-4, 5);
        Main.dust[num47].position.Y = Projectile.position.Y + (float)(Projectile.height / 2) + (float)Main.rand.Next(-4, 5);
        Main.dust[num47].noGravity = true;

        if (Projectile.velocity.X != 0f || Projectile.velocity.Y != 0f)
        {
            Projectile.rotation = (float)Math.Atan2((double)Projectile.velocity.Y, (double)Projectile.velocity.X) - 2.355f;
        }
    }
    public override void Kill(int timeLeft)
    {

        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);

        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width / 2), Projectile.position.Y + (float)(Projectile.height / 2), 0, 0, ModContent.ProjectileType<EnemySpellLightning3Bolt>(), Projectile.damage, 8f, Projectile.owner);
        }
        Vector2 projectilePos = new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y - Projectile.velocity.Y);
        int num41 = Dust.NewDust(projectilePos, Projectile.width, Projectile.height, DustID.MagicMirror, 0f, 0f, 100, default, 1f);
        Main.dust[num41].noGravity = true;
        Main.dust[num41].velocity *= 2f;
        Dust.NewDust(projectilePos, Projectile.width, Projectile.height, DustID.MagicMirror, 0f, 0f, 100, default, 1f);
    }
}
