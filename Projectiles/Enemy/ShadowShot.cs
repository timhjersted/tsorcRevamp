using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles.Enemy
{
    class ShadowShot : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.height = Projectile.width = 15;
            Projectile.tileCollide = false;
            Projectile.aiStyle = 0;
        }

        public override bool PreKill(int timeLeft)
        {
            Projectile.type = 44;
            return true;
        }

        public override void AI()
        {
            int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 52, 0, 0, 100, default, 2.0f);
            Main.dust[dust].noGravity = true;

            if (Projectile.velocity.X <= 10 && Projectile.velocity.Y <= 10 && Projectile.velocity.X >= -10 && Projectile.velocity.Y >= -10)
            {
                Projectile.velocity.X *= 1.01f;
                Projectile.velocity.Y *= 1.01f;
            }

        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(BuffID.Bleeding, 600);
            target.AddBuff(BuffID.Poisoned, 300);
            target.AddBuff(BuffID.PotionSickness, 1200); // 20s of potion sick? that is *vile* why would you do that
            target.AddBuff(ModContent.BuffType<Buffs.BrokenSpirit>(), 600); //no kb resist
        }
    }
}
