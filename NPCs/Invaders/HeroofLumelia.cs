using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.Ammo;
using tsorcRevamp.Content.Items.Armor.Ranged;
using tsorcRevamp.Content.Items.BossBags;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Items.Tools;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.VFX;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Invaders
{
    // ── Hero of Lumelia — Puppet combat rework ─────────────────────────────────────────
    // The avenging human hero / rogue-ranger duelist, a mirror of the player.  Rebuilt on the
    // PuppetNPC puppet system so he wears the Archer of Lumelia armor set and swings the
    // AncientFireSword through the shared melee-combo framework.
    //
    // Class name / namespace deliberately unchanged so every external reference keeps working:
    // the scripted-event fight (HeroofLumeliaFight), PoiseProfiles, loot, NewSlain gating,
    // BossChecklist, gores, localization keys.
    //
    // KIT
    //   Melee    — AncientFireSword Broadsword combos; heavy swings throw a HeroFireArc crescent.
    //   A1       — Knife volleys (base CursedKnives): 1–3 volleys in a row, each a varied fan.
    //   A2       — Vanishing Strike (TickVanish): grey-smoke blink behind the player → reappear
    //              flash → point-blank knife burst.  The signature identity move.
    //   A3       — Archer's Volley (TickHorn): raise a war-cry; his four artillery ManHunters (spawned with the fight,
    //              ~100 tiles out either side) answer with a marching line of lobbed flaming arrows.
    //   A4       — Piercing Dash (base CanPierce, NO impale): fiery committed lunge across the arena.
    //   A5       — Rally (TickRally): at 50% / 25% HP, a war-cry — widen his spacing and lob potion bombs. At 25% a
    //              Red Cloud Hunter joins him.
    //   Archers  — TickArchers: spawns/respawns (20s) the four artillery archers; at 60% and 35% HP one pair each
    //              stops lobbing and walks in to join the melee. The archers run their own AI (ManHunter).
    //   Bomb     — TickBomb: a lobbed HeroPotionBomb that inflicts Potion Sickness (denies healing).
    [AutoloadBossHead]
    class HeroofLumelia : PuppetNPC
    {
        public override string BossHeadTexture => UsefulFunctions.RefactorableFilepath(typeof(HeroofLumelia)) + "_Head_Boss";

        protected override string InvaderTitle => "The Hero of Lumelia";

        // Damage values (kept from the legacy kit — hostile-projectile balance already tuned around them).
        public int throwingKnifeDamage = 34;
        public int potionBombDamage = 43;
        int fireArcDamage = 44;

        NPCDespawnHandler despawnHandler;

        // ── Loadout: the Archer of Lumelia armor set + the Ancient Fire Sword ────────────
        protected override int HeadArmorItemType => ModContent.ItemType<ArcherOfLumeliaHairStyle>();
        protected override int BodyArmorItemType => ModContent.ItemType<ArcherOfLumeliaShirt>();
        protected override int LegsArmorItemType => ModContent.ItemType<ArcherOfLumeliaPants>();

        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemyAncientFireSword>();
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Broadsword;

        // Ranged handled by the bespoke ticks + CursedKnives, not the base generic ranged branch.
        // SecondaryRangedWeaponItemType provides the held flask sprite during TickBomb.
        protected override int RangedWeaponItemType => -1;
        protected override int SecondaryRangedWeaponItemType => ModContent.ItemType<EnemySmokeBomb>();
        protected override bool SecondaryRangedAvailable => false;
        protected override int MeleeDamage => 65;
        protected override int RangedDamage => 42;

        // ── Melee tuning — agile fire-sword duelist ─────────────────────────────────────
        const string QuickslashName = "Quickslash";
        const string UnderOverName = "Under-Over";
        const string EmberLungeName = "Ember Lunge";
        const string ThreeHitStandardName = "3-Hit Standard";
        const string FiveHitFinisherName = "5-Hit Finisher";

        const float ArcHighRad = -1.85f;
        const float ArcLowRad = 1.90f;
        const float EmberLungeMinRange = 140f;
        const float EmberLungeMaxRange = 280f;

        protected override float TopSpeed => 3f;
        protected override float Acceleration => 0.11f;
        protected override float MeleeRange => 72f;
        protected override float StabRange => 140f;
        protected override float ComboMaxStartRange => EmberLungeMaxRange;
        protected override float MeleeEngageRange => 88f;
        protected override float ComboTelegraphAdvanceSpeedMult => 0.50f;
        protected override float ComboTelegraphAdvanceStopDistance => 56f;
        protected override int MeleeComboChance => 80;
        protected override int MeleeTelegraphTicks => 30;
        protected override int MinComboTelegraphTicks => 24;
        protected override float ComboTelegraphMultiplier => 1f;
        protected override bool UseSwingEasing => true;
        protected override bool UseAimAdaptiveArc => true;
        protected override bool UseCompositeArmSwing => true;
        protected override bool UseCompositeArmForAdditionalPhase =>
            Phase == AttackPhase.PierceTelegraph
            || Phase == AttackPhase.PierceDash
            || Phase == AttackPhase.PierceRecovery
            || Phase == AttackPhase.KnivesTelegraph
            || Phase == AttackPhase.KnivesThrowPause
            || Phase == AttackPhase.KnivesThrow
            || (Phase == AttackPhase.NovaRecovery
                && ((_vanishTimer > 0 && VanishTotal - _vanishTimer >= VanishTeleportTicks)
                    || _bombTimer > 0));

        protected override bool ForceHeldWeaponVisible
        {
            get
            {
                if (Phase != AttackPhase.NovaRecovery)
                {
                    return false;
                }

                if (_vanishTimer > 0)
                {
                    int elapsed = VanishTotal - _vanishTimer;
                    int releaseTick = VanishTeleportTicks + VanishPostTeleportThrowTicks;
                    return elapsed >= VanishTeleportTicks && elapsed <= releaseTick + ThrowReleaseTicks;
                }

                if (_bombTimer > 0)
                {
                    int elapsed = BombTotal - _bombTimer;
                    return elapsed <= BombWindupTicks;
                }

                return false;
            }
        }

        protected override bool MirrorHeldRangedRotationByFacing => true;

        protected override float GetHeldRangedDrawScale(int itemType)
        {
            if (itemType == SecondaryRangedWeaponItemType)
            {
                return 0.85f;
            }

            if (itemType == CursedKnivesWeaponItemType)
            {
                return 0.95f;
            }

            return base.GetHeldRangedDrawScale(itemType);
        }

        protected override float GetHeldRangedRotationOffset(int itemType)
        {
            if (itemType == CursedKnivesWeaponItemType)
            {
                return MathHelper.PiOver2;
            }

            return base.GetHeldRangedRotationOffset(itemType);
        }

        protected override void ModifyAdditionalPhaseWeaponRotation(ref float weaponRotation)
        {
            const float WindupStartRad = 0.15f;
            const float WindupCockedRad = -2.00f;
            const float ThrowFollowThroughRad = 0.55f;

            bool inThrowWindup = false;
            float windupProgress = 0f;
            bool inThrowRelease = false;
            float releaseProgress = 0f;

            if (Phase == AttackPhase.KnivesTelegraph || Phase == AttackPhase.KnivesThrowPause)
            {
                inThrowWindup = true;
                windupProgress = 1f - MathHelper.Clamp(PhaseTimer / (float)Math.Max(1, _knivesWindupTotal), 0f, 1f);
            }
            else if (Phase == AttackPhase.KnivesThrow)
            {
                inThrowRelease = true;
                releaseProgress = 1f - MathHelper.Clamp(PhaseTimer / (float)Math.Max(1, CursedKnivesThrowTicks), 0f, 1f);
            }
            else if (Phase == AttackPhase.NovaRecovery && _vanishTimer > 0)
            {
                int elapsed = VanishTotal - _vanishTimer;
                int releaseTick = VanishTeleportTicks + VanishPostTeleportThrowTicks;
                if (elapsed >= VanishTeleportTicks && elapsed < releaseTick)
                {
                    inThrowWindup = true;
                    windupProgress = MathHelper.Clamp(
                        (elapsed - VanishTeleportTicks) / (float)VanishPostTeleportThrowTicks,
                        0f,
                        1f);
                }
                else if (elapsed >= releaseTick)
                {
                    inThrowRelease = true;
                    releaseProgress = MathHelper.Clamp((elapsed - releaseTick) / (float)ThrowReleaseTicks, 0f, 1f);
                }
            }
            else if (Phase == AttackPhase.NovaRecovery && _bombTimer > 0)
            {
                int elapsed = BombTotal - _bombTimer;
                if (elapsed < BombWindupTicks)
                {
                    inThrowWindup = true;
                    windupProgress = MathHelper.Clamp(elapsed / (float)BombWindupTicks, 0f, 1f);
                }
                else
                {
                    inThrowRelease = true;
                    releaseProgress = MathHelper.Clamp((elapsed - BombWindupTicks) / (float)ThrowReleaseTicks, 0f, 1f);
                }
            }

            if (inThrowWindup)
            {
                weaponRotation = MathHelper.SmoothStep(WindupStartRad, WindupCockedRad, windupProgress);
            }
            else if (inThrowRelease)
            {
                weaponRotation = MathHelper.SmoothStep(WindupCockedRad, ThrowFollowThroughRad, releaseProgress);
            }
        }
        protected override bool UseAuthoredComboSwingClock => true;
        protected override bool AuthoredClockCoversJoustDash => true;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override int MeleeComboInterStepLingerTicks => 3;
        protected override int MeleeRecoveryLingerTicks => 6;
        protected override float PuppetJumpPower => 12f;
        protected override float PuppetJumpBoost => 6f;
        protected override Color MeleeTelegraphFlashColor => Color.OrangeRed;

        protected override bool HasFireSlashVFX => true;
        protected override Color FireSlashCinderColor => new Color(56, 10, 2);
        protected override Color FireSlashFlameColor => new Color(255, 118, 18);
        protected override Color FireSlashCoreColor => new Color(255, 238, 165);
        protected override bool HasSwordArcVFX => true;
        protected override VanillaSwordArcTexture SwordArcVFXTexture => VanillaSwordArcTexture.Excalibur;
        protected override Color SwordArcVFXColor => new Color(255, 130, 35);

        private bool EmberLungeActive =>
            ActiveMeleeComboName == EmberLungeName
            && (Phase == AttackPhase.MeleeComboTelegraph
                || Phase == AttackPhase.MeleeComboAttack
                || Phase == AttackPhase.MeleeComboPause
                || Phase == AttackPhase.MeleeComboRecovery);

        protected override bool UseLandingTimedLeapSlam => EmberLungeActive;
        protected override float LeapSlamCarryRotation => -1.70f;
        protected override float LeapSlamImpactRotation => 1.85f;
        protected override MeleeCombo[] MeleeComboPoolOverride => HeroCombos;

        private static MeleeComboStep HeroCut(ComboMotion motion, int telegraphTicks, int easeInTicks,
            int easeOutTicks, int pauseAfter, float stepInDistance, float damageMult = 1f,
            float reachMult = 1f, float decay = 7f)
        {
            // Keep HitWindowEnd synced with >= 30% peak weighted velocity so trailing settle frames never deal phantom hits.
            int attackTicks = easeInTicks + easeOutTicks;
            float hitWindowEnd = (easeInTicks + easeOutTicks * 1.204f / decay) / attackTicks;
            return new MeleeComboStep
            {
                Motion = motion,
                TelegraphTicks = telegraphTicks,
                AttackTicks = attackTicks,
                PostStepPause = pauseAfter,
                DamageMult = damageMult,
                ReachMult = reachMult,
                ForwardPushMult = 0f,
                StepInDistance = stepInDistance,
                SwingSpeedMult = 1f,
                Ease = SwingEaseStyle.Weighted,
                EaseInTicks = easeInTicks,
                EaseOutTicks = easeOutTicks,
                EaseOutDecay = decay,
                HitWindowEnd = hitWindowEnd,
            };
        }

        // Spec sheets (attack-timing-design): 215 deg envelope (-1.85..1.90 rad), ~172 deg live sweep,
        // all live windows 12-16t (< 22t roll i-frames), inter-step live gaps >= 36t (Rule 3 & Rule 5 safe).
        // Hitbox reach 72 * 0.7 = 50.4px matches visual 39.6px * 1.25 scale = 49.5px within 2%.
        private static readonly MeleeCombo[] HeroCombos = new[]
        {
            // Quickslash — 24t tell -> 7+32=39t Weighted(7.0) (peak ~31 deg/t, live 12.5t), 18t recovery (44.5t open).
            new MeleeCombo
            {
                Name = QuickslashName,
                BaseWeight = 90,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.White,
                CooldownAfterUse = 55,
                RecoveryTicks = 18,
                MoveBrake = 0.08f,
                Steps = new[]
                {
                    HeroCut(ComboMotion.OverheadArc, 24, 7, 32, 0, stepInDistance: 28f),
                },
            },
            // Under-Over — 32t low-coiled tell -> 39t Underhand -> 12t pause (46t gap) -> 42t Overhead + HeroFireArc, 26t rec.
            new MeleeCombo
            {
                Name = UnderOverName,
                BaseWeight = 85,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.LightYellow,
                CooldownAfterUse = 90,
                RecoveryTicks = 26,
                MoveBrake = 0.12f,
                Steps = new[]
                {
                    HeroCut(ComboMotion.UnderhandArc, 32, 7, 32, 12, stepInDistance: 34f, damageMult: 1.0f, reachMult: 1.05f),
                    HeroCut(ComboMotion.OverheadArc, 0, 8, 34, 0, stepInDistance: 42f, damageMult: 1.15f, reachMult: 1.12f),
                },
            },
            // Ember Lunge (RangedStartOnly 140..280px) — 30t tell -> 45.6t solved leap (up 6.84, max vx 8.42 -> ~384px reach)
            // -> 10t landing chop (-1.70..1.85) + HeroFireArc, 24t rec. Selectable outside MeleeEngageRange only.
            new MeleeCombo
            {
                Name = EmberLungeName,
                BaseWeight = 75,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Orange,
                CooldownAfterUse = 130,
                HeavyCommit = true,
                HyperArmor = true,
                RangedStartOnly = true,
                RecoveryTicks = 24,
                MoveBrake = 0.06f,
                Steps = new[]
                {
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.LeapSlam,
                        TelegraphTicks = 30,
                        AttackTicks = 78,
                        PostStepPause = 0,
                        DamageMult = 1.25f,
                        ReachMult = 1.20f,
                        ForwardPushMult = 1.65f,
                        SwingSpeedMult = 1f,
                        Ease = SwingEaseStyle.Whip,
                        LeapHeightMult = 0.72f,
                        LeapForwardSpeedMult = 1.65f,
                        LeapStrikeRange = 115f,
                    },
                },
            },
            // 3-Hit Standard — 36t tell -> Over 40t (pause 12, gap 46t) -> Under 39t (pause 14, gap 49t)
            // -> Heavy Over 45t + HeroFireArc, 32t recovery (~61t open). Gated anti-whiff in ShouldContinueMeleeCombo.
            new MeleeCombo
            {
                Name = ThreeHitStandardName,
                BaseWeight = 65,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Cyan,
                CooldownAfterUse = 140,
                RecoveryTicks = 32,
                MoveBrake = 0.14f,
                Steps = new[]
                {
                    HeroCut(ComboMotion.OverheadArc, 36, 8, 32, 12, stepInDistance: 34f, damageMult: 0.95f),
                    HeroCut(ComboMotion.UnderhandArc, 0, 7, 32, 14, stepInDistance: 38f, damageMult: 1.0f),
                    HeroCut(ComboMotion.OverheadArc, 0, 9, 36, 0, stepInDistance: 46f, damageMult: 1.15f, reachMult: 1.12f),
                },
            },
            // 5-Hit Finisher — 46t tell, HyperArmor -> 4 light weaves (38/36/36/38t, 20t dramatic beat before finisher)
            // -> 48t heavy Overhead (live 16.5t, stepIn 64px) + HeroFireArc, 44t recovery (~75t open).
            new MeleeCombo
            {
                Name = FiveHitFinisherName,
                BaseWeight = 35,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Red,
                CooldownAfterUse = 240,
                HeavyCommit = true,
                HyperArmor = true,
                RecoveryTicks = 44,
                MoveBrake = 0.18f,
                Steps = new[]
                {
                    HeroCut(ComboMotion.OverheadArc, 46, 8, 30, 12, stepInDistance: 28f, damageMult: 0.75f),
                    HeroCut(ComboMotion.UnderhandArc, 0, 7, 29, 12, stepInDistance: 28f, damageMult: 0.75f),
                    HeroCut(ComboMotion.OverheadArc, 0, 7, 29, 12, stepInDistance: 32f, damageMult: 0.80f),
                    HeroCut(ComboMotion.UnderhandArc, 0, 8, 30, 20, stepInDistance: 36f, damageMult: 0.85f),
                    HeroCut(ComboMotion.OverheadArc, 0, 10, 38, 0, stepInDistance: 64f, damageMult: 1.50f, reachMult: 1.22f),
                },
            },
        };

        protected override void ModifySwordArcVFX(MeleeComboStep step, VanillaSwordArcSettings settings)
        {
            settings.Opacity = 0.48f;
            settings.Radius = Math.Max(52f, ComboReachBase * 0.7f * step.ReachMult * 1.15f);
            settings.DustType = DustID.Torch;
        }

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            if (combo.Name == EmberLungeName)
            {
                return distance >= EmberLungeMinRange && distance <= EmberLungeMaxRange;
            }

            return true;
        }

        protected override bool ShouldContinueMeleeCombo(
            string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            if (previousStepHit)
            {
                return true;
            }

            MeleeCombo combo = Array.Find(HeroCombos, candidate => candidate.Name == comboName);
            if (combo.Steps == null || nextStepIndex >= combo.Steps.Length)
            {
                return false;
            }

            MeleeComboStep nextStep = combo.Steps[nextStepIndex];
            float reach = ComboReachBase * 0.7f * nextStep.ReachMult;
            float maxFollowDistance = reach + Math.Max(nextStep.StepInDistance, 56f);
            return NPC.Distance(target.Center) <= maxFollowDistance;
        }

        protected override void ModifyNextMeleeComboStep(
            string comboName, int nextStepIndex, Player target, ref MeleeComboStep nextStep)
        {
            if (nextStep.Ease != SwingEaseStyle.Weighted)
            {
                return;
            }

            float horizontalGap = Math.Abs(target.Center.X - NPC.Center.X);
            nextStep.StepInDistance = MathHelper.Clamp(horizontalGap - 44f, 20f, 68f);
        }

        protected override void ModifyMeleeArcEndpoints(ComboMotion motion,
            ref float startRotation, ref float endRotation)
        {
            if (Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeComboAttack
                || Phase == AttackPhase.MeleeComboPause || Phase == AttackPhase.MeleeComboRecovery)
            {
                // Keep HeroofLumelia's broadsword arcs inside a clean shoulder-friendly 215 deg envelope
                // (-1.85 overhead to +1.90 low-down) aligned with MeleeWeaponRotationOffset = PiOver4.
                if (motion == ComboMotion.OverheadArc)
                {
                    startRotation = ArcHighRad;
                    endRotation = ArcLowRad;
                }
                else if (motion == ComboMotion.UnderhandArc)
                {
                    startRotation = ArcLowRad;
                    endRotation = ArcHighRad;
                }
            }
        }

        protected override float GetLogicalWindupOppositeEnd(ComboMotion motion, float attackStart, float oppositeEnd)
        {
            // Coil slightly opposite the cut instead of sweeping the full 215 deg arc backwards during windup.
            if (motion == ComboMotion.OverheadArc)
            {
                return 0.35f;
            }

            if (motion == ComboMotion.UnderhandArc)
            {
                return -0.55f;
            }

            return base.GetLogicalWindupOppositeEnd(motion, attackStart, oppositeEnd);
        }

        // Grey-smoke blink identity (kept from the legacy TeleportVisualStyle.GreySmoke).
        protected override int TeleportTelegraphTicks => 30;
        protected override int TeleportDustCount => 22;
        protected override Color TeleportDustTint => new Color(180, 180, 180);
        protected override int CasualStrollChance => 12;

        // Grip tuning for AncientFireSword (-45 deg diagonal broadsword): +PiOver4 aligns the blade
        // directly with _weaponRotation; small arm offset keeps the wrist natural without overhead pinch.
        protected override Vector2 MeleeHandleNorm => new Vector2(0.2f, 0.8f);
        protected override float MeleeWeaponDrawScale => 1.25f;
        protected override float MeleeWeaponRotationOffset => MathHelper.PiOver4;
        protected override float MeleeCompositeArmRotationOffset => 0.15f;

        // ── A1: Knife volleys (base CursedKnives template) ──────────────────────────────
        protected override bool CanThrowCursedKnives => true;
        protected override int CursedKnivesWeaponItemType => ModContent.ItemType<EnemyCursedKnife>();
        protected override float CursedKnivesRange => 780f;
        protected override float CursedKnivesMinRange => 90f;
        protected override float CursedKnivesCloseRange => 220f;
        protected override float CursedKnivesMidRange => 470f;
        protected override int CursedKnivesChance => 5;
        protected override int CursedKnivesTelegraphTicks => BaseCursedKnivesTelegraphTicks
            + (_postTeleportKnifeTimer > 0 ? PostTeleportKnifeExtraTicks : 0);
        protected override int CursedKnivesThrowTicks => 10;
        protected override int CursedKnivesBackToBackGap => BaseCursedKnivesTelegraphTicks;
        protected override int CursedKnivesFarGap => BaseCursedKnivesTelegraphTicks;
        protected override int CursedKnivesRecoveryTicks => 38;
        protected override int CursedKnivesCooldownAfterUse => 300;
        protected override Color CursedKnivesTelegraphFlashColor => Color.White;

        // ── A4: Piercing Dash (base CanPierce), NO impale (PierceStabChance 0) ───────────
        protected override bool CanPierce => true;
        protected override int PierceStabChance => 0;
        protected override float PierceRange => 650f;
        protected override float MinPierceRange => 200f;
        protected override int PierceChance => 4;
        protected override int PierceTelegraphTicks => 50;
        protected override int PierceDashTicks => 34;
        protected override float PierceDashSpeed => 15f;
        protected override float PierceOvershootPastTarget => 160f;
        protected override int PierceDashAccelTicks => 6;
        protected override int PierceDashMaxTicks => 48;
        // Cock the blade forward-down at thigh height instead of dangling vertical (+PiOver2).
        protected override float PierceTelegraphRotation => 0.42f;
        protected override int PierceRecoveryTicks => 80;
        protected override int PierceCooldownAfterUse => 480;

        // ── Bespoke set-piece & teleport state ──────────────────────────────────────────
        const int BaseCursedKnivesTelegraphTicks = 40;
        const int PostTeleportKnifeExtraTicks = 30;
        const int PostTeleportGraceDurationTicks = 120;
        const int ThrowReleaseTicks = 10;
        const int NoLosTeleportTicks = 240; // 4 seconds without straight-line LOS

        int _attackLabelTimer;
        int _knifePatternIndex;
        int _knivesWindupTotal = BaseCursedKnivesTelegraphTicks;
        int _noLosTicks;
        int _postTeleportKnifeTimer;

        int _vanishTimer;
        int _vanishCd = 260;
        Vector2 _vanishOrigin; // departure point for departure smoke & hold during 30t vanish
        Vector2 _vanishDest; // validated arrival point behind/near the player
        const int VanishTeleportTicks = 30;
        const int VanishPostTeleportThrowTicks = 70; // 40t base knife windup + 30t post-TP extra telegraph
        const int VanishFollowThroughTicks = 16;
        const int VanishTotal = VanishTeleportTicks + VanishPostTeleportThrowTicks + VanishFollowThroughTicks;

        int _bombTimer;
        int _bombCd = 200;
        const int BombWindupTicks = 42; // 22t base + 20t extra telegraph
        const int BombFollowThroughTicks = 12;
        const int BombTotal = BombWindupTicks + BombFollowThroughTicks;

        int _hornTimer;
        int _hornCd = 360;
        // 45t war-cry pose (the read) + a short hold. The archers draw for 60t after the order, so their first arrow
        // leaves about when he lowers his arm; each arrow then takes 127t (~2.1s: climb, then fall from above) to land.
        const int HornTotal = 70;

        int _rallyTimer;
        int _rallyStage; // 0 = none fired, 1 = 50% fired, 2 = 25% fired
        const int RallyTotal = 300;

        // ── Artillery archers (server only; the archers carry their own synced state) ─────────────────────
        // Slot = side (even left, odd right) + pair: slots 0/1 hold 94 tiles out and join at 60% HP, slots 2/3 hold 106
        // out and join at 35%. A dead artillery archer is replaced after ArcherRespawnTicks; a slot whose pair has been
        // sent to join (_joinStage) is never spawned or respawned again.
        const int ArcherSlotCount = 4;
        const int ArcherRespawnTicks = 20 * 60;
        const float NearPairStandoffTiles = 94f;
        const float FarPairStandoffTiles = 106f;
        const float FirstJoinHpFraction = 0.60f;
        const float SecondJoinHpFraction = 0.35f;
        readonly int[] _archerIds = { -1, -1, -1, -1 };
        readonly int[] _archerRespawn = new int[ArcherSlotCount];
        int _joinStage; // 0 = none sent, 1 = slots 0/1 sent (60%), 2 = slots 2/3 sent (35%)

        enum HeroSetPiece { Vanish, Bomb, Horn, Rally }
        // A client plays a set-piece's start cue when its synced timer arrives within this many ticks of full length.
        const int SetPieceStartCueWindowTicks = 10;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 42;
            NPC.lifeMax = 8000;
            NPC.defense = 75;
            NPC.damage = 0; // contact 0 — damage comes from weapon hitboxes / projectiles (invader convention)
            NPC.boss = true;
            NPC.npcSlots = 5f;
            NPC.aiStyle = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 137430;
            NPC.lavaImmune = true;
            NPC.knockBackResist = 0.15f;
            NPC.rarity = 4;

            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.HeroofLumelia.DespawnHandler"), Color.Gold, DustID.GoldFlame);

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseMax = 120f; // boss tier (also reflected in PoiseProfiles)
            globalNPC.PoiseStaggerResetsAI = true;
            globalNPC.Agility = 0.35f;
            globalNPC.NavSearchRadius = 80;
            globalNPC.NavGiveUpTicks = 180;
            globalNPC.CanUseRopes = true;
            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = TeleportStyle.Aggressive;
            globalNPC.TeleportVisualStyle = TeleportVisualStyle.GreySmoke;
            // The 30t smoke windup IS the entire blink; avoid an extra hidden hold/second mist pair.
            globalNPC.TeleportAppearanceDelay = 0;
            globalNPC.TeleportArrivalMistTime = 0;

            EvasiveProfile.RedKnight(globalNPC);
        }

        protected override void RunMovementAI(float speedMult)
        {
            if (_vanishTimer > 0 && VanishTotal - _vanishTimer < VanishTeleportTicks)
            {
                NPC.velocity = Vector2.Zero;
                return;
            }

            var globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 80;
            globalNPC.RemembersLastKnownPos = true;

            SmartFighter4AI.Run(NPC,
                topSpeed: TopSpeed * speedMult,
                acceleration: Acceleration,
                doorBreakingDamage: 10,
                attackRange: MeleeRange);
        }

        protected override bool ShouldHoldAutomaticTeleport
        {
            get
            {
                if (_vanishTimer > 0 || _bombTimer > 0 || _rallyTimer > 0)
                {
                    return true;
                }

                // Rain of Arrows (TickHorn) parks in NovaRecovery: hold teleport until 4s of no LOS
                // elapses so he still finishes nearby casts cleanly, then blink near the fleeing player.
                if (_hornTimer > 0)
                {
                    return _noLosTicks < NoLosTeleportTicks;
                }

                return base.ShouldHoldAutomaticTeleport;
            }
        }

        protected override void OnTeleportCompleted()
        {
            _noLosTicks = 0;
            _hornTimer = 0;
            _postTeleportKnifeTimer = PostTeleportGraceDurationTicks;

            if (Main.netMode == NetmodeID.Server)
            {
                NPC.netUpdate = true;
            }
        }

        public override void OnStagger(NPC npc)
        {
            _vanishTimer = 0;
            _bombTimer = 0;
            _hornTimer = 0;
            _rallyTimer = 0;
            NPC.alpha = 0;
            SetDisplayWeapon(MeleeWeaponItemType, swing: false);
            base.OnStagger(npc);
        }

        public override void AI()
        {
            // Contact damage is live only during the Piercing Dash lunge (all other damage is
            // weapon hitboxes / projectiles).
            NPC.damage = Phase == AttackPhase.PierceDash ? (int)(MeleeDamage * 1.2f) : 0;

            base.AI();
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            TickArchers();

            if (Phase == AttackPhase.KnivesTelegraph || Phase == AttackPhase.KnivesThrowPause)
            {
                if (PhaseTimer > _knivesWindupTotal)
                {
                    _knivesWindupTotal = PhaseTimer;
                }

                if (!Main.dedServ && Main.rand.NextBool(3))
                {
                    Dust glintDust = Dust.NewDustPerfect(
                        PuppetHandPosition + Main.rand.NextVector2Circular(4f, 4f),
                        DustID.SilverFlame,
                        new Vector2(0f, -0.6f),
                        80,
                        Color.White,
                        0.85f);
                    glintDust.noGravity = true;
                }
            }
            else
            {
                _knivesWindupTotal = BaseCursedKnivesTelegraphTicks;
            }

            if (_attackLabelTimer > 0)
            {
                _attackLabelTimer--;
                if (_attackLabelTimer == 0)
                {
                    DebugAttackLabel = null;
                }
            }

            if (_postTeleportKnifeTimer > 0)
            {
                _postTeleportKnifeTimer--;
            }

            Player target = Main.player[NPC.target];
            if (target.dead || !target.active)
            {
                return;
            }

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            bool teleportBusy = globalNPC.TeleportCountdown > 0 || globalNPC.TeleportAppearanceTimer > 0;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (teleportBusy)
                {
                    _noLosTicks = 0;
                }
                else
                {
                    bool hasLineOfSight = Collision.CanHit(NPC.position, NPC.width, NPC.height, target.position, target.width, target.height)
                        && Collision.CanHitLine(NPC.position, NPC.width, NPC.height, target.position, target.width, target.height);

                    if (hasLineOfSight)
                    {
                        _noLosTicks = 0;
                    }
                    else
                    {
                        _noLosTicks++;

                        // 4s without straight-line LOS forces a re-acquire blink even when A* thinks
                        // a long path makes progress or Rain of Arrows is keeping him parked.
                        if (_noLosTicks >= NoLosTeleportTicks
                            && !ShouldHoldAutomaticTeleport
                            && globalNPC.CanTeleport
                            && globalNPC.TeleportChargesRemaining > 0
                            && globalNPC.TeleportCooldownTimer == 0)
                        {
                            if (tsorcRevampAIs.TryTeleportReacquire(NPC, globalNPC))
                            {
                                _noLosTicks = 0;

                                if (_hornTimer > 0)
                                {
                                    _hornTimer = 0;
                                    EnterPhase(AttackPhase.Idle, 0);
                                }

                                NPC.netUpdate = true;
                            }
                            else
                            {
                                globalNPC.TeleportCooldownTimer = 30;
                            }
                        }
                    }
                }
            }

            // Rally is HP-gated and preempts the cooldown set-pieces; the rest run while idle. The set-piece bodies run on
            // every machine off the synced timers (movement, dust, sounds); starting one rolls, teleports and spawns, so the
            // start checks inside each tick are server-only.
            TickRally(target);
            TickVanish(target);
            TickHorn(target);
            TickBomb(target);
        }

        void SetAttackLabel(string name, int ticks = 90)
        {
            DebugAttackLabel = name;
            _attackLabelTimer = ticks;
        }

        /// <summary>Label, sound and smoke for a set-piece's start. Called where it starts (server / singleplayer) and on a
        /// client when the synced timer shows it began, since the start roll only runs on the server.</summary>
        void PlaySetPieceStartCue(HeroSetPiece piece)
        {
            switch (piece)
            {
                case HeroSetPiece.Vanish:
                    SetAttackLabel("Vanishing Strike", VanishTotal);
                    SmokePuff(_vanishOrigin);
                    SoundEngine.PlaySound(SoundID.Item8 with { Pitch = -0.2f }, _vanishOrigin);
                    break;

                case HeroSetPiece.Bomb:
                    SetAttackLabel("Alchemist's Bomb", 50);
                    break;

                case HeroSetPiece.Horn:
                    SetAttackLabel("Archer's Volley", 110);
                    // War-cry / horn signal (swap for a dedicated horn sound if one is added).
                    SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.55f, Pitch = 0.2f }, NPC.Center);
                    break;

                case HeroSetPiece.Rally:
                    SetAttackLabel("Rally the Warband", 120);
                    SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.7f, Pitch = -0.3f }, NPC.Center);
                    break;
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            // Set-piece timers: the bodies run on every machine off these, so clients move, dust and cue in step.
            writer.Write((short)_vanishTimer);
            writer.Write((short)_bombTimer);
            writer.Write((short)_hornTimer);
            writer.Write((short)_rallyTimer);
            writer.Write((short)_postTeleportKnifeTimer);
            writer.Write((byte)_rallyStage);
            writer.WriteVector2(_vanishOrigin);
            writer.WriteVector2(_vanishDest);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            int vanishTimer = reader.ReadInt16();
            int bombTimer = reader.ReadInt16();
            int hornTimer = reader.ReadInt16();
            int rallyTimer = reader.ReadInt16();
            _postTeleportKnifeTimer = reader.ReadInt16();
            _rallyStage = reader.ReadByte();
            _vanishOrigin = reader.ReadVector2();
            _vanishDest = reader.ReadVector2();

            // A timer idle here that arrives near its full length started on the server this tick. The window ignores a
            // late snapshot of a set-piece this client already counted down.
            bool vanishStarted = _vanishTimer <= 0 && vanishTimer > VanishTotal - SetPieceStartCueWindowTicks;
            bool bombStarted = _bombTimer <= 0 && bombTimer > BombTotal - SetPieceStartCueWindowTicks;
            bool hornStarted = _hornTimer <= 0 && hornTimer > HornTotal - SetPieceStartCueWindowTicks;
            bool rallyStarted = _rallyTimer <= 0 && rallyTimer > RallyTotal - SetPieceStartCueWindowTicks;

            _vanishTimer = vanishTimer;
            _bombTimer = bombTimer;
            _hornTimer = hornTimer;
            _rallyTimer = rallyTimer;

            if (vanishStarted)
            {
                PlaySetPieceStartCue(HeroSetPiece.Vanish);
            }
            if (bombStarted)
            {
                PlaySetPieceStartCue(HeroSetPiece.Bomb);
            }
            if (hornStarted)
            {
                PlaySetPieceStartCue(HeroSetPiece.Horn);
            }
            if (rallyStarted)
            {
                PlaySetPieceStartCue(HeroSetPiece.Rally);
            }
        }

        private bool CanStartSetPiece =>
            (Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll) && _rallyTimer <= 0;

        // ── A2: Vanishing Strike ────────────────────────────────────────────────────────
        // 30t grey-smoke vanish telegraph (departure + validated landing mist) → blink behind
        // the player → 56t post-TP knife telegraph (26 base + 30 post-TP) → point-blank fan.
        private void TickVanish(Player target)
        {
            if (_vanishTimer > 0)
            {
                int elapsed = VanishTotal - _vanishTimer;
                _vanishTimer--;

                if (elapsed < VanishTeleportTicks)
                {
                    NPC.alpha = 255;
                    NPC.velocity = Vector2.Zero;
                    NPC.Center = _vanishOrigin;
                    return;
                }

                if (elapsed == VanishTeleportTicks)
                {
                    NPC.Center = _vanishDest;
                    NPC.velocity = Vector2.Zero;
                    NPC.alpha = 0;
                    SmokePuff(NPC.Center);
                    SoundEngine.PlaySound(SoundID.Item8 with { Pitch = -0.2f }, NPC.Center);
                    SpawnTelegraphFlash(new Color(200, 200, 200));
                }

                NPC.alpha = 0;
                NPC.velocity.X *= 0.7f;
                int face = target.Center.X < NPC.Center.X ? -1 : 1;
                NPC.direction = face;
                NPC.spriteDirection = face;

                int throwElapsed = VanishTeleportTicks + VanishPostTeleportThrowTicks;
                if (elapsed < throwElapsed)
                {
                    SetDisplayWeapon(CursedKnivesWeaponItemType, swing: false);
                    if (elapsed == VanishTeleportTicks + 1 || elapsed == throwElapsed - 30)
                    {
                        SpawnTelegraphFlash(Color.White);
                    }

                    if (!Main.dedServ && Main.rand.NextBool(3))
                    {
                        Dust sparkDust = Dust.NewDustPerfect(
                            PuppetHandPosition + Main.rand.NextVector2Circular(4f, 4f),
                            DustID.SilverFlame,
                            new Vector2(0f, -0.6f),
                            90,
                            Color.White,
                            0.9f);
                        sparkDust.noGravity = true;
                    }
                }
                else if (elapsed == throwElapsed)
                {
                    SetDisplayWeapon(CursedKnivesWeaponItemType, swing: true);
                    ThrowKnifeFan(5, 46f, 12f, target.Center);
                    SoundEngine.PlaySound(SoundID.Item17 with { Pitch = 0.2f }, NPC.Center);
                }
                else if (_vanishTimer == 0)
                {
                    SetDisplayWeapon(MeleeWeaponItemType, swing: false);
                }

                return;
            }

            // Starting rolls and teleports: server only. A client starts from the synced timer (ReceiveExtraAI).
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            if (_vanishCd > 0)
            {
                _vanishCd--;
                return;
            }

            if (!CanStartSetPiece)
            {
                return;
            }

            float dist = NPC.Distance(target.Center);
            bool inBand = dist >= 150f && dist <= 720f;
            if (!inBand || !Main.rand.NextBool(150))
            {
                return;
            }

            // Pick a solid-floored, non-embedded landing spot (prefer behind player, fallback in front).
            float[] candidateOffsetsX =
            {
                -target.direction * 96f,
                -target.direction * 72f,
                -target.direction * 128f,
                target.direction * 96f,
                target.direction * 72f,
            };

            Vector2? landingCenter = null;
            int halfWidthTiles = (int)Math.Ceiling(NPC.width * 0.5f / 16f);
            for (int offsetIndex = 0; offsetIndex < candidateOffsetsX.Length && !landingCenter.HasValue; offsetIndex++)
            {
                float candidateX = target.Center.X + candidateOffsetsX[offsetIndex];
                int centerTileX = (int)(candidateX / 16f);
                int baseFloorTileY = (int)(target.Bottom.Y / 16f);

                for (int step = 0; step <= 12; step++)
                {
                    int deltaY = (step + 1) / 2;
                    if (step % 2 == 0 && step > 0)
                    {
                        deltaY = -deltaY;
                    }

                    int floorTileY = baseFloorTileY + deltaY;
                    if (!WorldGen.InWorld(centerTileX, floorTileY, 10))
                    {
                        continue;
                    }

                    Tile floorTile = Framing.GetTileSafely(centerTileX, floorTileY);
                    bool solidFloor = WorldGen.SolidOrSlopedTile(floorTile) || Main.tileSolidTop[floorTile.TileType];
                    if (!floorTile.HasTile || !solidFloor)
                    {
                        continue;
                    }

                    Vector2 boxBottom = new Vector2(candidateX, floorTileY * 16f);
                    float boxLeft = boxBottom.X - NPC.width * 0.5f;
                    float boxTop = boxBottom.Y - NPC.height;
                    int tileLeft = (int)Math.Floor(boxLeft / 16f);
                    int tileRight = (int)Math.Floor((boxLeft + NPC.width - 0.01f) / 16f);
                    int tileTop = (int)Math.Floor(boxTop / 16f);
                    int tileBottom = (int)Math.Floor((boxBottom.Y - 0.01f) / 16f);
                    if (Collision.SolidTiles(tileLeft, tileRight, tileTop, tileBottom))
                    {
                        continue;
                    }

                    if (!NPC.lavaImmune && Collision.LavaCollision(new Vector2(boxLeft, boxTop), NPC.width, NPC.height))
                    {
                        continue;
                    }

                    if (centerTileX - halfWidthTiles >= 5)
                    {
                        landingCenter = new Vector2(boxBottom.X, boxBottom.Y - NPC.height * 0.5f);
                        break;
                    }
                }
            }

            if (!landingCenter.HasValue)
            {
                return;
            }

            _vanishCd = 380 + Main.rand.Next(200);
            _vanishTimer = VanishTotal;
            _vanishOrigin = NPC.Center;
            _vanishDest = landingCenter.Value;
            NPC.alpha = 255;
            NPC.velocity = Vector2.Zero;

            // Grey mist telegraphs both origin and validated landing for the full 30t vanish window.
            float mistRadius = Math.Max(NPC.width, NPC.height) * 0.625f;
            Projectile originMist = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), _vanishOrigin, Vector2.Zero,
                ModContent.ProjectileType<TeleportMistLinger>(), 0, 0, Main.myPlayer, 0f, mistRadius);
            originMist.timeLeft = VanishTeleportTicks;
            Projectile destMist = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), _vanishDest, Vector2.Zero,
                ModContent.ProjectileType<TeleportMistLinger>(), 0, 0, Main.myPlayer, 0f, mistRadius);
            destMist.timeLeft = VanishTeleportTicks;

            PlaySetPieceStartCue(HeroSetPiece.Vanish);
            EnterPhase(AttackPhase.NovaRecovery, VanishTotal + 6); // park the combat machine
            NPC.netUpdate = true;
        }

        // ── Bomb: lobbed alchemist flask → Potion Sickness ─────────────────────────────
        private void TickBomb(Player target)
        {
            if (_bombTimer > 0)
            {
                int elapsed = BombTotal - _bombTimer;
                _bombTimer--;
                NPC.velocity.X *= 0.85f;
                int face = target.Center.X < NPC.Center.X ? -1 : 1;
                NPC.direction = face;
                NPC.spriteDirection = face;

                if (elapsed <= BombWindupTicks)
                {
                    SetDisplayWeapon(SecondaryRangedWeaponItemType, swing: false);
                    if (!Main.dedServ && Main.rand.NextBool(2))
                    {
                        Dust fuseDust = Dust.NewDustPerfect(
                            PuppetHandPosition,
                            DustID.Torch,
                            Main.rand.NextVector2Circular(1f, 1f),
                            60,
                            new Color(180, 90, 220),
                            1.2f);
                        fuseDust.noGravity = true;
                    }
                }

                if (elapsed == BombWindupTicks) // throw near the end of the 42t wind-up
                {
                    // The flask is a server spawn; the throw sound plays everywhere.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 muzzle = PuppetHandPosition;
                        Vector2 vel = UsefulFunctions.BallisticTrajectory(muzzle, target.Center, 10f, fallback: true);
                        vel += Main.rand.NextVector2Circular(1.5f, 0f);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), muzzle, vel, ModContent.ProjectileType<HeroPotionBomb>(), potionBombDamage, 0f, Main.myPlayer);
                    }
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.3f }, NPC.Center);
                }

                if (_bombTimer == 0)
                {
                    SetDisplayWeapon(MeleeWeaponItemType, swing: false);
                }
                return;
            }

            // Starting rolls: server only. A client starts from the synced timer (ReceiveExtraAI).
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            if (_bombCd > 0)
            {
                _bombCd--;
                return;
            }

            if (!CanStartSetPiece)
            {
                return;
            }

            float dist = NPC.Distance(target.Center);
            bool inBand = dist >= 120f && dist <= 620f;
            if (!inBand || !Main.rand.NextBool(180))
            {
                return;
            }

            _bombCd = 420 + Main.rand.Next(180);
            _bombTimer = BombTotal;
            SetDisplayWeapon(SecondaryRangedWeaponItemType, swing: false);
            PlaySetPieceStartCue(HeroSetPiece.Bomb);
            SpawnTelegraphFlash(new Color(180, 90, 220));
            EnterPhase(AttackPhase.NovaRecovery, BombTotal + 6);
            NPC.netUpdate = true;
        }

        // ── A3: Archer's Volley ────────────────────────────────────────────────────────
        // He raises a war-cry (the read) and orders a volley: every artillery archer that is holding a post with a
        // open sky above it draws, then looses five flaming arrows nearly straight up. They fall back out of the sky
        // (straight down or at 45 degrees) in one marching line of 20 landing points across the player's position,
        // marked on the ground ~2.1s ahead (ManHunterLobArrow). Run ALONG the sweep, stand in a gap, or break to cover. The Hero only gives the cue here; each archer
        // decides for itself whether it can fire (ManHunter.TickArtillery).
        private void TickHorn(Player target)
        {
            if (_hornTimer > 0)
            {
                _hornTimer--;
                NPC.velocity.X *= 0.8f;
                int face = target.Center.X < NPC.Center.X ? -1 : 1;
                NPC.direction = face;
                NPC.spriteDirection = face;

                int elapsed = HornTotal - _hornTimer;
                // Telegraph (first 45t): rally pose — gold motes rising off his raised arm.
                if (elapsed < 45)
                {
                    if (Main.rand.NextBool(2))
                    {
                        Dust moteDust = Dust.NewDustPerfect(NPC.Center + new Vector2(NPC.direction * 8f, -18f) + Main.rand.NextVector2Circular(6f, 6f), DustID.GoldFlame, new Vector2(0f, -1.4f), 60, default, 1.4f);
                        moteDust.noGravity = true;
                    }
                    Lighting.AddLight(NPC.Center, 0.9f, 0.7f, 0.2f);
                }
                return;
            }

            // Starting rolls: server only. A client starts from the synced timer (ReceiveExtraAI).
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            if (_hornCd > 0)
            {
                _hornCd--;
                return;
            }

            if (!CanStartSetPiece)
            {
                return;
            }

            float dist = NPC.Distance(target.Center);
            bool outOfBand = dist < 130f || dist > 950f;
            if (outOfBand || !Main.rand.NextBool(150))
            {
                return;
            }

            // No cue with nobody to answer it (all four archers dead or already joined): keep the cooldown short and retry.
            if (!OrderArcherVolley())
            {
                _hornCd = 120;
                return;
            }

            _hornCd = 520 + Main.rand.Next(260);
            _hornTimer = HornTotal;
            PlaySetPieceStartCue(HeroSetPiece.Horn);
            if (Main.rand.NextBool(3))
            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.HeroofLumelia.Archers1"), 175, 75, 255);
            }
            SpawnTelegraphFlash(Color.Gold);
            EnterPhase(AttackPhase.NovaRecovery, HornTotal + 6);
            NPC.netUpdate = true;
        }

        // ── A5: Rally — the warband mini-event (50% / 25% HP) ───────────────────────────
        // War-cry → he backs off (widens spacing), lobs potion bombs, then returns to normal aggression. The
        // warband itself is the artillery archers (TickArchers); at 25% a Red Cloud Hunter also joins him.
        private void TickRally(Player target)
        {
            if (_rallyTimer > 0)
            {
                int elapsed = RallyTotal - _rallyTimer;
                _rallyTimer--;

                // Widen spacing: back away from the player while the warband forms (fast inside 440px, a drift beyond).
                int awayDir = NPC.Center.X > target.Center.X ? 1 : -1;
                float dist = NPC.Distance(target.Center);
                float backAwaySpeed = 0.6f;
                if (dist < 440f)
                {
                    backAwaySpeed = 3.4f;
                }
                NPC.velocity.X = awayDir * backAwaySpeed;
                NPC.direction = -awayDir; // still face the player
                NPC.spriteDirection = -awayDir;

                // Grey smoke roiling off him.
                if (Main.rand.NextBool(2))
                {
                    Dust smokeDust = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(14f, 20f), DustID.Smoke, new Vector2(0f, -0.6f), 120, new Color(150, 150, 150), 1.5f);
                    smokeDust.noGravity = true;
                }

                // Potion bombs to cover the warband's arrival (server spawn, sound everywhere).
                if (elapsed == 90 || elapsed == 190)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 muzzle = NPC.Center + new Vector2(NPC.direction * 10f, -8f);
                        Vector2 vel = UsefulFunctions.BallisticTrajectory(muzzle, target.Center, 10f, fallback: true);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), muzzle, vel, ModContent.ProjectileType<HeroPotionBomb>(), potionBombDamage, 0f, Main.myPlayer);
                    }
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.3f }, NPC.Center);
                }
                // Capstone lieutenant: only the 25% rally calls one (the 50% rally has no add).
                if (elapsed == 250 && _rallyStage >= 2)
                {
                    SpawnSupport(ModContent.NPCType<NPCs.Enemies.Archer.RedCloudHunter>());
                    SoundEngine.PlaySound(SoundID.NPCHit6 with { Volume = 0.35f, Pitch = 0.1f }, NPC.Center);
                }
                return;
            }

            // Starting is the server's call. A client starts from the synced timer (ReceiveExtraAI).
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            // The HP trigger only fires from a free phase (it arms silently while busy).
            bool freePhase = Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll;
            if (!freePhase)
            {
                return;
            }

            float hpFrac = (float)NPC.life / NPC.lifeMax;
            bool firstRally = _rallyStage == 0 && hpFrac <= 0.5f;
            bool secondRally = _rallyStage == 1 && hpFrac <= 0.25f;
            if (!firstRally && !secondRally)
            {
                return;
            }

            _rallyStage++;
            _rallyTimer = RallyTotal;
            PlaySetPieceStartCue(HeroSetPiece.Rally);
            SpawnTelegraphFlash(Color.Gold);
            EnterPhase(AttackPhase.NovaRecovery, RallyTotal + 6);
            NPC.netUpdate = true;
        }

        // ── A1 payload: the varied knife fans ───────────────────────────────────────────
        protected override void DoCursedKnivesThrow()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            Player target = Main.player[NPC.target];

            switch (_knifePatternIndex % 3)
            {
                case 0: // tight fast 3-fan
                    ThrowKnifeFan(3, 16f, 12.5f, target.Center);
                    break;
                case 1: // wide slow 5-fan
                    ThrowKnifeFan(5, 46f, 10f, target.Center);
                    break;
                default: // high/low double pair
                    ThrowKnifeFan(2, 10f, 11.5f, target.Center + new Vector2(0f, -60f));
                    ThrowKnifeFan(2, 10f, 11.5f, target.Center + new Vector2(0f, 60f));
                    break;
            }
            _knifePatternIndex++;
            SoundEngine.PlaySound(SoundID.Item17 with { PitchVariance = 0.15f }, NPC.Center);
        }

        private void ThrowKnifeFan(int count, float totalSpreadDeg, float speed, Vector2 aimAt)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            Vector2 muzzle = NPC.Center + new Vector2(NPC.direction * 10f, -8f);
            Vector2 baseAim = (aimAt - muzzle).SafeNormalize(new Vector2(NPC.direction, 0f));
            float step = count > 1 ? totalSpreadDeg / (count - 1) : 0f;
            float start = -totalSpreadDeg / 2f;
            for (int i = 0; i < count; i++)
            {
                Vector2 vel = baseAim.RotatedBy(MathHelper.ToRadians(start + step * i)) * speed;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), muzzle, vel, ModContent.ProjectileType<EnemyThrowingKnife>(), throwingKnifeDamage, 0f, Main.myPlayer);
            }
        }

        // ── A4 payload: fiery lunge VFX ─────────────────────────────────────────────────
        protected override void DoPierceWindup(int elapsed)
        {
            if (elapsed == 0)
            {
                SetAttackLabel("Piercing Dash", 90);
                SpawnTelegraphFlash(Color.OrangeRed);
                SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.5f }, NPC.Center);
            }
            if (Main.dedServ) return;
            // Fire converging inward to the blade — the wind-up read.
            for (int i = 0; i < 2; i++)
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = NPC.Center + ang.ToRotationVector2() * (40f + Main.rand.NextFloat(20f));
                Dust d = Dust.NewDustPerfect(pos, DustID.Torch, (NPC.Center - pos) * 0.08f, 40, default, 1.3f);
                d.noGravity = true;
            }
            Lighting.AddLight(NPC.Center, 1f, 0.5f, 0.15f);
        }

        protected override void DoPierceDashTick()
        {
            if (Main.dedServ) return;
            for (int i = 0; i < 3; i++)
            {
                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                Dust d = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(12f, 16f), type, -NPC.velocity * 0.2f, 50, Color.OrangeRed, Main.rand.NextFloat(1.2f, 1.8f));
                d.noGravity = true;
            }
            Lighting.AddLight(NPC.Center, 1.1f, 0.55f, 0.15f);
        }

        protected override void OnPierceContact(Player target, bool isStab)
        {
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.5f, Pitch = 0.3f }, NPC.Center);
            UsefulFunctions.ScreenShake(NPC.Center, 4f, 12, distanceFalloff: 500f);
            if (Main.dedServ) return;
            for (int i = 0; i < 20; i++)
            {
                Dust d = Dust.NewDustPerfect(target.Center, DustID.Torch, Main.rand.NextVector2Circular(6f, 6f), 40, Color.OrangeRed, 1.6f);
                d.noGravity = true;
            }
        }

        // ── Melee: fire flair on swings; heavy steps throw a HeroFireArc crescent ────────
        bool _landingLeapFireArcReleased;

        protected override void DoMeleeAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, PitchVariance = 0.2f }, NPC.Center);
            TryMeleeHit();
        }

        protected override void DoComboMeleeHit(MeleeComboStep step)
        {
            base.DoComboMeleeHit(step);
            _landingLeapFireArcReleased = false;
        }

        protected override void OnMeleeComboTelegraphTick(
            MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            if (Main.dedServ || elapsed % 3 != 0)
            {
                return;
            }

            Vector2 weaponDir = PuppetWeaponDirection.SafeNormalize(new Vector2(NPC.direction, 0f));
            Vector2 bladeTip = PuppetHandPosition + weaponDir * (ComboReachBase * 0.7f * step.ReachMult);
            Dust ember = Dust.NewDustPerfect(
                bladeTip + Main.rand.NextVector2Circular(6f, 6f),
                DustID.Torch,
                -weaponDir * 0.6f + new Vector2(0f, -0.5f),
                80,
                Color.OrangeRed,
                Main.rand.NextFloat(1.0f, 1.4f));
            ember.noGravity = true;
        }

        protected override void OnMeleeComboAttackTick(
            MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            float bladeReach = ComboReachBase * 0.7f * step.ReachMult;

            if (step.Motion != ComboMotion.LeapSlam)
            {
                float progress = elapsed / (float)Math.Max(1, total - 1);
                bool withinLiveWindow = step.HitWindowEnd <= 0f || progress <= step.HitWindowEnd;
                if (withinLiveWindow)
                {
                    // Arm fire slash shader only while hitbox is live so fadeout matches disarm.
                    ArmFireSlashVFX(bladeReach, progress);
                }

                int peakTick = Math.Max(1, step.EaseInTicks);
                if (elapsed == peakTick && step.ReachMult >= 1.1f)
                {
                    ReleaseHeroFireArc();
                }
            }
        }

        protected override void OnLandingTimedLeapSlamSwingTick(MeleeComboStep step, float progress)
        {
            base.OnLandingTimedLeapSlamSwingTick(step, progress);

            float bladeReach = ComboReachBase * 0.7f * step.ReachMult;
            ArmFireSlashVFX(bladeReach, progress);

            if (!_landingLeapFireArcReleased && progress >= 0.35f && step.ReachMult >= 1.1f)
            {
                _landingLeapFireArcReleased = true;
                ReleaseHeroFireArc();
            }
        }

        private void ReleaseHeroFireArc()
        {
            if (!Main.dedServ)
            {
                Vector2 weaponDir = PuppetWeaponDirection.SafeNormalize(new Vector2(NPC.direction, 0f));
                Vector2 tip = PuppetHandPosition + weaponDir * 44f;
                for (int i = 0; i < 6; i++)
                {
                    Dust spark = Dust.NewDustPerfect(
                        tip + Main.rand.NextVector2Circular(12f, 12f),
                        DustID.Torch,
                        weaponDir * Main.rand.NextFloat(1.5f, 3.5f),
                        60,
                        Color.OrangeRed,
                        Main.rand.NextFloat(1.2f, 1.7f));
                    spark.noGravity = true;
                }

                SoundEngine.PlaySound(SoundID.Item73 with { Volume = 0.5f, Pitch = 0.2f }, NPC.Center);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Player target = Main.player[NPC.target];
                Vector2 origin = NPC.Center + new Vector2(NPC.direction * 24f, -6f);
                Vector2 velocity = (target.Center - origin).SafeNormalize(new Vector2(NPC.direction, 0f)) * 8f;
                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    origin,
                    velocity,
                    ModContent.ProjectileType<HeroFireArc>(),
                    fireArcDamage,
                    3f,
                    Main.myPlayer);
            }
        }

        protected override void DoRangedAttack() { }

        // ── Idle fire identity ──────────────────────────────────────────────────────────
        public override void PostAI()
        {
            base.PostAI();
            if (Main.dedServ) return;

            // Life-scaled ember aura.
            Lighting.AddLight(NPC.Center, 0.5f, 0.28f, 0.08f);
            if ((Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll) && Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(10f, 18f), DustID.Torch, new Vector2(0f, -0.8f), 120, default, Main.rand.NextFloat(0.7f, 1.2f));
                d.noGravity = true;
            }
        }

        private void SmokePuff(Vector2 pos)
        {
            if (Main.dedServ) return;
            for (int i = 0; i < 22; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(6f, 6f);
                Dust d = Dust.NewDustPerfect(pos, DustID.Smoke, vel, 60, new Color(170, 170, 170), 1.6f);
                d.noGravity = true;
            }
        }

        private void SpawnTelegraphFlash(Color color)
        {
            // The flash is a synced projectile: the server spawns it once for everyone (a client spawn would duplicate it).
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(color));
        }

        // ── Artillery archers ───────────────────────────────────────────────────────────
        // The archers run their own AI (ManHunter); the Hero spawns them, replaces dead ones, sends the volley cue
        // (TickHorn) and flips a pair to the melee at 60% / 35% HP. Server only. The archers despawn themselves when
        // this NPC is gone, so there is no cleanup here.
        private void TickArchers()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget)
            {
                return;
            }

            float hpFraction = (float)NPC.life / NPC.lifeMax;

            if (_joinStage == 0 && hpFraction <= FirstJoinHpFraction)
            {
                SendArchersToJoin(0, 1);
                _joinStage = 1;
            }
            else if (_joinStage == 1 && hpFraction <= SecondJoinHpFraction)
            {
                SendArchersToJoin(2, 3);
                _joinStage = 2;
            }

            for (int slot = 0; slot < ArcherSlotCount; slot++)
            {
                // A pair that has been sent in is part of the melee now: never spawn or replace it.
                bool pairJoined = _joinStage >= 1 && slot < 2;
                pairJoined = pairJoined || (_joinStage >= 2 && slot >= 2);

                if (pairJoined)
                {
                    continue;
                }

                NPCs.Enemies.Archer.ManHunter archer;

                if (_archerIds[slot] >= 0 && !TryGetArcher(slot, out archer))
                {
                    _archerIds[slot] = -1;
                    _archerRespawn[slot] = ArcherRespawnTicks;
                }

                if (_archerIds[slot] >= 0)
                {
                    continue;
                }

                if (_archerRespawn[slot] > 0)
                {
                    _archerRespawn[slot]--;
                    continue;
                }

                // No post found (or no NPC slot): retry in a second.
                if (!TrySpawnArcher(slot))
                {
                    _archerRespawn[slot] = 60;
                }
            }
        }

        private bool TryGetArcher(int slot, out NPCs.Enemies.Archer.ManHunter archer)
        {
            archer = null;
            int npcIndex = _archerIds[slot];

            if (npcIndex < 0 || npcIndex >= Main.maxNPCs || !Main.npc[npcIndex].active)
            {
                return false;
            }

            archer = Main.npc[npcIndex].ModNPC as NPCs.Enemies.Archer.ManHunter;
            return archer != null && archer.OwnerId == NPC.whoAmI;
        }

        // Spawns the slot's archer directly on its post (no walk-in: it is off-screen). Even slots are left of the
        // Hero, odd right; the near pair holds 94 tiles out, the far pair 106.
        private bool TrySpawnArcher(int slot)
        {
            int side = 1;

            if (slot % 2 == 0)
            {
                side = -1;
            }

            float standoffTiles = FarPairStandoffTiles;

            if (slot < 2)
            {
                standoffTiles = NearPairStandoffTiles;
            }

            float desiredX = NPC.Center.X + side * standoffTiles * 16f;
            int feetTileY = (int)(NPC.Bottom.Y / 16f);
            Vector2 postFeet;
            bool foundPost = NPCs.Enemies.Archer.ManHunter.TryFindPost(desiredX, feetTileY, out postFeet);

            if (!foundPost)
            {
                return false;
            }

            int npcIndex = NPC.NewNPC(NPC.GetSource_FromAI(), (int)postFeet.X, (int)postFeet.Y, ModContent.NPCType<NPCs.Enemies.Archer.ManHunter>());

            if (npcIndex >= Main.maxNPCs)
            {
                return false;
            }

            NPCs.Enemies.Archer.ManHunter archer = Main.npc[npcIndex].ModNPC as NPCs.Enemies.Archer.ManHunter;

            if (archer != null)
            {
                archer.ConfigureAsHeroArcher(NPC.whoAmI, NPC.type, slot, side, standoffTiles, postFeet);
            }

            _archerIds[slot] = npcIndex;
            return true;
        }

        private void SendArchersToJoin(int firstSlot, int secondSlot)
        {
            NPCs.Enemies.Archer.ManHunter archer;

            if (TryGetArcher(firstSlot, out archer))
            {
                archer.BeginJoin();
            }

            if (TryGetArcher(secondSlot, out archer))
            {
                archer.BeginJoin();
            }
        }

        // The volley cue: every artillery archer gets the same sweep direction. Returns false if there is nobody to answer.
        private bool OrderArcherVolley()
        {
            int sweepDirection = 1;

            if (Main.rand.NextBool())
            {
                sweepDirection = -1;
            }

            int ordered = 0;

            for (int slot = 0; slot < ArcherSlotCount; slot++)
            {
                NPCs.Enemies.Archer.ManHunter archer;

                if (TryGetArcher(slot, out archer) && archer.IsArtillery)
                {
                    archer.OrderVolley(sweepDirection);
                    ordered++;
                }
            }

            return ordered > 0;
        }

        private void SpawnSupport(int npcType)
        {
            // The ally is a server spawn; the summon sparkle plays everywhere.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, npcType, 0);
                NPC.netUpdate = true;
            }
            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, NPC.velocity.X, NPC.velocity.Y);
            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, NPC.velocity.X, NPC.velocity.Y);
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByItem(player, item, hit, damageDone);
            tsorcRevampAIs.EvasiveOnHit(NPC, true);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByProjectile(projectile, hit, damageDone);
            tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
        }

        #region Gore
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Hero of Lumelia Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Hero of Lumelia Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Hero of Lumelia Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Hero of Lumelia Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Hero of Lumelia Gore 3").Type, 1f);
            }
        }
        #endregion

        #region Debuffs
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 60 * 60, false);
            target.AddBuff(ModContent.BuffType<Crippled>(), 20 * 60, false);

            if (Main.rand.NextBool(2))
            {
                target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 30 * 60, false);
            }
        }
        #endregion

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<HeroOfLumeliaBag>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<StaminaVessel>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<SoulCoin>(), 1, 7, 14));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<ArrowOfBard>(), 1, 7, 14));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ItemID.WaterWalkingBoots));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<MagicBarrierScroll>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Humanity>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ItemID.ObsidianSkinPotion));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<CrimsonPotion>(), 1, 1, 2));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<ShockwavePotion>(), 1, 1, 2));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<GreaterRestorationPotion>(), 1, 1, 2));
            npcLoot.Add(notExpertCondition);
        }
    }
}
