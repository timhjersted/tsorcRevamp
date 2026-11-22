using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TsorcRevamp.DevData.EnemySpawns;

// Reads the spawn registry (Systems/EnemySpawns.cs) with Roslyn and writes DevData/EnemySpawns/enemy_spawns.html (the
// report.html template with the data embedded as JSON) plus a one-row-per-rule CSV. Nothing is evaluated at runtime: the
// registry's Spawn lines are already one condition + one weight per stage, so the report shows them as written.
internal static class Program
{
    private const string DataPlaceholder = "/*__SPAWN_DATA__*/null";

    // Biome buckets in display order. Rules whose condition names none of these go to "Any biome".
    private static readonly string[] BiomeOrder =
    [
        "Forest", "Underground", "Corruption", "Crimson", "Hallow", "Jungle", "Snow", "Desert", "Ocean", "Dungeon", "Meteor",
        "Glowing Mushroom", "Graveyard", "Sky", "Underworld", "Jungle Temple",
        "Catacombs", "Forgotten City", "Machine Temple", "Wyvern Fortress", "Sunken Sky Temple", "Any biome",
    ];

    // Named tests that put a rule in a biome bucket when they appear un-negated.
    private static readonly Dictionary<string, string> AtomBiomes = new(StringComparer.Ordinal)
    {
        ["Zone.Forest"] = "Forest",
        ["Zone.NormalUnderground"] = "Underground",
        ["Zone.NormalCaverns"] = "Underground",
        ["Zone.Corruption"] = "Corruption",
        ["Zone.Crimson"] = "Crimson",
        ["Zone.Hallow"] = "Hallow",
        ["Zone.Jungle"] = "Jungle",
        ["Zone.Snow"] = "Snow",
        ["Zone.Desert"] = "Desert",
        ["Zone.UndergroundDesert"] = "Desert",
        ["Zone.Beach"] = "Ocean",
        ["Region.Ocean"] = "Ocean",
        ["Region.WestCoast"] = "Ocean",
        ["Region.WesternSea"] = "Ocean",
        ["Region.FrozenOcean"] = "Ocean",
        ["Region.TropicalOcean"] = "Ocean",
        ["Zone.Dungeon"] = "Dungeon",
        ["Zone.Meteor"] = "Meteor",
        ["Zone.Glowshroom"] = "Glowing Mushroom",
        ["Region.MushroomCaves"] = "Glowing Mushroom",
        ["Zone.Graveyard"] = "Graveyard",
        ["Depth.Sky"] = "Sky",
        ["Depth.Underworld"] = "Underworld",
        ["Zone.Lihzahrd"] = "Jungle Temple",
    };

    // Composites that name a hand-built map location: bucketed as that place instead of being expanded into their parts.
    private static readonly Dictionary<string, string> PlaceBiomes = new(StringComparer.Ordinal)
    {
        ["CatacombWalls"] = "Catacombs",
        ["GreenDungeonWalls"] = "Forgotten City",
        ["MachineTemple"] = "Machine Temple",
        ["WyvernFortress"] = "Wyvern Fortress",
        ["AdventureWyvernFortress"] = "Wyvern Fortress",
        ["SunkenSkyTemple"] = "Sunken Sky Temple",
    };

    // Short labels for named tests whose class + name would read badly as a chip.
    private static readonly Dictionary<string, string> AtomLabels = new(StringComparer.Ordinal)
    {
        ["Water.Wet"] = "In water",
        ["Water.Dry"] = "Dry land",
        ["Town.None"] = "No town NPCs",
        ["Town.AtMostOne"] = "≤ 1 town NPC",
        ["Town.UnderOne"] = "< 1 town NPC",
        ["Invasion.None"] = "No invasion",
        ["Invasion.NoMoonEvent"] = "No moon event",
        ["Mode.Sandbox"] = "Not Adventure Mode",
        ["Mode.Expert"] = "Expert",
        ["Mode.Remix"] = "Remix Map",
        ["Mode.AdventureMap"] = "Adventure map",
        ["Progress.Hardmode"] = "Hardmode",
        ["Progress.SuperHardmode"] = "Super Hardmode",
        ["Downed.Eye"] = "Eye downed",
        ["Downed.EaterOrBrain"] = "Eater/Brain downed",
        ["Downed.Skeletron"] = "Skeletron downed",
        ["Zone.Lihzahrd"] = "Jungle Temple",
        ["Zone.Glowshroom"] = "Glowing Mushroom",
        ["Zone.NormalUnderground"] = "Plain underground",
        ["Zone.NormalCaverns"] = "Plain caverns",
        ["Time.BloodMoon"] = "Blood Moon",
        ["Weather.RainZone"] = "In rain zone",
    };

