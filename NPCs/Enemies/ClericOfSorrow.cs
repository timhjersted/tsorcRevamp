using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Projectiles.Enemy;               // GigasIceShard, GigasUndertowZone (reused)
using tsorcRevamp.Projectiles.Enemy.ClericOfSorrow; // ClericLastRitesRing

namespace tsorcRevamp.NPCs.Enemies
{
    // A REGULAR enemy (not an invader — no "INVADED BY" banner) rendered on the InvaderNPC puppet system.
    // A drowned priest of The Sorrow: a Capricorn-masked, black-robed cultist on Jim's Wings that FLIES
    // (out of water and in it — the wings double as the underwater hover) and casts frost through a
    // Sapphire Staff, every spell leaving the staff TIP.
    //
    // Kit split by nature (see EnemyRedesignSkillGuide G0.7):
    //   • Ranged OFFENSE (Hail Prayer / Blizzard Veil / Undertow) runs through the base MAGIC phase, so the
    //     staff raises + telegraphs + fires from the tip for free.
    //   • SET-PIECES (Ice Shell encase / Communion ally-heal / Last Rites death-curse) are NOT attacks that
    //     fit the magic template, so they run on the base's generic AttackPhase.Custom (StartCustomAttack),
    //     triggered from this AI() override on their own conditions.
    public class ClericOfSorrow : InvaderNPC
    {
        // Not an invasion — regular spawn, no banner.
        protected override bool AnnounceInvasion => false;
        protected override string InvaderTitle => "Cleric of Sorrow";

        #region Puppet loadout
        protected override int HeadArmorItemType => ItemID.CapricornMask;   // "Capricorn Helmet"
        protected override int BodyArmorItemType => ItemID.BlueLunaticRobe;  // real vanilla item; bodySlot 181 = Lunar Cultist Robe
        protected override int LegsArmorItemType => 0;                       // no legs/feet slot
        protected override int HeadArmorDyeItemType => ItemID.BlackDye;
        protected override int BodyArmorDyeItemType => ItemID.BlackDye;
        protected override Color PuppetSkinColor => new Color(0x5A, 0x71, 0xFF); // #5A71FF
        protected override Color PuppetEyeColor => new Color(0x26, 0x26, 0x26);  // #262626

        // Magic-only: no melee / no ranged weapon, so the base only ever enters the MAGIC phase on its own.
        protected override int MeleeWeaponItemType => -1;
        protected override int RangedWeaponItemType => -1;
        protected override int MagicWeaponItemType => ItemID.SapphireStaff;
        protected override int MeleeDamage => 0;
        protected override int RangedDamage => 0;
        protected override int MagicDamage => 16;
        #endregion

        #region Flight (Jim's Wings — amphibious: flies in air AND hovers underwater)
        protected override bool HasWings => true;
        protected override int WingsAccessoryItemType => ItemID.JimsWings;
        protected override bool ShowWingsWhenGrounded => true;
        protected override int RandomTakeoffChance => 45;   // strongly prefers the air
        protected override float FlightHeightTrigger => 40f;
        protected override float FlightHpEscalationFrac => 1f; // always "escalated" — it's an aerial caster
        #endregion

        #region Magic pacing / ranges
        protected override float MagicRange => 900f;
        protected override float MinMagicRange => 0f;
        protected override int MagicTelegraphTicks => 45;
        protected override int MagicAttackTicks => 20;
        protected override int MagicRecoveryTicks => 40;
        protected override int MagicCooldownAfterUse => 130;
        protected override Color MagicTelegraphFlashColor => Color.Cyan;
        protected override float TopSpeed => 2.2f;
        protected override float Acceleration => 0.08f;
        #endregion

        // Ranged offense (base MAGIC phase).
        enum MagicCast : byte { Hail, Veil, Undertow }
        // Bespoke set-pieces (base CUSTOM phase).
        enum CustomKind : byte { None, IceShell, Communion, LastRites }

        MagicCast _magicCast = MagicCast.Hail;     // which ranged cast is running
        MagicCast _lastMagicCast = MagicCast.Hail;
        CustomKind _customKind = CustomKind.None;  // which set-piece is running (read only while Phase==Custom)
        int _undertowCd = 0;
        int _shellCd = 0;
        int _communionCd = 0;
        int _communionTarget = -1;
        bool _lastRitesDone = false;

