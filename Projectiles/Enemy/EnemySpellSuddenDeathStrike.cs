using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class EnemySpellSuddenDeathStrike : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 44;
            Projectile.height = 40;
            Main.projFrames[Projectile.type] = 12;
            DrawOriginOffsetX = 15;
            DrawOriginOffsetY = 10;
            Projectile.hostile = true;
            Projectile.penetrate = 50;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
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
            if (Projectile.frame >= 12)
            {
                Projectile.Kill();
                return;
            }
        }
        #endregion

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<PowerfulCurseBuildup>(), 36000);
            target.AddBuff(BuffID.Poisoned, 3600);
            target.AddBuff(BuffID.Weak, 180);
        }

    }
}