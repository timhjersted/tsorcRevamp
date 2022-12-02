using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class JungleWyvernFire : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Breath of the Jungle");

        }
        public override void SetDefaults()
        {
            Projectile.aiStyle = 23;
            Projectile.hostile = true;
            Projectile.height = 28;
            Projectile.width = 28;
            Projectile.light = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 10;
            Projectile.scale = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 300;
            Projectile.width = 28;
            Projectile.alpha = 255;
        }

        public override bool PreAI()
        {
            Projectile.ai[1]++;

            if (Projectile.ai[1] > 3)
            {
                int dust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.CursedTorch, Projectile.velocity.X * 2f, Projectile.velocity.Y * 2f, 70, default(Color), Main.rand.NextFloat(2.5f, 4f));
                Main.dust[dust].noGravity = true;
            }

            return false;

        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (!tsorcRevampWorld.Slain.ContainsKey(NPCID.SkeletronHead))
            {
                target.AddBuff(BuffID.Poisoned, 180);
                target.AddBuff(BuffID.Bleeding, 180);
            }

            if (tsorcRevampWorld.Slain.ContainsKey(NPCID.SkeletronHead))
            {
                target.AddBuff(BuffID.Poisoned, 1200);
                target.AddBuff(BuffID.Bleeding, 1200);
            }
        }
    }
}