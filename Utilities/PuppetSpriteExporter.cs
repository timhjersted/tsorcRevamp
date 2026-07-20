using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Puppets;

namespace tsorcRevamp.Utilities
{
    [Autoload(Side = ModSide.Client)]
    public sealed class PuppetSpriteExportCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;

        public override string Command => "exportpuppets";

        public override string Description =>
            "Export transparent idle and primary-weapon portraits for one or all PuppetNPC types";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            List<ModNPC> puppetTypes = Mod.GetContent<ModNPC>()
                .Where(content => content is PuppetNPC)
                .OrderBy(content => content.Name)
                .ToList();

            if (args.Length > 0 && args[0].Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                caller.Reply("Puppets: " + string.Join(", ", puppetTypes.Select(content => content.Name)), Color.LightCyan);
                return;
            }

            if (PuppetSpriteExporterSystem.IsBusy)
            {
                caller.Reply("A puppet export is already in progress.", Color.Orange);
                return;
            }

            List<ModNPC> selected;
            if (args.Length == 0 || args[0].Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                selected = puppetTypes;
            }
            else
            {
                string query = string.Join(" ", args);
                if (NormalizeName(query).Length == 0)
                {
                    caller.Reply("Usage: /exportpuppets [all|list|puppet name]", Color.Orange);
                    return;
                }
                selected = FindPuppets(puppetTypes, query);
                if (selected.Count == 0)
                {
                    caller.Reply($"No PuppetNPC matched '{query}'. Use /exportpuppets list for internal names.", Color.Orange);
                    return;
                }
                if (selected.Count > 1)
                {
                    caller.Reply(
                        $"'{query}' matched multiple puppets: {string.Join(", ", selected.Select(content => content.Name))}",
                        Color.Orange);
                    return;
                }
            }

            if (selected.Count == 0)
            {
                caller.Reply("No PuppetNPC content types are loaded.", Color.Orange);
                return;
            }

