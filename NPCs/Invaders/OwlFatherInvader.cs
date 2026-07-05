using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Armors;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Weapons.Enemy;

namespace tsorcRevamp.NPCs.Invaders
{
    /// <summary>
    /// Pilot invader (2026-07) for the ported BroadswordRework swing-polish features: swing easing,
    /// alternating flip, aim-adaptive arc bias, and the slash VFX layer — all opted into HERE ONLY,
    /// so every other invader (StuddedLeatherWarrior included, the other Axe-archetype user) keeps
    /// its exact current behavior. Wields EnemyGreatFireAxe (own enemy-only sprite/dimensions, not
    /// the player AncientFireAxe item directly). Loot still drops the real player AncientFireAxe —
    /// there's no player-facing GreatFireAxe item, just the enemy-only sprite.
    ///
    /// Ranged attack #1 of a planned 6 ("Fire Volley"): reuses the shared Spiral Fan
    /// swing+staggered-burst system (axe swing -> 3 fireballs, 30 ticks apart) for the base volley,
    /// then chains via TryContinueSpiralFanChain into an optional reposition (backward leap if
    /// there's room, or a leaping dodge-roll THROUGH the player if they're close) for a second
    /// volley, and optionally escalates into a big arc-jump-over with a third volley fired mid-air.
    /// No recovery after the chain ends either way — a clean hit-and-run poke.
    /// </summary>
    public class OwlFatherInvader : InvaderNPC
    {
        protected override string InvaderTitle => "Owl Father";

        protected override int HeadArmorItemType => ModContent.ItemType<OwlFatherMask>();
        protected override int BodyArmorItemType => ModContent.ItemType<OwlFatherArmor>();
        protected override int LegsArmorItemType => ModContent.ItemType<OwlFatherGreaves>();

        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemyGreatFireAxe>();
        protected override int RangedWeaponItemType => -1; // no ranged yet — melee-only pilot
        protected override int RangedDamage => 0;

        protected override int MeleeDamage => 30;

        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Axe;

        // ── Swing-polish opt-ins — THE reason this invader exists ──────────────────
        protected override bool UseSwingEasing => true;
        protected override bool UseAlternateFlip => true;
        protected override bool UseAimAdaptiveArc => true;
        protected override bool HasSlashVFX => true;
        protected override Color SlashVFXColor => Color.OrangeRed; // matches AncientFireAxe's own slashColor

        // ── Axe draw tuning ─────────────────────────────────────────────────────────
        // GreatFireAxe (72x64) has an extended handle vs the old sprite, with the grip sitting
        // right at the bottom-left corner. Rotation offset/scale are still starting guesses from
        // the old shorter-handled sprite — both likely need a fresh in-game tuning pass now that
        // the handle is longer (a longer handle shifts where the swing visually "leads" from).
        protected override Vector2 MeleeHandleNorm => new Vector2(0.08f, 0.92f);
        protected override float MeleeWeaponDrawScale => 0.85f;
        protected override float MeleeWeaponRotationOffset => 1.0f;

        protected override float TopSpeed => 2.65f;
        protected override float Acceleration => 0.095f;
        protected override float MeleeRange => 82f;
        protected override float StabRange => 150f;
        protected override float ComboMaxStartRange => 210f;
        protected override int MeleeComboChance => 85;
        protected override float ComboTelegraphMultiplier => 1.45f;

        protected override int MeleeTelegraphTicks => 36;
        protected override int StabTelegraphTicks => 40;
        protected override int StabAttackTicks => 8;
        protected override int StabRecoveryTicks => 34;
        protected override bool CanStab => true;

