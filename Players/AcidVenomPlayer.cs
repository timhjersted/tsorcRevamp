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
    /// 100-point acid venom meter, owned by the player's own client; a mirror of <see cref="PoisonPlayer"/>. Its
    /// sources are hits that already run on the victim, and its only result is vanilla Venom, which the owner adds
    /// and vanilla syncs. The level is relayed to other clients at most every SyncIntervalTicks, for their meter only.
    /// </summary>
    public class AcidVenomPlayer : ModPlayer
    {
        // Lavender: lighter than Miasma's deep indigo (104, 18, 205) and bluer than powerful curse's plum (146, 0, 138).
        public static readonly Color VenomColor = new Color(185, 110, 255);

        // Throttle for the display relay, so a per-tick source can't send 60 packets a second.
        private const int SyncIntervalTicks = 10;

        public int VenomLevel { get; private set; }

        private int decayTimer;
        private int lastSentLevel;
        private int syncTimer;

        internal void AddBuildupLocal(int buildup)
        {
            int clampedBuildup = Math.Clamp(buildup, 1, AcidVenomBuildup.MaximumBuildup);
            VenomLevel = Math.Min(VenomLevel + clampedBuildup, AcidVenomBuildup.MaximumBuildup);

            if (VenomLevel < AcidVenomBuildup.MaximumBuildup)
            {
                Player.AddBuff(ModContent.BuffType<AcidVenomBuildup>(), AcidVenomBuildup.MarkerDurationTicks);
                return;
            }

            // Full meter: reset it and apply Venom. Unlike PoisonPlayer, the timer is NOT overwritten afterwards, so
            // Venom's LongerExpertDebuff doubling applies in Expert/Master on purpose (17s → 34s).
            VenomLevel = 0;
            decayTimer = 0;
            Player.ClearBuff(ModContent.BuffType<AcidVenomBuildup>());
            Player.AddBuff(BuffID.Venom, AcidVenomBuildup.VenomDurationTicks);

            PlayTriggerEffects(Player);
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
            packet.Write(tsorcPacketID.SyncAcidVenomLevel);

            if (Main.netMode == NetmodeID.Server)
            {
                packet.Write((byte)Player.whoAmI);
            }

            packet.Write((byte)VenomLevel);
            packet.Send(toWho, ignoreClient);
            lastSentLevel = VenomLevel;
        }

        internal void ReceiveLevel(int level, int fromClient)
        {
            VenomLevel = Math.Clamp(level, 0, AcidVenomBuildup.MaximumBuildup);

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
            if (Player.whoAmI != Main.myPlayer || VenomLevel <= 0)
            {
                decayTimer = 0;
                return;
            }

            if (!Player.HasBuff(ModContent.BuffType<AcidVenomBuildup>()))
            {
                VenomLevel = 0;
                decayTimer = 0;
                return;
            }

            decayTimer++;
            if (decayTimer >= AcidVenomBuildup.DecayIntervalTicks)
            {
                VenomLevel--;
                decayTimer = 0;

                if (VenomLevel == 0)
                {
                    Player.ClearBuff(ModContent.BuffType<AcidVenomBuildup>());
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
            if (syncTimer >= SyncIntervalTicks && VenomLevel != lastSentLevel)
            {
                syncTimer = 0;
                SendLevel();
            }
        }

        public override void UpdateDead()
        {
            VenomLevel = 0;
            decayTimer = 0;
        }

        private static void PlayTriggerEffects(Player player)
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.NPCDeath13 with { Volume = 0.7f, Pitch = -0.2f }, player.Center);

            // Venom-purple splash; tinted toward the meter colour so the trigger matches the bar that just emptied.
            for (int i = 0; i < 30; i++)
            {
                Dust dust = Dust.NewDustPerfect(player.Center, DustID.Venom, Main.rand.NextVector2Circular(4f, 4f), 60, VenomColor, Main.rand.NextFloat(1f, 1.6f));
                dust.noGravity = true;
            }
        }
    }

    internal class AcidVenomMeterDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.FrontAccFront);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
            => !Main.gameMenu && !drawInfo.drawPlayer.dead && drawInfo.drawPlayer.GetModPlayer<AcidVenomPlayer>().VenomLevel > 0
            && ModContent.GetInstance<tsorcRevampVisualConfig>().ClassicStatusMeters; // replaced by Players/StatusBars.cs unless Classic is on

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            Texture2D empty = ModContent.Request<Texture2D>("tsorcRevamp/Textures/StatusMeter_empty").Value;
            Texture2D full = ModContent.Request<Texture2D>("tsorcRevamp/Textures/StatusMeter_full").Value;

            // Stack above every other visible status meter (poison included), 42px per meter.
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

            if (player.GetModPlayer<PoisonPlayer>().PoisonLevel > 0)
            {
                precedingMeters++;
            }

            float abovePlayer = 82f + precedingMeters * 42f;
            Point origin = (player.Center - new Vector2(empty.Width / 2f, abovePlayer) - Main.screenPosition).ToPoint();
            Main.spriteBatch.Draw(empty, new Rectangle(origin.X, origin.Y, empty.Width, empty.Height), Color.White);

            // Fill rises from the bottom of the meter.
            float percentage = player.GetModPlayer<AcidVenomPlayer>().VenomLevel / (float)AcidVenomBuildup.MaximumBuildup;
            int fillHeight = Math.Max(1, (int)Math.Round(full.Height * percentage));
            int sourceY = full.Height - fillHeight;
            Rectangle source = new Rectangle(0, sourceY, full.Width, fillHeight);
            Rectangle destination = new Rectangle(origin.X, origin.Y + sourceY, full.Width, fillHeight);
            Main.spriteBatch.Draw(full, destination, source, AcidVenomPlayer.VenomColor);
        }
    }
}
