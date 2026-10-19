using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace tsorcRevamp.Utilities
{
    // /glintlegacy toggles the Archdeacon's glintstone projectiles between the shader look (default, Effects/
    // ArchdeaconGlintstone.fx) and the older sprite + dust draws, for side-by-side comparison. Client-side and
    // visual only: it changes nothing about hitboxes or behaviour, and resets to the shader look on reload.
    public class GlintLegacyCommand : ModCommand
    {
        // Read by every Archdeacon glint projectile's PreDraw (and the caster's staff-tip previews).
        public static bool UseLegacySprites;

        public override CommandType Type => CommandType.Chat;

        public override string Command => "glintlegacy";

        public override string Description => "Toggle the Archdeacon's glintstone visuals between the shaders and the old sprites.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            UseLegacySprites = !UseLegacySprites;

            string look = "shaders (default)";
            if (UseLegacySprites)
            {
                look = "legacy sprites + dust";
            }

            caller.Reply("Archdeacon glintstone visuals: " + look, Color.LightSkyBlue);
        }
    }
}
