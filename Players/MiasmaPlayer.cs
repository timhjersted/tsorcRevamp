using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp
{
    /// <summary>
    /// Server-owned 100-point Miasma meter. A full meter applies the Miasma buff, which drains
    /// life on the owning client through vanilla bad-life-regen (synced like poison).
    /// </summary>
    public class MiasmaPlayer : ModPlayer
    {
        // Deep violet. Shared by the meter/status bar fill, the infection dust and Plantera's breath.
        public static readonly Color MiasmaColor = new Color(104, 18, 205);
        // Hot pink accent mixed into the hit burst.
        public static readonly Color MiasmaPinkColor = new Color(255, 105, 215);

        public int MiasmaLevel { get; private set; }

        private int decayTimer;
        private int procSequence;
        private int lastResolvedProc;

        public void RequestBuildup(int buildup)
        {
            buildup = Math.Clamp(buildup, 1, MiasmaBuildup.MaximumBuildup);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                // Hostile hits execute on the victim. Never report another player's buildup.
                if (Player.whoAmI != Main.myPlayer)
                {
                    return;
                }

                // The victim plays its own burst immediately; the server relays it to everyone else.
                PlayHitBurst(Player);

                ModPacket packet = Mod.GetPacket();
                packet.Write(tsorcPacketID.ReportMiasmaBuildup);
                packet.Write((byte)buildup);
                packet.Send();
                return;
            }

            ApplyBuildupAuthoritative(buildup);
        }

        internal void ApplyBuildupAuthoritative(int buildup)
        {
            int buildupType = ModContent.BuffType<MiasmaBuildup>();

            if (Main.netMode == NetmodeID.MultiplayerClient || !Player.active || Player.dead || Player.buffImmune[buildupType])
            {
                return;
            }

            int clampedBuildup = Math.Clamp(buildup, 1, MiasmaBuildup.MaximumBuildup);
            MiasmaLevel = Math.Clamp(MiasmaLevel + clampedBuildup, 0, MiasmaBuildup.MaximumBuildup);

            // Singleplayer only here: MP victims played theirs in RequestBuildup, the rest get it via SyncState.
            if (!Main.dedServ)
            {
                PlayHitBurst(Player);
            }

            bool triggered = MiasmaLevel >= MiasmaBuildup.MaximumBuildup;
            if (triggered)
            {
                // Meter resets; procSequence lets clients apply this proc exactly once.
                MiasmaLevel = 0;
                decayTimer = 0;
                procSequence++;
                Player.ClearBuff(buildupType);
                Player.AddBuff(ModContent.BuffType<Miasma>(), Miasma.DurationTicks);

                if (!Main.dedServ)
                {
                    PlayTriggerEffects(Player);
                }
            }
            else
            {
                Player.AddBuff(buildupType, MiasmaBuildup.MarkerDurationTicks);
            }

            SyncState(triggered, true);
        }

        private void SyncState(bool triggered = false, bool hitBurst = false, int toWho = -1, int fromWho = -1)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                return;
            }

            ModPacket packet = Mod.GetPacket();
            packet.Write(tsorcPacketID.SyncMiasmaState);
            packet.Write((byte)Player.whoAmI);
            packet.Write((byte)MiasmaLevel);
            packet.Write((byte)decayTimer);
            packet.Write(procSequence);
            packet.Write(triggered);
            packet.Write(hitBurst);
            packet.Send(toWho, fromWho);
        }

        internal void ReceiveState(int buildup, int decayPhase, int sequence, bool triggered, bool hitBurst)
        {
            // The victim already played its own burst when it reported the hit.
            if (hitBurst && Player.whoAmI != Main.myPlayer && !Player.dead)
            {
                PlayHitBurst(Player);
            }

            MiasmaLevel = Math.Clamp(buildup, 0, MiasmaBuildup.MaximumBuildup);
            decayTimer = Math.Clamp(decayPhase, 0, MiasmaBuildup.DecayIntervalTicks - 1);

            int buildupType = ModContent.BuffType<MiasmaBuildup>();
            if (MiasmaLevel == 0)
            {
                Player.ClearBuff(buildupType);
            }
            else
            {
                Player.AddBuff(buildupType, MiasmaBuildup.MarkerDurationTicks);
            }

            // The owner needs the buff for the drain; other clients need it for the dust.
            bool isNewProc = triggered && sequence > lastResolvedProc;
            if (isNewProc && !Player.dead)
            {
                Player.AddBuff(ModContent.BuffType<Miasma>(), Miasma.DurationTicks);
                PlayTriggerEffects(Player);
            }

            lastResolvedProc = Math.Max(lastResolvedProc, sequence);
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            SyncState(false, false, toWho, fromWho);
        }

        public override void PostUpdateBuffs()
        {
            // Server/singleplayer decays the meter 1 point per DecayIntervalTicks and syncs each step.
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            if (MiasmaLevel <= 0)
            {
                decayTimer = 0;
                return;
            }

            if (!Player.HasBuff(ModContent.BuffType<MiasmaBuildup>()))
            {
                MiasmaLevel = 0;
                decayTimer = 0;
                SyncState();
                return;
            }

            decayTimer++;
            if (decayTimer >= MiasmaBuildup.DecayIntervalTicks)
            {
                MiasmaLevel = Math.Max(0, MiasmaLevel - 1);
                decayTimer = 0;

                if (MiasmaLevel == 0)
                {
                    Player.ClearBuff(ModContent.BuffType<MiasmaBuildup>());
                }

                SyncState();
            }
        }

        public override void UpdateBadLifeRegen()
        {
            if (!Player.HasBuff(ModContent.BuffType<Miasma>()))
            {
                return;
            }

            // Poison-style drain. lifeRegen is in half-HP per second, so 2 units = 1 HP/s.
            if (Player.lifeRegen > 0)
            {
                Player.lifeRegen = 0;
            }

            Player.lifeRegenTime = 0;
            int drainPerSecond = Miasma.CurrentDrainPerSecond();
            Player.lifeRegen -= drainPerSecond * 2;
        }

        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource)
        {
            // Vanilla bad-life-regen deaths use "other" reasons 8-10. Rename them while Miasma is draining.
            bool diedToLifeDrain = damageSource.SourceOtherIndex >= 8 && damageSource.SourceOtherIndex <= 10;

            if (diedToLifeDrain && Player.HasBuff(ModContent.BuffType<Miasma>()))
            {
                damageSource = PlayerDeathReason.ByCustomReason(NetworkText.FromKey(
                    "Mods.tsorcRevamp.DeathText.MiasmaDeathReason", Player.name));
            }

            return true;
        }

        public override void UpdateDead()
        {
            bool changed = MiasmaLevel != 0;
            MiasmaLevel = 0;
            decayTimer = 0;

            // Proc sequence history is kept across death to reject delayed duplicate packets.
            if (changed)
            {
                SyncState();
            }
        }

        public override void PostUpdate()
        {
            // Infected players vent miasma: 2 rising motes every 3 ticks, same colour as the meter.
            if (Main.dedServ || Player.dead || !Player.HasBuff(ModContent.BuffType<Miasma>()))
            {
                return;
            }

            if (Main.GameUpdateCount % 3 != 0)
            {
                return;
            }

            for (int i = 0; i < 2; i++)
            {
                Vector2 spawnPosition = Player.Center + Main.rand.NextVector2Circular(12f, 20f);
                Vector2 driftVelocity = new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), Main.rand.NextFloat(-1.2f, -0.4f));
                float scale = Main.rand.NextFloat(0.7f, 1.1f);

                Dust dust = Dust.NewDustPerfect(spawnPosition, DustID.RainbowMk2, driftVelocity, 100, MiasmaColor, scale);
                dust.noGravity = true;
            }
        }

        // Bright purple + pink puff on every buildup hit. Alternates the two colours, flung outward from the player.
        private static void PlayHitBurst(Player player)
        {
            for (int i = 0; i < 16; i++)
            {
                Color dustColor = MiasmaColor;

                if (i % 2 == 0)
                {
                    dustColor = MiasmaPinkColor;
                }

                Vector2 spawnPosition = player.Center + Main.rand.NextVector2Circular(10f, 16f);
                Vector2 burstVelocity = Main.rand.NextVector2Circular(3.5f, 3.5f);
                float scale = Main.rand.NextFloat(0.9f, 1.4f);

                Dust dust = Dust.NewDustPerfect(spawnPosition, DustID.RainbowMk2, burstVelocity, 60, dustColor, scale);
                dust.noGravity = true;
            }
        }

        private static void PlayTriggerEffects(Player player)
        {
            SoundEngine.PlaySound(SoundID.NPCDeath13 with { Volume = 0.8f, Pitch = -0.3f }, player.Center);

            for (int i = 0; i < 30; i++)
            {
                Dust dust = Dust.NewDustPerfect(player.Center, DustID.RainbowMk2, Main.rand.NextVector2Circular(5f, 5f), 80, MiasmaColor, Main.rand.NextFloat(1f, 1.6f));
                dust.noGravity = true;
            }
        }
    }

    internal class MiasmaMeterDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.FrontAccFront);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
            => !Main.gameMenu && !drawInfo.drawPlayer.dead && drawInfo.drawPlayer.GetModPlayer<MiasmaPlayer>().MiasmaLevel > 0
            && ModContent.GetInstance<tsorcRevampVisualConfig>().ClassicStatusMeters; // replaced by Players/StatusBars.cs unless Classic is on

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            Texture2D empty = ModContent.Request<Texture2D>("tsorcRevamp/Textures/StatusMeter_empty").Value;
            Texture2D full = ModContent.Request<Texture2D>("tsorcRevamp/Textures/StatusMeter_full").Value;

            // Stack above any other visible status meters, 42px per meter (same spacing as Frost/Blight).
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

            float abovePlayer = 82f + precedingMeters * 42f;
            Point origin = (player.Center - new Vector2(empty.Width / 2f, abovePlayer) - Main.screenPosition).ToPoint();
            Main.spriteBatch.Draw(empty, new Rectangle(origin.X, origin.Y, empty.Width, empty.Height), Color.White);

            // Fill rises from the bottom of the meter.
            float percentage = player.GetModPlayer<MiasmaPlayer>().MiasmaLevel / (float)MiasmaBuildup.MaximumBuildup;
            int fillHeight = Math.Max(1, (int)Math.Round(full.Height * percentage));
            int sourceY = full.Height - fillHeight;
            Rectangle source = new Rectangle(0, sourceY, full.Width, fillHeight);
            Rectangle destination = new Rectangle(origin.X, origin.Y + sourceY, full.Width, fillHeight);
            Main.spriteBatch.Draw(full, destination, source, MiasmaPlayer.MiasmaColor);
        }
    }
}
