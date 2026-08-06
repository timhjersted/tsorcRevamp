using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Buffs;
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
    // 4 more attacks. Death is a survive-only spectacle. See Documentation/VesselOfSouls_Design.md.
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
        const int SwallowFadeDone = 420;    // 3s (180t) of fading to black after capture
        const int SwallowUnfadeDone = 453;  // quick unfade into the void
        const int SwallowEnd = 465;         // → phase 2
        const int WallTelegraphTicks = 40;
        const int NovaTelegraphTicks = 35;
        const int PlungeRiseTicks = 40;
        const int DeathDurationTicks = 420;  // ~7s survive-only spectacle

        // ── Projectile damage (author HALF — hostile projectiles deal 2× on hit) ──
        int SkullDamage => ScaleDamage(12);
        int ConeDamage => ScaleDamage(14);
        int BreathDamage => ScaleDamage(15);
        int NovaDamage => ScaleDamage(16);
        int PlungeDamage => ScaleDamage(14);
        int ScaleDamage(int b) => SHM ? (int)(b * 1.3f * tsorcRevampWorld.SHMScale) : b;

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

        // Death spectacle
        bool _deathSpectacleDone;
        bool _hideBody;             // reforming from dust: draw dust only
        Vector2 _swallowReturnPos;  // where to drop the local player after the swallow (client-local; not synced)

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
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.VesselOfSoulsBag>()));
            IItemDropRule notExpert = new LeadingConditionRule(new Conditions.NotExpert());
            notExpert.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Items.Materials.SoulOfTheVessel>(), 1, 2, 2));
            npcLoot.Add(notExpert);
        }

        // ── Networking ──
        public override void SendExtraAI(BinaryWriter w)
        {
            w.Write((byte)State); w.Write((byte)Mood); w.Write((byte)CurrentAttack);
            w.Write(AttackTimer); w.Write(AttackCooldown); w.Write(AttackPhase); w.Write(ChargesLeft);
            w.Write(LockedAim.X); w.Write(LockedAim.Y);
            w.Write(OrbitAngle); w.Write((sbyte)OrbitDir); w.Write(OrbitRadius);
            w.Write(DriftAnchor.X); w.Write(DriftAnchor.Y);
            w.Write(MouthOpen); w.Write(Phase2); w.Write(SHM); w.Write(_hideBody);
        }
        public override void ReceiveExtraAI(BinaryReader r)
        {
            State = (VesselState)r.ReadByte(); Mood = (MoveMood)r.ReadByte(); CurrentAttack = (VesselAttack)r.ReadByte();
            AttackTimer = r.ReadInt32(); AttackCooldown = r.ReadInt32(); AttackPhase = r.ReadInt32(); ChargesLeft = r.ReadInt32();
            LockedAim.X = r.ReadSingle(); LockedAim.Y = r.ReadSingle();
            OrbitAngle = r.ReadSingle(); OrbitDir = r.ReadSByte(); OrbitRadius = r.ReadSingle();
            DriftAnchor.X = r.ReadSingle(); DriftAnchor.Y = r.ReadSingle();
            MouthOpen = r.ReadBoolean(); Phase2 = r.ReadBoolean(); SHM = r.ReadBoolean(); _hideBody = r.ReadBoolean();
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
                    int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Shadowflame, vel.X, vel.Y, 120, default, 1.4f);
                    Main.dust[d].noGravity = true;
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
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            tsorcRevampGlobalNPC g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            Player player = Main.player[NPC.target];

            g.AttackTelegraphing = false;
            g.AttackCommitted = false;
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
            bool voidShouldBeOn = Phase2 && !preReveal && State != VesselState.Dying;
            TickVoid(voidShouldBeOn);
            if (voidShouldBeOn && !Main.dedServ) SpawnVoidFog(player);

            if (g.StaggerTimer > 0 && State != VesselState.PhaseTransition && State != VesselState.Dying)
            {
                NPC.velocity *= 0.9f;
                if (Main.rand.NextBool(3))
                {
                    int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Shadowflame, 0f, 1f, 120, default, 1f);
                    Main.dust[d].velocity *= 0.3f;
                }
                UpdateAura();
                return;
            }

            // One-time phase transition at 50%.
            if (!Phase2 && State == VesselState.Idle && NPC.life <= NPC.lifeMax / 2)
                StartPhaseTransition();

            switch (State)
            {
                case VesselState.Idle: RunIdle(g, player); break;
                case VesselState.Attack: RunAttack(g, player); break;
                case VesselState.PhaseTransition: RunSwallow(player); break;
                case VesselState.Dying: RunDying(player); break;
            }

            UpdateRotation(player);
            UpdateAura();
            ClampToWorld();
        }

        void InitializeStats()
        {
            if (statsInitialized) return;
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

        void RunIdle(tsorcRevampGlobalNPC g, Player player)
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
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.PurpleTorch, 0f, -0.6f, 160, default, 0.8f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 0.3f;
            }

            if (AttackCooldown > 0) AttackCooldown--;
            if (Main.netMode != NetmodeID.MultiplayerClient && AttackCooldown <= 0 && NPC.Distance(player.Center) < 1000f)
                PickAttack(player);
        }

        void UpdateMood(Player player)
        {
            float distTiles = NPC.Distance(player.Center) / 16f;
            if (distTiles < 10f && Mood != MoveMood.Retreat) { SetMood(MoveMood.Retreat, 40); return; }
            if (MoodTimer > 0) { MoodTimer--; return; }
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            Span<(MoveMood mood, float weight)> pool = stackalloc (MoveMood, float)[]
            {
                (MoveMood.Orbit,   1.2f),
                (MoveMood.Drift,   1.0f),
                (MoveMood.Menace,  0.8f),
                (MoveMood.Retreat, distTiles < 18f ? 0.6f : 0.15f),
            };
            float total = 0f;
            for (int i = 0; i < pool.Length; i++) { if (pool[i].mood == Mood) pool[i].weight *= 0.4f; total += pool[i].weight; }
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
            if (mood == MoveMood.Orbit && Main.rand.NextBool(2)) OrbitDir = -OrbitDir;
            if (mood == MoveMood.Orbit) OrbitRadius = Main.rand.NextFloat(340f, 500f);
            if (mood == MoveMood.Drift) DriftAnchor = Main.rand.NextVector2Circular(360f, 300f) - new Vector2(0f, 120f);
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
            float dt = NPC.Distance(player.Center) / 16f;
            Span<(VesselAttack a, float w)> pool = Phase2
                ? stackalloc (VesselAttack, float)[]
                {
                    (VesselAttack.VesselLunge, (dt > 20f && dt < 55f) ? 1.0f : 0.3f),
                    (VesselAttack.SoulSpew,    1.0f),
                    (VesselAttack.GravityWell, dt < 40f ? 0.5f : 1.0f),
                    (VesselAttack.WatchingWall,0.8f),
                    (VesselAttack.SoulNova,    0.9f),
                    (VesselAttack.VoidPlunge,  0.9f),
                }
                : stackalloc (VesselAttack, float)[]
                {
                    (VesselAttack.VesselLunge,  (dt > 20f && dt < 55f) ? 1.1f : 0.4f),
                    (VesselAttack.SoulSpew,     1.0f),
                    (VesselAttack.GravemawTug,  dt < 45f ? 1.0f : 0.5f),
                    (VesselAttack.SpawnWatchers,0.7f),
                };
            float total = 0f;
            for (int i = 0; i < pool.Length; i++) { if (pool[i].a == LastAttack) pool[i].w *= 0.5f; total += pool[i].w; }
            float roll = Main.rand.NextFloat(total);
            for (int i = 0; i < pool.Length; i++)
            {
                roll -= pool[i].w;
                if (roll <= 0f) { StartAttack(pool[i].a); return; }
            }
            StartAttack(pool[0].a);
        }

        void StartAttack(VesselAttack a)
        {
            State = VesselState.Attack;
            CurrentAttack = a;
            AttackTimer = 0;
            AttackPhase = 0;
            if (a == VesselAttack.VesselLunge) ChargesLeft = Main.rand.Next(1, Phase2 ? 4 : 3);
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

        void RunAttack(tsorcRevampGlobalNPC g, Player player)
        {
            if (player.dead || !player.active) { EndAttack(60); return; }
            AttackTimer++;
            switch (CurrentAttack)
            {
                case VesselAttack.VesselLunge: RunVesselLunge(g, player); break;
                case VesselAttack.SoulSpew: RunSoulSpew(g, player); break;
                case VesselAttack.GravemawTug: RunGravemawTug(g, player); break;
                case VesselAttack.SpawnWatchers: RunSpawnWatchers(g, player); break;
                case VesselAttack.GravityWell: RunGravityWell(g, player); break;
                case VesselAttack.WatchingWall: RunWatchingWall(g, player); break;
                case VesselAttack.SoulNova: RunSoulNova(g, player); break;
                case VesselAttack.VoidPlunge: RunVoidPlunge(g, player); break;
                default: EndAttack(120); break;
            }
        }

        // A1 — Vessel Lunge: recoil windup → committed dash → (chain) → recovery.
        void RunVesselLunge(tsorcRevampGlobalNPC g, Player player)
        {
            g.AttackCommitted = true;
            switch (AttackPhase)
            {
                case 0: // windup
                    MouthOpen = true;
                    Vector2 away = (NPC.Center - player.Center).SafeNormalize(Vector2.UnitX);
                    float currentDist = Vector2.Distance(NPC.Center, player.Center);
                    // Dynamic retreat speed: back up stronger if close to maintain minimum charging distance (~380px)
                    float retreatSpeed = MathHelper.Lerp(12f, 4f, MathHelper.Clamp(currentDist / 380f, 0f, 1f));
                    NPC.velocity = Vector2.Lerp(NPC.velocity, away * retreatSpeed, 0.15f);
                    if (AttackTimer == 1) SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.5f, Pitch = -0.2f }, NPC.Center);
                    SpawnInhaleMote();
                    // Pink "dash incoming" flash at the mouth, ~25 ticks before the dash launches.
                    if (AttackTimer == Math.Max(1, LungeWindupTicks - 25) && Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), Mouth(), Vector2.Zero,
                            ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.HotPink));
                    if (!Main.dedServ && AttackTimer >= LungeWindupTicks - 25 && Main.rand.NextBool(3))
                    {
                        Vector2 pinkMouth = Mouth();
                        int pd = Dust.NewDust(pinkMouth - new Vector2(6f), 12, 12, DustID.PinkTorch, 0f, 0f, 100, default, 1.4f);
                        Main.dust[pd].noGravity = true;
                        Main.dust[pd].velocity *= 0.3f;
                    }
                    if (AttackTimer >= LungeWindupTicks)
                    {
                        LockedAim = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
                        NPC.velocity = LockedAim * 17f;
                        SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.8f, Pitch = 0.1f }, NPC.Center);
                        AttackPhase = 1; AttackTimer = 0; NPC.netUpdate = true;
                    }
                    break;
                case 1: // dash
                    MouthOpen = true;
                    NPC.velocity = LockedAim * 17f;
                    if (AttackTimer >= LungeDashTicks)
                    {
                        ChargesLeft--;
                        if (ChargesLeft > 0) { AttackPhase = 0; AttackTimer = 0; NPC.netUpdate = true; }
                        else { AttackPhase = 2; AttackTimer = 0; NPC.netUpdate = true; }
                    }
                    break;
                default: // recovery (no armor)
                    MouthOpen = false;
                    NPC.velocity *= 0.9f;
                    if (AttackTimer >= LungeRecoveryTicks) EndAttack(Phase2 ? 200 : 260);
                    break;
            }
        }

        // A2 — Soul Spew: inhale telegraph → sweeping fan of homing skulls.
        void RunSoulSpew(tsorcRevampGlobalNPC g, Player player)
        {
            g.AttackCommitted = true;
            MouthOpen = true;
            NPC.velocity *= 0.92f;
            if (AttackTimer == 1) SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.6f, Pitch = -0.3f }, NPC.Center);
            if (AttackTimer <= SpewTelegraphTicks) { MouthChargeTelegraph(); return; }

            int k = AttackTimer - SpewTelegraphTicks; // 1..
            if (k % 5 == 1 && k <= 36 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int idx = (k - 1) / 5; // 0..6
                Vector2 mouth = Mouth();
                float baseAng = (player.Center - mouth).ToRotation();
                float spread = MathHelper.ToRadians((idx - 3) * 9f);
                Vector2 vel = (baseAng + spread).ToRotationVector2() * 8f;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), mouth, vel,
                    ModContent.ProjectileType<Projectiles.Enemy.VesselOfSouls.PurpleSkull>(), SkullDamage, 1f, Main.myPlayer, 0.02f);
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.35f, Pitch = 0.2f }, NPC.Center);
            }
            if (AttackTimer >= SpewTelegraphTicks + 45) EndAttack(210);
        }

        // A3 — Gravemaw Tug: gentle pull → point-blank Black Nip cone.
        void RunGravemawTug(tsorcRevampGlobalNPC g, Player player)
        {
            g.AttackCommitted = true;
            MouthOpen = true;
            NPC.velocity *= 0.9f;
            if (AttackTimer == 1) SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.6f, Pitch = -0.5f }, NPC.Center);
            if (AttackTimer <= TugTelegraphTicks) { MouthChargeTelegraph(); return; }
            if (AttackTimer == TugTelegraphTicks + 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), Mouth(), Vector2.Zero,
                    ModContent.ProjectileType<Projectiles.Enemy.VesselOfSouls.VesselGravityWell>(), 0, 0f, Main.myPlayer,
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
                        ModContent.ProjectileType<Projectiles.Enemy.VesselOfSouls.PurpleSkull>(), ConeDamage, 1f, Main.myPlayer, 0f);
                }
                SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.5f, Pitch = -0.2f }, NPC.Center);
            }
            if (AttackTimer >= TugTelegraphTicks + 80) EndAttack(240);
        }

        // A4 — Spawn the Watchers: conjure servant eyes.
        void RunSpawnWatchers(tsorcRevampGlobalNPC g, Player player)
        {
            g.AttackCommitted = true;
            MouthOpen = true;
            NPC.velocity *= 0.9f;
            if (AttackTimer <= WatchersTelegraphTicks) { MouthChargeTelegraph(); return; }
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
            if (AttackTimer >= WatchersTelegraphTicks + 30) EndAttack(300);
        }

        // A5 — Gravity Well → Black Breath (the staggerable channel).
        void RunGravityWell(tsorcRevampGlobalNPC g, Player player)
        {
            MouthOpen = true;
            NPC.velocity *= 0.85f;

            if (AttackTimer <= WellTelegraphTicks)
            {
                g.AttackTelegraphing = true; // interruptible — a poise break here cancels it (OnStagger)
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.9f, Pitch = -0.7f }, NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), Mouth(), Vector2.Zero,
                            ModContent.ProjectileType<Projectiles.Enemy.VesselOfSouls.VesselGravityWell>(), 0, 0f, Main.myPlayer,
                            NPC.whoAmI, WellTelegraphTicks, 760f); // full pull (radius ≥700 → strong)
                }
                MouthChargeTelegraph();
            }
            else
            {
                g.AttackCommitted = true;
                int c = AttackTimer - WellTelegraphTicks;
                if (c == 1)
                {
                    // Commit cue: "too late to interrupt" — kill the pull, flash, breathe.
                    KillOwnedWells();
                    SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.9f, Pitch = 0.1f }, NPC.Center);
                    UsefulFunctions.ScreenShake(NPC.Center, 7f, 18);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                            ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.MediumPurple));
                }
                // Black Breath: wide cone waves fired where the pull left the player.
                if ((c == 2 || c == 12 || c == 22) && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 mouth = Mouth();
                    float baseAng = (player.Center - mouth).ToRotation();
                    for (int i = -4; i <= 4; i++)
                    {
                        // The three-wave "shotgun" is 27 skulls total. Slow this specific barrage by 40%
                        // without changing the speed of the Vessel's other skull patterns.
                        Vector2 vel = (baseAng + MathHelper.ToRadians(i * 12f)).ToRotationVector2() * Main.rand.NextFloat(4.2f, 5.4f);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), mouth, vel,
                            ModContent.ProjectileType<Projectiles.Enemy.VesselOfSouls.PurpleSkull>(), BreathDamage, 1f, Main.myPlayer, 0f);
                    }
                    SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.6f, Pitch = -0.4f }, NPC.Center);
                }
                if (c >= WellCommittedTicks)
                {
                    MouthOpen = false;
                    if (AttackTimer >= WellTelegraphTicks + WellCommittedTicks + WellRecoveryTicks) EndAttack(360);
                }
            }
        }

        // A6 — The Watching Wall: a ring of sentinel eyes firing inward.
        void RunWatchingWall(tsorcRevampGlobalNPC g, Player player)
        {
            g.AttackCommitted = true;
            NPC.velocity *= 0.9f;
            if (AttackTimer <= WallTelegraphTicks) { if (Main.rand.NextBool(2)) SpawnInhaleMote(); return; }
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
            if (AttackTimer >= WallTelegraphTicks + 120) EndAttack(360);
        }

        // A7 — Soul Nova: orbiting skulls lock, then an expanding annulus + radial burst.
        void RunSoulNova(tsorcRevampGlobalNPC g, Player player)
        {
            g.AttackCommitted = true;
            MouthOpen = true;
            NPC.velocity *= 0.9f;
            if (AttackTimer <= NovaTelegraphTicks)
            {
                // orbit read
                float r = MathHelper.Lerp(140f, 60f, AttackTimer / (float)NovaTelegraphTicks);
                for (int i = 0; i < 2; i++)
                {
                    float a = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 pos = NPC.Center + a.ToRotationVector2() * r;
                    int d = Dust.NewDust(pos, 4, 4, DustID.PurpleTorch, 0f, 0f, 100, default, 1.2f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity = a.ToRotationVector2() * 1.5f;
                }
                MouthChargeTelegraph();
                if (AttackTimer == 1) SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.7f, Pitch = 0f }, NPC.Center);
                return;
            }
            if (AttackTimer == NovaTelegraphTicks + 1)
            {
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.8f, Pitch = 0.2f }, NPC.Center);
                UsefulFunctions.ScreenShake(NPC.Center, 5f, 14);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                        ModContent.ProjectileType<Projectiles.Enemy.VesselOfSouls.VesselSoulNova>(), NovaDamage, 4f, Main.myPlayer, 480f);
                    for (int i = 0; i < 12; i++)
                    {
                        Vector2 vel = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * 7f;
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
                            ModContent.ProjectileType<Projectiles.Enemy.VesselOfSouls.PurpleSkull>(), NovaDamage, 1f, Main.myPlayer, 0f);
                    }
                }
            }
            if (AttackTimer >= NovaTelegraphTicks + 40) EndAttack(300);
        }

        // A8 — Void Plunge: rise → crashing dive → radial skull shockwave.
        void RunVoidPlunge(tsorcRevampGlobalNPC g, Player player)
        {
            g.AttackCommitted = true;
            switch (AttackPhase)
            {
                case 0: // rise + aim
                    MouthOpen = true;
                    Vector2 above = player.Center + new Vector2(0f, -420f);
                    Steer(above, 12f, 0.09f);
                    MouthChargeTelegraph();
                    if (AttackTimer == 1) { UsefulFunctions.ScreenShake(NPC.Center, 4f, 10); SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.7f, Pitch = -0.4f }, NPC.Center); }
                    if (AttackTimer >= PlungeRiseTicks)
                    {
                        LockedAim = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitY);
                        NPC.velocity = LockedAim * 20f;
                        AttackPhase = 1; AttackTimer = 0; NPC.netUpdate = true;
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
                                ModContent.ProjectileType<Projectiles.Enemy.VesselOfSouls.VesselSoulRuptureVFX>(),
                                0, 0f, Main.myPlayer, 145f, 1f, 20f);
                            for (int i = 0; i < 14; i++)
                            {
                                Vector2 vel = (MathHelper.TwoPi * i / 14f).ToRotationVector2() * Main.rand.NextFloat(6f, 8.5f);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
                                    ModContent.ProjectileType<Projectiles.Enemy.VesselOfSouls.PurpleSkull>(), PlungeDamage, 1f, Main.myPlayer, 0f);
                            }
                        }
                        AttackPhase = 2; AttackTimer = 0; NPC.netUpdate = true;
                    }
                    break;
                default: // recovery
                    MouthOpen = false;
                    NPC.velocity *= 0.88f;
                    if (AttackTimer >= 45) EndAttack(330);
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
            KillOwnedWells();
            NPC.netUpdate = true;
        }

        // The cinematic swallow. Timeline (AttackTimer):
        //   1-120    weak, resistible pull: the mouth opens and appears punishable
        //   121-240  pull ramps sharply until the player is swallowed
        //   240      capture: freeze players, flash, and begin fading to black
        //   420      full black → void ON, boss warps to arena center, players released
        //   420-453  unfade, revealing the void
        //   465      done → phase 2 Idle
        void RunSwallow(Player player)
        {
            AttackTimer++; // this state isn't dispatched through RunAttack, so advance its own timeline
            NPC.velocity *= 0.85f;
            MouthOpen = true;

            Player local = Main.dedServ ? null : Main.LocalPlayer;
            tsorcRevampPlayer localMp = local != null ? local.GetModPlayer<tsorcRevampPlayer>() : null;

            if (AttackTimer == 1)
            {
                UsefulFunctions.ScreenShake(NPC.Center, 8f, 24);
                SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 1f, Pitch = -0.8f }, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), Mouth(), Vector2.Zero,
                        ModContent.ProjectileType<Projectiles.Enemy.VesselOfSouls.VesselGravityWell>(), 0, 0f, Main.myPlayer,
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

            // Capture: remember where to drop the player later; implode into the mouth.
            if (AttackTimer == SwallowCapture)
            {
                if (local != null && local.active && !local.dead) _swallowReturnPos = local.Center;
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item74 with { Volume = 1f, Pitch = -0.5f }, NPC.Center);
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), Mouth(), Vector2.Zero,
                        ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.MediumPurple));
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), Mouth(), Vector2.Zero,
                        ModContent.ProjectileType<Projectiles.Enemy.VesselOfSouls.VesselSoulRuptureVFX>(),
                        0, 0f, Main.myPlayer, 185f, 0f, 22f);
                }
            }

            // Held inside: pin the local player to the mouth — hidden + can't move — and fade to black over 3s.
            if (AttackTimer >= SwallowCapture && AttackTimer < SwallowFadeDone)
            {
                if (localMp != null && local.active && !local.dead)
                {
                    localMp.SwallowHidden = true;
                    localMp.ImpaleFreezeTimer = 4;
                    localMp.ImpaleWorldPosition = Mouth();
                    local.velocity = Vector2.Zero;
                }
                SpawnInhaleMote(); SpawnInhaleMote();
                VesselOfSoulsFadeSystem.FadeAlpha = (AttackTimer - SwallowCapture) / (float)(SwallowFadeDone - SwallowCapture);
            }

            // Full black → reveal: drop the player back on solid ground, warp + reform the boss, void ON.
            if (AttackTimer == SwallowFadeDone)
            {
                VesselOfSoulsFadeSystem.FadeAlpha = 1f;
                if (localMp != null && local.active && !local.dead)
                {
                    localMp.SwallowHidden = false;
                    if (_swallowReturnPos != Vector2.Zero) { local.Center = _swallowReturnPos; local.velocity = Vector2.Zero; }
                }
                if (player.active && !player.dead) NPC.Center = player.Center + new Vector2(0f, -360f);
                NPC.velocity = Vector2.Zero;
                _hideBody = true; // vanished — reforms from dust below
                NPC.netUpdate = true;
            }

            // Quick unfade into the void.
            if (AttackTimer > SwallowFadeDone && AttackTimer < SwallowUnfadeDone)
                VesselOfSoulsFadeSystem.FadeAlpha = 1f - (AttackTimer - SwallowFadeDone) / (float)(SwallowUnfadeDone - SwallowFadeDone);

            // Reform from a purple/black dust cloud.
            if (AttackTimer == SwallowFadeDone + 1)
            {
                _hideBody = false;
                UsefulFunctions.ScreenShake(NPC.Center, 7f, 20);
                SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.9f, Pitch = -0.2f }, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                        ModContent.ProjectileType<Projectiles.Enemy.VesselOfSouls.VesselSoulRuptureVFX>(),
                        0, 0f, Main.myPlayer, 220f, 0f, 28f);
                if (!Main.dedServ)
                    for (int i = 0; i < 18; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Circular(9f, 9f);
                        int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, Main.rand.NextBool() ? DustID.Shadowflame : DustID.PurpleTorch, vel.X, vel.Y, 80, default, 1.9f);
                        Main.dust[d].noGravity = true;
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
            if (_deathSpectacleDone) return true;
            if (State != VesselState.Dying)
            {
                NPC.life = 1;
                NPC.dontTakeDamage = true;
                State = VesselState.Dying;
                CurrentAttack = VesselAttack.None;
                AttackTimer = 0;
                MouthOpen = true;
                KillOwnedWells();
                if (Main.netMode != NetmodeID.MultiplayerClient) NPC.netUpdate = true;
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
                        ModContent.ProjectileType<Projectiles.Enemy.VesselOfSouls.PurpleSkull>(), SkullDamage, 1f, Main.myPlayer, 0f);
                }
            }
            // Radial dust engulfing the body, growing.
            if (!Main.dedServ && AttackTimer % 2 == 0)
            {
                float grow = AttackTimer / (float)DeathDurationTicks;
                for (int i = 0; i < 1; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(4f + grow * 6f, 4f + grow * 6f);
                    int d = Dust.NewDust(NPC.Center - new Vector2(20f), 40, 40, Main.rand.NextBool() ? DustID.PurpleTorch : DustID.Shadowflame, vel.X, vel.Y, 80, default, 1.2f + grow);
                    Main.dust[d].noGravity = true;
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
                        int d = Dust.NewDust(pos, 4, 4, Main.rand.NextBool() ? DustID.Shadowflame : DustID.PurpleTorch, vel.X, vel.Y, 60, default, Main.rand.NextFloat(1.6f, 2.6f));
                        Main.dust[d].noGravity = true;
                    }
                }
                // The real kill is server-authoritative (RunDying runs in AI() on every client).
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                        ModContent.ProjectileType<Projectiles.Enemy.VesselOfSouls.VesselSoulRuptureVFX>(),
                        0, 0f, Main.myPlayer, 430f, 1f, 38f);
                    _deathSpectacleDone = true;
                    NPC.dontTakeDamage = false;
                    NPC.life = 0;
                    NPC.StrikeInstantKill();
                }
            }
        }

        #endregion

        #region Helpers

        Vector2 Mouth() => NPC.Center + new Vector2(0f, NPC.height * 0.32f).RotatedBy(NPC.rotation);

        void SpawnInhaleMote()
        {
            if (Main.dedServ || !Main.rand.NextBool(3)) return;
            Vector2 mouth = Mouth();
            Vector2 from = mouth + Main.rand.NextVector2Circular(140f, 140f);
            int d = Dust.NewDust(from, 4, 4, Main.rand.NextBool(3) ? DustID.Shadowflame : DustID.PurpleTorch, 0f, 0f, 120, default, 1.1f);
            Main.dust[d].noGravity = true;
            Main.dust[d].velocity = (mouth - from) * 0.06f;
        }

        ///<summary>The prominent pre-shot telegraph: purple dust sucking INWARD to the mouth (every
        ///projectile attack shows this for its windup) plus dark/black dust churning in the mouth hole.</summary>
        void MouthChargeTelegraph()
        {
            if (Main.dedServ) return;
            Vector2 mouth = Mouth();
            // Purple inward suck — dense and fast enough to read clearly.
            for (int i = 0; i < 1; i++)
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 from = mouth + ang.ToRotationVector2() * Main.rand.NextFloat(60f, 160f);
                int d = Dust.NewDust(from, 4, 4, DustID.PurpleTorch, 0f, 0f, 100, default, Main.rand.NextFloat(1.3f, 1.9f));
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity = (mouth - from) * 0.10f;
                Main.dust[d].fadeIn = 0.3f;
            }
            // Black dust roiling in the hole.
            for (int i = 0; i < 1; i++)
            {
                Vector2 pos = mouth + Main.rand.NextVector2Circular(26f, 26f);
                int d = Dust.NewDust(pos, 4, 4, Main.rand.NextBool() ? DustID.Shadowflame : DustID.Smoke, 0f, 0f, 220, default, 1.4f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 0.15f;
            }
            Lighting.AddLight(mouth, 0.5f, 0.1f, 0.65f);
        }

        // Slow, large red/black motes drifting across the arena view during phase 2.
        void SpawnVoidFog(Player player)
        {
            if (!player.active || !Main.rand.NextBool(8)) return;
            Vector2 pos = player.Center + Main.rand.NextVector2Circular(760f, 540f);
            int d = Dust.NewDust(pos, 8, 8, Main.rand.NextBool(3) ? DustID.Shadowflame : DustID.RedTorch, 0f, 0f, 190, default, Main.rand.NextFloat(1.7f, 2.8f));
            Main.dust[d].noGravity = true;
            Main.dust[d].velocity = new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(-0.45f, 0.2f));
            Main.dust[d].fadeIn = 0.5f;
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
            int type = ModContent.ProjectileType<Projectiles.Enemy.VesselOfSouls.VesselGravityWell>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == type && (int)p.ai[0] == NPC.whoAmI) p.Kill();
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
                    if (VoidRefresh > 0) VoidRefresh--;
                    if (VoidRefresh <= 0)
                    {
                        VoidRefresh = 40;
                        for (int i = 0; i < Main.maxPlayers; i++)
                            if (Main.player[i].active && !Main.player[i].dead)
                                Main.player[i].AddBuff(ModContent.BuffType<VesselVoid>(), 80);
                    }
                }
            }
            else if (VoidWasOn)
            {
                VoidWasOn = false;
                for (int i = 0; i < Main.maxPlayers; i++)
                    if (Main.player[i].active) Main.player[i].ClearBuff(ModContent.BuffType<VesselVoid>());
            }
        }

        #endregion

        #region Presentation

        void UpdateRotation(Player player)
        {
            if (State == VesselState.Dying) return; // Dying spins freely
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
            if (State == VesselState.Attack && AttackTimer < 20) intensity += 0.5f;
            if (State == VesselState.PhaseTransition || State == VesselState.Dying) intensity += 0.6f;
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
            if (Main.dedServ) return;
            int frameHeight = TextureAssets.Npc[NPC.type].Height() / Main.npcFrameCount[NPC.type];
            FrameTimer++;
            if (FrameTimer >= 8) { FrameTimer = 0; FrameIndex = (FrameIndex + 1) % 3; }
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
                Projectiles.Enemy.VesselOfSouls.VesselVFX.DrawRammingWake(
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

            Projectiles.Enemy.VesselOfSouls.VesselVFX.DrawMaw(
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

            if (_hideBody) return false; // reforming from dust

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
            if (Main.dedServ || NPC.life > 0) return;
            for (int i = 0; i < 20; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(6f, 6f);
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, Main.rand.NextBool() ? DustID.Shadowflame : DustID.PurpleTorch, vel.X, vel.Y, 80, default, 1.6f);
                Main.dust[d].noGravity = true;
            }
        }

        public override void OnKill()
        {
            // EoC-slot boss: set the vanilla boss1 downed flag so mod progression gated on it unlocks.
            if (!NPC.downedBoss1)
            {
                NPC.downedBoss1 = true;
                if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.WorldData);
            }
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPCDefinition def = new(NPC.type);
                if (!tsorcRevampWorld.NewSlain.ContainsKey(def))
                {
                    tsorcRevampWorld.NewSlain.Add(def, 1);
                    if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.WorldData);
                }
            }
            // Safety: clear the void from every player so no one is stranded in the render.
            for (int i = 0; i < Main.maxPlayers; i++)
                if (Main.player[i].active) Main.player[i].ClearBuff(ModContent.BuffType<VesselVoid>());
            VesselOfSoulsFadeSystem.FadeAlpha = 0f;
            // TODO(polish): gores, BossBag / unique weapon loot, souls economy tuning, boss music.
        }

        #endregion
    }
}
