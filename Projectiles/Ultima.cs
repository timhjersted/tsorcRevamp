using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class Ultima : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 13;
        }

        public override void SetDefaults()
        {
            Projectile.width = 75;
            Projectile.height = 75;
            Projectile.friendly = true;
            Projectile.penetrate = 4;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
            Projectile.scale = 1.2f;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 3)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 13)
            {
                Projectile.Kill();
                if (Projectile.owner == Main.myPlayer)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width / 2), Projectile.position.Y + (float)(Projectile.height / 2), 0, 0, ModContent.ProjectileType<Projectiles.Magic.UltimaExplosion>(), Projectile.damage / 2, 8f, Projectile.owner);
                }
                return;
            }
            {
                if (Projectile.ai[1] == 0f)
                {
                    Projectile.ai[1] = 1f;
                    Projectile.netUpdate = true;
                }
            }
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
            }
            Projectile.alpha += (int)(25f * Projectile.localAI[0]);
            if (Projectile.alpha > 200)
            {
                Projectile.alpha = 200;
                Projectile.localAI[0] = -1f;
            }
            if (Projectile.alpha < 0)
            {
                Projectile.alpha = 0;
                Projectile.localAI[0] = 1f;
            }
            Projectile.rotation += (Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y)) * 0.01f * (float)Projectile.direction;
            if (Projectile.ai[1] == 1f || Projectile.type == 92)
            {
                Projectile.light = 0.9f;
                if (Main.rand.NextBool(10))
                {
                    Vector2 arg_1328_0 = Projectile.position;
                    int arg_1328_1 = Projectile.width;
                    int arg_1328_2 = Projectile.height;
                    int arg_1328_3 = 107;
                    float arg_1328_4 = Projectile.velocity.X * 0.5f;
                    float arg_1328_5 = Projectile.velocity.Y * 0.5f;
                    int arg_1328_6 = 150;
                    Dust.NewDust(arg_1328_0, arg_1328_1, arg_1328_2, arg_1328_3, arg_1328_4, arg_1328_5, arg_1328_6, default, 1.2f);
                }
                if (Main.rand.NextBool(20))
                {
                    if (!Main.dedServ)
                    {
                        int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 107, Projectile.velocity.X, Projectile.velocity.Y, 0, default(Color), 1.5f);
                        Main.dust[dust].noGravity = true;
                    }
                    return;
                }
            }
        }
        
        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.5f, Pitch = 1f }, Projectile.Center);

            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center.X,
                    Projectile.Center.Y,
                    0, 0,
                    ModContent.ProjectileType<Projectiles.Magic.UltimaExplosion>(),
                    Projectile.damage,
                    8f,
                    Projectile.owner
                );
            }
        }
    }
}
