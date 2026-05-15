using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemySpellIcestormIcicle1 : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 52;
            Projectile.hostile = true;
            Projectile.penetrate = 16;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 400;
            Projectile.DamageType = DamageClass.Magic;
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


        public override bool PreDraw(Player player, ref Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */
        {
            lightColor = Color.White;
            return base.PreDraw(player, ref lightColor);
        }
    }
}
