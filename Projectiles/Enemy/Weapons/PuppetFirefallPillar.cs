using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    /// <summary>Visible ground marker that becomes a short-lived vertical fire pillar. ai[0] is
    /// the remaining warning time; ai[1] is 0 while warning and 1 while the pillar is damaging;
    /// ai[2] opts into Dread Wraith's shader/dust presentation without changing other users.</summary>
    public class PuppetFirefallPillar : ModProjectile
    {
        public const float DreadWraithFireVisualStyle = 1f;
        private const float DreadDrawWidth = 64f;
        // Half the eruption height and drawn point-down (see DrawDreadWraithFirefall) so the warning
        // reads as something building INTO the ground rather than an already-burning flame — the two
        // used to share one upward silhouette, which made "safe" and "damaging" look identical.
        private const float DreadTelegraphHeight = 40f;
        private const float DreadEruptionHeight = 160f;
        private const float PixelBlockSize = 2f;
        private const string TextureRoot = "tsorcRevamp/Textures/Noise/";

        private static Asset<Effect> dreadFirefallEffect;
        private static Asset<Texture2D> dreadShapeNoise;
        private static Asset<Texture2D> dreadDetailNoise;

        private bool Erupting => Projectile.ai[1] == 1f;
        private bool UsesDreadWraithVisuals => Projectile.ai[2] == DreadWraithFireVisualStyle;
        private float TelegraphProgress => MathHelper.Clamp(
            1f - Projectile.ai[0] / System.Math.Max(Projectile.localAI[0], 1f), 0f, 1f);

        public override string Texture => "Terraria/Images/MagicPixel";

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 4;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 180;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            // Cosmetic only: remembering the initial warning duration lets every staggered pillar
            // ramp its mirror-flame telegraph across its own real countdown.
            Projectile.localAI[0] = System.Math.Max(Projectile.ai[0], 1f);
        }

        public override bool? CanDamage() => Erupting ? null : false;

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;

            if (!Erupting)
            {
                Projectile.ai[0]--;
                Lighting.AddLight(Projectile.Bottom, 0.45f, 0.14f, 0.02f);
                if (!Main.dedServ && UsesDreadWraithVisuals)
                {
                    SpawnDreadTelegraphDust();
                }
                else if (!Main.dedServ)
                {
                    SpawnLegacyTelegraphDust();
                }

                if (Projectile.ai[0] <= 0f)
                    BeginEruption();
                return;
            }

            Lighting.AddLight(Projectile.Center, 1.1f, 0.38f, 0.04f);
            if (!Main.dedServ && UsesDreadWraithVisuals)
            {
                SpawnDreadEruptionDust();
            }
            else if (!Main.dedServ)
            {
                SpawnLegacyEruptionDust();
            }
        }

        /// <summary>Legacy (non-Dread) telegraph dust — up to 3 rolls per tick, ramping with
        /// TelegraphProgress, same shape as SpawnDreadTelegraphDust below but keeping Owl Father's own
        /// plain-Torch/OrangeRed identity instead of the red/yellow torch tint. Was a single 25%-chance
        /// dust per tick; that's the "more dusts" the ground buildup needed.</summary>
        private void SpawnLegacyTelegraphDust()
        {
            const int maxRolls = 3;
            float chance = MathHelper.Lerp(0.45f, 0.9f, TelegraphProgress);
            for (int i = 0; i < maxRolls; i++)
            {
                if (Main.rand.NextFloat() >= chance)
                {
                    continue;
                }

                Dust warning = Dust.NewDustPerfect(
                    Projectile.Bottom + new Vector2(Main.rand.NextFloat(-22f, 22f), -2f),
                    DustID.Torch,
                    new Vector2(0f, Main.rand.NextFloat(-1.4f, -0.4f)),
                    120,
                    Color.OrangeRed,
                    Main.rand.NextFloat(0.65f, 0.95f));
                warning.noGravity = true;
            }
        }

        /// <summary>Legacy (non-Dread) eruption dust — 4x the original per-tick count (4 -> 16), same
        /// "more dusts" pass as Dread Wraith's own eruption dust got.</summary>
        private void SpawnLegacyEruptionDust()
        {
            for (int i = 0; i < 16; i++)
            {
                Dust flame = Dust.NewDustPerfect(
                    Projectile.Bottom + new Vector2(
                        Main.rand.NextFloat(-Projectile.width * 0.4f, Projectile.width * 0.4f),
                        Main.rand.NextFloat(-Projectile.height, -4f)),
                    Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame,
                    new Vector2(Main.rand.NextFloat(-0.8f, 0.8f), Main.rand.NextFloat(-3.5f, -1.2f)),
                    60,
                    Color.OrangeRed,
                    Main.rand.NextFloat(1.0f, 1.65f));
                flame.noGravity = true;
            }
        }

        /// <summary>Legacy (non-Dread) eruption burst — fired once from BeginEruption. There was no
        /// burst at all for this style before (only the Dread visual got one), so the legacy pillar had
        /// no punchy "whoosh" moment when it actually erupts. Same two-layer shape as
        /// SpawnDreadEruptionBurst (body motes + faster sparks), Owl Father's own Torch/GoldFlame colors.</summary>
        private void SpawnLegacyEruptionBurst(Vector2 bottom)
        {
            for (int i = 0; i < 40; i++)
            {
                Dust burst = Dust.NewDustPerfect(
                    bottom + new Vector2(Main.rand.NextFloat(-18f, 18f), Main.rand.NextFloat(-5f, 0f)),
                    DustID.GoldFlame,
                    new Vector2(Main.rand.NextFloat(-1.8f, 1.8f), Main.rand.NextFloat(-5.2f, -2.5f)),
                    55,
                    Color.OrangeRed,
                    Main.rand.NextFloat(0.68f, 1.12f));
                burst.noGravity = true;
            }

            for (int i = 0; i < 24; i++)
            {
                Dust spark = Dust.NewDustPerfect(
                    bottom + new Vector2(Main.rand.NextFloat(-14f, 14f), Main.rand.NextFloat(-4f, 0f)),
                    DustID.Torch,
                    new Vector2(Main.rand.NextFloat(-1.25f, 1.25f), Main.rand.NextFloat(-7f, -4f)),
                    45,
                    Color.Gold,
                    Main.rand.NextFloat(0.42f, 0.74f));
                spark.noGravity = true;
            }
        }

        /// <summary>The ENTIRE telegraph visual now, since the upside-down shader draw was removed (see
        /// PreDraw) — same DustID.RedTorch recipe as the goat's own charge embers (DreadWraith.cs,
        /// EmitFlameBurst's per-tick loop: alpha 100, no forced tint, scale 1.1-1.8), because that
        /// already reads well and reuses a look the player already associates with this boss. Ramps
        /// 3->7 per tick as release nears, up from the old shader-plus-sparse-dust combo which read as
        /// having no telegraph dust at all.</summary>
        private void SpawnDreadTelegraphDust()
        {
            int count = (int)MathHelper.Lerp(3f, 7f, TelegraphProgress);
            for (int i = 0; i < count; i++)
            {
                Dust flame = Dust.NewDustPerfect(
                    Projectile.Bottom + new Vector2(Main.rand.NextFloat(-16f, 16f), -2f),
                    DustID.RedTorch,
                    new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(-2.2f, -0.6f)),
                    100,
                    default,
                    Main.rand.NextFloat(1.1f, 1.8f));
                flame.noGravity = true;
            }
        }

        private void SpawnDreadEruptionDust()
        {
            // 4x the original per-tick count, per user request, so the eruption itself reads as
            // unambiguously more violent than the ground-level telegraph dust.
            for (int i = 0; i < 12; i++)
            {
                bool gold = Main.rand.NextBool(3);
                Dust flame = Dust.NewDustPerfect(
                    Projectile.Bottom + new Vector2(
                        Main.rand.NextFloat(-Projectile.width * 0.42f, Projectile.width * 0.42f),
                        Main.rand.NextFloat(-Projectile.height, -4f)),
                    gold ? DustID.GoldFlame : DustID.Torch,
                    new Vector2(Main.rand.NextFloat(-0.7f, 0.7f), Main.rand.NextFloat(-3.1f, -1.15f)),
                    gold ? 85 : 65,
                    gold ? new Color(255, 188, 42) : new Color(244, 76, 18),
                    Main.rand.NextFloat(0.68f, 1.18f));
                flame.noGravity = true;
            }
        }

        private void SpawnDreadEruptionBurst(Vector2 bottom)
        {
            // Two upward layers: gold body motes and smaller, faster yellow sparks. The cone follows
            // the pillar's motion rather than producing a generic radial explosion. Counts are 4x the
            // original (14/8 -> 56/32), per user request.
            for (int i = 0; i < 56; i++)
            {
                Dust burst = Dust.NewDustPerfect(
                    bottom + new Vector2(Main.rand.NextFloat(-18f, 18f), Main.rand.NextFloat(-5f, 0f)),
                    DustID.GoldFlame,
                    new Vector2(Main.rand.NextFloat(-1.8f, 1.8f), Main.rand.NextFloat(-5.2f, -2.5f)),
                    55,
                    new Color(255, 184, 34),
                    Main.rand.NextFloat(0.68f, 1.12f));
                burst.noGravity = true;
            }

            for (int i = 0; i < 32; i++)
            {
                Dust spark = Dust.NewDustPerfect(
                    bottom + new Vector2(Main.rand.NextFloat(-14f, 14f), Main.rand.NextFloat(-4f, 0f)),
                    DustID.YellowTorch,
                    new Vector2(Main.rand.NextFloat(-1.25f, 1.25f), Main.rand.NextFloat(-7f, -4f)),
                    45,
                    new Color(255, 220, 82),
                    Main.rand.NextFloat(0.42f, 0.74f));
                spark.noGravity = true;
            }
        }

        private void BeginEruption()
        {
            Vector2 bottom = Projectile.Bottom;
            if (!Main.dedServ)
            {
                if (UsesDreadWraithVisuals)
                {
                    SpawnDreadEruptionBurst(bottom);
                }
                else
                {
                    SpawnLegacyEruptionBurst(bottom);
                }
            }

            Projectile.ai[1] = 1f;
            Projectile.width = 36;
            Projectile.height = 160;
            Projectile.Bottom = bottom;
            Projectile.timeLeft = 22;
            Projectile.netUpdate = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (UsesDreadWraithVisuals)
            {
                // Telegraph no longer draws anything — the upside-down point-down shader read as
                // unclear ("is this safe or already damaging?") and hid the warning dust underneath it.
                // Dust (SpawnDreadTelegraphDust, called from AI()) IS the entire telegraph now. The
                // eruption itself keeps the shader.
                if (Erupting)
                {
                    DrawDreadWraithFirefall();
                }
                return false;
            }

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            if (!Erupting)
            {
                float pulse = 0.55f + 0.25f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 8f);
                Main.EntitySpriteDraw(
                    pixel,
                    Projectile.Bottom - Main.screenPosition,
                    null,
                    Color.OrangeRed * pulse,
                    0f,
                    new Vector2(0.5f),
                    new Vector2(48f, 3f),
                    SpriteEffects.None);
                for (int direction = -1; direction <= 1; direction += 2)
                {
                    Main.EntitySpriteDraw(
                        pixel,
                        Projectile.Bottom + new Vector2(direction * 20f, -5f) - Main.screenPosition,
                        null,
                        Color.Gold * pulse,
                        direction * 0.65f,
                        new Vector2(0.5f),
                        new Vector2(11f, 2f),
                        SpriteEffects.None);
                }
                return false;
            }

            float life = MathHelper.Clamp(Projectile.timeLeft / 8f, 0f, 1f);
            // Solid stacked blocks — exactly the original look, UNCHANGED (i goes 0..7 here, not 0..8:
            // the 9th/topmost block, previously a flat-cut rectangle, is replaced below by the flame
            // tip instead of drawn as a block). progress still divides by 8 so widths/colors/positions
            // for these 8 blocks land exactly where they always did.
            Vector2 tipAnchor = Vector2.Zero;
            for (int i = 0; i < 8; i++)
            {
                float progress = i / 8f;
                float width = MathHelper.Lerp(30f, 8f, progress);
                Vector2 offset = new Vector2((i % 2 == 0 ? -1f : 1f) * (1f - progress) * 5f, -progress * Projectile.height);
                Vector2 position = Projectile.Bottom + offset - Main.screenPosition;
                Main.EntitySpriteDraw(
                    pixel,
                    position,
                    null,
                    Color.Lerp(Color.OrangeRed, Color.Gold, progress) * (0.82f * life),
                    0f,
                    new Vector2(0.5f),
                    new Vector2(width, 20f),
                    SpriteEffects.None);

                if (i == 7)
                {
                    // Top edge of the last solid block (its center + half its own 20px height) is
                    // where the flame tip sprouts from.
                    tipAnchor = Projectile.Bottom + offset - new Vector2(0f, 10f);
                }
            }

            DrawFlameTip(pixel, tipAnchor, life);
            return false;
        }

        /// <summary>Replaces the old flat-cut top block with 3 thin tongues that sway independently
        /// (sine of real time, staggered phase per tongue and per projectile instance so a row of
        /// pillars doesn't flicker in lockstep) — reads as a living flame peak instead of a hard-edged
        /// rectangle, without touching the solid blocky body the rest of the pillar keeps.</summary>
        private void DrawFlameTip(Texture2D pixel, Vector2 anchorWorld, float life)
        {
            const int TongueCount = 3;
            for (int t = 0; t < TongueCount; t++)
            {
                float phase = t * 2.1f + Projectile.whoAmI * 0.7f;
                float sway = (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 6f + phase) * 0.22f;

                // Middle tongue reaches highest; the two side ones are shorter and thinner, so the
                // silhouette reads as one peak flanked by smaller licks rather than three equal spikes.
                bool isCenter = t == TongueCount / 2;
                float tongueHeight = (isCenter ? 22f : 14f) * life;
                float tongueWidth = isCenter ? 6f : 4f;
                float baseOffsetX = (t - TongueCount / 2) * 5f;
                float lean = sway + baseOffsetX * 0.03f; // side tongues also lean outward a little

                Vector2 baseWorld = anchorWorld + new Vector2(baseOffsetX, 0f);
                Color tongueColor = Color.Lerp(Color.Gold, Color.OrangeRed, isCenter ? 0f : 0.4f) * (0.75f * life);

                Main.EntitySpriteDraw(
                    pixel,
                    baseWorld - Main.screenPosition,
                    null,
                    tongueColor,
                    lean,
                    new Vector2(0.5f, 1f), // pivot at the tongue's OWN base, not its center, so it sways from its root
                    new Vector2(tongueWidth, tongueHeight),
                    SpriteEffects.None);
            }
        }

        private static void LoadDreadAssets()
        {
            dreadFirefallEffect ??= ModContent.Request<Effect>(
                "tsorcRevamp/Effects/DreadWraithFirefall", AssetRequestMode.ImmediateLoad);
            dreadShapeNoise ??= ModContent.Request<Texture2D>(
                TextureRoot + "SmoothNoise", AssetRequestMode.ImmediateLoad);
            dreadDetailNoise ??= ModContent.Request<Texture2D>(
                TextureRoot + "T_MarbleNoise_tiled", AssetRequestMode.ImmediateLoad);
        }

        private void DrawDreadWraithFirefall()
        {
            LoadDreadAssets();

            float drawHeight = Erupting ? DreadEruptionHeight : DreadTelegraphHeight;
            Vector2 drawSize = new Vector2(DreadDrawWidth, drawHeight);
            Vector2 pixelBlocks = drawSize / PixelBlockSize;
            Vector4 pixelGrid = new Vector4(
                pixelBlocks.X,
                pixelBlocks.Y,
                1f / pixelBlocks.X,
                1f / pixelBlocks.Y);
            float fadeOut = Erupting ? MathHelper.Clamp(Projectile.timeLeft / 8f, 0f, 1f) : 1f;
            float opacity = Erupting
                ? 0.96f * fadeOut
                : MathHelper.Lerp(0.24f, 0.38f, TelegraphProgress);
            float progress = Erupting ? 1f : TelegraphProgress;
            Texture2D primary = dreadShapeNoise.Value;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];
            try
            {
                graphicsDevice.Textures[1] = dreadDetailNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                Effect effect = dreadFirefallEffect.Value;
                effect.CurrentTechnique = effect.Techniques["DreadWraithFirefall"];
                effect.Parameters["DarkColor"]?.SetValue(new Color(108, 13, 2).ToVector3());
                effect.Parameters["FlameColor"]?.SetValue(new Color(255, 92, 8).ToVector3());
                effect.Parameters["CoreColor"]?.SetValue(new Color(255, 224, 82).ToVector3());
                effect.Parameters["Opacity"]?.SetValue(opacity);
                effect.Parameters["Time"]?.SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["Progress"]?.SetValue(progress);
                effect.Parameters["Active"]?.SetValue(Erupting ? 1f : 0f);
                effect.Parameters["PixelGrid"]?.SetValue(pixelGrid);
                effect.CurrentTechnique.Passes[0].Apply();

                Vector2 drawCenter = Projectile.Bottom - Vector2.UnitY * (drawHeight * 0.5f);
                // The shader's silhouette is narrow at texcoord v=0 and widest at v=1 (comment in the
                // .fx: "0 at the flame tip, 1 at the ground"). FlipVertically swaps which screen edge
                // gets which v, so the warning's wide end lands in the air and its narrow tip lands at
                // the ground — a spike stabbing DOWN, instead of the eruption's flame reaching UP.
                SpriteEffects telegraphFlip = Erupting ? SpriteEffects.None : SpriteEffects.FlipVertically;
                Main.EntitySpriteDraw(
                    primary,
                    drawCenter - Main.screenPosition,
                    null,
                    Color.White,
                    0f,
                    primary.Size() * 0.5f,
                    new Vector2(drawSize.X / primary.Width, drawSize.Y / primary.Height),
                    telegraphFlip,
                    0);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 6 * 60);
        }
    }
}
