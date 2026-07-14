using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Accessories.Defensive.Rings;
using tsorcRevamp.Items.Armors;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Weapons.Enemy;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Projectiles.Melee.Shortswords;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    [AutoloadBossHead]
    class Artorias : InvaderNPC
    {
        // InvaderNPC overrides Texture to a shared puppet placeholder, so the default
        // Texture + "_Head_Boss" convention [AutoloadBossHead] relies on would look for
        // "InvaderPlaceholder_Head_Boss" instead of this boss's actual head icon.
        public override string BossHeadTexture => "tsorcRevamp/NPCs/Bosses/SuperHardMode/Artorias_Head_Boss";

        protected override string InvaderTitle => "Artorias";

        // ── Loadout: ArtoriasOfTheAbyss (Abysswalker) armor set ─────────────────────
        protected override int HeadArmorItemType => ModContent.ItemType<ArtoriasOfTheAbyssHelm>();
        protected override int BodyArmorItemType => ModContent.ItemType<ArtoriasOfTheAbyssArmor>();
        protected override int LegsArmorItemType => ModContent.ItemType<ArtoriasOfTheAbyssGreaves>();

        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemyArtoriasGreatsword>();
        protected override int RangedWeaponItemType => -1; // melee-only

        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Greatsword;
        protected override bool HasSlashVFX => true;
        protected override Color SlashVFXColor => Color.DarkViolet;
        protected override float SlashVFXOpacity => 0.22f;
        protected override float SlashVFXScale => 0.72f;

        protected override int MeleeDamage => 55;
        protected override int RangedDamage => 0; // unused, no ranged weapon

        protected override float TopSpeed => 2.4f;
        protected override float Acceleration => 0.12f;

        // ── Piercing Dash ────────────────────────────────────────────────────────
        protected override bool  CanPierce            => true;
        protected override float PierceRange          => 700f;
        protected override float MinPierceRange       => 250f;
        protected override int   PierceChance          => 4;
        protected override int   PierceTelegraphTicks => 60;
        protected override int   PierceDashTicks      => 40;
        protected override float PierceDashSpeed      => 16f;
        protected override int   PierceRecoveryTicks  => 90;
        protected override int   PierceStabChance     => 50;
        protected override int   PierceStabRaiseTicks => 180;
        protected override int   PierceStabFlickTicks => 20;
        protected override int   PierceCooldownAfterUse => 480;

        // ── Jumping Downward Slash ───────────────────────────────────────────────
        protected override bool  CanJumpSlash          => true;
        protected override float JumpSlashMinRange      => 0f;
        protected override float JumpSlashMaxRange      => 50f * 16f;
        protected override float JumpSlashMaxForwardSpeed => 8.5f;
        protected override float JumpSlashMaxUpSpeed    => 18f;
        protected override int   JumpSlashChance        => 5;
        protected override int   JumpSlashCooldownAfterUse => 420;

        protected override int EstusChargesMax => 4;

        // ── Forward Flip Slash ───────────────────────────────────────────────────
        protected override bool  CanFlipSlash              => true;
        protected override float FlipSlashMinRange         => 150f;
        protected override float FlipSlashMaxRange         => 450f;
        protected override int   FlipSlashChance           => 4;
        protected override int   FlipSlashCooldownAfterUse => 420;

        // ── Abyss Slash ──────────────────────────────────────────────────────────
        protected override bool  CanAbyssSlash              => true;
        protected override float AbyssSlashMinRange         => 250f;
        protected override float AbyssSlashMaxRange         => 900f;
        protected override int   AbyssSlashChance           => 5;
        protected override int   AbyssSlashCooldownAfterUse => 300;

        // ── Umbral Echo Step ─────────────────────────────────────────────────────
        // Rides along on Piercing Dash and Forward Flip Slash (see TryArmEchoStep call sites).
        protected override bool CanEchoStep => true;

        // ── Abyss Tendril Grab ───────────────────────────────────────────────────
        protected override bool  CanTendrilGrab          => true;
        protected override float TendrilMinRange         => 150f;
        protected override float TendrilMaxRange          => 500f;
        protected override int   TendrilChance            => 4;
        protected override int   TendrilCooldownAfterUse  => 480;

        const int TendrilGrabDamage = 30;

        const int PierceContactDamage   = 75;
        const int PierceStabBonusDamage = 125;
        const int PierceStabHealAmount  = 5000;
        const float PierceFlickDistance = 10 * 16f; // 10 tiles
        const float ImpaleSwordReach    = 70f;

        private int _impaleSwordProjIndex = -1;

        NPCDespawnHandler despawnHandler;

        // Only a fabled blade can pierce Artorias's protective shield: the Barrow Blade
        // (via its projectile, since the item itself only damages through it) or the
        // Forgotten Gaia Sword, or the DispelShadow debuff those weapons apply.
        bool defenseBroken = false;
        int textCooldown;

        // Fixed damage ring: captured at spawn, never moves. 100 tiles wide (radius 800px),
        // ~5 tile band. Anyone who touches/crosses the band takes lethal damage. Public so
        // MethodSwaps' screen-darkening overlay (outside the ring) can read the same geometry.
        public const float RingRadius = 50 * 16f;      // 100-tile diameter
        const float RingBandHalfWidth = 40f;
        Vector2 _ringCenter;
        public Vector2 RingCenter => _ringCenter;
        int _ringVfxTimer;

        // Live effective radius - equal to RingRadius except during a Ring Collapse (below), which
        // temporarily shrinks it. Everything that used to read the RingRadius const directly (the
        // boundary check, the dust ring, the screen-darkening overlay in MethodSwaps) now reads this
        // instead, so the darkness/lethal-boundary always track the contraction live.
        float _currentRingRadius = RingRadius;
        public float EffectiveRingRadius => _currentRingRadius;

        // ── Ring Collapse: twice per fight (60% and 30% HP), the fixed ring briefly squeezes in,
        // holds, then releases back to its normal radius. A preview dust ring appears at the NEW
        // (contracted) radius for RingCollapseTelegraphTicks before the boundary actually starts
        // moving there, so players get a clean warning before the arena shrinks under them.
        enum RingCollapseState { Inactive, Telegraph, Contracting, Holding, Expanding }
        RingCollapseState _ringCollapseState = RingCollapseState.Inactive;
        int _ringCollapseTimer;
        float _ringCollapseFrom;
        float _ringCollapseTo;
        bool _ringCollapseDone60;
        bool _ringCollapseDone30;

        const float RingCollapseContractFrac  = 0.30f;      // shrink to 70% of normal radius
        const int   RingCollapseTelegraphTicks = 30;
        const int   RingCollapseMoveTicks      = 120;       // 2s each way - "slowly"
        const int   RingCollapseHoldTicks      = 10 * 60;   // 10s held at the contracted radius

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Ichor] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire3] = true;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.knockBackResist = 0;
            NPC.damage = 0; // all damage via weapon hitboxes
            NPC.defense = 75;
            NPC.height = 40;
            NPC.width = 30;
            NPC.lifeMax = 250000;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.value = 750000;
            NPC.rarity = 39;
            NPC.boss = true;
            NPC.lavaImmune = true;
            NPC.coldDamage = true;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.Artorias.DespawnHandler"), Color.Gold, DustID.GoldFlame);

            tsorcRevampGlobalNPC artoriasGlobalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            artoriasGlobalNPC.Agility = 0.4f; // frequent proactive dodges (see EvadesProjectiles below)
            artoriasGlobalNPC.CanTeleport = true;
            artoriasGlobalNPC.TeleportStyle = NPCs.TeleportStyle.Aggressive;
            artoriasGlobalNPC.TeleportVisualStyle = NPCs.TeleportVisualStyle.Plague;
            artoriasGlobalNPC.NavSearchRadius = 80;

            // On-hit dodgeroll: hop/leap/dash away, or blink away (using the same plague-style
            // teleport set above) when able. Same bundle as the Red Knight family.
            EvasiveProfile.RedKnight(artoriasGlobalNPC);
        }

        // Proactive dodge: scans for an incoming aimed projectile and jumps/i-frame rolls it away
        // (rolls Agility above), same mechanism CursedDragonInvader uses - evasion BEFORE getting hit.
        protected override bool EvadesProjectiles => true;

        public override void OnSpawn(IEntitySource source)
        {
            _ringCenter = NPC.Center;
            NPC.netUpdate = true;
        }

        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            if (!NPC.active)
            {
                return;
            }

            if (despawnHandler.IsDespawning)
            {
                NPC.dontTakeDamage = true;
                NPC.velocity *= 0.85f;
                _abyssSurgeTimer = 0;
                _currentRingRadius = RingRadius;

                if (_impaleSwordProjIndex >= 0 && _impaleSwordProjIndex < Main.maxProjectiles
                    && Main.projectile[_impaleSwordProjIndex].active)
                {
                    Main.projectile[_impaleSwordProjIndex].Kill();
                    _impaleSwordProjIndex = -1;
                }

                return;
            }

            base.AI();
            TickProjectileSwordTelegraphs();

            // The puppet body and hand-drawn greatsword both sample the normal light map, so this
            // shared white light keeps the whole silhouette readable when the abyss-space scene is up.
            Lighting.AddLight(NPC.Center, Color.White.ToVector3() * 1.5f);

            if (NPC.HasBuff(ModContent.BuffType<Buffs.DispelShadow>()))
            {
                defenseBroken = true;
            }

            TickAbyssRing();
            TickRingCollapse();
            TickAbyssSurges();

            if (!_abyssShardUnlocked && NPC.life <= NPC.lifeMax * 0.6f)
            {
                _abyssShardUnlocked = true;
            }
        }

        void TickProjectileSwordTelegraphs()
        {
            if (Main.dedServ)
            {
                return;
            }

            bool chargingProjectile = Phase == AttackPhase.AbyssSlashTelegraph
                || Phase == AttackPhase.AbyssSlashPause
                || Phase == AttackPhase.HomingVolleySwingTelegraph
                || Phase == AttackPhase.BoomerangSwingTelegraph
                || Phase == AttackPhase.SpiralFanSwingTelegraph;

            if (!chargingProjectile)
            {
                return;
            }

            Vector2 hand = PuppetHandPosition;
            Vector2 tip = PuppetWeaponTipPosition(54f);
            for (int i = 0; i < 2; i++)
            {
                Vector2 position = Vector2.Lerp(hand, tip, Main.rand.NextFloat(0.45f, 1f))
                    + Main.rand.NextVector2Circular(5f, 5f);
                bool white = Main.rand.NextBool(3);
                Dust dust = Dust.NewDustPerfect(position,
                    white ? DustID.SilverFlame : DustID.ShadowbeamStaff,
                    Main.rand.NextVector2Circular(0.7f, 0.7f), 90,
                    white ? Color.White : Color.DarkViolet,
                    Main.rand.NextFloat(0.9f, 1.35f));
                dust.noGravity = true;
            }
        }

        // At half health Artorias tears open the Abyss for the remainder of the fight.
        const int PersistentAbyssSurge = -1;
        const int AbyssSurgeTendrilCount = 10;
        const float AbyssSurgeTendrilSpeed = 11f;
        bool _abyssSurgeDone50;
        int _abyssSurgeTimer;
        public bool AbyssSurgeActive => _abyssSurgeTimer == PersistentAbyssSurge;

        void TickAbyssSurges()
        {
            if (AbyssSurgeActive)
            {
                MaintainAbyssDebuff();
                return;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            float healthFraction = (float)NPC.life / NPC.lifeMax;
            if (!_abyssSurgeDone50 && healthFraction <= 0.50f)
            {
                _abyssSurgeDone50 = true;
                StartAbyssSurge();
            }
        }

        void MaintainAbyssDebuff()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead)
                {
                    player.AddBuff(ModContent.BuffType<Abyss>(), 2 * 60);
                }
            }
        }

        void StartAbyssSurge()
        {
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.8f, Pitch = -0.35f }, NPC.Center);
            UsefulFunctions.ScreenShake(NPC.Center, strength: 6f, frames: 20);
            _abyssSurgeTimer = PersistentAbyssSurge;
            MaintainAbyssDebuff();

            Projectile.NewProjectile(
                NPC.GetSource_FromThis(),
                NPC.Center,
                Vector2.Zero,
                ModContent.ProjectileType<Projectiles.Enemy.ArtoriasSurgeAura>(),
                0,
                0f,
                Main.myPlayer,
                NPC.whoAmI);

            for (int i = 0; i < AbyssSurgeTendrilCount; i++)
            {
                float angle = MathHelper.TwoPi * i / AbyssSurgeTendrilCount;
                Vector2 velocity = angle.ToRotationVector2() * AbyssSurgeTendrilSpeed;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, velocity,
                    ModContent.ProjectileType<Projectiles.Enemy.ArtoriasAbyssTendril>(), TendrilGrabDamage, 0f,
                    Main.myPlayer, NPC.whoAmI);
            }

            NPC.netUpdate = true;
        }

        // ── Fixed abyss ring: a permanent lethal boundary at the spot Artorias first spawned ──
        void TickAbyssRing()
        {
            if (_ringVfxTimer > 0)
            {
                _ringVfxTimer--;
            }
            else
            {
                _ringVfxTimer = 4;
                UsefulFunctions.DustRingPrecise(_ringCenter, _currentRingRadius, DustID.BlueTorch, 90, alpha: 40, scale: 1.6f);
                SpawnAbyssRingFlames();
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead)
                {
                    continue;
                }

                float dist = Vector2.Distance(player.Center, _ringCenter);
                if (Math.Abs(dist - _currentRingRadius) <= RingBandHalfWidth)
                {
                    int hitDir = player.Center.X < _ringCenter.X ? -1 : 1;
                    player.Hurt(PlayerDeathReason.ByNPC(NPC.whoAmI), 9999, hitDir, dodgeable: false);
                }

                if (dist <= _currentRingRadius)
                {
                    player.AddBuff(ModContent.BuffType<TornWings>(), 120, false);
                }
            }
        }

        void SpawnAbyssRingFlames()
        {
            if (Main.dedServ)
            {
                return;
            }

            const int flameCount = 12;
            for (int i = 0; i < flameCount; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 ringPosition = _ringCenter + angle.ToRotationVector2() * _currentRingRadius;
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-0.35f, 0.35f), Main.rand.NextFloat(-2.8f, -1.2f));
                Dust flame = Dust.NewDustPerfect(ringPosition, DustID.BlueTorch, velocity, 40, new Color(125, 190, 255), Main.rand.NextFloat(1.35f, 1.8f));
                flame.noGravity = true;
            }
        }

        // ── Ring Collapse state machine ─────────────────────────────────────────────
        void TickRingCollapse()
        {
            float hpFrac = (float)NPC.life / NPC.lifeMax;

            if (_ringCollapseState == RingCollapseState.Inactive)
            {
                if (!_ringCollapseDone60 && hpFrac <= 0.60f)
                {
                    _ringCollapseDone60 = true;
                    StartRingCollapse();
                }
                else if (!_ringCollapseDone30 && hpFrac <= 0.30f)
                {
                    _ringCollapseDone30 = true;
                    StartRingCollapse();
                }
                return;
            }

            switch (_ringCollapseState)
            {
                case RingCollapseState.Telegraph:
                    if (!Main.dedServ && Main.rand.NextBool(2))
                    {
                        UsefulFunctions.DustRingPrecise(_ringCenter, _ringCollapseTo, DustID.PurpleTorch, 60, alpha: 60, scale: 1.4f);
                    }
                    if (--_ringCollapseTimer <= 0)
                    {
                        _ringCollapseState = RingCollapseState.Contracting;
                        _ringCollapseTimer = RingCollapseMoveTicks;
                    }
                    break;

                case RingCollapseState.Contracting:
                {
                    float t = 1f - _ringCollapseTimer / (float)RingCollapseMoveTicks;
                    _currentRingRadius = MathHelper.Lerp(_ringCollapseFrom, _ringCollapseTo, EaseInOut(t));
                    if (--_ringCollapseTimer <= 0)
                    {
                        _currentRingRadius = _ringCollapseTo;
                        _ringCollapseState = RingCollapseState.Holding;
                        _ringCollapseTimer = RingCollapseHoldTicks;
                    }
                    break;
                }

                case RingCollapseState.Holding:
                    _currentRingRadius = _ringCollapseTo;
                    if (--_ringCollapseTimer <= 0)
                    {
                        _ringCollapseState = RingCollapseState.Expanding;
                        _ringCollapseTimer = RingCollapseMoveTicks;
                        _ringCollapseFrom = _ringCollapseTo;
                        _ringCollapseTo = RingRadius;
                    }
                    break;

                case RingCollapseState.Expanding:
                {
                    float t = 1f - _ringCollapseTimer / (float)RingCollapseMoveTicks;
                    _currentRingRadius = MathHelper.Lerp(_ringCollapseFrom, _ringCollapseTo, EaseInOut(t));
                    if (--_ringCollapseTimer <= 0)
                    {
                        _currentRingRadius = RingRadius;
                        _ringCollapseState = RingCollapseState.Inactive;
                    }
                    break;
                }
            }
        }

        void StartRingCollapse()
        {
            _ringCollapseFrom = _currentRingRadius; // should already be RingRadius
            _ringCollapseTo = RingRadius * (1f - RingCollapseContractFrac);
            _ringCollapseState = RingCollapseState.Telegraph;
            _ringCollapseTimer = RingCollapseTelegraphTicks;
        }

        static float EaseInOut(float t) => t * t * (3f - 2f * t);

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

        protected override void DoMeleeAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.6f, PitchVariance = 0.2f }, NPC.Center);
            TryMeleeHit();
        }

        protected override void DoRangedAttack()
        {
            // No ranged weapon; never invoked (RangedWeaponItemType is -1).
        }

        // ── Piercing Dash / Stabbing Piercing Dash hooks ────────────────────────────
        protected override void DoPierceWindup(int elapsed)
        {
            if (Main.dedServ)
            {
                return;
            }

            if (!Main.rand.NextBool(2))
            {
                return;
            }

            Vector2 pos = NPC.Center + new Vector2(NPC.direction * 22f, -6f);
            if (IsPierceStab)
            {
                Dust d = Dust.NewDustPerfect(pos, DustID.PurpleTorch, Vector2.Zero, 100, new Color(160, 40, 220), 1.1f);
                d.noGravity = true;
            }
            else
            {
                Dust d = Dust.NewDustPerfect(pos, DustID.SilverFlame, Vector2.Zero, 100, default, 0.9f);
                d.noGravity = true;
            }
        }

        protected override void DoPierceDashTick()
        {
            // Keeps the shared afterimage trail alive every tick of the dash; it decays on its own
            // once this stops being called (dash ends).
            AfterimageTicks = 10;

            if (Main.dedServ || !Main.rand.NextBool(3))
            {
                return;
            }

            Dust d = Dust.NewDustPerfect(NPC.Center, IsPierceStab ? DustID.PurpleTorch : DustID.SilverFlame,
                -NPC.velocity * 0.3f, 100, default, 1f);
            d.noGravity = true;
        }

        protected override void OnPierceContact(Player target, bool isStab)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            int damage = PierceContactDamage + (isStab ? PierceStabBonusDamage : 0);
            int hitDir = NPC.direction;
            target.Hurt(PlayerDeathReason.ByNPC(NPC.whoAmI), damage, hitDir, dodgeable: true, knockback: isStab ? 0f : 6f);

            if (!isStab)
            {
                return;
            }

            NPC.life = Math.Min(NPC.lifeMax, NPC.life + PierceStabHealAmount);
            NPC.HealEffect(PierceStabHealAmount);
            UsefulFunctions.ScreenShake(target.Center, strength: 6f, frames: 12);

            _impaleSwordProjIndex = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                ModContent.ProjectileType<Projectiles.Enemy.ArtoriasImpalingSword>(), 0, 0f, Main.myPlayer, NPC.whoAmI, target.whoAmI);

            var modPlayer = target.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.ImpaleFreezeTimer = 10;
            modPlayer.ImpaleWorldPosition = GetSwordTipWorldPosition();
        }

        protected override void DoPierceStabHoldTick(Player target, float raiseProgress01)
        {
            var modPlayer = target.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.ImpaleFreezeTimer = 10;
            modPlayer.ImpaleWorldPosition = GetSwordTipWorldPosition();
        }

        protected override void OnPierceFlick(Player target)
        {
            var modPlayer = target.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.ImpaleFreezeTimer = 0;

            Vector2 away = new Vector2(NPC.direction, -0.3f);
            away.Normalize();
            target.Center += away * PierceFlickDistance;
            target.velocity = away * 8f;

            if (_impaleSwordProjIndex >= 0 && _impaleSwordProjIndex < Main.maxProjectiles)
            {
                Main.projectile[_impaleSwordProjIndex].Kill();
                _impaleSwordProjIndex = -1;
            }
        }

        /// <summary>Progress (0-1) through the PierceStabHold raise, or 1 once past it; 0 outside the sequence.</summary>
        public float GetImpaleRaiseProgress01()
        {
            if (Phase == AttackPhase.PierceStabHold)
            {
                return PierceStabRaiseTicks > 0 ? 1f - (float)PhaseTimer / PierceStabRaiseTicks : 1f;
            }
            if (Phase == AttackPhase.PierceStabFlick)
            {
                return 1f;
            }
            return 0f;
        }

        /// <summary>Progress (0-1) through the PierceStabFlick release; 0 outside that phase.</summary>
        public float GetImpaleFlickProgress01()
        {
            if (Phase != AttackPhase.PierceStabFlick)
            {
                return 0f;
            }
            return PierceStabFlickTicks > 0 ? 1f - (float)PhaseTimer / PierceStabFlickTicks : 1f;
        }

        /// <summary>World position of the impaling sword's tip, read every tick by ArtoriasImpalingSword
        /// and used to anchor the frozen target. Sword starts pointed straight at the target (horizontal),
        /// raises to vertical over PierceStabHold, then flicks forward-and-down to release.</summary>
        public Vector2 GetSwordTipWorldPosition()
        {
            Vector2 dir;
            if (Phase == AttackPhase.PierceStabFlick)
            {
                dir = Vector2.Lerp(new Vector2(0f, -1f), new Vector2(NPC.direction * 0.8f, 0.6f), GetImpaleFlickProgress01());
            }
            else
            {
                dir = Vector2.Lerp(new Vector2(NPC.direction, 0f), new Vector2(0f, -1f), GetImpaleRaiseProgress01());
            }
            if (dir != Vector2.Zero)
            {
                dir.Normalize();
            }
            return NPC.Center + dir * ImpaleSwordReach;
        }

        // ── Jumping Downward Slash hooks ─────────────────────────────────────────
        protected override void DoJumpSlashDodgebackTick()
        {
            if (Main.dedServ)
            {
                return;
            }
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(NPC.Center, DustID.SilverFlame, -NPC.velocity * 0.4f, 100, default, 0.8f);
                d.noGravity = true;
            }
        }

        protected override void DoJumpSlashRiseTick()
        {
            if (Main.dedServ || !Main.rand.NextBool(2))
            {
                return;
            }
            Vector2 pos = NPC.Center + new Vector2(NPC.direction * 20f, -10f);
            Dust d = Dust.NewDustPerfect(pos, DustID.SilverFlame, Vector2.Zero, 100, default, 1f);
            d.noGravity = true;
        }

        protected override void DoJumpSlashAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, PitchVariance = 0.15f }, NPC.Center);
            TryMeleeHit(reach: 100f);
        }

        // ── Forward Flip Slash hooks ─────────────────────────────────────────────
        // Three cosmetic/landing-effect variants of the same jump-spin attack, rolled once per use.
        private enum FlipVariant { Basic, PurpleOrbBlast, PurplePinkWall }
        private FlipVariant _flipVariant;

        protected override void DoFlipSlashRiseTick()
        {
            if (Phase == AttackPhase.FlipSlashRise && PhaseTimer == FlipSlashRiseMaxTicks)
            {
                _flipVariant = (FlipVariant)Main.rand.Next(3);
            }

            if (Main.dedServ || !Main.rand.NextBool(2))
            {
                return;
            }

            switch (_flipVariant)
            {
                case FlipVariant.PurpleOrbBlast:
                {
                    Dust d = Dust.NewDustPerfect(NPC.Center, DustID.PurpleTorch, -NPC.velocity * 0.3f, 100, new Color(160, 40, 220), 1.1f);
                    d.noGravity = true;
                    break;
                }
                case FlipVariant.PurplePinkWall:
                {
                    int dustId = Main.rand.NextBool(2) ? DustID.PurpleTorch : DustID.PinkTorch;
                    Dust d = Dust.NewDustPerfect(NPC.Center, dustId, -NPC.velocity * 0.3f, 100, default, 1.1f);
                    d.noGravity = true;
                    break;
                }
                default:
                {
                    Dust d = Dust.NewDustPerfect(NPC.Center, DustID.SilverFlame, -NPC.velocity * 0.3f, 100, default, 1f);
                    d.noGravity = true;
                    break;
                }
            }
        }

        protected override void DoFlipSlashHit()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, PitchVariance = 0.15f }, NPC.Center);
            TryMeleeHit(reach: 90f);
        }

        protected override void OnFlipSlashLand()
        {
            switch (_flipVariant)
            {
                case FlipVariant.PurpleOrbBlast:
                    OnFlipSlashLandPurpleOrbBlast();
                    break;
                case FlipVariant.PurplePinkWall:
                    OnFlipSlashLandPurplePinkWall();
                    break;
                default:
                    OnFlipSlashLandBasic();
                    break;
            }
        }

        void OnFlipSlashLandBasic()
        {
            UsefulFunctions.ScreenShake(NPC.Center, strength: 4f, frames: 10);

            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 18; i++)
            {
                float spawnX = Main.rand.NextFloat(-24f, 24f);
                Vector2 spawnPos = NPC.Bottom + new Vector2(spawnX, -4f);
                float angle = -MathHelper.PiOver2 + Main.rand.NextFloat(-0.6f, 0.6f);
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(1f, 6f);
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.Dirt, velocity, 60, default, Main.rand.NextFloat(1.1f, 1.5f));
                d.noGravity = false;
            }
        }

        // Purple circular AOE blast that, after lingering ~1/3 second, bursts into 6 seeking flame
        // orbs fanning out in every direction.
        const int FlipBlastDamage = 60;
        void OnFlipSlashLandPurpleOrbBlast()
        {
            UsefulFunctions.ScreenShake(NPC.Center, strength: 5f, frames: 11);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                ModContent.ProjectileType<Projectiles.Enemy.ArtoriasAbyssBlast>(), FlipBlastDamage, 0f, Main.myPlayer);
        }

        // A tall pillar AOE at the slam point, then two purple flame walls peel off left and right,
        // growing tall as they travel.
        const int FlipPillarDamage = 60;
        const int FlipBlazeDamage = 50;
        void OnFlipSlashLandPurplePinkWall()
        {
            UsefulFunctions.ScreenShake(NPC.Center, strength: 5f, frames: 11);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                ModContent.ProjectileType<Projectiles.Enemy.ArtoriasAbyssPillar>(), FlipPillarDamage, 0f, Main.myPlayer);

            Vector2 spawnPos = NPC.Bottom;
            Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, new Vector2(-5f, 0f),
                ModContent.ProjectileType<Projectiles.Enemy.ArtoriasAbyssBlaze>(), FlipBlazeDamage, 0f, Main.myPlayer);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, new Vector2(5f, 0f),
                ModContent.ProjectileType<Projectiles.Enemy.ArtoriasAbyssBlaze>(), FlipBlazeDamage, 0f, Main.myPlayer);
        }

        // ── Abyss Slash hooks ─────────────────────────────────────────────────────
        // Three swipe-count/timing variants, rolled once at the very first swipe:
        //   0: one slash, then an overhead swing that fans 3 seeking orbs upper-left/up/upper-right
        //   1: three slashes, 60 ticks apart
        //   2: two slashes 30 ticks apart, then a third 60 ticks later, then two more 30 ticks apart
        const int AbyssSlashDamage = 45;
        const int AbyssOrbFinisherDamage = 40;
        const float AbyssSlashSpeed = 9f;

        int _abyssSlashVariant;
        static readonly int[][] AbyssSlashGapTables = new int[][]
        {
            new int[] { 40 },              // variant 0: swipe0 -> 40 ticks -> orb finisher (swipe1)
            new int[] { 60, 60 },          // variant 1: swipe0 -> 60 -> swipe1 -> 60 -> swipe2
            new int[] { 30, 60, 30, 30 },  // variant 2: swipe0 ->30-> swipe1 ->60-> swipe2 ->30-> swipe3 ->30-> swipe4
        };

        protected override int NextAbyssSlashDelay(int completedSwipeIndex)
        {
            int[] gaps = AbyssSlashGapTables[_abyssSlashVariant];
            return completedSwipeIndex < gaps.Length ? gaps[completedSwipeIndex] : -1;
        }

        protected override void DoAbyssSlashFire(int swipeIndex)
        {
            if (swipeIndex == 0)
            {
                _abyssSlashVariant = Main.rand.Next(AbyssSlashGapTables.Length);
            }

            bool isOrbFinisher = _abyssSlashVariant == 0 && swipeIndex == 1;
            if (isOrbFinisher)
            {
                FireAbyssOrbFinisher();
            }
            else
            {
                FireAbyssSlashProjectile();
            }
        }

        void FireAbyssSlashProjectile()
        {
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.6f, Pitch = -0.1f }, NPC.Center);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            // Aims at the player's current position, so an airborne/jumping target naturally gets an
            // angled shot rather than a flat horizontal one.
            Player target = Main.player[NPC.target];
            Vector2 vel = UsefulFunctions.Aim(NPC.Center, target.Center, AbyssSlashSpeed);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
                ModContent.ProjectileType<Projectiles.Enemy.AbyssSlash>(), AbyssSlashDamage, 0f, Main.myPlayer, NPC.whoAmI + 1);
        }

        void FireAbyssOrbFinisher()
        {
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.6f, Pitch = -0.2f }, NPC.Center);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            // Upper-left, straight up, upper-right.
            float[] angles = { -MathHelper.PiOver2 - MathHelper.PiOver4, -MathHelper.PiOver2, -MathHelper.PiOver2 + MathHelper.PiOver4 };
            foreach (float angle in angles)
            {
                Vector2 vel = angle.ToRotationVector2() * 4f;
                // ai[1] is offset by +1 (0 = "no owner") since ArtoriasAbyssBlast's own orb-fan
                // spawns this same projectile without an owner - see ArtoriasFlameOrb.OnHitPlayer.
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
                    ModContent.ProjectileType<Projectiles.Enemy.ArtoriasFlameOrb>(), AbyssOrbFinisherDamage, 0f, Main.myPlayer, 0f, NPC.whoAmI + 1);
            }
        }

        // ── Abyss Tendril Grab hooks ─────────────────────────────────────────────
        protected override void DoTendrilTelegraphTick(int elapsed)
        {
            if (Main.dedServ)
            {
                return;
            }

            // The arm "dissolving into shadow" - heavy, gravity-less black/white dust building at
            // the hand, growing denser as the wind-up progresses.
            Vector2 handPos = NPC.Center + new Vector2(NPC.direction * 18f, -6f);
            int count = 1 + elapsed / 6;
            for (int i = 0; i < count; i++)
            {
                Color tint = Main.rand.NextBool() ? Color.Black : Color.White;
                Dust d = Dust.NewDustPerfect(handPos + Main.rand.NextVector2Circular(10f, 10f), DustID.Smoke,
                    Main.rand.NextVector2Circular(1.2f, 1.2f), 130, tint, Main.rand.NextFloat(0.8f, 1.2f));
                d.noGravity = true;
            }
        }

        protected override void DoTendrilLaunch()
        {
            SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.4f, Pitch = -0.4f }, NPC.Center);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 origin = NPC.Center + new Vector2(NPC.direction * (NPC.width * 0.5f + 10f), -NPC.height * 0.3f);
            Vector2 vel = UsefulFunctions.Aim(origin, target.Center, 12f);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, vel,
                ModContent.ProjectileType<Projectiles.Enemy.ArtoriasAbyssTendril>(), TendrilGrabDamage, 0f, Main.myPlayer, NPC.whoAmI);
        }

        protected override void DoTendrilReachTick()
        {
            if (Main.dedServ || !Main.rand.NextBool(3))
            {
                return;
            }

            Vector2 handPos = NPC.Center + new Vector2(NPC.direction * 18f, -6f);
            Color tint = Main.rand.NextBool() ? Color.Black : Color.White;
            Dust d = Dust.NewDustPerfect(handPos, DustID.Smoke, Vector2.Zero, 130, tint, 0.9f);
            d.noGravity = true;
        }

        protected override void DoTendrilSwing()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, PitchVariance = 0.15f }, NPC.Center);
            TryMeleeHit(reach: 100f);
        }

        // ── Charge-up Nova: one-shot set-piece at 50% / 20% / 10% HP ────────────────
        // Assumed reading of the brief: the three trigger thresholds are 50/20/10% HP, and blast
        // sizes escalate 500/600/700px in that same order (the "final AOE" is the 10% one).
        static readonly (float hpFrac, float radius, int damage)[] NovaStages =
        {
            (0.50f, 500f, 90),
            (0.20f, 600f, 110),
            (0.10f, 700f, 130),
        };
        readonly bool[] _novaStageDone = new bool[NovaStages.Length];
        int _novaStageIndex = -1;

        protected override bool CanNova => true;
        protected override int NovaChargeTicks => 4 * 60;
        protected override int NovaBlastHoldTicks => 24;
        protected override int NovaRecoveryTicks => 90;

        protected override bool ShouldTriggerNova()
        {
            float hp = (float)NPC.life / NPC.lifeMax;
            for (int i = 0; i < NovaStages.Length; i++)
            {
                if (!_novaStageDone[i] && hp <= NovaStages[i].hpFrac)
                {
                    _novaStageDone[i] = true;
                    _novaStageIndex = i;
                    return true;
                }
            }
            return false;
        }

        protected override void DoNovaChargeTick(int elapsed, int total)
        {
            if (Main.dedServ || _novaStageIndex < 0)
            {
                return;
            }

            float innerT = elapsed / (float)total;                     // 0 -> 1 over the full charge
            float outerT = Math.Min(innerT * 1.6f, 1f);                 // outer ring races out ahead

            float radius = NovaStages[_novaStageIndex].radius;

            // Heavy engulfing dust thickening around Artorias, radius growing with innerT.
            int count = 2 + elapsed / 8;
            for (int i = 0; i < count; i++)
            {
                float r = MathHelper.Lerp(20f, radius * 0.35f, innerT);
                Vector2 pos = NPC.Center + Main.rand.NextVector2Circular(r, r);
                Color tint = Main.rand.NextBool(3) ? (Main.rand.NextBool() ? Color.Black : Color.White) : default;
                Dust d = Dust.NewDustPerfect(pos, DustID.PurpleTorch, Vector2.Zero, 60, tint, Main.rand.NextFloat(1.3f, 2f));
                d.noGravity = true;
            }

            // Thin outer telegraph ring, expanding faster than the inner dust cloud.
            if (elapsed % 3 == 0)
            {
                UsefulFunctions.DustRingPrecise(NPC.Center, radius * outerT, DustID.PurpleTorch, 40, alpha: 70, scale: 1.3f);
            }
        }

        protected override void DoNovaBlast()
        {
            if (_novaStageIndex < 0)
            {
                return;
            }

            var (_, radius, damage) = NovaStages[_novaStageIndex];

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1f, Pitch = -0.3f }, NPC.Center);
            UsefulFunctions.ScreenShake(NPC.Center, strength: 10f, frames: 20);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                ModContent.ProjectileType<Projectiles.Enemy.ArtoriasChargeNova>(), damage, 6f, Main.myPlayer, radius);
        }

        // ── Abyss Shard: ground-spike combos, unlocked permanently at 60% HP ────────
        // Each individual shard (spawned via SpawnShardAt) carries its own 40-tick ground telegraph
        // and pop - the boss side only decides WHERE and WHEN to spawn one. No recovery phase per the
        // brief: the moment a sequence ends, control returns immediately so another attack can follow.
        enum AbyssShardVariant { Burst3, Domino6, DominoPulse4x2, Escalating357 }
        bool _abyssShardUnlocked;
        AbyssShardVariant _abyssShardVariant;
        int _abyssShardDominoDir;
        Vector2 _abyssShardAnchor;

        const int AbyssShardDamage = 35;
        const int AbyssShardDominoGapTicks = 10;
        const int AbyssShardWaveGapTicks = 60;
        const float AbyssShardSpacing = 4 * 16f; // 4 tiles

        protected override bool CanAbyssShard => _abyssShardUnlocked;
        protected override float AbyssShardMinRange => 80f;
        protected override float AbyssShardMaxRange => 900f;
        protected override int AbyssShardChance => 12;
        protected override int AbyssShardCooldownAfterUse => 420;
        protected override int AbyssShardTelegraphTicks => 30;

        protected override void DoAbyssShardFire(int fireIndex)
        {
            Player target = Main.player[NPC.target];

            if (fireIndex == 0)
            {
                _abyssShardVariant = (AbyssShardVariant)Main.rand.Next(4);
                _abyssShardDominoDir = Main.rand.NextBool() ? -1 : 1;
                _abyssShardAnchor = target.Center;
            }

            switch (_abyssShardVariant)
            {
                // 3 shards centered on the player, 4 tiles apart, all at once.
                case AbyssShardVariant.Burst3:
                    if (fireIndex == 0)
                        SpawnShardCluster(target.Center, 3, AbyssShardSpacing);
                    break;

                // 6 shards marching left-or-right from the player, adjacent tiles, one every 10 ticks.
                case AbyssShardVariant.Domino6:
                    SpawnShardAt(_abyssShardAnchor + new Vector2(_abyssShardDominoDir * 16f * fireIndex, 0f));
                    break;

                // 4 marching one way from a fixed point, then 4 more marching back the other way.
                case AbyssShardVariant.DominoPulse4x2:
                    if (fireIndex < 4)
                        SpawnShardAt(_abyssShardAnchor + new Vector2(16f * fireIndex, 0f));
                    else
                        SpawnShardAt(_abyssShardAnchor - new Vector2(16f * (fireIndex - 4), 0f));
                    break;

                // Escalating spaced volleys: 3, then 5, then 7 - each re-centered on the player.
                case AbyssShardVariant.Escalating357:
                    int count = fireIndex switch { 0 => 3, 1 => 5, _ => 7 };
                    SpawnShardCluster(target.Center, count, AbyssShardSpacing);
                    break;
            }
        }

        protected override int NextAbyssShardDelay(int completedFireIndex)
        {
            switch (_abyssShardVariant)
            {
                case AbyssShardVariant.Burst3:
                    return -1; // single simultaneous burst

                case AbyssShardVariant.Domino6:
                    return completedFireIndex < 5 ? AbyssShardDominoGapTicks : -1;

                case AbyssShardVariant.DominoPulse4x2:
                    return completedFireIndex < 7 ? AbyssShardDominoGapTicks : -1;

                case AbyssShardVariant.Escalating357:
                    return completedFireIndex < 2 ? AbyssShardWaveGapTicks : -1;

                default:
                    return -1;
            }
        }

        void SpawnShardCluster(Vector2 center, int count, float spacing)
        {
            float startOffset = -(count - 1) / 2f * spacing;
            for (int i = 0; i < count; i++)
            {
                SpawnShardAt(center + new Vector2(startOffset + i * spacing, 0f));
            }
        }

        void SpawnShardAt(Vector2 worldPos)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            Projectile.NewProjectile(NPC.GetSource_FromThis(), worldPos, Vector2.Zero,
                ModContent.ProjectileType<Projectiles.Enemy.AbyssShard>(), AbyssShardDamage, 0f, Main.myPlayer);
        }

        // ── Homing Volley: dodgeback + overhead chop that fires one of 3 delayed-homing patterns ──
        enum HomingVolleyVariant { StaggeredFan, PincerSplit, LatticeSnap }
        HomingVolleyVariant _homingVolleyVariant;

        const int HomingVolleyOrbDamage = 30;
        const float HomingVolleyOrbSpeed = 8.5f;
        const int HomingVolleyCurveTicks = 16;

        protected override bool CanHomingVolley => true;
        protected override float HomingVolleyMinRange => 280f;
        protected override float HomingVolleyMaxRange => 650f;
        protected override int HomingVolleyChance => 10;
        protected override int HomingVolleyCooldownAfterUse => 300;

        protected override void DoHomingVolleySwingTick(int elapsed, int total)
        {
            if (Main.dedServ)
            {
                return;
            }

            // Same overhead-chop angle range the rotation sync uses, recomputed here purely for
            // the dust position - the sword itself is driven independently in InvaderNPC.cs.
            float swingT = total > 0 ? elapsed / (float)total : 1f;
            float angle = MathHelper.Lerp(MathHelper.ToRadians(-100f), MathHelper.ToRadians(70f), swingT);
            Vector2 dir = new Vector2(NPC.direction, 0f).RotatedBy(angle);
            Vector2 bladePos = NPC.Center + dir * 46f;

            if (Main.rand.NextBool(2))
            {
                Color tint = Main.rand.NextBool() ? new Color(190, 90, 255) : new Color(255, 140, 210);
                Dust d = Dust.NewDustPerfect(bladePos + Main.rand.NextVector2Circular(6f, 6f), DustID.PurpleTorch,
                    Vector2.Zero, 100, tint, Main.rand.NextFloat(1f, 1.6f));
                d.noGravity = true;
            }
        }

        protected override void DoHomingVolleyFire()
        {
            _homingVolleyVariant = (HomingVolleyVariant)Main.rand.Next(3);

            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.65f, Pitch = -0.1f }, NPC.Center);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 origin = NPC.Center + new Vector2(NPC.direction * 30f, -20f);

            switch (_homingVolleyVariant)
            {
                case HomingVolleyVariant.StaggeredFan:
                    FireStaggeredFan(origin, target);
                    break;
                case HomingVolleyVariant.PincerSplit:
                    FirePincerSplit(origin, target);
                    break;
                case HomingVolleyVariant.LatticeSnap:
                    FireLatticeSnap(origin, target);
                    break;
            }
        }

        // 5 orbs in a tight forward spread, each with its OWN curve-start delay (staggered 8 ticks
        // apart) so they bend onto the player one after another instead of all at once.
        void FireStaggeredFan(Vector2 origin, Player target)
        {
            float baseAngle = (target.Center - origin).ToRotation();
            float[] spreadDeg = { -16f, -8f, 0f, 8f, 16f };
            for (int i = 0; i < spreadDeg.Length; i++)
            {
                float angle = baseAngle + MathHelper.ToRadians(spreadDeg[i]);
                Vector2 vel = angle.ToRotationVector2() * HomingVolleyOrbSpeed;
                int straightTicks = 18 + i * 8;
                SpawnHomingOrb(origin, vel, straightTicks);
            }
        }

        // 2 orbs launched wide of the player on diverging paths - they look like a clean miss on
        // both sides, then curve inward at the SAME moment, converging from opposite sides.
        void FirePincerSplit(Vector2 origin, Player target)
        {
            float baseAngle = (target.Center - origin).ToRotation();
            float[] spreadDeg = { -30f, 30f };
            foreach (float deg in spreadDeg)
            {
                float angle = baseAngle + MathHelper.ToRadians(deg);
                Vector2 vel = angle.ToRotationVector2() * HomingVolleyOrbSpeed;
                SpawnHomingOrb(origin, vel, 32);
            }
        }

        // 6 orbs launched in a parallel wall (same heading, offset perpendicular to it) so they
        // travel straight in formation, then ALL curve at once toward wherever the player then is -
        // the wall "snaps" onto the player's position rather than converging gradually.
        void FireLatticeSnap(Vector2 origin, Player target)
        {
            float baseAngle = (target.Center - origin).ToRotation();
            Vector2 aimDir = baseAngle.ToRotationVector2();
            Vector2 perp = aimDir.RotatedBy(MathHelper.PiOver2);
            Vector2 vel = aimDir * HomingVolleyOrbSpeed;

            const int count = 6;
            const float spacing = 40f;
            float startOffset = -(count - 1) / 2f * spacing;
            for (int i = 0; i < count; i++)
            {
                Vector2 spawnPos = origin + perp * (startOffset + i * spacing);
                SpawnHomingOrb(spawnPos, vel, 34);
            }
        }

        void SpawnHomingOrb(Vector2 position, Vector2 velocity, int straightTicks)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            Projectile.NewProjectile(NPC.GetSource_FromThis(), position, velocity,
                ModContent.ProjectileType<Projectiles.Enemy.HomingAbyssOrb>(), HomingVolleyOrbDamage, 0f,
                Main.myPlayer, straightTicks, HomingVolleyCurveTicks);
        }

        // ── Boomerang Crescent: 2 variants, both using the shared overhead-chop launch ──────────
        const int BoomerangDamage = 38;
        const float BoomerangSpeed = 7f;

        protected override bool CanBoomerang => true;
        protected override float BoomerangMinRange => 60f;
        protected override float BoomerangMaxRange => 650f;
        protected override int BoomerangChance => 9;
        protected override int BoomerangCooldownAfterUse => 330;

        protected override void DoBoomerangSwingTick(int elapsed, int total)
        {
            if (Main.dedServ)
            {
                return;
            }

            float swingT = total > 0 ? elapsed / (float)total : 1f;
            float angle = MathHelper.Lerp(MathHelper.ToRadians(-100f), MathHelper.ToRadians(70f), swingT);
            Vector2 dir = new Vector2(NPC.direction, 0f).RotatedBy(angle);
            Vector2 bladePos = NPC.Center + dir * 46f;

            if (Main.rand.NextBool(2))
            {
                Color tint = Main.rand.NextBool() ? new Color(190, 90, 255) : new Color(255, 140, 210);
                Dust d = Dust.NewDustPerfect(bladePos + Main.rand.NextVector2Circular(6f, 6f), DustID.PurpleTorch,
                    Vector2.Zero, 100, tint, Main.rand.NextFloat(1f, 1.6f));
                d.noGravity = true;
            }
        }

        protected override void DoBoomerangFire()
        {
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.65f, Pitch = -0.15f }, NPC.Center);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 origin = NPC.Center + new Vector2(NPC.direction * 30f, -20f);
            float baseAngle = (target.Center - origin).ToRotation();

            if (Main.rand.NextBool())
            {
                // Mirrored Twin Loops: launched wide of the player on both sides, curling INWARD
                // across each other, then both homing back to the caster - a converging double
                // return that crosses near the player on the way back.
                float leftAngle = baseAngle - MathHelper.ToRadians(35f);
                float rightAngle = baseAngle + MathHelper.ToRadians(35f);
                SpawnBoomerang(origin, leftAngle.ToRotationVector2() * BoomerangSpeed, 1f);
                SpawnBoomerang(origin, rightAngle.ToRotationVector2() * BoomerangSpeed, -1f);
            }
            else
            {
                // Wide Solo Loop: one big crescent, curl direction random, sweeping a wide arc
                // across and past the player before returning.
                float sign = Main.rand.NextBool() ? 1f : -1f;
                float launchAngle = baseAngle - sign * MathHelper.ToRadians(45f);
                SpawnBoomerang(origin, launchAngle.ToRotationVector2() * BoomerangSpeed, sign);
            }
        }

        void SpawnBoomerang(Vector2 position, Vector2 velocity, float curveDir)
        {
            Projectile.NewProjectile(NPC.GetSource_FromThis(), position, velocity,
                ModContent.ProjectileType<Projectiles.Enemy.BoomerangCrescent>(), BoomerangDamage, 0f,
                Main.myPlayer, curveDir, NPC.whoAmI);
        }

        // ── Spiral Fan: 3 variants, a rotating-angle burst reusing the AbyssSlash crescent ───────
        enum SpiralFanVariant { SingleStream, DoubleCounterSpiral, FullRotationSweep }
        SpiralFanVariant _spiralFanVariant;
        float _spiralFanBaseAngle;
        float _spiralFanDir;

        const int SpiralFanShotDamage = 18;
        const float SpiralFanShotSpeed = 8.5f;

        protected override bool CanSpiralFan => true;
        protected override float SpiralFanMinRange => 60f;
        protected override float SpiralFanMaxRange => 650f;
        protected override int SpiralFanChance => 8;
        protected override int SpiralFanCooldownAfterUse => 360;

        protected override void DoSpiralFanSwingTick(int elapsed, int total)
        {
            if (Main.dedServ)
            {
                return;
            }

            float swingT = total > 0 ? elapsed / (float)total : 1f;
            float angle = MathHelper.Lerp(MathHelper.ToRadians(-100f), MathHelper.ToRadians(70f), swingT);
            Vector2 dir = new Vector2(NPC.direction, 0f).RotatedBy(angle);
            Vector2 bladePos = NPC.Center + dir * 46f;

            if (Main.rand.NextBool(2))
            {
                Color tint = Main.rand.NextBool() ? new Color(190, 90, 255) : new Color(255, 140, 210);
                Dust d = Dust.NewDustPerfect(bladePos + Main.rand.NextVector2Circular(6f, 6f), DustID.PurpleTorch,
                    Vector2.Zero, 100, tint, Main.rand.NextFloat(1f, 1.6f));
                d.noGravity = true;
            }
        }

        protected override void DoSpiralFanFire(int shotIndex)
        {
            Vector2 origin = NPC.Center + new Vector2(NPC.direction * 30f, -20f);

            if (shotIndex == 0)
            {
                _spiralFanVariant = (SpiralFanVariant)Main.rand.Next(3);
                _spiralFanDir = Main.rand.NextBool() ? 1f : -1f;
                Player target = Main.player[NPC.target];
                _spiralFanBaseAngle = (target.Center - origin).ToRotation();
            }

            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = 0.1f, PitchVariance = 0.1f }, NPC.Center);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            switch (_spiralFanVariant)
            {
                case SpiralFanVariant.SingleStream:
                {
                    float angleStep = MathHelper.ToRadians(18f);
                    float angle = _spiralFanBaseAngle + shotIndex * angleStep * _spiralFanDir;
                    FireSpiralShot(origin, angle);
                    break;
                }
                case SpiralFanVariant.DoubleCounterSpiral:
                {
                    float angleStep = MathHelper.ToRadians(16f);
                    float angleA = _spiralFanBaseAngle + shotIndex * angleStep * _spiralFanDir;
                    float angleB = _spiralFanBaseAngle - shotIndex * angleStep * _spiralFanDir;
                    FireSpiralShot(origin, angleA);
                    FireSpiralShot(origin, angleB);
                    break;
                }
                case SpiralFanVariant.FullRotationSweep:
                {
                    // 18 steps of 20° = a full 360° rotation, so this variant also threatens
                    // whoever's beside/behind the invader, not just the forward arc.
                    float angleStep = MathHelper.ToRadians(20f);
                    float angle = _spiralFanBaseAngle + shotIndex * angleStep * _spiralFanDir;
                    FireSpiralShot(origin, angle);
                    break;
                }
            }
        }

        protected override int NextSpiralFanDelay(int completedShotIndex)
        {
            switch (_spiralFanVariant)
            {
                case SpiralFanVariant.SingleStream:
                    return completedShotIndex < 9 ? 5 : -1;
                case SpiralFanVariant.DoubleCounterSpiral:
                    return completedShotIndex < 7 ? 5 : -1;
                case SpiralFanVariant.FullRotationSweep:
                    return completedShotIndex < 17 ? 6 : -1;
                default:
                    return -1;
            }
        }

        void FireSpiralShot(Vector2 origin, float angle)
        {
            Vector2 vel = angle.ToRotationVector2() * SpiralFanShotSpeed;
            Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, vel,
                ModContent.ProjectileType<Projectiles.Enemy.AbyssSlash>(), SpiralFanShotDamage, 0f, Main.myPlayer);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            if (NPC.HasBuff(ModContent.BuffType<Buffs.DispelShadow>()))
            {
                defenseBroken = true;
            }
            writer.Write(defenseBroken);
            writer.Write(_ringCenter.X);
            writer.Write(_ringCenter.Y);
            writer.Write(_abyssSurgeTimer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            bool receivedBrokenDef = reader.ReadBoolean();
            if (receivedBrokenDef)
            {
                defenseBroken = true;
                NPC.defense = 0;
            }
            _ringCenter = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            _abyssSurgeTimer = reader.ReadInt32();
        }

        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            //item.type == ModContent.ItemType<Items.Weapons.Melee.Shortswords.BarrowBlade>() doesn't work since Barrow Blade only damages with its projectile now, put that into its projectile below
            if (item.type == ModContent.ItemType<Items.Weapons.Melee.Broadswords.ForgottenGaiaSword>())
            {
                defenseBroken = true;
            }
            if (!defenseBroken)
            {
                if (textCooldown == 0)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Artorias.BarrowBladeHint"));
                    textCooldown = 5;
                }
                else
                {
                    textCooldown--;
                }
                CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.Artorias.Immune"), true, false);
                modifiers.SetMaxDamage(1);
            }
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (projectile.type == ModContent.ProjectileType<BarrowBladeProjectile>())
            {
                defenseBroken = true;
            }
            if (!defenseBroken)
            {
                if (textCooldown == 0)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Artorias.BarrowBladeHint"));
                    textCooldown = 5;
                }
                else
                {
                    textCooldown--;
                }
                CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.Artorias.Immune"), true, false);
                modifiers.SetMaxDamage(1);
            }
        }

        public override bool CheckActive()
        {
            return false;
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.ArtoriasBag>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.AdventureModeRule, ItemID.LargeAmethyst));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<GuardianSoul>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<WolfRing>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<SoulOfArtorias>(), 1, 6, 6));
            npcLoot.Add(notExpertCondition);
        }

        #region Gore
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Easterling Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Easterling Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Easterling Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Easterling Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Easterling Gore 3").Type, 1f);
            }
        }
        #endregion
    }
}
