using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class EnemySpellPoisonField : ModProjectile
    {
        public override void SetDefaults()
        {

            Projectile.width = 26;
            Projectile.height = 40;
            Main.projFrames[Projectile.type] = 5;
            Projectile.aiStyle = 4;
            Projectile.hostile = true;
            Projectile.damage = 60;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 260;
            Projectile.penetrate = 50;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(BuffID.Poisoned, 360);
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
        }
        #endregion

    }
}