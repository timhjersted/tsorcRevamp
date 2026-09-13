using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>Harmless, full-size Dark Bead held at the Purple Gem Staff tip for the final 30 ticks of its tell.</summary>
    class CinderDarkBeadPreview : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/ArtoriasDarkBead";

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 30;
            Projectile.alpha = 20;
        }

        public override void AI()
        {
            int ownerNpc = (int)Projectile.ai[0];
            if (ownerNpc < 0 || ownerNpc >= Main.maxNPCs || !Main.npc[ownerNpc].active)
            {
                Projectile.Kill();
                return;
            }

            NPC caster = Main.npc[ownerNpc];
            Projectile.Center = caster.Center + new Vector2(30f * caster.direction, -42f);
            Projectile.rotation += 0.20f;
            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Clentaminator_Cyan,
                    Main.rand.NextVector2Circular(0.45f, 0.45f), 100, new Color(115, 190, 255), 0.8f);
                dust.noGravity = true;
            }
        }
    }
}
