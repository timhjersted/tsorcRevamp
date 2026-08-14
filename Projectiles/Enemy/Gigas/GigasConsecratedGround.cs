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
    ///Gigas consecrated ground: a lingering patch of golden fire left by slam landings and solar
    ///boulder impacts. Same invisible-projectile/dust approach as AcidPool, in holy gold.
    ///ai[0] = custom lifetime in ticks (0 = default); ai[1] = size variant; ai[2] = visual column
    ///scale. Each instance is one terrain tile wide, so a fire field follows individual hill tiles.
    ///</summary>
    class GigasConsecratedGround : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public const int PatchWidth = 16; // one terrain tile per module
        public const int SlamHeight = 32; // twice the former 16px damaging fire height
        public const int BoulderHeight = 64; // four tiles tall: a boulder impact needs a real vertical fire zone
        public const int SlamSpanTiles = 12; // 192px, three times the former 64px patch
        public const int BoulderSpanTiles = 16; // 256px, four times the former 64px patch
        public const int SlamVariant = 1;
        public const int BoulderVariant = 2;
        public const int PatchLifetime = 5 * 60;
        const string TextureRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> groundEffect;
        static Asset<Texture2D> macroNoise;
        static Asset<Texture2D> detailNoise;

        int Lifetime => Projectile.ai[0] > 0f ? (int)Projectile.ai[0] : PatchLifetime;
        int Variant => (int)Projectile.ai[1];
        int PatchHeight => HeightForVariant(Variant);
        bool IsBoulderFire => Variant == BoulderVariant;
        float ColumnScale => Projectile.ai[2] > 0f ? Projectile.ai[2] : 1f;
        float VisualWidth => (IsBoulderFire ? 52f : 60f) * ColumnScale;
        float VisualHeight => (IsBoulderFire ? 180f : 152f) * ColumnScale;

        public static int HeightForVariant(int variant) => variant == BoulderVariant ? BoulderHeight : SlamHeight;

        /// <summary>Stable organic interior variation, with deliberate 2/3 and 1/3 end caps.</summary>
        public static float ColumnScaleForField(int offset, int spanTiles, int variant)
        {
            int leftEdgeDistance = offset + spanTiles / 2;
            int rightEdgeDistance = spanTiles / 2 - 1 - offset;
            int edgeDistance = Math.Min(leftEdgeDistance, rightEdgeDistance);
            if (edgeDistance <= 0) return 1f / 3f;
            if (edgeDistance == 1) return 2f / 3f;

            int hash = Math.Abs(offset * 37 + spanTiles * 19 + variant * 53) % 7;
            return 0.88f + hash * 0.04f;
        }

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
            Projectile.height = SlamHeight;
            Projectile.tileCollide = false;
            Projectile.aiStyle = 0;
            Projectile.penetrate = -1;
            Projectile.timeLeft = PatchLifetime;
            Projectile.light = 0.4f;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            // NewProjectile assigns AI before OnSpawn. Preserve the authored ground-aligned center
            // while applying the mode's collision height.
            Vector2 center = Projectile.Center;
            Projectile.width = PatchWidth;
            Projectile.height = PatchHeight;
            Projectile.Center = center;
            Projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;

            // Taller columns gain density through count, not oversized square dust. Interior plumes
            // average 1.5 (Slam) / 2 (Boulder) at 0.70-1.25 scale; tapered ends stay sparse.
            int plumeCount = ColumnScale >= 0.85f && Main.rand.NextBool(2) ? 2 : 1;
            if (IsBoulderFire && ColumnScale >= 0.85f && Main.rand.NextBool(2))
            {
                plumeCount++;
            }
            for (int i = 0; i < plumeCount; i++)
            {
                Vector2 pos = new Vector2(Projectile.Center.X + Main.rand.NextFloat(-VisualWidth * 0.32f, VisualWidth * 0.32f), Projectile.Bottom.Y - Main.rand.NextFloat(VisualHeight * 0.85f));
                int dust = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, 0f, -1f, 70, default,
                    Main.rand.NextFloat(0.70f, IsBoulderFire ? 1.25f : 1.10f));
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = new Vector2(Main.rand.NextFloat(-0.35f, 0.35f), Main.rand.NextFloat(-3.1f, -1.2f));
            }
            //Occasional bright sparkle drifting up — fades as the patch ages
            float remaining = Projectile.timeLeft / (float)Lifetime;
            if (Main.rand.NextFloat() < 0.5f * remaining)
            {
                Vector2 pos = new Vector2(Projectile.Center.X + Main.rand.NextFloat(-VisualWidth * 0.28f, VisualWidth * 0.28f), Projectile.Bottom.Y - Main.rand.NextFloat(VisualHeight));
                int sparkle = Dust.NewDust(pos, 4, 4, DustID.GoldCoin, 0f, -1.5f, 0, default, 0.75f);
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
                effect.Parameters["OuterColor"].SetValue(new Color(92, 50, 3).ToVector3());
                effect.Parameters["MiddleColor"].SetValue(new Color(255, 183, 20).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 249, 190).ToVector3());
                effect.Parameters["Opacity"].SetValue(0.94f);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["Remaining"].SetValue(remaining);
                effect.Parameters["Phase"].SetValue(Projectile.identity * 0.173f);
                effect.Parameters["DrawSize"].SetValue(new Vector2(VisualWidth, VisualHeight));
                effect.Parameters["PixelBlockSize"].SetValue(2f);
                effect.CurrentTechnique.Passes[0].Apply();

                float groundEmbed = IsBoulderFire ? 38f : 12f;
                // The sharp damage column is one tile wide and PatchHeight tall. The filtered shell
                // overlaps its neighbours only decoratively. Its low fade extends into the tile,
                // so the visible flame reaches the surface without ending in a flat horizontal cut.
                Main.EntitySpriteDraw(primary, new Vector2(Projectile.Center.X, Projectile.Bottom.Y + groundEmbed - VisualHeight / 2f) - Main.screenPosition, null, Color.White,
                    0f, primary.Size() * 0.5f, new Vector2(VisualWidth / primary.Width, VisualHeight / primary.Height),
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
