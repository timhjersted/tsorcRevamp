using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Achievements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.NPCs.Bosses.SuperHardMode;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.GhostWyvernMage;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Seath;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Systems
{
    /// <summary>
    /// The world owns the Abyss; equipment only protects against its curse. Entry consumes eight real
    /// seconds without moving the clock. Thereafter each distinct required boss brings the frozen night
    /// closer to dawn, independently of the twelve-boss difficulty ramp in ProgressionScaling.
    /// </summary>
    public class AbyssWorldSystem : ModSystem
    {
        public const int EntryDurationTicks = 8 * 60;
        public const int DawnStepDurationTicks = 6 * 60;
        public const int NightCycleTicks = 9 * 60 * 60;
        public const int RequiredBossCount = 6;

        // Seconds since 7:30 PM: midnight, 1 AM, 2 AM, 3 AM, 3:45 AM, 4:20 AM, 4:29 AM.
        private static readonly double[] NightTimes = { 16200, 19800, 23400, 27000, 29700, 31800, 32340 };
        private static int[] requiredBossTypes = Array.Empty<int>();
        private static int entryTicksRemaining;
        private static float visualStage = -1f;
        private static int nightCycleTicks;
        private static int moonEventTicks;
        private static bool wasActive;
        private static bool worldLoaded;

        public static bool Active => worldLoaded && tsorcRevampWorld.SuperHardMode
            && !tsorcRevampWorld.BossDefeated(ModContent.NPCType<Gwyn>());
        public static bool CurseActive => Active && entryTicksRemaining == 0;
        public static float EntryProgress => Active
            ? MathHelper.SmoothStep(0f, 1f, 1f - entryTicksRemaining / (float)EntryDurationTicks) : 0f;
        public static float Darkness => Main.gameMenu ? 0f
            : EntryProgress * (1f - 0.15f * Math.Max(0f, visualStage));

        public static bool IsRequiredBoss(int npcType) => Array.IndexOf(requiredBossTypes, npcType) >= 0;
        public static int RequiredBossesDowned
        {
            get
            {
                int count = 0;
                foreach (int bossType in requiredBossTypes)
                    if (tsorcRevampWorld.BossDefeated(bossType)) count++;
                return count;
            }
        }

        public static double FixedNightTime
        {
            get
            {
                float stage = MathHelper.Clamp(visualStage, 0f, RequiredBossCount);
                int index = Math.Min((int)stage, RequiredBossCount - 1);
                return NightTimes[index] + (NightTimes[index + 1] - NightTimes[index]) * (stage - index);
            }
        }

        public override void OnWorldLoad()
        {
            // Resolve content IDs only after all NPC content has loaded, never in a static initializer.
            requiredBossTypes = new[]
            {
                ModContent.NPCType<WaterFiendKraken>(), ModContent.NPCType<FireFiendMarilith>(),
                ModContent.NPCType<EarthFiendLich>(), ModContent.NPCType<WyvernMageShadow>(),
                ModContent.NPCType<Artorias>(), ModContent.NPCType<SeathTheScalelessHead>()
            };
            ResetState();
            worldLoaded = true;
        }

        public override void OnWorldUnload()
        {
            ResetState();
            worldLoaded = false;
            requiredBossTypes = Array.Empty<int>();
        }

        private static void ResetState()
        {
            entryTicksRemaining = 0;
            visualStage = -1f;
            nightCycleTicks = 0;
            moonEventTicks = 0;
            wasActive = false;
        }

        public static void BeginEntry()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            entryTicksRemaining = EntryDurationTicks;
            visualStage = RequiredBossesDowned;
            nightCycleTicks = 0;
            moonEventTicks = 0;
        }

        public static void RestoreDawn()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            ResetState();
            ClearEnvironmentalCurse();
            Main.dayTime = true;
            Main.time = 0;
            Main.bloodMoon = false;
            Main.stopMoonEvent();
            SyncWorld();
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag["EntryTicksRemaining"] = entryTicksRemaining;
            tag["VisualStage"] = visualStage;
            tag["NightCycleTicks"] = nightCycleTicks;
            tag["MoonEventTicks"] = moonEventTicks;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            entryTicksRemaining = Math.Clamp(tag.GetInt("EntryTicksRemaining"), 0, EntryDurationTicks);
            visualStage = tag.ContainsKey("VisualStage") ? MathHelper.Clamp(tag.GetFloat("VisualStage"), -1f, RequiredBossCount) : -1f;
            nightCycleTicks = Math.Clamp(tag.GetInt("NightCycleTicks"), 0, NightCycleTicks);
            moonEventTicks = Math.Clamp(tag.GetInt("MoonEventTicks"), 0, NightCycleTicks);
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write((short)entryTicksRemaining);
            writer.Write(visualStage);
            writer.Write(nightCycleTicks);
            writer.Write(moonEventTicks);
        }

        public override void NetReceive(BinaryReader reader)
        {
            entryTicksRemaining = Math.Clamp((int)reader.ReadInt16(), 0, EntryDurationTicks);
            visualStage = MathHelper.Clamp(reader.ReadSingle(), -1f, RequiredBossCount);
            nightCycleTicks = Math.Clamp(reader.ReadInt32(), 0, NightCycleTicks);
            moonEventTicks = Math.Clamp(reader.ReadInt32(), 0, NightCycleTicks);
        }

        public override void ModifyTimeRate(ref double timeRate, ref double tileUpdateRate, ref double eventUpdateRate)
        {
            // Freeze only the clock. Weather, spawning and tile updates still run normally.
            if (!Main.gameMenu && CurseActive) timeRate = 0;
        }

        public override void PreUpdateTime() => PinClock();
        public override void PostUpdateTime() => PinClock();

        private static void PinClock()
        {
            if (Main.gameMenu || !CurseActive) return;
            Main.dayTime = false;
            Main.time = FixedNightTime;
            Main.moonPhase = 0;
        }

        public override void PostUpdateEverything()
        {
            // Save-and-quit can enter the menu before world saving finishes. Preserve the saved transition;
            // OnWorldUnload, not the menu flag, owns resetting it.
            if (Main.gameMenu) return;
            if (!Active)
            {
                if (wasActive) ClearEnvironmentalCurse();
                ResetState();
                return;
            }

            wasActive = true;
            int targetStage = RequiredBossesDowned;
            if (visualStage < 0f) visualStage = targetStage; // Existing SHM saves join at their earned stage.
            bool moving = Math.Abs(visualStage - targetStage) > 0.0001f;
            visualStage += MathHelper.Clamp(targetStage - visualStage, -1f / DawnStepDurationTicks, 1f / DawnStepDurationTicks);

            if (entryTicksRemaining > 0)
            {
                entryTicksRemaining--;
                if (entryTicksRemaining == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Attraidies.Hint"), new Color(249, 202, 12));
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Attraidies.MartianHint"), new Color(12, 200, 255));
                    PinClock();
                    SyncWorld();
                }
            }

            PinClock();
            if (CurseActive && Main.netMode != NetmodeID.MultiplayerClient) UpdateMoonEvents();

            // Clients predict cosmetic timers from the server snapshot. Periodic correction also supports late joins.
            if (((entryTicksRemaining > 0 || moving) && Main.GameUpdateCount % 60 == 0) || entryTicksRemaining == 1)
                SyncWorld();
        }

        private static void ClearEnvironmentalCurse()
        {
            int buffType = ModContent.BuffType<Abyss>();
            foreach (Player player in Main.ActivePlayers)
            {
                int index = player.FindBuffIndex(buffType);
                // Preserve longer, separately inflicted boss/enemy curses.
                if (index >= 0 && player.buffTime[index] <= 60) player.ClearBuff(buffType);
            }
        }

        private static void UpdateMoonEvents()
        {
            bool moonEvent = Main.bloodMoon || Main.pumpkinMoon || Main.snowMoon;
            moonEventTicks = moonEvent ? moonEventTicks + 1 : 0;
            // Absolute Death owns its Blood Moon during the fight; end the event once it releases that ownership.
            if (moonEventTicks >= NightCycleTicks && !NPC.AnyNPCs(ModContent.NPCType<NPCs.Special.TrueDeath>()))
            {
                Main.bloodMoon = false;
                Main.stopMoonEvent();
                moonEventTicks = 0;
                nightCycleTicks = 0;
                SyncWorld();
                return;
            }

            if (++nightCycleTicks < NightCycleTicks) return;
            nightCycleTicks = 0;
            if (moonEvent || Main.invasionType != 0 || WorldGen.spawnEye) return;
            if (!Main.rand.NextBool(Main.tenthAnniversaryWorld ? 6 : 9)) return;
            foreach (Player player in Main.ActivePlayers)
            {
                if (player.ConsumedLifeCrystals <= 1) continue;
                Main.bloodMoon = true;
                Main.sundialCooldown = 0;
                Main.moondialCooldown = 0;
                AchievementsHelper.NotifyProgressionEvent(4);
                UsefulFunctions.BroadcastText(Lang.misc[8].Value, new Color(50, 255, 130));
                SyncWorld();
                break;
            }
        }

        public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
        {
            // Modify sky illumination only: torches, spells and other local lights retain their normal strength.
            float darkness = Darkness;
            tileColor = Color.Lerp(tileColor, new Color(18, 20, 30), darkness);
            backgroundColor = Color.Lerp(backgroundColor, new Color(3, 4, 10), darkness);
        }

        private static void SyncWorld()
        {
            if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.WorldData);
        }
    }
}
