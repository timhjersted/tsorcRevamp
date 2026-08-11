using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    [AutoloadBossHead]
    class GreatRedKnight : ModNPC, IHumanoidMeleeHitEffects, NPCs.IDebugAttackLabel
    {
        public int redKnightsSpearDamage = 45;
        public int redMagicDamage = 40;
        public int redKnightsGreatDamage = 50;
        // The bomb used to be spawned with redKnightsSpearDamage (45) and then had its explosion
        // damage OVERWRITTEN by EnemyFirebomb's flat 38 tier value, so it landed below every other
        // attack in this kit despite being the slowest and most heavily telegraphed. Benchmarked
        // against this boss's own numbers rather than picked out of the air: the great attack (50)
        // is the heaviest thing it has, and a lobbed AoE with an arc, a visible fuse and a 120px
        // blast the player has ~2s to walk out of should sit just above it.
        public int redKnightsBombDamage = 62;

        readonly RedKnightAttackController specialAttacks = new RedKnightAttackController();

        // --- Crimson Dominion death finale ----------------------------------------------------
        // Dominion no longer ends on a timer, so its finishing seal + nova is GRK's DEATH: the
        // moment lethal damage lands, CheckDead pins the knight at 1 HP and refuses to let it die
        // until the blast actually goes off. Precedent for the pattern: BossBase.CheckDead
        // (deathAnimationProgress) and Cataluminance.CheckDead (NPCs/Bosses/Cataluminance.cs:747).
        //
        // The order is deliberately NOVA-FIRST, DEATH-SECOND — the explosion is a real last attack,
        // not a cosmetic parting shot:
        //
        //   t = 0   .. 59   REPLANT. The knight freezes, drives its spear back into the ground and
        //                   the Destined Death engulf returns. Nothing is dead yet.
        //   t = 60          The finale CrimsonDominionController spawns and the seal starts filling.
        //   t = 60  .. 149  Seal fill (SealFillTicks = 90) — the telegraph to get clear.
        //   t = 150         NOVA. The blast fires AND the knight dies on the same tick, so loot,
        //                   Dark Souls, boss-down flags and BossExtras (all of which hang off the
        //                   real OnKill) land exactly when the explosion does.
        //
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
            + Projectiles.Enemy.Weapons.CrimsonDominionController.SealFillTicks;

        public bool InDominionDeathSequence => dominionDeathTimer >= 0;

        const int FallbackAirborneMeleeFrame = 8;
        int lastGroundedWalkFrame = FallbackAirborneMeleeFrame;
        int airborneMeleeFrame = -1;

        NPCDespawnHandler despawnHandler;

        #region Defaults
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 16;
            NPCID.Sets.TrailCacheLength[NPC.type] = 3; //How many copies of shadow/trail
            NPCID.Sets.TrailingMode[NPC.type] = 0;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire3] = true;
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            AnimationType = 28;
            NPC.height = 40;
            NPC.width = 20;
            NPC.damage = 100;
            NPC.defense = 61;
            NPC.lifeMax = 30000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 225000; // 9000 Dark Souls via GlobalNPC.OnKill expert-mode payout
            NPC.knockBackResist = 0.2f; // poise flinch dial (boss). BasicAI restores this each tick despite the attack-state =0f lines.
            NPC.scale = 1.15f;
            NPC.boss = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.GreatRedKnightBanner>();
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.GreatRedKnight.DespawnHandler"), Color.Red, DustID.RedTorch);
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

            // Navigation tuning: maximum jumps, double jump, and ledge routing
            redKnightGlobalNPC.MaxJumpPower = 12f;
            redKnightGlobalNPC.MaxJumpBoost = 8f;
            redKnightGlobalNPC.CanDoubleJump = true;
            redKnightGlobalNPC.DoubleJumpPower = 8f;
            // Step 6 boss lever: blink aggressively the moment it loses LOS (keeps arena pressure).
            redKnightGlobalNPC.CanTeleport = true;
            redKnightGlobalNPC.TeleportStyle = NPCs.TeleportStyle.Aggressive;
            redKnightGlobalNPC.TeleportVisualStyle = NPCs.TeleportVisualStyle.Fire;
            redKnightGlobalNPC.NavSearchRadius = 80; // Phase 2: SmartFighter4AI movement
            redKnightGlobalNPC.CanUseRopes = true;
        }
        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
            redKnightsSpearDamage = (int)(redKnightsSpearDamage * tsorcRevampWorld.SHMScale);
            redMagicDamage = (int)(redMagicDamage * tsorcRevampWorld.SHMScale);
            redKnightsGreatDamage = (int)(redKnightsGreatDamage * tsorcRevampWorld.SHMScale);
            redKnightsBombDamage = (int)(redKnightsBombDamage * tsorcRevampWorld.SHMScale);
        }
        #endregion


        public Player player
        {
            get => Main.player[NPC.target];
        }

        public string DebugAttackLabel
        {
            get
            {
                if (dominionDeathTimer >= 0)
                {
                    string beat = dominionDeathTimer < DominionDeathReplantTicks
                        ? "Replant"
                        : "Seal Fill";
                    return $"Dominion Finale — {beat} ({dominionDeathTimer}/{DominionDeathSequenceTicks})";
                }
                if (specialAttacks.Active)
                {
                    return specialAttacks.DebugAttackName;
                }
                // Dominion is a permanent phase, so surface which lightning stage is running —
                // otherwise the readout says "nothing" for the entire second half of the fight.
                if (specialAttacks.DominionEngaged)
                {
                    return "Dominion — " + specialAttacks.DominionStageName;
                }
                return "Idle";
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            specialAttacks.Send(writer);
            // CheckDead only ever runs server-side, so without this a multiplayer client would see
            // the knight freeze with its spear in hand and no flames while the finale played.
            writer.Write(dominionDeathTimer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            specialAttacks.Receive(reader);
            dominionDeathTimer = reader.ReadInt32();
        }

        #region On Hit
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.EvasiveOnHit(NPC, true);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
        }
        #endregion
        
        //Never despawn except by timing out
        public override bool CheckActive()
        {
            return false;
        }

        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            if (!NPC.active || despawnHandler.IsDespawning)
            {
                return;
            }

            // Proximity Debuffs
            if (NPC.Distance(player.Center) < 700)
            {
                player.AddBuff(ModContent.BuffType<TornWings>(), 1 * 60, false);
                player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 1 * 60, false);
            }

            specialAttacks.TickCooldowns();
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            KnightAttackStats attackStats = new KnightAttackStats(
                redKnightsSpearDamage, redMagicDamage, redKnightsGreatDamage, redKnightsBombDamage);

            // DEATH FINALE owns the knight completely — no movement, no attacks, no new lightning.
            if (dominionDeathTimer >= 0)
            {
                TickDominionDeathSequence();
                return;
            }

            // CRIMSON DOMINION phase 2 runs the lightning loop on top of everything else, including
            // during the plant-and-hold below, which is what puts the first Stage A bolts inside it.
            specialAttacks.TickDominionSequence(NPC, player, attackStats);

            if (specialAttacks.Active)
            {
                specialAttacks.Tick(NPC, player, attackStats);
                Lighting.AddLight(NPC.Center, new Color(190, 35, 30).ToVector3() * 0.65f);
                return;
            }

            // Neutral sentinels for the shared controller's idle-start contract. The retired
            // ai[1]/ai[2] conveyor has been removed, so these values never advance independently.
            NPC.ai[1] = 60f;
            NPC.ai[2] = -100f;

            // Dominion phase 2 owns a narrow vocabulary: Crimson Advance, Firebomb Reversal and
            // Royal Spear Throw. Generic evasive dashes, fallback hops and teleports are shut
            // down so their independent tells cannot compete with the authored attacks.
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
                globalNPC.Agility = 0.58f;   // was 0.45f

                if (specialAttacks.TryStartDominion(NPC, player, globalNPC))
                {
                    specialAttacks.Tick(NPC, player, attackStats);
                    return;
                }

                tsorcRevampAIs.FighterAI(NPC, 2.6f, canTeleport: false, enragePercent: 0.5f, enrageTopSpeed: 5, canDodgeroll: true);
            }
            else
            {
                tsorcRevampAIs.FighterAI(NPC, 2, canTeleport: true, enragePercent: 0.5f, enrageTopSpeed: 4, canDodgeroll: true);
            }

            // The full Great Knight pool is phase-one only. Dominion's narrow pool gets first
            // refusal above, before FighterAI can begin a competing shared action.
            if (!specialAttacks.DominionEngaged && specialAttacks.TryStartGreat(NPC, player, globalNPC))
            {
                specialAttacks.Tick(NPC, player, attackStats);
                return;
            }

        }

        public override void FindFrame(int frameHeight)
        {
            int currentFrame = frameHeight > 0 ? NPC.frame.Y / frameHeight : 0;
            bool airborne = NPC.velocity.Y < -0.01f || (!NPC.collideY
                && (Math.Abs(NPC.velocity.Y) > 0.01f || Math.Abs(NPC.oldVelocity.Y) > 0.01f));

            if (!airborne)
            {
                if (currentFrame >= 2 && currentFrame < Main.npcFrameCount[NPC.type])
                {
                    lastGroundedWalkFrame = currentFrame;
                }

                airborneMeleeFrame = -1;
                return;
            }

            if (specialAttacks.UsesStableMeleeFrame)
            {
                if (airborneMeleeFrame < 2 || airborneMeleeFrame >= Main.npcFrameCount[NPC.type])
                {
                    // Frame 8 is only a fallback if a client first observes the knight already airborne.
                    airborneMeleeFrame = lastGroundedWalkFrame;
                }

                NPC.frame.Y = airborneMeleeFrame * frameHeight;
                NPC.frameCounter = 0d;
            }
        }


        #region Dominion death finale
        /// <summary>
        /// Crimson Dominion's finishing seal + nova, repurposed as GRK's death animation. Once the
        /// knight is in Dominion it never leaves, so the blast is triggered by lethal damage rather
        /// than by a timer: pin at 1 HP, refuse to die, play the sequence, then actually die.
        ///
        /// Same pattern as BossBase.CheckDead's deathAnimationProgress gate and
        /// Cataluminance.CheckDead (NPCs/Bosses/Cataluminance.cs:747). Loot and boss-down flags all
        /// hang off OnKill, so they simply happen ~2.3s later — nothing else needs to know.
        /// </summary>
        public override bool CheckDead()
        {
            // Killed before ever reaching 30% (e.g. a burst kill from above the gate): no Dominion,
            // no finale, die normally. Never gate the boss's death on a phase it never entered.
            if (!specialAttacks.DominionEngaged)
            {
                return true;
            }

            if (dominionDeathTimer < 0)
            {
                // BEAT 1 — the replant. The knight stops dead and drives its spear back into the
                // ground; the engulf ramps back on over the next 30 ticks. No projectile yet, and
                // emphatically no loot yet: the fight is not over until the nova fires.
                dominionDeathTimer = 0;
                NPC.dontTakeDamage = true;
                NPC.velocity = Vector2.Zero;
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item74 with { Volume = 0.9f, Pitch = -0.7f }, NPC.Center);
                tsorcRevampAIs.SpawnTelegraphFlash(NPC, new Color(206, 16, 34));
                NPC.netUpdate = true;
            }

            if (dominionDeathTimer < DominionDeathSequenceTicks)
            {
                NPC.life = 1;
                return false;
            }
            return true;
        }

        /// <summary>Runs instead of the normal AI while the finale plays. The knight is frozen,
        /// re-planted and engulfed, and immune; on the tick the nova fires it takes the killing
        /// blow it was spared, so OnKill (and therefore all loot) coincides with the explosion.</summary>
        void TickDominionDeathSequence()
        {
            dominionDeathTimer++;
            NPC.velocity.X = 0f;
            NPC.velocity.Y = Math.Min(NPC.velocity.Y + 0.35f, 10f);
            NPC.dontTakeDamage = true;
            NPC.knockBackResist = 0f;
            Lighting.AddLight(NPC.Center, new Color(206, 16, 34).ToVector3()
                * (0.5f + 0.5f * (dominionDeathTimer / (float)DominionDeathSequenceTicks)));

            // BEAT 2 — the spear is in the ground and the flames are up, so the seal starts to
            // fill. |ai[0]| == 2 selects finale mode: seal fill -> nova -> fade, with none of the
            // containment ring, wall or arena-edge barrage. See CrimsonDominionController.
            if (dominionDeathTimer == DominionDeathReplantTicks
                && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_Death(), NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<Projectiles.Enemy.Weapons.CrimsonDominionController>(),
                    redKnightsGreatDamage, 0f, Main.myPlayer,
                    2f, Main.rand.NextFloat(MathHelper.Pi / 12f), NPC.whoAmI);
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item74 with { Volume = 0.8f, Pitch = -0.3f }, NPC.Center);
            }

            // BEAT 3 — the nova fires on this exact tick (spawn + SealFillTicks), and the knight
            // dies with it. Releasing the pin here is what makes loot / Dark Souls / boss-down
            // flags / BossExtras land on the explosion rather than before or after it.
            // Server-authoritative: a client must never kill the NPC itself, it just plays the
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
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Humanity>(), 1));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SoulCoin>(), 1, 20, 50));

            IItemDropRule drop1 = ItemDropRule.Common(ModContent.ItemType<RedTitanite>(), 1, 4, 6);
            IItemDropRule drop2 = ItemDropRule.Common(ModContent.ItemType<Items.PurgingStone>(), 1);
            IItemDropRule drop3 = ItemDropRule.Common(ModContent.ItemType<FlameOfTheAbyss>(), 1, 2, 3);
            SuperHardmodeRule SHM = new();
            IItemDropRule shmCondition = new LeadingConditionRule(SHM);
            shmCondition.OnSuccess(drop1);
            shmCondition.OnSuccess(drop2);
            shmCondition.OnSuccess(drop3);
            npcLoot.Add(shmCondition);
        }

        #region Debuffs
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            ApplyHitDebuffs(target);
        }

        public void OnHumanoidMeleeHit(Player target)
        {
            ApplyHitDebuffs(target);
        }

        static void ApplyHitDebuffs(Player target)
        {
            target.AddBuff(BuffID.OnFire, 30 * 60, false);
            target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 6 * 60, false); // knockback on hit
            target.AddBuff(ModContent.BuffType<DarkInferno>(), 6 * 60, false); // no health regen
            target.AddBuff(ModContent.BuffType<Crippled>(), 6 * 60, false); // loss of flight mobility
            target.AddBuff(BuffID.BrokenArmor, 6 * 60, false);
        }
        #endregion

        #region PreDraw
        /// <summary>
        /// Ramp for Crimson Dominion's body engulf. The flame belongs to the PLANTED SPEAR, not to
        /// the Dominion phase: it appears when the spear goes into the ground and leaves when the
        /// spear comes back out. So it plays exactly twice — across phase 1's plant-and-hold, and
        /// again across the death finale's replant — and is completely absent during phase 2's
        /// melee, where the knight is hopping and dashing with the spear in hand.
        /// </summary>
        float DominionEngulfOpacity
        {
            get
            {
                // DEATH FINALE — the spear is driven back in and the flames return as the finale's
                // opening beat, then hold at full through the seal fill and the nova.
                if (dominionDeathTimer >= 0)
                {
                    return MathHelper.Clamp(dominionDeathTimer / 30f, 0f, 1f);
                }

                // PHASE 1 (plant & hold) — on over the first 45t, and off again across the same
                // 50t window in which SpearGripSlide retracts the spear, so flame and spear leave
                // together instead of the fire outliving the thing it is burning on.
                if (specialAttacks.Attack == KnightSpecialAttack.CrimsonDominion)
                {
                    int timer = specialAttacks.Timer;
                    return MathHelper.Clamp(timer / 45f, 0f, 1f)
                        * MathHelper.Clamp(
                            (RedKnightAttackController.DominionHoldTicks - timer) / 50f, 0f, 1f);
                }

                // PHASE 2 (retract & fight) — no engulf at all.
                return 0f;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (NPC.alpha < 255 && globalNPC.TeleportCountdown <= 0 && globalNPC.TeleportAppearanceTimer <= 0)
            {
                if (specialAttacks.IsHerald)
                {
                    if (specialAttacks.Attack == KnightSpecialAttack.FurnaceHerald)
                    {
                        // The Royal Standard's planted black-and-red fire sits behind the boss while
                        // the three Furnace Herald rings build and release.
                        Projectiles.Enemy.RedKnightVFX.DrawStandardCharge(
                            NPC.Bottom - new Vector2(0f, NPC.gfxOffY),
                            specialAttacks.TelegraphProgress,
                            Projectiles.Enemy.Weapons.KnightStandardMode.GreatCenter);
                    }
                    Projectiles.Enemy.RedKnightVFX.DrawHerald(NPC.Center,
                        specialAttacks.TelegraphProgress,
                        specialAttacks.Attack == KnightSpecialAttack.StormHerald);
                }
                else if (specialAttacks.IsSpectralHandBarrage)
                {
                    Projectiles.Enemy.RedKnightVFX.DrawUltrakillSeal(NPC.Center,
                        specialAttacks.SpectralGatherProgress);
                }
                else if (DominionEngulfOpacity > 0f)
                {
                    // Body engulf, drawn only while the spear is actually planted (phase 1's hold
                    // and the death finale's replant — see DominionEngulfOpacity). The knight
                    // stands wrapped in the same black-and-crimson Destined Death flame the death
                    // seal detonates with. Anchored on the sprite's FEET (bottom-centre, gfxOffY
                    // applied), because the flame technique is bottom-anchored — NPC.Center floats it.
                    //
                    // Gating on the opacity rather than on DominionEngaged also matters for the
                    // branch BELOW: an engaged-based gate swallowed the Ultrakill seal for the
                    // entire second half of the fight, because this else-if always won.
                    Projectiles.Enemy.RedKnightVFX.DrawDominionEngulf(
                        NPC.Bottom - new Vector2(0f, NPC.gfxOffY), NPC.scale,
                        DominionEngulfOpacity * 0.95f, front: false);
                }
            }

            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            if (NPC.velocity.X > 5f || NPC.velocity.X < -5f)
            {
                for (int k = 0; k < NPC.oldPos.Length; k++)
                {
                    Vector2 drawPos = NPC.oldPos[k] + new Vector2(NPC.width / 2f, NPC.height - NPC.gfxOffY - 2) - Main.screenPosition; // Where to draw trails, adjusted by 2 pixels
                    Color color = NPC.GetAlpha(lightColor) * ((float)(NPC.oldPos.Length - k) / (float)NPC.oldPos.Length);
                    spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, drawPos, new Rectangle(NPC.frame.X, NPC.frame.Y, 74, 56), color, NPC.rotation, new Vector2(74 * 0.5f, 56), NPC.scale, effects, 0f);
                }
            }
            return true;
        }
        #endregion

        #region Draw Attack Sprites
        static Texture2D spearTexture;
        static Texture2D bombTexture;
        static Texture2D armOverlayTexture;

        // --- Hand-overlay experiment ---
        // The held weapon and the hand both anchor to the knight's gripping hand, which we track per animation frame.
        // Layering: body (normal draw) < weapon (here) < hand (here, on top), so the hand appears to grip the weapon.

        // Sheet is 70x56 per frame, raw art faces LEFT. Hand pixel = where the body's gripping hand sits in each frame.
        // frame 0 = idle, frame 1 = jump (hands up by the head), frames 2-15 = walk cycle. Tune these to your sheet.
        const float FrameW = 70f;
        const float FrameH = 56f;
        static readonly Vector2[] HandPixel = new Vector2[16]
        {
            new Vector2(47, 42), // 0 idle
            new Vector2(33, 12), // 1 jump — hands raised near the head
            new Vector2(40, 35), // 2
            new Vector2(40, 35), // 3
            new Vector2(41, 35), // 4
            new Vector2(41, 35), // 5
            new Vector2(42, 35), // 6
            new Vector2(42, 35), // 7
            new Vector2(43, 35), // 8
            new Vector2(43, 35), // 9
            new Vector2(43, 35), // 10
            new Vector2(43, 35), // 11
            new Vector2(42, 35), // 12
            new Vector2(42, 35), // 13
            new Vector2(41, 35), // 14
            new Vector2(41, 35), // 15
        };
        static readonly Vector2[] OverlayHandPixel = new Vector2[16]
        {
            // Idle overlay ends at y=41 and the measured body grip is y=42. The old y=47 was
            // five source pixels below the fist; walk/jump entries already match their artwork.
            new Vector2(48, 42), // 0 idle
            new Vector2(49, 26), // 1 jump
            new Vector2(48, 33), // 2
            new Vector2(50, 31), // 3
            new Vector2(50, 31), // 4
            new Vector2(50, 31), // 5
            new Vector2(50, 33), // 6
            new Vector2(48, 33), // 7
            new Vector2(48, 33), // 8
            new Vector2(48, 33), // 9
            new Vector2(46, 31), // 10
            new Vector2(44, 31), // 11
            new Vector2(44, 31), // 12
            new Vector2(46, 33), // 13
            new Vector2(48, 33), // 14
            new Vector2(48, 33), // 15
        };
        // Global correction if the whole overlay is consistently off by a few px (tune once, applies to all frames).
        static readonly Vector2 OverlayFudge = new Vector2(0f, 0f);

        // Grip pixel ON each sprite = the point that should land on the knight's hand (rotation pivot for the weapon).
        static readonly Vector2 SpearGripOrigin = new Vector2(54f, 54f);  // EnemyAncientBloodLanceProj is 108x108, grip at center (54, 54)
        // EnemyAncientBloodLanceProj is authored from top-left to bottom-right. A thrust must move
        // its origin along this normalized shaft axis; adding slide to texture Y alone introduces
        // an equal perpendicular component after the texture's 45-degree rotation.
        static readonly Vector2 SpearTextureAxis = new Vector2(0.70710678f, 0.70710678f);
        static readonly Vector2 BombGripOrigin = new Vector2(11f, 18f);  // EnemyFirebomb is 22x24, hand near the bottom
        static readonly Vector2 MagicBallGripOrigin = new Vector2(8f, 8f);
        const float MagicBallBodyInset = 8f;

        // World position of the body's gripping hand for the current animation frame.
        Vector2 CurrentHandWorld(int facingDirection)
        {
            int frame = NPC.frame.Height > 0 ? NPC.frame.Y / NPC.frame.Height : 0;
            if (frame < 0 || frame >= OverlayHandPixel.Length)
            {
                frame = 0;
            }
            Vector2 fp = OverlayHandPixel[frame];
            // Map a 70x56 frame pixel to world: horizontally centered on the hitbox, bottom of frame 4px below the hitbox bottom.
            float x = NPC.Center.X + (fp.X - FrameW / 2f) * NPC.scale * -facingDirection;
            float y = NPC.Center.Y + 24f + NPC.gfxOffY + (fp.Y - FrameH) * NPC.scale;
            return new Vector2(x, y) + OverlayFudge;
        }

        Vector2 CurrentHandWorld()
        {
            return CurrentHandWorld(NPC.spriteDirection);
        }

        Vector2 CurrentMagicBallWorld()
        {
            Vector2 handWorld = CurrentHandWorld();
            float bodyDirection = Math.Sign(NPC.Center.X - handWorld.X);
            return handWorld + new Vector2(bodyDirection * MagicBallBodyInset, 0f);
        }

        Vector2 CurrentSpearWorld(int facingDirection)
        {
            return CurrentHandWorld(facingDirection);
        }

        Vector2 CurrentSpearWorld()
        {
            return CurrentSpearWorld(NPC.spriteDirection);
        }

        internal Vector2 GetAttackSpearLaunchSource(int facingDirection)
        {
            return CurrentSpearWorld(facingDirection);
        }

        void DrawHeldSpear(SpriteBatch spriteBatch, Vector2 screenPosition, float rotation,
            Color drawColor, float gripSlide = 0f)
        {
            Vector2 gripOrigin = SpearGripOrigin + SpearTextureAxis * gripSlide;
            spriteBatch.Draw(spearTexture, screenPosition, null, drawColor, rotation + MathHelper.PiOver4,
                gripOrigin, 0.8f * NPC.scale, SpriteEffects.None, 0f);
        }

        void DrawArmOverlay(SpriteBatch spriteBatch, Color drawColor, int facingDirection)
        {
            if (armOverlayTexture == null)
            {
                return;
            }

            SpriteEffects effects = facingDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Rectangle sourceRectangle = new Rectangle(0, NPC.frame.Y, (int)FrameW, (int)FrameH);
            Vector2 drawPosition = NPC.Center + new Vector2(0f, 24f + NPC.gfxOffY) - Main.screenPosition;
            spriteBatch.Draw(armOverlayTexture, drawPosition, sourceRectangle, drawColor, NPC.rotation, new Vector2(FrameW / 2f, FrameH), NPC.scale, effects, 0f);
        }

        void DrawArmOverlay(SpriteBatch spriteBatch, Color drawColor)
        {
            DrawArmOverlay(spriteBatch, drawColor, NPC.spriteDirection);
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (NPC.alpha >= 255 || globalNPC.TeleportCountdown > 0 || globalNPC.TeleportAppearanceTimer > 0)
            {
                return;
            }

            if (spearTexture == null)
            {
                spearTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/EnemyAncientBloodLanceProj");
            }

            if (bombTexture == null)
            {
                bombTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/EnemyFirebomb");
            }

            if (armOverlayTexture == null)
            {
                armOverlayTexture = ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Enemies/RedKnight_LeftArm", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            // Front half of the body engulf, drawn after the sprite AND any held prop so the flame
            // wraps the knight instead of only silhouetting behind it. Much lighter than the
            // PreDraw pass so the sprite stays readable through it. Only runs while the spear is
            // planted, same as the PreDraw pass.
            void DrawDominionEngulfFront()
            {
                if (DominionEngulfOpacity > 0f)
                {
                    Projectiles.Enemy.RedKnightVFX.DrawDominionEngulf(
                        NPC.Bottom - new Vector2(0f, NPC.gfxOffY), NPC.scale,
                        DominionEngulfOpacity * 0.42f, front: true);
                }
            }

            // DEATH FINALE: specialAttacks is no longer Active by this point, so the planted spear
            // has to be drawn here or the knight would stand empty-handed through its own replant.
            if (InDominionDeathSequence)
            {
                DrawDominionFinaleSpear(spriteBatch, drawColor);
                DrawDominionEngulfFront();
                return;
            }

            if (specialAttacks.Active)
            {
                DrawSpecialAttack(spriteBatch, drawColor);
                DrawDominionEngulfFront();
                return;
            }
            DrawDominionEngulfFront();
        }

        /// <summary>
        /// The death finale's re-planted spear. Mirrors what DrawSpecialAttack does for Dominion
        /// in phase 1 — same hand anchor, same straight-down rotation
        /// (RedKnightAttackController.GetSpearRotation uses Vector2.UnitY for Dominion) — but drives
        /// the grip slide off the finale clock instead of the attack timer, so the spear visibly
        /// drives into the ground over the first 30 ticks and then stays there.
        /// </summary>
        void DrawDominionFinaleSpear(SpriteBatch spriteBatch, Color drawColor)
        {
            int facing = NPC.direction >= 0 ? 1 : -1;
            Vector2 handWorld = CurrentSpearWorld(facing);
            float rotation = Vector2.UnitY.ToRotation() + MathHelper.PiOver2;
            float gripSlide = MathHelper.Lerp(0f, 20f,
                MathHelper.Clamp(dominionDeathTimer / 30f, 0f, 1f));
            DrawHeldSpear(spriteBatch, handWorld - Main.screenPosition, rotation, drawColor, gripSlide);
            DrawArmOverlay(spriteBatch, drawColor, facing);
        }

        void DrawSpecialAttack(SpriteBatch spriteBatch, Color drawColor)
        {
            KnightHeldProp heldProp = specialAttacks.HeldProp;
            if (heldProp == KnightHeldProp.Spear)
            {
                Vector2 handWorld = CurrentSpearWorld(specialAttacks.Direction);
                int bodyFrame = NPC.frame.Height > 0 ? NPC.frame.Y / NPC.frame.Height : 0;
                if (specialAttacks.Attack == KnightSpecialAttack.CrimsonAdvance && bodyFrame == 0)
                {
                    // This is a world-space correction: GRK is drawn at 1.15x, so changing the
                    // sheet anchor by three pixels would overshoot to 3.45 visible pixels.
                    handWorld.Y -= 3f;
                }
                float rotation = specialAttacks.GetSpearRotation(handWorld, NPC.Center);
                float gripSlide = specialAttacks.SpearGripSlide;
                if (specialAttacks.SpearDamageWake)
                {
                    Vector2 forward = (rotation - MathHelper.PiOver2).ToRotationVector2();
                    // Was RedKnightVFX.DrawSpearWake (the crimson filament wake) — retired in favour
                    // of the generic grey displaced-air wake the Black Knights already use. The old
                    // `empowered: true` is folded in as a larger, slightly stronger quad.
                    Projectiles.Enemy.EnemyVFX.DrawBlackKnightSpearWake(
                        handWorld + forward * (gripSlide * 0.5f), forward.ToRotation(),
                        new Vector2(86f, 20f), 0.66f);
                }
                DrawHeldSpear(spriteBatch, handWorld - Main.screenPosition, rotation, drawColor, gripSlide);
                DrawArmOverlay(spriteBatch, drawColor, specialAttacks.Direction);
                return;
            }

            if (heldProp == KnightHeldProp.Bomb)
            {
                Vector2 handWorld = CurrentHandWorld(specialAttacks.Direction);
                float rotation = new Vector2(specialAttacks.Direction, 0f).ToRotation() + MathHelper.PiOver2;
                Vector2 fusePoint = handWorld + new Vector2(0f, -15f).RotatedBy(rotation);
                Projectiles.Enemy.RedKnightVFX.DrawBombFuse(fusePoint,
                    specialAttacks.TelegraphProgress, planted: false);
                spriteBatch.Draw(bombTexture, handWorld - Main.screenPosition, null, drawColor,
                    rotation, BombGripOrigin, 1f, SpriteEffects.None, 0f);
                DrawArmOverlay(spriteBatch, drawColor, specialAttacks.Direction);
                return;
            }

            if (heldProp == KnightHeldProp.Magic)
            {
                Vector2 magicBallWorld = CurrentMagicBallWorld();
                if (specialAttacks.Attack == KnightSpecialAttack.CinderRain)
                {
                    Projectiles.Enemy.RedKnightVFX.DrawCinderMotes(magicBallWorld,
                        specialAttacks.TelegraphProgress, 20f);
                }
                else
                {
                    Projectiles.Enemy.RedKnightVFX.DrawToxicMotes(magicBallWorld, 3,
                        specialAttacks.TelegraphProgress, 20f);
                }
                DrawArmOverlay(spriteBatch, drawColor, specialAttacks.Direction);
            }
        }
        #endregion



    }
}
