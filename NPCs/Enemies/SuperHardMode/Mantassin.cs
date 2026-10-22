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
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.MantassinSorcery;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    // Sprite by Omnir, from Omnir's Nostalgia Pack: https://forums.terraria.org/index.php?threads/omnirs-nostalgia-pack.11875/
    //
    // Mantassin (was Quara Mantassin): a slow, methodical glintstone FINGER SORCERER stalking the SHM ice caves.
    // Design: tsorcDocs/QuaraMantassin_Revamp_Proposal.md (rev 3). Every attack is a spell whose tell is the spell itself
    // forming, themed on Elden Ring's finger sorceries from the user's reference art.
    //   Glintstone Nails (walk-cast) / Nail Volley / Encircling Nails: MantassinGlintNail — forms above it, accelerates,
    //     15->200px path trail, and (the first two) ricochets off tiles up to 4 times, homing in by the last bounce.
    //   Fleeting Microcosm: a 400px black star-filled disc that pulses once (MantassinMicrocosm).
    //   Cherishing Light: three orbiting light arcs that hurt, eat projectiles and block melee (MantassinCherishingArc).
    //   Glintstone Blink: its own telegraphed, interruptible reposition (not the shared teleport styles, which hide the
    //     body through the telegraph and so can't be interrupted).
    // Movement: SmartFighter4 at half its old speed; no pounce, no dodge hop, no evasion — the only jumps are terrain
    //   navigation.
    // Stalker cloak (InvisibilityStyle.Stalker): each 25% of max life lost (10s cooldown, so up to 3 times) it vanishes to
    //   ~2% visibility, takes 1 hidden damage per hit (poise still builds; a stagger reveals it), drops its kite band and stalks in at nearly 3x speed for 4s, relocates to the
    //   player's far side, glints only in its first and last 60t, then reappears and casts almost at once (the ambush).
    //   It never casts while hidden.
    // Poise (0.3 / 20 in PoiseProfiles): low. Every tell is interruptible (AttackTelegraphing); only the release ticks are
    //   hyper-armored. A poise break shatters whatever it is still forming.
    // MP: picks, gates, destinations and spawns are server-only. State, timer, cooldown and the locked points/directions
    //   are synced in SendExtraAI; projectiles run their own timelines from synced ai[].
    class Mantassin : ModNPC, IStaggerable, IDebugAttackLabel
    {
        enum AttackState : byte
        {
            None = 0,
            GlintstoneNails,   // 3 nails above its head, walk-cast, ricochet
            NailVolley,        // 7 nails in a staggered row, re-aimed one by one, ricochet
            EncirclingNails,   // 6 nails ringing a floor sigil under the player, converge together
            FleetingMicrocosm, // 400px black disc in front of it, one pulse
            CherishingLight,   // 3 orbiting arcs: hurt, block projectiles and melee
            Blink,             // telegraphed reposition to 10-15 tiles with line of sight
        }

        // Glintstone Nails: three nails launch at ticks 37 / 43 / 49, then a 30t recovery.
        const int NailsTellTicks = 36;
        const int NailsLaunchSpacing = 6;
        const int NailsCount = 3;
        const int NailsRecoveryTicks = 30;
        const int NailsBounces = 4;
        const float NailsAcceleration = 0.6f;

        // Nail Volley: one nail forms every 6t (7 nails, done by tick 43); first launch at 52, then every 10t.
        const int VolleyCount = 7;
        const int VolleyFormSpacing = 6;
        const int VolleyFirstLaunch = 52;
        const int VolleyLaunchSpacing = 10;
        const int VolleyRecoveryTicks = 40;
        const int VolleyBounces = 4; // watch item: 7 x 4 bounces may be too dense in tight caves
        const float VolleyAcceleration = 0.8f;

        // Encircling Nails: hang 45t, launch together, arrive ~29t later (0.3 * t^2 = 260px).
        const int EncircleHangTicks = 45;
        const int EncircleCount = 6;
        const float EncircleRadius = 260f;
        const float EncircleSpreadDegrees = 200f;
        const int EncircleRecoveryTicks = 35;
        const float EncircleAcceleration = 0.6f;
        const int EncircleTravelTicks = 29;

        // Fleeting Microcosm: the projectile owns its 45t form / 8t pulse; the caster stands locked through it.
        const float MicrocosmForward = 140f;
        const int MicrocosmPulseTicks = 8;
        const int MicrocosmRecoveryTicks = 45;
        const int MicrocosmFacingHoldTicks = 20;
        const int MicrocosmCooldownTicks = 420;

        // Cherishing Light: the arcs own their timeline (40 trace + 180 active + 15 fade).
        const int CherishingRecoveryTicks = 30;
        const int CherishingCooldownTicks = 600;

        // Glintstone Blink.
        const int BlinkTellTicks = 45;
        const int BlinkRecoveryTicks = 20;
        const int BlinkCooldownTicks = 360;
        const int BlinkCloseTriggerTicks = 90;   // player within BlinkCloseRange this long -> blink away
        const float BlinkCloseRange = 5 * 16f;
        const int BlinkLostSightTicks = 240;     // no line of sight this long -> blink back in (its nav recovery)
        const int BlinkGhostTicks = 30;

        // Walking speed (px/t, accel px/t^2). Half the old 2.5 normally; while cloaked it stalks in at nearly 3x that.
        const float WalkSpeed = 1.25f;
        const float WalkAcceleration = 0.04f;
        const float StalkSpeed = 3.5f;
        const float StalkAcceleration = 0.12f;
        const float KiteRangeMaxTiles = 15f;

        // Pacing between casts: 120-180t + rand(30). It walks in between.
        const int CastCooldownMin = 120;
        const int CastCooldownMax = 180;

        // Projectile damage (hostile projectiles deal 2x in normal mode, 4x in Expert; ProgressionScaling ramps on top).
        const int NailDamage = 38;
        const int MicrocosmDamage = 45;
        const int CherishingDamage = 32;

        AttackState State = AttackState.None;
        AttackState LastAttack = AttackState.None;
        int AttackTimer;
        int AttackCooldown = 90;
        Vector2 lockedDirection;    // Microcosm facing / aim
        Vector2 sigilPoint;         // Encircling Nails convergence point
        Vector2 blinkDestination;   // Blink arrival centre

        // Server-only decision state (never needs syncing: only the server picks).
        int microcosmCooldown;
        int cherishingCooldown;
        int blinkCooldown;
        int playerCloseTimer;
        int lostSightTimer;
        int recentlyHitTimer;
        bool blinkAfterCherishing;
        readonly List<AttackState> nailBag = new List<AttackState>();
        readonly List<int> castProjectiles = new List<int>();

        // Client-local visuals.
        Vector2 blinkGhostPosition;
        int blinkGhostTimer;
        int footstepTimer;
        int blockFeedbackCooldown;
        bool wasCloaked;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 15;
        }

        public override void SetDefaults()
        {
            NPC.width = 18;
            NPC.height = 45;
            NPC.scale = 1.2f;
            AnimationType = NPCID.Skeleton;
            NPC.aiStyle = -1;
            NPC.damage = 0; // every attack is a spell; Fleeting Microcosm is its answer to being crowded
            NPC.defense = 36;
            NPC.lifeMax = 3000;
            NPC.value = 6000;
            NPC.npcSlots = 3;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.3f; //overridden by the PoiseProfiles entry

            tsorcRevampGlobalNPC g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            // SmartFighter4 navigation. A modest jump: enough for ledges the path needs, never acrobatics.
            g.NavSearchRadius = 50;
            g.MaxJumpPower = 8f;
            // Methodical caster: advances to 7-15 tiles and holds there, backpedalling slowly if crowded.
            g.KiteRangeMin = 7f;
            g.KiteRangeMax = KiteRangeMaxTiles;
            g.KiteLooseness = 0.4f;
            g.PatrolMode = PatrolMode.Pace;
            g.RemembersLastKnownPos = true;
            // Stalker cloak: alpha 250 = ~2% visible (user: "5 out of 255"). Hunter / detectCreature still shows it at 50.
            g.CanGoInvisible = true;
            g.InvisibilityStyle = InvisibilityStyle.Stalker;
            g.InvisibleAlpha = 250;
            g.StalkerLifeStep = 0.25f;
            g.StalkerCooldownTicks = 600;
            g.StalkerGlintTicks = 60;
            g.StalkerHiddenTicks = 240;
            g.StalkerGlintColor = EnemyVFX.MantassinCore;
        }

        // Spawning lives in Systems/EnemySpawns.cs (underground ice, SHM only).
        public override float SpawnChance(NPCSpawnInfo spawnInfo) => 0f;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)State);
            writer.Write(AttackTimer);
            writer.Write(AttackCooldown);
            writer.WriteVector2(lockedDirection);
            writer.WriteVector2(sigilPoint);
            writer.WriteVector2(blinkDestination);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            State = (AttackState)reader.ReadByte();
            AttackTimer = reader.ReadInt32();
            AttackCooldown = reader.ReadInt32();
            lockedDirection = reader.ReadVector2();
            sigilPoint = reader.ReadVector2();
            blinkDestination = reader.ReadVector2();
        }

        public string DebugAttackLabel => State switch
        {
            AttackState.GlintstoneNails => "Glintstone Nails",
            AttackState.NailVolley => "Nail Volley",
            AttackState.EncirclingNails => "Encircling Nails",
            AttackState.FleetingMicrocosm => "Fleeting Microcosm",
            AttackState.CherishingLight => "Cherishing Light",
            AttackState.Blink => "Glintstone Blink",
            _ => "Neutral"
        };

        ///<summary>Its casting hand. Measured from the sheet: the hand sits at about (60, 28) of the 65x56 frame (right edge,
        ///mid-height, art facing right), so on screen it is on the side the sprite isn't flipped toward. 1.2 scale.</summary>
        Vector2 Fingertip => NPC.Center + new Vector2(-NPC.spriteDirection * 30f, -10f);

        ///<summary>Poise break: whatever it is still forming shatters — hanging nails, an unopened Microcosm, the arcs.
        ///Already-launched nails keep flying.</summary>
        public void OnStagger(NPC npc)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < castProjectiles.Count; i++)
                {
                    Projectile cast = Main.projectile[castProjectiles[i]];
                    if (!cast.active)
                    {
                        continue;
                    }

                    bool stillForming = false;
                    if (cast.ModProjectile is MantassinGlintNail nail)
                    {
                        stillForming = nail.IsHanging;
                    }
                    else if (cast.ModProjectile is MantassinMicrocosm microcosm)
                    {
                        stillForming = microcosm.IsForming;
                    }
                    else if (cast.ModProjectile is MantassinCherishingArc)
                    {
                        stillForming = true;
                    }
                    else if (cast.ModProjectile is MantassinRune)
                    {
                        stillForming = true;
                    }

                    if (stillForming)
                    {
                        cast.Kill();
                    }
                }
            }

            if (State != AttackState.None && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.NPCHit5 with { Volume = 0.6f, Pitch = 0.1f }, NPC.Center);
                for (int i = 0; i < 18; i++)
                {
                    Dust shard = Dust.NewDustPerfect(Fingertip, DustID.GemDiamond, Main.rand.NextVector2Circular(3f, 3f), 60, default, 0.9f);
                    shard.noGravity = true;
                }
            }

            State = AttackState.None;
            AttackTimer = 0;
            castProjectiles.Clear();
            blinkAfterCherishing = false;
            AttackCooldown = Math.Max(AttackCooldown, 70);
        }

        public override void AI()
        {
            tsorcRevampGlobalNPC g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            Player player = Main.player[NPC.target];

            g.AttackTelegraphing = false;
            g.AttackCommitted = false;

            if (blinkGhostTimer > 0)
            {
                blinkGhostTimer--;
            }
            if (blockFeedbackCooldown > 0)
            {
                blockFeedbackCooldown--;
            }

            if (g.StaggerTimer > 0)
            {
                NPC.rotation = MathHelper.Lerp(NPC.rotation, -NPC.direction * 0.2f, 0.1f);
                if (!Main.dedServ && Main.rand.NextBool(4))
                {
                    Dust frost = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Frost, 0f, 1f, 80, default, 0.9f);
                    frost.velocity *= 0.3f;
                }
                return;
            }
            NPC.rotation *= 0.85f;

            TickDecisionTimers(player);
            DrawIdleIdentity(g);

            // Ambush: the moment the Stalker cloak drops (it has just flanked to the player's far side), the next cast is
            // nearly ready. Server-only, like every pick.
            bool cloaked = g.IsInvisible;
            if (wasCloaked && !cloaked && Main.netMode != NetmodeID.MultiplayerClient)
            {
                AttackCooldown = Math.Min(AttackCooldown, 15);
                NPC.netUpdate = true;
            }
            wasCloaked = cloaked;

            // Cloaked: no kite band (it closes in instead of holding 7-15 tiles) and it moves fast; the cloak's relocation
            // at the end puts it on the player's far side. Restored the tick it reappears.
            float walkSpeed = WalkSpeed;
            float walkAcceleration = WalkAcceleration;
            if (cloaked)
            {
                walkSpeed = StalkSpeed;
                walkAcceleration = StalkAcceleration;
                g.KiteRangeMax = 0f;
            }
            else
            {
                g.KiteRangeMax = KiteRangeMaxTiles;
            }

            if (State == AttackState.None)
            {
                tsorcRevampAIs.FighterAI(NPC, walkSpeed, walkAcceleration, canTeleport: false, lavaJumping: true, canDodgeroll: false, canPounce: false, canWalkBackwards: true);

                if (AttackCooldown > 0)
                {
                    AttackCooldown--;
                }

                // Never casts while cloaked: every spell's tell would give it away anyway, and the reappear is the ambush.
                bool canStartCast = AttackCooldown <= 0 && NPC.velocity.Y == 0f && player.active && !player.dead
                    && NPC.Distance(player.Center) < 40 * 16f && !g.IsInvisible;
                if (Main.netMode != NetmodeID.MultiplayerClient && canStartCast)
                {
                    PickAttack(player);
                }
            }
            else
            {
                RunAttack(g, player);
            }
        }

        // Server-side gate timers for the reactive picks, and the cooldowns. Clients never pick, so never need these.
        void TickDecisionTimers(Player player)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            if (microcosmCooldown > 0)
            {
                microcosmCooldown--;
            }
            if (cherishingCooldown > 0)
            {
                cherishingCooldown--;
            }
            if (blinkCooldown > 0)
            {
                blinkCooldown--;
            }
            if (recentlyHitTimer > 0)
            {
                recentlyHitTimer--;
            }
            // justHit is set on the server when any strike lands, so this is the server-reliable "being attacked" signal.
            if (NPC.justHit)
            {
                recentlyHitTimer = 120;
            }

            float distance = NPC.Distance(player.Center);
            if (State == AttackState.None && distance < BlinkCloseRange)
            {
                playerCloseTimer++;
            }
            else
            {
                playerCloseTimer = 0;
            }

            bool lineOfSight = Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1);
            if (!lineOfSight && distance < 1500f)
            {
                lostSightTimer++;
            }
            else
            {
                lostSightTimer = 0;
            }
        }

        // Idle identity: a cold glint at the fingertip, frost footprints while walking. All of it stops while cloaked — the
        // Stalker cloak's own first/last 60t glints are the only tells then.
        void DrawIdleIdentity(tsorcRevampGlobalNPC g)
        {
            if (Main.dedServ || g.IsInvisible)
            {
                return;
            }

            if (Main.GameUpdateCount % 6 == 0)
            {
                Dust glint = Dust.NewDustPerfect(Fingertip, DustID.TintableDustLighted, Vector2.Zero, 60, EnemyVFX.MantassinCore, 0.6f);
                glint.noGravity = true;
            }

            bool walking = NPC.velocity.Y == 0f && Math.Abs(NPC.velocity.X) > 0.3f;
            if (walking)
            {
                footstepTimer++;
                if (footstepTimer >= 30)
                {
                    footstepTimer = 0;
                    Dust footprint = Dust.NewDustDirect(NPC.BottomLeft + new Vector2(0f, -4f), NPC.width, 4, DustID.Frost, 0f, -0.3f, 80, default, 0.8f);
                    footprint.noGravity = true;
                    footprint.velocity *= 0.2f;
                }
            }

            Lighting.AddLight(Fingertip, EnemyVFX.MantassinMid.ToVector3() * 0.25f);
        }

        void PickAttack(Player player)
        {
            float distance = NPC.Distance(player.Center);
            float distanceTiles = distance / 16f;
            bool lineOfSight = Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1);
            AttackState chosen = AttackState.None;

            // Reactive overrides first, in priority order. Blink: crowded for 1.5s with Microcosm unavailable, right
            // after Cherishing Light (half the time), or out of sight for 4s (its nav recovery).
            bool crowded = playerCloseTimer >= BlinkCloseTriggerTicks && microcosmCooldown > 0;
            bool lostSight = lostSightTimer >= BlinkLostSightTicks;
            if (blinkCooldown <= 0 && (crowded || lostSight || blinkAfterCherishing))
            {
                chosen = AttackState.Blink;
            }
            else if (!lineOfSight)
            {
                return; // every other spell needs line of sight
            }
            else if (microcosmCooldown <= 0 && distanceTiles < 6f)
            {
                chosen = AttackState.FleetingMicrocosm;
            }
            else if (cherishingCooldown <= 0 && LastAttack != AttackState.CherishingLight
                && (recentlyHitTimer > 0 || distanceTiles < 10f))
            {
                chosen = AttackState.CherishingLight;
            }
            else if (microcosmCooldown <= 0 && distanceTiles < 12f && Main.rand.NextBool(4))
            {
                chosen = AttackState.FleetingMicrocosm;
            }
            else
            {
                chosen = DrawNailSpell(distanceTiles);
            }

            blinkAfterCherishing = false;
            State = chosen;
            AttackTimer = 0;
            castProjectiles.Clear();
            NPC.netUpdate = true;
        }

        // The nail spells come out of a shuffled bag (Glintstone Nails x2, Volley, Encircling), never the same one twice
        // running. Encircling Nails only fits at 6-20 tiles; out of that band it is skipped, not lost.
        AttackState DrawNailSpell(float distanceTiles)
        {
            if (nailBag.Count == 0)
            {
                nailBag.Add(AttackState.GlintstoneNails);
                nailBag.Add(AttackState.GlintstoneNails);
                nailBag.Add(AttackState.NailVolley);
                nailBag.Add(AttackState.EncirclingNails);
                for (int i = nailBag.Count - 1; i > 0; i--)
                {
                    int swap = Main.rand.Next(i + 1);
                    (nailBag[i], nailBag[swap]) = (nailBag[swap], nailBag[i]);
                }
            }

            bool encirclingFits = distanceTiles >= 6f && distanceTiles <= 20f;
            for (int i = 0; i < nailBag.Count; i++)
            {
                AttackState candidate = nailBag[i];
                if (candidate == LastAttack)
                {
                    continue;
                }
                if (candidate == AttackState.EncirclingNails && !encirclingFits)
                {
                    continue;
                }
                nailBag.RemoveAt(i);
                return candidate;
            }

            return AttackState.GlintstoneNails;
        }

        void EndAttack(int recoveryCooldown)
        {
            LastAttack = State;
            State = AttackState.None;
            AttackTimer = 0;
            castProjectiles.Clear();
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                AttackCooldown = recoveryCooldown + Main.rand.Next(30);
                NPC.netUpdate = true;
            }
        }

        int CastCooldown()
        {
            return Main.rand.Next(CastCooldownMin, CastCooldownMax + 1);
        }

        void RunAttack(tsorcRevampGlobalNPC g, Player player)
        {
            if (player.dead || !player.active)
            {
                EndAttack(60);
                return;
            }

            // Face the player, except while the Microcosm's facing is locked. AttackTimer is still 0 on the first call, so
            // it turns to the player once before the lock (taken on tick 1) holds that facing.
            bool facingLocked = State == AttackState.FleetingMicrocosm && AttackTimer >= 1
                && AttackTimer <= MantassinMicrocosm.FormTicks + MicrocosmPulseTicks + MicrocosmFacingHoldTicks;
            if (!facingLocked)
            {
                NPC.direction = player.Center.X > NPC.Center.X ? 1 : -1;
            }
            NPC.spriteDirection = NPC.direction;

            AttackTimer++;
            switch (State)
            {
                case AttackState.GlintstoneNails:
                    RunGlintstoneNails(g, player);
                    break;
                case AttackState.NailVolley:
                    RunNailVolley(g, player);
                    break;
                case AttackState.EncirclingNails:
                    RunEncirclingNails(g, player);
                    break;
                case AttackState.FleetingMicrocosm:
                    RunFleetingMicrocosm(g, player);
                    break;
                case AttackState.CherishingLight:
                    RunCherishingLight(g);
                    break;
                case AttackState.Blink:
                    RunBlink(g, player);
                    break;
            }
        }

        // Three nails form in a short row above its head and track the player, launching 6t apart. It keeps walking.
        void RunGlintstoneNails(tsorcRevampGlobalNPC g, Player player)
        {
            int lastLaunch = NailsTellTicks + 1 + (NailsCount - 1) * NailsLaunchSpacing;

            if (AttackTimer <= NailsTellTicks)
            {
                g.AttackTelegraphing = true;
                tsorcRevampAIs.FighterAI(NPC, WalkSpeed, WalkAcceleration, canTeleport: false, lavaJumping: true, canDodgeroll: false, canPounce: false, canWalkBackwards: true);
                CastMotes(2);
            }
            else if (AttackTimer <= lastLaunch)
            {
                g.AttackCommitted = true;
                NPC.velocity.X *= 0.8f;
            }
            else
            {
                NPC.velocity.X *= 0.8f;
            }

            if (AttackTimer == 1)
            {
                PlayCastChime(0.1f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < NailsCount; i++)
                    {
                        Vector2 position = NPC.Top + new Vector2((i - 1) * 22f, -36f);
                        int hang = NailsTellTicks + i * NailsLaunchSpacing;
                        SpawnNail(position, player.Center - position, hang, NailsBounces, NailsAcceleration, player.whoAmI);
                    }
                }
            }

            if (AttackTimer >= lastLaunch + NailsRecoveryTicks)
            {
                EndAttack(CastCooldown());
            }
        }

        // Seven nails form one every 6t in a staggered row rising up and back over it (the reference art's layout), then
        // each flares and fires re-aimed at the player 10t apart, front to back. It stands to cast.
        void RunNailVolley(tsorcRevampGlobalNPC g, Player player)
        {
            int lastLaunch = VolleyFirstLaunch + (VolleyCount - 1) * VolleyLaunchSpacing;
            NPC.velocity.X *= 0.8f;

            if (AttackTimer < VolleyFirstLaunch)
            {
                g.AttackTelegraphing = true;
                CastMotes(3);
            }
            else if (AttackTimer <= lastLaunch)
            {
                g.AttackCommitted = true;
            }

            bool formingTick = (AttackTimer - 1) % VolleyFormSpacing == 0;
            int nailIndex = (AttackTimer - 1) / VolleyFormSpacing;
            if (formingTick && nailIndex < VolleyCount)
            {
                if (AttackTimer == 1)
                {
                    PlayCastChime(-0.1f);
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // Front nail (index 0) is low and ahead; each next one steps 12px back and 6px up.
                    Vector2 position = NPC.Top + new Vector2(NPC.direction * (24f - nailIndex * 12f), -40f - nailIndex * 6f);
                    int launchTick = VolleyFirstLaunch + nailIndex * VolleyLaunchSpacing;
                    int hang = launchTick - AttackTimer;
                    SpawnNail(position, player.Center - position, hang, VolleyBounces, VolleyAcceleration, player.whoAmI);
                }
            }

            if (AttackTimer >= lastLaunch + VolleyRecoveryTicks)
            {
                EndAttack(CastCooldown());
            }
        }

        // A sigil locks on the floor under the player; six nails bloom in a 200-degree arc 260px around it (open side away
        // from the caster) and converge together after 45t. No bounces: a ricocheting ring would be unreadable.
        void RunEncirclingNails(tsorcRevampGlobalNPC g, Player player)
        {
            NPC.velocity.X *= 0.8f;

            if (AttackTimer <= EncircleHangTicks)
            {
                g.AttackTelegraphing = true;
                CastMotes(2);
            }
            else if (AttackTimer <= EncircleHangTicks + 5)
            {
                g.AttackCommitted = true;
            }

            if (AttackTimer == 1)
            {
                PlayCastChime(0.25f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 floor = FindFloorBelow(player.Center, 20);
                    if (floor == Vector2.Zero)
                    {
                        floor = player.Bottom;
                    }
                    // The nails converge on the player's chest height above the sigil.
                    sigilPoint = floor - new Vector2(0f, 24f);
                    NPC.netUpdate = true;

                    int runeIndex = Projectile.NewProjectile(NPC.GetSource_FromAI(), floor, Vector2.Zero, ModContent.ProjectileType<MantassinRune>(),
                        0, 0f, Main.myPlayer, EncircleHangTicks + EncircleTravelTicks, 96f);
                    castProjectiles.Add(runeIndex);

                    float towardCaster = (NPC.Center - sigilPoint).ToRotation();
                    float spread = MathHelper.ToRadians(EncircleSpreadDegrees);
                    for (int i = 0; i < EncircleCount; i++)
                    {
                        float angle = towardCaster - spread * 0.5f + spread * i / (EncircleCount - 1);
                        Vector2 position = sigilPoint + angle.ToRotationVector2() * EncircleRadius;
                        // A nail born inside rock would shatter on launch; skip that slot instead.
                        if (Collision.SolidCollision(position - new Vector2(6f), 12, 12))
                        {
                            continue;
                        }
                        SpawnNail(position, sigilPoint - position, EncircleHangTicks, 0, EncircleAcceleration, -1);
                    }
                }
            }

            if (AttackTimer >= EncircleHangTicks + EncircleRecoveryTicks)
            {
                EndAttack(CastCooldown());
            }
        }

        // The disc opens 140px in front of it over 45t, pulses once, collapses. Facing locks at tick 1 and holds through
        // the first 20t of the recovery — walking through it (contact 0) and hitting its back is the positional answer.
        void RunFleetingMicrocosm(tsorcRevampGlobalNPC g, Player player)
        {
            int pulseEnd = MantassinMicrocosm.FormTicks + MicrocosmPulseTicks;
            NPC.velocity.X *= 0.8f;

            if (AttackTimer <= MantassinMicrocosm.FormTicks)
            {
                g.AttackTelegraphing = true;
            }
            else if (AttackTimer <= pulseEnd)
            {
                g.AttackCommitted = true;
            }

            if (AttackTimer == 1)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    lockedDirection = new Vector2(NPC.direction, 0f);
                    microcosmCooldown = MicrocosmCooldownTicks;
                    Vector2 center = NPC.Center + lockedDirection * MicrocosmForward + new Vector2(0f, -10f);
                    int microcosmIndex = Projectile.NewProjectile(NPC.GetSource_FromAI(), center, Vector2.Zero,
                        ModContent.ProjectileType<MantassinMicrocosm>(), MicrocosmDamage, 0f, Main.myPlayer);
                    castProjectiles.Add(microcosmIndex);
                    NPC.netUpdate = true;
                }
            }

            // Light drawn out of the fingertip toward the forming disc.
            if (!Main.dedServ && AttackTimer <= MantassinMicrocosm.FormTicks && Main.rand.NextBool(2))
            {
                Vector2 discCenter = NPC.Center + new Vector2(NPC.direction * MicrocosmForward, -10f);
                Vector2 toDisc = discCenter - Fingertip;
                Dust stream = Dust.NewDustPerfect(Fingertip, DustID.TintableDustLighted, toDisc * 0.04f, 40, EnemyVFX.MantassinGold, 0.7f);
                stream.noGravity = true;
            }

            if (AttackTimer >= pulseEnd + MicrocosmRecoveryTicks)
            {
                EndAttack(CastCooldown());
            }
        }

        // Three arcs trace in around it and orbit for 3 seconds while it keeps slow-walking toward the player. No other
        // casts meanwhile. The whole state stays interruptible: hits through the gaps build poise and a break shatters them.
        void RunCherishingLight(tsorcRevampGlobalNPC g)
        {
            int arcsEnd = MantassinCherishingArc.TraceTicks + MantassinCherishingArc.ActiveTicks + MantassinCherishingArc.FadeTicks;

            if (AttackTimer <= arcsEnd)
            {
                g.AttackTelegraphing = true;
                tsorcRevampAIs.FighterAI(NPC, WalkSpeed, WalkAcceleration, canTeleport: false, lavaJumping: true, canDodgeroll: false, canPounce: false, canWalkBackwards: false);
            }
            else
            {
                NPC.velocity.X *= 0.8f;
            }

            if (AttackTimer == 1)
            {
                PlayCastChime(-0.3f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    cherishingCooldown = CherishingCooldownTicks;
                    float startAngle = (Main.player[NPC.target].Center - NPC.Center).ToRotation();
                    for (int i = 0; i < 3; i++)
                    {
                        float offset = startAngle + MathHelper.TwoPi * i / 3f;
                        int arcIndex = Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero,
                            ModContent.ProjectileType<MantassinCherishingArc>(), CherishingDamage, 0f, Main.myPlayer, NPC.whoAmI, offset);
                        castProjectiles.Add(arcIndex);
                    }
                }
            }

            if (AttackTimer >= arcsEnd + CherishingRecoveryTicks)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    blinkAfterCherishing = Main.rand.NextBool();
                }
                EndAttack(30);
            }
        }

        // Glintstone Blink: motes spiral into the body while a sigil blooms at the destination (so the player can see where
        // it will appear and chase). Interruptible for all 45t. At the end: a silver rift at both ends, a fading
        // afterimage left behind, and it arrives visible.
        void RunBlink(tsorcRevampGlobalNPC g, Player player)
        {
            NPC.velocity.X *= 0.8f;

            if (AttackTimer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2? destination = tsorcRevampAIs.GenerateTeleportPosition(NPC, 15, true, false, 10);
                if (!destination.HasValue)
                {
                    blinkCooldown = 60;
                    EndAttack(30);
                    return;
                }

                blinkCooldown = BlinkCooldownTicks;
                playerCloseTimer = 0;
                lostSightTimer = 0;
                blinkDestination = destination.Value;
                Vector2 floor = blinkDestination + new Vector2(0f, NPC.height * 0.5f);
                int runeIndex = Projectile.NewProjectile(NPC.GetSource_FromAI(), floor, Vector2.Zero, ModContent.ProjectileType<MantassinRune>(),
                    0, 0f, Main.myPlayer, BlinkTellTicks, 72f);
                castProjectiles.Add(runeIndex);
                NPC.netUpdate = true;
            }

            if (AttackTimer <= BlinkTellTicks)
            {
                g.AttackTelegraphing = true;
                if (AttackTimer == 1 && !Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item79 with { Volume = 0.5f, Pitch = 0.3f }, NPC.Center);
                }
                if (!Main.dedServ)
                {
                    // Motes spiralling into the body.
                    Vector2 outward = Main.rand.NextVector2Unit();
                    Vector2 tangent = new Vector2(-outward.Y, outward.X);
                    Dust mote = Dust.NewDustPerfect(NPC.Center + outward * 48f, DustID.TintableDustLighted,
                        -outward * 2.5f + tangent * 1.5f, 50, EnemyVFX.MantassinMid, 0.8f);
                    mote.noGravity = true;
                }
            }

            // The destination arrives by sync; a client that missed it (Zero) just waits for the server's position update.
            if (AttackTimer == BlinkTellTicks && blinkDestination != Vector2.Zero)
            {
                blinkGhostPosition = NPC.position;
                blinkGhostTimer = BlinkGhostTicks;
                EnemyShaderBurst.Spawn(NPC.GetSource_FromAI(), NPC.Center, EnemyVFXBurstKind.MantassinBlink);
                EnemyShaderBurst.Spawn(NPC.GetSource_FromAI(), blinkDestination, EnemyVFXBurstKind.MantassinBlink);
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.7f, Pitch = 0.2f }, blinkDestination);
                    for (int i = 0; i < 30; i++)
                    {
                        Vector2 velocity = Main.rand.NextVector2CircularEdge(4f, 4f);
                        Dust shard = Dust.NewDustPerfect(blinkDestination, DustID.TintableDustLighted, velocity, 40, EnemyVFX.MantassinCore, 0.9f);
                        shard.noGravity = true;
                    }
                }
                NPC.Center = blinkDestination;
                NPC.velocity = Vector2.Zero;
                blinkDestination = Vector2.Zero;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.netUpdate = true;
                }
            }

            if (AttackTimer >= BlinkTellTicks + BlinkRecoveryTicks)
            {
                EndAttack(CastCooldown() / 2);
            }
        }

        // Server only. Remembers the nail so a poise break can shatter it while it still hangs.
        void SpawnNail(Vector2 position, Vector2 aim, int hangTicks, int bounces, float acceleration, int targetPlayer)
        {
            int nailIndex = MantassinGlintNail.Spawn(NPC.GetSource_FromAI(), position, aim, NailDamage, hangTicks, bounces, acceleration, targetPlayer);
            if (nailIndex >= 0 && nailIndex < Main.maxProjectiles)
            {
                castProjectiles.Add(nailIndex);
            }
        }

        // Silver motes drawn in toward the fingertip: the shared "a spell is forming" read for the nail casts.
        void CastMotes(int oneIn)
        {
            if (Main.dedServ || !Main.rand.NextBool(oneIn))
            {
                return;
            }
            Vector2 offset = Main.rand.NextVector2CircularEdge(22f, 22f);
            Dust mote = Dust.NewDustPerfect(Fingertip + offset, DustID.TintableDustLighted, -offset * 0.08f, 50, EnemyVFX.MantassinMid, 0.75f);
            mote.noGravity = true;
        }

        void PlayCastChime(float pitch)
        {
            if (Main.dedServ)
            {
                return;
            }
            SoundEngine.PlaySound(SoundID.Item25 with { Volume = 0.55f, Pitch = pitch, PitchVariance = 0.05f }, NPC.Center);
        }

        // First solid tile top below a point, within maxTilesDown. Zero when there is none.
        static Vector2 FindFloorBelow(Vector2 worldPosition, int maxTilesDown)
        {
            int tileX = (int)(worldPosition.X / 16f);
            int startY = (int)(worldPosition.Y / 16f);
            for (int y = startY; y <= startY + maxTilesDown; y++)
            {
                if (WorldGen.InWorld(tileX, y, 16) && WorldGen.SolidTile(tileX, y))
                {
                    return new Vector2(tileX * 16f + 8f, y * 16f);
                }
            }
            return Vector2.Zero;
        }

        // Cherishing Light blocks a hit whose attacker stands inside one of its arcs' angles. Called from CanBeHitBy*,
        // which run on the attacker's machine; the arc angles are a pure function of their synced spawn, so every machine
        // agrees. A blocked hit never lands: no damage, no poise. Feedback (clang + arc flash) is throttled to 20t.
        bool IsBlockedByCherishingLight(Vector2 attackerPosition)
        {
            if (State != AttackState.CherishingLight)
            {
                return false;
            }

            float angle = (attackerPosition - NPC.Center).ToRotation();
            int arcType = ModContent.ProjectileType<MantassinCherishingArc>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.type != arcType || (int)projectile.ai[0] != NPC.whoAmI)
                {
                    continue;
                }

                MantassinCherishingArc arc = (MantassinCherishingArc)projectile.ModProjectile;
                if (!arc.CoversAngle(angle))
                {
                    continue;
                }

                if (blockFeedbackCooldown <= 0 && !Main.dedServ)
                {
                    blockFeedbackCooldown = 20;
                    arc.Flash();
                    SoundEngine.PlaySound(SoundID.NPCHit4 with { Volume = 0.6f, Pitch = 0.3f }, NPC.Center);
                }
                return true;
            }
            return false;
        }

        public override bool? CanBeHitByItem(Player player, Item item)
        {
            if (IsBlockedByCherishingLight(player.Center))
            {
                return false;
            }
            return null;
        }

        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            // Held melee projectiles (spears, shortswords, flails...) are judged by where their wielder stands. Free
            // projectiles that reach the body went through a gap — the arcs destroy the rest before they get here.
            if (projectile.ownerHitCheck && projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
            {
                if (IsBlockedByCherishingLight(Main.player[projectile.owner].Center))
                {
                    return false;
                }
            }
            return null;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Blink afterimage: the body it left behind, fading over 30t.
            if (blinkGhostTimer > 0)
            {
                Texture2D texture = TextureAssets.Npc[Type].Value;
                float fade = blinkGhostTimer / (float)BlinkGhostTicks;
                Vector2 ghostCenter = blinkGhostPosition + new Vector2(NPC.width * 0.5f, NPC.height - NPC.frame.Height * NPC.scale * 0.5f + 4f);
                SpriteEffects effects = SpriteEffects.None;
                if (NPC.spriteDirection == 1)
                {
                    effects = SpriteEffects.FlipHorizontally;
                }
                spriteBatch.Draw(texture, ghostCenter - screenPos, NPC.frame, EnemyVFX.MantassinMid * (0.5f * fade),
                    NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, effects, 0f);
            }
            return true;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (Main.dedServ || NPC.life > 0)
            {
                return;
            }

            // Death: the glintstone in it escapes in a cold burst before the body falls.
            SoundEngine.PlaySound(SoundID.NPCHit5 with { Volume = 0.8f, Pitch = -0.2f }, NPC.Center);
            for (int i = 0; i < 50; i++)
            {
                Dust burst = Dust.NewDustPerfect(NPC.Center, DustID.TintableDustLighted, Main.rand.NextVector2Circular(5f, 5f), 40, EnemyVFX.MantassinCore, 1.1f);
                burst.noGravity = true;
            }
            for (int i = 0; i < 20; i++)
            {
                Dust shard = Dust.NewDustPerfect(NPC.Center, DustID.GemDiamond, Main.rand.NextVector2Circular(4f, 4f), 60, default, 1f);
                shard.noGravity = true;
            }

            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("MantassinGore1").Type, 1.2f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("MantassinGore2").Type, 1.2f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("MantassinGore2").Type, 1.2f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("MantassinGore3").Type, 1.2f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("MantassinGore3").Type, 1.2f);
        }
    }
}
