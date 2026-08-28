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
using tsorcRevamp.Projectiles.Enemy;

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
        const float ImpalingThrustMaxRange = 650f;

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
        const int SlashActiveTicks = 18; // must match NitoSwordSlash.timeLeft so the visible arc matches the hitbox
        const int SlashReturnTicks = 14;
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
            || State == AttackState.GravelordJudgment || State == AttackState.FollowUpSlash;

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
                        PickAttack(player);
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
                ProximityWeight(dist, 520f, 210f, 7f));
            AddAttackOption(pool, AttackState.TripleReaperCombo, PhaseTwo && horizontal <= 370f && sameLevel,
                ProximityWeight(dist, 220f, 240f, 11f));
            AddAttackOption(pool, AttackState.DraggingAdvance, horizontal >= 180f && horizontal <= 700f && sameLevel,
                ProximityWeight(dist, 390f, 330f, 8f));
            AddAttackOption(pool, AttackState.LeapingCleave, vertical > 80f || horizontal >= 380f,
                ProximityWeight(dist, 520f, 480f, 7f));
            AddAttackOption(pool, AttackState.SwordRain, vertical > 90f || horizontal >= 260f,
                ProximityWeight(dist, 520f, 460f, 7f));
            AddAttackOption(pool, AttackState.BoneVolley, horizontal >= 220f,
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
                => AttackFamily.SwordSequence,
            AttackState.DraggingAdvance or AttackState.LeapingCleave => AttackFamily.GapCloser,
            AttackState.SwordRain or AttackState.GravelordJudgment => AttackFamily.OverheadRain,
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
                    RunSwordAttack(globalNPC, player, lockedKind, Telegraph(30), 44, 84, SlashDamage);
                    break;

                case AttackState.BackhandSweep:
                    RunSwordAttack(globalNPC, player, lockedKind, Telegraph(26), 38, 78, SlashDamage);
                    break;

                case AttackState.OverheadCleave:
                    RunSwordAttack(globalNPC, player, lockedKind, Telegraph(HeavyTelegraph), 64, 116, HeavySlashDamage);
                    break;

                case AttackState.ImpalingThrust:
                    RunImpalingThrust(globalNPC, player);
                    break;

                case AttackState.TripleReaperCombo:
                    RunTripleCombo(globalNPC, player);
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
                    int followUpTelegraph = lockedKind == 2 ? 26 : 22;
                    int followUpRelease = lockedKind == 2 ? 34 : 30;
                    RunSwordAttack(globalNPC, player, lockedKind, followUpTelegraph,
                        followUpRelease, followUpRelease + 38, SlashDamage, canQueueFollowUp: false);
                    break;

                case AttackState.PhaseTransition:
                    RunPhaseTransition(globalNPC, player);
                    break;

                case AttackState.ComboRecovery:
                    RunComboRecovery();
                    break;

            }
        }

        void RunSwordAttack(tsorcRevampGlobalNPC globalNPC, Player player, int slashKind, int telegraphTicks,
            int releaseTick, int endTick, int damage, bool canQueueFollowUp = true)
        {
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
                float approachSpeed = slashKind == 1 ? 1.9f : slashKind == 3 ? 2.15f : 2.4f;
                AdvanceTowardPlayer(player, approachSpeed, 0.16f, updateFacing: true);
                SwordTelegraphDust(slashKind);
                if (State == AttackState.FollowUpSlash && slashKind == 2 && AttackTimer % 5 == 0)
                {
                    tsorcRevampAIs.SpawnLeapTelegraph(NPC, new Color(18, 2, 7));
                }
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
                float strikeStep = State == AttackState.FollowUpSlash && slashKind == 2
                    ? 7.1f
                    : slashKind == 1 ? 2.8f : slashKind == 3 ? 3.25f : 3.8f;
                NPC.velocity.X = lockedDir * strikeStep;
                SpawnSlash(slashKind, damage);
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
                EndAttack(60);
            }
        }

        ///<summary>A committed forward LUNGE rather than the old stationary poke — sells the thrust the
        ///way a real gap-closer reads (and a whiffed one leaves him overextended, a fair punish window)
        ///instead of a blade that merely stretches in place. NitoSwordSlash's hitbox already re-reads
        ///owner.Center every frame, so physically dashing Nito carries the whole thrust arc with him;
        ///no hitbox-side change needed. Kept as its own method (not RunSwordAttack) because it needs to
        ///drive velocity itself instead of just braking.</summary>
        void RunImpalingThrust(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            const int release = 44;
            const int end = 82;
            const float LungeSpeed = 7.8f;
            int telegraphTicks = Telegraph(34);
            int aimLockTick = Math.Max(16, telegraphTicks - 11);

            if (AttackTimer <= release)
            {
                globalNPC.AttackCommitted = true;
            }
            if (AttackTimer == 1)
            {
                TelegraphCue(Color.LightGray);
                ArmSlashWindup(lockedKind, 1, release);
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
            else if (AttackTimer == telegraphTicks + 1)
            {
                NPC.velocity.X = lockedDir * LungeSpeed; // the spring
                NPC.netUpdate = true;
            }
            else if (AttackTimer <= end)
            {
                // Momentum carries him through and past the strike, bleeding off gradually instead of
                // snapping to a stop — reads as a real lunge, not a teleport-in poke.
                NPC.velocity.X *= 0.94f;
            }
            if (AttackTimer == release)
            {
                SpawnSlash(lockedKind, SlashDamage);
            }
            if (AttackTimer >= end)
            {
                TryQueueMeleeFollowUp(player, lockedKind);
                EndAttack(70);
            }
        }

        void RunTripleCombo(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            globalNPC.AttackCommitted = AttackTimer <= 110;
            if (AttackTimer == 1)
            {
                TelegraphCue(new Color(180, 180, 210));
                ArmSlashWindup(0, 1, 30);
            }
            if (AttackTimer < 23)
            {
                AdvanceTowardPlayer(player, 2.4f, 0.17f, updateFacing: true);
                SwordTelegraphDust(0);
            }
            if (AttackTimer == 30)
            {
                NPC.velocity.X = lockedDir * 3.8f;
                SpawnSlash(0, SlashDamage);
            }
            if (AttackTimer == 34)
            {
                FacePlayer(player);
                ArmSlashWindup(3, 34, 58);
                NPC.netUpdate = true;
            }
            if (AttackTimer >= 34 && AttackTimer <= 47)
            {
                AdvanceTowardPlayer(player, 2.25f, 0.18f, updateFacing: true);
                SwordTelegraphDust(3);
            }
            else if (AttackTimer > 47 && AttackTimer < 58)
            {
                NPC.velocity.X *= 0.9f;
            }
            if (AttackTimer == 58)
            {
                NPC.velocity.X = lockedDir * 3.3f;
                SpawnSlash(3, SlashDamage);
            }
            if (AttackTimer == 63)
            {
                FacePlayer(player);
                ArmSlashWindup(1, 63, 92);
                NPC.netUpdate = true;
            }
            if (AttackTimer >= 63 && AttackTimer <= 78)
            {
                AdvanceTowardPlayer(player, 2.1f, 0.16f, updateFacing: true);
                SwordTelegraphDust(1);
            }
            else if (AttackTimer > 78 && AttackTimer < 92)
            {
                NPC.velocity.X *= 0.9f;
            }
            if (AttackTimer == 92)
            {
                NPC.velocity.X = lockedDir * 3f;
                SpawnSlash(1, HeavySlashDamage);
            }
            if (AttackTimer > 92)
            {
                NPC.velocity.X *= 0.91f;
            }
            if (AttackTimer >= 132)
            {
                RecordCompletedAttack(AttackState.TripleReaperCombo);
                ComboRecoveryTicks = 30;
                StartAttack(AttackState.ComboRecovery, player);
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
                NPC.velocity.X = MathHelper.Clamp(MathHelper.Lerp(NPC.velocity.X, lockedDir * RunSpeed, RunAccel), -RunSpeed, RunSpeed);
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
            else if (AttackTimer > SlashActiveStartTick + SlashActiveTicks && NPC.collideY)
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
            if (AttackTimer > SlashActiveStartTick + SlashActiveTicks && NPC.collideY)
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
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<NitoDeathNova>(), DeathDamage, 5f, Main.myPlayer, 300f);
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
                NPC.velocity.X = lockedDir * 3.2f;
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
                NPC.velocity.X = lockedDir * 3.2f;
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
            AttackCooldown = Math.Max(30, (int)(cooldown * phaseRecoveryScale)) + Main.rand.Next(variance);
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
            if (vertical > 135f || horizontal > 700f)
            {
                followUp = AttackState.LeapingCleave;
            }
            else if (horizontal >= ImpalingThrustMinRange && horizontal <= ImpalingThrustMaxRange)
            {
                followUp = AttackState.ImpalingThrust;
            }
            else if (horizontal > 330f)
            {
                followUp = AttackState.DraggingAdvance;
            }
            else
            {
                followUp = AttackState.OverheadCleave;
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
        ///their decision-lock tick so the final strike cannot rotate through a successful dodge.</summary>
        void AdvanceTowardPlayer(Player player, float topSpeed, float acceleration, bool updateFacing)
        {
            if (updateFacing)
            {
                FacePlayer(player);
            }
            float desiredVelocity = lockedDir * topSpeed;
            NPC.velocity.X = MathHelper.Clamp(
                MathHelper.Lerp(NPC.velocity.X, desiredVelocity, acceleration), -topSpeed, topSpeed);
            FootstepEffects();
        }

        void SpawnSlash(int kind, int damage)
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = kind == 1 ? -0.25f : 0.05f }, NPC.Center);
            // The loose sword layer now plays the visible swing itself (see PreDraw's active-window);
            // the projectile is just a matching invisible hitbox. Record the release so both agree.
            SlashWindupActive = false;
            SlashActiveKind = kind;
            SlashActiveStartTick = AttackTimer;
            SlashActiveDirection = lockedDir;
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
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                ModContent.ProjectileType<NitoSwordSlash>(), finalDamage, 5f, Main.myPlayer,
                NPC.whoAmI, kind, SlashActiveDirection);
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
        void ArmSlashWindup(int kind, int windupStartTick, int releaseTick)
        {
            SlashWindupKind = kind;
            SlashWindupStartTick = windupStartTick;
            SlashWindupEndTick = Math.Max(windupStartTick + 1, releaseTick);
            SlashWindupActive = true;
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
            if (Main.netMode != NetmodeID.Server)
            {
                tsorcRevampAIs.SpawnTelegraphFlash(NPC, color, NPC.Center + new Vector2(0f, -85f));
            }
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
            int activeEndTick = SlashActiveStartTick + SlashActiveTicks;
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
                int activeEnd = SlashActiveStartTick + SlashActiveTicks;
                if (SlashActiveKind >= 0 && AttackTimer >= SlashActiveStartTick && AttackTimer <= activeEnd)
                {
                    // The visible swing itself (mirrors the invisible NitoSwordSlash hitbox arc).
                    float progress = MathHelper.Clamp((AttackTimer - SlashActiveStartTick) / (float)SlashActiveTicks, 0f, 1f);
                    phi = SlashPhi(SlashActiveKind, progress);
                    reach = SlashReach(SlashActiveKind, progress);
                }
                else if (SlashActiveKind >= 0 && !SlashWindupActive && AttackTimer > activeEnd && AttackTimer <= activeEnd + SlashReturnTicks)
                {
                    // Ease back to idle after the swing (unless a combo's next windup already armed).
                    float progress = MathHelper.Clamp((AttackTimer - activeEnd) / (float)SlashReturnTicks, 0f, 1f);
                    phi = MathHelper.Lerp(SlashPhi(SlashActiveKind, 1f), idlePhi, progress);
                    reach = MathHelper.Lerp(SlashReach(SlashActiveKind, 1f), SwordIdleReach, progress);
                }
                else if (SlashWindupActive)
                {
                    // Wind up from idle toward the swing's start pose.
                    float windupProgress = SlashWindupEndTick > SlashWindupStartTick
                        ? MathHelper.Clamp((AttackTimer - SlashWindupStartTick) / (float)(SlashWindupEndTick - SlashWindupStartTick), 0f, 1f)
                        : 1f;
                    phi = MathHelper.Lerp(idlePhi, SlashPhi(SlashWindupKind, 0f), windupProgress);
                    reach = MathHelper.Lerp(SwordIdleReach, SlashReach(SlashWindupKind, 0f), windupProgress);
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
