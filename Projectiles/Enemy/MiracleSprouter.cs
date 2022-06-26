using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class MiracleSprouter : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            Projectile.height = 15;
            Projectile.width = 15;
            Projectile.scale = 1.2f;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.hostile = true;
            Projectile.netUpdate = true;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.position, 0.1f, .35f, .25f);
            Projectile.rotation = (float)Math.Atan2((double)Projectile.velocity.X, (double)Projectile.velocity.Y);
            int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 75, 0, 0, 100, default, 2.0f);
            Main.dust[dust].noGravity = true;
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int projIndex = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X, Projectile.position.Y, 0, -3f, ModContent.ProjectileType<MiracleVines>(), Projectile.damage, 0f, Main.myPlayer); Projectile.active = false;
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, projIndex);
                    NetMessage.SendData(MessageID.KillProjectile, -1, -1, null, this.Projectile.whoAmI);
                }
            }
        }
    }
}
