using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
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
        float RotationProgress;

        public override void AI()
        {
            Projectile.Center = Main.npc[(int)Projectile.ai[0]].Center;
            RotationProgress += 0.005f;
            if (!instantiated)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.Okiku.DarkLaser>(), Projectile.damage, 0, Main.myPlayer, i, UsefulFunctions.EncodeID(Projectile));
                    }
                }
                instantiated = true;
            }
            

            for (int i = 0; i < 5; i++)
            {
                Dust.NewDustPerfect(Projectile.Center, 54, new Vector2(8, 0).RotatedBy(RotationProgress + (i * 2 * Math.PI / 5))).noGravity = true;
            }            
        }

        void InstantiateLaser()
        {
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}
