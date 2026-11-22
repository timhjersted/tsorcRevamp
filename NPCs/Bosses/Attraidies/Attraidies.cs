using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Content.Items.BossBags;
using tsorcRevamp.Content.Items.Lore;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Tools;
using tsorcRevamp.Content.Items.Weapons.Magic.Tomes;
using tsorcRevamp.Content.Items.Weapons.Melee.Broadswords;
using tsorcRevamp.Content.Items.Weapons.Ranged.Guns;
using tsorcRevamp.Content.Items.Weapons.Summon;
using tsorcRevamp.Content.Projectiles.Enemy.Marilith;
using tsorcRevamp.Content.Projectiles.Enemy.Okiku;
using tsorcRevamp.Content.Projectiles.VFX;
using tsorcRevamp.NPCs.Bosses.Okiku.FinalForm;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.Attraidies
{
    /// <summary>
    /// The final form. Fight state lives in ai[0..2] plus SendExtraAI; every machine runs the same move bodies from it,
    /// and the server alone picks moves, teleports, spawns projectiles and fragments, and lands the death strike.
    /// </summary>
    [AutoloadBossHead]
    public class Attraidies : AttraidiesPuppet
    {
        private enum Move
        {
            PoisonTrails,
            NebulaStorm,
            SolarDetonators,
            DarkLasers,
            VortexLightning,
            BlackFireRain,
            StardustLasers,
            SolarAndVortex,
        }

        private static readonly Move[][] PhaseMoves =
        {
            new[] { Move.PoisonTrails, Move.NebulaStorm, Move.SolarDetonators, Move.DarkLasers, Move.VortexLightning, Move.BlackFireRain, Move.StardustLasers },
            new[] { Move.DarkLasers, Move.PoisonTrails, Move.SolarAndVortex },
            new[] { Move.PoisonTrails, Move.DarkLasers },
        };

        // Indexed like PhaseMoves. Built once: the aura reads these every drawn frame.
        private static readonly Color[][][] PhaseMoveColors =
        {
            new[]
            {
                new[] { Color.YellowGreen },
                new[] { Color.Purple * 3 },
                new[] { Color.OrangeRed },
                new[] { Color.MediumPurple },
                new[] { new Color(0, 255, 120) },
                new[] { Color.Purple },
                new[] { Color.Cyan },
            },
            new[]
            {
                new[] { Color.MediumPurple, Color.Cyan, Color.Purple * 3 },
                new[] { Color.YellowGreen, Color.MediumPurple },
                new[] { Color.OrangeRed, Color.Teal },
            },
            new[]
            {
                new[] { Color.YellowGreen, Color.MediumPurple, Color.Cyan },
                new[] { Color.MediumPurple, Color.OrangeRed, Color.Teal },
            },
        };

        private static readonly Color[] FinalStandColors = { Color.YellowGreen, Color.Purple * 3, Color.Teal, Color.YellowGreen, Color.MediumPurple, Color.Cyan };

        private const int FinalStandPhase = 3;
        private const int IntroTicks = 120;
        private const int MoveGapTicks = 90;
        private const int DeathHiddenTick = 150;
        private const int DeathStrikeAfterTick = 400;
        private const int FragmentCount = 5;
        private const float AuraRadius = 500f;

        // The old body drew at 1.2x and the aura kept that size; the puppet itself is scaled separately.
        private const float AuraDrawScale = 1.2f;

        private const int DetonatorLeadTicks = 30;
        private const int VolleyThrowLeadTicks = ThrowTicks;
        private const int StardustSecondTeleportTick = 290;
        private const int StardustThirdTeleportTick = 580;
        private const int StardustEndTick = 849;
        private const int StardustBeamTick = 240;
        private const int StardustTeleportTellStart = 250;
        private const int StardustTeleportTellTicks = 40;
        private const int VortexTeleportTick = 5;
        private const int VortexTellTicks = 36;
        private const int SolarVortexOrbTick = 120;
        private const int SolarVortexPullEndTick = 720;
        private const float PullDistance = 1000f;
        private const float PullSpeed = 10f;
        private const int SharedLifeHitInterval = 20;

        private const int PoisonTrailDamage = 40;
        private const int BlackFireDamage = 35;
        private const int StardustLaserDamage = 45;
        private const int NebulaShotDamage = 35;
        private const int SolarDetonationDamage = 45;
        private const int LightningStrikeDamage = 35;
        private const int DarkLaserDamage = 45;

        private static readonly Color PoisonColor = Color.YellowGreen;
        private static readonly Color NebulaColor = new Color(235, 90, 200);
        private static readonly Color SolarColor = Color.OrangeRed;
        private static readonly Color VortexColor = new Color(0, 230, 170);
        private static readonly Color StardustColor = Color.Cyan;
        private static readonly Color VoidColor = new Color(170, 80, 255);

        private static Effect auraEffect;

        // Per machine: the projectile slot's last accepted hit on the shared life pool.
        private static readonly uint[] sharedHitTick = new uint[Main.maxProjectiles];
        private static readonly int[] sharedHitType = new int[Main.maxProjectiles];
        private static readonly int[] sharedHitTimeLeft = new int[Main.maxProjectiles];

        private NPCDespawnHandler despawnHandler;
        private AttackBag<int>[] phaseBags;

        private bool initialized;
        private bool moveJustStarted;
        private int seenFightPhase;

        // Synced fight state.
        private int introTimer;
        private int attackSwitchDelay;
        private int transitionTimer;
        private int deathTimer;
        private int clockwise;
        private bool nebulaUsed;
        private float detonatorHoverAngle;
        private bool hasPendingDetonator;
        private Vector2 pendingDetonatorOffset;
        private Vector2 pendingVolleyVelocity;

        // Local aura animation.
        private float baseRadius;
        private float collapseSpeed;
        private float baseFade;
        private float fadeInPercent;
        private float ringCollapse;
        private float auraExpandedSize;
        private float effectTimer;

        private int CurrentAttackMode
        {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }

        private float AttackTimer
        {
            get => NPC.ai[1];
            set => NPC.ai[1] = value;
        }

        private int FightPhase
        {
            get => (int)NPC.ai[2];
            set => NPC.ai[2] = value;
        }

        private Move CurrentMove
        {
            get
            {
                Move[] moves = PhaseMoves[Math.Min(FightPhase, PhaseMoves.Length - 1)];
                int index = Math.Clamp(CurrentAttackMode, 0, moves.Length - 1);
                return moves[index];
            }
        }

        private bool IsDying => deathTimer > 0 || NPC.life <= 1;

        protected override NPCDespawnHandler DespawnHandler => despawnHandler;
        protected override Color TeleportFlashColor => VoidColor;
        internal override float BodyOpacity
        {
            get
            {
                if (introTimer < IntroTicks)
                {
                    return 0f;
                }

                return MathHelper.Clamp(1f - deathTimer / (float)DeathHiddenTick, 0f, 1f);
            }
        }

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();

            // Set NeedsExpertScaling for lifeMax scaling in multiplayer, otherwise scaling is skipped since NPC.damage=0.
            NPCID.Sets.NeedsExpertScaling[Type] = true;
        }

        public override void SetDefaults()
        {
            ApplyBodyDefaults();
            NPC.npcSlots = 10;
            NPC.defense = 25;
            NPC.timeLeft = 22500;
            Music = 12;
            NPC.lifeMax = 400000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.boss = true;
            NPC.value = 600000;
            NPC.rarity = 30;
            NPC.dontTakeDamage = true;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.Attraidies.DespawnHandler"), Color.DarkMagenta, DustID.PurpleCrystalShard);
        }

        public override void Unload()
        {
            auraEffect = null;
        }

        // ── Fight loop ──────────────────────────────────────────────────────
        protected override void TickForm()
        {
            bool authority = IsAuthority;

            if (!initialized)
            {
                initialized = true;
                seenFightPhase = FightPhase;
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Attraidies.Quote"), 175, 75, 255);

                if (!Main.tile[1365, ExpandedWorldTransform.MapTileY(1365, 280)].IsActuated)
                {
                    ActuateAttraidiesArena();
                }
            }

            effectTimer++;
            FreeHandGesture = null;
            NPC.dontTakeDamage = introTimer < IntroTicks || transitionTimer > 0 || IsDying;

            if (introTimer < IntroTicks)
            {
                RunIntro(authority);
                return;
            }

            if (FightPhase != seenFightPhase)
            {
                seenFightPhase = FightPhase;
                StartAura(0.2f);
            }

            ManageTime(authority);
            HandleAura();
            SpawnPhaseDust();

            if (IsDying)
            {
                transitionTimer = 0;
                attackSwitchDelay = 999;
                deathTimer++;
                SetPose(AttraidiesPose.Overhead);
                HandleDeath(authority);
                return;
            }

            if (transitionTimer > 0)
            {
                SetPose(AttraidiesPose.Overhead);
                TickTransition(authority);
                return;
            }

            if (authority)
            {
                CheckPhaseThresholds();
                if (transitionTimer > 0)
                {
                    return;
                }
            }

            if (attackSwitchDelay > 0)
            {
                attackSwitchDelay--;
                NPC.velocity *= 0.95f;
                PoseDuringGap();
                return;
            }

            RunCurrentMove();
            moveJustStarted = false;
            AttackTimer++;
        }

        private void RunIntro(bool authority)
        {
            introTimer++;
            SetPose(AttraidiesPose.Overhead);

            if (introTimer == 1 && authority)
            {
                NPC.Center = TargetPlayer.Center + new Vector2(0, -300);
                RequestNetworkSnapshot();
            }

            if (introTimer == 60 && authority)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(32, 32), Main.rand.NextVector2CircularEdge(1, 1), ModContent.ProjectileType<RealityCrack>(), 0, 0, Main.myPlayer);
            }

            if (introTimer == IntroTicks)
            {
                SoundEngine.PlaySound(new SoundStyle("Terraria/Sounds/Thunder_0") with { Volume = 2f });
                NPC.dontTakeDamage = false;

                if (authority)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<BossDeath>(), 0, 0, Main.myPlayer);
                    DrawNextMove();
                    AttackTimer = 0;
                    RequestNetworkSnapshot();
                }
            }
        }

        // The sky darkens with the boss's health, then dawn breaks below a quarter. Server and single player own the clock.
        private void ManageTime(bool authority)
        {
            if (!authority)
            {
                return;
            }

            float quarterLife = NPC.lifeMax * (1f / 4f);
            float maxTime;
            if (NPC.life > quarterLife)
            {
                Main.dayTime = false;
                float ratio = (NPC.life - quarterLife) / (NPC.lifeMax - quarterLife);
                maxTime = MathHelper.Lerp((float)Main.nightLength, 1, ratio);
            }
            else
            {
                Main.dayTime = true;
                maxTime = MathHelper.Lerp((float)Main.dayLength * 0.1f, 1, NPC.life / quarterLife);
            }

            if (Main.time < maxTime)
            {
                Main.time += 5;
            }
            else
            {
                Main.time = maxTime;
            }

            ShareWorldTime();
        }

        private void TickTransition(bool authority)
        {
            transitionTimer--;
            NPC.velocity *= 0.9f;

            if (transitionTimer == 1 && authority)
            {
                FightPhase++;
                seenFightPhase = FightPhase;
                AttackTimer = 0;
                StartAura(0.2f);
                ClearObstructiveProjectiles();
                DeactivateAll(ModContent.NPCType<AttraidiesFragment>());

                if (FightPhase < FinalStandPhase)
                {
                    DrawNextMove();
                }

                RequestNetworkSnapshot();
            }
        }

        private void CheckPhaseThresholds()
        {
            if (FightPhase == 0 && NPC.life < NPC.lifeMax * 0.7f)
            {
                BeginTransition(120);
            }
            else if (FightPhase == 1 && NPC.life < NPC.lifeMax * 0.5f)
            {
                BeginTransition(60);
            }
            else if (FightPhase == 2 && NPC.life < NPC.lifeMax * 0.20f)
            {
                ClearObstructiveProjectiles();
                NPC.Center = TargetPlayer.Center + new Vector2(0, -600);
                if (NPC.Center.Y < 160)
                {
                    NPC.position.Y = 160;
                }

                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Attraidies.Thought"), Color.MediumPurple);
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Attraidies.Flight"), Color.Green);
                BeginTransition(120);
            }
        }

        private void BeginTransition(int ticks)
        {
            transitionTimer = ticks;
            NPC.dontTakeDamage = true;
            RequestNetworkSnapshot();
        }

        private void ClearObstructiveProjectiles()
        {
            UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<VortexOrb>());
            UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<SolarDetonator>());
            UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<SolarBlast>());
            UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<MarilithLightning>());
            UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<DarkLaser>());
            UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<StardustShot>());
            UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<StardustBeam>());
            UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<NebulaShot>());
        }

        private void SpawnPhaseDust()
        {
            if (FightPhase == 1)
            {
                int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Torch, NPC.velocity.X, NPC.velocity.Y, 200, Color.Red, 1f);
                Main.dust[dust].noGravity = true;
            }

            if (FightPhase == 2)
            {
                int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, 54, NPC.velocity.X, NPC.velocity.Y, 140, Color.Red, 2f);
                Main.dust[dust].noGravity = true;
            }
        }

        // ── Move selection ──────────────────────────────────────────────────
        // Each phase keeps its own bag because a card is an index into that phase's move list.
        private void DrawNextMove()
        {
            if (phaseBags == null)
            {
                phaseBags = new AttackBag<int>[PhaseMoves.Length];
                for (int phase = 0; phase < PhaseMoves.Length; phase++)
                {
                    int moveCount = PhaseMoves[phase].Length;
                    phaseBags[phase] = new AttackBag<int>(cards =>
                    {
                        for (int card = 0; card < moveCount; card++)
                        {
                            cards.Add(card);
                        }
                    });
                }
            }

            if (phaseBags[FightPhase].TryDraw(card => true, out int drawn))
            {
                CurrentAttackMode = drawn;
            }

            moveJustStarted = true;
            hasPendingDetonator = false;
        }

        private void NextAttack()
        {
            if (!IsAuthority)
            {
                return;
            }

            if (CurrentMove == Move.NebulaStorm)
            {
                nebulaUsed = true;
            }

            DrawNextMove();
            attackSwitchDelay = MoveGapTicks;
            StartAura(0.2f);
            AttackTimer = 0;
            RequestNetworkSnapshot();
        }

        private void RunCurrentMove()
        {
            if (FightPhase >= FinalStandPhase)
            {
                FinalStand();
                return;
            }

            switch (CurrentMove)
            {
                case Move.PoisonTrails:
                    ChargingPoisonTrails();
                    break;
                case Move.NebulaStorm:
                    NebulaStorm();
                    break;
                case Move.SolarDetonators:
                    SolarDetonators();
                    break;
                case Move.DarkLasers:
                    DarkLasers();
                    break;
                case Move.VortexLightning:
                    VortexLightning();
                    break;
                case Move.BlackFireRain:
                    BlackFireRain();
                    break;
                case Move.StardustLasers:
                    StardustLasers();
                    break;
                case Move.SolarAndVortex:
                    SolarAndVortex();
                    break;
            }
        }

        // The end of the gap telegraphs a move that opens with a teleport, since the next move is already chosen.
        private void PoseDuringGap()
        {
            SetPose(AttraidiesPose.Carry);

            if (FightPhase >= FinalStandPhase)
            {
                return;
            }

            if (CurrentMove == Move.VortexLightning && attackSwitchDelay <= VortexTellTicks)
            {
                SetPose(AttraidiesPose.Overhead);
                ShowTeleportTell(VortexArrivalPoint, VortexColor);
            }

            if (CurrentMove == Move.StardustLasers && attackSwitchDelay <= StardustTeleportTellTicks)
            {
                SetPose(AttraidiesPose.Overhead);
                ShowTeleportTell(StardustTeleportPoint(0), StardustColor);
            }
        }

        private void ShowTeleportTell(Vector2 arrivalPoint, Color color)
        {
            GatherDust(NPC.Center, color, 34f);
            GatherDust(arrivalPoint, color, 34f);
            GatherDust(arrivalPoint, color, 18f);
        }

        // ── Shared move pieces ──────────────────────────────────────────────
        private Vector2 VortexArrivalPoint => TargetPlayer.Center + new Vector2(0, -300);

        private void SpawnVortexOrbs()
        {
            if (!IsAuthority)
            {
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                Vector2 velocity = new Vector2(10, 0).RotatedBy(Math.PI / 2f * i);
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, velocity, ModContent.ProjectileType<VortexOrb>(), LightningStrikeDamage, .5f, Main.myPlayer, i, FightPhase);
            }
        }

        private void SpawnFragments()
        {
            if (!IsAuthority)
            {
                return;
            }

            for (int i = 0; i < FragmentCount; i++)
            {
                NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<AttraidiesFragment>(), ai0: NPC.whoAmI, ai1: i, ai3: FightPhase);
            }
        }

        private void SpawnRing(int count, float speed, int projectileType, int damage, float knockback, float ai0 = 0f, float ai1 = 0f)
        {
            if (!IsAuthority)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = new Vector2(speed, 0).RotatedBy(i * MathHelper.TwoPi / count);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, velocity, projectileType, damage, knockback, Main.myPlayer, ai0, ai1);
            }
        }

        // Rolls the next detonator's offset from the target ahead of time, so the staff and dust can show the spot first.
        private void PrepareDetonator(float squareHalfSize)
        {
            if (!IsAuthority || hasPendingDetonator)
            {
                return;
            }

            hasPendingDetonator = true;
            pendingDetonatorOffset = Main.rand.NextVector2Square(-squareHalfSize, squareHalfSize);
            RequestNetworkSnapshot();
        }

        private void ReleaseDetonator(float squareHalfSize)
        {
            StartAura(0.15f);
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 1 }, NPC.Center);

            if (!IsAuthority)
            {
                return;
            }

            // A move's first tick can land on the schedule before any lead tick has passed.
            PrepareDetonator(squareHalfSize);
            Vector2 position = TargetPlayer.Center + pendingDetonatorOffset;
            Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), position, Vector2.Zero, ModContent.ProjectileType<SolarDetonator>(), SolarDetonationDamage, .5f, Main.myPlayer, NPC.target);
            hasPendingDetonator = false;
            RequestNetworkSnapshot();
        }

        private void TelegraphPendingDetonator()
        {
            if (!hasPendingDetonator)
            {
                return;
            }

            Vector2 point = TargetPlayer.Center + pendingDetonatorOffset;
            SetAimPose(point);
            GatherDust(point, SolarColor, 30f);
            GatherDust(StaffTip, SolarColor, 12f, 0.7f);
        }

        private void ApplyEarthAlignment(int ticks)
        {
            Player localPlayer = LocalLivingPlayer;
            if (localPlayer != null)
            {
                localPlayer.AddBuff(ModContent.BuffType<Buffs.EarthAlignment>(), ticks);
            }
        }

        private void PullLocalPlayerIfFar()
        {
            Player localPlayer = LocalLivingPlayer;
            if (localPlayer != null && localPlayer.Center.Distance(NPC.Center) > PullDistance)
            {
                localPlayer.velocity = UsefulFunctions.Aim(localPlayer.Center, NPC.Center, PullSpeed);
                localPlayer.AddBuff(BuffID.Electrified, 100);
            }
        }

        // ── Moves ───────────────────────────────────────────────────────────
        private void ChargingPoisonTrails()
        {
            int timer = (int)AttackTimer;
            Lighting.AddLight(NPC.position, Color.DarkOrange.ToVector3());

            if (FightPhase == 2)
            {
                ApplyEarthAlignment(300);
            }

            int chargeDelay = 60;
            if (FightPhase == 1)
            {
                chargeDelay = 75;
            }

            if (FightPhase == 2)
            {
                chargeDelay = 90;
            }

            // The opening is silent charge time: the staff tracks the target while poison gathers at the gem.
            if (timer < 120)
            {
                SetAimPose(TargetPlayer.Center);
                float chargeProgress = timer / 120f;
                GatherDust(StaffTip, PoisonColor, MathHelper.Lerp(12f, 30f, chargeProgress), MathHelper.Lerp(0.7f, 1.2f, chargeProgress));
                return;
            }

            int cycleTick = timer % chargeDelay;
            bool ringTellActive = FightPhase > 0 && timer < 860 && cycleTick >= 30 && cycleTick < 45;

            if (ringTellActive)
            {
                SetPose(AttraidiesPose.Overhead);
                GatherDust(NPC.Center, PoisonColor, 40f);
            }
            else if (cycleTick >= chargeDelay - 20)
            {
                SetAimPose(TargetPlayer.Center);
                GatherDust(StaffTip, PoisonColor, 14f);
            }
            else
            {
                SetPose(AttraidiesPose.Carry);
            }

            if (cycleTick == 0)
            {
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 1 }, NPC.Center);
                StartAura(0.15f);
                NPC.velocity = UsefulFunctions.Aim(NPC.Center, TargetPlayer.Center, 20);

                if (IsAuthority)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<PoisonTrail>(), PoisonTrailDamage, 1, Main.myPlayer, 1, NPC.whoAmI);
                }
            }

            if (cycleTick > 45)
            {
                NPC.velocity *= 0.9f;
            }

            if (cycleTick == 45 && FightPhase > 0 && timer < 860)
            {
                if (FightPhase >= 2 && timer % (chargeDelay * 2) == 45)
                {
                    SpawnRing(8, 5f, ModContent.ProjectileType<StardustShot>(), StardustLaserDamage, 1, NPC.target, 120);
                }

                SpawnRing(12, 5f, ModContent.ProjectileType<EnemyAttraidiesBlackFire>(), BlackFireDamage, -1, -1);
            }

            if (timer >= 860)
            {
                NPC.velocity *= 0.9f;
            }

            if (timer > 870)
            {
                NextAttack();
            }
        }

        private void BlackFireRain()
        {
            int timer = (int)AttackTimer;
            Lighting.AddLight(NPC.position, Color.Purple.ToVector3());
            UsefulFunctions.SmoothHoming(NPC, TargetPlayer.Center + new Vector2(0, -300), 0.05f, 5f);
            SetPose(AttraidiesPose.Overhead);

            if (timer > 15 && timer % 45 == 0)
            {
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 1 }, NPC.Center);
                StartAura(0.15f);

                if (IsAuthority)
                {
                    Vector2 tip = StaffTip;
                    for (int i = 0; i < 10; i++)
                    {
                        // Mostly for show, though these do deal damage: a clump thrown straight into the sky.
                        Vector2 position = tip + new Vector2(Main.rand.Next(-20, 20), Main.rand.Next(-20, 20));
                        Vector2 velocity = new Vector2(Main.rand.Next(-2, 2), -50);
                        Projectile blackFire = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), position, velocity, ModContent.ProjectileType<EnemyBlackFireVisual>(), BlackFireDamage, .5f, Main.myPlayer);
                        blackFire.timeLeft = 20;
                    }
                }
            }

            if (timer > 75 && timer % 5 == 0 && IsAuthority)
            {
                // Falls through blocks until it reaches the player's height; its explosion is harmless.
                Vector2 position = TargetPlayer.Center + new Vector2(Main.rand.Next(-1400, 1400), -700);
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), position, new Vector2(0, 5), ModContent.ProjectileType<EnemyAttraidiesBlackFire>(), BlackFireDamage, .5f, Main.myPlayer, NPC.target);
            }

            if (timer > 600)
            {
                NextAttack();
            }
        }

        private void DarkLasers()
        {
            int timer = (int)AttackTimer;
            Lighting.AddLight(NPC.position, Color.Purple.ToVector3());
            ApplyEarthAlignment(1200);

            if (timer <= 90)
            {
                SetPose(AttraidiesPose.Overhead);
            }
            else if (timer < 240)
            {
                SetPose(AttraidiesPose.Conduct);
            }
            else
            {
                SetPose(AttraidiesPose.Overhead);
            }

            if (timer < 90)
            {
                UsefulFunctions.SmoothHoming(NPC, TargetPlayer.Center + new Vector2(0, -500), 0.5f, 50f);
            }
            else
            {
                StartAura(0.15f);
                NPC.velocity *= 0.9f;
            }

            if (timer == 5)
            {
                clockwise = 1;

                if (FightPhase != 2)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Attraidies.Weightless"), Color.DeepSkyBlue);
                }

                if (FightPhase != 0)
                {
                    SoundEngine.PlaySound(SoundID.Shatter with { Volume = 1 }, NPC.Center);
                    SpawnFragments();
                }

                if (IsAuthority)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 aimVec = new Vector2(1, 0).RotatedBy(i * MathHelper.TwoPi / 5);
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, aimVec, ModContent.ProjectileType<DarkLaser>(), DarkLaserDamage, .5f, Main.myPlayer, 0, NPC.whoAmI);
                    }
                }
            }

            if (FightPhase == 0)
            {
                if (timer % 10 == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item7 with { Volume = 3 }, NPC.Center);
                }

                if (timer % 2 == 0 && timer > 120 && IsAuthority)
                {
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Main.rand.NextVector2CircularEdge(8, 8), ModContent.ProjectileType<ObscureShot>(), DarkLaserDamage, .5f, Main.myPlayer);
                }
            }

            if (FightPhase == 1 && timer > 120 && timer < 1000)
            {
                if (timer % 140 == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 1 }, NPC.Center);
                    SpawnRing(4, 5f, ModContent.ProjectileType<StardustShot>(), StardustLaserDamage, .5f, NPC.target, 100);
                }

                if (timer % 120 == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.8f }, NPC.Center);
                    SpawnRing(16, 5f, ModContent.ProjectileType<NebulaShot>(), NebulaShotDamage, 1, clockwise);

                    if (IsAuthority)
                    {
                        clockwise *= -1;
                        RequestNetworkSnapshot();
                    }
                }
            }

            if (FightPhase == 2)
            {
                if (timer == 5)
                {
                    StartAura(0.2f);
                    SpawnVortexOrbs();
                }

                if (timer < 1000)
                {
                    if ((timer + DetonatorLeadTicks) % 60 == 0)
                    {
                        PrepareDetonator(300f);
                    }

                    if (timer % 60 >= 60 - DetonatorLeadTicks)
                    {
                        TelegraphPendingDetonator();
                    }

                    if (timer % 60 == 0)
                    {
                        ReleaseDetonator(300f);
                    }
                }
            }

            if (timer > 1200)
            {
                NextAttack();
            }
        }

        private Vector2 StardustTeleportPoint(int teleportIndex)
        {
            float angle = (float)(Math.PI / 2f);
            if (teleportIndex == 0)
            {
                angle = (float)(-Math.PI / 3f);
            }
            else if (teleportIndex == 1)
            {
                angle = (float)(4f * Math.PI / 3f);
            }

            return TargetPlayer.Center + new Vector2(-500, 0).RotatedBy(angle);
        }

        private void TeleportForStardust(int teleportIndex)
        {
            if (IsAuthority)
            {
                NPC.Center = StardustTeleportPoint(teleportIndex);
                NPC.velocity = Vector2.Zero;
                RequestNetworkSnapshot();
            }
        }

        // Three teleports with a volley fan each. Stars hang where they stop, then all fire together at segment tick 240.
        private void StardustLasers()
        {
            int timer = (int)AttackTimer;
            Lighting.AddLight(NPC.position, Color.Cyan.ToVector3());

            int segmentIndex = 0;
            int segmentStart = 0;
            if (timer >= StardustThirdTeleportTick)
            {
                segmentIndex = 2;
                segmentStart = StardustThirdTeleportTick;
            }
            else if (timer >= StardustSecondTeleportTick)
            {
                segmentIndex = 1;
                segmentStart = StardustSecondTeleportTick;
            }

            int segmentTick = timer - segmentStart;
            bool opening = moveJustStarted || (!IsAuthority && timer == 1);

            if (moveJustStarted)
            {
                TeleportForStardust(0);
            }

            if (timer == StardustSecondTeleportTick)
            {
                TeleportForStardust(1);
            }

            if (timer == StardustThirdTeleportTick)
            {
                TeleportForStardust(2);
            }

            bool scheduledVolley = timer > 1 && segmentTick % 45 == 0 && segmentTick < 180;
            bool throwLeadTick = segmentTick % 45 == 45 - VolleyThrowLeadTicks && segmentTick < 180 - VolleyThrowLeadTicks;

            if (throwLeadTick && IsAuthority)
            {
                RollVolleyVelocity();
            }

            if (opening || scheduledVolley)
            {
                StartAura(0.15f);
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 1 }, NPC.Center);

                if (IsAuthority)
                {
                    // Later volleys were rolled at their throw lead; the first of a segment has no earlier tick to use.
                    if (opening || segmentTick == 0)
                    {
                        RollVolleyVelocity();
                    }

                    Vector2 tip = StaffTip;
                    int firingDelay = StardustBeamTick - segmentTick;
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 velocity = pendingVolleyVelocity + new Vector2(Main.rand.Next(-5, 5), Main.rand.Next(-5, 5));
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), tip, velocity, ModContent.ProjectileType<StardustShot>(), StardustLaserDamage, .5f, Main.myPlayer, NPC.target, firingDelay);
                    }
                }
            }

            bool nearVolley = segmentTick % 45 >= 45 - VolleyThrowLeadTicks || segmentTick % 45 <= VolleyThrowLeadTicks;
            if (segmentTick < 180 && nearVolley && pendingVolleyVelocity != Vector2.Zero)
            {
                SetThrowPose(pendingVolleyVelocity);
            }
            else if (segmentTick < 180)
            {
                SetPose(AttraidiesPose.Overhead);
            }
            else if (segmentTick < StardustBeamTick)
            {
                SetPose(AttraidiesPose.Conduct);
            }
            else if (segmentTick < StardustTeleportTellStart || segmentIndex == 2)
            {
                SetAimPose(TargetPlayer.Center);
            }
            else
            {
                SetPose(AttraidiesPose.Overhead);
                ShowTeleportTell(StardustTeleportPoint(segmentIndex + 1), StardustColor);
            }

            if (timer >= StardustEndTick)
            {
                NextAttack();
            }
        }

        private void RollVolleyVelocity()
        {
            int speed = 35;
            pendingVolleyVelocity = new Vector2(Main.rand.Next(-speed, speed), Main.rand.Next(-speed, speed));
            RequestNetworkSnapshot();
        }

        private void VortexLightning()
        {
            int timer = (int)AttackTimer;
            Lighting.AddLight(NPC.position, Color.Teal.ToVector3());

            if (timer < VortexTeleportTick)
            {
                SetPose(AttraidiesPose.Overhead);
                ShowTeleportTell(VortexArrivalPoint, VortexColor);
            }
            else if (timer < VortexTeleportTick + 30)
            {
                SetPose(AttraidiesPose.Overhead);
            }
            else
            {
                SetPose(AttraidiesPose.Carry);
            }

            if (timer > 180)
            {
                PullLocalPlayerIfFar();
            }

            if (timer == VortexTeleportTick)
            {
                StartAura(0.20f);

                if (IsAuthority)
                {
                    NPC.velocity = Vector2.Zero;
                    NPC.Center = VortexArrivalPoint;
                    RequestNetworkSnapshot();
                }

                SpawnVortexOrbs();
            }

            if (timer > 600)
            {
                NextAttack();
            }
        }

        private void SolarDetonators()
        {
            int timer = (int)AttackTimer;
            Lighting.AddLight(NPC.position, Color.OrangeRed.ToVector3());
            Vector2 homingTarget = TargetPlayer.Center + new Vector2(150, 0).RotatedBy(detonatorHoverAngle);
            UsefulFunctions.SmoothHoming(NPC, homingTarget, 0.1f, 5f, bufferZone: true);
            detonatorHoverAngle += 0.01f;
            SetPose(AttraidiesPose.Carry);

            if (timer < 550)
            {
                if ((timer + DetonatorLeadTicks) % 45 == 0)
                {
                    PrepareDetonator(600f);
                }

                if (timer % 45 >= 45 - DetonatorLeadTicks)
                {
                    TelegraphPendingDetonator();
                }

                if (timer % 45 == 0)
                {
                    ReleaseDetonator(600f);
                }
            }

            if (timer > 700)
            {
                NextAttack();
            }
        }

        private void NebulaStorm()
        {
            int timer = (int)AttackTimer;
            Lighting.AddLight(NPC.position, Color.HotPink.ToVector3());
            NPC.velocity *= 0.95f;

            if (timer < 90)
            {
                SetPose(AttraidiesPose.Conduct);
                GatherDust(StaffTip, NebulaColor, 18f);
            }
            else if (timer > 90 && timer % 120 >= 90)
            {
                SetPose(AttraidiesPose.Overhead);
                GatherDust(StaffTip, NebulaColor, 14f);
            }
            else
            {
                SetPose(AttraidiesPose.Carry);
            }

            if (timer == 90)
            {
                StartAura(0.2f);
                SoundEngine.PlaySound(SoundID.Shatter with { Volume = 1 }, NPC.Center);
                SpawnFragments();
            }

            if (timer % 120 == 0 && timer > 120)
            {
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 1 }, NPC.Center);
                StartAura(0.15f);

                // The fight's first Nebula Storm flies straight; every later one curves.
                int curve = 0;
                if (nebulaUsed)
                {
                    curve = clockwise;
                    if (curve == 0)
                    {
                        curve = 1;
                    }
                }

                SpawnRing(4, 5f, ModContent.ProjectileType<NebulaShot>(), NebulaShotDamage, 1, curve);
            }

            if (timer >= 900)
            {
                NextAttack();
            }
        }

        private void SolarAndVortex()
        {
            int timer = (int)AttackTimer;

            if (timer < SolarVortexOrbTick)
            {
                SetPose(AttraidiesPose.Conduct);
                GatherDust(StaffTip, VortexColor, 18f);
                GatherDust(StaffTip, SolarColor, 18f);
                return;
            }

            SetPose(AttraidiesPose.Carry);

            // The pull belongs to the orbs: they spawn at tick 120 and live 600 ticks.
            if (timer > 300 && timer <= SolarVortexPullEndTick)
            {
                PullLocalPlayerIfFar();
            }

            if (timer == SolarVortexOrbTick)
            {
                StartAura(0.2f);
                SpawnVortexOrbs();
            }

            if (timer < 800)
            {
                if ((timer + DetonatorLeadTicks) % 90 == 0)
                {
                    PrepareDetonator(600f);
                }

                if (timer % 90 >= 90 - DetonatorLeadTicks)
                {
                    TelegraphPendingDetonator();
                }

                if (timer % 90 == 0)
                {
                    ReleaseDetonator(600f);
                }
            }

            if (timer > 900)
            {
                NextAttack();
            }
        }

        private void FinalStand()
        {
            int timer = (int)AttackTimer;
            NPC.velocity = Vector2.Zero;
            ApplyEarthAlignment(300);

            if (timer >= 90 && timer < 240)
            {
                SetPose(AttraidiesPose.Conduct);
            }
            else
            {
                SetPose(AttraidiesPose.Overhead);
            }

            // Both repeating bursts are led by the free hand and gathering dust for 30 ticks.
            bool ringLead = timer % 240 >= 210;
            bool pentagramLead = timer % 360 >= 90 && timer % 360 < 120;
            if (ringLead || pentagramLead)
            {
                FreeHandGesture = -0.25f;
                GatherDust(StaffTip, VoidColor, 18f);
            }

            if (timer == 2 && IsAuthority)
            {
                for (int i = 0; i < 5; i++)
                {
                    Vector2 aimVec = new Vector2(1, 0).RotatedBy(i * MathHelper.TwoPi / 5);
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, aimVec, ModContent.ProjectileType<DarkLaser>(), DarkLaserDamage, .5f, Main.myPlayer, 0, NPC.whoAmI, 10);
                }
            }

            if (timer == 60)
            {
                SpawnVortexOrbs();
            }

            if (timer == 240)
            {
                SoundEngine.PlaySound(SoundID.Shatter with { Volume = 1 }, NPC.Center);
                SpawnFragments();
            }

            if (timer % 240 == 0)
            {
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 1 }, NPC.Center);

                if (IsAuthority)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        Vector2 projVel = new Vector2(5, 0).RotatedBy(i * 2f * MathHelper.Pi / 16f);
                        if (i % 4 == 0)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projVel, ModContent.ProjectileType<StardustShot>(), StardustLaserDamage, 1, Main.myPlayer, NPC.target, 120);
                        }
                        else
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projVel, ModContent.ProjectileType<EnemyAttraidiesBlackFire>(), BlackFireDamage, -1, Main.myPlayer, -1);
                        }
                    }
                }
            }

            if (timer % 360 == 120)
            {
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 1 }, NPC.Center);

                if (IsAuthority)
                {
                    if (timer % 720 == 120)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            Vector2 position = NPC.Center + new Vector2(0, -1600).RotatedBy(2 * Math.PI / 5f * i);
                            Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), position, Vector2.Zero, ModContent.ProjectileType<SolarDetonator>(), SolarDetonationDamage, .5f, Main.myPlayer, i, FightPhase);
                        }

                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), TargetPlayer.Center, Vector2.Zero, ModContent.ProjectileType<SolarDetonator>(), SolarDetonationDamage, .5f, Main.myPlayer, 0, FightPhase);
                    }
                    else
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<PoisonTrail>(), PoisonTrailDamage, 2, Main.myPlayer, 2, NPC.whoAmI);
                    }
                }
            }
        }

        // ── Shared life pool ────────────────────────────────────────────────
        /// <summary>The active new or preserved final boss, or -1. Boss-owned projectiles follow whichever fight is running.</summary>
        public static int ActiveFinalBossIndex()
        {
            int? index = UsefulFunctions.GetFirstNPC(ModContent.NPCType<Attraidies>());
            if (!index.HasValue)
            {
                index = UsefulFunctions.GetFirstNPC(ModContent.NPCType<OldAttraidies>());
            }

            if (index.HasValue)
            {
                return index.Value;
            }

            return -1;
        }

        /// <summary>Runs where the hit is decided. While fragments exist one projectile may damage the pool once per 20 ticks.</summary>
        internal static bool SharedLifeAllowsHit(Projectile projectile)
        {
            if (!NPC.AnyNPCs(ModContent.NPCType<AttraidiesFragment>()))
            {
                return true;
            }

            int slot = projectile.whoAmI;

            // A different type or a longer remaining life means this slot was reused by a new projectile.
            bool sameProjectile = sharedHitType[slot] == projectile.type && projectile.timeLeft <= sharedHitTimeLeft[slot];
            bool onCooldown = Main.GameUpdateCount - sharedHitTick[slot] < SharedLifeHitInterval;
            return !(sameProjectile && onCooldown);
        }

        internal static void RecordSharedLifeHit(Projectile projectile)
        {
            int slot = projectile.whoAmI;
            sharedHitTick[slot] = Main.GameUpdateCount;
            sharedHitType[slot] = projectile.type;
            sharedHitTimeLeft[slot] = projectile.timeLeft;
        }

        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            if (!SharedLifeAllowsHit(projectile))
            {
                return false;
            }

            return base.CanBeHitByProjectile(projectile);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByProjectile(projectile, hit, damageDone);
            RecordSharedLifeHit(projectile);
        }

        // ── Aura ────────────────────────────────────────────────────────────
        private static void EnsureAuraEffect()
        {
            if (auraEffect == null || auraEffect.IsDisposed)
            {
                auraEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/AttraidiesAura", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
        }

        private static void DrawAuraLayers(Vector2 worldCenter, Color[] colors, float radius, float drawScale, float ringProgress, float fade, float sizeFactor, float colorClock, float whiteBlend)
        {
            Rectangle baseRectangle = new Rectangle(0, 0, (int)radius, (int)radius);
            Vector2 baseOrigin = baseRectangle.Size() / 2f;

            for (int i = 0; i < colors.Length; i++)
            {
                Color primaryColor = colors[i];
                Color secondColor = colors[(i + 1) % colors.Length];
                primaryColor = Color.Lerp(primaryColor, Color.White, whiteBlend);
                secondColor = Color.Lerp(secondColor, Color.White, whiteBlend);

                auraEffect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseTurbulent.Width);
                auraEffect.Parameters["effectSize"].SetValue(baseRectangle.Size());
                auraEffect.Parameters["effectColor1"].SetValue(UsefulFunctions.ShiftColor(primaryColor, colorClock).ToVector4());
                auraEffect.Parameters["effectColor2"].SetValue(UsefulFunctions.ShiftColor(secondColor, colorClock).ToVector4());
                auraEffect.Parameters["ringProgress"].SetValue(ringProgress);
                auraEffect.Parameters["fadePercent"].SetValue(fade);
                auraEffect.Parameters["scaleFactor"].SetValue(sizeFactor);
                auraEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.05f * 0.5f);
                auraEffect.Parameters["colorSplitAngle"].SetValue(MathHelper.TwoPi / colors.Length);
                auraEffect.CurrentTechnique.Passes[0].Apply();

                float layerRotation = MathHelper.TwoPi * i / colors.Length;
                Main.EntitySpriteDraw(tsorcRevamp.NoiseTurbulent, worldCenter - Main.screenPosition, baseRectangle, Color.White, layerRotation, baseOrigin, drawScale, SpriteEffects.None, 0);
            }
        }

        /// <summary>The Final Stand aura for the Boss Rematch Tome's hovered portrait. Leaves the caller's batch restarted.</summary>
        internal static void DrawTomeAura(Vector2 worldCenter, float drawScale)
        {
            EnsureAuraEffect();
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            DrawAuraLayers(worldCenter, FinalStandColors, AuraRadius, drawScale, 0.25f, 0.45f, 25f, Main.GameUpdateCount / 25f, 0f);
            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
        }

        private Color[] CurrentAuraColors
        {
            get
            {
                if (FightPhase >= FinalStandPhase)
                {
                    return FinalStandColors;
                }

                Color[][] phaseColors = PhaseMoveColors[FightPhase];
                return phaseColors[Math.Clamp(CurrentAttackMode, 0, phaseColors.Length - 1)];
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Lighting.AddLight((int)NPC.Center.X / 16, (int)NPC.Center.Y / 16, 1f, 0.4f, 0.4f);

            // The aura grows out of nothing through the intro; at factor 0 it has no size and would divide by zero.
            float introFactor = introTimer / (float)IntroTicks;
            if (introFactor > 0f)
            {
                EnsureAuraEffect();
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                float whiteBlend = 0f;
                if (attackSwitchDelay > 0)
                {
                    whiteBlend = MathHelper.Clamp(attackSwitchDelay / (float)MoveGapTicks, 0f, 1f);
                }

                DrawAuraLayers(NPC.Center, CurrentAuraColors, AuraRadius * introFactor, AuraDrawScale, baseRadius * introFactor, baseFade / introFactor, 25f * introFactor, effectTimer / 25f, whiteBlend);
                RestoreNpcBatch();
            }

            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }

        public void StartAura(float radius, float ringSpeed = 1.05f)
        {
            baseRadius = radius;
            collapseSpeed = ringSpeed;
            ringCollapse = 1;
            fadeInPercent = 0;
        }

        private void HandleAura()
        {
            if (fadeInPercent < 1)
            {
                fadeInPercent += 1f / 30f;
            }

            if (ringCollapse > 0.1f)
            {
                ringCollapse /= collapseSpeed;
            }

            float intensityMinimum = 0.77f;
            float radiusMinimum = 0.06f;

            if (FightPhase == FinalStandPhase)
            {
                intensityMinimum = 0.45f;
                radiusMinimum = 0.25f;
            }

            baseFade = Math.Min(baseFade + 0.02f, intensityMinimum);
            baseRadius = Math.Max(baseRadius - 0.01f, radiusMinimum);
        }

        // ── Death ───────────────────────────────────────────────────────────
        private void HandleDeath(bool authority)
        {
            StartAura(0.2f + auraExpandedSize);
            auraExpandedSize += 0.01f;
            NPC.dontTakeDamage = true;
            NPC.velocity *= 0.9f;

            if (deathTimer == 1)
            {
                DeactivateAll(ModContent.NPCType<AttraidiesFragment>());
                if (authority)
                {
                    ClearObstructiveProjectiles();
                    RequestNetworkSnapshot();
                }

                SoundEngine.PlaySound(new SoundStyle("Terraria/Sounds/Thunder_0"));
                SoundEngine.PlaySound(SoundID.Item62);
            }

            if (deathTimer == DeathHiddenTick)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Custom/EvilLaugh") with { Volume = 2, PlayOnlyIfFocused = false, MaxInstances = 0 });
                if (authority)
                {
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, TargetPlayer.Center, 1), ModContent.ProjectileType<RealityCrack>(), 0, 0, Main.myPlayer, 1);
                }
            }

            if (deathTimer > DeathStrikeAfterTick && authority)
            {
                if (!tsorcRevampWorld.PrimordialAge)
                {
                    NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<NPCs.Special.AbyssPortal>(), ai0: 1);
                }

                tsorcRevampWorld.AbyssPortalLocation = NPC.Center;
                NPC.dontTakeDamage = false;

                NPC.HitInfo deathHit = NPC.CalculateHitInfo(999999, 1, true, 0);
                NPC.StrikeNPC(deathHit, false, false);
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendStrikeNPC(NPC, deathHit);
                }
            }
        }

        public override bool CheckDead()
        {
            if (deathTimer < 300)
            {
                NPC.life = 1;
                return false;
            }

            return true;
        }

        public override bool CheckActive()
        {
            return false;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            base.HitEffect(hit);

            if (NPC.life > 0)
            {
                return;
            }

            // Every machine keeps this for its own map and portal logic.
            tsorcRevampWorld.AbyssPortalLocation = NPC.Center;

            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Custom/MetalShatter") with { Volume = 1f, PlayOnlyIfFocused = false, MaxInstances = 0 });
            for (int i = 0; i < 50; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(10, 10);
                int dust = Dust.NewDust(NPC.Center, 30, 30, DustID.PurpleCrystalShard, velocity.X, velocity.Y, 100, default, 5f);
                Main.dust[dust].noGravity = true;

                velocity = Main.rand.NextVector2Circular(10, 10);
                dust = Dust.NewDust(NPC.Center, 30, 30, DustID.PurpleCrystalShard, velocity.X, velocity.Y, 240, default, 5f);
                Main.dust[dust].noGravity = true;

                velocity = Main.rand.NextVector2Circular(20, 20);
                dust = Dust.NewDust(NPC.Center, 30, 30, 234, velocity.X, velocity.Y, 240, default, 5f);
                Main.dust[dust].noGravity = true;

                Dust.NewDust(NPC.Center, 30, 30, DustID.Torch, velocity.X, velocity.Y, 200, default, 3f);
            }
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<AttraidiesBag>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<TheEnd>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<ShatteredReflection>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<SeveringDusk>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<BloomShards>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<PyroclasticFlow>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<SoulOfAttraidies>(), 1, 10, 17));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<HeavenPiercer>()));
            npcLoot.Add(notExpertCondition);
        }

        public override void OnKill()
        {
            if (!tsorcRevampWorld.SuperHardMode)
            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Attraidies.Portal"), new Color(255, 255, 60));

                Main.hardMode = true;
                tsorcRevampWorld.SuperHardMode = true;
                tsorcRevampWorld.TheEnd = false;
                Systems.AbyssWorldSystem.BeginEntry();

                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.WorldData);
                }

                if (tsorcRevampWorld.RemixMap)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Attraidies.HallowDarker"), new Color(255, 40, 255));
                }
            }
            else if (!tsorcRevampWorld.PrimordialAge)
            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Attraidies.AbyssOpen"), new Color(255, 255, 60));
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Attraidies.Hint"), new Color(249, 202, 12));

                tsorcRevampWorld.SuperHardMode = true;
                Main.hardMode = true;
                tsorcRevampWorld.TheEnd = false;
            }

            if (tsorcRevampWorld.RemixMap && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>())))
            {
                (int xMin, int yMin, int xMax, int yMax)[] zones = new[]
                {
                    (7054, 1123, 7055, 1135),
                    (7002, 1194, 7003, 1206),
                    (7111, 1332, 7123, 1333),
                    (7171, 1232, 7183, 1233),
                    (7540,  994, 7541, 1012)
                };

                foreach (var (xMin, yMin, xMax, yMax) in zones)
                {
                    for (int x = xMin; x <= xMax; x++)
                    {
                        for (int y = yMin; y <= yMax; y++)
                        {
                            Tile tile = Main.tile[x, y];
                            if (tile != null && tile.IsActuated)
                            {
                                tile.IsActuated = false;
                                WorldGen.SquareTileFrame(x, y);
                            }
                        }
                    }

                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendTileSquare(-1, xMin, yMin, (xMax - xMin) + (yMax - yMin) + 2);
                    }
                }
            }
        }

        public static void ActuateAttraidiesArena()
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && !tsorcRevampWorld.RemixMap)
            {
                //Expanded-world Y offset: the arena (Y 59–306) is in the flat +200 band; X is invariant. 0 on legacy.
                int dY = ExpandedWorldTransform.MapTileY(1365, 280) - 280;
                for (int x = 1158; x < 1633; x++)
                {
                    for (int y = 59 + dY; y < 306 + dY; y++)
                    {
                        Wiring.ActuateForced(x, y);
                    }
                }
            }
        }

        // ── Sync ────────────────────────────────────────────────────────────
        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((short)introTimer);
            writer.Write((short)attackSwitchDelay);
            writer.Write((short)transitionTimer);
            writer.Write((short)deathTimer);
            writer.Write((sbyte)clockwise);
            writer.Write(nebulaUsed);
            writer.Write(detonatorHoverAngle);
            writer.Write(hasPendingDetonator);
            writer.WriteVector2(pendingDetonatorOffset);
            writer.WriteVector2(pendingVolleyVelocity);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            introTimer = reader.ReadInt16();

            int previousGap = attackSwitchDelay;
            attackSwitchDelay = reader.ReadInt16();
            if (attackSwitchDelay > previousGap && attackSwitchDelay == MoveGapTicks)
            {
                StartAura(0.2f);
            }

            transitionTimer = reader.ReadInt16();
            deathTimer = Math.Max(deathTimer, reader.ReadInt16());
            clockwise = reader.ReadSByte();
            nebulaUsed = reader.ReadBoolean();
            detonatorHoverAngle = reader.ReadSingle();
            hasPendingDetonator = reader.ReadBoolean();
            pendingDetonatorOffset = reader.ReadVector2();
            pendingVolleyVelocity = reader.ReadVector2();
        }
    }
}
