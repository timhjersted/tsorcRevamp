using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.OolacileSerpent;

namespace tsorcRevamp.NPCs
{
    ///<summary>
    ///Span-graph A* pathfinding for the GreatSerpent boss -- the planning layer scoped in
    ///Documentation/SmartSerpent4AI_Plan.md. Forked from the ARCHITECTURE of SmartFighter4AI (span graph +
    ///PriorityQueue + A* + plan/replan/bad-edge memory), not its code: the movement vocabulary is rewritten
    ///for a grounded snake (Slither / Climb / WallScale / SpanGap instead of Walk / Jump / Drop / Rope).
    ///<para/>
    ///Division of labour: this file only decides WHAT waypoint the head should steer toward next.
    ///SerpentAI.RunGroundMovement stays the per-tick executor -- its existing climb / wall-scale / ground-snap /
    ///anti-stuck logic moves the head, merely aimed at the current plan step's target instead of the raw player
    ///position. With no plan (player directly engageable, planning failed, or same span) the serpent behaves
    ///exactly as it did before this file existed.
    ///<para/>
    ///Nav state (Plan/PlanIndex/PlanStepTimer/ReplanCooldownTimer/BadEdges/LastPlanResult) lives directly on
    ///GreatSerpentHead, per this boss's convention (ClimbBudget, StuckTimer, ...), not a separate NavState.
    ///</summary>
    public static class SmartSerpent4AI
    {
        const int TileSize = 16;

        //-- Graph build window --
        //The span box covers head-position union player-position plus padding, hard-capped so a distant player
        //can't blow up the build. Single boss instance -> the perf bar is low (no SF4-style tiering needed),
        //but the build still only runs on the replan cadence, never every tick.
        const int EdgePadTiles = 12;
        const int MaxPlanSpanXTiles = 110;   //give up planning beyond this horizontal separation
        const int MaxPlanSpanYTiles = 70;

        //-- Replan cadence / step execution --
        const int ReplanIntervalTicks = 30;      //min gap between graph rebuilds (SF4's ReplanCooldown pattern)
        const int FailedPlanRetryTicks = 60;     //back off longer after a no-path result
        const int StepTimeoutTicks = 300;        //a step that hasn't completed in 5s is marked bad and replanned
        const int BadEdgeTTLTicks = 600;         //how long a failed step target repels the planner (10s)
        const int PlanStaleXTiles = 16;          //player drifted this far from the plan's end -> stale, replan
        const int PlanStaleYTiles = 10;

        //-- Direct-engagement gate: no plan needed when the player is visibly right there --
        const float DirectEngageXTiles = 30f;
        const float DirectEngageYTiles = 10f;

        //-- Edge limits (each grounded in an ability the reactive layer already has) --
        //Slither same-row/step limit matches SerpentAI.SmallStepTiles; climb limit matches MaxClimbHeightTiles.
        const int SlitherStepTiles = SerpentAI.SmallStepTiles;          //2
        const int ClimbMaxTiles = SerpentAI.MaxClimbHeightTiles;        //11
        const int DescendMaxTiles = 16;   //slither-off-a-ledge descent; SnapHeadToGround's far-search rides it down
        const int WallScaleMaxTiles = 45; //sheer-wall hug-and-rise (the serpent's "ledge access"); generous per plan
        const int SpanGapMaxTiles = 10;   //bridge a gap with the body chain; conservative fraction of body length

        //-- Arrival tolerances (tiles) --
        const float ArriveXTiles = 2.5f;
        const int ArriveYTiles = 4;       //slither/gap steps
        const int ArriveYClimbBelow = 2;  //climb/wall-scale: must actually have RISEN to the target row

        public enum StepKind { Slither, Climb, WallScale, SpanGap }

        public struct PlanStep
        {
            public StepKind Kind;
            public int TargetX, TargetY;   //tile coords of the waypoint (TargetY = the row the head occupies; floor at +1)
            public int LaunchX;            //reference column on the departing span (wall base / gap lip)
            public PlanStep(StepKind k, int tx, int ty, int lx) { Kind = k; TargetX = tx; TargetY = ty; LaunchX = lx; }
        }

        enum EdgeKind { Slither, Climb, WallScale, SpanGap }

