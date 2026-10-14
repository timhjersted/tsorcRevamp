using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Puppets
{
    /// <summary>Registers the animated solar-light armor shader used by Owl Father's phase-two
    /// X2 armor copies. Not tied to any item, so no player gear can carry the effect.</summary>
    [Autoload(Side = ModSide.Client)]
    public sealed class OwlFatherSolarArmorShaderSystem : ModSystem
    {
        // Terraria's armor-shader registry is keyed by item type. A negative key is never an item,
        // so the shader cannot become a dye; the boss reads it back only through ShaderId.
        private const int RegistryKey = -73101;

        internal static int ShaderId { get; private set; }

        public override void Load()
        {
            if (Main.dedServ)
                return;

            Asset<Effect> effect = ModContent.Request<Effect>(
                "tsorcRevamp/Effects/OwlFatherSolarArmorMask", AssetRequestMode.ImmediateLoad);
            Asset<Texture2D> noise = ModContent.Request<Texture2D>(
                "tsorcRevamp/Textures/Noise/SmoothNoise", AssetRequestMode.ImmediateLoad);

            GameShaders.Armor.BindShader(RegistryKey,
                new ArmorShaderData(effect, "OwlFatherSolarArmorMaskPass")
                    .UseImage(noise)
                    .UseColor(new Color(255, 200, 40))
                    .UseSecondaryColor(new Color(255, 250, 210))
                    // 0.5 = 50% more transparent. The .fx scales rgb and alpha together
                    // (premultiplied), so this halves the overlay evenly without a shader rebuild.
                    .UseOpacity(0.5f));
            ShaderId = GameShaders.Armor.GetShaderIdFromItemId(RegistryKey);
        }

        public override void Unload()
        {
            ShaderId = 0;
        }
    }
}
