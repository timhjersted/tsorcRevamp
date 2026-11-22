using Terraria.ModLoader;

namespace tsorcRevamp.Systems.CeruleanFlask;


public class CeruleanFlaskUsesCommand : ModCommand
{
    public override CommandType Type => CommandType.Chat;
    public override string Command => "ceruleanflaskuses";
    public override string Description => "Set your maximum number of Flask uses.";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (args[0][0] >= '0' && args[0][0] <= '9')
        {
            int uses = (int)args[0][0] - '0';
            caller.Player.GetModPlayer<CeruleanFlaskPlayer>().CeruleanChargesMax = uses;
            caller.Reply("Set your flask uses to " + uses);
        }
        else
        {
            caller.Reply("Enter a number between 0 and 9.");
        }
    }
}