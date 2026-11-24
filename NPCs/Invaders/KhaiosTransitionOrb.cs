using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Invaders
{
    /// <summary>
    /// Dramatic ~2.5s ascension intermission between Phase 1 Khaios and Phase 2 Spirit of Khaios.
    /// Pulls emerald-to-gold spirit dust inward with a rising dark-fantasy resonance, ascends safely
    /// below any solid ceiling, and manifests Spirit of Khaios in a golden shockwave burst.
    /// </summary>
    public class KhaiosTransitionOrb : ModNPC
    {
        public override string Texture => "tsorcRevamp/NPCs/Invaders/PuppetPlaceholder";

        private const int GatherTicks = 255;
        private const int RiseTicks = 75;
        private const int TotalAscensionTicks = GatherTicks + RiseTicks;
        private const float DesiredRiseDistance = 8f * 16f;
        private const float CeilingClearancePixels = 52f;

        private static ArmorShaderData orbShader;
        private float safeRiseSpeed;
        private bool riseSolved;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.NPCBestiaryDrawModifiers hideBestiary = new NPCID.Sets.NPCBestiaryDrawModifiers
            {
                Hide = true,
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, hideBestiary);
        }

        public override void SetDefaults()
        {
            NPC.width = 40;
            NPC.height = 40;
            NPC.aiStyle = -1;
            NPC.lifeMax = 1;
            NPC.damage = 0;
            NPC.defense = 0;
            NPC.dontTakeDamage = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.knockBackResist = 0f;
            NPC.alpha = 80;
            NPC.npcSlots = 0f;
        }

        public override void AI()
        {
            NPC.ai[0]++;
            int elapsed = (int)NPC.ai[0];
            float totalProgress = MathHelper.Clamp(elapsed / (float)TotalAscensionTicks, 0f, 1f);

            if (!riseSolved)
            {
                riseSolved = true;
                float safeDistance = SolveSafeRiseDistance(NPC.Center, DesiredRiseDistance);
                safeRiseSpeed = safeDistance / RiseTicks;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && !AnyLivingPlayerNearby())
            {
                NPC.active = false;
                NPC.netUpdate = true;
                return;
            }

            if (!Main.dedServ)
            {
                Vector3 lightColor = Vector3.Lerp(new Vector3(0.35f, 0.95f, 0.55f), new Vector3(1.1f, 0.88f, 0.35f), totalProgress);
                Lighting.AddLight(NPC.Center, lightColor * (0.85f + totalProgress * 0.85f));

                if (elapsed == 1)
                {
                    SoundEngine.PlaySound(
                        new SoundStyle("tsorcRevamp/Sounds/HollowKnight/ghost_absorb") with { Volume = 0.56f, Pitch = -0.10f },
                        NPC.Center);
                }
                else if (elapsed == 165)
                {
                    // Mid-hold resonant swell so the extended 4.25s glow stays dramatic and builds momentum.
                    SoundEngine.PlaySound(
                        new SoundStyle("tsorcRevamp/Sounds/DarkSouls/harmonic-magic") with { Volume = 0.44f, Pitch = -0.22f },
                        NPC.Center);
                }

                // Escalating emerald -> gold -> white converging spirit energy throughout the 5.5s ascension.
                int dustCount = 3;
                if (elapsed >= 200)
                {
                    dustCount = 8;
                }
                else if (elapsed >= 90)
                {
                    dustCount = 5;
                }

                for (int i = 0; i < dustCount; i++)
                {
                    float ringRadius = MathHelper.Lerp(135f, 36f, totalProgress);
                    Vector2 offset = Main.rand.NextVector2CircularEdge(ringRadius, ringRadius);
                    Vector2 inward = -offset.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(2.4f, 5.4f, totalProgress);
                    int dustType = DustID.TerraBlade;
                    Color dustTint = new Color(90, 255, 140);

                    if (totalProgress > 0.72f && i % 3 == 0)
                    {
                        dustType = DustID.WhiteTorch;
                        dustTint = Color.White;
                    }
                    else if (Main.rand.NextFloat() < totalProgress)
                    {
                        dustType = DustID.GoldFlame;
                        dustTint = new Color(255, 220, 80);
                    }

                    Dust mote = Dust.NewDustPerfect(
                        NPC.Center + offset,
                        dustType,
                        inward,
                        30,
                        dustTint,
                        Main.rand.NextFloat(1.0f, 1.65f));
                    mote.noGravity = true;
                }

                // Rising radiant sparks boiling off the orb itself as it charges up.
                for (int i = 0; i < 2; i++)
                {
                    int riseType = totalProgress > 0.55f ? DustID.GoldFlame : DustID.WhiteTorch;
                    Dust rising = Dust.NewDustPerfect(
                        NPC.Center + Main.rand.NextVector2Circular(26f, 26f),
                        riseType,
                        new Vector2(Main.rand.NextFloat(-1.2f, 1.2f), Main.rand.NextFloat(-4.2f, -1.2f)),
                        15,
                        Color.White,
                        Main.rand.NextFloat(0.95f, 1.55f));
                    rising.noGravity = true;
                }

                if (elapsed == GatherTicks)
                {
                    UsefulFunctions.ScreenShake(NPC.Center, 3.0f, 28, distanceFalloff: 750f);
                }
            }

            if (elapsed < GatherTicks)
            {
                NPC.velocity = Vector2.Zero;
                return;
            }

            if (elapsed < TotalAscensionTicks)
            {
                NPC.velocity = new Vector2(0f, -safeRiseSpeed);
                return;
            }

            NPC.velocity = Vector2.Zero;
            CreateTransformationBurst();

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.NewNPC(
                    NPC.GetSource_FromAI(),
                    (int)NPC.Center.X,
                    (int)NPC.Center.Y,
                    ModContent.NPCType<SpiritOfKhaios>());
                NPC.active = false;
                NPC.netUpdate = true;
            }
        }

        public override void OnKill()
        {
            CreateTransformationBurst();
        }

        private static float SolveSafeRiseDistance(Vector2 startCenter, float desiredRise)
        {
            const float stepPixels = 8f;
            float clearDistance = 0f;

            while (clearDistance < desiredRise)
            {
                float probeY = startCenter.Y - clearDistance - CeilingClearancePixels;
                int tileX = (int)(startCenter.X / 16f);
                int tileY = (int)(probeY / 16f);

                if (tileX < 2 || tileX >= Main.maxTilesX - 2 || tileY < 2 || tileY >= Main.maxTilesY - 2)
                {
                    break;
                }

                Tile tile = Framing.GetTileSafely(tileX, tileY);
                if (tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
                {
                    break;
                }

                clearDistance += stepPixels;
            }

            return Math.Max(0f, clearDistance);
        }

        private bool AnyLivingPlayerNearby()
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead && player.DistanceSQ(NPC.Center) < 3000f * 3000f)
                {
                    return true;
                }
            }

            return false;
        }

        private void CreateTransformationBurst()
        {
            if (NPC.localAI[0] != 0f || Main.dedServ)
            {
                return;
            }

            NPC.localAI[0] = 1f;
            SoundEngine.PlaySound(
                new SoundStyle("tsorcRevamp/Sounds/HollowKnight/dream_enter_pt_2") with { Volume = 0.80f, Pitch = -0.15f },
                NPC.Center);
            SoundEngine.PlaySound(
                new SoundStyle("tsorcRevamp/Sounds/DarkSouls/souls-gained-large") with { Volume = 0.50f, Pitch = 0.05f },
                NPC.Center);
            UsefulFunctions.ScreenShake(NPC.Center, 8.5f, 26, distanceFalloff: 950f);

            // Layer 1 (120 motes): blazing golden-white apotheosis core nova.
            for (int i = 0; i < 120; i++)
            {
                int dustType = DustID.WhiteTorch;
                Color tint = Color.White;
                if (i % 2 == 0)
                {
                    dustType = DustID.GoldFlame;
                    tint = new Color(255, 228, 100);
                }
                else if (i % 5 == 0)
                {
                    dustType = DustID.Electric;
                    tint = Color.Cyan;
                }

                Dust nova = Dust.NewDustPerfect(
                    NPC.Center + Main.rand.NextVector2Circular(28f, 34f),
                    dustType,
                    Main.rand.NextVector2Circular(13.5f, 13.5f),
                    10,
                    tint,
                    Main.rand.NextFloat(1.15f, 2.0f));
                nova.noGravity = true;
            }

            // Layer 2 (72 motes): crisp 360-degree expanding seraphic shockwave halo.
            for (int i = 0; i < 72; i++)
            {
                float ringAngle = MathHelper.TwoPi * i / 72f;
                Vector2 ringDir = ringAngle.ToRotationVector2();
                int ringType = i % 2 == 0 ? DustID.SilverFlame : DustID.GoldFlame;
                Color ringTint = i % 2 == 0 ? Color.White : new Color(255, 225, 95);
                Dust ringSpark = Dust.NewDustPerfect(
                    NPC.Center + ringDir * 16f,
                    ringType,
                    ringDir * Main.rand.NextFloat(10.5f, 13.5f),
                    0,
                    ringTint,
                    Main.rand.NextFloat(1.25f, 1.85f));
                ringSpark.noGravity = true;
            }

            // Layer 3 (110 motes): sweeping left/right wing-plume fountain raining sparks across the arena.
            for (int i = 0; i < 110; i++)
            {
                float sideSign = i % 2 == 0 ? 1f : -1f;
                Vector2 wingVelocity = new Vector2(
                    sideSign * Main.rand.NextFloat(3.5f, 14.5f),
                    Main.rand.NextFloat(-11.5f, 1.2f));
                int plumeType = DustID.WhiteTorch;
                Color plumeTint = Color.White;
                if (i % 3 == 0)
                {
                    plumeType = DustID.GoldFlame;
                    plumeTint = new Color(255, 220, 90);
                }
                else if (i % 4 == 0)
                {
                    plumeType = DustID.IceTorch;
                    plumeTint = Color.LightCyan;
                }

                Dust plume = Dust.NewDustPerfect(
                    NPC.Center + new Vector2(sideSign * Main.rand.NextFloat(6f, 34f), Main.rand.NextFloat(-24f, 24f)),
                    plumeType,
                    wingVelocity,
                    15,
                    plumeTint,
                    Main.rand.NextFloat(1.1f, 2.0f));
                plume.noGravity = i % 5 == 0;
            }

            // Layer 4 (90 motes): tall vertical radiant ascension beam framing Spirit of Khaios's arrival.
            for (int i = 0; i < 90; i++)
            {
                int pillarType = i % 2 == 0 ? DustID.AncientLight : DustID.GoldFlame;
                Color pillarTint = i % 2 == 0 ? Color.White : new Color(255, 232, 120);
                Vector2 pillarOffset = new Vector2(Main.rand.NextFloat(-34f, 34f), Main.rand.NextFloat(-56f, 56f));
                Vector2 pillarVelocity = new Vector2(Main.rand.NextFloat(-2.2f, 2.2f), Main.rand.NextFloat(-14.5f, 7.5f));
                Dust beam = Dust.NewDustPerfect(
                    NPC.Center + pillarOffset,
                    pillarType,
                    pillarVelocity,
                    0,
                    pillarTint,
                    Main.rand.NextFloat(0.95f, 1.75f));
                beam.noGravity = true;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.Additive,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            orbShader ??= new ArmorShaderData(
                new Ref<Effect>(ModContent.Request<Effect>("tsorcRevamp/Effects/MarilithIntro", AssetRequestMode.ImmediateLoad).Value),
                "MarilithIntroPass");

            float progress = MathHelper.Clamp(NPC.ai[0] / TotalAscensionTicks, 0f, 1f);
            float pulse = 35f + 22f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * (4f + progress * 5f));
            orbShader.UseSaturation(pulse);
            orbShader.Apply(null);

            Rectangle source = tsorcRevamp.NoiseTurbulent.Bounds;
            float scale = MathHelper.Lerp(1.05f, 1.55f, progress)
                + 0.08f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f);
            Main.spriteBatch.Draw(
                tsorcRevamp.NoiseTurbulent,
                NPC.Center - Main.screenPosition,
                source,
                Color.White,
                0f,
                source.Size() * 0.5f,
                scale,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.GameViewMatrix.TransformationMatrix);
            return false;
        }

        public override bool CheckActive() => false;
    }
}
