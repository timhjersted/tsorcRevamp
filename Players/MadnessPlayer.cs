using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Utilities;

namespace tsorcRevamp
{
    public class MadnessPlayer : ModPlayer
    {
        private const int TrailCount = 12;
        private const int FlatTriggerDamage = 200;
        private const int BuildupDecayIntervalTicks = 60;

        public int MadnessLevel;

        private int buildupDecayTimer;
        private readonly Vector2[] trailPositions = new Vector2[TrailCount];
        private bool trailInitialized;

        public bool MadnessActive => Player.HasBuff(ModContent.BuffType<Madness>());
        internal Vector2[] TrailPositions => trailPositions;

        public void RequestBuildup(int buildup, int durationTicks)
        {
            buildup = Math.Clamp(buildup, 1, MadnessBuildup.MaximumBuildup);
            durationTicks = Math.Clamp(durationTicks, 1, 60 * 60);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                // Hostile OnHitPlayer hooks run on the hit player's machine. Only that player may
                // report their buildup; the server clamps it and resolves the proc.
                if (Player.whoAmI != Main.myPlayer)
                {
                    return;
                }

                ModPacket packet = Mod.GetPacket();
                packet.Write((byte)tsorcPacketID.ReportMadnessBuildup);
                packet.Write((byte)buildup);
                packet.Write((short)durationTicks);
                packet.Send();
                return;
            }

            ApplyBuildupAuthoritative(buildup, durationTicks);
        }

