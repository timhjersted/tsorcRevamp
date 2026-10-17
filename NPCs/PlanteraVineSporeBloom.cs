using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.NPCs
{
    /// <summary>
    /// Spore Bloom: Plantera's vines (vanilla draws one from each Plantera's Hook to her center) shed Miasma spores
    /// while their hook is anchored. A hook that starts relocating goes quiet, so hook movement is the breathing room.
    /// Phase 1: 2.5-tile-wide spore band. Phase 2 (below 50% life): 4.5 tiles wide, and every vine falls silent while
    /// Miasma Breath runs (PlanteraMiasmaBreath), resuming BreathResumeDelayTicks after it ends.
    ///
    /// No new sync: every machine already has the hooks' synced positions and target tiles (ai[0]/ai[1]) plus
    /// Plantera's breath clock (ai[3]), so each machine derives the same bloom state locally. Dust is client-only;
    /// Miasma buildup is checked by each victim client against its own player and reported via MiasmaBuildup.Apply.
    /// Counterplay: a dodge roll (tsorcRevampPlayer.isDodging) crosses a band without building anything.
    /// </summary>
    public class PlanteraVineSporeBloom : GlobalNPC
    {
        // A hook counts as anchored within this many px of its target tile centre; vanilla snaps it there on arrival.
        private const float AnchorTolerancePx = 6f;

        // On anchoring: 60 ticks of sparse, harmless trickle (the telegraph), then full bloom. On losing the anchor
        // (or a breath pause) a full bloom thins out over 20 ticks, also harmless.
        private const int TelegraphTicks = 60;
        private const int FadeTicks = 20;

        // Total band width 2.5 tiles (phase 1) / 4.5 tiles (phase 2), stored as half-widths either side of the vine.
        private const float Phase1HalfWidthPx = 1.25f * 16f;
        private const float Phase2HalfWidthPx = 2.25f * 16f;

        // Spores start this far beyond the edge of Plantera's body, leaving a spore-free gap to fight her in.
        private const float BodyGapPx = 6f * 16f;

        // Phase 2: vines stay silent for 3s after Miasma Breath ends.
        private const int BreathResumeDelayTicks = 3 * 60;

        // Buildup accrues per tick spent inside any band (not on entry), sent as one report per 20 exposed ticks so the
        // multiplayer report packet goes out 3x a second at most. 8 per 20 ticks = 24/s: ~4s of standing fills the meter.
        private const int ExposureTicksPerReport = 20;
        private const int MiasmaBuildupPerReport = 8;

        // Spore emission: dusts per tick per 64px of vine during full bloom; the telegraph runs at a quarter of that.
        private const float SporesPer64PxPerTick = 0.7f;
        private const float TelegraphDensity = 0.25f;

        public override bool InstancePerEntity => true;

        // Per hook, per machine; deliberately not synced (derived from synced state every tick).
        private int anchoredTicks;
        private int fadeTicksLeft;
        private ulong lastBreathActiveTick;
        private bool breathSeen;

        // Local-player exposure, shared across all hooks so standing where two bands overlap still counts once per tick.
        private static int localExposureTicks;
        private static ulong lastExposureTick;

        public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
        {
            return entity.type == NPCID.PlanterasHook;
        }

        public override void PostAI(NPC npc)
        {
            int planteraIndex = NPC.plantBoss;
            bool planteraValid = planteraIndex >= 0 && planteraIndex < Main.maxNPCs
                && Main.npc[planteraIndex].active && Main.npc[planteraIndex].type == NPCID.Plantera;

            if (!planteraValid)
            {
                anchoredTicks = 0;
                fadeTicksLeft = 0;
                return;
            }

            NPC plantera = Main.npc[planteraIndex];
            bool inSecondPhase = plantera.life < plantera.lifeMax / 2;

            // --- Decide: is this vine allowed to bloom this tick? ---

            // Anchored = sitting on its target tile. ai[0]/ai[1] are tile coords; 0 means no target picked yet.
            bool hasTarget = npc.ai[0] > 0f && npc.ai[1] > 0f;
            Vector2 targetPoint = new Vector2(npc.ai[0] * 16f - 8f, npc.ai[1] * 16f - 8f);
            float distanceToTarget = Vector2.Distance(npc.Center, targetPoint);
            bool anchored = hasTarget && distanceToTarget <= AnchorTolerancePx;

            // Phase 2 breath pause. PlanteraMiasmaBreath's clock (Plantera ai[3]) is above CooldownTicks exactly while
            // the breath is winding up or exhaling; it is held at 0 in phase 1, so this never trips there.
            bool breathActive = plantera.ai[3] > PlanteraMiasmaBreath.CooldownTicks;
            if (breathActive)
            {
                lastBreathActiveTick = Main.GameUpdateCount;
                breathSeen = true;
            }

            ulong ticksSinceBreath = Main.GameUpdateCount - lastBreathActiveTick;
            bool breathPause = inSecondPhase && breathSeen && ticksSinceBreath < BreathResumeDelayTicks;

            bool canBloom = anchored && !breathPause;

            if (canBloom)
            {
                anchoredTicks++;
            }
            else
            {
                anchoredTicks = 0;
            }

            bool fullBloom = canBloom && anchoredTicks > TelegraphTicks;
            bool telegraphing = canBloom && !fullBloom;

            if (fullBloom)
            {
                fadeTicksLeft = FadeTicks;
            }
            else if (fadeTicksLeft > 0 && !telegraphing)
            {
                fadeTicksLeft--;
            }

            bool fading = !canBloom && fadeTicksLeft > 0;
            if (!fullBloom && !telegraphing && !fading)
            {
                return;
            }

            // --- The band: from just past Plantera's body out to the hook ---

            Vector2 toHook = npc.Center - plantera.Center;
            float vineLength = toHook.Length();
            float bodyRadius = System.Math.Max(plantera.width, plantera.height) * 0.5f;
            float bandStartDistance = bodyRadius + BodyGapPx;

            if (vineLength <= bandStartDistance)
            {
                return;
            }

            Vector2 vineDirection = toHook / vineLength;
            Vector2 bandStart = plantera.Center + vineDirection * bandStartDistance;
            Vector2 bandEnd = npc.Center;
            float bandLength = vineLength - bandStartDistance;
            float halfWidth = inSecondPhase ? Phase2HalfWidthPx : Phase1HalfWidthPx;

            // --- Spores: drift outward from the vine on both sides, reaching about the band edge before fading ---

            if (!Main.dedServ)
            {
                float density = 1f;
                if (telegraphing)
                {
                    density = TelegraphDensity;
                }
                else if (fading)
                {
                    density = fadeTicksLeft / (float)FadeTicks;
                }

                float sporesThisTick = bandLength / 64f * SporesPer64PxPerTick * density;
                int wholeSpores = (int)sporesThisTick;
                float fractionalSpore = sporesThisTick - wholeSpores;
                if (Main.rand.NextFloat() < fractionalSpore)
                {
                    wholeSpores++;
                }

                Vector2 perpendicular = new Vector2(-vineDirection.Y, vineDirection.X);
                for (int i = 0; i < wholeSpores; i++)
                {
                    float alongVine = Main.rand.NextFloat(bandLength);
                    int side = Main.rand.NextBool() ? 1 : -1;
                    Vector2 sporePosition = bandStart + vineDirection * alongVine + perpendicular * side * Main.rand.NextFloat(2f, 6f);

                    // Outward drift scaled to the band, so the wider phase-2 spores travel proportionally farther.
                    float driftSpeed = halfWidth / 28f * Main.rand.NextFloat(0.7f, 1.2f);
                    Vector2 sporeVelocity = perpendicular * side * driftSpeed + vineDirection * Main.rand.NextFloat(-0.3f, 0.3f);

                    Color sporeColor = Main.rand.NextBool() ? MiasmaPlayer.MiasmaPinkColor : MiasmaPlayer.MiasmaColor;
                    Dust spore = Dust.NewDustPerfect(sporePosition, DustID.RainbowMk2, sporeVelocity, 90, sporeColor, Main.rand.NextFloat(0.9f, 1.35f));
                    spore.noGravity = true;
                }

                if (Main.rand.NextBool(6))
                {
                    Vector2 lightPoint = bandStart + vineDirection * Main.rand.NextFloat(bandLength);
                    Lighting.AddLight(lightPoint, MiasmaPlayer.MiasmaColor.ToVector3() * 0.35f * density);
                }
            }

            // --- Buildup: only during full bloom, only for the local player, never while dodge-rolling ---

            if (!fullBloom || Main.netMode == NetmodeID.Server)
            {
                return;
            }

            Player localPlayer = Main.LocalPlayer;
            if (!localPlayer.active || localPlayer.dead)
            {
                return;
            }

            // Line-vs-AABB with the band's full width = the player's hitbox overlapping the spore band.
            float collisionPoint = 0f;
            bool inBand = Collision.CheckAABBvLineCollision(localPlayer.TopLeft, localPlayer.Size, bandStart, bandEnd, halfWidth * 2f, ref collisionPoint);
            bool dodging = localPlayer.GetModPlayer<tsorcRevampPlayer>().isDodging;
            bool alreadyCountedThisTick = lastExposureTick == Main.GameUpdateCount;

            if (!inBand || dodging || alreadyCountedThisTick)
            {
                return;
            }

            lastExposureTick = Main.GameUpdateCount;
            localExposureTicks++;

            if (localExposureTicks >= ExposureTicksPerReport)
            {
                localExposureTicks -= ExposureTicksPerReport;
                MiasmaBuildup.Apply(localPlayer, MiasmaBuildupPerReport);
            }
        }
    }
}
