using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy.Okiku;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.Attraidies
{
    /// <summary>
    /// Third form: teleports into one of four random attack phases. The server picks phases and teleports; clients follow
    /// the synced phase. At half health it passes its remaining life to the fourth form.
    /// </summary>
    [AutoloadBossHead]
    public class Okiku : AttraidiesPuppet
    {
        private const int OpeningPhase = -1;
        private const int TeleportTellTicks = 30;

        // Teleport targets were authored for the old 58x120 body's corner, so they are converted to that body's center.
        private const float OldHalfWidth = 29f;
        private const float OldHalfHeight = 60f;

        private static readonly Color TeleportColor = new Color(170, 80, 255);

        private NPCDespawnHandler despawnHandler;
        private bool initialized;

        // Synced fight state.
        private int attackPhase = OpeningPhase;
        private int nextAttackPhase;
        private int subPhase;
        private int nextSubPhase;
        private int genericTimer;
        private int genericTimer2;
        private int phaseTime = 120;
        private bool phaseStarted;
        private bool teleportLeft;
        private float nextWarpAngle;

        protected override NPCDespawnHandler DespawnHandler => despawnHandler;
        protected override bool PlaysArrivalBurst => true;
        protected override Color TeleportFlashColor => TeleportColor;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Ichor] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Venom] = true;

            // Set NeedsExpertScaling for lifeMax scaling in multiplayer, otherwise scaling is skipped since NPC.damage=0.
            NPCID.Sets.NeedsExpertScaling[Type] = true;
        }

        public override void SetDefaults()
        {
            ApplyBodyDefaults();
            NPC.defense = 36;
            NPC.boss = true;
            NPC.lifeMax = (int)(22400 * (Main.masterMode ? 1.5f : 1));
            NPC.npcSlots = 2f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = Item.buyPrice(0, 26);
            Music = 39;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.Okiku.DespawnHandler"), Color.DarkMagenta, DustID.PurpleCrystalShard);
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            base.ModifyHitByProjectile(projectile, ref modifiers);
            modifiers.DisableCrit();
            modifiers.SetMaxDamage(NPC.life - 50);
        }

        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            base.ModifyHitByItem(player, item, ref modifiers);
            modifiers.DisableCrit();
            modifiers.SetMaxDamage(NPC.life - 50);
        }

        private Vector2 FromOldCorner(float cornerX, float cornerY)
        {
            return new Vector2(cornerX + OldHalfWidth, cornerY + OldHalfHeight);
        }

        private Vector2 CircleTeleportPoint
        {
            get
            {
                float cornerX = TargetPlayer.position.X + (float)(600 * Math.Cos(nextWarpAngle) * -1);
                float cornerY = TargetPlayer.position.Y + (float)(600 * Math.Sin(nextWarpAngle) * -1);
                return FromOldCorner(cornerX, cornerY);
            }
        }

        private Vector2 SideTeleportPoint
        {
            get
            {
                float sideOffset = 500f;
                if (teleportLeft)
                {
                    sideOffset = -500f;
                }

                return FromOldCorner(TargetPlayer.position.X + sideOffset, TargetPlayer.position.Y + 400);
            }
        }

        private Vector2 OverheadTeleportPoint => FromOldCorner(TargetPlayer.position.X, TargetPlayer.position.Y - 300);

        private Vector2 TeleportPointFor(int phase)
        {
            if (phase == 0)
            {
                return SideTeleportPoint;
            }

            if (phase == 1)
            {
                return OverheadTeleportPoint;
            }

            return CircleTeleportPoint;
        }

        protected override void TickForm()
        {
            bool authority = IsAuthority;

            if (!initialized)
            {
                initialized = true;
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Okiku.Quote"), new Color(150, 150, 150));
            }

            DispelGravitation();

            int passiveDust = Dust.NewDust(NPC.position, NPC.width, NPC.height, 54, NPC.velocity.X, NPC.velocity.Y, 100, Color.White, 1f);
            Main.dust[passiveDust].noGravity = true;

            genericTimer++;
            genericTimer2++;

            if (authority && !phaseStarted && attackPhase != OpeningPhase)
            {
                StartAttackPhase();
            }

            RunAttackPhase(authority);

            // The destination is known ahead of time, so both ends of the coming teleport gather dust.
            if (genericTimer >= phaseTime - TeleportTellTicks)
            {
                GatherDust(NPC.Center, TeleportColor, 30f);
                GatherDust(TeleportPointFor(nextAttackPhase), TeleportColor, 30f);
            }

            if (!authority)
            {
                return;
            }

            if (genericTimer >= phaseTime)
            {
                attackPhase = nextAttackPhase;
                nextAttackPhase = Main.rand.Next(4);
                genericTimer = 0;
                genericTimer2 = 0;
                phaseStarted = false;
                RequestNetworkSnapshot();
            }

            if (NPC.life <= NPC.lifeMax / 2)
            {
                SpawnNextForm<BrokenOkiku>(removeSelf: true, transferredLife: NPC.life);
            }
        }

        // Server only: the teleport and launch that open a phase.
        private void StartAttackPhase()
        {
            phaseStarted = true;
            phaseTime = 90;
            NPC.velocity = Vector2.Zero;

            switch (attackPhase)
            {
                case 0:
                    NPC.Center = SideTeleportPoint;
                    teleportLeft = !teleportLeft;
                    break;

                case 1:
                    subPhase = nextSubPhase;
                    nextSubPhase = Main.rand.Next(2);
                    int crazyOrbType = ModContent.ProjectileType<CrazyOrb>();
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        if (Main.projectile[i].active && Main.projectile[i].type == crazyOrbType)
                        {
                            subPhase = 0;
                            break;
                        }
                    }

                    NPC.Center = OverheadTeleportPoint;
                    break;

                case 2:
                    NPC.Center = CircleTeleportPoint;
                    nextWarpAngle = MathHelper.ToRadians(Main.rand.Next(360));
                    NPC.velocity = UsefulFunctions.Aim(NPC.Center, TargetPlayer.Center, 14);
                    break;

                case 3:
                    phaseTime = 180;
                    NPC.Center = CircleTeleportPoint;
                    nextWarpAngle = MathHelper.ToRadians(Main.rand.Next(360));
                    break;
            }

            RequestNetworkSnapshot();
        }

        private void RunAttackPhase(bool authority)
        {
            Vector2 center = NPC.Center;
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

                    NPC.velocity.Y = -10;
                    SetAimPose(center + new Vector2(sideDirection * 600f, 0f));

                    if (genericTimer2 == 15)
                    {
                        genericTimer2 = 0;
                        if (authority)
                        {
                            Vector2 velocity = new Vector2(sideDirection * 6 + Main.rand.Next(-1, 1), Main.rand.Next(-10, 10) / 5);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), StaffTip, velocity, ModContent.ProjectileType<CrazedOrb>(), 55, 0f, Main.myPlayer);
                        }
                    }
                    break;
                }

                case 1:
                    SetPose(AttraidiesPose.Overhead);
                    if (genericTimer2 >= 50)
                    {
                        if (subPhase == 0)
                        {
                            genericTimer2 = 0;
                            if (authority)
                            {
                                int randomRotation = Main.rand.Next(-20, 20) / 2;
                                for (int i = 0; i < 9; i++)
                                {
                                    float angle = randomRotation + ((360 / 13) * (1 + i)) * 3;
                                    Vector2 velocity = new Vector2((float)Math.Sin(angle), (float)Math.Cos(angle));
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), center, velocity, ModContent.ProjectileType<ObscureSaw>(), 65, 0f, Main.myPlayer);
                                }
                            }
                        }
                        else
                        {
                            genericTimer2 = -200;
                            if (authority)
                            {
                                int randomRotation = Main.rand.Next(-20, 20) / 2;
                                for (int i = 0; i < 6; i++)
                                {
                                    float angle = randomRotation + ((360 / 10) * (1 + i));
                                    Vector2 velocity = new Vector2((float)Math.Sin(angle) * 6, (float)Math.Cos(angle) * 6);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), center, velocity, ModContent.ProjectileType<CrazyOrb>(), 45, 0f, Main.myPlayer, NPC.target);
                                }
                            }
                        }
                    }
                    break;

                case 2:
                    SetAimPose(target.Center);
                    NPC.velocity *= 0.99f;

                    if (genericTimer2 >= 20)
                    {
                        genericTimer2 = 0;
                        if (authority)
                        {
                            Vector2 velocity = UsefulFunctions.Aim(center, target.Center, 0.5f);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), StaffTip, velocity, ModContent.ProjectileType<ObscureSaw>(), 65, 0f, Main.myPlayer);
                        }
                    }
                    break;

                case 3:
                    SetPose(AttraidiesPose.Conduct);
                    NPC.velocity = UsefulFunctions.Aim(center, target.Center, 4);

                    if (genericTimer2 >= 12)
                    {
                        genericTimer2 = 0;
                        if (authority)
                        {
                            float rotation = (target.Center - center).ToRotation();
                            float speed = 0.5f + Main.rand.Next(-3, 3) / 10;
                            Vector2 position = center + new Vector2(Main.rand.Next(-100, 100), Main.rand.Next(-100, 100));
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), position, rotation.ToRotationVector2() * speed, ModContent.ProjectileType<PoisonSmog>(), 18, 0f, Main.myPlayer);
                        }
                    }
                    break;

                default:
                    SetPose(AttraidiesPose.Carry);
                    break;
            }
        }

        // The buff belongs to its player, so only that player's machine removes it.
        private void DispelGravitation()
        {
            if (Main.dedServ || Main.myPlayer != NPC.target)
            {
                return;
            }

            Player player = Main.LocalPlayer;
            for (int i = 0; i < 10; i++)
            {
                if (player.buffType[i] == BuffID.Gravitation)
                {
                    player.DelBuff(i);
                    Main.NewText(LangUtils.GetTextValue("NPCs.Okiku.NoGravitation"), 150, 150, 150);
                    break;
                }
            }
        }

        public override bool CheckActive()
        {
            return false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((sbyte)attackPhase);
            writer.Write((sbyte)nextAttackPhase);
            writer.Write((sbyte)subPhase);
            writer.Write((sbyte)nextSubPhase);
            writer.Write((short)genericTimer);
            writer.Write((short)genericTimer2);
            writer.Write((short)phaseTime);
            writer.Write(phaseStarted);
            writer.Write(teleportLeft);
            writer.Write(nextWarpAngle);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            attackPhase = reader.ReadSByte();
            nextAttackPhase = reader.ReadSByte();
            subPhase = reader.ReadSByte();
            nextSubPhase = reader.ReadSByte();
            genericTimer = reader.ReadInt16();
            genericTimer2 = reader.ReadInt16();
            phaseTime = reader.ReadInt16();
            phaseStarted = reader.ReadBoolean();
            teleportLeft = reader.ReadBoolean();
            nextWarpAngle = reader.ReadSingle();
        }
    }
}
