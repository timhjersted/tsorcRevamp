using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace tsorcRevamp.Textures
{
    internal class ForceLoadTexture
    {
        public string _texturePath { get; private set; }
        public Texture2D texture { get; private set; }

        public ForceLoadTexture(string path)
        {
            _texturePath = path;
        }

        internal void KeepLoaded()
        {
            if (texture == null || texture.IsDisposed)
            {
                texture = ModContent.Request<Texture2D>(_texturePath, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
        }
    }
}
