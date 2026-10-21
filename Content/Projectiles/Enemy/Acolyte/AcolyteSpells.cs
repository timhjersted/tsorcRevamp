using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy.Acolyte
{
    // Every projectile of the Acolyte of Grand Occultist (NPCs/Enemies/SuperHardMode/OolacileAcolyte.cs) lives in this file:
    // one system, one file. The Madness ones draw through Effects/OolacileMadness.fx (AcolyteMadnessDraw below); none of
    // them have a sprite, so they all borrow the transparent InvisibleNothingProj texture on purpose.

    /// <summary>
    /// The acolyte's Madness buildup per hit, in one table so the proc rate can be tuned in one place (plan risk #1).
    /// A Marked player (Mark of Madness) takes double from every acolyte hit.
    /// </summary>
    public static class AcolyteMadness
    {
        public const int Shriek = 25;
        public const int Gaze = 15;
        public const int Scrawl = 20;
        public const int Spiral = 8;
        public const int FrenziedVolley = 10;
        public const int Hallucination = 8;
        public const int Mirror = 15;
        public const int Cradle = 15;
        public const int Clock = 8;

        public static readonly Color DarkColor = new Color(120, 78, 0);
        public static readonly Color MidColor = new Color(255, 220, 45);
        public static readonly Color CoreColor = Color.White;

        // Runs in a hostile projectile's OnHitPlayer, i.e. on the hit player's own machine; MadnessBuildup.Apply then reports
        // it to the server, which owns the meter.
        public static void Apply(Player player, int buildup)
        {
            if (player.HasBuff(ModContent.BuffType<MadnessMarked>()))
            {
                buildup *= 2;
            }

            MadnessBuildup.Apply(player, buildup);
        }

        // Yellow/white spark burst used by most Madness deaths.
        public static void SparkBurst(Vector2 center, int count, float speed)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                int dustType = DustID.YellowTorch;
                if (i % 3 == 0)
                {
                    dustType = DustID.WhiteTorch;
                }

                Dust spark = Dust.NewDustPerfect(center, dustType, Main.rand.NextVector2Circular(speed, speed), 0, default, Main.rand.NextFloat(1f, 1.6f));
                spark.noGravity = true;
            }
        }
    }

    /// <summary>
    /// Draws the Effects/OolacileMadness.fx techniques. Quads go through a SpriteBatch (cloud noise drawn as the quad, broken
    /// noise in s1, like EnemyVFX); strips (Scrawl, Cradle threads) build a VertexStrip along world-space points. Both use
    /// premultiplied AlphaBlend and the 2px pixel filter (PixelGrid pre-divided here, not in HLSL). Its own helper rather
    /// than EnemyVFX.Draw because these techniques take a float4 Extra and the strips need a vertex shader.
    /// The Frenzy* helpers drive the "frenzied" look: every value re-rolls on a 6-frame step (<= 10 Hz, plan §5 guard).
    /// </summary>
    public static class AcolyteMadnessDraw
    {
        public readonly struct Palette
        {
            public readonly Color Dark;
            public readonly Color Mid;
            public readonly Color Core;

            public Palette(Color dark, Color mid, Color core)
            {
                Dark = dark;
                Mid = mid;
                Core = core;
            }
        }

        public static readonly Palette Madness = new Palette(AcolyteMadness.DarkColor, AcolyteMadness.MidColor, AcolyteMadness.CoreColor);
        public static readonly Palette Abyss = new Palette(new Color(40, 0, 10), new Color(190, 20, 45), new Color(255, 140, 150));

        private const float PixelBlockSize = 2f;
        private const int FrenzyStepFrames = 6;

        private static Asset<Effect> _effect;
        private static Asset<Texture2D> _cloudNoise;
        private static Asset<Texture2D> _detailNoise;

        private static bool LoadAssets()
        {
            if (Main.dedServ)
            {
                return false;
            }

            _effect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/OolacileMadness", AssetRequestMode.ImmediateLoad);
            _cloudNoise ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/T_CloudNoise_Tiled", AssetRequestMode.ImmediateLoad);
            _detailNoise ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/T_VFX_Noise41", AssetRequestMode.ImmediateLoad);
            return true;
        }

        private static void SetParameters(Effect effect, string technique, Palette palette, float opacity, float progress, float active, Vector4 extra, Vector2 pixelSize)
        {
            effect.CurrentTechnique = effect.Techniques[technique];
            effect.Parameters["DarkColor"]?.SetValue(palette.Dark.ToVector3());
            effect.Parameters["MidColor"]?.SetValue(palette.Mid.ToVector3());
            effect.Parameters["CoreColor"]?.SetValue(palette.Core.ToVector3());
            effect.Parameters["Opacity"]?.SetValue(opacity);
            effect.Parameters["Time"]?.SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["Progress"]?.SetValue(progress);
            effect.Parameters["Active"]?.SetValue(active);
            effect.Parameters["Extra"]?.SetValue(extra);

            Vector2 pixelBlocks = Vector2.Max(pixelSize / PixelBlockSize, Vector2.One);
            effect.Parameters["PixelGrid"]?.SetValue(new Vector4(pixelBlocks.X, pixelBlocks.Y, 1f / pixelBlocks.X, 1f / pixelBlocks.Y));
        }

        // A procedural quad of `size` world pixels centred on `worldCenter`, rotated by `rotation` (0 = +x along the quad).
        public static void Quad(string technique, Vector2 worldCenter, Vector2 size, float rotation, Palette palette, float opacity,
            float progress, Vector4 extra, float active = 1f)
        {
            if (opacity <= 0f || !LoadAssets())
            {
                return;
            }

            Texture2D primary = _cloudNoise.Value;
            Vector2 scale = size / primary.Size();

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None,
                RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            GraphicsDevice device = Main.instance.GraphicsDevice;
            Texture previousTexture = device.Textures[1];
            SamplerState previousSampler = device.SamplerStates[1];

            try
            {
                device.Textures[1] = _detailNoise.Value;
                device.SamplerStates[1] = SamplerState.LinearWrap;
                SetParameters(_effect.Value, technique, palette, opacity, progress, active, extra, size);
                _effect.Value.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(primary, worldCenter - Main.screenPosition, null, Color.White, rotation, primary.Size() * 0.5f, scale, SpriteEffects.None, 0);
            }
            finally
            {
                device.Textures[1] = previousTexture;
                device.SamplerStates[1] = previousSampler;
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }
        }

        // A band of half-width `halfWidth` along world-space `points` (texcoord x runs 0..1 from the first point to the last).
        public static void Strip(string technique, Vector2[] points, float halfWidth, Palette palette, float opacity, float progress,
            Vector4 extra, float active = 1f)
        {
            if (opacity <= 0f || points == null || points.Length < 2 || !LoadAssets())
            {
                return;
            }

            float length = 0f;
            float[] rotations = new float[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                int from = Math.Max(0, i - 1);
                int to = Math.Min(points.Length - 1, i + 1);
                rotations[i] = (points[to] - points[from]).ToRotation();

                if (i > 0)
                {
                    length += Vector2.Distance(points[i - 1], points[i]);
                }
            }

            Main.spriteBatch.End();
            GraphicsDevice device = Main.instance.GraphicsDevice;
            Texture previousTexture0 = device.Textures[0];
            Texture previousTexture1 = device.Textures[1];
            SamplerState previousSampler0 = device.SamplerStates[0];
            SamplerState previousSampler1 = device.SamplerStates[1];

            try
            {
                device.BlendState = BlendState.AlphaBlend;
                device.RasterizerState = RasterizerState.CullNone;
                device.DepthStencilState = DepthStencilState.None;
                device.Textures[0] = _cloudNoise.Value;
                device.Textures[1] = _detailNoise.Value;
                device.SamplerStates[0] = SamplerState.LinearWrap;
                device.SamplerStates[1] = SamplerState.LinearWrap;

                Effect effect = _effect.Value;
                SetParameters(effect, technique, palette, opacity, progress, active, extra, new Vector2(length, halfWidth * 2f));
                effect.Parameters["WorldViewProjection"]?.SetValue(VFX.DynamicTrail.GetWorldViewProjectionMatrix());
                effect.CurrentTechnique.Passes[0].Apply();

                VertexStrip strip = new VertexStrip();
                strip.PrepareStrip(points, rotations, progressAlong => Color.White, progressAlong => halfWidth, -Main.screenPosition, includeBacksides: true);
                strip.DrawTrail();
            }
            finally
            {
                device.Textures[0] = previousTexture0;
                device.Textures[1] = previousTexture1;
                device.SamplerStates[0] = previousSampler0;
                device.SamplerStates[1] = previousSampler1;
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None,
                    RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            }
        }

        // Deterministic 0..1 value for (salt, channel) that changes once per 6-frame step.
        public static float FrenzyRandom(int salt, int channel)
        {
            uint step = Main.GameUpdateCount / FrenzyStepFrames;
            uint hash = step * 747796405u + (uint)salt * 2891336453u + (uint)channel * 277803737u;
            hash ^= hash >> 15;
            hash *= 2246822519u;
            hash ^= hash >> 13;
            return (hash & 0xFFFF) / 65535f;
        }

        // A sprite offset of up to +-maxPixels, snapped to the 2px grid, re-rolled every 6 frames. Visual only: hitboxes ignore it.
        public static Vector2 FrenzyJitter(int salt, float maxPixels)
        {
            float jitterX = (FrenzyRandom(salt, 0) * 2f - 1f) * maxPixels;
            float jitterY = (FrenzyRandom(salt, 1) * 2f - 1f) * maxPixels;
            return new Vector2(MathF.Round(jitterX / 2f) * 2f, MathF.Round(jitterY / 2f) * 2f);
        }

        // Core-brightness flicker in 0.8..1.1: local to one projectile, never black, never a full white flash.
        public static float Flicker(int salt)
        {
            return 0.8f + FrenzyRandom(salt, 2) * 0.3f;
        }

        // MadnessMote's Extra: inverse arm lengths (+x, -x, +y, -y), each arm re-rolled between 0.45 and 0.95 of the quad.
        public static Vector4 FlareArms(int salt)
        {
            float armRight = MathHelper.Lerp(0.45f, 0.95f, FrenzyRandom(salt, 3));
            float armLeft = MathHelper.Lerp(0.45f, 0.95f, FrenzyRandom(salt, 4));
            float armDown = MathHelper.Lerp(0.45f, 0.95f, FrenzyRandom(salt, 5));
            float armUp = MathHelper.Lerp(0.45f, 0.95f, FrenzyRandom(salt, 6));
            return new Vector4(1f / armRight, 1f / armLeft, 1f / armDown, 1f / armUp);
        }
    }

    /// <summary>
    /// Shriek / the Crack: a jagged yellow-white ring expanding from 0 to 160px over 14 ticks around the acolyte, then fading
    /// over 8 (harmless while it fades). True annulus collision (22px band), so standing inside it or rolling through the
    /// band is safe. Knocks the player 7 px/t away and 3 up through ordinary velocity, so tiles stop the throw.
    /// </summary>
    public class AcolyteShriekRing : ModProjectile
    {
        public const float MaxRadius = 160f;
        public const int ExpandTicks = 14;
        private const int FadeTicks = 8;
        private const float HalfThickness = 11f;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private int Age
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        private float Radius => MaxRadius * Math.Min(1f, Age / (float)ExpandTicks);

        public override void SetDefaults()
        {
            // Broadphase box = the full ring; NewProjectile centres it on the acolyte.
            Projectile.width = (int)(MaxRadius * 2f);
            Projectile.height = (int)(MaxRadius * 2f);
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = ExpandTicks + FadeTicks;
        }

        public override bool? CanDamage()
        {
            if (Age > ExpandTicks)
            {
                return false;
            }

            return null;
        }

        public override void AI()
        {
            Age++;
            Projectile.velocity = Vector2.Zero;

            if (Main.dedServ)
            {
                return;
            }

            // Crackling edge sparks ride the ring's live radius (the shader draws the ring itself).
            for (int i = 0; i < 5; i++)
            {
                Vector2 outward = Main.rand.NextVector2Unit();
                int dustType = DustID.YellowTorch;
                if (i % 3 == 0)
                {
                    dustType = DustID.WhiteTorch;
                }

                Dust spark = Dust.NewDustPerfect(Projectile.Center + outward * Radius, dustType, outward * 1.5f, 0, default, 1.3f);
                spark.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, AcolyteMadness.MidColor.ToVector3() * 0.6f);
        }

        // Hit iff the hitbox reaches the band: nearest point within r + half-thickness, farthest corner beyond r - half-thickness.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 center = Projectile.Center;
            float radius = Radius;
            Vector2 nearest = Vector2.Clamp(center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
            float nearestDistance = Vector2.Distance(center, nearest);

            float farX = Math.Max(Math.Abs(targetHitbox.Left - center.X), Math.Abs(targetHitbox.Right - center.X));
            float farY = Math.Max(Math.Abs(targetHitbox.Top - center.Y), Math.Abs(targetHitbox.Bottom - center.Y));
            float farthestDistance = new Vector2(farX, farY).Length();

            return nearestDistance <= radius + HalfThickness && farthestDistance >= radius - HalfThickness;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            int awayDirection = 1;
            if (target.Center.X < Projectile.Center.X)
            {
                awayDirection = -1;
            }

            target.velocity = new Vector2(awayDirection * 7f, -3f);
            AcolyteMadness.Apply(target, AcolyteMadness.Shriek);
        }

        // MadnessRing: the quad's half-size is MaxRadius / 0.8, so the ring's p-space radius stays <= 0.8 as the technique
        // requires; the 11px band half-thickness is 11 / 200 = 0.055 in p-space. Fades over the 8 harmless ticks.
        public override bool PreDraw(ref Color lightColor)
        {
            const float QuadHalfSize = MaxRadius / 0.8f;
            float ringProgress = 0.8f * Radius / MaxRadius;
            float fade = 1f;
            if (Age > ExpandTicks)
            {
                fade = 1f - (Age - ExpandTicks) / (float)FadeTicks;
            }

            Vector4 extra = new Vector4(QuadHalfSize / HalfThickness, 0f, 0f, 0f);
            AcolyteMadnessDraw.Quad("MadnessRing", Projectile.Center, new Vector2(QuadHalfSize * 2f), 0f, AcolyteMadnessDraw.Madness,
                fade, ringProgress, extra, AcolyteMadnessDraw.Flicker(Projectile.identity));
            return false;
        }
    }

    /// <summary>
    /// Abyss Drop's telegraph: a dark-red portal ring that opens over the player at the START of the acolyte's 40-tick tell
    /// (anchored where it opened; it does not follow). Dust spirals in, a Dark Orb forms inside it over the last 20 ticks,
    /// and on tick 40 the server drops the real orb (OolacileDarkOrb drop mode) from rest. Harmless itself. If the acolyte is
    /// staggered out of the cast first, the portal collapses without dropping anything.
    /// ai[0] = acolyte whoAmI, ai[1] = target player.
    /// </summary>
    public class AbyssDropPortal : ModProjectile
    {
        public const int OpenTicks = 40;
        private const int CloseTicks = 12;
        private const int OrbFormTicks = 20;
        private const int OrbDamage = 40;
        public const float RingRadius = 34f;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private int Age
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        private bool _dropped;
        private bool _collapsed;

        public override void SetDefaults()
        {
            Projectile.width = 68;
            Projectile.height = 68;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = OpenTicks + CloseTicks;
        }

        public override bool? CanDamage() => false;

        // Open (0..1) as it opens, then closes again after the drop or a collapse.
        public float Openness
        {
            get
            {
                float opening = Math.Min(1f, Age / 10f);
                return opening * Math.Min(1f, Projectile.timeLeft / (float)CloseTicks);
            }
        }

        public float OrbFormProgress
        {
            get
            {
                if (_dropped || _collapsed)
                {
                    return 0f;
                }

                return MathHelper.Clamp((Age - (OpenTicks - OrbFormTicks)) / (float)OrbFormTicks, 0f, 1f);
            }
        }

        public override void AI()
        {
            Age++;
            Projectile.velocity = Vector2.Zero;

            NPC owner = Main.npc[(int)Projectile.ai[0]];
            bool ownerCasting = owner.active && owner.ModNPC is NPCs.Enemies.SuperHardMode.OolacileAcolyte acolyte && acolyte.IsCastingAbyssDrop;

            if (Age < OpenTicks && !ownerCasting && !_collapsed)
            {
                Collapse();
            }

            if (Age == OpenTicks && !_collapsed)
            {
                _dropped = true;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<OolacileDarkOrb>(),
                        OrbDamage, 0f, Main.myPlayer, Projectile.ai[1], 2f);
                }

                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.7f, Pitch = -0.4f }, Projectile.Center);
                }
            }

            if (Main.dedServ || _collapsed || _dropped)
            {
                return;
            }

            // Dust spirals inward to the centre, tightening as the tell runs out.
            float tellProgress = Age / (float)OpenTicks;
            for (int i = 0; i < 2; i++)
            {
                float angle = Age * 0.25f + i * MathHelper.Pi;
                Vector2 rim = angle.ToRotationVector2() * RingRadius * Openness;
                Vector2 inward = (-rim).SafeNormalize(Vector2.UnitY).RotatedBy(0.8f) * MathHelper.Lerp(0.8f, 2.4f, tellProgress);
                int dustType = DustID.CrimsonSpray;
                if (i == 1)
                {
                    dustType = DustID.Shadowflame;
                }

                Dust spiral = Dust.NewDustPerfect(Projectile.Center + rim, dustType, inward, 60, default, 1.2f);
                spiral.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, 0.6f * Openness, 0.08f, 0.12f);
        }

        // The acolyte was interrupted: snap shut early with a puff instead of dropping the orb.
        private void Collapse()
        {
            _collapsed = true;
            Projectile.timeLeft = Math.Min(Projectile.timeLeft, CloseTicks);

            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 20; i++)
            {
                Dust puff = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(RingRadius, RingRadius), DustID.CrimsonSpray,
                    Main.rand.NextVector2Circular(2f, 2f), 60, default, 1.2f);
                puff.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // The portal: MadnessRing in the Abyss palette with a dark interior. Quad half-size = radius / 0.75; the 4.5px band
            // half-thickness is 0.1 of that (the technique's maximum).
            const float QuadHalfSize = RingRadius / 0.75f;
            float ringProgress = 0.75f * Openness;
            Vector4 extra = new Vector4(10f, 0.9f, 0f, 0f);
            AcolyteMadnessDraw.Quad("MadnessRing", Projectile.Center, new Vector2(QuadHalfSize * 2f), Age * 0.02f, AcolyteMadnessDraw.Abyss,
                Openness, ringProgress, extra);

            // The orb forming inside the ring: the real orb's sprite, growing to its full 2x scale over the last 20 ticks.
            float orbProgress = OrbFormProgress;
            if (orbProgress > 0f)
            {
                Texture2D orbTexture = Terraria.GameContent.TextureAssets.Projectile[ModContent.ProjectileType<OolacileDarkOrb>()].Value;
                int frameHeight = orbTexture.Height / 4;
                int frame = (int)(Main.GameUpdateCount / 3 % 4);
                Rectangle source = new Rectangle(0, frame * frameHeight, orbTexture.Width, frameHeight);
                Vector2 origin = source.Size() * 0.5f;
                Main.EntitySpriteDraw(orbTexture, Projectile.Center - Main.screenPosition, source, Color.White * orbProgress,
                    Age * 0.5f, origin, 2f * orbProgress, SpriteEffects.None, 0);
            }

            return false;
        }
    }

    /// <summary>
    /// Player-renderer copies for two spells, both drawn through Main.PlayerRenderer.DrawPlayer like MirrorPuppetNPC.
    /// - Hallucination: a look-alike of the acolyte (its exact loadout and dyes), translucent, with a raised staff.
    /// - Mirror of Madness: a "yellow you": a copy of the target player with every dye forced to Reflective Gold Dye and
    ///   skin/hair/eyes in Madness yellow and white. Dye shader IDs are written straight into the c* fields (MirrorPuppetNPC's
    ///   approach) so no mod-player update hooks ever run on the synthetic players.
    /// DrawPlayer's `shadow` argument is the afterimage translucency (0 = solid, 1 = gone).
    /// </summary>
    public static class AcolyteCloneRender
    {
        private static Player _lookalike;

        private static Player Lookalike
        {
            get
            {
                if (_lookalike != null)
                {
                    return _lookalike;
                }

                Player lookalike = new Player();
                lookalike.active = true;
                lookalike.whoAmI = 0;
                lookalike.gravDir = 1f;
                lookalike.skinColor = new Color(38, 22, 26);

                int[] armorTypes = { ItemID.EaterMask, ItemID.SpectreRobe, ItemID.SpectrePants };
                int[] dyeTypes = { ItemID.BurningHadesDye, ItemID.BrownAndBlackDye, ItemID.BrownAndBlackDye };
                for (int slot = 0; slot < 3; slot++)
                {
                    lookalike.armor[slot] = new Item();
                    lookalike.armor[slot].SetDefaults(armorTypes[slot]);
                    lookalike.dye[slot] = new Item();
                    lookalike.dye[slot].SetDefaults(dyeTypes[slot]);
                }

                _lookalike = lookalike;
                return lookalike;
            }
        }

        // The acolyte's look standing at `bottomCenter`, front arm at `armRotation` (vanilla composite-arm angle, facing
        // right), holding the staff sprite along that arm.
        public static void DrawLookalike(Vector2 bottomCenter, int direction, float armRotation, float opacity)
        {
            if (Main.dedServ || opacity <= 0f)
            {
                return;
            }

            Player lookalike = Lookalike;
            lookalike.direction = direction;
            lookalike.velocity = Vector2.Zero;
            lookalike.position = bottomCenter - new Vector2(lookalike.width * 0.5f, lookalike.height);
            lookalike.PlayerFrame();
            lookalike.cHead = lookalike.dye[0].dye;
            lookalike.cBody = lookalike.dye[1].dye;
            lookalike.cLegs = lookalike.dye[2].dye;

            float facedArm = armRotation * direction;
            lookalike.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, facedArm);

            Main.PlayerRenderer.DrawPlayer(Main.Camera, lookalike, lookalike.position, 0f, Vector2.Zero, 1f - opacity, 1f);

            // The staff (the real acolyte's sprite) gripped 15% up at 0.65 scale, pointing along the arm. A composite arm at
            // angle a points along (a + pi/2) in world space.
            Texture2D staff = Terraria.GameContent.TextureAssets.Item[ModContent.ItemType<Items.Weapons.Enemy.EnemyOolacileAcolyteStaff>()].Value;
            Vector2 hand = lookalike.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, facedArm);
            float staffAngle = facedArm + MathHelper.PiOver2;

            // The sprite's shaft runs butt (0,63) -> gem (55,0), i.e. at -45 degrees; mirrored when facing left.
            SpriteEffects flip = SpriteEffects.None;
            float spriteRotation = staffAngle + MathHelper.PiOver4;
            Vector2 grip = staff.Size() * new Vector2(0.14f, 0.84f);
            if (direction == -1)
            {
                flip = SpriteEffects.FlipHorizontally;
                spriteRotation = staffAngle - MathHelper.PiOver4 - MathHelper.Pi;
                grip.X = staff.Width - grip.X;
            }

            Color staffColor = Lighting.GetColor(hand.ToTileCoordinates()) * opacity;
            Main.EntitySpriteDraw(staff, hand - Main.screenPosition, null, staffColor, spriteRotation, grip, 0.65f, flip, 0);
        }

        // A gold-dyed copy of `target`. Built once per echo and kept by the caller (its look is frozen at cast time).
        public static Player BuildEcho(Player target)
        {
            Player echo = new Player();
            echo.active = true;
            echo.whoAmI = 0;
            echo.gravDir = 1f;
            echo.Male = target.Male;
            echo.skinVariant = target.skinVariant;
            echo.hair = target.hair;
            echo.wearsRobe = target.wearsRobe;

            // Madness yellow and white in place of the player's own colours.
            echo.skinColor = new Color(255, 236, 140);
            echo.hairColor = Color.White;
            echo.eyeColor = AcolyteMadness.MidColor;
            echo.shirtColor = AcolyteMadness.MidColor;
            echo.underShirtColor = Color.White;
            echo.pantsColor = AcolyteMadness.MidColor;
            echo.shoeColor = Color.White;

            for (int slot = 0; slot < echo.armor.Length && slot < target.armor.Length; slot++)
            {
                echo.armor[slot] = target.armor[slot].Clone();
            }

            for (int slot = 0; slot < echo.hideVisibleAccessory.Length && slot < target.hideVisibleAccessory.Length; slot++)
            {
                echo.hideVisibleAccessory[slot] = target.hideVisibleAccessory[slot];
            }

            ApplyGoldDye(echo);
            return echo;
        }

        // Every armor / vanity / accessory dye slot resolves to Reflective Gold Dye. Re-applied after each PlayerFrame.
        private static void ApplyGoldDye(Player echo)
        {
            Item goldDye = new Item();
            goldDye.SetDefaults(ItemID.ReflectiveGoldDye);
            int gold = goldDye.dye;

            echo.cHead = gold;
            echo.cBody = gold;
            echo.cLegs = gold;
            echo.cHandOn = gold;
            echo.cHandOff = gold;
            echo.cBack = gold;
            echo.cFront = gold;
            echo.cShoe = gold;
            echo.cWaist = gold;
            echo.cShield = gold;
            echo.cNeck = gold;
            echo.cFace = gold;
            echo.cFaceHead = gold;
            echo.cFaceFlower = gold;
            echo.cBalloon = gold;
            echo.cBalloonFront = gold;
            echo.cBackpack = gold;
            echo.cTail = gold;
            echo.cBeard = gold;
            echo.cWings = gold;
        }

        // `topLeft` is a player-hitbox position (20x42); velocity picks the walk/jump frame.
        public static void DrawEcho(Player echo, Vector2 topLeft, Vector2 velocity, int direction, float opacity)
        {
            if (Main.dedServ || echo == null || opacity <= 0f)
            {
                return;
            }

            echo.position = topLeft;
            echo.velocity = velocity;
            echo.direction = direction;
            echo.PlayerFrame();
            ApplyGoldDye(echo);
            Main.PlayerRenderer.DrawPlayer(Main.Camera, echo, topLeft, 0f, Vector2.Zero, 1f - opacity, 1f);
        }
    }

    /// <summary>
    /// The small yellow-white Madness mote (MadnessMote shader): a white-hot core, a yellow halo and a jittering 4-point flare.
    /// ai[0] = Mode, ai[1] = lifetime override in ticks (Clock shots pass the time to cover 600px past their eye).
    /// The sprite's jitter is VISUAL ONLY: the 14px hitbox follows the straight core path (plan risk #2).
    /// Dies on tiles (6-spark burst) and on players; times out with a 15-tick shrink/fade.
    /// </summary>
    public class MadnessMote : ModProjectile
    {
        public enum Mode : byte
        {
            Spiral,
            Volley,
            Hallucination,
            Clock,
        }

        private const int FadeTicks = 15;
        private const float BaseDrawSize = 36f;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private Mode MoteMode => (Mode)(int)Projectile.ai[0];

        private int Lifetime
        {
            get
            {
                if (Projectile.ai[1] > 0f)
                {
                    return (int)Projectile.ai[1];
                }

                if (MoteMode == Mode.Spiral)
                {
                    return 150;
                }

                return 120;
            }
        }

        private float DrawScale
        {
            get
            {
                if (MoteMode == Mode.Volley)
                {
                    return 1.2f;
                }

                return 1f;
            }
        }

        private int MadnessPerHit
        {
            get
            {
                if (MoteMode == Mode.Volley)
                {
                    return AcolyteMadness.FrenziedVolley;
                }

                if (MoteMode == Mode.Hallucination)
                {
                    return AcolyteMadness.Hallucination;
                }

                if (MoteMode == Mode.Clock)
                {
                    return AcolyteMadness.Clock;
                }

                return AcolyteMadness.Spiral;
            }
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage()
        {
            if (Projectile.timeLeft < FadeTicks)
            {
                return false;
            }

            return null;
        }

        public override void AI()
        {
            // Every peer sets the mode's lifetime on its first tick (OnSpawn would only run on the spawning machine).
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.timeLeft = Lifetime;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();

            if (Main.dedServ)
            {
                return;
            }

            if (Main.rand.NextBool(4))
            {
                Dust trail = Dust.NewDustPerfect(Projectile.Center, DustID.YellowTorch, -Projectile.velocity * 0.15f, 0, default, 0.9f);
                trail.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, AcolyteMadness.MidColor.ToVector3() * 0.35f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            AcolyteMadness.Apply(target, MadnessPerHit);
        }

        public override void OnKill(int timeLeft)
        {
            int sparks = 6;
            if (timeLeft <= 0)
            {
                sparks = 3;
            }

            AcolyteMadness.SparkBurst(Projectile.Center, sparks, 2.5f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float fade = Math.Min(1f, Projectile.timeLeft / (float)FadeTicks);

            // Frenzied Volley's sprite jitters +-6px around its core path; the other modes only shiver 2px.
            float jitterPixels = 2f;
            if (MoteMode == Mode.Volley)
            {
                jitterPixels = 6f;
            }

            Vector2 drawCenter = Projectile.Center + AcolyteMadnessDraw.FrenzyJitter(Projectile.identity, jitterPixels);
            float size = BaseDrawSize * DrawScale * (0.6f + 0.4f * fade);
            AcolyteMadnessDraw.Quad("MadnessMote", drawCenter, new Vector2(size), 0f, AcolyteMadnessDraw.Madness, fade, 0f,
                AcolyteMadnessDraw.FlareArms(Projectile.identity), AcolyteMadnessDraw.Flicker(Projectile.identity));
            return false;
        }
    }

    /// <summary>
    /// Gaze of Madness: a floating eye that opens near the player, stares, and fires a beam.
    /// ai[0] = target player, ai[1] = open delay (eyes open in turn). Anchored where it spawned.
    ///   open   20t  the lid widens from a slit (harmless)
    ///   track  40t  the pupil follows the player; a thin flickering yellow aim line
    ///   lock   14t  aim frozen (stored in velocity at 1/1000 magnitude, which syncs), the line turns solid white,
    ///               the pupil contracts and the eye trembles. THIS is the cue to move.
    ///   fire   12t  a white-yellow beam, 8px hitbox, stopping at the first solid tile (max 600px)
    ///   close  15t  the eye squints shut and shrivels, then 20 yellow sparks
    /// </summary>
    public class MadnessEye : ModProjectile
    {
        private const int OpenTicks = 20;
        private const int TrackTicks = 40;
        private const int LockTicks = 14;
        private const int FireTicks = 12;
        private const int CloseTicks = 15;
        private const int LockStart = OpenTicks + TrackTicks;
        private const int FireStart = LockStart + LockTicks;
        private const int CloseStart = FireStart + FireTicks;
        public const int LifeTicks = CloseStart + CloseTicks;
        private const float BeamMaxLength = 600f;
        private const float BeamHitWidth = 8f;
        private const float EyeDrawSize = 56f;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private int Age
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        private int LocalTime => Age - (int)Projectile.ai[1];
        private Player Target => Main.player[(int)Projectile.ai[0]];
        private bool Firing => LocalTime >= FireStart && LocalTime < CloseStart;
        private Vector2 LockDirection => Projectile.velocity.SafeNormalize(Vector2.UnitY);

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = LifeTicks;
        }

        public override bool? CanDamage()
        {
            if (!Firing)
            {
                return false;
            }

            return null;
        }

        public override void AI()
        {
            Age++;
            if (Age == 1)
            {
                Projectile.timeLeft = LifeTicks + (int)Projectile.ai[1];
            }

            int time = LocalTime;

            // Every peer freezes the aim on the same tick from the synced target position; the server's netUpdate then
            // overwrites any client's slightly different copy. Before the lock, velocity stays zero so the eye is anchored.
            if (time < LockStart)
            {
                Projectile.velocity = Vector2.Zero;
            }

            if (time == LockStart)
            {
                Vector2 lockAim = (Target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
                Projectile.velocity = lockAim * 0.001f;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.netUpdate = true;
                }

                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.8f, Pitch = 0.6f }, Projectile.Center);
                }
            }

            if (time == FireStart && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.55f, Pitch = 0.7f }, Projectile.Center);
            }

            if (time >= 0 && !Main.dedServ)
            {
                Lighting.AddLight(Projectile.Center, AcolyteMadness.MidColor.ToVector3() * 0.3f);
            }
        }

        // First solid tile along `direction`, in 4px steps, up to 600px. The drawn beam and the hitbox share this raycast.
        private float RaycastLength(Vector2 direction)
        {
            for (float distance = 0f; distance < BeamMaxLength; distance += 4f)
            {
                Vector2 point = Projectile.Center + direction * distance;
                Point tile = point.ToTileCoordinates();
                if (WorldGen.SolidTile(tile.X, tile.Y))
                {
                    return distance;
                }
            }

            return BeamMaxLength;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 direction = LockDirection;
            Vector2 end = Projectile.Center + direction * RaycastLength(direction);
            float discard = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, end, BeamHitWidth, ref discard);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            AcolyteMadness.Apply(target, AcolyteMadness.Gaze);
        }

        public override void OnKill(int timeLeft)
        {
            AcolyteMadness.SparkBurst(Projectile.Center, 20, 3f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int time = LocalTime;
            if (time < 0)
            {
                return false;
            }

            float lid = 1f;
            float shrivel = 1f;
            if (time < OpenTicks)
            {
                lid = time / (float)OpenTicks;
            }
            else if (time >= CloseStart)
            {
                float closing = (time - CloseStart) / (float)CloseTicks;
                lid = 1f - closing;
                shrivel = 1f - closing * 0.5f;
            }

            bool locked = time >= LockStart;
            Vector2 look = (Target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
            if (locked)
            {
                look = LockDirection;
            }

            // Aim line: thin flickering yellow while tracking, solid white once locked (until the beam fires).
            bool tracking = time >= OpenTicks && time < LockStart;
            bool lockedLine = time >= LockStart && time < FireStart;
            if (tracking || lockedLine)
            {
                float lineLength = RaycastLength(look);
                Vector2 lineCenter = Projectile.Center + look * lineLength * 0.5f;
                float lineHeight = 4f;
                float lineOpacity = 0.25f + AcolyteMadnessDraw.FrenzyRandom(Projectile.identity, 7) * 0.2f;
                AcolyteMadnessDraw.Palette linePalette = AcolyteMadnessDraw.Madness;
                if (lockedLine)
                {
                    lineHeight = 3f;
                    lineOpacity = 0.9f;
                    linePalette = new AcolyteMadnessDraw.Palette(Color.White, Color.White, Color.White);
                }

                Vector4 lineExtra = new Vector4(lineLength / lineHeight, lineLength / lineHeight * 3f, 0f, 0f);
                AcolyteMadnessDraw.Quad("MadnessBeam", lineCenter, new Vector2(lineLength, lineHeight), look.ToRotation(), linePalette,
                    lineOpacity, 0f, lineExtra);
            }

            if (Firing)
            {
                const float BeamDrawHeight = 18f;
                float beamLength = RaycastLength(look);
                Vector2 beamCenter = Projectile.Center + look * beamLength * 0.5f;
                Vector4 beamExtra = new Vector4(beamLength / BeamDrawHeight, beamLength / BeamDrawHeight * 3f, 0f, 0f);
                AcolyteMadnessDraw.Quad("MadnessBeam", beamCenter, new Vector2(beamLength, BeamDrawHeight), look.ToRotation(), AcolyteMadnessDraw.Madness,
                    1f, 0f, beamExtra, AcolyteMadnessDraw.Flicker(Projectile.identity) + 0.2f);
            }

            // The eye itself: it trembles 2px once locked; the pupil contracts from 0.14 to 0.07.
            Vector2 eyeCenter = Projectile.Center;
            float pupil = 0.14f;
            float flash = 0f;
            if (locked)
            {
                eyeCenter += AcolyteMadnessDraw.FrenzyJitter(Projectile.identity, 2f);
                pupil = 0.07f;
                flash = 1f;
            }

            Vector4 eyeExtra = new Vector4(look.X, look.Y, pupil, 0.8f);
            AcolyteMadnessDraw.Quad("MadnessEye", eyeCenter, new Vector2(EyeDrawSize * shrivel), 0f, AcolyteMadnessDraw.Madness, 1f, lid, eyeExtra, flash);
            return false;
        }
    }

    /// <summary>
    /// Mad Scrawl: the acolyte writes a jagged line through the air with its staff tip, and the writing detonates.
    /// ai[0] = path seed, ai[1] = path length in px + aim angle packed as a fraction (PackLengthAndAngle), ai[2] = start
    /// delay. The projectile never moves (it sits at the path start); every peer rebuilds the same zig-zag (+-12..24px every
    /// 20px) from the seed, so the path is identical everywhere.
    ///   draw   the pen-head runs out at 14 px/t; harmless
    ///   hold   30t, brightening from yellow to a flickering white
    ///   ripple a bright band runs start -> end over 15t; each 20px segment is live for 8t as it passes, then crumbles
    /// </summary>
    public class MadnessScrawl : ModProjectile
    {
        public const float PenSpeed = 14f;
        private const float KnotSpacing = 20f;
        private const float MinSwing = 12f;
        private const float MaxSwing = 24f;
        private const int HoldTicks = 30;
        private const int RippleTicks = 15;
        private const int SegmentLiveTicks = 8;
        private const float StripHalfWidth = 10f;
        private const float HitWidth = 12f;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private Vector2[] _knots;

        private int Age
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        private float PathLength => (float)Math.Floor(Projectile.ai[1]);
        private float PathAngle => (Projectile.ai[1] - PathLength) / 0.999f * MathHelper.TwoPi - MathHelper.Pi;
        private int DrawTicks => (int)Math.Ceiling(PathLength / PenSpeed);
        private int LocalTime => Age - (int)Projectile.ai[2];
        private int RippleTime => LocalTime - DrawTicks - HoldTicks;
        private float RippleFront => RippleTime / (float)RippleTicks;
        private float LiveWindow => SegmentLiveTicks / (float)RippleTicks;

        public static float PackLengthAndAngle(float length, float angle)
        {
            float wrapped = MathHelper.WrapAngle(angle);
            return (float)Math.Floor(length) + (wrapped + MathHelper.Pi) / MathHelper.TwoPi * 0.999f;
        }

        public static int LifetimeFor(float length, int delay)
        {
            int drawTicks = (int)Math.Ceiling(length / PenSpeed);
            return delay + drawTicks + HoldTicks + RippleTicks + SegmentLiveTicks + 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
        }

        // Knot k sits k*20px along the aim line, swung sideways by an alternating 12-24px; both ends sit on the line.
        private Vector2[] Knots
        {
            get
            {
                if (_knots != null)
                {
                    return _knots;
                }

                Terraria.Utilities.UnifiedRandom random = new Terraria.Utilities.UnifiedRandom((int)Projectile.ai[0]);
                Vector2 direction = PathAngle.ToRotationVector2();
                Vector2 side = direction.RotatedBy(MathHelper.PiOver2);
                int knotCount = Math.Max(2, (int)Math.Ceiling(PathLength / KnotSpacing) + 1);
                _knots = new Vector2[knotCount];

                for (int knot = 0; knot < knotCount; knot++)
                {
                    float along = Math.Min(knot * KnotSpacing, PathLength);
                    float swing = random.NextFloat(MinSwing, MaxSwing);
                    if (knot % 2 == 0)
                    {
                        swing = -swing;
                    }

                    bool endKnot = knot == 0 || knot == knotCount - 1;
                    if (endKnot)
                    {
                        swing = 0f;
                    }

                    _knots[knot] = Projectile.Center + direction * along + side * swing;
                }

                return _knots;
            }
        }

        public override void AI()
        {
            Age++;
            if (Age == 1)
            {
                Projectile.timeLeft = LifetimeFor(PathLength, (int)Projectile.ai[2]);
            }

            Projectile.velocity = Vector2.Zero;

            if (Main.dedServ || RippleTime < 0)
            {
                return;
            }

            if (RippleTime == 0)
            {
                SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.5f, Pitch = 0.8f }, Knots[0]);
            }

            // Segments crumble into yellow sparks just behind the ripple.
            float crumbleFront = RippleFront - LiveWindow;
            if (crumbleFront > 0f && crumbleFront < 1f)
            {
                Vector2[] knots = Knots;
                float knotIndex = crumbleFront * (knots.Length - 1);
                int lower = (int)knotIndex;
                int upper = Math.Min(knots.Length - 1, lower + 1);
                Vector2 point = Vector2.Lerp(knots[lower], knots[upper], knotIndex - lower);
                AcolyteMadness.SparkBurst(point, 2, 2f);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (RippleTime < 0)
            {
                return false;
            }

            Vector2[] knots = Knots;
            int segments = knots.Length - 1;
            float front = RippleFront;
            float discard = 0f;

            for (int segment = 0; segment < segments; segment++)
            {
                float fraction = (segment + 0.5f) / segments;
                bool live = fraction <= front && fraction >= front - LiveWindow;
                if (!live)
                {
                    continue;
                }

                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), knots[segment], knots[segment + 1], HitWidth, ref discard))
                {
                    return true;
                }
            }

            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            AcolyteMadness.Apply(target, AcolyteMadness.Scrawl);
        }

        public override void OnKill(int timeLeft)
        {
            Vector2[] knots = Knots;
            AcolyteMadness.SparkBurst(knots[knots.Length - 1], 8, 2.5f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int time = LocalTime;
            if (time < 1)
            {
                return false;
            }

            Vector2[] knots = Knots;
            float drawnLength = Math.Min(PathLength, time * PenSpeed);
            int fullKnots = Math.Min(knots.Length, (int)(drawnLength / KnotSpacing) + 1);

            // The written part of the path, plus an interpolated point under the pen-head.
            Vector2[] points = knots;
            Vector2 penHead = knots[knots.Length - 1];
            bool drawing = drawnLength < PathLength;
            if (drawing)
            {
                points = new Vector2[fullKnots + 1];
                Array.Copy(knots, points, fullKnots);
                int nextKnot = Math.Min(knots.Length - 1, fullKnots);
                float partial = (drawnLength - (fullKnots - 1) * KnotSpacing) / KnotSpacing;
                penHead = Vector2.Lerp(knots[fullKnots - 1], knots[nextKnot], MathHelper.Clamp(partial, 0f, 1f));
                points[fullKnots] = penHead;
            }

            float ripple = -1f;
            float crumble = -1f;
            float armed = 0f;
            int holdTime = time - DrawTicks;
            if (holdTime >= 0)
            {
                armed = Math.Min(1f, holdTime / (float)HoldTicks) * AcolyteMadnessDraw.Flicker(Projectile.identity);
            }

            if (RippleTime >= 0)
            {
                ripple = RippleFront;
                crumble = RippleFront - LiveWindow;
            }

            // Extra: crumble front, armed brightness, length / width (noise scale), 1 / ripple half-width (6% of the path).
            Vector4 extra = new Vector4(crumble, armed, Math.Max(1f, drawnLength / (StripHalfWidth * 2f)), 1f / 0.06f);
            AcolyteMadnessDraw.Strip("MadnessScrawl", points, StripHalfWidth, AcolyteMadnessDraw.Madness, 1f, ripple, extra);

            if (drawing)
            {
                AcolyteMadnessDraw.Quad("MadnessMote", penHead, new Vector2(30f), 0f, AcolyteMadnessDraw.Madness, 1f, 0f,
                    AcolyteMadnessDraw.FlareArms(Projectile.identity), 1.1f);
            }

            return false;
        }
    }

    /// <summary>
    /// Hallucination: a translucent yellow-white afterimage of the acolyte left at every chained-blink departure spot below
    /// 50%. After 20 ticks it raises its staff and throws a 3-mote fan (+-12 degrees) at the player, then shatters over 15
    /// ticks into sparks. A projectile on purpose: it can't be hit and doesn't count as an enemy. Harmless itself.
    /// ai[0] = target player, ai[1] = facing (+1 / -1). At most 3 alive (OolacileAcolyte.SpawnHallucination).
    /// </summary>
    public class AcolyteHallucination : ModProjectile
    {
        private const int IdleTicks = 20;
        private const int RaiseTicks = 10;
        private const int ShatterTicks = 15;
        private const int ThrowTick = IdleTicks + RaiseTicks;
        public const int LifeTicks = ThrowTick + ShatterTicks;
        private const float FanSpreadDegrees = 12f;
        private const float MoteSpeed = 7f;
        private const int MoteDamage = 26;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private int Age
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 44;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = LifeTicks;
        }

        public override bool? CanDamage() => false;

        private int Facing
        {
            get
            {
                if (Projectile.ai[1] < 0f)
                {
                    return -1;
                }

                return 1;
            }
        }

        // The staff tip the fan leaves from: overhead and slightly forward once raised.
        private Vector2 ThrowOrigin => Projectile.Center + new Vector2(Facing * 12f, -40f);

        public override void AI()
        {
            Age++;
            Projectile.velocity = Vector2.Zero;

            if (Age == ThrowTick)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Player target = Main.player[(int)Projectile.ai[0]];
                    Vector2 aim = (target.Center - ThrowOrigin).SafeNormalize(Vector2.UnitX * Facing);
                    for (int i = -1; i <= 1; i++)
                    {
                        Vector2 velocity = aim.RotatedBy(MathHelper.ToRadians(FanSpreadDegrees * i)) * MoteSpeed;
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), ThrowOrigin, velocity, ModContent.ProjectileType<MadnessMote>(),
                            MoteDamage, 0f, Main.myPlayer, (float)MadnessMote.Mode.Hallucination);
                    }
                }

                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.4f, Pitch = 0.8f }, Projectile.Center);
                }
            }

            if (Age > ThrowTick && !Main.dedServ && Main.rand.NextBool(2))
            {
                Vector2 shard = Projectile.position + new Vector2(Main.rand.NextFloat(Projectile.width), Main.rand.NextFloat(Projectile.height));
                AcolyteMadness.SparkBurst(shard, 1, 2f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 45% opacity, fading in over 6 ticks and out across the shatter.
            float opacity = 0.45f * Math.Min(1f, Age / 6f);
            if (Age > ThrowTick)
            {
                opacity *= 1f - (Age - ThrowTick) / (float)ShatterTicks;
            }

            // Vanilla composite angle (facing right): 0 = hanging straight down, -pi = straight up. Hangs, then raises.
            float raise = MathHelper.Clamp((Age - IdleTicks) / (float)RaiseTicks, 0f, 1f);
            float armRotation = MathHelper.Lerp(-0.6f, -MathHelper.Pi + 0.25f, MathHelper.SmoothStep(0f, 1f, raise));

            AcolyteCloneRender.DrawLookalike(Projectile.Bottom, Facing, armRotation, opacity);

            // MadnessGlyph shimmer over the body.
            AcolyteMadnessDraw.Quad("MadnessGlyph", Projectile.Center, new Vector2(64f), Age * 0.05f, AcolyteMadnessDraw.Madness,
                opacity * 0.8f, 1f, Vector4.Zero, AcolyteMadnessDraw.Flicker(Projectile.identity));
            return false;
        }
    }

    /// <summary>
    /// Mark of Madness. Tell (40t): an eye glyph forms over the target's head and closes from 96 to 32px, tethered to the
    /// acolyte's staff tip by a thin yellow line. 15 consecutive ticks without line of sight (or the acolyte being
    /// interrupted) fizzle it. On tick 40 the MARKED PLAYER'S OWN CLIENT adds the 8s Marked debuff; the glyph then rides
    /// over their head as a small eye rune for as long as the debuff lasts. ai[0] = acolyte whoAmI, ai[1] = target player.
    /// </summary>
    public class MarkGlyph : ModProjectile
    {
        public const int TellTicks = 40;
        private const int FizzleBlockedTicks = 15;
        private const int FadeTicks = 10;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private int Age
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        private int _blockedTicks;
        private int _fadeTicks = -1;

        private Player Target => Main.player[(int)Projectile.ai[1]];

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TellTicks + MadnessMarked.DurationTicks + 60;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Age++;
            Player target = Target;
            Projectile.velocity = Vector2.Zero;
            Projectile.Center = target.Top - new Vector2(0f, 26f + MathF.Sin(Age * 0.08f) * 3f);

            if (_fadeTicks >= 0)
            {
                _fadeTicks++;
                if (_fadeTicks >= FadeTicks)
                {
                    Projectile.Kill();
                }

                return;
            }

            if (!target.active || target.dead)
            {
                BeginFade(true);
                return;
            }

            if (Age < TellTicks)
            {
                NPC owner = Main.npc[(int)Projectile.ai[0]];
                bool ownerCasting = owner.active && owner.ModNPC is NPCs.Enemies.SuperHardMode.OolacileAcolyte acolyte && acolyte.IsCastingMark;
                bool clear = owner.active && Collision.CanHitLine(owner.Center, 1, 1, target.Center, 1, 1);

                if (clear)
                {
                    _blockedTicks = 0;
                }
                else
                {
                    _blockedTicks++;
                }

                if (!ownerCasting || _blockedTicks >= FizzleBlockedTicks)
                {
                    BeginFade(true);
                }

                return;
            }

            if (Age == TellTicks)
            {
                // Only the marked player applies their own buff; buffs then sync to everyone else.
                if (target.whoAmI == Main.myPlayer)
                {
                    target.AddBuff(ModContent.BuffType<MadnessMarked>(), MadnessMarked.DurationTicks);
                }

                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.7f, Pitch = 0.4f }, target.Center);
                    AcolyteMadness.SparkBurst(Projectile.Center, 16, 3f);
                }

                return;
            }

            // A grace window after the tell so the synced buff has time to reach other clients before they test for it.
            bool marked = target.HasBuff(ModContent.BuffType<MadnessMarked>());
            if (!marked && Age > TellTicks + 30)
            {
                BeginFade(false);
            }
        }

        private void BeginFade(bool fizzled)
        {
            _fadeTicks = 0;
            if (fizzled && !Main.dedServ)
            {
                AcolyteMadness.SparkBurst(Projectile.Center, 12, 2f);
                SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.6f, Pitch = -0.4f }, Projectile.Center);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float fade = 1f;
            if (_fadeTicks >= 0)
            {
                fade = 1f - _fadeTicks / (float)FadeTicks;
            }

            bool telling = Age < TellTicks;
            float size = 28f;
            float reveal = 1f;
            if (telling)
            {
                float tellProgress = Age / (float)TellTicks;
                size = MathHelper.Lerp(96f, 32f, tellProgress);
                reveal = Math.Min(1f, Age / 10f);

                // Tether from the acolyte's staff tip to the glyph.
                NPC owner = Main.npc[(int)Projectile.ai[0]];
                bool tethered = owner.active && _fadeTicks < 0 && owner.ModNPC is NPCs.Enemies.SuperHardMode.OolacileAcolyte;
                if (tethered)
                {
                    NPCs.Enemies.SuperHardMode.OolacileAcolyte acolyte = (NPCs.Enemies.SuperHardMode.OolacileAcolyte)owner.ModNPC;
                    Vector2 from = acolyte.StaffTipForVisuals;
                    Vector2 toGlyph = Projectile.Center - from;
                    float tetherLength = toGlyph.Length();
                    const float TetherHeight = 3f;
                    Vector4 tetherExtra = new Vector4(tetherLength / TetherHeight, tetherLength / TetherHeight * 3f, 0f, 0f);
                    AcolyteMadnessDraw.Quad("MadnessBeam", from + toGlyph * 0.5f, new Vector2(tetherLength, TetherHeight), toGlyph.ToRotation(),
                        AcolyteMadnessDraw.Madness, 0.35f + 0.3f * tellProgress, 0f, tetherExtra, AcolyteMadnessDraw.Flicker(Projectile.identity));
                }
            }

            AcolyteMadnessDraw.Quad("MadnessGlyphEye", Projectile.Center, new Vector2(size), 0f, AcolyteMadnessDraw.Madness, fade, reveal,
                Vector4.Zero, AcolyteMadnessDraw.Flicker(Projectile.identity));
            return false;
        }
    }

    /// <summary>
    /// Mirror of Madness: a gold-dyed "yellow you" that replays the target's own movement `delay` ticks behind them.
    /// ai[0] = target player, ai[1] = delay (60 or 120). Every peer keeps its own 150-entry ring buffer of the target's
    /// Center from spawn; the echo stands at history[now - delay]. Player positions are synced and damage resolves on the
    /// victim's client against its own buffer, so the echo is exactly on the victim's real path where it matters.
    ///   hidden  `delay` ticks (the buffer filling); then it fades in over 30t where the player stood at cast time
    ///   replay  300t total, hitbox = the player's own 20x42; it hurts any player who walks into it
    ///   death   after 300t, or when the replay reaches a jump over 300px (Recall, Rod of Discord): it shatters into
    ///           30 yellow sparks over 15t and never jumps across the teleport
    /// </summary>
    public class MirrorEcho : ModProjectile
    {
        private const int HistoryLength = 150;
        private const int FadeInTicks = 30;
        private const int ReplayTicks = 300;
        private const int ShatterTicks = 15;
        private const float TeleportGap = 300f;
        private const float EchoOpacity = 0.6f;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private readonly Vector2[] _history = new Vector2[HistoryLength];
        private int _recorded;
        private int _shatterTicks = -1;
        private Player _echoLook;

        private int Age
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        private int Delay => (int)Projectile.ai[1];
        private Player Target => Main.player[(int)Projectile.ai[0]];
        private int ReplayTime => Age - Delay;

        public static int LifetimeFor(int delay) => delay + ReplayTicks + ShatterTicks + 2;

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 42;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = LifetimeFor(120);
        }

        public override bool? CanDamage()
        {
            bool visible = ReplayTime >= FadeInTicks / 2;
            if (!visible || _shatterTicks >= 0)
            {
                return false;
            }

            return null;
        }

        // The recorded Center `ticksAgo` ticks before the newest entry.
        private Vector2 HistoryAt(int ticksAgo)
        {
            int index = (_recorded - 1 - ticksAgo) % HistoryLength;
            if (index < 0)
            {
                index += HistoryLength;
            }

            return _history[index];
        }

        public override void AI()
        {
            Age++;
            if (Age == 1)
            {
                Projectile.timeLeft = LifetimeFor(Delay);
            }

            Projectile.velocity = Vector2.Zero;

            Player target = Target;
            _history[_recorded % HistoryLength] = target.Center;
            _recorded++;

            if (_shatterTicks >= 0)
            {
                _shatterTicks++;
                if (!Main.dedServ)
                {
                    AcolyteMadness.SparkBurst(Projectile.Center, 2, 3f);
                }

                if (_shatterTicks >= ShatterTicks)
                {
                    Projectile.Kill();
                }

                return;
            }

            if (ReplayTime < 0 || _recorded <= Delay)
            {
                Projectile.Center = target.Center;
                return;
            }

            Vector2 replayed = HistoryAt(Delay);
            Vector2 previous = HistoryAt(Delay + 1);
            bool teleportGap = Vector2.Distance(replayed, previous) > TeleportGap;
            bool expired = ReplayTime >= ReplayTicks;
            bool targetGone = !target.active || target.dead;

            if (teleportGap || expired || targetGone)
            {
                Shatter();
                return;
            }

            Projectile.Center = replayed;
        }

        private void Shatter()
        {
            _shatterTicks = 0;
            if (!Main.dedServ)
            {
                AcolyteMadness.SparkBurst(Projectile.Center, 30, 4f);
                SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.5f, Pitch = 0.9f }, Projectile.Center);
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            AcolyteMadness.Apply(target, AcolyteMadness.Mirror);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (ReplayTime < 0 || _recorded <= Delay + 1)
            {
                return false;
            }

            _echoLook ??= AcolyteCloneRender.BuildEcho(Target);

            float opacity = EchoOpacity * Math.Min(1f, ReplayTime / (float)FadeInTicks);
            if (_shatterTicks >= 0)
            {
                opacity *= 1f - _shatterTicks / (float)ShatterTicks;
            }

            // Walk frames and facing come from the replayed motion itself.
            Vector2 motion = HistoryAt(Delay) - HistoryAt(Delay + 1);
            int facing = 1;
            if (motion.X < -0.1f)
            {
                facing = -1;
            }
            else if (Math.Abs(motion.X) <= 0.1f)
            {
                facing = Target.direction;
            }

            AcolyteCloneRender.DrawEcho(_echoLook, Projectile.position, motion, facing, opacity);
            Lighting.AddLight(Projectile.Center, AcolyteMadness.MidColor.ToVector3() * 0.4f * opacity);
            return false;
        }
    }

    /// <summary>
    /// One Cat's Cradle thread, strung wall to wall across a point near the player. The SERVER did the anchor raycasts and
    /// spawned this at the first anchor; no client re-derives them.
    /// ai[0] = span length (px), ai[1] = start delay, ai[2] = fray bits (1 = start frayed, 2 = end frayed) + 4 * acolyte
    /// whoAmI; velocity = the span direction at 1/100th magnitude (synced). Timeline after the delay:
    ///   leg 1  8t   a pen flies from the acolyte's staff tip to the first anchor, trailing the strand
    ///   leg 2  8t   it whips across to the second anchor (the web is visibly strung, never popped in)
    ///   slack  30t  sagging up to 24px mid-span (gravity, so always downward), yellow, trembling, harmless
    ///   taut   10t  the sag pulls straight and flickers white; a rising harp twang on the first taut tick (the cue)
    ///   slice  10t  live: the straight line, 6px thick
    ///   snap   15t  it breaks in the middle and the halves whip back to their anchors, fading in yellow sparks
    /// </summary>
    public class MadnessThread : ModProjectile
    {
        private const int LegTicks = 8;
        private const int SlackTicks = 30;
        private const int TautTicks = 10;
        private const int SliceTicks = 10;
        private const int SnapTicks = 15;
        private const int SlackStart = LegTicks * 2;
        private const int TautStart = SlackStart + SlackTicks;
        private const int SliceStart = TautStart + TautTicks;
        private const int SnapStart = SliceStart + SliceTicks;
        public const int LifeTicks = SnapStart + SnapTicks;
        public const float MaxSag = 24f;
        private const float HitWidth = 6f;
        private const float StripHalfWidth = 7f;
        private const int CurveSegments = 16;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private Vector2 _legOrigin;
        private bool _legOriginSet;

        private int Age
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        private int LocalTime => Age - (int)Projectile.ai[1];
        private float Span => Projectile.ai[0];
        private int PackedFlags => (int)Projectile.ai[2];
        private bool StartFrayed => (PackedFlags & 1) != 0;
        private bool EndFrayed => (PackedFlags & 2) != 0;
        private int OwnerIndex => PackedFlags / 4;
        private Vector2 SpanDirection => Projectile.velocity.SafeNormalize(Vector2.UnitX);
        private Vector2 AnchorA => Projectile.Center;
        private Vector2 AnchorB => Projectile.Center + SpanDirection * Span;

        public static float PackFlags(bool startFrayed, bool endFrayed, int ownerIndex)
        {
            int bits = 0;
            if (startFrayed)
            {
                bits += 1;
            }

            if (endFrayed)
            {
                bits += 2;
            }

            return bits + ownerIndex * 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = LifeTicks;
        }

        public override bool? CanDamage()
        {
            bool slicing = LocalTime >= SliceStart && LocalTime < SnapStart;
            if (!slicing)
            {
                return false;
            }

            return null;
        }

        public override void AI()
        {
            Age++;
            if (Age == 1)
            {
                Projectile.timeLeft = LifeTicks + (int)Projectile.ai[1];
            }

            // Keep the synced direction but never move: 1/100th of a unit vector, cancelled every tick.
            Projectile.position -= Projectile.velocity;

            int time = LocalTime;
            if (time == 0)
            {
                // The pen leaves from wherever the staff tip is right now (cosmetic only, so each peer uses its own view).
                _legOrigin = AnchorA;
                NPC owner = Main.npc[OwnerIndex];
                if (owner.active && owner.ModNPC is NPCs.Enemies.SuperHardMode.OolacileAcolyte acolyte)
                {
                    _legOrigin = acolyte.StaffTipForVisuals;
                }

                _legOriginSet = true;
            }

            if (Main.dedServ)
            {
                return;
            }

            if (time == TautStart)
            {
                SoundEngine.PlaySound(SoundID.Item26 with { Volume = 0.6f, Pitch = 0.6f }, Vector2.Lerp(AnchorA, AnchorB, 0.5f));
            }

            if (time == SnapStart)
            {
                AcolyteMadness.SparkBurst(Vector2.Lerp(AnchorA, AnchorB, 0.5f), 10, 3f);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float discard = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), AnchorA, AnchorB, HitWidth, ref discard);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            AcolyteMadness.Apply(target, AcolyteMadness.Cradle);
        }

        // The thread from `from` to `to`, drooping by `sag` px at mid-span in WORLD down (+y): a parabola 4t(1-t).
        private static Vector2[] SaggingCurve(Vector2 from, Vector2 to, float sag, int segments)
        {
            Vector2[] points = new Vector2[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float along = i / (float)segments;
                float droop = sag * 4f * along * (1f - along);
                points[i] = Vector2.Lerp(from, to, along) + new Vector2(0f, droop);
            }

            return points;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int time = LocalTime;
            if (time < 0 || !_legOriginSet)
            {
                return false;
            }

            Vector2 anchorA = AnchorA;
            Vector2 anchorB = AnchorB;
            AcolyteMadnessDraw.Palette palette = AcolyteMadnessDraw.Madness;
            float flicker = AcolyteMadnessDraw.Flicker(Projectile.identity);
            Vector4 extra = new Vector4(0f, 0f, Math.Max(1f, Span / (StripHalfWidth * 2f)), 0f);

            if (time < LegTicks)
            {
                // Leg 1: the pen flies from the staff tip to the first anchor.
                Vector2 pen = Vector2.Lerp(_legOrigin, anchorA, (time + 1) / (float)LegTicks);
                AcolyteMadnessDraw.Strip("MadnessThread", SaggingCurve(_legOrigin, pen, 0f, 4), StripHalfWidth, palette, 0.7f, 0f, extra, flicker);
                AcolyteMadnessDraw.Quad("MadnessMote", pen, new Vector2(24f), 0f, palette, 1f, 0f, AcolyteMadnessDraw.FlareArms(Projectile.identity), 1.1f);
                return false;
            }

            if (time < SlackStart)
            {
                // Leg 2: it whips across to the second anchor.
                Vector2 pen = Vector2.Lerp(anchorA, anchorB, (time - LegTicks + 1) / (float)LegTicks);
                AcolyteMadnessDraw.Strip("MadnessThread", SaggingCurve(anchorA, pen, 0f, CurveSegments), StripHalfWidth, palette, 1f, 0f, extra, flicker);
                AcolyteMadnessDraw.Quad("MadnessMote", pen, new Vector2(24f), 0f, palette, 1f, 0f, AcolyteMadnessDraw.FlareArms(Projectile.identity), 1.1f);
                return false;
            }

            float maxSag = Math.Min(MaxSag, Span * 0.1f);
            if (time < SliceStart)
            {
                // Slack sags; the last 10t (taut) pull it straight and whiten it.
                float taut = MathHelper.Clamp((time - TautStart) / (float)TautTicks, 0f, 1f);
                float sag = maxSag * (1f - taut);
                extra.X = StartFrayed ? 1f : 0f;
                extra.Y = EndFrayed ? 1f : 0f;
                AcolyteMadnessDraw.Strip("MadnessThread", SaggingCurve(anchorA, anchorB, sag, CurveSegments), StripHalfWidth, palette, 1f, taut, extra, flicker);
                return false;
            }

            if (time < SnapStart)
            {
                // Slicing: straight, white, at full strength.
                extra.X = StartFrayed ? 1f : 0f;
                extra.Y = EndFrayed ? 1f : 0f;
                AcolyteMadnessDraw.Strip("MadnessThread", SaggingCurve(anchorA, anchorB, 0f, CurveSegments), StripHalfWidth * 1.3f, palette, 1f, 1f, extra, 1.1f);
                return false;
            }

            // Snap: the halves whip back toward their anchors and fade.
            float snap = (time - SnapStart) / (float)SnapTicks;
            Vector2 middle = Vector2.Lerp(anchorA, anchorB, 0.5f);
            float retract = 0.5f * (1f - snap);
            Vector2 tipA = Vector2.Lerp(anchorA, anchorB, retract);
            Vector2 tipB = Vector2.Lerp(anchorB, anchorA, retract);
            float fade = 1f - snap;
            AcolyteMadnessDraw.Strip("MadnessThread", SaggingCurve(anchorA, tipA, maxSag * snap, 8), StripHalfWidth, palette, fade, 0.3f, extra, flicker);
            AcolyteMadnessDraw.Strip("MadnessThread", SaggingCurve(anchorB, tipB, maxSag * snap, 8), StripHalfWidth, palette, fade, 0.3f, extra, flicker);

            if (Main.rand.NextBool(2))
            {
                AcolyteMadness.SparkBurst(Vector2.Lerp(middle, tipA, 0.9f), 1, 1.5f);
            }

            return false;
        }
    }

    /// <summary>
    /// Madness Clock: one server-spawned controller at the ring centre (where the player stood at cast time; ANCHORED). It
    /// draws the 12 eyes and the hand on every peer from its own timer and spawns each shot server-side as the hand passes
    /// an eye. Harmless itself.
    ///   open   eyes open one per 3t clockwise from 12 o'clock (36t)
    ///   rev 1  a thin, harmless white hand sweeps clockwise, one revolution in 90t (4 degrees/t)
    ///   glitch 10t of stutter and flicker at 12 o'clock
    ///   rev 2  the hand sweeps COUNTER-clockwise
    ///   close  eyes squint shut and shrivel over 15t after their last shot
    /// Each pass fires that eye's MadnessMote straight through the centre at 8 px/t, carrying 600px past the far side
    /// (140t of life; dies on tiles). Shots are 7.5t apart, so the danger line is the hand's whole diameter.
    /// </summary>
    public class MadnessClock : ModProjectile
    {
        public const int EyeCount = 12;
        public const float RingRadius = 260f;
        private const int EyeOpenSpacing = 3;
        private const int OpenTicks = EyeCount * EyeOpenSpacing;
        private const int RevolutionTicks = 90;
        private const int GlitchTicks = 10;
        private const int CloseTicks = 15;
        private const int FirstRevolutionStart = OpenTicks;
        private const int GlitchStart = FirstRevolutionStart + RevolutionTicks;
        private const int SecondRevolutionStart = GlitchStart + GlitchTicks;
        private const int CloseStart = SecondRevolutionStart + RevolutionTicks;
        public const int LifeTicks = CloseStart + CloseTicks;
        private const float ShotSpeed = 8f;
        private const int ShotLifetime = (int)((RingRadius * 2f + 600f) / ShotSpeed);
        private const int ShotDamage = 28;
        private const float EyeDrawSize = 56f * 0.6f;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private int Age
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = LifeTicks;
        }

        public override bool? CanDamage() => false;

        // Eye k sits at 12 o'clock + 30k degrees, clockwise (screen y points down, so clockwise is +angle).
        private static float EyeAngle(int eye) => -MathHelper.PiOver2 + eye * MathHelper.TwoPi / EyeCount;

        private Vector2 EyePosition(int eye) => Projectile.Center + EyeAngle(eye).ToRotationVector2() * RingRadius;

        // Degrees swept clockwise from 12 o'clock at `time`: 0..360 on the first revolution, back down 360..0 on the second.
        private static float HandSweepDegrees(int time)
        {
            if (time < FirstRevolutionStart)
            {
                return 0f;
            }

            if (time < GlitchStart)
            {
                return (time - FirstRevolutionStart) * 360f / RevolutionTicks;
            }

            if (time < SecondRevolutionStart)
            {
                return 360f;
            }

            if (time < CloseStart)
            {
                return 360f - (time - SecondRevolutionStart) * 360f / RevolutionTicks;
            }

            return 0f;
        }

        public override void AI()
        {
            Age++;
            Projectile.velocity = Vector2.Zero;
            int time = Age;

            bool firstRevolution = time > FirstRevolutionStart && time <= GlitchStart;
            bool secondRevolution = time > SecondRevolutionStart && time <= CloseStart;

            // An eye fires on the tick the hand crosses it (eye 0 sits at both 0 and 360 degrees: it fires at the start of the
            // first revolution and at the end of the second).
            if ((firstRevolution || secondRevolution) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float before = HandSweepDegrees(time - 1);
                float after = HandSweepDegrees(time);
                float low = Math.Min(before, after);
                float high = Math.Max(before, after);

                for (int eye = 0; eye < EyeCount; eye++)
                {
                    float eyeDegrees = eye * 360f / EyeCount;

                    bool crossed = eyeDegrees > low && eyeDegrees <= high;
                    bool startTick = eye == 0 && firstRevolution && time == FirstRevolutionStart + 1;
                    bool endTick = eye == 0 && secondRevolution && time == CloseStart;
                    if (crossed || startTick || endTick)
                    {
                        FireShot(eye);
                    }
                }
            }

            if (Main.dedServ)
            {
                return;
            }

            if (time == GlitchStart || time == FirstRevolutionStart)
            {
                SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.7f, Pitch = 0.2f }, Projectile.Center);
            }

            Lighting.AddLight(Projectile.Center, AcolyteMadness.MidColor.ToVector3() * 0.2f);
        }

        private void FireShot(int eye)
        {
            Vector2 from = EyePosition(eye);
            Vector2 across = (Projectile.Center - from).SafeNormalize(Vector2.UnitY);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), from, across * ShotSpeed, ModContent.ProjectileType<MadnessMote>(),
                ShotDamage, 0f, Main.myPlayer, (float)MadnessMote.Mode.Clock, ShotLifetime);
        }

        public override void OnKill(int timeLeft)
        {
            for (int eye = 0; eye < EyeCount; eye++)
            {
                AcolyteMadness.SparkBurst(EyePosition(eye), 4, 2f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int time = Age;

            // The hand: a dim, harmless MadnessBeam from the centre to the ring. It stutters and flickers in the glitch.
            bool handVisible = time >= FirstRevolutionStart && time < CloseStart;
            if (handVisible)
            {
                float sweep = MathHelper.ToRadians(HandSweepDegrees(time));
                float handOpacity = 0.35f;
                bool glitching = time >= GlitchStart && time < SecondRevolutionStart;
                if (glitching)
                {
                    sweep += (AcolyteMadnessDraw.FrenzyRandom(Projectile.identity, 8) - 0.5f) * 0.5f;
                    handOpacity = 0.15f + AcolyteMadnessDraw.FrenzyRandom(Projectile.identity, 9) * 0.4f;
                }

                float handAngle = -MathHelper.PiOver2 + sweep;
                Vector2 handDirection = handAngle.ToRotationVector2();
                const float HandHeight = 6f;
                Vector4 handExtra = new Vector4(RingRadius / HandHeight, RingRadius / HandHeight * 3f, 0f, 0f);
                AcolyteMadnessDraw.Palette white = new AcolyteMadnessDraw.Palette(Color.White, Color.White, Color.White);
                AcolyteMadnessDraw.Quad("MadnessBeam", Projectile.Center + handDirection * RingRadius * 0.5f, new Vector2(RingRadius, HandHeight),
                    handAngle, white, handOpacity, 0f, handExtra);
            }

            // The eyes: each opens in turn, stares at the centre, and shrivels after its last shot.
            for (int eye = 0; eye < EyeCount; eye++)
            {
                int openedAt = eye * EyeOpenSpacing;
                float lid = MathHelper.Clamp((time - openedAt) / 12f, 0f, 1f);
                float shrivel = 1f;
                if (time >= CloseStart)
                {
                    float closing = (time - CloseStart) / (float)CloseTicks;
                    lid *= 1f - closing;
                    shrivel = 1f - closing * 0.5f;
                }

                if (lid <= 0f)
                {
                    continue;
                }

                Vector2 eyePosition = EyePosition(eye);
                Vector2 inward = (Projectile.Center - eyePosition).SafeNormalize(Vector2.UnitY);
                Vector4 eyeExtra = new Vector4(inward.X, inward.Y, 0.1f, 0.6f);
                AcolyteMadnessDraw.Quad("MadnessEye", eyePosition, new Vector2(EyeDrawSize * shrivel), 0f, AcolyteMadnessDraw.Madness, 1f, lid, eyeExtra);
            }

            return false;
        }
    }
}