        class Edge
        {
            public Span To;
            public EdgeKind Kind;
            public int Cost;
            public int LaunchX, LandX;
            public Edge(Span to, EdgeKind k, int cost, int launchX = 0, int landX = 0)
            { To = to; Kind = k; Cost = cost; LaunchX = launchX; LandX = landX; }
        }

        //A horizontal run of "the head could rest here" (floor below, row itself open). Species-agnostic --
        //same shape as SF4's Span.
        class Span
        {
            public int LeftX, RightX, Y;
            public List<Edge> Edges = new List<Edge>();
            public Span(int l, int r, int y) { LeftX = l; RightX = r; Y = y; }
        }

        // ================================================================================
        //  Entry point -- called every RunGroundMovement tick
        // ================================================================================

        ///<summary>
        ///Maintains the plan (replan cadence, staleness, step arrival/timeout/bad-edge memory) and returns the
        ///current waypoint the reactive layer should steer toward. Returns false (steer at the player directly,
        ///today's behaviour) when the player is directly engageable, planning failed, or the plan just finished.
        ///</summary>
        internal static bool UpdateAndSteer(NPC npc, GreatSerpentHead data, Player player, out Vector2 target, out StepKind kind)
        {
            target = player.Center;
            kind = StepKind.Slither;

            if (data.ReplanCooldownTimer > 0)
            {
                data.ReplanCooldownTimer--;
            }

            //Directly engageable -> no plan. LOS plus roughly in the fighting envelope; the kiting/attack logic
            //owns everything from here.
            float dxTiles = Math.Abs(player.Center.X - npc.Center.X) / TileSize;
            float dyTiles = Math.Abs(player.Center.Y - npc.Center.Y) / TileSize;
            bool los = Collision.CanHit(npc.position, npc.width, npc.height, player.position, player.width, player.height);
            if (los && dxTiles <= DirectEngageXTiles && dyTiles <= DirectEngageYTiles)
            {
                if (data.Plan != null)
                {
                    data.Plan = null;
                    data.LastPlanResult = "direct";
                }
                return false;
            }

            bool needPlan = data.Plan == null || data.PlanIndex >= data.Plan.Count || PlanStale(data, player);
            if (needPlan && data.ReplanCooldownTimer <= 0)
            {
                Replan(npc, data, player);
                data.ReplanCooldownTimer = data.Plan != null ? ReplanIntervalTicks : FailedPlanRetryTicks;
            }

            if (data.Plan == null || data.PlanIndex >= data.Plan.Count)
            {
                return false; //no plan -> exactly today's direct-at-player behaviour
            }

            PlanStep step = data.Plan[data.PlanIndex];

            //-- Arrival check --
            float targetPxX = step.TargetX * TileSize + TileSize / 2f;
            int feetTileY = (int)((npc.position.Y + npc.height) / TileSize);
            int yErr = feetTileY - (step.TargetY + 1); //feet row vs the span's floor row; positive = still below
            bool xClose = Math.Abs(npc.Center.X - targetPxX) <= ArriveXTiles * TileSize;
            bool yClose = (step.Kind == StepKind.Climb || step.Kind == StepKind.WallScale)
                ? (yErr <= ArriveYClimbBelow && yErr >= -ArriveYTiles)  //must have gained the elevation
                : Math.Abs(yErr) <= ArriveYTiles;
            if (xClose && yClose)
            {
                data.PlanIndex++;
                data.PlanStepTimer = StepTimeoutTicks;
                if (data.PlanIndex >= data.Plan.Count)
                {
                    data.Plan = null;
                    data.LastPlanResult = "plan-done";
                    return false;
                }
                step = data.Plan[data.PlanIndex];
            }
            else
            {
                //-- Step timeout -> bad-edge the target so the next replan routes around it --
                data.PlanStepTimer--;
                if (data.PlanStepTimer <= 0)
                {
                    data.BadEdges[(step.TargetX, step.TargetY)] = (int)Main.GameUpdateCount + BadEdgeTTLTicks;
                    data.Plan = null;
                    data.LastPlanResult = $"step-timeout {step.Kind}->{step.TargetX},{step.TargetY}";
                    return false;
                }
            }

            //Waypoint world position: the head's center when resting on that span's floor.
            target = new Vector2(step.TargetX * TileSize + TileSize / 2f,
                                 (step.TargetY + 1) * TileSize - npc.height * 0.5f);
            kind = step.Kind;
            return true;
        }

