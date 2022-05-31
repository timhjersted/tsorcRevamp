using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class MiracleVinesTrail : ModProjectile {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/MiracleVines";
        public override void SetDefaults() {
            Projectile.width = 15;
            Projectile.height = 15;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.MaxUpdates = 2;
            Projectile.penetrate = 3;
            Projectile.hostile = true;
            Projectile.netUpdate = true;
        }

        public override void AI() {
            Projectile.rotation = (float)Math.Atan2((double)this.Projectile.velocity.Y, (double)this.Projectile.velocity.X) + 1.57f;

            if (Projectile.alpha < 170 && Projectile.alpha + 5 >= 170) {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 17, Projectile.velocity.X * 0.025f, Projectile.velocity.Y * 0.025f, 170, default, 1.2f);
            }
            Projectile.alpha += 5;
            if (Projectile.alpha > 210) {
                Projectile.damage = 0;
            }
            if (Projectile.alpha >= 255) {
                Projectile.Kill();
                NetMessage.SendData(MessageID.KillProjectile, -1, -1, null);
                return;
            }


        }

        public override void Kill(int timeLeft)
        {
            Projectile.active = false;
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(MessageID.KillProjectile, -1, -1, null);
            }
        }
    }
}
