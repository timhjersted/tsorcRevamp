using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.VFX
{
    // Toxic Gas Nova's telegraph fill: reuses TelegraphFlash's proven CatFinalStandAttack.fx swirling-noise
    // glow shader (see TelegraphFlash.cs) — same shader, same NoiseWavy texture, same draw technique — but
    // tinted green and held near FULL SIZE the whole telegraph (pulsing), so it reads as "this is where the
    // blast will land" rather than something still charging up in size. Brightens into a flash in its last
    // few frames to sell the detonation moment.
    // Purely visual: CanDamage() is false, Eland applies the actual hit itself when the telegraph ends.
    // ai[0] = owning NPC's whoAmI (so the glow tracks it if it drifts during the telegraph).
    // ai[1] = final radius the glow should grow to.
    public class ToxicGasNovaGlow : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";

        public const int DurationTicks = 60; // matches Toxic Gas Nova's telegraph length

        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = DurationTicks + 5;
        }

        float effectTimer;

        public override void AI()
        {
            int npcIndex = (int)Projectile.ai[0];
            if (npcIndex >= 0 && npcIndex < Main.maxNPCs && Main.npc[npcIndex].active)
            {
                Projectile.Center = Main.npc[npcIndex].Center;
            }

            effectTimer++;
            if (effectTimer >= DurationTicks)
            {
                Projectile.Kill();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrawGlow();
            return false;
        }

        static Effect lightEffect;
        float rotation;
        void DrawGlow()
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            if (lightEffect == null)
            {
                lightEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CatFinalStandAttack", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            rotation += 0.04f;

            // Steady pulse at (nearly) full radius — shows the true blast size throughout, doesn't
            // "grow into" it. A quick ramp-in over the first few frames avoids an instant pop-in.
            float rampIn = MathHelper.Clamp(effectTimer / 8f, 0f, 1f);
            float pulse = 0.85f + 0.15f * (float)Math.Sin(effectTimer * 0.3f);
            float growth = rampIn * pulse;

            // Detonation flash: brighten sharply in the last few frames.
            float ticksLeft = DurationTicks - effectTimer;
            float flash = ticksLeft <= 6f ? MathHelper.Lerp(1f, 2f, 1f - ticksLeft / 6f) : 1f;

            float radius = Projectile.ai[1];
            float baseSize = radius * 2.2f; // big enough that the glow spills past the dust ring a bit

            Color glow = new Color(60, 200, 90) * (0.5f + 0.3f * (float)Math.Sin(Main.GameUpdateCount * 0.15f)) * flash;

            Rectangle rect = new Rectangle(0, 0, (int)(baseSize * growth), (int)(baseSize * growth));
            Vector2 origin = rect.Size() / 2f;

            lightEffect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseWavy.Width);
            lightEffect.Parameters["effectSize"].SetValue(rect.Size());
            lightEffect.Parameters["effectColor"].SetValue(glow.ToVector4());
            lightEffect.Parameters["ringProgress"].SetValue(0.5f);
            lightEffect.Parameters["fadePercent"].SetValue(MathHelper.Clamp(1f - growth * 0.6f * flash, 0f, 1f));
            lightEffect.Parameters["time"].SetValue(-Main.GlobalTimeWrappedHourly * 2f);

            lightEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, Projectile.Center - Main.screenPosition, rect, Color.White, rotation, origin, 1, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, Projectile.Center - Main.screenPosition, rect, Color.White, -rotation, origin, 1, SpriteEffects.None, 0);

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
        }

        public override bool? CanDamage() => false;
    }
}
