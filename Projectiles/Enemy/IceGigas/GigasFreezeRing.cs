using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Absolute Zero's release: an expanding annular freeze wave (same true-ring collision as
    ///GigasNovaRing — inside the wave is safe, and it can be rolled through). ai[0] = max radius (px).
    ///</summary>
    class GigasFreezeRing : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float ExpandSpeed = 9f;
        const float RingHalfThickness = 22f;
        //Margin between the outer lip (RingRadius + RingHalfThickness) and the quad edge, so the
        //shader's feathering has somewhere to land instead of being sliced flat by the boundary.
        const float QuadPadding = 40f;
        const float TrailLength = 50f; //how far the ragged cold wake trails INSIDE the lip
        const float FadeTicks = 8f;
        const string TextureRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> ringEffect;
        static Asset<Texture2D> cellNoise;
        static Asset<Texture2D> crackNoise;

        float MaxRadius => Projectile.ai[0] > 0f ? Projectile.ai[0] : 300f;
        float Radius => Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 660;
            Projectile.height = 660;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 40;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = (int)(MaxRadius / ExpandSpeed) + 2;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0] += ExpandSpeed;

            int points = 36;
            for (int i = 0; i < points; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = Projectile.Center + angle.ToRotationVector2() * (Radius + Main.rand.NextFloat(-8f, 8f));
                int dust = Dust.NewDust(pos, 4, 4, DustID.IceTorch, 0f, 0f, 60, default, Main.rand.NextFloat(1.3f, 1.9f));
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = angle.ToRotationVector2() * 2.5f;
            }
            if (Main.rand.NextBool(2))
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = Projectile.Center + angle.ToRotationVector2() * Radius;
                int crystal = Dust.NewDust(pos, 4, 4, DustID.IceRod, 0f, 0f, 40, default, 1.1f);
                Main.dust[crystal].noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.6f, 0.9f, 1.3f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = new Vector2(
                MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right),
                MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            float distClosest = Vector2.Distance(Projectile.Center, closest);
            float distFarthest = 0f;
            distFarthest = System.Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Left, targetHitbox.Top)));
            distFarthest = System.Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Right, targetHitbox.Top)));
            distFarthest = System.Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Left, targetHitbox.Bottom)));
            distFarthest = System.Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Right, targetHitbox.Bottom)));
            return distClosest <= Radius + RingHalfThickness && distFarthest >= Radius - RingHalfThickness;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Frostburn, 4 * 60);
            target.AddBuff(BuffID.Chilled, 3 * 60);
        }

        static void LoadAssets()
        {
            ringEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/IceGigasFreezeRing", AssetRequestMode.ImmediateLoad);
            //Flat-shaded polygonal cells: used as crystal facet GEOMETRY, not as a smooth modulator
            cellNoise ??= ModContent.Request<Texture2D>(TextureRoot + "VoronoiNoise", AssetRequestMode.ImmediateLoad);
            crackNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Vein_07-512x512", AssetRequestMode.ImmediateLoad);
        }

        ///<summary>The giant should stand in FRONT of its own nova, and the existing dust already
        ///draws in a later pass — so this shader lands behind both without touching either.</summary>
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
            List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LoadAssets();
            float drawRadius = Radius + QuadPadding;
            int diameter = (int)Math.Ceiling(drawRadius * 2f);
            if (diameter <= 0)
            {
                return false; //first tick, before the wave has any radius to draw
            }
            Texture2D primary = cellNoise.Value;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];
            try
            {
                graphicsDevice.Textures[1] = crackNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                Effect effect = ringEffect.Value;
                effect.CurrentTechnique = effect.Techniques["IceGigasFreezeRing"];
                effect.Parameters["OuterColor"].SetValue(new Color(18, 40, 68).ToVector3());
                effect.Parameters["MiddleColor"].SetValue(new Color(88, 168, 226).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(205, 236, 250).ToVector3());
                effect.Parameters["Opacity"].SetValue(MathHelper.Clamp(Projectile.timeLeft / FadeTicks, 0f, 1f));
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["DrawSize"].SetValue(new Vector2(diameter));
                effect.Parameters["RingRadius"].SetValue(Radius);
                effect.Parameters["RingHalfThickness"].SetValue(RingHalfThickness);
                effect.Parameters["TrailLength"].SetValue(TrailLength);
                //Pre-divided pixel grid (2px blocks). Doing this division in HLSL instead would
                //cost ~12 per-pixel slots and push the shader over the ps_2_0 arithmetic budget.
                effect.Parameters["PixelGrid"].SetValue(new Vector4(
                    diameter / 2f, diameter / 2f, 2f / diameter, 2f / diameter));
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(primary, Projectile.Center - Main.screenPosition, null, Color.White, 0f,
                    primary.Size() * 0.5f, diameter / (float)primary.Width, SpriteEffects.None, 0);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
            }

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            return false;
        }
    }
}
