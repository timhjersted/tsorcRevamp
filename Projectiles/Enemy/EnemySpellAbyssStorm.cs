using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemySpellAbyssStorm : ModProjectile
    {
		public override void SetStaticDefaults()
		{
            DisplayName.SetDefault("Dark Wave Storm");
		}

		public override void SetDefaults()
        {
            projectile.width = 194;
            projectile.height = 194;
            drawOriginOffsetX = -96;
            drawOriginOffsetY = 94;
            Main.projFrames[projectile.type] = 7;
            projectile.hostile = true;
            projectile.penetrate = 50;
            projectile.scale = 2;
            projectile.magic = true;
            projectile.light = 1;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
        }

        float size = 0;
        int dustCount = 0;

        public override void AI()
        {
            if (size < 20 * 16)
            {
                size += ((14 * 16) / 30f); //Increase to its full size (7 blocks) in half a second (30 ticks)
                dustCount = (int)(2 * MathHelper.Pi * size / 10); //Spawn dust according to its size                
            }
            else
            {
                //Fade out after reaching max radius, and then despawn
                dustCount /= 2;
                if(dustCount <= 0)
                {
                    projectile.Kill();
                    return;
                }
            }

            for (int j = 0; j < dustCount; j++)
            {
                Vector2 dir = Main.rand.NextVector2CircularEdge(size, size);
                Vector2 dustPos = projectile.Center + dir;
                dir.Normalize();
                Vector2 dustVel = dir;
                Dust.NewDustPerfect(dustPos, DustID.BlueCrystalShard, dustVel, 200).noGravity = true;
            }
        }

        //Circular collision
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float distance = Vector2.Distance(projHitbox.Center.ToVector2(), targetHitbox.Center.ToVector2());
            if (distance < size && distance > size - 32)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (Main.expertMode)
            {
                target.AddBuff(BuffID.OnFire, 450, false);
            }
            else
            {
                target.AddBuff(BuffID.OnFire, 900, false);
            }
        }
    }
}