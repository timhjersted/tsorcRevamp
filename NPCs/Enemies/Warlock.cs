using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.Armor.WarlockSet;
using tsorcRevamp.Content.Items.ConsumableSoul;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Items.Potions.RadiantLifegem;
using tsorcRevamp.Content.Items.Potions.StarlightShard;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.WarlockArcana;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    // The Warlock: a rare Hardmode cavern elite, rendered as a puppet in the Warlock armor with a short gem staff held in
    // the front hand and pointed like a wand. Its magic is PRISMATIC ARCANA: blue-violet energy refracted through floating
    // crystal prisms (Effects/WarlockArcana.fx; every spell lives in Content/Projectiles/Enemy/WarlockArcana/WarlockSpells.cs).
    // It reads straight lines and splash points, and it anchors a Ward ring that heals and shields its allies. It has no
    // ground threat, no status effects and no summons; it fades into a cloak when you close in.
    //
    // Telegraph rule (user, 2026-11-17): no line or dotted-line telegraphs. Each spell is read from the staff pose plus dust
    // and shader magic: Point (Bolt, Collapse), Level with both hands (Beam, Prism Beam), Overhead (Orrery), Conduct
    // (Crossfire), Plant (Ward).
    //
    // Spec (ticks at 60/s; damage is Expert before defense, HM / SHM):
    //   Energy Bolt      36t tell (aim locks at t24) / one bolt from the gem, 8 px/t, splashes 24px where it ends / 30t. 72/96.
    //   Great Beam       60t tell (aim locks at t42) / a 24px beam, gem to wall (768px max), live 12t / 12t glow + 24t. 72/96.
    //   Prism Beam       as the Beam, but at the lock a crystal grows in the wall it will strike, showing the bounce; both
    //                    segments live 12t. Needs a wall within 720px leaving >= 160px of bounce. 72/96 each.
    //   Orrery Volley    48t tell / 3 (SHM 4) orbs orbit the gem and launch 32t apart, each holding still 6t to lock aim,
    //                    7 px/t / 12t + 30t. 64/84.
    //   Crossfire        36t / 3 prisms on a 240px circle round the target, each turning to face them; they lock 20t apart
    //                    and fire 18t after locking, live 10t / 30t after the last. 64/84.
    //   Collapse Point   32t tell / a dark orb drifts 3.5 px/t to where the target stood, collapses 60t pulling players in
    //                    (tile-checked, weaker than walking), then bursts 72px for 10t. 72/96.
    //   Covenant Ward    90t interruptible channel / Bastion ring (320px) from t16: allies inside take 25% less damage; at t90
    //                    wounded allies inside +15%, and the Warlock +10% if at or below half (twice per spawn); the ring
    //                    stands 240t more / 12t + 30t. Readiness 300t.
    // Pacing: one offensive group at a time (no new cast while its bolts, orbs, prisms or vortices are alive, then 30t
    // grace), a 24t neutral gap (18t below half). Selection: a server-only AttackBag (no repeat unless it is the only castable spell);
    // the Ward is a readiness action dealt ahead of the bag.
    // Poise (0.35 / 30): tells are interruptible; only the last ticks before each release are committed; recoveries are
    // the punish window. The Ward channel is fully interruptible.
    class Warlock : PuppetNPC
    {
        internal enum Spell : byte
        {
            None = 0,
            Bolt,
            Beam,
            PrismBeam,
            Orrery,
            Crossfire,
            Collapse,
            Ward,
        }

        //Timings (ticks)
        const int BoltTellTicks = 36;
        const int BoltLockTick = 24;
        const int BoltTotalTicks = BoltTellTicks + 30;
        const int BeamTellTicks = 60;
        const int BeamLockTick = 42;
        const int BeamLiveTicks = 12;
        const int BeamTotalTicks = BeamTellTicks + BeamLiveTicks + 12 + 24;
        const int OrreryFirstForm = 12;
        const int OrreryFormSpacing = 10;
        const int OrreryFirstLaunch = 48;
        const int OrreryLaunchSpacing = 32;
        const int CrossfirePrismCount = 3;
        const int CrossfireTellTicks = 36;
        const int CrossfireLastFire = WarlockCrossfirePrism.FirstLockAge + WarlockCrossfirePrism.LockSpacing * (CrossfirePrismCount - 1)
            + WarlockCrossfirePrism.LockToFireTicks;
        const int CrossfireTotalTicks = CrossfireLastFire + 10 + 30;
        const int CollapseTellTicks = 32;
        const int CollapseTotalTicks = CollapseTellTicks + 30;
        const int WardStrikeTick = 16;
        const int WardChannelTicks = 90;
        const int WardTotalTicks = WardChannelTicks + 12 + 30;

        //Cooldowns after a spell ends (ticks)
        const int BoltCooldown = 60;
        const int BeamCooldown = 300;
        const int PrismCooldown = 360;
        const int OrreryCooldown = 420;
        const int CrossfireCooldown = 480;
        const int CollapseCooldown = 420;
        const int WardReadiness = 300;
        const int NeutralGapTicks = 24;
        const int NeutralGapLowLifeTicks = 18;
        const int HazardGraceTicks = 30;

        //Damage, declared as Expert damage before defense (EnemyDamage.Projectile divides once); SHM gets one flat x1.3 step
        const int BoltDamage = 72;
        const int BoltDamageSHM = 96;
        const int BeamDamage = 72;
        const int BeamDamageSHM = 96;
        const int OrbDamage = 64;
        const int OrbDamageSHM = 84;
        const int CrossfireDamage = 64;
        const int CrossfireDamageSHM = 84;
        const int CollapseDamage = 72;
        const int CollapseDamageSHM = 96;

        //Geometry
        const float BeamReach = 768f;
        const float PrismWallReach = 720f;
        const float PrismBounceReach = 640f;
        const float PrismMinBounce = 160f;
        const float CrossfireRingRadius = 240f;
        const float WardRadius = WarlockWardRing.Radius;
        const float AllyHealFraction = 0.15f;
        const float SelfHealFraction = 0.10f;
        const int SelfSharesPerSpawn = 2;
        // EnemyWarlockStaff grip is 30% up the 57px shaft, so the gem sits ~40px from the hand (same canvas as Archdeacon's).
        const float StaffTipReach = 40f;
        const float CarryRotation = -0.35f;

        //Synced cast state
        Spell spell = Spell.None;
        int spellTimer;
        int spellSerial;
        int targetIndex;
        int facing = 1;
        Vector2 aimDirection = Vector2.UnitX;
        bool aimLocked;
        bool prismBounce;          // Prism Beam: the lock found a wall with room to bounce
        Vector2 beamEnd;           // first segment's end (the wall point for a prism)
        Vector2 bounceEnd;         // second segment's end
        Vector2 wardAnchor;
        bool lowLife;
        bool superHardmode;
        int selfSharesUsed;
        int neutralGap = 60;

        //Server-only selection state (gates selection, which only the server runs; never synced)
        AttackBag<Spell> spellBag;
        readonly Dictionary<Spell, int> cooldowns = new Dictionary<Spell, int>();
        int wardReadiness = 180;
        int hazardGrace;
        bool wardSinceOffense;
        bool saidIntro;
        bool initialized;

        Player _staffPose;

        internal Spell CurrentSpell => spell;
        internal int SpellSerial => spellSerial;
        internal int OwnerKey => spellSerial * 256 + NPC.whoAmI;
        internal int OrreryCount
        {
            get
            {
                if (superHardmode)
                {
                    return 4;
                }

                return 3;
            }
        }

        bool Neutral => spell == Spell.None;
        Player Target => Main.player[targetIndex];

        #region Puppet loadout
        public override bool UsesPuppetDifficultyScaling => false;
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        protected override bool DespawnsOnPartyWipe => false;
        protected override string InvaderTitle => "Warlock";
        protected override int HeadArmorItemType => ModContent.ItemType<WarlockHeadpiece>();
        protected override int BodyArmorItemType => ModContent.ItemType<WarlockRobe>();
        protected override int LegsArmorItemType => ModContent.ItemType<WarlockBoots>();
        protected override Color PuppetSkinColor => new Color(240, 150, 110); // the old sheet's face tone, for any skin the armor doesn't hide
        protected override int MeleeWeaponItemType => -1;
        protected override int RangedWeaponItemType => -1;
        protected override int MagicWeaponItemType => ModContent.ItemType<EnemyWarlockStaff>();
        protected override int MeleeDamage => 0;
        protected override int RangedDamage => 0;
        protected override int EstusChargesMax => 0;
        protected override int CasualStrollChance => 0;
        protected override int UnblockableFinisherBlocks => 0;
        protected override bool HoldAttackSelection => true;
        protected override bool SlowDownDuringCustom => true;
        protected override bool ShowWeaponDuringNeutral => true;
        protected override float TopSpeed => 1.8f;
        protected override float Acceleration => 0.03f;
        // Staff draw: same 42x40 canvas and grip as the Archdeacon's, so the same rotation conventions.
        protected override Vector2 MagicGripNorm => new Vector2(0.30f, 0.70f);
        protected override float MagicWeaponRotationOffset => MathHelper.PiOver4;
        protected override bool MirrorMagicWeaponRotationByFacing => true;
        protected override bool UseCompositeArmForAdditionalPhase => true;
        protected override bool UseCompositeArmSwing => true;
        internal override float BodyOpacity => 1f - NPC.alpha / 255f;

        // Hyper-armor only in the last ticks before each release; every tell before that, and every recovery, is open.
        protected override bool CustomAttackCommitted
        {
            get
            {
                switch (spell)
                {
                    case Spell.Bolt:
                        return spellTimer >= BoltTellTicks - 6 && spellTimer <= BoltTellTicks;
                    case Spell.Beam:
                    case Spell.PrismBeam:
                        return spellTimer >= BeamTellTicks - 12 && spellTimer < BeamTellTicks + BeamLiveTicks;
                    case Spell.Orrery:
                        {
                            for (int i = 0; i < OrreryCount; i++)
                            {
                                int launch = OrreryFirstLaunch + OrreryLaunchSpacing * i;
                                if (spellTimer >= launch - 6 && spellTimer <= launch)
                                {
                                    return true;
                                }
                            }

                            return false;
                        }
                    case Spell.Crossfire:
                        return spellTimer >= CrossfireTellTicks - 8 && spellTimer <= CrossfireTellTicks;
                    case Spell.Collapse:
                        return spellTimer >= CollapseTellTicks - 6 && spellTimer <= CollapseTellTicks;
                }

                return false;
            }
        }

        // The rear hand: open palm behind the staff for aimed casts, braced on the shaft for the beam, reaching toward the
        // target to conduct the prisms, braced low for the plant. Null in neutral (the body frame).
        protected override float? FreeBackArmFacingAngle
        {
            get
            {
                if (Neutral)
                {
                    return null;
                }

                float aim = AimFacingAngle;
                float pose = MathHelper.PiOver2;
                switch (spell)
                {
                    case Spell.Bolt:
                    case Spell.Collapse:
                        pose = 0.9f;
                        break;
                    case Spell.Beam:
                    case Spell.PrismBeam:
                        pose = aim + 0.12f;
                        break;
                    case Spell.Orrery:
                        pose = -0.5f;
                        break;
                    case Spell.Crossfire:
                        pose = aim;
                        break;
                    case Spell.Ward:
                        pose = 1.0f;
                        break;
                }

                return MathHelper.Lerp(MathHelper.PiOver2, pose, PoseBlend);
            }
        }

        protected override void DoMeleeAttack()
        {
            // No melee: every spell is released by the Custom action clock below.
        }

        protected override void DoRangedAttack()
        {
        }
        #endregion

        public override void SetStaticDefaults()
        {
            // Keeps the old sheet's slicing for the bestiary/banner; the puppet Player draws the body in game.
            Main.npcFrameCount[NPC.type] = 15;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            AnimationType = -1;
            NPC.aiStyle = -1;
            NPC.knockBackResist = 0.1f; // overridden by the PoiseProfiles entry
            NPC.damage = 0;
            NPC.npcSlots = 7;
            NPC.defense = 5;
            NPC.height = 40;
            NPC.width = 20;
            // Authored 1,500 (Hardmode only since rev 4); the EnemyBalance registry sets the real HM / SHM values.
            NPC.lifeMax = 1500;
            NPC.scale = 1f;
            NPC.HitSound = SoundID.NPCHit37;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 15000; // rare enemy, so not dividing health by 2
            NPC.rarity = 3;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.WarlockBanner>();

            // Caster levers: remembers the last-known position, kites a loose 7-13 tile band, cloaks when threatened
            // (neutral only, see AI), and blinks to reacquire line of sight (neutral only).
            tsorcRevampGlobalNPC casterGlobalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            casterGlobalNPC.RemembersLastKnownPos = true;
            casterGlobalNPC.TeleportStyle = NPCs.TeleportStyle.Aggressive;
            casterGlobalNPC.CanTeleport = true;
            casterGlobalNPC.NavSearchRadius = 80;
            casterGlobalNPC.CanUseRopes = true;
            casterGlobalNPC.KiteRangeMin = 7f;
            casterGlobalNPC.KiteRangeMax = 13f;
            casterGlobalNPC.KiteLooseness = 0.2f;
            casterGlobalNPC.CanWalkBackwards = true;
            casterGlobalNPC.CanGoInvisible = true;
            casterGlobalNPC.InvisibleAlpha = 230;
            casterGlobalNPC.PoiseStaggerResetsAI = true;
            EvasiveProfile.EvasiveCloak(casterGlobalNPC, cloakChance: 0.15f, threatRange: 220);
            // The Ward replaces both the old passive ally heal and the old cast heal.
            casterGlobalNPC.CanHealAllies = false;
            casterGlobalNPC.CanSelfHeal = false;
        }

        // Never despawn except by timing out
        public override bool CheckActive()
        {
            return false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)spell);
            writer.Write((short)spellTimer);
            writer.Write((ushort)spellSerial);
            writer.Write((byte)targetIndex);
            writer.Write((sbyte)facing);
            writer.WriteVector2(aimDirection);
            writer.Write(aimLocked);
            writer.Write(prismBounce);
            writer.WriteVector2(beamEnd);
            writer.WriteVector2(bounceEnd);
            writer.WriteVector2(wardAnchor);
            writer.Write(lowLife);
            writer.Write(superHardmode);
            writer.Write((byte)selfSharesUsed);
            writer.Write((short)neutralGap);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            spell = (Spell)reader.ReadByte();
            spellTimer = reader.ReadInt16();
            spellSerial = reader.ReadUInt16();
            targetIndex = reader.ReadByte();
            facing = reader.ReadSByte();
            aimDirection = reader.ReadVector2();
            aimLocked = reader.ReadBoolean();
            prismBounce = reader.ReadBoolean();
            beamEnd = reader.ReadVector2();
            bounceEnd = reader.ReadVector2();
            wardAnchor = reader.ReadVector2();
            lowLife = reader.ReadBoolean();
            superHardmode = reader.ReadBoolean();
            selfSharesUsed = reader.ReadByte();
            neutralGap = reader.ReadInt16();
        }

        public override void OnStagger(NPC npc)
        {
            base.OnStagger(npc);

            // Poise break: the spell fizzles. Pending orbs and prisms see the cast end and scatter; a Ward channel's ring
            // collapses (no heal, no Bastion). Released bolts, beams and vortices finish on their own.
            if (!Neutral && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.6f, Pitch = -0.2f }, NPC.Center);
                WarlockArcanaDraw.MoteBurst(GemTip, 16, 3f, 1f);
            }

            bool wasWard = spell == Spell.Ward;
            Spell interrupted = spell;
            spell = Spell.None;
            spellTimer = 0;
            aimLocked = false;
            prismBounce = false;
            neutralGap = Math.Max(neutralGap, 60);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (interrupted != Spell.None)
                {
                    StartCooldown(interrupted);
                }

                if (wasWard)
                {
                    wardReadiness = WardReadiness;
                }

                NPC.netUpdate = true;
            }
        }

        protected override void RunMovementAI(float speedMult)
        {
            // Plants for every cast; neutral uses the shared puppet navigation, which honours the kite band.
            if (!Neutral)
            {
                NPC.velocity.X *= 0.8f;
                return;
            }

            base.RunMovementAI(speedMult);
        }

        public override void AI()
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;

            if (!initialized)
            {
                initialized = true;
                SetDisplayWeapon(MagicWeaponItemType, false);
                if (authority)
                {
                    superHardmode = tsorcRevampWorld.SuperHardMode;
                    NPC.netUpdate = true;
                }
            }

            // Announce once, from the server (or in single player): clients never roll their own line.
            if (authority && !saidIntro)
            {
                saidIntro = true;
                string line = LangUtils.GetTextValue("NPCs.Warlock.Hunt");
                if (Main.rand.NextBool())
                {
                    line = LangUtils.GetTextValue("NPCs.Warlock.Near");
                }

                UsefulFunctions.BroadcastText(line, 175, 75, 255);
            }

            NPC.timeLeft = 100;

            if (authority)
            {
                TickServerClocks(globalNPC);
            }

            // Keep the Custom phase alive for as long as a spell runs; face the cast direction throughout, and never
            // stay cloaked or blink mid-spell.
            if (!Neutral)
            {
                PhaseTimer = 2;
                NPC.target = targetIndex;
                NPC.direction = facing;
                NPC.spriteDirection = facing;
                globalNPC.IsInvisible = false;
                NPC.alpha = 0;
            }

            globalNPC.CanGoInvisible = Neutral;
            globalNPC.CanTeleport = Neutral;
            base.AI();

            if (!NPC.active)
            {
                return;
            }

            SetDisplayWeapon(MagicWeaponItemType, false);
            if (!Neutral)
            {
                NPC.direction = facing;
                NPC.spriteDirection = facing;
                globalNPC.AttackCommitted = CustomAttackCommitted;
                globalNPC.AttackTelegraphing = InTell && !globalNPC.AttackCommitted;
            }

            DebugAttackLabel = spell + "/" + spellTimer;

            if (!Main.dedServ)
            {
                // Idle identity: a faint mote off the gem now and then (the old Warlock's soft white light, now blue-violet).
                if (Main.GameUpdateCount % 8 == 0 && NPC.alpha < 200)
                {
                    WarlockArcanaDraw.Mote(GemTip + Main.rand.NextVector2Circular(3f, 3f), new Vector2(0f, -0.4f), WarlockArcanaDraw.Mid, 0.7f);
                }

                Lighting.AddLight(NPC.Center, 0.25f, 0.22f, 0.45f);
            }
        }

        // Server: cooldowns, the low-life latch, pacing gates, and selection.
        void TickServerClocks(tsorcRevampGlobalNPC globalNPC)
        {
            List<Spell> keys = new List<Spell>(cooldowns.Keys);
            foreach (Spell key in keys)
            {
                if (cooldowns[key] > 0)
                {
                    cooldowns[key]--;
                }
            }

            if (wardReadiness > 0)
            {
                wardReadiness--;
            }

            if (!lowLife && NPC.life < NPC.lifeMax / 2)
            {
                lowLife = true;
                NPC.netUpdate = true;
            }

            if (!Neutral)
            {
                return;
            }

            // One offensive group at a time: wait for our own bolts / orbs / prisms / vortices to finish, then 30t grace.
            int liveHazards = CountLiveHazards();
            if (liveHazards > 0)
            {
                hazardGrace = HazardGraceTicks;
                return;
            }

            if (hazardGrace > 0)
            {
                hazardGrace--;
                return;
            }

            if (neutralGap > 0)
            {
                if (globalNPC.StaggerTimer <= 0)
                {
                    neutralGap--;
                }

                return;
            }

            bool canCast = globalNPC.StaggerTimer <= 0 && NPC.velocity.Y == 0f && !globalNPC.IsInvisible
                && globalNPC.TeleportCountdown == 0 && globalNPC.TeleportAppearanceTimer == 0;
            if (!canCast)
            {
                return;
            }

            NPC.TargetClosest(false);
            Player player = Main.player[NPC.target];
            bool targetUsable = NPC.HasValidTarget && !player.dead && player.active && NPC.Distance(player.Center) < 1000f;
            if (targetUsable)
            {
                PickSpell();
            }
        }

        #region Selection
        void PickSpell()
        {
            spellBag ??= new AttackBag<Spell>(RefillSpellBag);
            targetIndex = NPC.target;

            // The Ward is a readiness action dealt ahead of the bag, at most once between offensive casts.
            Spell chosen = Spell.None;
            if (wardReadiness <= 0 && !wardSinceOffense && WardGateOpen())
            {
                chosen = Spell.Ward;
            }
            else if (!spellBag.TryDraw(IsEligible, out chosen))
            {
                // Nothing castable from here: keep kiting/navigating and look again shortly.
                neutralGap = 20;
                return;
            }

            BeginSpell(chosen);
        }

        // Duplicates are weight. Every spell is available from Hardmode; below half life the bag adds a second Prism Beam
        // and a second Orrery.
        void RefillSpellBag(List<Spell> cards)
        {
            cards.Add(Spell.Bolt);
            cards.Add(Spell.Bolt);
            cards.Add(Spell.Beam);
            cards.Add(Spell.PrismBeam);
            cards.Add(Spell.Orrery);
            cards.Add(Spell.Crossfire);
            cards.Add(Spell.Collapse);
            if (lowLife)
            {
                cards.Add(Spell.PrismBeam);
                cards.Add(Spell.Orrery);
            }
        }

        bool IsEligible(Spell candidate)
        {
            if (cooldowns.TryGetValue(candidate, out int cooldown) && cooldown > 0)
            {
                return false;
            }

            Player player = Main.player[NPC.target];
            float distance = NPC.Distance(player.Center);
            bool lineOfSight = Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1);
            if (!lineOfSight)
            {
                return false;
            }

            switch (candidate)
            {
                case Spell.Bolt:
                    return distance >= 80f && distance <= 800f;
                case Spell.Beam:
                    return distance >= 96f && distance <= 640f;
                case Spell.PrismBeam:
                    {
                        if (distance < 96f || distance > 640f)
                        {
                            return false;
                        }

                        // Predict the bounce from the body toward the target; the real geometry is re-checked at the lock.
                        Vector2 direction = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
                        return TryPrismGeometry(NPC.Center, direction, out _, out _);
                    }
                case Spell.Orrery:
                    return distance >= 160f && distance <= 560f;
                case Spell.Crossfire:
                    return distance >= 128f && distance <= 560f;
                case Spell.Collapse:
                    return distance >= 160f && distance <= 480f;
            }

            return false;
        }

        // Ward: at least one ally within the ring radius (any HP: Bastion alone is worth it), or the Warlock itself at or
        // below half life with a self-share left.
        bool WardGateOpen()
        {
            bool selfNeedsIt = NPC.life <= NPC.lifeMax / 2 && selfSharesUsed < SelfSharesPerSpawn;
            if (selfNeedsIt)
            {
                return true;
            }

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC ally = Main.npc[i];
                if (IsWardAlly(ally) && ally.Distance(NPC.Center) <= WardRadius)
                {
                    return true;
                }
            }

            return false;
        }

        bool IsWardAlly(NPC other)
        {
            bool basic = other.active && other.whoAmI != NPC.whoAmI && !other.friendly && !other.townNPC && !other.boss;
            if (!basic)
            {
                return false;
            }

            bool damageable = !other.dontTakeDamage && !other.immortal && other.lifeMax > 5 && !other.CountsAsACritter;
            bool wormSegment = other.realLife >= 0 && other.realLife != other.whoAmI;
            return damageable && !wormSegment;
        }

        void BeginSpell(Spell chosen)
        {
            Player player = Main.player[NPC.target];
            facing = 1;
            if (player.Center.X < NPC.Center.X)
            {
                facing = -1;
            }

            spell = chosen;
            spellTimer = 0;
            spellSerial = (spellSerial + 1) % 65536;
            aimDirection = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitX * facing);
            aimLocked = false;
            prismBounce = false;

            if (chosen == Spell.Ward)
            {
                wardSinceOffense = true;
            }
            else
            {
                wardSinceOffense = false;
            }

            StartCustomAttack(2, MagicWeaponItemType);
            NPC.netUpdate = true;
        }

        void EndSpell()
        {
            Spell finished = spell;
            spell = Spell.None;
            spellTimer = 0;
            aimLocked = false;
            StartCooldown(finished);

            neutralGap = NeutralGapTicks;
            if (lowLife)
            {
                neutralGap = NeutralGapLowLifeTicks;
            }

            EnterPhase(AttackPhase.Idle, 0);
            NPC.netUpdate = true;
        }

        void StartCooldown(Spell finished)
        {
            switch (finished)
            {
                case Spell.Bolt:
                    cooldowns[finished] = BoltCooldown;
                    break;
                case Spell.Beam:
                    cooldowns[finished] = BeamCooldown;
                    break;
                case Spell.PrismBeam:
                    cooldowns[finished] = PrismCooldown;
                    break;
                case Spell.Orrery:
                    cooldowns[finished] = OrreryCooldown;
                    break;
                case Spell.Crossfire:
                    cooldowns[finished] = CrossfireCooldown;
                    break;
                case Spell.Collapse:
                    cooldowns[finished] = CollapseCooldown;
                    break;
                case Spell.Ward:
                    wardReadiness = WardReadiness;
                    break;
            }
        }

        int TotalTicks
        {
            get
            {
                switch (spell)
                {
                    case Spell.Bolt:
                        return BoltTotalTicks;
                    case Spell.Beam:
                    case Spell.PrismBeam:
                        return BeamTotalTicks;
                    case Spell.Orrery:
                        return OrreryLastLaunch + 12 + 30;
                    case Spell.Crossfire:
                        return CrossfireTotalTicks;
                    case Spell.Collapse:
                        return CollapseTotalTicks;
                    case Spell.Ward:
                        return WardTotalTicks;
                }

                return 1;
            }
        }

        int OrreryLastLaunch => OrreryFirstLaunch + OrreryLaunchSpacing * (OrreryCount - 1);

        bool InTell
        {
            get
            {
                switch (spell)
                {
                    case Spell.Bolt:
                        return spellTimer < BoltTellTicks;
                    case Spell.Beam:
                    case Spell.PrismBeam:
                        return spellTimer < BeamTellTicks;
                    case Spell.Orrery:
                        return spellTimer < OrreryFirstLaunch;
                    case Spell.Crossfire:
                        return spellTimer < CrossfireTellTicks;
                    case Spell.Collapse:
                        return spellTimer < CollapseTellTicks;
                    case Spell.Ward:
                        return spellTimer < WardChannelTicks;
                }

                return false;
            }
        }

        int CountLiveHazards()
        {
            int bolt = ModContent.ProjectileType<WarlockEnergyBolt>();
            int orb = ModContent.ProjectileType<WarlockOrreryOrb>();
            int prism = ModContent.ProjectileType<WarlockCrossfirePrism>();
            int collapse = ModContent.ProjectileType<WarlockCollapsePoint>();
            int count = 0;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                bool ours = projectile.active && ((int)projectile.ai[0] & 255) == NPC.whoAmI;
                bool hazardType = projectile.type == bolt || projectile.type == orb || projectile.type == prism || projectile.type == collapse;
                if (ours && hazardType)
                {
                    count++;
                }
            }

            return count;
        }
        #endregion

        #region Staff pose
        // Facing-space angle of the locked (or tracked) aim: 0 = straight ahead, negative = up. Clamped to the forward
        // hemisphere the arm can reach.
        float AimFacingAngle
        {
            get
            {
                float angle = aimDirection.ToRotation();
                if (facing != 1)
                {
                    angle = MathHelper.Pi - angle;
                }

                angle = MathHelper.WrapAngle(angle);
                return MathHelper.Clamp(angle, -1.5f, 1.3f);
            }
        }

        // 0..1 blend from the carry into the spell's pose: in over its raise, out over its lowering.
        float PoseBlend
        {
            get
            {
                int raiseEnd = 20;
                int holdEnd = TotalTicks - 24;
                switch (spell)
                {
                    case Spell.Bolt:
                        raiseEnd = 20;
                        holdEnd = BoltTellTicks + 10;
                        break;
                    case Spell.Beam:
                    case Spell.PrismBeam:
                        raiseEnd = 24;
                        holdEnd = BeamTellTicks + BeamLiveTicks + 12;
                        break;
                    case Spell.Orrery:
                        raiseEnd = 20;
                        holdEnd = OrreryLastLaunch + 12;
                        break;
                    case Spell.Crossfire:
                        raiseEnd = 18;
                        holdEnd = CrossfireLastFire + 10;
                        break;
                    case Spell.Collapse:
                        raiseEnd = 18;
                        holdEnd = CollapseTellTicks + 10;
                        break;
                    case Spell.Ward:
                        raiseEnd = WardStrikeTick;
                        holdEnd = WardChannelTicks + 12;
                        break;
                }

                float raise = MathHelper.Clamp(spellTimer / (float)raiseEnd, 0f, 1f);
                float lower = MathHelper.Clamp((TotalTicks - spellTimer) / (float)Math.Max(1, TotalTicks - holdEnd), 0f, 1f);
                return MathHelper.SmoothStep(0f, 1f, Math.Min(raise, lower));
            }
        }

        // Where the gem points in facing space. A pure function of synced state, so the server and every client agree on
        // the gem position (and so on every spell origin).
        float StaffRotation
        {
            get
            {
                if (Neutral)
                {
                    return CarryRotation;
                }

                float pose = CarryRotation;
                switch (spell)
                {
                    case Spell.Bolt:
                    case Spell.Collapse:
                    case Spell.Beam:
                    case Spell.PrismBeam:
                        pose = AimFacingAngle;
                        break;
                    case Spell.Orrery:
                        pose = -MathHelper.PiOver2;
                        break;
                    case Spell.Crossfire:
                        pose = -0.75f; // higher lays the hand across the face (SwingPreview --warlock-poses)
                        break;
                    case Spell.Ward:
                        pose = 0.6f; // gem at the floor just ahead of the feet; steeper drives it into the ground
                        break;
                }

                return MathHelper.Lerp(CarryRotation, pose, PoseBlend);
            }
        }

        protected override void ModifyAdditionalPhaseWeaponRotation(ref float weaponRotation)
        {
            weaponRotation = StaffRotation;
        }

        ///<summary>The staff gem: where every spell forms and leaves. Pose maths, so it is valid on a dedicated server.</summary>
        internal Vector2 GemTip
        {
            get
            {
                // Mirrors SyncPuppet + vanilla GetFrontHandPosition with a throwaway Player, so a dedicated server (which
                // never builds the drawn puppet) gets the same tip as the clients. Same method as the Archdeacon.
                _staffPose ??= new Player();
                _staffPose.position = NPC.Bottom - new Vector2(PuppetVisualWidth, PuppetVisualHeight) + PuppetVisualOffset;
                _staffPose.width = PuppetVisualWidth;
                _staffPose.height = PuppetVisualHeight;
                _staffPose.direction = NPC.direction;
                _staffPose.gravDir = 1f;

                float angle = StaffRotation;
                float armRotation = (angle - MathHelper.PiOver2 + CompositeArmRotationOffset) * NPC.direction;
                Vector2 hand = _staffPose.GetFrontHandPosition(CompositeArmStretch, armRotation);

                float worldAngle = angle;
                if (NPC.direction != 1)
                {
                    worldAngle = MathHelper.Pi - angle;
                }

                return hand + worldAngle.ToRotationVector2() * StaffTipReach * NPC.scale;
            }
        }
        #endregion

        #region Spells
        protected override void DoCustomTick(int ticksRemaining)
        {
            if (Neutral)
            {
                return;
            }

            spellTimer++;
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            if (authority && (Target.dead || !Target.active))
            {
                EndSpell();
                return;
            }

            switch (spell)
            {
                case Spell.Bolt:
                    RunBolt(authority);
                    break;
                case Spell.Beam:
                case Spell.PrismBeam:
                    RunBeam(authority);
                    break;
                case Spell.Orrery:
                    RunOrrery(authority);
                    break;
                case Spell.Crossfire:
                    RunCrossfire(authority);
                    break;
                case Spell.Collapse:
                    RunCollapse(authority);
                    break;
                case Spell.Ward:
                    RunWard(authority);
                    break;
            }

            if (authority && !Neutral && spellTimer >= TotalTicks)
            {
                EndSpell();
            }
        }

        // Server: the aim (and facing) follows the target until `lockTick`, then freezes. Synced every 6 ticks and at the
        // lock. Turning with the target keeps the aim in front of the staff, so the shot always leaves where it points.
        void TrackAim(int lockTick)
        {
            if (aimLocked)
            {
                return;
            }

            int wantedFacing = 1;
            if (Target.Center.X < NPC.Center.X)
            {
                wantedFacing = -1;
            }

            if (wantedFacing != facing)
            {
                facing = wantedFacing;
                NPC.netUpdate = true;
            }

            aimDirection = (Target.Center - NPC.Center).SafeNormalize(Vector2.UnitX * facing);
            if (spellTimer >= lockTick)
            {
                aimLocked = true;
                NPC.netUpdate = true;
            }
            else if (spellTimer % 6 == 0)
            {
                NPC.netUpdate = true;
            }
        }

        // Motes converge on the gem from a ring of `radius`, a few per tick.
        void GatherAtGem(float radius, int count, Color color)
        {
            Vector2 gem = GemTip;
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
                WarlockArcanaDraw.Mote(gem + offset, -offset * 0.11f, color, 0.9f);
            }

            Lighting.AddLight(gem, color.ToVector3() * 0.4f);
        }

        int Damage(int hardmode, int superHardmodeValue)
        {
            int expert = hardmode;
            if (superHardmode)
            {
                expert = superHardmodeValue;
            }

            return EnemyDamage.Projectile(expert);
        }

        void RunBolt(bool authority)
        {
            if (authority)
            {
                TrackAim(BoltLockTick);
            }

            if (!Main.dedServ && spellTimer < BoltTellTicks)
            {
                if (spellTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.2f, Pitch = -0.8f }, NPC.Center);
                }

                GatherAtGem(22f, 1, WarlockArcanaDraw.Mid);
            }

            if (authority && spellTimer == BoltTellTicks)
            {
                Projectile.NewProjectile(NPC.GetSource_FromAI(), GemTip, aimDirection * 8f, ModContent.ProjectileType<WarlockEnergyBolt>(),
                    Damage(BoltDamage, BoltDamageSHM), 1f, Main.myPlayer, OwnerKey);
            }

            if (!Main.dedServ && spellTimer == BoltTellTicks)
            {
                SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.5f, Pitch = -0.4f }, NPC.Center);
            }
        }

        void RunBeam(bool authority)
        {
            if (authority)
            {
                TrackAim(BeamLockTick);
                if (spellTimer == BeamLockTick)
                {
                    LockBeamGeometry();
                }
            }

            if (!Main.dedServ && spellTimer < BeamTellTicks)
            {
                if (spellTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.6f, Pitch = -0.6f }, NPC.Center);
                }

                float progress = spellTimer / (float)BeamTellTicks;
                GatherAtGem(MathHelper.Lerp(48f, 20f, progress), 2, WarlockArcanaDraw.Mid);
                if (spellTimer == BeamLockTick)
                {
                    SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.5f, Pitch = -0.3f }, NPC.Center);
                    WarlockArcanaDraw.MoteBurst(GemTip, 10, 2f, 0.9f);
                }
            }

            if (authority && spellTimer == BeamTellTicks)
            {
                int damage = Damage(BeamDamage, BeamDamageSHM);
                int beamType = ModContent.ProjectileType<WarlockBeam>();
                Vector2 gem = GemTip;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), gem, Vector2.Zero, beamType, damage, 0f, Main.myPlayer,
                    OwnerKey, beamEnd.X, beamEnd.Y);
                if (prismBounce)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), beamEnd, Vector2.Zero, beamType, damage, 0f, Main.myPlayer,
                        OwnerKey, bounceEnd.X, bounceEnd.Y);
                }
            }
        }

        // Server, at the beam's lock: freeze where the ray meets the wall and, for a Prism Beam, the bounce. A Prism Beam
        // whose wall no longer leaves room to bounce fires as a plain beam; its crystal is spawned here.
        void LockBeamGeometry()
        {
            Vector2 gem = GemTip;
            beamEnd = WarlockGeometry.CastToWall(gem, aimDirection, BeamReach, out _);
            prismBounce = false;

            if (spell == Spell.PrismBeam && TryPrismGeometry(gem, aimDirection, out Vector2 wallPoint, out Vector2 bounce))
            {
                prismBounce = true;
                beamEnd = wallPoint;
                bounceEnd = bounce;
                float bounceAngle = (bounce - wallPoint).ToRotation();
                Projectile.NewProjectile(NPC.GetSource_FromAI(), wallPoint, Vector2.Zero, ModContent.ProjectileType<WarlockWallPrism>(),
                    0, 0f, Main.myPlayer, OwnerKey, bounceAngle);
            }

            NPC.netUpdate = true;
        }

        // A wall within reach, and an open bounce of at least PrismMinBounce off it.
        static bool TryPrismGeometry(Vector2 start, Vector2 direction, out Vector2 wallPoint, out Vector2 bounceEndPoint)
        {
            wallPoint = WarlockGeometry.CastToWall(start, direction, PrismWallReach, out bool hitWall);
            bounceEndPoint = wallPoint;
            if (!hitWall)
            {
                return false;
            }

            Vector2 reflected = WarlockGeometry.Reflect(wallPoint, direction);
            bounceEndPoint = WarlockGeometry.CastToWall(wallPoint, reflected, PrismBounceReach, out _);
            float bounceLength = Vector2.Distance(wallPoint, bounceEndPoint);
            return bounceLength >= PrismMinBounce;
        }

        void RunOrrery(bool authority)
        {
            if (!Main.dedServ && spellTimer == 1)
            {
                SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.6f, Pitch = -0.2f }, NPC.Center);
            }

            for (int i = 0; i < OrreryCount; i++)
            {
                int formTick = OrreryFirstForm + OrreryFormSpacing * i;
                if (spellTimer != formTick)
                {
                    continue;
                }

                if (authority)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), GemTip, Vector2.Zero, ModContent.ProjectileType<WarlockOrreryOrb>(),
                        Damage(OrbDamage, OrbDamageSHM), 0f, Main.myPlayer, OwnerKey, targetIndex, i);
                }

                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item25 with { Volume = 0.45f, Pitch = 0.1f * i }, NPC.Center);
                    WarlockArcanaDraw.MoteBurst(GemTip, 8, 1.6f, 0.8f);
                }
            }

            if (!Main.dedServ && spellTimer < OrreryFirstLaunch)
            {
                GatherAtGem(26f, 1, WarlockArcanaDraw.Accent);
            }
        }

        void RunCrossfire(bool authority)
        {
            if (!Main.dedServ && spellTimer < CrossfireTellTicks)
            {
                if (spellTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.6f, Pitch = 0.3f }, NPC.Center);
                }

                GatherAtGem(20f, 1, WarlockArcanaDraw.Accent);
            }

            if (!authority || spellTimer != 1)
            {
                return;
            }

            // Three prisms 60 degrees apart on a circle round the target, so their lanes cross where the target stands.
            // A spot inside a wall, or without sight of the target, flips to the opposite side; failing that it is skipped.
            Vector2 centre = Target.Center;
            float baseAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            int placed = 0;
            int prismType = ModContent.ProjectileType<WarlockCrossfirePrism>();
            int damage = Damage(CrossfireDamage, CrossfireDamageSHM);

            for (int i = 0; i < CrossfirePrismCount; i++)
            {
                float angle = baseAngle + i * MathHelper.Pi / 3f;
                Vector2 spot = centre + angle.ToRotationVector2() * CrossfireRingRadius;
                if (!OpenPrismSpot(spot, centre))
                {
                    spot = centre - angle.ToRotationVector2() * CrossfireRingRadius;
                    if (!OpenPrismSpot(spot, centre))
                    {
                        continue;
                    }
                }

                Projectile.NewProjectile(NPC.GetSource_FromAI(), spot, Vector2.Zero, prismType, damage, 0f, Main.myPlayer,
                    OwnerKey, targetIndex, placed);
                placed++;
            }

            if (placed == 0)
            {
                EndSpell();
            }
        }

        static bool OpenPrismSpot(Vector2 spot, Vector2 target)
        {
            bool inWall = Collision.SolidCollision(spot - new Vector2(10f), 20, 20);
            bool sight = Collision.CanHitLine(spot, 1, 1, target, 1, 1);
            return !inWall && sight;
        }

        void RunCollapse(bool authority)
        {
            if (!Main.dedServ && spellTimer < CollapseTellTicks)
            {
                if (spellTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item104 with { Volume = 0.5f, Pitch = -0.4f }, NPC.Center);
                }

                // Dark motes sink into the gem: this one is heavier than the bolt.
                GatherAtGem(30f, 1, WarlockArcanaDraw.Deep);
            }

            if (authority)
            {
                TrackAim(CollapseTellTicks);
            }

            if (authority && spellTimer == CollapseTellTicks)
            {
                Vector2 gem = GemTip;
                Vector2 stopPoint = Target.Center;
                Vector2 velocity = (stopPoint - gem).SafeNormalize(Vector2.UnitX * facing) * WarlockCollapsePoint.TravelSpeed;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), gem, velocity, ModContent.ProjectileType<WarlockCollapsePoint>(),
                    Damage(CollapseDamage, CollapseDamageSHM), 0f, Main.myPlayer, OwnerKey, stopPoint.X, stopPoint.Y);
            }
        }

        void RunWard(bool authority)
        {
            if (spellTimer == WardStrikeTick)
            {
                if (authority)
                {
                    // The ring stands on the floor under the planted gem (6 tiles down at most, else under the feet).
                    Vector2 gem = GemTip;
                    float floorY = WarlockGeometry.FindFloorY(gem, 6);
                    wardAnchor = new Vector2(gem.X, NPC.Bottom.Y);
                    if (floorY > 0f)
                    {
                        wardAnchor = new Vector2(gem.X, floorY);
                    }

                    Projectile.NewProjectile(NPC.GetSource_FromAI(), wardAnchor, Vector2.Zero, ModContent.ProjectileType<WarlockWardRing>(),
                        0, 0f, Main.myPlayer, OwnerKey);
                    NPC.netUpdate = true;
                }

                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.8f, Pitch = -0.2f }, NPC.Center);
                    WarlockArcanaDraw.MoteBurst(GemTip, 20, 3f, 1f);
                }
            }

            if (!Main.dedServ && spellTimer < WardChannelTicks)
            {
                GatherAtGem(18f, 1, WarlockArcanaDraw.Accent);
            }

            if (spellTimer == WardChannelTicks)
            {
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.8f, Pitch = 0.2f }, NPC.Center);
                }

                if (authority)
                {
                    PayWard();
                }
            }
        }

        // Server: +15% to every wounded ally inside the ring, and the Warlock's own share if it qualifies.
        void PayWard()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC ally = Main.npc[i];
                bool inside = IsWardAlly(ally) && Vector2.Distance(ally.Center, wardAnchor) <= WardRadius;
                if (!inside || ally.life >= ally.lifeMax)
                {
                    continue;
                }

                int amount = Math.Max(1, (int)(ally.lifeMax * AllyHealFraction));
                amount = Math.Min(amount, ally.lifeMax - ally.life);
                ally.life += amount;
                ally.HealEffect(amount);
                ally.netUpdate = true;
            }

            bool selfShare = NPC.life <= NPC.lifeMax / 2 && selfSharesUsed < SelfSharesPerSpawn;
            if (selfShare)
            {
                int amount = (int)(NPC.lifeMax * SelfHealFraction);
                amount = Math.Min(amount, NPC.lifeMax - NPC.life);
                NPC.life += amount;
                NPC.HealEffect(amount);
                selfSharesUsed++;
            }

            NPC.netUpdate = true;
        }
        #endregion

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // The gem charges for every aimed spell: an energy sphere grows at the tip over the tell and flares at the lock.
            if (!Neutral && NPC.alpha < 200)
            {
                float size = 0f;
                float charge = 0f;
                switch (spell)
                {
                    case Spell.Bolt:
                        {
                            float tell = MathHelper.Clamp(spellTimer / (float)BoltTellTicks, 0f, 1f);
                            if (spellTimer <= BoltTellTicks)
                            {
                                size = MathHelper.Lerp(6f, 26f, tell);
                                charge = tell;
                            }

                            break;
                        }
                    case Spell.Beam:
                    case Spell.PrismBeam:
                        {
                            float tell = MathHelper.Clamp(spellTimer / (float)BeamTellTicks, 0f, 1f);
                            if (spellTimer <= BeamTellTicks + BeamLiveTicks)
                            {
                                size = MathHelper.Lerp(20f, 44f, tell);
                                charge = 0.4f;
                                if (aimLocked)
                                {
                                    charge = 1f;
                                }
                            }

                            break;
                        }
                    case Spell.Crossfire:
                        size = 20f;
                        charge = 0.5f;
                        break;
                    case Spell.Ward:
                        if (spellTimer <= WardChannelTicks + 12)
                        {
                            size = 24f;
                            charge = 0.7f;
                        }

                        break;
                }

                if (size > 0f)
                {
                    WarlockArcanaDraw.Orb(GemTip, size, 1f, charge, NPC.whoAmI);
                }

                if (spell == Spell.Collapse && spellTimer <= CollapseTellTicks)
                {
                    float tell = MathHelper.Clamp(spellTimer / (float)CollapseTellTicks, 0f, 1f);
                    WarlockArcanaDraw.Vortex(GemTip, MathHelper.Lerp(12f, 56f, tell), 1f, 0.6f);
                }
            }

            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }

        #region Gore
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                WarlockArcanaDraw.MoteBurst(GemTip, 30, 4f, 1.2f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Warlock Gore 1").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Warlock Gore 2").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Warlock Gore 3").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Warlock Gore 2").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Warlock Gore 3").Type, 1.1f);
            }
        }
        #endregion

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.LifeforcePotion));
            npcLoot.Add(ItemDropRule.Common(ItemID.MagicPowerPotion, 10));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<RadiantLifegemItem>(), 3, 2, 6));
            npcLoot.Add(ItemDropRule.Common(ItemID.UnicornHorn, 3));
            IItemDropRule hmCondition = new LeadingConditionRule(new Conditions.IsHardmode());
            hmCondition.OnSuccess(ItemDropRule.Common(ItemID.SoulofLight, 1, 3, 6));
            npcLoot.Add(hmCondition);
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShardItem>(), 2));

            // took all this from an enemy that it didn't make sense for it to have
            npcLoot.Add(ItemDropRule.Common(ItemID.BattlePotion, 20));
            npcLoot.Add(ItemDropRule.Common(ItemID.WaterWalkingPotion, 20));
            npcLoot.Add(ItemDropRule.Common(ItemID.SwiftnessPotion, 10));
            npcLoot.Add(ItemDropRule.Common(ItemID.SpelunkerPotion, 10));
            npcLoot.Add(ItemDropRule.Common(ItemID.ShinePotion, 10));
            npcLoot.Add(ItemDropRule.Common(ItemID.RegenerationPotion, 10));
            npcLoot.Add(ItemDropRule.Common(ItemID.MagicPowerPotion, 10));
            npcLoot.Add(ItemDropRule.Common(ItemID.GillsPotion, 10));
            npcLoot.Add(ItemDropRule.Common(ItemID.HunterPotion, 10));
            npcLoot.Add(ItemDropRule.Common(ItemID.ArcheryPotion, 10));
            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 10));
            npcLoot.Add(ItemDropRule.Common(ItemID.BloodMoonStarter, 10));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ShockwavePotion>(), 10));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<StrengthPotion>(), 10));
            npcLoot.Add(ItemDropRule.Common(ItemID.ManaRegenerationPotion, 15));
            npcLoot.Add(ItemDropRule.Common(ItemID.GreaterHealingPotion, 10));
            npcLoot.Add(ItemDropRule.Common(ItemID.GreaterHealingPotion, 10)); //not a typo
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CrimsonPotion>(), 15));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BloodredMossClump>(), 8, 1, 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SoulCoin>(), 1, 10, 20));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<FadingSoul>(), 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CharcoalPineResin>(), 8));
        }
    }
}
