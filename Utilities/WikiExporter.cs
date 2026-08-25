using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;

namespace tsorcRevamp.Utilities
{
    public class WikiExporterCommand : ModCommand
    {
        private sealed class DropSource
        {
            public string SourceKind { get; init; }
            public int SourceId { get; init; }
            public string SourceName { get; init; }
            public string SourceInternalName { get; init; }
            public DropRateInfo Drop { get; init; }
        }

        private sealed class ShopSource
        {
            public int NpcType { get; init; }
            public string NpcName { get; init; }
            public string ShopName { get; init; }
            public int Price { get; init; }
            public int CurrencyId { get; init; }
            public IEnumerable<string> Conditions { get; init; }
        }

        private sealed class WormGroup
        {
            public ModNPC Head { get; init; }
            public List<ModNPC> Pieces { get; init; }
        }

        private sealed class Portrait
        {
            public Color[] Pixels { get; init; }
            public int Width { get; init; }
            public int Height { get; init; }
        }

        // NPCs that inherit an animated vanilla NPC but do not declare their own frame count.
        private static readonly Dictionary<string, int> PortraitFrameCountOverrides = new Dictionary<string, int>
        {
            ["AncestralSpirit"] = 8
        };

        // These pre-composed portraits are maintained for Boss Checklist and are a more accurate depiction
        // than arranging the individual moving NPC segments at export time.
        private static readonly Dictionary<string, string> BossChecklistPortraits = new Dictionary<string, string>
        {
            ["HellkiteDragonHead"] = "HellkiteDragon_Portrait.png",
            ["JungleWyvernHead"] = "JungleWyvern_Portrait.png",
            ["SeathTheScalelessHead"] = "Seath_Portrait.png",
            ["SerrisHead"] = "Serris_Portrait.png"
        };

        // Internal combat helpers, unused prototypes, and spoiler forms that should never have standalone wiki pages.
        private static readonly HashSet<string> ExcludedWikiNPCs = new HashSet<string>
        {
            "AttraidiesMimic",
            "DarkDragonMask",
            "AttraidiesApparition",
            "AttraidiesFragment",
            "AttraidiesShield",
            "AttraidiesIllusion",
            "AttraidiesManifestation",
            "MarilithDeath",
            "MarilithIntro",
            "MarilithSeeker",
            "MarilithSpiritTwin",
            "SoulOfCinder",
            "DestroyerLaserProbe",
            "PrimeLaserProbe",
            "PrimeIntro",
            "PrimeBeam",
            "PrimeBuzzsaw",
            "PrimeGatling",
            "PrimeIon",
            "PrimeSiege",
            "PrimeWelder",
            "BrokenOkiku",
            "Okiku",
            "GhostDragonHead",
            "MechaDragonHead",
            "ShadowDragonHead"
        };

        public override CommandType Type => CommandType.Chat;

        public override string Command => "exportwikidata";

