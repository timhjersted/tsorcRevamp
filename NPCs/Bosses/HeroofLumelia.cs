using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Ammo;
using tsorcRevamp.Items.Armors.Ranged;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.Tools;
using tsorcRevamp.Items.Weapons.Enemy;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses
{
    // ── Hero of Lumelia — Invader-puppet rework ─────────────────────────────────────────
    // The avenging human hero / rogue-ranger duelist, a mirror of the player.  Rebuilt on the
    // InvaderNPC puppet system so he wears the Archer of Lumelia armor set and swings the
    // AncientFireSword through the shared melee-combo framework.
    //
    // Class name / namespace deliberately unchanged so every external reference keeps working:
    // the scripted-event fight (LumeliaCustomCondition), PoiseProfiles, loot, NewSlain gating,
    // BossChecklist, gores, localization keys, SpawnChance.
    //
    // KIT
    //   Melee    — AncientFireSword Broadsword combos; heavy swings throw a HeroFireArc crescent.
    //   A1       — Knife volleys (base CursedKnives): 1–3 volleys in a row, each a varied fan.
    //   A2       — Vanishing Strike (TickVanish): grey-smoke blink behind the player → reappear
    //              flash → point-blank knife burst.  The signature identity move.
    //   A3       — Archer's Volley (TickHorn): raise a war-cry, then a marching line of arrows.
    //   A4       — Piercing Dash (base CanPierce, NO impale): fiery committed lunge across the arena.
    //   A5       — Rally (TickRally): at 50% / 25% HP, a warband event — widen his spacing, call in
    //              ManHunters one after another, lob potion bombs, then return to normal aggression.
    //   Bomb     — TickBomb: a lobbed HeroPotionBomb that inflicts Potion Sickness (denies healing).
    class HeroofLumelia : InvaderNPC
    {
        protected override string InvaderTitle => "The Hero of Lumelia";

        // Damage values (kept from the legacy kit — hostile-projectile balance already tuned around them).
        public int throwingKnifeDamage = 34;
        public int potionBombDamage = 43;
        int herosArrowDamage = 42;
        int fireArcDamage = 44;

        NPCDespawnHandler despawnHandler;

        // ── Loadout: the Archer of Lumelia armor set + the Ancient Fire Sword ────────────
        protected override int HeadArmorItemType => ModContent.ItemType<ArcherOfLumeliaHairStyle>();
        protected override int BodyArmorItemType => ModContent.ItemType<ArcherOfLumeliaShirt>();
        protected override int LegsArmorItemType => ModContent.ItemType<ArcherOfLumeliaPants>();

        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemyAncientFireSword>();
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Broadsword;

        // Ranged handled by the bespoke ticks + CursedKnives, not the base generic ranged branch.
        protected override int RangedWeaponItemType => -1;
        protected override int MeleeDamage => 65;
        protected override int RangedDamage => 42;

        // ── Melee tuning — agile duelist ────────────────────────────────────────────────
        protected override float TopSpeed => 3f;
        protected override float Acceleration => 0.11f;
        protected override float MeleeRange => 78f;
        protected override float StabRange => 140f;
        protected override float ComboMaxStartRange => 200f;
        protected override int MeleeComboChance => 80;
        protected override int MeleeTelegraphTicks => 34;
        protected override float InvaderJumpPower => 12f;
        protected override float InvaderJumpBoost => 6f;
        protected override Color MeleeTelegraphFlashColor => Color.OrangeRed;

        // Grey-smoke blink identity (kept from the legacy TeleportVisualStyle.GreySmoke).
        protected override int TeleportTelegraphTicks => 120;
        protected override int TeleportDustCount => 22;
        protected override Color TeleportDustTint => new Color(180, 180, 180);
        protected override int CasualStrollChance => 12;

        // Grip tuning for the AncientFireSword sprite — starting values, fine-tune in-game.
        protected override Vector2 MeleeHandleNorm => new Vector2(0.2f, 0.8f);
        protected override float MeleeWeaponDrawScale => 1f;
        protected override float MeleeWeaponRotationOffset => 0.9f;

        // ── A1: Knife volleys (base CursedKnives template) ──────────────────────────────
        protected override bool CanThrowCursedKnives => true;
        protected override int CursedKnivesWeaponItemType => -1;
        protected override float CursedKnivesRange => 780f;
        protected override float CursedKnivesMinRange => 90f;
        protected override float CursedKnivesCloseRange => 220f;
        protected override float CursedKnivesMidRange => 470f;
        protected override int CursedKnivesChance => 5;
        protected override int CursedKnivesTelegraphTicks => 32;
        protected override int CursedKnivesThrowTicks => 10;
        protected override int CursedKnivesBackToBackGap => 12;
        protected override int CursedKnivesFarGap => 22;
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
        protected override int PierceRecoveryTicks => 80;
        protected override int PierceCooldownAfterUse => 480;

        // ── Bespoke set-piece state ─────────────────────────────────────────────────────
        int _attackLabelTimer;
        int _knifePatternIndex;

        int _vanishTimer;
        int _vanishCd = 260;
        const int VanishTotal = 48;

        int _bombTimer;
        int _bombCd = 200;
        const int BombTotal = 34;

        int _hornTimer;
        int _hornCd = 360;
        const int HornTotal = 110;

        int _rallyTimer;
        int _rallyStage; // 0 = none fired, 1 = 50% fired, 2 = 25% fired
        const int RallyTotal = 300;

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

            EvasiveProfile.RedKnight(globalNPC);
        }

        protected override void RunMovementAI(float speedMult)
        {
            var globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 80;
            globalNPC.RemembersLastKnownPos = true;

            SmartFighter4AI.Run(NPC,
                topSpeed: TopSpeed * speedMult,
                acceleration: Acceleration,
                doorBreakingDamage: 10,
                attackRange: MeleeRange);
        }

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player;
            if (Main.hardMode && !tsorcRevampWorld.SuperHardMode && P.ZoneOverworldHeight && P.ZoneDesert && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.HeroofLumelia>())) && !(P.ZoneCorrupt || P.ZoneCrimson) && !P.ZoneBeach && Main.rand.NextBool(500))
            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.HeroofLumelia.Spawn"), 175, 75, 255);
                return 1;
            }

            return 0;
        }
        #endregion

        public override void AI()
        {
            // Contact damage is live only during the Piercing Dash lunge (all other damage is
            // weapon hitboxes / projectiles).
            NPC.damage = Phase == AttackPhase.PierceDash ? (int)(MeleeDamage * 1.2f) : 0;

            base.AI();
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            if (_attackLabelTimer > 0 && --_attackLabelTimer == 0)
            {
                DebugAttackLabel = null;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            if (target.dead || !target.active)
            {
                return;
            }

            // Rally is HP-gated and preempts the cooldown set-pieces; the rest run while idle.
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

        private bool CanStartSetPiece =>
            (Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll) && _rallyTimer <= 0;

        // ── A2: Vanishing Strike ────────────────────────────────────────────────────────
        // Grey-smoke vanish → blink behind the player → reappear flash (≥26t read) → point-blank
        // knife burst.  The signature identity move — a rogue's disengage-and-punish.
        private void TickVanish(Player target)
        {
            if (_vanishTimer > 0)
            {
                _vanishTimer--;
                NPC.velocity.X *= 0.7f;
                int face = target.Center.X < NPC.Center.X ? -1 : 1;
                NPC.direction = face; NPC.spriteDirection = face;

                if (_vanishTimer == VanishTotal - 26) // ~26t after reappearing = the strike
                {
                    ThrowKnifeFan(5, 46f, 12f, target.Center);
                    SoundEngine.PlaySound(SoundID.Item17 with { Pitch = 0.2f }, NPC.Center);
                }
                return;
            }

            if (_vanishCd > 0) { _vanishCd--; return; }
            if (!CanStartSetPiece) return;

            float dist = NPC.Distance(target.Center);
            if (dist < 150f || dist > 720f || !Main.rand.NextBool(150)) return;

            _vanishCd = 380 + Main.rand.Next(200);
            _vanishTimer = VanishTotal;
            SetAttackLabel("Vanishing Strike", 60);
            SmokePuff(NPC.Center);
            SoundEngine.PlaySound(SoundID.Item8 with { Pitch = -0.2f }, NPC.Center);

            // Reappear just behind the player (opposite their facing).
            float destX = target.Center.X - target.direction * 64f;
            NPC.Bottom = new Vector2(destX, target.Bottom.Y);
            NPC.direction = target.Center.X > NPC.Center.X ? 1 : -1;
            NPC.spriteDirection = NPC.direction;
            SmokePuff(NPC.Center);
            SpawnTelegraphFlash(new Color(200, 200, 200));

            EnterPhase(AttackPhase.NovaRecovery, VanishTotal + 6); // park the combat machine
            NPC.netUpdate = true;
        }

        // ── Bomb: lobbed alchemist flask → Potion Sickness ─────────────────────────────
        private void TickBomb(Player target)
        {
            if (_bombTimer > 0)
            {
                _bombTimer--;
                NPC.velocity.X *= 0.85f;
                int face = target.Center.X < NPC.Center.X ? -1 : 1;
                NPC.direction = face; NPC.spriteDirection = face;

                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(NPC.Center + new Vector2(NPC.direction * 10f, -8f), DustID.Torch, Main.rand.NextVector2Circular(1f, 1f), 60, new Color(180, 90, 220), 1.2f);
                    d.noGravity = true;
                }

                if (_bombTimer == BombTotal - 22) // throw near the end of the wind-up
                {
                    Vector2 muzzle = NPC.Center + new Vector2(NPC.direction * 10f, -8f);
                    Vector2 vel = UsefulFunctions.BallisticTrajectory(muzzle, target.Center, 10f, fallback: true);
                    vel += Main.rand.NextVector2Circular(1.5f, 0f);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), muzzle, vel, ModContent.ProjectileType<Projectiles.Enemy.HeroPotionBomb>(), potionBombDamage, 0f, Main.myPlayer);
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.3f }, NPC.Center);
                }
                return;
            }

            if (_bombCd > 0) { _bombCd--; return; }
            if (!CanStartSetPiece) return;

            float dist = NPC.Distance(target.Center);
            if (dist < 120f || dist > 620f || !Main.rand.NextBool(180)) return;

            _bombCd = 420 + Main.rand.Next(180);
            _bombTimer = BombTotal;
            SetAttackLabel("Alchemist's Bomb", 50);
            EnterPhase(AttackPhase.NovaRecovery, BombTotal + 6);
            NPC.netUpdate = true;
        }

        // ── A3: Archer's Volley ────────────────────────────────────────────────────────
        // He raises a war-cry (the read), then his archers loose a marching line of arrows that
        // sweeps across the player's position.  Run ALONG the sweep or break to cover.
        private void TickHorn(Player target)
        {
            if (_hornTimer > 0)
            {
                _hornTimer--;
                NPC.velocity.X *= 0.8f;
                int face = target.Center.X < NPC.Center.X ? -1 : 1;
                NPC.direction = face; NPC.spriteDirection = face;

                int elapsed = HornTotal - _hornTimer;
                // Telegraph (first 45t): rally pose — gold motes rising off his raised arm.
                if (elapsed < 45)
                {
                    if (Main.rand.NextBool(2))
                    {
                        Dust d = Dust.NewDustPerfect(NPC.Center + new Vector2(NPC.direction * 8f, -18f) + Main.rand.NextVector2Circular(6f, 6f), DustID.GoldFlame, new Vector2(0f, -1.4f), 60, default, 1.4f);
                        d.noGravity = true;
                    }
                    Lighting.AddLight(NPC.Center, 0.9f, 0.7f, 0.2f);
                }
                // Active (45..95t): march a falling line of arrows across the player.
                else if (elapsed >= 45 && elapsed < 95 && elapsed % 5 == 0)
                {
                    float sweep = MathHelper.Lerp(-320f, 320f, (elapsed - 45) / 50f);
                    float x = target.Center.X + sweep;
                    for (int i = 0; i < 2; i++)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(),
                            x + Main.rand.Next(-30, 30), target.Center.Y - 780f,
                            (-30 + Main.rand.Next(60)) / 10f, 8.4f,
                            ModContent.ProjectileType<Projectiles.Enemy.HerosArrow>(), herosArrowDamage, 2f, Main.myPlayer);
                    }
                    SoundEngine.PlaySound(SoundID.Item5, NPC.Center);
                }
                return;
            }

            if (_hornCd > 0) { _hornCd--; return; }
            if (!CanStartSetPiece) return;

            float dist = NPC.Distance(target.Center);
            if (dist < 130f || !Main.rand.NextBool(150)) return;

            _hornCd = 520 + Main.rand.Next(260);
            _hornTimer = HornTotal;
            SetAttackLabel("Archer's Volley", 110);
            if (Main.rand.NextBool(3))
            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.HeroofLumelia.Archers1"), 175, 75, 255);
            }
            // War-cry / horn signal (swap for a dedicated horn sound if one is added).
            SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.55f, Pitch = 0.2f }, NPC.Center);
            SpawnTelegraphFlash(Color.Gold);
            EnterPhase(AttackPhase.NovaRecovery, HornTotal + 6);
            NPC.netUpdate = true;
        }

        // ── A5: Rally — the warband mini-event (50% / 25% HP) ───────────────────────────
        // War-cry → he backs off (widens spacing), calls in ManHunters one after another, lobs
        // potion bombs to cover the assembly, then returns to normal aggression.
        private void TickRally(Player target)
        {
            if (_rallyTimer > 0)
            {
                int elapsed = RallyTotal - _rallyTimer;
                _rallyTimer--;

                // Widen spacing: back away from the player while the warband forms.
                int awayDir = NPC.Center.X > target.Center.X ? 1 : -1;
                float dist = NPC.Distance(target.Center);
                NPC.velocity.X = dist < 440f ? awayDir * 3.4f : awayDir * 0.6f;
                NPC.direction = -awayDir; NPC.spriteDirection = -awayDir; // still face the player

                // Grey smoke roiling off him.
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(14f, 20f), DustID.Smoke, new Vector2(0f, -0.6f), 120, new Color(150, 150, 150), 1.5f);
                    d.noGravity = true;
                }

                // Call in ManHunters one after another (five, at a steady cadence).
                if (elapsed == 30 || elapsed == 75 || elapsed == 120 || elapsed == 165 || elapsed == 210)
                {
                    SpawnSupport(ModContent.NPCType<NPCs.Enemies.ManHunter>());
                    SoundEngine.PlaySound(SoundID.NPCHit6 with { Volume = 0.3f }, NPC.Center);
                }
                // Potion bombs to cover the assembly.
                if (elapsed == 90 || elapsed == 190)
                {
                    Vector2 muzzle = NPC.Center + new Vector2(NPC.direction * 10f, -8f);
                    Vector2 vel = UsefulFunctions.BallisticTrajectory(muzzle, target.Center, 10f, fallback: true);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), muzzle, vel, ModContent.ProjectileType<Projectiles.Enemy.HeroPotionBomb>(), potionBombDamage, 0f, Main.myPlayer);
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.3f }, NPC.Center);
                }
                // Capstone lieutenant.
                if (elapsed == 250)
                {
                    SpawnSupport(_rallyStage >= 2 ? ModContent.NPCType<NPCs.Enemies.RedCloudHunter>() : ModContent.NPCType<NPCs.Enemies.Assassin>());
                    SoundEngine.PlaySound(SoundID.NPCHit6 with { Volume = 0.35f, Pitch = 0.1f }, NPC.Center);
                }
                return;
            }

            // The HP trigger only fires from a free phase (it arms silently while busy).
            if (Phase != AttackPhase.Idle && Phase != AttackPhase.CasualStroll) return;

            float hpFrac = (float)NPC.life / NPC.lifeMax;
            bool trigger = (_rallyStage == 0 && hpFrac <= 0.5f) || (_rallyStage == 1 && hpFrac <= 0.25f);
            if (!trigger) return;

            _rallyStage++;
            _rallyTimer = RallyTotal;
            SetAttackLabel("Rally the Warband", 120);
            SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.7f, Pitch = -0.3f }, NPC.Center);
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
                Projectile.NewProjectile(NPC.GetSource_FromThis(), muzzle, vel, ModContent.ProjectileType<Projectiles.Enemy.EnemyThrowingKnife>(), throwingKnifeDamage, 0f, Main.myPlayer);
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
        protected override void DoMeleeAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, PitchVariance = 0.2f }, NPC.Center);
            TryMeleeHit();
        }

        protected override void DoComboMeleeHit(MeleeComboStep step)
        {
            base.DoComboMeleeHit(step);

            if (!Main.dedServ)
            {
                Vector2 tip = NPC.Center + new Vector2(NPC.direction * 34f, -6f);
                for (int i = 0; i < 4; i++)
                {
                    Dust d = Dust.NewDustPerfect(tip + Main.rand.NextVector2Circular(16f, 16f), DustID.Torch, Vector2.Zero, 60, Color.OrangeRed, Main.rand.NextFloat(1.1f, 1.6f));
                    d.noGravity = true;
                }
            }

            // Heavy swings hurl a short-range fire crescent past the blade.
            if (step.ReachMult >= 1.1f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Player target = Main.player[NPC.target];
                Vector2 origin = NPC.Center + new Vector2(NPC.direction * 24f, -6f);
                Vector2 vel = (target.Center - origin).SafeNormalize(new Vector2(NPC.direction, 0f)) * 8f;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, vel, ModContent.ProjectileType<Projectiles.Enemy.HeroFireArc>(), fireArcDamage, 3f, Main.myPlayer);
                SoundEngine.PlaySound(SoundID.Item73 with { Volume = 0.5f, Pitch = 0.2f }, NPC.Center);
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
            if (Main.netMode == NetmodeID.Server) return;
            Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(color));
        }

        private void SpawnSupport(int npcType)
        {
            NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, npcType, 0);
            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, NPC.velocity.X, NPC.velocity.Y);
            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, NPC.velocity.X, NPC.velocity.Y);
            NPC.netUpdate = true;
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
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.HeroOfLumeliaBag>()));
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