        protected override Color MeleeTelegraphFlashColor => new Color(255, 120, 40);

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 42;
            NPC.lifeMax = 2600;
            NPC.defense = 16;
            NPC.damage = 0;
            NPC.knockBackResist = 0.22f;
            NPC.aiStyle = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 14000f;
            NPC.boss = true;
            NPC.npcSlots = 5f;

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseMax = 35f;
            globalNPC.PoiseStaggerResetsAI = true;
            globalNPC.NavGiveUpTicks = 180;
            globalNPC.CanUseRopes = true;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<OwlFatherMask>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<OwlFatherArmor>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<OwlFatherGreaves>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Weapons.Melee.Axes.AncientFireAxe>(), 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DarkSoul>(), 1, 500, 750));
        }

        protected override void DoMeleeAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.65f, PitchVariance = 0.2f }, NPC.Center);
            TryMeleeHit();
        }

        protected override void DoStabAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, PitchVariance = 0.15f }, NPC.Center);
            TryMeleeHit(reach: StabRange * 0.55f);
        }

        protected override void DoRangedAttack()
        {
            // No ranged weapon yet (RangedWeaponItemType = -1 keeps this from ever being called) —
            // the player axe's fireball-on-hit is a planned follow-up, not part of this pass.
        }

        // Every confirmed axe hit (plain swings, combos) sets the target ablaze — matches the
        // player AncientFireAxe's own fire theme (that one applies 12s; 6s here per the enemy tuning).
        protected override void OnBladeHit(Player player)
        {
            player.AddBuff(BuffID.OnFire, 6 * 60);
        }

        // ── Fire Volley (ranged attack #1 of 6) ─────────────────────────────────────
        protected override bool  CanSpiralFan               => true;
        protected override float SpiralFanMinRange          => 200f;  // "at range", not melee
        protected override float SpiralFanMaxRange          => 700f;
        protected override int   SpiralFanChance            => 12;
        protected override int   SpiralFanCooldownAfterUse  => 400;   // whole chain can run long
        protected override int   SpiralFanSwingTelegraphTicks => 30;
        protected override int   SpiralFanSwingTicks        => 30;
        protected override int   SpiralFanFireTicks         => 4;
        protected override int   SpiralFanRecoveryTicks     => 1;     // "no recovery" after the chain ends

        // 30 ticks between each of the 3 shots (fires at index 0/1/2; -1 after 2 ends the volley).
        protected override int NextSpiralFanDelay(int completedShotIndex)
            => completedShotIndex < 2 ? 30 : -1;

        protected override void DoSpiralFanFire(int shotIndex)
            => FireOneVolleyShot(shotIndex % FireVolleySpawnOffsets.Length);

        // Left / middle (1 tile higher) / right, each ~3 tiles (48px) apart.
        private static readonly Vector2[] FireVolleySpawnOffsets =
        {
            new Vector2(-48f, 0f),
            new Vector2(0f, -16f),
            new Vector2(48f, 0f),
        };

        private void FireOneVolleyShot(int offsetIndex)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget) return;
            Player target = Main.player[NPC.target];

            Vector2 spawnPos = NPC.Center + new Vector2(0f, -NPC.height * 0.9f) + FireVolleySpawnOffsets[offsetIndex];
            Vector2 toPlayer = target.Center - spawnPos;
            toPlayer.Y -= 120f; // bias the launch upward, not straight at them — "travel up and towards"
            if (toPlayer == Vector2.Zero) toPlayer = new Vector2(0f, -1f);
            toPlayer.Normalize();
            Vector2 velocity = toPlayer * 6f;

            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.6f, PitchVariance = 0.2f }, NPC.Center);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, velocity,
                ModContent.ProjectileType<Projectiles.Enemy.Weapons.EnemyGreatFireAxeFireball>(),
                20, 2f, Main.myPlayer);
        }

        // ── Chain: reposition (backward leap / dodge-through) -> volley 2 -> optional arc-jump -> volley 3
        private int _fireVolleyChain; // 0 = fresh, 1 = one bonus volley granted, 2 = arc-jump granted

        private const int   FireVolleyChainChance = 45; // roll after volley 1, to attempt volley 2
        private const int   FireVolleyArcChance   = 40; // roll after volley 2, to escalate into the arc-jump
        private const float FireVolleyCloseRange  = 220f; // below this, dodge-through instead of back-leap

        protected override void OnSpiralFanSequenceStart()
        {
            _fireVolleyChain = 0;
        }

        protected override bool TryContinueSpiralFanChain()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget) return false;
            Player target = Main.player[NPC.target];

            if (_fireVolleyChain == 0)
            {
                if (Main.rand.Next(100) >= FireVolleyChainChance) return false;

                float dist = NPC.Distance(target.Center);
                if (dist <= FireVolleyCloseRange)
                {
                    _fireVolleyChain = 1;
                    EnterPhase(AttackPhase.FireVolleyDodgeThrough, FireVolleyDodgeThroughTimeoutTicks);
                    return true;
                }

                if (HasRoomToBackLeap())
                {
                    _fireVolleyChain = 1;
                    EnterPhase(AttackPhase.FireVolleyBackLeap, FireVolleyBackLeapTicks);
                    return true;
                }

                return false; // no valid reposition — ends here, no recovery either way
            }

            if (_fireVolleyChain == 1)
            {
                if (Main.rand.Next(100) >= FireVolleyArcChance) return false;
                _fireVolleyChain = 2;
                BeginFireVolleyArcJump();
                EnterPhase(AttackPhase.FireVolleyArcJump, 100);
                return true;
            }

            return false;
        }

        // Both reposition moves (backward leap, dodge-through) funnel back into the same swing
        // system for volley 2 — re-entering SpiralFanSwingTelegraph gives it its own axe-swing
        // telegraph, matching "with axe swing as telegraph" for the dodge-through landing too.
        protected override void OnFireVolleyRepositionLanded()
            => EnterPhase(AttackPhase.SpiralFanSwingTelegraph, SpiralFanSwingTelegraphTicks);

        // Volley 3: all 3 shots fire together mid-air at the arc's apex (one continuous swing,
        // not a staggered burst like the grounded volleys).
        protected override void DoFireVolleyArcFire()
        {
            for (int i = 0; i < FireVolleySpawnOffsets.Length; i++)
                FireOneVolleyShot(i);
        }

        protected override void OnFireVolleyArcJumpLanded()
            => EnterCasualOrIdle();

        /// <summary>Lightweight clearance probe for the backward leap: an unobstructed line to the
        /// landing point, and solid ground under it. "About 8 tiles if there's room" is approximate
        /// by design, so this doesn't need to be pixel-exact.</summary>
        private bool HasRoomToBackLeap()
        {
            int dir = -NPC.direction; // away from the player
            Vector2 landing = NPC.Center + new Vector2(dir * 128f, 0f); // ~8 tiles
            bool clearPath = Collision.CanHitLine(
                NPC.position, NPC.width, NPC.height,
                landing - new Vector2(NPC.width / 2f, NPC.height / 2f), NPC.width, NPC.height);
            bool groundBelow = Collision.SolidCollision(
                new Vector2(landing.X - 8f, landing.Y + NPC.height / 2f), 16, 24);
            return clearPath && groundBelow;
        }
    }
}
