using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShaderPreview
{
    /// <summary>
    /// Renders the mod's REAL compiled .xnb shaders offline, on a real GPU, with the parameters,
    /// textures and blend state their actual call sites use.
    ///
    /// This sits ALONGSIDE .agents/tools/ShaderSketch/ - it does not replace it. That
    /// harness hand-ports the HLSL to C#; this one runs the shipped bytecode. Keeping both means a
    /// disagreement between them is informative rather than ambiguous.
    ///
    /// Run the mod's build first so Effects/*.xnb are current:
    ///     dotnet build tsorcRevamp.csproj -t:Compile
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            string repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
            string outDir = Path.Combine(repoRoot, "tsorcDocs", "ShaderReports");
            string only = null;
            var progressValues = new List<float>();
            bool verify = false;
            int size = 256;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "--only": only = args[++i]; break;
                    case "--out": outDir = args[++i]; break;
                    case "--size": size = int.Parse(args[++i]); break;
                    case "--verify": verify = true; break;
                    case "--progress":
                        foreach (string part in args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries))
                        {
                            progressValues.Add(float.Parse(part, System.Globalization.CultureInfo.InvariantCulture));
                        }
                        break;
                    case "--list":
                        foreach (Recipe r in Recipes.All()) { Console.WriteLine($"  {r.Name,-16} {r.Effect} / {r.Technique}"); }
                        return 0;
                    default:
                        Console.WriteLine($"unknown option {args[i]}");
                        return 1;
                }
            }

            if (progressValues.Count == 0)
            {
                // More than one Progress, always: a technique that looks right at 0.5 routinely falls
                // apart at the ends, and that is the whole point of previewing it.
                progressValues.AddRange(new[] { 0.15f, 0.5f, 0.85f });
            }

            Recipe[] recipes = Recipes.All();
            if (only != null)
            {
                recipes = recipes.Where(r => r.Name.Contains(only, StringComparison.OrdinalIgnoreCase)).ToArray();
                if (recipes.Length == 0) { Console.WriteLine($"no recipe matching '{only}'"); return 1; }
            }

            if (verify)
            {
                return RecipeVerifier.Run(repoRoot, Recipes.All()) ? 0 : 2;
            }

            Directory.CreateDirectory(outDir);
            using var harness = new PreviewGame(repoRoot, outDir, recipes, progressValues, size);
            harness.Run();

            if (harness.Failure != null)
            {
                Console.WriteLine($"FAILED: {harness.Failure}");
                return 3;
            }
            return 0;
        }
    }

    /// <summary>
    /// Re-reads ArtoriasVFX.cs and reports whether each recipe's technique still appears with the
    /// same primary texture and blend state. Not a parser - a deliberately shallow textual check,
    /// because the failure it guards against (someone retunes a call site and the preview silently
    /// keeps showing the old one) is exactly the kind a shallow check catches.
    /// </summary>
    internal static class RecipeVerifier
    {
        internal static bool Run(string repoRoot, Recipe[] recipes)
        {
            string source = Path.Combine(repoRoot, "Projectiles", "Enemy", "ArtoriasVFX.cs");
            if (!File.Exists(source))
            {
                Console.WriteLine($"cannot verify: {source} not found");
                return false;
            }

            string text = File.ReadAllText(source);
            bool ok = true;

            foreach (Recipe recipe in recipes)
            {
                string needle = $"\"{recipe.Technique}\"";
                string blendName = recipe.Blend == BlendState.Additive ? "BlendState.Additive" : "BlendState.AlphaBlend";

                // Every occurrence, not just the first: an effect's own name also appears in
                // LoadAssets' ModContent.Request lines, and anchoring there looks at asset loading
                // instead of the draw call and reports drift that is not there.
                bool found = false;
                bool blendOk = false;
                int from = 0;
                while (true)
                {
                    int idx = text.IndexOf(needle, from, StringComparison.Ordinal);
                    if (idx < 0) { break; }
                    found = true;
                    string window = text.Substring(idx, Math.Min(600, text.Length - idx));
                    if (window.Contains(blendName, StringComparison.Ordinal)) { blendOk = true; break; }
                    from = idx + needle.Length;
                }

                if (!found)
                {
                    Console.WriteLine($"  MISSING  {recipe.Name,-16} technique {needle} no longer in ArtoriasVFX.cs");
                    ok = false;
                    continue;
                }

                if (!blendOk)
                {
                    Console.WriteLine($"  DRIFT    {recipe.Name,-16} expected {blendName} near \"{recipe.Technique}\"");
                    ok = false;
                }
                else
                {
                    Console.WriteLine($"  ok       {recipe.Name,-16} {recipe.Technique} / {blendName}");
                }
            }

            Console.WriteLine(ok
                ? "verify: recipes still match the call sites"
                : "verify: DRIFT DETECTED - re-read ArtoriasVFX.cs and update Recipes.cs");
            return ok;
        }
    }
}
