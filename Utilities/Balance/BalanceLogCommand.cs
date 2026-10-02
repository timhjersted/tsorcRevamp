using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Utilities.Balance
{
    /// <summary>
    /// Player-facing control for the boss balance log.
    ///
    /// Usage:
    ///   /balancelog              → status: on/off, encounters recorded, file location
    ///   /balancelog on | off     → enable/disable recording
    ///   /balancelog id           → this character's log tag (it is in the file name)
    ///   /balancelog path         → print the full path to the file, for uploading
    ///   /balancelog zip          → pack this character's log files into one .zip for uploading
    ///   /balancelog clear        → archive the current file and start a fresh one
    /// </summary>
    public class BalanceLogCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;

        public override string Command => "balancelog";

        public override string Description => "Control the boss balance/DPS log used for mod tuning";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            string sub = args.Length > 0 ? args[0].ToLowerInvariant() : "status";

            switch (sub)
            {
                case "on":
                    BalanceLog.Enabled = true;
                    ReplyStatus(caller);
                    break;

                case "off":
                    BalanceLog.Enabled = false;
                    ReplyStatus(caller);
                    break;

                case "id":
                    caller.Reply($"This character's log tag is {BalanceLog.CharacterTag} (file: {BalanceLog.FileName}).", Color.LightBlue);
                    break;

                case "path":
                    caller.Reply(BalanceLog.LogPath, Color.LightBlue);
                    break;

                case "zip":
                    Zip(caller);
                    break;

                case "clear":
                    Clear(caller);
                    break;

                case "status":
                    ReplyStatus(caller);
                    break;

                default:
                    caller.Reply("Usage: /balancelog [on|off|id|path|zip|clear|status]", Color.Orange);
                    break;
            }
        }

        private static void ReplyStatus(CommandCaller caller)
        {
            caller.Reply(
                $"Boss balance log: {(BalanceLog.Enabled ? "ON" : "OFF")} " +
                $"({BalanceLog.EncountersThisSession} encounter(s) recorded this session)",
                BalanceLog.Enabled ? Color.Lime : Color.Gray);

            if (BalanceLog.EncounterActive)
                caller.Reply($"Currently recording: {BalanceLog.ActiveBossName}", Color.Yellow);

            if (Main.netMode != Terraria.ID.NetmodeID.SinglePlayer)
            {
                caller.Reply(
                    "Note: recording is single-player only — per-weapon damage attribution isn't reliable in multiplayer.",
                    Color.Orange);
            }

            caller.Reply($"File: Logs/{BalanceLog.FileName}  (/balancelog path for the full path, /balancelog zip to pack it for upload)", Color.LightBlue);
        }

        /// <summary>
        /// Packs this character's balance files (the live file plus any rolled or archived ones, which share its name
        /// as a prefix), the weapon bench file and the character's whole kill table into one zip next to them. Log text
        /// compresses about tenfold.
        /// </summary>
        private static void Zip(CommandCaller caller)
        {
            try
            {
                string directory = BalanceLog.LogDirectory;
                string stem = Path.GetFileNameWithoutExtension(BalanceLog.FileName);
                string zipPath = Path.Combine(directory, stem + ".zip");

                var filesToPack = Directory.GetFiles(directory, stem + "*.jsonl").ToList();

                string benchPath = Path.Combine(directory, WeaponBench.FileName);
                if (File.Exists(benchPath))
                {
                    filesToPack.Add(benchPath);
                }

                var killStats = caller.Player.GetModPlayer<EnemyKillStatsPlayer>().Stats;

                if (filesToPack.Count == 0 && killStats.Count == 0)
                {
                    caller.Reply("Nothing to zip yet: no log file or kill statistics for this character.", Color.Gray);
                    return;
                }

                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }

                using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    foreach (string file in filesToPack)
                    {
                        ZipArchiveEntry entry = archive.CreateEntry(Path.GetFileName(file), CompressionLevel.Optimal);

                        // Shared read, so a file the logger has open does not block the zip.
                        using FileStream source = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        using Stream destination = entry.Open();
                        source.CopyTo(destination);
                    }

                    // Every enemy kill's count and time-to-kill histogram. The log file only holds the sampled kills.
                    ZipArchiveEntry summaryEntry = archive.CreateEntry(stem + "-killstats.json", CompressionLevel.Optimal);
                    using StreamWriter summaryWriter = new StreamWriter(summaryEntry.Open());
                    summaryWriter.Write(EnemyKillLog.BuildSummaryJson(caller.Player));
                }

                long zipBytes = new FileInfo(zipPath).Length;
                caller.Reply($"Packed {filesToPack.Count} log file(s) plus the kill statistics into {zipBytes / 1024} KB:", Color.Lime);
                caller.Reply(zipPath, Color.LightBlue);
            }
            catch (Exception e)
            {
                caller.Reply($"Failed to zip the log: {e.Message}", Color.Red);
            }
        }

        private static void Clear(CommandCaller caller)
        {
            try
            {
                string path = BalanceLog.LogPath;
                if (File.Exists(path))
                {
                    // Archive rather than delete — this is the only copy of data that can take weeks to collect.
                    string archived = Path.Combine(
                        Path.GetDirectoryName(path),
                        $"{Path.GetFileNameWithoutExtension(path)}-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.jsonl");
                    File.Move(path, archived);
                    caller.Reply($"Archived previous log to {Path.GetFileName(archived)}.", Color.Lime);
                }
                else
                {
                    caller.Reply("No log file to archive yet.", Color.Gray);
                }
            }
            catch (Exception e)
            {
                caller.Reply($"Failed to archive log: {e.Message}", Color.Red);
            }
        }
    }
}
