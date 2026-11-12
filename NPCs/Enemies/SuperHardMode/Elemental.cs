using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Armor.Enemy.ElementalSet;
using tsorcRevamp.Content.Items.Materials.Titanite;
using tsorcRevamp.NPCs.Enemies.Dworc;
using tsorcRevamp.NPCs.Invaders;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    // A blue-skinned brute rendered as a puppet in the Elemental armor, Phase-dyed so it shimmers in and out of sight.
    // No weapon and no swing: it runs the player down (3 px/t, 5 below half HP), leaps at them from close range and
    // hurts on contact through the whole leap. Natural spawns pay souls; ones raised by an Elemental Necromancer
    // pay none and burst apart when their Necromancer dies.
    class Elemental : PuppetNPC
    {
        // Pursuit and leap values carried over unchanged from the FighterAI version.
        const float PursuitTopSpeed = 3f;
        const float EnragedTopSpeed = 5f;
        const float LeapSpeedX = 3f;
        const float LeapSpeedY = 3.4f;
        const float LeapMinimumSpeed = 2.5f;
        const float LeapRange = 128f;
        const float LandingTraction = 0.58f;
        const int LeapCooldownTicks = 75;
        const int LandingTractionTicks = 16;

        // A raised Elemental claws out of the ground for 24t, then gets 18t of grace before it may leap or hurt anyone.
        internal const int EmergenceTicks = 24;
        internal const int EmergenceGraceTicks = 18;
        const int OwnerCheckInterval = 30;

        bool leapInAir;
        int leapCooldown;
        int landingTractionTimer;
        int emergenceTimer; //>0 = still rising out of the ground or in its grace; synced

        // Server-only ownership: the Elemental Necromancer that raised it (slot + its spawn token, so a recycled NPC slot
        // is never mistaken for it). -1 = a natural spawn, which never checks an owner.
        int ownerSlot = -1;
        int ownerToken;
        int ownerCheckTimer;

        #region Puppet loadout
        public override bool UsesPuppetDifficultyScaling => false;
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        protected override bool DespawnsOnPartyWipe => false;
        protected override string InvaderTitle => "Elemental";
        protected override int HeadArmorItemType => ModContent.ItemType<ElementalHeadpiece>();
        protected override int BodyArmorItemType => ModContent.ItemType<ElementalBreastplate>();
        protected override int LegsArmorItemType => ModContent.ItemType<ElementalGreaves>();
        protected override int HeadArmorDyeItemType => ItemID.PhaseDye;
        protected override int BodyArmorDyeItemType => ItemID.PhaseDye;
        protected override int LegsArmorDyeItemType => ItemID.PhaseDye;
        protected override Color PuppetSkinColor => new Color(0x1F, 0x5E, 0x8F); // the sheet's blue skin, for anything the armor doesn't hide
        protected override int MeleeWeaponItemType => -1;
        protected override int RangedWeaponItemType => -1;
        protected override int MagicWeaponItemType => -1;
        protected override int MeleeDamage => 0;
        protected override int RangedDamage => 0;
        protected override int EstusChargesMax => 0;
        protected override int CasualStrollChance => 0;
        protected override int UnblockableFinisherBlocks => 0;
        protected override bool HoldAttackSelection => true;
        protected override float Acceleration => 0.08f;

        // Enrage below half HP, as the old enragePercent: 0.5f did. NPC.life is synced, so every machine agrees.
        protected override float TopSpeed
        {
            get
            {
                if (NPC.life < NPC.lifeMax / 2)
                {
                    return EnragedTopSpeed;
                }

                return PursuitTopSpeed;
            }
        }

        protected override void DoMeleeAttack()
        {
            // No weapon: its only attack is the body itself (contact damage + the leap).
        }

        protected override void DoRangedAttack()
        {
        }
        #endregion

        public override void SetStaticDefaults()
        {
            // Keeps the old sheet's slicing for the bestiary/banner; the puppet Player draws the body in game.
            Main.npcFrameCount[NPC.type] = 15;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Ichor] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 3;
            NPC.width = 18;
            NPC.height = 40;
            AnimationType = -1;
            NPC.aiStyle = -1;
            NPC.knockBackResist = 0.2f; //overridden by the PoiseProfiles entry
            NPC.timeLeft = 750;
            NPC.damage = 50;
            NPC.defense = 42;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lavaImmune = true;
            NPC.lifeMax = 1600;
            NPC.value = 4000;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.ElementalBanner>();

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 40;
            globalNPC.CanTeleport = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write(leapInAir);
            writer.Write((byte)leapCooldown);
            writer.Write((byte)landingTractionTimer);
            writer.Write((byte)emergenceTimer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            leapInAir = reader.ReadBoolean();
            leapCooldown = reader.ReadByte();
            landingTractionTimer = reader.ReadByte();
            emergenceTimer = reader.ReadByte();
        }

        /// <summary>Server, right after an Elemental Necromancer raises this Elemental. Raised Elementals pay no Dark
        /// Souls or coins (both come from NPC.value at the kill, read server-side); their item drops are untouched.
        /// Starts the synced emergence + grace window.</summary>
        public void BindOwner(int slot, int token)
        {
            ownerSlot = slot;
            ownerToken = token;
            NPC.value = 0f;
            emergenceTimer = EmergenceTicks + EmergenceGraceTicks;
            NPC.netUpdate = true;
        }

        /// <summary>Server: raised by this Necromancer (slot + spawn token).</summary>
        public bool BelongsTo(int slot, int token)
        {
            return ownerSlot == slot && ownerToken == token;
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            // Still clawing out of the ground (or in its grace): the body is harmless.
            return emergenceTimer <= 0;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            // Runs on the victim's machine, which owns its own buffs.
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.Battle, 30 * 60, false);
                target.AddBuff(BuffID.Weak, 40 * 60, false);
            }
        }

        public override void AI()
        {
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;

            // Owner gone (dead, despawned, or its slot reused): a real kill, never life = 0 by hand (that skips OnKill
            // and loot). Its HitEffect plays the purple burst on every client.
            if (authority && ownerSlot >= 0)
            {
                ownerCheckTimer++;
                if (ownerCheckTimer >= OwnerCheckInterval)
                {
                    ownerCheckTimer = 0;
                    bool ownerAlive = false;
                    if (ownerSlot < Main.maxNPCs)
                    {
                        NPC owner = Main.npc[ownerSlot];
                        bool isNecromancer = owner.active && owner.life > 0 && owner.type == ModContent.NPCType<ElementalNecromancer>();
                        ownerAlive = isNecromancer && owner.GetGlobalNPC<DworcSpawnIdentity>().Token == ownerToken;
                    }

                    if (!ownerAlive)
                    {
                        NPC.StrikeInstantKill();
                        return;
                    }
                }
            }

            if (emergenceTimer > 0)
            {
                emergenceTimer--;
                if (!Main.dedServ && emergenceTimer > EmergenceGraceTicks && Main.rand.NextBool(2))
                {
                    // Still climbing out: grave dust kicked up around its feet.
                    Dust soil = Dust.NewDustDirect(new Vector2(NPC.position.X - 6f, NPC.Bottom.Y - 8f), NPC.width + 12, 8, DustID.PurpleTorch, 0f, -1.5f, 100, default, 1.1f);
                    soil.noGravity = true;
                }
            }

            if (leapCooldown > 0)
            {
                leapCooldown--;
            }

            if (leapInAir && NPC.velocity.Y == 0f)
            {
                leapInAir = false;
                landingTractionTimer = LandingTractionTicks;
            }

            // Pursuit: the shared puppet navigation (SmartFighter4) at TopSpeed.
            base.AI();

            // Landing traction: 16 ticks of heavy friction so it skids to a stop instead of sliding past.
            if (landingTractionTimer > 0 && NPC.velocity.Y == 0f)
            {
                NPC.velocity.X *= LandingTraction;
                if (Math.Abs(NPC.velocity.X) < 0.1f)
                {
                    NPC.velocity.X = 0f;
                }

                landingTractionTimer--;
            }

            // The leap is decided on the server only; LeapAtPlayer sets netUpdate, so clients adopt the launch velocity.
            bool leapAllowed = authority && leapCooldown <= 0 && emergenceTimer <= 0 && NPC.HasValidTarget;
            if (leapAllowed)
            {
                float velocityYBeforeLeap = NPC.velocity.Y;
                tsorcRevampAIs.LeapAtPlayer(NPC, LeapSpeedX, LeapSpeedY, LeapMinimumSpeed, LeapRange);

                bool leapt = velocityYBeforeLeap == 0f && NPC.velocity.Y < 0f;
                if (leapt)
                {
                    leapInAir = true;
                    leapCooldown = LeapCooldownTicks;
                    landingTractionTimer = 0;
                    NPC.netUpdate = true;
                }
            }

            if (!Main.dedServ)
            {
                Lighting.AddLight(NPC.Center, 0.12f, 0.08f, 0.28f);
            }
        }

        // Replaces the puppet's generic blood burst. Runs on every client (OnKill does not run on MP clients).
        // ~80 purple motes thrown outward at 2-7 px/t plus a 24-mote ring at the edge; no gore. The death
        // sound is NPC.DeathSound (NPCDeath6).
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life > 0 || Main.dedServ)
            {
                return;
            }

            Lighting.AddLight(NPC.Center, 0.9f, 0.4f, 1.3f);

            for (int i = 0; i < 80; i++)
            {
                int dustType = DustID.PurpleTorch;
                if (i % 3 == 0)
                {
                    dustType = DustID.Shadowflame;
                }
                else if (i % 10 == 1)
                {
                    dustType = DustID.PurpleCrystalShard;
                }

                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 7f);
                Vector2 position = NPC.Center + Main.rand.NextVector2Circular(NPC.width * 0.5f, NPC.height * 0.5f);
                Dust mote = Dust.NewDustPerfect(position, dustType, velocity, 60, default, Main.rand.NextFloat(1.2f, 2f));
                mote.noGravity = true;
            }

            for (int i = 0; i < 24; i++)
            {
                float angle = MathHelper.TwoPi * i / 24f;
                Vector2 direction = angle.ToRotationVector2();
                Dust ring = Dust.NewDustPerfect(NPC.Center + direction * 10f, DustID.PurpleTorch, direction * 7f, 40, default, 1.8f);
                ring.noGravity = true;
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.SuperHardmodeRule, ModContent.ItemType<WhiteTitanite>(), 4, 1, 2, 50));
        }
    }
}
