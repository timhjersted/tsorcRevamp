using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Buffs;
using tsorcRevamp.Content.Items.BossBags;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Projectiles.Enemy.VesselOfSouls;
using tsorcRevamp.Content.Projectiles.VFX;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.VesselOfSouls
{
    // The Vessel of Souls — a bloated soul-stuffed reliquary eye that REPLACES the Eye of Cthulhu slot
    // (models HP/damage off EoC, sets the vanilla boss1 downed flag on death). Its signature verb is
    // INHALE: when the mouth-hole dilates (sprite frames 3–5) gravity turns on and something comes out.
    //
    // Bespoke FLYING AI (not FighterAI, not the Puppet system): eased acceleration-steered floating with
    // a top-level state machine (Idle / Attack / PhaseTransition / Dying) and a MoveMood sub-machine
    // (Orbit / Drift / Menace / Retreat). PHASE 1 = the open-world fight (4 attacks). At 50% it SWALLOWS
    // the player into a hazy-purple void (reusing the per-player EnterTheAbyss render) and PHASE 2 adds
    // 4 more attacks. Death is a survive-only spectacle. See tsorcDocs/VesselOfSouls_Design.md.
    //
    // Sprite: NPCs/Bosses/VesselOfSouls/VesselOfSouls.png 220×1990, 6 frames. Top 3 (0–2) = normal /
    // closed mouth. Bottom 3 (3–5) = mouth dilated (every inhale/gravity/breath move).
    [AutoloadBossHead] // loads VesselOfSouls_Head_Boss.png for the minimap arrow + boss bar icon
    public class VesselOfSouls : ModNPC, IStaggerable
    {
        public override string Texture => "tsorcRevamp/NPCs/Bosses/VesselOfSouls/VesselOfSouls";

        enum VesselState : byte { Idle = 0, Attack, PhaseTransition, Dying }
        enum MoveMood : byte { Orbit = 0, Drift, Menace, Retreat }

        enum VesselAttack : byte
        {
            None = 0,
            // Phase 1
            VesselLunge,    // EoC-style charge, 1–3 chained dashes
            SoulSpew,       // sweeping fan of PurpleSkull from the mouth
            GravemawTug,    // gentle gravity pull → point-blank Black Nip cone
            SpawnWatchers,  // conjure servant eyes that swarm-fire
            // Phase 2
            GravityWell,    // THE staggerable channel: full pull → Black Breath cone
            WatchingWall,   // ring of StrangeEye sentinels firing inward
            SoulNova,       // expanding true-annulus ring + radial skulls
            VoidPlunge,     // rise → crashing dive → radial skull shockwave
        }

        // ── Attack timings (ticks, 60/sec) ──
        const int LungeWindupTicks = 60; // 60t (1s) telegraph (+30t) so player has clear reaction time
        const int LungeDashTicks = 42;
        const int LungeRecoveryTicks = 30;
        const int SpewTelegraphTicks = 30;
        const int TugTelegraphTicks = 30;
        const int WatchersTelegraphTicks = 30;
        const int WellTelegraphTicks = 80;   // interruptible portion of the channel
        const int WellCommittedTicks = 40;   // committed portion (breath fires here)
        const int WellRecoveryTicks = 50;
        // Swallow set-piece timeline (RunSwallow)
        const int SwallowCapture = 240;     // 2s weak pull + 2s accelerating pull, then capture
        const int SwallowSuckDone = 300;    // 1s (60t) visible glide into the mouth, then the player is swallowed
        const int SwallowFadeDone = 480;    // 3s (180t) of fading to black after the swallow
        const int SwallowUnfadeDone = 513;  // quick unfade into the void
        const int SwallowEnd = 525;         // → phase 2
        const int WallTelegraphTicks = 40;
        const int NovaTelegraphTicks = 35;
        const float NovaMaxRadius = 960f;    // Soul Nova ring reach (px); it expands at a fixed speed, so twice the radius = twice the lifetime (~113t)
        const int PlungeRiseTicks = 40;
        const int DeathDurationTicks = 420;  // ~7s survive-only spectacle

        // ── Raw projectile spawn damage (about 2x per hit in Normal, 4x in Expert) ──
        int SkullDamage => ScaleDamage(12);
        int ConeDamage => ScaleDamage(14);
        int BreathDamage => ScaleDamage(15);
        int NovaDamage => ScaleDamage(16);
        int PlungeDamage => ScaleDamage(14);
        int ScaleDamage(int baseDamage) => SHM ? (int)(baseDamage * 1.3f * tsorcRevampWorld.SHMScale) : baseDamage;

        // ── State ──
        VesselState State = VesselState.Idle;
        MoveMood Mood = MoveMood.Orbit;
        VesselAttack CurrentAttack = VesselAttack.None;
        VesselAttack LastAttack = VesselAttack.None;

        int AttackTimer;
        int AttackCooldown = 120;
        int AttackPhase;             // sub-phase for multi-stage attacks (Lunge, VoidPlunge)
        int ChargesLeft;            // Vessel Lunge chained dashes remaining
        Vector2 LockedAim;          // committed dash/dive direction

        int MoodTimer;
        float OrbitAngle;
        int OrbitDir = 1;
        float OrbitRadius = 420f;
        Vector2 DriftAnchor;
        float BobPhase;

        bool MouthOpen;
        int FrameTimer;
        int FrameIndex;

        bool Phase2;
        bool SHM;
        bool statsInitialized;
        int _contactDamage = 24;    // captured base contact; zeroed during transition/death

        // Void (phase-2 arena) management
        bool VoidWasOn;
        int VoidRefresh;

        // Arena: a fixed oval, ~103.6 tiles wide by 78 tall, anchored on the spot the boss first spawned (captured in OnSpawn
        // on the server, then synced). It does NOT restrict the boss, which pursues anyone. In phase 2 a Firefly dust
        // ring marks the edge, players inside it get sucked into the void, and one who fought inside and then stays
        // who fought inside and then stays outside it gets a fuse: 4s of pull toward the boss, cancelled by returning inside, and after that a fatal drag into its mouth.
        public const float ArenaHalfWidth = 51.796875f * 16f;   // ellipse semi-axes, pixels: 828.75px = the previous 1105px x 0.75
        public const float ArenaHalfHeight = 39f * 16f;         // 624px = the previous 520px x 1.2

        // The oval's centre sits this far ABOVE the spawn point (-104px), so making the arena 20% taller extends only its
        // top: the bottom of the ring stays exactly where it was (spawn.Y + 520).
        const float ArenaCenterShiftY = 32.5f * 16f - ArenaHalfHeight;
        public Vector2 ArenaCenter;

        // True while the phase-2 void should be showing. Recomputed every tick on every machine (AI runs everywhere),
        // so the void buff and the boundary can read it without a packet of their own.
        public bool VoidActive { get; private set; }

        // Fleeing-the-arena bookkeeping for THIS machine's local player only (a player's own velocity, position and death
        // are owner-authoritative, so the server and other clients never touch it). Committed = was inside the arena while
        // the void was up. The sequence: cross the edge and stay out for FleeGraceTicks -> message, and a fuse starts, with a
        // steady weak pull toward the mouth. Getting back INSIDE cancels it (fuse and grace both reset), but once the 4s fuse
        // reaches FleeFuseTicks it is locked in: the boss drags you into its mouth (the swallow's glide) and you die, wherever
        // you are by then — being pulled back inside no longer saves you.
        const int FleeGraceTicks = 90;
        const int FleeFuseTicks = 240;                 // 4s to the point of no return
        const float FleePullPerTick = 0.06f;           // the swallow's opening pull (0.035 -> 0.08), held steady
        const float FleePullSpeedCap = 2.5f;           // and its opening speed cap (2 -> 3)
        bool _fleeCommitted;
        int _fleeGraceTimer = FleeGraceTicks;
        int _fleeFuseTimer;                            // 0 = no fuse; counts up from 1 while outside, back to 0 on re-entry
        bool _fleeAnnounced;
        int _fleeGlideTimer;
        int _fleeGlideTicks;                           // > 0 while being dragged into the mouth
        int _fleeBlackHoldTimer;                       // counts down after the kill while the screen stays fully black
        int _fleeFadeInTimer;                          // then counts down while it fades back in
        const int FleeFadeTicks = 30;                  // fade to black over the last 30 ticks of the drag, and back in over 30
        const int FleeBlackHoldTicks = 30;             // ...with 30 ticks of full black between the two (90 in all)
        Vector2 _fleeGlideStart;

        ///<summary>The live Vessel, if one exists and has a known arena. Valid on any machine — the NPC and its synced fields
        ///exist everywhere, unlike the boss's own AI bookkeeping.</summary>
        public static bool TryGetActiveVessel(out VesselOfSouls vessel)
        {
            vessel = null;
            int npcIndex = NPC.FindFirstNPC(ModContent.NPCType<VesselOfSouls>());

            if (npcIndex < 0)
            {
                return false;
            }

            vessel = Main.npc[npcIndex].ModNPC as VesselOfSouls;

            // Zero = the arena centre hasn't arrived from the server yet.
            return vessel != null && vessel.ArenaCenter != Vector2.Zero;
        }

        ///<summary>Point-in-ellipse test against an arena centred on <paramref name="arenaCenter"/>. Margin grows both
        ///semi-axes outward (pixels); negative shrinks them.</summary>
        public static bool IsInsideArena(Vector2 arenaCenter, Vector2 point, float margin = 0f)
        {
            Vector2 offset = point - arenaCenter;
            float normalizedX = offset.X / (ArenaHalfWidth + margin);
            float normalizedY = offset.Y / (ArenaHalfHeight + margin);

            return normalizedX * normalizedX + normalizedY * normalizedY <= 1f;
        }

        // Death spectacle
        bool _deathSpectacleDone;
        bool _hideBody;             // reforming from dust: draw dust only
        Vector2 _swallowReturnPos;  // where to drop the local player after the swallow (client-local; not synced)
        // Swallow stage latches. Per-machine bookkeeping of which stages THIS machine has already played, so
        // deliberately not synced. The timeline runs off AttackTimer, which ReceiveExtraAI overwrites wholesale —
        // a snapshot landing on the transition tick makes the client's next evaluated value one PAST it, and an
        // exact-tick check is stepped straight over. Missing the reveal stage strands the player inside the boss,
        // so every stage fires on the first tick at OR PAST its threshold and latches instead.
        bool _swallowOpened;
        bool _swallowCaptured;
        bool _swallowArrived;       // the glide into the mouth has finished: the player is hidden and the flash has fired
        Vector2 _swallowSuckStart;  // where the local player was when the glide began (client-local; not synced)
        bool _swallowRevealed;
        bool _swallowReformed;
        bool _swallowLocalHeld;     // this machine's local player was inside the arena at capture, so it gets frozen + faded

        NPCDespawnHandler despawnHandler;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 6;
            NPCID.Sets.TrailCacheLength[NPC.type] = 9;
            NPCID.Sets.TrailingMode[NPC.type] = 0;
            NPCID.Sets.MPAllowedEnemies[NPC.type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(NPC.type);
        }

        public override void SetDefaults()
        {
            NPC.width = 200;
            NPC.height = 300;
            NPC.damage = 24;
            NPC.defense = 12;
            NPC.lifeMax = 3000;
            NPC.value = 40000f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0f;
            NPC.npcSlots = 20f;
            NPC.aiStyle = -1;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.lavaImmune = true;
            NPC.boss = true;
            NPC.rarity = 4;

            despawnHandler = new NPCDespawnHandler("The Vessel of Souls sinks back into the dark...", Color.MediumPurple, DustID.PurpleTorch);
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment) { }
        public override float SpawnChance(NPCSpawnInfo spawnInfo) => 0f;

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.HealingPotion;
        }

        public override void ModifyNPCLoot(Terraria.ModLoader.NPCLoot npcLoot)
        {
            // Expert: the treasure bag (holds 2× Soul of the Vessel). Non-expert: drop the 2 souls
            // directly so those players can still craft the Soul Reliquary + Gravemaw Tome.
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<VesselOfSoulsBag>()));
            IItemDropRule notExpert = new LeadingConditionRule(new Conditions.NotExpert());
            notExpert.OnSuccess(ItemDropRule.Common(ModContent.ItemType<SoulOfTheVessel>(), 1, 2, 2));
            npcLoot.Add(notExpert);
        }

        // ── Networking ──
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)State);
            writer.Write((byte)Mood);
            writer.Write(MoodTimer);
            writer.Write((byte)CurrentAttack);
            writer.Write(AttackTimer);
            writer.Write(AttackCooldown);
            writer.Write(AttackPhase);
            writer.Write(ChargesLeft);
            writer.Write(LockedAim.X);
            writer.Write(LockedAim.Y);
            writer.Write(OrbitAngle);
            writer.Write((sbyte)OrbitDir);
            writer.Write(OrbitRadius);
            writer.Write(DriftAnchor.X);
            writer.Write(DriftAnchor.Y);
            writer.Write(MouthOpen);
            writer.Write(Phase2);
            writer.Write(SHM);
            writer.Write(_hideBody);
            writer.Write(ArenaCenter.X);
            writer.Write(ArenaCenter.Y);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            State = (VesselState)reader.ReadByte();
            Mood = (MoveMood)reader.ReadByte();
            MoodTimer = reader.ReadInt32();
            CurrentAttack = (VesselAttack)reader.ReadByte();
            AttackTimer = reader.ReadInt32();
            AttackCooldown = reader.ReadInt32();
            AttackPhase = reader.ReadInt32();
            ChargesLeft = reader.ReadInt32();
            LockedAim.X = reader.ReadSingle();
            LockedAim.Y = reader.ReadSingle();
            OrbitAngle = reader.ReadSingle();
            OrbitDir = reader.ReadSByte();
            OrbitRadius = reader.ReadSingle();
            DriftAnchor.X = reader.ReadSingle();
            DriftAnchor.Y = reader.ReadSingle();
            MouthOpen = reader.ReadBoolean();
            Phase2 = reader.ReadBoolean();
            SHM = reader.ReadBoolean();
            _hideBody = reader.ReadBoolean();
            ArenaCenter.X = reader.ReadSingle();
            ArenaCenter.Y = reader.ReadSingle();
        }

        public override void OnSpawn(IEntitySource source)
        {
            // Server/single-player only: clients take the centre from the sync, so a client-side spawn can't seed it wrong.
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            ArenaCenter = NPC.Center + new Vector2(0f, ArenaCenterShiftY);
            NPC.netUpdate = true;
        }

        // ── Poise / stagger ──
        public void OnStagger(NPC npc)
        {
            if (State == VesselState.Attack && Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.NPCHit36 with { Volume = 0.8f, Pitch = -0.4f }, NPC.Center);
                for (int i = 0; i < 22; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(4f, 4f);
                    int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Shadowflame, vel.X, vel.Y, 120, default, 1.4f);
                    Main.dust[dustIndex].noGravity = true;
                }
            }
            // Never interrupt the swallow set-piece or the death sequence.
            if (State == VesselState.Attack)
            {
                KillOwnedWells();
                State = VesselState.Idle;
                CurrentAttack = VesselAttack.None;
                MouthOpen = false;
                AttackTimer = 0;
                AttackPhase = 0;
                AttackCooldown = Math.Max(AttackCooldown, 90);
            }
        }

        // ── Main AI ──
        public override void AI()
        {
            InitializeStats();

            // Once the killing blow has landed the death spectacle owns the fight. A player dying (or an
            // already-armed countdown) would otherwise delete the boss after 240t, before the 420t final blast.
            if (State != VesselState.Dying)
            {
                despawnHandler.TargetAndDespawn(NPC.whoAmI);
            }

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            Player player = Main.player[NPC.target];

            globalNPC.AttackTelegraphing = false;
            globalNPC.AttackCommitted = false;
            BobPhase += 0.05f;

            // Contact damage ONLY while it's physically ramming (Vessel Lunge dash / Void Plunge dive);
            // harmless to touch the rest of the time. dontTakeDamage only during the scripted set-pieces.
            bool scripted = State == VesselState.PhaseTransition || State == VesselState.Dying;
            bool ramming = State == VesselState.Attack
                && ((CurrentAttack == VesselAttack.VesselLunge && AttackPhase == 1)
                 || (CurrentAttack == VesselAttack.VoidPlunge && AttackPhase == 1));
            NPC.damage = ramming ? _contactDamage : 0;
            NPC.dontTakeDamage = scripted;

            // Ease the swallow fade back to 0 whenever we're not actively driving it.
            if (State != VesselState.PhaseTransition && VesselOfSoulsFadeSystem.FadeAlpha > 0f)
                VesselOfSoulsFadeSystem.FadeAlpha = MathHelper.Max(0f, VesselOfSoulsFadeSystem.FadeAlpha - 0.05f);

            // Keep the phase-2 void render alive until the final stand begins.
            // Void render is on for phase 2 — but NOT during the pre-reveal swallow (world still visible
            // while you're seized/fading), and NOT during the final stand/death spectacle.
            bool preReveal = State == VesselState.PhaseTransition && AttackTimer < SwallowFadeDone;
            // Also off once the despawn countdown is armed (everyone has died): otherwise the boss re-applies the
            // void to the respawned player for the rest of the countdown, then vanishes without ever clearing it.
            bool voidShouldBeOn = Phase2 && !preReveal && State != VesselState.Dying && !despawnHandler.IsDespawning;
            VoidActive = voidShouldBeOn;
            TickVoid(voidShouldBeOn);
            if (!Main.dedServ)
            {
                if (voidShouldBeOn)
                {
                    // Fog, the boundary ring and the coward check are all about whoever is looking: the local player,
                    // not the boss's target.
                    SpawnVoidFog(Main.LocalPlayer);
                    // DISABLED for now (kept for reuse): the Firefly ring that marked the arena edge.
                    // SpawnBoundaryRingDust();
                    TickFleeBoundary(Main.LocalPlayer);
                }
                else
                {
                    // Phase 1, the swallow, the death spectacle or a despawn: nobody is committed to the boundary.
                    if (_fleeGlideTicks > 0)
                    {
                        Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().ImpaleFreezeTimer = 0;   // never leave anyone pinned
                    }

                    ResetFleeState();
                    _fleeBlackHoldTimer = 0;
                    _fleeFadeInTimer = 0;
                }

                // DISABLED (kept for reuse, see the block comment near SpawnBoundaryRingDust): the pink cloud's per-tick update.
                // UpdateRingDust(voidShouldBeOn);

                // Flow of dust from a condemned player into the mouth (runs every tick so the last dust finish after a reset).
                UpdateSuckDust();
            }

            if (globalNPC.StaggerTimer > 0 && State != VesselState.PhaseTransition && State != VesselState.Dying)
            {
                NPC.velocity *= 0.9f;
                if (Main.rand.NextBool(3))
                {
                    int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Shadowflame, 0f, 1f, 120, default, 1f);
                    Main.dust[dustIndex].velocity *= 0.3f;
                }
                UpdateAura();
                return;
            }

            // One-time phase transition at 50%. Server-only: the State this reads lags on a client, so a client
            // still showing Idle would open the swallow — screen shake, capture cue and all — while the server is
            // mid-attack, then get yanked back by the next snapshot.
            if (Main.netMode != NetmodeID.MultiplayerClient
                && !Phase2 && State == VesselState.Idle && NPC.life <= NPC.lifeMax / 2)
            {
                StartPhaseTransition();
            }

            switch (State)
            {
                case VesselState.Idle:
                    RunIdle(globalNPC, player);
                    break;

                case VesselState.Attack:
                    RunAttack(globalNPC, player);
                    break;

                case VesselState.PhaseTransition:
                    RunSwallow(player);
                    break;

                case VesselState.Dying:
                    RunDying(player);
                    break;

            }

            UpdateRotation(player);
            UpdateAura();
            ClampToWorld();
        }

        /* ── DISABLED, KEPT FOR REUSE: the dark-pink "tracked cloud" ring dust ─────────────────────────────────────────
           Replaced by Gwyn's plain Firefly ring below. To bring it back: uncomment this block, call SpawnPinkCloudRingDust()
           from the ring block in AI() where SpawnBoundaryRingDust() is called, and uncomment the UpdateRingDust(...) call there.
           The cloud dust are driven per tick (RingDustTrack) because the game shrinks/slows every noGravity dust type except
           Firefly; see the comment inside. ─────────────────────────────────────────────────────────────────────────────────
        // Gwyn's coward-ring dust idea (TickCowardRing), run around the arena oval in pink. Gwyn's ring is a few sparks plus
        // a huge loose cloud of Firefly dust (100 a tick). The cloud is what makes it diffuse, and it can't be copied by
        // just swapping the dust ID: the game shrinks and slows EVERY noGravity dust type except Firefly (velocity x0.92,
        // scale -0.04 a tick), so any other type dies in ~22 ticks after drifting a few pixels — a thin dotted line. And
        // Firefly itself is hard-coded to draw white, so it can't be tinted. So the cloud dust here (PinkFairy / CrimsonTorch)
        // are DRIVEN by RingDustTrack: each one drifts slowly around the ring for its whole life, wobbling in and out of
        // it, fading in and out, which is what Firefly's own update does. Visual only: every machine draws its own.
        const int RingCloudPerTick = 10;
        const int RingCloudMinLife = 90;
        const int RingCloudMaxLife = 130;
        const float RingCloudMaxViewDistance = 1500f;   // only seed cloud within this of the local player; the rest is off-screen

        sealed class RingDustTrack
        {
            public Dust Dust;
            public float Angle;            // position on the ellipse, radians
            public float AngularSpeed;     // radians per tick (sign = direction of travel)
            public float RadialOffset;     // fixed offset off the ring line, pixels (+ = outward)
            public float WobblePhase;
            public float Scale;
            public int Age;
            public int Life;
        }

        readonly System.Collections.Generic.List<RingDustTrack> _ringDust = new();

        void SpawnPinkCloudRingDust()
        {
            if (ArenaCenter == Vector2.Zero)
            {
                return;
            }

            // Gwyn's sparks, kept sparse: they are the only part that sits exactly on the line.
            SpawnRingDustLayer(DustID.PinkTorch, 1, 1f);
            SpawnRingDustLayer(DustID.CrimsonTorch, 2, 2f);

            // The cloud. Angles are random on the ellipse like Gwyn's DustRing; ones that would be off-screen are skipped.
            for (int i = 0; i < RingCloudPerTick; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 seedPoint = ArenaCenter + new Vector2(ArenaHalfWidth * MathF.Cos(angle), ArenaHalfHeight * MathF.Sin(angle));

                if (Vector2.Distance(seedPoint, Main.LocalPlayer.Center) > RingCloudMaxViewDistance)
                {
                    continue;
                }

                // Half glowing pink fairy, half darker crimson, so the cloud has depth instead of one flat colour.
                int dustType = DustID.PinkFairy;

                if (Main.rand.NextBool())
                {
                    dustType = DustID.CrimsonTorch;
                }

                float scale = Main.rand.NextFloat(1.1f, 1.7f);
                Dust cloudDust = Dust.NewDustPerfect(seedPoint, dustType, Vector2.Zero, 255, default, scale);
                cloudDust.noGravity = true;
                cloudDust.noLightEmittence = true;

                // Mean radius ~625px, so 2-3.5 px/tick of travel is this many radians a tick. Counter-clockwise like
                // Gwyn's -3 layer for half of them and clockwise for the rest keeps the cloud from streaming one way.
                float speed = Main.rand.NextFloat(2f, 3.5f) / 625f;

                if (Main.rand.NextBool())
                {
                    speed = -speed;
                }

                RingDustTrack track = new RingDustTrack
                {
                    Dust = cloudDust,
                    Angle = angle,
                    AngularSpeed = speed,
                    RadialOffset = Main.rand.NextFloat(-22f, 22f),
                    WobblePhase = Main.rand.NextFloat(MathHelper.TwoPi),
                    Scale = scale,
                    Life = Main.rand.Next(RingCloudMinLife, RingCloudMaxLife),
                };

                // Tag the dust so a recycled slot (NewDust clears customData) is never mistaken for ours.
                cloudDust.customData = track;
                _ringDust.Add(track);
            }
        }

        // Advances every tracked cloud dust one tick, or removes them all when the ring is off (phase 1, the swallow,
        // the death spectacle, a despawn). Velocity is zeroed and position/scale/alpha are set outright every tick, so
        // vanilla's per-type dust decay can't shrink or slow them.
        void UpdateRingDust(bool ringActive)
        {
            for (int i = _ringDust.Count - 1; i >= 0; i--)
            {
                RingDustTrack track = _ringDust[i];
                Dust dust = track.Dust;
                bool stillOurs = dust.active && ReferenceEquals(dust.customData, track);
                track.Age++;

                if (!stillOurs || !ringActive || track.Age >= track.Life)
                {
                    if (stillOurs)
                    {
                        dust.active = false;
                    }

                    _ringDust.RemoveAt(i);
                    continue;
                }

                track.Angle += track.AngularSpeed;
                float cosine = MathF.Cos(track.Angle);
                float sine = MathF.Sin(track.Angle);

                // Outward normal of the ellipse (proportional to (b cos t, a sin t)); the wobble breathes in and out of
                // the ring line and bobs a little vertically, like a firefly.
                Vector2 outward = new Vector2(ArenaHalfHeight * cosine, ArenaHalfWidth * sine).SafeNormalize(Vector2.UnitX);
                float wobble = MathF.Sin(track.Age * 0.07f + track.WobblePhase) * 9f;
                Vector2 ringPoint = ArenaCenter + new Vector2(ArenaHalfWidth * cosine, ArenaHalfHeight * sine);

                dust.velocity = Vector2.Zero;
                dust.position = ringPoint + outward * (track.RadialOffset + wobble)
                    + new Vector2(0f, MathF.Sin(track.Age * 0.11f + track.WobblePhase) * 3f);
                dust.scale = track.Scale;

                // Fade in over the first 12 ticks, hold soft (alpha 90), fade out over the last 30. Alpha 255 = invisible.
                float alpha = 90f;

                if (track.Age < 12)
                {
                    alpha = MathHelper.Lerp(255f, 90f, track.Age / 12f);
                }
                else if (track.Age > track.Life - 30)
                {
                    alpha = MathHelper.Lerp(90f, 255f, (track.Age - (track.Life - 30)) / 30f);
                }

                dust.alpha = (int)alpha;
            }
        }
        */

        // DISABLED (the call in AI is commented out; kept for reuse). The arena boundary: Gwyn's coward ring (TickCowardRing) around the oval — his Firefly layer, and only that. Firefly
        // is the one dust type the game does NOT shrink or slow when noGravity is set, so each mote drifts along the ring at
        // its tangent speed for ~140 ticks, bobbing and glowing, and thousands of them make the loose diffuse band. It draws
        // white/pale whatever its colour. Gwyn spawns 100 a tick on a 2000px circle (12,566px round); this oval is ~4,590px
        // round, so the count is scaled by that ratio (x0.365) to keep the same density per pixel. Visual only: every machine
        // draws its own.
        const int BoundaryFireflyPerTick = 37;

        void SpawnBoundaryRingDust()
        {
            if (ArenaCenter == Vector2.Zero)
            {
                return;
            }

            SpawnRingDustLayer(DustID.Firefly, BoundaryFireflyPerTick, -3f);

            // Cap the ring at ~70% opacity. Firefly runs its own alpha (dust alpha 0 = fully opaque): it takes 20 off every
            // tick until the mote has grown, THEN adds 6 a tick to fade out. Left alone that peaks at 100%. Our AI runs before
            // the game's dust update, so raising anything below 97 back to 97 leaves it at 77 after that update's -20, i.e.
            // (255 - 77) / 255 = 70% opaque, through the whole fade-in and hold. The fade-out (alpha rising past 97) is untouched.
            for (int i = 0; i < Main.dust.Length; i++)
            {
                Dust dust = Main.dust[i];

                if (dust.active && ReferenceEquals(dust.customData, RingDustTag) && dust.alpha < RingDustAlphaFloor)
                {
                    dust.alpha = RingDustAlphaFloor;
                }
            }
        }

        // Marks ring dust so the opacity pass above can find it (NewDust clears customData when a slot is reused, so a stale
        // slot is never mistaken for ours). One shared tag is enough: nothing else needs to tell individual dust apart.
        static readonly object RingDustTag = new object();
        const int RingDustAlphaFloor = 97;

        // Scatters `count` dust at random angles on the arena ellipse, each moving along the tangent at `tangentSpeed`
        // (negative = the other way round). Same as UsefulFunctions.DustRing, which only does circles — plus a cull: Firefly
        // lives ~140 ticks, and seeding the whole 5,270px ring every tick would put the game near its 6,000-dust cap, so
        // points farther than 1,500px from the local player (off-screen anyway) are skipped.
        void SpawnRingDustLayer(int dustType, int count, float tangentSpeed)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float cosine = MathF.Cos(angle);
                float sine = MathF.Sin(angle);

                Vector2 position = ArenaCenter + new Vector2(ArenaHalfWidth * cosine, ArenaHalfHeight * sine);

                if (Vector2.Distance(position, Main.LocalPlayer.Center) > 1500f)
                {
                    continue;
                }

                // Derivative of (a cos t, b sin t) is (-a sin t, b cos t): the direction of travel along the oval.
                Vector2 tangent = new Vector2(-ArenaHalfWidth * sine, ArenaHalfHeight * cosine).SafeNormalize(Vector2.UnitX);

                Dust ringDust = Dust.NewDustPerfect(position, dustType, tangent * tangentSpeed, RingDustAlphaFloor);
                ringDust.noGravity = true;
                ringDust.customData = RingDustTag;
            }
        }

        // The nearest spot to `preferredCenter` where a box of `size` fits without touching any solid tile (8px margin), found
        // by checking growing squares of candidates 32px apart, out to ~960px. `minCenterY` refuses candidates above that
        // world Y (smaller Y = higher), so the boss can't be placed too high; pass float.NegativeInfinity for no limit. Falls
        // back to the preferred point if nothing is open, so a caller never gets a nonsense position. Used for both the
        // boss's reform point and the player's drop point after the swallow.
        Vector2 FindOpenCenter(Vector2 preferredCenter, Vector2 size, float minCenterY)
        {
            const float step = 32f;
            const int maxRings = 30;

            for (int ring = 0; ring <= maxRings; ring++)
            {
                for (int offsetX = -ring; offsetX <= ring; offsetX++)
                {
                    for (int offsetY = -ring; offsetY <= ring; offsetY++)
                    {
                        // Only the outline of this ring's square: the inside was checked on earlier passes.
                        if (Math.Max(Math.Abs(offsetX), Math.Abs(offsetY)) != ring)
                        {
                            continue;
                        }

                        Vector2 candidate = preferredCenter + new Vector2(offsetX, offsetY) * step;

                        if (candidate.Y < minCenterY)
                        {
                            continue;
                        }

                        Vector2 topLeft = candidate - size * 0.5f - new Vector2(8f);

                        if (!Collision.SolidCollision(topLeft, (int)size.X + 16, (int)size.Y + 16))
                        {
                            return candidate;
                        }
                    }
                }
            }

            return preferredCenter;
        }

        // The local player only. See the sequence described at the _flee fields. Someone who never came inside (an onlooker
        // walking past) is left alone. Death and the pull both act on the player's own client, which owns them, so no packet
        // is needed and each client handles only itself.
        void TickFleeBoundary(Player player)
        {
            // The black hold and then the fade back in after the kill. This has to run before the dead check below: the player
            // IS dead for the whole of it. Setting the alpha outright each tick also overrides the boss's generic 0.05-a-tick
            // fade decay that runs earlier in AI, which would otherwise shorten the fade to 20 ticks.
            if (_fleeBlackHoldTimer > 0)
            {
                VesselOfSoulsFadeSystem.FadeAlpha = 1f;
                _fleeBlackHoldTimer--;
            }
            else if (_fleeFadeInTimer > 0)
            {
                VesselOfSoulsFadeSystem.FadeAlpha = _fleeFadeInTimer / (float)FleeFadeTicks;
                _fleeFadeInTimer--;
            }

            if (player.dead || !player.active)
            {
                ResetFleeState();
                return;
            }

            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            // Point of no return passed: the drag into the mouth, then death. Nothing cancels this any more, including
            // being back inside the arena (so this check comes BEFORE the inside check below).
            if (_fleeGlideTicks > 0)
            {
                _fleeGlideTimer++;

                float glideProgress = MathHelper.Clamp(_fleeGlideTimer / (float)_fleeGlideTicks, 0f, 1f);
                float easedGlide = SuckEase(glideProgress);
                modPlayer.ImpaleFreezeTimer = 4;
                modPlayer.ImpaleWorldPosition = Vector2.Lerp(_fleeGlideStart, Mouth(), easedGlide);
                player.velocity = Vector2.Zero;

                // Full stream of purple dust from the player into the boss for the whole drag.
                SpawnSuckDust(player, 5);

                // Fade to black over the last FleeFadeTicks of the drag, so it is fully black on arrival — which is when the
                // player dies and the phase-2 void (a buff, cleared by death) and its background switch off at once. Doing that
                // in the open is what read as a glitchy cut; behind a black screen it is invisible.
                int glideTicksLeft = _fleeGlideTicks - _fleeGlideTimer;

                if (glideTicksLeft < FleeFadeTicks)
                {
                    VesselOfSoulsFadeSystem.FadeAlpha = 1f - glideTicksLeft / (float)FleeFadeTicks;
                }

                if (_fleeGlideTimer >= _fleeGlideTicks)
                {
                    // Arrival: the same flash and shake as the phase-2 swallow, then the kill.
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), Mouth(), Vector2.Zero,
                        ModContent.ProjectileType<TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.MediumPurple));
                    UsefulFunctions.ScreenShake(NPC.Center, 6f, 16);
                    SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 1f, Pitch = -0.3f }, Mouth());

                    modPlayer.ImpaleFreezeTimer = 0;
                    ResetFleeState();

                    // Fully black now: it holds there for FleeBlackHoldTicks, then fades back in, all through the death screen.
                    VesselOfSoulsFadeSystem.FadeAlpha = 1f;
                    _fleeBlackHoldTimer = FleeBlackHoldTicks;
                    _fleeFadeInTimer = FleeFadeTicks;
                    player.KillMe(PlayerDeathReason.ByNPC(NPC.whoAmI), 9999.0, 0);
                }

                return;
            }

            if (!IsOutsideArena(player))
            {
                // Inside (or back inside before the fuse ran out): everything is forgiven and starts over.
                _fleeCommitted = true;
                _fleeGraceTimer = FleeGraceTicks;
                _fleeFuseTimer = 0;
                _fleeAnnounced = false;
                return;
            }

            if (!_fleeCommitted)
            {
                return;
            }

            if (_fleeFuseTimer == 0)
            {
                _fleeGraceTimer--;

                if (_fleeGraceTimer > 0)
                {
                    return;
                }

                // Grace is over: the fuse starts.
                _fleeFuseTimer = 1;

                if (!_fleeAnnounced)
                {
                    _fleeAnnounced = true;
                    Main.NewText(LangUtils.GetTextValue("NPCs.VesselOfSouls.Coward"), 200, 60, 150);
                    SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.8f, Pitch = -0.7f }, player.Center);
                }
            }
            else
            {
                _fleeFuseTimer++;
            }

            // A steady, weak pull toward the mouth: the swallow's opening strength, held. Resistible (the fuse, not the pull,
            // is the threat), and it is what drags a fleeing player back toward the boss.
            Vector2 toMouth = Mouth() - player.Center;
            float mouthDistance = toMouth.Length();
            Vector2 pullDirection = toMouth.SafeNormalize(Vector2.UnitY);
            float towardSpeed = Vector2.Dot(player.velocity, pullDirection);

            if (towardSpeed < FleePullSpeedCap)
            {
                player.velocity += pullDirection * FleePullPerTick;
            }

            // Purple dust off the player that streams all the way into the boss, thickening as the fuse runs down: 2 a tick
            // at the start, 5 a tick just before the point of no return.
            int suckDustCount = 2 + (int)(3f * _fleeFuseTimer / (float)FleeFuseTicks);
            SpawnSuckDust(player, suckDustCount);

            if (_fleeFuseTimer >= FleeFuseTicks)
            {
                // Point of no return. The drag takes longer the farther away you are (60 to 120 ticks), so it always reads
                // as being pulled in rather than a teleport.
                _fleeGlideStart = player.Center;
                _fleeGlideTimer = 0;
                _fleeGlideTicks = (int)MathHelper.Clamp(mouthDistance / 14f, 60f, 120f);
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 1f, Pitch = -0.5f }, NPC.Center);
            }
        }

        // The swallow's glide curve, shared by the phase-2 swallow and the flee kill: progress 0..1 in, distance fraction out.
        // Starts slow (0.2x speed) and accelerates hard (2.6x at the end; last quarter covers ~51% of the distance, where the
        // old curve, 0.25t + 0.75t^2, covered ~39%), so the last ticks into the mouth read as a real gulp.
        static float SuckEase(float progress)
        {
            return 0.2f * progress + 0.8f * progress * progress * progress;
        }

        // One flow of dust from a doomed player into the boss. Each dust is DRIVEN for its whole short life (position set
        // every tick along the line from where it spawned to the boss's mouth, accelerating), because vanilla dust decays
        // in ~22 ticks after drifting a few pixels and would never get there. Same tracking pattern as the watcher's dust.
        sealed class SuckDustTrack
        {
            public Dust Dust;
            public Vector2 Start;
            public float StartScale;
            public int Age;
            public int Life;
        }

        readonly System.Collections.Generic.List<SuckDustTrack> _suckDust = new();

        void SpawnSuckDust(Player player, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 start = player.position + new Vector2(Main.rand.NextFloat(player.width), Main.rand.NextFloat(player.height));
                int dustType = DustID.PurpleTorch;

                if (Main.rand.NextBool(3))
                {
                    dustType = DustID.Shadowflame;
                }

                float startScale = Main.rand.NextFloat(1.2f, 1.7f);
                Dust dust = Dust.NewDustPerfect(start, dustType, Vector2.Zero, 60, default, startScale);
                dust.noGravity = true;
                dust.noLightEmittence = true;

                SuckDustTrack track = new SuckDustTrack
                {
                    Dust = dust,
                    Start = start,
                    StartScale = startScale,
                    Life = Main.rand.Next(24, 36),
                };

                // Tag the dust so a recycled slot (NewDust clears customData) is never mistaken for ours.
                dust.customData = track;
                _suckDust.Add(track);
            }
        }

        // Advances every tracked suck-dust one tick; the target is the boss's mouth as it is NOW, so the stream follows it.
        void UpdateSuckDust()
        {
            for (int i = _suckDust.Count - 1; i >= 0; i--)
            {
                SuckDustTrack track = _suckDust[i];
                Dust dust = track.Dust;
                bool stillOurs = dust.active && ReferenceEquals(dust.customData, track);
                track.Age++;

                if (!stillOurs || track.Age >= track.Life)
                {
                    if (stillOurs)
                    {
                        dust.active = false;
                    }

                    _suckDust.RemoveAt(i);
                    continue;
                }

                float progress = track.Age / (float)track.Life;
                float acceleratingTravel = progress * progress;   // slow off the player, fast into the mouth

                dust.velocity = Vector2.Zero;
                dust.position = Vector2.Lerp(track.Start, Mouth(), acceleratingTravel);
                dust.scale = track.StartScale * (1f - 0.5f * progress);
                dust.alpha = (int)MathHelper.Lerp(60f, 255f, progress * progress * progress);
            }
        }

        void ResetFleeState()
        {
            _fleeCommitted = false;
            _fleeGraceTimer = FleeGraceTicks;
            _fleeFuseTimer = 0;
            _fleeAnnounced = false;
            _fleeGlideTimer = 0;
            _fleeGlideTicks = 0;
        }

        // True when a player is known to be outside the arena. False while the arena centre is still unset (a client
        // that hasn't received its first sync), so an unsynced boss never treats everyone as out of bounds.
        bool IsOutsideArena(Player candidate)
        {
            return ArenaCenter != Vector2.Zero && !IsInsideArena(ArenaCenter, candidate.Center);
        }

        void InitializeStats()
        {
            if (statsInitialized)
            {
                return;
            }
            statsInitialized = true;
            _contactDamage = NPC.damage;
            if (tsorcRevampWorld.SuperHardMode)
            {
                SHM = true;
                NPC.lifeMax = 6000;
                NPC.life = NPC.lifeMax;
                NPC.damage = 44;
                _contactDamage = 44;
                NPC.defense = 20;
                NPC.netUpdate = true;
            }
        }

        #region Idle — movement + mood + attack picking

        void RunIdle(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            MouthOpen = false;
            if (player.dead || !player.active)
            {
                NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Zero, 0.03f);
                return;
            }

            UpdateMood(player);
            DoMoodMovement(player);

            if (Main.rand.NextBool(3))
            {
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.PurpleTorch, 0f, -0.6f, 160, default, 0.8f);
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].velocity *= 0.3f;
            }

            if (AttackCooldown > 0)
            {
                AttackCooldown--;
            }
            if (Main.netMode != NetmodeID.MultiplayerClient && AttackCooldown <= 0 && NPC.Distance(player.Center) < 1000f)
                PickAttack(player);
        }

        void UpdateMood(Player player)
        {
            // Every machine runs the countdown so the synced Mood expires at the same time on all of them.
            if (MoodTimer > 0)
            {
                MoodTimer--;
            }

            // Clients never CHOOSE a mood. SetMood rolls OrbitDir, OrbitRadius and DriftAnchor, which together
            // decide the whole flight path; those ride the mood's own snapshot. This gate sits above the
            // proximity escape below because that escape calls SetMood too.
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            float distTiles = NPC.Distance(player.Center) / 16f;

            // Inside 10 tiles: break off immediately, interrupting whatever mood is still running.
            if (distTiles < 10f && Mood != MoveMood.Retreat)
            {
                SetMood(MoveMood.Retreat, 40);
                return;
            }
            if (MoodTimer > 0)
            {
                return;
            }

            Span<(MoveMood mood, float weight)> pool = stackalloc (MoveMood, float)[]
            {
                (MoveMood.Orbit,   1.2f),
                (MoveMood.Drift,   1.0f),
                (MoveMood.Menace,  0.8f),
                (MoveMood.Retreat, distTiles < 18f ? 0.6f : 0.15f),
            };
            float total = 0f;
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i].mood == Mood)
                {
                    pool[i].weight *= 0.4f;
                }
                total += pool[i].weight;
            }
            float roll = Main.rand.NextFloat(total);
            for (int i = 0; i < pool.Length; i++)
            {
                roll -= pool[i].weight;
                if (roll <= 0f)
                {
                    SetMood(pool[i].mood, pool[i].mood switch
                    {
                        MoveMood.Orbit => Main.rand.Next(140, 260),
                        MoveMood.Drift => Main.rand.Next(120, 220),
                        MoveMood.Menace => Main.rand.Next(70, 110),
                        _ => Main.rand.Next(30, 55),
                    });
                    break;
                }
            }
        }

        void SetMood(MoveMood mood, int duration)
        {
            Mood = mood;
            MoodTimer = duration;
            if (mood == MoveMood.Orbit && Main.rand.NextBool(2))
            {
                OrbitDir = -OrbitDir;
            }
            if (mood == MoveMood.Orbit)
            {
                OrbitRadius = Main.rand.NextFloat(340f, 500f);
            }
            if (mood == MoveMood.Drift)
            {
                DriftAnchor = Main.rand.NextVector2Circular(360f, 300f) - new Vector2(0f, 120f);
            }
            NPC.netUpdate = true;
        }

        void DoMoodMovement(Player player)
        {
            Vector2 desired;
            float maxSpeed;
            float bob = (float)Math.Sin(BobPhase) * 26f;
            switch (Mood)
            {
                case MoveMood.Orbit:
                    OrbitAngle += OrbitDir * 0.018f;
                    desired = player.Center + OrbitAngle.ToRotationVector2() * OrbitRadius + new Vector2(0f, bob - 60f);
                    maxSpeed = 7.5f;
                    break;
                case MoveMood.Drift:
                    desired = player.Center + DriftAnchor + new Vector2(0f, bob);
                    if (NPC.Distance(desired) < 60f && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        DriftAnchor = Main.rand.NextVector2Circular(380f, 300f) - new Vector2(0f, 120f);
                        NPC.netUpdate = true;
                    }
                    maxSpeed = 6f;
                    break;
                case MoveMood.Menace:
                    MouthOpen = true;
                    Vector2 dir = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitY);
                    desired = player.Center - dir * 200f + new Vector2(0f, -40f);
                    maxSpeed = 4.5f;
                    SpawnInhaleMote();
                    break;
                default:
                    Vector2 away = (NPC.Center - player.Center).SafeNormalize(Vector2.UnitX);
                    desired = player.Center + away * 620f + new Vector2(0f, -80f);
                    maxSpeed = 11f;
                    break;
            }
            Steer(desired, maxSpeed, Mood == MoveMood.Retreat ? 0.09f : 0.055f);
        }

        void Steer(Vector2 desired, float maxSpeed, float responsiveness)
        {
            Vector2 to = desired - NPC.Center;
            float dist = to.Length();
            Vector2 seek = dist > 1f ? to / dist * MathHelper.Min(dist * 0.05f, maxSpeed) : Vector2.Zero;
            NPC.velocity = Vector2.Lerp(NPC.velocity, seek, responsiveness);
        }

        #endregion

        #region Attack selection

        void PickAttack(Player player)
        {
            float distTiles = NPC.Distance(player.Center) / 16f;
            Span<(VesselAttack a, float weight)> pool = Phase2
                ? stackalloc (VesselAttack, float)[]
                {
                    (VesselAttack.VesselLunge, (distTiles > 20f && distTiles < 55f) ? 1.0f : 0.3f),
                    (VesselAttack.SoulSpew,    1.0f),
                    (VesselAttack.GravityWell, distTiles < 40f ? 0.5f : 1.0f),
                    (VesselAttack.WatchingWall,0.8f),
                    (VesselAttack.SoulNova,    0.9f),
                    (VesselAttack.VoidPlunge,  0.9f),
                }
                : stackalloc (VesselAttack, float)[]
                {
                    (VesselAttack.VesselLunge,  (distTiles > 20f && distTiles < 55f) ? 1.1f : 0.4f),
                    (VesselAttack.SoulSpew,     1.0f),
                    (VesselAttack.GravemawTug,  distTiles < 45f ? 1.0f : 0.5f),
                    (VesselAttack.SpawnWatchers,0.7f),
                };
            float total = 0f;
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i].a == LastAttack)
                {
                    pool[i].weight *= 0.5f;
                }
                total += pool[i].weight;
            }
            float roll = Main.rand.NextFloat(total);
            for (int i = 0; i < pool.Length; i++)
            {
                roll -= pool[i].weight;
                if (roll <= 0f)
                {
                    StartAttack(pool[i].a);
                    return;
                }
            }
            StartAttack(pool[0].a);
        }

        void StartAttack(VesselAttack a)
        {
            State = VesselState.Attack;
            CurrentAttack = a;
            AttackTimer = 0;
            AttackPhase = 0;
            if (a == VesselAttack.VesselLunge)
            {
                ChargesLeft = Main.rand.Next(1, Phase2 ? 4 : 3);
            }
            NPC.netUpdate = true;
        }

        void EndAttack(int cooldown)
        {
            LastAttack = CurrentAttack;
            State = VesselState.Idle;
            CurrentAttack = VesselAttack.None;
            MouthOpen = false;
            AttackTimer = 0;
            AttackPhase = 0;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                AttackCooldown = cooldown + Main.rand.Next(60);
                NPC.netUpdate = true;
            }
        }

        #endregion

        #region Attack execution

        void RunAttack(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            if (player.dead || !player.active)
            {
                EndAttack(60);
                return;
            }
            AttackTimer++;
            switch (CurrentAttack)
            {
                case VesselAttack.VesselLunge:
                    RunVesselLunge(globalNPC, player);
                    break;

                case VesselAttack.SoulSpew:
                    RunSoulSpew(globalNPC, player);
                    break;

                case VesselAttack.GravemawTug:
                    RunGravemawTug(globalNPC, player);
                    break;

                case VesselAttack.SpawnWatchers:
                    RunSpawnWatchers(globalNPC, player);
                    break;

                case VesselAttack.GravityWell:
                    RunGravityWell(globalNPC, player);
                    break;

                case VesselAttack.WatchingWall:
                    RunWatchingWall(globalNPC, player);
                    break;

                case VesselAttack.SoulNova:
                    RunSoulNova(globalNPC, player);
                    break;

                case VesselAttack.VoidPlunge:
                    RunVoidPlunge(globalNPC, player);
                    break;

                default: EndAttack(120);
                break;
            }
        }

        // A1 — Vessel Lunge: recoil windup → committed dash → (chain) → recovery.
        void RunVesselLunge(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            globalNPC.AttackCommitted = true;
            switch (AttackPhase)
            {
                case 0: // windup
                    MouthOpen = true;
                    Vector2 away = (NPC.Center - player.Center).SafeNormalize(Vector2.UnitX);
                    float currentDist = Vector2.Distance(NPC.Center, player.Center);
                    // Dynamic retreat speed: back up stronger if close to maintain minimum charging distance (~380px)
                    float retreatSpeed = MathHelper.Lerp(12f, 4f, MathHelper.Clamp(currentDist / 380f, 0f, 1f));
                    NPC.velocity = Vector2.Lerp(NPC.velocity, away * retreatSpeed, 0.15f);
                    if (AttackTimer == 1)
                    {
                        SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.5f, Pitch = -0.2f }, NPC.Center);
                    }
                    SpawnInhaleMote();
                    // Pink "dash incoming" flash at the mouth, ~25 ticks before the dash launches.
                    if (AttackTimer == Math.Max(1, LungeWindupTicks - 25) && Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), Mouth(), Vector2.Zero,
                            ModContent.ProjectileType<TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.HotPink));
                    if (!Main.dedServ && AttackTimer >= LungeWindupTicks - 25 && Main.rand.NextBool(3))
                    {
                        Vector2 pinkMouth = Mouth();
                        int pinkDust = Dust.NewDust(pinkMouth - new Vector2(6f), 12, 12, DustID.PinkTorch, 0f, 0f, 100, default, 1.4f);
                        Main.dust[pinkDust].noGravity = true;
                        Main.dust[pinkDust].velocity *= 0.3f;
                    }
                    if (AttackTimer >= LungeWindupTicks)
                    {
                        LockedAim = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
                        NPC.velocity = LockedAim * 17f;
                        SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.8f, Pitch = 0.1f }, NPC.Center);
                        AttackPhase = 1;
                        AttackTimer = 0;
                        NPC.netUpdate = true;
                    }
                    break;
                case 1: // dash
                    MouthOpen = true;
                    NPC.velocity = LockedAim * 17f;
                    if (AttackTimer >= LungeDashTicks)
                    {
                        ChargesLeft--;
                        if (ChargesLeft > 0)
                        {
                            AttackPhase = 0;
                            AttackTimer = 0;
                            NPC.netUpdate = true;
                        }
                        else
                        {
                            AttackPhase = 2;
                            AttackTimer = 0;
                            NPC.netUpdate = true;
                        }

                    }
                    break;
                default: // recovery (no armor)
                    MouthOpen = false;
                    NPC.velocity *= 0.9f;
                    if (AttackTimer >= LungeRecoveryTicks)
                    {
                        EndAttack(Phase2 ? 200 : 260);
                    }
                    break;
            }
        }

        // A2 — Soul Spew: inhale telegraph → sweeping fan of homing skulls.
        void RunSoulSpew(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            globalNPC.AttackCommitted = true;
            MouthOpen = true;
            NPC.velocity *= 0.92f;
            if (AttackTimer == 1)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.6f, Pitch = -0.3f }, NPC.Center);
            }
            if (AttackTimer <= SpewTelegraphTicks)
            {
                MouthChargeTelegraph();
                return;
            }

            int spewTick = AttackTimer - SpewTelegraphTicks; // 1..
            if (spewTick % 5 == 1 && spewTick <= 36 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int idx = (spewTick - 1) / 5; // 0..6
                Vector2 mouth = Mouth();
                float baseAng = (player.Center - mouth).ToRotation();
                float spread = MathHelper.ToRadians((idx - 3) * 9f);
                Vector2 vel = (baseAng + spread).ToRotationVector2() * 8f;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), mouth, vel,
                    ModContent.ProjectileType<PurpleSkull>(), SkullDamage, 1f, Main.myPlayer, 0.02f);
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.35f, Pitch = 0.2f }, NPC.Center);
            }
            if (AttackTimer >= SpewTelegraphTicks + 45)
            {
                EndAttack(210);
            }
        }

        // A3 — Gravemaw Tug: gentle pull → point-blank Black Nip cone.
        void RunGravemawTug(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            globalNPC.AttackCommitted = true;
            MouthOpen = true;
            NPC.velocity *= 0.9f;
            if (AttackTimer == 1)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.6f, Pitch = -0.5f }, NPC.Center);
            }
            if (AttackTimer <= TugTelegraphTicks)
            {
                MouthChargeTelegraph();
                return;
            }
            if (AttackTimer == TugTelegraphTicks + 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), Mouth(), Vector2.Zero,
                    ModContent.ProjectileType<VesselGravityWell>(), 0, 0f, Main.myPlayer,
                    NPC.whoAmI, 45f, 450f); // gentle: short duration, small radius → weak pull
            }
            // Black Nip: a tight point-blank cone after the pull.
            if (AttackTimer == TugTelegraphTicks + 46 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 mouth = Mouth();
                float baseAng = (player.Center - mouth).ToRotation();
                for (int i = -1; i <= 1; i++)
                {
                    Vector2 vel = (baseAng + MathHelper.ToRadians(i * 12f)).ToRotationVector2() * 10f;
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), mouth, vel,
                        ModContent.ProjectileType<PurpleSkull>(), ConeDamage, 1f, Main.myPlayer, 0f);
                }
                SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.5f, Pitch = -0.2f }, NPC.Center);
            }
            if (AttackTimer >= TugTelegraphTicks + 80)
            {
                EndAttack(240);
            }
        }

        // A4 — Spawn the Watchers: conjure servant eyes.
        void RunSpawnWatchers(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            globalNPC.AttackCommitted = true;
            MouthOpen = true;
            NPC.velocity *= 0.9f;
            if (AttackTimer <= WatchersTelegraphTicks)
            {
                MouthChargeTelegraph();
                return;
            }
            if (AttackTimer == WatchersTelegraphTicks + 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int count = 3;
                for (int i = 0; i < count; i++)
                {
                    // Spawn at the mouth; each servant takes formation slot i and fans out around the player.
                    SpawnWatcher(NPC.Center, player.Center, mode: 0, fireOffset: i); // 4th arg = slot for servants
                }
                SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.7f, Pitch = 0.1f }, NPC.Center);
            }
            if (AttackTimer >= WatchersTelegraphTicks + 30)
            {
                EndAttack(300);
            }
        }

        // A5 — Gravity Well → Black Breath (the staggerable channel).
        void RunGravityWell(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            MouthOpen = true;
            NPC.velocity *= 0.85f;

            if (AttackTimer <= WellTelegraphTicks)
            {
                globalNPC.AttackTelegraphing = true; // interruptible — a poise break here cancels it (OnStagger)
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.9f, Pitch = -0.7f }, NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), Mouth(), Vector2.Zero,
                            ModContent.ProjectileType<VesselGravityWell>(), 0, 0f, Main.myPlayer,
                            NPC.whoAmI, WellTelegraphTicks, 760f); // full pull (radius ≥700 → strong)
                }
                MouthChargeTelegraph();
            }
            else
            {
                globalNPC.AttackCommitted = true;
                int wellTick = AttackTimer - WellTelegraphTicks;
                if (wellTick == 1)
                {
                    // Commit cue: "too late to interrupt" — kill the pull, flash, breathe.
                    KillOwnedWells();
                    SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.9f, Pitch = 0.1f }, NPC.Center);
                    UsefulFunctions.ScreenShake(NPC.Center, 7f, 18);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                            ModContent.ProjectileType<TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.MediumPurple));
                }
                // Black Breath: wide cone waves fired where the pull left the player.
                if ((wellTick == 2 || wellTick == 12 || wellTick == 22) && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 mouth = Mouth();
                    float baseAng = (player.Center - mouth).ToRotation();
                    for (int i = -4; i <= 4; i++)
                    {
                        // The three-wave "shotgun" is 27 skulls total. Slow this specific barrage by 40%
                        // without changing the speed of the Vessel's other skull patterns.
                        Vector2 vel = (baseAng + MathHelper.ToRadians(i * 12f)).ToRotationVector2() * Main.rand.NextFloat(4.2f, 5.4f);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), mouth, vel,
                            ModContent.ProjectileType<PurpleSkull>(), BreathDamage, 1f, Main.myPlayer, 0f);
                    }
                    SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.6f, Pitch = -0.4f }, NPC.Center);
                }
                if (wellTick >= WellCommittedTicks)
                {
                    MouthOpen = false;
                    if (AttackTimer >= WellTelegraphTicks + WellCommittedTicks + WellRecoveryTicks)
                    {
                        EndAttack(360);
                    }
                }
            }
        }

        // A6 — The Watching Wall: a ring of sentinel eyes firing inward.
        void RunWatchingWall(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            globalNPC.AttackCommitted = true;
            NPC.velocity *= 0.9f;
            if (AttackTimer <= WallTelegraphTicks)
            {
                if (Main.rand.NextBool(2))
                {
                    SpawnInhaleMote();
                }
                return;
            }
            if (AttackTimer == WallTelegraphTicks + 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int count = 10;
                for (int i = 0; i < count; i++)
                {
                    float ang = MathHelper.TwoPi * i / count;
                    // Pull the sentinel ring inward by exactly four tiles (64 pixels).
                    Vector2 anchor = player.Center + ang.ToRotationVector2() * 536f;
                    SpawnWatcher(anchor, anchor, mode: 1, fireOffset: i * 8);
                }
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.7f, Pitch = -0.2f }, NPC.Center);
            }
            if (AttackTimer >= WallTelegraphTicks + 120)
            {
                EndAttack(360);
            }
        }

        // A7 — Soul Nova: orbiting skulls lock, then an expanding annulus + radial burst.
        void RunSoulNova(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            globalNPC.AttackCommitted = true;
            MouthOpen = true;
            NPC.velocity *= 0.9f;
            if (AttackTimer <= NovaTelegraphTicks)
            {
                // orbit read
                float radius = MathHelper.Lerp(140f, 60f, AttackTimer / (float)NovaTelegraphTicks);
                for (int i = 0; i < 2; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 pos = NPC.Center + angle.ToRotationVector2() * radius;
                    int dustIndex = Dust.NewDust(pos, 4, 4, DustID.PurpleTorch, 0f, 0f, 100, default, 1.2f);
                    Main.dust[dustIndex].noGravity = true;
                    Main.dust[dustIndex].velocity = angle.ToRotationVector2() * 1.5f;
                }
                MouthChargeTelegraph();
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.7f, Pitch = 0f }, NPC.Center);
                }
                return;
            }
            if (AttackTimer == NovaTelegraphTicks + 1)
            {
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.8f, Pitch = 0.2f }, NPC.Center);
                UsefulFunctions.ScreenShake(NPC.Center, 5f, 14);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                        ModContent.ProjectileType<VesselSoulNova>(), NovaDamage, 4f, Main.myPlayer, NovaMaxRadius);
                    for (int i = 0; i < 12; i++)
                    {
                        Vector2 vel = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * 7f;
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
                            ModContent.ProjectileType<PurpleSkull>(), NovaDamage, 1f, Main.myPlayer, 0f);
                    }
                }
            }
            if (AttackTimer >= NovaTelegraphTicks + 40)
            {
                EndAttack(300);
            }
        }

        // A8 — Void Plunge: rise → crashing dive → radial skull shockwave.
        void RunVoidPlunge(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            globalNPC.AttackCommitted = true;
            switch (AttackPhase)
            {
                case 0: // rise + aim
                    MouthOpen = true;
                    Vector2 above = player.Center + new Vector2(0f, -420f);
                    Steer(above, 12f, 0.09f);
                    MouthChargeTelegraph();
                    if (AttackTimer == 1)
                    {
                        UsefulFunctions.ScreenShake(NPC.Center, 4f, 10);
                        SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.7f, Pitch = -0.4f }, NPC.Center);
                    }
                    if (AttackTimer >= PlungeRiseTicks)
                    {
                        LockedAim = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitY);
                        NPC.velocity = LockedAim * 20f;
                        AttackPhase = 1;
                        AttackTimer = 0;
                        NPC.netUpdate = true;
                    }
                    break;
                case 1: // dive
                    MouthOpen = true;
                    NPC.velocity = LockedAim * 20f;
                    if (AttackTimer >= 34 || NPC.Center.Y > player.Center.Y + 40f)
                    {
                        UsefulFunctions.ScreenShake(NPC.Center, 9f, 22);
                        SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.8f, Pitch = -0.3f }, NPC.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                                ModContent.ProjectileType<VesselSoulRuptureVFX>(),
                                0, 0f, Main.myPlayer, 145f, 1f, 20f);
                            for (int i = 0; i < 14; i++)
                            {
                                Vector2 vel = (MathHelper.TwoPi * i / 14f).ToRotationVector2() * Main.rand.NextFloat(6f, 8.5f);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
                                    ModContent.ProjectileType<PurpleSkull>(), PlungeDamage, 1f, Main.myPlayer, 0f);
                            }
                        }
                        AttackPhase = 2;
                        AttackTimer = 0;
                        NPC.netUpdate = true;
                    }
                    break;
                default: // recovery
                    MouthOpen = false;
                    NPC.velocity *= 0.88f;
                    if (AttackTimer >= 45)
                    {
                        EndAttack(330);
                    }
                    break;
            }
        }

        #endregion

        #region Phase-2 swallow set-piece

        void StartPhaseTransition()
        {
            Phase2 = true; // set on ENTER so burst damage can't double-trigger
            State = VesselState.PhaseTransition;
            CurrentAttack = VesselAttack.None;
            AttackTimer = 0;
            MouthOpen = true;
            // Arm the stage latches. A client never reaches here (this is server/singleplayer only) — it adopts
            // State through ReceiveExtraAI and relies on these defaulting to false on the freshly spawned NPC,
            // which holds because the swallow runs exactly once per fight.
            _swallowOpened = false;
            _swallowCaptured = false;
            _swallowArrived = false;
            _swallowRevealed = false;
            _swallowReformed = false;
            KillOwnedWells();
            NPC.netUpdate = true;
        }

        // The cinematic swallow. Timeline (AttackTimer):
        //   1-120    weak, resistible pull: the mouth opens and appears punishable
        //   121-240  pull ramps sharply until the player is swallowed
        //   240      capture: the well's pull ends and the player is taken over
        //   240-300  the player is dragged, visibly, the rest of the way into the mouth on an accelerating curve
        //   300      swallowed: hidden, flash + implosion, and the fade to black begins
        //   480      full black → void ON, boss warps to arena center, players released
        //   480-513  unfade, revealing the void
        //   525      done → phase 2 Idle
        void RunSwallow(Player player)
        {
            AttackTimer++; // this state isn't dispatched through RunAttack, so advance its own timeline
            NPC.velocity *= 0.85f;
            MouthOpen = true;

            Player local = Main.dedServ ? null : Main.LocalPlayer;
            tsorcRevampPlayer localMp = local != null ? local.GetModPlayer<tsorcRevampPlayer>() : null;

            if (!_swallowOpened)
            {
                _swallowOpened = true;
                UsefulFunctions.ScreenShake(NPC.Center, 8f, 24);
                SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 1f, Pitch = -0.8f }, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), Mouth(), Vector2.Zero,
                        ModContent.ProjectileType<VesselGravityWell>(), 0, 0f, Main.myPlayer,
                        NPC.whoAmI, SwallowCapture, 1000f); // swallow well owns the 2s gentle + 2s ramp curve
            }

            // Pull: inhale vortex while the well drags the player into the mouth.
            if (AttackTimer < SwallowCapture)
            {
                SpawnInhaleMote();
                if (AttackTimer >= 120)
                {
                    SpawnInhaleMote();
                    SpawnInhaleMote();
                }
            }

            // Capture: the well has just ended, so take the player over. Remember where they are (it doubles as the spot
            // they are dropped back at later) and where the glide into the mouth starts. On an overshoot the player is
            // already part-way down the well's pull, so both spots are a little closer to the mouth than intended.
            if (AttackTimer >= SwallowCapture && !_swallowCaptured)
            {
                _swallowCaptured = true;

                // Only a player standing inside the arena at this moment is swallowed; anyone outside keeps playing
                // in the normal world (no freeze, no black fade).
                _swallowLocalHeld = local != null && local.active && !local.dead && !IsOutsideArena(local);

                if (_swallowLocalHeld)
                {
                    _swallowReturnPos = local.Center;
                    _swallowSuckStart = local.Center;
                }
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item74 with { Volume = 1f, Pitch = -0.5f }, NPC.Center);
                }
            }

            // The glide: for one second the player is dragged the rest of the way to the mouth, visibly, instead of
            // popping in. ImpaleFreezeTimer pins Player.Center to ImpaleWorldPosition every tick, so moving that point
            // along the path moves the player. SuckEase starts gently and ends fast, so it reads as a gulp (see its comment); the
            // mouth is re-read each tick because the boss is still drifting.
            if (AttackTimer >= SwallowCapture && AttackTimer < SwallowSuckDone)
            {
                if (_swallowLocalHeld && local.active && !local.dead)
                {
                    float suckProgress = (AttackTimer - SwallowCapture) / (float)(SwallowSuckDone - SwallowCapture);
                    float eased = SuckEase(suckProgress);

                    localMp.ImpaleFreezeTimer = 4;
                    localMp.ImpaleWorldPosition = Vector2.Lerp(_swallowSuckStart, Mouth(), eased);
                    local.velocity = Vector2.Zero;

                    // A streak of soul dust peeling off the player and streaming into the mouth.
                    if (!Main.dedServ)
                    {
                        Vector2 toMouth = (Mouth() - local.Center).SafeNormalize(Vector2.UnitY);
                        int dustType = Main.rand.NextBool() ? DustID.PurpleTorch : DustID.Shadowflame;
                        int suckDust = Dust.NewDust(local.position, local.width, local.height, dustType, 0f, 0f, 100, default, 1.3f);
                        Main.dust[suckDust].noGravity = true;
                        Main.dust[suckDust].velocity = toMouth * Main.rand.NextFloat(5f, 9f);
                    }
                }

                SpawnInhaleMote();
                SpawnInhaleMote();
            }

            // Swallowed: the glide is over. The flash and implosion now land WITH the disappearance (they used to fire at
            // capture, which is why the player seemed to vanish before reaching the mouth).
            if (AttackTimer >= SwallowSuckDone && !_swallowArrived)
            {
                _swallowArrived = true;

                if (!Main.dedServ)
                {
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), Mouth(), Vector2.Zero,
                        ModContent.ProjectileType<TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.MediumPurple));
                    UsefulFunctions.ScreenShake(NPC.Center, 6f, 16);
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), Mouth(), Vector2.Zero,
                        ModContent.ProjectileType<VesselSoulRuptureVFX>(),
                        0, 0f, Main.myPlayer, 185f, 0f, 22f);
                }
            }

            // Held inside: pin the local player to the mouth — hidden + can't move — and fade to black over 3s.
            if (AttackTimer >= SwallowSuckDone && AttackTimer < SwallowFadeDone)
            {
                if (_swallowLocalHeld && local.active && !local.dead)
                {
                    localMp.SwallowHidden = true;
                    localMp.ImpaleFreezeTimer = 4;
                    localMp.ImpaleWorldPosition = Mouth();
                    local.velocity = Vector2.Zero;
                }
                SpawnInhaleMote();
                SpawnInhaleMote();

                if (_swallowLocalHeld)
                {
                    VesselOfSoulsFadeSystem.FadeAlpha = (AttackTimer - SwallowSuckDone) / (float)(SwallowFadeDone - SwallowSuckDone);
                }
            }

            // Full black → reveal: drop the player back on solid ground, warp + reform the boss, void ON.
            if (AttackTimer >= SwallowFadeDone && !_swallowRevealed)
            {
                _swallowRevealed = true;

                if (_swallowLocalHeld)
                {
                    VesselOfSoulsFadeSystem.FadeAlpha = 1f;
                }

                if (_swallowLocalHeld && local.active && !local.dead)
                {
                    localMp.SwallowHidden = false;

                    // Let go of the pin FIRST. The held stage sets ImpaleFreezeTimer to 4 every tick with the position at
                    // the boss's mouth; left running, it kept dragging the player back to the mouth for 4 more ticks
                    // after this teleport, and released them there — which can be inside terrain, since the boss flies
                    // through tiles. That was the "spat out inside solid blocks and stuck" bug.
                    localMp.ImpaleFreezeTimer = 0;

                    if (_swallowReturnPos != Vector2.Zero)
                    {
                        // The remembered spot was valid when captured, but check it anyway (the world can change).
                        local.Center = FindOpenCenter(_swallowReturnPos, local.Size, float.NegativeInfinity);
                        local.velocity = Vector2.Zero;

                        // The teleport must not read as a fall from wherever the glide left them.
                        local.fallStart = (int)(local.position.Y / 16f);
                        local.fallStart2 = local.fallStart;
                    }
                }
                if (player.active && !player.dead)
                {
                    // Reform above the player, but only in open air, and not more than 260px above them: the old fixed
                    // -360px offset could land inside the ceiling or somewhere far too high.
                    Vector2 preferredCenter = player.Center + new Vector2(0f, -260f);
                    NPC.Center = FindOpenCenter(preferredCenter, NPC.Size, player.Center.Y - 260f);
                }
                NPC.velocity = Vector2.Zero;
                _hideBody = true; // vanished — reforms from dust below
                NPC.netUpdate = true;
            }

            // Quick unfade into the void.
            if (_swallowLocalHeld && AttackTimer > SwallowFadeDone && AttackTimer < SwallowUnfadeDone)
                VesselOfSoulsFadeSystem.FadeAlpha = 1f - (AttackTimer - SwallowFadeDone) / (float)(SwallowUnfadeDone - SwallowFadeDone);

            // Reform from a purple/black dust cloud. Normally the tick after the reveal; on a big overshoot both
            // fire together, which reads as one hard cut rather than a missing reform.
            if (_swallowRevealed && !_swallowReformed && AttackTimer > SwallowFadeDone)
            {
                _swallowReformed = true;
                _hideBody = false;
                UsefulFunctions.ScreenShake(NPC.Center, 7f, 20);
                SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.9f, Pitch = -0.2f }, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                        ModContent.ProjectileType<VesselSoulRuptureVFX>(),
                        0, 0f, Main.myPlayer, 220f, 0f, 28f);
                if (!Main.dedServ)
                    for (int i = 0; i < 18; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Circular(9f, 9f);
                        int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, Main.rand.NextBool() ? DustID.Shadowflame : DustID.PurpleTorch, vel.X, vel.Y, 80, default, 1.9f);
                        Main.dust[dustIndex].noGravity = true;
                    }
            }

            if (AttackTimer >= SwallowEnd)
            {
                State = VesselState.Idle;
                MouthOpen = false;
                _hideBody = false;
                VesselOfSoulsFadeSystem.FadeAlpha = 0f;
                AttackTimer = 0;
                AttackCooldown = 90;
                NPC.netUpdate = true;
            }
        }

        #endregion

        #region Death spectacle

        public override bool CheckDead()
        {
            if (_deathSpectacleDone)
            {
                return true;
            }
            if (State != VesselState.Dying)
            {
                NPC.life = 1;
                NPC.dontTakeDamage = true;
                State = VesselState.Dying;
                CurrentAttack = VesselAttack.None;
                AttackTimer = 0;
                MouthOpen = true;
                KillOwnedWells();
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.netUpdate = true;
                }
                SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 1f, Pitch = -0.6f }, NPC.Center);
            }
            NPC.life = 1;
            return false;
        }

        // Survive-only: the normal world is restored as this starts, then the Vessel spins and sprays a
        // rotating spiral of skulls until the final blast. No contact damage (set in AI).
        void RunDying(Player player)
        {
            AttackTimer++; // this state isn't dispatched through RunAttack, so advance its own timeline
            NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Zero, 0.06f);
            NPC.rotation += 0.1f;
            MouthOpen = true;

            // Rotating skull fountain from the mouth.
            if (AttackTimer % 5 == 0 && AttackTimer < DeathDurationTicks - 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 mouth = Mouth();
                for (int i = 0; i < 2; i++)
                {
                    float ang = NPC.rotation + MathHelper.PiOver2 + MathHelper.ToRadians(i * 180f);
                    Vector2 vel = ang.ToRotationVector2() * 6f;
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), mouth, vel,
                        ModContent.ProjectileType<PurpleSkull>(), SkullDamage, 1f, Main.myPlayer, 0f);
                }
            }
            // Radial dust engulfing the body, growing.
            if (!Main.dedServ && AttackTimer % 2 == 0)
            {
                float grow = AttackTimer / (float)DeathDurationTicks;
                for (int i = 0; i < 1; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(4f + grow * 6f, 4f + grow * 6f);
                    int dustIndex = Dust.NewDust(NPC.Center - new Vector2(20f), 40, 40, Main.rand.NextBool() ? DustID.PurpleTorch : DustID.Shadowflame, vel.X, vel.Y, 80, default, 1.2f + grow);
                    Main.dust[dustIndex].noGravity = true;
                }
            }
            Lighting.AddLight(NPC.Center, 0.8f, 0.2f, 1f);

            // Final blast → real death.
            if (AttackTimer >= DeathDurationTicks)
            {
                if (!Main.dedServ)
                {
                    UsefulFunctions.ScreenShake(NPC.Center, 16f, 40);
                    SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1f, Pitch = -0.4f }, NPC.Center);
                    SoundEngine.PlaySound(SoundID.NPCDeath1 with { Volume = 1f }, NPC.Center);
                    // ~10× the dust and radial reach of a normal burst.
                    for (int i = 0; i < 50; i++)
                    {
                        Vector2 dir = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2();
                        Vector2 pos = NPC.Center + dir * Main.rand.NextFloat(0f, 340f); // spread across a big radius
                        Vector2 vel = dir * Main.rand.NextFloat(3f, 22f);               // flung far outward
                        int dustIndex = Dust.NewDust(pos, 4, 4, Main.rand.NextBool() ? DustID.Shadowflame : DustID.PurpleTorch, vel.X, vel.Y, 60, default, Main.rand.NextFloat(1.6f, 2.6f));
                        Main.dust[dustIndex].noGravity = true;
                    }
                }
                // The real kill is server-authoritative (RunDying runs in AI() on every client).
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                        ModContent.ProjectileType<VesselSoulRuptureVFX>(),
                        0, 0f, Main.myPlayer, 430f, 1f, 38f);
                    // Leave life at 1: StrikeNPC returns early when life <= 0, and vanilla UpdateNPC silently
                    // deactivates a life-0 NPC next tick (no CheckDead/OnKill/loot, scripted event reads it as a despawn).
                    // StrikeInstantKill takes the remaining 1 HP itself and reaches CheckDead, which now returns true.
                    _deathSpectacleDone = true;
                    NPC.dontTakeDamage = false;
                    NPC.StrikeInstantKill();
                }
            }
        }

        #endregion

        #region Helpers

        Vector2 Mouth() => NPC.Center + new Vector2(0f, NPC.height * 0.32f).RotatedBy(NPC.rotation);

        void SpawnInhaleMote()
        {
            if (Main.dedServ || !Main.rand.NextBool(3))
            {
                return;
            }
            Vector2 mouth = Mouth();
            Vector2 from = mouth + Main.rand.NextVector2Circular(140f, 140f);
            int dustIndex = Dust.NewDust(from, 4, 4, Main.rand.NextBool(3) ? DustID.Shadowflame : DustID.PurpleTorch, 0f, 0f, 120, default, 1.1f);
            Main.dust[dustIndex].noGravity = true;
            Main.dust[dustIndex].velocity = (mouth - from) * 0.06f;
        }

        ///<summary>The prominent pre-shot telegraph: purple dust sucking INWARD to the mouth (every
        ///projectile attack shows this for its windup) plus dark/black dust churning in the mouth hole.</summary>
        void MouthChargeTelegraph()
        {
            if (Main.dedServ)
            {
                return;
            }
            Vector2 mouth = Mouth();
            // Purple inward suck — dense and fast enough to read clearly.
            for (int i = 0; i < 1; i++)
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 from = mouth + ang.ToRotationVector2() * Main.rand.NextFloat(60f, 160f);
                int dustIndex = Dust.NewDust(from, 4, 4, DustID.PurpleTorch, 0f, 0f, 100, default, Main.rand.NextFloat(1.3f, 1.9f));
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].velocity = (mouth - from) * 0.10f;
                Main.dust[dustIndex].fadeIn = 0.3f;
            }
            // Black dust roiling in the hole.
            for (int i = 0; i < 1; i++)
            {
                Vector2 pos = mouth + Main.rand.NextVector2Circular(26f, 26f);
                int dustIndex = Dust.NewDust(pos, 4, 4, Main.rand.NextBool() ? DustID.Shadowflame : DustID.Smoke, 0f, 0f, 220, default, 1.4f);
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].velocity *= 0.15f;
            }
            Lighting.AddLight(mouth, 0.5f, 0.1f, 0.65f);
        }

        // Slow, large red/black motes drifting across the arena view during phase 2.
        void SpawnVoidFog(Player player)
        {
            // Fog follows the void: anyone who has it (even after leaving the arena) gets it, nobody else does.
            if (!player.active || !player.HasBuff(ModContent.BuffType<VesselVoid>()) || !Main.rand.NextBool(8))
            {
                return;
            }
            Vector2 pos = player.Center + Main.rand.NextVector2Circular(760f, 540f);
            int dustIndex = Dust.NewDust(pos, 8, 8, Main.rand.NextBool(3) ? DustID.Shadowflame : DustID.RedTorch, 0f, 0f, 190, default, Main.rand.NextFloat(1.7f, 2.8f));
            Main.dust[dustIndex].noGravity = true;
            Main.dust[dustIndex].velocity = new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(-0.45f, 0.2f));
            Main.dust[dustIndex].fadeIn = 0.5f;
        }

        void SpawnWatcher(Vector2 pos, Vector2 anchor, int mode, int fireOffset)
        {
            int idx = NPC.NewNPC(NPC.GetSource_FromThis(), (int)pos.X, (int)pos.Y,
                ModContent.NPCType<VesselWatcher>(), 0, anchor.X, anchor.Y, mode, fireOffset);
            if (Main.netMode == NetmodeID.Server && idx < Main.maxNPCs)
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, idx);
        }

        void KillOwnedWells()
        {
            int type = ModContent.ProjectileType<VesselGravityWell>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.type == type && (int)proj.ai[0] == NPC.whoAmI)
                {
                    proj.Kill();
                }
            }
        }

        // Refresh / clear the per-player void render (VesselVoid buff → EnterTheAbyss).
        void TickVoid(bool shouldBeOn)
        {
            if (shouldBeOn)
            {
                VoidWasOn = true;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (VoidRefresh > 0)
                    {
                        VoidRefresh--;
                    }
                    if (VoidRefresh <= 0)
                    {
                        VoidRefresh = 40;

                        // Only players standing inside the arena get the void. Once held, VesselVoid keeps ITSELF alive for
                        // its owner until the void ends (or they die), so leaving the arena doesn't drop it.
                        for (int i = 0; i < Main.maxPlayers; i++)
                        {
                            Player voidCandidate = Main.player[i];

                            if (voidCandidate.active && !voidCandidate.dead && !IsOutsideArena(voidCandidate))
                            {
                                voidCandidate.AddBuff(ModContent.BuffType<VesselVoid>(), 80);
                            }
                        }
                    }
                }
            }
            else if (VoidWasOn)
            {
                VoidWasOn = false;
                for (int i = 0; i < Main.maxPlayers; i++)
                    if (Main.player[i].active)
                    {
                        Main.player[i].ClearBuff(ModContent.BuffType<VesselVoid>());
                    }
            }
        }

        #endregion

        #region Presentation

        void UpdateRotation(Player player)
        {
            if (State == VesselState.Dying)
            {
                return;  // Dying spins freely
            }
            float target;
            bool diving = (CurrentAttack == VesselAttack.VesselLunge && AttackPhase == 1)
                       || (CurrentAttack == VesselAttack.VoidPlunge && AttackPhase == 1);
            // Mouth is at the BOTTOM of the sprite (local +Y), so "looking at" the player = pointing the
            // sprite's bottom at them. It looks at the player almost always; while repositioning (Retreat/
            // Drift) it doesn't need to, so it just sways.
            bool repositioning = State == VesselState.Idle && (Mood == MoveMood.Retreat || Mood == MoveMood.Drift);
            if (diving)
                target = NPC.velocity.ToRotation() - MathHelper.PiOver2; // mouth leads the charge
            else if (!repositioning && player.active && !player.dead)
                target = (player.Center - NPC.Center).ToRotation() - MathHelper.PiOver2;
            else
                target = (float)Math.Sin(BobPhase * 0.5f) * 0.12f;
            NPC.rotation = NPC.rotation.AngleLerp(target, 0.06f);
        }

        void UpdateAura()
        {
            float intensity = 0.25f + 0.5f * (1f - NPC.life / (float)NPC.lifeMax);
            if (State == VesselState.Attack && AttackTimer < 20)
            {
                intensity += 0.5f;
            }
            if (State == VesselState.PhaseTransition || State == VesselState.Dying)
            {
                intensity += 0.6f;
            }
            Lighting.AddLight(NPC.Center, intensity * 0.55f, intensity * 0.15f, intensity * 0.7f);
        }

        void ClampToWorld()
        {
            float pad = 32f * 16f;
            NPC.position.X = MathHelper.Clamp(NPC.position.X, pad, Main.maxTilesX * 16f - pad - NPC.width);
            NPC.position.Y = MathHelper.Clamp(NPC.position.Y, pad, Main.maxTilesY * 16f - pad - NPC.height);
        }

        public override void FindFrame(int frameHeightUnused)
        {
            if (Main.dedServ)
            {
                return;
            }
            int frameHeight = TextureAssets.Npc[NPC.type].Height() / Main.npcFrameCount[NPC.type];
            FrameTimer++;
            if (FrameTimer >= 8)
            {
                FrameTimer = 0;
                FrameIndex = (FrameIndex + 1) % 3;
            }
            int baseFrame = MouthOpen ? 3 : 0;
            NPC.frame.Width = TextureAssets.Npc[NPC.type].Width();
            NPC.frame.Height = frameHeight;
            NPC.frame.Y = (baseFrame + FrameIndex) * frameHeight;
        }

        void DrawBossVFX()
        {
            bool lunge = State == VesselState.Attack
                && CurrentAttack == VesselAttack.VesselLunge && AttackPhase == 1;
            bool plunge = State == VesselState.Attack
                && CurrentAttack == VesselAttack.VoidPlunge && AttackPhase == 1;
            if (lunge || plunge)
            {
                VesselVFX.DrawRammingWake(
                    NPC.Center, NPC.velocity, NPC.Size, plunge);
            }

            if (!MouthOpen)
                return;

            float progress = 0.45f;
            float radius = 165f;
            bool committed = false;

            if (State == VesselState.PhaseTransition)
            {
                progress = MathHelper.Clamp(AttackTimer / (float)SwallowCapture, 0f, 1f);
                committed = AttackTimer >= 120;
                radius = AttackTimer < SwallowCapture
                    ? MathHelper.Lerp(210f, 275f, progress)
                    : MathHelper.Lerp(190f, 110f, MathHelper.Clamp(
                        (AttackTimer - SwallowCapture) / 70f, 0f, 1f));
            }
            else if (State == VesselState.Dying)
            {
                progress = MathHelper.Clamp(AttackTimer / (float)DeathDurationTicks, 0f, 1f);
                committed = AttackTimer >= DeathDurationTicks - 40;
                if (committed)
                {
                    float collapse = MathHelper.Clamp(
                        (AttackTimer - (DeathDurationTicks - 40)) / 40f, 0f, 1f);
                    radius = MathHelper.Lerp(300f, 72f, collapse);
                    progress = 1f - collapse * 0.65f;
                }
                else
                {
                    radius = MathHelper.Lerp(175f, 300f, progress);
                }
            }
            else if (State == VesselState.Attack)
            {
                switch (CurrentAttack)
                {
                    case VesselAttack.VesselLunge:
                        progress = AttackPhase == 0
                            ? MathHelper.Clamp(AttackTimer / (float)LungeWindupTicks, 0f, 1f)
                            : 1f;
                        committed = AttackPhase == 1 || AttackTimer >= LungeWindupTicks - 25;
                        radius = committed ? 205f : 170f;
                        break;
                    case VesselAttack.SoulSpew:
                        progress = MathHelper.Clamp(AttackTimer / (float)SpewTelegraphTicks, 0f, 1f);
                        committed = AttackTimer > SpewTelegraphTicks;
                        break;
                    case VesselAttack.GravemawTug:
                        progress = MathHelper.Clamp(AttackTimer / (float)TugTelegraphTicks, 0f, 1f);
                        committed = AttackTimer >= TugTelegraphTicks + 40;
                        radius = 185f;
                        break;
                    case VesselAttack.GravityWell:
                        progress = MathHelper.Clamp(AttackTimer / (float)WellTelegraphTicks, 0f, 1f);
                        committed = AttackTimer > WellTelegraphTicks;
                        radius = 205f;
                        break;
                    case VesselAttack.SoulNova:
                        progress = MathHelper.Clamp(AttackTimer / (float)NovaTelegraphTicks, 0f, 1f);
                        committed = AttackTimer > NovaTelegraphTicks;
                        radius = 220f;
                        break;
                    case VesselAttack.VoidPlunge:
                        progress = AttackPhase == 0
                            ? MathHelper.Clamp(AttackTimer / (float)PlungeRiseTicks, 0f, 1f)
                            : 1f;
                        committed = AttackPhase == 1;
                        radius = 205f;
                        break;
                    default:
                        progress = MathHelper.Clamp(AttackTimer / 40f, 0f, 1f);
                        break;
                }
            }

            VesselVFX.DrawMaw(
                Mouth(), radius, progress, committed, innerDeadzone: 26f);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().DebugMode && !Main.dedServ)
            {
                string label = State == VesselState.Idle ? $"Idle / {Mood} (cd {AttackCooldown})"
                    : State == VesselState.Attack ? $"{CurrentAttack} p{AttackPhase} (t {AttackTimer})"
                    : $"{State} (t {AttackTimer})";
                Utils.DrawBorderString(spriteBatch, label, NPC.Top - screenPos - new Vector2(0f, 28f), Color.Violet, 1f, 0.5f, 0.5f);
            }

            DrawBossVFX();

            if (_hideBody)
            {
                return false;  // reforming from dust
            }

            bool fast = NPC.velocity.Length() > 9f
                || (CurrentAttack == VesselAttack.VesselLunge && AttackPhase == 1)
                || (CurrentAttack == VesselAttack.VoidPlunge && AttackPhase == 1);
            if (fast)
            {
                Texture2D tex = TextureAssets.Npc[NPC.type].Value;
                Vector2 origin = new Vector2(NPC.frame.Width / 2f, NPC.frame.Height / 2f);
                for (int k = NPC.oldPos.Length - 1; k > 0; k -= 2)
                {
                    Vector2 drawPos = NPC.oldPos[k] + new Vector2(NPC.width / 2f, NPC.height / 2f) - screenPos;
                    Color color = new Color(160, 60, 200, 0) * ((NPC.oldPos.Length - k) / (float)NPC.oldPos.Length) * 0.5f;
                    spriteBatch.Draw(tex, drawPos, NPC.frame, color, NPC.rotation, origin, NPC.scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (Main.dedServ || NPC.life > 0)
            {
                return;
            }
            for (int i = 0; i < 20; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(6f, 6f);
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, Main.rand.NextBool() ? DustID.Shadowflame : DustID.PurpleTorch, vel.X, vel.Y, 80, default, 1.6f);
                Main.dust[dustIndex].noGravity = true;
            }
        }

        public override void OnKill()
        {
            // EoC-slot boss: set the vanilla boss1 downed flag so mod progression gated on it unlocks.
            if (!NPC.downedBoss1)
            {
                NPC.downedBoss1 = true;
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.WorldData);
                }
            }
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPCDefinition def = new(NPC.type);
                if (!tsorcRevampWorld.NewSlain.ContainsKey(def))
                {
                    tsorcRevampWorld.NewSlain.Add(def, 1);
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.WorldData);
                    }
                }
            }
            // Safety: clear the void from every player so no one is stranded in the render.
            for (int i = 0; i < Main.maxPlayers; i++)
                if (Main.player[i].active)
                {
                    Main.player[i].ClearBuff(ModContent.BuffType<VesselVoid>());
                }
            VesselOfSoulsFadeSystem.FadeAlpha = 0f;
            // TODO(polish): gores, BossBag / unique weapon loot, souls economy tuning, boss music.
        }

        #endregion
    }
}
