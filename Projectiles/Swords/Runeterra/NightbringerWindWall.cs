using Terraria;
using System;
using Microsoft.Xna.Framework;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;

namespace tsorcRevamp.Projectiles.Swords.Runeterra
{
    public class NightbringerWindWall: ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 150;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 800;
            Projectile.DamageType = DamageClass.Melee;
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
                        other.Kill();
                    }
                }
            }
        }
    }
}