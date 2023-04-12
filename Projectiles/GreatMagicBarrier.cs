using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class GreatMagicBarrier : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Great Magic Barrier");
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
            Projectile.alpha = 100;
        }
        public override void AI()
        {

            var player = Main.player[Projectile.owner];

            if (player.dead)
            {
                Projectile.Kill();
                return;
            }

            if (Main.rand.NextBool(4))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 57, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 100, default(Color), 1.2f);
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