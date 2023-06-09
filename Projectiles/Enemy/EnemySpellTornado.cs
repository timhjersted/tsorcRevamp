using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemySpellTornado : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 100;
            Projectile.height = 280;
            Main.projFrames[Projectile.type] = 12;
            DrawOriginOffsetX = 160;
            DrawOriginOffsetY = 10;
            Projectile.hostile = true;
            Projectile.penetrate = 50;
            Projectile.scale = 1.3f;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 1f;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 1200;
        }
        int spinPlayer = 0;
        int spinCooldown = 0;
        static float maxSpeed = 7;
        Vector2 maxClamp = new Vector2(maxSpeed, maxSpeed);

        #region AI
        public override void AI()
        {
            Projectile.rotation = 0;
            spinCooldown--;
            maxSpeed = 7;
            if (Main.player[(int)Projectile.ai[0]] != null || Main.player[(int)Projectile.ai[0]].active)
            {
                Projectile.velocity += UsefulFunctions.Aim(Projectile.Center, Main.player[(int)Projectile.ai[0]].Center, 0.3f);
                if (Projectile.velocity.X > maxSpeed)
                {
                    Projectile.velocity.X = maxSpeed;
                }
                if (Projectile.velocity.X < -maxSpeed)
                {
                    Projectile.velocity.X = -maxSpeed;
                }
                if (Projectile.velocity.Y > maxSpeed)
                {
                    Projectile.velocity.Y = maxSpeed;
                }
                if (Projectile.velocity.Y < -maxSpeed)
                {
                    Projectile.velocity.Y = -maxSpeed;
                }
            }


            Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 31, -Projectile.velocity.X, -Projectile.velocity.Y);
            if (!Main.dedServ)
            {
                Projectile.frameCounter++;
                if (Projectile.frameCounter > 3)
                {
                    Projectile.frame++;
                    Projectile.frameCounter = 0;
                }
                if (Projectile.frame >= 12)
                {
                    Projectile.frame = 0;
                }
            }
        }
        #endregion

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (spinCooldown <= 0)
            {
                if ((target.velocity.X > 1) || (spinPlayer < 1))
                {
                    target.velocity.X = -8;
                    spinPlayer++;
                    spinCooldown = 10;
                }
                else
                {
                    target.velocity.X = 8;
                    spinPlayer = 0;
                    spinCooldown = 10;
                }
            }
        }
    }
}