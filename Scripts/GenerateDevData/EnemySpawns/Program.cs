using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TsorcRevamp.DevData.EnemySpawns;

internal static class Program
{
    private static readonly string[] InventoryRoots = ["Enemies", "Bosses", "Special"];
    private static readonly string[] PhaseOrder = ["Pre-Hardmode", "Hardmode", "SuperHardmode"];
    private static readonly Dictionary<string, int> BiomeOrder = new(StringComparer.Ordinal)
    {
        ["Neutral Overworld (Forest)"] = 0,
        ["Neutral Underground / Caverns"] = 1,
        ["Corruption"] = 2,
        ["Crimson"] = 3,
        ["Hallow"] = 4,
        ["Jungle"] = 5,
        ["Snow"] = 6,
        ["Desert"] = 7,
        ["Ocean"] = 8,
        ["Dungeon"] = 9,
        ["Lihzahrd Temple"] = 10,
        ["Meteor"] = 11,
        ["Glowing Mushroom"] = 12,
        ["Graveyard"] = 13,
        ["Underworld"] = 14,
        ["Sky"] = 15,
        ["Arazium's Mountain Caverns"] = 20,
        ["Machine Temple"] = 21,
        ["Catacombs"] = 22,
        ["Molten Sky Temple"] = 23,
        ["Origin of the Abyss"] = 24,
        ["Remix Obsidian Zone"] = 25,
        ["Remix Reef"] = 26,
        ["General / Custom"] = 99,
    };

    public static int Main(string[] args)
    {
        try
        {
            bool checkOnly = args.Contains("--check", StringComparer.OrdinalIgnoreCase);
            string repositoryRoot = FindRepositoryRoot();
            RegressionTests.Run();
            Generator generator = new(repositoryRoot);
            GeneratedReports reports = generator.Generate();

            string outputDirectory = Path.Combine(repositoryRoot, "DevData", "EnemySpawns");
            Dictionary<string, string> outputs = new(StringComparer.Ordinal)
            {
                [Path.Combine(outputDirectory, "enemy_spawns_inventory.csv")] = reports.InventoryCsv,
                [Path.Combine(outputDirectory, "enemy_spawns_inventory.html")] = reports.InventoryHtml,
                [Path.Combine(outputDirectory, "biome_spawn_pools.html")] = reports.BiomePoolsHtml,
            };

            if (checkOnly)
            {
                List<string> stale = outputs
                    .Where(pair => !File.Exists(pair.Key) || NormalizeNewlines(File.ReadAllText(pair.Key)) != NormalizeNewlines(pair.Value))
                    .Select(pair => Path.GetRelativePath(repositoryRoot, pair.Key).Replace('\\', '/'))
                    .ToList();

                if (stale.Count > 0)
                {
                    Console.Error.WriteLine("Enemy spawn developer data is missing or stale:");
                    foreach (string path in stale)
                    {
                        Console.Error.WriteLine($"  {path}");
                    }
                    return 1;
                }

                Console.WriteLine($"Enemy spawn developer data is current ({reports.NpcCount} NPCs, {reports.SpawnRuleCount} spawn rules, {reports.PoolMutationCount} pool mutations, {reports.BlockRuleCount} block rules)." );
                return 0;
            }

            Directory.CreateDirectory(outputDirectory);
            foreach ((string path, string content) in outputs)
            {
                File.WriteAllText(path, NormalizeNewlines(content), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                Console.WriteLine($"Wrote {Path.GetRelativePath(repositoryRoot, path)}");
            }

            Console.WriteLine($"Inventoried {reports.NpcCount} NPCs, {reports.SpawnRuleCount} spawn rules, {reports.PoolMutationCount} pool mutations, and {reports.BlockRuleCount} Adventure Mode block rules.");
            if (reports.WarningCount > 0)
            {
                Console.WriteLine($"Preserved {reports.WarningCount} unresolved values/conditions for manual review (see the generated warnings sections)." );
            }
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static string FindRepositoryRoot()
    {
        foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            DirectoryInfo? directory = new(start);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "tsorcRevamp.csproj")))
                {
                    return directory.FullName;
                }
                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not find tsorcRevamp.csproj above the current or executable directory.");
    }

    private static string NormalizeNewlines(string value) => value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private sealed class Generator
    {
        private readonly string repositoryRoot;
        private readonly string npcsRoot;
        private readonly List<SourceClass> classes = [];
        private readonly Dictionary<string, List<SourceClass>> classesBySimpleName = new(StringComparer.Ordinal);
        private readonly List<string> warnings = [];
        private readonly Dictionary<string, string> displayNames;

        public Generator(string repositoryRoot)
        {
            this.repositoryRoot = repositoryRoot;
            npcsRoot = Path.Combine(repositoryRoot, "NPCs");
            displayNames = LoadDisplayNames(Path.Combine(repositoryRoot, "Localization", "en-US.hjson"));
        }

        public GeneratedReports Generate()
        {
            ParseClasses();
            List<SourceClass> npcClasses = classes
                .Where(sourceClass => sourceClass.IsInInventoryRoot && !sourceClass.IsAbstract && InheritsModNpc(sourceClass, []))
                .OrderBy(sourceClass => sourceClass.RelativePath, StringComparer.Ordinal)
                .ThenBy(sourceClass => sourceClass.Line)
                .ToList();

            EnsureUniqueNpcTypes(npcClasses);

            List<NpcInventory> inventory = npcClasses.Select(CreateInventory).ToList();
            List<PoolMutation> mutations = ParsePoolMutations();
            List<BlockRule> blockRules = ParseAdventureModeBlocks();
            List<PoolEntry> poolEntries = CreatePoolEntries(inventory, mutations);

            string inventoryCsv = RenderInventoryCsv(inventory);
            string inventoryHtml = RenderInventoryHtml(inventory);
            string biomeHtml = RenderBiomePoolsHtml(poolEntries);

            return new GeneratedReports(
                inventoryCsv,
                inventoryHtml,
                biomeHtml,
                inventory.Count,
                inventory.Sum(npc => npc.SpawnRules.Count),
                mutations.Count,
                blockRules.Count,
                warnings.Distinct(StringComparer.Ordinal).Count());
        }

        private void ParseClasses()
        {
            foreach (string path in Directory.EnumerateFiles(npcsRoot, "*.cs", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal))
            {
                string source = File.ReadAllText(path);
                SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: path);
                CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
                string relativePath = Relative(path);

                foreach (ClassDeclarationSyntax declaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    string namespaceName = GetNamespace(declaration);
                    string fullName = string.IsNullOrEmpty(namespaceName)
                        ? declaration.Identifier.ValueText
                        : $"{namespaceName}.{declaration.Identifier.ValueText}";
                    List<string> baseTypes = declaration.BaseList?.Types.Select(type => CleanTypeName(Code(type.Type))).ToList() ?? [];
                    int line = GetLine(declaration);
                    bool inInventoryRoot = InventoryRoots.Any(rootName => relativePath.StartsWith($"NPCs/{rootName}/", StringComparison.OrdinalIgnoreCase));

                    SourceClass sourceClass = new(
                        declaration.Identifier.ValueText,
                        fullName,
                        namespaceName,
                        baseTypes,
                        declaration,
                        relativePath,
                        line,
                        declaration.Modifiers.Any(SyntaxKind.AbstractKeyword),
                        inInventoryRoot);
                    classes.Add(sourceClass);

                    if (!classesBySimpleName.TryGetValue(sourceClass.Name, out List<SourceClass>? matches))
                    {
                        matches = [];
                        classesBySimpleName[sourceClass.Name] = matches;
                    }
                    matches.Add(sourceClass);
                }
            }
        }

        private bool InheritsModNpc(SourceClass sourceClass, HashSet<string> visiting)
        {
            if (!visiting.Add(sourceClass.FullName))
            {
                return false;
            }

            foreach (string baseType in sourceClass.BaseTypes)
            {
                string simpleName = SimpleTypeName(baseType);
                if (simpleName == "ModNPC")
                {
                    return true;
                }

                SourceClass? resolved = ResolveClass(sourceClass, baseType);
                if (resolved is not null && InheritsModNpc(resolved, new HashSet<string>(visiting, StringComparer.Ordinal)))
                {
                    return true;
                }
            }

            return false;
        }

        private SourceClass? ResolveClass(SourceClass context, string typeName)
        {
            string cleaned = CleanTypeName(typeName);
            SourceClass? exact = classes.FirstOrDefault(candidate => candidate.FullName == cleaned);
            if (exact is not null)
            {
                return exact;
            }

            string inNamespace = string.IsNullOrEmpty(context.NamespaceName) ? cleaned : $"{context.NamespaceName}.{cleaned}";
            exact = classes.FirstOrDefault(candidate => candidate.FullName == inNamespace);
            if (exact is not null)
            {
                return exact;
            }

            string simpleName = SimpleTypeName(cleaned);
            return classesBySimpleName.TryGetValue(simpleName, out List<SourceClass>? matches) && matches.Count == 1 ? matches[0] : null;
        }

        private static void EnsureUniqueNpcTypes(List<SourceClass> npcClasses)
        {
            List<IGrouping<string, SourceClass>> duplicates = npcClasses.GroupBy(sourceClass => sourceClass.FullName, StringComparer.Ordinal).Where(group => group.Count() > 1).ToList();
            if (duplicates.Count > 0)
            {
                throw new InvalidOperationException($"Duplicate NPC type declarations found: {string.Join(", ", duplicates.Select(group => group.Key))}");
            }
        }

        private NpcInventory CreateInventory(SourceClass sourceClass)
        {
            MethodDeclarationSyntax? defaults = sourceClass.Declaration.Members.OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(method => method.Identifier.ValueText == "SetDefaults");
            Dictionary<string, string> defaultAssignments = defaults is null ? [] : FindNpcAssignments(defaults);

            List<SpawnRule> spawnRules = [];
            foreach (MethodDeclarationSyntax method in sourceClass.Declaration.Members.OfType<MethodDeclarationSyntax>()
                         .Where(method => method.Identifier.ValueText is "SpawnChance" or "CanSpawnLegacy"))
            {
                spawnRules.AddRange(SpawnFlowAnalyzer.Analyze(method, sourceClass.RelativePath, warnings));
            }

            bool isBoss = IsTrue(defaultAssignments.GetValueOrDefault("boss"));
            bool isFriendly = IsTrue(defaultAssignments.GetValueOrDefault("friendly"));
            bool isTownNpc = IsTrue(defaultAssignments.GetValueOrDefault("townNPC"));
            string baseClass = sourceClass.BaseTypes.FirstOrDefault() ?? "(none)";
            string kind = sourceClass.RelativePath.Split('/').Skip(1).FirstOrDefault() ?? "NPC";
            string slots = defaultAssignments.GetValueOrDefault("npcSlots") ?? "1";
            string slotOverrides = FindRuntimeSlotOverrides(sourceClass, slots);
            string life = defaultAssignments.GetValueOrDefault("lifeMax") ?? "—";
            string damage = defaultAssignments.GetValueOrDefault("damage") ?? "—";
            string defense = defaultAssignments.GetValueOrDefault("defense") ?? "—";
            string spawnStatus = DetermineSpawnStatus(spawnRules);

            return new NpcInventory(
                sourceClass.Name,
                displayNames.GetValueOrDefault(sourceClass.Name) ?? FormatDisplayName(sourceClass.Name),
                kind,
                baseClass,
                spawnStatus,
                slots,
                slotOverrides,
                life,
                damage,
                defense,
                isBoss,
                isFriendly,
                isTownNpc,
                sourceClass.RelativePath,
                sourceClass.Line,
                spawnRules);
        }

        private static Dictionary<string, string> FindNpcAssignments(MethodDeclarationSyntax method)
        {
            Dictionary<string, string> values = new(StringComparer.Ordinal);
            foreach (AssignmentExpressionSyntax assignment in method.DescendantNodes().OfType<AssignmentExpressionSyntax>())
            {
                if (assignment.Left is MemberAccessExpressionSyntax member && Code(member.Expression) == "NPC")
                {
                    values[member.Name.Identifier.ValueText] = Code(assignment.Right);
                }
            }
            return values;
        }