            string outputDirectory = Path.Combine(Main.SavePath, "PuppetExports", Mod.Name);
            PuppetSpriteExporterSystem.Queue(selected, outputDirectory);
            caller.Reply(
                $"Queued {selected.Count * 2} portrait exports ({selected.Count} puppets). " +
                $"One PNG will be written per frame to: {outputDirectory}",
                Color.LightGreen);
        }

        private static List<ModNPC> FindPuppets(List<ModNPC> puppetTypes, string query)
        {
            string normalizedQuery = NormalizeName(query);
            List<ModNPC> exact = puppetTypes.Where(content =>
                    NormalizeName(content.Name) == normalizedQuery
                    || NormalizeName(content.DisplayName.Value) == normalizedQuery)
                .ToList();
            if (exact.Count > 0)
                return exact;

            return puppetTypes.Where(content =>
                    NormalizeName(content.Name).Contains(normalizedQuery)
                    || NormalizeName(content.DisplayName.Value).Contains(normalizedQuery))
                .ToList();
        }

        private static string NormalizeName(string value)
        {
            return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        }
    }

    [Autoload(Side = ModSide.Client)]
    public sealed class PuppetSpriteExporterSystem : ModSystem
    {
        private const int RenderTargetSize = 512;
        private const int CropPadding = 2;

        private sealed class ExportJob
        {
            public int NpcType;
            public string Name;
            public bool WithPrimaryWeapon;
        }

        private static Queue<ExportJob> pendingJobs;
        private static string outputDirectory;
        private static int totalJobs;
        private static int completedJobs;
        private static int failedJobs;

        internal static bool IsBusy => pendingJobs != null && pendingJobs.Count > 0;

        internal static void Queue(IEnumerable<ModNPC> puppetTypes, string destination)
        {
            pendingJobs = new Queue<ExportJob>();
            outputDirectory = destination;
            completedJobs = 0;
            failedJobs = 0;

            foreach (ModNPC puppetType in puppetTypes)
            {
                pendingJobs.Enqueue(new ExportJob
                {
                    NpcType = puppetType.Type,
                    Name = puppetType.Name,
                    WithPrimaryWeapon = false,
                });
                pendingJobs.Enqueue(new ExportJob
                {
                    NpcType = puppetType.Type,
                    Name = puppetType.Name,
                    WithPrimaryWeapon = true,
                });
            }

            totalJobs = pendingJobs.Count;
            Directory.CreateDirectory(outputDirectory);
        }

        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            if (!IsBusy || Main.gameMenu)
                return;

            ExportJob job = pendingJobs.Dequeue();
            try
            {
                Export(job, spriteBatch);
                completedJobs++;
            }
            catch (Exception exception)
            {
                failedJobs++;
                Main.NewText($"Puppet export failed for {job.Name}: {exception.Message}", Color.OrangeRed);
                Mod.Logger.Error($"Failed exporting puppet portrait {job.Name}", exception);
            }

            if (pendingJobs.Count == 0)
            {
                Main.NewText(
                    $"Puppet export finished: {completedJobs}/{totalJobs} PNGs written" +
                    (failedJobs > 0 ? $", {failedJobs} failed" : string.Empty) +
                    $". Folder: {outputDirectory}",
                    failedJobs == 0 ? Color.LightGreen : Color.Orange);
            }
        }

        public override void Unload()
        {
            pendingJobs = null;
            outputDirectory = null;
            totalJobs = 0;
            completedJobs = 0;
            failedJobs = 0;
        }

        private static void Export(ExportJob job, SpriteBatch spriteBatch)
        {
            GraphicsDevice device = Main.graphics.GraphicsDevice;
            RenderTargetBinding[] previousTargets = device.GetRenderTargets();
            bool exportBatchStarted = false;
            bool targetsRestored = false;

            using RenderTarget2D renderTarget = new RenderTarget2D(
                device,
                RenderTargetSize,
                RenderTargetSize,
                false,
                SurfaceFormat.Color,
                DepthFormat.None,
                0,
                RenderTargetUsage.PreserveContents);

            spriteBatch.End();
            try
            {
                device.SetRenderTarget(renderTarget);
                device.Clear(Color.Transparent);

                // Immediate mode applies the render state before the player renderer switches to
                // Terraria's armor shader passes. Identity keeps every exported pixel at 1x scale.
                spriteBatch.Begin(
                    SpriteSortMode.Immediate,
                    BlendState.AlphaBlend,
                    SamplerState.PointClamp,
                    DepthStencilState.None,
                    RasterizerState.CullCounterClockwise,
                    null,
                    Matrix.Identity);
                exportBatchStarted = true;

                NPC npc = new NPC { whoAmI = 0 };
                npc.SetDefaults(job.NpcType);
                if (npc.ModNPC is not PuppetNPC puppet)
                    throw new InvalidOperationException($"NPC type {job.NpcType} is not a PuppetNPC.");

                Vector2 screenPosition = new Vector2(
                    (renderTarget.Width - npc.width) * 0.5f,
                    (renderTarget.Height - npc.height) * 0.5f);
                Vector2 worldPosition = Main.screenPosition + screenPosition;

                if (!puppet.DrawExportPose(worldPosition, job.WithPrimaryWeapon))
                    throw new InvalidOperationException("No primary weapon is configured.");

                spriteBatch.End();
                exportBatchStarted = false;

                RestoreTargets(device, previousTargets);
                targetsRestored = true;

                string suffix = job.WithPrimaryWeapon ? "PrimaryWeapon" : "Idle";
                string filename = $"{SanitizeFileName(job.Name)}_{suffix}.png";
                SaveCropped(renderTarget, Path.Combine(outputDirectory, filename));
            }
            finally
            {
                if (exportBatchStarted)
                    spriteBatch.End();
                if (!targetsRestored)
                    RestoreTargets(device, previousTargets);

                // PostDrawInterface expects the cursor/UI sprite batch to remain active for the
                // tooltip and mouse layers that Terraria draws immediately after this hook.
                spriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    Main.SamplerStateForCursor,
                    DepthStencilState.None,
                    RasterizerState.CullCounterClockwise,
                    null,
                    Main.UIScaleMatrix);
            }
        }

        private static void RestoreTargets(GraphicsDevice device, RenderTargetBinding[] previousTargets)
        {
            if (previousTargets.Length == 0)
                device.SetRenderTarget(null);
            else
                device.SetRenderTargets(previousTargets);
        }

        private static void SaveCropped(RenderTarget2D renderTarget, string path)
        {
            int width = renderTarget.Width;
            int height = renderTarget.Height;
            Color[] source = new Color[width * height];
            renderTarget.GetData(source);

            int minX = width;
            int minY = height;
            int maxX = -1;
            int maxY = -1;

            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    if (source[row + x].A == 0)
                        continue;

                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                }
            }

            if (maxX < minX || maxY < minY)
                throw new InvalidOperationException("The render target contained no visible pixels.");
            if (minX == 0 || minY == 0 || maxX == width - 1 || maxY == height - 1)
                throw new InvalidOperationException("The rendered pose touched the export canvas edge.");

            minX = Math.Max(0, minX - CropPadding);
            minY = Math.Max(0, minY - CropPadding);
            maxX = Math.Min(width - 1, maxX + CropPadding);
            maxY = Math.Min(height - 1, maxY + CropPadding);

            int croppedWidth = maxX - minX + 1;
            int croppedHeight = maxY - minY + 1;
            Color[] cropped = new Color[croppedWidth * croppedHeight];
            for (int y = 0; y < croppedHeight; y++)
            {
                Array.Copy(
                    source,
                    (minY + y) * width + minX,
                    cropped,
                    y * croppedWidth,
                    croppedWidth);
            }

            using Texture2D texture = new Texture2D(Main.graphics.GraphicsDevice, croppedWidth, croppedHeight);
            texture.SetData(cropped);
            using FileStream stream = File.Create(path);
            texture.SaveAsPng(stream, croppedWidth, croppedHeight);
        }

        private static string SanitizeFileName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            return new string(name.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
        }
    }
}