        public override string Description => "Exports item and NPC stats and XML import files for the wiki.gg database";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            try
            {
                string savePath = Main.SavePath;
                string workspacePath = Path.Combine(savePath, "ModSources", "tsorcRevamp", "Wiki");
                bool workspaceExists = Directory.Exists(workspacePath);

                string itemsFile = workspaceExists
                    ? Path.Combine(workspacePath, "items_data.lua")
                    : Path.Combine(savePath, "tsorc_items_data.lua");

                string npcsFile = workspaceExists
                    ? Path.Combine(workspacePath, "npcs_data.lua")
                    : Path.Combine(savePath, "tsorc_npcs_data.lua");

                string xmlFile = workspaceExists
                    ? Path.Combine(workspacePath, "wiki_pages_import.xml")
                    : Path.Combine(savePath, "tsorc_wiki_pages_import.xml");

                string imagesDirectory = workspaceExists
                    ? Path.Combine(workspacePath, "images")
                    : Path.Combine(savePath, "tsorc_wiki_images");

                string uploadBundleDirectory = workspaceExists
                    ? Path.Combine(workspacePath, $"upload-images-{DateTime.Now:yyyyMMdd-HHmmss}")
                    : Path.Combine(savePath, $"tsorc_wiki_upload_images_{DateTime.Now:yyyyMMdd-HHmmss}");

                string excludedNpcsFile = workspaceExists
                    ? Path.Combine(workspacePath, "wiki_excluded_npcs.txt")
                    : Path.Combine(savePath, "tsorc_wiki_excluded_npcs.txt");

                List<ModItem> modItems = ModContent.GetContent<ModItem>().Where(item => item.Mod == Mod).ToList();
                List<ModNPC> modNPCs = ModContent.GetContent<ModNPC>().Where(npc => npc.Mod == Mod).ToList();
                List<WormGroup> wormGroups = BuildWormGroups(modNPCs);
                HashSet<int> wormSegmentTypes = wormGroups.SelectMany(group => group.Pieces.Skip(1)).Select(npc => npc.Type).ToHashSet();
                List<ModNPC> wikiNPCs = modNPCs.Where(ShouldExportNpc).Where(npc => !wormSegmentTypes.Contains(npc.Type)).ToList();
                Dictionary<int, ModItem> modItemsByType = modItems.ToDictionary(item => item.Type);
                Dictionary<int, ModNPC> wikiNPCsByType = wikiNPCs.ToDictionary(npc => npc.Type);
                EnsureUniqueWikiPageNames(modItems, wikiNPCs);
                ExportExcludedNpcs(excludedNpcsFile, modNPCs, wikiNPCs, wormSegmentTypes);
                Dictionary<int, List<DropSource>> itemDropSources = BuildItemDropSources(modItemsByType, wikiNPCsByType);
                Dictionary<int, List<ShopSource>> itemShopSources = BuildItemShopSources(modItemsByType);

                ExportItems(itemsFile, modItems, itemDropSources, itemShopSources);
                ExportNPCs(npcsFile, wikiNPCs);
                ExportPagesXml(xmlFile, modItems, wikiNPCs);
                List<string> missingImages = ExportImages(imagesDirectory, modItems, wikiNPCs);
                List<string> missingBundleImages = ExportUploadBundle(uploadBundleDirectory, imagesDirectory, modItems, wikiNPCs);

                string msg = $"Success! Data exported to:\n- {itemsFile}\n- {npcsFile}\n- {xmlFile}\n- {imagesDirectory}\n- Clean upload bundle: {uploadBundleDirectory}\n- Excluded NPC audit: {excludedNpcsFile}";
                if (missingImages.Count > 0)
                {
                    msg += $"\n{missingImages.Count} image file(s) could not be found in the source folder. See client.log for the list.";
                    foreach (string missingImage in missingImages)
                    {
                        Mod.Logger.Warn($"Wiki image export skipped: {missingImage}");
                    }
                }
                if (missingBundleImages.Count > 0)
                {
                    msg += $"\n{missingBundleImages.Count} image file(s) were unavailable for the upload bundle. See client.log for the list.";
                    foreach (string missingImage in missingBundleImages)
                    {
                        Mod.Logger.Warn($"Wiki upload bundle skipped: {missingImage}");
                    }
                }
                caller.Reply(msg, Color.Lime);
            }
            catch (Exception ex)
            {
                caller.Reply($"Export failed: {ex.Message}\n{ex.StackTrace}", Color.Red);
            }
        }

        private void ExportItems(string filePath, IEnumerable<ModItem> modItems, IReadOnlyDictionary<int, List<DropSource>> itemDropSources, IReadOnlyDictionary<int, List<ShopSource>> itemShopSources)
        {
            var builder = new StringBuilder();
            builder.AppendLine("-- This file is auto-generated by the /exportwikidata command. Do not edit by hand.");
            builder.AppendLine("return {");

            foreach (ModItem modItem in modItems)
            {
                Item item = ContentSamples.ItemsByType[modItem.Type];
                string displayName = item.Name;
                if (string.IsNullOrEmpty(displayName))
                {
                    displayName = modItem.Name;
                }

                string escapedDisplayName = EscapeLua(displayName);
                string escapedInternalName = EscapeLua(modItem.Name);
                string itemType = GetItemType(item);
                string rarity = GetRarityName(item.rare);
                string tooltip = GetTooltipText(item);

                string wikiName = GetWikiPageName(displayName, modItem.Name);
                builder.AppendLine($"    [\"{escapedInternalName}\"] = {{");
                builder.AppendLine($"        name = \"{escapedDisplayName}\",");
                builder.AppendLine($"        internalName = \"{escapedInternalName}\",");
                builder.AppendLine($"        wikiName = \"{EscapeLua(wikiName)}\",");
                builder.AppendLine($"        image = \"{GetItemImageFileName(modItem)}\",");
                builder.AppendLine($"        type = \"{itemType}\",");
                builder.AppendLine($"        damage = {item.damage},");
                builder.AppendLine($"        defense = {item.defense},");
                builder.AppendLine($"        damageClass = \"{EscapeLua(item.DamageType.DisplayName.Value)}\",");
                builder.AppendLine($"        crit = {item.crit},");
                builder.AppendLine($"        knockback = {FormatNumber(item.knockBack)},");
                builder.AppendLine($"        mana = {item.mana},");
                builder.AppendLine($"        useTime = {item.useTime},");
                builder.AppendLine($"        useAnimation = {item.useAnimation},");
                builder.AppendLine($"        useStyle = {item.useStyle},");
                builder.AppendLine($"        autoReuse = {ToLuaBool(item.autoReuse)},");
                builder.AppendLine($"        consumable = {ToLuaBool(item.consumable)},");
                builder.AppendLine($"        maxStack = {item.maxStack},");
                builder.AppendLine($"        value = {item.value},");
                builder.AppendLine($"        rare = \"{rarity}\",");
                builder.AppendLine($"        tooltip = \"{EscapeLua(tooltip)}\",");
                builder.AppendLine($"        healLife = {item.healLife},");
                builder.AppendLine($"        healMana = {item.healMana},");
                builder.AppendLine($"        buffType = {item.buffType},");
                builder.AppendLine($"        buffTime = {item.buffTime},");
                builder.AppendLine($"        pickPower = {item.pick},");
                builder.AppendLine($"        axePower = {item.axe},");
                builder.AppendLine($"        hammerPower = {item.hammer},");
                builder.AppendLine($"        projectileType = {item.shoot},");
                builder.AppendLine($"        projectileSpeed = {FormatNumber(item.shootSpeed)},");
                builder.AppendLine($"        createsTile = {item.createTile},");
                builder.AppendLine($"        createsWall = {item.createWall},");
                AppendRecipes(builder, item);
                AppendDropSources(builder, itemDropSources.TryGetValue(modItem.Type, out List<DropSource> sources) ? sources : null);
                AppendShopSources(builder, itemShopSources.TryGetValue(modItem.Type, out List<ShopSource> shops) ? shops : null);
                builder.AppendLine("    },");
            }

            builder.AppendLine("}");
            File.WriteAllText(filePath, builder.ToString(), Encoding.UTF8);
        }

