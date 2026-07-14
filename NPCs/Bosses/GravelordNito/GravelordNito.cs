using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy;

namespace tsorcRevamp.NPCs.Bosses.GravelordNito
{
    public class GravelordNito : ModNPC, IStaggerable
    {
        public override string Texture => "tsorcRevamp/NPCs/Bosses/GravelordNito/GravelordNitoAttacking";
        // Explicit head path — [AutoloadBossHead] can't be used because it would derive the head from the
        // Texture above (GravelordNitoAttacking_Head_Boss), which doesn't exist.
        public override string BossHeadTexture => "tsorcRevamp/NPCs/Bosses/GravelordNito/GravelordNito_Head_Boss";

        enum AttackState : byte
        {
            None = 0,
            SideSweep,
            BackhandSweep,
            OverheadCleave,
            ImpalingThrust,
            TripleReaperCombo,
            DraggingAdvance,
            LeapingCleave,
            SwordRain,
            BoneVolley,
            GravelordSpikes,
            DeathNova,
            MiasmaBreath,
            BonePillarCage,
            GraveHands,
            QuietusCombo,
            CemeteryMarch,
            HollowCommand,
            GravelordJudgment,
            PhaseTransition,
            ComboRecovery,
        }

        const int FrameCount = 23;
        const int FrameHeight = 300;
        const int BodyWidth = 400;
        // The character art is NOT centered in the 400px frame — its opaque body is centered at
        // ~x=283 (measured). Mirroring around the frame's geometric center (200) on a facing flip
        // shifted the body ±83px = a ~166px "teleport". Use the true body center as the horizontal
        // origin so a flip mirrors the body IN PLACE.
        const float BodyDrawCenterX = 283f;
        const int HeavyTelegraph = 48;
        const int LongChannelTicks = 120;
        const int LongChannelStaggerTicks = 80;

        AttackState State = AttackState.None;
        AttackState LastAttack = AttackState.None;
        int AttackTimer;
        int AttackCooldown = 150;
        int lockedDir = 1;
        int lockedKind = 0; // vertical-read slash kind for the current single-hit attack; see ChooseVerticalKind
        int FootstepTimer;
        int ComboRecoveryTicks;
        bool PhaseTwo;
        bool HalfTelegraph;

        // ── Loose-sword swing animation ─────────────────────────────────────────
        // The body sheet is ALWAYS the no-sword art (GravelordNitoAttacking.png — despite the name,
        // it has no blade painted in; GravelordNito.png is the one with the sword baked in and is no
        // longer used for the body). A single loose sword layer (GravelordNitoSword.png) is drawn
        // BEHIND the body every frame. It rests in a horizontal idle pose, winds up toward a slash's
        // start pose, plays the full swing arc itself (the NitoSwordSlash projectile is an invisible
        // hitbox that mirrors this same arc), then eases back to idle. All poses come from the shared
        // SlashPhi/SlashReach helpers so the visible blade and the hitbox always agree.
        int SlashWindupKind;
        int SlashWindupStartTick;
        int SlashWindupEndTick;
        bool SlashWindupActive;
        int SlashActiveKind = -1; // kind of the most recently released slash this state; -1 = none fired yet
        int SlashActiveStartTick; // AttackTimer at that release
        const int SlashActiveTicks = 18; // must match NitoSwordSlash.timeLeft so the visible arc matches the hitbox
        const int SlashReturnTicks = 14;
        // Sword rig around NPC.Center: the "shoulder" the blade pivots from, its idle reach, and how
        // far the whole sprite is drawn sunk into the floor so his ragged base always touches ground.
        const float SwordPivotX = 18f;
        const float SwordPivotY = -96f;
        const float SwordIdleReach = 78f;
        const float GroundSinkPixels = 30f;

        int SlashDamage => 23;
        int HeavySlashDamage => 29;
        int BoneDamage => 20;
        int DeathDamage => 24;

        bool IsMeleeState => State == AttackState.SideSweep || State == AttackState.BackhandSweep
            || State == AttackState.OverheadCleave || State == AttackState.ImpalingThrust
            || State == AttackState.TripleReaperCombo || State == AttackState.DraggingAdvance
            || State == AttackState.LeapingCleave || State == AttackState.QuietusCombo;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = FrameCount;
            NPCID.Sets.TrailCacheLength[Type] = 6;
            NPCID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            NPC.width = 118;
            NPC.height = 174;
            NPC.damage = 0;
            NPC.defense = 12;
            NPC.lifeMax = 4000;
            NPC.HitSound = SoundID.NPCHit2;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.value = 91480f;
            NPC.npcSlots = 100f;
            NPC.boss = true;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.knockBackResist = 0.15f;
            NPC.aiStyle = -1;
            Music = MusicID.Boss2;

