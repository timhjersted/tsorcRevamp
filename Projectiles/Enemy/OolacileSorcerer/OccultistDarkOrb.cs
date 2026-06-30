using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.VFX;
using tsorcRevamp.NPCs.Bosses.SuperHardMode;

namespace tsorcRevamp.Projectiles.Enemy.OolacileSorcerer
{
    public class OccultistDarkOrb : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.Bullet);
            AIType = ProjectileID.Bullet;
            Projectile.hostile = true;
            Projectile.light = 0.8f;
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.timeLeft = 300;
            Projectile.scale = 2;
        }
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }

        public override void AI()
        {
            if (Projectile.frameCounter < 5)
                Projectile.frame = 0;
            else if (Projectile.frameCounter >= 5 && Projectile.frameCounter < 10)
                Projectile.frame = 1;
            else if (Projectile.frameCounter >= 10 && Projectile.frameCounter < 15)
                Projectile.frame = 2;
            else if (Projectile.frameCounter >= 15 && Projectile.frameCounter < 20)
                Projectile.frame = 3;
            else
                Projectile.frameCounter = 0;
            Projectile.frameCounter++;

            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 27, 0, 0, 50, Color.Purple, 1.0f);
                Main.dust[dust].noGravity = false;
            }


            Projectile.velocity *= 0.99f;
        }

        public override void OnKill(int timeLeft)
        {
            AbysmalOolacileSorcererTEST.ShootProjectile(Projectile.GetSource_Death(), Projectile.Center.RotatedByRandom(350), 3, ModContent.ProjectileType<OccultistStarBig>(), false, 3, 0, 360 / 3, Projectile.Center, 0, 60);

        }
    }
}