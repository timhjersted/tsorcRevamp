using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class PoisonField2 : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/PoisonField";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 300;
            Projectile.alpha = 90;
            Projectile.light = 0.3f;
            Projectile.penetrate = 12;
            DrawOffsetX = -4;
            DrawOriginOffsetY = -10;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 100;
        }
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.Poisoned, 580);
            }
        }

        public override void AI()
        {

            if (Main.rand.NextBool(10))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 74, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 100, default(Color), .8f); ;
                Main.dust[dust].velocity *= 0f;
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity += Projectile.velocity;
                Main.dust[dust].fadeIn = 1f;
            }

            if (Projectile.timeLeft <= 55)
            {
                Projectile.alpha += 3;
            }


            if (Projectile.ai[0] == 0)
            {
                Projectile.velocity.X *= 0.001f;
                Projectile.velocity.Y *= 0.001f;
                Projectile.ai[0] = 1;
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 5)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 5)
            {
                Projectile.frame = 0;
                return;
            }
        }
    }
}
