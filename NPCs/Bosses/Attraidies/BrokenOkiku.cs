using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.BossBags;
using tsorcRevamp.Content.Items.BossItems;
using tsorcRevamp.Content.Projectiles.Enemy.Okiku;

namespace tsorcRevamp.NPCs.Bosses.Attraidies
{
    /// <summary>
    /// Fourth form: a faster version of the third, always knowing its next teleport. The server chooses phases and the
    /// pre-rolled destination; clients show that destination during the last 30 ticks before the jump.
    /// </summary>
    [AutoloadBossHead]
    public class BrokenOkiku : AttraidiesPuppet
    {
        private const int OpeningPhase = -1;
        private const int TeleportTellTicks = 30;

        // The form fades toward this alpha, as the old sprite did.
        private const int GhostAlpha = 200;

        // Teleport targets were authored for the old 58x121 body's corner, so they are converted to that body's center.
        private static readonly Vector2 OldHalfSize = new Vector2(29f, 60.5f);

        private static readonly Color TeleportColor = new Color(230, 90, 170);

        private NPCDespawnHandler despawnHandler;
        private bool initialized;

        // Synced fight state.
        private int attackPhase = OpeningPhase;
        private int subPhase;
        private int genericTimer;
        private int genericTimer2;
        private int phaseTime = 120;
        private bool phaseStarted = true;
        private Vector2 nextTeleport;

        private int NextPhase
        {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }

        protected override NPCDespawnHandler DespawnHandler => despawnHandler;
        protected override bool PlaysArrivalBurst => true;
        protected override Color TeleportFlashColor => TeleportColor;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();

            NPCID.Sets.NPCBestiaryDrawModifiers hiddenFromBestiary = new NPCID.Sets.NPCBestiaryDrawModifiers();
            hiddenFromBestiary.Hide = true;
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, hiddenFromBestiary);

