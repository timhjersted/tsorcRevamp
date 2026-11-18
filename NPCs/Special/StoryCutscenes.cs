using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.BossItems;
using tsorcRevamp.Content.Items.Lore;
using tsorcRevamp.NPCs.Bosses.Birds;
using tsorcRevamp.Systems;

namespace tsorcRevamp.NPCs.Special
{
    public enum StoryScene : byte
    {
        None,
        ThroughGate,
        Reunion,
        Wisp,
        Counsel,
        Homecoming,
        Well,
        Idol,
        ElijahRescue,
        FireTemple,
        ShadowRumors,
        DesertPaths,
        RageWarning,
    }

    public enum StoryCue : byte
    {
        None,
        Seated,
        Standing,
        Studying,
        Vision,
        SevenFlames,
        Catalyst,
        Ward,
        CrestRises,
        Riddle,
        CrestReturns,
        Descend,
        Walking,
        Limping,
        Reaching,
        LoftGlint,
        SkullGlow,
        StoneRises,
        StoneToRed,
        StonePulse,
        WellSpeck,
        Memory,
        MemoryStrike,
        Depart,
        FireWarning,
        VioletGlow,
        Omen,
        BeastStirs,
        TimePasses,
        IdolOut,
        IdolResting,
        IdolStrike,
        IdolBack,
    }

    /// <summary>
    /// The rescue scenes for Asha and Araz. Each scene is a list of beats run in order on the server or in single player.
    /// Actors and packets carry the result to clients. Skip jumps to the directions the player still needs.
    /// </summary>
    public static class StoryCutscenes
    {
        private enum BeatKind : byte
        {
            Line,
            Wait,
            Walk,
            Turn,
            TurnActor,
            Camera,
            Zoom,
            Cue,
            EmoteOnRed,
            EmoteOnActor,
            OpenDoor,
            SkipLanding,
            ActorWalk,
            PropCue,
            GiveStone,
        }

        private enum Speaker : byte
        {
            Red,
            Asha,
            AshaVoice,
            Araz,
            UnknownVoice,
            Bodhi,
            Aaron,
            RedVoice,
            Elijah,
            ElijahVoice,
            Miakoda,
        }

        private enum Shot : byte
        {
            Red,
            RedAndActor,
            Tile,
        }

        private sealed class Beat
        {
            public BeatKind Kind;
            public Speaker LineSpeaker;
            public string LineKey;
            public bool HasRemixText;
            public int Ticks;
            public int Value;
            public float Position;
            public bool Snap;
            public Shot CameraShot;
            public StoryCue ActorCue;
            public Vector2 Tile;
            public Func<bool> Condition;

            public Beat If(Func<bool> condition)
            {
                Condition = condition;
                return this;
            }

            public Beat WithRemixText()
            {
                HasRemixText = true;
                return this;
            }
        }

        // Legacy tile coordinates; ExpandedWorldTransform moves their Y on the expanded map.
        private const int PrisonRowY = 1366;
        private const int CellDoorTileX = 288;
        private const int AshaChairTileX = 285;
        private const float CorridorDoorStopTile = 290.5f;
        private const float EmbraceStopTile = 286.4f;
        private const float TalkStopTile = 287.6f;
        private const float GateStopTile = 318.9f;
        private const int ArazRowY = 603;
        private const float ArazStandTile = 7322.5f;
        private const float CounselStopTile = 7318.5f;
        public const int WispShaftTileX = 7304;
        private const int VillageRowY = 712;
        private const float WellStopTile = 4208.6f;
        private const float WellShaftTile = 4205.5f;
        private const float WellSpeckRowY = 735f;
        private const int ElijahRowY = 1744;
        private const int ElijahDoorTileX = 4454;
        private const float ElijahDoorStopTile = 4455.8f;
        private const float ElijahTalkTile = 4451.6f;
        private const float ElijahChairTile = 4449.5f;
        private const float ElijahWindowRowY = 1738f;
        private const float FireReadTile = 4249.2f;
        private const float FireBookTile = 4251f;
        private const float FireBookRowY = 1827.4f;

        private const int SceneTicks = 60 * 60 * 15;
        private const int LineAutoTicks = 12 * 60;
        private const int LineGapTicks = 8;
        private const int BarCloseTicks = 20;
        private const int CameraEaseTicks = 50;
        private const int EmoteTicks = 180;
        private const float WalkArrivedDistance = 20f;
        private const int WalkTimeoutMargin = 90;
        private const float TalkZoom = 1.35f;
        private const float ResolveZoom = 1.6f;
        private const int ZoomTicks = 50;
        private const int WispSpawnOffset = 64;

        private static StoryScene activeScene;
        private static StoryScene finishedScene;
        private static List<Beat> beats;
        private static int beatIndex;
        private static bool beatStarted;
        private static bool lineClosed;
        private static int beatTimer;
        private static int walkTimeout;
        private static int leadPlayer = -1;
        private static int actorIndex = -1;
        private static int propIndex = -1;
        private static bool skipped;

        public static bool IsRunning => activeScene != StoryScene.None;

        private static string SceneId(StoryScene scene)
        {
            return "Story" + scene;
        }

        private static bool RageDead()
        {
            return tsorcRevampWorld.BossDefeated(ModContent.NPCType<TheRage>());
        }

        private static bool RageAlive()
        {
            return !RageDead();
        }

        private static bool WasSkipped()
        {
            return skipped;
        }

        private static bool NotSkipped()
        {
            return !skipped;
        }

        private static Beat Say(Speaker speaker, string lineKey)
        {
            return new Beat { Kind = BeatKind.Line, LineSpeaker = speaker, LineKey = lineKey };
        }

        private static Beat Wait(int ticks)
        {
            return new Beat { Kind = BeatKind.Wait, Ticks = ticks };
        }

        private static Beat WalkTo(float tileX, int facing)
        {
            return new Beat { Kind = BeatKind.Walk, Position = tileX, Value = facing };
        }

        private static Beat SnapTo(float tileX, int facing)
        {
            return new Beat { Kind = BeatKind.Walk, Position = tileX, Value = facing, Snap = true };
        }

        private static Beat Turn(int facing)
        {
            return new Beat { Kind = BeatKind.Turn, Value = facing };
        }

        private static Beat TurnActor(int facing)
        {
            return new Beat { Kind = BeatKind.TurnActor, Value = facing };
        }

        private static Beat Camera(Shot shot, int easeTicks)
        {
            return new Beat { Kind = BeatKind.Camera, CameraShot = shot, Ticks = easeTicks };
        }

        private static Beat CameraAt(float tileX, float legacyTileY, int easeTicks)
        {
            return new Beat { Kind = BeatKind.Camera, CameraShot = Shot.Tile, Tile = new Vector2(tileX, legacyTileY), Ticks = easeTicks };
        }

        private static Beat Zoom(float zoom, int easeTicks = ZoomTicks)
        {
            return new Beat { Kind = BeatKind.Zoom, Position = zoom, Ticks = easeTicks };
        }

        private static Beat Door(int tileX, int legacyTileY, int direction)
        {
            return new Beat { Kind = BeatKind.OpenDoor, Tile = new Vector2(tileX, legacyTileY), Value = direction };
        }

        private static Beat ActorWalk(float tileX, StoryCue gait)
        {
            return new Beat { Kind = BeatKind.ActorWalk, Position = tileX, ActorCue = gait };
        }

        private static Beat ActorSnap(float tileX)
        {
            return new Beat { Kind = BeatKind.ActorWalk, Position = tileX, Snap = true };
        }

        private static Beat Prop(StoryCue cue)
        {
            return new Beat { Kind = BeatKind.PropCue, ActorCue = cue };
        }

        private static Beat Cue(StoryCue cue)
        {
            return new Beat { Kind = BeatKind.Cue, ActorCue = cue };
        }

        private static Beat Emote(BeatKind kind, int emote)
        {
            return new Beat { Kind = kind, Value = emote };
        }

