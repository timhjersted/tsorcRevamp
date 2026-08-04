using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy;
using tsorcRevamp.Projectiles.Enemy.Weapons;

namespace tsorcRevamp.NPCs
{
    internal enum KnightSpecialAttack : byte
    {
        None,
        EmberReversal,
        VenomWake,
        CrimsonStandard,
        CrimsonAdvance,
        FurnacePincer,
        RoyalStandard,
        StormbreakerEdict,
        CrimsonDominion,
        FurnaceHerald,
        StormHerald
    }

    internal enum KnightHeldProp : byte
    {
        None,
        Spear,
        Bomb,
        Magic
    }

    internal readonly struct KnightAttackStats
    {
        public readonly int SpearDamage;
        public readonly int MagicDamage;
        public readonly int GreatDamage;

        public KnightAttackStats(int spearDamage, int magicDamage, int greatDamage)
        {
            SpearDamage = spearDamage;
            MagicDamage = magicDamage;
            GreatDamage = greatDamage;
        }
    }

    /// <summary>
    /// One server-authored state clock for the Red Knight family's new multi-step attacks. Presentation reads this
    /// same clock, while damage is delegated to synchronized projectiles with explicit geometry.
    /// </summary>
    internal sealed class RedKnightAttackController
    {
        public KnightSpecialAttack Attack { get; private set; }
        public int Timer { get; private set; }
        public int Direction { get; private set; } = 1;
        public Vector2 LockedTarget { get; private set; }
        public Vector2 AuxiliaryTargetA { get; private set; }
        public Vector2 AuxiliaryTargetB { get; private set; }
        public Vector2 LockedVelocity { get; private set; }
        public float ArenaBaseRotation { get; private set; }
        public int ArenaRotationDirection { get; private set; } = 1;
        public bool HalfHeraldComplete { get; private set; }
        public bool ThirdHeraldComplete { get; private set; }

        int attackCooldown = 240;
        int dominionCooldown;

        public bool Active => Attack != KnightSpecialAttack.None;

        public string DebugAttackName => Attack switch
        {
            KnightSpecialAttack.EmberReversal => "Ember Reversal",
            KnightSpecialAttack.VenomWake => "Venom Wake",
            KnightSpecialAttack.CrimsonStandard => "Crimson Standard",
            KnightSpecialAttack.CrimsonAdvance => "Crimson Advance",
            KnightSpecialAttack.FurnacePincer => "Furnace Pincer",
            KnightSpecialAttack.RoyalStandard => "Royal Standard",
            KnightSpecialAttack.StormbreakerEdict => "Stormbreaker Edict",
            KnightSpecialAttack.CrimsonDominion => "Crimson Dominion",
            KnightSpecialAttack.FurnaceHerald => "Furnace Herald",
            KnightSpecialAttack.StormHerald => "Storm Herald",
            _ => null
        };

        public bool IsHerald => Attack == KnightSpecialAttack.FurnaceHerald || Attack == KnightSpecialAttack.StormHerald;

        public void TickCooldowns()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (attackCooldown > 0)
            {
                attackCooldown--;
            }
            if (dominionCooldown > 0)
            {
                dominionCooldown--;
            }
        }

        public bool TryStartRed(NPC npc, Player target, tsorcRevampGlobalNPC globalNPC)
        {
            if (!CanStart(npc, target, globalNPC) || attackCooldown > 0)
            {
                return false;
            }

            bool hasLineOfSight = Collision.CanHitLine(npc.Center, 1, 1, target.Center, 1, 1);
            float distance = npc.Distance(target.Center);
            KnightSpecialAttack[] candidates = new KnightSpecialAttack[3];
            int count = 0;

            if (distance <= 210f)
            {
                candidates[count++] = KnightSpecialAttack.EmberReversal;
            }
            if (hasLineOfSight && distance >= 150f && distance <= 620f)
            {
                candidates[count++] = KnightSpecialAttack.VenomWake;
            }
            if (distance >= 120f && TryFindGround(target.Bottom, 10, 28, out Vector2 standardGround))
            {
                candidates[count++] = KnightSpecialAttack.CrimsonStandard;
                LockedTarget = standardGround;
            }

            if (count == 0)
            {
                attackCooldown = 30;
                return false;
            }

            KnightSpecialAttack selected = candidates[Main.rand.Next(count)];
            if (selected == KnightSpecialAttack.CrimsonStandard)
            {
                if (!TryFindGround(target.Bottom, 10, 28, out Vector2 lockedGround))
                {
                    attackCooldown = 30;
                    return false;
                }
                LockedTarget = lockedGround;
            }

            Begin(npc, target, globalNPC, selected);
            return true;
        }

