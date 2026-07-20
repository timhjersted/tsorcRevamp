using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using VFXApi = global::tsorcRevamp.VFX.VFX;

namespace tsorcRevamp.Utilities
{
    public sealed class VFXShowcaseCommand : ModCommand
    {
        public override string Command => "vfxshowcase";
        public override CommandType Type => CommandType.Chat;
        public override string Usage => "/vfxshowcase [beam|pillar|earth|worldspine|all]";
        public override string Description => "Preview the reusable tsorcRevamp VFX arsenal near the local player.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (!ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
            {
                caller.Reply("Enable Debug Mode before using the VFX showcase.", Color.OrangeRed);
                return;
            }

            string mode = args.Length == 0 ? "all" : args[0].ToLowerInvariant();
            Player player = caller.Player ?? Main.LocalPlayer;
            Vector2 origin = player.Center;

            if (mode == "beam" || mode == "all")
            {
                VFXApi.Beam(origin + new Vector2(-620f, -460f), origin + new Vector2(-120f, 40f),
                    135f, new Color(35, 225, 255), Color.White, 110, 12f);
            }

            if (mode == "pillar" || mode == "all")
            {
                VFXApi.FlamePillar(origin + new Vector2(260f, 120f), 720f, 150f,
                    new Color(115, 255, 40), Color.White, 130);
                VFXApi.SmokeVent(origin + new Vector2(260f, -560f), new Color(95, 95, 95), 16, 35f, 3f);
            }

            if (mode == "earth" || mode == "all")
            {
                VFXApi.GroundEruption(origin + new Vector2(-260f, 100f), new Color(75, 60, 55),
                    new Color(255, 190, 30), 120f, 36);
                VFXApi.SoulSpiral(origin + new Vector2(-260f, -100f), new Color(175, 45, 230), 22, 180f, 110);
            }

            if (mode == "worldspine" || mode == "all")
            {
                VFXApi.WorldspineArc(origin + new Vector2(-500f, 120f), origin + new Vector2(500f, 120f),
                    460f, 105f, new Color(210, 198, 165), new Color(180, 45, 225), 150);
            }

            caller.Reply($"VFX showcase: {mode}", new Color(220, 180, 255));
        }
    }
}
