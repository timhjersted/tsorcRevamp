using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Puppets;

namespace tsorcRevamp.Utilities
{
    /// <summary>
    /// Live A/B + tuning switch for the experimental composite-arm puppet swing.
    /// Only puppets that opt in via <c>UseCompositeArmSwing</c> (currently just
    /// StuddedLeatherWarrior) are affected — this command flips the global master enable
    /// and tunes the shared rotation offset / arm stretch without a rebuild.
    ///
    /// Usage:
    ///   /swingarm                       → toggle the composite-arm path on/off
    ///   /swingarm on | off              → force it on/off
    ///   /swingarm offset &lt;radians&gt;       → set the arm rotation offset (e.g. 0.15, -0.3)
    ///   /swingarm stretch none|quarter|threequarters|full
    ///   /swingarm flip [on|off]         → toggle the blade-leads-the-swing mirror (single-bladed weapons)
    ///   /swingarm aim [on|off]          → toggle full-360 aim-centered swing (axe archetype)
    ///   /swingarm log clear             → clear the structured attack and older puppet debug logs
    ///   /swingarm status                → print current values
    /// </summary>
    public class SwingArmCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;

        public override string Command => "swingarm";

        public override string Description => "Toggle/tune the experimental composite-arm puppet swing";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length == 0)
            {
                PuppetNPC.CompositeArmSwingMasterEnable = !PuppetNPC.CompositeArmSwingMasterEnable;
                ReplyStatus(caller);
                return;
            }

            switch (args[0].ToLowerInvariant())
            {
                case "on":
                    PuppetNPC.CompositeArmSwingMasterEnable = true;
                    ReplyStatus(caller);
                    break;

                case "off":
                    PuppetNPC.CompositeArmSwingMasterEnable = false;
                    ReplyStatus(caller);
                    break;

                case "offset":
                    if (args.Length >= 2 && float.TryParse(args[1], out float off))
                    {
                        PuppetNPC.CompositeArmRotationOffset = off;
                        ReplyStatus(caller);
                    }
                    else
                    {
                        caller.Reply("Usage: /swingarm offset <radians>  (e.g. 0.15)", Color.Orange);
                    }
                    break;

                case "stretch":
                    if (args.Length >= 2 && TryParseStretch(args[1], out Player.CompositeArmStretchAmount stretch))
                    {
                        PuppetNPC.CompositeArmStretch = stretch;
                        ReplyStatus(caller);
                    }
                    else
                    {
                        caller.Reply("Usage: /swingarm stretch none|quarter|threequarters|full", Color.Orange);
                    }
                    break;

                case "flip":
                    if (args.Length >= 2 && args[1].ToLowerInvariant() == "on")
                        PuppetNPC.BladeFlipMasterEnable = true;
                    else if (args.Length >= 2 && args[1].ToLowerInvariant() == "off")
                        PuppetNPC.BladeFlipMasterEnable = false;
                    else
                        PuppetNPC.BladeFlipMasterEnable = !PuppetNPC.BladeFlipMasterEnable;
                    caller.Reply(
                        $"Blade-flip mirror: {(PuppetNPC.BladeFlipMasterEnable ? "ON" : "OFF")}",
                        PuppetNPC.BladeFlipMasterEnable ? Color.Lime : Color.Gray);
                    break;

                case "aim":
                    if (args.Length >= 2 && args[1].ToLowerInvariant() == "on")
                        PuppetNPC.AimSwingMasterEnable = true;
                    else if (args.Length >= 2 && args[1].ToLowerInvariant() == "off")
                        PuppetNPC.AimSwingMasterEnable = false;
                    else
                        PuppetNPC.AimSwingMasterEnable = !PuppetNPC.AimSwingMasterEnable;
                    caller.Reply(
                        $"Aim-centered swing: {(PuppetNPC.AimSwingMasterEnable ? "ON (full 360 aim)" : "OFF (legacy fixed arc)")}",
                        PuppetNPC.AimSwingMasterEnable ? Color.Lime : Color.Gray);
                    break;

                case "log":
                    if (args.Length >= 2 && args[1].ToLowerInvariant() == "clear")
                    {
                        ClearLog(caller);
                        break;
                    }
                    bool wasLogging = PuppetNPC.SwingDebugLog;
                    if (args.Length >= 2 && args[1].ToLowerInvariant() == "on")
                        PuppetNPC.SwingDebugLog = true;
                    else if (args.Length >= 2 && args[1].ToLowerInvariant() == "off")
                        PuppetNPC.SwingDebugLog = false;
                    else
                        PuppetNPC.SwingDebugLog = !PuppetNPC.SwingDebugLog;
                    if (wasLogging && !PuppetNPC.SwingDebugLog)
                        PuppetAttackTelemetry.StopAll("logging-disabled");
                    caller.Reply(
                        $"Puppet attack telemetry: {(PuppetNPC.SwingDebugLog ? "ON" : "OFF")} " +
                        $"(Logs/{PuppetAttackTelemetry.FileName})",
                        PuppetNPC.SwingDebugLog ? Color.Lime : Color.Gray);
                    break;

                case "status":
                    ReplyFullStatus(caller);
                    break;

                default:
                    caller.Reply("Usage: /swingarm [on|off|offset <radians>|stretch <amount>|flip [on|off]|aim [on|off]|log [on|off|clear]|status]", Color.Orange);
                    break;
            }
        }

        /// <summary>Single-line reply for on/off/offset/stretch so the state you just changed is the
        /// ONLY line printed — a full status dump after "/swingarm off" buried the OFF line under
        /// still-ON flip/aim lines and read as if the command hadn't worked.</summary>
        private static void ReplyStatus(CommandCaller caller)
        {
            Color c = PuppetNPC.CompositeArmSwingMasterEnable ? Color.Lime : Color.Gray;
            caller.Reply(
                $"Composite-arm swing: {(PuppetNPC.CompositeArmSwingMasterEnable ? "ON (new)" : "OFF (legacy 4-frame)")} | " +
                $"offset={PuppetNPC.CompositeArmRotationOffset:0.###} rad | stretch={PuppetNPC.CompositeArmStretch}",
                c);
        }

        /// <summary>Full three-line dump, printed only for the explicit "/swingarm status".</summary>
        private static void ReplyFullStatus(CommandCaller caller)
        {
            ReplyStatus(caller);
            caller.Reply(
                $"Blade-flip mirror: {(PuppetNPC.BladeFlipMasterEnable ? "ON" : "OFF")}",
                PuppetNPC.BladeFlipMasterEnable ? Color.Lime : Color.Gray);
            caller.Reply(
                $"Aim-centered swing: {(PuppetNPC.AimSwingMasterEnable ? "ON (full 360 aim)" : "OFF (legacy fixed arc)")}",
                PuppetNPC.AimSwingMasterEnable ? Color.Lime : Color.Gray);
            caller.Reply(
                $"Puppet attack telemetry: {(PuppetNPC.SwingDebugLog ? "ON" : "OFF")} (Logs/{PuppetAttackTelemetry.FileName})",
                PuppetNPC.SwingDebugLog ? Color.Lime : Color.Gray);
        }

        private static void ClearLog(CommandCaller caller)
        {
            try
            {
                string logDir = System.IO.Path.Combine(Main.SavePath, "Logs");
                System.IO.Directory.CreateDirectory(logDir);
                PuppetAttackTelemetry.Clear();
                System.IO.File.WriteAllText(System.IO.Path.Combine(logDir, "tsorcRevamp-puppet-swing.log"), string.Empty);
                System.IO.File.WriteAllText(System.IO.Path.Combine(logDir, "tsorcRevamp-puppet-weapon.log"), string.Empty);
                caller.Reply($"Puppet telemetry cleared ({PuppetAttackTelemetry.FileName}).", Color.Lime);
            }
            catch (System.Exception e)
            {
                caller.Reply($"Failed to clear log: {e.Message}", Color.Red);
            }
        }

        private static bool TryParseStretch(string s, out Player.CompositeArmStretchAmount stretch)
        {
            switch (s.ToLowerInvariant())
            {
                case "none":          stretch = Player.CompositeArmStretchAmount.None;          return true;
                case "quarter":       stretch = Player.CompositeArmStretchAmount.Quarter;       return true;
                case "threequarters": stretch = Player.CompositeArmStretchAmount.ThreeQuarters; return true;
                case "full":          stretch = Player.CompositeArmStretchAmount.Full;          return true;
                default:              stretch = Player.CompositeArmStretchAmount.Full;          return false;
            }
        }
    }
}