        private static string DetermineSpawnStatus(List<SpawnRule> rules)
        {
            List<SpawnRule> current = rules.Where(rule => rule.Method == "SpawnChance").ToList();
            List<SpawnRule> legacy = rules.Where(rule => rule.Method == "CanSpawnLegacy").ToList();
            if (current.Any(rule => !rule.IsDefinitelyZero))
            {
                return "Natural spawn rules";
            }
            if (current.Count > 0 && legacy.Any(rule => !rule.IsDefinitelyZero))
            {
                return "Legacy spawn logic (current override disabled)";
            }
            if (current.Count > 0)
            {
                return "Natural spawning disabled";
            }
            if (legacy.Any(rule => !rule.IsDefinitelyZero))
            {
                return "Legacy spawn logic only";
            }
            return "No natural spawn method";
        }

        private static string FindRuntimeSlotOverrides(SourceClass sourceClass, string baseSlots)
        {
            List<string> values = sourceClass.Declaration.DescendantNodes().OfType<AssignmentExpressionSyntax>()
                .Where(assignment => Code(assignment.Left) == "NPC.npcSlots")
                .Select(assignment => Code(assignment.Right))
                .Distinct(StringComparer.Ordinal)
                .Where(value => value != baseSlots)
                .ToList();
            return values.Count == 0 ? "—" : string.Join(" / ", values);
        }

