using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Projectiles.VFX;

namespace tsorcRevamp
{
    public class BlightPlayer : ModPlayer
    {
        public int BlightLevel { get; private set; }
        private int decayTimer, procSequence, lastResolvedProc;
        private const int PostDeathDisplayTicks = 6 * 60;
        private int postDeathDisplayTicks;
        private bool deathDisplayStarted;

        public void RequestBuildup(int buildup)
        {
            buildup = Math.Clamp(buildup, 1, BlightBuildup.MaximumBuildup);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                if (Player.whoAmI != Main.myPlayer) return;
                ModPacket packet = Mod.GetPacket();
                packet.Write(tsorcPacketID.ReportBlightBuildup);
                packet.Write((byte)buildup);
                packet.Send();
                return;
            }
            ApplyBuildupAuthoritative(buildup);
        }

        internal void ApplyBuildupAuthoritative(int buildup)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !Player.active || Player.dead
                || Player.buffImmune[ModContent.BuffType<BlightBuildup>()]) return;

            BlightLevel = Math.Clamp(BlightLevel + Math.Clamp(buildup, 1, BlightBuildup.MaximumBuildup),
                0, BlightBuildup.MaximumBuildup);
            int triggerDamage = 0;
            if (BlightLevel >= BlightBuildup.MaximumBuildup)
            {
                BlightLevel = 0;
                decayTimer = 0;
                procSequence++;
                Player.ClearBuff(ModContent.BuffType<BlightBuildup>());
                Player.AddBuff(ModContent.BuffType<Blight>(), Blight.DurationTicks);
                triggerDamage = Math.Max(1, Player.statLifeMax2 * 2);

                // The dust-only projectile keeps the eight tendrils visible after the fatal hit.
                Projectile.NewProjectile(Player.GetSource_Misc("BlightProc"), Player.Center, Vector2.Zero,
                    ModContent.ProjectileType<BlightTendrils>(), 0, 0f, Main.myPlayer);

                if (Main.netMode == NetmodeID.Server)
                    Player.statLife = Math.Max(0, Player.statLife - triggerDamage);
                else
                {
                    ApplyTriggerDamage(triggerDamage);
                    PlayTriggerEffects(Player, triggerDamage);
                }
            }
            else Player.AddBuff(ModContent.BuffType<BlightBuildup>(), BlightBuildup.MarkerDurationTicks);

            SyncState(triggerDamage);
        }

        private void ApplyTriggerDamage(int damage)
        {
            if (Player.dead) return;
            Player.statLife -= damage;
            if (Player.statLife <= 0)
                Player.KillMe(PlayerDeathReason.ByCustomReason(NetworkText.FromKey(
                    "Mods.tsorcRevamp.DeathText.BlightDeathReason", Player.name)), damage, 0, false);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, Player.whoAmI);
        }

        private void SyncState(int triggerDamage = 0, int toWho = -1, int fromWho = -1)
        {
            if (Main.netMode != NetmodeID.Server) return;
            ModPacket packet = Mod.GetPacket();
            packet.Write(tsorcPacketID.SyncBlightState);
            packet.Write((byte)Player.whoAmI);
            packet.Write((byte)BlightLevel);
            packet.Write((byte)decayTimer);
            packet.Write(procSequence);
            packet.Write(triggerDamage);
            packet.Send(toWho, fromWho);
        }

        internal void ReceiveState(int buildup, int decayPhase, int sequence, int triggerDamage)
        {
            BlightLevel = Math.Clamp(buildup, 0, BlightBuildup.MaximumBuildup);
            decayTimer = Math.Clamp(decayPhase, 0, BlightBuildup.DecayIntervalTicks - 1);
            if (BlightLevel == 0) Player.ClearBuff(ModContent.BuffType<BlightBuildup>());
            else Player.AddBuff(ModContent.BuffType<BlightBuildup>(), BlightBuildup.MarkerDurationTicks);

            if (triggerDamage > 0 && sequence > lastResolvedProc)
            {
                lastResolvedProc = sequence;
                // The harmless icon must also arrive when vanilla death synced before this packet.
                Player.AddBuff(ModContent.BuffType<Blight>(), Blight.DurationTicks);
                if (Player.dead)
                {
                    deathDisplayStarted = true;
                    postDeathDisplayTicks = PostDeathDisplayTicks;
                }
                if (!Player.dead)
                {
                    if (Player.whoAmI == Main.myPlayer) ApplyTriggerDamage(triggerDamage);
                    if (!Main.dedServ) PlayTriggerEffects(Player, triggerDamage);
                }
            }
            lastResolvedProc = Math.Max(lastResolvedProc, sequence);
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) => SyncState(0, toWho, fromWho);

        public override void PostUpdateBuffs()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (BlightLevel <= 0) { decayTimer = 0; return; }
            if (!Player.HasBuff(ModContent.BuffType<BlightBuildup>()))
            { BlightLevel = 0; decayTimer = 0; SyncState(); return; }
            if (++decayTimer >= BlightBuildup.DecayIntervalTicks)
            {
                BlightLevel = Math.Max(0, BlightLevel - 1);
                decayTimer = 0;
                if (BlightLevel == 0) Player.ClearBuff(ModContent.BuffType<BlightBuildup>());
                SyncState();
            }
        }

        public override void UpdateDead()
        {
            int blightType = ModContent.BuffType<Blight>();
            if (!deathDisplayStarted)
            {
                deathDisplayStarted = true;
                if (Player.HasBuff(blightType)) postDeathDisplayTicks = PostDeathDisplayTicks;
            }
            if (postDeathDisplayTicks > 0)
            {
                int index = Player.FindBuffIndex(blightType);
                if (index < 0)
                {
                    Player.AddBuff(blightType, postDeathDisplayTicks);
                    index = Player.FindBuffIndex(blightType);
                }
                if (index >= 0) Player.buffTime[index] = postDeathDisplayTicks;
                postDeathDisplayTicks--;
            }
            else Player.ClearBuff(blightType);

            bool changed = BlightLevel != 0;
            BlightLevel = 0;
            decayTimer = 0;
            // Preserve proc sequence history across death to reject delayed duplicate packets.
            if (changed) SyncState();
        }

        public override void PostUpdate()
        {
            if (!Player.dead && deathDisplayStarted)
            {
                deathDisplayStarted = false;
                postDeathDisplayTicks = 0;
                Player.ClearBuff(ModContent.BuffType<Blight>());
            }
            if (Main.dedServ || Player.dead || (BlightLevel <= 0 && !Player.HasBuff(ModContent.BuffType<Blight>()))) return;
            if (Main.GameUpdateCount % 3 != 0) return;
            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2Circular(12f, 20f),
                    DustID.Wraith, Main.rand.NextVector2Circular(0.25f, 0.3f), 135,
                    new Color(65, 55, 80), Main.rand.NextFloat(0.65f, 1.05f));
                dust.noGravity = true;
            }
        }

        public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
        {
            if (BlightLevel <= 0 && !Player.HasBuff(ModContent.BuffType<Blight>())) return;
            // Override only draw-time shader IDs; never touch the player's equipped dye items.
            int shader = GameShaders.Armor.GetShaderIdFromItemId(ItemID.ReflectiveObsidianDye);
            drawInfo.cHead = shader;
            drawInfo.cBody = shader;
            drawInfo.cLegs = shader;
            drawInfo.cHandOn = shader;
            drawInfo.cHandOff = shader;
            drawInfo.cShoe = shader;
            drawInfo.cWaist = shader;
        }

        private static void PlayTriggerEffects(Player player, int damage)
        {
            if (Main.dedServ) return;
            CombatText.NewText(player.Hitbox, new Color(155, 115, 175), damage);
            SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.85f, Pitch = -0.45f }, player.Center);
        }
    }

    internal class BlightMeterDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.FrontAccFront);
        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
            => !Main.gameMenu && !drawInfo.drawPlayer.dead && drawInfo.drawPlayer.GetModPlayer<BlightPlayer>().BlightLevel > 0;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            Texture2D empty = ModContent.Request<Texture2D>("tsorcRevamp/Textures/StatusMeter_empty_blight").Value;
            Texture2D full = ModContent.Request<Texture2D>("tsorcRevamp/Textures/StatusMeter_full").Value;
            tsorcRevampPlayer legacy = player.GetModPlayer<tsorcRevampPlayer>();
            int preceding = (legacy.CurseLevel > 1 || legacy.PowerfulCurseLevel > 1 ? 1 : 0)
                + (player.GetModPlayer<MadnessPlayer>().MadnessLevel > 0 ? 1 : 0)
                + (player.GetModPlayer<FrostPlayer>().FrostLevel > 0 ? 1 : 0);
            Point origin = (player.Center - new Vector2(empty.Width / 2f, 82f + preceding * 42f) - Main.screenPosition).ToPoint();
            Main.spriteBatch.Draw(empty, new Rectangle(origin.X, origin.Y, empty.Width, empty.Height), Color.White);
            int fillHeight = Math.Max(1, (int)Math.Round(full.Height * player.GetModPlayer<BlightPlayer>().BlightLevel
                / (float)BlightBuildup.MaximumBuildup));
            int sourceY = full.Height - fillHeight;
            Rectangle source = new Rectangle(0, sourceY, full.Width, fillHeight);
            Main.spriteBatch.Draw(full, new Rectangle(origin.X, origin.Y + sourceY, full.Width, fillHeight),
                source, new Color(40, 40, 45));
        }
    }
}
