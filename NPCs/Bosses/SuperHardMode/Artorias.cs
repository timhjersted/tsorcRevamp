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
        protected override float JumpSlashMinRange      => 200f;
        protected override float JumpSlashMaxRange      => 500f;
        protected override int   JumpSlashChance        => 5;
        protected override int   JumpSlashCooldownAfterUse => 420;

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
            artoriasGlobalNPC.NavSearchRadius = 100;

            // On-hit dodgeroll: hop/leap/dash away, or blink away (using the same plague-style
            // teleport set above) when able. Same bundle as the Red Knight family.
            EvasiveProfile.RedKnight(artoriasGlobalNPC);
        }

        // Proactive dodge: scans for an incoming aimed projectile and jumps/i-frame rolls it away
        // (rolls Agility above), same mechanism CursedDragonInvader uses - evasion BEFORE getting hit.
        protected override bool EvadesProjectiles => true;

        // Cursed by his own hand the moment he stepped into the Abyss - applied to everyone
        // present at the start of the fight, same pattern as Witchking.OnSpawn.
        public override void OnSpawn(IEntitySource source)
        {
            _ringCenter = NPC.Center;
            NPC.netUpdate = true;

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active)
                {
                    Main.player[i].AddBuff(ModContent.BuffType<Abyss>(), 30 * 60 * 60);
                }
            }
        }

        public override void AI()
        {
            base.AI();
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            if (NPC.HasBuff(ModContent.BuffType<Buffs.DispelShadow>()))
            {
                defenseBroken = true;
            }

            TickAbyssRing();
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
                UsefulFunctions.DustRingPrecise(_ringCenter, RingRadius, DustID.BlueTorch, 90, alpha: 40, scale: 1.6f);
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
                if (Math.Abs(dist - RingRadius) <= RingBandHalfWidth)
                {
                    int hitDir = player.Center.X < _ringCenter.X ? -1 : 1;
                    player.Hurt(PlayerDeathReason.ByNPC(NPC.whoAmI), 9999, hitDir, dodgeable: false);
                }
            }
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
                ModContent.ProjectileType<Projectiles.Enemy.AbyssSlash>(), AbyssSlashDamage, 0f, Main.myPlayer);
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
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
                    ModContent.ProjectileType<Projectiles.Enemy.ArtoriasFlameOrb>(), AbyssOrbFinisherDamage, 0f, Main.myPlayer);
            }
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