        private List<PoolMutation> ParsePoolMutations()
        {
            string path = Path.Combine(npcsRoot, "GlobalNPC.cs");
            SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path);
            MethodDeclarationSyntax method = tree.GetCompilationUnitRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(candidate => candidate.Identifier.ValueText == "EditSpawnPool")
                ?? throw new InvalidOperationException("Could not find GlobalNPC.EditSpawnPool.");
            return MutationAnalyzer.Analyze(method, Relative(path));
        }

        private List<BlockRule> ParseAdventureModeBlocks()
        {
            string path = Path.Combine(npcsRoot, "VanillaChanges.cs");
            SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path);
            MethodDeclarationSyntax method = tree.GetCompilationUnitRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(candidate => candidate.Identifier.ValueText == "AI" && candidate.ParameterList.Parameters.Any(parameter => parameter.Identifier.ValueText == "npc"))
                ?? throw new InvalidOperationException("Could not find VanillaChanges.AI(NPC npc).");
            return BlockAnalyzer.Analyze(method, Relative(path));
        }

        private List<PoolEntry> CreatePoolEntries(List<NpcInventory> inventory, List<PoolMutation> mutations)
        {
            Dictionary<string, NpcInventory> inventoryByName = inventory.ToDictionary(npc => npc.InternalName, StringComparer.Ordinal);
            List<PoolEntry> entries = [];

            foreach (NpcInventory npc in inventory)
            {
                foreach (SpawnRule rule in npc.SpawnRules.Where(rule => rule.Method == "SpawnChance" && !rule.IsDefinitelyZero))
                {
                    ConditionFacts facts = ConditionFacts.From(rule.Conditions);
                    if (facts.IsDefinitelyImpossible())
                    {
                        continue;
                    }
                    foreach (string phase in facts.Phases())
                    {
                        entries.Add(new PoolEntry(
                            phase,
                            facts.Biome(),
                            facts.Depth(),
                            facts.Time(),
                            "Mod SpawnChance",
                            npc.InternalName,
                            npc.DisplayName,
                            npc.NpcSlots,
                            rule.ResolvedWeight,
                            facts.RandomGate(),
                            facts.Water(),
                            rule.ConditionText,
                            rule.SourceFile,
                            rule.Line));
                    }
                }
            }

            foreach (PoolMutation mutation in mutations.Where(mutation => mutation.Operation == "Add"))
            {
                ConditionFacts facts = ConditionFacts.From(mutation.Conditions);
                if (facts.IsDefinitelyImpossible())
                {
                    continue;
                }
                string displayName = inventoryByName.TryGetValue(mutation.TargetName, out NpcInventory? npc)
                    ? npc.DisplayName
                    : FormatDisplayName(mutation.TargetName);
                string slots = npc?.NpcSlots ?? "vanilla/default";
                foreach (string phase in facts.Phases())
                {
                    entries.Add(new PoolEntry(
                        phase,
                        facts.Biome(),
                        facts.Depth(),
                        facts.Time(),
                        mutation.IsModNpc ? "Global mod pool add" : "Global vanilla pool add",
                        mutation.TargetName,
                        displayName,
                        slots,
                        mutation.Weight ?? "—",
                        facts.RandomGate(),
                        facts.Water(),
                        mutation.ConditionText,
                        mutation.SourceFile,
                        mutation.Line));
                }
            }

            return entries;
        }

        private string RenderInventoryCsv(List<NpcInventory> inventory)
        {
            StringBuilder output = new();
            WriteCsvRow(output,
                "Record Type", "Internal Name", "Display Name", "Category", "Base Class", "Spawn Status", "NPC Slots",
                "Runtime Slot Overrides", "Life Max", "Damage", "Defense", "NPC.boss", "Friendly", "Town NPC", "Rule Method", "Rule Weight",
                "Progression", "Biome", "Depth", "Time", "Random Gate", "Water", "Conditions", "Source File", "Source Line");

            foreach (NpcInventory npc in inventory.OrderBy(npc => npc.InternalName, StringComparer.Ordinal))
            {
                List<SpawnRule> rules = npc.SpawnRules.Count == 0 ? [SpawnRule.Empty(npc.SourceFile, npc.SourceLine)] : npc.SpawnRules;
                foreach (SpawnRule rule in rules.OrderBy(rule => rule.Line))
                {
                    ConditionFacts facts = ConditionFacts.From(rule.Conditions);
                    bool hasRule = rule.Method != "None";
                    WriteCsvRow(output,
                        rule.Method == "None" ? "NPC" : "NPC Spawn Rule",
                        npc.InternalName,
                        npc.DisplayName,
                        npc.Category,
                        npc.BaseClass,
                        npc.SpawnStatus,
                        npc.NpcSlots,
                        npc.RuntimeSlotOverrides,
                        npc.LifeMax,
                        npc.Damage,
                        npc.Defense,
                        YesNo(npc.IsBoss),
                        YesNo(npc.IsFriendly),
                        YesNo(npc.IsTownNpc),
                        rule.Method,
                        rule.ResolvedWeight,
                        hasRule ? string.Join(" + ", facts.Phases()) : "—",
                        hasRule ? facts.Biome() : "—",
                        hasRule ? facts.Depth() : "—",
                        hasRule ? facts.Time() : "—",
                        hasRule ? facts.RandomGate() : "—",
                        hasRule ? facts.Water() : "—",
                        hasRule ? rule.ConditionText : "—",
                        rule.SourceFile,
                        rule.Line.ToString(CultureInfo.InvariantCulture));
                }
            }
            return output.ToString();
        }

        private string RenderInventoryHtml(List<NpcInventory> inventory)
        {
            StringBuilder output = new();
            output.AppendLine("""
                <!doctype html>
                <html lang="en">
                <head>
                  <meta charset="utf-8">
                  <meta name="viewport" content="width=device-width, initial-scale=1">
                  <title>tsorcRevamp Enemy Spawn Inventory</title>
                  <style>
                    :root {
                      color-scheme: dark;
                      --background: #0d1117;
                      --panel: #161b22;
                      --panel-raised: #1c222b;
                      --border: #30363d;
                      --text: #e6edf3;
                      --muted: #9da7b3;
                      --accent: #58a6ff;
                      --rule: #d2a8ff;
                      --condition: #11161d;
                    }
                    * { box-sizing: border-box; }
                    html { scroll-behavior: smooth; }
                    body {
                      margin: 0;
                      background: var(--background);
                      color: var(--text);
                      font: 16px/1.5 system-ui, -apple-system, "Segoe UI", sans-serif;
                    }
                    a { color: var(--accent); }
                    code {
                      font-family: "Cascadia Code", "SFMono-Regular", Consolas, monospace;
                      font-size: .92em;
                    }
                    .page { width: 100%; padding: clamp(16px, 2.5vw, 40px); }
                    .intro { max-width: 1100px; margin-bottom: 24px; }
                    h1 { margin: 0 0 8px; font-size: clamp(1.7rem, 3vw, 2.5rem); }
                    .muted { color: var(--muted); }
                    .toolbar {
                      position: sticky;
                      top: 0;
                      z-index: 10;
                      display: flex;
                      gap: 12px;
                      align-items: center;
                      padding: 12px 0;
                      background: color-mix(in srgb, var(--background) 94%, transparent);
                      backdrop-filter: blur(8px);
                    }
                    #search {
                      width: min(680px, 100%);
                      padding: 10px 12px;
                      border: 1px solid var(--border);
                      border-radius: 7px;
                      background: var(--panel);
                      color: var(--text);
                      font: inherit;
                    }
                    #visible-count { white-space: nowrap; color: var(--muted); }
                    .inventory { display: grid; gap: 18px; }
                    .npc-card {
                      width: 100%;
                      overflow: hidden;
                      border: 1px solid var(--border);
                      border-radius: 10px;
                      background: var(--panel);
                    }
                    .npc-header { padding: 18px 20px; background: var(--panel-raised); }
                    .npc-title-row {
                      display: flex;
                      flex-wrap: wrap;
                      justify-content: space-between;
                      gap: 10px 20px;
                      align-items: baseline;
                    }
                    .npc-title { margin: 0; font-size: 1.35rem; }
                    .internal-name { color: var(--muted); }
                    .facts, .rule-facts {
                      display: grid;
                      grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
                      gap: 10px 18px;
                      margin-top: 14px;
                    }
                    .fact { min-width: 0; }
                    .label {
                      display: block;
                      margin-bottom: 2px;
                      color: var(--muted);
                      font-size: .72rem;
                      font-weight: 700;
                      letter-spacing: .06em;
                      text-transform: uppercase;
                    }
                    .value { overflow-wrap: anywhere; }
                    .rule { padding: 18px 20px; border-top: 1px solid var(--border); }
                    .rule:nth-child(even) { background: rgb(255 255 255 / 1.5%); }
                    .rule-heading {
                      display: flex;
                      flex-wrap: wrap;
                      justify-content: space-between;
                      gap: 8px 18px;
                      align-items: baseline;
                    }
                    .rule-title { margin: 0; color: var(--rule); font-size: 1.08rem; }
                    .condition {
                      margin-top: 14px;
                      padding: 12px 14px;
                      border-left: 3px solid var(--rule);
                      border-radius: 4px;
                      background: var(--condition);
                    }
                    .translation {
                      margin-top: 14px;
                      padding: 12px 14px;
                      border-left: 3px solid var(--accent);
                      border-radius: 4px;
                      background: rgb(88 166 255 / 8%);
                    }
                    .translation ul { margin: 6px 0 0; padding-left: 22px; }
                    .translation li + li { margin-top: 5px; }
                    .condition code {
                      display: block;
                      white-space: pre-wrap;
                      overflow-wrap: anywhere;
                      word-break: break-word;
                    }
                    .empty { padding: 18px 20px; border-top: 1px solid var(--border); color: var(--muted); }
                    .warnings { margin-top: 28px; padding: 18px 20px; border: 1px solid #9e6a03; border-radius: 10px; }
                    .warnings h2 { margin-top: 0; }
                    [hidden] { display: none !important; }
                    @media (max-width: 640px) {
                      .page { padding: 12px; }
                      .toolbar { align-items: stretch; flex-direction: column; }
                      #search { width: 100%; }
                      .facts, .rule-facts { grid-template-columns: repeat(auto-fit, minmax(130px, 1fr)); }
                    }
                  </style>
                </head>
                <body>
                  <main class="page">
                    <header class="intro">
                      <h1>Enemy Spawn Inventory</h1>
                      <p class="muted">Generated from the C# source by <code>Scripts/GenerateDevData/EnemySpawns</code>. Do not edit this file by hand.</p>
                """);
            output.AppendLine($"      <p><strong>{inventory.Count}</strong> concrete mod NPCs · <strong>{inventory.Sum(npc => npc.SpawnRules.Count)}</strong> spawn-method return paths</p>");
            output.AppendLine("""
                      <p>Abstract implementation bases, including <code>PuppetNPC</code>, are excluded. Concrete classes inheriting through those bases are included. Zero-weight rules remain visible because they document explicit spawn blockers.</p>
                      <p class="muted">Human-readable conditions are generated explanations. The exact C# condition beneath each explanation remains the authoritative reference.</p>
                    </header>
                    <div class="toolbar">
                      <input id="search" type="search" placeholder="Filter by enemy, biome, condition, source…" aria-label="Filter enemy spawn inventory">
                      <span id="visible-count" aria-live="polite"></span>
                    </div>
                    <div class="inventory" id="inventory">
                """);

            foreach (NpcInventory npc in inventory.OrderBy(npc => npc.InternalName, StringComparer.Ordinal))
            {
                string flags = string.Join(", ", new[]
                {
                    npc.IsBoss ? "NPC.boss" : null,
                    npc.IsFriendly ? "Friendly" : null,
                    npc.IsTownNpc ? "Town NPC" : null,
                }.Where(flag => flag is not null)) is { Length: > 0 } value ? value : "—";
                string slots = npc.RuntimeSlotOverrides == "—"
                    ? npc.NpcSlots
                    : $"{npc.NpcSlots} (runtime: {npc.RuntimeSlotOverrides})";
                string searchText = string.Join(" ", new[]
                {
                    npc.DisplayName,
                    npc.InternalName,
                    npc.Category,
                    npc.BaseClass,
                    npc.SpawnStatus,
                    string.Join(" ", npc.SpawnRules.Select(rule => rule.ConditionText)),
                });

                output.AppendLine($"      <section class=\"npc-card\" data-search=\"{EscapeHtml(searchText.ToLowerInvariant())}\">");
                output.AppendLine("        <header class=\"npc-header\">");
                output.AppendLine("          <div class=\"npc-title-row\">");
                output.AppendLine($"            <h2 class=\"npc-title\">{EscapeHtml(npc.DisplayName)} <code class=\"internal-name\">{EscapeHtml(npc.InternalName)}</code></h2>");
                output.AppendLine($"            <span>{HtmlSourceLink(npc.SourceFile, npc.SourceLine)}</span>");
                output.AppendLine("          </div>");
                output.AppendLine("          <div class=\"facts\">");
                AppendHtmlFact(output, "Spawn status", npc.SpawnStatus);
                AppendHtmlFact(output, "Category", npc.Category);
                AppendHtmlFact(output, "Base class", npc.BaseClass, code: true);
                AppendHtmlFact(output, "NPC slots", slots);
                AppendHtmlFact(output, "Life", npc.LifeMax);
                AppendHtmlFact(output, "Damage", npc.Damage);
                AppendHtmlFact(output, "Defense", npc.Defense);
                AppendHtmlFact(output, "Flags", flags);
                output.AppendLine("          </div>");
                output.AppendLine("        </header>");

                int ruleNumber = 0;
                foreach (SpawnRule rule in npc.SpawnRules.OrderBy(rule => rule.Line).ThenBy(rule => rule.Method, StringComparer.Ordinal))
                {
                    ruleNumber++;
                    ConditionFacts facts = ConditionFacts.From(rule.Conditions);
                    string method = rule.Method == "CanSpawnLegacy" ? "CanSpawnLegacy (legacy)" : rule.Method;
                    output.AppendLine("        <article class=\"rule\">");
                    output.AppendLine("          <div class=\"rule-heading\">");
                    output.AppendLine($"            <h3 class=\"rule-title\">Rule {ruleNumber} · <code>{EscapeHtml(method)}</code></h3>");
                    output.AppendLine($"            <span>{HtmlSourceLink(rule.SourceFile, rule.Line)}</span>");
                    output.AppendLine("          </div>");
                    output.AppendLine("          <div class=\"rule-facts\">");
                    AppendHtmlFact(output, "Weight", rule.ResolvedWeight, code: true);
                    AppendHtmlFact(output, "Progression", string.Join(" + ", facts.Phases()));
                    AppendHtmlFact(output, "Biome", facts.Biome());
                    AppendHtmlFact(output, "Depth", facts.Depth());
                    AppendHtmlFact(output, "Time", facts.Time());
                    AppendHtmlFact(output, "Random gate", facts.RandomGate());
                    AppendHtmlFact(output, "Water", facts.Water());
                    output.AppendLine("          </div>");
                    output.AppendLine("          <div class=\"translation\">");
                    output.AppendLine("            <span class=\"label\">Human-readable conditions</span>");
                    if (rule.Conditions.Count == 0)
                    {
                        output.AppendLine("            <div>Always reaches this spawn return path.</div>");
                    }
                    else
                    {
                        output.AppendLine("            <ul>");
                        foreach (string condition in rule.Conditions)
                        {
                            output.AppendLine($"              <li>{EscapeHtml(HumanizeCondition(condition))}</li>");
                        }
                        output.AppendLine("            </ul>");
                    }
                    output.AppendLine("          </div>");
                    output.AppendLine("          <div class=\"condition\">");
                    output.AppendLine("            <span class=\"label\">Exact condition</span>");
                    output.AppendLine($"            <code>{EscapeHtml(rule.ConditionText)}</code>");
                    output.AppendLine("          </div>");
                    output.AppendLine("        </article>");
                }

                if (npc.SpawnRules.Count == 0)
                {
                    output.AppendLine("        <div class=\"empty\">No <code>SpawnChance</code> or <code>CanSpawnLegacy</code> return paths.</div>");
                }
                output.AppendLine("      </section>");
            }

            output.AppendLine("    </div>");
            List<string> distinctWarnings = warnings.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToList();
            if (distinctWarnings.Count > 0)
            {
                output.AppendLine("    <section class=\"warnings\">");
                output.AppendLine("      <h2>Generator warnings</h2>");
                output.AppendLine("      <p>These expressions were preserved instead of guessed and may require manual review.</p>");
                output.AppendLine("      <ul>");
                foreach (string warning in distinctWarnings)
                {
                    output.AppendLine($"        <li>{EscapeHtml(warning)}</li>");
                }
                output.AppendLine("      </ul>");
                output.AppendLine("    </section>");
            }
            output.AppendLine("""
                  </main>
                  <script>
                    const cards = [...document.querySelectorAll('.npc-card')];
                    const search = document.querySelector('#search');
                    const visibleCount = document.querySelector('#visible-count');
                    function filterInventory() {
                      const terms = search.value.toLocaleLowerCase().trim().split(/\s+/).filter(Boolean);
                      let visible = 0;
                      for (const card of cards) {
                        const matches = terms.every(term => card.dataset.search.includes(term));
                        card.hidden = !matches;
                        if (matches) visible++;
                      }
                      visibleCount.textContent = `${visible} of ${cards.length} NPCs`;
                    }
                    search.addEventListener('input', filterInventory);
                    filterInventory();
                  </script>
                </body>
                </html>
                """);
            return output.ToString();
        }

        private static void AppendHtmlFact(StringBuilder output, string label, string value, bool code = false)
        {
            string escapedValue = EscapeHtml(value);
            string renderedValue = code ? $"<code>{escapedValue}</code>" : escapedValue;
            output.AppendLine($"            <div class=\"fact\"><span class=\"label\">{EscapeHtml(label)}</span><span class=\"value\">{renderedValue}</span></div>");
        }

        private string RenderBiomePoolsHtml(List<PoolEntry> entries)
        {
            StringBuilder output = new();
            output.AppendLine("""
                <!doctype html>
                <html lang="en">
                <head>
                  <meta charset="utf-8">
                  <meta name="viewport" content="width=device-width, initial-scale=1">
                  <title>tsorcRevamp Biome Spawn Pools</title>
                  <style>
                    :root {
                      color-scheme: dark;
                      --background: #0d1117;
                      --panel: #161b22;
                      --panel-raised: #1c222b;
                      --border: #30363d;
                      --text: #e6edf3;
                      --muted: #9da7b3;
                      --accent: #58a6ff;
                      --weight: #d2a8ff;
                    }
                    * { box-sizing: border-box; }
                    html { scroll-behavior: smooth; }
                    body {
                      margin: 0;
                      background: var(--background);
                      color: var(--text);
                      font: 16px/1.45 system-ui, -apple-system, "Segoe UI", sans-serif;
                    }
                    a { color: var(--accent); text-decoration: none; }
                    a:hover { text-decoration: underline; }
                    code { font-family: "Cascadia Code", "SFMono-Regular", Consolas, monospace; }
                    .page { width: 100%; padding: clamp(16px, 2.5vw, 40px); }
                    .intro { max-width: 1050px; }
                    h1 { margin: 0 0 8px; font-size: clamp(1.8rem, 3vw, 2.6rem); }
                    .muted { color: var(--muted); }
                    .phase-nav {
                      position: sticky;
                      top: 0;
                      z-index: 10;
                      display: flex;
                      flex-wrap: wrap;
                      gap: 10px;
                      padding: 14px 0;
                      background: rgb(13 17 23 / 94%);
                      backdrop-filter: blur(8px);
                    }
                    .phase-nav a {
                      padding: 8px 14px;
                      border: 1px solid var(--border);
                      border-radius: 999px;
                      background: var(--panel);
                      font-weight: 650;
                    }
                    .phase { margin-top: 34px; scroll-margin-top: 72px; }
                    .phase-heading {
                      display: flex;
                      flex-wrap: wrap;
                      align-items: baseline;
                      gap: 10px;
                      margin-bottom: 14px;
                    }
                    .phase-heading h2 { margin: 0; font-size: 1.65rem; }
                    .biome-grid {
                      display: grid;
                      grid-template-columns: repeat(auto-fit, minmax(min(340px, 100%), 1fr));
                      gap: 16px;
                      align-items: start;
                    }
                    .biome {
                      overflow: hidden;
                      border: 1px solid var(--border);
                      border-radius: 10px;
                      background: var(--panel);
                    }
                    .biome h3 {
                      margin: 0;
                      padding: 13px 16px;
                      border-bottom: 1px solid var(--border);
                      background: var(--panel-raised);
                      font-size: 1.08rem;
                    }
                    table { width: 100%; border-collapse: collapse; }
                    th, td { padding: 9px 12px; border-bottom: 1px solid var(--border); text-align: left; vertical-align: top; }
                    tr:last-child td { border-bottom: 0; }
                    th {
                      color: var(--muted);
                      font-size: .72rem;
                      letter-spacing: .06em;
                      text-transform: uppercase;
                    }
                    th:last-child, td:last-child { width: 42%; }
                    .weight-set { display: flex; flex-wrap: wrap; gap: 5px; }
                    .weight {
                      display: inline-flex;
                      flex-direction: column;
                      gap: 5px;
                      align-items: flex-start;
                      padding: 2px 7px;
                      border-radius: 999px;
                      background: rgb(210 168 255 / 12%);
                      color: var(--weight);
                      white-space: nowrap;
                    }
                    .calculation { color: var(--muted); font-size: .72rem; }
                    @media (max-width: 520px) {
                      .page { padding: 12px; }
                      th, td { padding: 8px 10px; }
                    }
                  </style>
                </head>
                <body>
                  <main class="page">
                    <header class="intro">
                      <h1>Biome Spawn Pools</h1>
                      <p>Enemies grouped only by progression phase and biome, with their gate-adjusted average weights.</p>
                      <p class="muted">A rule returning weight <code>1</code> behind <code>NextBool(5)</code> is shown as <code>0.2</code>, with <code>1 × 1/5</code> beneath it. These are average pool contributions, not final spawn percentages. Exact conditions remain available in <a href="enemy_spawns_inventory.html">the full enemy inventory</a>.</p>
                    </header>
                    <nav class="phase-nav" aria-label="Progression phases">
                      <a href="#pre-hardmode">Pre-Hardmode</a>
                      <a href="#hardmode">Hardmode</a>
                      <a href="#superhardmode">Super Hardmode</a>
                    </nav>
                """);

            foreach (string phase in PhaseOrder)
            {
                List<PoolEntry> phaseEntries = entries.Where(entry => entry.Phase == phase).ToList();
                int enemyCount = phaseEntries.Select(entry => entry.InternalName).Distinct(StringComparer.Ordinal).Count();
                string id = phase switch
                {
                    "Pre-Hardmode" => "pre-hardmode",
                    "Hardmode" => "hardmode",
                    _ => "superhardmode",
                };
                string heading = phase == "SuperHardmode" ? "Super Hardmode" : phase;
                output.AppendLine($"    <section class=\"phase\" id=\"{id}\">");
                output.AppendLine($"      <div class=\"phase-heading\"><h2>{EscapeHtml(heading)}</h2><span class=\"muted\">{enemyCount} enemies</span></div>");
                output.AppendLine("      <div class=\"biome-grid\">");

                foreach (IGrouping<string, PoolEntry> biomeGroup in phaseEntries
                             .GroupBy(entry => entry.Biome, StringComparer.Ordinal)
                             .OrderBy(group => BiomeOrder.GetValueOrDefault(group.Key, 98))
                             .ThenBy(group => group.Key, StringComparer.Ordinal))
                {
                    List<IGrouping<(string InternalName, string DisplayName), PoolEntry>> enemies = biomeGroup
                        .GroupBy(entry => (entry.InternalName, entry.DisplayName))
                        .OrderBy(group => group.Key.DisplayName, StringComparer.Ordinal)
                        .ThenBy(group => group.Key.InternalName, StringComparer.Ordinal)
                        .ToList();
                    output.AppendLine("        <article class=\"biome\">");
                    output.AppendLine($"          <h3>{EscapeHtml(biomeGroup.Key)} <span class=\"muted\">({enemies.Count})</span></h3>");
                    output.AppendLine("          <table>");
                    output.AppendLine("            <thead><tr><th>Enemy</th><th>Effective average weight</th></tr></thead>");
                    output.AppendLine("            <tbody>");
                    foreach (IGrouping<(string InternalName, string DisplayName), PoolEntry> enemy in enemies)
                    {
                        PoolEntry source = enemy.OrderBy(entry => entry.SourceFile, StringComparer.Ordinal).ThenBy(entry => entry.Line).First();
                        List<(string Weight, string RandomGate)> weights = enemy
                            .Select(entry => (entry.Weight, entry.RandomGate))
                            .Distinct()
                            .OrderBy(value => EffectivePoolWeight(value.Weight, value.RandomGate) is null)
                            .ThenBy(value => EffectivePoolWeight(value.Weight, value.RandomGate) ?? 0d)
                            .ThenBy(value => value.Weight, StringComparer.Ordinal)
                            .ThenBy(value => value.RandomGate, StringComparer.Ordinal)
                            .ToList();
                        string weightHtml = string.Join(string.Empty, weights.Select(value => RenderPoolWeight(value.Weight, value.RandomGate)));
                        output.AppendLine($"              <tr><td>{HtmlSourceLink(source.SourceFile, source.Line, enemy.Key.DisplayName)}</td><td><div class=\"weight-set\">{weightHtml}</div></td></tr>");
                    }
                    output.AppendLine("            </tbody>");
                    output.AppendLine("          </table>");
                    output.AppendLine("        </article>");
                }
                output.AppendLine("      </div>");
                output.AppendLine("    </section>");
            }

            output.AppendLine("""
                  </main>
                </body>
                </html>
                """);
            return output.ToString();
        }

        private static string RenderPoolWeight(string weight, string randomGate)
        {
            double? effective = EffectivePoolWeight(weight, randomGate);
            if (randomGate == "—")
            {
                return $"<span class=\"weight\"><code>{EscapeHtml(weight)}</code></span>";
            }

            string displayed = effective is null ? $"{weight} × {randomGate}" : FormatWeight(effective.Value);
            string calculation = effective is null
                ? string.Empty
                : $"<span class=\"calculation\">{EscapeHtml(weight)} × {EscapeHtml(randomGate)}</span>";
            return $"<span class=\"weight\"><code>{EscapeHtml(displayed)}</code>{calculation}</span>";
        }

        internal static double? EffectivePoolWeight(string weight, string randomGate)
        {
            double? numericWeight = NumericExpression.TryEvaluate(weight);
            if (numericWeight is null)
            {
                return null;
            }
            if (randomGate == "—")
            {
                return numericWeight;
            }

            MatchCollection gates = Regex.Matches(randomGate, @"1/(?<denominator>\d+)");
            if (gates.Count == 0 || randomGate.StartsWith("Conditional:", StringComparison.Ordinal))
            {
                return null;
            }
            double probability = 1d;
            foreach (Match gate in gates)
            {
                if (!double.TryParse(gate.Groups["denominator"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out double denominator) || denominator <= 0d)
                {
                    return null;
                }
                probability /= denominator;
            }
            return numericWeight.Value * probability;
        }

        private static string FormatWeight(double value) => value.ToString("0.########", CultureInfo.InvariantCulture);

        private void AppendWarnings(StringBuilder output)
        {
            if (warnings.Count == 0)
            {
                return;
            }

            output.AppendLine();
            output.AppendLine("# Generator Review Notes");
            output.AppendLine();
            output.AppendLine("The generator preserved these expressions instead of replacing them with guessed numeric values:");
            output.AppendLine();
            foreach (string warning in warnings.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal))
            {
                output.AppendLine($"- {EscapeMarkdown(warning)}");
            }
        }

        private string Relative(string path) => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');

        private static string GetNamespace(SyntaxNode node)
        {
            IEnumerable<string> names = node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(declaration => Code(declaration.Name));
            return string.Join(".", names);
        }

        private static int GetLine(SyntaxNode node) => node.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1;

        private static string CleanTypeName(string typeName)
        {
            string value = typeName.Trim().Replace("global::", string.Empty, StringComparison.Ordinal);
            int generic = value.IndexOf('<');
            return generic >= 0 ? value[..generic] : value;
        }

        private static string SimpleTypeName(string typeName) => CleanTypeName(typeName).Split('.').Last();

        private static Dictionary<string, string> LoadDisplayNames(string path)
        {
            Dictionary<string, string> names = new(StringComparer.Ordinal);
            if (!File.Exists(path))
            {
                return names;
            }

            string? activeBlock = null;
            bool inNpcSection = false;
            int npcIndent = -1;
            int blockIndent = -1;
            foreach (string rawLine in File.ReadLines(path))
            {
                string trimmed = rawLine.Trim();
                int indent = rawLine.TakeWhile(char.IsWhiteSpace).Count();
                if (!inNpcSection)
                {
                    if (trimmed == "NPCs: {")
                    {
                        inNpcSection = true;
                        npcIndent = indent;
                    }
                    continue;
                }

                if (indent <= npcIndent && trimmed == "}")
                {
                    break;
                }

                Match dotted = Regex.Match(trimmed, @"^(?<name>\w+)\.DisplayName\s*:\s*(?<value>.+)$");
                if (dotted.Success)
                {
                    names[dotted.Groups["name"].Value] = CleanHjsonValue(dotted.Groups["value"].Value);
                    continue;
                }

                Match block = Regex.Match(trimmed, @"^(?<name>\w+)\s*:\s*\{$");
                if (block.Success)
                {
                    activeBlock = block.Groups["name"].Value;
                    blockIndent = indent;
                    continue;
                }

                if (activeBlock is not null && indent > blockIndent)
                {
                    Match display = Regex.Match(trimmed, @"^DisplayName\s*:\s*(?<value>.+)$");
                    if (display.Success)
                    {
                        names[activeBlock] = CleanHjsonValue(display.Groups["value"].Value);
                    }
                }
                else if (activeBlock is not null && indent <= blockIndent)
                {
                    activeBlock = null;
                }
            }
            return names;
        }

        private static string CleanHjsonValue(string value)
        {
            string trimmed = value.Trim();
            if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
            {
                return trimmed[1..^1];
            }
            return trimmed;
        }
    }

    private static class SpawnFlowAnalyzer
    {
        public static List<SpawnRule> Analyze(MethodDeclarationSyntax method, string sourceFile, List<string> warnings)
        {
            List<SpawnRule> rules = [];
            if (method.ExpressionBody is not null)
            {
                AddRule(method.Identifier.ValueText, method.ExpressionBody.Expression, new FlowState(), sourceFile, rules, warnings);
                return rules;
            }

            if (method.Body is null)
            {
                return rules;
            }

            ProcessStatements(method.Body.Statements, [new FlowState()], method.Identifier.ValueText, sourceFile, rules, warnings);
            return rules;
        }

        private static List<FlowState> ProcessStatements(
            SyntaxList<StatementSyntax> statements,
            List<FlowState> states,
            string method,
            string sourceFile,
            List<SpawnRule> rules,
            List<string> warnings)
        {
            List<FlowState> current = states;
            foreach (StatementSyntax statement in statements)
            {
                current = current.SelectMany(state => ProcessStatement(statement, state, method, sourceFile, rules, warnings)).ToList();
                if (current.Count == 0)
                {
                    break;
                }
            }
            return current;
        }

        private static List<FlowState> ProcessStatement(
            StatementSyntax statement,
            FlowState state,
            string method,
            string sourceFile,
            List<SpawnRule> rules,
            List<string> warnings)
        {
            switch (statement)
            {
                case BlockSyntax block:
                    return ProcessStatements(block.Statements, [state], method, sourceFile, rules, warnings);

                case LocalDeclarationStatementSyntax declaration:
                    FlowState declared = state.Clone();
                    foreach (VariableDeclaratorSyntax variable in declaration.Declaration.Variables)
                    {
                        if (variable.Initializer is not null)
                        {
                            declared.Variables[variable.Identifier.ValueText] = ResolveExpression(variable.Initializer.Value, declared);
                        }
                    }
                    return [declared];

                case ExpressionStatementSyntax expressionStatement when expressionStatement.Expression is AssignmentExpressionSyntax assignment:
                    FlowState assigned = state.Clone();
                    ApplyAssignment(assignment, assigned);
                    return [assigned];

                case ReturnStatementSyntax returnStatement:
                    if (returnStatement.Expression is not null)
                    {
                        AddRule(method, returnStatement.Expression, state, sourceFile, rules, warnings);
                    }
                    return [];

                case IfStatementSyntax ifStatement:
                    string resolvedCondition = ResolveCondition(ifStatement.Condition, state);
                    FlowState whenTrue = state.Clone();
                    whenTrue.Conditions.Add(resolvedCondition);
                    List<FlowState> continuations = ProcessStatement(ifStatement.Statement, whenTrue, method, sourceFile, rules, warnings);

                    FlowState whenFalse = state.Clone();
                    whenFalse.Conditions.Add(Negate(resolvedCondition));
                    if (ifStatement.Else is not null)
                    {
                        continuations.AddRange(ProcessStatement(ifStatement.Else.Statement, whenFalse, method, sourceFile, rules, warnings));
                    }
                    else
                    {
                        continuations.Add(whenFalse);
                    }
                    return continuations;

                default:
                    return [state];
            }
        }

        private static void ApplyAssignment(AssignmentExpressionSyntax assignment, FlowState state)
        {
            if (assignment.Left is not IdentifierNameSyntax identifier)
            {
                return;
            }

            string name = identifier.Identifier.ValueText;
            string right = ResolveExpression(assignment.Right, state);
            string existing = state.Variables.GetValueOrDefault(name) ?? name;
            state.Variables[name] = assignment.Kind() switch
            {
                SyntaxKind.SimpleAssignmentExpression => right,
                SyntaxKind.AddAssignmentExpression => $"({existing}) + ({right})",
                SyntaxKind.SubtractAssignmentExpression => $"({existing}) - ({right})",
                SyntaxKind.MultiplyAssignmentExpression => $"({existing}) * ({right})",
                SyntaxKind.DivideAssignmentExpression => $"({existing}) / ({right})",
                _ => Code(assignment),
            };
        }

        private static void AddRule(string method, ExpressionSyntax expression, FlowState state, string sourceFile, List<SpawnRule> rules, List<string> warnings)
        {
            string resolved = ResolveExpression(expression, state);
            int line = expression.SyntaxTree.GetLineSpan(expression.Span).StartLinePosition.Line + 1;
            ExpandResolvedRule(method, Code(expression), resolved, state, sourceFile, line, rules, warnings);
        }

        private static void ExpandResolvedRule(
            string method,
            string raw,
            string resolved,
            FlowState state,
            string sourceFile,
            int line,
            List<SpawnRule> rules,
            List<string> warnings)
        {
            ExpressionSyntax parsed = SyntaxFactory.ParseExpression(resolved);
            if (parsed is ConditionalExpressionSyntax conditional)
            {
                string condition = ResolveCondition(conditional.Condition, state);
                FlowState whenTrue = state.Clone();
                whenTrue.Conditions.Add(condition);
                string trueValue = ResolveExpression(conditional.WhenTrue, whenTrue);
                ExpandResolvedRule(method, Code(conditional.WhenTrue), trueValue, whenTrue, sourceFile, line, rules, warnings);

                FlowState whenFalse = state.Clone();
                whenFalse.Conditions.Add(Negate(condition));
                string falseValue = ResolveExpression(conditional.WhenFalse, whenFalse);
                ExpandResolvedRule(method, Code(conditional.WhenFalse), falseValue, whenFalse, sourceFile, line, rules, warnings);
                return;
            }

            double? numeric = resolved == "base.SpawnChance(spawnInfo)" ? 0d : NumericExpression.TryEvaluate(resolved);
            if (numeric is null)
            {
                warnings.Add($"{sourceFile}:{line} — unresolved {method} return `{resolved}`");
            }
            rules.Add(new SpawnRule(method, raw, resolved, numeric, [.. state.Conditions], sourceFile, line));
        }

        private static string ResolveExpression(ExpressionSyntax expression, FlowState state, int depth = 0)
        {
            if (depth > 12)
            {
                return Code(expression);
            }

            if (expression is IdentifierNameSyntax identifier && state.Variables.TryGetValue(identifier.Identifier.ValueText, out string? value))
            {
                ExpressionSyntax parsed = SyntaxFactory.ParseExpression(value);
                return ResolveExpression(parsed, state, depth + 1);
            }

            if (expression is AssignmentExpressionSyntax assignment && assignment.Left is IdentifierNameSyntax assignedIdentifier)
            {
                string existing = state.Variables.GetValueOrDefault(assignedIdentifier.Identifier.ValueText) ?? assignedIdentifier.Identifier.ValueText;
                string right = ResolveExpression(assignment.Right, state, depth + 1);
                return assignment.Kind() switch
                {
                    SyntaxKind.SimpleAssignmentExpression => right,
                    SyntaxKind.DivideAssignmentExpression => $"({existing}) / ({right})",
                    SyntaxKind.MultiplyAssignmentExpression => $"({existing}) * ({right})",
                    SyntaxKind.AddAssignmentExpression => $"({existing}) + ({right})",
                    SyntaxKind.SubtractAssignmentExpression => $"({existing}) - ({right})",
                    _ => Code(expression),
                };
            }

            return Code(expression);
        }

        private static string ResolveCondition(ExpressionSyntax expression, FlowState state)
        {
            SyntaxNode? rewritten = new VariableSubstitutionRewriter(state.Variables).Visit(expression);
            return rewritten is null ? Code(expression) : Code(rewritten);
        }

        private static string Negate(string expression) => $"!({expression})";

        private sealed class VariableSubstitutionRewriter(Dictionary<string, string> variables) : CSharpSyntaxRewriter
        {
            private readonly HashSet<string> resolving = new(StringComparer.Ordinal);

            public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
            {
                string name = node.Identifier.ValueText;
                if (!variables.TryGetValue(name, out string? replacement) || !resolving.Add(name))
                {
                    return base.VisitIdentifierName(node);
                }

                try
                {
                    ExpressionSyntax parsed = SyntaxFactory.ParseExpression(replacement);
                    ExpressionSyntax? visited = Visit(parsed) as ExpressionSyntax;
                    if (visited is null)
                    {
                        return base.VisitIdentifierName(node);
                    }

                    ExpressionSyntax safeReplacement = visited is BinaryExpressionSyntax or ConditionalExpressionSyntax or AssignmentExpressionSyntax
                        ? SyntaxFactory.ParenthesizedExpression(visited)
                        : visited;
                    return safeReplacement.WithTriviaFrom(node);
                }
                finally
                {
                    resolving.Remove(name);
                }
            }
        }
    }

    private static class MutationAnalyzer
    {
        public static List<PoolMutation> Analyze(MethodDeclarationSyntax method, string sourceFile)
        {
            List<PoolMutation> mutations = [];
            if (method.Body is not null)
            {
                VisitStatements(method.Body.Statements, [], sourceFile, mutations);
            }
            return mutations;
        }

        private static void VisitStatements(SyntaxList<StatementSyntax> statements, List<string> conditions, string sourceFile, List<PoolMutation> mutations)
        {
            foreach (StatementSyntax statement in statements)
            {
                VisitStatement(statement, conditions, sourceFile, mutations);
            }
        }

        private static void VisitStatement(StatementSyntax statement, List<string> conditions, string sourceFile, List<PoolMutation> mutations)
        {
            switch (statement)
            {
                case BlockSyntax block:
                    VisitStatements(block.Statements, conditions, sourceFile, mutations);
                    return;
                case IfStatementSyntax ifStatement:
                    VisitStatement(ifStatement.Statement, [.. conditions, Code(ifStatement.Condition)], sourceFile, mutations);
                    if (ifStatement.Else is not null)
                    {
                        VisitStatement(ifStatement.Else.Statement, [.. conditions, $"!({Code(ifStatement.Condition)})"], sourceFile, mutations);
                    }
                    return;
                case ForEachStatementSyntax forEach:
                    VisitStatement(forEach.Statement, [.. conditions, $"foreach ({forEach.Identifier} in {forEach.Expression})"], sourceFile, mutations);
                    return;
            }

            foreach (InvocationExpressionSyntax invocation in statement.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
            {
                if (invocation.Expression is not MemberAccessExpressionSyntax member || Code(member.Expression) != "pool")
                {
                    continue;
                }

                string operation = member.Name.Identifier.ValueText;
                if (operation is not ("Add" or "Remove" or "Clear"))
                {
                    continue;
                }

                SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;
                string targetExpression = arguments.Count > 0 ? Code(arguments[0].Expression) : "(entire pool)";
                string? weight = arguments.Count > 1 ? Code(arguments[1].Expression) : null;
                (string targetName, bool isModNpc) = ParseTarget(targetExpression);
                int line = invocation.SyntaxTree.GetLineSpan(invocation.Span).StartLinePosition.Line + 1;
                mutations.Add(new PoolMutation(operation, targetExpression, targetName, isModNpc, weight, [.. conditions], sourceFile, line));
            }
        }

        private static (string Name, bool IsModNpc) ParseTarget(string expression)
        {
            Match modType = Regex.Match(expression, @"ModContent\.NPCType<(?<type>[\w.]+)>");
            if (modType.Success)
            {
                return (modType.Groups["type"].Value.Split('.').Last(), true);
            }

            Match vanilla = Regex.Match(expression, @"NPCID\.(?<type>\w+)");
            if (vanilla.Success)
            {
                return (vanilla.Groups["type"].Value, false);
            }
            return (expression, false);
        }
    }

    private static class BlockAnalyzer
    {
        public static List<BlockRule> Analyze(MethodDeclarationSyntax method, string sourceFile)
        {
            List<BlockRule> rules = [];
            if (method.Body is not null)
            {
                VisitStatements(method.Body.Statements, [], sourceFile, rules);
            }
            return rules;
        }

        private static void VisitStatements(SyntaxList<StatementSyntax> statements, List<string> conditions, string sourceFile, List<BlockRule> rules)
        {
            foreach (StatementSyntax statement in statements)
            {
                VisitStatement(statement, conditions, sourceFile, rules);
            }
        }

        private static void VisitStatement(StatementSyntax statement, List<string> conditions, string sourceFile, List<BlockRule> rules)
        {
            switch (statement)
            {
                case BlockSyntax block:
                    VisitStatements(block.Statements, conditions, sourceFile, rules);
                    return;
                case IfStatementSyntax ifStatement:
                    VisitStatement(ifStatement.Statement, [.. conditions, Code(ifStatement.Condition)], sourceFile, rules);
                    if (ifStatement.Else is not null)
                    {
                        VisitStatement(ifStatement.Else.Statement, [.. conditions, $"!({Code(ifStatement.Condition)})"], sourceFile, rules);
                    }
                    return;
            }

            foreach (AssignmentExpressionSyntax assignment in statement.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>())
            {
                if (Code(assignment.Left) != "npc.active" || !IsFalse(Code(assignment.Right)))
                {
                    continue;
                }

                string conditionText = JoinConditions(conditions);
                if (!conditionText.Contains("AdventureMode", StringComparison.Ordinal) || !conditionText.Contains("npc.type", StringComparison.Ordinal))
                {
                    continue;
                }

                string selector = string.Join(" || ", Regex.Matches(conditionText, @"npc\.type\s*(?:==|>=|<=|>|<)\s*NPCID\.\w+")
                    .Select(match => match.Value)
                    .Distinct(StringComparer.Ordinal));
                if (string.IsNullOrEmpty(selector))
                {
                    selector = "See full condition";
                }
                int line = assignment.SyntaxTree.GetLineSpan(assignment.Span).StartLinePosition.Line + 1;
                rules.Add(new BlockRule(selector, [.. conditions], sourceFile, line));
            }
        }
    }

    private sealed class ConditionFacts
    {
        private readonly HashSet<string> positive = new(StringComparer.Ordinal);
        private readonly HashSet<string> negative = new(StringComparer.Ordinal);
        private readonly HashSet<string> definitePositive = new(StringComparer.Ordinal);
        private readonly HashSet<string> definiteNegative = new(StringComparer.Ordinal);
        private readonly HashSet<string> candidateRandomGates = new(StringComparer.Ordinal);
        private readonly HashSet<string> definiteRandomGates = new(StringComparer.Ordinal);
        private readonly List<string> rawConditions;

        private ConditionFacts(List<string> conditions)
        {
            rawConditions = conditions;
            foreach (string condition in conditions)
            {
                ExpressionSyntax expression = SyntaxFactory.ParseExpression(condition);
                Collect(expression, negated: false);
                FactSummary definite = SummarizeDefinite(expression, negated: false);
                definitePositive.UnionWith(definite.Positive);
                definiteNegative.UnionWith(definite.Negative);
                definiteRandomGates.UnionWith(definite.RandomGates);
            }
        }

        public static ConditionFacts From(List<string> conditions) => new(conditions);

        public bool IsDefinitelyImpossible()
        {
            if (definitePositive.Overlaps(definiteNegative))
            {
                return true;
            }
            // Super Hardmode is a later Hardmode phase in tsorcRevamp, so a path requiring
            // SuperHardMode while explicitly excluding Main.hardMode cannot be reached.
            return definitePositive.Contains("SuperHardMode") && definiteNegative.Contains("hardMode");
        }

        public IReadOnlyList<string> Phases()
        {
            bool shm = definitePositive.Contains("SuperHardMode");
            bool notShm = definiteNegative.Contains("SuperHardMode");
            bool hm = definitePositive.Contains("hardMode");
            bool notHm = definiteNegative.Contains("hardMode");

            if (shm) return ["SuperHardmode"];
            if (notHm) return ["Pre-Hardmode"];
            if (hm && notShm) return ["Hardmode"];
            if (hm) return ["Hardmode", "SuperHardmode"];
            if (notShm) return ["Pre-Hardmode", "Hardmode"];
            return PhaseOrder;
        }

        public string Biome()
        {
            string? specialZone = SpecialZone();
            if (specialZone is not null)
            {
                return specialZone;
            }

            List<string> biomes = [];
            AddIfPositive(biomes, "ZoneCorrupt", "Corruption");
            AddIfPositive(biomes, "ZoneCrimson", "Crimson");
            AddIfPositive(biomes, "ZoneHallow", "Hallow");
            AddIfPositive(biomes, "ZoneJungle", "Jungle");
            AddIfPositive(biomes, "ZoneSnow", "Snow");
            if (positive.Contains("ZoneDesert") || positive.Contains("ZoneUndergroundDesert")) biomes.Add("Desert");
            if (positive.Contains("ZoneBeach") || positive.Contains("Ocean")) biomes.Add("Ocean");
            AddIfPositive(biomes, "ZoneDungeon", "Dungeon");
            AddIfPositive(biomes, "Lihzahrd", "Lihzahrd Temple");
            AddIfPositive(biomes, "ZoneMeteor", "Meteor");
            AddIfPositive(biomes, "ZoneGlowshroom", "Glowing Mushroom");
            AddIfPositive(biomes, "ZoneGraveyard", "Graveyard");
            AddIfPositive(biomes, "ZoneUnderworldHeight", "Underworld");
            AddIfPositive(biomes, "ZoneSkyHeight", "Sky");

            biomes = biomes.Distinct(StringComparer.Ordinal).OrderBy(name => BiomeOrder.GetValueOrDefault(name, 98)).ToList();
            if (biomes.Count > 0)
            {
                return string.Join(" + ", biomes);
            }

            if (positive.Contains("ZoneForest"))
            {
                return "Neutral Overworld (Forest)";
            }

            bool excludesBiome = new[] { "ZoneCorrupt", "ZoneCrimson", "ZoneHallow", "ZoneJungle", "ZoneSnow", "ZoneDesert", "ZoneBeach" }.Any(definiteNegative.Contains);
            if (excludesBiome && IsUnderground())
            {
                return "Neutral Underground / Caverns";
            }
            if (excludesBiome && IsSurface())
            {
                return "Neutral Overworld (Forest)";
            }
            return "General / Custom";
        }

        private string? SpecialZone()
        {
            string text = string.Join(" ", rawConditions);
            if (text.Contains("StarlitHeavenWallpaper", StringComparison.Ordinal)) return "Origin of the Abyss";
            if (text.Contains("ObsidianBrickUnsafe", StringComparison.Ordinal) && text.Contains("RemixMap", StringComparison.Ordinal)) return "Remix Obsidian Zone";
            if (text.Contains("WallType == 98", StringComparison.Ordinal) || text.Contains("GreenDungeonSlabUnsafe", StringComparison.Ordinal)) return "Machine Temple";
            if (text.Contains("DirtUnsafe1", StringComparison.Ordinal) || text.Contains("DirtUnsafe2", StringComparison.Ordinal)) return "Arazium's Mountain Caverns";
            if (text.Contains("TileID.BoneBlock", StringComparison.Ordinal)) return "Catacombs";
            if (text.Contains("HeavenforgeBrick", StringComparison.Ordinal)) return "Molten Sky Temple";
            if ((text.Contains("TileID.Coralstone", StringComparison.Ordinal) || text.Contains("TileID.ReefBlock", StringComparison.Ordinal)) && text.Contains("RemixMap", StringComparison.Ordinal)) return "Remix Reef";
            return null;
        }

        public string Depth()
        {
            List<string> depths = [];
            if (positive.Contains("ZoneSkyHeight")) depths.Add("Sky");
            if (positive.Contains("ZoneOverworldHeight") || positive.Contains("ZoneForest")) depths.Add("Surface");
            if (positive.Contains("ZoneDirtLayerHeight") || positive.Contains("ZoneNormalUnderground")) depths.Add("Underground");
            if (positive.Contains("ZoneRockLayerHeight") || positive.Contains("ZoneNormalCaverns")) depths.Add("Caverns");
            if (positive.Contains("ZoneUnderworldHeight")) depths.Add("Underworld");
            return depths.Count == 0 ? "Any / condition-defined depth" : string.Join(" or ", depths.Distinct(StringComparer.Ordinal));
        }

        public string Time()
        {
            if (definitePositive.Contains("dayTime") && !definiteNegative.Contains("dayTime")) return "Day";
            if (definiteNegative.Contains("dayTime") && !definitePositive.Contains("dayTime")) return "Night";
            return "Day and night / unspecified";
        }

        public string RandomGate()
        {
            if (definiteRandomGates.Count > 0)
            {
                return string.Join(" × ", definiteRandomGates.OrderBy(value => value, StringComparer.Ordinal));
            }
            return candidateRandomGates.Count == 0
                ? "—"
                : $"Conditional: {string.Join(" / ", candidateRandomGates.OrderBy(value => value, StringComparer.Ordinal))}";
        }

        public string Water()
        {
            if (definitePositive.Contains("Water") && !definiteNegative.Contains("Water")) return "Required";
            if (definiteNegative.Contains("Water") && !definitePositive.Contains("Water")) return "Excluded";
            if (positive.Contains("Water") || negative.Contains("Water")) return "Conditional / mixed";
            return "Unspecified";
        }

        private bool IsSurface() => positive.Contains("ZoneOverworldHeight") || positive.Contains("ZoneForest");
        private bool IsUnderground() => positive.Contains("ZoneDirtLayerHeight") || positive.Contains("ZoneRockLayerHeight") || positive.Contains("ZoneNormalUnderground") || positive.Contains("ZoneNormalCaverns");

        private void AddIfPositive(List<string> output, string fact, string label)
        {
            if (positive.Contains(fact)) output.Add(label);
        }

        private void Collect(ExpressionSyntax expression, bool negated)
        {
            switch (expression)
            {
                case ParenthesizedExpressionSyntax parenthesized:
                    Collect(parenthesized.Expression, negated);
                    return;
                case PrefixUnaryExpressionSyntax prefix when prefix.IsKind(SyntaxKind.LogicalNotExpression):
                    Collect(prefix.Operand, !negated);
                    return;
                case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.LogicalAndExpression):
                    // !(A && B) only guarantees that at least one operand is false. Treating both
                    // as negative facts is what caused prior-return guards to pollute later pools.
                    if (!negated)
                    {
                        Collect(binary.Left, negated: false);
                        Collect(binary.Right, negated: false);
                    }
                    return;
                case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.LogicalOrExpression):
                    // A || B supplies alternative positive candidates; !(A || B) guarantees both
                    // exclusions and is the common neutral-biome pattern in this mod.
                    Collect(binary.Left, negated);
                    Collect(binary.Right, negated);
                    return;
            }

            if (!negated)
            {
                foreach (Match gate in Regex.Matches(Code(expression), @"NextBool\s*\(\s*(?<n>\d+)\s*\)"))
                {
                    candidateRandomGates.Add($"1/{gate.Groups["n"].Value}");
                }
            }

            foreach (string fact in ExtractFacts(expression))
            {
                (negated ? negative : positive).Add(fact);
            }
        }

        private static IEnumerable<string> ExtractFacts(ExpressionSyntax expression)
        {
            HashSet<string> facts = new(StringComparer.Ordinal);
            foreach (SimpleNameSyntax name in expression.DescendantNodesAndSelf().OfType<SimpleNameSyntax>())
            {
                string value = name.Identifier.ValueText;
                if (value.StartsWith("Zone", StringComparison.Ordinal) || value is "Ocean" or "Water" or "Lihzahrd" or "hardMode" or "dayTime" or "SuperHardMode")
                {
                    facts.Add(value);
                }
            }
            return facts;
        }

        private static FactSummary SummarizeDefinite(ExpressionSyntax expression, bool negated)
        {
            switch (expression)
            {
                case ParenthesizedExpressionSyntax parenthesized:
                    return SummarizeDefinite(parenthesized.Expression, negated);
                case PrefixUnaryExpressionSyntax prefix when prefix.IsKind(SyntaxKind.LogicalNotExpression):
                    return SummarizeDefinite(prefix.Operand, !negated);
                case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.LogicalAndExpression) || binary.IsKind(SyntaxKind.LogicalOrExpression):
                    FactSummary left = SummarizeDefinite(binary.Left, negated);
                    FactSummary right = SummarizeDefinite(binary.Right, negated);
                    bool conjunction = binary.IsKind(SyntaxKind.LogicalAndExpression) != negated;
                    return conjunction ? FactSummary.Union(left, right) : FactSummary.Intersection(left, right);
            }

            HashSet<string> facts = ExtractFacts(expression).ToHashSet(StringComparer.Ordinal);
            HashSet<string> gates = [];
            if (!negated)
            {
                foreach (Match gate in Regex.Matches(Code(expression), @"NextBool\s*\(\s*(?<n>\d+)\s*\)"))
                {
                    gates.Add($"1/{gate.Groups["n"].Value}");
                }
            }
            return negated
                ? new FactSummary([], facts, [])
                : new FactSummary(facts, [], gates);
        }

        private sealed record FactSummary(HashSet<string> Positive, HashSet<string> Negative, HashSet<string> RandomGates)
        {
            public static FactSummary Union(FactSummary left, FactSummary right) => new(
                left.Positive.Union(right.Positive, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal),
                left.Negative.Union(right.Negative, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal),
                left.RandomGates.Union(right.RandomGates, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal));

            public static FactSummary Intersection(FactSummary left, FactSummary right) => new(
                left.Positive.Intersect(right.Positive, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal),
                left.Negative.Intersect(right.Negative, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal),
                left.RandomGates.Intersect(right.RandomGates, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal));
        }
    }

    private static class NumericExpression
    {
        public static double? TryEvaluate(string text)
        {
            try
            {
                return Evaluate(SyntaxFactory.ParseExpression(text));
            }
            catch
            {
                return null;
            }
        }

        private static double? Evaluate(ExpressionSyntax expression)
        {
            return expression switch
            {
                LiteralExpressionSyntax literal when literal.Token.Value is IConvertible convertible => convertible.ToDouble(CultureInfo.InvariantCulture),
                ParenthesizedExpressionSyntax parenthesized => Evaluate(parenthesized.Expression),
                PrefixUnaryExpressionSyntax unary when unary.IsKind(SyntaxKind.UnaryMinusExpression) => -Evaluate(unary.Operand),
                PrefixUnaryExpressionSyntax unary when unary.IsKind(SyntaxKind.UnaryPlusExpression) => Evaluate(unary.Operand),
                CastExpressionSyntax cast => Evaluate(cast.Expression),
                BinaryExpressionSyntax binary => EvaluateBinary(binary),
                _ => null,
            };
        }

        private static double? EvaluateBinary(BinaryExpressionSyntax binary)
        {
            double? left = Evaluate(binary.Left);
            double? right = Evaluate(binary.Right);
            if (left is null || right is null)
            {
                return null;
            }
            return binary.Kind() switch
            {
                SyntaxKind.AddExpression => left + right,
                SyntaxKind.SubtractExpression => left - right,
                SyntaxKind.MultiplyExpression => left * right,
                SyntaxKind.DivideExpression when right != 0 => left / right,
                _ => null,
            };
        }
    }

    private static class RegressionTests
    {
        public static void Run()
        {
            TestChanceAssignmentAndNeutralTaxonomy();
            TestConditionalReturnExpansion();
            TestRandomGateAndUnderworldTaxonomy();
            TestHumanReadableConditions();
            TestImpossiblePathDetection();
            TestGateAdjustedWeights();
            TestPoolReplacementParsing();
            TestAdventureBlockParsing();
        }

        private static void TestChanceAssignmentAndNeutralTaxonomy()
        {
            const string source = """
                class Fixture {
                    public float SpawnChance(NPCSpawnInfo spawnInfo) {
                        Player p = spawnInfo.Player;
                        float chance = 0f;
                        bool neutralSurface = p.ZoneOverworldHeight && !(p.ZoneCorrupt || p.ZoneCrimson || p.ZoneJungle);
                        if (!Main.hardMode && neutralSurface)
                            chance = 0.25f;
                        return chance;
                    }
                }
                """;
            MethodDeclarationSyntax method = ParseMethod(source, "SpawnChance");
            List<SpawnRule> rules = SpawnFlowAnalyzer.Analyze(method, "fixture.cs", []);
            SpawnRule rule = rules.Single(candidate => candidate.NumericWeight == 0.25d);
            ConditionFacts facts = ConditionFacts.From(rule.Conditions);
            Assert(facts.Phases().SequenceEqual(["Pre-Hardmode"]), "fixture progression classification");
            Assert(facts.Biome() == "Neutral Overworld (Forest)", "fixture neutral biome classification");
            Assert(facts.Depth() == "Surface", "fixture surface classification");
        }

        private static void TestRandomGateAndUnderworldTaxonomy()
        {
            const string source = """
                class Fixture {
                    public float SpawnChance(NPCSpawnInfo spawnInfo) {
                        if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneUnderworldHeight && Main.rand.NextBool(40))
                            return 1f;
                        return 0f;
                    }
                }
                """;
            SpawnRule rule = SpawnFlowAnalyzer.Analyze(ParseMethod(source, "SpawnChance"), "fixture.cs", [])
                .Single(candidate => candidate.NumericWeight == 1d);
            ConditionFacts facts = ConditionFacts.From(rule.Conditions);
            Assert(rule.ResolvedWeight == "1f", "fixture literal return weight");
            Assert(facts.RandomGate() == "1/40", "fixture random gate separation");
            Assert(facts.Biome() == "Underworld", "fixture underworld classification");
            Assert(facts.Phases().SequenceEqual(["SuperHardmode"]), "fixture SuperHardmode classification");
        }

        private static void TestConditionalReturnExpansion()
        {
            const string source = """
                class Fixture {
                    public float SpawnChance(NPCSpawnInfo spawnInfo) {
                        bool insideSpawnArea = spawnInfo.Player.ZoneGlowshroom;
                        return insideSpawnArea ? 0.5f : 0f;
                    }
                }
                """;
            List<SpawnRule> rules = SpawnFlowAnalyzer.Analyze(ParseMethod(source, "SpawnChance"), "fixture.cs", []);
            Assert(rules.Count == 2, "fixture conditional return expansion");
            SpawnRule active = rules.Single(rule => rule.NumericWeight == 0.5d);
            Assert(ConditionFacts.From(active.Conditions).Biome() == "Glowing Mushroom", "fixture conditional return taxonomy");
        }

        private static void TestHumanReadableConditions()
        {
            string description = HumanizeCondition("!spawnInfo.Invasion && spawnInfo.Water && Main.rand.NextBool(10)");
            Assert(description.Contains("no invasion is active", StringComparison.Ordinal), "human-readable invasion exclusion");
            Assert(description.Contains("spawn tile is in water", StringComparison.Ordinal), "human-readable water requirement");
            Assert(description.Contains("1-in-10 random roll succeeds", StringComparison.Ordinal), "human-readable random gate");

            string neutral = HumanizeCondition("!(spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson)");
            Assert(neutral.Contains("None of the following are true", StringComparison.Ordinal), "human-readable exclusion group");
            Assert(neutral.Contains("player is in the Corruption", StringComparison.Ordinal), "human-readable Corruption exclusion");
            Assert(neutral.Contains("player is in the Crimson", StringComparison.Ordinal), "human-readable Crimson exclusion");
        }

        private static void TestImpossiblePathDetection()
        {
            Assert(ConditionFacts.From(["Main.hardMode", "!Main.hardMode"]).IsDefinitelyImpossible(), "contradictory Hardmode path detection");
            Assert(ConditionFacts.From(["tsorcRevampWorld.SuperHardMode", "!Main.hardMode"]).IsDefinitelyImpossible(), "Super Hardmode domain contradiction detection");
            Assert(!ConditionFacts.From(["Main.hardMode", "!tsorcRevampWorld.SuperHardMode"]).IsDefinitelyImpossible(), "ordinary Hardmode path remains valid");
        }

        private static void TestGateAdjustedWeights()
        {
            Assert(Math.Abs(Generator.EffectivePoolWeight("1", "1/5")!.Value - 0.2d) < 0.0000001d, "single random gate adjustment");
            Assert(Math.Abs(Generator.EffectivePoolWeight("1.5f", "1/5 × 1/3")!.Value - 0.1d) < 0.0000001d, "multiple random gate adjustment");
            Assert(Math.Abs(Generator.EffectivePoolWeight("0.25f", "—")!.Value - 0.25d) < 0.0000001d, "ungated weight preservation");
        }

        private static void TestPoolReplacementParsing()
        {
            const string source = """
                class Fixture {
                    public void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo) {
                        if (spawnInfo.Water && Main.hardMode) {
                            pool.Clear();
                            pool.Add(ModContent.NPCType<Enemies.HumanityPhantom>(), 10f);
                        }
                    }
                }
                """;
            List<PoolMutation> mutations = MutationAnalyzer.Analyze(ParseMethod(source, "EditSpawnPool"), "fixture.cs");
            Assert(mutations.Count == 2, "fixture pool mutation count");
            Assert(mutations.Any(mutation => mutation.Operation == "Clear"), "fixture pool clear");
            Assert(mutations.Any(mutation => mutation.Operation == "Add" && mutation.IsModNpc && mutation.TargetName == "HumanityPhantom"), "fixture mod pool add");
        }

        private static void TestAdventureBlockParsing()
        {
            const string source = """
                class Fixture {
                    public void AI(NPC npc) {
                        if (Config.AdventureMode && tsorcRevampWorld.SuperHardMode) {
                            if (npc.type == NPCID.CaveBat || npc.type == NPCID.Hellbat)
                                npc.active = false;
                        }
                    }
                }
                """;
            List<BlockRule> rules = BlockAnalyzer.Analyze(ParseMethod(source, "AI"), "fixture.cs");
            Assert(rules.Count == 1 && rules[0].NpcSelector.Contains("CaveBat", StringComparison.Ordinal), "fixture Adventure Mode block");
        }

        private static MethodDeclarationSyntax ParseMethod(string source, string name)
        {
            return CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
                .Single(method => method.Identifier.ValueText == name);
        }

        private static void Assert(bool condition, string description)
        {
            if (!condition)
            {
                throw new InvalidOperationException($"Enemy spawn generator regression test failed: {description}.");
            }
        }
    }

    private sealed class FlowState
    {
        public List<string> Conditions { get; } = [];
        public Dictionary<string, string> Variables { get; } = new(StringComparer.Ordinal);

        public FlowState Clone()
        {
            FlowState clone = new();
            clone.Conditions.AddRange(Conditions);
            foreach ((string key, string value) in Variables)
            {
                clone.Variables[key] = value;
            }
            return clone;
        }
    }

    private sealed record SourceClass(
        string Name,
        string FullName,
        string NamespaceName,
        List<string> BaseTypes,
        ClassDeclarationSyntax Declaration,
        string RelativePath,
        int Line,
        bool IsAbstract,
        bool IsInInventoryRoot);

    private sealed record NpcInventory(
        string InternalName,
        string DisplayName,
        string Category,
        string BaseClass,
        string SpawnStatus,
        string NpcSlots,
        string RuntimeSlotOverrides,
        string LifeMax,
        string Damage,
        string Defense,
        bool IsBoss,
        bool IsFriendly,
        bool IsTownNpc,
        string SourceFile,
        int SourceLine,
        List<SpawnRule> SpawnRules);

    private sealed record SpawnRule(
        string Method,
        string RawWeight,
        string ResolvedWeight,
        double? NumericWeight,
        List<string> Conditions,
        string SourceFile,
        int Line)
    {
        public bool IsDefinitelyZero => NumericWeight is not null && Math.Abs(NumericWeight.Value) < double.Epsilon;
        public string ConditionText => JoinConditions(Conditions);
        public static SpawnRule Empty(string sourceFile, int line) => new("None", "—", "—", 0, [], sourceFile, line);
    }

    private sealed record PoolMutation(
        string Operation,
        string TargetExpression,
        string TargetName,
        bool IsModNpc,
        string? Weight,
        List<string> Conditions,
        string SourceFile,
        int Line)
    {
        public string ConditionText => JoinConditions(Conditions);
    }

    private sealed record BlockRule(string NpcSelector, List<string> Conditions, string SourceFile, int Line)
    {
        public string ConditionText => JoinConditions(Conditions);
    }

    private sealed record PoolEntry(
        string Phase,
        string Biome,
        string Depth,
        string Time,
        string SourceKind,
        string InternalName,
        string DisplayName,
        string NpcSlots,
        string Weight,
        string RandomGate,
        string Water,
        string Conditions,
        string SourceFile,
        int Line);

    private sealed record GeneratedReports(
        string InventoryCsv,
        string InventoryHtml,
        string BiomePoolsHtml,
        int NpcCount,
        int SpawnRuleCount,
        int PoolMutationCount,
        int BlockRuleCount,
        int WarningCount);

    private static string HumanizeCondition(string condition)
    {
        ExpressionSyntax expression = SyntaxFactory.ParseExpression(condition);
        string description = expression.ContainsDiagnostics
            ? $"the source condition `{condition}` is satisfied"
            : HumanizeExpression(expression, negated: false);
        if (description.Length == 0)
        {
            return "The condition is satisfied.";
        }
        return char.ToUpperInvariant(description[0]) + description[1..].TrimEnd('.') + ".";
    }

    private static string HumanizeExpression(ExpressionSyntax expression, bool negated)
    {
        switch (expression)
        {
            case ParenthesizedExpressionSyntax parenthesized:
                return HumanizeExpression(parenthesized.Expression, negated);
            case PrefixUnaryExpressionSyntax prefix when prefix.IsKind(SyntaxKind.LogicalNotExpression):
                return HumanizeExpression(prefix.Operand, !negated);
            case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.LogicalAndExpression) || binary.IsKind(SyntaxKind.LogicalOrExpression):
                return HumanizeLogical(binary, negated);
            case BinaryExpressionSyntax binary when IsComparison(binary.Kind()):
                return HumanizeComparison(binary, negated);
            case LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.TrueLiteralExpression):
                return negated ? "this condition is false" : "this condition is true";
            case LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.FalseLiteralExpression):
                return negated ? "this condition is true" : "this condition is false";
        }

        string code = Code(expression);
        Match randomGate = Regex.Match(code, @"(?:Main\.)?rand\.NextBool\s*\(\s*(?<n>\d+)\s*\)");
        if (randomGate.Success)
        {
            return negated
                ? $"a 1-in-{randomGate.Groups["n"].Value} random roll does not succeed"
                : $"a 1-in-{randomGate.Groups["n"].Value} random roll succeeds";
        }

        string positive = HumanizeBooleanAtom(code);
        return negated ? NegateHumanPhrase(code, positive) : positive;
    }

    private static string HumanizeLogical(BinaryExpressionSyntax expression, bool negated)
    {
        bool conjunction = expression.IsKind(SyntaxKind.LogicalAndExpression);
        List<ExpressionSyntax> terms = [];
        CollectLogicalTerms(expression, expression.Kind(), terms);
        string joined = string.Join("; ", terms.Select(term =>
        {
            string description = HumanizeExpression(term, negated: false);
            return IsLogicalExpression(term) ? $"[{description}]" : description;
        }));
        return (conjunction, negated) switch
        {
            (true, false) => $"all of the following are true: {joined}",
            (true, true) => $"not all of the following are true: {joined}",
            (false, false) => $"at least one of the following is true: {joined}",
            (false, true) => $"none of the following are true: {joined}",
        };
    }

    private static void CollectLogicalTerms(ExpressionSyntax expression, SyntaxKind operatorKind, List<ExpressionSyntax> terms)
    {
        if (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            CollectLogicalTerms(parenthesized.Expression, operatorKind, terms);
            return;
        }
        if (expression is BinaryExpressionSyntax binary && binary.IsKind(operatorKind))
        {
            CollectLogicalTerms(binary.Left, operatorKind, terms);
            CollectLogicalTerms(binary.Right, operatorKind, terms);
            return;
        }
        terms.Add(expression);
    }

    private static bool IsLogicalExpression(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized.Expression;
        }
        if (expression is PrefixUnaryExpressionSyntax prefix && prefix.IsKind(SyntaxKind.LogicalNotExpression))
        {
            return IsLogicalExpression(prefix.Operand);
        }
        return expression is BinaryExpressionSyntax binary &&
               (binary.IsKind(SyntaxKind.LogicalAndExpression) || binary.IsKind(SyntaxKind.LogicalOrExpression));
    }

    private static bool IsComparison(SyntaxKind kind) => kind is
        SyntaxKind.EqualsExpression or
        SyntaxKind.NotEqualsExpression or
        SyntaxKind.LessThanExpression or
        SyntaxKind.LessThanOrEqualExpression or
        SyntaxKind.GreaterThanExpression or
        SyntaxKind.GreaterThanOrEqualExpression;

    private static string HumanizeComparison(BinaryExpressionSyntax binary, bool negated)
    {
        SyntaxKind kind = negated ? InvertComparison(binary.Kind()) : binary.Kind();
        string left = HumanizeValue(binary.Left);
        string right = HumanizeValue(binary.Right);
        string comparison = kind switch
        {
            SyntaxKind.EqualsExpression => "is",
            SyntaxKind.NotEqualsExpression => "is not",
            SyntaxKind.LessThanExpression => "is less than",
            SyntaxKind.LessThanOrEqualExpression => "is at most",
            SyntaxKind.GreaterThanExpression => "is greater than",
            SyntaxKind.GreaterThanOrEqualExpression => "is at least",
            _ => "satisfies",
        };
        return $"{left} {comparison} {right}";
    }

    private static SyntaxKind InvertComparison(SyntaxKind kind) => kind switch
    {
        SyntaxKind.EqualsExpression => SyntaxKind.NotEqualsExpression,
        SyntaxKind.NotEqualsExpression => SyntaxKind.EqualsExpression,
        SyntaxKind.LessThanExpression => SyntaxKind.GreaterThanOrEqualExpression,
        SyntaxKind.LessThanOrEqualExpression => SyntaxKind.GreaterThanExpression,
        SyntaxKind.GreaterThanExpression => SyntaxKind.LessThanOrEqualExpression,
        SyntaxKind.GreaterThanOrEqualExpression => SyntaxKind.LessThanExpression,
        _ => kind,
    };

    private static string HumanizeBooleanAtom(string code)
    {
        string member = code.Split('.').Last();
        Dictionary<string, string> exact = new(StringComparer.Ordinal)
        {
            ["Main.dayTime"] = "it is daytime",
            ["Main.hardMode"] = "Hardmode is active",
            ["Main.bloodMoon"] = "a Blood Moon is active",
            ["Main.eclipse"] = "a Solar Eclipse is active",
            ["Main.pumpkinMoon"] = "the Pumpkin Moon event is active",
            ["Main.snowMoon"] = "the Frost Moon event is active",
            ["Main.raining"] = "it is raining",
            ["NPC.downedBoss1"] = "the Eye of Cthulhu has been defeated",
            ["NPC.downedBoss2"] = "the Eater of Worlds or Brain of Cthulhu has been defeated",
            ["NPC.downedBoss3"] = "Skeletron has been defeated",
            ["NPC.downedPlantBoss"] = "Plantera has been defeated",
            ["NPC.downedGolemBoss"] = "Golem has been defeated",
            ["NPC.downedMoonlord"] = "Moon Lord has been defeated",
            ["tsorcRevampWorld.SuperHardMode"] = "Super Hardmode is active",
            ["spawnInfo.Invasion"] = "an invasion is active",
            ["spawnInfo.Water"] = "the spawn tile is in water",
            ["spawnInfo.PlayerSafe"] = "the player is in a safe area",
            ["spawnInfo.Sky"] = "the spawn point is in the sky",
            ["spawnInfo.SpiderCave"] = "the spawn point is in a Spider Cave",
            ["spawnInfo.DesertCave"] = "the spawn point is in the Underground Desert",
        };
        if (exact.TryGetValue(code, out string? phrase))
        {
            return phrase;
        }

        if (code.StartsWith("BasiliskSpawnRules.MeetsSharedRequirements", StringComparison.Ordinal))
        {
            return "the spawn tile is dry, no town NPCs are nearby, and fewer than two basilisks of this type are active";
        }

        Dictionary<string, string> zones = new(StringComparer.Ordinal)
        {
            ["ZoneCorrupt"] = "the player is in the Corruption",
            ["ZoneCrimson"] = "the player is in the Crimson",
            ["ZoneHallow"] = "the player is in the Hallow",
            ["ZoneJungle"] = "the player is in the Jungle",
            ["ZoneSnow"] = "the player is in the Snow biome",
            ["ZoneDesert"] = "the player is in the Desert",
            ["ZoneUndergroundDesert"] = "the player is in the Underground Desert",
            ["ZoneBeach"] = "the player is at the Ocean",
            ["ZoneDungeon"] = "the player is in the Dungeon",
            ["ZoneMeteor"] = "the player is in a Meteor biome",
            ["ZoneGlowshroom"] = "the player is in a Glowing Mushroom biome",
            ["ZoneGraveyard"] = "the player is in a Graveyard",
            ["ZoneForest"] = "the player is in the Forest",
            ["ZoneSkyHeight"] = "the player is at sky height",
            ["ZoneOverworldHeight"] = "the player is at surface height",
            ["ZoneDirtLayerHeight"] = "the player is in the underground dirt layer",
            ["ZoneRockLayerHeight"] = "the player is in the cavern layer",
            ["ZoneUnderworldHeight"] = "the player is in the Underworld",
            ["ZoneNormalUnderground"] = "the player is in the normal Underground",
            ["ZoneNormalCaverns"] = "the player is in the normal Caverns",
            ["Lihzahrd"] = "the player is in the Lihzahrd Temple",
        };
        if (zones.TryGetValue(member, out phrase))
        {
            return phrase;
        }

        Match anyNpcs = Regex.Match(code, @"NPC\.AnyNPCs\s*\(\s*ModContent\.NPCType<(?<npc>[\w.]+)>\(\)\s*\)");
        if (anyNpcs.Success)
        {
            return $"at least one {FormatDisplayName(anyNpcs.Groups["npc"].Value.Split('.').Last())} NPC exists";
        }

        if (code.StartsWith("tsorcRevampWorld.", StringComparison.Ordinal) || code.StartsWith("NPC.downed", StringComparison.Ordinal))
        {
            return $"the {FormatDisplayName(member)} world flag is set";
        }
        return $"`{code}` is true";
    }

    private static string NegateHumanPhrase(string code, string positive)
    {
        Dictionary<string, string> exact = new(StringComparer.Ordinal)
        {
            ["Main.dayTime"] = "it is nighttime",
            ["Main.hardMode"] = "the world is pre-Hardmode",
            ["Main.bloodMoon"] = "no Blood Moon is active",
            ["Main.eclipse"] = "no Solar Eclipse is active",
            ["Main.raining"] = "it is not raining",
            ["tsorcRevampWorld.SuperHardMode"] = "Super Hardmode is not active",
            ["spawnInfo.Invasion"] = "no invasion is active",
            ["spawnInfo.Water"] = "the spawn tile is not in water",
            ["spawnInfo.PlayerSafe"] = "the player is not in a safe area",
        };
        if (exact.TryGetValue(code, out string? phrase))
        {
            return phrase;
        }
        if (positive.StartsWith("the player is ", StringComparison.Ordinal))
        {
            return positive.Replace("the player is ", "the player is not ", StringComparison.Ordinal);
        }
        if (positive.StartsWith("at least one ", StringComparison.Ordinal) && positive.EndsWith(" exists", StringComparison.Ordinal))
        {
            return "no " + positive["at least one ".Length..];
        }
        if (positive.EndsWith(" world flag is set", StringComparison.Ordinal))
        {
            return positive[..^" is set".Length] + " is not set";
        }
        if (positive.EndsWith(" has been defeated", StringComparison.Ordinal))
        {
            return positive[..^" has been defeated".Length] + " has not been defeated";
        }
        if (positive.EndsWith(" is true", StringComparison.Ordinal))
        {
            return positive[..^" is true".Length] + " is false";
        }
        return $"it is not true that {positive}";
    }

    private static string HumanizeValue(ExpressionSyntax expression)
    {
        string code = Code(expression);
        Dictionary<string, string> exact = new(StringComparer.Ordinal)
        {
            ["spawnInfo.SpawnTileType"] = "the spawn tile type",
            ["spawnInfo.SpawnTileX"] = "the spawn tile X position",
            ["spawnInfo.SpawnTileY"] = "the spawn tile Y position",
            ["spawnInfo.Player.townNPCs"] = "the number of nearby town NPCs",
            ["WallID.None"] = "no wall",
            ["true"] = "true",
            ["false"] = "false",
        };
        if (exact.TryGetValue(code, out string? phrase))
        {
            return phrase;
        }

        Match npcCount = Regex.Match(code, @"NPC\.CountNPCS\s*\(\s*ModContent\.NPCType<(?<npc>[\w.]+)>\(\)\s*\)");
        if (npcCount.Success)
        {
            return $"the number of active {FormatDisplayName(npcCount.Groups["npc"].Value.Split('.').Last())} NPCs";
        }
        Match npcType = Regex.Match(code, @"ModContent\.NPCType<(?<npc>[\w.]+)>\(\)");
        if (npcType.Success)
        {
            return $"the {FormatDisplayName(npcType.Groups["npc"].Value.Split('.').Last())} NPC type";
        }
        if (expression is LiteralExpressionSyntax || expression is PrefixUnaryExpressionSyntax { Operand: LiteralExpressionSyntax })
        {
            return Regex.Replace(code, @"(?<=\d)[fFdDmM]$", string.Empty);
        }
        if (code.StartsWith("TileID.", StringComparison.Ordinal))
        {
            return $"{FormatDisplayName(code["TileID.".Length..])} tiles";
        }
        if (code.StartsWith("WallID.", StringComparison.Ordinal))
        {
            return $"{FormatDisplayName(code["WallID.".Length..])} walls";
        }
        return $"`{code}`";
    }

    private static string JoinConditions(List<string> conditions) => conditions.Count == 0 ? "Always" : string.Join(" && ", conditions.Select(condition => $"({condition})"));

    private static string Code(SyntaxNode node)
    {
        SyntaxNode withoutTrivia = node.ReplaceTrivia(
            node.DescendantTrivia(descendIntoTrivia: true),
            static (_, _) => default);
        return withoutTrivia.NormalizeWhitespace().ToFullString();
    }

    private static bool IsTrue(string? value) => string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
    private static bool IsFalse(string? value) => string.Equals(value?.Trim(), "false", StringComparison.OrdinalIgnoreCase);
    private static string YesNo(bool value) => value ? "Yes" : "No";

    private static string FormatDisplayName(string name)
    {
        string value = Regex.Replace(name, "(?<=[a-z0-9])(?=[A-Z])", " ");
        return Regex.Replace(value, "(?<=[A-Z])(?=[A-Z][a-z])", " ");
    }

    private static string EscapeMarkdown(string value) => value.Replace("|", "\\|", StringComparison.Ordinal).Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);

    private static string EscapeHtml(string value) => WebUtility.HtmlEncode(value.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal));

    private static string HtmlSourceLink(string relativePath, int line)
        => HtmlSourceLink(relativePath, line, Path.GetFileName(relativePath));

    private static string HtmlSourceLink(string relativePath, int line, string label)
    {
        string normalized = relativePath.Replace('\\', '/');
        return $"<a href=\"../../{EscapeHtml(normalized)}#L{line}\">{EscapeHtml(label)}</a>";
    }

    private static string SourceLink(string relativePath, int line)
    {
        string normalized = relativePath.Replace('\\', '/');
        string fileName = Path.GetFileName(normalized);
        return $"[{fileName}](../../{normalized}#L{line})";
    }

    private static void WriteCsvRow(StringBuilder output, params object?[] values)
    {
        output.AppendLine(string.Join(",", values.Select(value => Csv(value?.ToString() ?? string.Empty))));
    }

    private static string Csv(string value)
    {
        bool quote = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        string escaped = value.Replace("\"", "\"\"", StringComparison.Ordinal);
        return quote ? $"\"{escaped}\"" : escaped;
    }
}
