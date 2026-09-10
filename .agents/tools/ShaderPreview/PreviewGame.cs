using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShaderPreview
{
    /// <summary>
    /// Minimal FNA host: creates a real device, runs the real shader, reads back a PNG, exits.
    ///
    /// Reproduces ArtoriasVFX.Draw() exactly. The subtleties that matter, all learned by getting
    /// them wrong first:
    ///
    ///   * The quad IS the primary texture, drawn with the effect applied - not a white pixel. The
    ///     shader samples PrimarySampler for its shape, so a white quad produces something that has
    ///     nothing to do with the real effect.
    ///   * DrawSize is the SOURCE rect size, WorldDrawSize is the on-screen size. They differ
    ///     whenever fullTexture is false, and swapping them quietly changes every UV calculation.
    ///   * Textures must be premultiplied on load. FNA's Texture2D.FromStream does not do it;
    ///     tModLoader does. Skipping it is the T_Windstreak3 trap - a texture whose image lives in
    ///     its alpha channel previews fine and renders as a flat constant in game.
    ///   * SamplerState.LinearWrap on BOTH s0 and s1.
    /// </summary>
    internal sealed class PreviewGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private readonly string _repoRoot;
        private readonly string _outDir;
        private readonly Recipe[] _recipes;
        private readonly List<float> _progressValues;
        private readonly int _size;
        private readonly Dictionary<string, Texture2D> _textures = new();

        internal string Failure { get; private set; }

        internal PreviewGame(string repoRoot, string outDir, Recipe[] recipes, List<float> progressValues, int size)
        {
            _repoRoot = repoRoot;
            _outDir = outDir;
            _recipes = recipes;
            _progressValues = progressValues;
            _size = size;

            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = size,
                PreferredBackBufferHeight = size,
                SynchronizeWithVerticalRetrace = false,
            };
            IsFixedTimeStep = false;
        }

        protected override void LoadContent()
        {
            try
            {
                Console.WriteLine($"device: {GraphicsDevice.Adapter.Description}");

                foreach (Recipe recipe in _recipes)
                {
                    RenderRecipe(recipe);
                }
            }
            catch (Exception ex)
            {
                Failure = $"{ex.GetType().Name}: {ex.Message}";
            }
            finally
            {
                Exit();
            }
        }

        private void RenderRecipe(Recipe recipe)
        {
            byte[] bytecode = XnbEffect.Extract(Path.Combine(_repoRoot, "Effects", recipe.Effect + ".xnb"));
            var effect = new Effect(GraphicsDevice, bytecode);

            if (effect.Techniques[recipe.Technique] == null)
            {
                Console.WriteLine($"  {recipe.Name,-16} SKIPPED - no technique '{recipe.Technique}' in {recipe.Effect}");
                return;
            }

            Texture2D primary = GetTexture(recipe.Primary);
            Texture2D detail = GetTexture(recipe.Detail);

            var tiles = new List<Bitmapish>();

            foreach (float progress in _progressValues)
            {
                var target = new RenderTarget2D(GraphicsDevice, _size, _size);
                GraphicsDevice.SetRenderTarget(target);
                GraphicsDevice.Clear(Color.Transparent);

                // ArtoriasVFX.Draw: source rect is the full texture when fullTexture, else the draw
                // size clamped to the texture; scale maps that source onto the requested world size.
                int sourceWidth = recipe.FullTexture
                    ? primary.Width
                    : Math.Clamp((int)recipe.DrawSize.X, 1, primary.Width);
                int sourceHeight = recipe.FullTexture
                    ? primary.Height
                    : Math.Clamp((int)recipe.DrawSize.Y, 1, primary.Height);
                var source = new Rectangle(0, 0, sourceWidth, sourceHeight);
                var actualSize = new Vector2(sourceWidth, sourceHeight);

                // The call site draws at its world size; we fit that into the preview square so the
                // aspect ratio the technique was tuned against is preserved.
                float fit = Math.Min(_size / recipe.DrawSize.X, _size / recipe.DrawSize.Y) * 0.9f;
                Vector2 onScreen = recipe.DrawSize * fit;
                Vector2 scale = onScreen / actualSize;

                GraphicsDevice.Textures[1] = detail;
                GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                effect.CurrentTechnique = effect.Techniques[recipe.Technique];
                effect.Parameters["DarkColor"]?.SetValue(recipe.Dark.ToVector3());
                effect.Parameters["MidColor"]?.SetValue(recipe.Mid.ToVector3());
                effect.Parameters["CoreColor"]?.SetValue(recipe.Core.ToVector3());
                effect.Parameters["Opacity"]?.SetValue(recipe.Opacity);
                effect.Parameters["Time"]?.SetValue(recipe.Time);
                effect.Parameters["Progress"]?.SetValue(progress);
                effect.Parameters["Active"]?.SetValue(recipe.Active);
                effect.Parameters["Direction"]?.SetValue(recipe.Direction);
                effect.Parameters["DrawSize"]?.SetValue(actualSize);
                effect.Parameters["PrimaryTextureSize"]?.SetValue(new Vector2(primary.Width, primary.Height));
                effect.Parameters["WorldDrawSize"]?.SetValue(recipe.DrawSize);

                Vector2 blocks = Vector2.Max(recipe.DrawSize / recipe.PixelBlockSize, Vector2.One);
                effect.Parameters["PixelGrid"]?.SetValue(
                    new Vector4(blocks.X, blocks.Y, 1f / blocks.X, 1f / blocks.Y));

                var batch = new SpriteBatch(GraphicsDevice);
                batch.Begin(SpriteSortMode.Immediate, recipe.Blend, SamplerState.LinearWrap,
                    DepthStencilState.None, RasterizerState.CullNone, effect);
                batch.Draw(primary, new Vector2(_size / 2f, _size / 2f), source, Color.White,
                    recipe.Rotation, actualSize * 0.5f, scale, SpriteEffects.None, 0f);
                batch.End();

                GraphicsDevice.SetRenderTarget(null);

                var pixels = new Color[_size * _size];
                target.GetData(pixels);
                int lit = 0;
                foreach (Color p in pixels)
                {
                    if (p.A > 4 || p.R > 4 || p.G > 4 || p.B > 4) { lit++; }
                }

                string file = Path.Combine(_outDir, $"{recipe.Name}-p{progress:0.00}.png");
                using (FileStream stream = File.Create(file))
                {
                    target.SaveAsPng(stream, _size, _size);
                }

                tiles.Add(new Bitmapish { Path = file, Progress = progress, Lit = lit });
            }

            string litSummary = string.Join(" ", tiles.ConvertAll(t => $"p{t.Progress:0.00}:{t.Lit * 100 / (_size * _size)}%"));
            Console.WriteLine($"  {recipe.Name,-16} {recipe.Technique,-32} {recipe.Blend switch { var b when b == BlendState.Additive => "Additive", _ => "AlphaBlend" },-11} {litSummary}");
        }

        /// <summary>Loads a noise/gradient texture and PREMULTIPLIES it, matching tModLoader.</summary>
        private Texture2D GetTexture(string name)
        {
            if (_textures.TryGetValue(name, out Texture2D cached)) { return cached; }

            string path = ResolveTexturePath(name);
            using FileStream stream = File.OpenRead(path);
            var texture = Texture2D.FromStream(GraphicsDevice, stream);

            var data = new Color[texture.Width * texture.Height];
            texture.GetData(data);
            for (int i = 0; i < data.Length; i++)
            {
                float a = data[i].A / 255f;
                data[i] = new Color((byte)(data[i].R * a), (byte)(data[i].G * a), (byte)(data[i].B * a), data[i].A);
            }
            texture.SetData(data);

            _textures[name] = texture;
            return texture;
        }

        private string ResolveTexturePath(string name)
        {
            string[] candidates =
            {
                Path.Combine(_repoRoot, "Textures", "Noise", name + ".png"),
                Path.Combine(_repoRoot, "Textures", name + ".png"),
                Path.Combine(_repoRoot, "Textures", "Particles", name + ".png"),
            };

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate)) { return candidate; }
            }
            throw new FileNotFoundException($"texture '{name}' not found under Textures/");
        }

        protected override void Draw(GameTime gameTime) { }

        private sealed class Bitmapish
        {
            public string Path;
            public float Progress;
            public int Lit;
        }
    }
}
