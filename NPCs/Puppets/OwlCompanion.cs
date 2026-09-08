using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy;
using tsorcRevamp.Projectiles.Enemy.Weapons;

namespace tsorcRevamp.NPCs.Puppets
{
    /// <summary>
    /// Owl Father's companion owl. Spawned alongside him (see OwlFatherInvader.OnSpawn), wanders the
    /// battlefield on FloaterAI, alternates between dive-bomb sweeps and perch-and-snipe eye-beam
    /// volleys, and is killed outright by Owl Father himself the instant his health crosses 50% (see
    /// OwlFatherInvader's threshold check) — that kill is what triggers Owl Father's spectral form,
    /// not anything this class tracks on its own.
    /// </summary>
    public class OwlCompanion : ModNPC
    {
        // Art file is "Owl.png", not "OwlCompanion.png" — the class name and sprite filename never
        // matched, so autoload silently threw MissingResourceException at load. Point at the real file.
        public override string Texture => "tsorcRevamp/NPCs/Puppets/Owl";

        private enum OwlState
        {
            Wander,
            DiveBombWindup,
            DiveBombing,
            FlyingToPerch,
            PerchLanding,
            EyeBeamTelegraph,
            EyeBeamRecovery,
        }

        // ── Tuning (first pass — all NEEDS IN-GAME CALIBRATION) ─────────────────────
        private const float WanderTopSpeed = 4.5f;
        private const float WanderAccel = 0.09f;
        private const float WanderHoverMin = 140f;
        private const float WanderHoverMax = 320f;
        private const int DecisionIntervalTicks = 90;
        private const int DiveBombChance = 50;   // out of 100, rolled only when the cooldown is down
        private const int PerchChance = 40;      // out of 100, rolled the rest of the time
        private const int DiveBombCooldownTicks = 10 * 60;
        private const int DiveBombWindupTicks = 34;
        private const float DiveBombSpeed = 12f;
        private const int DiveBombMaxTicks = 50; // safety cap so a missed dive doesn't fly forever
        private const int DiveBombDamage = 20;
        private const int TreeSearchRadiusTiles = 45;
        private const int PerchLandingTicks = 12;
        private const int EyeBeamTelegraphTicks = 90; // per design: "90 tick growing glowing telegraph"
        private const int EyeBeamRecoveryTicks = 30;
        private const int EyeBeamDamage = 18;
        private const float EyeBeamSpeed = 9f;
        private const float EyeBeamSeparation = 6f; // twin shots, one per eye

        private FloaterAI.FloaterState _floaterState = new();
        private OwlState _state = OwlState.Wander;
        private int _stateTimer;
        private int _decisionTimer;
        private int _diveBombCooldown;
        private Vector2 _diveBombTargetVelocity;
        private Vector2 _perchPoint;
        private int _eyeBeamLookFrame = FramePerchForward;

        /// <summary>Owl Father's NPC index, set at spawn (ai[0]). Used only to notice if he despawns
        /// by some OTHER means (e.g. the whole encounter resetting) — the normal 50%-health kill is
        /// driven entirely from Owl Father's side, not read from here.</summary>
        private int OwnerNpcIndex => (int)NPC.ai[0];

        public override void SetStaticDefaults()
        {
            // First-pass frame ranges from the raw sprite sheet — a perched/blinking idle loop, a
            // wing-flap cycle for wandering flight, and a swept-back glide pose for the dive. Confirm
            // against the actual sheet in-game and adjust the ranges below if they're off.
            Main.npcFrameCount[Type] = 13;
        }

