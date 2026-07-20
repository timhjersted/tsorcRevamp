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
using tsorcRevamp.Items.Armors;
using tsorcRevamp.Items.Weapons.Enemy;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    [AutoloadBossHead]
    class SoulOfCinder : PuppetNPC
    {
        public override string BossHeadTexture => "tsorcRevamp/NPCs/Bosses/SuperHardMode/SoulOfCinder_Head_Boss";

        protected override string InvaderTitle => "Soul of Cinder";

        protected override int HeadArmorItemType => ModContent.ItemType<FirelinkHelm>();
        protected override int BodyArmorItemType => ModContent.ItemType<FirelinkArmor>();
        protected override int LegsArmorItemType => ModContent.ItemType<FirelinkLeggings>();

        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemyArtoriasGreatsword>();
        protected override int RangedWeaponItemType => ModContent.ItemType<EnemySmokeBomb>();
        protected override int SecondaryRangedWeaponItemType => ModContent.ItemType<EnemyVenomStaff>();
        protected override int MagicWeaponItemType => ModContent.ItemType<EnemyMeteorStorm>();

        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Greatsword;
        protected override RangedStyle RangedAnimStyle => RangedStyle.Throw;
        protected override RangedStyle SecondaryRangedAnimStyle => RangedStyle.Crossbow;

        protected override int MeleeDamage => 95;
        protected override int RangedDamage => 58;
        protected override int SecondaryRangedDamage => 68;
        protected override int MagicDamage => 70;

        protected override float TopSpeed => _swordDead ? 3.05f : 2.65f;
        protected override float Acceleration => 0.12f;
        protected override float RunDistance => 360f;
        protected override float MeleeRange => 112f;
        protected override float StabRange => 190f;
        protected override float ComboReachBase => 138f;
        protected override float MeleeEngageRange => 132f;
        protected override float ComboMaxStartRange => 290f;
        protected override int MeleeComboChance => 85;
        protected override float ComboTelegraphMultiplier => 1.15f;
        protected override int MeleeRecoveryTicks => 32;
        protected override bool CanStab => true;
        protected override bool UseSwingEasing => true;
        protected override bool UseAlternateFlip => true;
        protected override bool UseAimAdaptiveArc => true;
        protected override bool HasSlashVFX => true;
        protected override Color SlashVFXColor => new Color(255, 120, 40);
        protected override Color MeleeTelegraphFlashColor => new Color(255, 140, 60);

        protected override float RangedRange => 780f;
        protected override float MinRangedRange => 160f;
        protected override int RangedTelegraphTicks => 42;
        protected override int RangedAttackTicks => 10;
        protected override int RangedRecoveryTicks => 54;
        protected override int RangedCooldownAfterUse => 170;
        protected override int MaxRangedBurst => 2;
        protected override int SingleRangedBurstChance => 35;
        protected override Color RangedTelegraphFlashColor => new Color(255, 95, 45);

        protected override float SecondaryRangedRange => 920f;
        protected override float SecondaryRangedMinRange => 300f;
        protected override int SecondaryRangedTelegraphTicks => 50;
        protected override int SecondaryRangedAttackTicks => 10;
        protected override int SecondaryRangedRecoveryTicks => 70;
        protected override int SecondaryRangedCooldownAfterUse => 210;
        protected override int SecondaryRangedChance => 45;
        protected override Color SecondaryRangedFlashColor => new Color(130, 255, 120);

        protected override float MagicRange => 1000f;
        protected override float MinMagicRange => 260f;
        protected override int MagicTelegraphTicks => 62;
        protected override int MagicAttackTicks => 16;
        protected override int MagicRecoveryTicks => 82;
        protected override int MagicCooldownAfterUse => 210;
        protected override int MagicPreferenceChance => 55;
        protected override Color MagicTelegraphFlashColor => new Color(180, 90, 255);

        protected override bool CanBreathe => true;
        protected override float BreathRange => 680f;
        protected override float MinBreathRange => 120f;
        protected override int BreathTelegraphTicks => 76;
        protected override int BreathDurationTicks => _swordDead ? 135 : 105;
        protected override int BreathRecoveryTicks => 70;
        protected override int BreathCooldownAfterUse => 360;
        protected override int BreathChance => 5;

        protected override bool CanPierce => true;
        protected override float PierceRange => 760f;
        protected override float MinPierceRange => 230f;
        protected override int PierceChance => 5;
        protected override int PierceTelegraphTicks => 56;
        protected override int PierceDashTicks => 34;
        protected override float PierceDashSpeed => _swordDead ? 18f : 15.5f;
        protected override int PierceRecoveryTicks => 82;
        protected override int PierceCooldownAfterUse => 300;
        protected override int PierceStabChance => 0;

        protected override bool CanJumpSlash => true;
        protected override float JumpSlashMinRange => 160f;
        protected override float JumpSlashMaxRange => 520f;
        protected override int JumpSlashChance => 4;
        protected override int JumpSlashRecoveryTicks => 72;
        protected override int JumpSlashCooldownAfterUse => 300;

        protected override bool CanFlipSlash => true;
        protected override float FlipSlashMinRange => 130f;
        protected override float FlipSlashMaxRange => 470f;
        protected override int FlipSlashChance => 4;
        protected override int FlipSlashCooldownAfterUse => 360;

        protected override bool CanHomingVolley => true;
        protected override float HomingVolleyMinRange => 280f;
        protected override float HomingVolleyMaxRange => 760f;
        protected override int HomingVolleyChance => 7;
        protected override int HomingVolleyRecoveryTicks => 88;
        protected override int HomingVolleyCooldownAfterUse => 280;

        protected override bool CanBoomerang => true;
        protected override float BoomerangMinRange => 120f;
        protected override float BoomerangMaxRange => 760f;
        protected override int BoomerangChance => 7;
        protected override int BoomerangRecoveryTicks => 92;
        protected override int BoomerangCooldownAfterUse => 320;

        protected override bool CanSpiralFan => true;
        protected override float SpiralFanMinRange => 220f;
        protected override float SpiralFanMaxRange => 850f;
        protected override int SpiralFanChance => 7;
        protected override int SpiralFanRecoveryTicks => 95;
        protected override int SpiralFanCooldownAfterUse => 360;

        public static readonly float ARENA_WIDTH = 864;
        public static readonly float ARENA_HEIGHT = 656;
        //ARENA_LOCATION_ADVENTURE removed: this boss is currently a copy of the old Gwyn boss and has no real
        //arena location yet — a proper event location is pending. Re-add (routed through ExpandedWorldTransform,
        //legacy 2000-space) once that's designed.

        const int NormalDefense = 550;
        const int SwordBrokenDefense = 130;
        const float ProtectionRadius = 1000f;
        const float KillRingRadius = 2000f;
        const int CowardGraceTicks = 90;

        const int DarkBeadDamage = 36;
        const int PhantomSeekerDamage = 55;
        const int SmokeBombDamage = 58;
        const int BioSpitDamage = 68;
        const int BioSpitFinalDamage = 76;
        const int PlasmaOrbDamage = 70;
        const int CursedBreathDamage = 68;
        const int FireBreathDamage = 50;
        const int IceStormDamage = 40;
        const int DisruptDamage = 68;
        const int LostSoulDamage = 38;
        const int CultistFireDamage = 41;
        const int CultistMagicDamage = 62;
        const int GravityBallDamage = 66;
        const int TridentDamage = 43;
        const int OrangeProjDamage = 45;
        const int SwordProjectileDamage = 69;

        NPCDespawnHandler despawnHandler;
        bool _swordSpawned;
        bool _swordDead;
        bool _announcedSwordBreak;
        int _protectionTextCooldown;
        readonly int[] _cowardTimers = new int[Main.maxPlayers];
        CinderRangedAttack _queuedRangedAttack = CinderRangedAttack.DarkBead;
        CinderMagicAttack _queuedMagicAttack = CinderMagicAttack.IceStorm;
        int _magicBurstsRemaining;
        int _magicBurstTimer;

        enum CinderRangedAttack
        {
            SmokeBomb,
            DarkBead,
            PhantomSeeker,
            BioSpit,
            PlasmaCross
        }

        enum CinderMagicAttack
        {
            IceStorm,
            PhasedMatterBlast,
            DemonFireLob,
            CultistFire,
            LostSoulCurse
        }

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
            NPC.npcSlots = 10;
            NPC.height = 40;
            NPC.width = 30;
            NPC.scale = 1.15f;
            NPC.damage = 0;
            NPC.defense = NormalDefense;
            NPC.lifeMax = 750000;
            NPC.knockBackResist = 0f;
            NPC.boss = true;
            NPC.netAlways = true;
            NPC.lavaImmune = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 2000000;
            NPC.rarity = 47;
            Music = 12;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.SoulOfCinder.DespawnHandler"), Color.OrangeRed, 6);

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.Agility = 0.25f;
            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = NPCs.TeleportStyle.Aggressive;
            globalNPC.TeleportVisualStyle = NPCs.TeleportVisualStyle.Fire;
            globalNPC.NavSearchRadius = 80;
            EvasiveProfile.RedKnight(globalNPC);
        }

        public override void OnSpawn(IEntitySource source)
        {
            SpawnSwordOfCinder();
        }

        public override void AI()
        {
            base.AI();
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            NPC.TargetClosest(true);

            SpawnSwordOfCinder();
            TickSwordState();
            TickDistanceRules();
        }

        void SpawnSwordOfCinder()
        {
            if (_swordSpawned || Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            int swordID = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<SwordOfCinder>(), NPC.whoAmI);
            Main.npc[swordID].velocity.Y = -10f;
            Main.npc[swordID].netUpdate = true;
            _swordSpawned = true;
            NPC.netUpdate = true;
        }

        void TickSwordState()
        {
            if (_swordDead)
            {
                return;
            }

            if (_swordSpawned && !NPC.AnyNPCs(ModContent.NPCType<SwordOfCinder>()))
            {
                _swordDead = true;
                NPC.defense = SwordBrokenDefense;
                if (!_announcedSwordBreak)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Gwyn.SwordShattered"), 150, 75, 255);
                    _announcedSwordBreak = true;
                }
                NPC.netUpdate = true;
            }
        }

        void TickDistanceRules()
        {
            UsefulFunctions.DustRing(NPC.Center, (int)ProtectionRadius, DustID.BlueTorch, 20, 1f);
            UsefulFunctions.DustRing(NPC.Center, (int)KillRingRadius, DustID.RedsWingsRun, 1, 1f);
            UsefulFunctions.DustRing(NPC.Center, (int)KillRingRadius, DustID.Torch, 10, 1f);
            UsefulFunctions.DustRing(NPC.Center, (int)KillRingRadius, DustID.RedTorch, 5, 2f);
            UsefulFunctions.DustRing(NPC.Center, (int)KillRingRadius, DustID.Firefly, 100, -3f);

            bool protectedByDistance = NPC.HasValidTarget && NPC.Distance(Main.player[NPC.target].Center) > ProtectionRadius;
            if (protectedByDistance)
            {
                NPC.defense = 9999;
                if (_protectionTextCooldown <= 0)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Gwyn.Protected"), 175, 75, 255);
                    _protectionTextCooldown = 200;
                }
            }
            else if (_protectionTextCooldown <= 0)
            {
                NPC.defense = _swordDead ? SwordBrokenDefense : NormalDefense;
            }

            if (_protectionTextCooldown > 0)
            {
                _protectionTextCooldown--;
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead)
                {
                    _cowardTimers[i] = 0;
                    continue;
                }

                float distance = NPC.Distance(player.Center);
                if (distance < KillRingRadius)
                {
                    player.AddBuff(ModContent.BuffType<TornWings>(), 60, false);
                }
                if (distance < 700f)
                {
                    player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 60, false);
                }

                if (distance > KillRingRadius)
                {
                    _cowardTimers[i]++;
                    if (_cowardTimers[i] >= CowardGraceTicks)
                    {
                        player.AddBuff(ModContent.BuffType<CowardsAffliction>(), 30, false);
                        if (_cowardTimers[i] == CowardGraceTicks)
                        {
                            UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Gwyn.Coward"), 235, 199, 23);
                        }
                    }
                }
                else
                {
                    _cowardTimers[i] = 0;
                }
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.OnFire, 10 * 60, false);
            target.AddBuff(ModContent.BuffType<FracturingArmor>(), 40 * 60, false);
            target.AddBuff(ModContent.BuffType<SlowedLifeRegen>(), 30 * 60, false);
            target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 30 * 60, false);
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.Weak, 10 * 60, false);
                target.AddBuff(BuffID.BrokenArmor, 3 * 60, false);
            }
        }

        protected override void DoMeleeAttack()
        {
            PlayMeleeSwingSound();
            TryMeleeHit(MeleeRange);
        }

        protected override void DoStabAttack()
        {
            SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.65f, PitchVariance = 0.15f }, NPC.Center);
            TryMeleeHit(StabRange * 0.7f);
        }

        protected override void DoPierceDashTick()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || Main.GameUpdateCount % 8 != 0)
            {
                return;
            }

            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.position, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.Gwyn.TackleProjectile>(), MeleeDamage + 7, 1f, Main.myPlayer, 10, NPC.whoAmI);
        }

        protected override void OnRangedBurstStarted(bool secondary)
        {
            Player target = Main.player[NPC.target];
            if (secondary)
            {
                _queuedRangedAttack = NPC.life < NPC.lifeMax / 3 ? CinderRangedAttack.PlasmaCross : CinderRangedAttack.BioSpit;
                return;
            }

            float dist = NPC.Distance(target.Center);
            if (dist < 260f)
            {
                _queuedRangedAttack = CinderRangedAttack.SmokeBomb;
                SoundEngine.PlaySound(UsefulFunctions.BombFuse with { Volume = 0.6f }, NPC.Center);
            }
            else if (_swordDead && Main.rand.NextBool(3))
            {
                _queuedRangedAttack = CinderRangedAttack.PhantomSeeker;
            }
            else
            {
                _queuedRangedAttack = CinderRangedAttack.DarkBead;
            }
        }

        protected override void DoRangedAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            switch (_queuedRangedAttack)
            {
                case CinderRangedAttack.SmokeBomb:
                    FireSmokeBomb(target);
                    break;
                case CinderRangedAttack.PhantomSeeker:
                    FirePhantomSeeker(target);
                    break;
                case CinderRangedAttack.BioSpit:
                    FireBioSpit(target, NPC.life <= NPC.lifeMax / 5 ? BioSpitFinalDamage : BioSpitDamage);
                    break;
                case CinderRangedAttack.PlasmaCross:
                    FirePlasmaCross(target);
                    break;
                default:
                    FireDarkBead(target);
                    break;
            }
        }

        void FireSmokeBomb(Player target)
        {
            Vector2 velocity = UsefulFunctions.BallisticTrajectory(NPC.Center, target.Center, 10f);
            velocity += Main.rand.NextVector2Circular(-4f, 0f);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, velocity, ModContent.ProjectileType<Projectiles.Enemy.EnemySmokebomb>(), SmokeBombDamage, 0f, Main.myPlayer);
            SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.8f, PitchVariance = 0.3f }, NPC.Center);
        }

        void FireDarkBead(Player target)
        {
            Vector2 baseVelocity = UsefulFunctions.Aim(NPC.Center, target.Center, 7f);
            for (int i = -1; i <= 1; i++)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, baseVelocity.RotatedBy(MathHelper.ToRadians(i * 9f)), ModContent.ProjectileType<Projectiles.Enemy.ArtoriasDarkBead>(), DarkBeadDamage, 0f, Main.myPlayer);
            }
            SoundEngine.PlaySound(SoundID.Item80 with { Volume = 0.4f, Pitch = 0.1f }, NPC.Center);
        }

        void FirePhantomSeeker(Player target)
        {
            int id = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(20f, 20f), Main.rand.NextVector2Circular(5f, 5f), ModContent.ProjectileType<Projectiles.Enemy.BurningPhantomSeeker>(), PhantomSeekerDamage, 0f, Main.myPlayer);
            Main.projectile[id].timeLeft = 460;
            Main.projectile[id].rotation = Main.rand.Next(700) / 100f;
            Main.projectile[id].ai[0] = target.whoAmI;
            SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
        }

        void FireBioSpit(Player target, int damage)
        {
            Vector2 velocity = UsefulFunctions.BallisticTrajectory(NPC.Center, target.Center, 10f);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, velocity, ModContent.ProjectileType<Projectiles.Enemy.EnemyBioSpitBall>(), damage, 5f, Main.myPlayer);
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = 0.5f }, NPC.Center);
        }

        void FirePlasmaCross(Player target)
        {
            const float speed = 6f;
            Vector2 origin = NPC.Center;
            Vector2 aimed = UsefulFunctions.Aim(origin, target.Center, speed);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, aimed, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), PlasmaOrbDamage, 0f, Main.myPlayer);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, new Vector2(speed, speed), ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), PlasmaOrbDamage, 0f, Main.myPlayer);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, new Vector2(-speed, speed), ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), PlasmaOrbDamage, 0f, Main.myPlayer);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, new Vector2(speed, -speed), ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), PlasmaOrbDamage, 0f, Main.myPlayer);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, new Vector2(-speed, -speed), ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), PlasmaOrbDamage, 0f, Main.myPlayer);
            SoundEngine.PlaySound(SoundID.Item79 with { Volume = 0.3f }, NPC.Center);
        }

        protected override void DoMagicAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            float hpFrac = NPC.life / (float)NPC.lifeMax;
            if (hpFrac <= 0.20f)
            {
                _queuedMagicAttack = Main.rand.NextBool() ? CinderMagicAttack.CultistFire : CinderMagicAttack.DemonFireLob;
            }
            else if (hpFrac <= 0.50f)
            {
                _queuedMagicAttack = Main.rand.NextBool() ? CinderMagicAttack.PhasedMatterBlast : CinderMagicAttack.LostSoulCurse;
            }
            else
            {
                _queuedMagicAttack = CinderMagicAttack.IceStorm;
            }

            _magicBurstsRemaining = hpFrac <= 0.33f ? 3 : 2;
            _magicBurstTimer = 1;
            _magicAttackTicksOverride = 56;
            FireQueuedMagic(Main.player[NPC.target]);
        }

        protected override void DoMagicTick(int ticksRemaining)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || _magicBurstsRemaining <= 1)
            {
                return;
            }

            _magicBurstTimer++;
            if (_magicBurstTimer >= 18)
            {
                _magicBurstTimer = 0;
                _magicBurstsRemaining--;
                FireQueuedMagic(Main.player[NPC.target]);
            }
        }

        void FireQueuedMagic(Player target)
        {
            switch (_queuedMagicAttack)
            {
                case CinderMagicAttack.PhasedMatterBlast:
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.BallisticTrajectory(NPC.Center, target.Center, 6f, 1.06f, true, true), ModContent.ProjectileType<Projectiles.Enemy.Okiku.PhasedMatterBlast>(), DisruptDamage, 5f, Main.myPlayer);
                    SoundEngine.PlaySound(SoundID.Item79 with { Volume = 0.2f, Pitch = 0.4f }, NPC.Center);
                    break;
                case CinderMagicAttack.DemonFireLob:
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.BallisticTrajectory(NPC.Center, target.Center, 5f), ProjectileID.DD2DrakinShot, FireBreathDamage, 0f, Main.myPlayer);
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);
                    break;
                case CinderMagicAttack.CultistFire:
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, target.Center, 0.1f), Main.rand.NextBool() ? ProjectileID.CultistBossFireBall : ProjectileID.CultistBossFireBallClone, Main.rand.NextBool() ? CultistFireDamage : CultistMagicDamage, 0.1f, Main.myPlayer);
                    SoundEngine.PlaySound(SoundID.NPCHit34, NPC.Center);
                    break;
                case CinderMagicAttack.LostSoulCurse:
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.BallisticTrajectory(NPC.Center, target.Center, 8f, 1.06f, true, true), ProjectileID.DesertDjinnCurse, LostSoulDamage, 7f, Main.myPlayer);
                    SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.6f, Pitch = 0.5f }, NPC.Center);
                    break;
                default:
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.BallisticTrajectory(NPC.Center, target.Center, 14f, 0.015f, true), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellIce3Ball>(), IceStormDamage, 0f, Main.myPlayer);
                    SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.3f }, NPC.Center);
                    break;
            }
        }

        protected override void DoBreathWindup(int elapsed)
        {
            if (Main.dedServ)
            {
                return;
            }

            float t = MathHelper.Clamp(elapsed / (float)BreathTelegraphTicks, 0f, 1f);
            float radius = MathHelper.Lerp(60f, 12f, t);
            UsefulFunctions.DustRing(NPC.Center, (int)radius, NPC.life > NPC.lifeMax * 0.8f ? DustID.CursedTorch : DustID.Torch, 24, 2f);
            Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * (1f + t * 3f));
        }

        protected override void OnBreathStart()
        {
            SoundEngine.PlaySound(NPC.life > NPC.lifeMax * 0.8f ? SoundID.NPCHit30 : new SoundStyle("tsorcRevamp/Sounds/DarkSouls/breath1") with { Volume = 0.5f }, NPC.Center);
        }

        protected override void DoBreathTick(int ticksRemaining)
        {
            Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 2f);
            if (Main.netMode == NetmodeID.MultiplayerClient || ticksRemaining % 3 != 0)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 breathVel = UsefulFunctions.Aim(NPC.Center, target.Center, 9f) + Main.rand.NextVector2Circular(1.5f, 1.5f);
            int type = NPC.life > NPC.lifeMax * 0.8f ? ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedBreath>() : ModContent.ProjectileType<Projectiles.Enemy.FireBreath>();
            int damage = NPC.life > NPC.lifeMax * 0.8f ? CursedBreathDamage : FireBreathDamage;
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(5f * NPC.direction, -12f), breathVel, type, damage, 0f, Main.myPlayer);
            SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.12f, Pitch = 0.2f }, NPC.Center);
        }

        protected override void DoHomingVolleyFire()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            if (_swordDead)
            {
                for (int i = 0; i < 5; i++)
                {
                    Vector2 targetPoint = target.Center + new Vector2(-480 + 240 * i, 0f);
                    Vector2 velocity = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPoint, 12f, 0.1f, true, true);
                    if (velocity != Vector2.Zero && Math.Abs(velocity.X) < -velocity.Y)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, velocity, ModContent.ProjectileType<Projectiles.Enemy.EarthTrident>(), TridentDamage, 0.5f, Main.myPlayer);
                    }
                }
            }
            else
            {
                Vector2 velocity = UsefulFunctions.BallisticTrajectory(NPC.Center, target.Center, 6f, 0.1f, true, true);
                if (velocity != Vector2.Zero && Math.Abs(velocity.X) < -velocity.Y)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, velocity + target.velocity / 1.5f, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellLightning4Ball>(), GravityBallDamage, 0.5f, Main.myPlayer);
                }
            }
        }

        protected override void DoBoomerangFire()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, NPC.velocity, ModContent.ProjectileType<Projectiles.Enemy.Cinder.SwordOfCinder>(), SwordProjectileDamage, 0.5f, Main.myPlayer, NPC.whoAmI);
        }

        protected override void DoSpiralFanFire(int shotIndex)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 origin = NPC.Center + new Vector2(0f, -64f);
            int bulletCount = _swordDead ? 7 : 5;
            float spread = _swordDead ? 67f : 52.5f;
            Vector2 baseVelocity = UsefulFunctions.Aim(origin, target.Center, 24f);
            for (int i = -(bulletCount / 2); i <= bulletCount / 2; i++)
            {
                Vector2 velocity = baseVelocity.RotatedBy(MathHelper.ToRadians(-((spread / (bulletCount - 1)) * i)));
                Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, velocity, ModContent.ProjectileType<Projectiles.Enemy.Gwyn.FarronHailSpawner>(), OrangeProjDamage, 1f, Main.myPlayer, 0, 30);
            }
        }

        protected override int NextSpiralFanDelay(int completedShotIndex)
        {
            return completedShotIndex < (_swordDead ? 5 : 3) ? 30 : -1;
        }

        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (_swordDead)
            {
                modifiers.FinalDamage *= 1.1f;
            }
            else
            {
                modifiers.FinalDamage *= 0.7f;
            }
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (_swordDead)
            {
                modifiers.FinalDamage *= 1.1f;
            }
            else
            {
                modifiers.FinalDamage *= 0.7f;
            }

            if (projectile.minion)
            {
                modifiers.Knockback *= 0f;
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
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.SoulOfCinderBag>()));
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(_swordSpawned);
            writer.Write(_swordDead);
            writer.Write(_announcedSwordBreak);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            _swordSpawned = reader.ReadBoolean();
            _swordDead = reader.ReadBoolean();
            _announcedSwordBreak = reader.ReadBoolean();
        }
    }
}
