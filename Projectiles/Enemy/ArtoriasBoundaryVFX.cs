using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.SuperHardMode;

namespace tsorcRevamp.Projectiles.Enemy
{
    class ArtoriasBoundaryVFX : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        int OwnerIndex => (int)Projectile.ai[0];

        public override void SetDefaults()
        {
            // Match the full exterior draw canvas so Terraria does not cull the projectile while
            // the camera is near or beyond the arena edge.
            Projectile.width = 5600;
            Projectile.height = 5600;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (OwnerIndex < 0 || OwnerIndex >= Main.maxNPCs
                || !Main.npc[OwnerIndex].active
                || Main.npc[OwnerIndex].ModNPC is not Artorias artorias)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = artorias.RingCenter;
            Projectile.timeLeft = 2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (OwnerIndex < 0 || OwnerIndex >= Main.maxNPCs
                || !Main.npc[OwnerIndex].active
                || Main.npc[OwnerIndex].ModNPC is not Artorias artorias)
            {
                return false;
            }

            ArtoriasVFX.DrawBoundary(artorias.RingCenter, artorias.EffectiveRingRadius,
                artorias.RingBandHalfWidthPixels, 1f, warning: false);

            if (artorias.RingCollapseWarningActive)
            {
                ArtoriasVFX.DrawBoundary(artorias.RingCenter, artorias.RingCollapseWarningRadius,
                    artorias.RingBandHalfWidthPixels, 0.32f, warning: true);
            }

            return false;
        }
    }
}
