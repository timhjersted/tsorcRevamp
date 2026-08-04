using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.VFX
{
    /// <summary>
    /// Server-spawned, non-damaging telegraph for Eland's 400px toxic nova. The shader's bright
    /// circumference is the exact future damage radius; its dim interior remains only a warning.
    /// ai[0] = owning NPC index, ai[1] = radius.
    /// </summary>
    public class ToxicGasNovaGlow : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public const int DurationTicks = 60;

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = DurationTicks + 5;
            Projectile.hostile = false;
            Projectile.friendly = false;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            int npcIndex = (int)Projectile.ai[0];
            if (npcIndex >= 0 && npcIndex < Main.maxNPCs && Main.npc[npcIndex].active)
            {
                Projectile.Center = Main.npc[npcIndex].Center;
            }

            Projectile.localAI[0]++;
            if (Projectile.localAI[0] >= DurationTicks)
            {
                Projectile.Kill();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Pass real 0->1 progress: the old `progress * 0.18f` was a brightness hack that also
            // pinned the shader to the very start of its animation curve. Telegraph dimming is the
            // shader's own job now (the active:false path renders at roughly half strength).
            float progress = MathHelper.Clamp(Projectile.localAI[0] / DurationTicks, 0f, 1f);
            Projectiles.Enemy.EnemyVFX.DrawElandToxicField(
                Projectile.Center, Vector2.One * Projectile.ai[1] * 2f,
                progress, false);
            return false;
        }
    }
}