        ///<summary>The stuck detector arming its forced-wander recovery means plan execution has demonstrably
        ///failed -- drop the plan and bad-edge the step it died on so the post-wander replan tries another route.</summary>
        internal static void OnStuckRecoveryArmed(GreatSerpentHead data)
        {
            if (data.Plan != null && data.PlanIndex < data.Plan.Count)
            {
                PlanStep step = data.Plan[data.PlanIndex];
                data.BadEdges[(step.TargetX, step.TargetY)] = (int)Main.GameUpdateCount + BadEdgeTTLTicks;
            }
            data.Plan = null;
            data.LastPlanResult = "stuck-wander";
        }

        ///<summary>For SerpentAI's log line: `plan=<idx>/<count> <kind>-><tx>,<ty>` (SF4's nav-log convention).</summary>
        internal static string DescribePlan(GreatSerpentHead data)
        {
            if (data.Plan == null)
            {
                return "none";
            }
            string value = $"{data.PlanIndex}/{data.Plan.Count}";
            if (data.PlanIndex < data.Plan.Count)
            {
                PlanStep step = data.Plan[data.PlanIndex];
                value += $" {step.Kind}->{step.TargetX},{step.TargetY}";
            }
            return value;
        }

        static bool PlanStale(GreatSerpentHead data, Player player)
        {
            if (data.Plan == null || data.Plan.Count == 0)
            {
                return true;
            }
            PlanStep last = data.Plan[data.Plan.Count - 1];
            if (Math.Abs(player.Center.X - last.TargetX * TileSize) > PlanStaleXTiles * TileSize) return true;
            if (Math.Abs(player.Center.Y - last.TargetY * TileSize) > PlanStaleYTiles * TileSize) return true;
            return false;
        }

        // ================================================================================
        //  Replan
        // ================================================================================

        static void Replan(NPC npc, GreatSerpentHead data, Player player)
        {
            data.Plan = null;
            data.PlanIndex = 0;

            int headCx = (int)(npc.Center.X / TileSize);
            int headFeetY = (int)((npc.position.Y + npc.height) / TileSize);
            int playerCx = (int)(player.Center.X / TileSize);
            int playerFeetY = (int)((player.Bottom.Y - 1f) / TileSize);

            if (Math.Abs(playerCx - headCx) > MaxPlanSpanXTiles || Math.Abs(playerFeetY - headFeetY) > MaxPlanSpanYTiles)
            {
                data.LastPlanResult = "too-far";
                return;
            }

            int xMin = Math.Min(headCx, playerCx) - EdgePadTiles;
            int xMax = Math.Max(headCx, playerCx) + EdgePadTiles;
            int yMin = Math.Min(headFeetY, playerFeetY) - EdgePadTiles;
            int yMax = Math.Max(headFeetY, playerFeetY) + EdgePadTiles;

            //Prune expired bad-edge entries before they feed edge costs.
            int now = (int)Main.GameUpdateCount;
            if (data.BadEdges.Count > 0)
            {
                List<(int, int)> dead = new List<(int, int)>();
                foreach (var kv in data.BadEdges) if (kv.Value <= now) dead.Add(kv.Key);
                foreach (var k in dead) data.BadEdges.Remove(k);
            }

            List<Span> spans = BuildSpans(xMin, xMax, yMin, yMax);
            BuildEdges(spans, data.BadEdges);

            Span start = FindContainingSpan(spans, headCx, headFeetY);
            Span goal = FindContainingSpan(spans, playerCx, playerFeetY);
            if (start == null || goal == null)
            {
                data.LastPlanResult = start == null ? "no-start-span" : "no-goal-span";
                return;
            }
            if (start == goal)
            {
                data.LastPlanResult = "same-span"; //adjacent -> direct behaviour handles it
                return;
            }

            List<Span> path = AStar(start, goal, playerCx, playerFeetY);
            if (path == null)
            {
                data.LastPlanResult = $"no-path spans={spans.Count}";
                return;
            }

            data.Plan = ConvertToSteps(path, playerCx);
            data.PlanIndex = 0;
            data.PlanStepTimer = StepTimeoutTicks;
            data.LastPlanResult = $"plan steps={data.Plan.Count} spans={spans.Count} pathLen={path.Count}";
        }

        // ================================================================================
        //  Span graph
        // ================================================================================

