using System;
using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class Curse : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            tip = CurseTooltipText.AppendRolledStats(tip, Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>(), powerful: false);
        }
    }

    internal static class CurseTooltipText
    {
        private const string NegativeColor = "ff6666";
        private const string PositiveColor = "66ff88";

        public static string AppendRolledStats(string baseTip, tsorcRevampPlayer modPlayer, bool powerful)
        {
            baseTip ??= "";

            List<string> penalties = new();
            List<string> boons = new();

            if (powerful)
            {
                AddStat(penalties, boons, "Max Life", Math.Min(modPlayer.powerfulCurseMaxLifeMultiplier, 0f), "%");
                AddStat(penalties, boons, "Life Regeneration", Effective(modPlayer.powerfulCurseLifeRegenerationBonus, modPlayer), "");
                AddStat(penalties, boons, "Defense", Effective(modPlayer.powerfulCurseDefenseBonus, modPlayer), "");
                AddStat(penalties, boons, "Resistance", Effective(modPlayer.powerfulCurseResistanceBonus, modPlayer), "%");
                AddStat(penalties, boons, "Damage", Effective(modPlayer.powerfulCurseDamageBonus, modPlayer), "%");
                AddStat(penalties, boons, "Attack Speed", Effective(modPlayer.powerfulCurseAttackSpeedBonus, modPlayer), "%");
                AddStat(penalties, boons, "Movement Speed", Effective(modPlayer.powerfulCurseMovementSpeedBonus, modPlayer), "%");
            }
            else
            {
                AddStat(penalties, boons, "Max Life", Math.Min(modPlayer.CurseMaxLifeMultiplier, 0f), "%");
                AddStat(penalties, boons, "Life Regeneration", Effective(modPlayer.CurseLifeRegenerationBonus, modPlayer), "");
                AddStat(penalties, boons, "Defense", Effective(modPlayer.CurseDefenseBonus, modPlayer), "");
                AddStat(penalties, boons, "Resistance", Effective(modPlayer.CurseResistanceBonus, modPlayer), "%");
                AddStat(penalties, boons, "Damage", Effective(modPlayer.CurseDamageBonus, modPlayer), "%");
                AddStat(penalties, boons, "Attack Speed", Effective(modPlayer.CurseAttackSpeedBonus, modPlayer), "%");
                AddStat(penalties, boons, "Movement Speed", Effective(modPlayer.CurseMovementSpeedBonus, modPlayer), "%");
            }

            StringBuilder text = new(baseTip.TrimEnd());
            text.AppendLine();
            text.AppendLine();
            text.AppendLine("Rolled effects:");

            if (penalties.Count > 0)
            {
                text.AppendLine($"[c/{NegativeColor}:Penalties]");
                foreach (string penalty in penalties)
                {
                    text.AppendLine(penalty);
                }
            }

            if (boons.Count > 0)
            {
                text.AppendLine($"[c/{PositiveColor}:Boons]");
                foreach (string boon in boons)
                {
                    text.AppendLine(boon);
                }
            }

            return text.ToString().TrimEnd();
        }

        private static float Effective(float value, tsorcRevampPlayer modPlayer)
        {
            return value > 0f ? value * modPlayer.CursePositiveStatsMultiplier : value;
        }

        private static void AddStat(List<string> penalties, List<string> boons, string label, float value, string suffix)
        {
            if (Math.Abs(value) < 0.01f)
            {
                return;
            }

            bool positive = value > 0f;
            string color = positive ? PositiveColor : NegativeColor;
            string sign = positive ? "+" : "";
            string formatted = $"{sign}{FormatNumber(value)}{suffix}";
            string line = $"{label}: [c/{color}:{formatted}]";

            if (positive)
            {
                boons.Add(line);
            }
            else
            {
                penalties.Add(line);
            }
        }

        private static string FormatNumber(float value)
        {
            float rounded = MathF.Round(value);
            return Math.Abs(value - rounded) < 0.05f ? rounded.ToString("0") : value.ToString("0.#");
        }
    }
}
