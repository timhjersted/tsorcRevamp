using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles.Enemy.DarkCloud
{
    class DarkWave : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/DarkCloud/DarkCloudSpark";
        public override void SetDefaults()
        {
            projectile.aiStyle = 0;
            projectile.width = 26;
            projectile.height = 26;
            projectile.hostile = true;
            projectile.penetrate = 20;
            projectile.tileCollide = false;
            projectile.timeLeft = 240;
        }


        public override void AI()
        {
            //Aka lmao if you think you can just outrun this
            projectile.width = 10 + (240 - projectile.timeLeft) / 2;
            projectile.height = 10 + (240 - projectile.timeLeft) / 2;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            
            Vector2 offset = Main.rand.NextVector2CircularEdge(projectile.width, projectile.height);
            Vector2 velocity = new Vector2(-2, 0).RotatedBy(offset.ToRotation()) * Main.rand.NextFloat(2);
            Dust.NewDustPerfect(projectile.Center + offset, DustID.ShadowbeamStaff, velocity, Scale: 3.5f).noGravity = true;
               
            return false;
        }
    }
}