        private static List<Beat> BuildBeats(StoryScene scene)
        {
            switch (scene)
            {
                case StoryScene.ThroughGate:
                    return new List<Beat>
                    {
                        // Red presses up to the sealed gate before anyone speaks, so the voice has somewhere to come from.
                        Wait(25),
                        WalkTo(GateStopTile, -1),
                        Wait(30),
                        Say(Speaker.Red, "Line01"),
                        Wait(25),
                        Say(Speaker.AshaVoice, "Line02"),
                        Emote(BeatKind.EmoteOnRed, EmoteID.EmotionLove),
                        Wait(50),
                        Say(Speaker.Red, "Line03"),
                        Wait(20),
                    };

                case StoryScene.Reunion:
                    return new List<Beat>
                    {
                        Wait(25),
                        CameraAt(CellDoorTileX + 0.5f, PrisonRowY + 1, CameraEaseTicks),
                        WalkTo(CorridorDoorStopTile, -1),
                        Wait(25),
                        Door(CellDoorTileX, PrisonRowY, -1),
                        Wait(35),

                        // She hears the door, rises and turns. Red crosses the room, and the hearts stand in for the embrace.
                        Camera(Shot.RedAndActor, CameraEaseTicks),
                        Zoom(TalkZoom),
                        Wait(25),
                        Cue(StoryCue.Standing),
                        TurnActor(1),
                        Emote(BeatKind.EmoteOnActor, EmoteID.EmotionAlert),
                        Wait(55),
                        WalkTo(EmbraceStopTile, -1),
                        Emote(BeatKind.EmoteOnRed, EmoteID.EmotionLove),
                        Emote(BeatKind.EmoteOnActor, EmoteID.EmotionLove),
                        Wait(130),
                        WalkTo(TalkStopTile, -1),
                        Wait(20),

                        Say(Speaker.Red, "Line01"),
                        Say(Speaker.Asha, "Line02"),
                        Say(Speaker.Asha, "Line03"),
                        Say(Speaker.Red, "Line04"),
                        Say(Speaker.Asha, "Line05"),

                        // The camera closes on Red for the vow, then returns to the two of them.
                        Camera(Shot.Red, 30),
                        Zoom(ResolveZoom),
                        Wait(35),
                        Say(Speaker.Red, "Line06"),
                        Camera(Shot.RedAndActor, 30),
                        Zoom(TalkZoom),
                        Say(Speaker.Red, "Line07"),
                        Say(Speaker.Asha, "Line08"),

                        // He cannot look at her while answering.
                        Turn(1),
                        Wait(55),
                        Say(Speaker.Red, "Line09"),
                        Wait(20),
                        Turn(-1),
                        Say(Speaker.Asha, "Line10"),
                        Say(Speaker.Red, "Line11"),
                        Emote(BeatKind.EmoteOnActor, EmoteID.EmotionLove),
                        Say(Speaker.Red, "Line12").WithRemixText(),

                        new Beat { Kind = BeatKind.SkipLanding },
                        Door(CellDoorTileX, PrisonRowY, -1).If(WasSkipped),
                        Cue(StoryCue.Standing).If(WasSkipped),
                        TurnActor(1).If(WasSkipped),
                        SnapTo(TalkStopTile, -1).If(WasSkipped),
                        Camera(Shot.RedAndActor, BarCloseTicks).If(WasSkipped),
                        Zoom(TalkZoom).If(WasSkipped),
                        Wait(BarCloseTicks).If(WasSkipped),

                        Say(Speaker.Asha, "Line13"),
                        Say(Speaker.Asha, "Line14").WithRemixText(),
                        Say(Speaker.Asha, "Line15").WithRemixText(),
                        Say(Speaker.Red, "Line16"),
                        Zoom(1f),
                        Wait(ZoomTicks),
                    };

                case StoryScene.Wisp:
                    return new List<Beat>
                    {
                        Wait(75),
                        Say(Speaker.UnknownVoice, "Line01"),
                        Say(Speaker.Araz, "Line02"),
                        Say(Speaker.Red, "Line03"),

                        // The wisp shows where he is by diving down the shaft below the gates.
                        Cue(StoryCue.Descend),
                        Wait(StoryWispActor.DescendTicks - BarCloseTicks),
                    };

                case StoryScene.Counsel:
                    return new List<Beat>
                    {
                        Wait(25),
                        Camera(Shot.RedAndActor, CameraEaseTicks),
                        WalkTo(CounselStopTile, 1),
                        Zoom(TalkZoom),
                        Wait(15),
                        TurnActor(-1),
                        Cue(StoryCue.Standing),
                        Emote(BeatKind.EmoteOnActor, EmoteID.EmotionAlert),
                        Wait(60),

                        Say(Speaker.Red, "Line01"),
                        Say(Speaker.Red, "Line02"),
                        Say(Speaker.Red, "Line03"),
                        Wait(25),

                        Cue(StoryCue.Vision),
                        Wait(45),
                        Say(Speaker.Araz, "Line04"),
                        Say(Speaker.Araz, "Line05"),
                        Cue(StoryCue.SevenFlames),
                        Wait(75),
                        Say(Speaker.Araz, "Line06").If(RageDead),
                        Say(Speaker.Araz, "Line06NoRage").If(RageAlive),
                        Say(Speaker.Araz, "Line07"),

                        new Beat { Kind = BeatKind.SkipLanding },
                        TurnActor(-1).If(WasSkipped),
                        SnapTo(CounselStopTile, 1).If(WasSkipped),
                        Camera(Shot.RedAndActor, BarCloseTicks).If(WasSkipped),
                        Zoom(TalkZoom).If(WasSkipped),
                        Cue(StoryCue.SevenFlames).If(WasSkipped),
                        Wait(BarCloseTicks).If(WasSkipped),

                        Cue(StoryCue.Catalyst),
                        Wait(StoryArazActor.CatalystFormTicks + 20),
                        Say(Speaker.Araz, "Line08"),
                        Cue(StoryCue.Ward),
                        Wait(StoryArazActor.WardTravelTicks),
                        Say(Speaker.Araz, "Line09"),
                        Cue(StoryCue.Standing),
                        Say(Speaker.Red, "Line10").If(NotSkipped),
                        Say(Speaker.Araz, "Line11").If(NotSkipped),

                        Cue(StoryCue.CrestRises).If(RageDead),
                        Wait(StoryArazActor.CrestTravelTicks + 15).If(RageDead),
                        Say(Speaker.Araz, "Line12").If(RageDead),
                        Cue(StoryCue.Riddle).If(RageDead),
                        Wait(40).If(RageDead),
                        Say(Speaker.Araz, "Line13").If(RageDead).WithRemixText(),
                        Say(Speaker.Araz, "Line13NoRage").If(RageAlive),
                        Cue(StoryCue.CrestReturns).If(RageDead),
                        Wait(StoryArazActor.CrestTravelTicks).If(RageDead),
                        Zoom(1f),
                        Wait(ZoomTicks),
                    };

                case StoryScene.Homecoming:
                {
                    VillageMarks marks = Marks;
                    return new List<Beat>
                    {
                        // Bodhi sees Red in the doorway, then limps to him: the old injury is shown before it is mentioned.
                        Wait(20),
                        Camera(Shot.RedAndActor, CameraEaseTicks),
                        Door(marks.BodhiDoorX, VillageRowY, -1),
                        WalkTo(marks.BodhiDoorX - 0.5f, -1),
                        Wait(20),
                        Emote(BeatKind.EmoteOnActor, EmoteID.EmotionAlert),
                        Wait(50),
                        Cue(StoryCue.Standing),
                        Wait(30),
                        ActorWalk(marks.BodhiMeet, StoryCue.Limping),
                        WalkTo(marks.RedEmbrace, -1),
                        Emote(BeatKind.EmoteOnRed, EmoteID.EmotionLove),
                        Emote(BeatKind.EmoteOnActor, EmoteID.EmotionLove),
                        Wait(130),
                        WalkTo(marks.RedTalk, -1),
                        Zoom(TalkZoom),
                        Wait(20),

                        Say(Speaker.Bodhi, "Line01"),
                        Say(Speaker.Red, "Line02"),
                        Say(Speaker.Bodhi, "Line03"),
                        Say(Speaker.Bodhi, "Line04"),

                        // Red cannot look at him. The silence carries what the old sign had to describe.
                        Turn(1),
                        Emote(BeatKind.EmoteOnRed, EmoteID.EmoteSadness),
                        Wait(110),
                        Turn(-1),
                        Wait(25),
                        Say(Speaker.Bodhi, "Line05"),
                        Say(Speaker.Bodhi, "Line06"),
                        Say(Speaker.Red, "Line07"),
                        Wait(30),

                        Turn(1),
                        Say(Speaker.Red, "Line08"),
                        Wait(55),
                        Turn(-1),
                        Camera(Shot.Red, 30),
                        Zoom(ResolveZoom),
                        Wait(35),
                        Say(Speaker.Red, "Line09"),
                        Camera(Shot.RedAndActor, 30),
                        Zoom(TalkZoom),
                        Say(Speaker.Bodhi, "Line10"),
                        Say(Speaker.Bodhi, "Line11"),

                        new Beat { Kind = BeatKind.SkipLanding },
                        Cue(StoryCue.Standing).If(WasSkipped),
                        TurnActor(1).If(WasSkipped),
                        ActorSnap(marks.BodhiMeet).If(WasSkipped),
                        Door(marks.BodhiDoorX, VillageRowY, -1).If(WasSkipped),
                        SnapTo(marks.RedTalk, -1).If(WasSkipped),
                        Zoom(TalkZoom).If(WasSkipped),
                        Wait(BarCloseTicks).If(WasSkipped),

                        // The supplies are upstairs, so the camera goes to show them.
                        Prop(StoryCue.LoftGlint),
                        CameraAt(marks.LoftChest.X, marks.LoftChest.Y, 60),
                        Wait(60),
                        Say(Speaker.Bodhi, "Line12"),
                        Camera(Shot.RedAndActor, 50),
                        Wait(30),
                        Say(Speaker.Bodhi, "Line13"),
                        Say(Speaker.Red, "Line14"),
                        Zoom(1f),
                        Wait(ZoomTicks),
                    };
                }

                case StoryScene.Well:
                    return new List<Beat>
                    {
                        Wait(20),
                        WalkTo(WellStopTile, -1),
                        Wait(30),
                        Prop(StoryCue.WellSpeck),
                        CameraAt(WellShaftTile, WellSpeckRowY, 80),
                        Zoom(1.4f),
                        Wait(95),
                        Say(Speaker.RedVoice, "Line01"),
                        Wait(15),

                        Prop(StoryCue.Memory),
                        Wait(StoryPropActor.MemoryCryTick),
                        Say(Speaker.RedVoice, "Line02"),

                        // The white flash hides the camera returning to the surface.
                        Prop(StoryCue.MemoryStrike),
                        Camera(Shot.Red, 0),
                        Zoom(1f, 8),
                        Wait(10),
                        WalkTo(WellStopTile + 1.1f, -1),
                        Wait(StoryPropActor.MemoryStrikeTicks),
                        Wait(70),
                        Say(Speaker.Red, "Line03"),
                        Wait(20),
                    };

                case StoryScene.Idol:
                {
                    VillageMarks marks = Marks;
                    int entry = marks.EntryDirection;
                    return new List<Beat>
                    {
                        Wait(20),
                        Camera(Shot.RedAndActor, CameraEaseTicks),
                        Door(marks.AaronDoorX, VillageRowY, entry),
                        WalkTo(marks.RedByAaron, entry),
                        Zoom(TalkZoom),
                        Cue(StoryCue.Standing),
                        TurnActor(-entry),
                        Emote(BeatKind.EmoteOnActor, EmoteID.EmotionAlert),
                        Wait(45),

                        Say(Speaker.Aaron, "Line01"),
                        Say(Speaker.Red, "Line02"),
                        Say(Speaker.Aaron, "Line03"),
                        TurnActor(entry),
                        Wait(30),
                        TurnActor(-entry),
                        Say(Speaker.Red, "Line04"),

                        // Red says nothing while he understands what he has just chosen.
                        Camera(Shot.Red, 30),
                        Zoom(ResolveZoom),
                        Wait(100),
                        Camera(Shot.RedAndActor, 30),
                        Zoom(TalkZoom),
                        Say(Speaker.Red, "Line05"),
                        Say(Speaker.Aaron, "Line06"),
                        ActorWalk(marks.AaronBySkull, StoryCue.Walking),

                        new Beat { Kind = BeatKind.SkipLanding },
                        ActorSnap(marks.AaronBySkull).If(WasSkipped),
                        Door(marks.AaronDoorX, VillageRowY, entry).If(WasSkipped),
                        SnapTo(marks.RedByAaron, entry).If(WasSkipped),
                        Camera(Shot.RedAndActor, BarCloseTicks).If(WasSkipped),
                        Zoom(TalkZoom).If(WasSkipped),
                        Wait(BarCloseTicks).If(WasSkipped),

                        Cue(StoryCue.Reaching),
                        Prop(StoryCue.SkullGlow),
                        Wait(45),
                        Prop(StoryCue.StoneRises),
                        Wait(StoryPropActor.StoneTravelTicks),
                        TurnActor(-entry),
                        Cue(StoryCue.Standing),
                        Say(Speaker.Aaron, "Line07"),
                        Prop(StoryCue.StoneToRed),
                        Wait(StoryPropActor.StoneTravelTicks),
                        new Beat { Kind = BeatKind.GiveStone },

                        // Red turns to go, and only the player sees what the stone really is.
                        Turn(-entry),
                        Prop(StoryCue.StonePulse),
                        Wait(90),
                        Zoom(1f),
                        Wait(ZoomTicks),
                    };
                }

                case StoryScene.ElijahRescue:
                    return new List<Beat>
                    {
                        // Elijah is heard through the cell door before he is seen.
                        Wait(25),
                        Say(Speaker.ElijahVoice, "Line01"),
                        WalkTo(ElijahDoorStopTile, -1),
                        Wait(20),
                        Door(ElijahDoorTileX, ElijahRowY, -1),
                        Wait(30),
                        Camera(Shot.RedAndActor, CameraEaseTicks),
                        WalkTo(ElijahTalkTile, -1),
                        Zoom(TalkZoom),
                        Cue(StoryCue.Standing),
                        TurnActor(1),
                        Emote(BeatKind.EmoteOnRed, EmoteID.EmotionLove),
                        Emote(BeatKind.EmoteOnActor, EmoteID.EmotionLove),
                        Wait(110),

                        Say(Speaker.Elijah, "Line02"),
                        Say(Speaker.Elijah, "Line03"),
                        Emote(BeatKind.EmoteOnRed, EmoteID.EmotionAlert),
                        Wait(30),
                        Say(Speaker.Elijah, "Line04"),
                        Say(Speaker.Elijah, "Line05"),
                        Say(Speaker.Red, "Line06"),
                        Say(Speaker.Elijah, "Line07"),
                        Say(Speaker.Elijah, "Line08"),
                        Say(Speaker.Elijah, "Line09"),
                        Wait(25),
                        Say(Speaker.Elijah, "Line10"),

                        // The stone Aaron gave Red answers his name with one dark flicker.
                        Emote(BeatKind.EmoteOnRed, EmoteID.EmotionAnger),
                        Prop(StoryCue.StonePulse),
                        Wait(60),
                        Say(Speaker.Elijah, "Line11"),
                        Say(Speaker.Elijah, "Line12"),
                        Say(Speaker.Elijah, "Line13"),
                        Say(Speaker.Elijah, "Line14"),
                        Camera(Shot.Red, 30),
                        Zoom(ResolveZoom),
                        Wait(40),
                        Say(Speaker.Elijah, "Line15"),
                        Wait(70),
                        Camera(Shot.RedAndActor, 30),
                        Zoom(TalkZoom),
                        Say(Speaker.Elijah, "Line16"),
                        Say(Speaker.Elijah, "Line17"),

                        new Beat { Kind = BeatKind.SkipLanding },
                        Door(ElijahDoorTileX, ElijahRowY, -1).If(WasSkipped),
                        Cue(StoryCue.Standing).If(WasSkipped),
                        TurnActor(1).If(WasSkipped),
                        SnapTo(ElijahTalkTile, -1).If(WasSkipped),
                        Camera(Shot.RedAndActor, BarCloseTicks).If(WasSkipped),
                        Zoom(TalkZoom).If(WasSkipped),
                        Wait(BarCloseTicks).If(WasSkipped),

                        Say(Speaker.Elijah, "Line18"),
                        Prop(StoryCue.BeastStirs),
                        CameraAt(ElijahChairTile, ElijahWindowRowY, 50),
                        Wait(60),
                        Say(Speaker.Elijah, "Line19"),
                        Camera(Shot.RedAndActor, 40),
                        Say(Speaker.Elijah, "Line20"),
                        Prop(StoryCue.Omen),
                        Say(Speaker.Elijah, "Line21"),
                        Wait(20),
                        Say(Speaker.Elijah, "Line22"),
                        Zoom(1f),
                        Wait(ZoomTicks),
                    };

                case StoryScene.FireTemple:
                    return new List<Beat>
                    {
                        Wait(25),
                        Camera(Shot.RedAndActor, CameraEaseTicks),
                        WalkTo(FireReadTile, 1),
                        Zoom(TalkZoom),
                        Prop(StoryCue.LoftGlint),
                        Wait(35),
                        Say(Speaker.Red, "Line01"),
                        Say(Speaker.Miakoda, "Line02"),
                        Say(Speaker.Red, "Line03"),

                        // The night passes over the book.
                        Prop(StoryCue.TimePasses),
                        Wait(StoryPropActor.TimePassesTicks),
                        Prop(StoryCue.LoftGlint),
                        Wait(20),
                        Say(Speaker.Red, "Line04"),
                        Say(Speaker.Red, "Line05"),
                        Say(Speaker.Miakoda, "Line06"),
                        Say(Speaker.Miakoda, "Line07"),
                        Say(Speaker.Miakoda, "Line08"),
                        Wait(30),

                        // The stone leaves Red's pack by itself, stirred by the words on the page.
                        Prop(StoryCue.IdolOut),
                        Wait(StoryPropActor.StoneTravelTicks),
                        Prop(StoryCue.IdolResting),
                        Say(Speaker.Red, "Line09"),
                        Say(Speaker.Miakoda, "Line10"),
                        Say(Speaker.Red, "Line11"),
                        Wait(25),
                        Prop(StoryCue.IdolStrike),
                        Wait(StoryPropActor.IdolStrikeTicks),
                        Say(Speaker.Red, "Line12"),
                        Prop(StoryCue.IdolBack),
                        Wait(StoryPropActor.StoneTravelTicks),
                        Zoom(1f),
                        Wait(ZoomTicks),
                    };

                case StoryScene.ShadowRumors:
                    return new List<Beat>
                    {
                        Wait(45),
                        Camera(Shot.RedAndActor, CameraEaseTicks),
                        Zoom(TalkZoom),
                        Say(Speaker.Miakoda, "Line01"),
                        Say(Speaker.Miakoda, "Line02").WithRemixText(),
                        Wait(20),
                        Say(Speaker.Miakoda, "Line03"),
                        Say(Speaker.Miakoda, "Line04").WithRemixText(),
                        Wait(60),
                        Say(Speaker.Miakoda, "Line05"),
                        Zoom(1f),
                        Wait(ZoomTicks),
                    };

                case StoryScene.DesertPaths:
                    return new List<Beat>
                    {
                        Wait(45),
                        Camera(Shot.RedAndActor, CameraEaseTicks),
                        Zoom(TalkZoom),
                        Cue(StoryCue.VioletGlow),
                        Say(Speaker.Miakoda, "Line01"),
                        Cue(StoryCue.Standing),
                        Say(Speaker.Miakoda, "Line02"),
                        Say(Speaker.Miakoda, "Line03"),
                        Say(Speaker.Miakoda, "Line04"),
                        Say(Speaker.Miakoda, "Line05"),
                        Say(Speaker.Miakoda, "Line06"),
                        Zoom(1f),
                        Wait(ZoomTicks),
                    };

                case StoryScene.RageWarning:
                    return new List<Beat>
                    {
                        Wait(45),
                        Camera(Shot.RedAndActor, CameraEaseTicks),
                        Zoom(TalkZoom),
                        Say(Speaker.Miakoda, "Line01"),
                        Say(Speaker.Miakoda, "Line02"),
                        Say(Speaker.Miakoda, "Line03"),
                        Cue(StoryCue.FireWarning),
                        Wait(35),
                        Say(Speaker.Miakoda, "Line04"),
                        Say(Speaker.Miakoda, "Line05"),
                        Cue(StoryCue.Standing),
                        Zoom(1f),
                        Wait(ZoomTicks),
                    };
            }

            return new List<Beat>();
        }

        // Everything in Ashen Peak that differs between the two maps. Rows and tiles are legacy coordinates.
        private sealed class VillageMarks
        {
            public float BodhiChair;
            public int BodhiDoorX;
            public float BodhiMeet;
            public float RedEmbrace;
            public float RedTalk;
            public Vector2 LoftChest;
            public int AaronDoorX;
            public int EntryDirection;
            public float AaronStart;
            public float AaronBySkull;
            public float RedByAaron;
            public Point Skull;
        }

