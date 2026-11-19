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
    /// Second form: rides the Shadow Dragon's head. With the dragon dead it hovers above the target raining Obscure Drops,
    /// then channels the dragon back. Killing this form hands over to the third.
    /// </summary>
    [AutoloadBossHead]
    public class DarkDragonMask : AttraidiesPuppet
    {
        private const int RainTicks = 600;
        private const int RespawnTick = 780;
        private const int RainInterval = 2;
        private const float RainHeight = 250f;
        private const int ObscureDropDamage = 60;

        // The rider is a faint shape on the dragon and solidifies when it is exposed, as the old form did.
        private const int RidingAlpha = 200;
        private const int ExposedAlpha = 40;

        private NPCDespawnHandler despawnHandler;
        private bool dragonSpawned;
        private int rainTimer;
        private int dragonIndex;
        private int rainOffsetX;

        private int NextRainOffsetX
        {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }

        private int RespawnTimer
        {
            get => (int)NPC.ai[1];
            set => NPC.ai[1] = value;
        }

        protected override NPCDespawnHandler DespawnHandler => despawnHandler;
        protected override bool PlaysArrivalBurst => true;

        public override void SetDefaults()
        {
            ApplyBodyDefaults();
            NPC.damage = 1;
            NPC.defense = 36;
            NPC.boss = true;
            NPC.lifeMax = 24000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.npcSlots = 6f;
            NPC.alpha = RidingAlpha;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.DarkDragonMask.DespawnHandler"), Color.DarkMagenta, DustID.PurpleCrystalShard);
        }

        private void SpawnDragon()
        {
            dragonIndex = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<ShadowDragonHead>(), NPC.whoAmI);
            Main.npc[dragonIndex].velocity.Y = -10;
            RequestNetworkSnapshot();
        }

        protected override void TickForm()
        {
            bool authority = IsAuthority;
            int passiveDust = Dust.NewDust(NPC.position, NPC.width, NPC.height, 62, 0, 0, 100, Color.White, 1.0f);
            Main.dust[passiveDust].noGravity = true;

            DispelGravitation();

            if (!dragonSpawned)
            {
                if (authority)
                {
                    SpawnDragon();
                }

                dragonSpawned = true;
            }

            int headType = ModContent.NPCType<ShadowDragonHead>();
            bool dragonDead = !NPC.AnyNPCs(headType);

            int alphaTarget = RidingAlpha;
            if (dragonDead)
            {
                alphaTarget = ExposedAlpha;
            }

            if (NPC.alpha < alphaTarget)
            {
                NPC.alpha++;
            }
            else if (NPC.alpha > alphaTarget)
            {
                NPC.alpha--;
            }

            if (!dragonDead)
            {
                SetPose(AttraidiesPose.Carry);

                bool indexStale = !Main.npc[dragonIndex].active || Main.npc[dragonIndex].type != headType;
                if (indexStale)
                {
                    int? foundIndex = UsefulFunctions.GetFirstNPC(headType);
                    if (foundIndex.HasValue)
                    {
                        dragonIndex = foundIndex.Value;
                    }
                }

                NPC.Center = Main.npc[dragonIndex].Center;
                return;
            }

            RespawnTimer++;

            if (RespawnTimer == 1 && authority)
            {
                rainOffsetX = NextRainOffsetX;
                NextRainOffsetX = Main.rand.Next(-150, 150);
                RequestNetworkSnapshot();
            }

            if (RespawnTimer <= RainTicks)
            {
                SetPose(AttraidiesPose.Overhead);
                NPC.velocity = Vector2.Zero;

                // Every machine follows the same synced offset above its view of the target.
                NPC.position.X = TargetPlayer.position.X + rainOffsetX;
                NPC.position.Y = TargetPlayer.position.Y - RainHeight;

                rainTimer++;
                if (rainTimer >= RainInterval)
                {
                    rainTimer = 0;
                    if (authority)
                    {
                        SpawnObscureDrops();
                    }
                }
            }
            else
            {
                SetPose(AttraidiesPose.Conduct);
                NPC.velocity = Vector2.Zero;
                GatherDust(StaffTip, ArrivalColor, 20f);
            }

            if (RespawnTimer >= RespawnTick && authority)
            {
                SpawnDragon();
                RespawnTimer = 0;
            }
        }

        private void SpawnObscureDrops()
        {
            Vector2 tip = StaffTip;
            Projectile.NewProjectile(NPC.GetSource_FromThis(), tip.X, tip.Y, Main.rand.Next(-120, 120) / 10, -7, ModContent.ProjectileType<ObscureDrop>(), ObscureDropDamage, 0f, Main.myPlayer);

            if (Main.rand.NextBool(4))
            {
                int speedX = Main.rand.Next(Main.rand.Next(-160, -120), Main.rand.Next(120, 160)) / 10;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), tip.X, tip.Y, speedX, -7, ModContent.ProjectileType<ObscureDrop>(), ObscureDropDamage, 0f, Main.myPlayer);
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
                    Main.NewText(LangUtils.GetTextValue("NPCs.DarkDragonMask.NoGravitaion"), 150, 150, 150);
                    break;
                }
            }
        }

        public override bool CheckActive()
        {
            return false;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            base.HitEffect(hit);

            if (NPC.life > 0 || Main.dedServ)
            {
                return;
            }

            BurstDust(NPC.Center, ArrivalColor, 50, 9f);
        }

        public override void OnKill()
        {
            if (Main.npc[dragonIndex].active && Main.npc[dragonIndex].type == ModContent.NPCType<ShadowDragonHead>())
            {
                Main.npc[dragonIndex].life = 0;
                Main.npc[dragonIndex].active = false;
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, number: dragonIndex);
                }
            }

            SpawnNextForm<Okiku>(removeSelf: false);
            UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.DarkDragonMask.Laughter"), new Color(175, 75, 255));
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((short)dragonIndex);
            writer.Write(dragonSpawned);
            writer.Write((short)rainOffsetX);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            dragonIndex = reader.ReadInt16();
            dragonSpawned = reader.ReadBoolean();
            rainOffsetX = reader.ReadInt16();
        }
    }
}
