using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Seath;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    class EnemySpellIcestormIcicle2 : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.penetrate = 16;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 400;
            Projectile.coldDamage = true;
        }
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Enemy Spell Ice Storm");
        }
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(90);
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            SeathTheScalelessHead.ApplyProjectileFrost(Projectile, target);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Color.White;
            return base.PreDraw(ref lightColor);
        }
    }
}
