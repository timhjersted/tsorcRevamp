using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles.Enemy.DarkCloud
{
    class DarkFlow : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/DarkCloud/DarkCloudSpark";
        public override void SetDefaults()
        {
            projectile.aiStyle = 0;
            projectile.width = 10;
            projectile.height = 10;
            projectile.hostile = true;
            projectile.penetrate = 20;
            projectile.tileCollide = false;
        }
        public NPC darkCloud
        {
            get => Main.npc[(int)projectile.ai[0]];
        }

        bool initialized = false;
        float initialDistance = 0;
        public override void AI()
        {
            float distance = Vector2.Distance(projectile.Center, darkCloud.Center);
            if (!initialized)
            {
                projectile.timeLeft = (int)projectile.ai[1];
                initialDistance = distance;
                initialized = true;
            }
            //Could these all be one line? Sure. Would make it even more obnoxious to figure out what it does, though
            Vector2 target = UsefulFunctions.GenerateTargetingVector(projectile.Center, darkCloud.Center, 8);
            projectile.velocity = target;
            projectile.velocity += (2.2f * ((initialDistance - distance)/initialDistance) * target.RotatedBy(MathHelper.ToRadians(90)));

            //Only spawn dust that will actually be onscreen
            if (Vector2.Distance(projectile.Center, Main.player[Main.myPlayer].Center) < 1000)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(8, 8);
                Vector2 velocity = new Vector2(-2, 0).RotatedBy(offset.ToRotation()) * Main.rand.NextFloat(2);
                Dust.NewDustPerfect(projectile.Center + offset, DustID.ShadowbeamStaff, velocity, Scale: 3.5f).noGravity = true;
            }

            if (distance < 120)
            {
                projectile.Kill();
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            Main.player[Main.myPlayer].AddBuff(BuffID.Frozen, 15, false);
        }
    }
}