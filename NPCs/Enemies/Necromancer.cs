using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Accessories.Mobility;
using tsorcRevamp.Content.Items.Armor.Enemy.NecromancerSet;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Items.Potions.StarlightShard;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Necromancer;
using tsorcRevamp.NPCs.Enemies.Dworc;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    // A kiting ritual caster rendered as a puppet in the Necromancer armor, bare bone-gauntleted hands, no staff.
    // It keeps an 8-18 tile band, raises up to four Spellbound Ghouls, pressures with bone fans and grave hands,
    // drains life through Soul Siphon, EATS one of its own ghouls to heal (kill them to deny it - they detonate
    // either way), and shoves you off with a curse nova when cornered. Its ghouls dissolve when it dies.
    //
    // Spell colour language: PURPLE shadowflame = curse (Death Strike / Grave Hands / Nova),
    // GREEN cursed-flame = soul & healing (Siphon / Sacrifice), WHITE bone = the fan.
    // Every tell is dust in the hands plus an arm pose - no drawn telegraph lines.
    //
    // Poise contract (0.4 / 26 in PoiseProfiles): short casts are hyper-armored through their tell; the two
    // RITUALS (Soul Siphon, Sacrifice Heal) are staggerable for their first two thirds - a poise break fizzles them.
    class Necromancer : PuppetNPC
    {
        enum AttackState : byte
        {
            None = 0,
            DeathStrike,   // the classic wall-piercing curse sigil
            BoneFan,       // 5 ricocheting bone shards in a fan
            GraveHands,    // three shadow claws erupt under the player; the third leads their escape
            SoulSiphon,    // ritual: drains the player's life into itself while range + LOS hold
            RaiseGhoul,    // ritual: a Spellbound Ghoul claws out of the ground
            SacrificeHeal, // ritual: consumes one of its own ghouls to heal
            CurseNova,     // below 50%, point-blank: shadow ring that buys the caster its distance
        }

        //Timings (ticks)
        const int StrikeTelegraphTicks = 30;
        const int FanTelegraphTicks = 25;
        const int HandsCastTicks = 75;
        const int SiphonChannelTicks = 90;
        const int SiphonStaggerableTicks = 60;  //first two thirds: a poise break cancels the drain
        const int RaiseRitualTicks = 45;
        const int SacrificeChannelTicks = 60;
        const int SacrificeStaggerableTicks = 40; //likewise interruptible
        const int NovaTelegraphTicks = 25;

        //Projectile damage, declared as Expert damage before defense (EnemyDamage.Projectile divides by 4)
        const int StrikeExpertDamage = 72;
        const int BoneExpertDamage = 56;
        const int HandExpertDamage = 60;
        const int SiphonPulseExpertDamage = 32;
        const int NovaExpertDamage = 60;

        //Heals scale with max HP so they matter in every tier (lifeMax already includes registry + progression scaling)
        const float SiphonHealFraction = 0.03f;    //per pulse; 4 pulses = 12% per full channel
        const float SacrificeHealFraction = 0.20f;
        const int MaxGhouls = 4;                   //per Necromancer
        const float DeathStrikeMaxRange = 352f;    //the shared sigil dies with its 12-frame animation (~48t at 8px/t = 384px)

        AttackState State = AttackState.None;
        int AttackTimer;
        int AttackCooldown = 90;
        int SacrificeTarget = -1; //whoAmI of the ghoul being consumed (synced for the tether visual)
        int targetIndex;

        // Server-only selection state: the bag itself is never synced, only the State it deals.
        AttackBag<AttackState> attackBag;
        int castsSinceNova = 10;
        int castsSinceSacrifice = 10;
        int spawnToken; //server-local DworcSpawnIdentity token; ghouls compare against it on the server only

        #region Puppet loadout
        public override bool UsesPuppetDifficultyScaling => false;
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        protected override bool DespawnsOnPartyWipe => false;
        protected override string InvaderTitle => "Necromancer";
        protected override int HeadArmorItemType => ModContent.ItemType<NecromancerHood>();
        protected override int BodyArmorItemType => ModContent.ItemType<NecromancerRobe>();
        protected override int LegsArmorItemType => ModContent.ItemType<NecromancerBoots>();
        protected override Color PuppetSkinColor => new Color(235, 115, 83); // the hood sheet's face tone, for any skin the armor doesn't hide
        protected override int MeleeWeaponItemType => -1;
        protected override int RangedWeaponItemType => -1;
        protected override int MagicWeaponItemType => -1;
        protected override int MeleeDamage => 0;
        protected override int RangedDamage => 0;
        protected override int EstusChargesMax => 0;
        protected override int CasualStrollChance => 0;
        protected override int UnblockableFinisherBlocks => 0;
        protected override bool HoldAttackSelection => true;
        protected override bool SlowDownDuringCustom => true;
        protected override float TopSpeed => 1.5f;
        protected override float Acceleration => 0.07f;

        // Short casts are hyper-armored through their tell; the two rituals only for their final third.
        protected override bool CustomAttackCommitted
        {
            get
            {
                switch (State)
                {
                    case AttackState.DeathStrike:
                        return AttackTimer <= StrikeTelegraphTicks;
                    case AttackState.BoneFan:
                        return AttackTimer <= FanTelegraphTicks;
                    case AttackState.GraveHands:
                        return AttackTimer <= HandsCastTicks - 20;
                    case AttackState.SoulSiphon:
                        return AttackTimer >= SiphonStaggerableTicks && AttackTimer <= SiphonChannelTicks;
                    case AttackState.RaiseGhoul:
                        return AttackTimer <= RaiseRitualTicks;
                    case AttackState.SacrificeHeal:
                        return AttackTimer >= SacrificeStaggerableTicks && AttackTimer <= SacrificeChannelTicks;
                    case AttackState.CurseNova:
                        return AttackTimer <= NovaTelegraphTicks;
                }

                return false;
            }
        }

        protected override float? FreeFrontArmFacingAngle
        {
            get
            {
                if (State == AttackState.None)
                {
                    return null;
                }

                return FrontArmPose;
            }
        }

        protected override float? FreeBackArmFacingAngle
        {
            get
            {
                if (State == AttackState.None)
                {
                    return null;
                }

                return BackArmPose;
            }
        }

        protected override void DoMeleeAttack()
        {
            // Empty hands: every spell is released by the Custom action clock below.
        }

        protected override void DoRangedAttack()
        {
        }
        #endregion

        public override void SetStaticDefaults()
        {
            // Keeps the old sheet's slicing for the bestiary/banner; the puppet Player draws the body in game.
            Main.npcFrameCount[NPC.type] = 15;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            AnimationType = -1;
            NPC.knockBackResist = 0.2f; //overridden by the PoiseProfiles entry
            NPC.aiStyle = -1;
            NPC.damage = 0;
            NPC.defense = 25;
            NPC.height = 40;
            NPC.width = 20;
            NPC.scale = 1f;
            NPC.lifeMax = 800;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 4000;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.NecromancerBanner>();

            // Caster levers: remember last-known position; pace when it patrols; kite an 8-18 tile band and
            // backpedal rather than march into melee. Teleport is the navigation unstick, neutral-only (see AI).
            tsorcRevampGlobalNPC casterGlobalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            casterGlobalNPC.RemembersLastKnownPos = true;
            casterGlobalNPC.PatrolMode = NPCs.PatrolMode.Pace;
            casterGlobalNPC.NavSearchRadius = 80;
            casterGlobalNPC.KiteRangeMin = 8f;
            casterGlobalNPC.KiteRangeMax = 18f;
            casterGlobalNPC.KiteLooseness = 0.2f;
            casterGlobalNPC.CanWalkBackwards = true;
            casterGlobalNPC.CanTeleport = true;
            casterGlobalNPC.CanUseRopes = true;
            casterGlobalNPC.CanSelfHeal = false;
            casterGlobalNPC.CanHealAllies = false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)State);
            writer.Write(AttackTimer);
            writer.Write(AttackCooldown);
            writer.Write((short)SacrificeTarget);
            writer.Write((byte)targetIndex);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            State = (AttackState)reader.ReadByte();
            AttackTimer = reader.ReadInt32();
            AttackCooldown = reader.ReadInt32();
            SacrificeTarget = reader.ReadInt16();
            targetIndex = reader.ReadByte();
        }

        ///<summary>Its casting palm - where every spell forms and leaves. Pose maths, so it is valid on a dedicated server.</summary>
        Vector2 Palm => PuppetFrontHandPositionAt(FrontArmPose);

        ///<summary>The off hand, for client-side dust only (the dedicated server has no drawn puppet to read).</summary>
        Vector2 OffPalm => PuppetBackHandPositionAt(BackArmPose);

        Player Target => Main.player[targetIndex];

        public override void OnStagger(NPC npc)
        {
            base.OnStagger(npc);

            //Poise break: the working fizzles
            if (State != AttackState.None && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.6f, Pitch = -0.4f }, NPC.Center);
                for (int i = 0; i < 14; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2Circular(3.5f, 3.5f);
                    int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Shadowflame, velocity.X, velocity.Y, 90, default, 1.3f);
                    Main.dust[dust].noGravity = true;
                }
            }

            State = AttackState.None;
            AttackTimer = 0;
            SacrificeTarget = -1;
            AttackCooldown = Math.Max(AttackCooldown, 70);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.netUpdate = true;
            }
        }

        protected override void RunMovementAI(float speedMult)
        {
            // Rooted while casting (SlowDownDuringCustom also brakes); neutral uses the shared puppet navigation,
            // which honours the kite band set in SetDefaults.
            if (State != AttackState.None)
            {
                NPC.velocity.X *= 0.85f;
                return;
            }

            base.RunMovementAI(speedMult);
        }

        public override void AI()
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;

            if (authority && spawnToken == 0)
            {
                spawnToken = NPC.GetGlobalNPC<DworcSpawnIdentity>().Token;
            }

            if (authority && State == AttackState.None)
            {
                if (AttackCooldown > 0)
                {
                    AttackCooldown--;
                }

                bool canCast = AttackCooldown <= 0 && globalNPC.StaggerTimer <= 0 && NPC.velocity.Y == 0f
                    && globalNPC.TeleportCountdown == 0 && globalNPC.TeleportAppearanceTimer == 0;
                if (canCast)
                {
                    NPC.TargetClosest(false);
                    Player player = Main.player[NPC.target];
                    bool targetUsable = NPC.HasValidTarget && !player.dead && player.active && NPC.Distance(player.Center) < 1000f;
                    if (targetUsable)
                    {
                        PickAttack();
                    }
                }
            }

            // Keep the Custom phase alive for as long as a spell runs; face the target throughout.
            if (State != AttackState.None)
            {
                PhaseTimer = 2;
                NPC.target = targetIndex;
                FaceTarget();
            }

            globalNPC.CanTeleport = State == AttackState.None;
            base.AI();

            if (State != AttackState.None)
            {
                FaceTarget();
            }

            DebugAttackLabel = State + "/" + AttackTimer;

            //Idle identity: stray shadow wisps - a thing that shouldn't be walking
            if (!Main.dedServ)
            {
                if (Main.rand.NextBool(4))
                {
                    Vector2 position = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                    int wisp = Dust.NewDust(position, 4, 4, DustID.Shadowflame, 0f, -0.5f, 150, default, 0.8f);
                    Main.dust[wisp].noGravity = true;
                    Main.dust[wisp].velocity *= 0.25f;
                }

                Lighting.AddLight(NPC.Center, 0.15f, 0.08f, 0.22f);
            }
        }

        void FaceTarget()
        {
            int direction = 1;
            if (Target.Center.X < NPC.Center.X)
            {
                direction = -1;
            }

            NPC.direction = direction;
            NPC.spriteDirection = direction;
        }

        #region Selection
        void PickAttack()
        {
            attackBag ??= new AttackBag<AttackState>(RefillAttackBag);
            targetIndex = NPC.target;

            // Priority before the bag keeps the urgency the old 1.3 / 1.2 weights gave the two situational spells:
            // Nova whenever cornered below half (not twice within two casts); Sacrifice below half (not within three).
            AttackState chosen = AttackState.None;
            bool belowHalf = NPC.life < NPC.lifeMax / 2;
            if (castsSinceNova >= 2 && IsEligible(AttackState.CurseNova))
            {
                chosen = AttackState.CurseNova;
            }
            else if (belowHalf && castsSinceSacrifice >= 3 && IsEligible(AttackState.SacrificeHeal))
            {
                chosen = AttackState.SacrificeHeal;
            }
            else if (!attackBag.TryDraw(IsEligible, out chosen))
            {
                //Nothing castable from here: keep kiting/navigating and look again shortly
                AttackCooldown = 20;
                return;
            }

            //Lock the sacrifice victim now so clients can draw the tether
            if (chosen == AttackState.SacrificeHeal)
            {
                NPC ghoul = FindNearestOwnGhoul(640f);
                SacrificeTarget = -1;
                if (ghoul != null)
                {
                    SacrificeTarget = ghoul.whoAmI;
                }
            }

            castsSinceNova++;
            castsSinceSacrifice++;
            if (chosen == AttackState.CurseNova)
            {
                castsSinceNova = 0;
            }
            if (chosen == AttackState.SacrificeHeal)
            {
                castsSinceSacrifice = 0;
            }

            State = chosen;
            AttackTimer = 0;
            StartCustomAttack(2);
            NPC.netUpdate = true;
        }

        // Today's weights x2, rounded: every spell gets two cards; the gates below defer them, never cut them.
        void RefillAttackBag(List<AttackState> cards)
        {
            for (int i = 0; i < 2; i++)
            {
                cards.Add(AttackState.DeathStrike);
                cards.Add(AttackState.BoneFan);
                cards.Add(AttackState.GraveHands);
                cards.Add(AttackState.SoulSiphon);
                cards.Add(AttackState.RaiseGhoul);
                cards.Add(AttackState.SacrificeHeal);
                cards.Add(AttackState.CurseNova);
            }
        }

        bool IsEligible(AttackState candidate)
        {
            Player player = Main.player[NPC.target];
            float distance = NPC.Distance(player.Center);
            bool lineOfSight = Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1);

            switch (candidate)
            {
                case AttackState.DeathStrike:
                    return distance <= DeathStrikeMaxRange;
                case AttackState.BoneFan:
                    return lineOfSight;
                case AttackState.GraveHands:
                    return true;
                case AttackState.SoulSiphon:
                    return distance < 14f * 16f && lineOfSight;
                case AttackState.RaiseGhoul:
                    {
                        // Grave spot 4 tiles ahead toward the target, needing real ground within 10 tiles below
                        int direction = 1;
                        if (player.Center.X < NPC.Center.X)
                        {
                            direction = -1;
                        }

                        Vector2 grave = NPC.Center + new Vector2(direction * 4 * 16f, -32f);
                        bool groundFound = FindGroundY(grave, 10) > 0f;
                        return CountOwnGhouls() < MaxGhouls && groundFound;
                    }
                case AttackState.SacrificeHeal:
                    return NPC.life < NPC.lifeMax * 0.7f && FindNearestOwnGhoul(640f) != null;
                case AttackState.CurseNova:
                    return NPC.life < NPC.lifeMax / 2 && distance < 7f * 16f;
            }

            return false;
        }

        void EndAttack(int cooldown)
        {
            State = AttackState.None;
            AttackTimer = 0;
            SacrificeTarget = -1;
            AttackCooldown = cooldown + Main.rand.Next(40);
            EnterPhase(AttackPhase.Idle, 0);
            NPC.netUpdate = true;
        }

        static int TotalTicks(AttackState state)
        {
            switch (state)
            {
                case AttackState.DeathStrike:
                    return StrikeTelegraphTicks + 25;
                case AttackState.BoneFan:
                    return FanTelegraphTicks + 20;
                case AttackState.GraveHands:
                    return HandsCastTicks + 20;
                case AttackState.SoulSiphon:
                    return SiphonChannelTicks + 25;
                case AttackState.RaiseGhoul:
                    return RaiseRitualTicks + 15;
                case AttackState.SacrificeHeal:
                    return SacrificeChannelTicks + 15;
                case AttackState.CurseNova:
                    return NovaTelegraphTicks + 20;
            }

            return 1;
        }
        #endregion

        #region Arm poses
        // Facing-space arm angles: 0 = straight ahead, negative = up, +PiOver2 = hanging at rest.
        // Measured in the body render: the hood's face sits at its front edge, so an authored pose above ~-0.5 rad (front)
        // or ~-0.3 rad (back) lays a bone hand across it, and a back arm past straight down (> PiOver2) folds up beside
        // the face too. Authored poses stay inside that band; only live aim at a target above the caster lifts higher.
        // Each pose eases in over 10 ticks and back to rest over the last 15 ticks of the spell.
        // The *For functions are static and pure so the offline pose preview renders exactly what the game draws.
        static float PoseBlendFor(AttackState state, int timer)
        {
            int total = TotalTicks(state);
            float blendIn = MathHelper.Clamp(timer / 10f, 0f, 1f);
            float blendOut = MathHelper.Clamp((total - timer) / 15f, 0f, 1f);
            return Math.Min(blendIn, blendOut);
        }

        // Facing-space angle from the shoulder to a world point, clamped to what the arm can reach forward.
        float AimAngleTo(Vector2 worldPoint)
        {
            Vector2 shoulder = NPC.Center + new Vector2(0f, -8f);
            Vector2 toPoint = worldPoint - shoulder;
            float forward = toPoint.X * NPC.direction;
            float angle = (float)Math.Atan2(toPoint.Y, Math.Max(forward, 1f));
            return MathHelper.Clamp(angle, -1.6f, 1.4f);
        }

        float FrontArmPose
        {
            get
            {
                // What the reaching palm points at: the victim during Sacrifice (0.3 = low reach if it is gone), else the target.
                float aimAngle = AimAngleTo(Target.Center);
                if (State == AttackState.SacrificeHeal)
                {
                    aimAngle = 0.3f;
                    NPC ghoul = SacrificeGhoul;
                    if (ghoul != null)
                    {
                        aimAngle = AimAngleTo(ghoul.Center);
                    }
                }

                return FrontArmPoseFor(State, AttackTimer, aimAngle);
            }
        }

        float BackArmPose => BackArmPoseFor(State, AttackTimer);

        static float FrontArmPoseFor(AttackState state, int timer, float aimAngle)
        {
            float pose = MathHelper.PiOver2;
            switch (state)
            {
                case AttackState.DeathStrike:
                case AttackState.SoulSiphon:
                case AttackState.SacrificeHeal:
                    pose = aimAngle;
                    break;

                case AttackState.BoneFan:
                    {
                        // Cocks 0.15 rad above the aim line through the rattle, then sweeps down to 0.45 below it through the fan in 6 ticks
                        float sweep = -0.15f * MathHelper.Clamp(timer / (float)FanTelegraphTicks, 0f, 1f);
                        if (timer > FanTelegraphTicks)
                        {
                            float release = MathHelper.Clamp((timer - FanTelegraphTicks) / 6f, 0f, 1f);
                            sweep = MathHelper.Lerp(-0.15f, 0.45f, release);
                        }

                        pose = aimAngle + sweep;
                        break;
                    }

                case AttackState.GraveHands:
                    {
                        // Raise (-0.45) then thrust the palm at the floor (+1.0) on each claw tick (15 / 35 / 55):
                        // 6t thrust down, 6t hold, 8t lift, repeat; holds down after the last claw.
                        const float Raised = -0.45f;
                        const float Thrust = 1.0f;
                        if (timer <= 9)
                        {
                            pose = Raised;
                        }
                        else if (timer <= 15)
                        {
                            pose = MathHelper.SmoothStep(Raised, Thrust, (timer - 9) / 6f);
                        }
                        else if (timer >= 55)
                        {
                            pose = Thrust;
                        }
                        else
                        {
                            int cycleTick = (timer - 15) % 20;
                            if (cycleTick <= 6)
                            {
                                pose = Thrust;
                            }
                            else if (cycleTick <= 14)
                            {
                                pose = MathHelper.SmoothStep(Thrust, Raised, (cycleTick - 6) / 8f);
                            }
                            else
                            {
                                pose = MathHelper.SmoothStep(Raised, Thrust, (cycleTick - 14) / 6f);
                            }
                        }

                        break;
                    }

                case AttackState.RaiseGhoul:
                    // Palms start low over the grave and lift the dead out of it across the ritual
                    pose = MathHelper.SmoothStep(0.9f, -0.45f, MathHelper.Clamp(timer / (float)RaiseRitualTicks, 0f, 1f));
                    break;

                case AttackState.CurseNova:
                    // Drawn in and down while the curse gathers, flung straight out on release
                    if (timer < NovaTelegraphTicks)
                    {
                        pose = MathHelper.SmoothStep(0f, 1.3f, timer / (float)NovaTelegraphTicks);
                    }
                    else
                    {
                        pose = -0.1f;
                    }

                    break;
            }

            return MathHelper.SmoothStep(MathHelper.PiOver2, pose, PoseBlendFor(state, timer));
        }

        static float BackArmPoseFor(AttackState state, int timer)
        {
            float pose = MathHelper.PiOver2;
            switch (state)
            {
                case AttackState.DeathStrike:
                    pose = -0.3f;
                    break;
                case AttackState.BoneFan:
                    pose = 1.1f; //dropped low and forward, shaking out bone chips
                    break;
                case AttackState.GraveHands:
                    pose = -0.2f;
                    break;
                case AttackState.SoulSiphon:
                    // Held to the chest, trembling with the drain (0.08 rad, ~10-tick wobble)
                    pose = 0.4f + 0.08f * (float)Math.Sin(timer * 0.6f);
                    break;
                case AttackState.RaiseGhoul:
                    pose = MathHelper.SmoothStep(0.9f, -0.25f, MathHelper.Clamp(timer / (float)RaiseRitualTicks, 0f, 1f));
                    break;
                case AttackState.SacrificeHeal:
                    pose = -0.2f;
                    if (timer >= SacrificeChannelTicks)
                    {
                        pose = 0.6f; //the stolen soul pressed to the chest
                    }
                    break;
                case AttackState.CurseNova:
                    if (timer < NovaTelegraphTicks)
                    {
                        pose = MathHelper.SmoothStep(0f, 1.3f, timer / (float)NovaTelegraphTicks);
                    }
                    else
                    {
                        pose = 0.1f; //both palms flung straight out
                    }
                    break;
            }

            return MathHelper.SmoothStep(MathHelper.PiOver2, pose, PoseBlendFor(state, timer));
        }
        #endregion

        #region Spells
        protected override void DoCustomTick(int ticksRemaining)
        {
            if (State == AttackState.None)
            {
                return;
            }

            AttackTimer++;
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            if (authority && (Target.dead || !Target.active))
            {
                EndAttack(60);
                return;
            }

            switch (State)
            {
                case AttackState.DeathStrike:
                    RunDeathStrike();
                    break;
                case AttackState.BoneFan:
                    RunBoneFan();
                    break;
                case AttackState.GraveHands:
                    RunGraveHands();
                    break;
                case AttackState.SoulSiphon:
                    RunSoulSiphon();
                    break;
                case AttackState.RaiseGhoul:
                    RunRaiseGhoul();
                    break;
                case AttackState.SacrificeHeal:
                    RunSacrificeHeal();
                    break;
                case AttackState.CurseNova:
                    RunCurseNova();
                    break;
            }

            if (authority && State != AttackState.None && AttackTimer >= TotalTicks(State))
            {
                EndAttack(CooldownAfter(State));
            }
        }

        static int CooldownAfter(AttackState state)
        {
            switch (state)
            {
                case AttackState.DeathStrike:
                    return 180;
                case AttackState.BoneFan:
                    return 150;
                case AttackState.GraveHands:
                    return 240;
                case AttackState.SoulSiphon:
                    return 300;
                case AttackState.RaiseGhoul:
                    return 360;
                case AttackState.SacrificeHeal:
                    return 420;
                case AttackState.CurseNova:
                    return 330;
            }

            return 60;
        }

        void RunDeathStrike()
        {
            Vector2 palm = Palm;
            if (!Main.dedServ && AttackTimer <= StrikeTelegraphTicks)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.6f, Pitch = -0.4f }, NPC.Center);
                }

                //PURPLE: curse gathering into the casting palm, thicker as it nears release
                float progress = AttackTimer / (float)StrikeTelegraphTicks;
                int gatherCount = 1 + (int)(progress * 2f);
                for (int i = 0; i < gatherCount; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 position = palm + angle.ToRotationVector2() * Main.rand.NextFloat(8f, 26f);
                    Dust dust = Dust.NewDustPerfect(position, DustID.Shadowflame, (palm - position) * 0.15f, 100, default, 1.1f);
                    dust.noGravity = true;
                }

                //The off hand smoulders with the same curse
                if (Main.rand.NextBool(3))
                {
                    Dust ember = Dust.NewDustPerfect(OffPalm + Main.rand.NextVector2Circular(4f, 4f), DustID.Shadowflame, new Vector2(0f, -0.6f), 120, default, 0.9f);
                    ember.noGravity = true;
                }

                Lighting.AddLight(palm, 0.35f * progress, 0.15f * progress, 0.5f * progress);
            }

            if (AttackTimer == StrikeTelegraphTicks && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 velocity = (Target.Center - palm).SafeNormalize(new Vector2(NPC.direction, 0f)) * 8f;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), palm, velocity,
                    ModContent.ProjectileType<EnemySpellSuddenDeathStrike>(), EnemyDamage.Projectile(StrikeExpertDamage), 1f, Main.myPlayer);
            }
        }

        void RunBoneFan()
        {
            Vector2 palm = Palm;
            if (!Main.dedServ && AttackTimer <= FanTelegraphTicks)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item2 with { Volume = 0.6f, Pitch = -0.3f }, NPC.Center); //dry rattle
                }

                //WHITE: bone dust rattling around the cocked palm; the flared off hand sheds a few chips too
                if (Main.rand.NextBool(2))
                {
                    Dust bone = Dust.NewDustPerfect(palm + Main.rand.NextVector2Circular(8f, 8f), DustID.Bone, new Vector2(0f, -0.5f), 80, default, 1f);
                    bone.noGravity = true;
                    bone.velocity *= 0.3f;
                }

                if (Main.rand.NextBool(5))
                {
                    Dust chip = Dust.NewDustPerfect(OffPalm, DustID.Bone, Main.rand.NextVector2Circular(0.6f, 0.6f), 80, default, 0.8f);
                    chip.noGravity = true;
                }
            }

            if (AttackTimer == FanTelegraphTicks)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 aim = (Target.Center - palm).SafeNormalize(new Vector2(NPC.direction, 0f));
                    for (int i = -2; i <= 2; i++)
                    {
                        Vector2 velocity = aim.RotatedBy(i * 0.175f) * 8.5f;
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), palm, velocity,
                            ModContent.ProjectileType<NecroBoneShard>(), EnemyDamage.Projectile(BoneExpertDamage), 2f, Main.myPlayer, 0.12f);
                    }
                }

                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item2 with { Volume = 0.7f, Pitch = 0.2f }, NPC.Center);
                }
            }
        }

        void RunGraveHands()
        {
            if (!Main.dedServ)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.6f, Pitch = -0.6f }, NPC.Center);
                }

                //PURPLE dripping from the thrusting palm while the graves open under the player
                if (AttackTimer <= HandsCastTicks - 20 && Main.rand.NextBool(2))
                {
                    Dust drip = Dust.NewDustPerfect(Palm + Main.rand.NextVector2Circular(5f, 5f), DustID.Shadowflame, new Vector2(0f, 1.2f), 100, default, 1.1f);
                    drip.noGravity = true;
                }

                if (AttackTimer <= HandsCastTicks - 20 && Main.rand.NextBool(4))
                {
                    Dust ember = Dust.NewDustPerfect(OffPalm, DustID.Shadowflame, new Vector2(0f, -0.8f), 120, default, 0.9f);
                    ember.noGravity = true;
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                //Two hands chase the player, the third leads their escape
                if (AttackTimer == 15 || AttackTimer == 35)
                {
                    SpawnGraveHand(Target.Center.X);
                }

                if (AttackTimer == 55)
                {
                    SpawnGraveHand(Target.Center.X + Target.velocity.X * 25f);
                }
            }
        }

        void RunSoulSiphon()
        {
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            if (!Main.dedServ)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.6f, Pitch = -0.6f }, NPC.Center);
                }

                if (AttackTimer == SiphonStaggerableTicks)
                {
                    SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.6f, Pitch = 0.2f }, NPC.Center); //commit cue
                }
            }

            if (AttackTimer > SiphonChannelTicks)
            {
                return; //recovery, no armor
            }

            //Range or line of sight broken: the thread snaps. The server decides; clients only stop drawing the stream.
            Vector2 palm = Palm;
            bool connected = NPC.Distance(Target.Center) < 224f && Collision.CanHitLine(NPC.Center, 1, 1, Target.Center, 1, 1);
            if (!connected)
            {
                if (authority)
                {
                    EndAttack(120);
                }

                if (!Main.dedServ)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        Dust snap = Dust.NewDustPerfect(palm + Main.rand.NextVector2Circular(8f, 8f), DustID.CursedTorch, new Vector2(0f, -1f), 120, default, 1f);
                        snap.noGravity = true;
                    }
                }

                return;
            }

            if (!Main.dedServ)
            {
                //GREEN: soul wisps streaming FROM the player INTO the reaching palm
                Vector2 streamDirection = (palm - Target.Center).SafeNormalize(Vector2.UnitX);
                for (int i = 0; i < 2; i++)
                {
                    float along = Main.rand.NextFloat();
                    Vector2 position = Vector2.Lerp(Target.Center, palm, along) + Main.rand.NextVector2Circular(8f, 8f);
                    Dust wisp = Dust.NewDustPerfect(position, DustID.CursedTorch, streamDirection * 4f, 80, default, 1.1f);
                    wisp.noGravity = true;
                }

                //What it has stolen pools in the hand held to its chest
                if (Main.rand.NextBool(3))
                {
                    Dust pooled = Dust.NewDustPerfect(OffPalm + Main.rand.NextVector2Circular(4f, 4f), DustID.CursedTorch, Vector2.Zero, 90, default, 0.9f);
                    pooled.noGravity = true;
                }
            }

            //Drain pulses after a 30-tick warm-up, every 20 ticks (t30/50/70/90): each bites the player and feeds the caster
            bool pulseTick = AttackTimer >= 30 && (AttackTimer - 30) % 20 == 0;
            if (pulseTick)
            {
                if (authority)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), Target.Center, Vector2.Zero,
                        ModContent.ProjectileType<NecroSiphonPulse>(), EnemyDamage.Projectile(SiphonPulseExpertDamage), 0f, Main.myPlayer);
                    HealSelf(SiphonHealFraction);
                }

                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.4f, Pitch = 0.5f }, NPC.Center);
                }
            }
        }

        void RunRaiseGhoul()
        {
            Vector2 grave = NPC.Center + new Vector2(NPC.direction * 4 * 16f, 0f);
            float groundY = FindGroundY(grave - new Vector2(0f, 32f), 10);

            if (AttackTimer > RaiseRitualTicks)
            {
                return;
            }

            if (!Main.dedServ)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.7f, Pitch = -0.7f }, NPC.Center);
                }

                if (groundY > 0f)
                {
                    //The grave circle: dark simmer + rising glints where the ghoul will claw out
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 position = new Vector2(grave.X - 20f + Main.rand.NextFloat(40f), groundY - 6f);
                        int dust = Dust.NewDust(position, 4, 4, DustID.Shadowflame, 0f, -1.5f, 110, default, 1.1f);
                        Main.dust[dust].noGravity = true;
                    }
                }

                //Both lifting palms trail the same shadow, drawn upward with them
                if (Main.rand.NextBool(2))
                {
                    Dust front = Dust.NewDustPerfect(Palm, DustID.Shadowflame, new Vector2(0f, -1.5f), 100, default, 1f);
                    front.noGravity = true;
                    Dust back = Dust.NewDustPerfect(OffPalm, DustID.Shadowflame, new Vector2(0f, -1.5f), 100, default, 1f);
                    back.noGravity = true;
                }
            }

            if (AttackTimer != RaiseRitualTicks || groundY <= 0f)
            {
                return;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && CountOwnGhouls() < MaxGhouls)
            {
                int spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)grave.X, (int)groundY, ModContent.NPCType<SpellboundGhoul>());
                if (spawned < Main.maxNPCs && Main.npc[spawned].ModNPC is SpellboundGhoul ghoul)
                {
                    ghoul.BindOwner(NPC.whoAmI, spawnToken);
                    Main.npc[spawned].velocity.Y = -5f; //claws up out of the earth
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawned);
                    }
                }
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath2 with { Volume = 0.6f, Pitch = -0.4f }, grave);
                for (int i = 0; i < 16; i++)
                {
                    int dirt = Dust.NewDust(new Vector2(grave.X - 20f, groundY - 12f), 40, 12, DustID.Dirt, Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-5f, -2f), 60, default, 1.3f);
                    Main.dust[dirt].velocity.Y -= 1f;
                    int wisp = Dust.NewDust(new Vector2(grave.X - 20f, groundY - 20f), 40, 20, DustID.Shadowflame, 0f, -2f, 80, default, 1.2f);
                    Main.dust[wisp].noGravity = true;
                }
            }
        }

        ///<summary>The locked sacrifice victim if it still exists here, else null. Clients trust the synced index; the server also checks ownership.</summary>
        NPC SacrificeGhoul
        {
            get
            {
                if (SacrificeTarget < 0 || SacrificeTarget >= Main.maxNPCs)
                {
                    return null;
                }

                NPC ghoul = Main.npc[SacrificeTarget];
                if (!ghoul.active || ghoul.ModNPC is not SpellboundGhoul spellbound)
                {
                    return null;
                }

                if (Main.netMode != NetmodeID.MultiplayerClient && !spellbound.BelongsTo(NPC.whoAmI, spawnToken))
                {
                    return null;
                }

                return ghoul;
            }
        }

        void RunSacrificeHeal()
        {
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            NPC ghoul = SacrificeGhoul;
            if (ghoul == null)
            {
                //The victim died first: heal denied
                if (authority && AttackTimer <= SacrificeChannelTicks)
                {
                    EndAttack(150);
                }

                return;
            }

            if (AttackTimer > SacrificeChannelTicks)
            {
                return;
            }

            Vector2 palm = Palm;
            if (!Main.dedServ)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.6f, Pitch = -0.8f }, NPC.Center);
                }

                if (AttackTimer == SacrificeStaggerableTicks)
                {
                    SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.6f, Pitch = 0f }, NPC.Center); //commit cue
                }

                //GREEN: the ghoul's essence streaming into the reaching palm; the victim shudders
                Vector2 streamDirection = (palm - ghoul.Center).SafeNormalize(Vector2.UnitX);
                for (int i = 0; i < 2; i++)
                {
                    float along = Main.rand.NextFloat();
                    Vector2 position = Vector2.Lerp(ghoul.Center, palm, along) + Main.rand.NextVector2Circular(6f, 6f);
                    Dust wisp = Dust.NewDustPerfect(position, DustID.CursedTorch, streamDirection * 4f, 70, default, 1.1f);
                    wisp.noGravity = true;
                }

                if (Main.rand.NextBool(3))
                {
                    int shudder = Dust.NewDust(ghoul.position, ghoul.width, ghoul.height, DustID.Shadowflame, 0f, -1f, 100, default, 1f);
                    Main.dust[shudder].noGravity = true;
                }

                if (Main.rand.NextBool(3))
                {
                    Dust readied = Dust.NewDustPerfect(OffPalm, DustID.CursedTorch, new Vector2(0f, -0.6f), 90, default, 0.9f);
                    readied.noGravity = true;
                }
            }

            if (AttackTimer != SacrificeChannelTicks)
            {
                return;
            }

            //Consume it: a real kill (never life = 0 by hand - that skips OnKill), so the ghoul still detonates
            if (authority)
            {
                ghoul.StrikeInstantKill();
                HealSelf(SacrificeHealFraction);
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath2 with { Volume = 0.7f, Pitch = 0.2f }, ghoul.Center);
                for (int i = 0; i < 20; i++)
                {
                    int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CursedTorch, 0f, -2f, 60, default, 1.4f);
                    Main.dust[dust].noGravity = true;
                }
            }
        }

        void RunCurseNova()
        {
            if (AttackTimer > NovaTelegraphTicks)
            {
                return;
            }

            if (!Main.dedServ)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.7f, Pitch = -0.2f }, NPC.Center);
                }

                //PURPLE spiralling inward (70px -> 12px) - get off it NOW. Both lowered palms burn with it.
                float progress = AttackTimer / (float)NovaTelegraphTicks;
                for (int i = 0; i < 2; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    float radius = MathHelper.Lerp(70f, 12f, progress) + Main.rand.NextFloat(10f);
                    Vector2 position = NPC.Center + angle.ToRotationVector2() * radius;
                    Dust dust = Dust.NewDustPerfect(position, DustID.Shadowflame, (NPC.Center - position) * 0.1f, 90, default, 1.1f);
                    dust.noGravity = true;
                }

                if (Main.rand.NextBool(2))
                {
                    Dust front = Dust.NewDustPerfect(Palm, DustID.Shadowflame, Vector2.Zero, 90, default, 1f);
                    front.noGravity = true;
                    Dust back = Dust.NewDustPerfect(OffPalm, DustID.Shadowflame, Vector2.Zero, 90, default, 1f);
                    back.noGravity = true;
                }
            }

            if (AttackTimer != NovaTelegraphTicks)
            {
                return;
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.6f, Pitch = -0.4f }, NPC.Center);
            }

            //The ring leaves the body centre (it is a ring, not a palm shot): 112px max radius
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<NecroCurseNova>(), EnemyDamage.Projectile(NovaExpertDamage), 9f, Main.myPlayer, 112f);
            }
        }
        #endregion

        #region Helpers
        ///<summary>Server only: heal a fraction of max HP (capped at missing life) and sync it.</summary>
        void HealSelf(float fraction)
        {
            int healAmount = (int)(NPC.lifeMax * fraction);
            healAmount = Math.Min(healAmount, NPC.lifeMax - NPC.life);
            if (healAmount <= 0)
            {
                return;
            }

            NPC.life += healAmount;
            NPC.HealEffect(healAmount);
            NPC.netUpdate = true;
        }

        void SpawnGraveHand(float x)
        {
            float groundY = FindGroundY(new Vector2(x, Target.Bottom.Y - 8f), 20);
            if (groundY < 0f)
            {
                return;
            }

            Projectile.NewProjectile(NPC.GetSource_FromThis(), new Vector2(x, groundY - 23f), Vector2.Zero,
                ModContent.ProjectileType<NecroGraveHand>(), EnemyDamage.Projectile(HandExpertDamage), 1f, Main.myPlayer, 28f);
        }

        ///<summary>Server only: ghouls this Necromancer raised (ownership is server-local state).</summary>
        int CountOwnGhouls()
        {
            int count = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.ModNPC is SpellboundGhoul ghoul && ghoul.BelongsTo(NPC.whoAmI, spawnToken))
                {
                    count++;
                }
            }

            return count;
        }

        ///<summary>Server only: nearest own ghoul within maxDistance px, or null.</summary>
        NPC FindNearestOwnGhoul(float maxDistance)
        {
            NPC best = null;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.ModNPC is not SpellboundGhoul ghoul || !ghoul.BelongsTo(NPC.whoAmI, spawnToken))
                {
                    continue;
                }

                float distance = Vector2.Distance(npc.Center, NPC.Center);
                if (distance < maxDistance)
                {
                    maxDistance = distance;
                    best = npc;
                }
            }

            return best;
        }

        ///<summary>World Y of the first solid tile top under the point (scanning down), or -1 if none in range.</summary>
        static float FindGroundY(Vector2 worldPos, int maxTilesDown)
        {
            int tileX = (int)(worldPos.X / 16f);
            int tileY = (int)(worldPos.Y / 16f);
            if (tileX < 5 || tileX > Main.maxTilesX - 5)
            {
                return -1f;
            }

            for (int depth = 0; depth <= maxTilesDown; depth++)
            {
                int y = tileY + depth;
                if (y >= Main.maxTilesY - 5)
                {
                    break;
                }

                Tile tile = Main.tile[tileX, y];
                if (tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType])
                {
                    return y * 16f;
                }
            }

            return -1f;
        }
        #endregion

        #region Death
        // Replaces the puppet's generic blood burst. Runs on every client (OnKill does not run on MP clients),
        // so the escaping souls and gore are seen in multiplayer too.
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life > 0 || Main.dedServ)
            {
                return;
            }

            //Its collected souls escape: a spiral of wisps before the body falls
            SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.6f, Pitch = 0.2f }, NPC.Center);
            for (int i = 0; i < 24; i++)
            {
                float angle = MathHelper.TwoPi * i / 24f;
                int wisp = Dust.NewDust(NPC.Center + angle.ToRotationVector2() * 8f, 4, 4, DustID.Shadowflame, 0f, 0f, 60, default, 1.5f);
                Main.dust[wisp].noGravity = true;
                Main.dust[wisp].velocity = angle.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * 2.5f + new Vector2(0f, -3f);
            }

            string[] goreNames = { "Necromancer Gore 1", "Necromancer Gore 2", "Necromancer Gore 3", "Necromancer Gore 2", "Necromancer Gore 3" };
            foreach (string goreName in goreNames)
            {
                Vector2 goreVelocity = new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, goreVelocity, Mod.Find<ModGore>(goreName).Type, 1.1f);
            }
        }
        #endregion

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BootsOfHaste>(), 30));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CrimsonPotion>(), 40));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<StrengthPotion>(), 20));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ShockwavePotion>(), 20));
            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 20));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BattlefrontPotion>(), 25));
            npcLoot.Add(ItemDropRule.Common(ItemID.BloodMoonStarter, 25));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShardItem>(), 11));
        }
    }
}
