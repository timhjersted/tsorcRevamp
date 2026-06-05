using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class EnemyFrostburnArrow : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.FrostburnArrow;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Enemy Frostburn Arrow");
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.FrostburnArrow);
            Projectile.friendly = false;
            Projectile.hostile = true;
            AIType = ProjectileID.FrostburnArrow;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Frostburn, 180); // 3 seconds of Frostburn debuff
        }
    }
}
