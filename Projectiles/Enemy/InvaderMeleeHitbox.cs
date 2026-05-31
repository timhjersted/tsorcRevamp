using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>
    /// Invisible 8-tick hostile hitbox spawned along an InvaderNPC's swing arc.
    /// hostile=true means it damages players; friendly=false means it won't hit NPCs.
    /// penetrate=1 so it hits once and dies, respecting player i-frames correctly.
    ///
    /// SIZING — the spawn site passes the desired width/height via <c>ai[0]</c> and
    /// <c>ai[1]</c>. On first spawn we resize the projectile around its current center
    /// so the hitbox covers the requested zone. ai==0 falls back to the default 100×60
    /// box (covers a player perfectly stacked on the NPC AND a player at typical melee reach).
    /// </summary>
    public class InvaderMeleeHitbox : ModProjectile
    {
        public override string Texture => "tsorcRevamp/NPCs/Invaders/InvaderPlaceholder";

        public override void SetDefaults()
        {
            // Default dimensions sized so a player can't slip through by standing on or directly
            // adjacent to the NPC. Spawn sites that want a longer reach pass overrides via ai[0]/ai[1].
            Projectile.width       = 100;
            Projectile.height      = 60;
            Projectile.hostile     = true;
            Projectile.friendly    = false;
            Projectile.penetrate   = 1;
            Projectile.timeLeft    = 8;
            Projectile.tileCollide = false;
            Projectile.alpha       = 255;   // fully invisible
            Projectile.ignoreWater = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            // Optional dynamic sizing from spawn args. Keep the requested CENTER stable while
            // changing dimensions — Projectile.position is the top-left corner.
            int requestedW = (int)Projectile.ai[0];
            int requestedH = (int)Projectile.ai[1];
            if (requestedW <= 0 && requestedH <= 0) return;

            int newW = requestedW > 0 ? requestedW : Projectile.width;
            int newH = requestedH > 0 ? requestedH : Projectile.height;
            Vector2 center = Projectile.Center;
            Projectile.width  = newW;
            Projectile.height = newH;
            Projectile.Center = center;
        }
    }
}
