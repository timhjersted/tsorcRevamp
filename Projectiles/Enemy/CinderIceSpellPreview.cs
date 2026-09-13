using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>Harmless staff-tip preview for Soul of Cinder's rising ice fan.</summary>
    class CinderIceSpellPreview : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Ice1Ball";

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 30;
            Projectile.alpha = 25;
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
            float progress = 1f - Projectile.timeLeft / 30f;
            Projectile.scale = MathHelper.Lerp(0.18f, 1f, progress);
            Projectile.rotation += 0.14f;
            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.IceTorch,
                    Main.rand.NextVector2Circular(0.65f, 0.65f), 80, new Color(125, 205, 255), 0.8f);
                dust.noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, new Vector3(0.20f, 0.55f, 1f) * Projectile.scale);
        }
    }
}