        private void ExportNPCs(string filePath, IEnumerable<ModNPC> modNPCs)
        {
            var builder = new StringBuilder();
            builder.AppendLine("-- This file is auto-generated by the /exportwikidata command. Do not edit by hand.");
            builder.AppendLine("return {");

            foreach (ModNPC modNPC in modNPCs)
            {
                NPC npc = ContentSamples.NpcsByNetId[modNPC.Type];
                string displayName = Lang.GetNPCNameValue(npc.type);
                if (string.IsNullOrEmpty(displayName))
                {
                    displayName = modNPC.Name;
                }

                string escapedDisplayName = EscapeLua(displayName);
                string escapedInternalName = EscapeLua(modNPC.Name);
                string category = npc.friendly ? (npc.townNPC ? "Town NPC" : "Friendly NPC") : (npc.boss ? "Boss" : "Enemy");
                float kbResist = 1f - npc.knockBackResist;

                string wikiName = GetWikiPageName(displayName, modNPC.Name);
                builder.AppendLine($"    [\"{escapedInternalName}\"] = {{");
                builder.AppendLine($"        name = \"{escapedDisplayName}\",");
                builder.AppendLine($"        internalName = \"{escapedInternalName}\",");
                builder.AppendLine($"        wikiName = \"{EscapeLua(wikiName)}\",");
                builder.AppendLine($"        image = \"{GetNpcImageFileName(modNPC)}\",");
                builder.AppendLine($"        category = \"{category}\",");
                builder.AppendLine($"        lifeMax = {npc.lifeMax},");
                builder.AppendLine($"        defense = {npc.defense},");
                builder.AppendLine($"        damage = {npc.damage},");
                builder.AppendLine($"        knockbackResist = {FormatNumber(kbResist)},");
                builder.AppendLine($"        value = {npc.value},");
                builder.AppendLine($"        boss = {(npc.boss ? "true" : "false")},");
                builder.AppendLine($"        townNPC = {ToLuaBool(npc.townNPC)},");
                builder.AppendLine($"        width = {npc.width},");
                builder.AppendLine($"        height = {npc.height},");
                builder.AppendLine($"        aiStyle = {npc.aiStyle},");
                builder.AppendLine($"        noGravity = {ToLuaBool(npc.noGravity)},");
                builder.AppendLine($"        noTileCollide = {ToLuaBool(npc.noTileCollide)},");
                builder.AppendLine($"        banner = {modNPC.Banner},");
                builder.AppendLine($"        bannerItem = {modNPC.BannerItem},");
                AppendLootTable(builder, GetLootTable(Main.ItemDropsDB.GetRulesForNPCID(modNPC.Type, includeGlobalDrops: false)));
                builder.AppendLine("    },");
            }

            builder.AppendLine("}");
            File.WriteAllText(filePath, builder.ToString(), Encoding.UTF8);
        }

        private List<string> ExportImages(string imagesDirectory, IEnumerable<ModItem> modItems, IEnumerable<ModNPC> modNPCs)
        {
            Directory.CreateDirectory(imagesDirectory);
            var missingImages = new List<string>();

            foreach (ModItem modItem in modItems)
            {
                ExportItemImage(imagesDirectory, modItem, missingImages);
            }

            foreach (ModNPC modNPC in modNPCs)
            {
                if (TryExportBossChecklistPortrait(imagesDirectory, modNPC, missingImages))
                {
                    continue;
                }

                ExportNpcPortrait(imagesDirectory, modNPC, missingImages);
            }

            return missingImages;
        }

