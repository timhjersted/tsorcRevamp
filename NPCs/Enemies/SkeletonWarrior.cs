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
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    /// <summary>
    /// Skeleton Warrior: a plain sword-and-board skeleton for the pre-Hardmode caverns, jungle and graveyards. It walks
    /// at you behind a raised Cobalt Shield (80% off frontal hits) and fights with two Cutlass cuts: the close Overhead
    /// Cut and the mid-range Rising Lunge Cut. A stagger knocks the shield out of its hand for good. Its first death
    /// leaves a bone pile that reassembles (SkeletonPuppet); a magic killing blow keeps it down.
    ///
    /// Multiplayer: combo picks, both distance gates, the guard and the shield loss are server decisions; the combo,
    /// phase and guard ride the PuppetNPC payload, the pile rides SkeletonPuppet's, and _shieldLost is written after.
    /// The falling-shield animation, dust and sounds are per machine.
    /// </summary>
    public class SkeletonWarrior : SkeletonPuppet
    {
        // ── Damage: declared Expert damage before defense, converted once by EnemyDamage ──────────────────────
        public const int CutExpertDamage = 72;
        public const int CutExpertDamageHardmode = 120;
        // Lunge damage relative to the cut: 65 pre-Hardmode, 108 in Hardmode.
        const float LungeDamageFraction = 0.9f;

        // ── Cutlass geometry, measured from the vanilla 40x48 Item_672: pommel (2,45), basket guard y28-43, tip (37,1).
        //   The hand sits in the basket at (9.5, 39): 46.9px grip -> tip at scale 1. Hitbox reach = 67 x 0.7 = 46.9px.
        static readonly Vector2 CutlassGrip = new Vector2(9.5f, 39f);
        static readonly Vector2 CutlassTip = new Vector2(37f, 1f);
        static readonly Vector2 CutlassPommel = new Vector2(2f, 45f);
        const float CutlassGripToTip = 46.9f;

        // ── Cobalt Shield: vanilla equip strip frame 0 (the edge-on carry shield) cut out at 12x24 from (24, 26).
        static readonly Rectangle CobaltShieldCutout = new Rectangle(24, 26, 12, 24);

        // ── Overhead Cut (close, <= 70px) ────────────────────────────────────────────────────────────────────
        //   Poses -1.55 -> 2.36 (224deg envelope, ~188deg live). Tell 30: eases from carry into the raised guard over
        //   22t, holds 8t; the shield tucks. Strike Weighted 8/0/30 k6 -> peak 26.5deg/t at t9.
        //   Live: (8 + 30 x 1.204 / 6) / 38 = 0.369 -> 14t (< 22: one roll). Tail 24t + recovery 30t = 54t opening.
        //   Planted; no hyper armor. Cooldown 60t.
        public const string OverheadCutName = "Overhead Cut";
        const float OverheadStart = -1.55f;
        const float OverheadEnd = 2.36f;
        const int OverheadTell = 30;
        const int OverheadEaseIn = 8;
        const int OverheadEaseOut = 30;
        const float OverheadDecay = 6f;
        const float OverheadMaxRange = 70f;

        // ── Rising Lunge Cut (mid range, 70-150px, |dy| < 40, clear line) ─────────────────────────────────────
        //   Poses 3.0 (blade low behind the hip) -> -0.75 (high forward): 215deg envelope, ~174deg live. Tell 36: the
        //   blade sinks low-back over 26t, holds 10t. Strike Weighted 6/0/28 k7 -> peak 31.7deg/t at t7.
        //   Live: (6 + 28 x 1.204 / 7) / 34 = 0.318 -> 11t (< 22). Steps in 72px shaped to the blade (peak on the
        //   strike); 72px + the 47px blade reaches a start ~120px away, so the 150px edge whiffs on purpose.
        //   Hyper armor during the 6t ease-in only. Tail 23t + recovery 36t = 59t. Cooldown 180t.
        public const string RisingLungeName = "Rising Lunge Cut";
        const float LungeStart = 3.0f;
        const float LungeEnd = -0.75f;
        const int LungeTell = 36;
        const int LungeEaseIn = 6;
        const int LungeEaseOut = 28;
        const float LungeDecay = 7f;
        const float LungeStepIn = 72f;
        const float LungeMinRange = 70f;
        const float LungeMaxRange = 150f;
        const float LungeMaxRise = 40f;

        // Both tells settle into their guard over ~73% of the tell, then hold (22/30 and 26/36).
        const float TellSettleFraction = 0.73f;

        // ── Shield loss: tumble from the back hand to the floor over 20t, lie there, fade over the last 30t ───────
        const int ShieldDropTicks = 150;
        const int ShieldTumbleTicks = 20;
        const int ShieldFadeTicks = 30;

        const float KnockbackResist = 0.3f;

        // ── Synced (written after the SkeletonPuppet payload) ─────────────────────────────────────────────────
        bool _shieldLost;

        // ── Per machine: the shield-drop animation and this tick's lunge commit ────────────────────────────────
        bool _shieldStateSeen;
        bool _seenShieldLost;
        int _shieldDropTimer;
        Vector2 _shieldDropFrom;
        Vector2 _shieldDropTo;
        int _shieldDropFacing = 1;
        bool _lungeCommitting;

        bool Server => Main.netMode != NetmodeID.MultiplayerClient;
        bool InComboPhase => Phase == AttackPhase.MeleeComboTelegraph
            || Phase == AttackPhase.MeleeComboAttack
            || Phase == AttackPhase.MeleeComboPause
            || Phase == AttackPhase.MeleeComboRecovery;
        int CutDamage => Main.hardMode ? CutExpertDamageHardmode : CutExpertDamage;

        // ── Puppet configuration ─────────────────────────────────────────────────────────────────────────────
        protected override string InvaderTitle => "Skeleton Warrior";
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        public override bool UsesPuppetDifficultyScaling => false;
        protected override bool DespawnsOnPartyWipe => false;
        protected override int CasualStrollChance => 0;
        protected override int EstusChargesMax => 0;
        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemySkeletonCutlass>();
        protected override int RangedWeaponItemType => -1;
        protected override int MeleeDamage => EnemyDamage.Projectile(CutDamage);
        protected override int RangedDamage => 0;
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Broadsword;
        protected override MeleeCombo[] MeleeComboPoolOverride => Combos;
        protected override void DoMeleeAttack() { }
        protected override void DoRangedAttack() { }
        protected override int OnKillDustType => DustID.Bone;
        protected override int OnKillDustCount => 40;

        protected override float TopSpeed => Main.hardMode ? 1.6f : 1.3f;
        protected override float Acceleration => 0.07f;
        protected override float PuppetJumpPower => 9f;
        protected override float PuppetJumpBoost => 5f;

        // Sword in the near hand, carried out at HoldRotation; the far hand holds the shield.
        protected override bool ShowWeaponDuringNeutral => true;
        protected override bool ShowMeleeWeaponWhileClosingDistance => true;
        protected override bool HoldNeutralWeaponOnCompositeArm => true;
        protected override bool UseCompositeArmSwing => true;
        protected override bool UseTwoHandedCompositeSwing => false;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override Vector2 MeleeHandleNorm => new Vector2(CutlassGrip.X / 40f, CutlassGrip.Y / 48f);
        protected override float MeleeWeaponDrawScale => 1f;
        protected override float ComboReachBase => CutlassGripToTip / 0.7f;

        // Swing polish: authored Weighted clock, tells eased straight into the guard (see ModifyMeleeArcEndpoints).
        protected override bool UseAuthoredComboSwingClock => true;
        protected override bool UseSwingEasing => true;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override float LogicalWindupSettleFraction => TellSettleFraction;
        protected override float ComboTelegraphMultiplier => 1f;
        protected override int MinComboTelegraphTicks => 20;
        protected override int PhaseTelegraphMinimumTicks => 20;
        protected override int MeleeRecoveryLingerTicks => 6;

        // Ranges: the cut starts inside 70px, the lunge is the authored opener out to 150px. Every pick is made.
        protected override float MeleeRange => OverheadMaxRange;
        protected override float MeleeEngageRange => OverheadMaxRange;
        protected override float ComboMaxStartRange => OverheadMaxRange;
        protected override float RangedStartComboMaxRange => LungeMaxRange;
        protected override int MeleeComboChance => 100;
        protected override int RangedStartMeleeComboChance => 100;
        // Server: no new attack for 60t after it stands up again.
        protected override bool HoldAttackSelection => PostReformTicks > 0;

        // ── Shield ───────────────────────────────────────────────────────────────────────────────────────────
        // The vanilla Cobalt Shield: PuppetNPC resolves its equip strip (Acc_Shield_1) with no PNG. Raised while
        // advancing from just outside cut range (70px) to 30 tiles: front hits deal 20% and build half poise.
        // Gone for good once _shieldLost latches; the guard, the cut, the arm pose and the draw all follow it.
        protected override int ShieldItemType => _shieldLost ? -1 : ItemID.CobaltShield;
        protected override bool HoldShieldWhileAdvancing => true;
        protected override float AdvancingGuardMaxRange => 30f * 16f;
        protected override float ShieldDamageReduction => 0.2f;
        protected override bool AllowReactiveDefense => false;

        // ── Moves (legacy MeleeCombo, Weighted, authored clock) ───────────────────────────────────────────────
        static readonly MeleeCombo[] Combos =
        {
            new MeleeCombo
            {
                Name = OverheadCutName,
                BaseWeight = 100,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.White,
                CooldownAfterUse = 60,
                RecoveryTicks = 30,
                MoveBrake = 0.4f,
                Steps = new[]
                {
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.OverheadArc,
                        TelegraphTicks = OverheadTell,
                        AttackTicks = OverheadEaseIn + OverheadEaseOut,
                        DamageMult = 1f,
                        ReachMult = 1f,
                        ForwardPushMult = 0f,
                        SwingSpeedMult = 1f,
                        Ease = SwingEaseStyle.Weighted,
                        EaseInTicks = OverheadEaseIn,
                        EaseOutTicks = OverheadEaseOut,
                        EaseOutDecay = OverheadDecay,
                        HitWindowEnd = (OverheadEaseIn + OverheadEaseOut * 1.204f / OverheadDecay) / (OverheadEaseIn + OverheadEaseOut),
                    },
                },
            },
            new MeleeCombo
            {
                Name = RisingLungeName,
                BaseWeight = 100,
                Preferred = ComboRangeBand.Mid,
                RangedStartOnly = true,
                InitialFlashColor = Color.White,
                CooldownAfterUse = 180,
                RecoveryTicks = 36,
                Steps = new[]
                {
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.UnderhandArc,
                        TelegraphTicks = LungeTell,
                        AttackTicks = LungeEaseIn + LungeEaseOut,
                        DamageMult = LungeDamageFraction,
                        ReachMult = 1f,
                        StepInDistance = LungeStepIn,
                        SwingSpeedMult = 1f,
                        Ease = SwingEaseStyle.Weighted,
                        EaseInTicks = LungeEaseIn,
                        EaseOutTicks = LungeEaseOut,
                        EaseOutDecay = LungeDecay,
                        HitWindowEnd = (LungeEaseIn + LungeEaseOut * 1.204f / LungeDecay) / (LungeEaseIn + LungeEaseOut),
                    },
                },
            },
        };

        // Scoped to this puppet's two combos while they play. The logical windup runs carry -> arc END -> arc START,
        // which whirls a wide arc ~220deg around the hand; collapsing END onto START for the tell makes it a straight
        // ease into the guard, then a hold (the Darkmoon Knight fix).
        protected override void ModifyMeleeArcEndpoints(ComboMotion motion, ref float startRotation, ref float endRotation)
        {
            if (!InComboPhase)
            {
                return;
            }

            bool overheadCut = ActiveMeleeComboName == OverheadCutName && motion == ComboMotion.OverheadArc;
            bool risingLunge = ActiveMeleeComboName == RisingLungeName && motion == ComboMotion.UnderhandArc;

            if (overheadCut)
            {
                startRotation = OverheadStart;
                endRotation = OverheadEnd;
            }

            if (risingLunge)
            {
                startRotation = LungeStart;
                endRotation = LungeEnd;
            }

            bool telling = Phase == AttackPhase.MeleeComboTelegraph;
            if (telling && (overheadCut || risingLunge))
            {
                endRotation = startRotation;
            }
        }

        // Server: the cut needs the target inside 70px; the lunge needs it 70-150px away, roughly level (|dy| < 40,
        // so the rising cut and its step-in can meet it) and in clear line.
        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            if (combo.Name == OverheadCutName)
            {
                return distance <= OverheadMaxRange;
            }

            if (combo.Name != RisingLungeName || !NPC.HasValidTarget)
            {
                return false;
            }

            Player target = Main.player[NPC.target];
            bool inBand = distance >= LungeMinRange && distance <= LungeMaxRange;
            float rise = Math.Abs(target.Center.Y - NPC.Center.Y);
            bool level = rise < LungeMaxRise;

            if (!inBand || !level)
            {
                return false;
            }

            return Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);
        }

        // ── Per-tick AI (skipped while collapsed) ─────────────────────────────────────────────────────────────
        protected override void SkeletonAI()
        {
            if (Server && !NPC.HasValidTarget)
            {
                NPC.TargetClosest();
            }

            _lungeCommitting = false;
            RunPuppetAI();

            // Hyper armor only through the lunge's 6t ease-in (set by OnMeleeComboAttackTick); the cut and every
            // recovery can be staggered.
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            bool neutral = Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll || Phase == AttackPhase.ClosingDistance;
            globalNPC.AttackCommitted = _lungeCommitting;
            globalNPC.AttackTelegraphing = !neutral;

            TrackShieldLoss();

            string label = null;
            if (!neutral && InComboPhase)
            {
                label = ActiveMeleeComboName;
            }
            else if (AdvancingGuardUp)
            {
                label = "Shield Advance";
            }

            DebugAttackLabel = label;
        }

        /// <summary>Every machine: the tick _shieldLost first reads true here, the shield falls from the back hand. A
        /// late joiner who arrives with it already lost sees no drop.</summary>
        void TrackShieldLoss()
        {
            if (!_shieldStateSeen)
            {
                _shieldStateSeen = true;
                _seenShieldLost = _shieldLost;
            }

            if (_shieldDropTimer > 0)
            {
                _shieldDropTimer--;
            }

            if (!_shieldLost || _seenShieldLost)
            {
                return;
            }

            _seenShieldLost = true;
            _shieldDropTimer = ShieldDropTicks;
            _shieldDropFacing = NPC.direction;
            // From the back fist (just behind the chest) to the floor a step behind its heels.
            _shieldDropFrom = NPC.Center + new Vector2(-NPC.direction * 4f, 2f);
            _shieldDropTo = new Vector2(NPC.Center.X - NPC.direction * 14f, NPC.Bottom.Y - 3f);

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.NPCHit4 with { Pitch = 0.2f }, _shieldDropFrom);
                for (int i = 0; i < 8; i++)
                {
                    Dust.NewDustPerfect(_shieldDropFrom, DustID.Cobalt, Main.rand.NextVector2Circular(2f, 2f), 0, default, 1f);
                }
            }
        }

        protected override void OnMeleeComboTelegraphTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            if (Main.dedServ)
            {
                return;
            }

            if (elapsed == 0)
            {
                SoundEngine.PlaySound(SoundID.NPCHit2 with { Pitch = 0.3f, Volume = 0.6f }, NPC.Center);
            }

            // A steel glint runs along the blade over the tell's last 8t, landing on the tip as it releases.
            int glintStart = total - 8;
            if (elapsed >= glintStart && elapsed % 2 == 0)
            {
                float along = (elapsed - glintStart) / 8f;
                Vector2 spot = PuppetWeaponTipPosition(CutlassGripToTip * MathHelper.Lerp(0.4f, 1f, along));
                Dust glint = Dust.NewDustPerfect(spot, DustID.SilverCoin, Vector2.Zero, 0, Color.White, 0.9f);
                glint.noGravity = true;
            }
        }

        protected override void OnMeleeComboAttackTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            if (combo.Name == RisingLungeName && elapsed < LungeEaseIn)
            {
                _lungeCommitting = true;
            }
        }

        // ── Movement: a planted advance via SmartFighter4; combos own their brake and step-in ─────────────────
        protected override void RunMovementAI(float speedMult)
        {
            SmartFighter4AI.Run(NPC, topSpeed: TopSpeed * speedMult, acceleration: Acceleration,
                doorBreakingDamage: 3, attackRange: 700f);
        }

        // Server: any completed poise break knocks the shield away for the rest of its life (reassembly included).
        public override void OnStagger(NPC npc)
        {
            base.OnStagger(npc);

            if (!Server || _shieldLost)
            {
                return;
            }

            _shieldLost = true;
            NPC.netUpdate = true;
            RequestNetworkSnapshot();
        }

        // ── The falling shield (client) ──────────────────────────────────────────────────────────────────────
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            base.PostDraw(spriteBatch, screenPos, drawColor);

            if (_shieldDropTimer <= 0)
            {
                return;
            }

            int elapsed = ShieldDropTicks - _shieldDropTimer;

            // Falls like a drop (eased in) while turning onto its face, then lies still and fades.
            float fall = MathHelper.Clamp(elapsed / (float)ShieldTumbleTicks, 0f, 1f);
            Vector2 position = Vector2.Lerp(_shieldDropFrom, _shieldDropTo, fall * fall);
            float rotation = -_shieldDropFacing * MathHelper.PiOver2 * fall;
            float alpha = MathHelper.Clamp(_shieldDropTimer / (float)ShieldFadeTicks, 0f, 1f);

            Color light = Lighting.GetColor(position.ToTileCoordinates()) * alpha;
            DrawCobaltShield(spriteBatch, position - screenPos, rotation, _shieldDropFacing, light);
        }

        /// <summary>Client: the Cobalt Shield cut-out centred on screenPosition, mirrored to face the given way. Used by
        /// the falling shield and the bone pile.</summary>
        void DrawCobaltShield(SpriteBatch spriteBatch, Vector2 screenPosition, float rotation, int facing, Color color)
        {
            int shieldSlot = ContentSamples.ItemsByType[ItemID.CobaltShield].shieldSlot;
            Main.instance.LoadAccShield(shieldSlot);
            Texture2D strip = TextureAssets.AccShield[shieldSlot].Value;

            SpriteEffects flip = SpriteEffects.None;
            if (facing == -1)
            {
                flip = SpriteEffects.FlipHorizontally;
            }

            Vector2 origin = CobaltShieldCutout.Size() * 0.5f;
            spriteBatch.Draw(strip, screenPosition, CobaltShieldCutout, color, rotation, origin, NPC.scale, flip, 0f);
        }

        // ── The bone pile (SkeletonPuppet) ────────────────────────────────────────────────────────────────────
        protected override int PileLife => Math.Max(1, (int)(NPC.lifeMax * 0.15f));
        protected override float StandingKnockbackResist => KnockbackResist;

        // Vanilla Skeleton gores: 42 skull, 43 arm, 44 leg (the set vanilla's HitEffect scatters for NPCID.Skeleton).
        static readonly PileBone[] VanillaSkeletonPile =
        {
            new PileBone { GoreType = 44, RestOffset = new Vector2(-12f, -4f), Rotation = -0.4f, StandingOffset = new Vector2(-3f, -10f) },
            new PileBone { GoreType = 44, RestOffset = new Vector2(9f, -4f), Rotation = 1.2f, StandingOffset = new Vector2(3f, -10f) },
            new PileBone { GoreType = 43, RestOffset = new Vector2(14f, -7f), Rotation = -1.0f, StandingOffset = new Vector2(4f, -26f) },
            new PileBone { GoreType = 43, RestOffset = new Vector2(-2f, -7f), Rotation = 0.8f, StandingOffset = new Vector2(-4f, -26f) },
            new PileBone { GoreType = 42, RestOffset = new Vector2(-5f, -10f), Rotation = 0.3f, StandingOffset = new Vector2(0f, -46f), IsSkull = true },
        };

        protected override PileBone[] PileBones => VanillaSkeletonPile;

        // The Cutlass lies flat at its feet, point forward, with the shield (if it still has one) face-down behind;
        // both appear over 6t and leave over the first 10t of the reform as the frame pulls them back up.
        protected override void DrawPileProps(SpriteBatch spriteBatch, Vector2 screenPos, Vector2 anchor, int facing, Color light)
        {
            int timer = ReassemblyTimer;
            float propAlpha = MathHelper.Clamp(timer / 6f, 0f, 1f);
            if (ReassemblyPhase == Reassembly.Reform)
            {
                propAlpha = MathHelper.Clamp(1f - timer / 10f, 0f, 1f);
            }

            if (propAlpha <= 0f)
            {
                return;
            }

            if (!_shieldLost)
            {
                Vector2 shieldCenter = anchor + new Vector2(-facing * 8f, -3f);
                DrawCobaltShield(spriteBatch, shieldCenter - screenPos, -facing * MathHelper.PiOver2, facing, light * propAlpha);
            }

            Main.instance.LoadItem(ItemID.Cutlass);
            Texture2D cutlass = TextureAssets.Item[ItemID.Cutlass].Value;
            float headingAngle = 0f;
            if (facing == -1)
            {
                headingAngle = MathHelper.Pi;
            }

            // Turn the sprite's pommel -> tip line onto the heading, centred on the blade's midpoint.
            Vector2 nativeLine = CutlassTip - CutlassPommel;
            float nativeAngle = (float)Math.Atan2(nativeLine.Y, nativeLine.X);
            Vector2 bladeMidpoint = (CutlassTip + CutlassPommel) * 0.5f;
            Vector2 cutlassCenter = anchor + new Vector2(facing * 6f, -3f);
            spriteBatch.Draw(cutlass, cutlassCenter - screenPos, null, light * propAlpha, headingAngle - nativeAngle,
                bladeMidpoint, 0.9f, SpriteEffects.None, 0f);
        }

        // Client, the real death: the vanilla skeleton's own gore set; a shattered pile also bursts into bone chips.
        protected override void ScatterRealDeath(bool fromPile)
        {
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, 42, NPC.scale);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position + new Vector2(0f, 20f), NPC.velocity, 43, NPC.scale);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position + new Vector2(0f, 20f), NPC.velocity, 43, NPC.scale);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position + new Vector2(0f, 34f), NPC.velocity, 44, NPC.scale);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position + new Vector2(0f, 34f), NPC.velocity, 44, NPC.scale);

            if (!fromPile)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.NPCDeath2 with { Pitch = -0.2f }, NPC.Center);
            for (int i = 0; i < 40; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(5f, 5f) + new Vector2(0f, -2f);
                Dust.NewDustPerfect(NPC.Bottom + new Vector2(0f, -8f), DustID.Bone, velocity, 0, default, 1.2f);
            }
        }

        // ── Spawn, stats and loot ────────────────────────────────────────────────────────────────────────────
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 1;
            NPC.aiStyle = -1;
            NPC.width = 20;
            NPC.height = 40;
            NPC.damage = 0;
            NPC.knockBackResist = KnockbackResist;
            NPC.timeLeft = 750;
            NPC.scale = 1f;
            NPC.HitSound = SoundID.NPCHit2;
            NPC.DeathSound = SoundID.NPCDeath2;
            // Authored HP; EnemyBalance sets the live values (450 pre-Hardmode / 1,000 Hardmode).
            NPC.lifeMax = 450;
            NPC.defense = 14;
            NPC.value = 600;
            if (Main.hardMode)
            {
                NPC.lifeMax = 1000;
                NPC.defense = 30;
                NPC.value = 1500;
            }

            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.SkeletonWarriorBanner>();

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PatrolMode = PatrolMode.Wander;
            globalNPC.PatrolAnchorSource = PatrolAnchorSource.GiveUpLocation;
            globalNPC.NavSearchRadius = 40;
            globalNPC.CanUseRopes = true;
            globalNPC.PatrolUsesNavigation = true;
            globalNPC.MaxJumpPower = 9f;
            globalNPC.MaxJumpBoost = 5f;
            globalNPC.CanTeleport = false;
            globalNPC.TeleportMaxCharges = 0;
            globalNPC.TeleportChargesRemaining = 0;
            globalNPC.EvasiveTeleportAway = false;
            globalNPC.EvasiveQuickStep = false;
            globalNPC.EvasiveRetreatAndShoot = false;
            globalNPC.KiteRangeMin = 0f;
            globalNPC.KiteRangeMax = 0f;
        }

        // Bone chips instead of blood on every hit (EnemyImpactVFX registers it as None).
        public override void HitEffect(NPC.HitInfo hit)
        {
            base.HitEffect(hit);

            if (Main.dedServ || NPC.life <= 0)
            {
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Bone, hit.HitDirection * 1.5f, -1f, 0, default, 1f);
            }
        }

        // Bones only, once Skeletron or Gravelord Nito is down. Souls come from value. No sword, shield or armor.
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.DownedSkeletronOrNitoRule, ItemID.Bone, 1, 1, 3));
        }

        // ── Multiplayer payload: PuppetNPC, then the SkeletonPuppet reassembly, then this ───────────────────
        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write(_shieldLost);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            _shieldLost = reader.ReadBoolean();
        }
    }
}
