using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Gigas consecrated ground: a lingering patch of golden fire left by slam landings and solar
    ///boulder impacts. Same invisible-projectile/dust approach as AcidPool, in holy gold.
    ///ai[0] = custom lifetime in ticks (0 = default).
    ///</summary>
    class GigasConsecratedGround : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public const int PatchWidth = 64;  //4 tiles
        public const int PatchHeight = 16;
        public const int PatchLifetime = 5 * 60;
        const string TextureRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> groundEffect;
        static Asset<Texture2D> macroNoise;
        static Asset<Texture2D> detailNoise;

        int Lifetime => Projectile.ai[0] > 0f ? (int)Projectile.ai[0] : PatchLifetime;

        static void LoadAssets()
        {
            groundEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GigasConsecratedGround", AssetRequestMode.ImmediateLoad);
            macroNoise ??= ModContent.Request<Texture2D>(TextureRoot + "SmoothNoise", AssetRequestMode.ImmediateLoad);
            detailNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Turbulence_06-512x512", AssetRequestMode.ImmediateLoad);
        }

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = PatchWidth;
            Projectile.height = PatchHeight;
            Projectile.tileCollide = false;
            Projectile.aiStyle = 0;
            Projectile.penetrate = -1;
            Projectile.timeLeft = PatchLifetime;
            Projectile.light = 0.4f;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;

            //Low burning layer of golden flame on the surface
            for (int i = 0; i < 2; i++)
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(PatchWidth), Projectile.position.Y + Main.rand.NextFloat(PatchHeight * 0.6f));
                int dust = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, 0f, -1f, 100, default, 1.3f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.2f;
                Main.dust[dust].velocity.Y = -1f;
            }
            //Occasional bright sparkle drifting up — fades as the patch ages
            float remaining = Projectile.timeLeft / (float)Lifetime;
            if (Main.rand.NextFloat() < 0.5f * remaining)
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(PatchWidth), Projectile.position.Y - 8f + Main.rand.NextFloat(10f));
                int sparkle = Dust.NewDust(pos, 4, 4, DustID.GoldCoin, 0f, -1.5f, 0, default, 0.9f);
                Main.dust[sparkle].noGravity = true;
                Main.dust[sparkle].velocity *= 0.2f;
                Main.dust[sparkle].velocity.Y = -1.4f;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 3 * 60);
        }

        // The lower flame tongues can disappear into the ground instead of layering over tiles;
        // the existing dust remains on Terraria's normal particle layer above this shader.
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
            List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LoadAssets();
            Texture2D primary = macroNoise.Value;
            float remaining = MathHelper.Clamp(Projectile.timeLeft / (float)Lifetime, 0f, 1f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];
            try
            {
                graphicsDevice.Textures[1] = detailNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                Effect effect = groundEffect.Value;
                effect.CurrentTechnique = effect.Techniques["GigasConsecratedGround"];
                effect.Parameters["OuterColor"].SetValue(new Color(52, 25, 3).ToVector3());
                effect.Parameters["MiddleColor"].SetValue(new Color(222, 127, 9).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 230, 138).ToVector3());
                effect.Parameters["Opacity"].SetValue(0.88f);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["Remaining"].SetValue(remaining);
                effect.CurrentTechnique.Passes[0].Apply();

                // 88x36 gives the shader room for its decorative canopy. Its bright bed is mapped
                // precisely to PatchWidth x PatchHeight, so collision and telegraph stay aligned.
                Main.EntitySpriteDraw(primary, Projectile.Center - Main.screenPosition, null, Color.White,
                    0f, primary.Size() * 0.5f, new Vector2(88f / primary.Width, 36f / primary.Height),
                    SpriteEffects.None, 0);
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
