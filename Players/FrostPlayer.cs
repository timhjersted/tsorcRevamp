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

namespace tsorcRevamp
{
    /// <summary>Server-owned100-point frost meter; victim clients report hits and execute proc damage once.</summary>
    public class FrostPlayer : ModPlayer
    {
        public int FrostLevel { get; private set; }
        private int _decayTimer, _procSequence, _lastResolvedProc;
        public float MovementSlowFraction => FrostLevel > 0
            ? Math.Clamp(FrostLevel / (float)FrostBuildup.MaximumBuildup * 0.1f, 0.01f, 0.1f) : 0f;

        public void RequestBuildup(int buildup)
        {
            buildup = Math.Clamp(buildup, 1, FrostBuildup.MaximumBuildup);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                // Hostile hits execute on the victim. Never report another player's buildup.
                if (Player.whoAmI != Main.myPlayer) return;
                ModPacket packet = Mod.GetPacket();
                packet.Write(tsorcPacketID.ReportFrostBuildup);
                packet.Write((byte)buildup);
                packet.Send();
                return;
            }
            ApplyBuildupAuthoritative(buildup);
        }
        internal void ApplyBuildupAuthoritative(int buildup)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !Player.active || Player.dead
                || Player.buffImmune[ModContent.BuffType<FrostBuildup>()]) return;
            FrostLevel = Math.Clamp(FrostLevel + Math.Clamp(buildup, 1, FrostBuildup.MaximumBuildup), 0, FrostBuildup.MaximumBuildup);
            int triggerDamage = 0;
            if (FrostLevel >= FrostBuildup.MaximumBuildup)
            {
                FrostLevel = 0; _decayTimer = 0; _procSequence++;
                Player.ClearBuff(ModContent.BuffType<FrostBuildup>());
                ApplyFrozen(Player);
                Player.AddBuff(ModContent.BuffType<Frost>(), Frost.DurationTicks);
                // Exactly10% of effective maximum life, rounded up; no defense or damage variance.
                triggerDamage = Math.Max(1, (Player.statLifeMax2 + 9) / 10);
                if (Main.netMode == NetmodeID.Server)
                {
                    // Player health is owned by its client in Terraria. The server chooses the proc
                    // and damage; the victim applies this sequenced instruction after its original hit,
                    // then sends PlayerLifeMana. Sending a stale absolute HP snapshot can undo that hit.
                    Player.statLife = Math.Max(0, Player.statLife - triggerDamage);
                    // SyncState carries the buff application too; an extra AddPlayerBuff packet
                    // would apply vanilla's Expert extension before the exact-duration helper.
                }
                else
                {
                    ApplyTriggerDamage(triggerDamage);
                    PlayTriggerEffects(Player, triggerDamage);
                }
            }
            else Player.AddBuff(ModContent.BuffType<FrostBuildup>(), FrostBuildup.MarkerDurationTicks);
            SyncState(triggerDamage);
        }
        private void ApplyTriggerDamage(int damage)
        {
            if (Player.dead) return;
            Player.statLife -= damage;
            if (Player.statLife <= 0)
                Player.KillMe(PlayerDeathReason.ByCustomReason(NetworkText.FromKey(
                    "Mods.tsorcRevamp.DeathText.FrostDeathReason", Player.name)), damage, 0, false);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, Player.whoAmI);
        }
        private static void ApplyFrozen(Player player)
        {
            int index = player.FindBuffIndex(BuffID.Frozen);
            int existingTime = index >= 0 ? player.buffTime[index] : 0;
            player.AddBuff(BuffID.Frozen, Frost.FrozenDurationTicks);
            index = player.FindBuffIndex(BuffID.Frozen);
            // Frozen is LongerExpertDebuff in vanilla: retain unrelated longer freezes, but
            // this proc contributes exactly120 ticks even in Expert/Master.
            if (index >= 0) player.buffTime[index] = Math.Max(existingTime, Frost.FrozenDurationTicks);
        }
        private void SyncState(int triggerDamage = 0, int toWho = -1, int fromWho = -1)
        {
            if (Main.netMode != NetmodeID.Server) return;
            ModPacket packet = Mod.GetPacket();
            packet.Write(tsorcPacketID.SyncFrostState);
            packet.Write((byte)Player.whoAmI); packet.Write((byte)FrostLevel); packet.Write((byte)_decayTimer);
            packet.Write(_procSequence); packet.Write(triggerDamage);
            packet.Send(toWho, fromWho);
        }
        internal void ReceiveState(int buildup, int decayTimer, int procSequence, int triggerDamage)
        {
            FrostLevel = Math.Clamp(buildup, 0, FrostBuildup.MaximumBuildup);
            _decayTimer = Math.Clamp(decayTimer, 0, FrostBuildup.DecayIntervalTicks - 1);
            if (FrostLevel == 0) Player.ClearBuff(ModContent.BuffType<FrostBuildup>());
            else Player.AddBuff(ModContent.BuffType<FrostBuildup>(), FrostBuildup.MarkerDurationTicks);
            if (triggerDamage > 0 && procSequence > _lastResolvedProc)
            {
                _lastResolvedProc = procSequence;
                if (!Player.dead)
                {
                    ApplyFrozen(Player);
                    Player.AddBuff(ModContent.BuffType<Frost>(), Frost.DurationTicks);
                    if (Player.whoAmI == Main.myPlayer) ApplyTriggerDamage(triggerDamage);
                    if (!Main.dedServ) PlayTriggerEffects(Player, triggerDamage);
                }
            }
            _lastResolvedProc = Math.Max(_lastResolvedProc, procSequence);
        }
        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) => SyncState(0, toWho, fromWho);
        public override void PostUpdateBuffs()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (FrostLevel <= 0) { _decayTimer = 0; return; }
            if (!Player.HasBuff(ModContent.BuffType<FrostBuildup>()))
            { FrostLevel = 0; _decayTimer = 0; SyncState(); return; }
            if (++_decayTimer >= FrostBuildup.DecayIntervalTicks)
            {
                FrostLevel = Math.Max(0, FrostLevel - FrostBuildup.DecayPerInterval); _decayTimer = 0;
                if (FrostLevel == 0) Player.ClearBuff(ModContent.BuffType<FrostBuildup>());
                SyncState();
            }
        }
        public override void UpdateDead()
        {
            bool changed = FrostLevel != 0;
            FrostLevel = 0; _decayTimer = 0;
            // Keep proc sequences across respawns so old/duplicate trigger snapshots cannot deal damage again.
            if (changed) SyncState();
        }
        // Called at the end of the legacy mobility hook, after its boot speed assignments.
        internal void ApplyMovementSlow()
        {
            float multiplier = 1f - MovementSlowFraction;
            Player.maxRunSpeed *= multiplier;
            Player.accRunSpeed *= multiplier;
            Player.runAcceleration *= multiplier;
        }
        private static void PlayTriggerEffects(Player player, int damage)
        {
            if (Main.dedServ) return;
            CombatText.NewText(player.Hitbox, Color.LightBlue, damage);
            SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.8f, Pitch = -0.4f }, player.Center);
            for (int i = 0; i < 55; i++)
            {
                Dust dust = Dust.NewDustPerfect(player.Center, i % 3 == 0 ? DustID.BlueCrystalShard : DustID.Frost,
                    Main.rand.NextVector2Circular(5, 5), 70, Color.LightCyan, Main.rand.NextFloat(0.6f, 1.5f));
                dust.noGravity = i % 3 != 0;
            }
        }
    }
    internal class FrostMeterDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.FrontAccFront);
        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
            => !Main.gameMenu && !drawInfo.drawPlayer.dead && drawInfo.drawPlayer.GetModPlayer<FrostPlayer>().FrostLevel > 0;
        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            float percentage = MathHelper.Clamp(player.GetModPlayer<FrostPlayer>().FrostLevel / 100f, 0, 1);
            Texture2D empty = ModContent.Request<Texture2D>("tsorcRevamp/Textures/StatusMeter_empty").Value;
            Texture2D full = ModContent.Request<Texture2D>("tsorcRevamp/Textures/StatusMeter_full").Value;
            tsorcRevampPlayer legacy = player.GetModPlayer<tsorcRevampPlayer>();
            int precedingMeters = (legacy.CurseLevel > 1 || legacy.PowerfulCurseLevel > 1 ? 1 : 0)
                + (player.GetModPlayer<MadnessPlayer>().MadnessLevel > 0 ? 1 : 0);
            Point origin = (player.Center - new Vector2(empty.Width / 2f, 82f + precedingMeters * 42f) - Main.screenPosition).ToPoint();
            Main.spriteBatch.Draw(empty, new Rectangle(origin.X, origin.Y, empty.Width, empty.Height), Color.White);
            int fillHeight = Math.Max(1, (int)Math.Round(full.Height * percentage));
            int sourceY = full.Height - fillHeight;
            Main.spriteBatch.Draw(full, new Rectangle(origin.X, origin.Y + sourceY, full.Width, fillHeight),
                new Rectangle(0, sourceY, full.Width, fillHeight), new Color(75, 175, 255));
        }
    }
}