        internal void ApplyBuildupAuthoritative(int buildup, int durationTicks)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || Player.dead || !Player.active)
            {
                return;
            }

            MadnessLevel = Math.Clamp(MadnessLevel + buildup, 0, MadnessBuildup.MaximumBuildup);
            Player.AddBuff(ModContent.BuffType<MadnessBuildup>(), durationTicks);

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.AddPlayerBuff, -1, -1, null, Player.whoAmI,
                    ModContent.BuffType<MadnessBuildup>(), durationTicks);
            }

            bool triggered = MadnessLevel >= MadnessBuildup.MaximumBuildup;
            if (triggered)
            {
                TriggerMadnessAuthoritative();
            }

            SyncState(triggered);
        }

        private void TriggerMadnessAuthoritative()
        {
            MadnessLevel = 0;
            buildupDecayTimer = 0;
            Player.ClearBuff(ModContent.BuffType<MadnessBuildup>());

            Player.AddBuff(ModContent.BuffType<Madness>(), Madness.DurationTicks);
            Stagger.Apply(Player, Stagger.DebtHitDurationTicks);
            Player.AddBuff(BuffID.Ichor, Stagger.DebtHitDurationTicks);

            Player.statLife -= FlatTriggerDamage;
            if (Player.statLife <= 0)
            {
                Player.KillMe(PlayerDeathReason.ByCustomReason(NetworkText.FromKey(
                    "Mods.tsorcRevamp.DeathText.MadnessDeathReason", Player.name)),
                    FlatTriggerDamage, 0, false);
            }

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.AddPlayerBuff, -1, -1, null, Player.whoAmI,
                    ModContent.BuffType<Madness>(), Madness.DurationTicks);
                NetMessage.SendData(MessageID.AddPlayerBuff, -1, -1, null, Player.whoAmI,
                    ModContent.BuffType<Stagger>(), Stagger.DebtHitDurationTicks);
                NetMessage.SendData(MessageID.AddPlayerBuff, -1, -1, null, Player.whoAmI,
                    BuffID.Ichor, Stagger.DebtHitDurationTicks);
                NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, Player.whoAmI);
            }
            else
            {
                PlayTriggerEffects(Player);
            }
        }

        internal void ReceiveState(int buildup, bool playTriggerEffects)
        {
            MadnessLevel = Math.Clamp(buildup, 0, MadnessBuildup.MaximumBuildup);
            buildupDecayTimer = 0;

            if (playTriggerEffects && Player.whoAmI == Main.myPlayer)
            {
                PlayTriggerEffects(Player);
            }
        }

        private void SyncState(bool playTriggerEffects, int toWho = -1, int fromWho = -1)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                return;
            }

            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)tsorcPacketID.SyncMadnessState);
            packet.Write((byte)Player.whoAmI);
            packet.Write((byte)Math.Clamp(MadnessLevel, 0, MadnessBuildup.MaximumBuildup));
            packet.Write(playTriggerEffects);
            packet.Send(toWho, fromWho);
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            SyncState(false, toWho, fromWho);
        }

        public override void PostUpdateBuffs()
        {
            if (MadnessLevel <= 0)
            {
                buildupDecayTimer = 0;
                return;
            }

            if (!Player.HasBuff(ModContent.BuffType<MadnessBuildup>()))
            {
                MadnessLevel = 0;
                buildupDecayTimer = 0;
                return;
            }

            // Both server and clients advance this deterministic cosmetic-facing decay. The next
            // authoritative hit snapshot corrects any small latency drift.
            buildupDecayTimer++;
            if (buildupDecayTimer >= BuildupDecayIntervalTicks)
            {
                MadnessLevel--;
                buildupDecayTimer = 0;
            }
        }

        public override void PostUpdate()
        {
            if (!MadnessActive || Player.dead)
            {
                trailInitialized = false;
                return;
            }

            if (!trailInitialized)
            {
                for (int i = 0; i < trailPositions.Length; i++)
                {
                    trailPositions[i] = Player.position;
                }
                trailInitialized = true;
                return;
            }

            for (int i = trailPositions.Length - 1; i > 0; i--)
            {
                trailPositions[i] = trailPositions[i - 1];
            }
            trailPositions[0] = Player.position;
        }

        public override void UpdateDead()
        {
            MadnessLevel = 0;
            buildupDecayTimer = 0;
            trailInitialized = false;
        }

        private static void PlayTriggerEffects(Player player)
        {
            CombatText.NewText(player.Hitbox, CombatText.DamagedFriendly, FlatTriggerDamage);
            SoundEngine.PlaySound(SoundID.Item122 with
            {
                Volume = 0.85f,
                Pitch = 0.35f,
                PitchVariance = 0.12f
            }, player.Center);

            UsefulFunctions.ScreenShake(player.Center, 2f, 12,
                distanceFalloff: 500f, uniqueIdentity: "MadnessTrigger");

            for (int i = 0; i < 36; i++)
            {
                Dust dust = Dust.NewDustPerfect(player.Center,
                    i % 3 == 0 ? DustID.GoldFlame : DustID.YellowTorch,
                    Main.rand.NextVector2Circular(6f, 6f), 80,
                    new Color(255, 220, 55), Main.rand.NextFloat(1.1f, 2.1f));
                dust.noGravity = true;
            }
        }
    }

    internal class MadnessPlayerTrailDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.FrontAccFront);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
            => drawInfo.drawPlayer.whoAmI == Main.myPlayer
            && !drawInfo.drawPlayer.dead
            && drawInfo.drawPlayer.HasBuff(ModContent.BuffType<Madness>());

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            Vector2[] history = player.GetModPlayer<MadnessPlayer>().TrailPositions;
            int cacheCount = drawInfo.DrawDataCache.Count;
            Color madnessYellow = new Color(255, 215, 45);
            Vector2? lastDrawnPosition = null;

            for (int historyIndex = history.Length - 1; historyIndex >= 0; historyIndex--)
            {
                Vector2 offset = history[historyIndex] - player.position;
                if (offset.LengthSquared() < 1f
                    || (lastDrawnPosition.HasValue
                        && Vector2.DistanceSquared(history[historyIndex], lastDrawnPosition.Value) < 1f))
                {
                    continue;
                }
                lastDrawnPosition = history[historyIndex];

                float age = (historyIndex + 1f) / history.Length;
                float opacity = MathHelper.Lerp(0.11f, 0.025f, age);
                for (int drawIndex = 0; drawIndex < cacheCount; drawIndex++)
                {
                    DrawData data = drawInfo.DrawDataCache[drawIndex];
                    Color color = Color.Lerp(data.color, madnessYellow, 0.72f) * opacity;
                    Main.EntitySpriteDraw(data.texture, data.position + offset, data.sourceRect, color,
                        data.rotation, data.origin, data.scale, data.effect, 0);
                }
            }
        }
    }

    internal class MadnessMeterDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.FrontAccFront);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
            => !Main.gameMenu && !drawInfo.drawPlayer.dead
            && drawInfo.drawPlayer.GetModPlayer<MadnessPlayer>().MadnessLevel > 0;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            float percentage = MathHelper.Clamp(
                player.GetModPlayer<MadnessPlayer>().MadnessLevel / (float)MadnessBuildup.MaximumBuildup,
                0f, 1f);

            Texture2D empty = ModContent.Request<Texture2D>("tsorcRevamp/Textures/StatusMeter_empty").Value;
            Texture2D full = ModContent.Request<Texture2D>("tsorcRevamp/Textures/StatusMeter_full").Value;

            tsorcRevampPlayer legacyPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            bool curseMeterVisible = legacyPlayer.CurseLevel > 1 || legacyPlayer.PowerfulCurseLevel > 1;
            float abovePlayer = curseMeterVisible ? 124f : 82f;
            Point origin = (player.Center - new Vector2(empty.Width / 2f, abovePlayer) - Main.screenPosition).ToPoint();

            Main.spriteBatch.Draw(empty, new Rectangle(origin.X, origin.Y, empty.Width, empty.Height), Color.White);

            int fillHeight = Math.Max(1, (int)Math.Round(full.Height * percentage));
            int sourceY = full.Height - fillHeight;
            Rectangle source = new Rectangle(0, sourceY, full.Width, fillHeight);
            Rectangle destination = new Rectangle(origin.X, origin.Y + sourceY, full.Width, fillHeight);
            Main.spriteBatch.Draw(full, destination, source, new Color(255, 220, 45));
        }
    }
}
