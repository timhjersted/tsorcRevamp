using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>
    /// Invisible 8-tick hostile hitbox spawned at the sword tip when an InvaderNPC swings.
    /// hostile=true means it damages players; friendly=false means it won't hit NPCs.
    /// penetrate=1 so it hits once and dies, respecting player i-frames correctly.
    /// </summary>
    public class InvaderMeleeHitbox : ModProjectile
    {
        public override string Texture => "tsorcRevamp/NPCs/Invaders/InvaderPlaceholder";

        public override void SetDefaults()
        {
            Projectile.width      = 40;
            Projectile.height     = 40;
            Projectile.hostile    = true;
            Projectile.friendly   = false;
            Projectile.penetrate  = 1;
            Projectile.timeLeft   = 8;
            Projectile.tileCollide = false;
            Projectile.alpha      = 255;   // fully invisible
            Projectile.ignoreWater = true;
        }
    }
}