        private List<string> ExportUploadBundle(string bundleDirectory, string imagesDirectory, IEnumerable<ModItem> modItems, IEnumerable<ModNPC> modNPCs)
        {
            Directory.CreateDirectory(bundleDirectory);
            List<string> requiredImages = modItems.Select(GetItemImageFileName)
                .Concat(modNPCs.Select(GetNpcImageFileName))
                .OrderBy(fileName => fileName, StringComparer.Ordinal)
                .ToList();
            var missingImages = new List<string>();

            foreach (string fileName in requiredImages)
            {
                string sourceFile = Path.Combine(imagesDirectory, fileName);
                if (!File.Exists(sourceFile))
                {
                    missingImages.Add(fileName);
                    continue;
                }

                File.Copy(sourceFile, Path.Combine(bundleDirectory, fileName), overwrite: true);
            }

            File.WriteAllLines(Path.Combine(bundleDirectory, "upload-manifest.txt"), requiredImages, Encoding.UTF8);
            return missingImages;
        }

        private void ExportItemImage(string imagesDirectory, ModItem modItem, List<string> missingImages)
        {
            const string modAssetPrefix = "tsorcRevamp/";
            string outputFileName = GetItemImageFileName(modItem);
            string texturePath = modItem.Texture;
            if (!string.IsNullOrWhiteSpace(texturePath) && texturePath.StartsWith(modAssetPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string relativeTexturePath = texturePath.Substring(modAssetPrefix.Length).Replace('/', Path.DirectorySeparatorChar);
                string sourceFile = Path.Combine(Main.SavePath, "ModSources", "tsorcRevamp", relativeTexturePath + ".png");
                if (File.Exists(sourceFile))
                {
                    File.Copy(sourceFile, Path.Combine(imagesDirectory, outputFileName), overwrite: true);
                    return;
                }
            }

            try
            {
                Texture2D texture = TextureAssets.Item[modItem.Type].Value;
                using (FileStream output = File.Create(Path.Combine(imagesDirectory, outputFileName)))
                {
                    texture.SaveAsPng(output, texture.Width, texture.Height);
                }
            }
            catch (Exception ex)
            {
                missingImages.Add($"{outputFileName} (texture export failed: {ex.Message})");
            }
        }

        private bool TryExportBossChecklistPortrait(string imagesDirectory, ModNPC modNPC, List<string> missingImages)
        {
            if (!BossChecklistPortraits.TryGetValue(modNPC.Name, out string portraitFileName))
            {
                return false;
            }

            string sourceFile = Path.Combine(Main.SavePath, "ModSources", "tsorcRevamp", "NPCs", "Bosses", "Boss Checklist Replacement Sprites", portraitFileName);
            if (!File.Exists(sourceFile))
            {
                missingImages.Add($"{GetNpcImageFileName(modNPC)} (expected Boss Checklist portrait: {sourceFile})");
                return true;
            }

            File.Copy(sourceFile, Path.Combine(imagesDirectory, GetNpcImageFileName(modNPC)), overwrite: true);
            return true;
        }

        private void ExportNpcPortrait(string imagesDirectory, ModNPC modNPC, List<string> missingImages)
        {
            string outputFileName = GetNpcImageFileName(modNPC);
            try
            {
                SavePortrait(imagesDirectory, outputFileName, CreateNpcPortrait(modNPC));
            }
            catch (Exception ex)
            {
                missingImages.Add($"{outputFileName} (portrait export failed: {ex.Message})");
            }
        }

        private Portrait CreateNpcPortrait(ModNPC modNPC, int padding = 2)
        {
            Texture2D sourceTexture = TextureAssets.Npc[modNPC.Type].Value;
            int frameCount = PortraitFrameCountOverrides.TryGetValue(modNPC.Name, out int frameCountOverride)
                ? frameCountOverride
                : Math.Max(Main.npcFrameCount[modNPC.Type], 1);
            int frameHeight = sourceTexture.Height / frameCount;
            if (frameHeight <= 0)
            {
                throw new InvalidOperationException("The NPC texture has no valid animation frame height.");
            }

            Color[] framePixels = new Color[sourceTexture.Width * frameHeight];
            sourceTexture.GetData(0, new Rectangle(0, 0, sourceTexture.Width, frameHeight), framePixels, 0, framePixels.Length);
            Rectangle visibleArea = GetVisibleArea(framePixels, sourceTexture.Width, frameHeight, padding);
            var portraitPixels = new Color[visibleArea.Width * visibleArea.Height];
            for (int y = 0; y < visibleArea.Height; y++)
            {
                Array.Copy(framePixels, (visibleArea.Y + y) * sourceTexture.Width + visibleArea.X, portraitPixels, y * visibleArea.Width, visibleArea.Width);
            }

            return new Portrait { Pixels = portraitPixels, Width = visibleArea.Width, Height = visibleArea.Height };
        }

        private static void SavePortrait(string imagesDirectory, string outputFileName, Portrait portrait)
        {
            using (var texture = new Texture2D(Main.instance.GraphicsDevice, portrait.Width, portrait.Height))
            using (FileStream output = File.Create(Path.Combine(imagesDirectory, outputFileName)))
            {
                texture.SetData(portrait.Pixels);
                texture.SaveAsPng(output, texture.Width, texture.Height);
            }
        }

        private static List<WormGroup> BuildWormGroups(IEnumerable<ModNPC> modNPCs)
        {
            List<ModNPC> allNPCs = modNPCs.ToList();
            var wormGroups = new List<WormGroup>();

            foreach (ModNPC head in allNPCs.Where(npc => npc.Name.EndsWith("Head", StringComparison.Ordinal)))
            {
                string prefix = head.Name.Substring(0, head.Name.Length - "Head".Length);
                ModNPC tail = allNPCs.FirstOrDefault(npc => npc.Name == prefix + "Tail");
                if (tail == null)
                {
                    continue;
                }

                List<ModNPC> bodyPieces = allNPCs
                    .Where(npc => npc.Name.StartsWith(prefix, StringComparison.Ordinal)
                        && npc.Name != head.Name
                        && npc.Name != tail.Name
                        && (npc.Name.StartsWith(prefix + "Body", StringComparison.Ordinal) || npc.Name == prefix + "Legs"))
                    .OrderBy(npc => npc.Name == prefix + "Body" ? 0 : npc.Name == prefix + "Legs" ? 1 : 2)
                    .ThenBy(npc => npc.Name, StringComparer.Ordinal)
                    .ToList();

                if (bodyPieces.Count == 0)
                {
                    continue;
                }

                bodyPieces.Insert(0, head);
                bodyPieces.Add(tail);
                wormGroups.Add(new WormGroup { Head = head, Pieces = bodyPieces });
            }

            return wormGroups;
        }

        private static Rectangle GetVisibleArea(Color[] pixels, int width, int height, int padding)
        {
            int minX = width;
            int minY = height;
            int maxX = -1;
            int maxY = -1;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (pixels[y * width + x].A == 0)
                    {
                        continue;
                    }

                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                }
            }

