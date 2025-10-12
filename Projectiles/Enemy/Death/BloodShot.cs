using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;


namespace tsorcRevamp.Projectiles.Enemy.Death
{
    class BloodShot : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.height = Projectile.width = 15;
            Projectile.tileCollide = false;
            Projectile.aiStyle = 0;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.light = 0.8f;
        }

        public override bool PreKill(int timeLeft)
        {
            Projectile.type = 44;
            return true;
        }

        public override void AI()
        {
            int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 266, 0, 0, 100, default, 2.3f);
            Main.dust[dust].noGravity = true;

            //great dust for bright effect that can be color matched
            int dust2 = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, DustID.AncientLight, Projectile.velocity.X, Projectile.velocity.Y, 150, Color.Red, 1.1f);
            Main.dust[dust2].noGravity = true;

            if (Projectile.velocity.X <= 10 && Projectile.velocity.Y <= 10 && Projectile.velocity.X >= -10 && Projectile.velocity.Y >= -10)
            {
                Projectile.velocity.X *= 1.03f;
                Projectile.velocity.Y *= 1.03f;
            }

        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.WitheredArmor, 450);
            target.AddBuff(BuffID.PotionSickness, 450); // 20s of potion sick? that is *vile* why would you do that (I don't know who did that but I'll blame old Tim or a typo! :d)
            target.AddBuff(ModContent.BuffType<SlowedLifeRegen>(), 450);
            target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 900); //no kb resist
        }
    }
}
