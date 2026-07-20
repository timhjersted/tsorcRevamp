using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Utilities
{
    // Diagnostic for the "can't move / can't open menu but the world is still running" soft-lock.
    //
    // /inputdebug         — snapshot of all input-suppression flags right now (runs inside chat so
    //                       blockInput may already be cleared; use /inputlog for the real stuck state).
    // /inputdebug log     — logs every non-trivial flag to client.log for 5 seconds, outside chat.
    //                       Watch the Logs folder after running it, then look for "INPUTLOG" lines.
    public class InputDebugCommand : ModCommand
    {
        // When > 0, PreUpdatePlayers logs flags each tick and counts down.
        public static int LogTicksRemaining = 0;
        private const int LogDuration = 300; // 5 seconds at 60fps

        public override CommandType Type => CommandType.Chat;
        public override string Command => "inputdebug";
        public override string Description => "Dumps input flags. Add 'log' to write to client.log for 5s outside chat mode.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            bool logMode = args.Length > 0 && args[0].ToLowerInvariant() == "log";

            if (logMode)
            {
                LogTicksRemaining = LogDuration;
                caller.Reply("Logging input state to client.log for 5 seconds. Move around (or try to), then check the log for INPUTLOG lines.", Color.Lime);
                return;
            }

            Player p = Main.LocalPlayer;
            var mod = ModContent.GetInstance<tsorcRevamp>();

            void Line(string label, bool value, bool flagWhenTrue = true)
            {
                Color c = (value == flagWhenTrue) ? Color.Orange : Color.Gray;
                caller.Reply($"{label}: {value}", c);
            }

            caller.Reply("=== tsorc input debug (chat-mode snapshot) ===", Color.Yellow);

            Line("Main.blockInput", Main.blockInput);
            Line("PlayerInput.WritingText", PlayerInput.WritingText);
            Line("Main.drawingPlayerChat", Main.drawingPlayerChat);
            Line("Main.editSign", Main.editSign);
            Line("Main.editChest", Main.editChest);
            Line("Main.gameMenu", Main.gameMenu);
            Line("Main.ingameOptionsWindow", Main.ingameOptionsWindow);
            Line("Main.playerInventory", Main.playerInventory);

            caller.Reply($"player.talkNPC: {p.talkNPC} (>= 0 = talking to NPC = can't move)", p.talkNPC >= 0 ? Color.Orange : Color.Gray);
            caller.Reply($"player.sign: {p.sign}", p.sign >= 0 ? Color.Orange : Color.Gray);
            Line("player.frozen", p.frozen);
            Line("player.CCed", p.CCed);
            Line("player.dead", p.dead);
            Line("player.stoned", p.stoned);
            Line("player.webbed", p.webbed);
            Line("player.tongued", p.tongued);
            Line("player.noItems", p.noItems);
            Line("player.mouseInterface", p.mouseInterface);
            caller.Reply($"player.immuneTime: {p.immuneTime}", p.immuneTime > 0 ? Color.Orange : Color.Gray);
            caller.Reply($"player.velocity: {p.velocity}", Color.Gray);

            // Control flags: if these are all False when you're pressing keys, something is clearing them.
            caller.Reply($"controlLeft={p.controlLeft} controlRight={p.controlRight} controlUp={p.controlUp} controlDown={p.controlDown} controlJump={p.controlJump}",
                (p.controlLeft || p.controlRight || p.controlUp || p.controlDown || p.controlJump) ? Color.Gray : Color.Orange);

            if (mod.EnemySelectionUI != null)
                Line("EnemySelectionUI.Visible", mod.EnemySelectionUI.Visible);
            if (mod.SpawnPointConfigUI != null)
                Line("SpawnPointConfigUI.Visible", mod.SpawnPointConfigUI.Visible);

            caller.Reply($"ForceSandbox={tsorcRevampWorld.ForceSandbox}  CustomMap={tsorcRevampWorld.CustomMap}  " +
                         $"RemixMap={tsorcRevampWorld.RemixMap}  AdventureMode={ModContent.GetInstance<tsorcRevampConfig>().AdventureMode}",
                         Color.Gray);

            StringBuilder buffs = new StringBuilder();
            for (int i = 0; i < p.buffType.Length; i++)
            {
                if (p.buffType[i] > 0 && p.buffTime[i] > 0)
                {
                    buffs.Append(p.buffType[i]);
                    buffs.Append('(');
                    buffs.Append(p.buffTime[i]);
                    buffs.Append(") ");
                }
            }
            caller.Reply("buffs: " + (buffs.Length == 0 ? "none" : buffs.ToString()), Color.Gray);

            caller.Reply("(NOTE: blockInput may already be false because chat resets it. Run '/inputdebug log' to capture the real stuck state.)", Color.Gray);
        }

        // Called from tsorcRevampSystems.PreUpdatePlayers when LogTicksRemaining > 0.
        // Logs once per second so the file doesn't flood but we always get real data.
        public static void LogTick()
        {
            if (LogTicksRemaining <= 0) return;
            LogTicksRemaining--;

            // Only write one line per second.
            if (Main.GameUpdateCount % 60 != 0) return;

            Player p = Main.LocalPlayer;
            var mod = ModContent.GetInstance<tsorcRevamp>();

            StringBuilder sb = new StringBuilder();
            sb.Append($"INPUTLOG t={Main.GameUpdateCount}: ");
            sb.Append($"blockInput={Main.blockInput} writingText={PlayerInput.WritingText} drawingChat={Main.drawingPlayerChat} ");
            sb.Append($"editSign={Main.editSign} editChest={Main.editChest} gameMenu={Main.gameMenu} ingameOptions={Main.ingameOptionsWindow} ");
            sb.Append($"talkNPC={p.talkNPC} sign={p.sign} ");  // talkNPC >= 0 means frozen talking to NPC
            sb.Append($"frozen={p.frozen} CCed={p.CCed} stoned={p.stoned} webbed={p.webbed} tongued={p.tongued} dead={p.dead} ");
            sb.Append($"mouseIface={p.mouseInterface} noItems={p.noItems} immuneTime={p.immuneTime} ");
            sb.Append($"ctrl: L={p.controlLeft} R={p.controlRight} U={p.controlUp} D={p.controlDown} J={p.controlJump} ");
            sb.Append($"vel={p.velocity} pos={p.position} ");
            if (mod?.EnemySelectionUI != null) sb.Append($"EnemSelUI={mod.EnemySelectionUI.Visible} ");
            if (mod?.SpawnPointConfigUI != null) sb.Append($"SpawnCfgUI={mod.SpawnPointConfigUI.Visible} ");
            mod?.Logger?.Info(sb.ToString());
        }
    }
}