            // Set NeedsExpertScaling for lifeMax scaling in multiplayer, otherwise scaling is skipped since NPC.damage=0.
            NPCID.Sets.NeedsExpertScaling[Type] = true;
        }

        public override void SetDefaults()
        {
            ApplyBodyDefaults();
            NPC.npcSlots = 10;
            NPC.defense = 36;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lifeMax = (int)(15400 * (Main.masterMode ? 1.5f : 1));
            NPC.timeLeft = 22500;
            NPC.friendly = false;
            NPC.boss = true;
            NPC.value = 350000;
            despawnHandler = new NPCDespawnHandler(DustID.PurpleCrystalShard);
        }

        protected override void TickForm()
        {
            bool authority = IsAuthority;

            if (!initialized)
            {
                initialized = true;
                if (authority)
                {
                    NextPhase = 1;
                    RollOverheadTeleport();
                    RequestNetworkSnapshot();
                }
            }

            int passiveDust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Torch, NPC.velocity.X, NPC.velocity.Y, 100, Color.Red, 1f);
            Main.dust[passiveDust].noGravity = true;

            if (NPC.alpha < GhostAlpha)
            {
                NPC.alpha++;
            }

            genericTimer++;
            genericTimer2++;

            if (authority && !phaseStarted)
            {
                StartAttackPhase();
            }

            RunAttackPhase(authority);

            if (genericTimer >= phaseTime - TeleportTellTicks)
            {
                GatherDust(NPC.Center, TeleportColor, 30f);
                GatherDust(nextTeleport, TeleportColor, 30f);
                GatherDust(nextTeleport, TeleportColor, 16f);
            }

            if (authority && genericTimer >= phaseTime)
            {
                attackPhase = NextPhase;
                NextPhase = Main.rand.Next(4);
                NPC.velocity = Vector2.Zero;
                NPC.Center = nextTeleport;
                RollNextTeleport();
                genericTimer = 0;
                genericTimer2 = 0;
                phaseStarted = false;
                RequestNetworkSnapshot();
            }
        }

        private void RollOverheadTeleport()
        {
            Vector2 corner = TargetPlayer.Center + Main.rand.NextVector2Circular(50, 50) - new Vector2(0, 300);
            nextTeleport = corner + OldHalfSize;
        }

        private void RollNextTeleport()
        {
            Vector2 corner;
            if (NextPhase == 0)
            {
                corner = TargetPlayer.Center + new Vector2(500, 400);
                if (Main.rand.NextBool())
                {
                    corner.X -= 1000;
                }

                nextTeleport = corner + OldHalfSize;
            }
            else if (NextPhase == 1)
            {
                RollOverheadTeleport();
            }
            else
            {
                corner = TargetPlayer.Center + Main.rand.NextVector2CircularEdge(600, 600);
                nextTeleport = corner + OldHalfSize;
            }
        }

        // Server only: the launch and timing that open a phase. The teleport itself happened as the phase was chosen.
        private void StartAttackPhase()
        {
            phaseStarted = true;

            switch (attackPhase)
            {
                case 0:
                    phaseTime = 60;
                    break;

                case 1:
                    phaseTime = 80;
                    subPhase = Main.rand.Next(2);
                    int pulseType = ModContent.ProjectileType<EnergyPulse>();
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        if (Main.projectile[i].active && Main.projectile[i].type == pulseType)
                        {
                            subPhase = 0;
                            break;
                        }
                    }
                    break;

                case 2:
                    phaseTime = 60;
                    NPC.velocity = UsefulFunctions.Aim(NPC.Center, TargetPlayer.Center, 16);
                    break;

                case 3:
                    phaseTime = 180;
                    break;
            }

            RequestNetworkSnapshot();
        }

        private void RunAttackPhase(bool authority)
        {
            Player target = TargetPlayer;

            switch (attackPhase)
            {
                case 0:
                {
                    int sideDirection = 1;
                    if (NPC.position.X > target.position.X)
                    {
                        sideDirection = -1;
                    }

                    NPC.velocity.Y = -15;
                    SetAimPose(NPC.Center + new Vector2(sideDirection * 600f, 0f));

                    if (genericTimer2 == 10)
                    {
                        genericTimer2 = 0;
                        if (authority)
                        {
                            Vector2 velocity = new Vector2(sideDirection * 6 + Main.rand.Next(-1, 1), Main.rand.Next(-10, 10) / 5);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), StaffTip, velocity, ModContent.ProjectileType<CrazedOrb>(), 62, 0f, Main.myPlayer);
                        }
                    }
                    break;
                }

                case 1:
                    SetPose(AttraidiesPose.Overhead);
                    if (genericTimer2 >= 40)
                    {
                        bool isRing = subPhase == 0;
                        genericTimer2 = -200;
                        if (isRing)
                        {
                            genericTimer2 = 0;
                        }

                        if (authority)
                        {
                            int randomRotation = Main.rand.Next(-20, 20) / 2;
                            if (isRing)
                            {
                                for (int i = 0; i < 9; i++)
                                {
                                    float angle = randomRotation + ((360 / 13) * (1 + i)) * 3;
                                    Vector2 velocity = new Vector2((float)Math.Sin(angle), (float)Math.Cos(angle));
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, velocity, ModContent.ProjectileType<EnergyPulse>(), 66, 0f, Main.myPlayer);
                                }
                            }
                            else
                            {
                                for (int i = 0; i < 6; i++)
                                {
                                    float angle = randomRotation + ((360 / 10) * (1 + i));
                                    Vector2 velocity = new Vector2((float)Math.Sin(angle) * 6, (float)Math.Cos(angle) * 6);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, velocity, ModContent.ProjectileType<EnergyPulse>(), 58, 0f, Main.myPlayer);
                                }
                            }
                        }
                    }
                    break;

                case 2:
                    SetAimPose(target.Center);
                    if (genericTimer2 >= 10)
                    {
                        genericTimer2 = 0;
                        if (authority)
                        {
                            Vector2 velocity = UsefulFunctions.Aim(NPC.Center, target.Center, 0.5f);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), StaffTip, velocity, ModContent.ProjectileType<PhasedMatterBlast>(), 65, 0f, Main.myPlayer);
                        }
                    }
                    break;

                case 3:
                    SetPose(AttraidiesPose.Conduct);
                    NPC.velocity = UsefulFunctions.Aim(NPC.Center, target.Center, 5);

                    if (genericTimer2 >= 8)
                    {
                        genericTimer2 = 0;
                        if (authority)
                        {
                            float rotation = (target.Center - NPC.Center).ToRotation();
                            float speed = 0.5f + Main.rand.Next(-3, 3) / 10;
                            Vector2 position = NPC.Center + new Vector2(Main.rand.Next(-100, 100), Main.rand.Next(-100, 100));
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), position, rotation.ToRotationVector2() * speed, ModContent.ProjectileType<PoisonSmog>(), 34, 0f, Main.myPlayer);
                        }
                    }
                    break;

                default:
                    SetPose(AttraidiesPose.Carry);
                    break;
            }
        }

        // The form leaves a streak of motes along its jump instead of a burst at each end.
        protected override void PlayTeleportFlash(Vector2 from, Vector2 to)
        {
            if (Main.dedServ)
            {
                return;
            }

            Vector2 difference = to - from;
            float length = difference.Length();
            Vector2 direction = difference.SafeNormalize(Vector2.UnitX);

            for (int i = 0; i < length; i++)
            {
                if (!Main.rand.NextBool())
                {
                    continue;
                }

                Vector2 dustPoint = from + direction * i;
                dustPoint.X += Main.rand.NextFloat(-NPC.width / 2, NPC.width / 2);
                dustPoint.Y += Main.rand.NextFloat(-NPC.height / 2, NPC.height / 2);

                int dustType = DustID.FireworkFountain_Pink;
                if (Main.rand.NextBool())
                {
                    dustType = 71;
                }

                Dust.NewDustPerfect(dustPoint, dustType, direction * 5, 200, default, 0.8f).noGravity = true;
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            base.HitEffect(hit);

            if (NPC.life > 0 || Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 200; i++)
            {
                int dustType = 54;
                if (i % 2 == 1)
                {
                    dustType = 62;
                }

                int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, dustType, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, default, 4f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<MindflayerIllusionBag>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<MindflayerIllusionRelic>()));
            npcLoot.Add(notExpertCondition);
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((sbyte)attackPhase);
            writer.Write((sbyte)subPhase);
            writer.Write((short)genericTimer);
            writer.Write((short)genericTimer2);
            writer.Write((short)phaseTime);
            writer.Write(phaseStarted);
            writer.WriteVector2(nextTeleport);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            attackPhase = reader.ReadSByte();
            subPhase = reader.ReadSByte();
            genericTimer = reader.ReadInt16();
            genericTimer2 = reader.ReadInt16();
            phaseTime = reader.ReadInt16();
            phaseStarted = reader.ReadBoolean();
            nextTeleport = reader.ReadVector2();
        }
    }
}