        private static readonly VillageMarks AdventureMarks = new VillageMarks
        {
            BodhiChair = 4236.5f,
            BodhiDoorX = 4246,
            BodhiMeet = 4241.5f,
            RedEmbrace = 4242.6f,
            RedTalk = 4243.7f,
            LoftChest = new Vector2(4227.5f, 699f),
            AaronDoorX = 4091,
            EntryDirection = -1,
            AaronStart = 4080.5f,
            AaronBySkull = 4085f,
            RedByAaron = 4088.5f,
            Skull = new Point(4085, 708),
        };

        private static readonly VillageMarks RemixMarks = new VillageMarks
        {
            BodhiChair = 4227.5f,
            BodhiDoorX = 4245,
            BodhiMeet = 4233.5f,
            RedEmbrace = 4234.6f,
            RedTalk = 4235.7f,
            LoftChest = new Vector2(4225.5f, 701f),
            AaronDoorX = 4068,
            EntryDirection = 1,
            AaronStart = 4077.5f,
            AaronBySkull = 4075.2f,
            RedByAaron = 4070.6f,
            Skull = new Point(4073, 713),
        };

        private static VillageMarks Marks
        {
            get
            {
                if (tsorcRevampWorld.RemixMap)
                {
                    return RemixMarks;
                }

                return AdventureMarks;
            }
        }

        // A sign the player has already read means they have lived this scene, so it must not replay on an old save.
        private static readonly Dictionary<Point, StoryScene> VillageSigns = new Dictionary<Point, StoryScene>
        {
            { new Point(4247, 709), StoryScene.Homecoming },
            { new Point(4249, 709), StoryScene.Homecoming },
            { new Point(4248, 713), StoryScene.Homecoming },
            { new Point(4244, 709), StoryScene.Homecoming },
            { new Point(4242, 709), StoryScene.Homecoming },
            { new Point(4234, 695), StoryScene.Homecoming },
            { new Point(4225, 699), StoryScene.Homecoming },
            { new Point(4223, 713), StoryScene.Homecoming },
            { new Point(4210, 713), StoryScene.Well },
            { new Point(4189, 679), StoryScene.Well },
            { new Point(4185, 655), StoryScene.Well },
            { new Point(4249, 1822), StoryScene.FireTemple },
            { new Point(4252, 1822), StoryScene.FireTemple },
            { new Point(4255, 1822), StoryScene.FireTemple },
            { new Point(1900, 1121), StoryScene.ShadowRumors },
            { new Point(1897, 1121), StoryScene.ShadowRumors },
            { new Point(2374, 843), StoryScene.DesertPaths },
            { new Point(2354, 843), StoryScene.DesertPaths },
            { new Point(7095, 1457), StoryScene.RageWarning },
            { new Point(7102, 1457), StoryScene.RageWarning },
            { new Point(4092, 709), StoryScene.Idol },
            { new Point(4089, 713), StoryScene.Idol },
            { new Point(4080, 713), StoryScene.Idol },
        };

        public static void NoteRetiredSignRead(int legacyTileX, int legacyTileY)
        {
            if (VillageSigns.TryGetValue(new Point(legacyTileX, legacyTileY), out StoryScene scene))
            {
                CutsceneSystem.MarkSeen(SceneId(scene));
            }
        }

        private static bool VillageSceneReady(StoryScene scene)
        {
            bool villageMap = tsorcRevampWorld.OnlyAdventureMap || tsorcRevampWorld.RemixMap;
            return villageMap && !Main.hardMode && !CutsceneSystem.IsActive && !CutsceneSystem.HasSeen(SceneId(scene));
        }

        private static bool IsMiakodaScene(StoryScene scene)
        {
            return scene == StoryScene.FireTemple || scene == StoryScene.ShadowRumors || scene == StoryScene.DesertPaths || scene == StoryScene.RageWarning;
        }

        public static bool IsMiakodaPet(int projectileType)
        {
            return projectileType == ModContent.ProjectileType<Content.Projectiles.Pets.Miakoda>()
                || projectileType == ModContent.ProjectileType<Content.Projectiles.Pets.MiakodaNew>()
                || projectileType == ModContent.ProjectileType<Content.Projectiles.Pets.MiakodaCrescent>()
                || projectileType == ModContent.ProjectileType<Content.Projectiles.Pets.MiakodaFull>();
        }

        /// <summary>True on every machine while a scene's Miakoda is standing in for this player's own.</summary>
        public static bool ReplacesMiakodaOf(int playerIndex)
        {
            if (!CutsceneSystem.IsActive || !CutsceneSystem.IsLead(Main.player[playerIndex]))
            {
                return false;
            }

            string sceneId = CutsceneSystem.SceneId;
            return sceneId == SceneId(StoryScene.FireTemple) || sceneId == SceneId(StoryScene.ShadowRumors)
                || sceneId == SceneId(StoryScene.DesertPaths) || sceneId == SceneId(StoryScene.RageWarning);
        }

        private static bool ConversationReady(StoryScene scene)
        {
            // Never pull the player out of a boss fight for a conversation.
            return tsorcRevampWorld.CustomMap && !Main.CurrentFrameFlags.AnyActiveBossNPC && !CutsceneSystem.IsActive && !CutsceneSystem.HasSeen(SceneId(scene));
        }

        public static bool FireTempleReady()
        {
            return ConversationReady(StoryScene.FireTemple) && !Main.hardMode;
        }

        public static bool ShadowRumorsReady()
        {
            return ConversationReady(StoryScene.ShadowRumors);
        }

        public static bool DesertPathsReady()
        {
            return tsorcRevampWorld.OnlyAdventureMap && ConversationReady(StoryScene.DesertPaths);
        }

        public static bool RageWarningReady()
        {
            return ConversationReady(StoryScene.RageWarning);
        }

        public static bool HomecomingReady()
        {
            return VillageSceneReady(StoryScene.Homecoming);
        }

        public static bool WellReady()
        {
            return VillageSceneReady(StoryScene.Well);
        }

        // Taking the stone from the skull by hand has already told this part of the story.
        public static bool IdolReady()
        {
            return VillageSceneReady(StoryScene.Idol) && StoneStillOnSkull();
        }

        public static bool AdventureIdolReady()
        {
            return tsorcRevampWorld.OnlyAdventureMap && IdolReady();
        }

        public static bool RemixIdolReady()
        {
            return tsorcRevampWorld.RemixMap && IdolReady();
        }

        private static bool IsSoulSkull(Tile tile)
        {
            return tile.HasTile && (tile.TileType == ModContent.TileType<Tiles.SoulSkullL>() || tile.TileType == ModContent.TileType<Tiles.SoulSkullR>());
        }

        // A soul skull's first 36 pixels of frames are its lit state; right-clicking shifts it to the dim state.
        private static bool StoneStillOnSkull()
        {
            Point skull = ExpandedWorldTransform.MapTile(Marks.Skull.X, Marks.Skull.Y);
            Tile tile = Main.tile[skull.X, skull.Y];
            return IsSoulSkull(tile) && tile.TileFrameX < 36;
        }

        private static void GiveStoneToPlayers()
        {
            if (!StoneStillOnSkull())
            {
                return;
            }

            // One stone per player, as the skull has always provided, delivered straight to each of them.
            EntitySource_Misc source = new EntitySource_Misc("Story Cutscene");
            int stoneType = ModContent.ItemType<AaronsProtectionStone>();
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active)
                {
                    Main.player[i].QuickSpawnItem(source, stoneType);
                }
            }

