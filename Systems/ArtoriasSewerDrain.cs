using System;
using System.IO;
using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace tsorcRevamp.Systems
{
    /// <summary>
    /// Ruins of Artorias sewer drain (expanded adventure map only). The map's lever at (5940, 2103) actuates the sewer gates, but
    /// vanilla liquid flow cannot empty a 1270-tile-wide pool through eight 7-wide shafts: it stalls, and any water that drops out
    /// of vanilla's active liquid list hangs where it is. Once the gates open, the server lowers the water line row by row down to
    /// the drain mouths (row 2182) and every client applies the same edit from a small progress packet.
    /// </summary>
    public class ArtoriasSewerDrain : ModSystem
    {
        // All coordinates are native tiles of the 2400-tall expanded world (this area lies in the fold gap, so it has no legacy
        // equivalent and is never routed through ExpandedWorldTransform). Measured from the map file.
        private const int PoolLeft = 4870;
        private const int PoolRight = 6150;
        private const int PoolTop = 2130;

        // Rows above this are emptied. The drain mouths sit at 2182, so this is where vanilla flow would have settled; basin
        // water at and below it (down to ~2210 in the east) is meant to stay.
        private const int DrainLineY = 2182;

        // The cavern under the shafts, above the lava (which starts at row 2318). Water down here is drain spill that froze mid-fall.
        private const int CavernTop = 2215;
        private const int CavernBottom = 2310;

        // One top-gate tile of the shaft at x 5322. Every gate shares the lever's wire, so one tile tells whether they are open.
        private const int GateCheckX = 5325;
        private const int GateCheckY = 2182;

        // The eight drain shafts: tile columns and the row of each top gate. 5322 is blocked by ash under its bottom gate,
        // so on its own it can only fill, never drain.
        private static readonly (int Left, int Right, int TopRow)[] Shafts =
        {
            (5142, 5148, 2182),
            (5322, 5328, 2182),
            (5367, 5373, 2182),
            (5550, 5556, 2182),
            (5617, 5624, 2180),
            (5685, 5691, 2182),
            (5777, 5783, 2182),
            (5915, 5921, 2184),
        };

        // 20 ticks per row: the pool's 47 rows of water take ~15.7s, ending about when the camera reaches the west shot.
        // Empty rows are skipped instantly.
        private const int TicksPerRow = 20;

        // After the line reaches the drain mouths, the shafts and cavern are swept 6 times, 30 ticks apart, so water that
        // was still falling when the line arrived is caught too.
        private const int SweepCount = 6;
        private const int TicksBetweenSweeps = 30;

        // Cutscene: holds players within 300 tiles of the lever and pans from the lever end of the pool to the west end.
        private const string SceneId = "ArtoriasSewerDrain";
        private const float SceneRange = 300f * 16f;
        // Scene timeline, in ticks from the start:
        //   0-140     camera glides from the player to the opening shot (CameraInTicks)
        //   140-200   hold on the opening shot
        //   200-560   pan east -> middle (PanTicks = 6s)
        //   560-620   hold on the middle shot (MiddleHoldTicks)
        //   620-980   pan middle -> west
        //   then, once the drain is also done: hold LastShotHoldTicks, fade to black and back out on the player (FadeOutTicks,
        //   half out, half in). The drain itself takes ~940 ticks, so it finishes about as the second pan lands.
        private const int CameraInTicks = 140;
        private const int FirstPanTick = 200;
        private const int PanTicks = 360;
        private const int MiddleHoldTicks = 60;
        private const int LastShotHoldTicks = 60;
        private const int FadeOutTicks = 180;
        private static readonly Vector2 LeverWorld = new Vector2(5940f, 2103f) * 16f;
        // Each shot sits a little lower than the last so the camera follows the surface down (it falls 2135 -> 2182).
        private static readonly Vector2 OpeningShot = new Vector2(5918f, 2152f) * 16f;
        private static readonly Vector2 MiddleShot = new Vector2(5650f, 2164f) * 16f;
        private static readonly Vector2 WestShot = new Vector2(5350f, 2176f) * 16f;

        private const byte PacketStep = 0;
        private const byte PacketSweep = 1;

        private enum DrainPhase : byte
        {
            Idle,
            Lowering,
            Sweeping,
        }

        // Shared by every machine: the server drives it, clients copy it from packets (dust and sound read it).
        private static DrainPhase phase;
        private static int currentRow;

        // Server only.
        private static int rowTick;
        private static int sweepsDone;
        private static int sweepTimer;
        private static bool drained;
        private static bool gateStateKnown;
        private static bool gateWasOpen;
        private static bool awaitingVisitor;
        private static bool sceneRunning;
        private static bool sceneFinishQueued;
        private static int sceneTick;

        // Client only: cosmetic timers.
        private static int effectTimer;

        // Gate opening, played once as the drain starts.
        private static readonly SoundStyle GateOpenStyle = new SoundStyle("tsorcRevamp/Sounds/HollowKnight/open_large_gate");

        // Rushing-water loop (client only). Played without a position so the moving cutscene camera doesn't pan it;
        // PostUpdateEverything scales it by distance instead. Waits RushStartDelayTicks (30) after the gate sound, then fades
        // in over 60 ticks; fades out over 120 once the line reaches the drain mouths.
        private static readonly SoundStyle RushStyle = new SoundStyle("tsorcRevamp/Sounds/HollowKnight/water_rushing_loop") with { IsLooped = true, MaxInstances = 0 };
        private const int RushStartDelayTicks = 30;
        private const float RushMaxVolume = 0.9f;
        private const float RushFadeInPerTick = 1f / 60f;
        private const float RushFadeOutPerTick = 1f / 120f;
        private const float RushHearingRange = 2000f;
        private static SlotId rushSlot = SlotId.Invalid;
        private static float rushFade;

        public override void OnWorldLoad()
        {
            ResetState();
        }

        public override void OnWorldUnload()
        {
            ResetState();

            if (SoundEngine.TryGetActiveSound(rushSlot, out ActiveSound rushSound))
            {
                rushSound.Stop();
            }

            rushSlot = SlotId.Invalid;
            rushFade = 0f;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            if (drained)
            {
                tag["artoriasSewerDrained"] = true;
            }
        }

        public override void LoadWorldData(TagCompound tag)
        {
            drained = tag.ContainsKey("artoriasSewerDrained");
        }

        private static void ResetState()
        {
            phase = DrainPhase.Idle;
            currentRow = PoolTop;
            rowTick = 0;
            sweepsDone = 0;
            sweepTimer = 0;
            drained = false;
            gateStateKnown = false;
            gateWasOpen = false;
            awaitingVisitor = false;
            sceneRunning = false;
            sceneFinishQueued = false;
            sceneTick = 0;
            effectTimer = 0;
        }

        // Server / single player only (tModLoader skips PostUpdateWorld on clients).
        public override void PostUpdateWorld()
        {
            if (!tsorcRevampWorld.ExpandedAdventure)
            {
                return;
            }

            if (phase == DrainPhase.Idle)
            {
                if (drained)
                {
                    return;
                }

                // The gate tile must carry an actuator - if it doesn't, this is not the map we measured, so do nothing.
                Tile gateTile = Main.tile[GateCheckX, GateCheckY];
                bool gateIsGate = gateTile.HasTile && gateTile.HasActuator;
                bool gatesOpen = gateIsGate && gateTile.IsActuated;

                // First look after loading. Gates already open means a save from before this drain existed: the lever was
                // pulled but the pool never emptied. Treat it as a pull that hasn't played yet and wait for a visitor.
                if (!gateStateKnown)
                {
                    gateStateKnown = true;
                    gateWasOpen = gatesOpen;
                    awaitingVisitor = gatesOpen;
                    return;
                }

                bool justOpened = gatesOpen && !gateWasOpen;
                gateWasOpen = gatesOpen;

                if (justOpened)
                {
                    BeginDrain();
                    return;
                }

                // Waiting for an old save's pull to play: start once any player is within SceneRange of the lever, so
                // somebody is there to see the cutscene.
                if (awaitingVisitor)
                {
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player player = Main.player[i];
                        bool nearLever = player.active && !player.dead && player.Distance(LeverWorld) <= SceneRange;

                        if (nearLever)
                        {
                            awaitingVisitor = false;
                            BeginDrain();
                            break;
                        }
                    }
                }

                return;
            }

            if (phase == DrainPhase.Lowering)
            {
                // Fade the current row out over TicksPerRow ticks so the surface sinks smoothly instead of in 16px steps.
                rowTick++;
                int remainingAmount = 255 - 255 * rowTick / TicksPerRow;

                if (remainingAmount < 0)
                {
                    remainingAmount = 0;
                }

                ApplyDrainStep(currentRow, (byte)remainingAmount);
                SendPacket(PacketStep, currentRow, (byte)remainingAmount);

                if (rowTick >= TicksPerRow)
                {
                    rowTick = 0;
                    currentRow = NextRowWithWater(currentRow + 1);

                    if (currentRow >= DrainLineY)
                    {
                        drained = true;
                        phase = DrainPhase.Sweeping;
                        sweepsDone = 0;
                        sweepTimer = TicksBetweenSweeps;
                    }
                }
            }
            else if (phase == DrainPhase.Sweeping)
            {
                sweepTimer++;

                if (sweepTimer >= TicksBetweenSweeps)
                {
                    sweepTimer = 0;
                    sweepsDone++;
                    SweepDrainedWater();

                    bool lastSweep = sweepsDone >= SweepCount;
                    byte finishedFlag = 0;

                    if (lastSweep)
                    {
                        finishedFlag = 1;
                        phase = DrainPhase.Idle;
                    }

                    SendPacket(PacketSweep, currentRow, finishedFlag);
                }
            }

            // Camera script. The drain runs on regardless of the scene: Skip only ends the camera work.
            if (!sceneRunning)
            {
                return;
            }

            if (!CutsceneSystem.IsActive || CutsceneSystem.SceneId != SceneId)
            {
                sceneRunning = false;
                return;
            }

            // Skip goes out through the same fade to black as the normal ending instead of snapping the camera home.
            // SetSkipAllowed(false) hides the button and clears the request so this runs once.
            if (CutsceneSystem.SkipRequested)
            {
                CutsceneSystem.SetSkipAllowed(false);
                CutsceneSystem.FinishIn(FadeOutTicks);
                sceneFinishQueued = true;
            }

            sceneTick++;

            if (sceneFinishQueued)
            {
                return;
            }

            int secondPanTick = FirstPanTick + PanTicks + MiddleHoldTicks;
            int secondPanEndTick = secondPanTick + PanTicks;

            if (sceneTick == FirstPanTick)
            {
                CutsceneSystem.SetFocus(CutsceneFocus.AtPoint(MiddleShot), PanTicks);
            }
            else if (sceneTick == secondPanTick)
            {
                CutsceneSystem.SetFocus(CutsceneFocus.AtPoint(WestShot), PanTicks);
            }

            // End once the camera has reached the west shot and the line has reached the drain mouths (whichever is
            // later): hold the shot briefly, then fade to black and back out on the player.
            bool panFinished = sceneTick >= secondPanEndTick;
            bool drainFinished = phase != DrainPhase.Lowering;

            if (panFinished && drainFinished)
            {
                CutsceneSystem.FinishIn(LastShotHoldTicks + FadeOutTicks);
                sceneFinishQueued = true;
            }
        }

        public static bool IsRunning => phase != DrainPhase.Idle;

        /// <summary>Server / single player: starts lowering the water line from the topmost row that has water. A player passed
        /// as viewer (the /artoriasdrain caller) is held by the cutscene and gets Skip wherever they stand; otherwise only
        /// players near the lever are. Returns false when there was no water above the line or no cutscene could start.</summary>
        internal static bool BeginDrain(int viewer = -1)
        {
            currentRow = NextRowWithWater(PoolTop);
            rowTick = 0;

            if (currentRow >= DrainLineY)
            {
                // Nothing above the line (the pool already drained some other way): just tidy the shafts and cavern.
                drained = true;
                phase = DrainPhase.Sweeping;
                sweepsDone = 0;
                sweepTimer = TicksBetweenSweeps;
                return false;
            }

            phase = DrainPhase.Lowering;

            if (!Main.dedServ)
            {
                PlayDrainStartEffects();
            }

            if (CutsceneSystem.IsActive)
            {
                return false;
            }

            // Hold the players near the lever (anyone elsewhere keeps playing) plus the viewer. The viewer, else the
            // player nearest the lever, gets Skip.
            CutsceneCast cast = new CutsceneCast();
            cast.AllowSkip = true;
            cast.CameraInTicks = CameraInTicks;
            cast.FadeOutTicks = FadeOutTicks;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];

                if (!player.active || player.dead)
                {
                    continue;
                }

                float distance = player.Distance(LeverWorld);

                if (distance > SceneRange && i != viewer)
                {
                    continue;
                }

                cast.Players.Add(i);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    cast.LeadPlayer = i;
                }
            }

            if (viewer >= 0 && cast.Players.Contains(viewer))
            {
                cast.LeadPlayer = viewer;
            }

            if (cast.Players.Count == 0)
            {
                return false;
            }

            // Upper bound on the scene's length: the later of the drain (every row with water costs TicksPerRow) and the
            // camera route, plus the ending. The script's FinishIn trims it to the real end once both are done.
            int waterRows = 0;
            int scanRow = NextRowWithWater(currentRow);

            while (scanRow < DrainLineY)
            {
                waterRows++;
                scanRow = NextRowWithWater(scanRow + 1);
            }

            int drainTicks = waterRows * TicksPerRow;
            int cameraRouteTicks = FirstPanTick + PanTicks + MiddleHoldTicks + PanTicks;
            int sceneTicks = Math.Max(drainTicks, cameraRouteTicks) + LastShotHoldTicks + FadeOutTicks + 60;

            CutsceneSystem.Start(SceneId, sceneTicks, CutsceneFocus.AtPoint(OpeningShot), -1, cast);
            sceneRunning = true;
            sceneFinishQueued = false;
            sceneTick = 0;
            return true;
        }

        /// <summary>First row at or below startRow (and above the drain line) that holds any water in the pool's span.
        /// Returns DrainLineY when there is none.</summary>
        private static int NextRowWithWater(int startRow)
        {
            for (int y = startRow; y < DrainLineY; y++)
            {
                for (int x = PoolLeft; x <= PoolRight; x++)
                {
                    Tile tile = Main.tile[x, y];

                    if (tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Water)
                    {
                        return y;
                    }
                }
            }

            return DrainLineY;
        }

        /// <summary>
        /// Every machine: caps the water in one row at remainingAmount (255 = full tile). Lava is left alone. When the row
        /// reaches 0, every row above it is cleared as well, so any stray water a client missed converges to the server's state.
        /// </summary>
        private static void ApplyDrainStep(int row, byte remainingAmount)
        {
            for (int x = PoolLeft; x <= PoolRight; x++)
            {
                Tile tile = Main.tile[x, row];

                if (tile.LiquidType == LiquidID.Water && tile.LiquidAmount > remainingAmount)
                {
                    tile.LiquidAmount = remainingAmount;
                }
            }

            if (remainingAmount == 0)
            {
                ClearWaterIn(PoolLeft, PoolRight, PoolTop, row);
            }
        }

        /// <summary>Every machine: clears what's left above the line, inside the shafts and in the cavern below them.
        /// Basin water at and below the line, outside the shafts, is kept.</summary>
        private static void SweepDrainedWater()
        {
            ClearWaterIn(PoolLeft, PoolRight, PoolTop, DrainLineY - 1);

            foreach ((int Left, int Right, int TopRow) shaft in Shafts)
            {
                ClearWaterIn(shaft.Left, shaft.Right, shaft.TopRow, CavernBottom);
            }

            ClearWaterIn(PoolLeft, PoolRight, CavernTop, CavernBottom);
        }

        // Inclusive tile rectangle; water only, so the lava sea under the cavern is never touched.
        private static void ClearWaterIn(int left, int right, int top, int bottom)
        {
            for (int x = left; x <= right; x++)
            {
                for (int y = top; y <= bottom; y++)
                {
                    Tile tile = Main.tile[x, y];

                    if (tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Water)
                    {
                        tile.LiquidAmount = 0;
                    }
                }
            }
        }

        private static void SendPacket(byte action, int row, byte value)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                return;
            }

            ModPacket packet = ModContent.GetInstance<tsorcRevamp>().GetPacket();
            packet.Write(tsorcPacketID.ArtoriasSewerDrainSync);
            packet.Write(action);
            packet.Write((short)row);
            packet.Write(value);
            packet.Send();
        }

        /// <summary>Client side of the server's drain: applies the same tile edit and tracks the phase for dust and sound.</summary>
        public static void ReceivePacket(BinaryReader reader)
        {
            byte action = reader.ReadByte();
            int row = reader.ReadInt16();
            byte value = reader.ReadByte();

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                return;
            }

            if (action == PacketStep)
            {
                if (phase != DrainPhase.Lowering)
                {
                    PlayDrainStartEffects();
                }

                phase = DrainPhase.Lowering;
                currentRow = row;
                ApplyDrainStep(row, value);
            }
            else if (action == PacketSweep)
            {
                SweepDrainedWater();
                phase = DrainPhase.Sweeping;

                if (value == 1)
                {
                    phase = DrainPhase.Idle;
                }
            }
        }

        // The gates grinding open, plus a shake for anyone near the pool. Played at the opening shot, where the cutscene
        // camera starts.
        private static void PlayDrainStartEffects()
        {
            Vector2 poolCenter = new Vector2((PoolLeft + PoolRight) * 0.5f, DrainLineY) * 16f;

            SoundEngine.PlaySound(GateOpenStyle, OpeningShot);
            UsefulFunctions.ScreenShake(poolCenter, 6f, 40, distanceFalloff: 12000f);
            effectTimer = 0;
        }

        // Client / single player cosmetics while the line is sinking: water swirling down over each shaft, a low rumble and
        // splashing. Only shafts within ~90 tiles of the screen centre spawn dust.
        public override void PostUpdateEverything()
        {
            if (Main.dedServ)
            {
                return;
            }

            Vector2 screenCenter = Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;

            // Rushing water: once the gate sound has had RushStartDelayTicks to itself, fade toward full while the line
            // sinks; fade toward silent otherwise (the out-fade runs over the sweeps after the line reaches the drain mouths).
            // effectTimer counts ticks since the drain started; it is reset in PlayDrainStartEffects.
            if (phase == DrainPhase.Lowering)
            {
                if (effectTimer >= RushStartDelayTicks)
                {
                    rushFade = MathHelper.Min(rushFade + RushFadeInPerTick, 1f);
                }
            }
            else
            {
                rushFade = MathHelper.Max(rushFade - RushFadeOutPerTick, 0f);
            }

            bool rushPlaying = SoundEngine.TryGetActiveSound(rushSlot, out ActiveSound rushSound);

            if (rushFade > 0f)
            {
                if (!rushPlaying)
                {
                    rushSlot = SoundEngine.PlaySound(RushStyle);
                    rushPlaying = SoundEngine.TryGetActiveSound(rushSlot, out rushSound);
                }

                // Distance from the screen centre to the nearest point of the drain area: full volume inside it, silent
                // RushHearingRange px outside.
                float nearestX = MathHelper.Clamp(screenCenter.X, PoolLeft * 16f, PoolRight * 16f);
                float nearestY = MathHelper.Clamp(screenCenter.Y, PoolTop * 16f, CavernBottom * 16f);
                float distanceToPool = Vector2.Distance(screenCenter, new Vector2(nearestX, nearestY));
                float proximity = MathHelper.Clamp(1f - distanceToPool / RushHearingRange, 0f, 1f);

                if (rushPlaying)
                {
                    rushSound.Volume = rushFade * proximity * RushMaxVolume;
                }
            }
            else if (rushPlaying)
            {
                rushSound.Stop();
                rushSlot = SlotId.Invalid;
            }

            if (phase != DrainPhase.Lowering)
            {
                return;
            }

            effectTimer++;

            Vector2 poolCenter = new Vector2((PoolLeft + PoolRight) * 0.5f, DrainLineY) * 16f;
            float surfaceY = currentRow * 16f + 8f;

            if (effectTimer % 120 == 0)
            {
                UsefulFunctions.ScreenShake(poolCenter, 2.5f, 30, distanceFalloff: 12000f);
            }

            foreach ((int Left, int Right, int TopRow) shaft in Shafts)
            {
                float shaftCenterX = (shaft.Left + shaft.Right + 1) * 0.5f * 16f;
                Vector2 vortexPoint = new Vector2(shaftCenterX, surfaceY);

                if (Vector2.Distance(vortexPoint, screenCenter) > 1400f)
                {
                    continue;
                }

                // A funnel: spawned up to 8 tiles out on the surface, drifting inward and down toward the shaft centre.
                if (effectTimer % 3 == 0)
                {
                    float offsetX = Main.rand.NextFloat(-128f, 128f);
                    Vector2 dustPosition = new Vector2(shaftCenterX + offsetX, surfaceY + Main.rand.NextFloat(0f, 10f));
                    Vector2 dustVelocity = new Vector2(-offsetX * 0.03f, Main.rand.NextFloat(0.5f, 2f));
                    Dust swirl = Dust.NewDustPerfect(dustPosition, DustID.Water, dustVelocity, 0, default, Main.rand.NextFloat(1.1f, 1.6f));
                    swirl.noGravity = true;
                }

                if (effectTimer % 45 == 0)
                {
                    SoundEngine.PlaySound(SoundID.Splash with { Volume = 0.45f, Pitch = -0.3f }, vortexPoint);
                }
            }
        }
    }

    /// <summary>Testing aid: runs the Artorias sewer drain and its cutscene again, even if this world already drained.</summary>
    public class ArtoriasDrainCommand : ModCommand
    {
        public override CommandType Type => CommandType.World;

        public override string Command => "artoriasdrain";

        public override string Description => "Re-run the Ruins of Artorias sewer drain and its cutscene (expanded adventure map)";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (!tsorcRevampWorld.ExpandedAdventure)
            {
                caller.Reply("The Artorias sewer drain only exists on the expanded adventure map.", Color.OrangeRed);
                return;
            }

            if (ArtoriasSewerDrain.IsRunning)
            {
                caller.Reply("The Artorias sewer drain is already running.", Color.OrangeRed);
                return;
            }

            if (CutsceneSystem.IsActive)
            {
                caller.Reply("Another cutscene is running; wait for it to end.", Color.OrangeRed);
                return;
            }

            int viewer = -1;

            if (caller.Player != null)
            {
                viewer = caller.Player.whoAmI;
            }

            bool sceneStarted = ArtoriasSewerDrain.BeginDrain(viewer);

            if (sceneStarted)
            {
                caller.Reply("Artorias sewer drain started with its cutscene.", Color.Cyan);
            }
            else
            {
                caller.Reply("No water above the drain line, so there is nothing to drain. Test on a world copy from before the drain.", Color.OrangeRed);
            }
        }
    }
}