        // Confirmed against the actual 13-frame sheet (contact-sheet render):
        //   0,1,3,5,6,7 — forward-facing perch/blink idle loop (no strong left/right asymmetry)
        //   2           — head turned to screen-RIGHT (single visible eye, tufts left) — aim-right art
        //   4           — head turned to screen-LEFT (mirror of 2) — aim-left art
        //   8           — single steeper dive/bank pose, distinct from the flight loop below
        //   9,10,11,12  — 4-frame wingbeat flight loop
        private static readonly int[] IdleBlinkFrames = { 0, 1, 3, 5, 6, 7 };
        private static readonly int[] FlyFrames = { 9, 10, 11, 12 };
        private const int DiveFrame = 8;
        private const int FramePerchForward = 0;
        private const int FrameLookRight = 2;
        private const int FrameLookLeft = 4;

        public override void SetDefaults()
        {
            NPC.width = 32;
            NPC.height = 32;
            NPC.lifeMax = 1; // not a combat target — see class doc; killed outright by Owl Father
            NPC.dontTakeDamage = true;
            NPC.noGravity = true;
            NPC.noTileCollide = false;
            NPC.aiStyle = -1;
            NPC.damage = 0; // toggled on only during the DiveBombing window below
            NPC.knockBackResist = 0f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 0f;
            NPC.npcSlots = 0f; // doesn't count against the boss's own slot budget
            NPC.friendly = false;
            NPC.lavaImmune = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            _diveBombCooldown = DiveBombCooldownTicks / 2; // don't dive immediately on spawn
        }

        public override void AI()
        {
            // Owner despawned/died by some means other than the 50%-health trigger (which kills this
            // NPC directly and never reaches this check) — nothing left to escort, so just leave too.
            if (OwnerNpcIndex < 0 || OwnerNpcIndex >= Main.maxNPCs || !Main.npc[OwnerNpcIndex].active)
            {
                NPC.active = false;
                return;
            }

            Player target = FindClosestPlayer();

            if (_diveBombCooldown > 0)
            {
                _diveBombCooldown--;
            }

            switch (_state)
            {
                case OwlState.Wander:
                    TickWander(target);
                    break;
                case OwlState.DiveBombWindup:
                    TickDiveBombWindup(target);
                    break;
                case OwlState.DiveBombing:
                    TickDiveBombing();
                    break;
                case OwlState.FlyingToPerch:
                    TickFlyingToPerch(target);
                    break;
                case OwlState.PerchLanding:
                    TickPerchLanding();
                    break;
                case OwlState.EyeBeamTelegraph:
                    TickEyeBeamTelegraph(target);
                    break;
                case OwlState.EyeBeamRecovery:
                    TickEyeBeamRecovery();
                    break;
            }

            UpdateFrame();
        }