            Point skull = ExpandedWorldTransform.MapTile(Marks.Skull.X, Marks.Skull.Y);
            for (int x = skull.X; x < skull.X + 2; x++)
            {
                for (int y = skull.Y; y < skull.Y + 2; y++)
                {
                    Tile tile = Main.tile[x, y];
                    if (IsSoulSkull(tile) && tile.TileFrameX < 36)
                    {
                        tile.TileFrameX += 36;
                    }
                }
            }

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendTileSquare(-1, skull.X, skull.Y, 3);
            }
        }

        /// <summary>
        /// Debug reset: puts the story back so every scene can trigger again. Rescued characters go back to being
        /// unrescued, because their scenes only start while they are absent, and Aaron's skull is lit again.
        /// </summary>
        public static void ResetProgress()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                bool rescuedCharacter = npc.type == NPCID.Mechanic || npc.type == NPCID.Wizard || npc.type == NPCID.GoblinTinkerer;
                if (npc.active && rescuedCharacter)
                {
                    npc.active = false;
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i);
                    }
                }
            }

            NPC.savedMech = false;
            NPC.savedWizard = false;
            NPC.savedGoblin = false;

            // Frames 36-71 are the skull's dim state. Higher frames are its separate joke variant, left alone.
            Point skull = ExpandedWorldTransform.MapTile(Marks.Skull.X, Marks.Skull.Y);
            for (int x = skull.X; x < skull.X + 2; x++)
            {
                for (int y = skull.Y; y < skull.Y + 2; y++)
                {
                    Tile tile = Main.tile[x, y];
                    if (IsSoulSkull(tile) && tile.TileFrameX >= 36 && tile.TileFrameX < 72)
                    {
                        tile.TileFrameX -= 36;
                    }
                }
            }

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendTileSquare(-1, skull.X, skull.Y, 3);
                NetMessage.SendData(MessageID.WorldData);
            }
        }

        /// <summary>True on every machine while this town NPC is playing a part. The scene id is already synced.</summary>
        public static bool HoldsNpc(NPC npc)
        {
            if (!CutsceneSystem.IsActive)
            {
                return false;
            }

            bool bodhi = npc.type == NPCID.ArmsDealer && CutsceneSystem.SceneId == SceneId(StoryScene.Homecoming);
            bool aaron = npc.type == NPCID.Guide && CutsceneSystem.SceneId == SceneId(StoryScene.Idol);
            return bodhi || aaron;
        }

        /// <summary>Puts the real villager on their mark. If they have died, this brings them back under the same name.</summary>
        private static int PlaceVillager(int npcType, string nameKey, float tileX, int facing)
        {
            int villagerIndex = NPC.FindFirstNPC(npcType);
            if (villagerIndex < 0)
            {
                villagerIndex = NPC.NewNPC(new EntitySource_Misc("Story Cutscene"), (int)(tileX * 16f), 0, npcType);
                if (villagerIndex >= Main.maxNPCs)
                {
                    return -1;
                }

                Main.npc[villagerIndex].GivenName = Terraria.Localization.Language.GetTextValue("Mods.tsorcRevamp." + nameKey);
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.UniqueTownNPCInfoSyncRequest, -1, -1, null, villagerIndex);
                }
            }

            // Search for the real floor. Tables count as standable elsewhere, but a villager belongs beside them.
            Vector2 markPoint = ExpandedWorldTransform.MapWorld(new Vector2(tileX, VillageRowY) * 16f);
            Point floorTile = markPoint.ToTileCoordinates();
            while (floorTile.Y < Main.maxTilesY - 1 && !(Main.tile[floorTile.X, floorTile.Y].HasUnactuatedTile && Main.tileSolid[Main.tile[floorTile.X, floorTile.Y].TileType] && !Main.tileSolidTop[Main.tile[floorTile.X, floorTile.Y].TileType]))
            {
                floorTile.Y++;
            }

            float floorY = floorTile.Y * 16f;
            Point seatTile = new Vector2(markPoint.X, floorY - 8f).ToTileCoordinates();
            Tile seat = Main.tile[seatTile.X, seatTile.Y];

            StoryCue openingCue = StoryCue.Standing;
            if (seat.HasTile && seat.TileType == TileID.Chairs)
            {
                openingCue = StoryCue.Seated;
            }

            NPC villager = Main.npc[villagerIndex];
            villager.Bottom = new Vector2(markPoint.X, floorY);
            villager.velocity = Vector2.Zero;
            villager.ai[0] = (float)openingCue;
            villager.ai[1] = 0f;
            villager.ai[2] = facing;
            villager.ai[3] = villager.Center.X;
            villager.netUpdate = true;
            return villagerIndex;
        }

        private static int SpawnProp(Vector2 legacyTile, int lead)
        {
            Vector2 worldPoint = ExpandedWorldTransform.MapWorld(legacyTile * 16f);
            int newProp = NPC.NewNPC(new EntitySource_Misc("Story Cutscene"), (int)worldPoint.X, (int)worldPoint.Y, ModContent.NPCType<StoryPropActor>(), 0, (float)StoryCue.None, 0f, 0f, lead);
            if (newProp < Main.maxNPCs)
            {
                Main.npc[newProp].Center = worldPoint;
                Main.npc[newProp].netUpdate = true;
            }

            return newProp;
        }

        private static void OpenDoor(Beat beat)
        {
            // A two-tile doorway may keep its closed door on either tile, depending on how the map was saved.
            for (int offset = 0; offset < 2; offset++)
            {
                Point doorTile = ExpandedWorldTransform.MapTile((int)beat.Tile.X + offset, (int)beat.Tile.Y);
                Tile door = Main.tile[doorTile.X, doorTile.Y];
                if (!door.HasTile || door.TileType != TileID.ClosedDoor)
                {
                    continue;
                }

                bool opened = WorldGen.OpenDoor(doorTile.X, doorTile.Y, beat.Value);
                if (opened && Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.ToggleDoorState, -1, -1, null, 0, doorTile.X, doorTile.Y, beat.Value);
                }

                return;
            }
        }

        public static bool AshaUnrescued()
        {
            return !NPC.savedMech && !NPC.AnyNPCs(NPCID.Mechanic) && !CutsceneSystem.IsActive;
        }

        public static bool ArazUnrescued()
        {
            return !NPC.savedWizard && !NPC.AnyNPCs(NPCID.Wizard) && !CutsceneSystem.IsActive;
        }

        /// <summary>Ring action, once per tick while its event runs. The runner itself ticks from StoryCutsceneSystem.</summary>
        public static EventActionStatus RunRing(ScriptedEvent ringEvent, StoryScene scene)
        {
            if (finishedScene == scene)
            {
                finishedScene = StoryScene.None;
                return EventActionStatus.CompletedEvent;
            }

            if (activeScene == scene)
            {
                return EventActionStatus.Continue;
            }

            bool isRescue = scene == StoryScene.Reunion || scene == StoryScene.Counsel || scene == StoryScene.ElijahRescue;
            bool alreadyRescued = (scene == StoryScene.Reunion && NPC.savedMech) || (scene == StoryScene.Counsel && NPC.savedWizard) || (scene == StoryScene.ElijahRescue && NPC.savedGoblin);

            // Older saves and non-map worlds keep the original behavior: the character simply appears.
            if (isRescue && (alreadyRescued || !tsorcRevampWorld.CustomMap))
            {
                SpawnRescuedCharacter(scene, RescueSpawnPoint(scene));
                return EventActionStatus.CompletedEvent;
            }

            bool started = Play(scene, ringEvent.TriggeringPlayer);
            if (!started)
            {
                return EventActionStatus.FailedEvent;
            }

            return EventActionStatus.Continue;
        }

        private static Vector2 RescueSpawnPoint(StoryScene scene)
        {
            Vector2 legacyPoint = new Vector2(ArazStandTile, ArazRowY) * 16f;
            if (scene == StoryScene.Reunion)
            {
                legacyPoint = new Vector2(AshaChairTileX + 0.5f, PrisonRowY) * 16f;
            }
            else if (scene == StoryScene.ElijahRescue)
            {
                legacyPoint = new Vector2(ElijahChairTile, ElijahRowY) * 16f;
            }

            Vector2 worldPoint = ExpandedWorldTransform.MapWorld(legacyPoint);
            worldPoint.Y = AttraidiesCutscenes.FindFloorY(worldPoint.X, worldPoint.Y);
            return worldPoint;
        }

        private static void SpawnRescuedCharacter(StoryScene scene, Vector2 feetPosition)
        {
            int npcType = NPCID.Wizard;
            if (scene == StoryScene.Reunion)
            {
                npcType = NPCID.Mechanic;
            }
            else if (scene == StoryScene.ElijahRescue)
            {
                npcType = NPCID.GoblinTinkerer;
            }

            if (!NPC.AnyNPCs(npcType))
            {
                int rescuedIndex = NPC.NewNPC(new EntitySource_Misc("Story Cutscene"), (int)feetPosition.X, (int)feetPosition.Y, npcType);
                if (scene == StoryScene.ElijahRescue && rescuedIndex < Main.maxNPCs)
                {
                    // He keeps the room he has always been assigned until the player gives him another.
                    Point home = ExpandedWorldTransform.MapTile(4449, 1740);
                    Main.npc[rescuedIndex].homeTileX = home.X;
                    Main.npc[rescuedIndex].homeTileY = home.Y;
                }
            }

            if (scene == StoryScene.Reunion)
            {
                NPC.savedMech = true;
            }
            else if (scene == StoryScene.ElijahRescue)
            {
                NPC.savedGoblin = true;
            }
            else
            {
                NPC.savedWizard = true;
            }

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.WorldData);
            }
        }

        /// <summary>Starts a scene on the server or in single player. Returns false if it could not begin.</summary>
        public static bool Play(StoryScene scene, int lead)
        {
            bool leadValid = lead >= 0 && lead < Main.maxPlayers && Main.player[lead].active && !Main.player[lead].dead;
            if (Main.netMode == NetmodeID.MultiplayerClient || IsRunning || CutsceneSystem.IsActive || !leadValid)
            {
                return false;
            }

            Player leadCharacter = Main.player[lead];
            EntitySource_Misc source = new EntitySource_Misc("Story Cutscene");
            int spawnedActor = -1;

            switch (scene)
            {
                case StoryScene.Reunion:
                {
                    Vector2 seatPoint = RescueSpawnPoint(scene);
                    Point seatTile = (seatPoint - new Vector2(0f, 8f)).ToTileCoordinates();
                    bool hasChair = Main.tile[seatTile.X, seatTile.Y].HasTile && Main.tile[seatTile.X, seatTile.Y].TileType == TileID.Chairs;

                    StoryCue openingCue = StoryCue.Standing;
                    if (hasChair)
                    {
                        openingCue = StoryCue.Seated;
                    }

                    spawnedActor = NPC.NewNPC(source, (int)seatPoint.X, (int)seatPoint.Y, ModContent.NPCType<StoryAshaActor>(), 0, (float)openingCue, 0f, -1f, lead);
                    break;
                }

                case StoryScene.Counsel:
                {
                    Vector2 standPoint = RescueSpawnPoint(scene);
                    spawnedActor = NPC.NewNPC(source, (int)standPoint.X, (int)standPoint.Y, ModContent.NPCType<StoryArazActor>(), 0, (float)StoryCue.Studying, 0f, 1f, lead);
                    break;
                }

                case StoryScene.Wisp:
                {
                    int wispX = (int)leadCharacter.Center.X + leadCharacter.direction * WispSpawnOffset;
                    spawnedActor = NPC.NewNPC(source, wispX, (int)leadCharacter.Top.Y, ModContent.NPCType<StoryWispActor>(), 0, (float)StoryCue.None, 0f, -leadCharacter.direction, lead);
                    break;
                }
            }

            int spawnedProp = -1;
            switch (scene)
            {
                case StoryScene.Homecoming:
                    spawnedActor = PlaceVillager(NPCID.ArmsDealer, "Cutscenes.Speakers.Bodhi", Marks.BodhiChair, 1);
                    spawnedProp = SpawnProp(Marks.LoftChest, lead);
                    break;

                case StoryScene.Well:
                    spawnedProp = SpawnProp(new Vector2(WellShaftTile, WellSpeckRowY), lead);
                    break;

                case StoryScene.Idol:
                    spawnedActor = PlaceVillager(NPCID.Guide, "Cutscenes.Speakers.Aaron", Marks.AaronStart, -Marks.EntryDirection);
                    spawnedProp = SpawnProp(new Vector2(Marks.Skull.X + 1f, Marks.Skull.Y + 1f), lead);
                    break;

                case StoryScene.ElijahRescue:
                {
                    Vector2 chairPoint = RescueSpawnPoint(scene);
                    Point chairTile = (chairPoint - new Vector2(0f, 8f)).ToTileCoordinates();
                    StoryCue elijahCue = StoryCue.Standing;
                    if (Main.tile[chairTile.X, chairTile.Y].HasTile && Main.tile[chairTile.X, chairTile.Y].TileType == TileID.Chairs)
                    {
                        elijahCue = StoryCue.Seated;
                    }

                    spawnedActor = NPC.NewNPC(source, (int)chairPoint.X, (int)chairPoint.Y, ModContent.NPCType<StoryElijahActor>(), 0, (float)elijahCue, 0f, -1f, lead);
                    spawnedProp = SpawnProp(new Vector2(ElijahChairTile, ElijahWindowRowY), lead);
                    break;
                }
            }

            if (IsMiakodaScene(scene))
            {
                // She starts where the pet already is, or beside Red when no pet is out.
                int side = leadCharacter.direction;
                Vector2 miakodaStart = leadCharacter.Center + new Vector2(side * 46f, -38f);
                Projectile pet = StoryMiakodaActor.FindPet(lead);
                if (pet != null)
                {
                    miakodaStart = pet.Center;
                }

                spawnedActor = NPC.NewNPC(source, (int)miakodaStart.X, (int)miakodaStart.Y, ModContent.NPCType<StoryMiakodaActor>(), 0, (float)StoryCue.None, 0f, -side, lead);
                if (spawnedActor < Main.maxNPCs)
                {
                    Main.npc[spawnedActor].Center = miakodaStart;
                    Main.npc[spawnedActor].netUpdate = true;
                }

                if (scene == StoryScene.FireTemple)
                {
                    spawnedProp = SpawnProp(new Vector2(FireBookTile, FireBookRowY), lead);
                }
            }

            bool needsActor = scene != StoryScene.ThroughGate && scene != StoryScene.Well;
            bool needsProp = scene == StoryScene.Homecoming || scene == StoryScene.Well || scene == StoryScene.Idol || scene == StoryScene.ElijahRescue || scene == StoryScene.FireTemple;
            bool actorMissing = needsActor && (spawnedActor < 0 || spawnedActor >= Main.maxNPCs);
            bool propMissing = needsProp && (spawnedProp < 0 || spawnedProp >= Main.maxNPCs);
            if (actorMissing || propMissing)
            {
                return false;
            }

            activeScene = scene;
            finishedScene = StoryScene.None;
            beats = BuildBeats(scene);
            beatIndex = 0;
            beatStarted = false;
            lineClosed = false;
            beatTimer = 0;
            leadPlayer = lead;
            actorIndex = spawnedActor;
            propIndex = spawnedProp;
            skipped = false;

            CutsceneCast cast = AttraidiesCutscenes.BuildCast(leadCharacter.Center, lead, SceneId(scene));
            // Miakoda's short talks hold hints, so they advance with Next rather than offering Skip.
            cast.AllowSkip = !IsMiakodaScene(scene) || scene == StoryScene.FireTemple;

            CutsceneFocus openingFocus = CutsceneFocus.OnPlayer(lead);
            if (needsActor)
            {
                openingFocus = CutsceneFocus.BetweenPlayerAndNpc(lead, spawnedActor);
            }

            CutsceneSystem.Start(SceneId(scene), SceneTicks, openingFocus, -1, cast);
            return true;
        }

        private static void SetActorCue(StoryCue cue)
        {
            NPC actor = Main.npc[actorIndex];
            actor.ai[0] = (float)cue;
            actor.ai[1] = 0f;
            actor.netUpdate = true;
        }


        private static void ShowLine(Beat beat)
        {
            string prefix = activeScene.ToString();
            string textKey = "Cutscenes." + prefix + "." + beat.LineKey;
            if (beat.HasRemixText && tsorcRevampWorld.RemixMap)
            {
                textKey += "Remix";
            }

            const string RedName = "Cutscenes.Speakers.Red";
            const string AshaName = "Cutscenes.Speakers.Asha";
            const string ArazName = "Cutscenes.Speakers.Araz";
            const string VoiceName = "Cutscenes.Speakers.Voice";
            const string BodhiName = "Cutscenes.Speakers.Bodhi";
            const string AaronName = "Cutscenes.Speakers.Aaron";
            const string ElijahName = "Cutscenes.Speakers.Elijah";
            const string MiakodaName = "Cutscenes.Speakers.Miakoda";
            CutsceneLine line;

            switch (beat.LineSpeaker)
            {
                case Speaker.Red:
                    line = CutsceneLine.FromPlayer(leadPlayer, RedName, textKey);
                    break;

                case Speaker.Asha:
                    line = CutsceneLine.FromNpc(actorIndex, AshaName, textKey);
                    break;

                case Speaker.AshaVoice:
                    line = CutsceneLine.FromNobody(AshaName, textKey);
                    break;

                case Speaker.Araz:
                    line = CutsceneLine.FromNpc(actorIndex, ArazName, textKey);
                    break;

                case Speaker.Bodhi:
                    line = CutsceneLine.FromNpc(actorIndex, BodhiName, textKey);
                    break;

                case Speaker.Aaron:
                    line = CutsceneLine.FromNpc(actorIndex, AaronName, textKey);
                    break;

                case Speaker.RedVoice:
                    line = CutsceneLine.FromNobody(RedName, textKey);
                    break;

                case Speaker.Elijah:
                    line = CutsceneLine.FromNpc(actorIndex, ElijahName, textKey);
                    break;

                case Speaker.ElijahVoice:
                    line = CutsceneLine.FromNobody(ElijahName, textKey);
                    break;

                case Speaker.Miakoda:
                    line = CutsceneLine.FromNpc(actorIndex, MiakodaName, textKey);
                    break;

                default:
                    line = CutsceneLine.FromNpc(actorIndex, VoiceName, textKey);
                    break;
            }

            CutsceneSystem.ShowLine(line, LineAutoTicks);
        }

        /// <summary>Applies a beat once. Beats with nothing to wait for finish on the same tick.</summary>
        private static void StartBeat(Beat beat)
        {
            Player lead = Main.player[leadPlayer];

            switch (beat.Kind)
            {
                case BeatKind.Line:
                    lineClosed = false;
                    ShowLine(beat);
                    break;

                case BeatKind.Walk:
                {
                    // Only X is staged; ExpandedWorldTransform never changes X.
                    float worldX = beat.Position * 16f;
                    float distance = Math.Abs(worldX - lead.Center.X);
                    walkTimeout = (int)(distance / 2f) + WalkTimeoutMargin;
                    CutsceneSystem.DirectPlayer(leadPlayer, worldX, beat.Value, beat.Snap);
                    break;
                }

                case BeatKind.Turn:
                    CutsceneSystem.DirectPlayer(leadPlayer, float.NaN, beat.Value, false);
                    break;

                case BeatKind.TurnActor:
                    Main.npc[actorIndex].ai[2] = beat.Value;
                    Main.npc[actorIndex].netUpdate = true;
                    break;

                case BeatKind.Camera:
                {
                    CutsceneFocus focus = CutsceneFocus.OnPlayer(leadPlayer);
                    if (beat.CameraShot == Shot.RedAndActor)
                    {
                        focus = CutsceneFocus.BetweenPlayerAndNpc(leadPlayer, actorIndex);
                    }
                    else if (beat.CameraShot == Shot.Tile)
                    {
                        focus = CutsceneFocus.AtPoint(ExpandedWorldTransform.MapWorld(beat.Tile * 16f));
                    }

                    CutsceneSystem.SetFocus(focus, beat.Ticks);
                    break;
                }

                case BeatKind.Zoom:
                    CutsceneSystem.SetZoom(beat.Position, beat.Ticks);
                    break;

                case BeatKind.Cue:
                    SetActorCue(beat.ActorCue);
                    break;

                case BeatKind.EmoteOnRed:
                    EmoteBubble.NewBubble(beat.Value, new WorldUIAnchor(lead), EmoteTicks);
                    break;

                case BeatKind.EmoteOnActor:
                    EmoteBubble.NewBubble(beat.Value, new WorldUIAnchor(Main.npc[actorIndex]), EmoteTicks);
                    break;

                case BeatKind.OpenDoor:
                    OpenDoor(beat);
                    break;

                case BeatKind.PropCue:
                    Main.npc[propIndex].ai[0] = (float)beat.ActorCue;
                    Main.npc[propIndex].ai[1] = 0f;
                    Main.npc[propIndex].netUpdate = true;
                    break;

                case BeatKind.GiveStone:
                    GiveStoneToPlayers();
                    break;

                case BeatKind.ActorWalk:
                {
                    NPC actor = Main.npc[actorIndex];
                    float worldX = beat.Position * 16f;
                    if (beat.Snap)
                    {
                        actor.Center = new Vector2(worldX, actor.Center.Y);
                        actor.ai[3] = worldX;
                        actor.netUpdate = true;
                        break;
                    }

                    float distance = worldX - actor.Center.X;
                    walkTimeout = (int)(Math.Abs(distance) * 3f) + WalkTimeoutMargin;
                    actor.ai[2] = Math.Sign(distance);
                    actor.ai[3] = worldX;
                    SetActorCue(beat.ActorCue);
                    break;
                }

                case BeatKind.SkipLanding:
                    // From here on the lines carry directions the player needs, so they cannot be skipped.
                    CutsceneSystem.SetSkipAllowed(false);
                    break;
            }
        }

        private static bool IsBeatDone(Beat beat)
        {
            switch (beat.Kind)
            {
                case BeatKind.Line:
                    if (!lineClosed && CutsceneSystem.LineFinished)
                    {
                        lineClosed = true;
                        beatTimer = 0;
                        CutsceneSystem.ClearLine();
                    }

                    return lineClosed && beatTimer >= LineGapTicks;

                case BeatKind.Wait:
                    return beatTimer >= beat.Ticks;

                case BeatKind.Walk:
                {
                    Player lead = Main.player[leadPlayer];
                    float remaining = Math.Abs(beat.Position * 16f - lead.Center.X);
                    return !lead.active || remaining <= WalkArrivedDistance || beatTimer >= walkTimeout;
                }

                case BeatKind.ActorWalk:
                {
                    if (beat.Snap)
                    {
                        return true;
                    }

                    NPC actor = Main.npc[actorIndex];
                    bool arrived = Math.Abs(beat.Position * 16f - actor.Center.X) <= 1f;
                    if (arrived || beatTimer >= walkTimeout)
                    {
                        SetActorCue(StoryCue.Standing);
                        return true;
                    }

                    return false;
                }
            }

            return true;
        }

        private static void FinishScene()
        {
            StoryScene scene = activeScene;
            bool hasActor = actorIndex >= 0 && Main.npc[actorIndex].active && Main.npc[actorIndex].ModNPC is StoryActor;

            if (scene == StoryScene.Reunion || scene == StoryScene.Counsel || scene == StoryScene.ElijahRescue)
            {
                Vector2 feetPosition = RescueSpawnPoint(scene);
                if (hasActor)
                {
                    feetPosition = Main.npc[actorIndex].Bottom;
                }

                SpawnRescuedCharacter(scene, feetPosition);
            }

            if (hasActor)
            {
                // Hold the frame while the bars close, since the camera can no longer follow a removed actor.
                NPC actor = Main.npc[actorIndex];
                Vector2 finalShot = (Main.player[leadPlayer].Center + actor.Center) * 0.5f;
                CutsceneSystem.SetFocus(CutsceneFocus.AtPoint(finalShot));

                // The wisp finishes its dive and removes itself; a skipped scene sends it on its way here.
                if (actor.ModNPC is StoryMiakodaActor)
                {
                    SetActorCue(StoryCue.Depart);
                }
                else if (scene != StoryScene.Wisp)
                {
                    ((StoryActor)actor.ModNPC).Remove();
                }
                else if ((StoryCue)(int)actor.ai[0] != StoryCue.Descend)
                {
                    SetActorCue(StoryCue.Descend);
                }
            }

            if (propIndex >= 0 && Main.npc[propIndex].active && Main.npc[propIndex].ModNPC is StoryPropActor stageProp)
            {
                stageProp.Remove();
            }

            CutsceneSystem.MarkSeen(SceneId(scene));
            CutsceneSystem.SetZoom(1f, BarCloseTicks);
            CutsceneSystem.FinishIn(BarCloseTicks);

            activeScene = StoryScene.None;
            finishedScene = scene;
            beats = null;
        }

        /// <summary>Advances the running scene. Beats run until one has to wait.</summary>
        internal static void Tick()
        {
            if (!IsRunning)
            {
                return;
            }

            // The anchor vanished or something stopped the scene: settle the story state instead of hanging.
            if (!CutsceneSystem.IsActive || CutsceneSystem.SceneId != SceneId(activeScene))
            {
                FinishScene();
                return;
            }

            if (CutsceneSystem.SkipRequested)
            {
                skipped = true;
                CutsceneSystem.SetSkipAllowed(false);
                CutsceneSystem.ClearLine();

                // Scenes with directions land on them; the others simply end.
                int landingIndex = beats.FindIndex(beat => beat.Kind == BeatKind.SkipLanding);
                if (landingIndex >= beatIndex)
                {
                    beatIndex = landingIndex;
                }
                else
                {
                    beatIndex = beats.Count;
                }

                beatStarted = false;
            }

            while (beatIndex < beats.Count)
            {
                Beat beat = beats[beatIndex];
                bool shouldRun = beat.Condition == null || beat.Condition();
                if (!shouldRun)
                {
                    beatIndex++;
                    continue;
                }

                if (!beatStarted)
                {
                    beatStarted = true;
                    beatTimer = 0;
                    StartBeat(beat);
                }

                if (!IsBeatDone(beat))
                {
                    beatTimer++;
                    return;
                }

                beatIndex++;
                beatStarted = false;
            }

            FinishScene();
        }

        internal static void Reset()
        {
            activeScene = StoryScene.None;
            finishedScene = StoryScene.None;
            beats = null;
            leadPlayer = -1;
            actorIndex = -1;
            propIndex = -1;
        }
    }

    public class StoryCutsceneSystem : ModSystem
    {
        public override void PostUpdateNPCs()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                StoryCutscenes.Tick();
            }
        }

        public override void OnWorldLoad()
        {
            StoryCutscenes.Reset();
        }

        public override void OnWorldUnload()
        {
            StoryCutscenes.Reset();
        }
    }

    /// <summary>
    /// A scripted stand-in: harmless, stationary and removed when its scene ends. ai[0] is the cue, ai[1] counts ticks since
    /// that cue, ai[2] is facing and ai[3] is the lead player. Vanilla syncs all four, so every machine plays the same staging.
    /// </summary>
    public abstract class StoryActor : ModNPC
    {
        protected StoryCue Cue => (StoryCue)(int)NPC.ai[0];
        protected int CueTimer => (int)NPC.ai[1];
        protected Player Lead => Main.player[(int)NPC.ai[3]];

        public override void SetStaticDefaults()
        {
            NPCID.Sets.NPCBestiaryDrawModifiers hiddenFromBestiary = new NPCID.Sets.NPCBestiaryDrawModifiers();
            hiddenFromBestiary.Hide = true;
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, hiddenFromBestiary);
        }

        public override void SetDefaults()
        {
            NPC.width = 18;
            NPC.height = 40;
            NPC.aiStyle = -1;
            NPC.damage = 0;
            NPC.defense = 0;
            NPC.lifeMax = 1000;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.dontTakeDamage = true;
            NPC.dontTakeDamageFromHostiles = true;
            NPC.knockBackResist = 0f;
            NPC.npcSlots = 0f;
            NPC.chaseable = false;
            NPC.ShowNameOnHover = false;
        }

        public override bool CheckActive()
        {
            return false;
        }

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }

        /// <summary>Server or single player: removes the actor without any death effects.</summary>
        public void Remove()
        {
            NPC.active = false;
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
            }
        }

        public override void AI()
        {
            NPC.velocity = Vector2.Zero;
            NPC.timeLeft = 600;
            NPC.direction = (int)NPC.ai[2];
            NPC.spriteDirection = NPC.direction;

            int cueTimer = CueTimer;
            NPC.ai[1]++;
            PerformCue(Cue, cueTimer);
        }

        protected abstract void PerformCue(StoryCue cue, int cueTimer);
    }

    /// <summary>Draws one of vanilla's town NPC sheets, picking poses from its idle, talking, sitting and attack frames.</summary>
    public abstract class StoryTownActor : StoryActor
    {
        protected abstract int VanillaType { get; }
        protected virtual bool IsCasting => false;

        public override string Texture => UsefulFunctions.VanillaTextureFilepath(VanillaType, "NPC");

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            Main.npcFrameCount[Type] = Main.npcFrameCount[VanillaType];
        }

        public override void FindFrame(int frameHeight)
        {
            // Every town sheet ends in its attack frames; the sit and talk poses sit at fixed offsets before them.
            int firstAttackFrame = Main.npcFrameCount[Type] - NPCID.Sets.AttackFrameCount[VanillaType];
            int sitFrame = firstAttackFrame - 3;
            int talkFrame = firstAttackFrame - 1;
            int frameIndex = 0;

            if (Cue == StoryCue.Seated)
            {
                frameIndex = sitFrame;
            }
            else if (IsCasting)
            {
                frameIndex = firstAttackFrame;
            }
            else if (CutsceneSystem.IsNpcSpeaking(NPC.whoAmI) && CueTimer % 16 < 8)
            {
                frameIndex = talkFrame;
            }

            NPC.frame.Y = frameIndex * frameHeight;
        }
    }

    public class StoryAshaActor : StoryTownActor
    {
        private const int BloodDripInterval = 26;

        protected override int VanillaType => NPCID.Mechanic;

        protected override void PerformCue(StoryCue cue, int cueTimer)
        {
            // Her wounds stay visible for the whole scene, in place of the sign's description of them.
            if (!Main.dedServ && cueTimer % BloodDripInterval == 0)
            {
                Vector2 dripPoint = NPC.Center + new Vector2(NPC.direction * Main.rand.NextFloat(2f, 7f), Main.rand.NextFloat(-6f, 8f));
                Dust blood = Dust.NewDustPerfect(dripPoint, DustID.Blood, new Vector2(0f, 0.6f), 40, default, Main.rand.NextFloat(0.7f, 1f));
                blood.noGravity = false;
            }
        }
    }

    /// <summary>Araz, whose words about the demons, the seven gods and the crest are shown as magic while he speaks.</summary>
    public class StoryArazActor : StoryTownActor
    {
        public const int CatalystFormTicks = 60;
        public const int WardTravelTicks = 30;
        public const int CrestTravelTicks = 40;

        private const int FlameCount = 7;
        private const int FlameAppearInterval = 10;
        private const float VisionHeight = 30f;
        private const float OrbitWidth = 48f;
        private const float OrbitHeight = 18f;
        private const float OrbitSpeed = 0.025f;
        private const float WardRadius = 42f;
        private const float RiddleBeamSpeed = 9f;
        private const float RiddleBeamLength = 900f;
        private const float CrestDrawScale = 1.25f;

        // In the order he names them: Rage, Sorrow, Hunter, Destroyer, Machine, Triad, Golem.
        private static readonly int[] FlameDusts =
        {
            DustID.Torch,
            DustID.IceTorch,
            DustID.CursedTorch,
            DustID.RedTorch,
            DustID.Electric,
            DustID.PurpleTorch,
            DustID.OrangeTorch,
        };

        protected override int VanillaType => NPCID.Wizard;

        protected override bool IsCasting
        {
            get
            {
                StoryCue cue = Cue;
                return cue == StoryCue.Vision || cue == StoryCue.SevenFlames || cue == StoryCue.Catalyst || cue == StoryCue.Ward || cue == StoryCue.Riddle;
            }
        }

        private Vector2 VisionPoint
        {
            get
            {
                float middleX = (NPC.Center.X + Lead.Center.X) * 0.5f;
                return new Vector2(middleX, NPC.Top.Y - VisionHeight);
            }
        }

        private static float SmoothStep(float progress)
        {
            float clamped = MathHelper.Clamp(progress, 0f, 1f);
            return clamped * clamped * (3f - 2f * clamped);
        }

        private Vector2 FlamePosition(int flameIndex, int cueTimer, float radiusScale)
        {
            float angle = cueTimer * OrbitSpeed + flameIndex * MathHelper.TwoPi / FlameCount;
            Vector2 orbitOffset = new Vector2((float)Math.Cos(angle) * OrbitWidth, (float)Math.Sin(angle) * OrbitHeight);
            return VisionPoint + orbitOffset * radiusScale;
        }

        private void EmitFlame(int flameIndex, Vector2 position)
        {
            // The Rage's flame is already out once the player has slain it.
            bool extinguished = flameIndex == 0 && tsorcRevampWorld.BossDefeated(ModContent.NPCType<TheRage>());
            if (extinguished)
            {
                Dust smoke = Dust.NewDustPerfect(position, DustID.Smoke, new Vector2(0f, -0.4f), 150, new Color(70, 62, 60), 0.7f);
                smoke.noGravity = true;
                smoke.noLight = true;
                return;
            }

            Dust flame = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(2f, 2f), FlameDusts[flameIndex], new Vector2(0f, -0.25f), 80, default, Main.rand.NextFloat(0.9f, 1.3f));
            flame.noGravity = true;
        }

        private Vector2 CrestPosition(StoryCue cue, int cueTimer)
        {
            Vector2 heldPoint = Lead.Top - new Vector2(0f, 6f);
            Vector2 hoverPoint = VisionPoint;

            if (cue == StoryCue.CrestRises)
            {
                return Vector2.Lerp(heldPoint, hoverPoint, SmoothStep(cueTimer / (float)CrestTravelTicks));
            }

            if (cue == StoryCue.CrestReturns)
            {
                return Vector2.Lerp(hoverPoint, heldPoint, SmoothStep(cueTimer / (float)CrestTravelTicks));
            }

            return hoverPoint;
        }

        protected override void PerformCue(StoryCue cue, int cueTimer)
        {
            if (Main.dedServ)
            {
                return;
            }

            Vector2 visionPoint = VisionPoint;

            switch (cue)
            {
                case StoryCue.Vision:
                {
                    // The portal he describes opens over the two of them: a fire rim drawing into a dark center.
                    float growth = SmoothStep(cueTimer / 60f);
                    Lighting.AddLight(visionPoint, 0.8f * growth, 0.25f * growth, 0.25f * growth);

                    for (int i = 0; i < 3; i++)
                    {
                        float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                        Vector2 rimOffset = new Vector2((float)Math.Cos(angle) * OrbitWidth, (float)Math.Sin(angle) * OrbitHeight) * growth;
                        Dust rim = Dust.NewDustPerfect(visionPoint + rimOffset, DustID.Torch, -rimOffset * 0.03f, 90, default, Main.rand.NextFloat(0.8f, 1.3f));
                        rim.noGravity = true;
                    }

                    Dust core = Dust.NewDustPerfect(visionPoint + Main.rand.NextVector2Circular(OrbitWidth * 0.35f * growth, OrbitHeight * 0.35f * growth), DustID.Shadowflame, Vector2.Zero, 150, new Color(30, 8, 20), Main.rand.NextFloat(0.8f, 1.2f));
                    core.noGravity = true;
                    core.noLight = true;
                    break;
                }

                case StoryCue.SevenFlames:
                {
                    int visibleFlames = Math.Min(FlameCount, 1 + cueTimer / FlameAppearInterval);
                    Lighting.AddLight(visionPoint, 0.6f, 0.5f, 0.45f);
                    for (int i = 0; i < visibleFlames; i++)
                    {
                        EmitFlame(i, FlamePosition(i, cueTimer, 1f));
                    }
                    break;
                }

                case StoryCue.Catalyst:
                {
                    // The seven gods' flames spiral together into the catalyst.
                    float radiusScale = 1f - SmoothStep(cueTimer / (float)CatalystFormTicks);
                    if (cueTimer < CatalystFormTicks)
                    {
                        for (int i = 0; i < FlameCount; i++)
                        {
                            EmitFlame(i, FlamePosition(i, cueTimer * 4, radiusScale));
                        }
                    }

                    if (cueTimer == CatalystFormTicks)
                    {
                        SoundEngine.PlaySound(SoundID.Item29, visionPoint);
                        for (int i = 0; i < 45; i++)
                        {
                            Vector2 burstVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.5f, 5f);
                            Dust.NewDustPerfect(visionPoint, DustID.GoldFlame, burstVelocity, 70, default, Main.rand.NextFloat(0.8f, 1.4f)).noGravity = true;
                        }
                    }

                    if (cueTimer >= CatalystFormTicks)
                    {
                        Lighting.AddLight(visionPoint, 1f, 0.85f, 0.45f);
                        Dust.NewDustPerfect(visionPoint + Main.rand.NextVector2Circular(5f, 5f), DustID.GoldFlame, Vector2.Zero, 60, default, Main.rand.NextFloat(0.9f, 1.3f)).noGravity = true;
                    }
                    break;
                }

                case StoryCue.Ward:
                {
                    // The light crosses to Red and closes around him: the protection Araz is describing.
                    float travel = SmoothStep(cueTimer / (float)WardTravelTicks);
                    Vector2 lightPoint = Vector2.Lerp(visionPoint, Lead.Center, travel);
                    Lighting.AddLight(lightPoint, 1f, 0.85f, 0.45f);
                    Dust.NewDustPerfect(lightPoint, DustID.GoldFlame, Vector2.Zero, 60, default, 1.2f).noGravity = true;

                    if (cueTimer >= WardTravelTicks)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            Vector2 ringOffset = Main.rand.NextVector2CircularEdge(WardRadius, WardRadius);
                            Vector2 tangent = new Vector2(-ringOffset.Y, ringOffset.X) * 0.03f;
                            Dust.NewDustPerfect(Lead.Center + ringOffset, DustID.GoldFlame, tangent, 80, default, Main.rand.NextFloat(0.7f, 1.1f)).noGravity = true;
                        }
                    }
                    break;
                }

                case StoryCue.CrestRises:
                case StoryCue.CrestReturns:
                {
                    Vector2 crestPoint = CrestPosition(cue, cueTimer);
                    Lighting.AddLight(crestPoint, 0.8f, 0.35f, 0.15f);
                    Dust.NewDustPerfect(crestPoint + Main.rand.NextVector2Circular(9f, 9f), DustID.Torch, new Vector2(0f, -0.5f), 90, default, Main.rand.NextFloat(0.6f, 0.95f)).noGravity = true;
                    break;
                }

                case StoryCue.Riddle:
                {
                    // The hidden riddle answers as a line of light racing east, toward the Frozen Ocean.
                    Lighting.AddLight(visionPoint, 1f, 0.5f, 0.2f);
                    if (cueTimer == 0)
                    {
                        SoundEngine.PlaySound(SoundID.Item4 with { Pitch = 0.2f }, visionPoint);
                    }

                    float beamDistance = cueTimer * RiddleBeamSpeed;
                    if (beamDistance <= RiddleBeamLength)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            Vector2 beamPoint = visionPoint + new Vector2(beamDistance - i * 3f, Main.rand.NextFloat(-2f, 2f));
                            Dust.NewDustPerfect(beamPoint, DustID.IceTorch, new Vector2(1.2f, 0f), 70, default, Main.rand.NextFloat(0.8f, 1.2f)).noGravity = true;
                        }
                    }
                    break;
                }
            }
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            StoryCue cue = Cue;
            bool crestVisible = cue == StoryCue.CrestRises || cue == StoryCue.Riddle || (cue == StoryCue.CrestReturns && CueTimer < CrestTravelTicks);
            if (!crestVisible)
            {
                return;
            }

            int crestType = ModContent.ItemType<CrestOfFire>();
            Main.instance.LoadItem(crestType);
            Texture2D crestTexture = TextureAssets.Item[crestType].Value;
            Texture2D glowTexture = TextureAssets.Extra[98].Value;
            Vector2 drawPosition = CrestPosition(cue, CueTimer) - screenPos;

            // Alpha 0 adds the glow to the scene without restarting the sprite batch.
            float pulse = 0.85f + 0.15f * (float)Math.Sin(CueTimer * 0.12f);
            Color glowColor = new Color(255, 120, 45, 0) * pulse;
            spriteBatch.Draw(glowTexture, drawPosition, null, glowColor, CueTimer * 0.03f, glowTexture.Size() * 0.5f, 0.55f, SpriteEffects.None, 0f);
            spriteBatch.Draw(crestTexture, drawPosition, null, Color.White, 0f, crestTexture.Size() * 0.5f, CrestDrawScale, SpriteEffects.None, 0f);
        }
    }

    /// <summary>The small blue wisp Araz speaks through. It gathers from dust, hovers, then dives down the gated shaft.</summary>
    public class StoryWispActor : StoryActor
    {
        public const int DescendTicks = 120;

        private const int FormTicks = 60;
        private const int SlideTicks = 40;
        private const float FallSpeed = 3.2f;
        private const int FadeStartTick = 85;

        public override string Texture => "Terraria/Images/Extra_98";

        private float Brightness
        {
            get
            {
                if (Cue != StoryCue.Descend)
                {
                    return MathHelper.Clamp(CueTimer / (float)FormTicks, 0f, 1f);
                }

                float fadeProgress = (CueTimer - FadeStartTick) / (float)(DescendTicks - FadeStartTick);
                return 1f - MathHelper.Clamp(fadeProgress, 0f, 1f);
            }
        }

        protected override void PerformCue(StoryCue cue, int cueTimer)
        {
            float brightness = Brightness;
            Lighting.AddLight(NPC.Center, 0.15f * brightness, 0.45f * brightness, 1f * brightness);

            if (cue == StoryCue.Descend)
            {
                if (NPC.localAI[0] == 0f)
                {
                    NPC.localAI[0] = NPC.Center.X;
                }

                float slideProgress = MathHelper.Clamp(cueTimer / (float)SlideTicks, 0f, 1f);
                float shaftCenterX = (StoryCutscenes.WispShaftTileX + 0.5f) * 16f;
                NPC.Center = new Vector2(MathHelper.Lerp(NPC.localAI[0], shaftCenterX, slideProgress), NPC.Center.Y);

                if (slideProgress >= 1f)
                {
                    NPC.position.Y += FallSpeed;
                }

                if (cueTimer >= DescendTicks && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Remove();
                }
            }
            else
            {
                NPC.position.Y += (float)Math.Sin(cueTimer * 0.07f) * 0.25f;
            }

            if (Main.dedServ)
            {
                return;
            }

            if (cueTimer == 0 && cue == StoryCue.None)
            {
                SoundEngine.PlaySound(SoundID.Item8 with { Pitch = 0.5f, Volume = 0.7f }, NPC.Center);
            }

            // While forming the motes are drawn inward; afterward they trail gently behind it.
            bool forming = cue == StoryCue.None && cueTimer < FormTicks;
            if (forming)
            {
                Vector2 gatherOffset = Main.rand.NextVector2CircularEdge(32f, 32f);
                Dust.NewDustPerfect(NPC.Center + gatherOffset, DustID.BlueTorch, -gatherOffset * 0.07f, 80, default, Main.rand.NextFloat(0.8f, 1.2f)).noGravity = true;
            }
            else if (cueTimer % 3 == 0 && brightness > 0.05f)
            {
                Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(5f, 5f), DustID.BlueTorch, new Vector2(0f, -0.3f), 100, default, Main.rand.NextFloat(0.6f, 0.9f)).noGravity = true;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D glowTexture = TextureAssets.Npc[Type].Value;
            Vector2 origin = glowTexture.Size() * 0.5f;
            Vector2 drawPosition = NPC.Center - screenPos;
            float brightness = Brightness;
            float spin = Main.GlobalTimeWrappedHourly * 2.2f;

            // Alpha 0 adds these colors to the scene, giving a glow without restarting the sprite batch.
            Color outerColor = new Color(60, 130, 255, 0) * brightness;
            Color coreColor = new Color(205, 235, 255, 0) * brightness;
            spriteBatch.Draw(glowTexture, drawPosition, null, outerColor, spin, origin, 0.45f, SpriteEffects.None, 0f);
            spriteBatch.Draw(glowTexture, drawPosition, null, outerColor, -spin + MathHelper.PiOver2, origin, 0.45f, SpriteEffects.None, 0f);
            spriteBatch.Draw(glowTexture, drawPosition, null, coreColor, spin * 0.6f, origin, 0.2f, SpriteEffects.None, 0f);
            return false;
        }
    }

    /// <summary>
    /// Lets Bodhi and Aaron act in their own scenes. While held, vanilla town AI stops and ai[0-3] mean cue, cue timer, facing
    /// and walk target, so every machine plays the same movement from the same synced values.
    /// </summary>
    public class StoryHeldNPC : GlobalNPC
    {
        private const float WalkSpeed = 1.2f;
        private const float LimpFastSpeed = 1.45f;
        private const float LimpDragSpeed = 0.2f;
        private const int LimpCycleTicks = 36;
        private const int LimpFastTicks = 14;
        private const int WalkFramesStart = 2;
        private const int WalkFrameCount = 14;

        private bool wasHeld;

        public override bool InstancePerEntity => true;

        public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
        {
            return entity.type == NPCID.ArmsDealer || entity.type == NPCID.Guide;
        }

        public override bool PreAI(NPC npc)
        {
            if (!StoryCutscenes.HoldsNpc(npc))
            {
                if (wasHeld)
                {
                    // Hand the NPC back with a clean slate, because vanilla reads these same slots as its own state.
                    wasHeld = false;
                    npc.dontTakeDamage = false;
                    Array.Clear(npc.ai);
                }

                return true;
            }

            wasHeld = true;
            npc.dontTakeDamage = true;
            npc.velocity.X = 0f;

            int facing = (int)npc.ai[2];
            if (facing == 1 || facing == -1)
            {
                npc.direction = facing;
                npc.spriteDirection = facing;
            }

            int cueTimer = (int)npc.ai[1];
            npc.ai[1]++;

            StoryCue cue = (StoryCue)(int)npc.ai[0];
            if (cue == StoryCue.Walking || cue == StoryCue.Limping)
            {
                float stepSpeed = WalkSpeed;
                if (cue == StoryCue.Limping)
                {
                    // A quick step on the good leg, then the injured one dragged after it.
                    stepSpeed = LimpDragSpeed;
                    if (cueTimer % LimpCycleTicks < LimpFastTicks)
                    {
                        stepSpeed = LimpFastSpeed;
                    }
                }

                float remaining = npc.ai[3] - npc.Center.X;
                float step = MathHelper.Clamp(remaining, -stepSpeed, stepSpeed);
                npc.position.X += step;
            }

            return false;
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            if (!StoryCutscenes.HoldsNpc(npc))
            {
                return;
            }

            int firstAttackFrame = Main.npcFrameCount[npc.type] - NPCID.Sets.AttackFrameCount[npc.type];
            int cueTimer = (int)npc.ai[1];
            StoryCue cue = (StoryCue)(int)npc.ai[0];
            int frameIndex = 0;

            switch (cue)
            {
                case StoryCue.Seated:
                    frameIndex = firstAttackFrame - 3;
                    break;

                case StoryCue.Reaching:
                    frameIndex = firstAttackFrame;
                    break;

                case StoryCue.Walking:
                    frameIndex = WalkFramesStart + cueTimer / 5 % WalkFrameCount;
                    break;

                case StoryCue.Limping:
                    frameIndex = WalkFramesStart + cueTimer / 9 % WalkFrameCount;
                    break;

                default:
                    if (CutsceneSystem.IsNpcSpeaking(npc.whoAmI) && cueTimer % 16 < 8)
                    {
                        frameIndex = firstAttackFrame - 1;
                    }
                    break;
            }

            npc.frame.Y = frameIndex * frameHeight;
        }

        public override bool? CanChat(NPC npc)
        {
            if (StoryCutscenes.HoldsNpc(npc))
            {
                return false;
            }

            return null;
        }
    }

    /// <summary>An invisible stage hand for one scene: the loft glint, the protection stone, and the memory at the well.</summary>
    public class StoryPropActor : StoryActor
    {
        public const int StoneTravelTicks = 40;
        public const int MemoryCryTick = 450;
        public const int MemoryStrikeTicks = 70;
        public const int TimePassesTicks = 170;
        public const int IdolStrikeTicks = 100;

        private const int IdolImpactTick = 34;
        private const int TimeFadeTicks = 50;
        private const float WeaponPivotHeight = 2f;

        private const float StoneHoverHeight = 34f;
        private const int SpeckGrowTicks = 90;

        // The memory's timeline, in ticks from when the ash closes in.
        private const int MemoryFadeTicks = 30;
        private const int AssassinEnterTick = 90;
        private const int KnifeTick = 150;
        private const int KnifeFlashTicks = 12;
        private const int ShoveTick = 205;
        private const int JoltTick = 275;
        private const int JoltTicks = 20;
        private const int FirstKnightTick = 295;
        private const int KnightInterval = 50;
        private const int StrikeHoldTicks = 10;

        private const float FigureScale = 3f;
        private const int BloodDrops = 18;
        private const int FogBanks = 14;

        private static readonly Color SilhouetteColor = new Color(16, 14, 17);
        private static readonly Color AshColor = new Color(62, 58, 56);

        public override string Texture => "Terraria/Images/Extra_98";

        private static float Ease(float progress)
        {
            float clamped = MathHelper.Clamp(progress, 0f, 1f);
            return clamped * clamped * (3f - 2f * clamped);
        }

        private Vector2 StonePosition(StoryCue cue, int cueTimer)
        {
            float travel = Ease(cueTimer / (float)StoneTravelTicks);
            if (cue == StoryCue.IdolOut)
            {
                return Vector2.Lerp(Lead.Center, NPC.Center, travel);
            }

            if (cue == StoryCue.IdolBack)
            {
                return Vector2.Lerp(NPC.Center, Lead.Center, travel);
            }

            if (cue == StoryCue.IdolResting || cue == StoryCue.IdolStrike)
            {
                return NPC.Center;
            }

            Vector2 hoverPoint = NPC.Center - new Vector2(0f, StoneHoverHeight);
            if (cue == StoryCue.StoneRises)
            {
                return Vector2.Lerp(NPC.Center, hoverPoint, Ease(cueTimer / (float)StoneTravelTicks));
            }

            return Vector2.Lerp(hoverPoint, Lead.Center, Ease(cueTimer / (float)StoneTravelTicks));
        }

        protected override void PerformCue(StoryCue cue, int cueTimer)
        {
            if (Main.dedServ)
            {
                return;
            }

            switch (cue)
            {
                case StoryCue.LoftGlint:
                    Lighting.AddLight(NPC.Center, 0.9f, 0.75f, 0.4f);
                    if (cueTimer % 4 == 0)
                    {
                        Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(16f, 14f), DustID.GoldFlame, new Vector2(0f, -0.35f), 70, default, Main.rand.NextFloat(0.6f, 1f)).noGravity = true;
                    }
                    break;

                case StoryCue.SkullGlow:
                case StoryCue.StoneRises:
                case StoryCue.StoneToRed:
                {
                    Vector2 glowPoint = NPC.Center;
                    if (cue != StoryCue.SkullGlow)
                    {
                        glowPoint = StonePosition(cue, cueTimer);
                    }

                    if (cue == StoryCue.StoneToRed && cueTimer >= StoneTravelTicks)
                    {
                        break;
                    }

                    if (cue == StoryCue.SkullGlow && cueTimer == 0)
                    {
                        SoundEngine.PlaySound(SoundID.NPCDeath52 with { Volume = 0.35f, Pitch = 0.3f }, NPC.Center);
                    }

                    Lighting.AddLight(glowPoint, 0.2f, 0.9f, 0.3f);
                    Dust.NewDustPerfect(glowPoint + Main.rand.NextVector2Circular(9f, 9f), DustID.CursedTorch, new Vector2(0f, -0.5f), 80, default, Main.rand.NextFloat(0.7f, 1.05f)).noGravity = true;
                    break;
                }

                case StoryCue.StonePulse:
                    // A single dark pulse as Red turns his back. It is meant to be easy to miss.
                    if (cueTimer == 25)
                    {
                        SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.3f, Pitch = -0.5f }, Lead.Center);
                        for (int i = 0; i < 26; i++)
                        {
                            Vector2 ringVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1f, 2.4f);
                            Dust.NewDustPerfect(Lead.Center, DustID.ShadowbeamStaff, ringVelocity, 110, default, Main.rand.NextFloat(0.6f, 0.95f)).noGravity = true;
                        }
                    }
                    break;

                case StoryCue.Omen:
                    // Pale stars wheel over Elijah's words about what Attraidies speaks with.
                    if (cueTimer < 150)
                    {
                        float omenAngle = cueTimer * 0.09f;
                        Vector2 starPoint = Lead.Center + new Vector2((float)Math.Cos(omenAngle) * 70f, -60f + (float)Math.Sin(omenAngle) * 18f);
                        Dust.NewDustPerfect(starPoint, DustID.PurpleTorch, Vector2.Zero, 70, default, 0.9f).noGravity = true;
                    }
                    break;

                case StoryCue.BeastStirs:
                    if (cueTimer == 30)
                    {
                        SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.5f, Pitch = -0.6f }, NPC.Center);
                        Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(NPC.Center, Vector2.UnitY, 4f, 5f, 30, 1200f, "StoryBeast"));
                    }
                    break;

                case StoryCue.IdolOut:
                case StoryCue.IdolBack:
                case StoryCue.IdolResting:
                {
                    Vector2 idolPoint = StonePosition(cue, cueTimer);
                    bool idolGone = cue == StoryCue.IdolBack && cueTimer >= StoneTravelTicks;
                    if (!idolGone)
                    {
                        Lighting.AddLight(idolPoint, 0.25f, 0.8f, 0.35f);
                        Dust.NewDustPerfect(idolPoint + Main.rand.NextVector2Circular(8f, 8f), DustID.CursedTorch, new Vector2(0f, -0.35f), 90, default, Main.rand.NextFloat(0.6f, 0.9f)).noGravity = true;
                    }
                    break;
                }

                case StoryCue.IdolStrike:
                {
                    // Raise, then drive the blade down. The stone answers with lightning and hurls Red away.
                    float strikeAngle = StrikeAngle(cueTimer);
                    if (cueTimer <= IdolImpactTick + 8)
                    {
                        // Composite arms measure from straight down, so subtract a quarter turn from the blade's world angle.
                        float worldAngle = strikeAngle;
                        if (Lead.direction == -1)
                        {
                            worldAngle = MathHelper.Pi - strikeAngle;
                        }

                        float armRotation = worldAngle - MathHelper.PiOver2;
                        Lead.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
                    }

                    if (cueTimer == IdolImpactTick)
                    {
                        SoundEngine.PlaySound(new SoundStyle("Terraria/Sounds/Thunder_0") with { Volume = 1.1f }, NPC.Center);
                        SoundEngine.PlaySound(SoundID.Item94, NPC.Center);
                        Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(NPC.Center, Main.rand.NextVector2Unit(), 10f, 6f, 24, 1400f, "StoryIdolShock"));

                        for (int i = 0; i < 70; i++)
                        {
                            Vector2 shockVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 9f);
                            Dust.NewDustPerfect(NPC.Center, DustID.Electric, shockVelocity, 70, default, Main.rand.NextFloat(0.6f, 1.1f)).noGravity = true;
                        }

                        if (Lead.whoAmI == Main.myPlayer)
                        {
                            Lead.GetModPlayer<CutscenePlayer>().BeginThrow(-Lead.direction, 5f * 16f, 26, -5.5f);
                        }
                    }

                    if (cueTimer > IdolImpactTick && cueTimer % 5 == 0)
                    {
                        Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(14f, 14f), DustID.Electric, Main.rand.NextVector2Circular(1.5f, 1.5f), 90, default, 0.8f).noGravity = true;
                    }

                    Lighting.AddLight(NPC.Center, 0.3f, 0.8f, 0.5f);
                    break;
                }

                case StoryCue.WellSpeck:
                {
                    float growth = Ease(cueTimer / (float)SpeckGrowTicks);
                    Lighting.AddLight(NPC.Center, 0.6f * growth, 0.75f * growth, 1f * growth);
                    if (cueTimer % 5 == 0)
                    {
                        Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(10f, 10f), DustID.AncientLight, Vector2.Zero, 70, default, 0.6f + 0.4f * growth).noGravity = true;
                    }
                    break;
                }

                case StoryCue.Memory:
                    if (cueTimer == 0)
                    {
                        SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.6f, Pitch = -0.7f });
                    }
                    else if (cueTimer == KnifeTick)
                    {
                        SoundEngine.PlaySound(SoundID.Item1 with { Pitch = 0.3f });
                        SoundEngine.PlaySound(SoundID.NPCHit1);
                    }
                    else if (cueTimer == JoltTick)
                    {
                        SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.8f, Pitch = -0.5f });
                    }

                    for (int i = 0; i < 3; i++)
                    {
                        if (cueTimer == FirstKnightTick + i * KnightInterval)
                        {
                            SoundEngine.PlaySound(SoundID.NPCHit4 with { Volume = 0.5f, Pitch = -0.6f });
                        }
                    }
                    break;

                case StoryCue.MemoryStrike:
                    if (cueTimer == 0)
                    {
                        SoundEngine.PlaySound(SoundID.Item71 with { Pitch = -0.4f });
                        SoundEngine.PlaySound(SoundID.NPCHit4 with { Pitch = -0.3f });
                        Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(Lead.Center, Main.rand.NextVector2Unit(), 12f, 6f, 24, 1200f, "StoryMemory"));
                    }
                    break;
            }
        }

        // Blade angle in radians from straight ahead: raised overhead, then swung down across the wind-up's last ticks.
        private static float StrikeAngle(int cueTimer)
        {
            float raise = Ease(cueTimer / 22f);
            float swing = Ease((cueTimer - 26f) / (IdolImpactTick - 26f));
            float raisedAngle = MathHelper.Lerp(-0.4f, -2.1f, raise);
            return MathHelper.Lerp(raisedAngle, 0.45f, swing);
        }

        private void DrawStrikeWeapon(SpriteBatch spriteBatch, Vector2 screenPos)
        {
            // Red's strongest hotbar weapon, or a plain sword when the hotbar holds none.
            Player lead = Lead;
            int weaponType = ItemID.CopperBroadsword;
            int bestDamage = 0;
            for (int i = 0; i < 10; i++)
            {
                Item item = lead.inventory[i];
                if (!item.IsAir && item.damage > bestDamage && item.DamageType.CountsAsClass(DamageClass.Melee))
                {
                    bestDamage = item.damage;
                    weaponType = item.type;
                }
            }

            Main.instance.LoadItem(weaponType);
            Texture2D weapon = TextureAssets.Item[weaponType].Value;
            Vector2 pivot = lead.MountedCenter + new Vector2(lead.direction * 4f, -WeaponPivotHeight) - screenPos;
            float bladeAngle = StrikeAngle(CueTimer);
            Vector2 origin = new Vector2(0f, weapon.Height);
            SpriteEffects flip = SpriteEffects.None;
            float rotation = bladeAngle + MathHelper.PiOver4;

            // Weapon sprites point up and to the right, so facing left mirrors both the art and the angle.
            if (lead.direction == -1)
            {
                origin.X = weapon.Width;
                flip = SpriteEffects.FlipHorizontally;
                rotation = -bladeAngle - MathHelper.PiOver4;
            }

            spriteBatch.Draw(weapon, pivot, null, Lighting.GetColor(lead.Center.ToTileCoordinates()), rotation, origin, 1f, flip, 0f);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            StoryCue cue = Cue;
            int cueTimer = CueTimer;
            Texture2D glowTexture = TextureAssets.Npc[Type].Value;
            Vector2 glowOrigin = glowTexture.Size() * 0.5f;
            float spin = Main.GlobalTimeWrappedHourly * 1.6f;

            if (cue == StoryCue.WellSpeck)
            {
                float growth = Ease(cueTimer / (float)SpeckGrowTicks);
                Vector2 speckPosition = NPC.Center - screenPos;
                spriteBatch.Draw(glowTexture, speckPosition, null, new Color(120, 170, 255, 0) * growth, spin, glowOrigin, 0.08f + 0.4f * growth, SpriteEffects.None, 0f);
                spriteBatch.Draw(glowTexture, speckPosition, null, new Color(235, 245, 255, 0) * growth, -spin, glowOrigin, 0.04f + 0.14f * growth, SpriteEffects.None, 0f);
                return false;
            }

            if (cue == StoryCue.IdolStrike && cueTimer <= IdolImpactTick + 8)
            {
                DrawStrikeWeapon(spriteBatch, screenPos);
            }

            bool idolVisible = cue == StoryCue.IdolOut || cue == StoryCue.IdolResting || cue == StoryCue.IdolStrike || (cue == StoryCue.IdolBack && cueTimer < StoneTravelTicks);
            bool stoneVisible = idolVisible || cue == StoryCue.StoneRises || (cue == StoryCue.StoneToRed && cueTimer < StoneTravelTicks);
            if (!stoneVisible)
            {
                return false;
            }

            int stoneType = ModContent.ItemType<AaronsProtectionStone>();
            Main.instance.LoadItem(stoneType);
            Texture2D stoneTexture = TextureAssets.Item[stoneType].Value;
            Vector2 stonePosition = StonePosition(cue, cueTimer) - screenPos;

            // Alpha 0 adds the glow to the scene without restarting the sprite batch.
            spriteBatch.Draw(glowTexture, stonePosition, null, new Color(70, 255, 110, 0) * 0.8f, spin, glowOrigin, 0.42f, SpriteEffects.None, 0f);
            spriteBatch.Draw(stoneTexture, stonePosition, null, Color.White, 0f, stoneTexture.Size() * 0.5f, 1.1f, SpriteEffects.None, 0f);
            return false;
        }

        /// <summary>Interface layer for scene participants. Finds the running memory, if any, and draws it over the world.</summary>
        public static bool DrawMemoryLayer()
        {
            int propType = ModContent.NPCType<StoryPropActor>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.type == propType)
                {
                    ((StoryPropActor)npc.ModNPC).DrawMemory();
                    break;
                }
            }

            return true;
        }

        private static void DrawFigure(Texture2D sheet, int frameCount, float centerX, float feetY, bool faceRight, float alpha)
        {
            int frameHeight = sheet.Height / frameCount;
            Rectangle frame = new Rectangle(0, 0, sheet.Width, frameHeight);
            Vector2 origin = new Vector2(sheet.Width * 0.5f, frameHeight);

            SpriteEffects flip = SpriteEffects.None;
            if (faceRight)
            {
                flip = SpriteEffects.FlipHorizontally;
            }

            Main.spriteBatch.Draw(sheet, new Vector2(centerX, feetY), frame, SilhouetteColor * alpha, 0f, origin, FigureScale, flip, 0f);
        }

        private static Texture2D TownSheet(int npcType)
        {
            Main.instance.LoadNPC(npcType);
            return TextureAssets.Npc[npcType].Value;
        }

        private void DrawMemory()
        {
            StoryCue cue = Cue;
            int timer = CueTimer;
            if (cue != StoryCue.Memory && cue != StoryCue.MemoryStrike && cue != StoryCue.TimePasses)
            {
                return;
            }

            SpriteBatch spriteBatch = Main.spriteBatch;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle pixelSource = new Rectangle(0, 0, 1, 1);
            float uiWidth = Main.screenWidth / Main.UIScale;
            float uiHeight = Main.screenHeight / Main.UIScale;
            Rectangle fullScreen = new Rectangle(0, 0, (int)uiWidth + 2, (int)uiHeight + 2);

            // A night's reading: down to black and back up to morning.
            if (cue == StoryCue.TimePasses)
            {
                float fadeDown = timer / (float)TimeFadeTicks;
                float fadeUp = (TimePassesTicks - timer) / (float)TimeFadeTicks;
                float nightAlpha = MathHelper.Clamp(Math.Min(fadeDown, fadeUp), 0f, 1f);
                spriteBatch.Draw(pixel, fullScreen, pixelSource, Color.Black * nightAlpha);
                return;
            }

            // The knight's blow: solid white, then the world shows through again.
            if (cue == StoryCue.MemoryStrike)
            {
                float whiteAlpha = 1f - MathHelper.Clamp((timer - StrikeHoldTicks) / (float)(MemoryStrikeTicks - StrikeHoldTicks), 0f, 1f);
                spriteBatch.Draw(pixel, fullScreen, pixelSource, Color.White * whiteAlpha);
                return;
            }

            float alpha = MathHelper.Clamp(timer / (float)MemoryFadeTicks, 0f, 1f);
            spriteBatch.Draw(pixel, fullScreen, pixelSource, AshColor * alpha);

            float shakeX = 0f;
            if (timer >= JoltTick && timer < JoltTick + JoltTicks)
            {
                float jolt = 1f - (timer - JoltTick) / (float)JoltTicks;
                shakeX = (float)Math.Sin(timer * 2.3f) * 12f * jolt;
            }

            float centerX = uiWidth * 0.5f + shakeX;
            float floorY = uiHeight * 0.68f;
            Rectangle floorBand = new Rectangle(0, (int)floorY, (int)uiWidth + 2, (int)(uiHeight - floorY) + 2);
            spriteBatch.Draw(pixel, floorBand, pixelSource, new Color(32, 29, 29) * alpha);

            // The family stands to the right, facing the danger coming from the left.
            float shove = Ease((timer - ShoveTick) / 18f);
            float fatherX = centerX + 40f;
            if (timer >= KnifeTick)
            {
                fatherX += 14f * Ease((timer - KnifeTick) / 10f);
            }

            float childX = MathHelper.Lerp(centerX + 110f, centerX + 300f, shove);
            float motherX = MathHelper.Lerp(centerX + 165f, centerX + 375f, shove);
            DrawFigure(TownSheet(NPCID.Nurse), Main.npcFrameCount[NPCID.Nurse], motherX, floorY, false, alpha);
            DrawFigure(TownSheet(NPCID.Guide), Main.npcFrameCount[NPCID.Guide], childX, floorY, false, alpha);
            DrawFigure(TownSheet(NPCID.ArmsDealer), Main.npcFrameCount[NPCID.ArmsDealer], fatherX, floorY, false, alpha);

            // The assassin glides in, strikes, and slips back into the fog with only its eyes showing.
            if (timer >= AssassinEnterTick)
            {
                float enter = Ease((timer - AssassinEnterTick) / (float)(KnifeTick - AssassinEnterTick));
                float retreat = Ease((timer - KnifeTick - 30) / 50f);
                float assassinX = MathHelper.Lerp(centerX - 560f, centerX - 35f, enter) - retreat * 190f;
                Texture2D assassinSheet = ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(Enemies.Archer.Assassin)), ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                DrawFigure(assassinSheet, 20, assassinX, floorY, true, alpha);

                float eyeGlow = 0.75f + 0.25f * (float)Math.Sin(timer * 0.2f);
                Color eyeColor = new Color(255, 215, 50) * (eyeGlow * alpha);
                Rectangle nearEye = new Rectangle((int)assassinX + 10, (int)floorY - 132, 7, 4);
                Rectangle farEye = new Rectangle((int)assassinX + 26, (int)floorY - 132, 7, 4);
                spriteBatch.Draw(pixel, nearEye, pixelSource, eyeColor);
                spriteBatch.Draw(pixel, farEye, pixelSource, eyeColor);

                if (timer >= KnifeTick && timer < KnifeTick + KnifeFlashTicks)
                {
                    Vector2 slashStart = new Vector2(fatherX - 80f, floorY - 112f);
                    spriteBatch.Draw(pixel, slashStart, pixelSource, Color.White * alpha, 0.5f, Vector2.Zero, new Vector2(110f, 3f), SpriteEffects.None, 0f);
                }
            }

            // Fixed arcs rather than random rolls, so the spray holds still from frame to frame.
            if (timer >= KnifeTick && timer < KnifeTick + 55)
            {
                int age = timer - KnifeTick;
                for (int i = 0; i < BloodDrops; i++)
                {
                    float spread = i / (float)BloodDrops;
                    float speedX = -0.6f + spread * 3.4f;
                    float speedY = -3.6f + (i % 4) * 0.6f;
                    Vector2 dropPoint = new Vector2(fatherX - 10f + speedX * age, floorY - 100f + speedY * age + 0.11f * age * age);
                    Rectangle drop = new Rectangle((int)dropPoint.X, (int)dropPoint.Y, 4, 4);
                    spriteBatch.Draw(pixel, drop, pixelSource, new Color(135, 20, 22) * (alpha * (1f - age / 55f)));
                }
            }

            Texture2D knightSheet = ModContent.Request<Texture2D>(KnightSheetPath, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            for (int i = 0; i < 3; i++)
            {
                int knightStart = FirstKnightTick + i * KnightInterval;
                if (timer < knightStart)
                {
                    continue;
                }

                float emerge = Ease((timer - knightStart) / 40f);
                float knightX = centerX - 170f - i * 125f - (1f - emerge) * 90f;
                DrawFigure(knightSheet, 16, knightX, floorY, true, alpha * emerge);
            }

            // Ash banks roll across last, so the figures seem to stand inside the fog.
            Texture2D fogTexture = TextureAssets.Extra[98].Value;
            Vector2 fogOrigin = fogTexture.Size() * 0.5f;
            float fogSpan = uiWidth + 500f;
            for (int i = 0; i < FogBanks; i++)
            {
                float travel = timer * (0.25f + i * 0.03f) + i * 173f;
                float fogX = travel % fogSpan - 250f;
                float fogY = floorY - 30f - i * 11 % 150;
                spriteBatch.Draw(fogTexture, new Vector2(fogX, fogY), null, new Color(150, 144, 138, 0) * (0.2f * alpha), i * 0.7f, fogOrigin, 2.4f + i % 3 * 0.5f, SpriteEffects.None, 0f);
            }
        }

        // No class owns this older sheet any more, so its path cannot come from a type.
        private const string KnightSheetPath = "tsorcRevamp/NPCs/UnusedSprites/BlackKnight";
    }


    /// <summary>Elijah, still hurt from his captivity. He bleeds and poses exactly as Asha does.</summary>
    public class StoryElijahActor : StoryAshaActor
    {
        protected override int VanillaType => NPCID.GoblinTinkerer;
    }

    /// <summary>
    /// Miakoda as a speaking character. If the lead's pet is out she takes its place and form, so nothing pops in; otherwise
    /// her spectral form gathers beside Red and fades when the scene ends.
    /// </summary>
    public class StoryMiakodaActor : StoryActor
    {
        public const int DepartTicks = 24;

        private const int FadeInTicks = 40;
        private const int FrameTicks = 5;
        private const float HoverSide = 46f;
        private const float HoverHeight = 38f;
        private const float FollowRate = 0.08f;

        private bool formChosen;
        private bool hadPet;
        private string texturePath = "";
        private int frameCount = 8;
        private float drawScale = 0.9f;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(Content.Projectiles.Pets.Miakoda)) + "Spectral";

        /// <summary>The lead's active Miakoda pet, or null.</summary>
        public static Projectile FindPet(int ownerIndex)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == ownerIndex && StoryCutscenes.IsMiakodaPet(projectile.type))
                {
                    return projectile;
                }
            }

            return null;
        }

        private void ChooseForm()
        {
            formChosen = true;
            texturePath = Texture;
            Projectile pet = FindPet((int)NPC.ai[3]);
            hadPet = pet != null;

            if (pet != null)
            {
                texturePath = pet.ModProjectile.Texture;
                frameCount = Main.projFrames[pet.type];
                drawScale = pet.scale;
            }
        }

        private float Opacity
        {
            get
            {
                if (Cue == StoryCue.Depart)
                {
                    return 1f - MathHelper.Clamp(CueTimer / (float)DepartTicks, 0f, 1f);
                }

                if (hadPet)
                {
                    return 1f;
                }

                return MathHelper.Clamp(NPC.localAI[0] / FadeInTicks, 0f, 1f);
            }
        }

        protected override void PerformCue(StoryCue cue, int cueTimer)
        {
            if (!formChosen)
            {
                ChooseForm();
            }

            NPC.localAI[0]++;
            float side = -NPC.ai[2];
            float bob = (float)Math.Sin(NPC.localAI[0] * 0.05f) * 4f;
            Vector2 hoverPoint = Lead.Center + new Vector2(side * HoverSide, -HoverHeight + bob);
            NPC.Center = Vector2.Lerp(NPC.Center, hoverPoint, FollowRate);

            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            if (cue == StoryCue.Depart && cueTimer >= DepartTicks && authority)
            {
                Remove();
                return;
            }

            if (Main.dedServ)
            {
                return;
            }

            Lighting.AddLight(NPC.Center, 0.45f, 0.5f, 0.6f);

            bool arriving = !hadPet && NPC.localAI[0] < FadeInTicks;
            bool departing = !hadPet && cue == StoryCue.Depart;
            if (arriving || departing)
            {
                Vector2 moteVelocity = Main.rand.NextVector2Circular(1.4f, 1.4f);
                Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(14f, 14f), 57, moteVelocity, 60, default, Main.rand.NextFloat(0.7f, 1.1f)).noGravity = true;
            }

            if (cue == StoryCue.FireWarning)
            {
                // She shows what to watch for: the bird's body glowing with fire.
                Lighting.AddLight(NPC.Center, 1f, 0.45f, 0.15f);
                for (int i = 0; i < 2; i++)
                {
                    Vector2 flameOffset = Main.rand.NextVector2CircularEdge(18f, 18f);
                    Dust.NewDustPerfect(NPC.Center + flameOffset, DustID.Torch, new Vector2(0f, -1.1f), 80, default, Main.rand.NextFloat(0.9f, 1.3f)).noGravity = true;
                }
            }
            else if (cue == StoryCue.VioletGlow)
            {
                // The ashen fog and violet glow from the crest's puzzle.
                Lighting.AddLight(NPC.Center, 0.55f, 0.25f, 0.8f);
                Vector2 glowOffset = Main.rand.NextVector2Circular(24f, 16f);
                Dust.NewDustPerfect(NPC.Center + glowOffset, DustID.PurpleTorch, new Vector2(0f, -0.4f), 90, default, Main.rand.NextFloat(0.7f, 1f)).noGravity = true;
                if (cueTimer % 4 == 0)
                {
                    Dust ash = Dust.NewDustPerfect(NPC.Center + glowOffset * 1.5f, DustID.Smoke, new Vector2(0f, -0.2f), 170, new Color(92, 86, 94), Main.rand.NextFloat(0.6f, 0.85f));
                    ash.noGravity = true;
                    ash.noLight = true;
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!formChosen)
            {
                return false;
            }

            Texture2D sheet = ModContent.Request<Texture2D>(texturePath, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            int frameHeight = sheet.Height / frameCount;
            int frameIndex = (int)NPC.localAI[0] / FrameTicks % frameCount;
            Rectangle frame = new Rectangle(0, frameIndex * frameHeight, sheet.Width, frameHeight);

            SpriteEffects flip = SpriteEffects.None;
            if (NPC.spriteDirection == 1)
            {
                flip = SpriteEffects.FlipHorizontally;
            }

            spriteBatch.Draw(sheet, NPC.Center - screenPos, frame, Color.White * Opacity, 0f, frame.Size() * 0.5f, drawScale, flip, 0f);
            return false;
        }
    }

    /// <summary>Hides the lead's own Miakoda while the scene's Miakoda stands in for her.</summary>
    public class StoryMiakodaPetHider : GlobalProjectile
    {
        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        {
            return StoryCutscenes.IsMiakodaPet(entity.type);
        }

        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            return !StoryCutscenes.ReplacesMiakodaOf(projectile.owner);
        }
    }

    /// <summary>Her pet form also keeps a chat NPC at its side, which must vanish along with it.</summary>
    public class StoryMiakodaChatHider : GlobalNPC
    {
        public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
        {
            return entity.type == ModContent.NPCType<Friendly.MiakodaNPC>();
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            int owner = ((Friendly.MiakodaNPC)npc.ModNPC).owner;
            return owner < 0 || !StoryCutscenes.ReplacesMiakodaOf(owner);
        }
    }

    /// <summary>Debug replay for playtesting: /storyscene plus a scene name, or credits.</summary>
    public class StorySceneCommand : ModCommand
    {
        public override CommandType Type => CommandType.World;
        public override string Command => "storyscene";
        public override string Usage => "/storyscene gate|reunion|wisp|counsel|homecoming|well|idol|elijah|firetemple|shadow|desert|rage|credits";
        public override string Description => "Debug: moves you to a story cutscene and plays it, or plays the credits.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length != 1)
            {
                caller.Reply(Usage, Color.OrangeRed);
                return;
            }

            StoryScene scene = StoryScene.None;
            Vector2 legacyTile = Vector2.Zero;

            switch (args[0].ToLowerInvariant())
            {
                case "credits":
                    CreditsRoll.StartForEveryone();
                    return;

                case "gate":
                    scene = StoryScene.ThroughGate;
                    legacyTile = new Vector2(322f, 1366f);
                    break;

                case "reunion":
                    scene = StoryScene.Reunion;
                    legacyTile = new Vector2(300f, 1366f);
                    break;

                case "wisp":
                    scene = StoryScene.Wisp;
                    legacyTile = new Vector2(7300f, 567f);
                    break;

                case "counsel":
                    scene = StoryScene.Counsel;
                    legacyTile = new Vector2(7314f, 603f);
                    break;

                case "homecoming":
                    scene = StoryScene.Homecoming;
                    legacyTile = new Vector2(4249f, 712f);
                    if (tsorcRevampWorld.RemixMap)
                    {
                        legacyTile.X = 4247f;
                    }
                    break;

                case "elijah":
                    scene = StoryScene.ElijahRescue;
                    legacyTile = new Vector2(4461f, 1744f);
                    break;

                case "firetemple":
                    scene = StoryScene.FireTemple;
                    legacyTile = new Vector2(4246f, 1827f);
                    break;

                case "shadow":
                    scene = StoryScene.ShadowRumors;
                    legacyTile = new Vector2(1899f, 1120f);
                    break;

                case "desert":
                    scene = StoryScene.DesertPaths;
                    legacyTile = new Vector2(2364f, 842f);
                    break;

                case "rage":
                    scene = StoryScene.RageWarning;
                    legacyTile = new Vector2(7098f, 1456f);
                    break;

                case "well":
                    scene = StoryScene.Well;
                    legacyTile = new Vector2(4211f, 712f);
                    break;

                case "idol":
                    scene = StoryScene.Idol;
                    legacyTile = new Vector2(4094f, 712f);
                    if (tsorcRevampWorld.RemixMap)
                    {
                        legacyTile.X = 4065f;
                    }
                    break;

                default:
                    caller.Reply(Usage, Color.OrangeRed);
                    return;
            }

            Player player = caller.Player;
            Vector2 destination = ExpandedWorldTransform.MapWorld(legacyTile * 16f);
            player.Teleport(destination, TeleportationStyleID.DebugTeleport);

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, player.whoAmI, destination.X, destination.Y, TeleportationStyleID.DebugTeleport);
            }

            bool started = StoryCutscenes.Play(scene, player.whoAmI);
            if (!started)
            {
                caller.Reply("Another cutscene is already running.", Color.OrangeRed);
            }
        }
    }
}