        ///<summary>The serpent's "standable" predicate: real solid floor below (SerpentAI.IsSolidTile already
        ///excludes platforms, per the platform-snagging fix) and the row itself open. No biped headroom check --
        ///the head phases through terrain and the body needs no clearance above it (per the plan doc).</summary>
        static bool IsSerpentStandable(int x, int y)
        {
            return SerpentAI.IsSolidTile(x, y + 1) && !SerpentAI.IsSolidTile(x, y);
        }

        static List<Span> BuildSpans(int xMin, int xMax, int yMin, int yMax)
        {
            List<Span> spans = new List<Span>();
            for (int y = yMin; y <= yMax; y++)
            {
                int spanStart = -1;
                for (int x = xMin; x <= xMax; x++)
                {
                    if (IsSerpentStandable(x, y))
                    {
                        if (spanStart == -1) spanStart = x;
                    }
                    else if (spanStart != -1)
                    {
                        spans.Add(new Span(spanStart, x - 1, y));
                        spanStart = -1;
                    }
                }
                if (spanStart != -1) spans.Add(new Span(spanStart, xMax, y));
            }
            return spans;
        }

        static int BadEdgePenalty(int landX, int spanY, Dictionary<(int x, int y), int> badEdges)
        {
            foreach (var kv in badEdges)
            {
                if (Math.Abs(kv.Key.x - landX) <= 2 && Math.Abs(kv.Key.y - spanY) <= 2)
                {
                    return 9999;
                }
            }
            return 0;
        }

        ///<summary>
        ///The serpent's edge vocabulary (the real design work vs SF4 -- see the plan doc's Edge Kinds section):
        ///<para/>Slither -- same-row / small-step / short-descent between touching spans (descents are free for
        ///a terrain-hugger: SnapHeadToGround's far-search rides it down, so they stay Slither, not a Drop kind).
        ///<para/>Climb -- a wall up to ClimbMaxTiles, executed by RunGroundMovement's budgeted climb branch.
        ///<para/>WallScale -- a taller sheer wall (the serpent's "ledge access"), executed by the WallClimbSpeed
        ///hug-and-rise branch. No biped equivalent.
        ///<para/>SpanGap -- bridge a moderate gap by simply advancing across it; the body chain physically holds
        ///the snake together over the void. THE snake-specific edge: a gap a biped needs a calibrated jump for
        ///is just a level glide for this NPC, up to a length cap.
        ///</summary>
        static void BuildEdges(List<Span> spans, Dictionary<(int x, int y), int> badEdges)
        {
            foreach (var a in spans)
            {
                foreach (var b in spans)
                {
                    if (b == a) continue;

                    int rise = a.Y - b.Y; //positive = b is above a
                    bool touching = b.LeftX <= a.RightX + 1 && b.RightX >= a.LeftX - 1;

                    if (touching)
                    {
                        if (rise >= -SlitherStepTiles && rise <= SlitherStepTiles)
                        {
                            //Plain slither / step-hop, the flat-ground branch. Cheapest edge.
                            a.Edges.Add(new Edge(b, EdgeKind.Slither, 1 + Math.Abs(rise),
                                ClampToSpan(JoinColumn(a, b), a), ClampToSpan(JoinColumn(a, b), b)));
                        }
                        else if (rise < -SlitherStepTiles && -rise <= DescendMaxTiles)
                        {
                            //Descent: slither off the lip; ground-follow settles onto the lower span.
                            int lip = JoinColumn(a, b);
                            int badP = BadEdgePenalty(ClampToSpan(lip, b), b.Y, badEdges);
                            a.Edges.Add(new Edge(b, EdgeKind.Slither, 2 + (-rise) / 2 + badP,
                                ClampToSpan(lip, a), ClampToSpan(lip, b)));
                        }
                        else if (rise > SlitherStepTiles && rise <= WallScaleMaxTiles)
                        {
                            //Vertical: verify a real contiguous wall stands at the join (reusing the same sensing
                            //the executor runs against, so the graph never promises a climb the reactive layer
                            //can't see). GetObstacleHeightAhead caps its scan, so ask for rise + 2.
                            int wallX = JoinColumn(a, b);
                            int sensed = SerpentAI.GetObstacleHeightAhead(wallX, a.Y, rise + 2);
                            if (sensed >= rise - 1)
                            {
                                int landX = ClampToSpan(wallX, b); //entry column on the upper span
                                int launchX = ClampToSpan(wallX, a);
                                int badP = BadEdgePenalty(landX, b.Y, badEdges);
                                if (rise <= ClimbMaxTiles)
                                {
                                    //Cost scales with height, same idea as SF4's JumpUp cost.
                                    a.Edges.Add(new Edge(b, EdgeKind.Climb, 4 + rise + badP, launchX, landX));
                                }
                                else
                                {
                                    //Steeper cost curve: the wall-scale is slower and riskier, prefer real routes
                                    //when one exists, but it stays available as the ledge-access answer.
                                    a.Edges.Add(new Edge(b, EdgeKind.WallScale, 10 + rise + badP, launchX, landX));
                                }
                            }
                        }
                    }
                    else if (Math.Abs(rise) <= SlitherStepTiles + 1)
                    {
                        //SpanGap: not touching, roughly level. Gap width measured between facing edges.
                        int gap = b.LeftX > a.RightX ? b.LeftX - a.RightX - 1 : a.LeftX - b.RightX - 1;
                        if (gap >= 1 && gap <= SpanGapMaxTiles)
                        {
                            int launchX = b.LeftX > a.RightX ? a.RightX : a.LeftX;
                            int landX = b.LeftX > a.RightX ? b.LeftX : b.RightX;
                            int badP = BadEdgePenalty(landX, b.Y, badEdges);
                            a.Edges.Add(new Edge(b, EdgeKind.SpanGap, 2 + gap + badP, launchX, landX));
                        }
                    }
                }
            }
        }

