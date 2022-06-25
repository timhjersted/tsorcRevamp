using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemySpellBlaze : ModProjectile
    {
        public override void SetDefaults()
        {
            Main.projFrames[Projectile.type] = 5;
            DrawOriginOffsetX = 15;
            DrawOriginOffsetY = 10;
            Projectile.width = 86;
            Projectile.height = 66;
            Projectile.aiStyle = 4;
            Projectile.hostile = true;
            Projectile.penetrate = 16;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 1f;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 100;
        }

        #region AI
        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 3)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 5)
            {
                Projectile.frame = 0;
                return;
            }
            if (Projectile.ai[0] == 0f)
            {
                Projectile.alpha -= 50;
                if (Projectile.alpha <= 0)
                {
                    Projectile.alpha = 0;
                    Projectile.ai[0] = 1f;
                    if (Projectile.ai[1] == 0f)
                    {
                        Projectile.ai[1] += 1f;
                        Projectile.position += Projectile.velocity * 1f;
                    }
                }
            }
            if (Projectile.timeLeft > 60)
            {
                Projectile.timeLeft = 60;
            }
            Projectile.rotation += 0.3f * (float)Projectile.direction;
            return;
        }
        #endregion

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            int expertScaling = 1;
            if (Main.expertMode) expertScaling = 2;
            target.AddBuff(BuffID.OnFire, 900 / expertScaling, false);
        }
    }
}