        // Timings / damage (hostile projectiles deal 2x on hit in normal mode)
        const int HailWindow = 30;
        const int VeilWindow = 44;
        const int UndertowChannel = 110;
        const int CommunionChannel = 90;
        const int ShellEncase = 110;
        const int LastRitesStand = 60;
        const int HailShardDamage = 16;
        const int VeilShardDamage = 15;
        const int UndertowShardDamage = 18;
        const int ShellShardDamage = 20;
        const int LastRitesRingDamage = 30;
        const int CommunionHealPerPulse = 20;
        const float UndertowMinTiles = 20f;
        const float UndertowMaxTiles = 40f;
        const float ShellTriggerTiles = 8f;

        private static Asset<Texture2D> ShellTexture;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1; // puppet body is drawn separately; NPC sprite is a placeholder
        }

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 40;
            NPC.aiStyle = -1;
            NPC.damage = 45;
            NPC.defense = 35;
            NPC.lifeMax = 400;
            NPC.knockBackResist = 0.4f; // overridden by the PoiseProfiles entry
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 2000;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.ClericOfSorrowBanner>();

            tsorcRevampGlobalNPC g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            g.PoiseStaggerResetsAI = true; // a poise break cancels the current cast

            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 1200;
                NPC.defense = 50;
                NPC.value = 2500;
            }
        }

        int Scaled(int dmg) => tsorcRevampWorld.SuperHardMode ? (int)(dmg * 1.3f) : dmg;

        // MP: the base InvaderNPC doesn't sync Phase/PhaseTimer (every peer simulates its own copy off
        // synced position/life/target — see EnemyRedesignSkillGuide G0.7); a subclass syncs whatever of ITS
        // OWN state matters. Here: the active ranged cast + set-piece kind (so remote clients show the same
        // spell instead of independently re-rolling), the heal target, the cooldowns, and the one-time
        // Last Rites flag.
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)_magicCast);
            writer.Write((byte)_customKind);
            writer.Write((short)_communionTarget);
            writer.Write(_undertowCd);
            writer.Write(_shellCd);
            writer.Write(_communionCd);
            writer.Write(_lastRitesDone);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            _magicCast = (MagicCast)reader.ReadByte();
            _customKind = (CustomKind)reader.ReadByte();
            _communionTarget = reader.ReadInt16();
            _undertowCd = reader.ReadInt32();
            _shellCd = reader.ReadInt32();
            _communionCd = reader.ReadInt32();
            _lastRitesDone = reader.ReadBoolean();
        }

        #region Spawn (unchanged — Hallow + its true home, the Frozen Ocean)
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player;
            bool Meteor = P.ZoneMeteor;
            bool Jungle = P.ZoneJungle;
            bool Dungeon = P.ZoneDungeon;
            bool Corruption = (P.ZoneCorrupt || P.ZoneCrimson);
            bool Hallow = P.ZoneHallow;
            bool InBrownLayer = P.ZoneDirtLayerHeight;
            bool InGrayLayer = P.ZoneRockLayerHeight;
            bool FrozenOcean = spawnInfo.SpawnTileX > (Main.maxTilesX - 800);

            if (spawnInfo.Player.townNPCs > 0f) return 0;
            if (Main.hardMode && !Meteor && !Jungle && !Dungeon && !Corruption && Hallow && Main.rand.NextBool(40)) return 1;
            if (Main.hardMode && !Meteor && !Jungle && !Dungeon && !Corruption && Hallow && InBrownLayer && Main.rand.NextBool(35)) return 1;
            if (Main.hardMode && !Meteor && !Jungle && !Dungeon && !Corruption && Hallow && InGrayLayer && Main.rand.NextBool(25)) return 1;
            if (Main.hardMode && FrozenOcean && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheSorrow>())) && Main.rand.NextBool(15)) return 1;
            if (Main.hardMode && FrozenOcean && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheSorrow>())) && Main.rand.NextBool(25)) return 1;
            return 0;
        }
        #endregion

        public override void AI()
        {
            base.AI(); // puppet render/flight/weapon/phase machine + ranged-magic selection

            if (_undertowCd > 0) _undertowCd--;
            if (_shellCd > 0) _shellCd--;
            if (_communionCd > 0) _communionCd--;

            // Frost identity + cold aura; extra propulsion motes at the feet while submerged.
            Lighting.AddLight(NPC.Center, 0.25f, 0.45f, 0.7f);
            if (NPC.wet && Main.rand.NextBool(2))
            {
                int jet = Dust.NewDust(NPC.Bottom - new Vector2(NPC.width / 2f, 2f), NPC.width, 6, DustID.IceTorch, 0f, 1.2f, 120, default, 1.1f);
                Main.dust[jet].noGravity = true;
                Main.dust[jet].velocity = new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(0.6f, 1.6f));
            }

            // ── Set-piece triggers (all conditions are deterministic — no Main.rand — so every peer
            // reaches them at the same tick off synced life/position/cooldowns; no server gate needed on
            // the TRIGGER, only on the projectile spawns / cross-entity writes inside them). ──
            Player target = Main.player[NPC.target];

            // Last Rites: hard interrupt the instant HP drops below 5% (interrupts any phase).
            if (!_lastRitesDone && NPC.life <= NPC.lifeMax * 0.05f && _customKind != CustomKind.LastRites)
            {
                _customKind = CustomKind.LastRites;
                StartCustomAttack(LastRitesStand, ItemID.SapphireStaff);
            }
            // Ice Shell / Communion: only from a free phase (won't interrupt a cast in progress).
            else if (Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll)
            {
                float distTiles = NPC.Distance(target.Center) / 16f;
                if (_shellCd <= 0 && distTiles < ShellTriggerTiles
                    && Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1))
                {
                    _shellCd = 600;
                    _customKind = CustomKind.IceShell;
                    StartCustomAttack(ShellEncase); // bare-handed — the crystal covers the body
                }
                else if (_communionCd <= 0)
                {
                    NPC ally = FindWoundedAlly(700f);
                    if (ally != null)
                    {
                        _communionTarget = ally.whoAmI;
                        _communionCd = 480;
                        _customKind = CustomKind.Communion;
                        StartCustomAttack(CommunionChannel, ItemID.SapphireStaff);
                    }
                }
            }
        }

        // Ground movement (only runs when NOT airborne). Prefer the air immediately; drift otherwise.
        protected override void RunMovementAI(float speedMult)
        {
            Flight?.RequestTakeoff();

            Player target = Main.player[NPC.target];
            float dx = target.Center.X - NPC.Center.X;
            NPC.direction = NPC.spriteDirection = dx >= 0 ? 1 : -1;
            NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, Math.Sign(dx) * TopSpeed * 0.5f, Acceleration);
        }

        #region Shared cast helpers
        ///<summary>World position of the staff tip (hand + aim toward the player).</summary>
        Vector2 StaffTip(Player target, float reach = 38f)
        {
            Vector2 hand = PuppetHandPosition;
            Vector2 aim = (target.Center - hand).SafeNormalize(new Vector2(NPC.direction, 0f));
            return hand + aim * reach;
        }

        void StaffTipDust(Vector2 tip, int count = 3)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 pos = tip + Main.rand.NextVector2Circular(6f, 6f);
                int d = Dust.NewDust(pos, 2, 2, DustID.IceTorch, 0f, 0f, 100, default, 1.1f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 0.3f;
            }
        }

        NPC FindWoundedAlly(float maxDist)
        {
            NPC best = null;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.whoAmI == NPC.whoAmI || npc.life <= 0 || npc.life >= npc.lifeMax)
                    continue;
                if (npc.lifeMax <= 5 || npc.townNPC) continue;
                float dist = Vector2.Distance(npc.Center, NPC.Center);
                if (dist < maxDist) { maxDist = dist; best = npc; }
            }
            return best;
        }
        #endregion

        #region Ranged offense (base MAGIC phase — Hail / Veil / Undertow)
        protected override void DoMagicAttack()
        {
            // The weighted RANDOM pick is a genuine coin-flip — server/singleplayer only (Phase 7 rule);
            // clients keep the last-synced cast so they never independently pick a different spell.
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            Player target = Main.player[NPC.target];
            float distTiles = NPC.Distance(target.Center) / 16f;

            if (authority)
            {
                bool undertowOk = _undertowCd <= 0 && distTiles >= UndertowMinTiles && distTiles <= UndertowMaxTiles;
                Span<(MagicCast cast, float weight)> pool = stackalloc (MagicCast, float)[]
                {
                    (MagicCast.Hail,     1f),
                    (MagicCast.Veil,     distTiles < 45f ? 1f : 0f),
                    (MagicCast.Undertow, undertowOk ? 1.2f : 0f),
                };
                float total = 0f;
                for (int i = 0; i < pool.Length; i++)
                {
                    if (pool[i].cast == _lastMagicCast) pool[i].weight *= 0.5f;
                    total += pool[i].weight;
                }
                float roll = Main.rand.NextFloat(total);
                MagicCast pick = MagicCast.Hail;
                for (int i = 0; i < pool.Length; i++)
                {
                    roll -= pool[i].weight;
                    if (roll <= 0f) { pick = pool[i].cast; break; }
                }
                _magicCast = pick;
                NPC.netUpdate = true;
            }
            _lastMagicCast = _magicCast;

            switch (_magicCast)
            {
                case MagicCast.Hail:
                    _magicAttackTicksOverride = HailWindow;
                    SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.5f, Pitch = -0.2f }, NPC.Center);
                    break;
                case MagicCast.Veil:
                    _magicAttackTicksOverride = VeilWindow;
                    SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.6f, Pitch = -0.5f }, NPC.Center);
                    break;
                case MagicCast.Undertow:
                    _magicAttackTicksOverride = UndertowChannel;
                    SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.7f, Pitch = -0.7f }, NPC.Center);
                    if (authority)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                            ModContent.ProjectileType<GigasUndertowZone>(), 0, 0f, Main.myPlayer, NPC.whoAmI, UndertowChannel);
                        _undertowCd = 900;
                    }
                    break;
            }
        }

        protected override void DoMagicTick(int ticksRemaining)
        {
            Player target = Main.player[NPC.target];
            bool server = Main.netMode != NetmodeID.MultiplayerClient;

            switch (_magicCast)
            {
                case MagicCast.Hail:
                {
                    Vector2 tip = StaffTip(target);
                    StaffTipDust(tip, 2);
                    int into = HailWindow - ticksRemaining;
                    if (into % 6 == 0 && server)
                    {
                        Vector2 aim = (target.Center - tip).SafeNormalize(new Vector2(NPC.direction, 0f));
                        Vector2 vel = aim.RotatedBy(Main.rand.NextFloat(-0.18f, 0.18f)) * Main.rand.NextFloat(8.5f, 11f);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), tip, vel,
                            ModContent.ProjectileType<GigasIceShard>(), Scaled(HailShardDamage), 2f, Main.myPlayer, 0f);
                        SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.4f, Pitch = 0.4f }, NPC.Center);
                    }
                    break;
                }
                case MagicCast.Veil:
                {
                    Vector2 tip = StaffTip(target);
                    StaffTipDust(tip, 3);
                    int into = VeilWindow - ticksRemaining;
                    if (into % 4 == 0 && server)
                    {
                        float baseAngle = (target.Center - tip).ToRotation();
                        for (int k = -1; k <= 1; k++)
                        {
                            float a = baseAngle + k * 0.5f + Main.rand.NextFloat(-0.12f, 0.12f);
                            Vector2 vel = a.ToRotationVector2() * Main.rand.NextFloat(5f, 7.5f);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), tip, vel,
                                ModContent.ProjectileType<GigasIceShard>(), Scaled(VeilShardDamage), 1.5f, Main.myPlayer, 0.06f);
                        }
                    }
                    break;
                }
                case MagicCast.Undertow:
                {
                    for (int i = 0; i < 3; i++)
                    {
                        float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                        Vector2 pos = NPC.Center + angle.ToRotationVector2() * Main.rand.NextFloat(120f, 300f);
                        int d = Dust.NewDust(pos, 4, 4, DustID.Frost, 0f, 0f, 120, default, 1.1f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].velocity = (NPC.Center - pos).SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(4f, 8f);
                    }
                    if (ticksRemaining == 1) // climax: radial ice-shard eruption
                    {
                        SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.6f, Pitch = -0.3f }, NPC.Center);
                        UsefulFunctions.ScreenShake(NPC.Center, 6f, 10, distanceFalloff: 900f);
                        if (server)
                            for (int i = 0; i < 18; i++)
                            {
                                float a = MathHelper.TwoPi * i / 18f;
                                Vector2 vel = a.ToRotationVector2() * Main.rand.NextFloat(6f, 9f);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
                                    ModContent.ProjectileType<GigasIceShard>(), Scaled(UndertowShardDamage), 2f, Main.myPlayer, 0f);
                            }
                    }
                    break;
                }
            }
        }
        #endregion

        #region Set-pieces (base CUSTOM phase — Ice Shell / Communion / Last Rites)
        protected override bool SlowDownDuringCustom => true;

        protected override void DoCustomAttack()
        {
            switch (_customKind)
            {
                case CustomKind.IceShell:
                    DebugAttackLabel = "Ice Shell";
                    SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.7f, Pitch = -0.5f }, NPC.Center);
                    if (!Main.dedServ)
                        for (int i = 0; i < 24; i++)
                        {
                            Vector2 pos = NPC.Center + Main.rand.NextVector2Circular(30f, 40f);
                            int d = Dust.NewDust(pos, 4, 4, DustID.IceTorch, 0f, 0f, 80, default, 1.4f);
                            Main.dust[d].noGravity = true;
                            Main.dust[d].velocity = (pos - NPC.Center) * 0.05f;
                        }
                    break;
                case CustomKind.Communion:
                    DebugAttackLabel = "Communion";
                    SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.6f, Pitch = -0.8f }, NPC.Center);
                    break;
                case CustomKind.LastRites:
                    DebugAttackLabel = "Last Rites";
                    SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.7f, Pitch = -0.7f }, NPC.Center);
                    break;
            }
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            bool server = Main.netMode != NetmodeID.MultiplayerClient;
            switch (_customKind)
            {
                case CustomKind.IceShell: TickIceShell(ticksRemaining, server); break;
                case CustomKind.Communion: TickCommunion(ticksRemaining, server); break;
                case CustomKind.LastRites: TickLastRites(ticksRemaining, server); break;
            }
        }

        void TickIceShell(int ticksRemaining, bool server)
        {
            NPC.velocity *= 0.85f; // frozen inside the shell
            if (Main.rand.NextBool(2))
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Frost, 0f, -0.4f, 150, default, 1f);
                Main.dust[d].noGravity = true;
            }
            if (ticksRemaining == 1) // shatter
            {
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.8f, Pitch = 0.2f }, NPC.Center);
                UsefulFunctions.ScreenShake(NPC.Center, 5f, 8, distanceFalloff: 700f);
                if (server)
                    for (int i = 0; i < 16; i++)
                    {
                        float a = MathHelper.TwoPi * i / 16f + Main.rand.NextFloat(-0.1f, 0.1f);
                        Vector2 vel = a.ToRotationVector2() * Main.rand.NextFloat(6f, 9f);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
                            ModContent.ProjectileType<GigasIceShard>(), Scaled(ShellShardDamage), 2f, Main.myPlayer, 0f);
                    }
                for (int i = 0; i < 30; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(7f, 7f);
                    int d = Dust.NewDust(NPC.Center, 4, 4, DustID.Ice, vel.X, vel.Y, 60, default, 1.5f);
                    Main.dust[d].noGravity = true;
                }
            }
        }

        void TickCommunion(int ticksRemaining, bool server)
        {
            NPC ally = _communionTarget >= 0 && _communionTarget < Main.maxNPCs ? Main.npc[_communionTarget] : null;
            bool valid = ally != null && ally.active && !ally.friendly && ally.life > 0 && ally.life < ally.lifeMax;
            if (!valid) return;

            Vector2 hand = PuppetHandPosition;
            for (int i = 0; i < 2; i++)
            {
                float t = Main.rand.NextFloat();
                Vector2 pos = Vector2.Lerp(hand, ally.Center, t) + Main.rand.NextVector2Circular(8f, 8f);
                int w = Dust.NewDust(pos, 4, 4, DustID.IceTorch, 0f, 0f, 80, default, 1.1f);
                Main.dust[w].noGravity = true;
                Main.dust[w].velocity = (ally.Center - hand).SafeNormalize(Vector2.UnitX) * 4f;
            }
            int into = CommunionChannel - ticksRemaining;
            if (into >= 30 && (into - 30) % 20 == 0)
            {
                // Mutating a DIFFERENT NPC's life is server-only (Necromancer ghoul-consumption precedent);
                // ally.life is itself normally-synced, so clients get the value from the ordinary NPC sync.
                if (server)
                {
                    int heal = Math.Max(CommunionHealPerPulse, (int)(ally.lifeMax * 0.03f));
                    ally.life = Math.Min(ally.lifeMax, ally.life + heal);
                    ally.HealEffect(heal);
                    ally.netUpdate = true;
                }
                if (Main.netMode != NetmodeID.Server)
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), ally.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.HealSpriteVFX>(), 0, 0);
                SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.4f, Pitch = 0.5f }, NPC.Center);
            }
        }

        void TickLastRites(int ticksRemaining, bool server)
        {
            if (ticksRemaining > 1)
            {
                // green cursed glow mounting
                for (int i = 0; i < 3; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 pos = NPC.Center + angle.ToRotationVector2() * (50f + Main.rand.NextFloat(10f));
                    int d = Dust.NewDust(pos, 4, 4, DustID.CursedTorch, 0f, 0f, 90, default, 1.2f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity = (NPC.Center - pos) * 0.08f;
                }
                return;
            }

            // ticksRemaining == 1: the curse erupts.
            _lastRitesDone = true;
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.8f, Pitch = -0.2f }, NPC.Center);
            UsefulFunctions.ScreenShake(NPC.Center, 6f, 10, distanceFalloff: 900f);
            if (server)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<ClericLastRitesRing>(), Scaled(LastRitesRingDamage), 4f, Main.myPlayer, 320f);
                // Spread the curse: nearby enemies inflict Cursed Inferno for 30s. Buffing OTHER NPCs is
                // server-only (their buffType[]/buffTime[] are themselves normally-synced fields).
                float radius = 40f * 16f;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC other = Main.npc[i];
                    if (other.active && !other.friendly && other.whoAmI != NPC.whoAmI && other.Distance(NPC.Center) < radius)
                        other.AddBuff(ModContent.BuffType<Buffs.SorrowfulCurseBuff>(), 30 * 60);
                }
            }
            for (int i = 0; i < 40; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(5f, 5f);
                int d = Dust.NewDust(NPC.Center, 4, 4, DustID.CursedTorch, vel.X, vel.Y, 60, default, 1.6f);
                Main.dust[d].noGravity = true;
            }
        }
        #endregion

        // Abstract on the base — unused (magic-only), so no-ops.
        protected override void DoMeleeAttack() { }
        protected override void DoRangedAttack() { }

        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            base.ModifyIncomingHit(ref modifiers);
            // Ice Shell: heavily damage-reduced while encased (a committed player can still chip it).
            if (Phase == AttackPhase.Custom && _customKind == CustomKind.IceShell)
            {
                modifiers.FinalDamage *= 0.2f;
            }
        }

        #region Draw (ice-crystal shell behind a half-transparent puppet)
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            bool shellActive = Phase == AttackPhase.Custom && _customKind == CustomKind.IceShell;
            if (shellActive && !Main.dedServ)
            {
                ShellTexture ??= ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Enemies/ClericIceShell");
                Texture2D tex = ShellTexture.Value;
                Vector2 pos = NPC.Center - Main.screenPosition + new Vector2(0f, -4f);
                Color tint = new Color(180, 220, 255, 90) * 0.8f;
                // Drawn BEFORE the puppet → sits behind it. 1.5x so it envelops the body.
                spriteBatch.Draw(tex, pos, null, tint, 0f, tex.Size() / 2f, 1.5f * NPC.scale, SpriteEffects.None, 0f);
                // Puppet drawn 50% transparent so it reads as frozen inside the crystal.
                return base.PreDraw(spriteBatch, screenPos, drawColor * 0.5f);
            }
            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }
        #endregion

        #region Gore / loot
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.6f, Pitch = -0.3f }, NPC.Center);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dark Elf Magi Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dark Elf Magi Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dark Elf Magi Gore 3").Type, 1f);
                for (int i = 0; i < 24; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(4f, 4f);
                    int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Frost, vel.X, vel.Y, 60, default, 1.3f);
                    Main.dust[d].noGravity = true;
                }
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 30));
            npcLoot.Add(ItemDropRule.Common(ItemID.ManaRegenerationPotion, 35));
            npcLoot.Add(ItemDropRule.Common(ItemID.GreaterHealingPotion, 20));
            npcLoot.Add(new CommonDrop(ItemID.GillsPotion, 100, 1, 1, 6));
            npcLoot.Add(new CommonDrop(ItemID.HunterPotion, 100, 1, 1, 6));
            npcLoot.Add(ItemDropRule.Common(ItemID.MagicPowerPotion, 25));
            npcLoot.Add(ItemDropRule.Common(ItemID.ShinePotion, 25));
            npcLoot.Add(ItemDropRule.Common(ItemID.SoulofNight, 2));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShard>(), 7));
        }
        #endregion
    }
}