        ///<summary>The column where span a meets span b horizontally: b's near edge if disjoint, else the
        ///overlap's start. Used as the wall/lip reference column for vertical and slither edges.</summary>
        static int JoinColumn(Span a, Span b)
        {
            if (b.LeftX > a.RightX) return b.LeftX;
            if (b.RightX < a.LeftX) return b.RightX;
            return Math.Max(a.LeftX, b.LeftX);
        }

        static int ClampToSpan(int x, Span sp) => Math.Clamp(x, sp.LeftX, sp.RightX);

        static Span FindContainingSpan(List<Span> spans, int x, int feetY)
        {
            //Strict pass: must contain x (+/-1) and be within 3 rows. (Span.Y is the occupied row; the floor is
            //at Y+1, so compare against feetY - 1.)
            int bodyY = feetY - 1;
            Span best = null;
            int bestScore = int.MaxValue;
            foreach (var sp in spans)
            {
                if (x < sp.LeftX - 1 || x > sp.RightX + 1) continue;
                int dy = Math.Abs(sp.Y - bodyY);
                if (dy > 3) continue;
                // Horizontal gap from x to this span, 0 when x already sits inside it. Vertical distance
                // is weighted 10x so a span on our own row always beats one a row off.
                int horizontalGap = 0;

                if (x < sp.LeftX)
                {
                    horizontalGap = sp.LeftX - x;
                }
                else if (x > sp.RightX)
                {
                    horizontalGap = x - sp.RightX;
                }

                int score = dy * 10 + horizontalGap;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = sp;
                }
            }
            if (best != null) return best;

            //Permissive fallback (airborne player / head mid-climb): nearest span by weighted Manhattan
            //distance. Vertical cap scales with the serpent's wall-scale reach, not a biped jump.
            int bestDist = int.MaxValue;
            foreach (var sp in spans)
            {
                // Horizontal gap from x to this span, 0 when x already sits inside it.
                int dx = 0;

                if (x < sp.LeftX)
                {
                    dx = sp.LeftX - x;
                }
                else if (x > sp.RightX)
                {
                    dx = x - sp.RightX;
                }

                int dy = Math.Abs(sp.Y - bodyY);
                if (dx > 16 || dy > WallScaleMaxTiles) continue;
                int d = dx + dy * 2;
                if (d < bestDist) { bestDist = d; best = sp; }
            }
            return best;
        }

        // ================================================================================
        //  A* search (algorithm verbatim from SF4; heuristic vertical weight retuned)
        // ================================================================================