            tsorcRevampGlobalNPC g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            g.NavSearchRadius = 70;
            // A small jump only — 1-2 tile ledges are already handled smoothly by the shared
            // AutoStepUp system (unconditional for grounded NPCs), so this lower ceiling only
            // covers genuine gaps the pathfinder can't step around; he should read as a heavy,
            // grounded fighter, not a leaping beast.
            g.MaxJumpPower = 6f;
            g.MaxJumpBoost = 3f;
            // Support core: the center 4 tiles must be on solid ground (matches his ~7.4-tile width).
            // The wider sprite edges — and up to half his ~11-tile height on a downslope — are allowed
            // to sink into terrain instead of floating in the air over uneven ground.
            g.BeastSinkMaxTiles = 5;
            g.KiteRangeMin = 0f;
            g.KiteRangeMax = 24f;
            g.KiteLooseness = 0.45f;
            g.PatrolMode = NPCs.PatrolMode.Wander; // "tsorcRevamp.NPCs" would resolve to the Mod class, not the namespace
            EvasiveProfile.HeavyBeast(g);
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) => 0f;

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.HealingPotion;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)State);
            writer.Write((byte)LastAttack);
            writer.Write(AttackTimer);
            writer.Write(AttackCooldown);
            writer.Write((sbyte)lockedDir);
            writer.Write(PhaseTwo);
            writer.Write(HalfTelegraph);
            writer.Write((short)ComboRecoveryTicks);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            State = (AttackState)reader.ReadByte();
            LastAttack = (AttackState)reader.ReadByte();
            AttackTimer = reader.ReadInt32();
            AttackCooldown = reader.ReadInt32();
            lockedDir = reader.ReadSByte();
            PhaseTwo = reader.ReadBoolean();
            HalfTelegraph = reader.ReadBoolean();
            ComboRecoveryTicks = reader.ReadInt16();
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            if (State == AttackState.None)
            {
                tsorcRevampAIs.EvasiveOnHit(NPC, true);
            }
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (State == AttackState.None)
            {
                tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
            }
        }

        public void OnStagger(NPC npc)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.85f, Pitch = -0.45f }, NPC.Center);
                for (int i = 0; i < 36; i++)
                {
                    Dust d = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(70f, 110f), DustID.BoneTorch, Main.rand.NextVector2Circular(4f, 4f), 90, default, 1.35f);
                    d.noGravity = true;
                }
            }

            State = AttackState.None;
            AttackTimer = 0;
            HalfTelegraph = false;
            SlashWindupActive = false;
            ComboRecoveryTicks = 0;
            AttackCooldown = Math.Max(AttackCooldown, 120);
            NPC.damage = 0;
            NPC.netUpdate = true;
        }

        public override void AI()
        {
            NPC.damage = 0;
            if (NPC.target < 0 || NPC.target >= Main.maxPlayers || !Main.player[NPC.target].active || Main.player[NPC.target].dead)
            {
                NPC.TargetClosest(false);
            }

            Player player = Main.player[NPC.target];
            tsorcRevampGlobalNPC g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            g.AttackTelegraphing = false;
            g.AttackCommitted = false;

            if (!PhaseTwo && Main.netMode != NetmodeID.MultiplayerClient && NPC.life <= NPC.lifeMax / 2)
            {
                PhaseTwo = true;
                StartAttack(AttackState.PhaseTransition, player);
            }

            if (g.StaggerTimer > 0)
            {
                NPC.rotation = MathHelper.Lerp(NPC.rotation, -NPC.direction * 0.12f, 0.08f);
                if (Main.rand.NextBool(3))
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.BoneTorch, 0f, 1.2f, 100, default, 0.9f);
                }
                UpdateAura();
                return;
            }

            NPC.rotation *= 0.9f;

            if (State == AttackState.None)
            {
                // canWalkBackwards:false — he now turns to FACE his walking direction instead of
                // moon-walking toward the player. StayGroundedRelativeTo cancels nav hops that would
                // carry him up onto platforms the player isn't standing on (LeapingCleave is his one
                // deliberate vertical answer).
                tsorcRevampAIs.FighterAI(NPC, topSpeed: 0.62f, acceleration: 0.45f, canTeleport: false, lavaJumping: true, canDodgeroll: false, canPounce: false, minSurfaceWidth: 4, canWalkBackwards: false);
                StayGroundedRelativeTo(player);
                FootstepEffects();

                if (AttackCooldown > 0)
                {
                    AttackCooldown--;
                }

                if (Main.netMode != NetmodeID.MultiplayerClient && AttackCooldown <= 0 && NPC.velocity.Y == 0f && player.active && !player.dead && NPC.Distance(player.Center) < 1050f)
                {
                    PickAttack(player);
                }
            }
            else
            {
                RunAttack(g, player);
            }

            UpdateAura();
        }

        void PickAttack(Player player)
        {
            float dist = NPC.Distance(player.Center);
            bool sameLevel = Math.Abs(player.Center.Y - NPC.Center.Y) < 120f;
            List<(AttackState state, int weight)> pool = new()
            {
                (AttackState.SideSweep, dist < 260f && sameLevel ? 8 : 1),
                (AttackState.BackhandSweep, dist < 220f && sameLevel ? 6 : 1),
                (AttackState.OverheadCleave, dist < 280f ? 6 : 2),
                (AttackState.ImpalingThrust, dist < 360f && sameLevel ? 6 : 1),
                (AttackState.TripleReaperCombo, PhaseTwo && dist < 340f ? 7 : 0),
                (AttackState.DraggingAdvance, dist > 180f && dist < 520f && sameLevel ? 7 : 1),
                (AttackState.LeapingCleave, Math.Abs(player.Center.Y - NPC.Center.Y) > 80f || dist > 420f ? 7 : 2),
                (AttackState.SwordRain, dist > 280f ? 6 : 2),
                (AttackState.BoneVolley, dist > 220f ? 6 : 2),
                (AttackState.GravelordSpikes, 7),
                (AttackState.DeathNova, PhaseTwo ? 5 : 2),
                (AttackState.MiasmaBreath, dist < 440f ? 5 : 2),
                (AttackState.BonePillarCage, PhaseTwo ? 5 : 0),
                (AttackState.GraveHands, 5),
                (AttackState.QuietusCombo, PhaseTwo && dist < 380f ? 5 : 0),
                (AttackState.CemeteryMarch, PhaseTwo ? 5 : 2),
                (AttackState.HollowCommand, 4),
                (AttackState.GravelordJudgment, PhaseTwo ? 4 : 1),
            };

            int total = 0;
            foreach ((AttackState state, int weight) in pool)
            {
                int adjusted = state == LastAttack ? weight / 2 : weight;
                total += Math.Max(0, adjusted);
            }
            int roll = Main.rand.Next(Math.Max(total, 1));
            foreach ((AttackState state, int weight) in pool)
            {
                int adjusted = state == LastAttack ? weight / 2 : weight;
                if (adjusted <= 0)
                {
                    continue;
                }
                roll -= adjusted;
                if (roll < 0)
                {
                    StartAttack(state, player);
                    return;
                }
            }
            StartAttack(AttackState.SideSweep, player);
        }

        void RunAttack(tsorcRevampGlobalNPC g, Player player)
        {
            AttackTimer++;
            switch (State)
            {
                case AttackState.SideSweep: RunSwordAttack(g, player, lockedKind, Telegraph(30), 44, 84, SlashDamage); break;
                case AttackState.BackhandSweep: RunSwordAttack(g, player, lockedKind, Telegraph(26), 38, 78, SlashDamage); break;
                case AttackState.OverheadCleave: RunSwordAttack(g, player, lockedKind, Telegraph(HeavyTelegraph), 64, 116, HeavySlashDamage); break;
                case AttackState.ImpalingThrust: RunSwordAttack(g, player, lockedKind, Telegraph(34), 44, 82, SlashDamage); break;
                case AttackState.TripleReaperCombo: RunTripleCombo(g, player); break;
                case AttackState.DraggingAdvance: RunDraggingAdvance(g); break;
                case AttackState.LeapingCleave: RunLeapingCleave(g, player); break;
                case AttackState.SwordRain: RunSwordRain(g, player); break;
                case AttackState.BoneVolley: RunBoneVolley(g, player); break;
                case AttackState.GravelordSpikes: RunGravelordSpikes(g, player); break;
                case AttackState.DeathNova: RunDeathNova(g); break;
                case AttackState.MiasmaBreath: RunMiasmaBreath(g); break;
                case AttackState.BonePillarCage: RunBonePillarCage(g, player); break;
                case AttackState.GraveHands: RunGraveHands(g, player); break;
                case AttackState.QuietusCombo: RunQuietusCombo(g); break;
                case AttackState.CemeteryMarch: RunCemeteryMarch(g, player); break;
                case AttackState.HollowCommand: RunHollowCommand(g, player); break;
                case AttackState.GravelordJudgment: RunGravelordJudgment(g, player); break;
                case AttackState.PhaseTransition: RunPhaseTransition(g); break;
                case AttackState.ComboRecovery: RunComboRecovery(); break;
            }
        }

        void RunSwordAttack(tsorcRevampGlobalNPC g, Player player, int slashKind, int telegraphTicks, int releaseTick, int endTick, int damage)
        {
            if (AttackTimer <= releaseTick)
            {
                g.AttackCommitted = true;
                FacePlayer(player);
                NPC.velocity.X *= 0.86f;
            }
            if (AttackTimer == 1)
            {
                TelegraphCue(Color.LightGray);
                ArmSlashWindup(slashKind, 1, releaseTick);
            }
            if (AttackTimer <= telegraphTicks)
            {
                SwordTelegraphDust(slashKind);
            }
            if (AttackTimer == releaseTick)
            {
                SpawnSlash(slashKind, damage);
            }
            if (AttackTimer >= endTick)
            {
                EndAttack(170);
            }
        }

        void RunTripleCombo(tsorcRevampGlobalNPC g, Player player)
        {
            g.AttackCommitted = AttackTimer <= 102;
            if (AttackTimer == 1)
            {
                TelegraphCue(new Color(180, 180, 210));
                ArmSlashWindup(0, 1, 30);
            }
            if (AttackTimer < 25)
            {
                FacePlayer(player);
                SwordTelegraphDust(0);
            }
            if (AttackTimer == 30) { SpawnSlash(0, SlashDamage); ArmSlashWindup(3, 30, 56); }
            if (AttackTimer == 56) { SpawnSlash(3, SlashDamage); ArmSlashWindup(1, 56, 84); }
            if (AttackTimer == 84) SpawnSlash(1, HeavySlashDamage);
            if (AttackTimer >= 128)
            {
                ComboRecoveryTicks = 45;
                StartAttack(AttackState.ComboRecovery, player);
            }
        }

        void RunDraggingAdvance(tsorcRevampGlobalNPC g)
        {
            int windup = Telegraph(38);
            g.AttackCommitted = AttackTimer <= 90;
            if (AttackTimer == 1)
            {
                TelegraphCue(new Color(170, 170, 190));
                ArmSlashWindup(0, 1, windup);
            }
            if (AttackTimer < windup)
            {
                NPC.velocity.X *= 0.82f;
                SwordTelegraphDust(0);
            }
            else if (AttackTimer <= 86)
            {
                NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, lockedDir * 4.2f, 0.12f);
                if (AttackTimer % 16 == 0) { SpawnSlash(0, SlashDamage); ArmSlashWindup(0, AttackTimer, AttackTimer + 16); }
                DragDust();
            }
            else
            {
                NPC.velocity.X *= 0.82f;
            }
            if (AttackTimer >= 120) EndAttack(190);
        }

        void RunLeapingCleave(tsorcRevampGlobalNPC g, Player player)
        {
            g.AttackCommitted = AttackTimer <= 82;
            if (AttackTimer == 1)
            {
                TelegraphCue(Color.White);
                ArmSlashWindup(1, 1, 60);
            }
            if (AttackTimer < 28)
            {
                FacePlayer(player);
                NPC.velocity.X *= 0.75f;
                SwordTelegraphDust(1);
            }
            if (AttackTimer == 28)
            {
                NPC.velocity = new Vector2(lockedDir * 5f, -8.8f);
                NPC.netUpdate = true;
            }
            if (AttackTimer == 60) SpawnSlash(1, HeavySlashDamage);
            if (AttackTimer > 60 && NPC.collideY)
            {
                UsefulFunctions.ScreenShake(NPC.Bottom, 5f, 12, 6f, 500f);
                SpawnGroundSpike(NPC.Bottom + new Vector2(lockedDir * 72f, 0f), 12, 1.2f);
                SpawnGroundSpike(NPC.Bottom + new Vector2(lockedDir * 132f, 0f), 18, 1f);
                EndAttack(210);
            }
            if (AttackTimer >= 160) EndAttack(210);
        }

        void RunSwordRain(tsorcRevampGlobalNPC g, Player player)
        {
            int cast = Telegraph(42);
            g.AttackCommitted = AttackTimer <= cast + 52;
            if (AttackTimer == 1) TelegraphCue(new Color(160, 160, 210));
            if (AttackTimer < cast)
            {
                NPC.velocity.X *= 0.8f;
                for (int i = 0; i < 2; i++) Dust.NewDust(player.position + new Vector2(Main.rand.NextFloat(-180f, 180f), -260f), 8, 8, DustID.BoneTorch, 0f, 1f, 90, default, 1f);
            }
            if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer >= cast && AttackTimer <= cast + 48 && (AttackTimer - cast) % 12 == 0)
            {
                Vector2 pos = player.Center + new Vector2(Main.rand.NextFloat(-220f, 220f), -330f);
                Vector2 velocity = UsefulFunctions.Aim(pos, player.Center + new Vector2(Main.rand.NextFloat(-40f, 40f), 0f), 8.5f);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), pos, velocity, ModContent.ProjectileType<NitoCeilingSpike>(), BoneDamage, 1f, Main.myPlayer);
            }
            if (AttackTimer >= cast + 90) EndAttack(190);
        }

        void RunBoneVolley(tsorcRevampGlobalNPC g, Player player)
        {
            int cast = Telegraph(30);
            g.AttackCommitted = AttackTimer <= cast + 30;
            if (AttackTimer == 1) TelegraphCue(new Color(180, 180, 180));
            if (AttackTimer < cast)
            {
                FacePlayer(player);
                NPC.velocity.X *= 0.85f;
            }
            if (Main.netMode != NetmodeID.MultiplayerClient && (AttackTimer == cast || AttackTimer == cast + 14 || AttackTimer == cast + 28))
            {
                for (int i = -2; i <= 2; i++)
                {
                    Vector2 velocity = UsefulFunctions.Aim(NPC.Center + new Vector2(lockedDir * 30f, -60f), player.Center, 7.5f).RotatedBy(i * 0.13f);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(lockedDir * 70f, -88f), velocity, ModContent.ProjectileType<NitoBoneShard>(), BoneDamage, 1f, Main.myPlayer);
                }
            }
            if (AttackTimer >= cast + 62) EndAttack(160);
        }

        void RunGravelordSpikes(tsorcRevampGlobalNPC g, Player player)
        {
            int cast = Telegraph(36);
            g.AttackCommitted = AttackTimer <= cast + 36;
            if (AttackTimer == 1) TelegraphCue(new Color(140, 140, 160));
            if (AttackTimer < cast)
            {
                NPC.velocity.X *= 0.8f;
                GraveDust(player.Bottom);
            }
            if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer == cast)
            {
                for (int i = -2; i <= 2; i++) SpawnGroundSpike(player.Bottom + new Vector2(i * 54f, 0f), 16 + Math.Abs(i) * 5, 1f + (2 - Math.Abs(i)) * 0.1f);
            }
            if (AttackTimer >= cast + 78) EndAttack(175);
        }

        void RunDeathNova(tsorcRevampGlobalNPC g)
        {
            if (AttackTimer <= LongChannelStaggerTicks) g.AttackTelegraphing = true;
            else if (AttackTimer <= LongChannelTicks) g.AttackCommitted = true;

            NPC.velocity.X *= 0.75f;
            if (AttackTimer == 1) TelegraphCue(new Color(120, 90, 160));
            if (AttackTimer == LongChannelStaggerTicks + 1) TelegraphCue(Color.Purple);
            if (AttackTimer <= LongChannelTicks)
            {
                float radius = MathHelper.Lerp(220f, 32f, AttackTimer / (float)LongChannelTicks);
                for (int i = 0; i < 4; i++)
                {
                    Vector2 pos = NPC.Center + Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * radius;
                    Dust d = Dust.NewDustPerfect(pos, DustID.Shadowflame, UsefulFunctions.Aim(pos, NPC.Center, 2.5f), 80, default, 1.1f);
                    d.noGravity = true;
                }
            }
            if (AttackTimer == LongChannelTicks && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<NitoDeathNova>(), DeathDamage, 5f, Main.myPlayer, 300f);
                UsefulFunctions.ScreenShake(NPC.Center, 8f, 18);
            }
            if (AttackTimer >= LongChannelTicks + 48) EndAttack(260);
        }

        void RunMiasmaBreath(tsorcRevampGlobalNPC g)
        {
            int cast = Telegraph(38);
            g.AttackCommitted = AttackTimer <= cast + 76;
            if (AttackTimer == 1) TelegraphCue(new Color(105, 140, 95));
            if (AttackTimer < cast) NPC.velocity.X *= 0.82f;
            if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer >= cast && AttackTimer <= cast + 70 && AttackTimer % 7 == 0)
            {
                Vector2 pos = NPC.Center + new Vector2(lockedDir * 90f, -90f);
                Vector2 velocity = new Vector2(lockedDir * Main.rand.NextFloat(2.8f, 4.4f), Main.rand.NextFloat(-1.2f, 1.2f));
                Projectile.NewProjectile(NPC.GetSource_FromThis(), pos, velocity, ModContent.ProjectileType<NitoMiasmaCloud>(), DeathDamage / 2, 0.2f, Main.myPlayer);
            }
            if (AttackTimer >= cast + 108) EndAttack(180);
        }

        void RunBonePillarCage(tsorcRevampGlobalNPC g, Player player)
        {
            int cast = Telegraph(42);
            g.AttackCommitted = AttackTimer <= cast + 24;
            if (AttackTimer == 1) TelegraphCue(new Color(190, 190, 210));
            if (AttackTimer < cast) GraveDust(player.Bottom);
            if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer == cast)
            {
                for (int i = -3; i <= 3; i++)
                {
                    if (i != 0) SpawnGroundSpike(player.Bottom + new Vector2(i * 46f, 0f), 20 + Math.Abs(i) * 4, 1.35f);
                }
            }
            if (AttackTimer >= cast + 82) EndAttack(210);
        }

        void RunGraveHands(tsorcRevampGlobalNPC g, Player player)
        {
            int cast = Telegraph(28);
            g.AttackCommitted = AttackTimer <= cast + 20;
            if (AttackTimer == 1) TelegraphCue(new Color(125, 125, 145));
            if (AttackTimer < cast) GraveDust(player.Bottom);
            if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer == cast)
            {
                for (int i = -1; i <= 1; i += 2) Projectile.NewProjectile(NPC.GetSource_FromThis(), player.Bottom + new Vector2(i * 84f, -34f), Vector2.Zero, ModContent.ProjectileType<NitoGraveHand>(), HeavySlashDamage, 2f, Main.myPlayer, 18f);
            }
            if (AttackTimer >= cast + 70) EndAttack(165);
        }

        void RunQuietusCombo(tsorcRevampGlobalNPC g)
        {
            g.AttackCommitted = AttackTimer <= 98;
            if (AttackTimer == 1)
            {
                TelegraphCue(new Color(120, 95, 160));
                ArmSlashWindup(2, 1, 28);
            }
            if (AttackTimer <= 24) SwordTelegraphDust(2);
            if (AttackTimer == 28) { SpawnSlash(2, SlashDamage); ArmSlashWindup(1, 28, 58); }
            if (AttackTimer == 58) SpawnSlash(1, HeavySlashDamage);
            if (AttackTimer == 94 && Main.netMode != NetmodeID.MultiplayerClient) Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<NitoDeathNova>(), DeathDamage, 5f, Main.myPlayer, 150f);
            if (AttackTimer >= 140) EndAttack(240);
        }

        void RunCemeteryMarch(tsorcRevampGlobalNPC g, Player player)
        {
            int cast = Telegraph(30);
            g.AttackCommitted = AttackTimer <= cast + 90;
            if (AttackTimer == 1) TelegraphCue(Color.Gray);
            if (AttackTimer < cast) FacePlayer(player);
            else if (AttackTimer <= cast + 82)
            {
                tsorcRevampAIs.FighterAI(NPC, topSpeed: 0.5f, acceleration: 0.35f, canTeleport: false, lavaJumping: true, canDodgeroll: false, canPounce: false, minSurfaceWidth: 4, canWalkBackwards: false);
                if ((AttackTimer - cast) % 18 == 0)
                {
                    SpawnGroundSpike(NPC.Bottom + new Vector2(lockedDir * 96f, 0f), 12, 0.9f);
                    SpawnGroundSpike(NPC.Bottom + new Vector2(lockedDir * 160f, 0f), 20, 1.05f);
                }
            }
            if (AttackTimer >= cast + 124) EndAttack(180);
        }

        void RunHollowCommand(tsorcRevampGlobalNPC g, Player player)
        {
            int cast = Telegraph(40);
            g.AttackCommitted = AttackTimer <= cast + 34;
            if (AttackTimer == 1) TelegraphCue(new Color(150, 150, 170));
            if (AttackTimer < cast) NPC.velocity.X *= 0.8f;
            if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer == cast)
            {
                for (int i = 0; i < 8; i++)
                {
                    float angle = MathHelper.TwoPi * i / 8f;
                    Vector2 velocity = angle.ToRotationVector2() * 5.4f;
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + velocity.SafeNormalize(Vector2.UnitY) * 80f, velocity, ModContent.ProjectileType<NitoBoneShard>(), BoneDamage, 1f, Main.myPlayer);
                }
                Projectile.NewProjectile(NPC.GetSource_FromThis(), player.Bottom + new Vector2(-92f, -34f), Vector2.Zero, ModContent.ProjectileType<NitoGraveHand>(), SlashDamage, 2f, Main.myPlayer, 26f);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), player.Bottom + new Vector2(92f, -34f), Vector2.Zero, ModContent.ProjectileType<NitoGraveHand>(), SlashDamage, 2f, Main.myPlayer, 26f);
            }
            if (AttackTimer >= cast + 88) EndAttack(200);
        }

        void RunGravelordJudgment(tsorcRevampGlobalNPC g, Player player)
        {
            int cast = Telegraph(50);
            g.AttackCommitted = AttackTimer <= cast + 60;
            if (AttackTimer == 1) TelegraphCue(new Color(210, 210, 230));
            if (AttackTimer < cast)
            {
                NPC.velocity.X *= 0.78f;
                for (int i = 0; i < 2; i++) Dust.NewDust(player.position + new Vector2(Main.rand.NextFloat(-240f, 240f), Main.rand.NextFloat(-330f, -220f)), 8, 8, DustID.BoneTorch, 0f, 0.8f, 80, default, 1.15f);
            }
            if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer >= cast && AttackTimer <= cast + 54 && (AttackTimer - cast) % 9 == 0)
            {
                Vector2 pos = player.Center + new Vector2(Main.rand.NextFloat(-280f, 280f), -360f);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), pos, new Vector2(Main.rand.NextFloat(-0.8f, 0.8f), 9f), ModContent.ProjectileType<NitoCeilingSpike>(), BoneDamage, 1f, Main.myPlayer);
            }
            if (AttackTimer >= cast + 98) EndAttack(220);
        }

        void RunPhaseTransition(tsorcRevampGlobalNPC g)
        {
            NPC.velocity.X *= 0.7f;
            g.AttackCommitted = AttackTimer <= 80;
            if (AttackTimer == 1)
            {
                SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.7f, Pitch = -0.45f }, NPC.Center);
                UsefulFunctions.ScreenShake(NPC.Center, 6f, 18);
            }
            if (AttackTimer % 3 == 0)
            {
                Dust d = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(110f, 130f), DustID.Shadowflame, Main.rand.NextVector2Circular(2f, 2f), 80, default, 1.3f);
                d.noGravity = true;
            }
            if (AttackTimer == 80 && Main.netMode != NetmodeID.MultiplayerClient) Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<NitoDeathNova>(), DeathDamage, 5f, Main.myPlayer, 180f);
            if (AttackTimer >= 125) EndAttack(120);
        }

        void RunComboRecovery()
        {
            NPC.velocity.X *= 0.75f;
            ComboRecoveryTicks--;
            if (Main.rand.NextBool(4)) Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, 0f, 1f, 100, default, 1f);
            if (ComboRecoveryTicks <= 0) EndAttack(210);
        }

        int Telegraph(int baseTicks) => HalfTelegraph ? Math.Max(12, baseTicks / 2) : baseTicks;

        void StartAttack(AttackState state, Player player)
        {
            State = state;
            AttackTimer = 0;
            lockedDir = player.Center.X >= NPC.Center.X ? 1 : -1;
            NPC.direction = lockedDir;
            NPC.spriteDirection = lockedDir;
            // Vertical read, locked once at attack-start (mirrors lockedDir) so the windup pose,
            // telegraph dust, and the eventual SpawnSlash all agree on the same kind.
            lockedKind = state switch
            {
                AttackState.SideSweep => ChooseVerticalKind(0, player),
                AttackState.BackhandSweep => ChooseVerticalKind(3, player),
                AttackState.OverheadCleave => ChooseVerticalKind(1, player),
                AttackState.ImpalingThrust => ChooseVerticalKind(2, player),
                _ => 0,
            };
            // Fresh state: no windup armed, no slash released yet — the loose sword rests at idle.
            SlashWindupActive = false;
            SlashActiveKind = -1;
            NPC.netUpdate = true;
        }

        void EndAttack(int cooldown)
        {
            LastAttack = State;
            State = AttackState.None;
            AttackTimer = 0;
            HalfTelegraph = false;
            SlashWindupActive = false;
            AttackCooldown = cooldown + Main.rand.Next(40);
            NPC.damage = 0;
            NPC.netUpdate = true;
        }

        ///<summary>Nito is a heavy, grounded fighter — he should never leap up onto ledges/platforms
        ///that sit above the player. If FighterAI just started a nav hop (upward velocity) and the
        ///player is NOT clearly above him, kill the upward impulse. AutoStepUp still carries him over
        ///1–2 tile ledges without a jump, so this only suppresses genuine climbing.</summary>
        void StayGroundedRelativeTo(Player player)
        {
            // Don't suppress a lava/liquid escape hop — only genuine climbing toward higher ground.
            if (NPC.lavaWet || NPC.wet)
            {
                return;
            }
            bool playerAbove = player.Center.Y < NPC.Center.Y - 64f;
            if (!playerAbove && NPC.velocity.Y < -1f)
            {
                NPC.velocity.Y = 0f;
            }
        }

        void FacePlayer(Player player)
        {
            if (player.Center.X != NPC.Center.X)
            {
                lockedDir = player.Center.X > NPC.Center.X ? 1 : -1;
                NPC.direction = lockedDir;
                NPC.spriteDirection = lockedDir;
            }
        }

        void SpawnSlash(int kind, int damage)
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = kind == 1 ? -0.25f : 0.05f }, NPC.Center);
            // The loose sword layer now plays the visible swing itself (see PreDraw's active-window);
            // the projectile is just a matching invisible hitbox. Record the release so both agree.
            SlashWindupActive = false;
            SlashActiveKind = kind;
            SlashActiveStartTick = AttackTimer;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(lockedDir, 0f), ModContent.ProjectileType<NitoSwordSlash>(), damage, 5f, Main.myPlayer, NPC.whoAmI, kind);
        }

        ///<summary>Arms the loose sword's windup: it lerps from the idle pose toward this kind's
        ///swing-start pose over [windupStartTick, releaseTick]. Call once per slash, at (or just
        ///before) the tick the windup should visibly begin.</summary>
        void ArmSlashWindup(int kind, int windupStartTick, int releaseTick)
        {
            SlashWindupKind = kind;
            SlashWindupStartTick = windupStartTick;
            SlashWindupEndTick = Math.Max(windupStartTick + 1, releaseTick);
            SlashWindupActive = true;
        }

        // ── Shared swing geometry (used by BOTH the boss draw and the NitoSwordSlash hitbox) ─────
        // A swing is described in FORWARD-RELATIVE angle phi: phi = 0 points the blade straight at the
        // player, negative tilts it up/overhead, +/-pi points it straight back. Each kind is one arc
        // (progress 0->1); overhead (1) and rising (4) are the same shape reversed, likewise side (0)
        // and backhand (3). Arcs are deliberately wide (~150-180 deg) so the sweep reads big.
        public static float SlashPhi(int kind, float progress) => kind switch
        {
            1 => MathHelper.Lerp(-2.0f, 0.7f, progress),   // overhead cleave: cocked up-back -> down-forward
            2 => 0f,                                        // thrust: held forward, reach extends instead
            3 => MathHelper.Lerp(0.6f, -2.4f, progress),    // backhand: forward -> up-back (reverse of side sweep)
            4 => MathHelper.Lerp(0.7f, -2.0f, progress),    // rising cut: down-forward -> up-back (reverse of overhead)
            _ => MathHelper.Lerp(-2.4f, 0.6f, progress),    // side sweep: up-back over the top -> down-forward
        };

        public static float SlashReach(int kind, float progress) => kind == 2 ? 60f + progress * 120f : 82f;

        ///<summary>World-space angle the blade points at, for a given facing (dir) and swing progress.</summary>
        public static float SlashWorldAngle(int kind, int dir, float progress)
        {
            float phi = SlashPhi(kind, progress);
            return dir >= 0 ? phi : MathHelper.Pi - phi;
        }

        ///<summary>Offset of the blade's CENTER from the wielder's Center for a given facing/progress.</summary>
        public static Vector2 SlashOffset(int kind, int dir, float progress)
        {
            float theta = SlashWorldAngle(kind, dir, progress);
            return new Vector2(dir * SwordPivotX, SwordPivotY) + theta.ToRotationVector2() * SlashReach(kind, progress);
        }

        ///<summary>Reads the player's vertical offset at attack-start and swaps in a better-fitting
        ///slash: a rising cut (kind 4) when the player is well above Nito, the downward/overhead
        ///cleave (kind 1) when they're clearly below — otherwise keeps the attack's own signature
        ///kind, since that already reads fine at roughly the same height ("in front").</summary>
        int ChooseVerticalKind(int defaultKind, Player player)
        {
            float verticalOffset = player.Center.Y - NPC.Center.Y; // negative = player above Nito
            if (verticalOffset < -140f)
            {
                return 4;
            }
            if (verticalOffset > 90f && defaultKind != 1)
            {
                return 1;
            }
            return defaultKind;
        }

        void SpawnGroundSpike(Vector2 roughBottom, int delay, float heightScale)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            // Only erupt from a real floor surface (solid tile with open space above). If there is no
            // such surface below the target — the player is airborne over a pit, in a tight spot, etc.
            // — skip the spike entirely rather than spawning it buried inside solid rock where it would
            // read as a stuck sliver. Platforms are passed through (they aren't treated as the floor).
            if (!FindGroundSurface(roughBottom, out Vector2 bottom)) return;
            Projectile.NewProjectile(NPC.GetSource_FromThis(), bottom - new Vector2(0f, 54f * heightScale), Vector2.Zero, ModContent.ProjectileType<NitoGraveSpike>(), DeathDamage, 3f, Main.myPlayer, delay, heightScale);
        }

        ///<summary>Scans downward from just above <paramref name="origin"/> for the first SOLID tile
        ///that has open space (air or a platform) directly above it — a true floor the spike can burst
        ///up through. Platforms themselves are skipped (passed through), so spikes track the real
        ///ground beneath them. Returns false when no such surface is found in range.</summary>
        static bool FindGroundSurface(Vector2 origin, out Vector2 surface)
        {
            int tileX = Math.Clamp((int)(origin.X / 16f), 5, Main.maxTilesX - 5);
            int startY = Math.Clamp((int)(origin.Y / 16f) - 3, 5, Main.maxTilesY - 6);
            for (int y = startY; y <= startY + 30 && y < Main.maxTilesY - 5; y++)
            {
                Tile tile = Main.tile[tileX, y];
                bool solid = tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
                if (!solid)
                {
                    continue;
                }
                Tile above = Main.tile[tileX, y - 1];
                bool openAbove = !above.HasTile || above.IsActuated || !Main.tileSolid[above.TileType] || Main.tileSolidTop[above.TileType];
                if (openAbove)
                {
                    surface = new Vector2(origin.X, y * 16f);
                    return true;
                }
            }
            surface = origin;
            return false;
        }

        void TelegraphCue(Color color)
        {
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.75f, Pitch = -0.35f }, NPC.Center);
            if (Main.netMode != NetmodeID.Server)
            {
                tsorcRevampAIs.SpawnTelegraphFlash(NPC, color, NPC.Center + new Vector2(0f, -85f));
            }
        }

        void SwordTelegraphDust(int kind)
        {
            // Rising slash gathers low near the hip (where the cut starts) rather than at shoulder height.
            Vector2 origin = NPC.Center + (kind == 4 ? new Vector2(lockedDir * 30f, -30f) : new Vector2(lockedDir * 40f, -92f));
            float arc = kind == 1 ? -MathHelper.PiOver2 : kind == 4 ? lockedDir * 0.9f : lockedDir > 0 ? -0.1f : MathHelper.Pi + 0.1f;
            for (int i = 0; i < 2; i++)
            {
                Vector2 pos = origin + (arc + Main.rand.NextFloat(-0.8f, 0.8f)).ToRotationVector2() * Main.rand.NextFloat(50f, 145f);
                Dust d = Dust.NewDustPerfect(pos, DustID.BoneTorch, Main.rand.NextVector2Circular(1f, 1f), 100, default, 0.95f);
                d.noGravity = true;
            }
        }

        void DragDust()
        {
            for (int i = 0; i < 3; i++)
            {
                Dust d = Dust.NewDustPerfect(NPC.Bottom + new Vector2(lockedDir * Main.rand.NextFloat(30f, 120f), -Main.rand.NextFloat(8f, 28f)), DustID.BoneTorch, new Vector2(-lockedDir * 1.2f, Main.rand.NextFloat(-2.4f, -0.6f)), 90, default, 1f);
                d.noGravity = true;
            }
        }

        void GraveDust(Vector2 center)
        {
            for (int i = 0; i < 3; i++)
            {
                Dust d = Dust.NewDustPerfect(center + new Vector2(Main.rand.NextFloat(-120f, 120f), -Main.rand.NextFloat(4f, 18f)), DustID.BoneTorch, new Vector2(0f, Main.rand.NextFloat(-1.5f, -0.2f)), 110, default, 0.9f);
                d.noGravity = true;
            }
        }

        void FootstepEffects()
        {
            if (Math.Abs(NPC.velocity.X) < 0.35f || NPC.velocity.Y != 0f)
            {
                FootstepTimer = 0;
                return;
            }
            FootstepTimer++;
            if (FootstepTimer >= 28)
            {
                FootstepTimer = 0;
                SoundEngine.PlaySound(SoundID.DeerclopsStep with { Volume = 0.45f, Pitch = -0.25f }, NPC.Bottom);
                UsefulFunctions.ScreenShake(NPC.Bottom, 1.3f, 7, 6f, 350f);
                for (int i = 0; i < 7; i++) Dust.NewDust(NPC.Bottom - new Vector2(NPC.width / 2f, 12f), NPC.width, 12, DustID.Smoke, 0f, -1f, 120, default, 0.9f);
            }
        }

        void UpdateAura()
        {
            float pulse = State == AttackState.None ? 0f : (float)Math.Sin(Main.GlobalTimeWrappedHourly * 9f) * 0.08f;
            Lighting.AddLight(NPC.Center, 0.2f + pulse, 0.2f + pulse, 0.28f + pulse);
            if (Main.rand.NextBool(State == AttackState.None ? 5 : 3))
            {
                Dust d = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(55f, 105f), PhaseTwo ? DustID.Shadowflame : DustID.BoneTorch, new Vector2(0f, Main.rand.NextFloat(-1.4f, -0.3f)), 120, default, 0.75f);
                d.noGravity = true;
            }
        }

        public override void FindFrame(int frameHeight)
        {
            bool moving = Math.Abs(NPC.velocity.X) > 0.25f || State != AttackState.None;
            if (!moving)
            {
                NPC.frame.Y = 0;
                NPC.frameCounter = 0;
                return;
            }
            NPC.frameCounter++;
            if (NPC.frameCounter >= 5)
            {
                NPC.frameCounter = 0;
                int frame = NPC.frame.Y / FrameHeight;
                NPC.frame.Y = ((frame + 1) % FrameCount) * FrameHeight;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Body is ALWAYS the no-sword sheet (TextureAssets.Npc[Type] = GravelordNitoAttacking.png,
            // which has no blade painted in). The single loose sword layer below is the ONLY sword —
            // this is what removes the old "two swords" (a baked-in blade + the loose one).
            Texture2D body = TextureAssets.Npc[Type].Value;
            Texture2D sword = ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Bosses/GravelordNito/GravelordNitoSword").Value;
            Rectangle frame = NPC.frame.Height > 0 ? NPC.frame : new Rectangle(0, 0, BodyWidth, FrameHeight);
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            // Sink the whole rig ~2 tiles into the floor so his ragged base is always buried and the
            // sprite reads as standing ON the ground, not floating above it.
            Vector2 drawBottom = NPC.Bottom + new Vector2(0f, 7f + NPC.gfxOffY + GroundSinkPixels);
            Vector2 swordAnchor = NPC.Center + new Vector2(0f, NPC.gfxOffY + GroundSinkPixels);

            // Resolve the loose sword's forward-relative pose (phi) + reach for this frame.
            int dir = NPC.spriteDirection;
            const float idlePhi = -MathHelper.Pi; // held straight back = a horizontal resting pose
            float phi = idlePhi;
            float reach = SwordIdleReach;
            if (State != AttackState.None)
            {
                int activeEnd = SlashActiveStartTick + SlashActiveTicks;
                if (SlashActiveKind >= 0 && AttackTimer >= SlashActiveStartTick && AttackTimer <= activeEnd)
                {
                    // The visible swing itself (mirrors the invisible NitoSwordSlash hitbox arc).
                    float p = MathHelper.Clamp((AttackTimer - SlashActiveStartTick) / (float)SlashActiveTicks, 0f, 1f);
                    phi = SlashPhi(SlashActiveKind, p);
                    reach = SlashReach(SlashActiveKind, p);
                }
                else if (SlashActiveKind >= 0 && !SlashWindupActive && AttackTimer > activeEnd && AttackTimer <= activeEnd + SlashReturnTicks)
                {
                    // Ease back to idle after the swing (unless a combo's next windup already armed).
                    float p = MathHelper.Clamp((AttackTimer - activeEnd) / (float)SlashReturnTicks, 0f, 1f);
                    phi = MathHelper.Lerp(SlashPhi(SlashActiveKind, 1f), idlePhi, p);
                    reach = MathHelper.Lerp(SlashReach(SlashActiveKind, 1f), SwordIdleReach, p);
                }
                else if (SlashWindupActive)
                {
                    // Wind up from idle toward the swing's start pose.
                    float w = SlashWindupEndTick > SlashWindupStartTick
                        ? MathHelper.Clamp((AttackTimer - SlashWindupStartTick) / (float)(SlashWindupEndTick - SlashWindupStartTick), 0f, 1f)
                        : 1f;
                    phi = MathHelper.Lerp(idlePhi, SlashPhi(SlashWindupKind, 0f), w);
                    reach = MathHelper.Lerp(SwordIdleReach, SlashReach(SlashWindupKind, 0f), w);
                }
                // else: a non-sword cast (bones/nova/etc.) — the blade simply rests at idle.
            }

            // Convert (phi, reach) to a world offset + a rotation/flip that keeps the blade's edge
            // consistent across the whole arc (flip is fixed by facing, so there is no mid-swing pop).
            float theta = dir >= 0 ? phi : MathHelper.Pi - phi;
            Vector2 swordOffset = new Vector2(dir * SwordPivotX, SwordPivotY) + theta.ToRotationVector2() * reach;
            bool flip = dir >= 0;
            float swordRotation = flip ? theta : theta - MathHelper.Pi;
            SpriteEffects swordEffects = flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            // Sword drawn BEFORE the body so it sits BEHIND Nito's silhouette.
            spriteBatch.Draw(sword, swordAnchor + swordOffset - screenPos, null, drawColor, swordRotation, new Vector2(sword.Width / 2f, sword.Height / 2f), NPC.scale, swordEffects, 0f);
            spriteBatch.Draw(body, drawBottom - screenPos, frame, drawColor, NPC.rotation, new Vector2(BodyDrawCenterX, FrameHeight), NPC.scale, effects, 0f);
            return false;
        }

        ///<summary>Nito stands in Skeletron's progression slot (Skeletron itself remains fightable as
        ///an optional boss). Defeating him must unlock everything the dungeon/post-Skeletron content
        ///gates on: the vanilla dungeon-guardian flag, AND the mod's own NewSlain[SkeletronHead] gate
        ///(read by Basilisk Walker/BarrowWight/spore trap/ink spit/Jungle Wyvern fire/Sublime Bone
        ///Dust — see NewSlain.ContainsKey(NPCDefinition(NPCID.SkeletronHead)) call sites).</summary>
        public override void OnKill()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            NPC.downedBoss3 = true;

            RegisterFirstKill(NPC.type);
            RegisterFirstKill(NPCID.SkeletronHead);

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.WorldData);
            }
        }

        static void RegisterFirstKill(int npcType)
        {
            Terraria.ModLoader.Config.NPCDefinition definition = new(npcType);
            if (!tsorcRevampWorld.NewSlain.ContainsKey(definition))
            {
                tsorcRevampWorld.NewSlain.Add(definition, 1);
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 8; i++) Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.BoneTorch, hit.HitDirection, -1f, 120, default, 0.9f);
            if (NPC.life <= 0)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 1f, Pitch = -0.25f }, NPC.Center);
                UsefulFunctions.ScreenShake(NPC.Center, 8f, 20);
                for (int i = 0; i < 70; i++)
                {
                    Dust d = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(80f, 120f), Main.rand.NextBool() ? DustID.BoneTorch : DustID.Shadowflame, Main.rand.NextVector2Circular(5f, 5f), 80, default, Main.rand.NextFloat(1f, 1.6f));
                    d.noGravity = true;
                }
            }
        }
    }
}