        public bool TryStartGreat(NPC npc, Player target, tsorcRevampGlobalNPC globalNPC)
        {
            if (!CanStart(npc, target, globalNPC))
            {
                return false;
            }

            if (!HalfHeraldComplete && npc.life <= npc.lifeMax / 2)
            {
                Begin(npc, target, globalNPC, KnightSpecialAttack.FurnaceHerald);
                return true;
            }
            if (HalfHeraldComplete && !ThirdHeraldComplete && npc.life <= npc.lifeMax / 3)
            {
                Begin(npc, target, globalNPC, KnightSpecialAttack.StormHerald);
                return true;
            }
            if (attackCooldown > 0)
            {
                return false;
            }

            float distance = npc.Distance(target.Center);
            bool hasLineOfSight = Collision.CanHitLine(npc.Center, 1, 1, target.Center, 1, 1);
            KnightSpecialAttack[] candidates = new KnightSpecialAttack[5];
            int count = 0;

            if (distance <= 330f)
            {
                candidates[count++] = KnightSpecialAttack.CrimsonAdvance;
            }

            bool hasRoyalCenter = TryFindGround(target.Bottom, 10, 30, out Vector2 royalCenter);
            bool hasRoyalLeft = TryFindGround(target.Bottom + new Vector2(-180f, 0f), 10, 30, out Vector2 royalLeft);
            bool hasRoyalRight = TryFindGround(target.Bottom + new Vector2(180f, 0f), 10, 30, out Vector2 royalRight);
            bool hasRoyalGround = hasRoyalCenter && hasRoyalLeft && hasRoyalRight;
            if (hasRoyalGround)
            {
                candidates[count++] = KnightSpecialAttack.RoyalStandard;
            }

            int targetDirection = target.Center.X >= npc.Center.X ? 1 : -1;
            Vector2 behindPoint = target.Bottom + new Vector2(targetDirection * 165f, 0f);
            if (HalfHeraldComplete && hasLineOfSight && distance >= 150f && distance <= 720f
                && TryFindGround(behindPoint, 10, 30, out Vector2 furnaceGround))
            {
                candidates[count++] = KnightSpecialAttack.FurnacePincer;
                AuxiliaryTargetA = furnaceGround;
            }
            if (ThirdHeraldComplete && hasLineOfSight && distance >= 110f && distance <= 560f)
            {
                candidates[count++] = KnightSpecialAttack.StormbreakerEdict;
            }
            if (HalfHeraldComplete && dominionCooldown <= 0 && npc.velocity.Y == 0f && distance <= 360f)
            {
                candidates[count++] = KnightSpecialAttack.CrimsonDominion;
            }

            if (count == 0)
            {
                attackCooldown = 30;
                return false;
            }

            KnightSpecialAttack selected = candidates[Main.rand.Next(count)];
            if (selected == KnightSpecialAttack.RoyalStandard)
            {
                LockedTarget = royalCenter;
                AuxiliaryTargetA = royalLeft;
                AuxiliaryTargetB = royalRight;
            }
            else if (selected == KnightSpecialAttack.FurnacePincer)
            {
                if (!TryFindGround(behindPoint, 10, 30, out Vector2 confirmedFurnaceGround))
                {
                    attackCooldown = 30;
                    return false;
                }
                AuxiliaryTargetA = confirmedFurnaceGround;
            }

            Begin(npc, target, globalNPC, selected);
            return true;
        }

        static bool CanStart(NPC npc, Player target, tsorcRevampGlobalNPC globalNPC)
        {
            return Main.netMode != NetmodeID.MultiplayerClient
                && target.active && !target.dead
                && npc.velocity.Y == 0f
                && npc.ai[1] >= 60f && npc.ai[1] < 120f
                && !globalNPC.CombatMeleeActive
                && !globalNPC.HasPendingCombatComboMove
                && !globalNPC.InCombatComboRecovery
                && !globalNPC.AttackTelegraphing
                && !globalNPC.AttackCommitted
                && globalNPC.StaggerTimer <= 0
                && globalNPC.TeleportCountdown <= 0
                && globalNPC.TeleportAppearanceTimer <= 0
                && globalNPC.DodgeTimer <= 0
                && globalNPC.PounceTimer <= 0
                && !globalNPC.Fleeing;
        }

