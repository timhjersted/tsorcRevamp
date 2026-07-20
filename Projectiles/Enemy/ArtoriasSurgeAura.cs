using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.SuperHardMode;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class ArtoriasSurgeAura : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        private static Asset<Effect> shaderEffect;
        private Vector2 noiseOffset;

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            int ownerIndex = (int)Projectile.ai[0];
            if (ownerIndex < 0 || ownerIndex >= Main.maxNPCs
                || !Main.npc[ownerIndex].active
                || Main.npc[ownerIndex].ModNPC is not Artorias artorias
                || !artorias.AbyssSurgeActive)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = artorias.NPC.Center;
            Projectile.timeLeft = 2;
            noiseOffset -= new Vector2(0.5f, -0.1f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int ownerIndex = (int)Projectile.ai[0];
            if (ownerIndex < 0 || ownerIndex >= Main.maxNPCs || !Main.npc[ownerIndex].active)
            {
                return false;
            }

            Vector2 center = Main.npc[ownerIndex].Center;

            shaderEffect ??= ModContent.Request<Effect>(
                "tsorcRevamp/Effects/MarilithFireAura", AssetRequestMode.ImmediateLoad);
            Effect effect = shaderEffect.Value;
            if (effect?.CurrentTechnique == null)
                return false;

            effect.Parameters["uColor"]?.SetValue(new Vector3(center.X, center.Y, noiseOffset.X));
            effect.Parameters["uOpacity"]?.SetValue(noiseOffset.Y);
            effect.Parameters["uSaturation"]?.SetValue(1f);

            Rectangle source = tsorcRevamp.NoiseTurbulent.Bounds;
            const float scale = 10f;
            Main.spriteBatch.End();
            try
            {
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp,
                    DepthStencilState.None, RasterizerState.CullNone, effect, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(tsorcRevamp.NoiseTurbulent, center - Main.screenPosition,
                    source, Color.White, 0f, source.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
            finally
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                    DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            }

            return false;
        }
    }
}
