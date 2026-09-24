using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Armor;

namespace tsorcRevamp.NPCs.Puppets
{
    /// <summary>Registers the animated solar-light armor shader used by Owl Father's phase-two
    /// X2 armor copies. The armor item is only a private lookup key for Terraria's shader registry;
    /// this does not apply the effect to players wearing the set.</summary>
    [Autoload(Side = ModSide.Client)]
    public sealed class OwlFatherSolarArmorShaderSystem : ModSystem
    {
        internal static int ShaderId { get; private set; }

        public override void Load()
        {
            if (Main.dedServ)
                return;

            Asset<Effect> effect = ModContent.Request<Effect>(
                "tsorcRevamp/Effects/OwlFatherSolarArmorMask", AssetRequestMode.ImmediateLoad);
            Asset<Texture2D> noise = ModContent.Request<Texture2D>(
                "tsorcRevamp/Textures/Noise/SmoothNoise", AssetRequestMode.ImmediateLoad);

            int registryKey = ModContent.ItemType<OwlFatherMask>();
            GameShaders.Armor.BindShader(registryKey,
                new ArmorShaderData(effect, "OwlFatherSolarArmorMaskPass")
                    .UseImage(noise)
                    .UseColor(new Color(255, 200, 40))
                    .UseSecondaryColor(new Color(255, 250, 210))
                    .UseOpacity(1f));
            ShaderId = GameShaders.Armor.GetShaderIdFromItemId(registryKey);
        }

        public override void Unload()
        {
            ShaderId = 0;
        }
    }
}
