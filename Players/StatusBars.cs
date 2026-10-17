using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp
{
    /// <summary>
    /// Overhead status bars: one small horizontal bar per active buildup meter (Curse, Madness, Frost,
    /// Blight, Miasma, Poison), stacked upward from a fixed bottom slot just above the stamina bar.
    /// Bars stack in arrival order (oldest at the bottom); when one empties the bars above drop down.
    /// A bar that triggers (jumps from 10%+ to 0 in one tick) flashes white in its slot before leaving.
    /// Drawn in the top-right HUD bar style (tsorcRevampSystems DrawBar), pixel-only, with a half-size
    /// buff icon on the left. The classic per-status icons remain behind tsorcRevampVisualConfig.ClassicStatusMeters.
    /// </summary>
    public class StatusBarPlayer : ModPlayer
    {
        /// <summary>One status that can show a bar. Fill is 0-1; OverlayFill is drawn on top in OverlayColor (Powerful Curse).</summary>
        public class StatusBarEntry
        {
            public Func<Player, float> Fill;
            public Func<Player, float> OverlayFill;
            public Color MainColor;
            public Color OverlayColor;
            public Func<int> IconBuffType;

            // Inside of the bar behind the fill. Null = the shared dark interior; set it for fills too dark to read on dark.
            public Color? BackgroundColor;
        }

        // To add a status, add one entry here. Order here doesn't matter; display order is arrival order.
        public static readonly List<StatusBarEntry> Entries = new List<StatusBarEntry>
        {
            // Curse colours are sampled from CurseMeter_full / CurseMeter_powerfulFull. Both levels idle at 1, hence the > 1 checks.
            new StatusBarEntry
            {
                Fill = player => CurseFill(player.GetModPlayer<tsorcRevampPlayer>().CurseLevel, 100f),
                OverlayFill = player => CurseFill(player.GetModPlayer<tsorcRevampPlayer>().PowerfulCurseLevel, 500f),
                MainColor = new Color(248, 0, 118),
                OverlayColor = PowerfulCurseBuildup.PowerfulCurseColor,
                IconBuffType = () => ModContent.BuffType<CurseBuildup>(),
            },
            new StatusBarEntry
            {
                Fill = player => player.GetModPlayer<MadnessPlayer>().MadnessLevel / (float)MadnessBuildup.MaximumBuildup,
                MainColor = new Color(255, 220, 45),
                IconBuffType = () => ModContent.BuffType<MadnessBuildup>(),
            },
            new StatusBarEntry
            {
                Fill = player => player.GetModPlayer<FrostPlayer>().FrostLevel / (float)FrostBuildup.MaximumBuildup,
                MainColor = new Color(75, 175, 255),
                IconBuffType = () => ModContent.BuffType<FrostBuildup>(),
            },
            // Blight keeps its classic near-black (40, 40, 45); a light grey interior gives it the contrast a dark one cannot.
            new StatusBarEntry
            {
                Fill = player => player.GetModPlayer<BlightPlayer>().BlightLevel / (float)BlightBuildup.MaximumBuildup,
                MainColor = new Color(40, 40, 45),
                BackgroundColor = new Color(165, 160, 170),
                IconBuffType = () => ModContent.BuffType<BlightBuildup>(),
            },
            new StatusBarEntry
            {
                Fill = player => player.GetModPlayer<MiasmaPlayer>().MiasmaLevel / (float)MiasmaBuildup.MaximumBuildup,
                MainColor = MiasmaPlayer.MiasmaColor,
                IconBuffType = () => ModContent.BuffType<MiasmaBuildup>(),
            },
            new StatusBarEntry
            {
                Fill = player => player.GetModPlayer<PoisonPlayer>().PoisonLevel / (float)PoisonBuildup.MaximumBuildup,
                MainColor = PoisonPlayer.PoisonColor,
                IconBuffType = () => ModContent.BuffType<PoisonBuildup>(),
            },
            new StatusBarEntry
            {
                Fill = player => player.GetModPlayer<AcidVenomPlayer>().VenomLevel / (float)AcidVenomBuildup.MaximumBuildup,
                MainColor = AcidVenomPlayer.VenomColor,
                IconBuffType = () => ModContent.BuffType<AcidVenomBuildup>(),
            },
        };

        // A drop of 10%+ straight to empty in one tick is a trigger; decay only ever moves 1 point per second.
        private const float TriggerDropThreshold = 0.1f;
        public const int FlashTicks = 24;

        // Entry indices currently on screen, bottom first. Per player and per machine: purely visual.
        internal readonly List<int> DisplayOrder = new List<int>();
        internal readonly int[] FlashTimers = new int[Entries.Count];
        private readonly float[] previousFills = new float[Entries.Count];

        private static float CurseFill(int level, float maximum)
        {
            if (level <= 1)
            {
                return 0f;
            }

            return level / maximum;
        }

        internal static float CombinedFill(StatusBarEntry entry, Player player)
        {
            float fill = MathHelper.Clamp(entry.Fill(player), 0f, 1f);

            if (entry.OverlayFill != null)
            {
                float overlayFill = MathHelper.Clamp(entry.OverlayFill(player), 0f, 1f);
                fill = Math.Max(fill, overlayFill);
            }

            return fill;
        }

        // Runs on every client for every player, so remote players' bars order and flash too.
        public override void PostUpdate()
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < Entries.Count; i++)
            {
                float fill = CombinedFill(Entries[i], Player);
                float drop = previousFills[i] - fill;
                bool triggered = fill <= 0f && drop >= TriggerDropThreshold;

                if (triggered)
                {
                    FlashTimers[i] = FlashTicks;
                }
                else if (FlashTimers[i] > 0)
                {
                    FlashTimers[i]--;
                }

                previousFills[i] = fill;

                bool shown = fill > 0f || FlashTimers[i] > 0;
                bool listed = DisplayOrder.Contains(i);

                if (shown && !listed)
                {
                    DisplayOrder.Add(i);
                }
                else if (!shown && listed)
                {
                    DisplayOrder.Remove(i);
                }
            }
        }

        // Death zeroes every meter; that must not read as six triggers at once on respawn.
        public override void UpdateDead()
        {
            DisplayOrder.Clear();
            Array.Clear(FlashTimers);
            Array.Clear(previousFills);
        }
    }

    internal class StatusBarDrawLayer : PlayerDrawLayer
    {
        // Bottom bar's top edge sits 61px above the player's centre: the overhead stamina bar spans 45..33px,
        // so this leaves a 4px gap above it whether or not stamina is showing. Each further bar is 16px higher.
        private const float BottomBarOffset = 61f;
        private const int BarGap = 4;

        // Same proportions as the HUD bars: 12px tall, 1px black outline, 1px gold inner border (no gold on
        // the bottom), 9px of fill split 2 highlight / 5 main / 2 shadow. 50px of fill = 2 points per pixel.
        private const int BarHeight = 12;
        private const int FillWidth = 50;
        private const int BorderWidth = 2;
        private const int SegmentPercent = 20;

        private const float IconScale = 0.5f;
        private const int IconGap = 2;

        private static readonly Color DefaultInterior = new Color(12, 10, 14, 220);

        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.FrontAccFront);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            if (Main.gameMenu || drawInfo.drawPlayer.dead)
            {
                return false;
            }

            if (ModContent.GetInstance<tsorcRevampVisualConfig>().ClassicStatusMeters)
            {
                return false;
            }

            return drawInfo.drawPlayer.GetModPlayer<StatusBarPlayer>().DisplayOrder.Count > 0;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            StatusBarPlayer statusPlayer = player.GetModPlayer<StatusBarPlayer>();
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            int totalWidth = FillWidth + BorderWidth * 2;
            Vector2 screenCenter = player.Center - Main.screenPosition;
            int left = (int)(screenCenter.X - totalWidth / 2f);
            int bottomTop = (int)(screenCenter.Y - BottomBarOffset);

            for (int slot = 0; slot < statusPlayer.DisplayOrder.Count; slot++)
            {
                int entryIndex = statusPlayer.DisplayOrder[slot];
                StatusBarPlayer.StatusBarEntry entry = StatusBarPlayer.Entries[entryIndex];
                int top = bottomTop - slot * (BarHeight + BarGap);
                int flashTimer = statusPlayer.FlashTimers[entryIndex];

                Color interior = DefaultInterior;

                if (entry.BackgroundColor.HasValue)
                {
                    interior = entry.BackgroundColor.Value;
                }

                DrawCasing(pixel, left, top, totalWidth, interior);

                int fillLeft = left + BorderWidth;
                int fillTop = top + BorderWidth;

                if (flashTimer > 0)
                {
                    // Trigger flash: the bar shows full, white fading back toward the status colour, then fades out.
                    float flashProgress = 1f - flashTimer / (float)StatusBarPlayer.FlashTicks;
                    Color flashColor = Color.Lerp(Color.White, entry.MainColor, flashProgress);
                    float flashOpacity = 1f - flashProgress * 0.6f;
                    DrawFill(pixel, fillLeft, fillTop, FillWidth, flashColor, flashOpacity);
                }
                else
                {
                    int mainWidth = (int)Math.Round(FillWidth * MathHelper.Clamp(entry.Fill(player), 0f, 1f));
                    DrawFill(pixel, fillLeft, fillTop, mainWidth, entry.MainColor);

                    if (entry.OverlayFill != null)
                    {
                        int overlayWidth = (int)Math.Round(FillWidth * MathHelper.Clamp(entry.OverlayFill(player), 0f, 1f));
                        DrawFill(pixel, fillLeft, fillTop, overlayWidth, entry.OverlayColor);
                    }
                }

                // Faint segment lines every 20%, as on the HUD bars.
                for (int percent = SegmentPercent; percent < 100; percent += SegmentPercent)
                {
                    int lineX = fillLeft + FillWidth * percent / 100;
                    Main.spriteBatch.Draw(pixel, new Rectangle(lineX, fillTop, 1, BarHeight - 3), Color.White * 0.15f);
                }

                // Half-size buff icon, right-aligned against the bar's left edge and centred on it vertically.
                Texture2D icon = TextureAssets.Buff[entry.IconBuffType()].Value;
                float iconSize = icon.Width * IconScale;
                Vector2 iconPosition = new Vector2(left - IconGap - iconSize, top + (BarHeight - iconSize) / 2f);
                Main.spriteBatch.Draw(icon, iconPosition, null, Color.White, 0f, Vector2.Zero, IconScale, SpriteEffects.None, 0f);
            }
        }

        // Black outline + gold inner border on top/left/right + dark interior, matching the HUD casing.
        private static void DrawCasing(Texture2D pixel, int left, int top, int width, Color interior)
        {
            Color borderGold = new Color(200, 160, 60);

            Main.spriteBatch.Draw(pixel, new Rectangle(left + 1, top + 1, width - 2, BarHeight - 2), interior);

            Main.spriteBatch.Draw(pixel, new Rectangle(left, top, width, 1), Color.Black);
            Main.spriteBatch.Draw(pixel, new Rectangle(left, top + BarHeight - 1, width, 1), Color.Black);
            Main.spriteBatch.Draw(pixel, new Rectangle(left, top + 1, 1, BarHeight - 2), Color.Black);
            Main.spriteBatch.Draw(pixel, new Rectangle(left + width - 1, top + 1, 1, BarHeight - 2), Color.Black);

            Main.spriteBatch.Draw(pixel, new Rectangle(left + 1, top + 1, width - 2, 1), borderGold);
            Main.spriteBatch.Draw(pixel, new Rectangle(left + 1, top + 2, 1, BarHeight - 3), borderGold);
            Main.spriteBatch.Draw(pixel, new Rectangle(left + width - 2, top + 2, 1, BarHeight - 3), borderGold);
        }

        // Three-tone fill (2px highlight / 5px main / 2px shadow) derived from one colour, so each entry only names one.
        private static void DrawFill(Texture2D pixel, int left, int top, int width, Color mainColor, float opacity = 1f)
        {
            if (width <= 0)
            {
                return;
            }

            // Tones are derived from the opaque colour first, then all three are faded together (premultiplied alpha).
            Color highlight = Color.Lerp(mainColor, Color.White, 0.45f) * opacity;
            Color shadow = Color.Lerp(mainColor, Color.Black, 0.45f) * opacity;
            Color main = mainColor * opacity;

            Main.spriteBatch.Draw(pixel, new Rectangle(left, top, width, 2), highlight);
            Main.spriteBatch.Draw(pixel, new Rectangle(left, top + 2, width, 5), main);
            Main.spriteBatch.Draw(pixel, new Rectangle(left, top + 7, width, 2), shadow);
        }
    }
}
