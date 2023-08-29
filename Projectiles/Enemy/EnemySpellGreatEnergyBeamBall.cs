using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy;

class EnemySpellGreatEnergyBeamBall : ModProjectile
{

    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Enemy Spell Great Energy Beam Ball");

    }
    public override void SetDefaults()
    {
        Projectile.aiStyle = 23;
        Projectile.hostile = true;
        Projectile.height = 16;
        Projectile.light = 1;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = 8;
        Projectile.scale = 1;
        Projectile.tileCollide = true;
        Projectile.width = 16;
        Projectile.timeLeft = 0;
    }

    #region AI
    public override void AI()
    {
        if (Projectile.aiStyle == 1)
        {
            if (Projectile.ai[1] == 0f)
            {
                Projectile.ai[1] = 1f;

            }
            Projectile.rotation = (float)Math.Atan2((double)Projectile.velocity.Y, (double)Projectile.velocity.X) + 1.57f;
            if (Projectile.velocity.Y > 16f)
            {
                Projectile.velocity.Y = 16f;
                return;
            }
        }
    }
    #endregion

    #region Kill
    public override bool PreKill(int timeLeft)
    {
        if (!Projectile.active)
        {
            return true;
        }
        Projectile.timeLeft = 0;
        {
            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Item93 with { Volume = 0.09f }, Projectile.Center); //electric zap long
                                                                                                                            //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 10);
            if (Projectile.position.X + (float)(Projectile.width / 2) > Main.player[Projectile.owner].position.X + (float)(Main.player[Projectile.owner].width / 2))
            {
                if (Projectile.owner == Main.myPlayer) Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width * -13), Projectile.position.Y + (float)(Projectile.height - 0.5f), 0, 0, ModContent.ProjectileType<EnemySpellGreatEnergyBeam>(), Projectile.damage, 8f, Projectile.owner);
            }
            else
            {
                if (Projectile.owner == Main.myPlayer) Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width * 14f), Projectile.position.Y + (float)(Projectile.height - 0.5f), 0, 0, ModContent.ProjectileType<EnemySpellGreatEnergyBeam>(), Projectile.damage, 8f, Projectile.owner);
            }
            Vector2 arg_1394_0 = new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y - Projectile.velocity.Y);
            int arg_1394_1 = Projectile.width;
            int arg_1394_2 = Projectile.height;
            int arg_1394_3 = 15;
            float arg_1394_4 = 0f;
            float arg_1394_5 = 0f;
            int arg_1394_6 = 100;
            Color newColor = default(Color);
            int num41 = Dust.NewDust(arg_1394_0, arg_1394_1, arg_1394_2, arg_1394_3, arg_1394_4, arg_1394_5, arg_1394_6, newColor, 2f);
            Main.dust[num41].noGravity = true;
            Dust expr_13B1 = Main.dust[num41];
            expr_13B1.velocity *= 2f;
            Vector2 arg_1422_0 = new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y - Projectile.velocity.Y);
            int arg_1422_1 = Projectile.width;
            int arg_1422_2 = Projectile.height;
            int arg_1422_3 = 15;
            float arg_1422_4 = 0f;
            float arg_1422_5 = 0f;
            int arg_1422_6 = 100;
            newColor = default(Color);
            num41 = Dust.NewDust(arg_1422_0, arg_1422_1, arg_1422_2, arg_1422_3, arg_1422_4, arg_1422_5, arg_1422_6, newColor, 1f);
        }
        Projectile.active = false;
        return true;
    }
    #endregion
}