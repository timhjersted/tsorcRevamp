using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Systems
{
    public enum CutsceneFocusMode : byte
    {
        FixedPoint,
        Npc,
        MidpointOfNpcs,
        MidpointLocalPlayerAndNpc,
        Player,
        MidpointPlayerAndNpc,
    }

    /// <summary>What the camera looks at during a cutscene. NpcA / NpcB are whoAmI slots; -1 = unused.</summary>
    public struct CutsceneFocus
    {
        public CutsceneFocusMode Mode;
        public Vector2 Point;
        public int NpcA;
        public int NpcB;

        public static CutsceneFocus AtPoint(Vector2 point)
        {
            return new CutsceneFocus { Mode = CutsceneFocusMode.FixedPoint, Point = point, NpcA = -1, NpcB = -1 };
        }

        public static CutsceneFocus OnNpc(int npcWhoAmI)
        {
            return new CutsceneFocus { Mode = CutsceneFocusMode.Npc, NpcA = npcWhoAmI, NpcB = -1 };
        }

        public static CutsceneFocus BetweenNpcs(int firstNpc, int secondNpc)
        {
            return new CutsceneFocus { Mode = CutsceneFocusMode.MidpointOfNpcs, NpcA = firstNpc, NpcB = secondNpc };
        }

        public static CutsceneFocus BetweenLocalPlayerAndNpc(int npcWhoAmI)
        {
            return new CutsceneFocus { Mode = CutsceneFocusMode.MidpointLocalPlayerAndNpc, NpcA = npcWhoAmI, NpcB = -1 };
        }

        /// <summary>Follows one player. The player's whoAmI travels in NpcA so the packet layout stays the same.</summary>
        public static CutsceneFocus OnPlayer(int playerWhoAmI)
        {
            return new CutsceneFocus { Mode = CutsceneFocusMode.Player, NpcA = playerWhoAmI, NpcB = -1 };
        }

        /// <summary>Frames one named player with an NPC, so every viewer sees the same shot. NpcA holds the player's whoAmI.</summary>
        public static CutsceneFocus BetweenPlayerAndNpc(int playerWhoAmI, int npcWhoAmI)
        {
            return new CutsceneFocus { Mode = CutsceneFocusMode.MidpointPlayerAndNpc, NpcA = playerWhoAmI, NpcB = npcWhoAmI };
        }
    }

    public enum CutsceneSpeaker : byte
    {
        None,
        Npc,
        Player,
    }

    /// <summary>One speech bubble. Keys are relative to Mods.tsorcRevamp. Speaker None draws at the top of the screen.</summary>
    public struct CutsceneLine
    {
        public CutsceneSpeaker Speaker;
        public int SpeakerIndex;
        public string NameKey;
        public string TextKey;

        public static CutsceneLine FromNpc(int npcWhoAmI, string nameKey, string textKey)
        {
            return new CutsceneLine { Speaker = CutsceneSpeaker.Npc, SpeakerIndex = npcWhoAmI, NameKey = nameKey, TextKey = textKey };
        }

        public static CutsceneLine FromPlayer(int playerWhoAmI, string nameKey, string textKey)
        {
            return new CutsceneLine { Speaker = CutsceneSpeaker.Player, SpeakerIndex = playerWhoAmI, NameKey = nameKey, TextKey = textKey };
        }

        public static CutsceneLine FromNobody(string nameKey, string textKey)
        {
            return new CutsceneLine { Speaker = CutsceneSpeaker.None, SpeakerIndex = -1, NameKey = nameKey, TextKey = textKey };
        }
    }

    /// <summary>Who a scene applies to. Scenes started without a cast apply to every player and have no lead.</summary>
    public sealed class CutsceneCast
    {
        public int LeadPlayer = -1;
        public bool AllowSkip;
        public List<int> Players = new List<int>();
    }

    /// <summary>
    /// Reusable synced cutscene frame: player freeze and immunity, letterbox bars, hidden HUD, camera focus and zoom, speech
    /// bubbles and no enemy spawns. It knows nothing about bosses: a script moves the actors (ordinary synced NPCs) and calls
    /// these methods on the server or in single player; clients follow the packets.
    /// </summary>
    public class CutsceneSystem : ModSystem
    {
        private const byte PacketStart = 0;
        private const byte PacketStop = 1;
        private const byte PacketFinishIn = 2;
        private const byte PacketShowLine = 3;
        private const byte PacketClearLine = 4;
        private const byte PacketSetFocus = 5;
        private const byte PacketSetZoom = 6;
        private const byte PacketRequestAdvance = 7;
        private const byte PacketRequestSkip = 8;
        private const byte PacketDirectPlayer = 9;
        private const byte PacketSetSkipAllowed = 10;
        private const byte PacketStartCredits = 11;

        private const int BarEaseTicks = 20;
        private const float BarScreenFraction = 0.11f;
        private const int ClientExpiryGraceTicks = 30;
        private const float ProjectileClearRange = 3000f;
        private const int PlayerSlots = Main.maxPlayers + 1;

        private const int BubbleFadeTicks = 12;
        private const float BubbleCharactersPerTick = 1.5f;
        private const float BubbleTextWidth = 380f;
        private const int BubblePadding = 14;
        private const int BubbleTailHeight = 12;
        private const int BubbleScreenMargin = 16;

        public static bool IsActive { get; private set; }
        public static string SceneId { get; private set; } = "";
        public static int Timer { get; private set; }
        public static int Duration { get; private set; }
        public static CutsceneFocus Focus { get; private set; }

        /// <summary>True when the local player is held by the running scene.</summary>
        public static bool FreezesPlayers => Freezes(Main.LocalPlayer);

        /// <summary>Server side: the current line timed out or the lead pressed Next.</summary>
        public static bool LineFinished { get; private set; }

        /// <summary>Server side: the lead pressed Skip during this scene.</summary>
        public static bool SkipRequested { get; private set; }

        private static int anchorNpc = -1;
        private static readonly bool[] participants = new bool[PlayerSlots];
        private static readonly bool[] frozen = new bool[PlayerSlots];
        private static int leadPlayer = -1;
        private static bool allowSkip;

        private static bool lineActive;
        private static CutsceneLine currentLine;
        private static int lineSequence;
        private static int lineTimer;
        private static int lineAutoAdvanceTicks;

        // Local bubble state: the wrapped text is cached per line, and a first Next press only finishes the typing.
        private static int cachedLineSequence = -1;
        private static string cachedSpeakerName = "";
        private static string cachedWrappedText = "";
        private static int revealedLineSequence = -1;
        private static bool localTextComplete;

        private static float zoomFrom = 1f;
        private static float zoomTo = 1f;
        private static int zoomTimer;
        private static int zoomTicks;

        // A focus change can glide from wherever this client's camera was, instead of cutting.
        private static Vector2 lastCameraPoint;
        private static Vector2 focusEaseFrom;
        private static int focusEaseTimer;
        private static int focusEaseTicks;

        // Scene ids already watched in this world. Only the server reads it, so it does not need syncing.
        private static readonly HashSet<string> seenScenes = new HashSet<string>();

        public static bool IsParticipant(Player player)
        {
            return IsActive && participants[player.whoAmI];
        }

        public static bool Freezes(Player player)
        {
            // Credits are private to each client, so they only ever hold that client's own player.
            bool heldByCredits = CreditsRoll.IsShowing && player.whoAmI == Main.myPlayer;
            return heldByCredits || (IsActive && frozen[player.whoAmI]);
        }

        /// <summary>True while this NPC's current line is still typing, so its sprite can move its mouth.</summary>
        public static bool IsNpcSpeaking(int npcWhoAmI)
        {
            bool isSpeaker = IsActive && lineActive && currentLine.Speaker == CutsceneSpeaker.Npc && currentLine.SpeakerIndex == npcWhoAmI;
            return isSpeaker && !localTextComplete;
        }

        public static bool IsLead(Player player)
        {
            return IsActive && leadPlayer == player.whoAmI;
        }

        public static bool HasSeen(string sceneId)
        {
            return seenScenes.Contains(sceneId);
        }

        /// <summary>Forgets every watched scene in this world and returns how many there were.</summary>
        public static int ClearSeenScenes()
        {
            int clearedCount = seenScenes.Count;
            seenScenes.Clear();
            return clearedCount;
        }

        public static void MarkSeen(string sceneId)
        {
            seenScenes.Add(sceneId);
        }

        /// <summary>Lets one player move and be hurt again while the scene plays on. Every machine calls this at the same event.</summary>
        public static void ReleasePlayer(int playerWhoAmI)
        {
            frozen[playerWhoAmI] = false;
        }

        /// <summary>Starts a scene (server / single player only). The scene ends at durationTicks, on Stop, or when the
        /// anchor NPC disappears. Hostile projectiles near the anchor are killed through Kill() so their effects play.</summary>
        public static void Start(string sceneId, int durationTicks, CutsceneFocus focus, int anchorNpcWhoAmI = -1, CutsceneCast cast = null)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Begin(sceneId, durationTicks, focus, anchorNpcWhoAmI, cast);
            ClearHostileProjectiles();

            if (Main.netMode != NetmodeID.Server)
            {
                return;
            }

            ModPacket packet = NewPacket(PacketStart);
            packet.Write(sceneId);
            packet.Write(durationTicks);
            WriteFocus(packet, focus);
            packet.Write((short)anchorNpcWhoAmI);

            packet.Write(cast != null);
            if (cast != null)
            {
                packet.Write((short)cast.LeadPlayer);
                packet.Write(cast.AllowSkip);
                packet.Write((byte)cast.Players.Count);
                foreach (int playerIndex in cast.Players)
                {
                    packet.Write((byte)playerIndex);
                }
            }

            packet.Send();
        }

        /// <summary>Ends a running scene early. A different or empty scene id is ignored.</summary>
        public static void Stop(string sceneId)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !IsActive || SceneId != sceneId)
            {
                return;
            }

            End();

            if (Main.netMode == NetmodeID.Server)
            {
                NewPacket(PacketStop).Send();
            }
        }

        /// <summary>Ends the scene this many ticks from now, so the bars can ease out. Also used to hold a scene open.</summary>
        public static void FinishIn(int remainingTicks)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !IsActive)
            {
                return;
            }

            Duration = Timer + remainingTicks;

            if (Main.netMode == NetmodeID.Server)
            {
                ModPacket packet = NewPacket(PacketFinishIn);
                packet.Write(remainingTicks);
                packet.Send();
            }
        }

        /// <summary>Shows a speech bubble. It stays up until the script replaces or clears it; LineFinished turns true when
        /// the lead presses Next or autoAdvanceTicks pass.</summary>
        public static void ShowLine(CutsceneLine line, int autoAdvanceTicks)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !IsActive)
            {
                return;
            }

            lineSequence++;
            BeginLine(line, lineSequence, autoAdvanceTicks);

            if (Main.netMode == NetmodeID.Server)
            {
                ModPacket packet = NewPacket(PacketShowLine);
                packet.Write(lineSequence);
                packet.Write((byte)line.Speaker);
                packet.Write((short)line.SpeakerIndex);
                packet.Write(line.NameKey);
                packet.Write(line.TextKey);
                packet.Write(autoAdvanceTicks);
                packet.Send();
            }
        }

        public static void ClearLine()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            lineActive = false;
            LineFinished = false;

            if (Main.netMode == NetmodeID.Server)
            {
                NewPacket(PacketClearLine).Send();
            }
        }

        /// <summary>Moves the camera to a new focus. easeTicks 0 cuts; anything higher glides there from the current view.</summary>
        public static void SetFocus(CutsceneFocus focus, int easeTicks = 0)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !IsActive)
            {
                return;
            }

            BeginFocus(focus, easeTicks);

            if (Main.netMode == NetmodeID.Server)
            {
                ModPacket packet = NewPacket(PacketSetFocus);
                WriteFocus(packet, focus);
                packet.Write(easeTicks);
                packet.Send();
            }
        }

        /// <summary>Walks a held player to a world X, or snaps them there, then keeps them facing a direction. A NaN target
        /// only turns them. The owning client moves its own player, so this travels as a packet in multiplayer.</summary>
        public static void DirectPlayer(int playerWhoAmI, float targetWorldX, int facing, bool snap)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !IsActive)
            {
                return;
            }

            if (Main.netMode == NetmodeID.Server)
            {
                ModPacket packet = NewPacket(PacketDirectPlayer);
                packet.Write((byte)playerWhoAmI);
                packet.Write(targetWorldX);
                packet.Write((sbyte)facing);
                packet.Write(snap);
                packet.Send(playerWhoAmI);
                return;
            }

            Main.player[playerWhoAmI].GetModPlayer<CutscenePlayer>().ReceiveDirection(targetWorldX, facing, snap);
        }

        /// <summary>Shows or hides Skip mid-scene and forgets any pending press.</summary>
        public static void SetSkipAllowed(bool allowed)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            allowSkip = allowed;
            SkipRequested = false;

            if (Main.netMode == NetmodeID.Server)
            {
                ModPacket packet = NewPacket(PacketSetSkipAllowed);
                packet.Write(allowed);
                packet.Send();
            }
        }

        /// <summary>Server side: tells every client to queue its own private credits roll.</summary>
        internal static void SendCreditsStart()
        {
            if (Main.netMode == NetmodeID.Server)
            {
                NewPacket(PacketStartCredits).Send();
            }
        }

        public static void SetZoom(float targetZoom, int easeTicks)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !IsActive)
            {
                return;
            }

            BeginZoom(targetZoom, easeTicks);

            if (Main.netMode == NetmodeID.Server)
            {
                ModPacket packet = NewPacket(PacketSetZoom);
                packet.Write(targetZoom);
                packet.Write(easeTicks);
                packet.Send();
            }
        }

        public static void ReceivePacket(BinaryReader reader, int sender)
        {
            byte action = reader.ReadByte();

            switch (action)
            {
                case PacketStop:
                    End();
                    break;

                case PacketStart:
                    ReceiveStart(reader);
                    break;

                case PacketFinishIn:
                    Duration = Timer + reader.ReadInt32();
                    break;

                case PacketShowLine:
                {
                    int sequence = reader.ReadInt32();
                    CutsceneLine line = new CutsceneLine
                    {
                        Speaker = (CutsceneSpeaker)reader.ReadByte(),
                        SpeakerIndex = reader.ReadInt16(),
                        NameKey = reader.ReadString(),
                        TextKey = reader.ReadString(),
                    };
                    int autoAdvanceTicks = reader.ReadInt32();
                    BeginLine(line, sequence, autoAdvanceTicks);
                    break;
                }

                case PacketClearLine:
                    lineActive = false;
                    break;

                case PacketSetFocus:
                {
                    CutsceneFocus focus = ReadFocus(reader);
                    int easeTicks = reader.ReadInt32();
                    BeginFocus(focus, easeTicks);
                    break;
                }

                case PacketDirectPlayer:
                {
                    int playerIndex = reader.ReadByte();
                    float targetWorldX = reader.ReadSingle();
                    int facing = reader.ReadSByte();
                    bool snap = reader.ReadBoolean();
                    Main.player[playerIndex].GetModPlayer<CutscenePlayer>().ReceiveDirection(targetWorldX, facing, snap);
                    break;
                }

                case PacketSetSkipAllowed:
                    allowSkip = reader.ReadBoolean();
                    break;

                case PacketStartCredits:
                    CreditsRoll.Queue(CreditsRoll.AfterBossDelayTicks);
                    break;

                case PacketSetZoom:
                {
                    float targetZoom = reader.ReadSingle();
                    int easeTicks = reader.ReadInt32();
                    BeginZoom(targetZoom, easeTicks);
                    break;
                }

                case PacketRequestAdvance:
                {
                    int sequence = reader.ReadInt32();
                    if (Main.netMode == NetmodeID.Server)
                    {
                        HandleAdvance(sequence, sender);
                    }
                    break;
                }

                case PacketRequestSkip:
                    if (Main.netMode == NetmodeID.Server)
                    {
                        HandleSkip(sender);
                    }
                    break;
            }
        }

        private static void ReceiveStart(BinaryReader reader)
        {
            string sceneId = reader.ReadString();
            int duration = reader.ReadInt32();
            CutsceneFocus focus = ReadFocus(reader);
            int anchor = reader.ReadInt16();

            CutsceneCast cast = null;
            bool hasCast = reader.ReadBoolean();
            if (hasCast)
            {
                cast = new CutsceneCast();
                cast.LeadPlayer = reader.ReadInt16();
                cast.AllowSkip = reader.ReadBoolean();

                int playerCount = reader.ReadByte();
                for (int i = 0; i < playerCount; i++)
                {
                    cast.Players.Add(reader.ReadByte());
                }
            }

            Begin(sceneId, duration, focus, anchor, cast);
        }

        private static ModPacket NewPacket(byte action)
        {
            ModPacket packet = ModContent.GetInstance<tsorcRevamp>().GetPacket();
            packet.Write(tsorcPacketID.CutsceneSync);
            packet.Write(action);
            return packet;
        }

        private static void WriteFocus(ModPacket packet, CutsceneFocus focus)
        {
            packet.Write((byte)focus.Mode);
            packet.WriteVector2(focus.Point);
            packet.Write((short)focus.NpcA);
            packet.Write((short)focus.NpcB);
        }

        private static CutsceneFocus ReadFocus(BinaryReader reader)
        {
            return new CutsceneFocus
            {
                Mode = (CutsceneFocusMode)reader.ReadByte(),
                Point = reader.ReadVector2(),
                NpcA = reader.ReadInt16(),
                NpcB = reader.ReadInt16(),
            };
        }

        private static void Begin(string sceneId, int durationTicks, CutsceneFocus focus, int anchorNpcWhoAmI, CutsceneCast cast)
        {
            IsActive = true;
            SceneId = sceneId;
            Duration = durationTicks;
            Timer = 0;
            Focus = focus;
            anchorNpc = anchorNpcWhoAmI;

            ResetSceneState();

            // No cast keeps the original behavior: the scene holds every player.
            bool everyone = cast == null;
            Array.Fill(participants, everyone);
            Array.Fill(frozen, everyone);

            if (cast != null)
            {
                leadPlayer = cast.LeadPlayer;
                allowSkip = cast.AllowSkip;

                foreach (int playerIndex in cast.Players)
                {
                    participants[playerIndex] = true;
                    frozen[playerIndex] = true;
                }
            }
        }

        private static void End()
        {
            IsActive = false;
            SceneId = "";
            Timer = 0;
            Duration = 0;
            anchorNpc = -1;

            ResetSceneState();
            Array.Clear(participants);
            Array.Clear(frozen);
        }

        private static void ResetSceneState()
        {
            leadPlayer = -1;
            allowSkip = false;
            lineActive = false;
            LineFinished = false;
            SkipRequested = false;
            lineTimer = 0;
            zoomFrom = 1f;
            zoomTo = 1f;
            zoomTimer = 0;
            zoomTicks = 0;
            focusEaseTimer = 0;
            focusEaseTicks = 0;
        }

        private static void BeginFocus(CutsceneFocus focus, int easeTicks)
        {
            Focus = focus;
            focusEaseFrom = lastCameraPoint;
            focusEaseTimer = 0;
            focusEaseTicks = easeTicks;
        }

        private static void BeginLine(CutsceneLine line, int sequence, int autoAdvanceTicks)
        {
            currentLine = line;
            lineSequence = sequence;
            lineAutoAdvanceTicks = autoAdvanceTicks;
            lineTimer = 0;
            lineActive = true;
            LineFinished = false;
            localTextComplete = false;
        }

        private static void BeginZoom(float targetZoom, int easeTicks)
        {
            zoomFrom = CurrentZoom;
            zoomTo = targetZoom;
            zoomTimer = 0;
            zoomTicks = easeTicks;
        }

        private static float CurrentZoom
        {
            get
            {
                if (zoomTicks <= 0)
                {
                    return zoomTo;
                }

                float progress = MathHelper.Clamp(zoomTimer / (float)zoomTicks, 0f, 1f);
                float eased = progress * progress * (3f - 2f * progress);
                return MathHelper.Lerp(zoomFrom, zoomTo, eased);
            }
        }

        private static void HandleAdvance(int sequence, int sender)
        {
            if (lineActive && sequence == lineSequence && sender == leadPlayer)
            {
                LineFinished = true;
            }
        }

        private static void HandleSkip(int sender)
        {
            if (IsActive && allowSkip && sender == leadPlayer)
            {
                SkipRequested = true;
            }
        }

        /// <summary>Local Next press: the first press finishes the typing, the next one asks to advance.</summary>
        private static void PressNext()
        {
            if (!localTextComplete)
            {
                revealedLineSequence = lineSequence;
                localTextComplete = true;
                return;
            }

            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                HandleAdvance(lineSequence, Main.myPlayer);
                return;
            }

            ModPacket packet = NewPacket(PacketRequestAdvance);
            packet.Write(lineSequence);
            packet.Send();
        }

        private static void PressSkip()
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                HandleSkip(Main.myPlayer);
                return;
            }

            NewPacket(PacketRequestSkip).Send();
        }

        /// <summary>Called by the local player's trigger hook, so Jump and controllers can advance a line.</summary>
        internal static void ProcessNextTrigger()
        {
            bool canPress = lineActive && IsLead(Main.LocalPlayer) && !Main.drawingPlayerChat;
            if (canPress && PlayerInput.Triggers.JustPressed.Jump)
            {
                PressNext();
            }
        }

        private static void ClearHostileProjectiles()
        {
            Vector2 center = FocusPoint(Main.LocalPlayer);
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                bool clearable = projectile.active && projectile.hostile && projectile.Distance(center) <= ProjectileClearRange;
                if (clearable)
                {
                    projectile.Kill();
                }
            }
        }

        /// <summary>World point the camera eases toward. A missing focus NPC falls back to the given player.</summary>
        public static Vector2 FocusPoint(Player viewer)
        {
            CutsceneFocus focus = Focus;
            bool firstValid = focus.NpcA >= 0 && focus.NpcA < Main.maxNPCs && Main.npc[focus.NpcA].active;
            bool secondValid = focus.NpcB >= 0 && focus.NpcB < Main.maxNPCs && Main.npc[focus.NpcB].active;

            switch (focus.Mode)
            {
                case CutsceneFocusMode.FixedPoint:
                    return focus.Point;

                case CutsceneFocusMode.Npc:
                    if (firstValid)
                    {
                        return Main.npc[focus.NpcA].Center;
                    }
                    break;

                case CutsceneFocusMode.MidpointOfNpcs:
                    if (firstValid && secondValid)
                    {
                        return (Main.npc[focus.NpcA].Center + Main.npc[focus.NpcB].Center) * 0.5f;
                    }
                    if (firstValid)
                    {
                        return Main.npc[focus.NpcA].Center;
                    }
                    break;

                case CutsceneFocusMode.MidpointLocalPlayerAndNpc:
                    if (firstValid)
                    {
                        return (viewer.Center + Main.npc[focus.NpcA].Center) * 0.5f;
                    }
                    break;

                case CutsceneFocusMode.Player:
                    if (focus.NpcA >= 0 && focus.NpcA < Main.maxPlayers && Main.player[focus.NpcA].active)
                    {
                        return Main.player[focus.NpcA].Center;
                    }
                    break;

                case CutsceneFocusMode.MidpointPlayerAndNpc:
                {
                    bool playerValid = focus.NpcA >= 0 && focus.NpcA < Main.maxPlayers && Main.player[focus.NpcA].active;
                    bool npcValid = focus.NpcB >= 0 && focus.NpcB < Main.maxNPCs && Main.npc[focus.NpcB].active;
                    if (playerValid && npcValid)
                    {
                        return (Main.player[focus.NpcA].Center + Main.npc[focus.NpcB].Center) * 0.5f;
                    }
                    if (playerValid)
                    {
                        return Main.player[focus.NpcA].Center;
                    }
                    break;
                }
            }

            return viewer.Center;
        }

        /// <summary>Where this client's camera belongs this frame: the focus, or partway there during an eased change.</summary>
        internal static Vector2 UpdateCameraPoint(Player viewer)
        {
            Vector2 cameraPoint = FocusPoint(viewer);

            if (focusEaseTimer < focusEaseTicks)
            {
                float progress = focusEaseTimer / (float)focusEaseTicks;
                float eased = progress * progress * (3f - 2f * progress);
                cameraPoint = Vector2.Lerp(focusEaseFrom, cameraPoint, eased);
            }

            lastCameraPoint = cameraPoint;
            return cameraPoint;
        }

        /// <summary>0 outside a scene, easing to 1 over the first and back over the last BarEaseTicks.</summary>
        public static float BarProgress
        {
            get
            {
                if (!IsActive)
                {
                    return 0f;
                }

                float easeIn = Timer / (float)BarEaseTicks;
                float easeOut = (Duration - Timer) / (float)BarEaseTicks;
                float progress = MathHelper.Clamp(Math.Min(easeIn, easeOut), 0f, 1f);
                return progress * progress * (3f - 2f * progress);
            }
        }

        public override void PostUpdateNPCs()
        {
            if (!IsActive)
            {
                return;
            }

            Timer++;
            zoomTimer++;
            focusEaseTimer++;

            if (lineActive)
            {
                lineTimer++;
            }

            bool client = Main.netMode == NetmodeID.MultiplayerClient;
            if (client)
            {
                // Self-expiry covers a lost stop packet; the server normally ends the scene first.
                if (Timer >= Duration + ClientExpiryGraceTicks)
                {
                    End();
                }

                return;
            }

            if (lineActive)
            {
                // A departed lead can never press Next, so their line moves on immediately.
                bool leadMissing = leadPlayer < 0 || !Main.player[leadPlayer].active;
                bool timedOut = lineAutoAdvanceTicks > 0 && lineTimer >= lineAutoAdvanceTicks;
                if (leadMissing || timedOut)
                {
                    LineFinished = true;
                }
            }

            bool anchorGone = anchorNpc >= 0 && !Main.npc[anchorNpc].active;
            if (Timer >= Duration || anchorGone)
            {
                Stop(SceneId);
            }
        }

        public override void ModifyTransformMatrix(ref SpriteViewMatrix Transform)
        {
            if (!IsParticipant(Main.LocalPlayer))
            {
                return;
            }

            float zoom = CurrentZoom;
            if (zoom != 1f)
            {
                Transform.Zoom *= zoom;
            }
        }

        public override void ClearWorld()
        {
            seenScenes.Clear();
        }

        public override void SaveWorldData(TagCompound tag)
        {
            if (seenScenes.Count > 0)
            {
                tag["SeenCutscenes"] = new List<string>(seenScenes);
            }
        }

        public override void LoadWorldData(TagCompound tag)
        {
            foreach (string sceneId in tag.GetList<string>("SeenCutscenes"))
            {
                seenScenes.Add(sceneId);
            }
        }

        public override void OnWorldLoad()
        {
            End();
        }

        public override void OnWorldUnload()
        {
            End();
        }

        /// <summary>Hides every interface layer for participants and adds the bars and bubbles. Called last from
        /// tsorcRevampSystems.ModifyInterfaceLayers so the mod's own layers are hidden too. Vanilla's Interface Logic layers
        /// keep running: they update UI state. The lead keeps the cursor whenever there is something to click.</summary>
        public static void ApplyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (CreditsRoll.IsShowing)
            {
                CreditsRoll.ApplyInterfaceLayers(layers);
                return;
            }

            Player localPlayer = Main.LocalPlayer;

            // A new world opens on black: the HUD and world stay hidden on the frames before the first scene has started.
            if (NPCs.Special.StoryCutscenes.BuriedSceneDue(localPlayer))
            {
                foreach (GameInterfaceLayer layer in layers)
                {
                    if (!layer.Name.Contains("Interface Logic"))
                    {
                        layer.Active = false;
                    }
                }

                layers.Add(new LegacyGameInterfaceLayer("tsorcRevamp: Opening Black", NPCs.Special.StoryPropActor.DrawMemoryLayer, InterfaceScaleType.UI));
                return;
            }

            if (!IsParticipant(localPlayer))
            {
                return;
            }

            bool leadNeedsCursor = IsLead(localPlayer) && (lineActive || allowSkip);
            int cursorLayerIndex = -1;

            for (int i = 0; i < layers.Count; i++)
            {
                GameInterfaceLayer layer = layers[i];
                if (layer.Name == "Vanilla: Cursor")
                {
                    cursorLayerIndex = i;
                    layer.Active = leadNeedsCursor;
                }
                else if (!layer.Name.Contains("Interface Logic"))
                {
                    layer.Active = false;
                }
            }

            GameInterfaceLayer barLayer = new LegacyGameInterfaceLayer("tsorcRevamp: Cutscene Bars", DrawBars, InterfaceScaleType.None);
            GameInterfaceLayer bubbleLayer = new LegacyGameInterfaceLayer("tsorcRevamp: Cutscene Dialogue", DrawDialogue, InterfaceScaleType.UI);

            // Both go under the cursor when it is present, so it stays visible over the Next button.
            int insertIndex = layers.Count;
            if (cursorLayerIndex >= 0)
            {
                insertIndex = cursorLayerIndex;
            }

            // The well's memory covers the world but stays under the bars and speech bubbles.
            GameInterfaceLayer memoryLayer = new LegacyGameInterfaceLayer("tsorcRevamp: Cutscene Memory", NPCs.Special.StoryPropActor.DrawMemoryLayer, InterfaceScaleType.UI);

            layers.Insert(insertIndex, memoryLayer);
            layers.Insert(insertIndex + 1, barLayer);
            layers.Insert(insertIndex + 2, bubbleLayer);
        }

        private static bool DrawBars()
        {
            int fullBarHeight = (int)(Main.screenHeight * BarScreenFraction);
            int barHeight = (int)(fullBarHeight * BarProgress);
            Rectangle pixelSource = new Rectangle(0, 0, 1, 1);
            Rectangle topBar = new Rectangle(0, 0, Main.screenWidth, barHeight);
            Rectangle bottomBar = new Rectangle(0, Main.screenHeight - barHeight, Main.screenWidth, barHeight);

            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, topBar, pixelSource, Color.Black);
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, bottomBar, pixelSource, Color.Black);
            return true;
        }

        /// <summary>World position above the speaker's head, in UI coordinates. Accounts for game zoom and UI scale.</summary>
        private static bool TryGetSpeakerAnchor(out Vector2 uiPosition)
        {
            uiPosition = Vector2.Zero;
            Vector2 worldPosition;

            if (currentLine.Speaker == CutsceneSpeaker.Npc && Main.npc[currentLine.SpeakerIndex].active)
            {
                worldPosition = Main.npc[currentLine.SpeakerIndex].Top;
            }
            else if (currentLine.Speaker == CutsceneSpeaker.Player && Main.player[currentLine.SpeakerIndex].active)
            {
                worldPosition = Main.player[currentLine.SpeakerIndex].Top;
            }
            else
            {
                return false;
            }

            Vector2 screenPosition = Vector2.Transform(worldPosition - Main.screenPosition, Main.GameViewMatrix.ZoomMatrix);
            uiPosition = screenPosition / Main.UIScale;
            return true;
        }

        private static bool DrawDialogue()
        {
            SpriteBatch spriteBatch = Main.spriteBatch;
            Player localPlayer = Main.LocalPlayer;
            bool isLead = IsLead(localPlayer);

            float uiWidth = Main.screenWidth / Main.UIScale;
            float uiHeight = Main.screenHeight / Main.UIScale;
            float barHeight = Main.screenHeight * BarScreenFraction * BarProgress / Main.UIScale;
            Point mousePoint = new Point(Main.mouseX, Main.mouseY);

            // Matches soapstones: the default font does not cover Chinese well.
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            if (Language.ActiveCulture.Name == "zh-Hans")
            {
                font = FontAssets.ItemStack.Value;
            }

            if (allowSkip && isLead)
            {
                string skipText = LangUtils.GetTextValue("Cutscenes.UI.Skip");
                Vector2 skipSize = font.MeasureString(skipText);
                Vector2 skipPosition = new Vector2(uiWidth - skipSize.X - BubbleScreenMargin * 2, uiHeight - skipSize.Y - BubbleScreenMargin);
                Rectangle skipArea = new Rectangle((int)skipPosition.X - 6, (int)skipPosition.Y - 2, (int)skipSize.X + 12, (int)skipSize.Y + 4);
                bool skipHovered = skipArea.Contains(mousePoint);

                if (skipHovered)
                {
                    localPlayer.mouseInterface = true;
                    if (Main.mouseLeft && Main.mouseLeftRelease)
                    {
                        Main.mouseLeftRelease = false;
                        PressSkip();
                    }
                }

                Color skipColor = Color.LightGray;
                if (skipHovered)
                {
                    skipColor = Color.White;
                }

                Utils.DrawBorderStringFourWay(spriteBatch, font, skipText, skipPosition.X, skipPosition.Y, skipColor, Color.Black, Vector2.Zero);
            }

            if (!lineActive)
            {
                return true;
            }

            // Wrapping is stable per line, so typing never makes a word jump to the next row.
            if (cachedLineSequence != lineSequence)
            {
                string fullText = LangUtils.GetTextValue(currentLine.TextKey);
                string wrappedText = UsefulFunctions.WrapString(fullText, font, BubbleTextWidth);
                cachedWrappedText = wrappedText.TrimStart().Replace("\n ", "\n");
                cachedSpeakerName = LangUtils.GetTextValue(currentLine.NameKey);
                cachedLineSequence = lineSequence;
            }

            int revealedCharacters = (int)(lineTimer * BubbleCharactersPerTick);
            if (revealedLineSequence == lineSequence || revealedCharacters >= cachedWrappedText.Length)
            {
                revealedCharacters = cachedWrappedText.Length;
                localTextComplete = true;
            }

            float opacity = MathHelper.Clamp(lineTimer / (float)BubbleFadeTicks, 0f, 1f);
            Vector2 textSize = font.MeasureString(cachedWrappedText);
            Vector2 nameSize = font.MeasureString(cachedSpeakerName);
            string nextText = LangUtils.GetTextValue("Cutscenes.UI.Next");
            Vector2 nextSize = font.MeasureString(nextText);

            float contentWidth = Math.Max(textSize.X, nameSize.X);
            float footerHeight = 0f;
            if (isLead)
            {
                contentWidth = Math.Max(contentWidth, nextSize.X);
                footerHeight = nextSize.Y;
            }

            int panelWidth = (int)contentWidth + BubblePadding * 2;
            int panelHeight = (int)(nameSize.Y + textSize.Y + footerHeight) + BubblePadding * 2;

            // Top of the screen for a voice with no body, otherwise just above the speaker's head.
            float topLimit = barHeight + BubbleScreenMargin;
            float bottomLimit = uiHeight - barHeight - BubbleScreenMargin - panelHeight;
            float panelX = (uiWidth - panelWidth) * 0.5f;
            float panelY = topLimit;
            bool hasAnchor = TryGetSpeakerAnchor(out Vector2 anchor);

            if (hasAnchor)
            {
                panelX = anchor.X - panelWidth * 0.5f;
                panelY = anchor.Y - BubbleTailHeight - panelHeight;
            }

            panelX = MathHelper.Clamp(panelX, BubbleScreenMargin, Math.Max(BubbleScreenMargin, uiWidth - BubbleScreenMargin - panelWidth));
            panelY = MathHelper.Clamp(panelY, topLimit, Math.Max(topLimit, bottomLimit));

            Rectangle panel = new Rectangle((int)panelX, (int)panelY, panelWidth, panelHeight);
            Color panelColor = new Color(38, 18, 66) * (0.92f * opacity);
            Utils.DrawInvBG(spriteBatch, panel, panelColor);

            // Tail: three narrowing rows from the panel edge toward the speaker. Hidden if clamping moved the panel away.
            bool tailReachesSpeaker = hasAnchor && anchor.Y >= panel.Bottom && anchor.X > panel.Left + 10 && anchor.X < panel.Right - 10;
            if (tailReachesSpeaker)
            {
                Rectangle pixelSource = new Rectangle(0, 0, 1, 1);
                int rowHeight = BubbleTailHeight / 3;
                for (int i = 0; i < 3; i++)
                {
                    int rowWidth = 14 - i * 5;
                    Rectangle row = new Rectangle((int)anchor.X - rowWidth / 2, panel.Bottom + i * rowHeight, rowWidth, rowHeight);
                    spriteBatch.Draw(TextureAssets.MagicPixel.Value, row, pixelSource, panelColor);
                }
            }

            Vector2 namePosition = new Vector2(panel.X + BubblePadding, panel.Y + BubblePadding);
            Color nameColor = new Color(205, 150, 255) * opacity;
            Color borderColor = Color.Black * opacity;
            Utils.DrawBorderStringFourWay(spriteBatch, font, cachedSpeakerName, namePosition.X, namePosition.Y, nameColor, borderColor, Vector2.Zero);

            string visibleText = cachedWrappedText.Substring(0, Math.Min(revealedCharacters, cachedWrappedText.Length));
            float textY = namePosition.Y + nameSize.Y;
            Utils.DrawBorderStringFourWay(spriteBatch, font, visibleText, namePosition.X, textY, Color.White * opacity, borderColor, Vector2.Zero);

            if (isLead)
            {
                Vector2 nextPosition = new Vector2(panel.Right - BubblePadding - nextSize.X, panel.Bottom - BubblePadding - nextSize.Y);
                Rectangle nextArea = new Rectangle((int)nextPosition.X - 6, (int)nextPosition.Y - 2, (int)nextSize.X + 12, (int)nextSize.Y + 4);
                bool nextHovered = nextArea.Contains(mousePoint);

                if (panel.Contains(mousePoint))
                {
                    localPlayer.mouseInterface = true;
                }

                if (nextHovered && Main.mouseLeft && Main.mouseLeftRelease)
                {
                    Main.mouseLeftRelease = false;
                    PressNext();
                }

                Color nextColor = new Color(190, 170, 215);
                if (nextHovered)
                {
                    nextColor = Color.White;
                }

                Utils.DrawBorderStringFourWay(spriteBatch, font, nextText, nextPosition.X, nextPosition.Y, nextColor * opacity, borderColor, Vector2.Zero);
            }

            return true;
        }
    }

    /// <summary>The freeze, immunity and camera are player-local, so each client applies them to its own player.</summary>
    public class CutscenePlayer : ModPlayer
    {
        private const float FrozenFriction = 0.8f;
        private const float ThrowSweepStep = 12f;
        private const int ThrowSafetyTicks = 240;
        private const int ThrowMinimumAirTicks = 6;

        // A scripted stroll: slower than a run, so the walk reads as a character moving rather than a player.
        private const float WalkMaxSpeed = 2.2f;
        private const float WalkArrivalDistance = 10f;
        private const int WalkSyncInterval = 3;

        private float walkTargetX = float.NaN;
        private int heldFacing;
        private int walkSyncTimer;

        private int throwTicksLeft;
        private int throwAge;
        private int throwDirection;
        private float throwSpeed;
        private float throwDistanceLeft;

        public bool IsThrown => throwTicksLeft > 0;

        private bool IsWalking => !float.IsNaN(walkTargetX);

        /// <summary>Owning client only: applies a stage direction from CutsceneSystem.DirectPlayer.</summary>
        public void ReceiveDirection(float targetWorldX, int facing, bool snap)
        {
            if (Player.whoAmI != Main.myPlayer)
            {
                return;
            }

            heldFacing = facing;
            walkTargetX = float.NaN;

            if (float.IsNaN(targetWorldX))
            {
                return;
            }

            // Mounts and hooks would override the walk, so they are cleared before the character moves.
            Player.mount.Dismount(Player);
            Player.RemoveAllGrapplingHooks();
            Player.sitting.SitUp(Player, false);
            Player.sleeping.StopSleeping(Player, false);

            if (!snap)
            {
                walkTargetX = targetWorldX;
                return;
            }

            Player.Center = new Vector2(targetWorldX, Player.Center.Y);
            Player.velocity = Vector2.Zero;

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, Player.whoAmI);
            }
        }

        /// <summary>Starts a scripted throw on the owning client: horizontal drive stopped by tiles, normal gravity, no
        /// player input, no invulnerability. Hazards apply. Only the throw's own fall damage is removed.</summary>
        public void BeginThrow(int direction, float distancePixels, int airTicks, float launchVelocityY)
        {
            tsorcRevampPlayer modPlayer = Player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.ImpaleFreezeTimer = 0;
            modPlayer.ImpaleDrawRotation = 0f;

            throwTicksLeft = ThrowSafetyTicks;
            throwAge = 0;
            throwDirection = direction;
            throwDistanceLeft = distancePixels;
            throwSpeed = distancePixels / airTicks;

            Player.velocity = new Vector2(0f, launchVelocityY);
            Player.fallStart = (int)(Player.position.Y / 16f);
            Player.fallStart2 = Player.fallStart;
        }

        private void ClearControls()
        {
            Player.controlLeft = false;
            Player.controlRight = false;
            Player.controlUp = false;
            Player.controlDown = false;
            Player.controlJump = false;
            Player.controlUseItem = false;
            Player.controlUseTile = false;
            Player.controlThrow = false;
            Player.controlHook = false;
            Player.controlMount = false;
            Player.controlQuickHeal = false;
            Player.controlQuickMana = false;
            Player.controlSmart = false;
            Player.controlTorch = false;
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            CutsceneSystem.ProcessNextTrigger();
        }

        public override void SetControls()
        {
            if (CutsceneSystem.Freezes(Player) || IsThrown)
            {
                ClearControls();
            }

            if (!IsWalking)
            {
                return;
            }

            // Real movement input, so stairs, slopes, animation and tile collision all behave as normal.
            float remainingDistance = walkTargetX - Player.Center.X;
            if (Math.Abs(remainingDistance) <= WalkArrivalDistance)
            {
                walkTargetX = float.NaN;
                return;
            }

            if (remainingDistance > 0f)
            {
                Player.controlRight = true;
            }
            else
            {
                Player.controlLeft = true;
            }
        }

        public override void PostUpdateRunSpeeds()
        {
            if (IsWalking)
            {
                Player.maxRunSpeed = WalkMaxSpeed;
                Player.accRunSpeed = WalkMaxSpeed;
            }
        }

        public override void PreUpdate()
        {
            if (!CutsceneSystem.Freezes(Player))
            {
                walkTargetX = float.NaN;
                heldFacing = 0;
                return;
            }

            Player.itemAnimation = 0;
            Player.itemTime = 0;
            Player.channel = false;
        }

        public override void PreUpdateMovement()
        {
            if (CutsceneSystem.Freezes(Player) && !IsWalking)
            {
                Player.velocity.X *= FrozenFriction;

                if (heldFacing != 0)
                {
                    Player.direction = heldFacing;
                }
            }

            if (IsWalking && Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer)
            {
                // The server waits on this position to know when the walk has finished.
                walkSyncTimer++;
                if (walkSyncTimer >= WalkSyncInterval)
                {
                    walkSyncTimer = 0;
                    NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, Player.whoAmI);
                }
            }

            if (!IsThrown)
            {
                return;
            }

            throwTicksLeft--;
            throwAge++;

            // Sweep in short steps so a fast throw cannot pass through a thin wall. Velocity.X stays zero because this
            // sweep already moved the player; vanilla would otherwise apply friction and move them a second time.
            float stepBudget = Math.Min(throwSpeed, throwDistanceLeft);
            while (stepBudget > 0.01f)
            {
                float stepLength = Math.Min(ThrowSweepStep, stepBudget);
                Vector2 attemptedStep = new Vector2(throwDirection * stepLength, 0f);
                Vector2 allowedStep = Collision.TileCollision(Player.position, attemptedStep, Player.width, Player.height, true, true, (int)Player.gravDir);

                Player.position.X += allowedStep.X;
                stepBudget -= stepLength;
                throwDistanceLeft -= stepLength;

                bool hitWall = Math.Abs(allowedStep.X) < stepLength - 0.01f;
                if (hitWall)
                {
                    throwDistanceLeft = 0f;
                    break;
                }
            }

            Player.velocity.X = 0f;

            // Re-arming each tick keeps the throw and any drop it causes from dealing fall damage. Spikes still apply.
            Player.fallStart = (int)(Player.position.Y / 16f);
            Player.fallStart2 = Player.fallStart;

            if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer && throwAge % 2 == 0)
            {
                NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, Player.whoAmI);
            }

            bool landed = throwAge > ThrowMinimumAirTicks && Player.velocity.Y == 0f && Player.oldVelocity.Y >= 0f;
            if (landed)
            {
                throwTicksLeft = 0;
            }
        }

        public override void UpdateDead()
        {
            throwTicksLeft = 0;
        }

        public override void PostUpdateMiscEffects()
        {
            if (!CutsceneSystem.Freezes(Player))
            {
                return;
            }

            Player.immune = true;
            Player.immuneTime = Math.Max(Player.immuneTime, 2);
            Player.immuneNoBlink = true;
        }

        // Player.immune does not stop spikes or lava (they use cooldown counters), so frozen players need this hook too.
        public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable)
        {
            return CutsceneSystem.Freezes(Player);
        }

        // Debuffs and drowning kill through KillMe, which ignores immunity. A held player stays alive at 1 HP or more.
        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource)
        {
            if (!CutsceneSystem.Freezes(Player))
            {
                return true;
            }

            Player.statLife = Math.Max(Player.statLife, 1);
            return false;
        }

        public override bool CanUseItem(Item item)
        {
            return !CutsceneSystem.Freezes(Player);
        }

        // Runs per drawn frame, so the blend comes from the scene clock rather than a value consumed once per tick.
        public override void ModifyScreenPosition()
        {
            if (!CutsceneSystem.IsParticipant(Player))
            {
                return;
            }

            float blend = CutsceneSystem.BarProgress;
            if (blend <= 0f)
            {
                return;
            }

            Vector2 focusTopLeft = CutsceneSystem.UpdateCameraPoint(Player) - Main.ScreenSize.ToVector2() * 0.5f;
            Main.screenPosition = Vector2.Lerp(Main.screenPosition, focusTopLeft, blend);
        }
    }

    public class CutsceneGlobalNPC : GlobalNPC
    {
        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (CutsceneSystem.IsParticipant(player))
            {
                maxSpawns = 0;
            }
        }
    }
}
