using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp
{
    /// <summary>
    /// Phase 7: when the Active Shields Revamp is live (config on + local player in SoulsMode), describe a
    /// registered shield's active-blocking behavior on its tooltip, and surgically remove the passive
    /// damage-reduction / move-speed / thorns lines that no longer apply in active mode. When the toggle is
    /// off or the player isn't in SoulsMode, the shield's normal (passive) tooltip is left untouched.
    /// Strings live in Localization (Mods.tsorcRevamp.ActiveShield.*).
    /// </summary>
    public class ActiveShieldGlobalItem : GlobalItem
    {
        private const string Key = "Mods.tsorcRevamp.ActiveShield.";

        // In active mode, swap a mod shield's large passive defense for its small active-mode value.
        // Net contribution from this item = ActiveDefense (the -item.defense cancels vanilla's auto-add,
        // regardless of hook order). Vanilla shields and wards (ActiveDefense 0) keep their own defense.
        public override void UpdateEquip(Item item, Player player)
        {
            if (!tsorcRevampActiveShieldPlayer.ActiveFor(player) || tsorcRevamp.ActiveShieldRegistry == null)
            {
                return;
            }
            if (!tsorcRevamp.ActiveShieldRegistry.TryGetValue(item.type, out ActiveShieldData data))
            {
                return;
            }
            if (data.ActiveDefense > 0)
            {
                player.statDefense -= item.defense;
                player.statDefense += data.ActiveDefense;
            }
            // Greatshield "equip load": an always-on regen tax that applies whether or not you ever raise it.
            // Safe here (UpdateEquip runs before the stamina system's PostUpdateMiscEffects, same as armor);
            // do NOT move it to PostUpdateRunSpeeds, which is after the regen calc.
            if (data.PassiveRegenPenalty > 0f)
            {
                player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult -= data.PassiveRegenPenalty;
            }
        }

        // One shield at a time (active mode only): can't equip a registered shield in an accessory slot while
        // another registered shield is in a different accessory slot OR in the Right-Click (2nd) slot.
        public override bool CanEquipAccessory(Item item, Player player, int slot, bool modded)
        {
            if (!tsorcRevampActiveShieldPlayer.ActiveFor(player) || tsorcRevamp.ActiveShieldRegistry == null
                || !tsorcRevamp.ActiveShieldRegistry.ContainsKey(item.type))
            {
                return true;
            }
            for (int i = 3; i < 8 + player.extraAccessorySlots; i++)
            {
                if (i == slot)
                {
                    continue;
                }
                Item acc = player.armor[i];
                if (acc != null && !acc.IsAir && tsorcRevamp.ActiveShieldRegistry.ContainsKey(acc.type))
                {
                    return false;
                }
            }
            Item rc = player.GetModPlayer<tsorcRevampPlayer>().RightClickSlot?.Item;
            if (rc != null && !rc.IsAir && tsorcRevamp.ActiveShieldRegistry.ContainsKey(rc.type))
            {
                return false;
            }
            return true;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (!tsorcRevampActiveShieldPlayer.RevampActive)
            {
                return;
            }
            if (tsorcRevamp.ActiveShieldRegistry == null
                || !tsorcRevamp.ActiveShieldRegistry.TryGetValue(item.type, out ActiveShieldData data))
            {
                return;
            }
            if (Main.LocalPlayer == null || !Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SoulsMode)
            {
                return;
            }

            // Surgically drop the now-inaccurate passive description lines (% DR, move-speed penalty, passive
            // thorns, the dropped non-melee damage penalty, and the "melee only" flavor). These are shared
            // CommonItemTooltip tokens, so we match by their localized prefix and work across languages. Vanilla
            // shields' text doesn't contain these phrases, so it's left intact.
            RemoveStalePassiveLines(tooltips);

            // When this shield grants its own active-mode defense, drop vanilla's auto-generated "N defense" line —
            // it shows the item's old (larger) Item.defense, which we've already swapped for ActiveDefense, so it
            // would otherwise contradict the "Grants N defense" line we add below.
            if (data.ActiveDefense > 0)
            {
                tooltips.RemoveAll(line => line.Mod == "Terraria" && line.Name == "Defense");
            }

            string resource = Language.GetTextValue(Key + (data.Resource == ShieldResource.Stamina ? "Stamina" : "Mana"));
            int pct = (int)Math.Round(data.DamageFactor * 100f);
            Color headerColor = new Color(120, 200, 255);
            Color bodyColor = new Color(180, 220, 245);
            Color highlightColor = new Color(255, 223, 80);
            Color hintColor = new Color(140, 160, 175);

            // Passive defense goes UP with the item's own passive lines rather than under the [Active Shield]
            // header: it applies whenever the shield is equipped, not only while the guard is raised, and sitting
            // under the active header it read as active-only. Inserted after the last of the item's description
            // lines (Tooltip0..N). Only mod shields reach this — vanilla shields (ActiveDefense 0) keep their own
            // native defense line, which is already in the right place.
            if (data.ActiveDefense > 0)
            {
                TooltipLine defenseLine = new TooltipLine(Mod, "ActiveShieldDefense", Language.GetTextValue(Key + "Defense", data.ActiveDefense)) { OverrideColor = bodyColor };
                int lastPassiveLine = tooltips.FindLastIndex(line => line.Name != null && line.Name.StartsWith("Tooltip"));

                if (lastPassiveLine != -1)
                {
                    tooltips.Insert(lastPassiveLine + 1, defenseLine);
                }
                else
                {
                    tooltips.Add(defenseLine);
                }
            }

            // --- [Active Shield] section ---------------------------------------------------------------------
            // Only this shield's own numbers and its signature ability stay on the face of the tooltip. The rules
            // that read word-for-word identically on every registered shield (how to raise it, the enemy wall,
            // guard break, perfect parry, raised-regen) fold behind Left Shift — repeating them on all 19 shields
            // is what turned this block into a wall of text. Same reveal pattern as the Runeterra weapons.
            tooltips.Add(new TooltipLine(Mod, "ActiveShieldHeader", Language.GetTextValue(Key + "Header")) { OverrideColor = headerColor });
            tooltips.Add(new TooltipLine(Mod, "ActiveShieldCost", Language.GetTextValue(Key + "Cost", (int)data.BaseCost, resource, pct)) { OverrideColor = bodyColor });

            int slowPct = (int)Math.Round((1f - data.MoveSpeedMult) * 100f);
            if (slowPct > 0)
            {
                tooltips.Add(new TooltipLine(Mod, "ActiveShieldSlow", Language.GetTextValue(Key + "Slow", slowPct)) { OverrideColor = bodyColor });
            }
            // Chip: the greatshield's "none gets through" is the archetype's selling point, so it gets the
            // highlight color; every other shield shows its actual leak rate as an ordinary stat.
            if (data.IsGreatshield)
            {
                tooltips.Add(new TooltipLine(Mod, "ActiveShieldGreatshield", Language.GetTextValue(Key + "Greatshield")) { OverrideColor = highlightColor });
            }
            else
            {
                int chipPct = (int)Math.Round(data.ResolvedChipFraction * 100f);
                tooltips.Add(new TooltipLine(Mod, "ActiveShieldChip", Language.GetTextValue(Key + "Chip", chipPct)) { OverrideColor = bodyColor });
            }
            if (data.PassiveRegenPenalty > 0f)
            {
                int penaltyPct = (int)Math.Round(data.PassiveRegenPenalty * 100f);
                tooltips.Add(new TooltipLine(Mod, "ActiveShieldRegenPenalty", Language.GetTextValue(Key + "RegenPenalty", penaltyPct)) { OverrideColor = bodyColor });
            }

            // Per-shield signature effect. Yellow sets these apart from the common block stats.
            string effectKey = EffectKey(item.type);
            if (effectKey != null)
            {
                tooltips.Add(new TooltipLine(Mod, "ActiveShieldEffect", Language.GetTextValue(Key + effectKey)) { OverrideColor = new Color(255, 223, 0) });
            }

            // The shared rules are APPENDED, never inserted, so holding Shift only ever adds lines underneath what
            // is already on screen instead of reshuffling the ones the player is mid-read of.
            if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift))
            {
                // "Right mouse click" is only the DEFAULT: both the 2nd Slot key and the optional Raise Shield
                // key are rebindable, so resolve the live bindings instead of hardcoding either. The dedicated
                // key leads when it is bound, because unlike right-click it works with any weapon in hand.
                string secondSlotKeyText = tsorcRevampSystems.GetSecondSlotBindingText();
                string raiseShieldKeyText = tsorcRevampSystems.GetRaiseShieldBindingText();
                string inputText;

                if (raiseShieldKeyText == null)
                {
                    inputText = Language.GetTextValue(Key + "UseInput", secondSlotKeyText);
                }
                else
                {
                    inputText = Language.GetTextValue(Key + "UseInputBound", raiseShieldKeyText, secondSlotKeyText);
                }

                tooltips.Add(new TooltipLine(Mod, "ActiveShieldUse", Language.GetTextValue(Key + "Use", inputText)) { OverrideColor = bodyColor });
                tooltips.Add(new TooltipLine(Mod, "ActiveShieldRebind", Language.GetTextValue(Key + "Rebind")) { OverrideColor = hintColor });
                tooltips.Add(new TooltipLine(Mod, "ActiveShieldWall", Language.GetTextValue(Key + "Wall")) { OverrideColor = bodyColor });
                tooltips.Add(new TooltipLine(Mod, "ActiveShieldBreak", Language.GetTextValue(Key + "Break", resource)) { OverrideColor = bodyColor });
                tooltips.Add(new TooltipLine(Mod, "ActiveShieldParry", Language.GetTextValue(Key + "Parry")) { OverrideColor = highlightColor });

                if (data.Resource == ShieldResource.Stamina)
                {
                    int raisedRegenPct = (int)Math.Round(tsorcRevampActiveShieldPlayer.BlockStaminaRegenMult * 100f);
                    tooltips.Add(new TooltipLine(Mod, "ActiveShieldRegen", Language.GetTextValue(Key + "Regen", raisedRegenPct)) { OverrideColor = bodyColor });
                }
            }
            else
            {
                tooltips.Add(new TooltipLine(Mod, "ActiveShieldDetails", Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.Details")) { OverrideColor = hintColor });
            }
        }

        // Maps a shield to its signature-effect localization key (Mods.tsorcRevamp.ActiveShield.<key>), or null.
        private static string EffectKey(int type)
        {
            if (type == ModContent.ItemType<Items.Accessories.Defensive.Shields.SpikedIronShield>()
                || type == ModContent.ItemType<Items.Accessories.Defensive.Shields.AncientDemonShield>())
            {
                return "ReflectEffect";
            }
            if (type == ModContent.ItemType<Items.Accessories.Melee.GazingShield>()) return "Watchful";
            if (type == ModContent.ItemType<Items.Accessories.Melee.BeholderShield>()) return "GazeConfuse";
            if (type == ModContent.ItemType<Items.Accessories.Melee.BeholderShield2>()) return "GazePetrify";
            if (type == ModContent.ItemType<Items.Accessories.Melee.EnchantedBeholderShield2>()) return "GazeBeam";
            if (type == ModContent.ItemType<Items.Accessories.Defensive.Shields.DragonCrestShield>()) return "FireCone";
            if (type == ModContent.ItemType<Items.Accessories.Damage.MythrilBulwark>()) return "BulwarkEffect";
            if (type == ModContent.ItemType<Items.Accessories.Defensive.Shields.IceboundMythrilAegis>()) return "FrostNova";
            if (type == ModContent.ItemType<Items.Accessories.Defensive.Shields.ManaShield>()) return "WardPulse";
            if (type == ModContent.ItemType<Items.Accessories.Defensive.Celestriad>()) return "WardPulseBolts";
            if (type == ItemID.EoCShield) return "DashEffect";
            return null;
        }

        private static void RemoveStalePassiveLines(List<TooltipLine> tooltips)
        {
            string[] needles =
            {
                CommonTooltipPrefix("DRStat"),
                CommonTooltipPrefix("BadMoveSpeedMult"),
                CommonTooltipPrefix("Thorns"),
                CommonTooltipPrefix("NonMeleeBadDmg"),   // melee shields' dropped non-melee damage penalty
                CommonTooltipPrefix("Melee.Only"),       // the "for melee warriors only" flavor line
                // The mana wards (Mana Shield / Celestriad) describe their old passive tank in raw text rather
                // than shared tokens, so match those phrases directly. (English-only; the ru/zh tooltips word
                // these differently and are handled when those translations are revisited.)
                "Reduces ranged, magic and summon damage by",
                "Inhibits both natural and artificial mana regen",
            };

            tooltips.RemoveAll(line =>
                line.Name != null && line.Name.StartsWith("Tooltip")
                && Array.Exists(needles, n => !string.IsNullOrEmpty(n) && line.Text != null && line.Text.Contains(n)));
        }

        /// <summary>Returns the localized text of a CommonItemTooltip token up to its first format placeholder,
        /// e.g. "Increases Resistance by" — a language-correct needle for matching rendered tooltip lines.</summary>
        private static string CommonTooltipPrefix(string token)
        {
            const string sentinel = "￿";
            string rendered = Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip." + token, sentinel);
            int idx = rendered.IndexOf(sentinel, StringComparison.Ordinal);
            return (idx > 0 ? rendered.Substring(0, idx) : rendered).Trim();
        }
    }
}
