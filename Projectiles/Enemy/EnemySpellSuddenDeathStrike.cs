using Microsoft.Xna.Framework;
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
            Projectile.light = 0.5f;
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

            //It flies through walls, so it must be LOUD: shadowflame wisp trail + purple glow
            for (int i = 0; i < 2; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default, 1.2f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.3f;
            }
            Lighting.AddLight(Projectile.Center, 0.35f, 0.15f, 0.5f);
        }
        #endregion

        public override void OnKill(int timeLeft)
        {
            //Expiry puff so a whiffed sigil still reads as a near miss
            for (int i = 0; i < 10; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(2.5f, 2.5f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, vel.X, vel.Y, 80, default, 1.3f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<PowerfulCurseBuildup>(), 36000);
            target.AddBuff(BuffID.Silenced, 120);
            target.AddBuff(BuffID.Weak, 600);
        }

    }
}