        void Begin(NPC npc, Player target, tsorcRevampGlobalNPC globalNPC, KnightSpecialAttack attack)
        {
            Attack = attack;
            Timer = 0;
            Direction = target.Center.X >= npc.Center.X ? 1 : -1;
            LockedTarget = attack == KnightSpecialAttack.CrimsonStandard || attack == KnightSpecialAttack.RoyalStandard
                ? LockedTarget
                : target.Center;
            AuxiliaryTargetB = attack == KnightSpecialAttack.RoyalStandard ? AuxiliaryTargetB : Vector2.Zero;
            LockedVelocity = Vector2.Zero;
            ArenaBaseRotation = Main.rand.NextFloat(MathHelper.Pi / 12f);
            ArenaRotationDirection = Main.rand.NextBool() ? 1 : -1;
            npc.direction = Direction;
            npc.spriteDirection = Direction;
            npc.ai[1] = 60f;
            npc.ai[2] = -100f;
            if (attack != KnightSpecialAttack.CrimsonStandard && attack != KnightSpecialAttack.RoyalStandard)
            {
                npc.velocity.X *= 0.25f;
            }
            globalNPC.ResetCombatTempoSequence(clearRecovery: true);
            globalNPC.AttackTelegraphing = true;
            globalNPC.AttackCommitted = false;
            tsorcRevampAIs.SpawnTelegraphFlash(npc, TelegraphColor(attack));
            npc.netUpdate = true;
        }

        static Color TelegraphColor(KnightSpecialAttack attack)
        {
            return attack switch
            {
                KnightSpecialAttack.VenomWake => Color.GreenYellow,
                KnightSpecialAttack.StormbreakerEdict => Color.Cyan,
                KnightSpecialAttack.StormHerald => new Color(120, 220, 255),
                _ => Color.OrangeRed
            };
        }

        public bool Tick(NPC npc, Player target, KnightAttackStats stats)
        {
            if (!Active)
            {
                return false;
            }

            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (!target.active || target.dead || globalNPC.StaggerTimer > 0)
            {
                Cancel(npc, globalNPC);
                return true;
            }

            npc.direction = Direction;
            npc.spriteDirection = Direction;
            npc.knockBackResist = 0f;
            npc.velocity.Y = Math.Min(npc.velocity.Y + 0.35f, 10f);
            SetCombatFlags(globalNPC);

            switch (Attack)
            {
                case KnightSpecialAttack.EmberReversal:
                    TickEmberReversal(npc, target, stats);
                    break;
                case KnightSpecialAttack.VenomWake:
                    TickVenomWake(npc, target, stats);
                    break;
                case KnightSpecialAttack.CrimsonStandard:
                    TickCrimsonStandard(npc, stats);
                    break;
                case KnightSpecialAttack.CrimsonAdvance:
                    TickCrimsonAdvance(npc, target, stats);
                    break;
                case KnightSpecialAttack.FurnacePincer:
                    TickFurnacePincer(npc, target, stats);
                    break;
                case KnightSpecialAttack.RoyalStandard:
                    TickRoyalStandard(npc, stats);
                    break;
                case KnightSpecialAttack.StormbreakerEdict:
                    TickStormbreaker(npc, target, stats);
                    break;
                case KnightSpecialAttack.CrimsonDominion:
                    TickCrimsonDominion(npc, stats);
                    break;
                case KnightSpecialAttack.FurnaceHerald:
                case KnightSpecialAttack.StormHerald:
                    TickHerald(npc, Attack == KnightSpecialAttack.StormHerald);
                    break;
            }

            // Standards own the knight only through the throw. Their planted charge and delayed
            // projectiles are self-contained, so the knight can immediately resume normal movement
            // and attack selection while the standard resolves independently.
            if (!Active)
            {
                return true;
            }

            Timer++;
            if (Main.netMode != NetmodeID.MultiplayerClient && Timer % 60 == 0)
            {
                npc.netUpdate = true;
            }

            int duration = Duration(Attack);
            if (Timer >= duration)
            {
                Finish(npc, globalNPC);
            }
            return true;
        }