            if (maxX < 0)
            {
                return new Rectangle(0, 0, width, height);
            }

            int left = Math.Max(0, minX - padding);
            int top = Math.Max(0, minY - padding);
            int right = Math.Min(width - 1, maxX + padding);
            int bottom = Math.Min(height - 1, maxY + padding);
            return new Rectangle(left, top, right - left + 1, bottom - top + 1);
        }

        private static string GetItemImageFileName(ModItem modItem)
        {
            return $"TSORC_Item_{modItem.Name}.png";
        }

        private static string GetNpcImageFileName(ModNPC modNPC)
        {
            return $"TSORC_NPC_{modNPC.Name}.png";
        }

        private static string GetWikiPageName(string displayName, string internalName)
        {
            return displayName;
        }

        private static bool ShouldExportNpc(ModNPC modNPC)
        {
            if (ExcludedWikiNPCs.Contains(modNPC.Name))
            {
                return false;
            }

            return !NPCID.Sets.NPCBestiaryDrawOffset.TryGetValue(modNPC.Type, out NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers)
                || !drawModifiers.Hide;
        }

        private static void EnsureUniqueWikiPageNames(IEnumerable<ModItem> modItems, IEnumerable<ModNPC> modNPCs)
        {
            List<string> duplicateNames = modItems.Select(item => GetWikiPageName(ContentSamples.ItemsByType[item.Type].Name, item.Name))
                .Concat(modNPCs.Select(npc => GetWikiPageName(Lang.GetNPCNameValue(npc.Type), npc.Name)))
                .GroupBy(name => name, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            if (duplicateNames.Count > 0)
            {
                throw new InvalidOperationException($"Duplicate wiki page names must be excluded or renamed before export: {string.Join(", ", duplicateNames)}");
            }
        }

        private static void ExportExcludedNpcs(string filePath, IEnumerable<ModNPC> modNPCs, IEnumerable<ModNPC> wikiNPCs, ISet<int> wormSegmentTypes)
        {
            HashSet<int> exportedTypes = wikiNPCs.Select(npc => npc.Type).ToHashSet();
            var lines = new List<string> { "Internal name\tDisplay name\tReason" };

            foreach (ModNPC modNPC in modNPCs.Where(npc => !exportedTypes.Contains(npc.Type)).OrderBy(npc => npc.Name, StringComparer.Ordinal))
            {
                string reason = ExcludedWikiNPCs.Contains(modNPC.Name)
                    ? "Explicit spoiler, prototype, boss phase, or helper exclusion"
                    : wormSegmentTypes.Contains(modNPC.Type)
                        ? "Worm segment; represented by its head when applicable"
                        : "Hidden from the Bestiary";
                lines.Add($"{modNPC.Name}\t{Lang.GetNPCNameValue(modNPC.Type)}\t{reason}");
            }

            File.WriteAllLines(filePath, lines, Encoding.UTF8);
        }

        private void ExportPagesXml(string filePath, IEnumerable<ModItem> modItems, IEnumerable<ModNPC> modNPCs)
        {
            var builder = new StringBuilder();
            builder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            builder.AppendLine("<mediawiki xmlns=\"http://www.mediawiki.org/xml/export-0.10/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://www.mediawiki.org/xml/export-0.10/ http://www.mediawiki.org/xml/export-0.10.xsd\" version=\"0.10\" xml:lang=\"en\">");

            foreach (ModItem modItem in modItems)
            {
                Item item = ContentSamples.ItemsByType[modItem.Type];
                string displayName = item.Name;
                if (string.IsNullOrEmpty(displayName))
                {
                    displayName = modItem.Name;
                }

                string pageTitle = $"The Story of Red Cloud/{GetWikiPageName(displayName, modItem.Name)}";
                var pageContent = new StringBuilder();
                pageContent.AppendLine("{{mod sub-page}}");
                pageContent.AppendLine($"{{{{TSORC Item|id={modItem.Name}}}}}");

                WriteXmlPage(builder, pageTitle, pageContent.ToString());
            }

            foreach (ModNPC modNPC in modNPCs)
            {
                NPC npc = ContentSamples.NpcsByNetId[modNPC.Type];
                string displayName = Lang.GetNPCNameValue(npc.type);
                if (string.IsNullOrEmpty(displayName))
                {
                    displayName = modNPC.Name;
                }

                string pageTitle = $"The Story of Red Cloud/{GetWikiPageName(displayName, modNPC.Name)}";
                var pageContent = new StringBuilder();
                pageContent.AppendLine("{{mod sub-page}}");
                pageContent.AppendLine($"{{{{TSORC NPC|id={modNPC.Name}}}}}");

                WriteXmlPage(builder, pageTitle, pageContent.ToString());
            }

            builder.AppendLine("</mediawiki>");
            File.WriteAllText(filePath, builder.ToString(), Encoding.UTF8);
        }

        private Dictionary<int, List<DropSource>> BuildItemDropSources(IReadOnlyDictionary<int, ModItem> modItemsByType, IReadOnlyDictionary<int, ModNPC> modNPCsByType)
        {
            var sourcesByItemType = new Dictionary<int, List<DropSource>>();

            for (int npcType = 0; npcType < NPCLoader.NPCCount; npcType++)
            {
                if (!ContentSamples.NpcsByNetId.TryGetValue(npcType, out NPC npc))
                {
                    continue;
                }

                if (npc.ModNPC?.Mod == Mod && !modNPCsByType.ContainsKey(npcType))
                {
                    continue;
                }

                string sourceName = Lang.GetNPCNameValue(npcType);
                string sourceInternalName = modNPCsByType.TryGetValue(npcType, out ModNPC modNPC) ? modNPC.Name : null;
                AddDropSources(sourcesByItemType, modItemsByType, "npc", npcType, sourceName, sourceInternalName, GetLootTable(Main.ItemDropsDB.GetRulesForNPCID(npcType, includeGlobalDrops: false)));
            }

            for (int itemType = 0; itemType < ItemLoader.ItemCount; itemType++)
            {
                if (!ContentSamples.ItemsByType.TryGetValue(itemType, out Item item))
                {
                    continue;
                }

                string sourceInternalName = item.ModItem?.Name;
                AddDropSources(sourcesByItemType, modItemsByType, "item", itemType, item.Name, sourceInternalName, GetLootTable(Main.ItemDropsDB.GetRulesForItemID(itemType)));
            }

            return sourcesByItemType;
        }

        private static Dictionary<int, List<ShopSource>> BuildItemShopSources(IReadOnlyDictionary<int, ModItem> modItemsByType)
        {
            var sourcesByItemType = new Dictionary<int, List<ShopSource>>();

            foreach (AbstractNPCShop shop in NPCShopDatabase.AllShops)
            {
                foreach (AbstractNPCShop.Entry entry in shop.ActiveEntries)
                {
                    if (!modItemsByType.ContainsKey(entry.Item.type))
                    {
                        continue;
                    }

                    if (!sourcesByItemType.TryGetValue(entry.Item.type, out List<ShopSource> sources))
                    {
                        sources = new List<ShopSource>();
                        sourcesByItemType[entry.Item.type] = sources;
                    }

                    sources.Add(new ShopSource
                    {
                        NpcType = shop.NpcType,
                        NpcName = Lang.GetNPCNameValue(shop.NpcType),
                        ShopName = shop.Name,
                        Price = entry.Item.shopCustomPrice ?? entry.Item.value,
                        CurrencyId = entry.Item.shopSpecialCurrency,
                        Conditions = entry.Conditions.Select(condition => condition.Description.Value).ToList()
                    });
                }
            }

            return sourcesByItemType;
        }

        private static void AddDropSources(Dictionary<int, List<DropSource>> sourcesByItemType, IReadOnlyDictionary<int, ModItem> modItemsByType, string sourceKind, int sourceId, string sourceName, string sourceInternalName, IEnumerable<DropRateInfo> drops)
        {
            foreach (DropRateInfo drop in drops)
            {
                if (!modItemsByType.ContainsKey(drop.itemId))
                {
                    continue;
                }

                if (!sourcesByItemType.TryGetValue(drop.itemId, out List<DropSource> sources))
                {
                    sources = new List<DropSource>();
                    sourcesByItemType[drop.itemId] = sources;
                }

                sources.Add(new DropSource
                {
                    SourceKind = sourceKind,
                    SourceId = sourceId,
                    SourceName = sourceName,
                    SourceInternalName = sourceInternalName,
                    Drop = drop
                });
            }
        }

        private static List<DropRateInfo> GetLootTable(IEnumerable<IItemDropRule> rules)
        {
            var drops = new List<DropRateInfo>();
            var ratesInfo = new DropRateInfoChainFeed(1f);

            foreach (IItemDropRule rule in rules)
            {
                rule.ReportDroprates(drops, ratesInfo);
            }

            return drops;
        }

        private void AppendRecipes(StringBuilder builder, Item result)
        {
            builder.AppendLine("        recipes = {");
            for (int recipeIndex = 0; recipeIndex < Recipe.numRecipes; recipeIndex++)
            {
                Recipe recipe = Main.recipe[recipeIndex];
                if (recipe == null || recipe.Disabled || recipe.createItem.type != result.type)
                {
                    continue;
                }

                builder.AppendLine("            {");
                builder.AppendLine($"                resultStack = {recipe.createItem.stack},");
                builder.AppendLine($"                decraftDisabled = {ToLuaBool(recipe.DecraftDisabled)},");
                builder.AppendLine("                ingredients = {");
                foreach (Item ingredient in recipe.requiredItem.Where(ingredient => ingredient.type > ItemID.None && ingredient.stack > 0))
                {
                    string groupName = recipe.ProcessGroupsForText(ingredient.type, out string groupText) ? groupText : "";
                    AppendItemReference(builder, ingredient.type, ingredient.stack, groupName, "                    ");
                }
                builder.AppendLine("                },");
                builder.AppendLine("                stations = {");
                foreach (int tileType in recipe.requiredTile.Where(tileType => tileType >= 0))
                {
                    builder.AppendLine($"                    {{ id = {tileType}, name = \"{EscapeLua(GetTileName(tileType))}\" }},");
                }
                builder.AppendLine("                },");
                AppendConditions(builder, recipe.Conditions.Select(condition => condition.Description.Value), "                ");
                builder.AppendLine("            },");
            }
            builder.AppendLine("        },");
        }

        private void AppendDropSources(StringBuilder builder, IEnumerable<DropSource> sources)
        {
            builder.AppendLine("        dropSources = {");
            if (sources != null)
            {
                foreach (DropSource source in sources.OrderBy(source => source.SourceKind).ThenBy(source => source.SourceName).ThenBy(source => source.Drop.dropRate))
                {
                    builder.AppendLine("            {");
                    builder.AppendLine($"                kind = \"{source.SourceKind}\",");
                    builder.AppendLine($"                id = {source.SourceId},");
                    builder.AppendLine($"                name = \"{EscapeLua(source.SourceName)}\",");
                    builder.AppendLine($"                internalName = \"{EscapeLua(source.SourceInternalName)}\",");
                    AppendDropDetails(builder, source.Drop, "                ");
                    builder.AppendLine("            },");
                }
            }
            builder.AppendLine("        },");
        }

        private void AppendShopSources(StringBuilder builder, IEnumerable<ShopSource> shops)
        {
            builder.AppendLine("        shopSources = {");
            if (shops != null)
            {
                foreach (ShopSource shop in shops.OrderBy(shop => shop.NpcName).ThenBy(shop => shop.ShopName))
                {
                    builder.AppendLine("            {");
                    builder.AppendLine($"                npcId = {shop.NpcType},");
                    builder.AppendLine($"                npcName = \"{EscapeLua(shop.NpcName)}\",");
                    builder.AppendLine($"                shopName = \"{EscapeLua(shop.ShopName)}\",");
                    builder.AppendLine($"                price = {shop.Price},");
                    builder.AppendLine($"                currencyId = {shop.CurrencyId},");
                    AppendConditions(builder, shop.Conditions, "                ");
                    builder.AppendLine("            },");
                }
            }
            builder.AppendLine("        },");
        }

        private void AppendLootTable(StringBuilder builder, IEnumerable<DropRateInfo> drops)
        {
            builder.AppendLine("        loot = {");
            foreach (DropRateInfo drop in drops.OrderBy(drop => GetItemName(drop.itemId)).ThenBy(drop => drop.dropRate))
            {
                builder.AppendLine("            {");
                AppendItemReference(builder, drop.itemId, null, "", "                ");
                AppendDropDetails(builder, drop, "                ");
                builder.AppendLine("            },");
            }
            builder.AppendLine("        },");
        }

        private void AppendDropDetails(StringBuilder builder, DropRateInfo drop, string indent)
        {
            builder.AppendLine($"{indent}minStack = {drop.stackMin},");
            builder.AppendLine($"{indent}maxStack = {drop.stackMax},");
            builder.AppendLine($"{indent}chance = {FormatNumber(drop.dropRate)},");
            builder.AppendLine($"{indent}chancePercent = {FormatNumber(drop.dropRate * 100f)},");
            AppendConditions(builder, GetConditionDescriptions(drop.conditions), indent);
        }

        private static void AppendConditions(StringBuilder builder, IEnumerable<string> conditions, string indent)
        {
            builder.AppendLine($"{indent}conditions = {{");
            foreach (string condition in conditions.Where(condition => !string.IsNullOrWhiteSpace(condition)).Distinct())
            {
                builder.AppendLine($"{indent}    \"{EscapeLua(condition)}\",");
            }
            builder.AppendLine($"{indent}}},");
        }

        private static IEnumerable<string> GetConditionDescriptions(IEnumerable<IItemDropRuleCondition> conditions)
        {
            return conditions?.Select(condition => condition.GetConditionDescription()) ?? Enumerable.Empty<string>();
        }

        private static void AppendItemReference(StringBuilder builder, int itemType, int? stack, string groupName, string indent)
        {
            Item item = GetSampleItem(itemType);
            builder.AppendLine($"{indent}itemId = {itemType},");
            builder.AppendLine($"{indent}itemName = \"{EscapeLua(item.Name)}\",");
            builder.AppendLine($"{indent}itemInternalName = \"{EscapeLua(item.ModItem?.Name)}\",");
            if (stack.HasValue)
            {
                builder.AppendLine($"{indent}stack = {stack.Value},");
            }
            if (!string.IsNullOrEmpty(groupName))
            {
                builder.AppendLine($"{indent}recipeGroup = \"{EscapeLua(groupName)}\",");
            }
        }

        private static Item GetSampleItem(int itemType)
        {
            if (ContentSamples.ItemsByType.TryGetValue(itemType, out Item item))
            {
                return item;
            }

            var fallback = new Item();
            fallback.SetDefaults(itemType);
            return fallback;
        }

        private static string GetItemName(int itemType)
        {
            return GetSampleItem(itemType).Name;
        }

        private static string GetTileName(int tileType)
        {
            string name = Lang.GetMapObjectName(MapHelper.TileToLookup(tileType, Recipe.GetRequiredTileStyle(tileType)));
            return string.IsNullOrEmpty(name) ? $"Tile {tileType}" : name;
        }

        private void WriteXmlPage(StringBuilder builder, string title, string text)
        {
            builder.AppendLine("  <page>");
            builder.AppendLine($"    <title>{EscapeXml(title)}</title>");
            builder.AppendLine("    <ns>0</ns>");
            builder.AppendLine("    <revision>");
            builder.AppendLine("      <model>wikitext</model>");
            builder.AppendLine("      <format>text/x-wiki</format>");
            builder.AppendLine($"      <text xml:space=\"preserve\">{EscapeXml(text)}</text>");
            builder.AppendLine("    </revision>");
            builder.AppendLine("  </page>");
        }

        private string GetItemType(Item item)
        {
            if (item.accessory)
            {
                return "Accessory";
            }
            if (item.damage > 0)
            {
                return "Weapon";
            }
            if (item.headSlot != -1 || item.bodySlot != -1 || item.legSlot != -1)
            {
                return "Armor";
            }
            if (item.pick > 0 || item.axe > 0 || item.hammer > 0)
            {
                return "Tool";
            }
            if (item.ammo != AmmoID.None)
            {
                return "Ammo";
            }
            if (item.consumable && (item.buffType > 0 || item.healLife > 0 || item.healMana > 0))
            {
                return "Potion";
            }
            if (item.createTile >= 0 || item.createWall >= 0)
            {
                return "Tile";
            }
            if (item.material)
            {
                return "Crafting material";
            }
            return "Miscellaneous";
        }

        private string GetRarityName(int rare)
        {
            if (rare >= 12 || rare < -11)
            {
                var customRarity = RarityLoader.GetRarity(rare);
                if (customRarity != null)
                {
                    return customRarity.Name;
                }
            }

            switch (rare)
            {
                case -11: return "Amber";
                case -1: return "Gray";
                case 0: return "White";
                case 1: return "Blue";
                case 2: return "Green";
                case 3: return "Orange";
                case 4: return "LightRed";
                case 5: return "Pink";
                case 6: return "LightPurple";
                case 7: return "Lime";
                case 8: return "Yellow";
                case 9: return "Cyan";
                case 10: return "Red";
                case 11: return "Purple";
                case 12: return "Rainbow";
                case 13: return "FieryRed";
                default: return "White";
            }
        }

        private string GetTooltipText(Item item)
        {
            var tooltip = Lang.GetTooltip(item.type);
            if (tooltip == null)
            {
                return "";
            }

            var lines = new List<string>();
            for (int i = 0; i < tooltip.Lines; i++)
            {
                string line = tooltip.GetLine(i);
                if (!string.IsNullOrEmpty(line))
                {
                    lines.Add(line);
                }
            }
            return string.Join("\n", lines);
        }

        private static string ToLuaBool(bool value)
        {
            return value ? "true" : "false";
        }

        private static string FormatNumber(float value)
        {
            return value.ToString("0.########", CultureInfo.InvariantCulture);
        }

        private static string EscapeLua(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return "";
            }
            return str
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "");
        }

        private string EscapeXml(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return "";
            }
            return str
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }
}
