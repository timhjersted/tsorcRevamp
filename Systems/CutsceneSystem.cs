using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace tsorcRevamp.Systems
{
    public enum CutsceneFocusMode : byte
    {
        FixedPoint,
        Npc,
        MidpointOfNpcs,
        MidpointLocalPlayerAndNpc,
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
    }

    /// <summary>
    /// Reusable synced cutscene frame: player freeze and immunity, letterbox bars, hidden HUD, camera focus and no enemy
    /// spawns. It knows nothing about bosses: a script moves the actors (ordinary synced NPCs) and calls Start / Stop on the
    /// server or in single player; clients follow the packet.
    /// </summary>
    public class CutsceneSystem : ModSystem
    {
        private const byte PacketStart = 0;
        private const byte PacketStop = 1;
        private const int BarEaseTicks = 20;
        private const float BarScreenFraction = 0.11f;
        private const int ClientExpiryGraceTicks = 30;
        private const float ProjectileClearRange = 3000f;

        public static bool IsActive { get; private set; }
        public static string SceneId { get; private set; } = "";
        public static int Timer { get; private set; }
        public static int Duration { get; private set; }
        public static CutsceneFocus Focus { get; private set; }
        public static bool FreezesPlayers => IsActive;

        private static int anchorNpc = -1;

        /// <summary>Starts a scene (server / single player only). The scene ends at durationTicks, on Stop, or when the
        /// anchor NPC disappears. Hostile projectiles near the anchor are killed through Kill() so their effects play.</summary>
        public static void Start(string sceneId, int durationTicks, CutsceneFocus focus, int anchorNpcWhoAmI = -1)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Begin(sceneId, durationTicks, focus, anchorNpcWhoAmI);
            ClearHostileProjectiles();

            if (Main.netMode != NetmodeID.Server)
            {
                return;
            }

            ModPacket packet = ModContent.GetInstance<tsorcRevamp>().GetPacket();
            packet.Write(tsorcPacketID.CutsceneSync);
            packet.Write(PacketStart);
            packet.Write(sceneId);
            packet.Write(durationTicks);
            packet.Write((byte)focus.Mode);
            packet.WriteVector2(focus.Point);
            packet.Write((short)focus.NpcA);
            packet.Write((short)focus.NpcB);
            packet.Write((short)anchorNpcWhoAmI);
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
                ModPacket packet = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                packet.Write(tsorcPacketID.CutsceneSync);
                packet.Write(PacketStop);
                packet.Send();
            }
        }

        public static void ReceivePacket(BinaryReader reader)
        {
            byte action = reader.ReadByte();
            if (action == PacketStop)
            {
                End();
                return;
            }

            string sceneId = reader.ReadString();
            int duration = reader.ReadInt32();
            CutsceneFocus focus = new CutsceneFocus
            {
                Mode = (CutsceneFocusMode)reader.ReadByte(),
                Point = reader.ReadVector2(),
                NpcA = reader.ReadInt16(),
                NpcB = reader.ReadInt16(),
            };
            int anchor = reader.ReadInt16();
            Begin(sceneId, duration, focus, anchor);
        }

        private static void Begin(string sceneId, int durationTicks, CutsceneFocus focus, int anchorNpcWhoAmI)
        {
            IsActive = true;
            SceneId = sceneId;
            Duration = durationTicks;
            Timer = 0;
            Focus = focus;
            anchorNpc = anchorNpcWhoAmI;
        }

        private static void End()
        {
            IsActive = false;
            SceneId = "";
            Timer = 0;
            Duration = 0;
            anchorNpc = -1;
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
            }

            return viewer.Center;
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

            bool anchorGone = anchorNpc >= 0 && !Main.npc[anchorNpc].active;
            if (Timer >= Duration || anchorGone)
            {
                Stop(SceneId);
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

        /// <summary>Hides every interface layer and adds the bars. Called last from tsorcRevampSystems.ModifyInterfaceLayers
        /// so the mod's own layers are hidden too. Vanilla's Interface Logic layers keep running: they update UI state.</summary>
        public static void ApplyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (!IsActive)
            {
                return;
            }

            foreach (GameInterfaceLayer layer in layers)
            {
                if (!layer.Name.Contains("Interface Logic"))
                {
                    layer.Active = false;
                }
            }

            layers.Add(new LegacyGameInterfaceLayer("tsorcRevamp: Cutscene Bars", DrawBars, InterfaceScaleType.None));
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
    }

    /// <summary>The freeze, immunity and camera are player-local, so each client applies them to its own player.</summary>
    public class CutscenePlayer : ModPlayer
    {
        private const float FrozenFriction = 0.8f;

        public override void SetControls()
        {
            if (!CutsceneSystem.FreezesPlayers)
            {
                return;
            }

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

        public override void PreUpdate()
        {
            if (!CutsceneSystem.FreezesPlayers)
            {
                return;
            }

            Player.itemAnimation = 0;
            Player.itemTime = 0;
            Player.channel = false;
        }

        public override void PreUpdateMovement()
        {
            if (CutsceneSystem.FreezesPlayers)
            {
                Player.velocity.X *= FrozenFriction;
            }
        }

        public override void PostUpdateMiscEffects()
        {
            if (!CutsceneSystem.FreezesPlayers)
            {
                return;
            }

            Player.immune = true;
            Player.immuneTime = Math.Max(Player.immuneTime, 2);
            Player.immuneNoBlink = true;
        }

        public override bool CanUseItem(Item item)
        {
            return !CutsceneSystem.FreezesPlayers;
        }

        // Runs per drawn frame, so the blend comes from the scene clock rather than a value consumed once per tick.
        public override void ModifyScreenPosition()
        {
            float blend = CutsceneSystem.BarProgress;
            if (blend <= 0f)
            {
                return;
            }

            Vector2 focusTopLeft = CutsceneSystem.FocusPoint(Player) - Main.ScreenSize.ToVector2() * 0.5f;
            Main.screenPosition = Vector2.Lerp(Main.screenPosition, focusTopLeft, blend);
        }
    }

    public class CutsceneGlobalNPC : GlobalNPC
    {
        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (CutsceneSystem.IsActive)
            {
                maxSpawns = 0;
            }
        }
    }
}
