using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Accessories.Mobility.Wings;

namespace tsorcRevamp.Utilities
{
    internal static class WingTooltipStats
    {
        private const float SpeedToTilesPerSecond = 60f / 16f;
        private const float AccelerationToTilesPerSecondSquared = 60f * 60f / 16f;
        private static readonly ConditionalWeakTable<Item, CachedDetails> DetailsCache = new();

        private sealed class CachedDetails
        {
            internal ulong Frame;
            internal Item FlightItem;
            internal bool Unkindled, BearerOfTheCurse, Souls, Suppressed;
            internal string Culture;
            internal List<TooltipLine> Lines;
        }

        internal static void Add(Item item, List<TooltipLine> tooltips, Mod mod)
        {
            if (!Main.keyState.IsKeyDown(Keys.LeftShift) && !Main.keyState.IsKeyDown(Keys.RightShift))
            {
                tooltips.Add(new TooltipLine(mod, "WingDetailsHint", Text("WingDetailsHint")));
                return;
            }

            Player local = Main.LocalPlayer;
            var localModPlayer = local.GetModPlayer<tsorcRevampPlayer>();
            bool souls = SoulsModeMobility.Enabled(local);
            bool harness1 = item.type == ModContent.ItemType<SupersonicWings>();
            bool harness2 = item.type == ModContent.ItemType<SupersonicWings2>();
            Item flightItem = item;
            if (souls && (harness1 || harness2) && localModPlayer.hasSupersonicHarness && localModPlayer.hasSlottedWing)
            {
                var slot = LoaderManager.Get<AccessorySlotLoader>().Get(ModContent.GetInstance<SupersonicWingSlot>().Type, local);
                if (slot?.FunctionalItem != null && !slot.FunctionalItem.IsAir)
                {
                    flightItem = slot.FunctionalItem;
                }
            }

            // Reuse the preview for half a second while the relevant equipment/mode is unchanged.
            var cache = DetailsCache.GetValue(item, _ => new CachedDetails());
            string culture = Language.ActiveCulture.CultureInfo.Name;
            if (cache.Lines != null && Main.GameUpdateCount >= cache.Frame && Main.GameUpdateCount - cache.Frame < 30
                && cache.FlightItem == flightItem && cache.Unkindled == localModPlayer.Unkindled
                && cache.BearerOfTheCurse == localModPlayer.BearerOfTheCurse && cache.Souls == souls
                && cache.Suppressed == localModPlayer.Suppressed && cache.Culture == culture)
            {
                foreach (var line in cache.Lines)
                    tooltips.Add(new TooltipLine(mod, line.Name, line.Text));
                return;
            }

            // Query movement on an isolated player, never on Main.LocalPlayer. Normal flight only:
            // no hover input, accessory boosts, jump boosts, mounts, or infinite-flight accessories.
            var preview = new Player();
            var previewModPlayer = preview.GetModPlayer<tsorcRevampPlayer>();
            previewModPlayer.Unkindled = localModPlayer.Unkindled;
            previewModPlayer.BearerOfTheCurse = localModPlayer.BearerOfTheCurse;
            previewModPlayer.Suppressed = localModPlayer.Suppressed;
            preview.equippedWings = flightItem.Clone();
            preview.wingsLogic = flightItem.wingSlot;
            preview.gravDir = 1f;
            preview.controlJump = true;
            preview.wingTime = 1000f;

            var stats = preview.GetWingStats(preview.wingsLogic);
            int flightTicks = stats.FlyTime;
            if (flightItem.type == ModContent.ItemType<SupersonicWings>())
                flightTicks = souls ? SoulsModeMobility.HarnessFallbackFlightTimeTier1 : 180;
            else if (flightItem.type == ModContent.ItemType<SupersonicWings2>())
                flightTicks = souls ? SoulsModeMobility.HarnessFallbackFlightTimeTier2 : 1200;
            else if (flightItem.type == ModContent.ItemType<WingsOfSeath>())
                flightTicks = souls ? SoulsModeMobility.WingsOfSeathFlightTime : int.MaxValue;

            bool suppressedWing = flightItem.type == ModContent.ItemType<WingsOfSeath>() || harness1 || harness2;
            if (localModPlayer.Suppressed && suppressedWing)
                flightTicks = Math.Min(flightTicks, flightItem.type == ModContent.ItemType<WingsOfSeath>() ? SoulsModeMobility.WingsOfSeathSuppressedFlightTime : 90);

            preview.accRunSpeed = Math.Max(3f, stats.AccRunSpeedOverride);
            preview.runAcceleration = 0.08f * stats.AccRunAccelerationMult;
            ItemLoader.HorizontalWingSpeeds(preview);

            // Souls Mode: harness and Seath fly at the higher of the Supersonic sprint formula and the wing's own
            // speed (PostUpdateRunSpeeds). Shown with only the item's own moveSpeed bonus, i.e. no other gear.
            if (souls)
            {
                float supersonicBaseSpeed = 0f;
                float supersonicBoostPercent = 0f;
                float supersonicMoveSpeedBonus = 0f;

                if (harness1)
                {
                    supersonicBaseSpeed = SoulsModeMobility.SupersonicWingsBaseSpeed;
                    supersonicBoostPercent = SoulsModeMobility.SupersonicWingsBoostPercent;
                    supersonicMoveSpeedBonus = SoulsModeMobility.SupersonicWingsMoveSpeedBonus;
                }
                else if (harness2)
                {
                    supersonicBaseSpeed = SoulsModeMobility.SupersonicWings2BaseSpeed;
                    supersonicBoostPercent = SoulsModeMobility.SupersonicWings2BoostPercent;
                    supersonicMoveSpeedBonus = SoulsModeMobility.SupersonicWings2MoveSpeedBonus;
                }
                else if (item.type == ModContent.ItemType<WingsOfSeath>())
                {
                    supersonicBaseSpeed = SoulsModeMobility.WingsOfSeathBaseSpeed;
                    supersonicBoostPercent = SoulsModeMobility.WingsOfSeathBoostPercent;
                    supersonicMoveSpeedBonus = SoulsModeMobility.WingsOfSeathMoveSpeedBonus;
                }

                if (supersonicBaseSpeed > 0f)
                {
                    float supersonicSpeed = SoulsModeMobility.SupersonicRunSpeed(supersonicBaseSpeed, supersonicBoostPercent, 1f + supersonicMoveSpeedBonus);
                    preview.accRunSpeed = Math.Max(preview.accRunSpeed, supersonicSpeed);
                }
            }

            // WingMovement includes vanilla's wing-specific ascent rules and modded wing hooks.
            // A fast upward input is clamped to the wing's normal ascent speed limit.
            preview.velocity = new Vector2(0f, -10000f);
            preview.WingMovement();
            float ascentSpeed = Math.Max(0f, -preview.velocity.Y);
            float initialAcceleration = MeasureAscentAcceleration(preview, -0.01f);
            float cruisingAcceleration = MeasureAscentAcceleration(preview, -ascentSpeed * 0.95f);
            float minAcceleration = Math.Min(initialAcceleration, cruisingAcceleration);
            float maxAcceleration = Math.Max(initialAcceleration, cruisingAcceleration);

            int firstDetail = tooltips.Count;
            tooltips.Add(new TooltipLine(mod, "WingDetailsHeading", Text("WingDetailsHeading")));
            if (flightItem != item)
                tooltips.Add(new TooltipLine(mod, "WingDetailsSource", Text("WingDetailsSource", flightItem.Name)));
            string flightTime = flightTicks >= 1000000 ? Text("WingDetailsUnlimited")
                : flightTicks > 0 ? Text("WingDetailsSeconds", Number(flightTicks / 60f)) : Text("WingDetailsCustomTime");
            tooltips.Add(new TooltipLine(mod, "WingFlightTime", Text("WingDetailsFlightTime", flightTime)));
            tooltips.Add(new TooltipLine(mod, "WingHorizontalSpeed", Text("WingDetailsHorizontalSpeed", Number(preview.accRunSpeed * SpeedToTilesPerSecond))));
            tooltips.Add(new TooltipLine(mod, "WingHorizontalAcceleration", Text("WingDetailsHorizontalAcceleration", Number(preview.runAcceleration * AccelerationToTilesPerSecondSquared))));
            tooltips.Add(new TooltipLine(mod, "WingAscentSpeed", Text("WingDetailsAscentSpeed", Number(ascentSpeed * SpeedToTilesPerSecond))));
            string ascentAcceleration = Number(minAcceleration * AccelerationToTilesPerSecondSquared);
            if (Math.Abs(maxAcceleration - minAcceleration) > 0.001f)
                ascentAcceleration = Text("WingDetailsRange", ascentAcceleration, Number(maxAcceleration * AccelerationToTilesPerSecondSquared));
            tooltips.Add(new TooltipLine(mod, "WingAscentAcceleration", Text("WingDetailsAscentAcceleration", ascentAcceleration)));
            cache.Frame = Main.GameUpdateCount;
            cache.FlightItem = flightItem;
            cache.Unkindled = localModPlayer.Unkindled;
            cache.BearerOfTheCurse = localModPlayer.BearerOfTheCurse;
            cache.Souls = souls;
            cache.Suppressed = localModPlayer.Suppressed;
            cache.Culture = culture;
            cache.Lines = tooltips.GetRange(firstDetail, tooltips.Count - firstDetail);
        }

        private static float MeasureAscentAcceleration(Player preview, float velocity)
        {
            preview.velocity = new Vector2(0f, velocity);
            preview.wingTime = 1000f;
            preview.WingMovement();
            return Math.Max(0f, velocity - preview.velocity.Y);
        }

        private static string Number(float value) => value.ToString("0.##", CultureInfo.CurrentCulture);
        private static string Text(string key, params object[] args) => Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip." + key, args);
    }
}
