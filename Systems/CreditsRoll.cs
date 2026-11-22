using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Systems
{
    /// <summary>
    /// The ending: the Epilogue text, the title, then CREDITS.txt scrolling over black to the Ending theme. It is drawn and
    /// timed on each client separately, so one player's Skip never affects another's.
    /// </summary>
    public class CreditsRoll : ModSystem
    {
        private enum Phase : byte
        {
            Idle,
            Delay,
            FadeIn,
            Epilogue,
            Title,
            Scroll,
            Thanks,
            FadeOut,
        }

        private enum RowKind : byte
        {
            Header,
            Entry,
            Note,
        }

        private struct CreditRow
        {
            public RowKind Kind;
            public string Name;
            public string Role;
            public float Y;
        }

        private struct Ember
        {
            public Vector2 Position;
            public float Speed;
            public float Size;
            public float Phase;
        }

        public const int AfterBossDelayTicks = 5 * 60;

        private const string SeenKey = "GwynCredits";
        private const string CreditsFile = "CREDITS.txt";

        private const int FadeInTicks = 120;
        private const int SentenceTicks = 270;
        private const int SentenceFadeTicks = 40;
        private const int TitleTicks = 300;
        private const int ThanksTicks = 360;
        private const int FadeOutTicks = 120;
        private const int SkipDelayTicks = 120;
        private const int HeldJumpSpeed = 4;

        // The Ending theme is 1m 54s. The scroll takes whatever the other phases leave of it.
        private const int MusicTicks = 114 * 60;
        private const float MinScrollSpeed = 0.7f;
        private const float MaxScrollSpeed = 1.6f;

        private const float RowHeight = 26f;
        private const float SectionGap = 44f;
        private const float ColumnGap = 18f;
        private const float RoleColumnWidth = 440f;
        private const float NoteWidth = 760f;
        private const float EpilogueWidth = 720f;
        private const int EmberCount = 60;

        private static readonly Color HeaderColor = new Color(232, 176, 82);
        private static readonly Color NameColor = Color.White;
        private static readonly Color RoleColor = new Color(180, 174, 188);
        private static readonly Color TitleColor = new Color(214, 62, 50);

        // The in-game font has no kana outside Chinese, so this credit needs a readable fallback.
        private static readonly Dictionary<string, string> FontSafeNames = new Dictionary<string, string>
        {
            { "ヨーグルト", "Yogurt" },
        };

        private static Phase phase = Phase.Idle;
        private static int phaseTimer;
        private static int shownTicks;
        private static float scrollOffset;
        private static float scrollSpeed = 1f;
        private static int sentenceIndex;
        private static float fadeOutStartAlpha;

        private static readonly List<CreditRow> rows = new List<CreditRow>();
        private static readonly List<string> sentences = new List<string>();
        private static float contentHeight;
        private static string layoutCulture = "";
        private static int musicSlot = MusicID.Credits;

        private static readonly Ember[] embers = new Ember[EmberCount];

        /// <summary>True from the first fade until the world is back: the local player is held and the HUD is replaced.</summary>
        public static bool IsShowing => phase != Phase.Idle && phase != Phase.Delay;

        public static bool IsQueuedOrShowing => phase != Phase.Idle;

        public override void PostSetupContent()
        {
            musicSlot = MusicID.Credits;

            string[] musicMods = { "tsorcMusic", "tsorcXelvaaMusic" };
            foreach (string modName in musicMods)
            {
                bool hasTrack = ModLoader.TryGetMod(modName, out Mod musicMod) && MusicLoader.MusicExists(musicMod, "Sounds/Music/Ending");
                if (hasTrack)
                {
                    musicSlot = MusicLoader.GetMusicSlot(musicMod, "Sounds/Music/Ending");
                    break;
                }
            }
        }

        public static int MusicSlot => musicSlot;

        /// <summary>Server or single player, on Gwyn's first defeat in this world.</summary>
        public static void StartAfterGwyn()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || tsorcRevampWorld.PrimordialAge || CutsceneSystem.HasSeen(SeenKey))
            {
                return;
            }

            CutsceneSystem.MarkSeen(SeenKey);
            StartForEveryone();
        }

        public static void StartForEveryone()
        {
            if (Main.netMode == NetmodeID.Server)
            {
                CutsceneSystem.SendCreditsStart();
                return;
            }

            Queue(AfterBossDelayTicks);
        }

        /// <summary>Starts this client's credits after a delay. Ignored if they are already on their way.</summary>
        public static void Queue(int delayTicks)
        {
            if (Main.dedServ || IsQueuedOrShowing)
            {
                return;
            }

            phase = Phase.Delay;
            phaseTimer = delayTicks;
            shownTicks = 0;
            scrollOffset = 0f;
            sentenceIndex = 0;
        }

        private static void Enter(Phase nextPhase)
        {
            phase = nextPhase;
            phaseTimer = 0;
        }

        private static DynamicSpriteFont BodyFont
        {
            get
            {
                // Matches soapstones: the default font does not cover Chinese well.
                if (Language.ActiveCulture.Name == "zh-Hans")
                {
                    return FontAssets.ItemStack.Value;
                }

                return FontAssets.MouseText.Value;
            }
        }

        private static string[] WrapToLines(string text, DynamicSpriteFont font, float width)
        {
            string wrapped = UsefulFunctions.WrapString(text, font, width).TrimStart().Replace("\n ", "\n");
            return wrapped.Split('\n');
        }

        /// <summary>Reads the credits file and the localized epilogue into rows, once per language.</summary>
        private void BuildLayout()
        {
            string culture = Language.ActiveCulture.Name;
            if (layoutCulture == culture)
            {
                return;
            }

            layoutCulture = culture;
            rows.Clear();
            sentences.Clear();
            DynamicSpriteFont font = BodyFont;

            // The first line of the Epilogue is the "true ending" notice, which the scene itself makes redundant.
            string epilogue = LangUtils.GetTextValue("Items.Epilogue.Obscured");
            int firstBreak = epilogue.IndexOf('\n');
            if (firstBreak >= 0)
            {
                epilogue = epilogue.Substring(firstBreak + 1);
            }

            epilogue = epilogue.Replace("\r", "").Replace("\n", " ");
            foreach (string sentence in Regex.Split(epilogue, @"(?<=[.!?])\s+|(?<=[。！？])"))
            {
                string trimmed = sentence.Trim();
                if (trimmed.Length > 0)
                {
                    sentences.Add(trimmed);
                }
            }

            byte[] fileBytes = Mod.GetFileBytes(CreditsFile);
            float cursorY = 0f;
            if (fileBytes == null)
            {
                contentHeight = 0f;
                return;
            }

            string[] fileLines = Encoding.UTF8.GetString(fileBytes).TrimStart('﻿').Replace("\r", "").Split('\n');
            for (int i = 0; i < fileLines.Length; i++)
            {
                string line = fileLines[i].Trim();
                if (line.Length == 0 || line.StartsWith("---"))
                {
                    continue;
                }

                if (line.StartsWith("*"))
                {
                    foreach (string noteLine in WrapToLines(line.Substring(1), font, NoteWidth))
                    {
                        rows.Add(new CreditRow { Kind = RowKind.Note, Name = noteLine, Y = cursorY });
                        cursorY += RowHeight;
                    }
                    continue;
                }

                // The last separator is the real one: some names contain a slash of their own.
                int separator = line.LastIndexOf(" / ", StringComparison.Ordinal);
                if (separator < 0)
                {
                    if (rows.Count > 0)
                    {
                        cursorY += SectionGap;
                    }

                    rows.Add(new CreditRow { Kind = RowKind.Header, Name = line, Y = cursorY });
                    cursorY += RowHeight * 1.5f;
                    continue;
                }

                string name = line.Substring(0, separator).Trim();
                if (FontSafeNames.TryGetValue(name, out string safeName))
                {
                    name = safeName;
                }

                string role = line.Substring(separator + 3).Trim();
                string[] roleLines = WrapToLines(role, font, RoleColumnWidth);
                for (int rowIndex = 0; rowIndex < roleLines.Length; rowIndex++)
                {
                    string rowName = "";
                    if (rowIndex == 0)
                    {
                        rowName = name;
                    }

                    rows.Add(new CreditRow { Kind = RowKind.Entry, Name = rowName, Role = roleLines[rowIndex], Y = cursorY });
                    cursorY += RowHeight;
                }
            }

            contentHeight = cursorY;
        }

        public override void PostUpdateEverything()
        {
            if (Main.dedServ || phase == Phase.Idle)
            {
                return;
            }

            if (phase == Phase.Delay)
            {
                phaseTimer--;
                if (phaseTimer <= 0)
                {
                    BuildLayout();
                    SeedEmbers();

                    // Every phase but the scroll has a fixed length, so the scroll is paced to end with the song.
                    int fixedTicks = FadeInTicks + sentences.Count * SentenceTicks + TitleTicks + ThanksTicks + FadeOutTicks;
                    int scrollTicks = Math.Max(1, MusicTicks - fixedTicks);
                    float travel = contentHeight + Main.screenHeight / Main.UIScale;
                    scrollSpeed = MathHelper.Clamp(travel / scrollTicks, MinScrollSpeed, MaxScrollSpeed);
                    Enter(Phase.FadeIn);
                }
                return;
            }

            shownTicks++;
            UpdateEmbers();

            // Holding Jump speeds the story along without skipping it. The fades keep their pace.
            bool jumpHeld = PlayerInput.Triggers.Current.Jump && !Main.drawingPlayerChat;
            int advance = 1;
            if (jumpHeld && phase != Phase.FadeIn && phase != Phase.FadeOut)
            {
                advance = HeldJumpSpeed;
            }

            phaseTimer += advance;

            switch (phase)
            {
                case Phase.FadeIn:
                    if (phaseTimer >= FadeInTicks)
                    {
                        Enter(Phase.Epilogue);
                    }
                    break;

                case Phase.Epilogue:
                    if (phaseTimer >= SentenceTicks)
                    {
                        sentenceIndex++;
                        phaseTimer = 0;
                        if (sentenceIndex >= sentences.Count)
                        {
                            Enter(Phase.Title);
                        }
                    }
                    break;

                case Phase.Title:
                    if (phaseTimer >= TitleTicks)
                    {
                        Enter(Phase.Scroll);
                    }
                    break;

                case Phase.Scroll:
                    scrollOffset += scrollSpeed * advance;
                    if (scrollOffset >= contentHeight + Main.screenHeight / Main.UIScale)
                    {
                        Enter(Phase.Thanks);
                    }
                    break;

                case Phase.Thanks:
                    if (phaseTimer >= ThanksTicks)
                    {
                        BeginFadeOut(1f);
                    }
                    break;

                case Phase.FadeOut:
                    if (phaseTimer >= FadeOutTicks)
                    {
                        Enter(Phase.Idle);
                    }
                    break;
            }
        }

        private static void BeginFadeOut(float startAlpha)
        {
            fadeOutStartAlpha = startAlpha;
            Enter(Phase.FadeOut);
        }

        private static void SeedEmbers()
        {
            float uiWidth = Main.screenWidth / Main.UIScale;
            float uiHeight = Main.screenHeight / Main.UIScale;
            for (int i = 0; i < embers.Length; i++)
            {
                embers[i].Position = new Vector2(Main.rand.NextFloat(uiWidth), Main.rand.NextFloat(uiHeight));
                embers[i].Speed = Main.rand.NextFloat(0.15f, 0.55f);
                embers[i].Size = Main.rand.NextFloat(1.5f, 3.5f);
                embers[i].Phase = Main.rand.NextFloat(MathHelper.TwoPi);
            }
        }

        private static void UpdateEmbers()
        {
            float uiWidth = Main.screenWidth / Main.UIScale;
            float uiHeight = Main.screenHeight / Main.UIScale;
            for (int i = 0; i < embers.Length; i++)
            {
                embers[i].Position.Y -= embers[i].Speed;
                embers[i].Position.X += (float)Math.Sin(shownTicks * 0.012f + embers[i].Phase) * 0.18f;
                if (embers[i].Position.Y < -6f)
                {
                    embers[i].Position = new Vector2(Main.rand.NextFloat(uiWidth), uiHeight + 6f);
                }
            }
        }

        /// <summary>Opacity of the black backdrop. It reaches full black before any text appears.</summary>
        private static float BackdropAlpha
        {
            get
            {
                if (phase == Phase.FadeIn)
                {
                    return MathHelper.Clamp(phaseTimer / (float)FadeInTicks, 0f, 1f);
                }

                if (phase == Phase.FadeOut)
                {
                    return fadeOutStartAlpha * (1f - MathHelper.Clamp(phaseTimer / (float)FadeOutTicks, 0f, 1f));
                }

                return 1f;
            }
        }

        private static float TimedFade(int timer, int totalTicks, int fadeTicks)
        {
            float fadeUp = timer / (float)fadeTicks;
            float fadeDown = (totalTicks - timer) / (float)fadeTicks;
            return MathHelper.Clamp(Math.Min(fadeUp, fadeDown), 0f, 1f);
        }

        /// <summary>Replaces the whole HUD with the credits, keeping vanilla's logic layers and the cursor for Skip.</summary>
        internal static void ApplyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int cursorLayerIndex = layers.Count;

            for (int i = 0; i < layers.Count; i++)
            {
                GameInterfaceLayer layer = layers[i];
                if (layer.Name == "Vanilla: Cursor")
                {
                    cursorLayerIndex = i;
                }
                else if (!layer.Name.Contains("Interface Logic"))
                {
                    layer.Active = false;
                }
            }

            layers.Insert(cursorLayerIndex, new LegacyGameInterfaceLayer("tsorcRevamp: Credits Backdrop", DrawBackdrop, InterfaceScaleType.None));
            layers.Insert(cursorLayerIndex + 1, new LegacyGameInterfaceLayer("tsorcRevamp: Credits", DrawCredits, InterfaceScaleType.UI));
        }

        private static bool DrawBackdrop()
        {
            Rectangle fullScreen = new Rectangle(0, 0, Main.screenWidth, Main.screenHeight);
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, fullScreen, new Rectangle(0, 0, 1, 1), Color.Black * BackdropAlpha);
            return true;
        }

        private static void DrawCentered(DynamicSpriteFont font, string text, float centerX, float y, Color color, float alpha, float scale = 1f)
        {
            Vector2 size = font.MeasureString(text) * scale;
            Utils.DrawBorderStringFourWay(Main.spriteBatch, font, text, centerX - size.X * 0.5f, y, color * alpha, Color.Black * alpha, Vector2.Zero, scale);
        }

        private static bool DrawCredits()
        {
            SpriteBatch spriteBatch = Main.spriteBatch;
            DynamicSpriteFont font = BodyFont;
            float uiWidth = Main.screenWidth / Main.UIScale;
            float uiHeight = Main.screenHeight / Main.UIScale;
            float centerX = uiWidth * 0.5f;
            float alpha = BackdropAlpha;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle pixelSource = new Rectangle(0, 0, 1, 1);
            for (int i = 0; i < embers.Length; i++)
            {
                float flicker = 0.45f + 0.35f * (float)Math.Sin(shownTicks * 0.05f + embers[i].Phase);
                int size = (int)embers[i].Size;
                Rectangle emberRect = new Rectangle((int)embers[i].Position.X, (int)embers[i].Position.Y, size, size);
                spriteBatch.Draw(pixel, emberRect, pixelSource, new Color(255, 120, 45) * (flicker * alpha));
            }

            switch (phase)
            {
                case Phase.Epilogue:
                    if (sentenceIndex < sentences.Count)
                    {
                        float sentenceAlpha = TimedFade(phaseTimer, SentenceTicks, SentenceFadeTicks);
                        string[] sentenceLines = WrapToLines(sentences[sentenceIndex], font, EpilogueWidth);
                        float lineY = uiHeight * 0.5f - sentenceLines.Length * RowHeight * 0.5f;
                        foreach (string sentenceLine in sentenceLines)
                        {
                            DrawCentered(font, sentenceLine, centerX, lineY, Color.White, sentenceAlpha);
                            lineY += RowHeight;
                        }
                    }
                    break;

                case Phase.Title:
                {
                    float titleAlpha = TimedFade(phaseTimer, TitleTicks, 60);
                    string title = LangUtils.GetTextValue("Cutscenes.Credits.Title");
                    DynamicSpriteFont titleFont = FontAssets.DeathText.Value;
                    float titleScale = 1f;
                    if (Language.ActiveCulture.Name == "zh-Hans")
                    {
                        titleFont = font;
                        titleScale = 2f;
                    }

                    float titleY = uiHeight * 0.5f - titleFont.MeasureString(title).Y * titleScale * 0.5f;
                    DrawCentered(titleFont, title, centerX, titleY, TitleColor, titleAlpha, titleScale);
                    break;
                }

                case Phase.Scroll:
                    foreach (CreditRow row in rows)
                    {
                        float rowY = uiHeight + row.Y - scrollOffset;
                        bool onScreen = rowY > -RowHeight * 2f && rowY < uiHeight + RowHeight;
                        if (!onScreen)
                        {
                            continue;
                        }

                        if (row.Kind == RowKind.Header)
                        {
                            DrawCentered(font, row.Name, centerX, rowY, HeaderColor, 1f, 1.15f);
                        }
                        else if (row.Kind == RowKind.Note)
                        {
                            DrawCentered(font, row.Name, centerX, rowY, RoleColor, 1f);
                        }
                        else
                        {
                            // Film-style columns: names end at the center line and their roles begin just after it.
                            if (row.Name.Length > 0)
                            {
                                float nameWidth = font.MeasureString(row.Name).X;
                                Utils.DrawBorderStringFourWay(spriteBatch, font, row.Name, centerX - ColumnGap - nameWidth, rowY, NameColor, Color.Black, Vector2.Zero);
                            }

                            Utils.DrawBorderStringFourWay(spriteBatch, font, row.Role, centerX + ColumnGap, rowY, RoleColor, Color.Black, Vector2.Zero);
                        }
                    }
                    break;

                case Phase.Thanks:
                {
                    float thanksAlpha = TimedFade(phaseTimer, ThanksTicks, 60);
                    string thanks = LangUtils.GetTextValue("Cutscenes.Credits.Thanks");
                    DrawCentered(font, thanks, centerX, uiHeight * 0.5f - RowHeight * 0.5f, Color.White, thanksAlpha, 1.3f);
                    break;
                }
            }

            bool skipAvailable = shownTicks >= SkipDelayTicks && phase != Phase.FadeOut;
            if (!skipAvailable)
            {
                return true;
            }

            string skipText = LangUtils.GetTextValue("Cutscenes.UI.Skip");
            Vector2 skipSize = font.MeasureString(skipText);
            Vector2 skipPosition = new Vector2(uiWidth - skipSize.X - 32f, uiHeight - skipSize.Y - 16f);
            Rectangle skipArea = new Rectangle((int)skipPosition.X - 6, (int)skipPosition.Y - 2, (int)skipSize.X + 12, (int)skipSize.Y + 4);
            bool hovered = skipArea.Contains(new Point(Main.mouseX, Main.mouseY));

            if (hovered)
            {
                Main.LocalPlayer.mouseInterface = true;
                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    Main.mouseLeftRelease = false;
                    BeginFadeOut(alpha);
                }
            }

            Color skipColor = Color.LightGray;
            if (hovered)
            {
                skipColor = Color.White;
            }

            Utils.DrawBorderStringFourWay(spriteBatch, font, skipText, skipPosition.X, skipPosition.Y, skipColor * alpha, Color.Black * alpha, Vector2.Zero);
            return true;
        }

        public override void OnWorldUnload()
        {
            phase = Phase.Idle;
            phaseTimer = 0;
        }
    }

    /// <summary>Plays the Ending theme for as long as this client's credits are on screen, above boss and biome music.</summary>
    public class CreditsMusicScene : ModSceneEffect
    {
        public override int Music => CreditsRoll.MusicSlot;
        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

        public override bool IsSceneEffectActive(Player player)
        {
            return CreditsRoll.IsShowing;
        }
    }
}