        private Player FindClosestPlayer()
        {
            Player closest = null;
            float bestDistSq = float.MaxValue;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead)
                {
                    continue;
                }
                float distSq = Vector2.DistanceSquared(player.Center, NPC.Center);
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    closest = player;
                }
            }
            return closest;
        }

        // ── Wander ────────────────────────────────────────────────────────────────
        private void TickWander(Player target)
        {
            FloaterAI.Run(NPC, target, _floaterState,
                topSpeed: WanderTopSpeed, accel: WanderAccel,
                hoverMin: WanderHoverMin, hoverMax: WanderHoverMax);

            if (target == null)
            {
                return;
            }

            _decisionTimer++;
            if (_decisionTimer < DecisionIntervalTicks)
            {
                return;
            }
            _decisionTimer = 0;

            int roll = Main.rand.Next(100);
            if (_diveBombCooldown <= 0 && roll < DiveBombChance)
            {
                _state = OwlState.DiveBombWindup;
                _stateTimer = DiveBombWindupTicks;
                NPC.netUpdate = true;
            }
            else if (roll < DiveBombChance + PerchChance)
            {
                _state = OwlState.FlyingToPerch;
                _perchPoint = TryFindTreePerch(out Vector2 treePoint) ? treePoint : FindGroundPerchNear(target);
                NPC.netUpdate = true;
            }
            // else: keep wandering this cycle.
        }

        /// <summary>Nearest TileID.Trees tile (with clear air above it to land in) within
        /// TreeSearchRadiusTiles. A one-off scan — only run when a perch decision is made, not
        /// every tick.</summary>
        private bool TryFindTreePerch(out Vector2 perchPoint)
        {
            perchPoint = Vector2.Zero;
            int centerTileX = (int)(NPC.Center.X / 16f);
            int centerTileY = (int)(NPC.Center.Y / 16f);
            int bestDistSq = int.MaxValue;
            bool found = false;

            int minX = System.Math.Max(1, centerTileX - TreeSearchRadiusTiles);
            int maxX = System.Math.Min(Main.maxTilesX - 2, centerTileX + TreeSearchRadiusTiles);
            int minY = System.Math.Max(1, centerTileY - TreeSearchRadiusTiles);
            int maxY = System.Math.Min(Main.maxTilesY - 2, centerTileY + TreeSearchRadiusTiles);

            for (int tileX = minX; tileX <= maxX; tileX++)
            {
                for (int tileY = minY; tileY <= maxY; tileY++)
                {
                    Tile tile = Main.tile[tileX, tileY];
                    if (tile == null || !tile.HasTile || tile.TileType != TileID.Trees)
                    {
                        continue;
                    }

                    Tile above = Main.tile[tileX, tileY - 1];
                    if (above != null && above.HasTile && Main.tileSolid[above.TileType])
                    {
                        continue; // no clear air above this trunk tile to land in
                    }

                    int dx = tileX - centerTileX;
                    int dy = tileY - centerTileY;
                    int distSq = dx * dx + dy * dy;
                    if (distSq >= bestDistSq)
                    {
                        continue;
                    }

                    bestDistSq = distSq;
                    perchPoint = new Vector2(tileX * 16f + 8f, tileY * 16f - 4f);
                    found = true;
                }
            }

            return found;
        }

        /// <summary>Fallback when no tree is in range: perch on solid ground near the player instead.</summary>
        private Vector2 FindGroundPerchNear(Player target)
        {
            float offsetX = Main.rand.NextFloat(-96f, 96f);
            float groundY = PuppetGroundDustWave.FindGroundY(target.Center.X + offsetX, target.Center.Y);
            return new Vector2(target.Center.X + offsetX, groundY - 12f);
        }

        // ── Dive bomb ─────────────────────────────────────────────────────────────
        private void TickDiveBombWindup(Player target)
        {
            if (target == null)
            {
                _state = OwlState.Wander;
                return;
            }

            // Hover just above/behind the player while winding up, growing ember dust as the tell.
            Vector2 holdPoint = target.Center + new Vector2(0f, -160f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (holdPoint - NPC.Center) * 0.05f, 0.2f);
            NPC.direction = target.Center.X < NPC.Center.X ? -1 : 1;

            float telegraphProgress = 1f - _stateTimer / (float)DiveBombWindupTicks;
            if (Main.rand.NextFloat() < 0.2f + telegraphProgress * 0.5f)
            {
                Dust ember = Dust.NewDustPerfect(NPC.Center, DustID.Torch,
                    Main.rand.NextVector2Circular(1.5f, 1.5f), 60, default,
                    MathHelper.Lerp(0.7f, 1.3f, telegraphProgress));
                ember.noGravity = true;
            }

            if (--_stateTimer > 0)
            {
                return;
            }

            // Lead the target's current velocity a little so the sweep isn't trivially sidestepped.
            Vector2 aimPoint = target.Center + target.velocity * 12f;
            Vector2 direction = (aimPoint - NPC.Center).SafeNormalize(new Vector2(NPC.direction, 0f));
            _diveBombTargetVelocity = direction * DiveBombSpeed;
            NPC.velocity = _diveBombTargetVelocity;
            NPC.damage = DiveBombDamage;
            _state = OwlState.DiveBombing;
            _stateTimer = DiveBombMaxTicks;
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = 0.2f }, NPC.Center);
            NPC.netUpdate = true;
        }

        private void TickDiveBombing()
        {
            NPC.velocity = _diveBombTargetVelocity;
            NPC.direction = NPC.velocity.X < 0f ? -1 : 1;
            NPC.rotation = NPC.velocity.ToRotation();

            Dust trail = Dust.NewDustPerfect(NPC.Center, DustID.Torch,
                -NPC.velocity * 0.1f + Main.rand.NextVector2Circular(1f, 1f), 60, default,
                Main.rand.NextFloat(1.1f, 1.6f));
            trail.noGravity = true;

            if (--_stateTimer <= 0)
            {
                EndDiveBomb();
            }
        }

        private void EndDiveBomb()
        {
            NPC.damage = 0;
            NPC.rotation = 0f;
            _diveBombCooldown = DiveBombCooldownTicks;
            _state = OwlState.Wander;
            NPC.netUpdate = true;
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            // Contact damage only during the committed sweep — NPC.damage is 0 the rest of the time,
            // so vanilla's own automatic contact-damage check simply can't fire outside this state.
            return _state == OwlState.DiveBombing;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurt)
        {
            // Only ever fires from the CanHitPlayer window above, so this is always a dive-bomb hit.
            // BuffID 148 — "Rabies" is its internal name, but it's vanilla's feral-bite debuff.
            target.AddBuff(BuffID.Rabies, 5 * 60);
        }

        // ── Perch + eye-beam ──────────────────────────────────────────────────────
        private void TickFlyingToPerch(Player target)
        {
            Vector2 toPerch = _perchPoint - NPC.Center;
            NPC.direction = toPerch.X < 0f ? -1 : 1;

            if (toPerch.Length() > 12f)
            {
                Vector2 desired = toPerch.SafeNormalize(Vector2.Zero) * WanderTopSpeed;
                NPC.velocity = Vector2.Lerp(NPC.velocity, desired, WanderAccel);

                // Give up and go back to wandering if the perch point turns out to be unreachable
                // (e.g. tiles changed under it) rather than hovering at it forever.
                if (++_stateTimer > 300)
                {
                    _state = OwlState.Wander;
                    _stateTimer = 0;
                }
                return;
            }

            NPC.velocity = Vector2.Zero;
            NPC.Center = _perchPoint;
            _state = OwlState.PerchLanding;
            _stateTimer = PerchLandingTicks;
        }

        private void TickPerchLanding()
        {
            NPC.velocity *= 0.5f;
            if (--_stateTimer <= 0)
            {
                _state = OwlState.EyeBeamTelegraph;
                _stateTimer = EyeBeamTelegraphTicks;
            }
        }

        private Vector2 EyePosition => NPC.Center + new Vector2(NPC.direction * 6f, -10f);

        private void TickEyeBeamTelegraph(Player target)
        {
            NPC.velocity = Vector2.Zero;

            if (target != null)
            {
                Vector2 aim = target.Center - NPC.Center;
                NPC.direction = aim.X < 0f ? -1 : 1;

                // Only two turned-head frames exist (screen-left / screen-right); a mostly-vertical
                // aim (target well above or below) has no dedicated art, so it just holds the
                // symmetric forward-facing pose per the authored sheet.
                if (System.Math.Abs(aim.X) < System.Math.Abs(aim.Y) * 0.5f)
                {
                    _eyeBeamLookFrame = FramePerchForward;
                }
                else
                {
                    _eyeBeamLookFrame = aim.X > 0f ? FrameLookRight : FrameLookLeft;
                }
            }

            // Growing glow: both size and spawn frequency ramp with telegraph progress, so the tell
            // reads as building toward the shot rather than a flat blink for 90 ticks.
            float progress = 1f - _stateTimer / (float)EyeBeamTelegraphTicks;
            Lighting.AddLight(EyePosition, 0.9f * progress, 0.7f * progress, 0.1f * progress);
            if (Main.rand.NextFloat() < 0.15f + progress * 0.5f)
            {
                Dust glow = Dust.NewDustPerfect(EyePosition, DustID.GoldFlame,
                    Vector2.Zero, 100, default, MathHelper.Lerp(0.6f, 1.8f, progress));
                glow.noGravity = true;
                glow.velocity *= 0.2f;
            }

            if (--_stateTimer > 0)
            {
                return;
            }

            FireEyeBeams(target);
            _state = OwlState.EyeBeamRecovery;
            _stateTimer = EyeBeamRecoveryTicks;
        }

        private void FireEyeBeams(Player target)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || target == null)
            {
                return;
            }

            Vector2 aim = (target.Center - NPC.Center).SafeNormalize(new Vector2(NPC.direction, 0f));
            Vector2 sideOffset = new Vector2(-aim.Y, aim.X) * EyeBeamSeparation;

            foreach (Vector2 origin in new[] { EyePosition - sideOffset, EyePosition + sideOffset })
            {
                Projectile.NewProjectile(NPC.GetSource_FromAI(), origin, aim * EyeBeamSpeed,
                    ModContent.ProjectileType<OwlEyeFlame>(), EyeBeamDamage, 1f, Main.myPlayer);
            }

            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.65f, Pitch = 0.3f }, NPC.Center);
        }

        private void TickEyeBeamRecovery()
        {
            if (--_stateTimer > 0)
            {
                return;
            }
            _state = OwlState.Wander;
            _decisionTimer = 0;
        }

        // ── Frame animation ───────────────────────────────────────────────────────
        // Owl.png is a 40x520 strip (13 frames of 40x40) — the sprite frame is taller than the
        // 32x32 hitbox (NPC.width/height), so the frame math below uses this, not NPC.height.
        private const int SpriteFrameSize = 40;

        private void UpdateFrame()
        {
            // aiStyle -1 means nothing sets these automatically — derive facing from actual
            // horizontal motion for the states that don't already aim NPC.direction themselves
            // (dive/eye-beam set it explicitly against the target instead, and are left alone here
            // since their velocity is momentarily zero or dive-locked, not a reliable direction cue).
            if (System.Math.Abs(NPC.velocity.X) > 0.05f)
            {
                NPC.direction = NPC.velocity.X < 0f ? -1 : 1;
            }
            NPC.spriteDirection = NPC.direction;

            if (_state == OwlState.EyeBeamTelegraph || _state == OwlState.EyeBeamRecovery)
            {
                // The turned-head frames are pre-authored per screen direction, not a symmetric pose
                // meant to be mirrored — force no-flip so FrameLookRight/FrameLookLeft draw as-is.
                NPC.spriteDirection = 1;
                NPC.frame.Y = _eyeBeamLookFrame * SpriteFrameSize;
                return;
            }

            if (_state == OwlState.DiveBombWindup || _state == OwlState.DiveBombing)
            {
                NPC.frame.Y = DiveFrame * SpriteFrameSize;
                return;
            }

            int[] frames = _state == OwlState.FlyingToPerch ? FlyFrames : IdleBlinkFrames;
            NPC.frameCounter++;
            if (NPC.frameCounter >= 6)
            {
                NPC.frameCounter = 0;
                int currentFrame = (int)(NPC.frame.Y / SpriteFrameSize);
                int currentIndex = System.Array.IndexOf(frames, currentFrame);
                int nextIndex = currentIndex < 0 ? 0 : (currentIndex + 1) % frames.Length;
                NPC.frame.Y = frames[nextIndex] * SpriteFrameSize;
            }
        }

        // Killed via Owl Father's NPC.StrikeInstantKill() call at the 50%-health threshold (see
        // OwlFatherInvader) — that routes through the normal StrikeNPC/checkDead pipeline, so OnKill
        // fires exactly like any other death regardless of dontTakeDamage above.
        public override void OnKill()
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 150; i++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood,
                    0f, -1f, 0, default, Main.rand.NextFloat(1f, 1.8f));
            }
        }
    }
}