    public static int Main(string[] args)
    {
        try
        {
            bool checkOnly = args.Contains("--check", StringComparer.OrdinalIgnoreCase);
            string repositoryRoot = FindRepositoryRoot();
            Report report = new Report(repositoryRoot);
            report.Build();

            string toolDirectory = Path.Combine(repositoryRoot, "Scripts", "GenerateDevData", "EnemySpawns");
            string template = File.ReadAllText(Path.Combine(toolDirectory, "report.html"));

            if (!template.Contains(DataPlaceholder, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"report.html must contain {DataPlaceholder} where the data goes.");
            }

            JsonSerializerOptions jsonOptions = new JsonSerializerOptions();
            jsonOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            // UnsafeRelaxed keeps "≤" and quotes readable; "</" is the only sequence that could close the script tag early.
            string json = report.Data.ToJsonString(jsonOptions).Replace("</", "<\\/", StringComparison.Ordinal);
            string html = template.Replace(DataPlaceholder, json, StringComparison.Ordinal);

            string outputDirectory = Path.Combine(repositoryRoot, "DevData", "EnemySpawns");
            Dictionary<string, string> outputs = new(StringComparer.Ordinal);
            outputs[Path.Combine(outputDirectory, "enemy_spawns.html")] = html;
            outputs[Path.Combine(outputDirectory, "enemy_spawns.csv")] = report.Csv;

            if (checkOnly)
            {
                List<string> stale = [];

                foreach ((string path, string content) in outputs)
                {
                    bool current = File.Exists(path) && NormalizeNewlines(File.ReadAllText(path)) == NormalizeNewlines(content);

                    if (!current)
                    {
                        stale.Add(Path.GetRelativePath(repositoryRoot, path));
                    }
                }

                if (stale.Count > 0)
                {
                    Console.Error.WriteLine("Enemy spawn report is missing or stale: " + string.Join(", ", stale));
                    return 1;
                }

                Console.WriteLine($"Enemy spawn report is current ({report.Summary}).");
                return 0;
            }

            Directory.CreateDirectory(outputDirectory);

            foreach ((string path, string content) in outputs)
            {
                File.WriteAllText(path, NormalizeNewlines(content), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                Console.WriteLine($"Wrote {Path.GetRelativePath(repositoryRoot, path)}");
            }

            Console.WriteLine($"Read {report.Summary}.");
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
            DirectoryInfo? directory = new DirectoryInfo(start);

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

    private static string NormalizeNewlines(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }

    // "UndergroundDesert" -> "Underground Desert". Used for test names, composites and vanilla NPC names.
    private static string SplitWords(string name)
    {
        return Regex.Replace(name, "(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])|(?<=[a-z])(?=[0-9])", " ");
    }

    private static string Code(SyntaxNode node)
    {
        return Regex.Replace(node.ToString(), @"\s+", " ").Trim();
    }

    private static int LineOf(SyntaxNode node)
    {
        return node.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1;
    }

    // The comment lines of a trivia list, split into blocks at blank lines. The last block is "attached" (directly above the
    // node) when no blank line separates it from the node.
    private static (List<List<string>> Blocks, bool LastAttached) CommentBlocks(SyntaxTriviaList trivia)
    {
        List<List<string>> blocks = [];
        List<string> current = [];
        int newlinesSinceText = 0;

        foreach (SyntaxTrivia piece in trivia)
        {
            if (piece.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                if (newlinesSinceText >= 2 && current.Count > 0)
                {
                    blocks.Add(current);
                    current = [];
                }

                string text = piece.ToString().TrimStart('/').Trim();
                current.Add(text);
                newlinesSinceText = 0;
            }
            else if (piece.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                newlinesSinceText++;
            }
        }

        if (current.Count > 0)
        {
            blocks.Add(current);
        }

        // One newline ends the comment's own line; a second one means a blank line before the node.
        bool lastAttached = blocks.Count > 0 && newlinesSinceText <= 1;
        return (blocks, lastAttached);
    }

    private sealed class Report
    {
        private readonly string repositoryRoot;
        private readonly Dictionary<string, string> displayNames;
        private readonly Dictionary<string, double> npcConstants = new(StringComparer.Ordinal);
        private readonly Dictionary<string, NpcClass> npcClasses = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ExpressionSyntax> composites = new(StringComparer.Ordinal);
        private readonly Dictionary<string, JsonObject> enemies = new(StringComparer.Ordinal);
        private readonly JsonArray zones = [];
        private readonly JsonArray glossary = [];
        private readonly List<string> noSpawnNames = [];
        private readonly Dictionary<string, List<string>> looseNotes = new(StringComparer.Ordinal);
        private readonly StringBuilder csv = new StringBuilder();
        private int ruleCount;

        public JsonObject Data { get; } = new JsonObject();

        public string Csv => csv.ToString();

        public string Summary => $"{enemies.Count} enemies, {ruleCount} spawn rules, {zones.Count} exclusive zones";

        public Report(string repositoryRoot)
        {
            this.repositoryRoot = repositoryRoot;
            displayNames = LoadDisplayNames(Path.Combine(repositoryRoot, "Localization", "en-US.hjson"));
        }

        public void Build()
        {
            ScanNpcClasses();

            string registryPath = Path.Combine(repositoryRoot, "Systems", "EnemySpawns.cs");
            SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(registryPath), path: registryPath);
            ClassDeclarationSyntax registryClass = tree.GetCompilationUnitRoot()
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .First(declaration => declaration.Identifier.ValueText == "EnemySpawns");

            ReadNamedTests(registryClass);

            MethodDeclarationSyntax buildRegistry = registryClass.Members
                .OfType<MethodDeclarationSyntax>()
                .First(method => method.Identifier.ValueText == "BuildRegistry");

            csv.AppendLine("Enemy,DisplayName,Line,Condition,PreHardmode,Hardmode,SuperHardmode,Max,Stop,Boost,Biomes,Comment");
            ReadRegistryStatements(buildRegistry);

            Data["source"] = "Systems/EnemySpawns.cs";
            Data["stages"] = new JsonArray("Pre-Hardmode", "Hardmode", "Super Hardmode");
            Data["biomes"] = new JsonArray(BiomeOrder.Select(biome => (JsonNode?)JsonValue.Create(biome)).ToArray());
            Data["enemies"] = new JsonArray(enemies.Values.OrderBy(enemy => (string?)enemy["display"], StringComparer.OrdinalIgnoreCase).Select(enemy => (JsonNode?)enemy).ToArray());
            Data["zones"] = zones;
            Data["glossary"] = glossary;
            Data["lists"] = BuildLists();
            Data["vanillaEdits"] = ReadVanillaPoolEdits();
        }

        // Every NPC class under NPCs/: its file, base types and whether it still overrides SpawnChance with a real body.
        private void ScanNpcClasses()
        {
            string npcsRoot = Path.Combine(repositoryRoot, "NPCs");

            foreach (string path in Directory.EnumerateFiles(npcsRoot, "*.cs", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal))
            {
                string source = File.ReadAllText(path);
                string relativePath = Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');
                CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();

                foreach (ClassDeclarationSyntax declaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    string name = declaration.Identifier.ValueText;
                    List<string> baseTypes = [];

                    if (declaration.BaseList is not null)
                    {
                        foreach (BaseTypeSyntax baseType in declaration.BaseList.Types)
                        {
                            string simpleName = Regex.Replace(baseType.Type.ToString(), "<.*>", "").Split('.').Last();
                            baseTypes.Add(simpleName);
                        }
                    }

                    // A SpawnChance that only returns 0 changes nothing; only a real rule counts as the enemy spawning itself.
                    MethodDeclarationSyntax? spawnChance = declaration.Members
                        .OfType<MethodDeclarationSyntax>()
                        .FirstOrDefault(method => method.Identifier.ValueText == "SpawnChance" && method.Modifiers.Any(SyntaxKind.OverrideKeyword));
                    bool ownsSpawnChance = false;

                    if (spawnChance is not null)
                    {
                        string body = Regex.Replace(spawnChance.Body?.ToString() ?? spawnChance.ExpressionBody?.ToString() ?? "", @"\s+", "");
                        ownsSpawnChance = body is not ("=>0f" or "=>0" or "{return0f;}" or "{return0;}");
                    }

                    foreach (FieldDeclarationSyntax field in declaration.Members.OfType<FieldDeclarationSyntax>())
                    {
                        if (!field.Modifiers.Any(SyntaxKind.ConstKeyword))
                        {
                            continue;
                        }

                        foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
                        {
                            string valueText = variable.Initializer?.Value.ToString().TrimEnd('f', 'F') ?? "";

                            if (double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                            {
                                npcConstants[name + "." + variable.Identifier.ValueText] = value;
                            }
                        }
                    }

                    NpcClass npcClass = new NpcClass(name, relativePath, baseTypes, declaration.Modifiers.Any(SyntaxKind.AbstractKeyword), ownsSpawnChance);
                    npcClasses.TryAdd(name, npcClass);
                }
            }
        }

        // Glossary entries for every named test (Zone.Jungle, Depth.Rows(...)) and every shared composite (Evil, HollowQuiet).
        private void ReadNamedTests(ClassDeclarationSyntax registryClass)
        {
            foreach (MemberDeclarationSyntax member in registryClass.Members)
            {
                if (member is ClassDeclarationSyntax testClass && testClass.Modifiers.Any(SyntaxKind.StaticKeyword))
                {
                    string className = testClass.Identifier.ValueText;

                    foreach (MemberDeclarationSyntax testMember in testClass.Members)
                    {
                        if (testMember is FieldDeclarationSyntax testField && testField.Declaration.Type.ToString() == "SpawnCondition")
                        {
                            VariableDeclaratorSyntax variable = testField.Declaration.Variables[0];
                            string key = className + "." + variable.Identifier.ValueText;
                            AddGlossary(key, className, "test", testField, variable.Initializer?.Value);
                        }
                        else if (testMember is MethodDeclarationSyntax testMethod && testMethod.ReturnType.ToString() == "SpawnCondition")
                        {
                            string key = className + "." + testMethod.Identifier.ValueText;
                            AddGlossary(key, className, "function", testMethod, testMethod.Body ?? (SyntaxNode?)testMethod.ExpressionBody);
                        }
                    }
                }
                else if (member is FieldDeclarationSyntax compositeField && compositeField.Declaration.Type.ToString() == "SpawnCondition")
                {
                    VariableDeclaratorSyntax variable = compositeField.Declaration.Variables[0];
                    string key = variable.Identifier.ValueText;

                    if (variable.Initializer is null)
                    {
                        continue;
                    }

                    composites[key] = variable.Initializer.Value;
                    AddGlossary(key, "Composite", "composite", compositeField, variable.Initializer.Value);
                }
            }
        }

        private void AddGlossary(string key, string group, string kind, SyntaxNode declaration, SyntaxNode? definition)
        {
            (List<List<string>> blocks, bool lastAttached) = CommentBlocks(declaration.GetLeadingTrivia());
            string doc = "";

            if (lastAttached)
            {
                doc = string.Join(" ", blocks[^1]);
            }

            JsonObject entry = new JsonObject();
            entry["key"] = key;
            entry["group"] = group;
            entry["kind"] = kind;
            entry["label"] = AtomLabel(key, []);
            entry["doc"] = doc;
            entry["code"] = definition is null ? "" : Code(definition);
            entry["line"] = LineOf(declaration);
            glossary.Add(entry);
        }

        // Walks BuildRegistry top to bottom: Spawn / SpawnNightBoosted lines become enemy rules, SpawnOnly lines become exclusive
        // zones, a comment that is just a class name marks a no-spawn enemy, and "Name: ..." comments are notes for that enemy.
        private void ReadRegistryStatements(MethodDeclarationSyntax buildRegistry)
        {
            if (buildRegistry.Body is null)
            {
                return;
            }

            foreach (StatementSyntax statement in buildRegistry.Body.Statements)
            {
                (List<List<string>> blocks, bool lastAttached) = CommentBlocks(statement.GetLeadingTrivia());
                List<string> attachedNotes = [];

                for (int i = 0; i < blocks.Count; i++)
                {
                    bool attached = lastAttached && i == blocks.Count - 1;

                    if (attached)
                    {
                        attachedNotes = blocks[i];
                    }
                    else
                    {
                        ReadLooseComment(blocks[i]);
                    }
                }

                if (statement is not ExpressionStatementSyntax { Expression: InvocationExpressionSyntax invocation })
                {
                    continue;
                }

                string trailing = "";
                SyntaxTrivia trailingComment = statement.GetTrailingTrivia().FirstOrDefault(piece => piece.IsKind(SyntaxKind.SingleLineCommentTrivia));

                if (trailingComment != default)
                {
                    trailing = trailingComment.ToString().TrimStart('/').Trim();
                }

                string method = invocation.Expression.ToString();

                if (method == "SpawnOnly")
                {
                    AddZone(invocation, attachedNotes, trailing);
                }
                else if (method is "Spawn" or "SpawnNightBoosted")
                {
                    AddSpawnCall(invocation, method == "SpawnNightBoosted", attachedNotes, trailing);
                }
            }

            (List<List<string>> closingBlocks, _) = CommentBlocks(buildRegistry.Body.CloseBraceToken.LeadingTrivia);

            foreach (List<string> block in closingBlocks)
            {
                ReadLooseComment(block);
            }
        }

        // "// Gigas" alone = registered with no natural spawn. "// CosmicCrystalLizard: ..." = a note for that enemy.
        private void ReadLooseComment(List<string> block)
        {
            foreach (string line in block)
            {
                if (Regex.IsMatch(line, @"^\w+$") && npcClasses.ContainsKey(line))
                {
                    noSpawnNames.Add(line);
                }
            }

            Match named = Regex.Match(block[0], @"^(?<name>\w+):\s*(?<rest>.*)$");

            if (named.Success && npcClasses.ContainsKey(named.Groups["name"].Value))
            {
                string name = named.Groups["name"].Value;
                List<string> text = [named.Groups["rest"].Value, .. block.Skip(1)];

                if (!looseNotes.TryGetValue(name, out List<string>? notes))
                {
                    notes = [];
                    looseNotes[name] = notes;
                }

                notes.Add(string.Join(" ", text));
            }
        }

        private void AddSpawnCall(InvocationExpressionSyntax invocation, bool nightBoosted, List<string> notes, string trailing)
        {
            SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;
            string npcName = ((LiteralExpressionSyntax)arguments[0].Expression).Token.ValueText;
            ExpressionSyntax condition = arguments[1].Expression;
            double[] weights = new double[3];
            int max = 0;
            bool stop = false;
            string[] positionalNames = ["name", "condition", "phm", "hm", "shm", "max", "stop"];

            for (int i = 2; i < arguments.Count; i++)
            {
                string parameter = arguments[i].NameColon?.Name.Identifier.ValueText ?? positionalNames[i];
                ExpressionSyntax value = arguments[i].Expression;

                if (parameter == "phm")
                {
                    weights[0] = Evaluate(value);
                }
                else if (parameter == "hm")
                {
                    weights[1] = Evaluate(value);
                }
                else if (parameter == "shm")
                {
                    weights[2] = Evaluate(value);
                }
                else if (parameter == "max")
                {
                    max = (int)Evaluate(value);
                }
                else if (parameter == "stop")
                {
                    stop = value.ToString() == "true";
                }
            }

            JsonObject enemy = GetEnemy(npcName);
            JsonArray enemyNotes = (JsonArray)enemy["notes"]!;

            if (notes.Count > 0)
            {
                enemyNotes.Add(string.Join(" ", notes));
            }

            int line = LineOf(invocation);
            JsonObject baseNode = ConditionNode(condition);
            List<string> biomes = Biomes(condition);
            AddRule(enemy, npcName, line, baseNode, Code(condition), weights, max, stop, "", trailing, biomes);

            // SpawnNightBoosted writes the base line plus a night line at x2 and a blood moon line at x4 (see the registry).
            if (nightBoosted)
            {
                double[] nightWeights = weights.Select(weight => weight * 2).ToArray();
                double[] bloodMoonWeights = weights.Select(weight => weight * 4).ToArray();
                JsonObject nightNode = AndNode(ConditionNode(condition), AtomNode("Time.Night", []));
                JsonObject bloodMoonNode = AndNode(ConditionNode(condition), AtomNode("Time.BloodMoon", []));
                AddRule(enemy, npcName, line, nightNode, Code(condition) + " & Time.Night", nightWeights, max, false, "night", trailing, biomes);
                AddRule(enemy, npcName, line, bloodMoonNode, Code(condition) + " & Time.BloodMoon", bloodMoonWeights, max, false, "bloodMoon", trailing, biomes);
            }
        }

        private void AddRule(JsonObject enemy, string npcName, int line, JsonObject condition, string conditionText, double[] weights, int max, bool stop, string boost, string comment, List<string> biomes)
        {
            JsonObject rule = new JsonObject();
            rule["line"] = line;
            rule["cond"] = condition;
            rule["text"] = conditionText;
            rule["w"] = new JsonArray(weights.Select(weight => (JsonNode?)JsonValue.Create(Math.Round(weight, 8))).ToArray());
            rule["max"] = max;
            rule["stop"] = stop;
            rule["boost"] = boost;
            rule["comment"] = comment;
            rule["biomes"] = new JsonArray(biomes.Select(biome => (JsonNode?)JsonValue.Create(biome)).ToArray());
            ((JsonArray)enemy["rules"]!).Add(rule);
            ruleCount++;

            string[] cells =
            [
                npcName, (string)enemy["display"]!, line.ToString(CultureInfo.InvariantCulture), conditionText,
                FormatNumber(weights[0]), FormatNumber(weights[1]), FormatNumber(weights[2]),
                FormatNumber(max), stop ? "true" : "", boost, string.Join("; ", biomes), comment,
            ];
            csv.AppendLine(string.Join(",", cells.Select(CsvCell)));
        }

        private void AddZone(InvocationExpressionSyntax invocation, List<string> notes, string trailing)
        {
            SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;
            ExpressionSyntax condition = arguments[0].Expression;
            JsonArray pool = [];

            for (int i = 1; i < arguments.Count; i++)
            {
                TupleExpressionSyntax entry = (TupleExpressionSyntax)arguments[i].Expression;
                string npcName = ((LiteralExpressionSyntax)entry.Arguments[0].Expression).Token.ValueText;
                bool isModNpc = npcClasses.ContainsKey(npcName);

                JsonObject member = new JsonObject();
                member["name"] = npcName;
                member["display"] = DisplayName(npcName);
                member["mod"] = isModNpc;
                member["weight"] = Evaluate(entry.Arguments[1].Expression);
                pool.Add(member);
            }

            JsonObject zone = new JsonObject();
            zone["line"] = LineOf(invocation);
            zone["cond"] = ConditionNode(condition);
            zone["text"] = Code(condition);
            zone["notes"] = new JsonArray(notes.Count > 0 ? [(JsonNode?)JsonValue.Create(string.Join(" ", notes))] : []);
            zone["comment"] = trailing;
            zone["pool"] = pool;
            zone["biomes"] = new JsonArray(Biomes(condition).Select(biome => (JsonNode?)JsonValue.Create(biome)).ToArray());
            zones.Add(zone);
        }

        private JsonObject GetEnemy(string npcName)
        {
            if (enemies.TryGetValue(npcName, out JsonObject? enemy))
            {
                return enemy;
            }

            if (!npcClasses.TryGetValue(npcName, out NpcClass? npcClass))
            {
                throw new InvalidDataException($"The registry names '{npcName}', but no class of that name exists under NPCs/.");
            }

            enemy = new JsonObject();
            enemy["name"] = npcName;
            enemy["display"] = DisplayName(npcName);
            enemy["file"] = npcClass.RelativePath;
            enemy["notes"] = new JsonArray();
            enemy["rules"] = new JsonArray();
            enemies[npcName] = enemy;
            return enemy;
        }

        // A condition expression as a tree for the page: and / or / not nodes over atoms (named tests, test calls, composites).
        private JsonObject ConditionNode(ExpressionSyntax expression)
        {
            if (expression is ParenthesizedExpressionSyntax parenthesized)
            {
                return ConditionNode(parenthesized.Expression);
            }

            if (expression is PrefixUnaryExpressionSyntax prefix && prefix.IsKind(SyntaxKind.LogicalNotExpression))
            {
                JsonObject notNode = new JsonObject();
                notNode["t"] = "not";
                notNode["c"] = ConditionNode(prefix.Operand);
                return notNode;
            }

            if (expression is BinaryExpressionSyntax binary && (binary.IsKind(SyntaxKind.BitwiseAndExpression) || binary.IsKind(SyntaxKind.BitwiseOrExpression)))
            {
                // Flatten a & b & c into one node instead of a lopsided chain of pairs.
                List<ExpressionSyntax> operands = [];
                CollectOperands(binary, binary.Kind(), operands);

                JsonObject groupNode = new JsonObject();
                groupNode["t"] = binary.IsKind(SyntaxKind.BitwiseAndExpression) ? "and" : "or";
                groupNode["c"] = new JsonArray(operands.Select(operand => (JsonNode?)ConditionNode(operand)).ToArray());
                return groupNode;
            }

            if (expression is InvocationExpressionSyntax invocation)
            {
                string callee = Regex.Replace(invocation.Expression.ToString(), @"\s+", "");
                Match generic = Regex.Match(callee, @"^(?<key>[\w.]+)<(?<type>\w+)>$");
                List<string> callArguments = invocation.ArgumentList.Arguments.Select(argument => Code(argument.Expression)).ToList();

                if (generic.Success)
                {
                    callee = generic.Groups["key"].Value;
                    callArguments.Insert(0, generic.Groups["type"].Value);
                }

                return AtomNode(callee, callArguments);
            }

            return AtomNode(Code(expression), []);
        }

        private static void CollectOperands(ExpressionSyntax expression, SyntaxKind kind, List<ExpressionSyntax> operands)
        {
            ExpressionSyntax unwrapped = expression;

            while (unwrapped is ParenthesizedExpressionSyntax parenthesized)
            {
                unwrapped = parenthesized.Expression;
            }

            if (unwrapped is BinaryExpressionSyntax binary && binary.IsKind(kind))
            {
                CollectOperands(binary.Left, kind, operands);
                CollectOperands(binary.Right, kind, operands);
                return;
            }

            operands.Add(expression);
        }

        private JsonObject AtomNode(string key, List<string> arguments)
        {
            string category = "other";

            if (composites.ContainsKey(key))
            {
                category = PlaceBiomes.ContainsKey(key) ? "place" : "composite";
            }
            else if (key.Contains('.'))
            {
                category = key.Split('.')[0].ToLowerInvariant();
            }

            JsonObject atom = new JsonObject();
            atom["t"] = "atom";
            atom["key"] = key;
            atom["label"] = AtomLabel(key, arguments);
            atom["cat"] = category;

            if (arguments.Count > 0)
            {
                atom["args"] = string.Join(", ", arguments);
            }

            return atom;
        }

        private static JsonObject AndNode(JsonObject left, JsonObject right)
        {
            JsonObject node = new JsonObject();
            node["t"] = "and";
            node["c"] = new JsonArray(left, right);
            return node;
        }

        // The chip text for a test: hand-picked labels first, then the call forms, then the name split into words.
        private static string AtomLabel(string key, List<string> arguments)
        {
            if (AtomLabels.TryGetValue(key, out string? label))
            {
                return label;
            }

            List<string> readable = arguments.Select(argument => SplitWords(Regex.Replace(argument, @"^(WallID|TileID)\.", ""))).ToList();
            List<string> percents = arguments.Select(argument => PercentText(argument)).ToList();

            if (key == "Depth.Rows" && arguments.Count == 2)
            {
                return $"Rows {percents[0]}–{percents[1]} of map height";
            }

            if (key == "Region.OutsideSpan" && arguments.Count == 2)
            {
                return $"Outside {percents[0]}–{percents[1]} of map width";
            }

            if (key == "Wall.Is")
            {
                return "Wall: " + string.Join(" / ", readable);
            }

            if (key == "Wall.AtPlayer")
            {
                return "Wall at player: " + string.Join(" / ", readable);
            }

            if (key == "SpawnTile.Is")
            {
                return "Tile: " + string.Join(" / ", readable);
            }

            if (key == "Alive.None" && arguments.Count > 0)
            {
                return $"No {readable[0]} alive";
            }

            if (key.StartsWith("Slain.", StringComparison.Ordinal))
            {
                return SplitWords(key["Slain.".Length..]) + " slain";
            }

            string name = key.Contains('.') ? key.Split('.')[1] : key;
            return SplitWords(name);
        }

        private static string PercentText(string argument)
        {
            string trimmed = argument.TrimEnd('f', 'F');

            if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out double fraction))
            {
                return (fraction * 100).ToString("0.#", CultureInfo.InvariantCulture) + "%";
            }

            return argument;
        }

        // Biome buckets of a condition: every un-negated test that names a biome, looking inside composites unless the
        // composite is itself a named map location.
        private List<string> Biomes(ExpressionSyntax condition)
        {
            HashSet<string> found = [];
            CollectBiomes(condition, false, found, []);

            List<string> ordered = BiomeOrder.Where(found.Contains).ToList();

            if (ordered.Count == 0)
            {
                ordered.Add("Any biome");
            }

            return ordered;
        }

        private void CollectBiomes(ExpressionSyntax expression, bool negated, HashSet<string> found, HashSet<string> expanding)
        {
            switch (expression)
            {
                case ParenthesizedExpressionSyntax parenthesized:
                    CollectBiomes(parenthesized.Expression, negated, found, expanding);
                    return;

                case PrefixUnaryExpressionSyntax prefix when prefix.IsKind(SyntaxKind.LogicalNotExpression):
                    CollectBiomes(prefix.Operand, !negated, found, expanding);
                    return;

                case BinaryExpressionSyntax binary:
                    CollectBiomes(binary.Left, negated, found, expanding);
                    CollectBiomes(binary.Right, negated, found, expanding);
                    return;
            }

            if (negated)
            {
                return;
            }

            string key = Code(expression);

            if (PlaceBiomes.TryGetValue(key, out string? place))
            {
                found.Add(place);
            }
            else if (AtomBiomes.TryGetValue(key, out string? biome))
            {
                found.Add(biome);
            }
            else if (composites.TryGetValue(key, out ExpressionSyntax? definition) && expanding.Add(key))
            {
                CollectBiomes(definition, false, found, expanding);
                expanding.Remove(key);
            }
        }

        // Weights are literals, simple arithmetic, or a const on an NPC class (Locust.NaturalSpawnChance).
        private double Evaluate(ExpressionSyntax expression)
        {
            switch (expression)
            {
                // Parse the written text: the token's float Value would turn 1.3f into 1.29999995.
                case LiteralExpressionSyntax literal:
                    return double.Parse(literal.Token.Text.TrimEnd('f', 'F', 'd', 'D'), NumberStyles.Float, CultureInfo.InvariantCulture);

                case ParenthesizedExpressionSyntax parenthesized:
                    return Evaluate(parenthesized.Expression);

                case PrefixUnaryExpressionSyntax prefix when prefix.IsKind(SyntaxKind.UnaryMinusExpression):
                    return -Evaluate(prefix.Operand);

                case BinaryExpressionSyntax binary:
                    double left = Evaluate(binary.Left);
                    double right = Evaluate(binary.Right);

                    switch (binary.Kind())
                    {
                        case SyntaxKind.MultiplyExpression:
                            return left * right;
                        case SyntaxKind.DivideExpression:
                            return left / right;
                        case SyntaxKind.AddExpression:
                            return left + right;
                        case SyntaxKind.SubtractExpression:
                            return left - right;
                    }

                    break;

                case MemberAccessExpressionSyntax member:
                    string constantKey = member.Expression.ToString().Split('.').Last() + "." + member.Name.Identifier.ValueText;

                    if (npcConstants.TryGetValue(constantKey, out double constant))
                    {
                        return constant;
                    }

                    break;
            }

            throw new InvalidDataException($"Line {LineOf(expression)}: cannot read the number '{expression}'.");
        }

        private JsonObject BuildLists()
        {
            HashSet<string> modNpcs = npcClasses.Keys.Where(name => InheritsModNpc(name, [])).ToHashSet(StringComparer.Ordinal);

            JsonArray noSpawn = [];

            foreach (string name in noSpawnNames.Distinct().OrderBy(name => DisplayName(name), StringComparer.OrdinalIgnoreCase))
            {
                noSpawn.Add(ListEntry(name, ""));
            }

            // Enemies outside the registry that still roll their own SpawnChance (the Cosmic Crystal Lizard, Friendly NPCs).
            JsonArray ownSpawnChance = [];

            foreach (NpcClass npcClass in npcClasses.Values.Where(npc => npc.OwnsSpawnChance && !enemies.ContainsKey(npc.Name)).OrderBy(npc => npc.Name, StringComparer.Ordinal))
            {
                string note = looseNotes.TryGetValue(npcClass.Name, out List<string>? notes) ? string.Join(" ", notes) : "";
                ownSpawnChance.Add(ListEntry(npcClass.Name, note));
            }

            // Concrete enemies under NPCs/Enemies or NPCs/Invaders that the registry never mentions. Skipped: worm segments
            // (their head spawns them) and the *Original copies, which exist only for side-by-side comparison testing.
            JsonArray unlisted = [];
            List<NpcClass> candidates = npcClasses.Values
                .Where(npc => modNpcs.Contains(npc.Name) && !npc.IsAbstract)
                .Where(npc => npc.RelativePath.StartsWith("NPCs/Enemies/", StringComparison.Ordinal) || npc.RelativePath.StartsWith("NPCs/Invaders/", StringComparison.Ordinal))
                .Where(npc => !Regex.IsMatch(npc.Name, @"(Body\d*|Legs|Tail\d*|Segment|Original)$"))
                .Where(npc => !enemies.ContainsKey(npc.Name) && !noSpawnNames.Contains(npc.Name) && !npc.OwnsSpawnChance)
                .OrderBy(npc => DisplayName(npc.Name), StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (NpcClass npcClass in candidates)
            {
                unlisted.Add(ListEntry(npcClass.Name, ""));
            }

            JsonObject lists = new JsonObject();
            lists["noSpawn"] = noSpawn;
            lists["ownSpawnChance"] = ownSpawnChance;
            lists["unlisted"] = unlisted;
            return lists;
        }

        private JsonObject ListEntry(string name, string note)
        {
            JsonObject entry = new JsonObject();
            entry["name"] = name;
            entry["display"] = DisplayName(name);
            entry["file"] = npcClasses.TryGetValue(name, out NpcClass? npcClass) ? npcClass.RelativePath : "";
            entry["note"] = note;
            return entry;
        }

        private bool InheritsModNpc(string name, HashSet<string> visiting)
        {
            if (!npcClasses.TryGetValue(name, out NpcClass? npcClass) || !visiting.Add(name))
            {
                return false;
            }

            foreach (string baseType in npcClass.BaseTypes)
            {
                if (baseType == "ModNPC" || InheritsModNpc(baseType, visiting))
                {
                    return true;
                }
            }

            return false;
        }

        // The pool.Add / Remove / Clear calls in tsorcRevampGlobalNPC.EditSpawnPool, with the if-conditions around each one.
        // These are the vanilla enemy edits that live outside the registry.
        private JsonArray ReadVanillaPoolEdits()
        {
            string path = Path.Combine(repositoryRoot, "NPCs", "GlobalNPC.cs");
            CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetCompilationUnitRoot();
            MethodDeclarationSyntax? method = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(candidate => candidate.Identifier.ValueText == "EditSpawnPool");
            JsonArray edits = [];

            if (method?.Body is null)
            {
                return edits;
            }

            foreach (InvocationExpressionSyntax invocation in method.Body.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (invocation.Expression is not MemberAccessExpressionSyntax member || member.Expression.ToString() != "pool")
                {
                    continue;
                }

                string operation = member.Name.Identifier.ValueText;

                if (operation is not ("Add" or "Remove" or "Clear"))
                {
                    continue;
                }

                // The if-conditions from the method body down to this call, outermost first. An else branch shows as !(...).
                List<string> conditions = [];
                SyntaxNode child = invocation;

                foreach (SyntaxNode ancestor in invocation.Ancestors().TakeWhile(node => node != method.Body))
                {
                    if (ancestor is IfStatementSyntax ifStatement)
                    {
                        bool inElse = ifStatement.Else is not null && ifStatement.Else.Span.Contains(child.Span);
                        string conditionText = Code(ifStatement.Condition);
                        conditions.Insert(0, inElse ? $"!({conditionText})" : conditionText);
                    }
                    else if (ancestor is ForEachStatementSyntax forEach)
                    {
                        conditions.Insert(0, $"for each {forEach.Identifier} in {Code(forEach.Expression)}");
                    }

                    child = ancestor;
                }

                SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;
                string target = arguments.Count > 0 ? Code(arguments[0].Expression) : "(whole pool)";
                Match vanilla = Regex.Match(target, @"^NPCID\.(?<name>\w+)$");
                Match modded = Regex.Match(target, @"ModContent\.NPCType<(?<name>[\w.]+)>");
                string targetName = target;

                if (vanilla.Success)
                {
                    targetName = SplitWords(vanilla.Groups["name"].Value);
                }
                else if (modded.Success)
                {
                    targetName = DisplayName(modded.Groups["name"].Value.Split('.').Last());
                }

                JsonObject edit = new JsonObject();
                edit["op"] = operation;
                edit["target"] = targetName;
                edit["code"] = target;
                edit["weight"] = arguments.Count > 1 ? Code(arguments[1].Expression).TrimEnd('f') : "";
                edit["conds"] = new JsonArray(conditions.Select(condition => (JsonNode?)JsonValue.Create(condition)).ToArray());
                edit["line"] = LineOf(invocation);
                edits.Add(edit);
            }

            return edits;
        }

        private string DisplayName(string name)
        {
            return displayNames.TryGetValue(name, out string? display) ? display : SplitWords(name);
        }

        private static string FormatNumber(double value)
        {
            if (value == 0)
            {
                return "";
            }

            return value.ToString("0.########", CultureInfo.InvariantCulture);
        }

        private static string CsvCell(string value)
        {
            if (value.IndexOfAny([',', '"', '\n']) < 0)
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }

        // NPC display names from the NPCs section of en-US.hjson, in both the dotted (Name.DisplayName: x) and block forms.
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
                    names[dotted.Groups["name"].Value] = dotted.Groups["value"].Value.Trim().Trim('"');
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
                        names[activeBlock] = display.Groups["value"].Value.Trim().Trim('"');
                    }
                }
                else if (activeBlock is not null && indent <= blockIndent)
                {
                    activeBlock = null;
                }
            }

            return names;
        }
    }

    private sealed record NpcClass(string Name, string RelativePath, List<string> BaseTypes, bool IsAbstract, bool OwnsSpawnChance);
}
