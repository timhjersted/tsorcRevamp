using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy.Okiku
{
    class ShadowOrb : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.timeLeft = 480;
            Projectile.hostile = true;
            Projectile.height = 15;
            Projectile.width = 15;
            Projectile.scale = 0.9f;
            Projectile.tileCollide = false;
        }

        public override void OnKill(int timeLeft)
        {
            Projectile.type = 44;
        }

        public override void AI()
        {
            Projectile.rotation++;
            int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 6, 0, 0, 100, Color.Red, 2.0f);
            Main.dust[dust].noGravity = true;

            if (Projectile.velocity.X <= 10 && Projectile.velocity.Y <= 10 && Projectile.velocity.X >= -10 && Projectile.velocity.Y >= -10)
            {
                Projectile.velocity.X *= 1.01f;
                Projectile.velocity.Y *= 1.01f;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.BrokenArmor, 120, false); //broken armor
                target.AddBuff(BuffID.Weak, 600, false); //weak
                target.AddBuff(BuffID.OnFire, 180, false); //on fire!
            }

            if (Main.rand.NextBool(8))
            {
                target.AddBuff(ModContent.BuffType<FracturingArmor>(), 1800, false);
            }
        }
    }
}
