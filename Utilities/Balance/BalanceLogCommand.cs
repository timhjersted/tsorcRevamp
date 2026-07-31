using Microsoft.Xna.Framework;
using System.IO;
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
    ///   /balancelog path         → print the full path to the file, for uploading
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

                case "path":
                    caller.Reply(BalanceLog.LogPath, Color.LightBlue);
                    break;

                case "clear":
                    Clear(caller);
                    break;

                case "status":
                    ReplyStatus(caller);
                    break;

                default:
                    caller.Reply("Usage: /balancelog [on|off|path|clear|status]", Color.Orange);
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

            caller.Reply($"File: Logs/{BalanceLog.FileName}  (/balancelog path for the full path)", Color.LightBlue);
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
                        $"tsorcRevamp-balance-{System.DateTimeOffset.Now:yyyyMMdd-HHmmss}.jsonl");
                    File.Move(path, archived);
                    caller.Reply($"Archived previous log to {Path.GetFileName(archived)}.", Color.Lime);
                }
                else
                {
                    caller.Reply("No log file to archive yet.", Color.Gray);
                }
            }
            catch (System.Exception e)
            {
                caller.Reply($"Failed to archive log: {e.Message}", Color.Red);
            }
        }
    }
}
