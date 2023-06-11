using Terraria;
using System;
using Microsoft.Xna.Framework;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;

namespace tsorcRevamp.Projectiles.Melee.Runeterra
{
    public class NightbringerWindWall: ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 8;
        }
        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 250;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 5 * 60;
            Projectile.DamageType = DamageClass.Default;
        }
        public override void OnSpawn(IEntitySource source)
        {
            Player player = Main.player[Projectile.owner];
            Vector2 unitVectorTowardsMouse = player.Center.DirectionTo(Main.MouseWorld).SafeNormalize(Vector2.UnitX * player.direction);
            player.ChangeDir((unitVectorTowardsMouse.X > 0f) ? 1 : (-1));
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (Main.GameUpdateCount % 15 == 0)
            {
                Projectile.velocity = Vector2.Zero;
            }
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];

                if (i != Projectile.whoAmI && other.active && !other.friendly && Projectile.Hitbox.Intersects(other.Hitbox))
                {
                    if (other.type != ProjectileID.PhantasmalDeathray && other.type != ProjectileID.SaucerDeathray)
                    {
                        Dust.NewDust(other.position, other.width * 2, other.height * 2, DustID.Torch);
                        other.Kill();
                    }
                }
            }
            Visuals();
        }
        private void Visuals()
        {
            float frameSpeed = 5f;

            Projectile.frameCounter++;

            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 0;
                }
            }
            Lighting.AddLight(Projectile.Center, Color.Gold.ToVector3() * 5f);
        }
    }
}