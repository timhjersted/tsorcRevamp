using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy.GravelordNito;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.GravelordNito
{
    public class GravelordNito : ModNPC, IStaggerable, NPCs.IDebugAttackLabel
    {
        public override string Texture => "tsorcRevamp/NPCs/Bosses/GravelordNito/GravelordNitoAttacking";
        // Explicit head path — [AutoloadBossHead] can't be used because it would derive the head from the
        // Texture above (GravelordNitoAttacking_Head_Boss), which doesn't exist.
        public override string BossHeadTexture => "tsorcRevamp/NPCs/Bosses/GravelordNito/GravelordNito_Head_Boss";

        enum AttackState : byte
        {
            None = 0,
            SideSweep,
            BackhandSweep,
            OverheadCleave,
            ImpalingThrust,
            TripleReaperCombo,
            DraggingAdvance,
            LeapingCleave,
            SwordRain,
            BoneVolley,
            GravelordSpikes,
            GravelordDance,
            DeathNova,
            MiasmaBreath,
            BonePillarCage,
            GraveHands,
            QuietusCombo,
            CemeteryMarch,
            HollowCommand,
            GravelordJudgment,
            FollowUpSlash,
            PhaseTransition,
            ComboRecovery,
            // Appended (not inserted) so the byte values of everything above stay stable.
            ReaperWeave,       // chained underhand/overhand swings in a random pole-to-pole order
            ReapersPendulum,   // fast strict back-and-forth chain: side / backhand / side ...
            SwordLineDance,    // rows of sky spikes walking in from the outside edge toward Nito
        }

        enum AttackFamily : byte
        {
            None,
            SwordSingle,
            SwordSequence,
            GapCloser,
            OverheadRain,
            TargetedGround,
            ProjectileVolley,
            AreaBurst,
            Miasma,
        }

        const int FrameCount = 23;
        const int FrameHeight = 300;
        const int BodyWidth = 400;
        // The character art is NOT centered in the 400px frame — its opaque body is centered at
        // ~x=283 (measured). SpriteEffects.FlipHorizontally mirrors texture SAMPLING within the fixed
        // draw quad, it does NOT mirror around the origin — so a single origin.X can only align ONE
        // facing. Draw uses BodyDrawCenterX when unflipped and (BodyWidth - BodyDrawCenterX) when
        // flipped (see PreDraw's bodyOriginX) so the body sits exactly on drawBottom.X either way,
        // instead of popping by up to (BodyWidth - 2*BodyDrawCenterX) = 166px on one facing.
        const float BodyDrawCenterX = 283f;
        const int HeavyTelegraph = 48;
        const int LongChannelTicks = 120;
        const int LongChannelStaggerTicks = 80;
        const float ImpalingThrustMinRange = 410f; // old 110 + requested 300px separation
        const float ImpalingThrustMaxRange = 810f; // was 650: +160px (10 tiles); RunImpalingThrust now really closes that gap

        // The dash's tell: a 34-tick coil, and the sword flashes white ThrustFlashLeadTicks before the
        // launch for ThrustFlashTicks (sharp rise, fast fade), so the cue lands well before the commit.
        const int ThrustTelegraphTicks = 34;
        const int ThrustFlashLeadTicks = 30;
        const int ThrustFlashTicks = 15;

        const float DeathNovaRadius = 600f;         // was 300: twice the ring. Duration follows: radius / 8px-per-tick expand
        const float BoneVolleyMaxRange = 800f;      // 50 tiles: the furthest the shard ballistics are asked to reach
        const float RangedRainMinDistance = 400f;   // horizontal gap that counts as "at range" for the independent SwordRain trigger
        const int RangedRainChargeTicks = 480;      // 8s spent at range (in any state) earns a guaranteed SwordRain
        const float PhaseOneApproachScale = 1.5f;   // phase-1 melee gap-closing speed multiplier (see AdvanceTowardPlayer)
        const float PhaseTwoApproachScale = 1.9f;   // phase 2 closes harder still (idle walk speed is NOT scaled)
        const float SwipePushScale = 1.35f;         // multiplier on the forward step every sword swipe releases with
        const int MaxComboSteps = 6;                // ComboKinds packs 3 bits per step into an int
        const float ComboContinueRange = 420f;      // a chain stops early once the player is further than this
        const float ComboContinueHeight = 190f;

        // Timing variants for the single swipes (SideSweep / BackhandSweep / OverheadCleave): the same
        // three moves at different rhythms, rolled per attack so the player can't metronome them.
        const int VariantStandard = 0;
        const int VariantQuick = 1;    // short tell, fast slash
        const int VariantDelayed = 2;  // long eased-in tell + a held beat, then a fast slash

        float ApproachScale => PhaseTwo ? PhaseTwoApproachScale : PhaseOneApproachScale;

        // AttackTimer tick the dash's white flash starts on: ThrustFlashLeadTicks before the launch tick
        // (telegraph + 1), never before tick 1 (a halved telegraph gets a shorter lead, not a negative one).
        int ThrustFlashStartTick => Math.Max(1, Telegraph(ThrustTelegraphTicks) + 1 - ThrustFlashLeadTicks);

        // White copy of the sword sprite for the dash flash; built on first use, disposed in Unload.
        static Texture2D swordSilhouette;

        public override void Unload()
        {
            Texture2D textureToDispose = swordSilhouette;
            swordSilhouette = null;

            // Mod content unloads on a worker thread, but FNA GPU resources may only be disposed on the
            // main thread (same pattern as the hurt-vignette texture in tsorcRevampSystems).
            if (textureToDispose != null && !textureToDispose.IsDisposed)
            {
                Main.QueueMainThreadAction(textureToDispose.Dispose);
            }
        }

        AttackState State = AttackState.None;

        /// <summary>DebugMode above-head readout (see IDebugAttackLabel).</summary>
        public string DebugAttackLabel => NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().StaggerTimer > 0
            ? "Staggered"
            : State == AttackState.None ? "Idle" : NPCs.DebugLabels.Humanize(State.ToString());

        public bool IsPhaseTwo => PhaseTwo;

        AttackState LastAttack = AttackState.None;
        AttackState PreviousAttack = AttackState.None;
        int AttackTimer;
        int AttackCooldown = 90;
        int lockedDir = 1;
        int lockedKind = 0; // vertical-read slash kind for the current single-hit attack; see ChooseVerticalKind
        // Deterministic overhead/rising alternation — kind 1 (top-to-bottom) and kind 4 (bottom-to-top)
        // are already exact reverses of the same arc (see SlashPhi), so each OverheadCleave just flips
        // which end it starts from. Mirrors the player's own FlipAttackEachSwing/AttackId%2 toggle
        // (QuickSlashMeleeAnimation.cs) — guaranteed back-and-forth, never the same direction twice in
        // a row, instead of leaving kind 4 to only ever appear as a reactive vertical-read substitute.
        bool NextOverheadIsRising;
        bool DragRunLeapLaunched; // one-shot: has DraggingAdvance's rising-uppercut leap fired yet this attack?
        // How many ticks ImpalingThrust dashes before the release, solved from the gap at dash start.
        // Synced: it decides WHEN the slash releases, so every machine must agree on it.
        int ThrustDashTicks;
        // Ticks spent at range; only the server reads it (idle-branch attack pick), so it isn't synced.
        int RangedRainCharge;
        // CemeteryMarch's procession line, locked once at cast so the player can't drag it around.
        float MarchOriginX;
        float MarchGroundY;
        int FootstepTimer;
        int ComboRecoveryTicks;
        bool PhaseTwo;
        bool HalfTelegraph;
        AttackState QueuedAttack = AttackState.None;
        int QueuedSlashKind = -1;
        Vector2 MiasmaAimDirection = Vector2.UnitX;

        // ── Loose-sword swing animation ─────────────────────────────────────────
        // The body sheet is ALWAYS the no-sword art (GravelordNitoAttacking.png — despite the name,
        // it has no blade painted in; GravelordNito.png is the one with the sword baked in and is no
        // longer used for the body). A single loose sword layer (GravelordNitoSword.png) is drawn
        // BEHIND the body every frame. It rests in a horizontal idle pose, winds up toward a slash's
        // start pose, plays the full swing arc itself (the NitoSwordSlash projectile is an invisible
        // hitbox that mirrors this same arc), then eases back to idle. All poses come from the shared
        // SlashPhi/SlashReach helpers so the visible blade and the hitbox always agree.
        int SlashWindupKind;
        int SlashWindupStartTick;
        int SlashWindupEndTick;
        bool SlashWindupActive;
        int SlashActiveKind = -1; // kind of the most recently released slash this state; -1 = none fired yet
        int SlashActiveStartTick; // AttackTimer at that release
        int SlashActiveDirection = 1; // facing locked at release; shared by sword, body, hitbox and shader
        int SlashActiveStyle;     // timing style (Style* below) of that slash; sword, hitbox and shader all read it
        // True when the armed windup starts from the PREVIOUS swing's end pose instead of the idle rest.
        // That is what lets a chained combo hold and re-cock the blade in place instead of dropping it.
        bool SlashWindupChained;
        int TimingVariant;        // Variant* of the current single swipe; rolled server-side, synced
        int ComboLength;          // steps PLANNED for the current chain (decides which step is the heavy finisher)
        int ComboEndStep;         // index of the last step actually performed; the server cuts it short if the player leaves
        int ComboKinds;           // slash kind per step, 3 bits each (step 0 in the low bits)

        // Legacy = the original linear 18-tick swing (thrust, leaps and Quietus still use it).
        // The eased styles are SwingEase.ApplyWeighted curves (same maths as Gwyn / Owl Father): a cubic
        // ease-in, then an exponential decay tail. The blade is only a hitbox while its speed is >= 30%
        // of peak (WeightedSwing.LiveTicks); the rest of the tail is the recovery, not dead time.
        public const int StyleLegacy = 0;
        public const int StyleFast = 1;   // in 7 / out 24 / k7: light flick, ~11 live ticks
        public const int StyleFlow = 2;   // in 9 / out 24 / k6.5: the combo cut, ~13 live ticks
        public const int StyleHeavy = 3;  // in 12 / out 32 / k6.5: finisher, ~18 live ticks
        const int SlashActiveTicks = 18;  // Legacy total; the projectile's timeLeft uses SlashStyleTicks
        const int SlashReturnTicks = 12;  // blade eases back to the idle rest after the last swing
        // Eased styles end past the nominal end pose (progress 1.12 = ~20 degrees further) so the ~85% of
        // the arc that is still live after the ease-out is still a full-size sweep. Follow-through ends low.
        const float EasedOvershoot = 1.12f;
        static readonly WeightedSwing[] SlashStyles =
        {
            default,
            new WeightedSwing(7, 24, 7f),
            new WeightedSwing(9, 24, 6.5f),
            new WeightedSwing(12, 32, 6.5f),
        };

        int SlashActiveLength => SlashStyleTicks(SlashActiveStyle);
        // Sword rig around NPC.Center: the HAND the blade pivots from, its idle reach, and the shared
        // vertical correction used by the body, loose sword, slash shader and collision arc.
        //
        // These were MEASURED off GravelordNito.png (the sheet with the sword painted in) by diffing it
        // against GravelordNitoAttacking.png (same body, no sword) — the difference is exactly the
        // baked sword, which sits at frame x[24..197] y[208..253], essentially horizontal (principal
        // axis -2 deg) and near-identical across walk frames 0/6/12. Fitting the loose sword sprite to
        // that (tip texX=2 lands on frame x=24) puts the grip at frame (227, 230) => 56px FORWARD of
        // and 24px BELOW NPC.Center. The old (18, -96) put it 96px ABOVE center — i.e. up at the
        // shoulder, 120px off — which is exactly the "fixed to the shoulder, not the hand" complaint.
        // Re-measure with the same diff if the art is ever re-exported.
        const float SwordPivotX = 56f;
        const float SwordPivotY = 24f;
        const float SwordIdleReach = 78f;
        // +9 combines with PreDraw's historical 7px frame-anchor allowance to place the complete rig
        // 16px below NPC.Bottom. The body, sword, slash shader and collision all consume this shared
        // correction, so lowering the art never detaches its attack geometry.
        internal const float GroundSinkPixels = 9f;
        // GravelordNitoSword.png is 250x58, tip at the left edge, hilt/pommel at the right — this is
        // the handle-grip column (measured), i.e. where the (hidden) hand actually holds the blade.
        // The sword is drawn with ITS OWN origin pinned there instead of the texture's geometric
        // center, so it ROTATES about the hand instead of orbiting its own middle around a "shoulder"
        // at radius `reach` (the old approach — that's what visibly detached the blade from the body).
        const float SwordHandleTexX = 205f;
        const float SwordHandleTexY = 29f;
        // Resting pose = blade held FORWARD and level, which is how the baked-in art draws it (the
        // measured blade runs dead horizontal from the grip out to the tip). The old -Pi ("straight
        // back") pointed it behind him, where his own silhouette hid it almost completely.
        const float IdlePhi = 0f;
        // How far the hand pivot itself is allowed to travel while swinging (see PreDraw's liftFactor)
        // — kept small because the grip must stay hidden behind the torso silhouette at all times.
        const float HandLiftMax = 55f;
        const float HandDriftMax = 8f;

        const float AttackDamageScale = 0.75f;
        static int ScaledAttackDamage(int baseDamage) => Math.Max(1,
            (int)Math.Round(baseDamage * AttackDamageScale, MidpointRounding.AwayFromZero));
        int SlashDamage => ScaledAttackDamage(23);       // 17
        int HeavySlashDamage => ScaledAttackDamage(29);  // 22
        int BoneDamage => ScaledAttackDamage(20);        // 15
        int DeathDamage => ScaledAttackDamage(24);       // 18

        bool IsMeleeState => State == AttackState.SideSweep || State == AttackState.BackhandSweep
            || State == AttackState.OverheadCleave || State == AttackState.ImpalingThrust
            || State == AttackState.TripleReaperCombo || State == AttackState.DraggingAdvance
            || State == AttackState.LeapingCleave || State == AttackState.QuietusCombo
            || State == AttackState.GravelordJudgment || State == AttackState.FollowUpSlash
            || State == AttackState.ReaperWeave || State == AttackState.ReapersPendulum;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = FrameCount;
            NPCID.Sets.TrailCacheLength[Type] = 6;
            NPCID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            NPC.width = 118;
            NPC.height = 174;
            NPC.damage = 0;
            NPC.defense = 12;
            NPC.lifeMax = 4000;
            NPC.HitSound = SoundID.NPCHit2;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.value = 91480f;
            NPC.npcSlots = 100f;
            NPC.boss = true;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.knockBackResist = 0.15f;
            NPC.aiStyle = -1;
            Music = MusicID.Boss1; // optional tsorcMusic override: Skeletron's Sandstorm track

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 70;
            // With 0.3 gravity, 6 power reaches a ~60px apex. 8.7 reaches ~126px: approximately four
            // additional tiles of potential height. The horizontal boost remains unchanged so this
            // expands vertical navigation without turning his heavy hop into a long-distance pounce.
            globalNPC.MaxJumpPower = 8.7f;
            globalNPC.MaxJumpBoost = 3f;
            // Support core: the center 4 tiles must be on solid ground (matches his ~7.4-tile width).
            // The wider sprite edges — and up to half his ~11-tile height on a downslope — are allowed
            // to sink into terrain instead of floating in the air over uneven ground.
            globalNPC.BeastSinkMaxTiles = 5;
            globalNPC.KiteRangeMin = 0f;
            globalNPC.KiteRangeMax = 24f;
            globalNPC.KiteLooseness = 0.45f;
            globalNPC.PatrolMode = NPCs.PatrolMode.Wander; // "tsorcRevamp.NPCs" would resolve to the Mod class, not the namespace
            EvasiveProfile.HeavyBeast(globalNPC);
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) => 0f;

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.HealingPotion;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)State);
            writer.Write((byte)LastAttack);
            writer.Write((byte)PreviousAttack);
            writer.Write(AttackTimer);
            writer.Write(AttackCooldown);
            writer.Write((sbyte)lockedDir);
            // lockedKind was previously never synced — StartAttack (and the ChooseVerticalKind read of
            // it) only runs where Main.netMode != MultiplayerClient, so remote clients were rendering
            // every swing at its stale default (kind 0) regardless of what the server actually chose.
            writer.Write((byte)lockedKind);
            writer.Write(NextOverheadIsRising);
            writer.Write(MarchOriginX);
            writer.Write(MarchGroundY);
            writer.Write(PhaseTwo);
            writer.Write(HalfTelegraph);
            writer.Write((short)ComboRecoveryTicks);
            writer.Write((byte)QueuedAttack);
            writer.Write((sbyte)QueuedSlashKind);
            writer.Write(MiasmaAimDirection.X);
            writer.Write(MiasmaAimDirection.Y);
            writer.Write((sbyte)SlashWindupKind);
            writer.Write(SlashWindupStartTick);
            writer.Write(SlashWindupEndTick);
            writer.Write(SlashWindupActive);
            writer.Write((sbyte)SlashActiveKind);
            writer.Write(SlashActiveStartTick);
            writer.Write((sbyte)SlashActiveDirection);
            writer.Write((short)ThrustDashTicks);
            writer.Write((byte)SlashActiveStyle);
            writer.Write(SlashWindupChained);
            writer.Write((byte)TimingVariant);
            writer.Write((byte)ComboLength);
            writer.Write((byte)ComboEndStep);
            writer.Write(ComboKinds);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            State = (AttackState)reader.ReadByte();
            LastAttack = (AttackState)reader.ReadByte();
            PreviousAttack = (AttackState)reader.ReadByte();
            AttackTimer = reader.ReadInt32();
            AttackCooldown = reader.ReadInt32();
            lockedDir = reader.ReadSByte();
            lockedKind = reader.ReadByte();
            NextOverheadIsRising = reader.ReadBoolean();
            MarchOriginX = reader.ReadSingle();
            MarchGroundY = reader.ReadSingle();
            PhaseTwo = reader.ReadBoolean();
            HalfTelegraph = reader.ReadBoolean();
            ComboRecoveryTicks = reader.ReadInt16();
            QueuedAttack = (AttackState)reader.ReadByte();
            QueuedSlashKind = reader.ReadSByte();
            MiasmaAimDirection = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            SlashWindupKind = reader.ReadSByte();
            SlashWindupStartTick = reader.ReadInt32();
            SlashWindupEndTick = reader.ReadInt32();
            SlashWindupActive = reader.ReadBoolean();
            SlashActiveKind = reader.ReadSByte();
            SlashActiveStartTick = reader.ReadInt32();
            SlashActiveDirection = reader.ReadSByte();
            ThrustDashTicks = reader.ReadInt16();
            SlashActiveStyle = reader.ReadByte();
            SlashWindupChained = reader.ReadBoolean();
            TimingVariant = reader.ReadByte();
            ComboLength = reader.ReadByte();
            ComboEndStep = reader.ReadByte();
            ComboKinds = reader.ReadInt32();
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            if (State == AttackState.None)
            {
                tsorcRevampAIs.EvasiveOnHit(NPC, true);
            }
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (State == AttackState.None)
            {
                tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
            }
        }

        public void OnStagger(NPC npc)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.85f, Pitch = -0.45f }, NPC.Center);
                for (int i = 0; i < 36; i++)
                {
                    Dust dust = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(70f, 110f), DustID.BoneTorch, Main.rand.NextVector2Circular(4f, 4f), 90, default, 1.35f);
                    dust.noGravity = true;
                }
            }

            State = AttackState.None;
            AttackTimer = 0;
            HalfTelegraph = false;
            SlashWindupActive = false;
            SlashActiveKind = -1;
            KillOwnedSwordSlashes();
            ComboRecoveryTicks = 0;
            AttackCooldown = Math.Max(AttackCooldown, 120);
            NPC.damage = 0;
            NPC.netUpdate = true;
        }

        public override void AI()
        {
            NPC.damage = 0;
            if (!HasLivingPlayer())
            {
                // No death animation or loot: this is a true encounter despawn when the party wipes.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.active = false;
                    NPC.netUpdate = true;
                }
                return;
            }
            if (NPC.target < 0 || NPC.target >= Main.maxPlayers || !Main.player[NPC.target].active || Main.player[NPC.target].dead)
            {
                NPC.TargetClosest(false);
            }

            Player player = Main.player[NPC.target];
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.AttackTelegraphing = false;
            globalNPC.AttackCommitted = false;

            // Independent SwordRain pressure: time spent at range banks up in EVERY state (a long channel
            // still counts) and drains slowly once the player closes in. It is only consumed by the
            // idle pick below, which bypasses the weighted pool and its no-immediate-repeat rule.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                float horizontalGap = Math.Abs(player.Center.X - NPC.Center.X);
                if (horizontalGap >= RangedRainMinDistance)
                {
                    RangedRainCharge++;
                }
                else
                {
                    RangedRainCharge = Math.Max(0, RangedRainCharge - 2);
                }
            }

            if (!PhaseTwo && Main.netMode != NetmodeID.MultiplayerClient && NPC.life <= NPC.lifeMax / 2)
            {
                PhaseTwo = true;
                StartAttack(AttackState.PhaseTransition, player);
            }

            if (globalNPC.StaggerTimer > 0)
            {
                NPC.rotation = MathHelper.Lerp(NPC.rotation, -NPC.direction * 0.12f, 0.08f);
                if (Main.rand.NextBool(3))
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.BoneTorch, 0f, 1.2f, 100, default, 0.9f);
                }
                UpdateAura();
                return;
            }

            NPC.rotation *= 0.9f;

            if (State == AttackState.None)
            {
                // canWalkBackwards:false — he turns to FACE his walking direction. Platforms are valid
                // footing near the player's level, but CanFallThroughPlatforms drops him through one
                // whenever it has left him meaningfully above the fight.
                tsorcRevampAIs.FighterAI(NPC, topSpeed: 0.62f, acceleration: 0.45f, canTeleport: false, lavaJumping: true, canDodgeroll: false, canPounce: false, minSurfaceWidth: 4, canWalkBackwards: false);
                StayGroundedRelativeTo(player);
                MaintainRecoveryPursuit(player);
                FootstepEffects();

                if (AttackCooldown > 0)
                {
                    AttackCooldown--;
                }

                if (Main.netMode != NetmodeID.MultiplayerClient && AttackCooldown <= 0 && NPC.velocity.Y == 0f && player.active && !player.dead && NPC.Distance(player.Center) < 1050f)
                {
                    if (!TryStartQueuedAttack(player))
                    {
                        if (RangedRainCharge >= RangedRainChargeTicks)
                        {
                            // Alternate the two sky attacks so the range pressure isn't one repeating move.
                            // StartAttack zeroes the charge for either, however it was chosen.
                            AttackState skyAttack = LastAttack == AttackState.SwordRain
                                ? AttackState.SwordLineDance
                                : AttackState.SwordRain;
                            StartAttack(skyAttack, player);
                        }
                        else
                        {
                            PickAttack(player);
                        }
                    }
                }
            }
            else
            {
                RunAttack(globalNPC, player);
            }

            UpdateAura();
        }

        void PickAttack(Player player)
        {
            float dist = NPC.Distance(player.Center);
            float horizontal = Math.Abs(player.Center.X - NPC.Center.X);
            float vertical = Math.Abs(player.Center.Y - NPC.Center.Y);
            bool sameLevel = vertical < 125f;
            List<(AttackState state, float weight)> pool = new();

            // Invalid attacks are excluded rather than left in the bag at a token weight. This is the
            // important difference between a proximity-aware selector and the old "mostly weighted"
            // bag: a stationary swipe can no longer win a far-range roll and attack empty air.
            AddAttackOption(pool, AttackState.SideSweep, horizontal <= 285f && sameLevel,
                ProximityWeight(dist, 165f, 220f, 9f));
            AddAttackOption(pool, AttackState.BackhandSweep, horizontal <= 255f && sameLevel,
                ProximityWeight(dist, 145f, 190f, 7f));
            AddAttackOption(pool, AttackState.OverheadCleave, horizontal <= 310f && vertical < 190f,
                ProximityWeight(dist, 210f, 250f, 6f));
            AddAttackOption(pool, AttackState.ImpalingThrust,
                horizontal >= ImpalingThrustMinRange && horizontal <= ImpalingThrustMaxRange && sameLevel,
                ProximityWeight(dist, 610f, 340f, 8f)); // the gap-closer: peak across its whole 410-810 band
            AddAttackOption(pool, AttackState.TripleReaperCombo, PhaseTwo && horizontal <= 370f && sameLevel,
                ProximityWeight(dist, 220f, 240f, 11f));
            // The chained combos exist in both phases (short in phase 1) and weigh more in phase 2.
            AddAttackOption(pool, AttackState.ReaperWeave, horizontal <= 330f && sameLevel,
                ProximityWeight(dist, 200f, 260f, PhaseTwo ? 11f : 8f));
            AddAttackOption(pool, AttackState.ReapersPendulum, horizontal <= 300f && sameLevel,
                ProximityWeight(dist, 170f, 230f, PhaseTwo ? 10f : 7f));
            AddAttackOption(pool, AttackState.DraggingAdvance, horizontal >= 180f && horizontal <= 700f && sameLevel,
                ProximityWeight(dist, 390f, 330f, 8f));
            AddAttackOption(pool, AttackState.LeapingCleave, vertical > 80f || horizontal >= 380f,
                ProximityWeight(dist, 520f, 480f, 7f));
            AddAttackOption(pool, AttackState.SwordRain, vertical > 90f || horizontal >= 260f,
                ProximityWeight(dist, 520f, 460f, 7f));
            AddAttackOption(pool, AttackState.SwordLineDance, horizontal >= 400f && horizontal <= 900f,
                ProximityWeight(dist, 560f, 400f, PhaseTwo ? 9f : 7f));
            AddAttackOption(pool, AttackState.BoneVolley, horizontal >= 220f && dist <= BoneVolleyMaxRange,
                ProximityWeight(dist, 480f, 420f, 6f));
            AddAttackOption(pool, AttackState.GravelordSpikes, horizontal >= 100f,
                ProximityWeight(dist, 310f, 420f, 7f));
            AddAttackOption(pool, AttackState.GravelordDance, true, PhaseTwo ? 8f : 7f);
            AddAttackOption(pool, AttackState.DeathNova, horizontal <= 480f,
                ProximityWeight(dist, 225f, 360f, PhaseTwo ? 6f : 3f));
            AddAttackOption(pool, AttackState.MiasmaBreath,
                horizontal >= 140f && horizontal <= 560f && vertical < 180f,
                ProximityWeight(dist, 335f, 280f, 6f));
            AddAttackOption(pool, AttackState.BonePillarCage,
                PhaseTwo && horizontal >= 140f && horizontal <= 620f,
                ProximityWeight(dist, 340f, 360f, 9f));
            AddAttackOption(pool, AttackState.GraveHands, horizontal >= 120f && horizontal <= 700f,
                ProximityWeight(dist, 360f, 420f, 6f));
            AddAttackOption(pool, AttackState.QuietusCombo, PhaseTwo && horizontal <= 390f && sameLevel,
                ProximityWeight(dist, 235f, 250f, 10f));
            AddAttackOption(pool, AttackState.CemeteryMarch,
                horizontal >= 160f && horizontal <= 760f && sameLevel,
                ProximityWeight(dist, 430f, 410f, PhaseTwo ? 6f : 4f));
            AddAttackOption(pool, AttackState.HollowCommand, horizontal >= 120f && horizontal <= 560f,
                ProximityWeight(dist, 300f, 330f, 5f));
            AddAttackOption(pool, AttackState.GravelordJudgment,
                PhaseTwo && (vertical > 110f || horizontal >= 300f),
                ProximityWeight(dist, 560f, 470f, 10f));

            float total = 0f;
            foreach ((AttackState state, float weight) in pool)
            {
                total += AdjustedSelectionWeight(state, weight);
            }
            if (total > 0.001f)
            {
                float roll = Main.rand.NextFloat(total);
                foreach ((AttackState state, float weight) in pool)
                {
                    float adjusted = AdjustedSelectionWeight(state, weight);
                    if (adjusted <= 0f)
                    {
                        continue;
                    }
                    roll -= adjusted;
                    if (roll <= 0f)
                    {
                        StartAttack(state, player);
                        return;
                    }
                }
            }

            // Geometry/terrain can occasionally leave only a suppressed exact repeat. Choose a valid
            // answer for the current band instead of reviving an invalid option from the random bag.
            StartAttack(FallbackAttack(horizontal, vertical, sameLevel), player);
        }

        static void AddAttackOption(List<(AttackState state, float weight)> pool, AttackState state,
            bool eligible, float weight)
        {
            if (eligible && weight > 0f)
            {
                pool.Add((state, weight));
            }
        }

        static float ProximityWeight(float distance, float idealDistance, float falloffDistance, float peakWeight)
        {
            float closeness = 1f - Math.Abs(distance - idealDistance) / Math.Max(1f, falloffDistance);
            return peakWeight * MathHelper.Lerp(0.42f, 1f, MathHelper.Clamp(closeness, 0f, 1f));
        }

        float AdjustedSelectionWeight(AttackState state, float weight)
        {
            if (state == LastAttack)
            {
                return 0f;
            }

            AttackFamily family = FamilyOf(state);
            if (family == AttackFamily.SwordSingle || family == AttackFamily.SwordSequence
                || family == AttackFamily.GapCloser)
            {
                // Melee is Nito's primary pressure language. Ranged casts now hand off into it while
                // their projectile-owned hazards continue, breaking up the old cast/cast cadence.
                weight *= PhaseTwo ? 2.55f : 2.25f;
            }
            if (family == AttackFamily.SwordSequence && PhaseTwo)
            {
                weight *= 1.5f; // phase 2 throws combos noticeably more often
            }
            if (family != AttackFamily.None && family == FamilyOf(LastAttack))
            {
                weight *= 0.32f;
            }
            if (family != AttackFamily.None && family == FamilyOf(PreviousAttack))
            {
                weight *= 0.68f;
            }
            return weight;
        }

        static AttackFamily FamilyOf(AttackState state) => state switch
        {
            AttackState.SideSweep or AttackState.BackhandSweep or AttackState.OverheadCleave
                or AttackState.ImpalingThrust => AttackFamily.SwordSingle,
            AttackState.TripleReaperCombo or AttackState.QuietusCombo or AttackState.FollowUpSlash
                or AttackState.ReaperWeave or AttackState.ReapersPendulum => AttackFamily.SwordSequence,
            AttackState.DraggingAdvance or AttackState.LeapingCleave => AttackFamily.GapCloser,
            AttackState.SwordRain or AttackState.GravelordJudgment or AttackState.SwordLineDance
                => AttackFamily.OverheadRain,
            AttackState.GravelordSpikes or AttackState.GravelordDance or AttackState.BonePillarCage
                or AttackState.GraveHands or AttackState.CemeteryMarch => AttackFamily.TargetedGround,
            AttackState.BoneVolley or AttackState.HollowCommand => AttackFamily.ProjectileVolley,
            AttackState.DeathNova => AttackFamily.AreaBurst,
            AttackState.MiasmaBreath => AttackFamily.Miasma,
            _ => AttackFamily.None,
        };

        static AttackState FallbackAttack(float horizontal, float vertical, bool sameLevel)
        {
            if (vertical > 80f)
            {
                return AttackState.LeapingCleave;
            }
            if (horizontal > 700f)
            {
                return AttackState.SwordRain;
            }
            if (horizontal >= ImpalingThrustMinRange && horizontal <= ImpalingThrustMaxRange && sameLevel)
            {
                return AttackState.ImpalingThrust;
            }
            if (horizontal > 360f && sameLevel)
            {
                return AttackState.DraggingAdvance;
            }
            return AttackState.SideSweep;
        }

        bool TryStartQueuedAttack(Player player)
        {
            if (QueuedAttack == AttackState.None)
            {
                return false;
            }

            AttackState queued = QueuedAttack;
            int queuedSlashKind = QueuedSlashKind;
            QueuedAttack = AttackState.None;
            QueuedSlashKind = -1;
            if (!IsQueuedAttackEligible(queued, player))
            {
                NPC.netUpdate = true;
                return false;
            }

            StartAttack(queued, player);
            if (queued == AttackState.FollowUpSlash)
            {
                lockedKind = Math.Clamp(queuedSlashKind, 0, 4);
                NPC.netUpdate = true;
            }
            return true;
        }

        void QueueFollowUp(AttackState followUp, Player player)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !IsQueuedAttackEligible(followUp, player))
            {
                return;
            }
            QueuedAttack = followUp;
            QueuedSlashKind = -1;
            NPC.netUpdate = true;
        }

        void TryQueueMeleeFollowUp(Player player, int priorKind)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !PhaseTwo
                || QueuedAttack != AttackState.None || Main.rand.NextFloat() >= 0.72f)
            {
                return;
            }

            float horizontal = Math.Abs(player.Center.X - NPC.Center.X);
            float vertical = Math.Abs(player.Center.Y - NPC.Center.Y);
            if (vertical >= 190f || horizontal > ImpalingThrustMaxRange + 30f)
            {
                return;
            }

            int alternateKind = priorKind switch
            {
                0 => 3,
                1 => 4,
                3 => 0,
                4 => 1,
                _ => NextOverheadIsRising ? 4 : 1,
            };

            bool lungeEligible = priorKind != 2 && horizontal >= ImpalingThrustMinRange
                && horizontal <= ImpalingThrustMaxRange + 30f;
            int followUpKind;
            if (horizontal > 330f)
            {
                if (!lungeEligible)
                {
                    return;
                }
                followUpKind = 2;
            }
            else if (lungeEligible && Main.rand.NextBool(3))
            {
                followUpKind = 2;
            }
            else
            {
                followUpKind = alternateKind;
            }

            QueuedAttack = AttackState.FollowUpSlash;
            QueuedSlashKind = followUpKind;
            NPC.netUpdate = true;
        }

        bool IsQueuedAttackEligible(AttackState state, Player player)
        {
            float horizontal = Math.Abs(player.Center.X - NPC.Center.X);
            float vertical = Math.Abs(player.Center.Y - NPC.Center.Y);
            bool sameLevel = vertical < 135f;
            return state switch
            {
                AttackState.ImpalingThrust => horizontal >= ImpalingThrustMinRange - 20f
                    && horizontal <= ImpalingThrustMaxRange + 30f && sameLevel,
                AttackState.DraggingAdvance => horizontal >= 160f && horizontal <= 740f && sameLevel,
                AttackState.OverheadCleave => horizontal <= 330f && vertical < 190f,
                AttackState.ReaperWeave or AttackState.ReapersPendulum => horizontal <= 340f && sameLevel,
                AttackState.QuietusCombo => PhaseTwo && horizontal <= 410f && sameLevel,
                AttackState.BonePillarCage => PhaseTwo && horizontal >= 120f && horizontal <= 650f,
                AttackState.GravelordJudgment => PhaseTwo && (vertical > 90f || horizontal >= 260f),
                AttackState.FollowUpSlash => PhaseTwo
                    && horizontal <= ImpalingThrustMaxRange + 30f && vertical < 190f,
                _ => true,
            };
        }

        void RunAttack(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            AttackTimer++;
            switch (State)
            {
                case AttackState.SideSweep:
                    RunSwordAttack(globalNPC, player, lockedKind, 30, 44, StyleFlow, SlashDamage);
                    break;

                case AttackState.BackhandSweep:
                    RunSwordAttack(globalNPC, player, lockedKind, 26, 38, StyleFlow, SlashDamage);
                    break;

                case AttackState.OverheadCleave:
                    RunSwordAttack(globalNPC, player, lockedKind, HeavyTelegraph, 64, StyleHeavy, HeavySlashDamage);
                    break;

                case AttackState.ImpalingThrust:
                    RunImpalingThrust(globalNPC, player, ThrustTelegraphTicks, true);
                    break;

                case AttackState.TripleReaperCombo:
                case AttackState.ReaperWeave:
                case AttackState.ReapersPendulum:
                    RunSwordChain(globalNPC, player);
                    break;

                case AttackState.SwordLineDance:
                    RunSwordLineDance(globalNPC, player);
                    break;

                case AttackState.DraggingAdvance:
                    RunDraggingAdvance(globalNPC, player);
                    break;

                case AttackState.LeapingCleave:
                    RunLeapingCleave(globalNPC, player);
                    break;

                case AttackState.SwordRain:
                    RunSwordRain(globalNPC, player);
                    break;

                case AttackState.BoneVolley:
                    RunBoneVolley(globalNPC, player);
                    break;

                case AttackState.GravelordSpikes:
                    RunGravelordSpikes(globalNPC, player);
                    break;

                case AttackState.GravelordDance:
                    RunGravelordDance(globalNPC, player);
                    break;

                case AttackState.DeathNova:
                    RunDeathNova(globalNPC);
                    break;

                case AttackState.MiasmaBreath:
                    RunMiasmaBreath(globalNPC, player);
                    break;

                case AttackState.BonePillarCage:
                    RunBonePillarCage(globalNPC, player);
                    break;

                case AttackState.GraveHands:
                    RunGraveHands(globalNPC, player);
                    break;

                case AttackState.QuietusCombo:
                    RunQuietusCombo(globalNPC, player);
                    break;

                case AttackState.CemeteryMarch:
                    RunCemeteryMarch(globalNPC, player);
                    break;

                case AttackState.HollowCommand:
                    RunHollowCommand(globalNPC, player);
                    break;

                case AttackState.GravelordJudgment:
                    RunGravelordJudgment(globalNPC, player);
                    break;

                case AttackState.FollowUpSlash:
                    if (lockedKind == 2)
                    {
                        // A follow-up thrust is the same lunge as ImpalingThrust (it can start 400+ px
                        // away), so it uses the same distance-solved dash AND the same 34-tick tell with
                        // the white flash (it was 26, too short for the flash's 30-tick lead).
                        RunImpalingThrust(globalNPC, player, ThrustTelegraphTicks, false);
                        break;
                    }
                    RunSwordAttack(globalNPC, player, lockedKind, 22, 30, StyleFlow, SlashDamage, canQueueFollowUp: false);
                    break;

                case AttackState.PhaseTransition:
                    RunPhaseTransition(globalNPC, player);
                    break;

                case AttackState.ComboRecovery:
                    RunComboRecovery();
                    break;

            }
        }

        // Approach speed while cocking a swing, and the forward step a swing releases with (before the
        // phase/SwipePush multipliers). Shared by the single swipes and the chain engine.
        static float SwipeApproachSpeed(int kind)
        {
            if (kind == 1)
            {
                return 1.9f;
            }
            if (kind == 3)
            {
                return 2.15f;
            }
            return 2.4f;
        }

        static float SwipePush(int kind)
        {
            if (kind == 1)
            {
                return 2.8f;
            }
            if (kind == 3)
            {
                return 3.25f;
            }
            return 3.8f;
        }

        ///<summary>One committed swipe (side / backhand / overhead / follow-up) with a rolled timing
        ///variant so the same three moves don't have one rhythm. Timeline (ticks from AttackTimer 1):
        ///  Standard: the authored telegraph, release gap and style (overhead is Heavy, the rest Flow).
        ///  Quick:    20t tell, released 8t later, Fast style: a snappy flick.
        ///  Delayed:  authored tell + 14t, then a 22t held beat before a Fast slash. The windup eases in
        ///            (long settle into the cock pose) and the blade then waits: the bait.
        ///The recovery is the eased tail of the swing itself plus a short blade-return to idle, not a
        ///separate dead beat.</summary>
        void RunSwordAttack(tsorcRevampGlobalNPC globalNPC, Player player, int slashKind, int standardTelegraph,
            int standardRelease, int standardStyle, int damage, bool canQueueFollowUp = true)
        {
            int releaseGap = standardRelease - standardTelegraph;
            int telegraphTicks = Telegraph(standardTelegraph);
            int style = standardStyle;

            if (TimingVariant == VariantQuick)
            {
                telegraphTicks = Telegraph(20);
                releaseGap = 8;
                style = StyleFast;
            }
            else if (TimingVariant == VariantDelayed)
            {
                telegraphTicks = Telegraph(standardTelegraph + 14);
                releaseGap = 22;
                style = StyleFast;
            }

            int releaseTick = telegraphTicks + releaseGap;
            int endTick = releaseTick + SlashStyleTicks(style) + SlashReturnTicks;

            if (AttackTimer <= releaseTick)
            {
                globalNPC.AttackCommitted = true;
            }
            if (AttackTimer == 1)
            {
                TelegraphCue(Color.LightGray);
                ArmSlashWindup(slashKind, 1, releaseTick);
            }
            if (AttackTimer <= telegraphTicks)
            {
                // He is still slow, but a sword windup is now an advancing threat instead of a planted
                // animation. Facing remains live during this readable approach, then locks for the final
                // release gap so a last-moment cross-up can evade rather than rotating the hitbox unfairly.
                AdvanceTowardPlayer(player, SwipeApproachSpeed(slashKind), 0.16f, updateFacing: true);
                SwordTelegraphDust(slashKind);
            }
            else if (AttackTimer < releaseTick)
            {
                NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, lockedDir * 0.9f, 0.12f);
            }
            if (AttackTimer == releaseTick)
            {
                // The active arc rides this whole-body step because NitoSwordSlash reads owner.Center
                // every tick. The blade and hit geometry therefore stay together without inventing an
                // articulated body pose the fixed sheet cannot make.
                NPC.velocity.X = lockedDir * SwipePush(slashKind) * SwipePushScale;
                SpawnSlash(slashKind, damage, style);
                NPC.netUpdate = true;
            }
            else if (AttackTimer > releaseTick)
            {
                NPC.velocity.X *= 0.9f;
            }
            if (AttackTimer >= endTick)
            {
                if (canQueueFollowUp)
                {
                    TryQueueMeleeFollowUp(player, slashKind);
                }
                EndAttack(46);
            }
        }

        ///<summary>The combo engine: a chain of 1-6 swipes with ONE telegraph up front, a short held beat
        ///between cuts, and no separate recovery per cut: the ease-out tail of each swing IS its
        ///recovery. Used by ReaperWeave (random pole-to-pole chain), ReapersPendulum (strict back and
        ///forth) and TripleReaperCombo (fixed 0-3-1).
        ///
        ///WHY THEY CHAIN: every swipe is an arc between two poles. Kinds 0 and 1 start at the UP-BACK
        ///pole (phi about -2.2) and end DOWN-FORWARD (about +0.65); kinds 3 and 4 do the reverse. So
        ///any cut that ends at one pole can be followed by ANY cut that starts there: the blade never
        ///snaps between cuts, it just rests a beat at the pole (the held windup) and swings back.
        ///
        ///TIMELINE (ticks): tell = Telegraph(base); cut n releases at cursor; its tail ends
        ///cursor + style ticks; the next cut releases `hold` ticks later (chained windup from the
        ///end pose). Flow cut 33t + hold 10 = 43t period; Fast cut 31t + hold 8 = 39t; the finisher is
        ///Heavy (44t tail). Fairness (attack-timing-design 2): live windows are ~11-18t (< the 22t
        ///roll); the next cut's blade doesn't reach the player until 6-12t after its release, i.e.
        ///more than 30t after the previous live window closed, so a late roller has their next roll.
        ///The hold re-faces the player, so rolling through him costs exactly the current cut.
        ///
        ///The server may cut the chain short (ComboEndStep) when the player leaves ComboContinueRange;
        ///clients follow the synced value. ComboLength stays the planned length so the cut-short last cut
        ///keeps the style it was thrown with.</summary>
        void RunSwordChain(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            int baseTelegraph = 28;
            int hold = 10;

            if (State == AttackState.ReaperWeave)
            {
                baseTelegraph = 30;
            }
            else if (State == AttackState.ReapersPendulum)
            {
                baseTelegraph = 26;
                hold = 8;
            }

            int telegraphTicks = Telegraph(baseTelegraph);
            int plannedLength = Math.Clamp(ComboLength, 1, MaxComboSteps);
            int stepCount = Math.Clamp(ComboEndStep + 1, 1, plannedLength);
            bool fastChain = State == AttackState.ReapersPendulum;
            int firstKind = ComboKinds & 7;

            if (AttackTimer == 1)
            {
                TelegraphCue(new Color(180, 180, 210));
                ArmSlashWindup(firstKind, 1, telegraphTicks);
            }
            if (AttackTimer <= telegraphTicks)
            {
                // Same readable advancing tell as a single swipe. Facing stays live until 8 ticks before
                // the release, then locks so a last-moment roll-through can't spin the first cut around.
                AdvanceTowardPlayer(player, SwipeApproachSpeed(firstKind), 0.16f,
                    updateFacing: AttackTimer <= telegraphTicks - 8);
                SwordTelegraphDust(firstKind);
            }

            // Walk the timeline. Every value is derived from AttackTimer + the synced script, so a
            // client needs no extra state to follow it.
            int cursor = telegraphTicks;
            int lastTailEnd = 0;
            for (int step = 0; step < stepCount; step++)
            {
                // lastStep = the last cut actually thrown (the server may cut the chain short);
                // plannedFinisher = the cut the script made the heavy one. They only differ after a cut-short.
                bool lastStep = step == stepCount - 1;
                bool plannedFinisher = step == plannedLength - 1;
                int kind = (ComboKinds >> (3 * step)) & 7;
                int style = StyleFlow;

                if (fastChain && !plannedFinisher)
                {
                    style = StyleFast;
                }
                else if (plannedFinisher && !fastChain)
                {
                    style = StyleHeavy;
                }

                int tailEnd = cursor + SlashStyleTicks(style);
                int nextRelease = tailEnd + hold;

                if (AttackTimer == cursor)
                {
                    int damage = lastStep ? HeavySlashDamage : SlashDamage;
                    NPC.velocity.X = lockedDir * SwipePush(kind) * SwipePushScale;
                    SpawnSlash(kind, damage, style);
                    NPC.netUpdate = true;
                }
                else if (AttackTimer > cursor && AttackTimer < tailEnd)
                {
                    NPC.velocity.X *= 0.9f; // the release step bleeds off through the follow-through
                }

                if (!lastStep && AttackTimer == tailEnd)
                {
                    // Between cuts. Only the server decides to stop early (clients follow ComboLength).
                    float horizontal = Math.Abs(player.Center.X - NPC.Center.X);
                    float vertical = Math.Abs(player.Center.Y - NPC.Center.Y);
                    bool inRange = horizontal <= ComboContinueRange && vertical < ComboContinueHeight;

                    if (!inRange && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        ComboEndStep = step;
                        NPC.netUpdate = true;
                    }
                    else
                    {
                        int nextKind = (ComboKinds >> (3 * (step + 1))) & 7;
                        FacePlayer(player);
                        ArmSlashWindup(nextKind, tailEnd, nextRelease, chained: true);
                    }
                }
                else if (!lastStep && AttackTimer > tailEnd && AttackTimer < nextRelease)
                {
                    // The held beat: keep closing (this is where phase 2's harder approach scale
                    // matters), with dust on the blade so the next cut is telegraphed. He re-faces
                    // during the first half of the hold only, then the aim is locked for the release.
                    int nextKind = (ComboKinds >> (3 * (step + 1))) & 7;
                    AdvanceTowardPlayer(player, SwipeApproachSpeed(nextKind), 0.16f,
                        updateFacing: AttackTimer <= tailEnd + hold / 2);
                    SwordTelegraphDust(nextKind);
                }

                lastTailEnd = tailEnd;
                cursor = nextRelease;
            }

            globalNPC.AttackCommitted = AttackTimer <= lastTailEnd - 8;

            int endTick = lastTailEnd + SlashReturnTicks;
            if (AttackTimer >= endTick)
            {
                EndAttack(42);
            }
        }

        ///<summary>A committed forward LUNGE rather than the old stationary poke — sells the thrust the
        ///way a real gap-closer reads (and a whiffed one leaves him overextended, a fair punish window)
        ///instead of a blade that merely stretches in place. NitoSwordSlash's hitbox already re-reads
        ///owner.Center every frame, so physically dashing Nito carries the whole thrust arc with him;
        ///no hitbox-side change needed. Kept as its own method (not RunSwordAttack) because it needs to
        ///drive velocity itself instead of just braking.</summary>
        void RunImpalingThrust(tsorcRevampGlobalNPC globalNPC, Player player, int baseTelegraph, bool canQueueFollowUp)
        {
            // The lunge used to be a fixed 7.8 px/tick spring that bled off at 0.94/tick: ~120px of
            // travel, on an attack that starts 410-810px away. It now solves its own distance: cruise
            // at DashSpeed until the blade is in reach, release the slash there, then let the same
            // 0.94 decay carry him the last stretch into the player.
            //
            // Reach: the hitbox line runs 60px behind to 170px ahead of the blade centre, which sits
            // SlashReach (60 -> 180) past the hand, 56px in front of NPC.Center. So the tip is 286px out
            // at release and 406px out 18 ticks later; stopping the dash 210px short puts a player who
            // stayed put inside the blade's length from the first live tick.
            // Counter: live window is 18 ticks (< the 22-tick roll), so rolling through him is clean.
            // Backing away is NOT a counter at 11 px/tick. (The blade line sits ~54px above the floor, a
            // 34px band, so a jump only clears it at the very top of the arc: the roll is the answer.)
            const float DashSpeed = 11f;        // px/tick cruise; the player's roll is 8 px/tick
            const float DashAccel = 0.35f;      // lerp toward DashSpeed; reads as a spring, not a teleport
            const int DashRampTicks = 2;        // the lerp ramp costs ~20px of travel = ~2 cruise ticks
            const int MaxDashTicks = 75;        // 75 * 11 = 825px: covers ImpalingThrustMaxRange from a standing start
            const int FollowThroughTicks = 38;  // release -> end (was 44 -> 82)
            const float ThrustStandOff = 210f;  // dash stops this far short of the target's predicted X
            const float CleaveStandOff = 140f;  // vertical read can turn this into a cleave (kind 1/4): shorter reach
            const float TargetLeadTicks = 18f;  // lead the player's velocity, same as LeapingCleave

            int telegraphTicks = Telegraph(baseTelegraph);
            int aimLockTick = Math.Max(16, telegraphTicks - 11);
            int dashStartTick = telegraphTicks + 1;

            if (AttackTimer == dashStartTick)
            {
                // Solved once, on every machine (the server's value wins on the next sync). Distance is
                // measured forward along the locked facing, so a player who slipped behind him during
                // the locked telegraph gives a negative gap -> 0 travel -> he swings at once, in place.
                float standOff = lockedKind == 2 ? ThrustStandOff : CleaveStandOff;
                float targetX = player.Center.X + player.velocity.X * TargetLeadTicks;
                float forwardGap = (targetX - NPC.Center.X) * lockedDir;
                float travel = Math.Max(0f, forwardGap - standOff);
                int cruiseTicks = (int)Math.Ceiling(travel / DashSpeed);

                ThrustDashTicks = Math.Clamp(cruiseTicks + DashRampTicks, DashRampTicks, MaxDashTicks);
                NPC.netUpdate = true;
            }

            int releaseTick = dashStartTick + ThrustDashTicks;
            int endTick = releaseTick + FollowThroughTicks;

            if (AttackTimer <= releaseTick)
            {
                globalNPC.AttackCommitted = true;
            }
            if (AttackTimer == 1)
            {
                TelegraphCue(Color.LightGray);
                ArmSlashWindup(lockedKind, 1, telegraphTicks + 10);
            }
            if (AttackTimer == ThrustFlashStartTick)
            {
                // The sword flashes white (drawn in PreDraw from the same tick math). Add a sharp cue and a
                // burst of white sparks along the blade so the flash is heard as well as seen.
                SoundEngine.PlaySound(SoundID.Item60 with { Volume = 0.7f, Pitch = 0.25f }, NPC.Center);

                Vector2 flashHilt = NPC.Center + new Vector2(lockedDir * SwordPivotX, SwordPivotY + GroundSinkPixels);
                for (int i = 0; i < 10; i++)
                {
                    Vector2 sparkPosition = flashHilt + new Vector2(lockedDir * Main.rand.NextFloat(20f, 200f), Main.rand.NextFloat(-6f, 6f));
                    Dust spark = Dust.NewDustPerfect(sparkPosition, DustID.WhiteTorch,
                        new Vector2(lockedDir * Main.rand.NextFloat(0.5f, 2.2f), Main.rand.NextFloat(-1.4f, 0.2f)),
                        60, default, Main.rand.NextFloat(1f, 1.4f));
                    spark.noGravity = true;
                }
            }

            if (AttackTimer <= aimLockTick)
            {
                // Walk the coil into useful thrust range. The final eleven ticks are locked and still,
                // preserving a clear dodge point before the lunge begins.
                AdvanceTowardPlayer(player, 2.55f, 0.18f, updateFacing: true);
                SwordTelegraphDust(lockedKind);
            }
            else if (AttackTimer <= telegraphTicks)
            {
                NPC.velocity.X *= 0.8f;
                SwordTelegraphDust(lockedKind);
            }
            else if (AttackTimer <= releaseTick)
            {
                // The dash: cruise toward the locked side with the blade dust still on the sword and a
                // kicked-up flash at his feet every 4 ticks, so the charge reads as one committed move.
                NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, lockedDir * DashSpeed, DashAccel);
                SwordTelegraphDust(lockedKind);
                FootstepEffects();
                if (AttackTimer % 4 == 0)
                {
                    tsorcRevampAIs.SpawnLeapTelegraph(NPC, new Color(18, 2, 7));
                }
            }
            else
            {
                // Momentum carries him through the strike, bleeding off gradually instead of snapping
                // to a stop: he finishes the lunge next to the target, not a screen away from it.
                NPC.velocity.X *= 0.94f;
            }

            if (AttackTimer == releaseTick)
            {
                SpawnSlash(lockedKind, SlashDamage);
            }
            if (AttackTimer >= endTick)
            {
                // The dash only exists to close distance; the chained swipes are the payoff, thrown once
                // he is in range. Phase 2 always follows up, phase 1 usually does. The chain itself checks
                // range (IsQueuedAttackEligible), so a dodged dash that ends far away queues nothing.
                if (canQueueFollowUp && Main.netMode != NetmodeID.MultiplayerClient
                    && (PhaseTwo || Main.rand.NextFloat() < 0.6f))
                {
                    QueueFollowUp(AttackState.ReaperWeave, player);
                }
                EndAttack(70);
            }
        }

        ///<summary>Ported (conceptually — Nito isn't on the Invader/PuppetNPC rig) from
        ///StuddedLeatherWarrior's LowAxeRun -> RisingUppercutLeap combo: drag the blade in at a capped
        ///chase speed, then launch a ballistically-timed rising leap-uppercut so it lands ON the player
        ///instead of falling short or overshooting. The correct read for the player is to dodge INTO
        ///him (roll through/toward), not retreat — he's closing ground, not swinging in place.
        ///
        ///Kind 4 ("rising cut") does double duty for both halves: its own START pose (progress=0) is
        ///already a held-low, angled-down-and-forward stance — which matters because the sword is a
        ///background layer drawn behind the body. The old repeated side-sweep's REST pose pointed
        ///straight back (idlePhi = -Pi), which would sit almost entirely hidden behind his own
        ///silhouette for the whole run-in — an unreadable telegraph. Holding kind 4's start pose keeps
        ///the blade visibly out front the entire chase, and its full arc (down-forward -> up-back) IS
        ///the rising uppercut, so releasing it at the leap is a seamless continuation of the held pose
        ///rather than a snap to a different angle.</summary>
        void RunDraggingAdvance(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            const float RunSpeed = 3.6f;       // capped chase speed while dragging the blade in
            const float RunAccel = 0.3f;
            const int RunWindupTicks = 20;      // blade eases from idle into the held drag pose
            const int RunMaxTicks = 110;        // safety: force the leap even if never quite in range
            const float UppercutRange = 130f;   // distance the run gives way to the leap-uppercut at
            const float UppercutUpSpeed = 8.5f;
            const float UppercutForwardMin = 3.4f; // never a half-hearted lunge
            const float UppercutForwardMax = 8.5f; // never so fast it reads as unfair/undodgeable

            globalNPC.AttackCommitted = true; // fully committed the whole way through — chasing or mid-air, not swinging in place

            if (!DragRunLeapLaunched)
            {
                if (AttackTimer == 1)
                {
                    TelegraphCue(new Color(170, 170, 190));
                    ArmSlashWindup(4, 1, RunWindupTicks); // eases to kind 4's down-forward start pose, then holds
                }
                FacePlayer(player); // re-face every tick — he's actively chasing, not committed to a fixed line
                float runSpeed = RunSpeed * ApproachScale; // same per-phase boost as AdvanceTowardPlayer
                NPC.velocity.X = MathHelper.Clamp(MathHelper.Lerp(NPC.velocity.X, lockedDir * runSpeed, RunAccel), -runSpeed, runSpeed);
                DragDust();

                bool inRange = NPC.Distance(player.Center) <= UppercutRange;
                if ((inRange && AttackTimer >= RunWindupTicks) || AttackTimer >= RunMaxTicks)
                {
                    // Ballistically timed so the leap's flight covers the remaining gap instead of
                    // falling short or blowing past the player — same approach as the Invader system's
                    // BeginRisingUppercutLeap (2*upSpeed/gravity airtime, solve forward speed for dx).
                    float dx = Math.Abs(player.Center.X - NPC.Center.X);
                    float airtime = 2f * UppercutUpSpeed / Math.Max(NPC.gravity, 0.1f);
                    float forwardSpeed = MathHelper.Clamp(dx / airtime, UppercutForwardMin, UppercutForwardMax);
                    NPC.velocity = new Vector2(lockedDir * forwardSpeed, -UppercutUpSpeed);
                    DragRunLeapLaunched = true;
                    SpawnSlash(4, HeavySlashDamage);
                    NPC.netUpdate = true;
                }
            }
            // Preserve the complete 18-tick slash before entering recovery. This keeps the loose sword,
            // shader and hit line together even if the ballistic arc lands unusually early.
            else if (AttackTimer > SlashActiveStartTick + SlashActiveLength && NPC.collideY)
            {
                // Landed — the invisible hitbox already rode NPC.Center through the whole flight, and
                // the blade eases back to idle on its own via PreDraw's SlashReturnTicks window.
                UsefulFunctions.ScreenShake(NPC.Bottom, 3f, 8, 5f, 350f);
                TryQueueMeleeFollowUp(player, 4);
                EndAttack(90);
            }
            // else: still airborne mid-arc — physics + PreDraw handle everything else this tick.

            if (AttackTimer >= 220)
            {
                EndAttack(90);  // absolute safety valve if he somehow never lands
            }
        }

        void RunLeapingCleave(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            globalNPC.AttackCommitted = AttackTimer <= 82;
            if (AttackTimer == 1)
            {
                TelegraphCue(Color.White);
                ArmSlashWindup(1, 1, 60);
            }
            if (AttackTimer < 28)
            {
                FacePlayer(player);
                NPC.velocity.X *= 0.75f;
                SwordTelegraphDust(1);
            }
            if (AttackTimer == 28)
            {
                // Solve the leap against the player's current/near-future horizontal position instead
                // of always using the same 5 px/tick hop. Aim locks here; later movement can still make
                // the cleave whiff, which is the intended punish for a successful dodge.
                float upSpeed = 8.8f;
                float airtime = 2f * upSpeed / Math.Max(NPC.gravity, 0.1f);
                float targetX = player.Center.X + player.velocity.X * 18f;
                lockedDir = targetX >= NPC.Center.X ? 1 : -1;
                NPC.direction = lockedDir;
                NPC.spriteDirection = lockedDir;
                float forwardSpeed = MathHelper.Clamp(Math.Abs(targetX - NPC.Center.X) / airtime, 4.2f, 8.5f);
                NPC.velocity = new Vector2(lockedDir * forwardSpeed, -upSpeed);
                NPC.netUpdate = true;
            }
            if (AttackTimer == 60)
            {
                SpawnSlash(1, HeavySlashDamage);
            }
            if (AttackTimer > SlashActiveStartTick + SlashActiveLength && NPC.collideY)
            {
                UsefulFunctions.ScreenShake(NPC.Bottom, 5f, 12, 6f, 500f);
                SpawnGroundSpike(NPC.Bottom + new Vector2(lockedDir * 72f, 0f), 12, 1.2f);
                SpawnGroundSpike(NPC.Bottom + new Vector2(lockedDir * 132f, 0f), 18, 1f);
                TryQueueMeleeFollowUp(player, 1);
                EndAttack(110);
            }
            if (AttackTimer >= 160)
            {
                EndAttack(110);
            }
        }

        void RunSwordRain(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            int cast = Telegraph(38);
            const int RainTicks = 50;
            const int RainInterval = 10;
            globalNPC.AttackCommitted = AttackTimer <= cast + RainTicks;
            if (AttackTimer == 1)
            {
                TelegraphCue(new Color(160, 160, 210));
            }
            if (AttackTimer < cast)
            {
                NPC.velocity.X *= 0.8f;
                if (Main.rand.NextBool(2))
                {
                    Dust.NewDust(player.position + new Vector2(Main.rand.NextFloat(-180f, 180f), -260f), 8, 8, DustID.BoneTorch, 0f, 1f, 90, default, 1f);
                }
            }
            if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer >= cast
                && AttackTimer <= cast + RainTicks && (AttackTimer - cast) % RainInterval == 0)
            {
                Vector2 pos = player.Center + new Vector2(Main.rand.NextFloat(-220f, 220f), -330f);
                Vector2 velocity = UsefulFunctions.Aim(pos, player.Center + new Vector2(Main.rand.NextFloat(-40f, 40f), 0f), 8.5f);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), pos, velocity, ModContent.ProjectileType<NitoCeilingSpike>(), BoneDamage, 1f, Main.myPlayer, 14f);
            }
            if (AttackTimer >= cast + RainTicks + 8)
            {
                QueueMeleePressure(player);
                EndAttack(80);
            }
        }

        ///<summary>SWORD LINE DANCE: Sword Rain's sky spikes, laid out as a wall that WALKS IN from the far
        ///side of the player toward Nito. Purpose: punish standing off at range by herding the target
        ///toward him (the follow-up is always melee pressure).
        ///
        ///Spec card: rows 2 (phase 1) / 3 (phase 2), 60t apart. Each row = 6 columns, 72px apart, first
        ///column 300px BEHIND the player (the outside edge), marching inward toward Nito, so the row
        ///covers +300 -> -60px around the player's position at the row's spawn. Each column = 2 spikes
        ///at +-18px (a ~50px block with ~22px gaps: too tight to stand in). Column n lands 24 + 9n ticks
        ///after its row spawns, so the wave front travels 72/9 = 8 px/tick: about a player's run speed,
        ///i.e. you can stay ahead of it by walking toward Nito but not by standing still. Every spike
        ///owns its portal telegraph (NitoCeilingSpike delay, 24t minimum) directly above where it falls,
        ///so the tell is exactly the danger column. Each row is re-anchored on the player's NEW position
        ///and away-from-Nito side, so row 2 pushes again from wherever they retreated to. Per-player: the
        ///whole layout repeats for every living player within 1600px. Spikes start straight above the
        ///landing point (330px up, less under a low ceiling; a column under a wall is skipped).</summary>
        void RunSwordLineDance(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            const int ColumnsPerRow = 6;
            const float ColumnSpacing = 72f;
            const float OutsideMargin = 300f;
            const float SpikeSpread = 18f;
            const int ColumnStagger = 9;
            const int ColumnPortalTicks = 24;
            const int RowGap = 60;
            const float SpawnHeight = 330f;
            const float MinSpawnHeight = 60f;

            int cast = Telegraph(34);
            int rows = PhaseTwo ? 3 : 2;
            int lastRowTick = cast + RowGap * (rows - 1);
            globalNPC.AttackCommitted = AttackTimer <= lastRowTick;

            if (AttackTimer == 1)
            {
                TelegraphCue(new Color(160, 160, 210));
            }
            if (AttackTimer < cast)
            {
                FacePlayer(player);
                NPC.velocity.X *= 0.8f;

                // Forecast the first row: bone dust in the sky along the line the columns will fill.
                float forecastAway = player.Center.X >= NPC.Center.X ? 1f : -1f;
                float forecastX = player.Center.X + forecastAway * OutsideMargin
                    - forecastAway * ColumnSpacing * Main.rand.Next(ColumnsPerRow);
                Dust dust = Dust.NewDustPerfect(new Vector2(forecastX, player.Center.Y - SpawnHeight + Main.rand.NextFloat(-14f, 14f)),
                    DustID.BoneTorch, new Vector2(0f, Main.rand.NextFloat(0.4f, 1.4f)), 90, default, 1.1f);
                dust.noGravity = true;
            }

            int ticksSinceCast = AttackTimer - cast;
            if (Main.netMode != NetmodeID.MultiplayerClient && ticksSinceCast >= 0
                && AttackTimer <= lastRowTick && ticksSinceCast % RowGap == 0)
            {
                for (int playerIndex = 0; playerIndex < Main.maxPlayers; playerIndex++)
                {
                    Player target = Main.player[playerIndex];
                    if (!target.active || target.dead || NPC.Distance(target.Center) > 1600f)
                    {
                        continue;
                    }

                    float awayFromBoss = target.Center.X >= NPC.Center.X ? 1f : -1f;
                    float anchorX = target.Center.X + awayFromBoss * OutsideMargin;

                    for (int column = 0; column < ColumnsPerRow; column++)
                    {
                        float columnX = anchorX - awayFromBoss * ColumnSpacing * column;
                        int delay = ColumnPortalTicks + column * ColumnStagger;

                        // Stop 32px short of any ceiling so a spike never starts inside stone. A column
                        // under a wall or a very low ceiling would start buried, so it is skipped.
                        float spawnHeight = SpawnHeight;
                        for (float probeHeight = 48f; probeHeight <= SpawnHeight; probeHeight += 16f)
                        {
                            Vector2 probe = new Vector2(columnX - 7f, target.Center.Y - probeHeight);
                            if (Collision.SolidCollision(probe, 14, 14))
                            {
                                spawnHeight = probeHeight - 32f;
                                break;
                            }
                        }
                        if (spawnHeight < MinSpawnHeight)
                        {
                            continue;
                        }

                        for (int side = -1; side <= 1; side += 2)
                        {
                            Vector2 spawnPosition = new Vector2(columnX + side * SpikeSpread, target.Center.Y - spawnHeight);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPosition, new Vector2(0f, 6f),
                                ModContent.ProjectileType<NitoCeilingSpike>(), BoneDamage, 1f, Main.myPlayer, delay);
                        }
                    }
                }
            }

            // The spikes own their portal, fall and death; once the last row is out he can start a melee
            // answer while it resolves (the point of the attack is to leave the player near him).
            if (AttackTimer >= lastRowTick + 10)
            {
                QueueMeleePressure(player);
                EndAttack(80);
            }
        }

        ///<summary>Each wave's shards now MATERIALISE in mid-air and hang there spinning for a full
        ///second before launching — the volley had no tell at all previously, it just appeared as
        ///damage. The hold is owned by the shard itself (ai[0] = charge ticks); it re-aims at release
        ///rather than at spawn, so the telegraph warns without making the shot free to walk away
        ///from.</summary>
        void RunBoneVolley(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            const int ShardCharge = 60;   // spin-up the player can read and react to
            const int WaveGap = 54;       // was 14 — +40 ticks of breathing room between waves
            int cast = Telegraph(30);
            globalNPC.AttackCommitted = AttackTimer <= cast + ShardCharge + WaveGap * 2;
            if (AttackTimer == 1)
            {
                TelegraphCue(new Color(180, 180, 180));
            }
            if (AttackTimer < cast)
            {
                FacePlayer(player);
                NPC.velocity.X *= 0.85f;
            }
            if (Main.netMode != NetmodeID.MultiplayerClient
                && (AttackTimer == cast || AttackTimer == cast + WaveGap || AttackTimer == cast + WaveGap * 2))
            {
                for (int i = -2; i <= 2; i++)
                {
                    // Spawned motionless — the shard holds position for ShardCharge ticks, then aims
                    // itself at Nito's target and launches along this fan offset.
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(lockedDir * 70f, -88f),
                        new Vector2(lockedDir * 7.5f, 0f), ModContent.ProjectileType<NitoBoneShard>(), BoneDamage, 1f,
                        Main.myPlayer, ShardCharge, NPC.whoAmI, i * 0.13f);
                }
            }
            // The shards own their full charge/re-aim/launch sequence. Once the last wave has been
            // placed, Nito can begin a proper melee state while that wave is still charging.
            if (AttackTimer >= cast + WaveGap * 2 + 8)
            {
                QueueMeleePressure(player);
                EndAttack(90);
            }
        }

        void RunGravelordSpikes(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            int cast = Telegraph(36);
            globalNPC.AttackCommitted = AttackTimer <= cast + 36;
            if (AttackTimer == 1)
            {
                TelegraphCue(new Color(140, 140, 160));
            }
            if (AttackTimer < cast)
            {
                NPC.velocity.X *= 0.8f;
                GraveDust(player.Bottom);
            }
            if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer == cast)
            {
                for (int i = -2; i <= 2; i++)
                {
                    SpawnGroundSpike(player.Bottom + new Vector2(i * 54f, 0f), 16 + Math.Abs(i) * 5, 1f + (2 - Math.Abs(i)) * 0.1f);
                }
            }
            // The spike projectile owns its telegraph, eruption and long hold. Hand control back to
            // melee as soon as the field is planted instead of watching the entire hazard resolve.
            if (AttackTimer >= cast + 8)
            {
                QueueMeleePressure(player);
                EndAttack(75);
            }
        }

        ///<summary>Four telegraphed volleys in phase one and five in phase two, each planting ONE spike under EVERY player (so it stays a
        ///real threat in multiplayer rather than only tracking the aggro target). The spike's own
        ///`delay` argument is the telegraph — its ground-rift VFX already reads as "something is
        ///about to burst here" — so the wait is owned by the projectile and the boss just paces the
        ///volleys. The last two volleys accelerate into a finish, especially after the transition.</summary>
        void RunGravelordDance(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            int volleys = PhaseTwo ? 5 : 4;
            int danceTelegraph = PhaseTwo ? 38 : 42;
            int longGap = PhaseTwo ? 20 : 24;
            int shortGap = PhaseTwo ? 10 : 14;

            globalNPC.AttackCommitted = true;
            NPC.velocity.X *= 0.85f;
            if (AttackTimer == 1)
            {
                TelegraphCue(new Color(150, 140, 175));
            }

            // Volley n fires at the accumulated start of its own (telegraph + gap) cycle. Computed
            // rather than stored so it needs no extra synced state.
            int cycleStart = 0;
            int lastVolleyStart = 0;
            for (int volley = 0; volley < volleys; volley++)
            {
                lastVolleyStart = cycleStart;
                if (AttackTimer == cycleStart + 1)
                {
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player target = Main.player[i];
                        if (target.active && !target.dead && NPC.Distance(target.Center) < 1600f)
                        {
                            SpawnGroundSpike(target.Bottom, danceTelegraph, PhaseTwo ? 1.25f : 1.15f);
                        }
                    }
                    GraveDust(NPC.Bottom);
                }
                cycleStart += danceTelegraph + (volley < volleys - 2 ? longGap : shortGap);
            }

            // Each spawned spike owns its telegraph, eruption, hold and withdrawal. The boss only
            // owns the casting cadence, then starts melee before the final marked ground erupts.
            if (AttackTimer >= lastVolleyStart + 9)
            {
                QueueMeleePressure(player);
                EndAttack(110);
            }
        }

        void RunDeathNova(tsorcRevampGlobalNPC globalNPC)
        {
            if (AttackTimer <= LongChannelStaggerTicks)
            {
                globalNPC.AttackTelegraphing = true;
            }
            else if (AttackTimer <= LongChannelTicks)
            {
                globalNPC.AttackCommitted = true;
            }

            NPC.velocity.X *= 0.75f;
            if (AttackTimer == 1)
            {
                TelegraphCue(new Color(120, 90, 160));
            }
            if (AttackTimer == LongChannelStaggerTicks + 1)
            {
                TelegraphCue(Color.Purple);
            }
            if (AttackTimer <= LongChannelTicks)
            {
                float radius = MathHelper.Lerp(220f, 32f, AttackTimer / (float)LongChannelTicks);
                for (int i = 0; i < 1; i++)
                {
                    Vector2 pos = NPC.Center + Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * radius;
                    Dust dust = Dust.NewDustPerfect(pos, DustID.Shadowflame, UsefulFunctions.Aim(pos, NPC.Center, 2.5f), 80, default, 1.1f);
                    dust.noGravity = true;
                }
            }
            if (AttackTimer == LongChannelTicks && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<NitoDeathNova>(), DeathDamage, 5f, Main.myPlayer, DeathNovaRadius);
                UsefulFunctions.ScreenShake(NPC.Center, 8f, 18);
            }
            if (AttackTimer >= LongChannelTicks + 48)
            {
                EndAttack(130);
            }
        }

        void RunMiasmaBreath(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            int cast = Telegraph(38);
            const int BreathTicks = 72;
            int emissionInterval = PhaseTwo ? 5 : 6;
            globalNPC.AttackCommitted = AttackTimer <= cast + BreathTicks;
            if (AttackTimer == 1)
            {
                TelegraphCue(new Color(105, 140, 95));
            }
            if (AttackTimer < cast - 10)
            {
                FacePlayer(player);
                NPC.velocity.X *= 0.82f;
            }
            if (AttackTimer == cast - 10)
            {
                Vector2 mouth = NPC.Center + new Vector2(lockedDir * 38f, -105f);
                Vector2 predictedTarget = player.Center + player.velocity * 18f;
                MiasmaAimDirection = (predictedTarget - mouth).SafeNormalize(new Vector2(lockedDir, 0f));
                lockedDir = MiasmaAimDirection.X >= 0f ? 1 : -1;
                NPC.direction = lockedDir;
                NPC.spriteDirection = lockedDir;
                NPC.netUpdate = true;
            }
            if (AttackTimer >= cast - 10 && AttackTimer < cast)
            {
                NPC.velocity.X *= 0.78f;
                Vector2 mouth = NPC.Center + new Vector2(lockedDir * 38f, -105f);
                for (int i = 0; i < 2; i++)
                {
                    Vector2 pos = mouth + MiasmaAimDirection * Main.rand.NextFloat(18f, 62f)
                        + MiasmaAimDirection.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-18f, 18f);
                    Dust dust = Dust.NewDustPerfect(pos, DustID.Poisoned,
                        MiasmaAimDirection * Main.rand.NextFloat(0.8f, 2f), 110, default, 1f);
                    dust.noGravity = true;
                }
            }
            if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer >= cast
                && AttackTimer <= cast + BreathTicks && (AttackTimer - cast) % emissionInterval == 0)
            {
                // Two overlapping projectile lanes make a continuous smog front without making each
                // puff home after release. The axis is committed ten ticks before the breath begins,
                // so the player can read it and then evade rather than being followed by every cloud.
                Vector2 pos = NPC.Center + new Vector2(lockedDir * 38f, -105f);
                Vector2 perpendicular = MiasmaAimDirection.RotatedBy(MathHelper.PiOver2);
                for (int lane = -1; lane <= 1; lane += 2)
                {
                    float coneOffset = lane * Main.rand.NextFloat(0.055f, 0.16f) + Main.rand.NextFloat(-0.025f, 0.025f);
                    Vector2 velocity = MiasmaAimDirection.RotatedBy(coneOffset) * Main.rand.NextFloat(5.9f, 8.4f);
                    Vector2 spawn = pos + perpendicular * lane * Main.rand.NextFloat(4f, 12f);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), spawn, velocity,
                        ModContent.ProjectileType<NitoMiasmaCloud>(), DeathDamage / 2, 0.2f, Main.myPlayer);
                }
            }
            // The cloud front remains active after emission stops, so begin the melee windup while
            // it is still crossing the arena instead of adding a separate post-breath idle beat.
            if (AttackTimer >= cast + BreathTicks + 8)
            {
                QueueMeleePressure(player);
                EndAttack(70);
            }
        }

        void RunBonePillarCage(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            int cast = Telegraph(42);
            globalNPC.AttackCommitted = AttackTimer <= cast + 24;
            if (AttackTimer == 1)
            {
                TelegraphCue(new Color(190, 190, 210));
            }
            if (AttackTimer < cast)
            {
                GraveDust(player.Bottom);
            }
            if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer == cast)
            {
                for (int i = -3; i <= 3; i++)
                {
                    if (i != 0)
                    {
                        SpawnGroundSpike(player.Bottom + new Vector2(i * 46f, 0f), 20 + Math.Abs(i) * 4, 1.35f);
                    }
                }
            }
            if (AttackTimer >= cast + 8)
            {
                // Prefer the phase-two signature sword string when its geometry is valid; otherwise
                // choose the normal proximity-aware melee answer. Either starts while the cage rises.
                if (IsQueuedAttackEligible(AttackState.QuietusCombo, player))
                {
                    QueueFollowUp(AttackState.QuietusCombo, player);
                }
                else
                {
                    QueueMeleePressure(player);
                }
                EndAttack(90);
            }
        }

        void RunGraveHands(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            int cast = Telegraph(28);
            globalNPC.AttackCommitted = AttackTimer <= cast + 20;
            if (AttackTimer == 1)
            {
                TelegraphCue(new Color(125, 125, 145));
            }
            if (AttackTimer < cast)
            {
                GraveDust(player.Bottom);
            }
            if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer == cast)
            {
                SpawnGraveHandPair(player, 36f);
            }
            // The hands own their emergence, held beat, inward sweep and possible retreating novas.
            // Nito is therefore free to threaten a sword attack behind their telegraph.
            if (AttackTimer >= cast + 10)
            {
                QueueMeleePressure(player);
                EndAttack(70);
            }
        }

        void RunQuietusCombo(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            globalNPC.AttackCommitted = AttackTimer <= 108;
            if (AttackTimer == 1)
            {
                TelegraphCue(new Color(120, 95, 160));
                ArmSlashWindup(2, 1, 30);
            }
            if (AttackTimer <= 21)
            {
                AdvanceTowardPlayer(player, 2.6f, 0.18f, updateFacing: true);
                SwordTelegraphDust(2);
            }
            else if (AttackTimer < 30)
            {
                NPC.velocity.X *= 0.82f;
            }
            if (AttackTimer == 30)
            {
                NPC.velocity.X = lockedDir * 6.6f;
                SpawnSlash(2, SlashDamage);
            }
            if (AttackTimer == 39)
            {
                FacePlayer(player);
                ArmSlashWindup(1, 39, 68);
                NPC.netUpdate = true;
            }
            if (AttackTimer >= 39 && AttackTimer <= 54)
            {
                AdvanceTowardPlayer(player, 2.35f, 0.17f, updateFacing: true);
                SwordTelegraphDust(1);
            }
            else if (AttackTimer > 54 && AttackTimer < 68)
            {
                NPC.velocity.X *= 0.88f;
            }
            if (AttackTimer == 68)
            {
                NPC.velocity.X = lockedDir * 3.2f * SwipePushScale;
                SpawnSlash(1, HeavySlashDamage);
            }
            if (AttackTimer > 68)
            {
                NPC.velocity.X *= 0.91f;
            }
            if (AttackTimer == 102 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<NitoDeathNova>(), DeathDamage, 5f, Main.myPlayer, 150f);
            }
            if (AttackTimer >= 148)
            {
                EndAttack(110);
            }
        }

        ///<summary>A procession of blades that walks ACROSS the player rather than out from Nito's own
        ///feet. The old version spawned every spike relative to NPC.Bottom, so on a boss who likes to
        ///keep his distance the whole march played out far away from the fight — the "triggers far from
        ///the player" complaint. The line is now anchored to the player's position, starting two
        ///spacings back on Nito's side and stepping toward (and then past) them, so the 3rd blade lands
        ///exactly where they stood and the player has to keep moving ahead of the procession.</summary>
        void RunCemeteryMarch(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            const int MarchSwords = 6;
            const float MarchSpacing = 80f;   // 5 tiles between blades
            const int MarchInterval = 40;     // ticks between each blade piercing the ground
            const int LeadSwords = 2;         // how many land short of the player before the line reaches them
            int cast = Telegraph(30);
            globalNPC.AttackCommitted = AttackTimer <= cast;

            if (AttackTimer == 1)
            {
                TelegraphCue(Color.Gray);
            }
            if (AttackTimer < cast)
            {
                FacePlayer(player);
                NPC.velocity.X *= 0.85f;
                GraveDust(player.Bottom);
            }
            else if (AttackTimer == cast)
            {
                // Lock the whole procession's geometry once, at cast: marching toward wherever the
                // player was standing. Every spike is planted immediately with its own staggered
                // eruption delay, so the march continues while Nito is already winding up melee.
                // Re-reading the player every step would let them drag the line around with them,
                // which defeats the "outrun it" read.
                MarchOriginX = player.Bottom.X - lockedDir * MarchSpacing * LeadSwords;
                MarchGroundY = player.Bottom.Y;
                for (int step = 0; step < MarchSwords; step++)
                {
                    SpawnGroundSpike(new Vector2(MarchOriginX + lockedDir * MarchSpacing * step, MarchGroundY),
                        14 + step * MarchInterval, 1.1f);
                }
                NPC.netUpdate = true;
            }
            if (AttackTimer >= cast + 8)
            {
                QueueMeleePressure(player);
                EndAttack(95);
            }
        }

        void RunHollowCommand(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            int cast = Telegraph(40);
            globalNPC.AttackCommitted = AttackTimer <= cast + 34;
            if (AttackTimer == 1)
            {
                TelegraphCue(new Color(150, 150, 170));
            }
            if (AttackTimer < cast)
            {
                NPC.velocity.X *= 0.8f;
            }
            if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer == cast)
            {
                for (int i = 0; i < 8; i++)
                {
                    float angle = MathHelper.TwoPi * i / 8f;
                    Vector2 velocity = angle.ToRotationVector2() * 5.4f;
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + velocity.SafeNormalize(Vector2.UnitY) * 80f, velocity, ModContent.ProjectileType<NitoBoneShard>(), BoneDamage, 1f, Main.myPlayer);
                }
                SpawnGraveHandPair(player, 52f); // doubled telegraph (was 26)
            }
            if (AttackTimer >= cast + 34)
            {
                QueueMeleePressure(player);
                EndAttack(95);
            }
        }

        void RunGravelordJudgment(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            int cast = Telegraph(46);
            const int RainTicks = 56;
            const int WindupStartOffset = 58;
            const int CleaveOffset = 88;
            globalNPC.AttackTelegraphing = AttackTimer < cast;
            globalNPC.AttackCommitted = AttackTimer >= cast && AttackTimer <= cast + CleaveOffset;
            if (AttackTimer == 1)
            {
                TelegraphCue(new Color(205, 54, 62));
            }
            if (AttackTimer < cast)
            {
                AdvanceTowardPlayer(player, 1.65f, 0.11f, updateFacing: true);
                if (Main.rand.NextBool(2))
                {
                    Dust.NewDust(player.position + new Vector2(Main.rand.NextFloat(-240f, 240f), Main.rand.NextFloat(-330f, -220f)), 8, 8, DustID.BoneTorch, 0f, 0.8f, 80, default, 1.15f);
                }
            }
            else if (AttackTimer < cast + WindupStartOffset)
            {
                AdvanceTowardPlayer(player, 1.65f, 0.09f, updateFacing: true);
            }
            if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer >= cast && AttackTimer <= cast + RainTicks && (AttackTimer - cast) % 8 == 0)
            {
                Vector2 pos = player.Center + new Vector2(Main.rand.NextFloat(-280f, 280f), -360f);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), pos, new Vector2(Main.rand.NextFloat(-0.8f, 0.8f), 9f), ModContent.ProjectileType<NitoCeilingSpike>(), BoneDamage, 1f, Main.myPlayer, 10f);
            }
            if (AttackTimer == cast + WindupStartOffset)
            {
                FacePlayer(player);
                ArmSlashWindup(1, cast + WindupStartOffset, cast + CleaveOffset);
                NPC.netUpdate = true;
            }
            if (AttackTimer >= cast + WindupStartOffset && AttackTimer < cast + CleaveOffset - 15)
            {
                AdvanceTowardPlayer(player, 2f, 0.12f, updateFacing: true);
            }
            else if (AttackTimer >= cast + CleaveOffset - 15 && AttackTimer < cast + CleaveOffset)
            {
                AdvanceTowardPlayer(player, 2f, 0.12f, updateFacing: false);
            }
            if (AttackTimer == cast + CleaveOffset)
            {
                NPC.velocity.X = lockedDir * 3.2f * SwipePushScale;
                SpawnSlash(1, HeavySlashDamage);
                NPC.netUpdate = true;
            }
            else if (AttackTimer > cast + CleaveOffset)
            {
                NPC.velocity.X *= 0.91f;
            }
            if (AttackTimer >= cast + 130)
            {
                EndAttack(110);
            }
        }

        void RunPhaseTransition(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            NPC.velocity.X *= 0.7f;
            globalNPC.AttackCommitted = AttackTimer <= 80;
            if (AttackTimer == 1)
            {
                SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.7f, Pitch = -0.45f }, NPC.Center);
                UsefulFunctions.ScreenShake(NPC.Center, 6f, 18);
            }
            if (AttackTimer % 3 == 0)
            {
                Dust dust = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(110f, 130f), DustID.Shadowflame, Main.rand.NextVector2Circular(2f, 2f), 80, default, 1.3f);
                dust.noGravity = true;
            }
            if (AttackTimer == 80)
            {
                NitoVFX.PyreBurst(NPC.Bottom, 54, 5.2f, 1.3f, 170f, 28f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<NitoDeathNova>(), DeathDamage, 5f, Main.myPlayer, 180f);
                    SpawnPhaseTwoPyre(NPC.Bottom + new Vector2(-180f, 0f), DeathDamage / 2);
                    SpawnPhaseTwoPyre(NPC.Bottom + new Vector2(-90f, 0f), DeathDamage / 2);
                    SpawnPhaseTwoPyre(NPC.Bottom + new Vector2(90f, 0f), DeathDamage / 2);
                    SpawnPhaseTwoPyre(NPC.Bottom + new Vector2(180f, 0f), DeathDamage / 2);
                }
            }
            if (AttackTimer >= 125)
            {
                float horizontal = Math.Abs(player.Center.X - NPC.Center.X);
                float vertical = Math.Abs(player.Center.Y - NPC.Center.Y);
                AttackState opener = horizontal <= 410f && vertical < 135f
                    ? AttackState.QuietusCombo
                    : horizontal >= 120f && horizontal <= 650f
                        ? AttackState.BonePillarCage
                        : AttackState.GravelordJudgment;
                QueueFollowUp(opener, player);
                EndAttack(75);
            }
        }

        void RunComboRecovery()
        {
            NPC.velocity.X *= 0.75f;
            ComboRecoveryTicks--;
            if (Main.rand.NextBool(4))
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, 0f, 1f, 100, default, 1f);
            }
            if (ComboRecoveryTicks <= 0)
            {
                EndAttack(70);
            }
        }

        int Telegraph(int baseTicks) => HalfTelegraph ? Math.Max(12, baseTicks / 2) : baseTicks;

        void StartAttack(AttackState state, Player player)
        {
            State = state;
            AttackTimer = 0;
            lockedDir = player.Center.X >= NPC.Center.X ? 1 : -1;
            NPC.direction = lockedDir;
            NPC.spriteDirection = lockedDir;
            // Vertical read, locked once at attack-start (mirrors lockedDir) so the windup pose,
            // telegraph dust, and the eventual SpawnSlash all agree on the same kind.
            lockedKind = state switch
            {
                AttackState.SideSweep => ChooseVerticalKind(0, player),
                AttackState.BackhandSweep => ChooseVerticalKind(3, player),
                AttackState.OverheadCleave => ChooseVerticalKind(NextOverheadIsRising ? 4 : 1, player),
                AttackState.ImpalingThrust => ChooseVerticalKind(2, player),
                _ => 0,
            };
            if (state == AttackState.OverheadCleave)
            {
                NextOverheadIsRising = !NextOverheadIsRising;
            }
            // Fresh state: no windup armed, no slash released yet — the loose sword rests at idle.
            SlashWindupActive = false;
            SlashActiveKind = -1;
            DragRunLeapLaunched = false;
            ThrustDashTicks = 0;
            if (state == AttackState.SwordRain || state == AttackState.SwordLineDance)
            {
                RangedRainCharge = 0;
            }

            // Everything below rolls Main.rand, so it is server-only; clients receive the results in the
            // netUpdate this method sets. (Only ComboRecovery ever reaches here on a client.)
            TimingVariant = VariantStandard;
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NPC.netUpdate = true;
                return;
            }

            if (state == AttackState.SideSweep || state == AttackState.BackhandSweep
                || state == AttackState.OverheadCleave)
            {
                // Phase 2 leans on the odd rhythms: 25/40/35 Standard/Quick/Delayed vs 45/35/20 in phase 1.
                float variantRoll = Main.rand.NextFloat();
                float quickShare = PhaseTwo ? 0.40f : 0.35f;
                float delayedShare = PhaseTwo ? 0.35f : 0.20f;

                if (variantRoll < quickShare)
                {
                    TimingVariant = VariantQuick;
                }
                else if (variantRoll < quickShare + delayedShare)
                {
                    TimingVariant = VariantDelayed;
                }
            }

            if (state == AttackState.TripleReaperCombo)
            {
                ComboLength = 3;
                ComboEndStep = 2;
                ComboKinds = 0 | (3 << 3) | (1 << 6); // side, backhand, overhead: the original triple
            }
            else if (state == AttackState.ReapersPendulum)
            {
                // Strict alternation of the two poles' side cuts: 0,3,0,3... or 3,0,3,0... Phase 2 runs
                // 4-6 cuts, phase 1 runs 3.
                ComboLength = PhaseTwo ? Main.rand.Next(4, MaxComboSteps + 1) : 3;
                ComboEndStep = ComboLength - 1;
                int firstPendulumKind = Main.rand.NextBool() ? 0 : 3;
                ComboKinds = 0;

                for (int step = 0; step < ComboLength; step++)
                {
                    int pendulumKind = step % 2 == 0 ? firstPendulumKind : 3 - firstPendulumKind;
                    ComboKinds |= pendulumKind << (3 * step);
                }
            }
            else if (state == AttackState.ReaperWeave)
            {
                // A random chain that alternates poles. From the UP-BACK pole the next cut may be a side
                // sweep (0) or an overhead cleave (1); from the DOWN-FORWARD pole it may be a backhand (3)
                // or a rising cut (4). Any of those starts exactly where the last one ended, so the blade
                // never snaps. Phase 1 runs 2-3 cuts, phase 2 runs 4-5 (the finisher is always the heavy).
                ComboLength = PhaseTwo ? Main.rand.Next(4, 6) : Main.rand.Next(2, 4);
                ComboEndStep = ComboLength - 1;
                bool atUpBackPole = Main.rand.NextBool();
                ComboKinds = 0;

                for (int step = 0; step < ComboLength; step++)
                {
                    bool finisher = step == ComboLength - 1;
                    bool pickHeavierCut = finisher || Main.rand.NextBool(); // overhead / rising vs side / backhand
                    int weaveKind;

                    if (atUpBackPole)
                    {
                        weaveKind = 0;

                        if (pickHeavierCut)
                        {
                            weaveKind = 1;
                        }
                    }
                    else
                    {
                        weaveKind = 3;

                        if (pickHeavierCut)
                        {
                            weaveKind = 4;
                        }
                    }

                    ComboKinds |= weaveKind << (3 * step);
                    atUpBackPole = !atUpBackPole;
                }
            }

            NPC.netUpdate = true;
        }

        void EndAttack(int cooldown)
        {
            if (State != AttackState.ComboRecovery && State != AttackState.PhaseTransition)
            {
                RecordCompletedAttack(State);
            }
            State = AttackState.None;
            AttackTimer = 0;
            HalfTelegraph = false;
            SlashWindupActive = false;
            // Phase two increases tempo through recovery, not by deleting readable windups. Normal
            // attacks now leave roughly 0.75-1.75 seconds before the next selection; signatures retain
            // a longer punish/reset window. This also lets intentional lingering hazards survive into
            // the next action instead of expiring during a four-second idle.
            float phaseRecoveryScale = PhaseTwo ? 0.78f : 1f;
            int variance = PhaseTwo ? 14 : 22;
            AttackCooldown = Math.Max(30, (int)(cooldown * phaseRecoveryScale));

            // Only the server rolls the variance — it is the only machine that picks the next attack, and the
            // result rides this transition's snapshot. A client rolling its own would idle for a different
            // length than the boss it is watching until that packet lands.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                AttackCooldown += Main.rand.Next(variance);
            }

            if (QueuedAttack != AttackState.None)
            {
                // These are authored continuations (rain -> thrust, smog -> advance, etc.), not a
                // second random attack. Keep only a brief beat so the existing hazard is still active
                // during the follow-up's telegraph, while preserving a readable state boundary.
                AttackCooldown = Math.Min(AttackCooldown, PhaseTwo ? 12 : 18);
            }
            NPC.damage = 0;
            NPC.netUpdate = true;
        }

        void RecordCompletedAttack(AttackState completedAttack)
        {
            if (completedAttack == AttackState.None || completedAttack == AttackState.ComboRecovery
                || completedAttack == AttackState.PhaseTransition)
            {
                return;
            }
            PreviousAttack = LastAttack;
            LastAttack = completedAttack;
        }

        static bool HasLivingPlayer()
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active && !Main.player[i].dead)
                {
                    return true;
                }
            }
            return false;
        }

        const float PlatformHeightTolerance = 56f; // 3.5 tiles of foot-height separation

        // Platforms are useful arena footing, so Nito stands on them while his feet are roughly level
        // with the player's. If a jump or a moving target strands him above the fight, he deliberately
        // drops through instead of pacing on the upper platform indefinitely.
        public override bool? CanFallThroughPlatforms()
        {
            if (NPC.target < 0 || NPC.target >= Main.maxPlayers)
            {
                return false;
            }
            Player player = Main.player[NPC.target];
            if (!player.active || player.dead)
            {
                return false;
            }
            return player.Bottom.Y - NPC.Bottom.Y > PlatformHeightTolerance;
        }

        ///<summary>Keeps recovery visually and physically pressuring the target. Shared navigation
        ///still handles walls, gaps and vertical routes first; on a clear, grounded lane this removes
        ///leftover strike momentum away from the player and restores Nito's deliberate slow advance.</summary>
        void MaintainRecoveryPursuit(Player player)
        {
            int towardPlayer = Math.Sign(player.Center.X - NPC.Center.X);
            if (towardPlayer == 0)
            {
                towardPlayer = NPC.spriteDirection == 0 ? 1 : NPC.spriteDirection;
            }

            // Recovery never presents Nito's back to his target, even when pathfinding briefly needs
            // a different physical route. FacePlayer also synchronizes a changed facing from server.
            FacePlayer(player);

            float horizontal = Math.Abs(player.Center.X - NPC.Center.X);
            bool directGroundLane = NPC.velocity.Y == 0f
                && Math.Abs(player.Center.Y - NPC.Center.Y) < 96f
                && Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1);
            if (!directGroundLane)
            {
                return;
            }

            if (horizontal <= 105f)
            {
                NPC.velocity.X *= 0.72f;
                if (Math.Abs(NPC.velocity.X) < 0.08f)
                {
                    NPC.velocity.X = 0f;
                }
                return;
            }

            if (NPC.velocity.X * towardPlayer < 0f)
            {
                // Do not let the release step from the previous swing become a visible retreat.
                NPC.velocity.X = 0f;
            }
            NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, towardPlayer * 0.62f, 0.18f);
            NPC.direction = towardPlayer;
            NPC.spriteDirection = towardPlayer;
        }

        ///<summary>Chooses a real melee answer for the geometry at the end of a ranged cast. The
        ///queued state still performs its complete windup, active frames, follow-through and recovery;
        ///only the ranged state's redundant post-cast waiting is removed.</summary>
        void QueueMeleePressure(Player player)
        {
            float horizontal = Math.Abs(player.Center.X - NPC.Center.X);
            float vertical = Math.Abs(player.Center.Y - NPC.Center.Y);
            AttackState followUp;
            if (vertical > 135f || horizontal > ImpalingThrustMaxRange)
            {
                followUp = AttackState.LeapingCleave;
            }
            else if (horizontal >= ImpalingThrustMinRange && horizontal <= ImpalingThrustMaxRange)
            {
                followUp = AttackState.ImpalingThrust;
            }
            else if (horizontal > 200f)
            {
                followUp = AttackState.DraggingAdvance; // just under the dash's 410px floor: run in, then leap
            }
            else
            {
                followUp = AttackState.ReaperWeave; // already in range: straight into a chained combo
            }
            QueueFollowUp(followUp, player);
        }

        ///<summary>Nito is a heavy, grounded fighter. If FighterAI starts a navigation hop while the
        ///player's FEET are not meaningfully above his, kill the upward impulse. Using foot height is
        ///important because Nito's much taller hitbox makes center-to-center comparisons misleading.
        ///AutoStepUp still carries him over 1–2 tile ledges without a jump.</summary>
        void StayGroundedRelativeTo(Player player)
        {
            // Don't suppress a lava/liquid escape hop — only genuine climbing toward higher ground.
            if (NPC.lavaWet || NPC.wet)
            {
                return;
            }
            bool playerAbove = player.Bottom.Y < NPC.Bottom.Y - PlatformHeightTolerance;
            if (!playerAbove && NPC.velocity.Y < -1f)
            {
                NPC.velocity.Y = 0f;
            }
        }

        void FacePlayer(Player player)
        {
            if (player.Center.X != NPC.Center.X)
            {
                int oldLockedDirection = lockedDir;
                int oldDirection = NPC.direction;
                int oldSpriteDirection = NPC.spriteDirection;
                lockedDir = player.Center.X > NPC.Center.X ? 1 : -1;
                NPC.direction = lockedDir;
                NPC.spriteDirection = lockedDir;
                if ((oldLockedDirection != lockedDir || oldDirection != NPC.direction
                    || oldSpriteDirection != NPC.spriteDirection)
                    && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.netUpdate = true;
                }
            }
        }

        ///<summary>Grounded, whole-body attack movement for Nito's fixed sprite rig. This is deliberately
        ///faster than his 0.62 idle walk but remains a heavy advance rather than a player-speed chase.
        ///Call with updateFacing only during the readable approach portion; callers stop re-facing at
        ///their decision-lock tick so the final strike cannot rotate through a successful dodge.
        ///Both the speed and the acceleration are multiplied by ApproachScale (1.5x in phase one, 1.9x in
        ///phase two) so his melee actually closes the gap. This is attack movement only: the idle walk
        ///(FighterAI 0.62) is not scaled.</summary>
        void AdvanceTowardPlayer(Player player, float topSpeed, float acceleration, bool updateFacing)
        {
            if (updateFacing)
            {
                FacePlayer(player);
            }
            float approachScale = ApproachScale;
            float scaledTopSpeed = topSpeed * approachScale;
            float scaledAcceleration = Math.Min(1f, acceleration * approachScale);

            float desiredVelocity = lockedDir * scaledTopSpeed;
            NPC.velocity.X = MathHelper.Clamp(
                MathHelper.Lerp(NPC.velocity.X, desiredVelocity, scaledAcceleration), -scaledTopSpeed, scaledTopSpeed);
            FootstepEffects();
        }

        void SpawnSlash(int kind, int damage, int style = StyleLegacy)
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = kind == 1 ? -0.25f : 0.05f }, NPC.Center);
            // The loose sword layer now plays the visible swing itself (see PreDraw's active-window);
            // the projectile is just a matching invisible hitbox. Record the release so both agree.
            SlashWindupActive = false;
            SlashActiveKind = kind;
            SlashActiveStartTick = AttackTimer;
            SlashActiveDirection = lockedDir;
            SlashActiveStyle = style;
            int finalDamage = damage;
            if (PhaseTwo)
            {
                Vector2 impact = NPC.Bottom + new Vector2(lockedDir * (kind == 2 ? 150f : 105f), -18f);
                NitoVFX.PyreBurst(impact, kind == 1 || kind == 4 ? 18 : 11, 3.8f, 1.05f, 44f, 24f);
                if (kind == 1 || kind == 4)
                {
                    SpawnPhaseTwoPyre(NPC.Bottom + new Vector2(lockedDir * 105f, 0f), finalDamage / 2);
                }
            }
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            NPC.netUpdate = true;
            // ai[1] packs the swing kind (0-4) with its timing style (kind + style * 8) so the hitbox
            // can rebuild the exact same eased arc.
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                ModContent.ProjectileType<NitoSwordSlash>(), finalDamage, 5f, Main.myPlayer,
                NPC.whoAmI, kind + style * 8, SlashActiveDirection);
        }

        void KillOwnedSwordSlashes()
        {
            int slashType = ModContent.ProjectileType<NitoSwordSlash>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile slash = Main.projectile[i];
                if (slash.active && slash.type == slashType && (int)slash.ai[0] == NPC.whoAmI)
                {
                    slash.Kill();
                }
            }
        }

        ///<summary>Arms the loose sword's windup: it lerps from the idle pose toward this kind's
        ///swing-start pose over [windupStartTick, releaseTick]. Call once per slash, at (or just
        ///before) the tick the windup should visibly begin.</summary>
        void ArmSlashWindup(int kind, int windupStartTick, int releaseTick, bool chained = false)
        {
            SlashWindupKind = kind;
            SlashWindupStartTick = windupStartTick;
            SlashWindupEndTick = Math.Max(windupStartTick + 1, releaseTick);
            SlashWindupActive = true;
            SlashWindupChained = chained;
        }

        // ── Shared swing geometry (used by BOTH the boss draw and the NitoSwordSlash hitbox) ─────
        // A swing is described in FORWARD-RELATIVE angle phi: phi = 0 points the blade straight at the
        // player, negative tilts it up/overhead, +/-pi points it straight back. Each kind is one arc
        // (progress 0->1); overhead (1) and rising (4) are the same shape reversed, likewise side (0)
        // and backhand (3). Arcs are deliberately wide (~150-180 deg) so the sweep reads big.
        public static float SlashPhi(int kind, float progress) => kind switch
        {
            1 => MathHelper.Lerp(-2.0f, 0.7f, progress),   // overhead cleave: cocked up-back -> down-forward
            2 => 0f,                                        // thrust: held forward, reach extends instead
            3 => MathHelper.Lerp(0.6f, -2.4f, progress),    // backhand: forward -> up-back (reverse of side sweep)
            4 => MathHelper.Lerp(0.7f, -2.0f, progress),    // rising cut: down-forward -> up-back (reverse of overhead)
            _ => MathHelper.Lerp(-2.4f, 0.6f, progress),    // side sweep: up-back over the top -> down-forward
        };

        public static float SlashReach(int kind, float progress) => kind == 2 ? 60f + progress * 120f : 82f;

        ///<summary>Total ticks the visible swing (and its hitbox projectile) lasts for a style.</summary>
        public static int SlashStyleTicks(int style) =>
            style == StyleLegacy ? SlashActiveTicks : SlashStyles[style].TotalTicks;

        ///<summary>Ticks from release the blade is still a hitbox (speed >= 30% of peak). Legacy is live throughout.</summary>
        public static float SlashArmedTicks(int style) =>
            style == StyleLegacy ? SlashActiveTicks : SlashStyles[style].LiveTicks;

        ///<summary>Swing progress the blade finishes at: 1 for Legacy, EasedOvershoot for the eased styles.</summary>
        public static float SlashEndProgress(int style) => style == StyleLegacy ? 1f : EasedOvershoot;

        ///<summary>Swing progress (0 to SlashEndProgress) after the given ticks since release. The boss
        ///draw and the NitoSwordSlash hitbox BOTH read this, so the visible blade and the hit line can't
        ///drift apart. Feed the result to SlashPhi / SlashReach / SlashOffset.</summary>
        public static float SlashEasedProgress(int style, float elapsedTicks)
        {
            if (style == StyleLegacy)
            {
                return MathHelper.Clamp(elapsedTicks / SlashActiveTicks, 0f, 1f);
            }

            WeightedSwing curve = SlashStyles[style];
            return SwingEase.ApplyWeighted(0f, EasedOvershoot, elapsedTicks, curve.TotalTicks,
                curve.EaseInTicks, curve.EaseOutTicks, curve.EaseOutDecay);
        }

        ///<summary>World-space angle the blade points at, for a given facing (dir) and swing progress.</summary>
        public static float SlashWorldAngle(int kind, int dir, float progress)
        {
            float phi = SlashPhi(kind, progress);
            return dir >= 0 ? phi : MathHelper.Pi - phi;
        }

        ///<summary>Offset of the blade's CENTER from the wielder's Center for a given facing/progress.</summary>
        public static Vector2 SlashOffset(int kind, int dir, float progress)
        {
            float theta = SlashWorldAngle(kind, dir, progress);
            return new Vector2(dir * SwordPivotX, SwordPivotY) + theta.ToRotationVector2() * SlashReach(kind, progress);
        }

        ///<summary>Reads the player's vertical offset at attack-start and swaps in a better-fitting
        ///slash: a rising cut (kind 4) when the player is well above Nito, the downward/overhead
        ///cleave (kind 1) when they're clearly below — otherwise keeps the attack's own signature
        ///kind, since that already reads fine at roughly the same height ("in front").</summary>
        int ChooseVerticalKind(int defaultKind, Player player)
        {
            float verticalOffset = player.Center.Y - NPC.Center.Y; // negative = player above Nito
            if (verticalOffset < -140f)
            {
                return 4;
            }
            if (verticalOffset > 90f && defaultKind != 1)
            {
                return 1;
            }
            return defaultKind;
        }

        ///<summary>Spawns the flanking grave-hand pair that erupts wide, holds, then CLAPS together on
        ///the spot the player occupied at spawn (see NitoGraveHand). Both hands share a convergence
        ///centre and side role; a pre-clap roll-through can turn that normal single blast into two
        ///outward, 300-pixel retreat detonations.</summary>
        void SpawnGraveHandPair(Player player, float telegraphTicks)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            const float HandSpread = NitoGraveHand.InitialHandOffset; // shared with the exact 300px retreat target
            float centerX = player.Bottom.X;
            for (int i = -1; i <= 1; i += 2)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), player.Bottom + new Vector2(i * HandSpread, -34f),
                    Vector2.Zero, ModContent.ProjectileType<NitoGraveHand>(), HeavySlashDamage, 2f, Main.myPlayer,
                    telegraphTicks, centerX, i > 0 ? 1f : 0f);
            }
        }

        void SpawnGroundSpike(Vector2 roughBottom, int delay, float heightScale)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            // Only erupt from a real floor surface (solid tile with open space above). If there is no
            // such surface below the target — the player is airborne over a pit, in a tight spot, etc.
            // — skip the spike entirely rather than spawning it buried inside solid rock where it would
            // read as a stuck sliver. Platforms are passed through (they aren't treated as the floor).
            if (!FindGroundSurface(roughBottom, out Vector2 bottom))
            {
                return;
            }
            // NewProjectile treats the position argument as the CENTER (it subtracts half width/height
            // internally), and NitoGraveSpike's height is 54*heightScale — so the center needs to sit
            // HALF that height above the surface for the spike's bottom edge to land exactly on the
            // ground. The old `54f * heightScale` (a full height, not half) planted the center a full
            // height too high, leaving the spike floating ~27-36px (1-2 tiles) above the real ground.
            Projectile.NewProjectile(NPC.GetSource_FromThis(), bottom - new Vector2(0f, 27f * heightScale), Vector2.Zero, ModContent.ProjectileType<NitoGraveSpike>(), DeathDamage, 3f, Main.myPlayer, delay, heightScale);
        }

        void SpawnPhaseTwoPyre(Vector2 roughBottom, int damage)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !FindGroundSurface(roughBottom, out Vector2 bottom))
            {
                return;
            }
            Projectile.NewProjectile(NPC.GetSource_FromThis(), bottom, Vector2.Zero,
                ModContent.ProjectileType<NitoPyreFire>(), Math.Max(1, damage), 0f, Main.myPlayer);
        }

        ///<summary>Scans downward from just above <paramref name="origin"/> for the first SOLID tile
        ///that has open space (air or a platform) directly above it — a true floor the spike can burst
        ///up through. Platforms themselves are skipped (passed through), so spikes track the real
        ///ground beneath them. Returns false when no such surface is found in range.</summary>
        static bool FindGroundSurface(Vector2 origin, out Vector2 surface)
        {
            int tileX = Math.Clamp((int)(origin.X / 16f), 5, Main.maxTilesX - 5);
            int startY = Math.Clamp((int)(origin.Y / 16f) - 3, 5, Main.maxTilesY - 6);
            for (int y = startY; y <= startY + 30 && y < Main.maxTilesY - 5; y++)
            {
                Tile tile = Main.tile[tileX, y];
                bool solid = tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
                if (!solid)
                {
                    continue;
                }
                Tile above = Main.tile[tileX, y - 1];
                bool openAbove = !above.HasTile || above.IsActuated || !Main.tileSolid[above.TileType] || Main.tileSolidTop[above.TileType];
                if (openAbove)
                {
                    surface = new Vector2(origin.X, y * 16f);
                    return true;
                }
            }
            surface = origin;
            return false;
        }

        void TelegraphCue(Color color)
        {
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.75f, Pitch = -0.35f }, NPC.Center);
            // No netMode guard here: SpawnTelegraphFlash spawns a NETWORKED projectile and gates itself to
            // server/singleplayer. The old "!= Server" wrapper meant the server skipped it and the client was
            // refused inside — so every attack's tell was invisible in multiplayer.
            tsorcRevampAIs.SpawnTelegraphFlash(NPC, color, NPC.Center + new Vector2(0f, -85f));
        }

        void SwordTelegraphDust(int kind)
        {
            if (kind == 2)
            {
                // The thrust tell lives on the actual loose sword, not at the old head-height proxy.
                // Thrust phi is horizontal, so this is the same hilt-to-tip axis PreDraw renders.
                Vector2 bladeDirection = new Vector2(lockedDir, 0f);
                Vector2 hilt = NPC.Center + new Vector2(
                    lockedDir * SwordPivotX, SwordPivotY + GroundSinkPixels);
                float windupProgress = SlashWindupEndTick > SlashWindupStartTick
                    ? MathHelper.Clamp((AttackTimer - SlashWindupStartTick)
                        / (float)(SlashWindupEndTick - SlashWindupStartTick), 0f, 1f)
                    : 1f;
                float reach = MathHelper.Lerp(SwordIdleReach, SlashReach(kind, 0f), windupProgress);
                float lengthScale = 1f + MathHelper.Clamp((reach - SwordIdleReach) / 100f, -0.15f, 0.45f);
                Vector2 tip = hilt + bladeDirection * 195f * lengthScale;
                Vector2 perpendicular = new Vector2(0f, 1f);
                for (int i = 0; i < 2; i++)
                {
                    Vector2 pos = Vector2.Lerp(hilt, tip, Main.rand.NextFloat(0.12f, 0.96f))
                        + perpendicular * Main.rand.NextFloat(-5f, 5f);
                    Dust dust = Dust.NewDustPerfect(pos, DustID.BoneTorch,
                        bladeDirection * Main.rand.NextFloat(0.25f, 1.15f)
                            + perpendicular * Main.rand.NextFloat(-0.45f, 0.45f),
                        100, default, Main.rand.NextFloat(0.72f, 0.9f));
                    dust.noGravity = true;
                }
                return;
            }

            // Rising slash gathers low near the hip (where the cut starts) rather than at shoulder height.
            Vector2 origin = NPC.Center + (kind == 4 ? new Vector2(lockedDir * 30f, -30f) : new Vector2(lockedDir * 40f, -92f));
            // Swing arc start angle by attack kind: 1 = straight overhead, 4 = wide sweep scaled by facing,
            // everything else = a shallow horizontal slash mirrored to the side we're facing.
            float arc;

            if (kind == 1)
            {
                arc = -MathHelper.PiOver2;
            }
            else if (kind == 4)
            {
                arc = lockedDir * 0.9f;
            }
            else if (lockedDir > 0)
            {
                arc = -0.1f;
            }
            else
            {
                arc = MathHelper.Pi + 0.1f;
            }

            for (int i = 0; i < 2; i++)
            {
                Vector2 pos = origin + (arc + Main.rand.NextFloat(-0.8f, 0.8f)).ToRotationVector2() * Main.rand.NextFloat(50f, 145f);
                Dust dust = Dust.NewDustPerfect(pos, DustID.BoneTorch, Main.rand.NextVector2Circular(1f, 1f), 100, default, 0.95f);
                dust.noGravity = true;
            }
        }

        void DragDust()
        {
            for (int i = 0; i < 3; i++)
            {
                Dust dust = Dust.NewDustPerfect(NPC.Bottom + new Vector2(lockedDir * Main.rand.NextFloat(30f, 120f), -Main.rand.NextFloat(8f, 28f)), DustID.BoneTorch, new Vector2(-lockedDir * 1.2f, Main.rand.NextFloat(-2.4f, -0.6f)), 90, default, 1f);
                dust.noGravity = true;
            }
        }

        void GraveDust(Vector2 center)
        {
            for (int i = 0; i < 1; i++)
            {
                Dust dust = Dust.NewDustPerfect(center + new Vector2(Main.rand.NextFloat(-120f, 120f), -Main.rand.NextFloat(4f, 18f)), DustID.BoneTorch, new Vector2(0f, Main.rand.NextFloat(-1.5f, -0.2f)), 110, default, 0.9f);
                dust.noGravity = true;
            }
        }

        void FootstepEffects()
        {
            if (Math.Abs(NPC.velocity.X) < 0.35f || NPC.velocity.Y != 0f)
            {
                FootstepTimer = 0;
                return;
            }
            FootstepTimer++;
            if (FootstepTimer >= 28)
            {
                FootstepTimer = 0;
                SoundEngine.PlaySound(SoundID.DeerclopsStep with { Volume = 0.45f, Pitch = -0.25f }, NPC.Bottom);
                UsefulFunctions.ScreenShake(NPC.Bottom, 1.3f, 7, 6f, 350f);
                for (int i = 0; i < 7; i++)
                {
                    Dust.NewDust(NPC.Bottom - new Vector2(NPC.width / 2f, 12f), NPC.width, 12, DustID.Smoke, 0f, -1f, 120, default, 0.9f);
                }
            }
        }

        void UpdateAura()
        {
            float pulse = State == AttackState.None ? 0f : (float)Math.Sin(Main.GlobalTimeWrappedHourly * 9f) * 0.08f;
            Lighting.AddLight(NPC.Center, 0.2f + pulse, 0.2f + pulse, 0.28f + pulse);
            if (Main.rand.NextBool(State == AttackState.None ? 8 : 5))
            {
                Dust dust = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(55f, 105f), PhaseTwo ? DustID.Shadowflame : DustID.BoneTorch, new Vector2(0f, Main.rand.NextFloat(-1.4f, -0.3f)), 120, default, 0.75f);
                dust.noGravity = true;
            }
            if (PhaseTwo && Main.netMode != NetmodeID.Server && Main.rand.NextBool(State == AttackState.None ? 2 : 4))
            {
                int direction = NPC.spriteDirection == 0 ? lockedDir : NPC.spriteDirection;
                Vector2 hilt = NPC.Center + new Vector2(direction * SwordPivotX, SwordPivotY + GroundSinkPixels);
                Vector2 position = hilt + Vector2.UnitX * direction * Main.rand.NextFloat(24f, 168f)
                    + Main.rand.NextVector2Circular(5f, 5f);
                int dustType = Main.rand.NextBool() ? DustID.Blood : DustID.Wraith;
                Dust swordDust = Dust.NewDustPerfect(position, dustType,
                    new Vector2(Main.rand.NextFloat(-0.35f, 0.35f), Main.rand.NextFloat(-2.5f, -0.8f)),
                    90, default, dustType == DustID.Blood ? 1.05f : 0.9f);
                swordDust.noGravity = true;
            }
        }

        public override void FindFrame(int frameHeight)
        {
            bool moving = Math.Abs(NPC.velocity.X) > 0.25f || State != AttackState.None;
            if (!moving)
            {
                NPC.frame.Y = 0;
                NPC.frameCounter = 0;
                return;
            }
            NPC.frameCounter++;
            if (NPC.frameCounter >= 5)
            {
                NPC.frameCounter = 0;
                int frame = NPC.frame.Y / FrameHeight;
                NPC.frame.Y = ((frame + 1) % FrameCount) * FrameHeight;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            float auraOpacity = PhaseTwo ? 0.34f : 0.22f;
            if (State == AttackState.PhaseTransition)
            {
                auraOpacity = 0.72f;
            }
            else if (State == AttackState.DeathNova || State == AttackState.HollowCommand
                || State == AttackState.GravelordJudgment)
            {
                auraOpacity = 0.52f;
            }
            float auraPulse = State == AttackState.None
                ? 0f
                : 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4.5f);
            float soulFlowDirection = State == AttackState.PhaseTransition || State == AttackState.DeathNova ? -1f : 1f;
            NitoVFX.DrawAura(NPC.Center + new Vector2(0f, -30f), new Vector2(250f, 320f),
                auraOpacity, PhaseTwo, auraPulse, soulFlowDirection);

            if ((State == AttackState.SwordRain || State == AttackState.GravelordJudgment)
                && NPC.target >= 0 && NPC.target < Main.maxPlayers && Main.player[NPC.target].active)
            {
                Player target = Main.player[NPC.target];
                bool judgment = State == AttackState.GravelordJudgment;
                float veilProgress = MathHelper.Clamp(AttackTimer / (judgment ? 50f : 42f), 0f, 1f);
                NitoVFX.DrawRainPortal(target.Center + new Vector2(0f, judgment ? -350f : -320f),
                    new Vector2(judgment ? 600f : 480f, judgment ? 112f : 96f), veilProgress,
                    judgment ? 0.24f : 0.18f);
            }

            if (State == AttackState.DeathNova && AttackTimer > 0 && AttackTimer <= LongChannelTicks)
            {
                float chargeProgress = MathHelper.Clamp(AttackTimer / (float)LongChannelTicks, 0f, 1f);
                float chargeRadius = MathHelper.Lerp(220f, 38f, chargeProgress);
                NitoVFX.DrawDeathRing(NPC.Center, chargeRadius, 10f,
                    MathHelper.Lerp(0.28f, 0.72f, chargeProgress));
            }

            // Body is ALWAYS the no-sword sheet (TextureAssets.Npc[Type] = GravelordNitoAttacking.png,
            // which has no blade painted in). The single loose sword layer below is the ONLY sword —
            // this is what removes the old "two swords" (a baked-in blade + the loose one).
            Texture2D body = TextureAssets.Npc[Type].Value;
            Texture2D sword = ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Bosses/GravelordNito/GravelordNitoSword").Value;
            Rectangle frame = NPC.frame.Height > 0 ? NPC.frame : new Rectangle(0, 0, BodyWidth, FrameHeight);
            // BOTH sheets are drawn facing LEFT (verified: the skull/ribcage detail's horizontal
            // centroid sits ~30px LEFT of the body's own centroid, and the baked sword extends off the
            // left edge). So facing RIGHT is the flipped case. The old condition flipped on
            // spriteDirection < 0 — the exact opposite — while the sword layer below correctly flipped
            // on >= 0, so body and blade mirrored OPPOSITELY: Nito's body always turned AWAY from the
            // player while his sword pointed at them. That is the "still facing wrong direction" bug.
            int activeEndTick = SlashActiveStartTick + SlashActiveLength;
            bool activeSlashPose = State != AttackState.None && SlashActiveKind >= 0
                && AttackTimer >= SlashActiveStartTick && AttackTimer <= activeEndTick;
            // The active projectile stores the same release direction in ai[2]. Even if a combo has
            // already acquired the player for its next swing, body, loose sword and shader finish the
            // current swipe together instead of mirroring independently.
            int renderDirection = activeSlashPose ? SlashActiveDirection : NPC.spriteDirection;
            bool faceRight = renderDirection >= 0;
            SpriteEffects effects = faceRight ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            // The shared correction places the visible base 16px below the collision feet, matching
            // the requested ground contact while keeping the loose sword and slash geometry attached.
            Vector2 drawBottom = NPC.Bottom + new Vector2(0f, 7f + NPC.gfxOffY + GroundSinkPixels);
            Vector2 swordAnchor = NPC.Center + new Vector2(0f, NPC.gfxOffY + GroundSinkPixels);

            // Resolve the loose sword's forward-relative pose (phi) + reach for this frame.
            int dir = renderDirection;
            const float idlePhi = IdlePhi; // blade forward and level — matches the baked-in art
            float phi = idlePhi;
            float reach = SwordIdleReach;
            if (State != AttackState.None)
            {
                int activeEnd = SlashActiveStartTick + SlashActiveLength;
                float endProgress = SlashEndProgress(SlashActiveStyle);
                if (SlashActiveKind >= 0 && AttackTimer >= SlashActiveStartTick && AttackTimer <= activeEnd)
                {
                    // The visible swing itself (mirrors the invisible NitoSwordSlash hitbox arc). Both read
                    // the same eased progress, so the blade and the hit line stay together through the tail.
                    float progress = SlashEasedProgress(SlashActiveStyle, AttackTimer - SlashActiveStartTick);
                    phi = SlashPhi(SlashActiveKind, progress);
                    reach = SlashReach(SlashActiveKind, progress);
                }
                else if (SlashActiveKind >= 0 && !SlashWindupActive && AttackTimer > activeEnd && AttackTimer <= activeEnd + SlashReturnTicks)
                {
                    // Ease back to idle after the swing (unless a combo's next windup already armed).
                    float progress = MathHelper.Clamp((AttackTimer - activeEnd) / (float)SlashReturnTicks, 0f, 1f);
                    float returnEase = 1f - (float)Math.Pow(1f - progress, 2f);
                    phi = MathHelper.Lerp(SlashPhi(SlashActiveKind, endProgress), idlePhi, returnEase);
                    reach = MathHelper.Lerp(SlashReach(SlashActiveKind, endProgress), SwordIdleReach, returnEase);
                }
                else if (SlashWindupActive)
                {
                    // Wind up toward the swing's start pose. Ease-OUT (1-(1-t)^2.6): the blade reaches most
                    // of the cocked pose early and then creeps the last part, so the tell ends on a long
                    // settle instead of a linear stop. A chained windup starts from the previous swing's
                    // end pose (the hold between two cuts) rather than from the idle rest.
                    float windupProgress = SlashWindupEndTick > SlashWindupStartTick
                        ? MathHelper.Clamp((AttackTimer - SlashWindupStartTick) / (float)(SlashWindupEndTick - SlashWindupStartTick), 0f, 1f)
                        : 1f;
                    float windupEase = 1f - (float)Math.Pow(1f - windupProgress, 2.6f);
                    float fromPhi = idlePhi;
                    float fromReach = SwordIdleReach;

                    if (SlashWindupChained && SlashActiveKind >= 0)
                    {
                        fromPhi = SlashPhi(SlashActiveKind, endProgress);
                        fromReach = SlashReach(SlashActiveKind, endProgress);
                    }

                    phi = MathHelper.Lerp(fromPhi, SlashPhi(SlashWindupKind, 0f), windupEase);
                    reach = MathHelper.Lerp(fromReach, SlashReach(SlashWindupKind, 0f), windupEase);
                }
                // else: a non-sword cast (bones/nova/etc.) — the blade simply rests at idle.
            }

            if (State == AttackState.FollowUpSlash && lockedKind == 2 && SlashWindupActive)
            {
                // The thrust tell keeps the blade leveled at the player, but a restrained tremor makes
                // the stored forward burst visible before the shared black leap flash fires below him.
                phi += (float)Math.Sin(AttackTimer * 2.7f) * 0.025f;
                reach += (float)Math.Sin(AttackTimer * 3.9f) * 3.5f;
            }

            // The blade ROTATES about its hilt (a fixed-ish point near NPC.Center, hidden behind the
            // torso) instead of orbiting its own texture-center around a "shoulder" at radius `reach`
            // — the old translate-by-reach approach is what visibly detached the sword from Nito's
            // hand. `liftFactor` peaks when phi points straight up (a real swordsman's shoulder rises
            // for an overhead swing) and is 0 at both the forward pose and the horizontal idle rest
            // (idlePhi = -Pi), so the hand only ever drifts a little — it has to stay concealed behind
            // the body silhouette at every frame, per the reference screenshot markup.
            float theta = dir >= 0 ? phi : MathHelper.Pi - phi;
            float liftFactor = MathHelper.Clamp((float)Math.Sin(-phi), 0f, 1f);
            Vector2 handPivot = swordAnchor + new Vector2(
                dir * (SwordPivotX + liftFactor * HandDriftMax),
                SwordPivotY - liftFactor * HandLiftMax);

            // The sword sprite points LEFT (tip at texX=2, pommel at texX=246), same as the body art,
            // so it flips on exactly the same condition the body now does.
            bool flip = faceRight;
            float swordRotation = flip ? theta : theta - MathHelper.Pi;
            SpriteEffects swordEffects = flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            // Origin = the handle pixel in the (unflipped) source art, mirrored to the other side of
            // the texture when flipped — SpriteEffects mirrors sampling within the fixed draw quad
            // rather than around the origin, so a single origin.X would keep the pivot correct for
            // only one facing (the same bug fixed below for the body's BodyDrawCenterX).
            float swordOriginX = flip ? sword.Width - SwordHandleTexX : SwordHandleTexX;
            Vector2 swordOrigin = new Vector2(swordOriginX, SwordHandleTexY);
            // Thrust (kind 2) never rotates — SlashPhi holds it at 0 and sells the lunge purely via
            // `reach` ramping 60->180 — so stretch the blade along its own length instead of
            // translating the (hidden) hand forward, which would drag the grip out from behind the
            // torso. Reach-driven rather than kind-gated, so it needs no extra state and naturally
            // settles back to ~1x at idle and during every other (near-constant-reach) swing kind.
            float lengthScale = 1f + MathHelper.Clamp((reach - SwordIdleReach) / 100f, -0.15f, 0.45f);

            // Sword drawn BEFORE the body so it sits BEHIND Nito's silhouette. Phase two adds a
            // restrained crimson cached glow around the same anchored sword sprite; it cannot drift
            // away from the grip or imply a larger damage shape because every copy shares the pose.
            Vector2 swordDrawPosition = handPivot - screenPos;
            Vector2 swordScale = new Vector2(lengthScale, 1f) * NPC.scale;
            if (PhaseTwo)
            {
                float glowPulse = 0.4f + 0.08f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f);
                Color swordGlow = new Color(190, 20, 34, 0) * glowPulse;
                for (int i = 0; i < 4; i++)
                {
                    Vector2 glowOffset = (MathHelper.PiOver2 * i).ToRotationVector2() * 3f;
                    spriteBatch.Draw(sword, swordDrawPosition + glowOffset, null, swordGlow,
                        swordRotation, swordOrigin, swordScale, swordEffects, 0f);
                }
            }
            spriteBatch.Draw(sword, swordDrawPosition, null, drawColor, swordRotation, swordOrigin, swordScale, swordEffects, 0f);

            // Dash tell: the sword flashes pure white for ThrustFlashTicks starting ThrustFlashLeadTicks
            // before the launch. Snaps on over 2 ticks, then fades as (1-t)^2 so it reads as a flash, not
            // a glow. Derived from AttackTimer, so clients draw the same flash with no extra sync.
            bool dashTell = State == AttackState.ImpalingThrust || (State == AttackState.FollowUpSlash && lockedKind == 2);
            int flashTick = AttackTimer - ThrustFlashStartTick;
            if (dashTell && flashTick >= 0 && flashTick < ThrustFlashTicks)
            {
                float flashFade = 1f - flashTick / (float)ThrustFlashTicks;
                float flashRamp = Math.Min(1f, (flashTick + 1) / 2f);
                float flashIntensity = flashRamp * flashFade * flashFade;

                // A drawn sprite is tinted by its own pixels, so a dark blade can't be flashed white by
                // colour alone. Build a premultiplied-white copy of the sword (same alpha) once.
                if (swordSilhouette == null || swordSilhouette.IsDisposed)
                {
                    Color[] silhouettePixels = new Color[sword.Width * sword.Height];
                    sword.GetData(silhouettePixels);

                    for (int i = 0; i < silhouettePixels.Length; i++)
                    {
                        int alpha = silhouettePixels[i].A;
                        silhouettePixels[i] = new Color(alpha, alpha, alpha, alpha);
                    }

                    swordSilhouette = new Texture2D(Main.graphics.GraphicsDevice, sword.Width, sword.Height);
                    swordSilhouette.SetData(silhouettePixels);
                }

                // Alpha 0 = additive under Terraria's premultiplied blend (the phase-2 glow above uses
                // the same trick). Second, wider pass at lower strength is the halo around the blade.
                Color flashColor = new Color(255, 255, 255, 0) * flashIntensity;
                Color haloColor = new Color(255, 255, 255, 0) * (flashIntensity * 0.4f);
                Vector2 haloScale = new Vector2(swordScale.X * 1.04f, swordScale.Y * 1.7f);
                spriteBatch.Draw(swordSilhouette, swordDrawPosition, null, haloColor, swordRotation, swordOrigin, haloScale, swordEffects, 0f);
                spriteBatch.Draw(swordSilhouette, swordDrawPosition, null, flashColor, swordRotation, swordOrigin, swordScale, swordEffects, 0f);
            }

            float bodyOriginX = faceRight ? BodyWidth - BodyDrawCenterX : BodyDrawCenterX;
            spriteBatch.Draw(body, drawBottom - screenPos, frame, drawColor, NPC.rotation, new Vector2(bodyOriginX, FrameHeight), NPC.scale, effects, 0f);
            return false;
        }

        ///<summary>Nito stands in Skeletron's progression slot while Skeletron remains an optional,
        ///separately tracked boss. downedBoss3 is Terraria's required dungeon-unlock compatibility bit;
        ///NewSlain records only Nito here, preserving Skeletron's real first-kill rewards and history.</summary>
        public override void OnKill()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            NPC.downedBoss3 = true;

            RegisterFirstKill(NPC.type);

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.WorldData);
            }
        }

        static void RegisterFirstKill(int npcType)
        {
            Terraria.ModLoader.Config.NPCDefinition definition = new(npcType);
            if (!tsorcRevampWorld.NewSlain.ContainsKey(definition))
            {
                tsorcRevampWorld.NewSlain.Add(definition, 1);
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 8; i++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.BoneTorch, hit.HitDirection, -1f, 120, default, 0.9f);
            }
            if (NPC.life <= 0)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 1f, Pitch = -0.25f }, NPC.Center);
                UsefulFunctions.ScreenShake(NPC.Center, 8f, 20);
                for (int i = 0; i < 70; i++)
                {
                    Dust dust = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(80f, 120f), Main.rand.NextBool() ? DustID.BoneTorch : DustID.Shadowflame, Main.rand.NextVector2Circular(5f, 5f), 80, default, Main.rand.NextFloat(1f, 1.6f));
                    dust.noGravity = true;
                }
            }
        }
    }
}
