using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Gwyn
{
    /// <summary>
    /// Lord's Verdict: a gold spectral copy of Gwyn's sword, an exact 4x his sword sprite (458px grip to tip). It forms
    /// above him during his tell, then slams down with his own overhead: its grip swings a quarter circle around a
    /// "shoulder" behind his heels while the blade turns from cocked-back to level, landing flat on the floor in front.
    /// Live for the whole slam (swept segments, as the Illusion's Rune Blade) and for 16 ticks lying on the floor.
    ///
    /// The sword follows Gwyn's own combo clock: it holds while he telegraphs, starts the slam the tick his attack
    /// starts, and dissolves without damage if he leaves the combo first. ai[0] = Gwyn's whoAmI; ai[1] = 1 when the
    /// impact also sends a ground wave outward from the tip (Prime Gwyn Phase II+).
    /// Multiplayer: spawned on the server; every machine runs the same timeline from the synced age and release tick.
    /// Spectral: ignores tiles on purpose and lands on Gwyn's floor height.
    /// </summary>
    public class GwynLordsVerdictBlade : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(Items.Weapons.Melee.Broadswords.SwordOfLordGwyn));

        // ── Geometry (px). Sprite measured: grip at MeleeHandleNorm (0.14, 0.86) of 96px, tip at (95, 2): 114.6px apart.
        // Scale 4 keeps every sprite pixel an even 4x4 block.
        public const float DrawScale = 4f;
        private static readonly Vector2 SpriteGrip = new Vector2(13.44f, 82.56f);
        private const float Reach = 458f;              // 114.6 x 4, grip to tip
        private const float HiltLength = 24f;          // the hilt end of the blade deals no damage
        private const float BladeHitWidth = 32f;       // collision band thickness; the drawn blade is about as thick
        private const float FloorBandLift = 16f;       // blade centre line above the floor, so the band sits on the floor
        private const float ShoulderBehind = 120f;     // shoulder point behind Gwyn's heels
        private const float ArmRadius = 180f;          // grip ends 60px in front of him; blade lies 60-518px out
        private static readonly float BladeStartAngle = MathHelper.ToRadians(-138f); // cocked back over his head
        private const float ArmStartAngle = -MathHelper.PiOver2;                      // arm straight up

        // ── Timeline (ticks). Before release: age from spawn (Gwyn's telegraph tick 0). After: ticks since release.
        private const int FadeInStartAge = 10;
        private const int GlintAge = 34;
        private const int GlintPulseTicks = 6;
        public const int SlamTicks = 18;               // Gwyn's step eases in over the same 18 ticks
        private const int FloorLiveTicks = 16;
        private const int RestTicks = 20;
        private const int DissolveTicks = 20;
        private const int FloorLiveEnd = SlamTicks + FloorLiveTicks - 1;
        private const int DissolveStart = FloorLiveEnd + 1 + RestTicks;
        private const int LifeEnd = DissolveStart + DissolveTicks;

        // ── Look
        private const float TellBrightness = 0.6f;
        private const float SlamBrightness = 0.75f;
        private const float RestBrightness = 0.45f;
        private const float HaloOpacity = 0.18f;
        private const float BodyOpacity = 0.6f;
        private const float AfterimageOpacity = 0.34f;
        private const int AfterimageCount = 4;
        private const float HaloOffset = DrawScale;     // one sprite pixel
        private static readonly Color Gold = new Color(255, 200, 70);
        private const string NoiseRoot = "tsorcRevamp/Textures/Noise/";

        // ── Impact
        private const float SurfaceSearchDepth = 48f;
        private const float DustSegment = 16f;
        private const float ShakeStrength = 16f;
        private const int ShakeFrames = 28;
        private const int GlintDust = 28;
        // Prime's tip wave: 0.55x his melee hit, as Sundering Cinderfall's; the blade itself carries 2x.
        private const float TipWaveDamageShare = 0.55f / 2f;
        private const float TipWaveReachTiles = 40f;
        private const float TipWaveSpeed = 9f;

        static Asset<Effect> cinderTrailEffect;
        static Asset<Texture2D> flowNoise;

        private int age;
        private int releaseAge = -1;
        private int facing = 1;
        private Vector2 shoulder;
        private bool dissolving;
        private int dissolveStartAge;

        private bool Released => releaseAge >= 0;
        private int SlamTick => age - releaseAge;
        private bool SendsTipWave => Projectile.ai[1] > 0.5f;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
            Projectile.hide = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            // In front of tiles, behind Gwyn and the players, so neither is hidden by the sword.
            behindNPCs.Add(index);
        }

        public override bool? CanDamage()
        {
            bool live = Released && !dissolving && SlamTick >= 1 && SlamTick <= FloorLiveEnd;
            if (live)
            {
                return null;
            }

            return false;
        }

        public override void AI()
        {
            age++;

            int ownerIndex = (int)Projectile.ai[0];
            NPC owner = null;
            if (ownerIndex >= 0 && ownerIndex < Main.maxNPCs && Main.npc[ownerIndex].active)
            {
                owner = Main.npc[ownerIndex];
            }

            NPCs.Bosses.SuperHardMode.Gwyn gwyn = owner?.ModNPC as NPCs.Bosses.SuperHardMode.Gwyn;

            // Follow Gwyn's combo: hold over him through his tell, start the slam with his strike, dissolve otherwise.
            if (!Released && !dissolving)
            {
                if (gwyn != null && gwyn.LordsVerdictTelegraphing)
                {
                    facing = owner.direction;
                    shoulder = new Vector2(owner.Bottom.X - facing * ShoulderBehind, owner.Bottom.Y - FloorBandLift);
                }
                else if (gwyn != null && gwyn.LordsVerdictStriking)
                {
                    releaseAge = age;
                    facing = owner.direction;
                    shoulder = new Vector2(owner.Bottom.X - facing * ShoulderBehind, owner.Bottom.Y - FloorBandLift);
                    if (!Main.dedServ)
                    {
                        SoundEngine.PlaySound(SoundID.Item71 with { Volume = 1f, Pitch = -0.6f }, owner.Center);
                    }
                }
                else if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // Server or single player only: a client can receive the sword before it hears the combo started,
                    // so it holds and waits for the server's decision instead of dissolving on its own.
                    dissolving = true;
                    dissolveStartAge = age;
                }

                if (Main.netMode == NetmodeID.Server && (Released || dissolving))
                {
                    Projectile.netUpdate = true;
                }
            }

            float progress = 0f;
            if (Released)
            {
                progress = SlamProgress(SlamTick);
            }

            GetPose(progress, out Vector2 grip, out float angle);
            Projectile.Center = grip;
            Projectile.velocity = Vector2.Zero;

            bool finished = (Released && SlamTick >= LifeEnd) || (dissolving && age - dissolveStartAge >= DissolveTicks);
            if (finished)
            {
                Projectile.Kill();
                return;
            }

            bool impactTick = Released && !dissolving && SlamTick == SlamTicks;
            Vector2 bladeDirection = angle.ToRotationVector2();
            Vector2 tip = grip + bladeDirection * Reach;

            // Prime Phase II+: the impact sends one ground wave outward from the tip, only toward a player who ran past it.
            if (impactTick && SendsTipWave && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int waveDamage = (int)(Projectile.damage * TipWaveDamageShare);
                Vector2 wavePosition = new Vector2(tip.X, shoulder.Y + FloorBandLift - 20f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), wavePosition, Vector2.Zero,
                    ModContent.ProjectileType<GwynGroundFireWave>(), waveDamage, 5f, Main.myPlayer,
                    facing, TipWaveReachTiles, TipWaveSpeed);
            }

            if (Main.dedServ)
            {
                return;
            }

            float brightness = Brightness();
            for (int i = 0; i < 6; i++)
            {
                Vector2 lightPoint = grip + bladeDirection * (Reach * (i + 0.5f) / 6f);
                Lighting.AddLight(lightPoint, Gold.ToVector3() * 0.9f * brightness);
            }

            // Tell: gold motes converge on the grip, tightening as the sword forms.
            if (!Released && !dissolving && age < GlintAge && age % 2 == 0)
            {
                float gatherProgress = age / (float)GlintAge;
                float radius = MathHelper.Lerp(90f, 12f, gatherProgress);
                for (int i = 0; i < 2; i++)
                {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
                    Dust mote = Dust.NewDustPerfect(grip + offset, DustID.GoldFlame, -offset * 0.08f, 60, default, MathHelper.Lerp(0.9f, 1.4f, gatherProgress));
                    mote.noGravity = true;
                }
            }

            // Glint: the "it is about to come down" cue, as False Dawn's.
            if (!Released && !dissolving && age == GlintAge)
            {
                SoundEngine.PlaySound(SoundID.MaxMana with { Volume = 1f, Pitch = 0.6f }, tip);
                for (int i = 0; i < GlintDust; i++)
                {
                    Dust glint = Dust.NewDustPerfect(tip, DustID.GoldFlame, Main.rand.NextVector2CircularEdge(5f, 5f), 10, Color.White, 1.5f);
                    glint.noGravity = true;
                }
            }

            if (impactTick)
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1f, Pitch = -0.5f }, grip);
                SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact with { Volume = 1f, Pitch = -0.6f }, tip);
                Vector2 impactCenter = grip + bladeDirection * (Reach * 0.5f);
                UsefulFunctions.ScreenShake(impactCenter, ShakeStrength, ShakeFrames);
            }

            // Dust cloud along both bands (Gwyn's feet to the tip): a big burst on impact, a rolling cloud while live.
            bool rollingCloudTick = Released && !dissolving && SlamTick > SlamTicks && SlamTick <= FloorLiveEnd && SlamTick % 3 == 0;
            if (impactTick || rollingCloudTick)
            {
                float bandStart = shoulder.X + facing * ShoulderBehind;
                float bandLength = ArmRadius + Reach - ShoulderBehind;
                for (float along = 0f; along <= bandLength; along += DustSegment)
                {
                    float worldX = bandStart + facing * along;
                    if (!FindSurfaceY(worldX, out float surfaceY))
                    {
                        continue;
                    }

                    Vector2 surfacePoint = new Vector2(worldX, surfaceY);
                    int smokeCount = 2;
                    if (impactTick)
                    {
                        smokeCount = 4;
                    }

                    for (int i = 0; i < smokeCount; i++)
                    {
                        Vector2 smokeVelocity = new Vector2(Main.rand.NextFloat(-2f, 2f) + facing * 0.8f, -Main.rand.NextFloat(1f, 4f));
                        Dust smoke = Dust.NewDustPerfect(surfacePoint + new Vector2(Main.rand.NextFloat(-8f, 8f), -4f), DustID.Smoke,
                            smokeVelocity, 100, new Color(255, 225, 160), Main.rand.NextFloat(2f, 3f));
                        smoke.noGravity = true;
                    }

                    if (!impactTick)
                    {
                        continue;
                    }

                    for (int i = 0; i < 2; i++)
                    {
                        Dust ember = Dust.NewDustPerfect(surfacePoint, DustID.GoldFlame, new Vector2(Main.rand.NextFloat(-3f, 3f), -Main.rand.NextFloat(2f, 6f)), 20, default, 1.4f);
                        ember.noGravity = true;
                    }

                    int tileX = (int)(worldX / 16f);
                    int tileY = (int)((surfaceY + 2f) / 16f);
                    if (WorldGen.InWorld(tileX, tileY))
                    {
                        WorldGen.KillTile_MakeTileDust(tileX, tileY, Main.tile[tileX, tileY]);
                        WorldGen.KillTile_MakeTileDust(tileX, tileY, Main.tile[tileX, tileY]);
                    }
                }
            }

            // Rest and dissolve: gold motes lift off the blade.
            bool shedding = dissolving || (Released && SlamTick > FloorLiveEnd);
            if (shedding && Main.rand.NextBool(2))
            {
                Vector2 motePoint = grip + bladeDirection * Main.rand.NextFloat(HiltLength, Reach);
                Dust mote = Dust.NewDustPerfect(motePoint, DustID.GoldFlame, new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), -Main.rand.NextFloat(1f, 2.5f)), 60, default, 1.1f);
                mote.noGravity = true;
            }
        }

        /// <summary>Cubic ease-in over the slam: speed grows as t², so the fastest frame is the impact.</summary>
        private static float SlamProgress(float slamTick)
        {
            float t = MathHelper.Clamp(slamTick / SlamTicks, 0f, 1f);
            return t * t * t;
        }

        /// <summary>
        /// Grip position and world blade angle at a slam progress (0 = raised over Gwyn, 1 = flat on the floor). The arm
        /// and the blade turn on the same progress, so the grip swings a quarter circle while the wrist turns 138°.
        /// </summary>
        private void GetPose(float progress, out Vector2 grip, out float angle)
        {
            float armAngle = MathHelper.Lerp(ArmStartAngle, 0f, progress);
            float bladeAngle = MathHelper.Lerp(BladeStartAngle, 0f, progress);

            Vector2 armOffset = armAngle.ToRotationVector2() * ArmRadius;
            armOffset.X *= facing;
            grip = shoulder + armOffset;

            angle = bladeAngle;
            if (facing == -1)
            {
                angle = MathHelper.Pi - bladeAngle;
            }
        }

        /// <summary>First solid (or platform) surface within SurfaceSearchDepth below the floor band, in world Y.</summary>
        private bool FindSurfaceY(float worldX, out float surfaceY)
        {
            surfaceY = 0f;
            int tileX = (int)(worldX / 16f);
            float floorY = shoulder.Y + FloorBandLift;
            int topTile = (int)((floorY - 8f) / 16f);
            int bottomTile = (int)((floorY + SurfaceSearchDepth) / 16f);

            for (int tileY = topTile; tileY <= bottomTile; tileY++)
            {
                if (!WorldGen.InWorld(tileX, tileY))
                {
                    return false;
                }

                Tile tile = Main.tile[tileX, tileY];
                bool standable = tile.HasUnactuatedTile && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);
                if (standable)
                {
                    surfaceY = tileY * 16f;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Overall glow strength, scaling every draw layer and the light. Brightest while the floor band is live.</summary>
        private float Brightness()
        {
            if (dissolving)
            {
                float dissolveFade = 1f - (age - dissolveStartAge) / (float)DissolveTicks;
                return MathHelper.Clamp(dissolveFade, 0f, 1f) * TellBrightness;
            }

            if (!Released)
            {
                float fadeIn = MathHelper.Clamp((age - FadeInStartAge) / (float)(GlintAge - FadeInStartAge), 0f, 1f);
                float brightness = fadeIn * TellBrightness;
                bool glinting = age >= GlintAge && age < GlintAge + GlintPulseTicks;
                if (glinting)
                {
                    float pulse = 1f - (age - GlintAge) / (float)GlintPulseTicks;
                    brightness = MathHelper.Lerp(TellBrightness, 1f, pulse);
                }
                return brightness;
            }

            int slamTick = SlamTick;
            if (slamTick < SlamTicks)
            {
                return SlamBrightness;
            }
            if (slamTick <= FloorLiveEnd)
            {
                return 1f;
            }
            if (slamTick < DissolveStart)
            {
                return RestBrightness;
            }

            float lateFade = 1f - (slamTick - DissolveStart) / (float)DissolveTicks;
            return MathHelper.Clamp(lateFade, 0f, 1f) * RestBrightness;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!Released || dissolving)
            {
                return false;
            }

            int slamTick = SlamTick;

            // On the floor: one fixed band.
            if (slamTick >= SlamTicks)
            {
                GetPose(1f, out Vector2 restGrip, out float restAngle);
                Vector2 restDirection = restAngle.ToRotationVector2();
                float restPoint = 0f;
                return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                    restGrip + restDirection * HiltLength, restGrip + restDirection * Reach, BladeHitWidth, ref restPoint);
            }

            // Slamming: every blade position between last tick's pose and this tick's, so a fast sweep can't skip a target.
            float previousProgress = SlamProgress(slamTick - 1);
            float currentProgress = SlamProgress(slamTick);
            GetPose(previousProgress, out Vector2 previousGrip, out float previousAngle);
            GetPose(currentProgress, out Vector2 currentGrip, out float currentAngle);

            float angleStep = Math.Abs(MathHelper.WrapAngle(currentAngle - previousAngle));
            float gripStep = Vector2.Distance(previousGrip, currentGrip);
            int samples = Math.Max(2, (int)Math.Ceiling(Math.Max(angleStep / MathHelper.ToRadians(3f), gripStep / 12f)));

            for (int i = 0; i <= samples; i++)
            {
                float blend = i / (float)samples;
                float sampleProgress = MathHelper.Lerp(previousProgress, currentProgress, blend);
                GetPose(sampleProgress, out Vector2 sampleGrip, out float sampleAngle);
                Vector2 direction = sampleAngle.ToRotationVector2();
                float collisionPoint = 0f;
                bool hit = Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                    sampleGrip + direction * HiltLength, sampleGrip + direction * Reach, BladeHitWidth, ref collisionPoint);
                if (hit)
                {
                    return true;
                }
            }

            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float brightness = Brightness();
            if (brightness <= 0.01f)
            {
                return false;
            }

            cinderTrailEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GwynCinderTrail", AssetRequestMode.ImmediateLoad);
            flowNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "T_Aurax44", AssetRequestMode.ImmediateLoad);

            Texture2D texture = TextureAssets.Projectile[Type].Value;

            // The sprite's blade points up-right at -45°; flipped for a left-facing Gwyn it points up-left at -135°.
            SpriteEffects flip = SpriteEffects.None;
            Vector2 origin = SpriteGrip;
            float spriteAngleOffset = MathHelper.PiOver4;
            if (facing == -1)
            {
                flip = SpriteEffects.FlipHorizontally;
                origin = new Vector2(texture.Width - SpriteGrip.X, SpriteGrip.Y);
                spriteAngleOffset = MathHelper.Pi * 0.75f;
            }

            float progress = 0f;
            bool slamming = Released && !dissolving && SlamTick < SlamTicks;
            if (Released)
            {
                progress = SlamProgress(SlamTick);
            }

            GetPose(progress, out Vector2 grip, out float angle);
            Vector2 drawGrip = grip - Main.screenPosition;
            float rotation = angle + spriteAngleOffset;

            // Glow passes (halo, afterimages) are additive; the body after them is alpha-blended. Pixel grid on the
            // sprite's own 96x96 texels with PointClamp, so every block is one 4x4 sprite pixel.
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Effect effect = cinderTrailEffect.Value;
            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];

            try
            {
                graphicsDevice.Textures[1] = flowNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                effect.CurrentTechnique = effect.Techniques["GwynCinderBlade"];
                effect.Parameters["CinderColor"].SetValue(new Color(255, 150, 30).ToVector3());
                effect.Parameters["FlameColor"].SetValue(new Color(255, 210, 80).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 250, 215).ToVector3());
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["DrawSize"].SetValue(texture.Size());
                effect.Parameters["PrimaryTextureSize"].SetValue(texture.Size());
                effect.Parameters["PixelGrid"].SetValue(new Vector4(texture.Width, texture.Height, 1f / texture.Width, 1f / texture.Height));
                effect.Parameters["Progress"].SetValue(0f);

                // Halo: four copies one sprite pixel out, so the glow follows the blade's own outline.
                effect.Parameters["Opacity"].SetValue(HaloOpacity * brightness);
                effect.CurrentTechnique.Passes[0].Apply();
                Vector2[] haloOffsets = { new Vector2(HaloOffset, 0f), new Vector2(-HaloOffset, 0f), new Vector2(0f, HaloOffset), new Vector2(0f, -HaloOffset) };
                foreach (Vector2 haloOffset in haloOffsets)
                {
                    Main.EntitySpriteDraw(texture, drawGrip + haloOffset, null, Color.White, rotation, origin, DrawScale, flip, 0);
                }

                // Afterimages: earlier points on the same curve, so they need no position history.
                if (slamming)
                {
                    for (int i = AfterimageCount; i >= 1; i--)
                    {
                        int pastTick = SlamTick - i;
                        if (pastTick < 0)
                        {
                            continue;
                        }

                        GetPose(SlamProgress(pastTick), out Vector2 pastGrip, out float pastAngle);
                        float trailOpacity = AfterimageOpacity * (1f - i / (float)(AfterimageCount + 1)) * brightness;
                        effect.Parameters["Opacity"].SetValue(trailOpacity);
                        effect.CurrentTechnique.Passes[0].Apply();
                        Main.EntitySpriteDraw(texture, pastGrip - Main.screenPosition, null, Color.White,
                            pastAngle + spriteAngleOffset, origin, DrawScale, flip, 0);
                    }
                }

                // Body: see-through gold with the fire moving inside it. Alpha-blended, not additive: additive gold
                // washes to white on a bright sky (ShaderPreview GwynVerdictBladeSky), alpha-blended gold reads on both.
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
                    DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                graphicsDevice.Textures[1] = flowNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                effect.Parameters["Opacity"].SetValue(BodyOpacity * brightness);
                effect.CurrentTechnique.Passes[0].Apply();
                Main.EntitySpriteDraw(texture, drawGrip, null, Color.White, rotation, origin, DrawScale, flip, 0);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
            }

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            return false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((short)age);
            writer.Write((short)releaseAge);
            writer.Write((sbyte)facing);
            writer.WriteVector2(shoulder);
            writer.Write(dissolving);
            writer.Write((short)dissolveStartAge);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            age = reader.ReadInt16();
            releaseAge = reader.ReadInt16();
            facing = reader.ReadSByte();
            shoulder = reader.ReadVector2();
            dissolving = reader.ReadBoolean();
            dissolveStartAge = reader.ReadInt16();
        }
    }
}
