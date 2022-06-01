using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku
{
    public class DarkLaserHost : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.aiStyle = 0;
            Projectile.height = 16;
            Projectile.scale = 1.2f;
            Projectile.tileCollide = false;
            Projectile.width = 16;
            Projectile.hostile = false;
            Projectile.timeLeft = 1300;
        }

        bool instantiated = false;
        GenericLaser[] DarkLasers;
        float RotationProgress;

        public override void AI()
        {

            if (!instantiated)
            {
                InstantiateLaser();
                instantiated = true;
            }
            Projectile.Center = Main.npc[(int)Projectile.ai[0]].Center;

            RotationProgress += 0.005f;
            for (int i = 0; i < DarkLasers.Length; i++)
            {
                Dust.NewDustPerfect(Projectile.Center, 54, new Vector2(8, 0).RotatedBy(RotationProgress + (i * 2 * Math.PI / 5))).noGravity = true;
                DarkLasers[i].LaserOrigin = Projectile.Center;
                DarkLasers[i].LaserTarget = Projectile.Center + new Vector2(1, 0).RotatedBy(RotationProgress + (i * 2 * Math.PI / 5));
            }

        }

        void InstantiateLaser()
        {
            DarkLasers = new GenericLaser[5];

            for (int i = 0; i < DarkLasers.Length; i++)
            {
                if (DarkLasers[i] == null)
                {
                    DarkLasers[i] = (Projectiles.GenericLaser)Projectile.NewProjectileDirect(Projectile.Center, new Vector2(0, 5), ModContent.ProjectileType<Projectiles.GenericLaser>(), Projectile.damage, .5f, Main.myPlayer).ModProjectile;
                    DarkLasers[i].LaserOrigin = Projectile.Center;
                    DarkLasers[i].LaserTarget = Projectile.Center + new Vector2(1, 0).RotatedBy(RotationProgress + (i * 2 * Math.PI / 5));
                    DarkLasers[i].TelegraphTime = 300;
                    DarkLasers[i].MaxCharge = 240;
                    DarkLasers[i].FiringDuration = 940;
                    DarkLasers[i].LaserLength = 10000;
                    DarkLasers[i].LaserColor = Color.Purple;
                    DarkLasers[i].TileCollide = false;
                    DarkLasers[i].CastLight = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}
