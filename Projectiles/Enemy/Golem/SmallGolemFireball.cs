using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Golem
{
    class SmallGolemFireball : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.Fireball);
        }
        public override void OnKill(int timeLeft)
        {
            int Difficulty = 1 + (Main.expertMode ? 1 : 0) + (Main.masterMode ? 1 : 0);

        }
    }
}
