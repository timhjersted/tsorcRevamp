using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku {
    public class DarkLaserHost : ModProjectile {

        public override void SetDefaults() {
            projectile.aiStyle = 0;
            projectile.height = 16;
            projectile.scale = 1.2f;
            projectile.tileCollide = false;
            projectile.width = 16;
            projectile.hostile = false;
            projectile.timeLeft = 1300;
        }

        bool instantiated = false;
        GenericLaser[] DarkLasers;
        float RotationProgress;

        public override void AI() {
            
            if (!instantiated)
            {
                InstantiateLaser();
                instantiated = true;
            }
            projectile.Center = Main.npc[(int)projectile.ai[0]].Center;

            RotationProgress += 0.005f;
            for (int i = 0; i < DarkLasers.Length; i++)
            {
                Dust.NewDustPerfect(projectile.Center, 54, new Vector2(8, 0).RotatedBy(RotationProgress + (i * 2 * Math.PI / 5))).noGravity = true;
                DarkLasers[i].LaserOrigin = projectile.Center;
                DarkLasers[i].LaserTarget = projectile.Center + new Vector2(1, 0).RotatedBy(RotationProgress + (i * 2 * Math.PI / 5));
            }

        }

        void InstantiateLaser()
        {
            DarkLasers = new GenericLaser[5]; 
            
            for (int i = 0; i < DarkLasers.Length; i++)
            {
                if (DarkLasers[i] == null)
                {
                    DarkLasers[i] = (Projectiles.GenericLaser)Projectile.NewProjectileDirect(projectile.Center, new Vector2(0, 5), ModContent.ProjectileType<Projectiles.GenericLaser>(), projectile.damage, .5f, Main.myPlayer).modProjectile;
                    DarkLasers[i].LaserOrigin = projectile.Center;
                    DarkLasers[i].LaserTarget = projectile.Center + new Vector2(1, 0).RotatedBy(RotationProgress + (i * 2 * Math.PI / 5));
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

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {         
            return false;
        }
    }
}
