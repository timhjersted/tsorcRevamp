using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class MagicShield : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Magic Shield");
        }
        public override void SetDefaults()
        {
            DrawHeldProjInFrontOfHeldItemAndArms = true; // Makes projectile appear in front of arms, not just in between body and arms
            Projectile.friendly = true;
            Projectile.width = 48;
            Projectile.height = 62;
            Projectile.penetrate = -1;
            Projectile.scale = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 2;
            Projectile.alpha = 160;
        }
        public override void AI()
        {

            var player = Main.player[Projectile.owner];

            if (player.dead)
            {
                Projectile.Kill();
                return;
            }

            for (int d = 0; d < 5; d++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 36, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 120, default(Color), 1f);
                Main.dust[dust].noGravity = true;
            }
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 36, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 120, default(Color), .7f);
                Main.dust[dust].noGravity = true;
            }
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 36, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 120, default(Color), .5f);
                Main.dust[dust].noGravity = true;
            }

            Player projOwner = Main.player[Projectile.owner];
            projOwner.heldProj = Projectile.whoAmI; //this makes it appear in front of the player
            Projectile.velocity.X = player.velocity.X;
            Projectile.velocity.Y = player.velocity.Y;
        }
        public override bool? CanDamage()
        {
            return false;
        }
    }
}