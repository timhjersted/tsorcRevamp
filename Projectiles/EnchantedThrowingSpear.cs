using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles
{
    class EnchantedThrowingSpear : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 3;
        }
        public override void SetDefaults()
        {
            Projectile.width = 45;
            Projectile.height = 45;
            Projectile.timeLeft = 120; 
            Projectile.light = 0.5f;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.ownerHitCheck = false;
            Projectile.melee = false;
            Projectile.tileCollide = false;
            Projectile.hide = false;
            Projectile.scale = 1f;
            Projectile.DamageType = DamageClass.Magic;

        }

        public override void AI()
        {
            Projectile.ai[0] += 1f;
            if (++Projectile.frameCounter >= 3)
            {
                Projectile.frameCounter = 0;
                if (++Projectile.frame >= 3)
                {
                    Projectile.frame = 0;
                }
            }
            if (Projectile.ai[0] >= 15f) { //this is the bit that makes it arc down
                Projectile.ai[0] = 15f;
                Projectile.velocity.Y += 0.1f;
            }
            if (Projectile.velocity.Y > 16f) { //this bit caps down velocity, and thus also caps down rotation if fired at a positive angle
                Projectile.velocity.Y = 16f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(90f); //simplified rotation code (no trig!)
        }
        public override void Kill(int timeLeft) {
            Main.PlaySound(SoundID.Dig, (int)Projectile.position.X, (int)Projectile.position.Y, 1);
            for (int i = 0; i < 10; i++) {
                Vector2 projPosition = new Vector2(Projectile.position.X, Projectile.position.Y);
                Dust.NewDust(projPosition, Projectile.width, Projectile.height, 7, 0f, 0f, 0, default, 1f);
            }
        }

    }

}
