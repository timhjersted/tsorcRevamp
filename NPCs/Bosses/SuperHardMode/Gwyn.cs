using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Armors;
using tsorcRevamp.Items.Weapons.Enemy;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Projectiles.Melee.Broadswords;
using tsorcRevamp.Utilities;

// NOTE: the folder is Gwyn/, but the namespace stays flat (…SuperHardMode) — a namespace segment
// named "Gwyn" would collide with the old Gwyn class still living in this same parent namespace.
namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    ///<summary>
    ///Gwyn, Lord of Cinder — the final boss, rebuilt from near-scratch on the Puppet system (the
    ///Artorias-revamp pattern: a puppet Player wearing the LordGwyn armor set, swinging the Great
    ///Lord Greatsword through the melee combo system).
    ///
    ///PHASE 1 (this file): the foundation. Core stats carried over from the old Gwyn, plus the three
    ///kept systems at their original distances —
    ///  • DEFENSE RING (1000px): beyond it his defense locks to 9999 ("protected by the First Flame");
    ///    inside it he is fightable. The old SwordOfLordGwyn guardian-NPC mechanic is GONE — base
    ///    defense sits at the old post-sword value permanently.
    ///  • COWARD'S RING (2000px): the flame-wall dust boundary; flight is torn down inside it, and
    ///    fleeing beyond it applies Coward's Affliction after a 90-tick grace.
    ///  • RAIN OF DEATH (&gt;600px): running keeps you under a random rain of death orbs.
    ///The old kit lives on in Soul of Cinder; everything else here is built new. The full 12-attack
    ///state machine lands in Phase 2.
    ///</summary>
    [AutoloadBossHead]
    class Gwyn : PuppetNPC
    {
        // PuppetNPC overrides Texture to the shared puppet placeholder, so point the boss-head
        // icon at the existing Gwyn head texture explicitly (same workaround as Artorias).
        public override string BossHeadTexture => "tsorcRevamp/NPCs/Bosses/SuperHardMode/Gwyn_Head_Boss";

        protected override string InvaderTitle => "Gwyn, Lord of Cinder";

        // ── Loadout: the Lord of Cinder's own regalia ────────────────────────────
        protected override int HeadArmorItemType => ModContent.ItemType<LordGwynHelm>();
        protected override int BodyArmorItemType => ModContent.ItemType<LordGwynArmor>();
        protected override int LegsArmorItemType => ModContent.ItemType<LordGwynLeggings>();
        protected override float PuppetDrawScale => 1.2f;
        // The composite body sheet supplies the gray sleeve, gold wrist, and brown hand. Its
        // transparent joins still use the synthetic player's skin substrate, so match that to
        // Gwyn's dark-brown authored palette instead of PuppetNPC's bright orange invader default.
        protected override Color PuppetSkinColor => new Color(130, 90, 60);

        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemySwordOfGwyn>();
        protected override int RangedWeaponItemType => -1; // melee + bespoke fire/lightning magic
        protected override Vector2 MeleeHandleNorm => new Vector2(0.14f, 0.86f);
        protected override float MeleeWeaponDrawScale => 0.65f;
        protected override float ComboReachBase => 125f;
        protected override float MeleeBladeWidth => 30f;

        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Greatsword;

        protected override int MeleeDamage => TooEarly ? TooEarlyDamage : 95; // the old contact damage, now via weapon hitboxes
        protected override int RangedDamage => 0;

        // DS1 Gwyn is shockingly fast for his size — relentless pressure, heavier than Artorias' grace.
        // Wrath of Gwyn (below 30% HP) turns the dial up: faster, more aggressive.
        protected override float TopSpeed => _wrathActive ? 3.2f : 2.6f;
        protected override float Acceleration => _wrathActive ? 0.19f : 0.14f;
        protected override int MeleeComboChance => _wrathActive ? 95 : 85;

        // ── Wings (storm-only flight; hidden whenever Gwyn is grounded) ─────────
        // Angel wings for the god of sunlight — traded for flame wings once the Wrath ignites.
        // Autonomous flight is fully disabled: the flight controller only lifts off when the
        // Sunlight Spear Storm commands it (Flight.RequestTakeoff in TickSpearStorm).
        protected override bool HasWings => true;
        protected override int WingsAccessoryItemType => _wrathActive ? ItemID.FlameWings : ItemID.AngelWings;
        protected override bool ShowWingsWhenGrounded => false;
        protected override int RandomTakeoffChance => 0;
        protected override float FlightHeightTrigger => 99999f;
        protected override float FlightHpEscalationFrac => 0f;

        // ── Greatsword reach (bigger than a normal blade) + combat feel ──────────
        protected override float MeleeRange => 110f;
        protected override float StabRange => 180f;
        protected override float ComboMaxStartRange => 340f;
        // The sword's authored arc now drives the arm, weapon, swept collision, and slash VFX.
        // Alternating the arc would muddy the explicit Backhand and Guillotine telegraphs.
        protected override bool UseSwingEasing => true;
        protected override bool UseAlternateFlip => false;
        protected override bool UseAimAdaptiveArc => true;
        protected override bool UseLogicalMeleeTelegraphs => true;
        // Gwyn's armor sheet authors the straight Use1-Use4 swing arms, like Abyssal Ninja.
        // Composite arms select the bent elbow pieces instead, which is the wrong silhouette.
        protected override bool UseCompositeArmSwing => false;
        protected override bool UseTwoHandedCompositeSwing => false;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override bool HasSlashVFX => false; // Gwyn draws a dedicated shader-lit fire slash in PostDraw.
        protected override float WalkAnimationSpeedMultiplier => 0.35f;
        protected override float OverheadWindupOvershoot => MathHelper.ToRadians(17f);
        protected override bool SlowDownBeforeMelee => false; // pursue through the windup — no walking out of the telegraph

        // ── The greatsword moveset (bespoke, reactive) ───────────────────────────
        // Fully reactive: ReactiveComboIndex reads the player's live dodge/launch/flank state each
        // time a combo starts and picks the counter, falling back to the weighted roll otherwise.
        // Flash colors are the Lord of Cinder's fire — orange bread & butter, red heavy commits.
        const int CB_CLEAVE = 0, CB_UNDEROVER = 1, CB_LEAP = 2, CB_SLIDE = 3, CB_SPIN = 4,
                  CB_CINDERFALL = 5, CB_GUILLOTINE = 6, CB_BACKHAND = 7, CB_THREEHIT = 8,
                  CB_ROLLCATCH = 9, CB_FLURRY = 10;

        static MeleeComboStep GS(ComboMotion m, int tel, int atk, int pause, float dmg = 1f, float reach = 1f, float push = 0f)
            => new MeleeComboStep { Motion = m, TelegraphTicks = tel, AttackTicks = atk, PostStepPause = pause, DamageMult = dmg, ReachMult = reach, ForwardPushMult = push };

        // Complete grounded strikes use the single-clock runtime. Movement attacks and linked
        // strings stay on MeleeCombo because their locomotion and continuation are the behavior.
        static readonly PuppetAttackClip CleaveV2 = new PuppetAttackClip(
            name: "Cleave",
            pose: PuppetPosePreset.TwoHandedSwing,
            windupTicks: 30,
            activeTicks: 24,
            recoveryTicks: 20,
            oppositeWindupRotation: 0.85f,
            attackStartRotation: -0.75f,
            attackEndRotation: 0.85f,
            hitWindowStart: 0.12f,
            hitWindowEnd: 0.82f,
            swingEase: SwingEaseStyle.Snap,
            maxAimCorrection: 0.30f,
            aimLockTicksBeforeActive: 14);

        static readonly PuppetAttackClip CinderfallV2 = new PuppetAttackClip(
            name: "Cinderfall",
            pose: PuppetPosePreset.TwoHandedSwing,
            windupTicks: 34,
            activeTicks: 28,
            recoveryTicks: 26,
            oppositeWindupRotation: 1.10f,
            attackStartRotation: -1.70f,
            attackEndRotation: 1.25f,
            hitWindowStart: 0.22f,
            hitWindowEnd: 0.88f,
            swingEase: SwingEaseStyle.Whip,
            maxAimCorrection: 0.25f,
            aimLockTicksBeforeActive: 18);

        static readonly PuppetAttackClip GuillotineV2 = new PuppetAttackClip(
            name: "Guillotine",
            pose: PuppetPosePreset.TwoHandedSwing,
            windupTicks: 40,
            activeTicks: 30,
            recoveryTicks: 30,
            oppositeWindupRotation: 1.10f,
            attackStartRotation: -1.75f,
            attackEndRotation: 1.15f,
            hitWindowStart: 0.22f,
            hitWindowEnd: 0.82f,
            swingEase: SwingEaseStyle.Whip,
            maxAimCorrection: 0.22f,
            aimLockTicksBeforeActive: 20);

        static readonly PuppetAttackClip BackhandV2 = new PuppetAttackClip(
            name: "Backhand Step",
            pose: PuppetPosePreset.TwoHandedSwing,
            windupTicks: 30,
            activeTicks: 20,
            recoveryTicks: 16,
            oppositeWindupRotation: -0.65f,
            attackStartRotation: 0.80f,
            attackEndRotation: -0.65f,
            hitWindowStart: 0.12f,
            hitWindowEnd: 0.78f,
            swingEase: SwingEaseStyle.Snap,
            maxAimCorrection: 0.30f,
            aimLockTicksBeforeActive: 10);

        static readonly MeleeCombo[] GwynCombos = new[]
        {
            // 0 — Cleave: the bread-and-butter wide swing
            new MeleeCombo { Name = "Cleave", BaseWeight = 100, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Orange, CooldownAfterUse = 40, RuntimeV2Clip = CleaveV2,
                Steps = new[] { GS(ComboMotion.HorizontalSweep, 15, 18, 0, 1.0f, 1.1f) } },
            // 1 — Under-Over juggle: rising launch into an overhead chop
            new MeleeCombo { Name = "Under-Over", BaseWeight = 70, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Gold, CooldownAfterUse = 130,
                Steps = new[] {
                    GS(ComboMotion.UnderhandArc, 15, 20, 12, 1.0f, 1.1f, 0.4f),
                    GS(ComboMotion.OverheadArc,   0, 22,  0, 1.3f, 1.15f),
                } },
            // 2 — Cindering Leap Overhead: tracking leap that reaches, then the 170° chop (roll it on landing)
            new MeleeCombo { Name = "Cindering Leap", BaseWeight = 55, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.OrangeRed, CooldownAfterUse = 150, HeavyCommit = true,
                Steps = new[] { GS(ComboMotion.LeapSlam, 25, 24, 0, 1.5f, 1.2f) } },
            // 3 — Sliding Thrust: low dash pierce, gap-closer
            new MeleeCombo { Name = "Sliding Thrust", BaseWeight = 55, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Yellow, CooldownAfterUse = 130,
                Steps = new[] { GS(ComboMotion.JoustDash, 20, 16, 0, 1.2f, 1.4f, 1.8f) } },
            // 4 — Sunspin: 360° sweep, anti-flank
            new MeleeCombo { Name = "Sunspin", BaseWeight = 45, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Yellow, CooldownAfterUse = 170, HeavyCommit = true,
                Steps = new[] { GS(ComboMotion.Spin, 20, 26, 0, 1.1f, 1.15f, 0.6f) } },
            // 5 — Cinderfall: committed ground cleave + fire AoE
            new MeleeCombo { Name = "Cinderfall", BaseWeight = 40, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Red, CooldownAfterUse = 200, HeavyCommit = true, RuntimeV2Clip = CinderfallV2,
                Steps = new[] { GS(ComboMotion.GroundSlam, 25, 24, 0, 1.6f, 1.3f) } },
            // 6 — Guillotine Drop: heavy standing overhead, the "respect me" punish
            new MeleeCombo { Name = "Guillotine", BaseWeight = 45, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Red, CooldownAfterUse = 200, HeavyCommit = true, RuntimeV2Clip = GuillotineV2,
                Steps = new[] { GS(ComboMotion.OverheadArc, 30, 22, 0, 1.8f, 1.15f) } },
            // 7 — Backhand + Step: quick re-engaging sweep, denies a roll-back
            new MeleeCombo { Name = "Backhand Step", BaseWeight = 60, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Orange, CooldownAfterUse = 70, RuntimeV2Clip = BackhandV2,
                Steps = new[] { GS(ComboMotion.HorizontalSweep, 12, 16, 0, 0.9f, 1.1f, 0.8f) } },
            // 8 — 3-Hit Standard: the staple pressure string
            new MeleeCombo { Name = "3-Hit", BaseWeight = 55, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.OrangeRed, CooldownAfterUse = 180,
                Steps = new[] {
                    GS(ComboMotion.HorizontalSweep, 14, 18, 12, 0.9f),
                    GS(ComboMotion.HorizontalSweep,  0, 18, 12, 0.9f, 1.05f),
                    GS(ComboMotion.OverheadArc,      0, 22,  0, 1.3f, 1.1f),
                } },
            // 9 — Roll-Catch: leap in, then the flip slam lands where a panicked roll ends
            new MeleeCombo { Name = "Roll-Catch", BaseWeight = 30, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Red, CooldownAfterUse = 240, HeavyCommit = true,
                Steps = new[] {
                    GS(ComboMotion.LeapSlam,   25, 22, 14, 1.3f, 1.2f),
                    GS(ComboMotion.GroundSlam,  0, 24,  0, 1.5f, 1.3f),
                } },
            // 10 — Wrath Flurry: the enrage full-commit chain (weighted up only at low HP)
            new MeleeCombo { Name = "Wrath Flurry", BaseWeight = 25, Preferred = ComboRangeBand.Any,
                InitialFlashColor = Color.Red, CooldownAfterUse = 300, HeavyCommit = true, HyperArmor = true,
                Steps = new[] {
                    GS(ComboMotion.JoustDash,    22, 14, 8,  1.0f, 1.2f, 1.5f),
                    GS(ComboMotion.UnderhandArc,  0, 16, 8,  0.9f, 1.1f),
                    GS(ComboMotion.OverheadArc,   0, 16, 8,  1.1f, 1.15f),
                    GS(ComboMotion.Spin,          0, 24, 0,  1.2f, 1.15f, 0.6f),
                } },
        };

        protected override MeleeCombo[] MeleeComboPoolOverride => GwynCombos;

        ///<summary>Fully reactive combo pick: read the player's live state and answer it. Falls
        ///through to the weighted roll (returns -1) when no specific read applies, or when the
        ///counter it wants is on cooldown.</summary>
        protected override int ReactiveComboIndex(float dist, ComboRangeBand band, int[] ready)
        {
            Player player = Main.player[NPC.target];
            if (player == null || !player.active || player.dead)
            {
                return -1;
            }
            //Judgment from Behind: the teleport just planted him at the player's back — the queued
            //punish is a guaranteed heavy overhead (fallback: the juggle) the moment a combo can start.
            if (_judgmentPending > 0)
            {
                if (Ready(ready, CB_GUILLOTINE)) { _judgmentPending = 0; return CB_GUILLOTINE; }
                if (Ready(ready, CB_UNDEROVER)) { _judgmentPending = 0; return CB_UNDEROVER; }
            }
            //Gravity of the Sun just reeled them in — greet them with the pressure string
            if (_pullComboNudge > 0)
            {
                if (Ready(ready, CB_THREEHIT)) { _pullComboNudge = 0; return CB_THREEHIT; }
                if (Ready(ready, CB_CLEAVE)) { _pullComboNudge = 0; return CB_CLEAVE; }
            }
            bool rolling = player.GetModPlayer<tsorcRevampPlayer>().isDodging;
            bool launched = player.velocity.Y < -3f && player.Center.Y < NPC.Center.Y - 24f;
            float awaySign = Math.Sign(player.Center.X - NPC.Center.X);      // side the player is on
            bool rollingAway = rolling && Math.Sign(player.velocity.X) == awaySign && Math.Abs(player.velocity.X) > 2f;
            bool rollingThrough = rolling && dist < MeleeRange + 20f;         // dodging through him at point-blank

            // Player popped into the air → leap up and catch them with the overhead
            if (launched && Ready(ready, CB_LEAP))
            {
                return CB_LEAP;
            }
            // Player rolled through/behind at close range → spin covers every side
            if (rollingThrough && Ready(ready, CB_SPIN))
            {
                return CB_SPIN;
            }
            // Player rolling away → chase: a slide up close, the roll-catch leap from farther out
            if (rollingAway)
            {
                if (dist > StabRange && Ready(ready, CB_ROLLCATCH))
                {
                    return CB_ROLLCATCH;
                }
                if (Ready(ready, CB_SLIDE))
                {
                    return CB_SLIDE;
                }
            }
            return -1; // no live read — let the weighted roll pick a standard swing
        }

        static bool Ready(int[] ready, int idx) => idx >= 0 && idx < ready.Length && ready[idx] > 0;

        // ── Kept systems (original distances) ────────────────────────────────────
        const float DefenseRingRadius = 1000f; // beyond this: defense 9999
        const int FightableDefense = 130;      // the old post-sword value; the sword mechanic is removed
        const float CowardRingRadius = 2000f;  // beyond this: Coward's Affliction (after grace)
        const float RainOfDeathRange = 600f;   // the old whyAreYouRunning
        const int BaseRainOfDeathDamage = 77;  // the old herosArrowDamage
        const int TooEarlyDamage = 10000;
        const float ProximityDebuffRange = 700f;

        int RainOfDeathDamage => TooEarly ? TooEarlyDamage : BaseRainOfDeathDamage;

        bool TooEarly =>
            !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Artorias>())) ||
            !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Seath.SeathTheScalelessHead>())) ||
            !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<EarthFiendLich>())) ||
            !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<FireFiendMarilith>())) ||
            !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<WaterFiendKraken>())) ||
            !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<GhostWyvernMage.WyvernMageShadow>()));

        int protectedHoldTimer;   // keeps the 9999 penalty (and the broadcast) from re-triggering every tick
        float cowardGraceTimer = 90;
        bool announcedCoward;

        NPCDespawnHandler despawnHandler;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire3] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.knockBackResist = 0;
            NPC.damage = 0; // all damage via weapon hitboxes
            NPC.defense = FightableDefense;
            NPC.height = 40;
            NPC.width = 30;
            NPC.lifeMax = 750000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 2000000;
            NPC.rarity = 44;
            NPC.boss = true;
            NPC.lavaImmune = true;
            Music = 12;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.Gwyn.DespawnHandler"), Color.OrangeRed, DustID.Torch);

            tsorcRevampGlobalNPC gwynGlobalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            gwynGlobalNPC.Agility = 0.35f; // proactive dodges (see EvadesProjectiles)
            gwynGlobalNPC.NavSearchRadius = 80;

            // On-hit dodgeroll bundle, same family as Artorias — the Lord does not stand in combos
            EvasiveProfile.RedKnight(gwynGlobalNPC);
        }

        protected override bool EvadesProjectiles => true;

        ///<summary>The old Gwyn's on-hit debuff stack — a hit from the Lord of Cinder RUINS you.</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.OnFire, 10 * 60, false);
            target.AddBuff(ModContent.BuffType<FracturingArmor>(), 40 * 60, false); //lose defense on hit
            target.AddBuff(ModContent.BuffType<SlowedLifeRegen>(), 30 * 60, false);
            target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 30 * 60, false);    //lose knockback resistance
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.Weak, 10 * 60, false);
                target.AddBuff(BuffID.BrokenArmor, 3 * 60, false);
            }
        }

        public override void AI()
        {
            _drawFireSlash = false;
            //Contact only hurts during the Unbroken Advance march (all other damage is weapon hitboxes)
            NPC.damage = TooEarly ? TooEarlyDamage : (_advanceTimer > 0 ? MeleeDamage : 0);

            base.AI();
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            TickDefenseRing();
            TickCowardRing();
            TickRainOfDeath();
            TickProximityDebuffs();
            TickWrath();
            TickFirestorm();
            TickDescent();
            TickFlashStep();
            TickJudgment();
            TickRiposte();
            TickSpearStorm();
            TickGravity();
            TickAdvance();
            TickWingedPlunge();
            if (_judgmentPending > 0)
            {
                _judgmentPending--;
            }
            if (_pullComboNudge > 0)
            {
                _pullComboNudge--;
            }

            //Debug HUD attack label (DebugMode overlay reads DebugAttackLabel)
            if (_attackLabelTimer > 0 && --_attackLabelTimer == 0)
            {
                DebugAttackLabel = null;
            }
        }

        int _attackLabelTimer;
        ///<summary>Announce the named attack to the DebugMode HUD for the given duration.</summary>
        void SetAttackLabel(string name, int ticks = 90)
        {
            DebugAttackLabel = name;
            _attackLabelTimer = ticks;
        }

        // ── Wrath of Gwyn (the <30% HP enrage phase change) ──────────────────────
        // One-time ignition: he wreathes himself in white-hot flame for the rest of the fight, moving
        // faster and attacking more (TopSpeed/Acceleration/MeleeComboChance above key off _wrathActive;
        // the summon cooldowns shrink), and the base combo core already weights the Wrath Flurry chain
        // up as HP drops. The First Flame, given fully.
        bool _wrathActive;

        void TickWrath()
        {
            if (!_wrathActive && NPC.life < NPC.lifeMax * 0.30f)
            {
                _wrathActive = true;
                SetAttackLabel("Wrath of Gwyn", 120);
                _firestormCd = System.Math.Min(_firestormCd, 120);
                _descentCd = System.Math.Min(_descentCd, 240);
                if (Main.netMode != NetmodeID.Server)
                {
                    SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.9f, Pitch = 0.3f }, NPC.Center);
                    UsefulFunctions.ScreenShake(NPC.Center, 10f, 24);
                    for (int i = 0; i < 60; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Circular(6f, 6f);
                        int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                        Dust d = Dust.NewDustPerfect(NPC.Center, type, vel, 40, default, 2f);
                        d.noGravity = true;
                    }
                }
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Gwyn.Fury"), 255, 120, 40);
                NPC.netUpdate = true;
            }

            //Permanent white-hot aura once ignited
            if (_wrathActive)
            {
                if (Main.rand.NextBool(2))
                {
                    Vector2 pos = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                    int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                    Dust d = Dust.NewDustPerfect(pos, type, new Vector2(0f, -1.5f), 60, default, 1.4f);
                    d.noGravity = true;
                }
                Lighting.AddLight(NPC.Center, 1f, 0.7f, 0.25f);
            }
        }

        // ── Flash Step (sunlight teleport — connective pressure) ─────────────────
        // Only from a free (non-attacking) phase: a burst of sunlight, and he reappears just behind
        // the player so back-turning is punished. Flows straight into the base combo AI from there.
        int _flashStepCd = 420;

        void TickFlashStep()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (_flashStepCd > 0)
            {
                _flashStepCd--;
                return;
            }
            if (Phase != AttackPhase.Idle && Phase != AttackPhase.CasualStroll)
            {
                return; // never mid-attack
            }
            Player player = Main.player[NPC.target];
            float dist = NPC.Distance(player.Center);
            if (!player.dead && player.active && dist > 300f && dist < 1000f && Main.rand.NextBool(90))
            {
                _flashStepCd = 420 + Main.rand.Next(240);
                SetAttackLabel("Flash Step", 40);
                FlashBurst(NPC.Center);
                float destX = player.Center.X - player.direction * 70f; // reappear just behind the player
                NPC.Bottom = new Vector2(destX, player.Bottom.Y);
                NPC.direction = player.Center.X > NPC.Center.X ? 1 : -1;
                NPC.spriteDirection = NPC.direction;
                FlashBurst(NPC.Center);
                NPC.netUpdate = true;
            }
        }

        void FlashBurst(Vector2 pos)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }
            SoundEngine.PlaySound(SoundID.Item8 with { Pitch = 0.3f }, pos);
            for (int i = 0; i < 24; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(7f, 7f);
                int type = Main.rand.NextBool() ? DustID.GoldFlame : DustID.Electric;
                Dust d = Dust.NewDustPerfect(pos, type, vel, 40, default, 1.6f);
                d.noGravity = true;
            }
        }

        // ── Greatsword Boomerang (full-lane reach — the edge-camper punish) ──────
        // Base Boomerang template: overhead wind-up + chop; the fire event hurls the blade spinning
        // across the arena, and it arcs back to his hand. While it flies he's briefly weaponless —
        // the punish window if you're close enough to use it.
        protected override bool  CanBoomerang               => true;
        protected override float BoomerangMinRange          => 200f;
        protected override float BoomerangMaxRange          => 950f;
        protected override int   BoomerangChance            => 7;
        protected override int   BoomerangCooldownAfterUse  => 420;
        protected override int   BoomerangSwingTelegraphTicks => 26;
        protected override int   BoomerangSwingTicks        => 28;
        protected override float BoomerangFireProgress      => 0.5f;
        protected override int   BoomerangRecoveryTicks     => 60;

        const int BoomerangDamage = 65;

        protected override void DoBoomerangSwingTick(int elapsed, int total)
        {
            if (Main.dedServ)
            {
                return;
            }
            float swingT = total > 0 ? elapsed / (float)total : 1f;
            float angle = MathHelper.Lerp(MathHelper.ToRadians(-100f), MathHelper.ToRadians(70f), swingT);
            Vector2 bladePos = NPC.Center + new Vector2(NPC.direction, 0f).RotatedBy(angle) * 48f;
            int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
            Dust d = Dust.NewDustPerfect(bladePos + Main.rand.NextVector2Circular(6f, 6f), type, Vector2.Zero, 70, default, Main.rand.NextFloat(1.1f, 1.6f));
            d.noGravity = true;
        }

        protected override void DoBoomerangFire()
        {
            SetAttackLabel("Greatsword Boomerang", 130);
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = -0.5f }, NPC.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            Player target = Main.player[NPC.target];
            Vector2 origin = NPC.Center + new Vector2(NPC.direction * 26f, -14f);
            Vector2 vel = (target.Center - origin).SafeNormalize(new Vector2(NPC.direction, 0f)) * 15f;
            Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, vel,
                ModContent.ProjectileType<Projectiles.Enemy.GwynGreatswordBoomerang>(), BoomerangDamage, 6f, Main.myPlayer, NPC.whoAmI, 50f);
        }

        // ── Judgment from Behind (the back-turn punish) ──────────────────────────
        // Fires specifically when the player is at range with their back to him: a flash-step to
        // their blind side, then a queued heavy overhead the instant the combo system can start
        // (see the _judgmentPending branch in ReactiveComboIndex). The appear-flash is the warning.
        int _judgmentCd = 600;
        int _judgmentPending;

        void TickJudgment()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (_judgmentCd > 0)
            {
                _judgmentCd--;
                return;
            }
            if (Phase != AttackPhase.Idle && Phase != AttackPhase.CasualStroll)
            {
                return;
            }
            Player player = Main.player[NPC.target];
            float dist = NPC.Distance(player.Center);
            bool facingAway = System.Math.Sign(NPC.Center.X - player.Center.X) != player.direction;
            if (!player.dead && player.active && facingAway && dist > 380f && dist < 1100f && Main.rand.NextBool(100))
            {
                _judgmentCd = 700 + Main.rand.Next(300);
                SetAttackLabel("Judgment from Behind", 110);
                FlashBurst(NPC.Center);
                float destX = player.Center.X - player.direction * 90f; //their blind side
                NPC.Bottom = new Vector2(destX, player.Bottom.Y);
                NPC.direction = player.Center.X > NPC.Center.X ? 1 : -1;
                NPC.spriteDirection = NPC.direction;
                FlashBurst(NPC.Center);
                _judgmentPending = 90; //the reactive hook converts this into a guaranteed Guillotine
                NPC.netUpdate = true;
            }
        }

        // ── Riposte Stance (the greedy-trade punish; ranged players are immune) ──
        // He drops into a shimmering guard for ~40 ticks. Melee-striking him during the window takes
        // 50% damage and triggers the PARRY: a clang, a flash, and a devastating counter-swing.
        // If nobody takes the bait, the stance simply ends.
        int _riposteCd = 700;
        int _riposteTimer;

        void TickRiposte()
        {
            if (_riposteTimer > 0)
            {
                _riposteTimer--;
                NPC.velocity.X *= 0.7f;
                //The guard shimmer: a sheen of gold glints along the raised blade
                if (Main.netMode != NetmodeID.Server)
                {
                    Vector2 bladePos = NPC.Center + new Vector2(NPC.direction * 16f, -26f) + Main.rand.NextVector2Circular(6f, 22f);
                    Dust d = Dust.NewDustPerfect(bladePos, DustID.GoldCoin, Vector2.Zero, 0, default, Main.rand.NextFloat(0.8f, 1.2f));
                    d.noGravity = true;
                    d.velocity *= 0.2f;
                }
                Lighting.AddLight(NPC.Center, 0.5f, 0.45f, 0.2f);
                return;
            }
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (_riposteCd > 0)
            {
                _riposteCd--;
                return;
            }
            if (Phase != AttackPhase.Idle && Phase != AttackPhase.CasualStroll)
            {
                return;
            }
            Player player = Main.player[NPC.target];
            if (!player.dead && player.active && NPC.Distance(player.Center) < 220f && Main.rand.NextBool(120))
            {
                _riposteCd = 800 + Main.rand.Next(400);
                _riposteTimer = 40;
                SetAttackLabel("Riposte Stance", 60);
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.5f, Pitch = 0.6f }, NPC.Center); //the sheen cue
                EnterPhase(AttackPhase.NovaRecovery, 44); //park the combat machine for the stance
                NPC.netUpdate = true;
            }
        }

        void TriggerRiposte()
        {
            _riposteTimer = 0;
            SetAttackLabel("RIPOSTE!", 70);
            SoundEngine.PlaySound(SoundID.NPCHit4 with { Volume = 0.9f, Pitch = 0.5f }, NPC.Center); //the parry clang
            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 26; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(6f, 6f);
                    int type = Main.rand.NextBool() ? DustID.GoldCoin : DustID.GoldFlame;
                    Dust d = Dust.NewDustPerfect(NPC.Center, type, vel, 40, default, 1.5f);
                    d.noGravity = true;
                }
            }
            //The devastating counter: a full-reach swing + a heavy fire crescent
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = -0.4f }, NPC.Center);
            TryMeleeHit(reach: 130f);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spawn = NPC.Center + new Vector2(NPC.direction * 24f, -8f);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), spawn, Vector2.Zero,
                    ModContent.ProjectileType<Projectiles.Enemy.GwynFireArc>(), (int)(MeleeDamage * 0.9f), 4f, Main.myPlayer, NPC.direction, 0f);
            }
        }

        ///<summary>The Riposte guard soaks half of what hits it — the parry read.</summary>
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            if (_riposteTimer > 0)
            {
                modifiers.FinalDamage *= 0.5f;
            }
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByItem(player, item, hit, damageDone);
            if (_riposteTimer > 0)
            {
                TriggerRiposte();
            }
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByProjectile(projectile, hit, damageDone);
            //Only melee-class projectiles (spear thrusts, true-melee extensions) spring the trap
            if (_riposteTimer > 0 && projectile.DamageType.CountsAsClass(DamageClass.Melee))
            {
                TriggerRiposte();
            }
        }

        // ── Sunlight Spear Storm (the airborne bullet-hell escalation of the volley) ──
        // He ascends on his wings (angel wings; flame wings once the Wrath ignites) and hangs aloft
        // while a dozen spear-nodes ring the player in three sequenced waves — each node telegraphs,
        // fires its spear, and dissipates. Landing exhausts him: the recovery is the reward.
        int _stormCd = 600;
        int _stormTimer;
        const int StormNodeDamage = 40;

        void TickSpearStorm()
        {
            if (_stormTimer > 0)
            {
                _stormTimer++;
                Player player = Main.player[NPC.target];

                //Sequenced waves of 4 nodes ringing the player, offset per wave
                if (Main.netMode != NetmodeID.MultiplayerClient && !player.dead
                    && (_stormTimer == 60 || _stormTimer == 105 || _stormTimer == 150))
                {
                    int wave = _stormTimer == 60 ? 0 : _stormTimer == 105 ? 1 : 2;
                    SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.6f, Pitch = 0.3f }, player.Center);
                    for (int i = 0; i < 4; i++)
                    {
                        float ang = MathHelper.ToRadians(wave * 30f) + MathHelper.PiOver2 * i;
                        Vector2 pos = player.Center + ang.ToRotationVector2() * 340f;
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), pos, Vector2.Zero,
                            ModContent.ProjectileType<Projectiles.Enemy.GwynSolarSpearNode>(), StormNodeDamage, 2f, Main.myPlayer, 22f);
                    }
                }
                //Radiance while he hangs aloft
                if (Main.rand.NextBool(2) && Main.netMode != NetmodeID.Server)
                {
                    int type = Main.rand.NextBool() ? DustID.GoldFlame : DustID.Electric;
                    Dust d = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(30f, 30f), type, new Vector2(0f, -1f), 60, default, 1.2f);
                    d.noGravity = true;
                }
                Lighting.AddLight(NPC.Center, 0.9f, 0.8f, 0.35f);

                if (_stormTimer == 200)
                {
                    Flight?.RequestLand();
                }
                if (_stormTimer >= 240 || player.dead)
                {
                    Flight?.RequestLand();
                    _stormTimer = 0;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        _stormCd = 1400 + Main.rand.Next(600);
                        NPC.netUpdate = true;
                    }
                }
                return;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (_stormCd > 0)
            {
                _stormCd--;
                return;
            }
            if (Phase != AttackPhase.Idle && Phase != AttackPhase.CasualStroll)
            {
                return;
            }
            Player target = Main.player[NPC.target];
            float dist = NPC.Distance(target.Center);
            if (!target.dead && target.active && dist > 250f && dist < 1000f && Main.rand.NextBool(150))
            {
                _stormTimer = 1;
                SetAttackLabel("Sunlight Spear Storm", 240);
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.9f, Pitch = -0.3f }, NPC.Center);
                EnterPhase(AttackPhase.NovaRecovery, 250); //park the ground machine for the whole storm
                Flight?.RequestTakeoff();
                NPC.netUpdate = true;
            }
        }

        // ── Gravity of the Sun (#13 — the keystone anti-kite) ────────────────────
        // He plants and a golden singularity forms at his chest: 40t of light spiralling inward (the
        // read), then a GwynGravityWell drags every player radially toward him for ~2s. Resistible by
        // holding away or rolling — but a stationary caster gets reeled straight into his melee, and
        // the reactive hook greets whoever arrives with the pressure string (_pullComboNudge).
        int _gravityCd = 500;
        int _gravityTimer;
        int _pullComboNudge;

        void TickGravity()
        {
            if (_gravityTimer > 0)
            {
                _gravityTimer++;
                NPC.velocity.X *= 0.75f; //planted

                if (_gravityTimer <= 40)
                {
                    //The singularity forming: gold spiralling tightly inward to his chest
                    float progress = _gravityTimer / 40f;
                    int count = 1 + (int)(progress * 3f);
                    for (int i = 0; i < count; i++)
                    {
                        float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                        float radius = MathHelper.Lerp(120f, 15f, progress) + Main.rand.NextFloat(15f);
                        Vector2 pos = NPC.Center + ang.ToRotationVector2() * radius;
                        int type = Main.rand.NextBool() ? DustID.GoldFlame : DustID.GoldCoin;
                        Dust d = Dust.NewDustPerfect(pos, type, (NPC.Center - pos) * 0.1f, 60, default, 1.2f);
                        d.noGravity = true;
                    }
                    if (_gravityTimer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.7f, Pitch = -0.6f }, NPC.Center);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                            ModContent.ProjectileType<Projectiles.Enemy.GwynGravityWell>(), 0, 0f, Main.myPlayer, NPC.whoAmI, 120f);
                    }
                }
                Lighting.AddLight(NPC.Center, 1f, 0.85f, 0.35f);

                if (_gravityTimer >= 160)
                {
                    _gravityTimer = 0;
                    _pullComboNudge = 90; //whoever got reeled in meets the 3-hit
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        _gravityCd = 800 + Main.rand.Next(300);
                        NPC.netUpdate = true;
                    }
                }
                return;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (_gravityCd > 0)
            {
                _gravityCd--;
                return;
            }
            if (Phase != AttackPhase.Idle && Phase != AttackPhase.CasualStroll)
            {
                return;
            }
            Player gPlayer = Main.player[NPC.target];
            float gDist = NPC.Distance(gPlayer.Center);
            if (!gPlayer.dead && gPlayer.active && gDist > 200f && gDist < 1100f && Main.rand.NextBool(130))
            {
                _gravityTimer = 1;
                SetAttackLabel("Gravity of the Sun", 170);
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.7f, Pitch = -0.7f }, NPC.Center);
                EnterPhase(AttackPhase.NovaRecovery, 165); //park the ground machine for the channel
                NPC.netUpdate = true;
            }
        }

        // ── Unbroken Advance (#15 — anti-knockback / anti-stunlock) ──────────────
        // Not a dash: a slow, relentless, ARMORED march (999 defense — see TickDefenseRing) straight
        // at the player for 3 seconds, fire boiling off him, contact damage live (see the NPC.damage
        // line in AI). No winded recovery: the moment the march ends the combat machine is free, so
        // it flows straight into whatever attack the player's position deserves.
        int _advanceCd = 600;
        int _advanceTimer;
        int _advanceWallTicks;

        void TickAdvance()
        {
            if (_advanceTimer > 0)
            {
                _advanceTimer--;
                Player player = Main.player[NPC.target];
                int dir = player.Center.X > NPC.Center.X ? 1 : -1;
                NPC.direction = dir;
                NPC.spriteDirection = dir;
                NPC.velocity.X = dir * 2.2f;

                //Fire boiling off him + the scorch line his blade drags
                if (Main.netMode != NetmodeID.Server)
                {
                    if (Main.rand.NextBool(2))
                    {
                        Vector2 pos = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                        int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                        Dust d = Dust.NewDustPerfect(pos, type, new Vector2(0f, -1.8f), 60, default, 1.4f);
                        d.noGravity = true;
                    }
                    Vector2 scorch = NPC.Bottom + new Vector2(-dir * 20f, -4f);
                    Dust s = Dust.NewDustPerfect(scorch, DustID.Torch, new Vector2(0f, -0.6f), 100, default, 1f);
                    s.noGravity = true;
                }
                if (_advanceTimer % 20 == 0)
                {
                    SoundEngine.PlaySound(SoundID.DeerclopsStep with { Volume = 0.35f, Pitch = 0.3f }, NPC.Bottom);
                }
                Lighting.AddLight(NPC.Center, 0.9f, 0.5f, 0.15f);

                //Walked into the arena wall long enough — the march ends early
                if (NPC.collideX)
                {
                    if (++_advanceWallTicks > 30)
                    {
                        _advanceTimer = 0;
                    }
                }
                else
                {
                    _advanceWallTicks = 0;
                }

                if (_advanceTimer == 0 || player.dead)
                {
                    _advanceTimer = 0;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        _advanceCd = 900 + Main.rand.Next(300);
                        NPC.netUpdate = true;
                    }
                }
                return;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (_advanceCd > 0)
            {
                _advanceCd--;
                return;
            }
            if (Phase != AttackPhase.Idle && Phase != AttackPhase.CasualStroll)
            {
                return;
            }
            Player aPlayer = Main.player[NPC.target];
            float aDist = NPC.Distance(aPlayer.Center);
            if (!aPlayer.dead && aPlayer.active && aDist > 250f && aDist < 900f && Main.rand.NextBool(140))
            {
                _advanceTimer = 180; //3 seconds of relentless
                _advanceWallTicks = 0;
                SetAttackLabel("Unbroken Advance", 190);
                if (Main.netMode != NetmodeID.Server)
                {
                    for (int i = 0; i < 24; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Circular(4f, 4f);
                        Dust d = Dust.NewDustPerfect(NPC.Center, DustID.Torch, vel, 40, default, 1.7f);
                        d.noGravity = true;
                    }
                }
                //The march + a beat: expiring lands him in Idle right as the march ends, so he can
                //combo into ANY attack immediately — no winded window.
                EnterPhase(AttackPhase.NovaRecovery, 182);
                NPC.netUpdate = true;
            }
        }

        // ── Winged Plunge (the wings' second act) ────────────────────────────────
        // He spreads his wings and rises (dome-aware: the ascent caps itself below any ceiling it
        // finds), hangs a beat to aim, then DIVES at the player's marked position trailing golden
        // echoes — ending in a flaming greatsword slash where they stood. The wings only ever show
        // while airborne (ShowWingsWhenGrounded is false — his cape keeps the grounded silhouette).
        int _plungeCd = 700;
        int _plungeTimer;
        int _plungePhase;
        Vector2 _plungeTarget;
        float _plungeApexY;

        const float PlungeRiseHeight = 380f;  //default ascent — clears the dome's center, not its edges
        const float PlungeCeilingPad = 70f;   //stay this far under whatever ceiling the check finds

        void TickWingedPlunge()
        {
            if (_plungeTimer > 0)
            {
                RunWingedPlunge();
                return;
            }
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (_plungeCd > 0)
            {
                _plungeCd--;
                return;
            }
            if (Phase != AttackPhase.Idle && Phase != AttackPhase.CasualStroll)
            {
                return;
            }
            Player player = Main.player[NPC.target];
            float dist = NPC.Distance(player.Center);
            if (!player.dead && player.active && dist > 150f && dist < 900f && Main.rand.NextBool(120))
            {
                _plungeTimer = 1;
                _plungePhase = 0;
                //Dome-aware apex: rise the default height, but never within the pad of a ceiling
                float ceiling = FindCeilingY(NPC.Center, 30);
                _plungeApexY = NPC.Center.Y - PlungeRiseHeight;
                if (ceiling > 0f)
                {
                    _plungeApexY = System.Math.Max(_plungeApexY, ceiling + PlungeCeilingPad);
                }
                SetAttackLabel("Winged Plunge", 200);
                SoundEngine.PlaySound(SoundID.Item32 with { Volume = 0.6f, Pitch = 0.2f }, NPC.Center); //wingbeat whoosh
                EnterPhase(AttackPhase.NovaRecovery, 210); //park the ground machine for the flight
                Flight?.RequestTakeoff();
                NPC.netUpdate = true;
            }
        }

        void RunWingedPlunge()
        {
            _plungeTimer++;
            Player player = Main.player[NPC.target];
            if (player.dead || !player.active)
            {
                EndPlunge();
                return;
            }

            switch (_plungePhase)
            {
                case 0: //Rise on the wings (overriding the flight controller's own intent)
                    NPC.velocity = new Vector2(NPC.velocity.X * 0.8f, -6.5f);
                    if (Main.rand.NextBool(2) && Main.netMode != NetmodeID.Server)
                    {
                        Dust d = Dust.NewDustPerfect(NPC.Bottom + Main.rand.NextVector2Circular(14f, 6f), DustID.GoldFlame, new Vector2(0f, 2f), 80, default, 1.2f);
                        d.noGravity = true;
                    }
                    if (NPC.Center.Y <= _plungeApexY || _plungeTimer > 70)
                    {
                        _plungePhase = 1;
                        _plungeTimer = 1;
                    }
                    break;

                case 1: //Hang and aim — the read
                    NPC.velocity *= 0.85f;
                    NPC.direction = player.Center.X > NPC.Center.X ? 1 : -1;
                    NPC.spriteDirection = NPC.direction;
                    if (_plungeTimer >= 20)
                    {
                        _plungeTarget = player.Center; //locked — reposition NOW
                        _plungePhase = 2;
                        _plungeTimer = 1;
                        SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.6f, Pitch = 0.4f }, NPC.Center);
                    }
                    break;

                case 2: //The dive, trailing golden echoes
                {
                    Vector2 dir = (_plungeTarget - NPC.Center).SafeNormalize(Vector2.UnitY);
                    NPC.velocity = dir * 17f;
                    if (_plungeTimer % 2 == 0 && Main.netMode != NetmodeID.Server)
                    {
                        //Echo: a body-sized puff of gold left hanging along the dive path
                        for (int i = 0; i < 5; i++)
                        {
                            Vector2 pos = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                            int type = Main.rand.NextBool() ? DustID.GoldFlame : DustID.Torch;
                            Dust d = Dust.NewDustPerfect(pos, type, Vector2.Zero, 80, default, 1.5f);
                            d.noGravity = true;
                            d.velocity = dir * -0.5f;
                        }
                    }
                    Lighting.AddLight(NPC.Center, 1f, 0.8f, 0.3f);

                    bool arrived = Vector2.Distance(NPC.Center, _plungeTarget) < 48f;
                    if (arrived || NPC.collideX || NPC.collideY || _plungeTimer > 55)
                    {
                        //The flaming slash at the marked position
                        SetAttackLabel("Winged Plunge — Slash", 60);
                        SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = -0.4f }, NPC.Center);
                        SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.6f, Pitch = 0.1f }, NPC.Center);
                        UsefulFunctions.ScreenShake(NPC.Center, 7f, 14);
                        if (Main.netMode != NetmodeID.Server)
                        {
                            for (int i = 0; i < 22; i++)
                            {
                                Vector2 vel = Main.rand.NextVector2Circular(5f, 5f);
                                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                                Dust d = Dust.NewDustPerfect(NPC.Center, type, vel, 40, default, 1.6f);
                                d.noGravity = true;
                            }
                        }
                        TryMeleeHit(reach: 140f);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 spawn = NPC.Center + new Vector2(NPC.direction * 24f, -8f);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), spawn, Vector2.Zero,
                                ModContent.ProjectileType<Projectiles.Enemy.GwynFireArc>(), (int)(MeleeDamage * 0.7f), 4f, Main.myPlayer, NPC.direction, 2f);
                            for (int direction = -1; direction <= 1; direction += 2)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Bottom - new Vector2(0f, 20f), Vector2.Zero,
                                    ModContent.ProjectileType<Projectiles.Enemy.GwynGroundFireWave>(), (int)(MeleeDamage * 0.55f), 5f,
                                    Main.myPlayer, direction, 20f);
                            }
                        }
                        Flight?.RequestLand();
                        _plungePhase = 3;
                        _plungeTimer = 1;
                    }
                    break;
                }

                case 3: //Landing recovery
                    NPC.velocity.X *= 0.8f;
                    if (_plungeTimer >= 40)
                    {
                        EndPlunge();
                    }
                    break;
            }
        }

        void EndPlunge()
        {
            Flight?.RequestLand();
            _plungeTimer = 0;
            _plungePhase = 0;
            NPC.noGravity = false;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                _plungeCd = 800 + Main.rand.Next(400);
                NPC.netUpdate = true;
            }
        }

        ///<summary>World Y of the first solid tile BOTTOM above the point (scanning up), or -1 if none
        ///in range — the dome-clearance check for the Winged Plunge's ascent.</summary>
        static float FindCeilingY(Vector2 worldPos, int maxTilesUp)
        {
            int tx = (int)(worldPos.X / 16f);
            int ty = (int)(worldPos.Y / 16f);
            if (tx < 5 || tx > Main.maxTilesX - 5)
            {
                return -1f;
            }
            for (int d = 2; d <= maxTilesUp; d++)
            {
                int y = ty - d;
                if (y <= 5)
                {
                    break;
                }
                Tile tile = Main.tile[tx, y];
                if (tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType])
                {
                    return (y + 1) * 16f;
                }
            }
            return -1f;
        }

        // ── Firestorm (summoned rain — the anti-heal / anti-camp pressure) ───────
        // Not a swing: it's channeled magic that rains fireballs over the player's area for a couple
        // seconds while Gwyn keeps pursuing, so standing still to heal is answered. Own cooldown.
        int _firestormCd = 480;
        const int FirestormDamage = 45;

        void TickFirestorm()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (_firestormCd > 0)
            {
                _firestormCd--;
                return;
            }
            Player player = Main.player[NPC.target];
            if (!player.dead && player.active && NPC.Distance(player.Center) > 400f && Main.rand.NextBool(160))
            {
                _firestormCd = 900 + Main.rand.Next(300);
                SetAttackLabel("Firestorm", 190);
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Gwyn.RainsDeath"), 235, 130, 40);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<Projectiles.Enemy.GwynFirestorm>(), 0, 0f, Main.myPlayer, FirestormDamage, 150f);
            }
        }

        // ── Descent of the Sun (the epic set-piece) ──────────────────────────────
        // Summoned magic: a meteor of sunlight hangs high over the player, marks its impact, then
        // crashes with a huge explosion + crater fireballs. Long cooldown — it's the "oh no" moment.
        int _descentCd = 900;
        const int DescentDamage = 80;

        void TickDescent()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (_descentCd > 0)
            {
                _descentCd--;
                return;
            }
            Player player = Main.player[NPC.target];
            if (!player.dead && player.active && Main.rand.NextBool(220))
            {
                _descentCd = 1500 + Main.rand.Next(600);
                SetAttackLabel("Descent of the Sun", 130);
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Gwyn.Fury"), 255, 200, 60);
                Vector2 spawn = new Vector2(player.Center.X, player.Center.Y - 640f);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), spawn, Vector2.Zero,
                    ModContent.ProjectileType<Projectiles.Enemy.GwynDescentMeteor>(), DescentDamage, 0f, Main.myPlayer, DescentDamage, player.Center.X);
            }
        }

        // ── Weapon-swing hooks. Phase 2 drives the bespoke reactive greatsword moveset above; the
        //    magic set-pieces (lightning spear etc.) layer on in Phase 3. ──────────────────────────
        protected override void DoMeleeAttack()
        {
            ArmFireSlash(0.5f, ComboReachBase * 0.7f);
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, PitchVariance = 0.2f }, NPC.Center);
            TryMeleeHit();
        }

        ///<summary>Each swing trails cinder-fire; the heavier ones (reach ≥ 1.15) fling a fire
        ///crescent forward so melee reaches a tile or two past the blade.</summary>
        protected override void OnMeleeComboAttackTick(
            MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            float progress = elapsed / (float)Math.Max(1, total - 1);
            float bladeReach = ComboReachBase * 0.7f * step.ReachMult;
            ArmFireSlash(progress, bladeReach);
            EmitSwingFireDust(bladeReach, elapsed);

            // Leap hits resolve on landing; emitting their fire at takeoff would contradict the
            // telegraph. All other attacks shed cinders as the authored blade crosses forward.
            if (step.Motion != ComboMotion.LeapSlam && elapsed == total / 2)
            {
                EmitComboCinders(step);
            }
        }

        bool _drawFireSlash;
        float _fireSlashProgress;
        float _fireSlashReach;
        static Asset<Effect> fireSlashEffect;
        static Asset<Texture2D> fireSlashTexture;
        static Asset<Texture2D> fireSlashNoise;

        void ArmFireSlash(float progress, float reach)
        {
            _drawFireSlash = true;
            _fireSlashProgress = MathHelper.Clamp(progress, 0f, 1f);
            _fireSlashReach = Math.Max(24f, reach);
        }

        void EmitSwingFireDust(float bladeReach, int elapsed)
        {
            if (Main.netMode == NetmodeID.Server || elapsed % 2 != 0)
                return;

            Vector2 hand = PuppetHandPosition;
            Vector2 tip = PuppetWeaponTipPosition(bladeReach);
            Vector2 direction = (tip - hand).SafeNormalize(new Vector2(NPC.direction, 0f));
            for (int i = 0; i < 3; i++)
            {
                Vector2 position = Vector2.Lerp(hand, tip, Main.rand.NextFloat(0.38f, 1f))
                    + Main.rand.NextVector2Circular(6f, 6f);
                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                Dust dust = Dust.NewDustPerfect(position, type,
                    direction * Main.rand.NextFloat(1.2f, 2.8f) + Main.rand.NextVector2Circular(0.8f, 0.8f),
                    60, default, Main.rand.NextFloat(1.05f, 1.55f));
                dust.noGravity = true;
            }
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!_drawFireSlash || _fireSlashReach <= 0f)
                return;

            fireSlashEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GwynCinderTrail", AssetRequestMode.ImmediateLoad);
            fireSlashTexture ??= ModContent.Request<Texture2D>(
                "tsorcRevamp/Items/Weapons/Melee/Broadswords/BroadswordRework/Common/Melee/Slash",
                AssetRequestMode.ImmediateLoad);
            fireSlashNoise ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/T_VFX_NoiseF1", AssetRequestMode.ImmediateLoad);

            Texture2D texture = fireSlashTexture.Value;
            var frame = new SpriteFrame(1, 3) { CurrentRow = (byte)Math.Min(2, (int)(_fireSlashProgress * 3f)) };
            Rectangle source = frame.GetSourceRectangle(texture);
            Vector2 hand = PuppetHandPosition;
            Vector2 tip = PuppetWeaponTipPosition(_fireSlashReach);
            Vector2 direction = (tip - hand).SafeNormalize(new Vector2(NPC.direction, 0f));
            Vector2 position = NPC.Center + direction * 3f - Main.screenPosition;
            float rotation = direction.ToRotation();
            float scale = _fireSlashReach / 30f * 0.88f * PuppetDrawScale;
            SpriteEffects effects = NPC.direction > 0 ? SpriteEffects.FlipVertically : SpriteEffects.None;
            float envelope = (float)Math.Sin(_fireSlashProgress * MathHelper.Pi);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Effect effect = fireSlashEffect.Value;
            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];
            try
            {
                graphicsDevice.Textures[1] = fireSlashNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                effect.CurrentTechnique = effect.Techniques["GwynCinderBlade"];
                effect.Parameters["CinderColor"].SetValue(new Color(255, 32, 2).ToVector3());
                effect.Parameters["FlameColor"].SetValue(new Color(255, 126, 12).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 238, 174).ToVector3());
                effect.Parameters["Opacity"].SetValue(0.9f * envelope);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["PrimaryTextureSize"].SetValue(texture.Size());
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(texture, position, source, Color.White * 0.42f, rotation,
                    source.Size() * 0.5f, scale * 1.12f, effects, 0);
                Main.EntitySpriteDraw(texture, position, source, Color.White, rotation,
                    source.Size() * 0.5f, scale, effects, 0);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
            }
            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
        }
        protected override void OnComboStepCompleted(MeleeComboStep step)
        {
            // Only a real landing gets the leap's impact crescent. A timed-out airborne leap keeps
            // its normal recovery without producing a disconnected ground effect.
            if (step.Motion == ComboMotion.LeapSlam && NPC.velocity.Y == 0f)
            {
                EmitComboCinders(step);
            }
        }

        void EmitComboCinders(MeleeComboStep step)
        {

            // Fire dust follows the same authored hand-to-tip line as collision and slash VFX.
            if (Main.netMode != NetmodeID.Server)
            {
                float bladeReach = ComboReachBase * 0.7f * step.ReachMult;
                Vector2 bladeTip = PuppetWeaponTipPosition(bladeReach);
                for (int i = 0; i < 5; i++)
                {
                    int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                    int dust = Dust.NewDust(bladeTip - new Vector2(8f, 20f), 16, 40, type, NPC.direction * 1.5f, 0f, 60, default, 1.4f);
                    Main.dust[dust].noGravity = true;
                }
            }

            // Heavy swings throw a reach crescent (vertical bias tilts it for overhead/rising arcs).
            if (step.ReachMult >= 1.15f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float vBias = step.Motion switch
                {
                    ComboMotion.OverheadArc => 3f,
                    ComboMotion.GroundSlam => 3f,
                    ComboMotion.UnderhandArc => -3f,
                    _ => 0f,
                };
                float bladeReach = ComboReachBase * 0.7f * step.ReachMult;
                Vector2 bladeTip = PuppetWeaponTipPosition(bladeReach);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), bladeTip, Vector2.Zero,
                    ModContent.ProjectileType<Projectiles.Enemy.GwynFireArc>(), (int)(MeleeDamage * step.DamageMult * 0.6f), 2f, Main.myPlayer, NPC.direction, vBias);
            }
        }

        protected override void DoRangedAttack()
        {
            // No ranged weapon; the fire/lightning magic will be its own state-machine attacks.
        }

        // ── Spear of the First Sun (the marquee lightning attack) ────────────────
        // Mapped onto the base HomingVolley template — "raise the weapon overhead, then fire a
        // projectile partway through the downswing" is exactly the throw motion. The two-stage
        // payload (contact explosion → delayed ground bolt → floor electricity) lives entirely in
        // the GwynLightningSpear → GwynLightningStrike → GwynFloorSpark projectile chain.
        protected override bool  CanHomingVolley             => true;
        protected override float HomingVolleyMinRange         => 200f;
        protected override float HomingVolleyMaxRange         => 1400f;   // works at nearly any range — it's a signature
        protected override int   HomingVolleyChance           => 8;
        protected override int   HomingVolleyCooldownAfterUse => 420;
        protected override int   HomingVolleyDodgebackTicks   => 8;       // barely a step — the raise is planted, imposing
        protected override float HomingVolleyDodgebackSpeed   => 1.5f;
        protected override int   HomingVolleySwingTelegraphTicks => 40;   // the long overhead raise — everyone sees it coming
        protected override int   HomingVolleySwingTicks       => 28;
        protected override float HomingVolleyFireProgress     => 0.55f;   // hurl it partway through the downswing
        protected override int   HomingVolleyRecoveryTicks    => 45;
        protected override bool  UseRaisedHomingVolleyHoldoutPose => true;

        const int LightningSpearDamage = 50;

        protected override bool DrawSpecialHeldWeapon(ref PlayerDrawSet drawInfo)
        {
            bool bareHandGrasp = Phase == AttackPhase.TendrilTelegraph
                || Phase == AttackPhase.TendrilReach;
            if (bareHandGrasp)
                return true;

            bool spearPhase = Phase == AttackPhase.HomingVolleyDodgeback
                || Phase == AttackPhase.HomingVolleySwingTelegraph
                || Phase == AttackPhase.HomingVolleySwing;
            if (!spearPhase)
                return false;

            if (Phase == AttackPhase.HomingVolleySwing)
            {
                float release = HomingVolleySwingTicks > 0
                    ? 1f - PhaseTimer / (float)HomingVolleySwingTicks
                    : 1f;
                if (release >= HomingVolleyFireProgress)
                    return true;
            }

            Player target = Main.player[NPC.target];
            Vector2 hand = PuppetHandPosition;
            Vector2 aim = target != null && target.active && !target.dead
                ? (target.Center - hand).SafeNormalize(new Vector2(NPC.direction, 0f))
                : new Vector2(NPC.direction, 0f);

            Texture2D texture = ModContent.Request<Texture2D>(
                "tsorcRevamp/Projectiles/Enemy/Gwyn/GwynLightningSpear").Value;
            int frameHeight = texture.Height / GwynLightningSpearFrames.FrameCount;
            int frameIndex = (int)(Main.GameUpdateCount / 5UL) % GwynLightningSpearFrames.FrameCount;
            Rectangle frame = new Rectangle(0, frameIndex * frameHeight, texture.Width, frameHeight);
            Vector2 origin = GwynLightningSpearFrames.GetVisualOrigin(frameIndex);
            //Rotation already points the authored spear toward either aim direction. Mirroring it
            //on right-facing holds reverses the spearhead, so the held version never needs a flip.
            SpriteEffects effects = SpriteEffects.None;

            drawInfo.DrawDataCache.Add(new DrawData(
                texture, hand - Main.screenPosition, frame, Color.White, aim.ToRotation(), origin,
                NPC.scale * 0.6f, effects, 0));
            return true;
        }

        ///<summary>Lightning gathers on the raised blade through the windup — the telegraph read.</summary>
        protected override void DoHomingVolleySwingTick(int elapsed, int total)
        {
            if (Main.dedServ)
            {
                return;
            }
            if (total > 0 && elapsed >= total * HomingVolleyFireProgress)
            {
                return;
            }
            Player target = Main.player[NPC.target];
            Vector2 hand = PuppetHandPosition;
            Vector2 aim = target != null && target.active && !target.dead
                ? (target.Center - hand).SafeNormalize(new Vector2(NPC.direction, 0f))
                : new Vector2(NPC.direction, 0f);
            Vector2 bladePos = hand + aim * 42f;
            for (int i = 0; i < 2; i++)
            {
                int type = Main.rand.NextBool() ? DustID.GoldFlame : DustID.Electric;
                Dust d = Dust.NewDustPerfect(bladePos + Main.rand.NextVector2Circular(8f, 8f), type, Vector2.Zero, 60, default, Main.rand.NextFloat(1.2f, 1.8f));
                d.noGravity = true;
            }
            Lighting.AddLight(bladePos, 0.7f, 0.6f, 0.25f);
        }

        ///<summary>Hurl the lightning spear at the player's locked position.</summary>
        protected override void DoHomingVolleyFire()
        {
            SetAttackLabel("Spear of the First Sun", 120);
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.8f, Pitch = 0.1f }, NPC.Center); // lightning cast
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            Player target = Main.player[NPC.target];
            Vector2 origin = PuppetHandPosition;
            Vector2 vel = (target.Center - origin).SafeNormalize(new Vector2(NPC.direction, 0f)) * 15f;
            Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, vel,
                ModContent.ProjectileType<Projectiles.Enemy.GwynLightningSpear>(), LightningSpearDamage, 4f, Main.myPlayer);
        }

        // ── Cinder Nova (point-blank space-maker + greed punish) ─────────────────
        // Base Nova template: root, gather fire inward over the charge, then detonate the expanding
        // ring. Triggers at three HP thresholds (one-shot each) and, otherwise, occasionally when the
        // player is point-blank so hugging him during a slow moment is answered.
        protected override bool CanNova           => true;
        protected override int  NovaChargeTicks    => 60;   // shorter than Artorias' — a snappier boss beat
        protected override int  NovaBlastHoldTicks => 20;
        protected override int  NovaRecoveryTicks  => 45;

        bool _nova75, _nova45, _nova20;
        int _novaPointBlankCd;

        protected override bool ShouldTriggerNova()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return false;
            }
            float frac = (float)NPC.life / NPC.lifeMax;
            if (frac <= 0.75f && !_nova75) { _nova75 = true; return true; }
            if (frac <= 0.45f && !_nova45) { _nova45 = true; return true; }
            if (frac <= 0.20f && !_nova20) { _nova20 = true; return true; }
            //Point-blank greed punish, on its own cooldown
            if (_novaPointBlankCd > 0) { _novaPointBlankCd--; return false; }
            if (NPC.Distance(Main.player[NPC.target].Center) < 140f && Main.rand.NextBool(240))
            {
                _novaPointBlankCd = 600;
                return true;
            }
            return false;
        }

        protected override void DoNovaChargeTick(int elapsed, int total)
        {
            if (elapsed == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<Projectiles.Enemy.GwynCinderNovaTelegraph>(), 0, 0f,
                    Main.myPlayer, NPC.whoAmI, total);
            }
            if (Main.dedServ)
            {
                return;
            }
            //Fire spiralling INWARD to his blade — the classic "get away" read
            float progress = total > 0 ? elapsed / (float)total : 1f;
            int count = 2 + (int)(progress * 4f);
            for (int i = 0; i < count; i++)
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                float radius = MathHelper.Lerp(170f, 20f, progress) + Main.rand.NextFloat(20f);
                Vector2 pos = NPC.Center + ang.ToRotationVector2() * radius;
                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                Dust d = Dust.NewDustPerfect(pos, type, Vector2.Zero, 60, default, 1.3f);
                d.noGravity = true;
                d.velocity = (NPC.Center - pos) * 0.07f;
            }
            Lighting.AddLight(NPC.Center, progress * 1.2f, progress * 0.6f, progress * 0.2f);
        }

        protected override void DoNovaBlast()
        {
            SetAttackLabel("Cinder Nova", 90);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1f, Pitch = -0.3f }, NPC.Center);
            UsefulFunctions.ScreenShake(NPC.Center, 12f, 22);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                ModContent.ProjectileType<Projectiles.Enemy.GwynCinderNova>(), NovaDamage, 10f, Main.myPlayer, 420f);
        }

        const int NovaDamage = 60;

        // ── Sunlight Spears Volley (mid-range answer) ────────────────────────────
        // Base AbyssShard template: a short raise, then a sequence of "fire events". Each event drops
        // a couple of GwynSolarSpearNode orbs in an arc above the player — each orb telegraphs, then
        // fires a spear at the player. The staggered events + per-orb telegraph make a readable,
        // rolling rhythm of spears rather than a wall.
        protected override bool  CanAbyssShard              => true;
        protected override float AbyssShardMinRange         => 220f;
        protected override float AbyssShardMaxRange         => 1200f;
        protected override int   AbyssShardChance           => 8;
        protected override int   AbyssShardCooldownAfterUse => 360;
        protected override int   AbyssShardTelegraphTicks   => 25;

        const int SunlightSpearDamage = 40;
        const int SpearVolleyEvents = 3;

        protected override int NextAbyssShardDelay(int completedFireIndex)
            => completedFireIndex < SpearVolleyEvents - 1 ? 14 : -1;

        protected override void DoAbyssShardFire(int fireIndex)
        {
            SetAttackLabel("Sunlight Spears Volley", 80);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.5f, Pitch = 0.2f }, NPC.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            Player target = Main.player[NPC.target];
            //Two orbs per event, arced above the player and spread horizontally by event index
            for (int i = 0; i < 2; i++)
            {
                float spreadX = (fireIndex - 1) * 90f + (i == 0 ? -55f : 55f);
                Vector2 pos = target.Center + new Vector2(spreadX, -220f - Main.rand.NextFloat(40f));
                Projectile.NewProjectile(NPC.GetSource_FromThis(), pos, Vector2.Zero,
                    ModContent.ProjectileType<Projectiles.Enemy.GwynSolarSpearNode>(), SunlightSpearDamage, 2f, Main.myPlayer, 20f);
            }
        }

        // ── Lord's Grasp (grab — the shield-turtle / spacing punish) ─────────────
        // Base Tendril template: gather fire at the hand, then launch a reaching flaming claw that
        // seizes and immolates on contact. Bypasses block comfort — it hunts turtling players.
        protected override bool  CanTendrilGrab          => true;
        protected override float TendrilMinRange         => 150f;
        protected override float TendrilMaxRange         => 560f;
        protected override int   TendrilChance            => 5;
        protected override int   TendrilCooldownAfterUse  => 480;
        protected override int   TendrilTelegraphTicks    => 30;
        protected override int   TendrilReachTicks        => 60;

        const int GraspDamage = 70;

        protected override void DoTendrilTelegraphTick(int elapsed)
        {
            if (Main.dedServ)
            {
                return;
            }
            Vector2 handPos = NPC.Center + new Vector2(NPC.direction * 20f, -6f);
            int count = 1 + elapsed / 6;
            for (int i = 0; i < count; i++)
            {
                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                Dust d = Dust.NewDustPerfect(handPos + Main.rand.NextVector2Circular(12f, 12f), type, Main.rand.NextVector2Circular(1f, 1f), 60, default, Main.rand.NextFloat(1f, 1.5f));
                d.noGravity = true;
            }
            Lighting.AddLight(handPos, 0.8f, 0.4f, 0.1f);
        }

        protected override void DoTendrilLaunch()
        {
            SetAttackLabel("Lord's Grasp", 100);
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.7f, Pitch = -0.3f }, NPC.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            Player target = Main.player[NPC.target];
            Vector2 origin = NPC.Center + new Vector2(NPC.direction * (NPC.width * 0.5f + 10f), -NPC.height * 0.3f);
            Vector2 vel = UsefulFunctions.Aim(origin, target.Center, 13f);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, vel,
                ModContent.ProjectileType<Projectiles.Enemy.GwynFlameGrasp>(), GraspDamage, 8f, Main.myPlayer, NPC.whoAmI);
        }

        protected override void DoTendrilSwing()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, PitchVariance = 0.15f }, NPC.Center);
            TryMeleeHit(reach: 110f);
        }

        ///<summary>KEPT (1000px): the First Flame shields him from cowards — full defense lock beyond
        ///the blue ring, the old fightable value inside it.</summary>
        void TickDefenseRing()
        {
            UsefulFunctions.DustRing(NPC.Center, (int)DefenseRingRadius, DustID.BlueTorch, 20, 1f);

            if (protectedHoldTimer > 0)
            {
                protectedHoldTimer--;
            }
            if (Vector2.Distance(NPC.Center, Main.player[NPC.target].Center) > DefenseRingRadius)
            {
                NPC.defense = 9999;
                if (protectedHoldTimer <= 0)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Gwyn.Protected"), 175, 75, 255);
                    protectedHoldTimer = 200;
                }
            }
            else if (protectedHoldTimer <= 0) // preserve the 9999 penalty until the hold expires
            {
                //Unbroken Advance: the march itself is armored — 999 defense until the 3s ends
                NPC.defense = _advanceTimer > 0 ? 999 : FightableDefense;
            }
        }

        ///<summary>KEPT (2000px): the outer flame wall. Flight is torn down anywhere inside it;
        ///fleeing beyond it brings the Coward's Affliction after a 90-tick grace.</summary>
        void TickCowardRing()
        {
            Player player = Main.player[NPC.target];

            if (NPC.Distance(player.Center) < CowardRingRadius)
            {
                player.AddBuff(ModContent.BuffType<TornWings>(), 60, false);
            }

            UsefulFunctions.DustRing(NPC.Center, (int)CowardRingRadius, DustID.RedsWingsRun, 1, 1f);
            UsefulFunctions.DustRing(NPC.Center, (int)CowardRingRadius, DustID.Torch, 10, 1f);
            UsefulFunctions.DustRing(NPC.Center, (int)CowardRingRadius, DustID.RedTorch, 5, 2f);
            UsefulFunctions.DustRing(NPC.Center, (int)CowardRingRadius, DustID.Firefly, 100, -3f);

            if (NPC.Distance(player.Center) > CowardRingRadius)
            {
                cowardGraceTimer--;
                if (cowardGraceTimer <= 0)
                {
                    player.AddBuff(ModContent.BuffType<CowardsAffliction>(), 1 * 30, false);
                    if (!announcedCoward)
                    {
                        UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Gwyn.Coward"), 235, 199, 23); //deep yellow
                        announcedCoward = true;
                    }
                }
            }
            else
            {
                cowardGraceTimer = 90;
                announcedCoward = false;
            }
        }

        ///<summary>KEPT (&gt;600px): running keeps you under a random rain of death orbs falling from
        ///above your own position — ranged builds don't get a free fight.</summary>
        void TickRainOfDeath()
        {
            Player player = Main.player[NPC.target];
            if (player.dead || !player.active)
            {
                return;
            }

            if (NPC.Distance(player.Center) > RainOfDeathRange && Main.rand.NextBool(400))
            {
                SpawnDeathRain(player, 3, -100f, 200, -600f, 2f);
            }

            if (NPC.life >= NPC.lifeMax / 10 * 2 && NPC.Distance(player.Center) > 650f && Main.rand.NextBool(180))
            {
                if (Main.rand.NextBool(20))
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Gwyn.RainsDeath"), 175, 75, 255);
                }

                SpawnDeathRain(player, 6, -600f, 600, -650f, 1f);
            }

            if (NPC.life <= NPC.lifeMax / 10 * 2 && NPC.Distance(player.Center) > 580f && NPC.Distance(player.Center) < 1199f && Main.rand.NextBool(120))
            {
                if (Main.rand.NextBool(16))
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Gwyn.TidalWave"), 175, 75, 255);
                }

                SpawnDeathRain(player, 8, -800f, 800, -650f, 1f);
            }
        }

        void SpawnDeathRain(Player player, int count, float horizontalOffset, int horizontalRange, float verticalOffset, float knockback)
        {
            for (int i = 0; i < count; i++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Vortex, NPC.velocity.X, NPC.velocity.Y);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(),
                        player.position.X + horizontalOffset + Main.rand.Next(horizontalRange), player.position.Y + verticalOffset,
                        (-50 + Main.rand.Next(100)) / 10f, 0.5f,
                        ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathBall>(), RainOfDeathDamage, knockback, Main.myPlayer);
                }
                Lighting.AddLight(NPC.Center, Color.White.ToVector3());
                SoundEngine.PlaySound(SoundID.Zombie53 with { Volume = 0.3f, Pitch = 0.1f }, NPC.Center);
            }
        }

        ///<summary>KEPT (700px): standing in the Lord's presence — no wings, no grapple.</summary>
        void TickProximityDebuffs()
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead)
                {
                    continue;
                }
                if (NPC.Distance(player.Center) < ProximityDebuffRange)
                {
                    player.AddBuff(ModContent.BuffType<TornWings>(), 1 * 60, false);
                    player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 1 * 60, false);
                }
            }
        }

        //Phase 2: the 12-attack state machine, boss bag + SwordOfGwyn drop, BossChecklist entry,
        //and the spawn gating land once the attack proposal is approved.

#region Gore
        public override void OnKill()
        {
            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));

            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gwyn Gore 1").Type, 1.5f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gwyn Gore 2").Type, 1.5f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gwyn Gore 3").Type, 1.5f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gwyn Gore 2").Type, 1.5f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gwyn Gore 3").Type, 1.5f);
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.BossDeath>(), 0, 0, Main.myPlayer, 3, UsefulFunctions.ColorToFloat(Color.Orange));

            }
            tsorcRevampWorld.InitiateTheEnd();
        }
        #endregion
    }
}
