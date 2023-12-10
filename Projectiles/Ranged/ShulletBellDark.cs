using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles.Ranged
{
    public class ShulletBellDark : ModProjectile
    {
        public override void SetDefaults()
        {
            DrawHeldProjInFrontOfHeldItemAndArms = true; // Makes projectile appear in front of arms, not just in between body and arms
            Projectile.height = 4;
            Projectile.width = 4;
            Projectile.scale = 0.4f;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.timeLeft = 600;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Ranged;

            DrawOffsetX = -5;
            DrawOriginOffsetY = -8;
        }
        public override void AI()
        {
            Player projOwner = Main.player[Projectile.owner];
            projOwner.heldProj = Projectile.whoAmI; //this makes it appear in front of the player

            if (Projectile.timeLeft < 255)
            {
                Projectile.alpha += 1;
            }

            Projectile.ai[0] += 1f;
            if (Projectile.ai[0] > 7f)
            {
                Projectile.ai[0] = 10f;
                // Roll speed dampening.
                if (Projectile.velocity.Y == 0f && Projectile.velocity.X != 0f)
                {
                    Projectile.velocity.X = Projectile.velocity.X * 0.95f;

                    if ((double)Projectile.velocity.X > -0.01 && (double)Projectile.velocity.X < 0.01)
                    {
                        Projectile.velocity.X = 0f;
                        Projectile.netUpdate = true;
                    }
                }
                Projectile.velocity.Y = Projectile.velocity.Y + 0.2f;
            }
            Projectile.rotation += Projectile.velocity.X * 0.4f;

            Projectile.velocity.Y = Projectile.velocity.Y + 0.1f; // 0.1f for arrow gravity, 0.4f for knife gravity
            if (Projectile.velocity.Y > 16f) // This check implements "terminal velocity". We don't want the projectile to keep getting faster and faster. Past 16f this projectile will travel through blocks, so this check is useful.
            {
                Projectile.velocity.Y = 16f;
            }
            return;
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.velocity.X != oldVelocity.X && Math.Abs(oldVelocity.X) > 1f)
            {
                Projectile.velocity.X = oldVelocity.X * -0.3f;
            }
            if (Projectile.velocity.Y != oldVelocity.Y && Math.Abs(oldVelocity.Y) > 1f)
            {
                Projectile.velocity.Y = oldVelocity.Y * -0.3f;
            }
            return false;
        }
        public override bool? CanDamage()
        {
            return false;
        }
    }
}