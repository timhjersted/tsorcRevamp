using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;


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
            Projectile.light = 0.7f;
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

            //great dust for bright effect that can be color matched
            int dust2 = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, DustID.AncientLight, Projectile.velocity.X, Projectile.velocity.Y, 150, Color.Purple, 0.9f);
            Main.dust[dust2].noGravity = true;

            if (Projectile.velocity.X <= 10 && Projectile.velocity.Y <= 10 && Projectile.velocity.X >= -10 && Projectile.velocity.Y >= -10)
            {
                Projectile.velocity.X *= 1.01f;
                Projectile.velocity.Y *= 1.01f;
            }

        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.WitheredArmor, 300);
            target.AddBuff(BuffID.PotionSickness, 300); // 20s of potion sick? that is *vile* why would you do that (I don't know who did that but I'll blame old Tim or a typo! :d)
            target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 600); //no kb resist
        }
    }
}