        void TickEmberReversal(NPC npc, Player target, KnightAttackStats stats)
        {
            if (Timer < 45)
            {
                npc.velocity.X *= 0.72f;
            }
            if (Timer == 45)
            {
                SpawnLunge(npc, stats.SpearDamage, 70f, 48f, 12, 3.5f);
                npc.velocity = new Vector2(Direction * 4.2f, -2.2f);
                PlaySound(SoundID.Item1 with { Volume = 0.8f }, npc.Center);
            }
            if (Timer == 65)
            {
                npc.velocity = new Vector2(-Direction * 4.1f, -4.8f);
                LockedTarget = target.Center;
                npc.netUpdate = true;
            }
            if (Timer > 78 && Timer < 120)
            {
                npc.velocity.X *= 0.94f;
            }
            if (Timer == 120 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 velocity = UsefulFunctions.BallisticTrajectory(npc.Center, LockedTarget, 9f, fallback: true);
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, velocity,
                    ModContent.ProjectileType<EnemyFirebomb>(), stats.SpearDamage, 0f, Main.myPlayer, ai2: 1f);
                PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = -0.35f }, npc.Center);
            }
        }

        void TickVenomWake(NPC npc, Player target, KnightAttackStats stats)
        {
            npc.velocity.X *= 0.8f;
            if (Timer == 30)
            {
                LockedTarget = target.Center;
                Direction = LockedTarget.X >= npc.Center.X ? 1 : -1;
                npc.netUpdate = true;
            }
            if (Timer == 60)
            {
                LockedVelocity = UsefulFunctions.BallisticTrajectory(npc.Center, LockedTarget, 13f, fallback: true);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, LockedVelocity,
                        ModContent.ProjectileType<BlackKnightSpear>(), stats.SpearDamage, 0f, Main.myPlayer, ai2: 1f);
                }
                PlaySound(SoundID.Item1 with { Volume = 0.8f }, npc.Center);
                npc.netUpdate = true;
            }
            if (Timer == 120 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 baseVelocity = LockedVelocity.SafeNormalize(new Vector2(Direction, 0f)) * 7f;
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 velocity = baseVelocity.RotatedBy(side * 0.22f);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, velocity,
                        ModContent.ProjectileType<EnemySpellAbyssPoisonStrikeBall>(), stats.MagicDamage,
                        0f, Main.myPlayer, ai2: 1f);
                }
                PlaySound(SoundID.Item20 with { Volume = 0.65f, Pitch = 0.15f }, npc.Center);
            }
        }

        void TickCrimsonStandard(NPC npc, KnightAttackStats stats)
        {
            if (Timer == 45)
            {
                float horizontalVelocity = Direction * npc.velocity.X >= 0.8f
                    ? npc.velocity.X
                    : Direction * 0.8f;
                npc.velocity = new Vector2(horizontalVelocity, -5.2f);
                npc.netUpdate = true;
            }
            if (Timer == 75)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    SpawnStandard(npc, LockedTarget, stats.MagicDamage, KnightStandardMode.RedKnight);
                }
                PlaySound(SoundID.Item1 with { Volume = 0.85f, Pitch = -0.1f }, npc.Center);
                ReleaseDetachedStandard(npc);
            }
        }

        void TickCrimsonAdvance(NPC npc, Player target, KnightAttackStats stats)
        {
            if (Timer < 60)
            {
                npc.velocity.X *= 0.7f;
            }
            if (Timer == 40)
            {
                LockedTarget = target.Center;
                Direction = LockedTarget.X >= npc.Center.X ? 1 : -1;
                npc.netUpdate = true;
            }
            if (Timer == 60)
            {
                Vector2 firstTarget = Vector2.Lerp(npc.Center, LockedTarget, 0.62f);
                LockedVelocity = GuidedLeapVelocity(npc.Center, firstTarget, 18);
                SpawnLunge(npc, stats.SpearDamage, 108f, 52f, 20, 5f);
                npc.velocity = LockedVelocity;
                PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = -0.15f }, npc.Center);
                npc.netUpdate = true;
            }
            if (Timer > 80 && Timer < 105)
            {
                npc.velocity.X *= 0.72f;
            }
            if (Timer == 105)
            {
                Direction = target.Center.X >= npc.Center.X ? 1 : -1;
                LockedTarget = target.Center;
                npc.netUpdate = true;
            }
            if (Timer == 135)
            {
                LockedVelocity = GuidedLeapVelocity(npc.Center, LockedTarget, 20);
                SpawnLunge(npc, stats.GreatDamage, 96f, 54f, 22, 5.5f);
                npc.velocity = LockedVelocity;
                PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = 0.05f }, npc.Center);
                npc.netUpdate = true;
            }
            if (Timer > 157)
            {
                npc.velocity.X *= 0.78f;
            }
        }

        void TickFurnacePincer(NPC npc, Player target, KnightAttackStats stats)
        {
            if (Timer < 60)
            {
                npc.velocity.X *= 0.76f;
            }
            if (Timer == 60 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero,
                    ModContent.ProjectileType<RedKnightDelayedBomb>(), stats.GreatDamage, 0f, Main.myPlayer,
                    AuxiliaryTargetA.X, AuxiliaryTargetA.Y, 1f);
                PlaySound(UsefulFunctions.BombFuse with { Volume = 0.75f }, npc.Center);
            }
            if (Timer == 125)
            {
                LockedTarget = target.Center;
                Direction = LockedTarget.X >= npc.Center.X ? 1 : -1;
                npc.netUpdate = true;
            }
            if (Timer == 165)
            {
                LockedVelocity = GuidedLeapVelocity(npc.Center, LockedTarget, 36);
                SpawnLunge(npc, stats.GreatDamage, 112f, 54f, 38, 6f);
                npc.velocity = LockedVelocity;
                PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = -0.25f }, npc.Center);
                npc.netUpdate = true;
            }
            if (Timer > 203)
            {
                npc.velocity.X *= 0.75f;
            }
        }

        void TickRoyalStandard(NPC npc, KnightAttackStats stats)
        {
            if (Timer == 45)
            {
                float horizontalVelocity = Direction * npc.velocity.X >= 0.6f
                    ? npc.velocity.X
                    : Direction * 0.6f;
                npc.velocity = new Vector2(horizontalVelocity, -6f);
                npc.netUpdate = true;
            }
            if (Timer == 75)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    SpawnStandard(npc, LockedTarget, stats.GreatDamage, KnightStandardMode.GreatCenter);
                    SpawnStandard(npc, AuxiliaryTargetA, stats.MagicDamage, KnightStandardMode.GreatLeft);
                    SpawnStandard(npc, AuxiliaryTargetB, stats.MagicDamage, KnightStandardMode.GreatRight);
                }
                PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = -0.2f }, npc.Center);
                ReleaseDetachedStandard(npc);
            }
        }

        void ReleaseDetachedStandard(NPC npc)
        {
            KnightSpecialAttack completed = Attack;
            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
            Attack = KnightSpecialAttack.None;
            Timer = 0;
            LockedVelocity = Vector2.Zero;
            globalNPC.AttackTelegraphing = false;
            globalNPC.AttackCommitted = false;
            globalNPC.SetActiveAttackDefenseTraits(npc, AttackDefenseTraits.None);
            npc.ai[1] = 60f;
            npc.ai[2] = -100f;
            attackCooldown = IsGreatAttack(completed) ? Main.rand.Next(260, 401) : Main.rand.Next(320, 481);
            npc.netUpdate = true;
        }

        void TickStormbreaker(NPC npc, Player target, KnightAttackStats stats)
        {
            if (Timer < 60)
            {
                npc.velocity.X *= 0.7f;
                if (Timer == 30)
                {
                    LockedTarget = target.Center;
                    Direction = LockedTarget.X >= npc.Center.X ? 1 : -1;
                    npc.netUpdate = true;
                }
            }
            if (Timer == 60)
            {
                SpawnLunge(npc, stats.GreatDamage, 120f, 58f, 16, 6.5f);
                npc.velocity = new Vector2(Direction * 8f, -2.2f);
                PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = -0.35f }, npc.Center);
            }
            if (Timer == 82 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = -2; i <= 2; i++)
                {
                    Vector2 velocity = new Vector2(Direction, 0f).RotatedBy(i * 0.18f);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, velocity,
                        ModContent.ProjectileType<RedKnightLightningLane>(), stats.MagicDamage,
                        0f, Main.myPlayer, ai0: 40f, ai1: 10f, ai2: 520f);
                }
                npc.velocity.X *= 0.35f;
                npc.netUpdate = true;
            }
            if (Timer > 82)
            {
                npc.velocity.X *= 0.8f;
            }
        }

        void TickCrimsonDominion(NPC npc, KnightAttackStats stats)
        {
            npc.velocity.X = 0f;
            if (Timer == 30)
            {
                npc.velocity.Y = -4.5f;
                npc.netUpdate = true;
            }
            if (Timer == 52)
            {
                npc.velocity.Y = Math.Max(npc.velocity.Y, 4.5f);
            }
            if (Timer == 60 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero,
                    ModContent.ProjectileType<CrimsonDominionController>(), stats.GreatDamage,
                    0f, Main.myPlayer, ArenaRotationDirection, ArenaBaseRotation, npc.whoAmI);
                PlaySound(SoundID.Item74 with { Volume = 0.7f, Pitch = -0.65f }, npc.Center);
            }
            if (Timer >= 60)
            {
                npc.velocity.X = 0f;
            }
        }

        void TickHerald(NPC npc, bool storm)
        {
            npc.velocity.X *= 0.82f;
            Lighting.AddLight(npc.Center,
                (storm ? new Color(90, 185, 255) : new Color(255, 55, 20)).ToVector3()
                * (0.45f + TelegraphProgress * 0.55f));

            if (Timer == 0)
            {
                PlaySound(SoundID.Item74 with { Volume = 0.72f, Pitch = storm ? 0.25f : -0.55f }, npc.Center);
            }
            else if (Timer == 75)
            {
                PlaySound(storm
                    ? SoundID.Item122 with { Volume = 0.72f, Pitch = -0.15f }
                    : SoundID.Item20 with { Volume = 0.78f, Pitch = -0.35f }, npc.Center);
                tsorcRevampAIs.SpawnTelegraphFlash(npc,
                    storm ? new Color(115, 220, 255) : new Color(255, 75, 25));
            }
            else if (Timer == 135)
            {
                PlaySound(SoundID.Item1 with { Volume = 0.95f, Pitch = -0.45f }, npc.Center);
            }
        }

        void SetCombatFlags(tsorcRevampGlobalNPC globalNPC)
        {
            bool committed = Attack switch
            {
                KnightSpecialAttack.EmberReversal => Timer >= 45 && Timer < 57,
                KnightSpecialAttack.VenomWake => (Timer >= 60 && Timer < 67) || (Timer >= 120 && Timer < 126),
                KnightSpecialAttack.CrimsonStandard => Timer >= 69 && Timer < 81,
                KnightSpecialAttack.CrimsonAdvance => (Timer >= 60 && Timer < 80) || (Timer >= 135 && Timer < 157),
                KnightSpecialAttack.FurnacePincer => Timer >= 165 && Timer < 203,
                KnightSpecialAttack.RoyalStandard => Timer >= 69 && Timer < 83,
                KnightSpecialAttack.StormbreakerEdict => Timer >= 60 && Timer < 76,
                KnightSpecialAttack.CrimsonDominion => Timer >= 60 && Timer < 505,
                _ => false
            };
            bool dominionRecovery = Attack == KnightSpecialAttack.CrimsonDominion && Timer >= 505;
            globalNPC.AttackCommitted = committed;
            globalNPC.AttackTelegraphing = !committed && !dominionRecovery && !IsHerald && Timer < Duration(Attack) - 20;
        }

        void Finish(NPC npc, tsorcRevampGlobalNPC globalNPC)
        {
            KnightSpecialAttack completed = Attack;
            if (completed == KnightSpecialAttack.FurnaceHerald)
            {
                HalfHeraldComplete = true;
            }
            else if (completed == KnightSpecialAttack.StormHerald)
            {
                ThirdHeraldComplete = true;
            }

            Attack = KnightSpecialAttack.None;
            Timer = 0;
            LockedVelocity = Vector2.Zero;
            globalNPC.AttackTelegraphing = false;
            globalNPC.AttackCommitted = false;
            globalNPC.SetActiveAttackDefenseTraits(npc, AttackDefenseTraits.None);
            npc.ai[1] = 60f;
            npc.ai[2] = -100f;
            npc.velocity.X *= 0.35f;
            attackCooldown = IsGreatAttack(completed) ? Main.rand.Next(260, 401) : Main.rand.Next(320, 481);
            if (completed == KnightSpecialAttack.CrimsonDominion)
            {
                dominionCooldown = Main.rand.Next(1200, 1501);
            }
            npc.netUpdate = true;
        }

        void Cancel(NPC npc, tsorcRevampGlobalNPC globalNPC)
        {
            Attack = KnightSpecialAttack.None;
            Timer = 0;
            LockedVelocity = Vector2.Zero;
            globalNPC.AttackTelegraphing = false;
            globalNPC.AttackCommitted = false;
            globalNPC.SetActiveAttackDefenseTraits(npc, AttackDefenseTraits.None);
            attackCooldown = 120;
            npc.ai[1] = 60f;
            npc.ai[2] = -100f;
            npc.netUpdate = true;
        }

        static bool IsGreatAttack(KnightSpecialAttack attack)
        {
            return attack >= KnightSpecialAttack.CrimsonAdvance;
        }

        static int Duration(KnightSpecialAttack attack)
        {
            return attack switch
            {
                KnightSpecialAttack.EmberReversal => 155,
                KnightSpecialAttack.VenomWake => 170,
                KnightSpecialAttack.CrimsonStandard => 210,
                KnightSpecialAttack.CrimsonAdvance => 195,
                KnightSpecialAttack.FurnacePincer => 230,
                KnightSpecialAttack.RoyalStandard => 330,
                KnightSpecialAttack.StormbreakerEdict => 200,
                KnightSpecialAttack.CrimsonDominion => 600,
                KnightSpecialAttack.FurnaceHerald => 150,
                KnightSpecialAttack.StormHerald => 150,
                _ => 1
            };
        }

        public KnightHeldProp HeldProp => Attack switch
        {
            KnightSpecialAttack.EmberReversal when Timer < 65 => KnightHeldProp.Spear,
            KnightSpecialAttack.EmberReversal when Timer < 120 => KnightHeldProp.Bomb,
            KnightSpecialAttack.VenomWake when Timer < 60 => KnightHeldProp.Spear,
            KnightSpecialAttack.VenomWake when Timer < 120 => KnightHeldProp.Magic,
            KnightSpecialAttack.CrimsonStandard when Timer < 75 => KnightHeldProp.Spear,
            KnightSpecialAttack.CrimsonAdvance => KnightHeldProp.Spear,
            KnightSpecialAttack.FurnacePincer when Timer < 60 => KnightHeldProp.Bomb,
            KnightSpecialAttack.FurnacePincer when Timer >= 105 && Timer < 205 => KnightHeldProp.Spear,
            KnightSpecialAttack.RoyalStandard when Timer < 75 => KnightHeldProp.Spear,
            KnightSpecialAttack.StormbreakerEdict when Timer < 122 => KnightHeldProp.Spear,
            KnightSpecialAttack.CrimsonDominion => KnightHeldProp.Spear,
            _ => KnightHeldProp.None
        };

        public float GetSpearRotation(Vector2 handWorld)
        {
            Vector2 direction = new Vector2(Direction, 0f);
            if ((Attack == KnightSpecialAttack.CrimsonStandard || Attack == KnightSpecialAttack.RoyalStandard)
                    && Timer >= 45)
            {
                direction = LockedTarget - handWorld;
            }
            else if (Attack == KnightSpecialAttack.CrimsonDominion)
            {
                direction = Vector2.UnitY;
            }
            else if (Attack == KnightSpecialAttack.VenomWake && LockedTarget != Vector2.Zero && Timer >= 20)
            {
                direction = LockedVelocity.LengthSquared() > 0.1f
                    ? LockedVelocity
                    : LockedTarget - handWorld;
            }
            return direction.SafeNormalize(new Vector2(Direction, 0f)).ToRotation() + MathHelper.PiOver2;
        }

        public float SpearGripSlide
        {
            get
            {
                if (Attack == KnightSpecialAttack.CrimsonAdvance)
                {
                    return PulseWindow(Timer, 60, 80, 20f) + PulseWindow(Timer, 135, 157, 18f);
                }
                if (Attack == KnightSpecialAttack.FurnacePincer)
                {
                    return PulseWindow(Timer, 165, 203, 24f);
                }
                if (Attack == KnightSpecialAttack.StormbreakerEdict)
                {
                    return PulseWindow(Timer, 60, 76, 24f);
                }
                if (Attack == KnightSpecialAttack.EmberReversal)
                {
                    return PulseWindow(Timer, 45, 57, 14f);
                }
                if (Attack == KnightSpecialAttack.CrimsonDominion)
                {
                    if (Timer < 60)
                    {
                        return MathHelper.Lerp(0f, 20f, MathHelper.Clamp(Timer / 60f, 0f, 1f));
                    }
                    if (Timer >= 550)
                    {
                        return MathHelper.Lerp(20f, 0f, MathHelper.Clamp((Timer - 550f) / 50f, 0f, 1f));
                    }
                    return 20f;
                }
                return 0f;
            }
        }

        public bool SpearDamageWake => Attack switch
        {
            KnightSpecialAttack.EmberReversal => Timer >= 45 && Timer < 57,
            KnightSpecialAttack.CrimsonAdvance => (Timer >= 60 && Timer < 80) || (Timer >= 135 && Timer < 157),
            KnightSpecialAttack.FurnacePincer => Timer >= 165 && Timer < 203,
            KnightSpecialAttack.StormbreakerEdict => Timer >= 60 && Timer < 76,
            _ => false
        };

        public float TelegraphProgress
        {
            get
            {
                int telegraph = Attack switch
                {
                    KnightSpecialAttack.EmberReversal => 45,
                    KnightSpecialAttack.VenomWake => Timer < 60 ? 60 : 120,
                    KnightSpecialAttack.CrimsonStandard => 75,
                    KnightSpecialAttack.CrimsonAdvance => Timer < 60 ? 60 : 135,
                    KnightSpecialAttack.FurnacePincer => Timer < 60 ? 60 : 165,
                    KnightSpecialAttack.RoyalStandard => 75,
                    KnightSpecialAttack.StormbreakerEdict => 60,
                    KnightSpecialAttack.CrimsonDominion => 90,
                    KnightSpecialAttack.FurnaceHerald or KnightSpecialAttack.StormHerald => 150,
                    _ => 1
                };
                int start = Attack switch
                {
                    KnightSpecialAttack.VenomWake when Timer >= 60 => 60,
                    KnightSpecialAttack.CrimsonAdvance when Timer >= 60 => 105,
                    KnightSpecialAttack.FurnacePincer when Timer >= 60 => 105,
                    _ => 0
                };
                return MathHelper.Clamp((Timer - start) / (float)Math.Max(1, telegraph - start), 0f, 1f);
            }
        }

        static float PulseWindow(int timer, int start, int end, float maximum)
        {
            if (timer < start || timer >= end)
            {
                return 0f;
            }
            float progress = (timer - start) / (float)Math.Max(1, end - start - 1);
            return (float)Math.Sin(progress * MathHelper.Pi) * maximum;
        }

        static Vector2 GuidedLeapVelocity(Vector2 source, Vector2 target, int flightTicks)
        {
            const float gravityPerTick = 0.35f;
            float ticks = Math.Max(1, flightTicks);
            Vector2 delta = target - source;
            float gravityDrop = gravityPerTick * (ticks - 1f) * ticks * 0.5f;
            return new Vector2(delta.X / ticks,
                MathHelper.Clamp((delta.Y - gravityDrop) / ticks, -14f, 8f));
        }

        static void SpawnLunge(NPC npc, int damage, float reach, float height, int duration, float knockback)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, new Vector2(npc.direction, 0f),
                ModContent.ProjectileType<RedKnightLungeHitbox>(), damage, knockback, Main.myPlayer,
                reach, height, duration);
        }

        static void SpawnStandard(NPC npc, Vector2 target, int damage, KnightStandardMode mode)
        {
            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero,
                ModContent.ProjectileType<RedKnightStandard>(), damage, 0f, Main.myPlayer,
                target.X, target.Y, (float)mode);
        }

        static void PlaySound(SoundStyle sound, Vector2 position)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(sound, position);
            }
        }

        internal static bool TryFindGround(Vector2 around, int searchUpTiles, int searchDownTiles, out Vector2 surface)
        {
            int tileX = Utils.Clamp((int)(around.X / 16f), 2, Main.maxTilesX - 3);
            int originY = Utils.Clamp((int)(around.Y / 16f), 5, Main.maxTilesY - 10);
            int startY = Utils.Clamp(originY - searchUpTiles, 5, Main.maxTilesY - 10);
            int endY = Utils.Clamp(originY + searchDownTiles, 5, Main.maxTilesY - 5);
            for (int tileY = startY; tileY <= endY; tileY++)
            {
                Tile tile = Framing.GetTileSafely(tileX, tileY);
                bool standable = tile.HasTile && !tile.IsActuated
                    && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);
                Tile above = Framing.GetTileSafely(tileX, tileY - 1);
                bool blockedAbove = above.HasTile && !above.IsActuated
                    && Main.tileSolid[above.TileType] && !Main.tileSolidTop[above.TileType];
                if (standable && !blockedAbove)
                {
                    surface = new Vector2(tileX * 16f + 8f, tileY * 16f - 2f);
                    return true;
                }
            }
            surface = Vector2.Zero;
            return false;
        }

        public void Send(BinaryWriter writer)
        {
            writer.Write((byte)Attack);
            writer.Write(Timer);
            writer.Write((sbyte)Direction);
            WriteVector(writer, LockedTarget);
            WriteVector(writer, AuxiliaryTargetA);
            WriteVector(writer, AuxiliaryTargetB);
            WriteVector(writer, LockedVelocity);
            writer.Write(ArenaBaseRotation);
            writer.Write((sbyte)ArenaRotationDirection);
            writer.Write(HalfHeraldComplete);
            writer.Write(ThirdHeraldComplete);
            writer.Write(attackCooldown);
            writer.Write(dominionCooldown);
        }

        public void Receive(BinaryReader reader)
        {
            Attack = (KnightSpecialAttack)reader.ReadByte();
            Timer = reader.ReadInt32();
            Direction = reader.ReadSByte();
            LockedTarget = ReadVector(reader);
            AuxiliaryTargetA = ReadVector(reader);
            AuxiliaryTargetB = ReadVector(reader);
            LockedVelocity = ReadVector(reader);
            ArenaBaseRotation = reader.ReadSingle();
            ArenaRotationDirection = reader.ReadSByte();
            HalfHeraldComplete = reader.ReadBoolean();
            ThirdHeraldComplete = reader.ReadBoolean();
            attackCooldown = reader.ReadInt32();
            dominionCooldown = reader.ReadInt32();
        }

        static void WriteVector(BinaryWriter writer, Vector2 value)
        {
            writer.Write(value.X);
            writer.Write(value.Y);
        }

        static Vector2 ReadVector(BinaryReader reader)
        {
            return new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }
    }
}
