using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Titanite;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.Enemies;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    /// <summary>
    /// Great Red Knight: the RedKnightPuppet body in the RedKnightSet armour under a Red &amp; Black dye, carrying the
    /// Ancient Blood Lance. Phase one draws from the Great pool (TryStartGreat) plus the shared spear combos; at 30% it
    /// plants into Crimson Dominion and from then on fights from the narrow Dominion pool, with the Dominion lightning
    /// loop running every tick. Its death is the Dominion finale: replant, seal, nova, then death.
    /// Drawn at 1.0x like Red Knight (no PuppetDrawScale); size reads from the lance, dye and two-handed poses.
    /// </summary>
    [AutoloadBossHead]
    class GreatRedKnight : RedKnightPuppet, IHumanoidMeleeHitEffects
    {
        NPCDespawnHandler despawnHandler;

        // --- Crimson Dominion death finale ----------------------------------------------------
        // Dominion no longer ends on a timer, so its finishing seal + nova is GRK's DEATH: the
        // moment lethal damage lands, CheckDead pins the knight at 1 HP and refuses to let it die
        // until the blast actually goes off. Precedent for the pattern: BossBase.CheckDead
        // (deathAnimationProgress) and Cataluminance.CheckDead (NPCs/Bosses/Cataluminance.cs:747).
        //
        // The order is deliberately NOVA-FIRST, DEATH-SECOND — the explosion is a real last attack,
        // not a cosmetic parting shot:
        //
        //   t = 0   .. 59   REPLANT. The knight freezes and drives its spear back into the ground in both
        //                   hands (the shared plant pose, finale timing) and the Destined Death engulf returns.
        //   t = 60          The finale CrimsonDominionController spawns and the seal starts filling.
        //   t = 60  .. 149  Seal fill (SealFillTicks = 90) — the telegraph to get clear.
        //   t = 150         NOVA. The blast fires AND the knight dies on the same tick, so loot,
        //                   Dark Souls, boss-down flags and BossExtras (all of which hang off the
        //                   real OnKill) land exactly when the explosion does.
        //
        // The finale runs as a held AttackPhase.Custom (started from CheckDead), so the puppet keeps animating and
        // every client follows the phase snapshot plus the synced dominionDeathTimer.
        // The controller projectile outlives the NPC by ~45 ticks to finish the nova + fade. That
        // is safe: it is netImportant, ShouldUpdatePosition() => false, and never dereferences the
        // NPC (ai[2] carries whoAmI but nothing reads it).
        // -1 = not started.
        int dominionDeathTimer = -1;

        /// <summary>Ticks the finale spends re-planting the spear before the seal begins to fill.</summary>
        public const int DominionDeathReplantTicks = 60;

        /// <summary>Ticks from lethal damage to the knight's ACTUAL death — which is the tick the
        /// nova fires, not the end of its fade.</summary>
        public const int DominionDeathSequenceTicks = DominionDeathReplantTicks
            + CrimsonDominionController.SealFillTicks;

        // Safety margin on the finale's Custom phase cap; the knight dies at DominionDeathSequenceTicks.
        const int FinalePhaseSlackTicks = 60;

        public bool InDominionDeathSequence => dominionDeathTimer >= 0;

        #region Defaults
        protected override string InvaderTitle => "Great Red Knight";
        protected override bool UsesGreatKnightPoses => true;
        protected override bool HoldsFinalePose => dominionDeathTimer >= 0;

        // The family armour, darkened to Destined Death black-and-crimson.
        protected override int HeadArmorDyeItemType => ItemID.RedandBlackDye;
        protected override int BodyArmorDyeItemType => ItemID.RedandBlackDye;
        protected override int LegsArmorDyeItemType => ItemID.RedandBlackDye;

        // Dominion speeds the knight up for its closing melee (was FighterAI 2 / 2.6).
        protected override float TopSpeed => specialAttacks.DominionEngaged ? 2.6f : 2f;

        // EnemyAncientBloodLanceProj.png measured: 108x108, head (red blade) at texel (0,0), butt at (107,107). Same
        // top-left-head diagonal as Crystal Knight's North Pole, so the same +90 degree draw offset. At 0.75 the lance
        // is ~115px long against Red Knight's 62px spear (chosen from a 1.0 / 0.75 / 0.5 render comparison).
        protected override string SpearDrawTexturePath => UsefulFunctions.RefactorableFilepath(typeof(EnemyAncientBloodLanceProj));
        protected override Vector2 SpearTextureSize => new Vector2(108f);
        protected override Vector2 SpearHeadNorm => new Vector2(0.5f / 108f);
        protected override Vector2 SpearBaseNorm => new Vector2(107.5f / 108f);
        protected override float SpearDrawRotationOffset => MathHelper.PiOver2;
        protected override float MeleeWeaponDrawScale => 0.75f;
        // Rear hand sits this far up the shaft from the front hand (negative = toward the butt).
        protected override float TwoHandedBackGripOffset => -5f;

        public override void SetStaticDefaults()
        {
            // GreatRedKnight.png (16 frames) stays as the bestiary / placeholder sprite; the puppet draws the body.
            Main.npcFrameCount[NPC.type] = 16;
            NPCID.Sets.TrailCacheLength[NPC.type] = 3;
            NPCID.Sets.TrailingMode[NPC.type] = 0;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire3] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            NPC.aiStyle = -1;
            NPC.width = 20;
            NPC.height = 46;
            // A humanoid puppet deals no body-contact damage; every hit is a lance, spell or bomb.
            NPC.damage = 0;
            NPC.defense = 61;
            NPC.lifeMax = 30000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 225000; // 9000 Dark Souls via GlobalNPC.OnKill expert-mode payout
            NPC.knockBackResist = 0.2f; // poise flinch dial (boss)
            NPC.boss = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.GreatRedKnightBanner>();
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.GreatRedKnight.DespawnHandler"), Color.Red, DustID.RedTorch);

            redKnightsSpearDamage = 45;
            redMagicDamage = 40;
            redKnightsGreatDamage = 50;
            // Benchmarked against this boss's own numbers: the great attack (50) is the heaviest thing it has, and a
            // lobbed AoE with an arc, a visible fuse and a 120px blast the player has ~2s to leave sits just above it.
            redKnightsBombDamage = 62;

            tsorcRevampGlobalNPC redKnightGlobalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            redKnightGlobalNPC.Agility = 0.45f;
            redKnightGlobalNPC.DirectionalDodgeRolls = true;
            redKnightGlobalNPC.PreserveJumpFacingUntilLanding = true;
            EvasiveProfile.RedKnight(redKnightGlobalNPC);
            redKnightGlobalNPC.EvasiveRetreatJump = false;
            redKnightGlobalNPC.EvasiveRetreatDash = false;
            redKnightGlobalNPC.EvasiveDodgeRoll = true;
            redKnightGlobalNPC.EvasiveTeleportAway = false;
            redKnightGlobalNPC.EvasiveOnHitChanceDenominator = 6;
            redKnightGlobalNPC.EvasiveOnHitCooldownTicks = 120;

            // Poise: boss-tier — many hits to stagger, and the impulse is halved for bosses. Tunable lever.
            redKnightGlobalNPC.PoiseMax = 80f;
            redKnightGlobalNPC.PoiseStaggerResetsAI = true; // a stagger cancels a windup attack → neutral

            // Blink aggressively the moment it loses LOS (keeps arena pressure); Dominion switches it off.
            redKnightGlobalNPC.CanTeleport = true;
            redKnightGlobalNPC.TeleportStyle = TeleportStyle.Aggressive;
            redKnightGlobalNPC.TeleportVisualStyle = TeleportVisualStyle.Fire;
            redKnightGlobalNPC.NavSearchRadius = 80;
            redKnightGlobalNPC.CanUseRopes = true;
        }
        #endregion

        //Never despawn except by timing out
        public override bool CheckActive()
        {
            return false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            // CheckDead only ever runs server-side, so without this a multiplayer client would never pose the finale.
            writer.Write(dominionDeathTimer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            dominionDeathTimer = reader.ReadInt32();
        }

        #region AI
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            if (!NPC.active || despawnHandler.IsDespawning)
            {
                return;
            }

            Player target = Main.player[NPC.target];

            // Proximity debuffs on the targeted player's own machine (a buff added elsewhere never reaches them).
            if (!Main.dedServ && NPC.target == Main.myPlayer && NPC.Distance(target.Center) < 700)
            {
                target.AddBuff(ModContent.BuffType<TornWings>(), 1 * 60, false);
                target.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 1 * 60, false);
            }

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (dominionDeathTimer < 0)
            {
                // CRIMSON DOMINION phase 2 runs the lightning loop on top of everything else, including during the
                // plant-and-hold, which is what puts the first Stage A bolts inside it.
                KnightAttackStats attackStats = new KnightAttackStats(
                    redKnightsSpearDamage, redMagicDamage, redKnightsGreatDamage, redKnightsBombDamage);
                specialAttacks.TickDominionSequence(NPC, target, attackStats);
            }

            // Dominion owns a narrow vocabulary: Crimson Advance, Firebomb Reversal and Royal Spear Throw. Generic
            // evasive dashes, fallback hops and teleports are shut down so their tells cannot compete with it.
            if (specialAttacks.DominionEngaged)
            {
                globalNPC.EvasiveRetreatJump = false;
                globalNPC.EvasiveRetreatDash = false;
                globalNPC.EvasiveTeleportAway = false;
                globalNPC.EvasiveLeapForward = false;
                globalNPC.EvasiveRunningDash = false;
                globalNPC.InSustainedEvasion = false;
                globalNPC.EvasiveTelegraphing = false;
                globalNPC.EvasiveTimer = 0;
                globalNPC.CanTeleport = false;
                globalNPC.Agility = 0.58f;
            }

            base.AI();

            // The finale is immune throughout; re-assert after the base tick in case anything in it cleared the flag.
            if (dominionDeathTimer >= 0)
            {
                NPC.dontTakeDamage = true;
            }

            Lighting.AddLight(NPC.Center, new Color(190, 35, 30).ToVector3() * 0.3f);
            DebugAttackLabel = DominionDebugLabel();
        }

        // The base labels controller attacks; Dominion is a permanent phase, so surface its stage and the finale.
        string DominionDebugLabel()
        {
            if (dominionDeathTimer >= 0)
            {
                string beat = "Seal Fill";
                if (dominionDeathTimer < DominionDeathReplantTicks)
                {
                    beat = "Replant";
                }
                return $"Dominion Finale — {beat} ({dominionDeathTimer}/{DominionDeathSequenceTicks})";
            }
            if (specialAttacks.Active)
            {
                return specialAttacks.DebugAttackName;
            }
            if (specialAttacks.DominionEngaged)
            {
                return "Dominion — " + specialAttacks.DominionStageName;
            }
            return null;
        }

        private protected override bool TryStartControllerAttack(Player target, tsorcRevampGlobalNPC globalNPC)
        {
            // The full Great pool is phase-one only (it also forces the Dominion plant at 30%).
            if (specialAttacks.DominionEngaged)
            {
                return specialAttacks.TryStartDominion(NPC, target, globalNPC);
            }
            return specialAttacks.TryStartGreat(NPC, target, globalNPC);
        }

        // The shared spear combos are phase-one only; Dominion fights from its own pool.
        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            if (specialAttacks.DominionEngaged)
            {
                return false;
            }
            return base.CanSelectMeleeCombo(combo, distance, healthFraction);
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            if (dominionDeathTimer >= 0)
            {
                TickDominionDeathSequence();
                UpdateControllerPose();
                return;
            }

            base.DoCustomTick(ticksRemaining);
        }

        protected override void ModifyControllerPoseInput(ref ControllerPoseInput input)
        {
            if (dominionDeathTimer >= 0)
            {
                input.DominionTimer = dominionDeathTimer;
                input.DominionFinale = true;
            }
        }
        #endregion

        #region Dominion death finale
        /// <summary>
        /// Crimson Dominion's finishing seal + nova, repurposed as GRK's death animation. Once the
        /// knight is in Dominion it never leaves, so the blast is triggered by lethal damage rather
        /// than by a timer: pin at 1 HP, refuse to die, play the sequence, then actually die.
        /// Loot and boss-down flags all hang off OnKill, so they simply happen ~2.5s later.
        /// </summary>
        public override bool CheckDead()
        {
            // Killed before ever reaching 30% (e.g. a burst kill from above the gate): no Dominion,
            // no finale, die normally. Never gate the boss's death on a phase it never entered.
            if (!specialAttacks.DominionEngaged)
            {
                return base.CheckDead();
            }

            if (dominionDeathTimer < 0)
            {
                // BEAT 1 — the replant, as a held Custom phase so the puppet poses it. Any running controller attack
                // is dropped; emphatically no loot yet: the fight is not over until the nova fires.
                dominionDeathTimer = 0;
                NPC.dontTakeDamage = true;
                NPC.velocity = Vector2.Zero;
                if (specialAttacks.Active)
                {
                    specialAttacks.Cancel(NPC, NPC.GetGlobalNPC<tsorcRevampGlobalNPC>());
                }
                StartCustomAttack(DominionDeathSequenceTicks + FinalePhaseSlackTicks, MeleeWeaponItemType, swingPose: false);
                NPC.netUpdate = true;
                RequestNetworkSnapshot();
            }

            if (dominionDeathTimer < DominionDeathSequenceTicks)
            {
                NPC.life = 1;
                return false;
            }
            return true;
        }

        /// <summary>Runs as the finale's Custom tick on every machine. The knight is frozen, re-planted and engulfed,
        /// and immune; on the tick the nova fires it takes the killing blow it was spared, so OnKill (and therefore
        /// all loot) coincides with the explosion.</summary>
        void TickDominionDeathSequence()
        {
            dominionDeathTimer++;
            NPC.velocity.X = 0f;
            NPC.dontTakeDamage = true;
            NPC.knockBackResist = 0f;
            Lighting.AddLight(NPC.Center, new Color(206, 16, 34).ToVector3()
                * (0.5f + 0.5f * (dominionDeathTimer / (float)DominionDeathSequenceTicks)));

            if (dominionDeathTimer == 1 && !Main.dedServ)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.9f, Pitch = -0.7f }, NPC.Center);
                tsorcRevampAIs.SpawnTelegraphFlash(NPC, new Color(206, 16, 34));
            }

            // BEAT 2 — the spear is in the ground and the flames are up, so the seal starts to
            // fill. |ai[0]| == 2 selects finale mode: seal fill -> nova -> fade, with none of the
            // containment ring, wall or arena-edge barrage. See CrimsonDominionController.
            if (dominionDeathTimer == DominionDeathReplantTicks
                && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_Death(), NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<CrimsonDominionController>(),
                    redKnightsGreatDamage, 0f, Main.myPlayer,
                    2f, Main.rand.NextFloat(MathHelper.Pi / 12f), NPC.whoAmI);
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item74 with { Volume = 0.8f, Pitch = -0.3f }, NPC.Center);
            }

            // BEAT 3 — the nova fires on this exact tick (spawn + SealFillTicks), and the knight
            // dies with it. Server-authoritative: a client never kills the NPC itself, it plays the
            // animation and waits for the server's death packet.
            if (dominionDeathTimer >= DominionDeathSequenceTicks
                && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.dontTakeDamage = false;
                NPC.life = 0;
                NPC.checkDead();
            }
        }
        #endregion

        public override void OnKill()
        {
            // create unknown embers that fill the explosion's radius
            for (int i = 0; i < 30; i++)
            {
                float velX = 1f - ((float)Main.rand.Next(20)) / 5f;
                float velY = 1f - ((float)Main.rand.Next(20)) / 5f;
                velX *= 4f;
                velY *= 4f;
                Dust.NewDust(new Vector2(NPC.position.X - (float)(NPC.width / 2), NPC.position.Y - (float)(NPC.height / 2)), NPC.width, NPC.height, DustID.EnchantedNightcrawler, velX, velY, 160, default, 1.5f);
                Dust.NewDust(new Vector2(NPC.position.X - (float)(NPC.width / 2), NPC.position.Y - (float)(NPC.height / 2)), NPC.width, NPC.height, DustID.CosmicEmber, velX, velY, 160, default, 1.5f);
                Dust.NewDust(new Vector2(NPC.position.X - (float)(NPC.width / 2), NPC.position.Y - (float)(NPC.height / 2)), NPC.width, NPC.height, DustID.EnchantedNightcrawler, velX, velY, 160, default, 1f);
                Dust.NewDust(new Vector2(NPC.position.X - (float)(NPC.width / 2), NPC.position.Y - (float)(NPC.height / 2)), NPC.width, NPC.height, DustID.CosmicEmber, velX, velY, 160, default, 1f);
            }

            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 3").Type, 1f);
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Humanity>(), 1));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SoulCoin>(), 1, 20, 50));

            IItemDropRule drop1 = ItemDropRule.Common(ModContent.ItemType<RedTitanite>(), 1, 4, 6);
            IItemDropRule drop2 = ItemDropRule.Common(ModContent.ItemType<PurgingStone>(), 1);
            IItemDropRule drop3 = ItemDropRule.Common(ModContent.ItemType<FlameOfTheAbyss>(), 1, 2, 3);
            SuperHardmodeRule SHM = new();
            IItemDropRule shmCondition = new LeadingConditionRule(SHM);
            shmCondition.OnSuccess(drop1);
            shmCondition.OnSuccess(drop2);
            shmCondition.OnSuccess(drop3);
            npcLoot.Add(shmCondition);
        }

        #region Debuffs
        // PuppetMeleeHitbox calls this on the hit player's machine after Hurt.
        public void OnHumanoidMeleeHit(Player target)
        {
            target.AddBuff(BuffID.OnFire, 30 * 60, false);
            target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 6 * 60, false); // knockback on hit
            target.AddBuff(ModContent.BuffType<DarkInferno>(), 6 * 60, false); // no health regen
            target.AddBuff(ModContent.BuffType<Crippled>(), 6 * 60, false); // loss of flight mobility
            target.AddBuff(BuffID.BrokenArmor, 6 * 60, false);
        }
        #endregion

        #region Draw
        /// <summary>
        /// Ramp for Crimson Dominion's body engulf. The flame belongs to the PLANTED SPEAR, not to
        /// the Dominion phase: it appears when the spear goes into the ground and leaves when the
        /// spear comes back out. So it plays exactly twice — across phase 1's plant-and-hold, and
        /// again across the death finale's replant.
        /// </summary>
        float DominionEngulfOpacity
        {
            get
            {
                // DEATH FINALE — the flames return as the finale's opening beat, then hold through the seal and nova.
                if (dominionDeathTimer >= 0)
                {
                    return MathHelper.Clamp(dominionDeathTimer / 30f, 0f, 1f);
                }

                // PHASE 1 (plant & hold) — on over the first 45t, and off again across the 50t retract.
                if (specialAttacks.Attack == KnightSpecialAttack.CrimsonDominion)
                {
                    int timer = specialAttacks.Timer;
                    return MathHelper.Clamp(timer / 45f, 0f, 1f)
                        * MathHelper.Clamp(
                            (RedKnightAttackController.DominionHoldTicks - timer) / 50f, 0f, 1f);
                }

                return 0f;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            bool visible = NPC.alpha < 255 && globalNPC.TeleportCountdown <= 0 && globalNPC.TeleportAppearanceTimer <= 0;
            if (visible && specialAttacks.IsHerald)
            {
                if (specialAttacks.Attack == KnightSpecialAttack.FurnaceHerald)
                {
                    // The Royal Standard's planted black-and-red fire sits behind the boss while
                    // the three Furnace Herald rings build and release.
                    RedKnightVFX.DrawStandardCharge(NPC.Bottom - new Vector2(0f, NPC.gfxOffY),
                        specialAttacks.TelegraphProgress, KnightStandardMode.GreatCenter);
                }
                RedKnightVFX.DrawHerald(NPC.Center, specialAttacks.TelegraphProgress,
                    specialAttacks.Attack == KnightSpecialAttack.StormHerald);
            }
            else if (visible && DominionEngulfOpacity > 0f)
            {
                // Back half of the engulf, anchored on the FEET: the flame technique is bottom-anchored.
                RedKnightVFX.DrawDominionEngulf(NPC.Bottom - new Vector2(0f, NPC.gfxOffY), NPC.scale,
                    DominionEngulfOpacity * 0.95f, front: false);
            }

            // The base draws the puppet (and the Spectral Hand Barrage's Ultrakill seal).
            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            base.PostDraw(spriteBatch, screenPos, drawColor);

            // Front half of the engulf, over the body and the planted lance, much lighter so the knight stays readable.
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            bool visible = NPC.alpha < 255 && globalNPC.TeleportCountdown <= 0 && globalNPC.TeleportAppearanceTimer <= 0;
            if (visible && DominionEngulfOpacity > 0f)
            {
                RedKnightVFX.DrawDominionEngulf(NPC.Bottom - new Vector2(0f, NPC.gfxOffY), NPC.scale,
                    DominionEngulfOpacity * 0.42f, front: true);
            }
        }
        #endregion
    }
}
