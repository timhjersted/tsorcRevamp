using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Gwyn's Cinder Nova blast: an expanding ring of the First Flame. A dedicated radial shader is
    ///driven by the same pixel radius and half-thickness as the true annulus hitbox, so its white-hot
    ///front is the damage read while turbulent fire trails inward over already-safe ground.
    ///ai[0] = max radius (px).
    ///</summary>
    class GwynCinderNova : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float ExpandSpeed = 10f;
        const float RingHalfThickness = 30f;
        const float FlameTrailLength = 58f;
        //The shader's outermost term is the lip, which dies 3px past RingRadius + RingHalfThickness.
        //12px of padding leaves the feather four times the room it needs and nothing can clip.
        const float DrawPadding = 12f;
        const float PixelBlockSize = 2f;
        const string TextureRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> cinderNovaEffect;
        static Asset<Texture2D> shapeNoise;
        static Asset<Texture2D> detailNoise;
        static Asset<Texture2D> ignitionFlare;

        float MaxRadius => Projectile.ai[0] > 0f ? Projectile.ai[0] : 420f;
        float Radius => Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 900;  // broadphase; real collision is the annulus in Colliding()
            Projectile.height = 900;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 60;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = (int)(MaxRadius / ExpandSpeed) + 4;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.9f, Pitch = -0.2f }, Projectile.Center); // fiery whoosh
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0] += ExpandSpeed;
            //Ember supplement on the leading edge
            for (int i = 0; i < 6; i++)
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = Projectile.Center + ang.ToRotationVector2() * Radius;
                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                int dust = Dust.NewDust(pos, 4, 4, type, 0f, 0f, 60, default, 1.4f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = ang.ToRotationVector2() * 2f;
            }
            Lighting.AddLight(Projectile.Center, 1.1f, 0.6f, 0.2f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = new Vector2(
                MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right),
                MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            float distClosest = Vector2.Distance(Projectile.Center, closest);
            float distFarthest = 0f;
            distFarthest = Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Left, targetHitbox.Top)));
            distFarthest = Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Right, targetHitbox.Top)));
            distFarthest = Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Left, targetHitbox.Bottom)));
            distFarthest = Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Right, targetHitbox.Bottom)));
            return distClosest <= Radius + RingHalfThickness && distFarthest >= Radius - RingHalfThickness;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 6 * 60);
        }

        static void LoadAssets()
        {
            cinderNovaEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GwynCinderNova", AssetRequestMode.ImmediateLoad);
            //Both fields are seamless under a 2x2 tile check. The previous pair were not: T_VFX_NoiseF1
            //shows its tile boundary outright, and T_VFX_Noise41 is posterized blobs, which is most of
            //why the ring used to read as a flat hoop rather than as fire.
            shapeNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Turbulence_06-512x512", AssetRequestMode.ImmediateLoad);
            detailNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Turbulence_07-512x512", AssetRequestMode.ImmediateLoad);
            ignitionFlare ??= ModContent.Request<Texture2D>(TextureRoot + "T_VFX_Flare_616", AssetRequestMode.ImmediateLoad);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LoadAssets();

            float opacity = MathHelper.Clamp(Projectile.timeLeft / 12f, 0.35f, 1f);
            float drawRadius = Math.Max(2f, Radius + RingHalfThickness + DrawPadding);
            int drawDiameter = Math.Max(2, (int)Math.Ceiling(drawRadius * 2f));
            Rectangle source = new Rectangle(0, 0, drawDiameter, drawDiameter);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            //Two passes with different blend states. The ignition flash is honestly just light, so it
            //stays additive; the ring is fire with a sooty body, which only premultiplied alpha can
            //render — additive cannot produce a saturated colour over the daytime sky, and that is
            //exactly why this effect used to wash out to a pale hoop.
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            DrawIgnitionFlash(drawPosition);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            DrawShaderRing(drawPosition, source, opacity);

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            return false;
        }

        void DrawIgnitionFlash(Vector2 drawPosition)
        {
            float ignition = MathHelper.Clamp(1f - Radius / 150f, 0f, 1f);
            if (ignition <= 0f)
                return;

            Texture2D texture = ignitionFlare.Value;
            float expansion = 1f - ignition;
            float scale = MathHelper.Lerp(0.18f, 0.42f, expansion);
            Main.EntitySpriteDraw(texture, drawPosition, null, new Color(255, 146, 32) * ignition,
                Main.GlobalTimeWrappedHourly * 0.35f, texture.Size() * 0.5f, scale, SpriteEffects.None, 0);
        }

        void DrawShaderRing(Vector2 drawPosition, Rectangle source, float opacity)
        {
            Effect effect = cinderNovaEffect.Value;
            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];

            try
            {
                graphicsDevice.Textures[1] = detailNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                Vector2 drawSize = source.Size();
                //Pre-divided so the shader does not repeat a max() and two reciprocals for every
                //pixel. Uniform-only maths is NOT free at ps_2_0 — there is no preshader — and this
                //form costs ~2 slots against ~12 for the same arithmetic written in HLSL.
                Vector2 pixelBlocks = drawSize / PixelBlockSize;
                Vector4 pixelGrid = new Vector4(pixelBlocks.X, pixelBlocks.Y, 1f / pixelBlocks.X, 1f / pixelBlocks.Y);

                //OuterColor is a dark ember red, not a bright one. Under premultiplied alpha the
                //trailing soot is meant to OCCLUDE the sky; a bright outer tone there just makes the
                //whole ring a glowing donut with no burnt tail.
                effect.CurrentTechnique = effect.Techniques["GwynCinderNova"];
                effect.Parameters["OuterColor"].SetValue(new Color(58, 9, 3).ToVector3());
                effect.Parameters["FlameColor"].SetValue(new Color(255, 122, 16).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 238, 176).ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["DrawSize"].SetValue(drawSize);
                effect.Parameters["CoordScale"].SetValue(shapeNoise.Value.Size() / drawSize);
                effect.Parameters["PixelGrid"].SetValue(pixelGrid);
                effect.Parameters["RingRadius"].SetValue(Radius);
                effect.Parameters["RingHalfThickness"].SetValue(RingHalfThickness);
                effect.Parameters["TrailLength"].SetValue(FlameTrailLength);
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(shapeNoise.Value, drawPosition, source, Color.White, 0f,
                    source.Size() * 0.5f, 1f, SpriteEffects.None, 0);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
            }
        }
    }
}