        static List<Span> AStar(Span start, Span goal, int goalX, int goalY)
        {
            var open = new PriorityQueue<Span>();
            var cameFrom = new Dictionary<Span, Span>();
            var gScore = new Dictionary<Span, int>();
            gScore[start] = 0;
            open.Push(start, Heuristic(start, goalX, goalY));

            int iter = 0;
            while (open.Count > 0 && iter++ < 2500)
            {
                Span cur = open.Pop();
                if (cur == goal)
                {
                    var path = new List<Span>();
                    Span c = cur;
                    while (c != null) { path.Add(c); cameFrom.TryGetValue(c, out c); }
                    path.Reverse();
                    return path;
                }
                int curG = gScore[cur];
                foreach (var e in cur.Edges)
                {
                    int tentative = curG + e.Cost;
                    if (!gScore.TryGetValue(e.To, out int existing) || tentative < existing)
                    {
                        gScore[e.To] = tentative;
                        cameFrom[e.To] = cur;
                        open.Push(e.To, tentative + Heuristic(e.To, goalX, goalY));
                    }
                }
            }
            return null;
        }

        //SF4 weights vertical mismatch x5 (a biped hates changing floors). The serpent climbs/wall-scales far
        //more readily, so a lighter x3 routes more naturally (plan doc suggests 2-3; retune from playtest logs).
        static int Heuristic(Span sp, int goalX, int goalY)
        {
            int dx = sp.LeftX > goalX ? sp.LeftX - goalX
                   : goalX > sp.RightX ? goalX - sp.RightX : 0;
            int dy = Math.Abs(sp.Y - goalY);
            return dx + dy * 3;
        }

        // ================================================================================
        //  Plan conversion
        // ================================================================================

        static List<PlanStep> ConvertToSteps(List<Span> spanPath, int playerX)
        {
            var steps = new List<PlanStep>();
            for (int i = 0; i < spanPath.Count - 1; i++)
            {
                Span from = spanPath[i];
                Span to = spanPath[i + 1];
                Edge edge = null;
                foreach (var e in from.Edges)
                    if (e.To == to) { edge = e; break; }
                if (edge == null) continue;

                switch (edge.Kind)
                {
                    case EdgeKind.Slither:
                    {
                        int entry = to.LeftX > from.RightX ? to.LeftX
                                  : to.RightX < from.LeftX ? to.RightX
                                  : (to.LeftX + to.RightX) / 2;
                        steps.Add(new PlanStep(StepKind.Slither, entry, to.Y, entry));
                        break;
                    }
                    case EdgeKind.Climb:
                        steps.Add(new PlanStep(StepKind.Climb, edge.LandX, to.Y, edge.LaunchX));
                        break;
                    case EdgeKind.WallScale:
                        steps.Add(new PlanStep(StepKind.WallScale, edge.LandX, to.Y, edge.LaunchX));
                        break;
                    case EdgeKind.SpanGap:
                        steps.Add(new PlanStep(StepKind.SpanGap, edge.LandX, to.Y, edge.LaunchX));
                        break;
                }
            }
            //Final approach to the player's column on the goal span.
            if (spanPath.Count > 0)
            {
                Span last = spanPath[spanPath.Count - 1];
                int finalX = Math.Clamp(playerX, last.LeftX, last.RightX);
                steps.Add(new PlanStep(StepKind.Slither, finalX, last.Y, finalX));
            }
            return steps;
        }

        // ================================================================================
        //  Generic binary min-heap (copied verbatim from SmartFighter4AI -- nothing species-specific)
        // ================================================================================

        class PriorityQueue<T>
        {
            private readonly List<(T item, int prio)> heap = new List<(T, int)>();
            public int Count => heap.Count;
            public void Push(T item, int prio)
            {
                heap.Add((item, prio));
                int i = heap.Count - 1;
                while (i > 0)
                {
                    int p = (i - 1) / 2;
                    if (heap[p].prio <= heap[i].prio) break;
                    (heap[p], heap[i]) = (heap[i], heap[p]);
                    i = p;
                }
            }
            public T Pop()
            {
                var top = heap[0].item;
                heap[0] = heap[heap.Count - 1];
                heap.RemoveAt(heap.Count - 1);
                int i = 0;
                while (true)
                {
                    int l = i * 2 + 1, r = l + 1, m = i;
                    if (l < heap.Count && heap[l].prio < heap[m].prio) m = l;
                    if (r < heap.Count && heap[r].prio < heap[m].prio) m = r;
                    if (m == i) break;
                    (heap[m], heap[i]) = (heap[i], heap[m]);
                    i = m;
                }
                return top;
            }
        }
    }
}
