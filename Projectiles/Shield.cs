using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class Shield : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shield");
        }
        public override void SetDefaults()
        {
            drawHeldProjInFrontOfHeldItemAndArms = true; // Makes projectile appear in front of arms, not just in between body and arms
            projectile.friendly = true;
            projectile.width = 48;
            projectile.height = 62;
            projectile.penetrate = -1;
            projectile.scale = 1;
            projectile.tileCollide = false;
            projectile.timeLeft = 2;
            projectile.alpha = 100;
        }
        public override void AI()
        {

            var player = Main.player[projectile.owner];

            if (player.dead)
            {
                projectile.Kill();
                return;
            }

            if (Main.rand.Next(4) == 0)
            {
                int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 57, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 100, default(Color), 1.2f);
                Main.dust[dust].noGravity = true;
            }


            Player projOwner = Main.player[projectile.owner];
            projOwner.heldProj = projectile.whoAmI; //this makes it appear in front of the player
            projectile.velocity.X = player.velocity.X;
            projectile.velocity.Y = player.velocity.Y;
        }
        public override bool CanDamage()
        {
            return false;
        }
    }
}