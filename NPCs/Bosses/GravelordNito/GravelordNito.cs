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
        const int HeavyTelegraph = 48;
        const int LongChannelTicks = 120;
        const int LongChannelStaggerTicks = 80;

        AttackState State = AttackState.None;
        AttackState LastAttack = AttackState.None;
        int AttackTimer;
        int AttackCooldown = 150;
        int lockedDir = 1;
        int FootstepTimer;
        int ComboRecoveryTicks;
        bool PhaseTwo;
        bool HalfTelegraph;

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
            g.MaxJumpPower = 9.5f;
            g.MaxJumpBoost = 5f;
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
                tsorcRevampAIs.FighterAI(NPC, topSpeed: 0.62f, acceleration: 0.45f, canTeleport: false, lavaJumping: true, canDodgeroll: false, canPounce: false, minSurfaceWidth: 4, canWalkBackwards: true);
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
                case AttackState.SideSweep: RunSwordAttack(g, player, 0, Telegraph(30), 44, 84, SlashDamage); break;
                case AttackState.BackhandSweep: RunSwordAttack(g, player, 3, Telegraph(26), 38, 78, SlashDamage); break;
                case AttackState.OverheadCleave: RunSwordAttack(g, player, 1, Telegraph(HeavyTelegraph), 64, 116, HeavySlashDamage); break;
                case AttackState.ImpalingThrust: RunSwordAttack(g, player, 2, Telegraph(34), 44, 82, SlashDamage); break;
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
            if (AttackTimer == 1) TelegraphCue(new Color(180, 180, 210));
            if (AttackTimer < 25)
            {
                FacePlayer(player);
                SwordTelegraphDust(0);
            }
            if (AttackTimer == 30) SpawnSlash(0, SlashDamage);
            if (AttackTimer == 56) SpawnSlash(3, SlashDamage);
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
            if (AttackTimer == 1) TelegraphCue(new Color(170, 170, 190));
            if (AttackTimer < windup)
            {
                NPC.velocity.X *= 0.82f;
                SwordTelegraphDust(0);
            }
            else if (AttackTimer <= 86)
            {
                NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, lockedDir * 4.2f, 0.12f);
                if (AttackTimer % 16 == 0) SpawnSlash(0, SlashDamage);
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
            if (AttackTimer == 1) TelegraphCue(Color.White);
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
                Projectile.NewProjectile(NPC.GetSource_FromThis(), pos, velocity, ModContent.ProjectileType<NitoBoneShard>(), BoneDamage, 1f, Main.myPlayer);
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
            if (AttackTimer == 1) TelegraphCue(new Color(120, 95, 160));
            if (AttackTimer <= 24) SwordTelegraphDust(2);
            if (AttackTimer == 28) SpawnSlash(2, SlashDamage);
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
                Projectile.NewProjectile(NPC.GetSource_FromThis(), pos, new Vector2(Main.rand.NextFloat(-0.8f, 0.8f), 9f), ModContent.ProjectileType<NitoBoneShard>(), BoneDamage, 1f, Main.myPlayer);
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
            NPC.netUpdate = true;
        }

        void EndAttack(int cooldown)
        {
            LastAttack = State;
            State = AttackState.None;
            AttackTimer = 0;
            HalfTelegraph = false;
            AttackCooldown = cooldown + Main.rand.Next(40);
            NPC.damage = 0;
            NPC.netUpdate = true;
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
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(lockedDir, 0f), ModContent.ProjectileType<NitoSwordSlash>(), damage, 5f, Main.myPlayer, NPC.whoAmI, kind);
        }

        void SpawnGroundSpike(Vector2 roughBottom, int delay, float heightScale)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            Vector2 bottom = FindGround(roughBottom);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), bottom - new Vector2(0f, 54f * heightScale), Vector2.Zero, ModContent.ProjectileType<NitoGraveSpike>(), DeathDamage, 3f, Main.myPlayer, delay, heightScale);
        }

        Vector2 FindGround(Vector2 origin)
        {
            int tileX = (int)(origin.X / 16f);
            int tileY = (int)(origin.Y / 16f);
            tileX = Math.Clamp(tileX, 5, Main.maxTilesX - 5);
            tileY = Math.Clamp(tileY, 5, Main.maxTilesY - 10);
            for (int i = -5; i <= 22; i++)
            {
                int y = Math.Clamp(tileY + i, 5, Main.maxTilesY - 10);
                Tile tile = Main.tile[tileX, y];
                if (tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType])
                {
                    return new Vector2(origin.X, y * 16f);
                }
            }
            return origin;
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
            Vector2 origin = NPC.Center + new Vector2(lockedDir * 40f, -92f);
            float arc = kind == 1 ? -MathHelper.PiOver2 : lockedDir > 0 ? -0.1f : MathHelper.Pi + 0.1f;
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
            Texture2D body = TextureAssets.Npc[Type].Value;
            Texture2D sword = ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Bosses/GravelordNito/GravelordNitoSword").Value;
            Rectangle frame = NPC.frame.Height > 0 ? NPC.frame : new Rectangle(0, 0, BodyWidth, FrameHeight);
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 drawBottom = NPC.Bottom + new Vector2(0f, 7f + NPC.gfxOffY);

            if (!IsMeleeState || AttackTimer < 12)
            {
                Vector2 swordOffset = new Vector2(NPC.spriteDirection * 26f, -132f);
                SpriteEffects swordEffects = NPC.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                spriteBatch.Draw(sword, drawBottom + swordOffset - screenPos, null, drawColor, NPC.spriteDirection * 0.11f, new Vector2(sword.Width / 2f, sword.Height / 2f), NPC.scale, swordEffects, 0f);
            }

            spriteBatch.Draw(body, drawBottom - screenPos, frame, drawColor, NPC.rotation, new Vector2(BodyWidth / 2f, FrameHeight), NPC.scale, effects, 0f);
            return false;
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
