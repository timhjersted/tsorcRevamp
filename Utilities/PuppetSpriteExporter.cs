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

            // Each run gets its own timestamped folder so an export never overwrites an earlier one. Prefer the
            // private tsorcDocs folder in the mod source (buildIgnore'd, so exports never ship in the .tmod);
            // fall back to the tModLoader save folder when there is no source checkout.
            string runFolder = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            string docsDirectory = Path.Combine(Main.SavePath, "ModSources", Mod.Name, "tsorcDocs");
            string outputDirectory = Path.Combine(Main.SavePath, "PuppetExports", Mod.Name, runFolder);
            if (Directory.Exists(docsDirectory))
            {
                outputDirectory = Path.Combine(docsDirectory, "PuppetExports", runFolder);
            }

            PuppetSpriteExporterSystem.Queue(selected, outputDirectory);
            caller.Reply(
                $"Queued {selected.Count * 2} portrait exports ({selected.Count} puppets), plus a boss head icon " +
                $"(<Name>_Head_Boss.png) cropped from each idle portrait. One export per frame, to: {outputDirectory}",
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

        /// <summary>Boss head icon (map / boss bar / Boss Checklist) outline colour; the icon itself is the top square of the
        /// idle portrait (SaveHeadIcon).</summary>
        private static readonly Color HeadIconOutline = new Color(20, 20, 24);

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
                SaveCropped(renderTarget, Path.Combine(outputDirectory, filename), out Color[] croppedPixels, out int croppedWidth, out int croppedHeight);

                // The idle pose also yields the boss head icon, named as tModLoader expects (<Class>_Head_Boss).
                if (!job.WithPrimaryWeapon)
                {
                    string headFilename = $"{SanitizeFileName(job.Name)}_Head_Boss.png";
                    SaveHeadIcon(croppedPixels, croppedWidth, croppedHeight, Path.Combine(outputDirectory, headFilename));
                }
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

        /// <summary>Writes the render trimmed to its visible pixels (+CropPadding), and hands the trimmed image back for
        /// the head icon.</summary>
        private static void SaveCropped(RenderTarget2D renderTarget, string path, out Color[] cropped, out int croppedWidth, out int croppedHeight)
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

            croppedWidth = maxX - minX + 1;
            croppedHeight = maxY - minY + 1;
            cropped = new Color[croppedWidth * croppedHeight];
            for (int y = 0; y < croppedHeight; y++)
            {
                Array.Copy(
                    source,
                    (minY + y) * width + minX,
                    cropped,
                    y * croppedWidth,
                    croppedWidth);
            }

            WritePng(cropped, croppedWidth, croppedHeight, path);
        }

        /// <summary>
        /// Boss head icon: the top square of the trimmed idle portrait — side = the portrait's width (its height if that is
        /// smaller), so a 32x48 idle gives a 32x32 icon of the head and shoulders at 1x. The portrait's 2-px crop padding
        /// leaves room for a 1-px dark outline (4-neighbour) so it reads on the map; the cut edge at the bottom gets none.
        /// </summary>
        private static void SaveHeadIcon(Color[] idlePixels, int idleWidth, int idleHeight, string path)
        {
            int iconWidth = Math.Min(idleWidth, idleHeight);
            int iconHeight = iconWidth;
            int leftOffset = (idleWidth - iconWidth) / 2;
            Color[] icon = new Color[iconWidth * iconHeight];

            for (int y = 0; y < iconHeight; y++)
            {
                Array.Copy(idlePixels, y * idleWidth + leftOffset, icon, y * iconWidth, iconWidth);
            }

            // Outline: every empty pixel touching an opaque one. Read from a copy so new outline pixels don't spread.
            Color[] body = (Color[])icon.Clone();

            for (int y = 0; y < iconHeight; y++)
            {
                for (int x = 0; x < iconWidth; x++)
                {
                    if (body[y * iconWidth + x].A > 0)
                    {
                        continue;
                    }

                    bool touchesLeft = x > 0 && body[y * iconWidth + x - 1].A > 0;
                    bool touchesRight = x < iconWidth - 1 && body[y * iconWidth + x + 1].A > 0;
                    bool touchesUp = y > 0 && body[(y - 1) * iconWidth + x].A > 0;
                    bool touchesDown = y < iconHeight - 1 && body[(y + 1) * iconWidth + x].A > 0;

                    if (touchesLeft || touchesRight || touchesUp || touchesDown)
                    {
                        icon[y * iconWidth + x] = HeadIconOutline;
                    }
                }
            }

            WritePng(icon, iconWidth, iconHeight, path);
        }

        /// <summary>Callers: the portrait crop and the head icon.</summary>
        private static void WritePng(Color[] pixels, int pixelWidth, int pixelHeight, string path)
        {
            using Texture2D texture = new Texture2D(Main.graphics.GraphicsDevice, pixelWidth, pixelHeight);
            texture.SetData(pixels);
            using FileStream stream = File.Create(path);
            texture.SaveAsPng(stream, pixelWidth, pixelHeight);
        }

        private static string SanitizeFileName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            return new string(name.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
        }
    }
}
