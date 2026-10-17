using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp
{
    /// <summary>
    /// 100-point poison meter, owned by the player's own client. Unlike Miasma/Blight this is not
    /// server-authoritative: its sources are local (water contact at 1/tick, hits that already run on
    /// the victim), and its only result is vanilla Poisoned, which the owner adds and vanilla syncs.
    /// The level is relayed to other clients at most every SyncIntervalTicks, purely for their meter display.
    /// </summary>
    public class PoisonPlayer : ModPlayer
    {
        public static readonly Color PoisonColor = new Color(110, 230, 60);

        // Throttle for the display relay: water adds 1 per tick, so per-change packets would be 60/s.
        private const int SyncIntervalTicks = 10;

        public int PoisonLevel { get; private set; }

        private int decayTimer;
        private int lastSentLevel;
        private int syncTimer;

        internal void AddBuildupLocal(int buildup)
        {
            int clampedBuildup = Math.Clamp(buildup, 1, PoisonBuildup.MaximumBuildup);
            PoisonLevel = Math.Min(PoisonLevel + clampedBuildup, PoisonBuildup.MaximumBuildup);

            if (PoisonLevel < PoisonBuildup.MaximumBuildup)
            {
                Player.AddBuff(ModContent.BuffType<PoisonBuildup>(), PoisonBuildup.MarkerDurationTicks);
                return;
            }

            // Full meter: reset it and poison for exactly PoisonDurationTicks. AddBuff stretches
            // Poisoned in Expert/Master (LongerExpertDebuff), so the timer is overwritten afterwards.
            PoisonLevel = 0;
            decayTimer = 0;
            Player.ClearBuff(ModContent.BuffType<PoisonBuildup>());
            Player.AddBuff(BuffID.Poisoned, PoisonBuildup.PoisonDurationTicks);

            int poisonIndex = Player.FindBuffIndex(BuffID.Poisoned);
            if (poisonIndex >= 0)
            {
                Player.buffTime[poisonIndex] = PoisonBuildup.PoisonDurationTicks;
            }

            PlayTriggerEffects(Player);
            SendLevel();
        }

        // Cures (Bloodred Moss Clump) empty the meter. Owner-only like AddBuildupLocal; the reset is relayed at once.
        internal void ClearBuildupLocal()
        {
            if (Player.whoAmI != Main.myPlayer || PoisonLevel <= 0)
            {
                return;
            }

            PoisonLevel = 0;
            decayTimer = 0;
            Player.ClearBuff(ModContent.BuffType<PoisonBuildup>());
            SendLevel();
        }

        // Client → server: [level]. Server → other clients: [player][level].
        private void SendLevel(int toWho = -1, int ignoreClient = -1)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                return;
            }

            ModPacket packet = Mod.GetPacket();
            packet.Write(tsorcPacketID.SyncPoisonLevel);

            if (Main.netMode == NetmodeID.Server)
            {
                packet.Write((byte)Player.whoAmI);
            }

            packet.Write((byte)PoisonLevel);
            packet.Send(toWho, ignoreClient);
            lastSentLevel = PoisonLevel;
        }

        internal void ReceiveLevel(int level, int fromClient)
        {
            PoisonLevel = Math.Clamp(level, 0, PoisonBuildup.MaximumBuildup);

            // The server forwards the owner's value to everyone else.
            if (Main.netMode == NetmodeID.Server)
            {
                SendLevel(-1, fromClient);
            }
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            SendLevel(toWho, fromWho);
        }

        public override void PostUpdateBuffs()
        {
            // Owner decays the meter 1 point per DecayIntervalTicks; it clears if the marker buff is gone.
            if (Player.whoAmI != Main.myPlayer || PoisonLevel <= 0)
            {
                decayTimer = 0;
                return;
            }

            if (!Player.HasBuff(ModContent.BuffType<PoisonBuildup>()))
            {
                PoisonLevel = 0;
                decayTimer = 0;
                return;
            }

            decayTimer++;
            if (decayTimer >= PoisonBuildup.DecayIntervalTicks)
            {
                PoisonLevel--;
                decayTimer = 0;

                if (PoisonLevel == 0)
                {
                    Player.ClearBuff(ModContent.BuffType<PoisonBuildup>());
                }
            }
        }

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer || Main.netMode != NetmodeID.MultiplayerClient)
            {
                return;
            }

            syncTimer++;
            if (syncTimer >= SyncIntervalTicks && PoisonLevel != lastSentLevel)
            {
                syncTimer = 0;
                SendLevel();
            }
        }

        public override void UpdateDead()
        {
            PoisonLevel = 0;
            decayTimer = 0;
        }

        private static void PlayTriggerEffects(Player player)
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.NPCDeath13 with { Volume = 0.7f, Pitch = 0.2f }, player.Center);

            for (int i = 0; i < 30; i++)
            {
                Dust dust = Dust.NewDustPerfect(player.Center, DustID.Poisoned, Main.rand.NextVector2Circular(4f, 4f), 60, default, Main.rand.NextFloat(1f, 1.6f));
                dust.noGravity = true;
            }
        }
    }

    internal class PoisonMeterDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.FrontAccFront);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
            => !Main.gameMenu && !drawInfo.drawPlayer.dead && drawInfo.drawPlayer.GetModPlayer<PoisonPlayer>().PoisonLevel > 0
            && ModContent.GetInstance<tsorcRevampVisualConfig>().ClassicStatusMeters; // replaced by Players/StatusBars.cs unless Classic is on

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            Texture2D empty = ModContent.Request<Texture2D>("tsorcRevamp/Textures/StatusMeter_empty").Value;
            Texture2D full = ModContent.Request<Texture2D>("tsorcRevamp/Textures/StatusMeter_full").Value;

            // Stack above any other visible status meters, 42px per meter (same spacing as Frost/Blight/Miasma).
            tsorcRevampPlayer legacy = player.GetModPlayer<tsorcRevampPlayer>();
            int precedingMeters = 0;

            if (legacy.CurseLevel > 1 || legacy.PowerfulCurseLevel > 1)
            {
                precedingMeters++;
            }

            if (player.GetModPlayer<MadnessPlayer>().MadnessLevel > 0)
            {
                precedingMeters++;
            }

            if (player.GetModPlayer<FrostPlayer>().FrostLevel > 0)
            {
                precedingMeters++;
            }

            if (player.GetModPlayer<BlightPlayer>().BlightLevel > 0)
            {
                precedingMeters++;
            }

            if (player.GetModPlayer<MiasmaPlayer>().MiasmaLevel > 0)
            {
                precedingMeters++;
            }

            float abovePlayer = 82f + precedingMeters * 42f;
            Point origin = (player.Center - new Vector2(empty.Width / 2f, abovePlayer) - Main.screenPosition).ToPoint();
            Main.spriteBatch.Draw(empty, new Rectangle(origin.X, origin.Y, empty.Width, empty.Height), Color.White);

            // Fill rises from the bottom of the meter.
            float percentage = player.GetModPlayer<PoisonPlayer>().PoisonLevel / (float)PoisonBuildup.MaximumBuildup;
            int fillHeight = Math.Max(1, (int)Math.Round(full.Height * percentage));
            int sourceY = full.Height - fillHeight;
            Rectangle source = new Rectangle(0, sourceY, full.Width, fillHeight);
            Rectangle destination = new Rectangle(origin.X, origin.Y + sourceY, full.Width, fillHeight);
            Main.spriteBatch.Draw(full, destination, source, PoisonPlayer.PoisonColor);
        }
    }
}
