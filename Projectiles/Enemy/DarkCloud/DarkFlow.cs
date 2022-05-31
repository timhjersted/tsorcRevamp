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
            Projectile.aiStyle = 0;
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.hostile = true;
            Projectile.penetrate = 20;
            Projectile.tileCollide = false;
        }
        public NPC darkCloud
        {
            get => Main.npc[(int)Projectile.ai[0]];
        }

        bool initialized = false;
        float initialDistance = 0;
        public override void AI()
        {
            float distance = Vector2.Distance(Projectile.Center, darkCloud.Center);
            if (!initialized)
            {
                Projectile.timeLeft = (int)Projectile.ai[1];
                initialDistance = distance;
                initialized = true;
            }
            //Could these all be one line? Sure. Would make it even more obnoxious to figure out what it does, though
            Vector2 target = UsefulFunctions.GenerateTargetingVector(Projectile.Center, darkCloud.Center, 8);
            Projectile.velocity = target;
            Projectile.velocity += (2.2f * ((initialDistance - distance)/initialDistance) * target.RotatedBy(MathHelper.ToRadians(90)));

            //Only spawn dust that will actually be onscreen
            if (Vector2.Distance(Projectile.Center, Main.player[Main.myPlayer].Center) < 1000)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(8, 8);
                Vector2 velocity = new Vector2(-2, 0).RotatedBy(offset.ToRotation()) * Main.rand.NextFloat(2);
                Dust.NewDustPerfect(Projectile.Center + offset, DustID.ShadowbeamStaff, velocity, Scale: 3.5f).noGravity = true;
            }

            if (distance < 120)
            {
                Projectile.Kill();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            Main.player[Main.myPlayer].AddBuff(BuffID.Frozen, 15, false);
        }
    }
}