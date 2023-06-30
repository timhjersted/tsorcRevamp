using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Buffs
{
    public class tsorcGlobalBuff : GlobalBuff
    {
        public static float SolarDR1 = 5f;
        public static float SolarDR2 = 10f;
        public static float SolarDR3 = 15f;
        public static float NebulaDMG1 = 8f;
        public static float NebulaDMG2 = 16f;
        public static float NebulaDMG3 = 24f;
        public override void Update(int type, Player player, ref int buffIndex)
        {
            if (type == BuffID.ManaSickness)
            {
                player.GetDamage(DamageClass.Melee) *= 1f - player.manaSickReduction;
                player.GetDamage(DamageClass.Ranged) *= 1f - player.manaSickReduction;
                player.GetDamage(DamageClass.Summon) *= 1f - player.manaSickReduction;
                player.GetDamage(DamageClass.Throwing) *= 1f - player.manaSickReduction;
            }

            if (type == BuffID.AmmoReservation)
            {
                player.GetModPlayer<tsorcRevampPlayer>().AmmoReservationPotion = true;
            }

            if (type == BuffID.Tipsy)
            {
                player.GetDamage(DamageClass.SummonMeleeSpeed) += 0.1f;
            }

            if (type == BuffID.Sharpened)
            {
                player.GetArmorPenetration(DamageClass.Melee) -= 12f;
                player.GetModPlayer<tsorcRevampPlayer>().Sharpened = true;
            }

            if (type == BuffID.AmmoBox)
            {
                player.GetModPlayer<tsorcRevampPlayer>().AmmoBox = true;
            }

            if (type == BuffID.SolarShield1)
            {
                player.endurance += -0.2f + SolarDR1 / 100f;
            }

            if (type == BuffID.SolarShield2)
            {
                player.endurance += -0.2f + SolarDR2 / 100f;
            }

            if (type == BuffID.SolarShield3)
            {
                player.endurance += -0.2f + SolarDR3 / 100f;
            }

            if (type == BuffID.NebulaUpDmg1)
            {
                player.GetDamage(DamageClass.Generic) += -0.15f + NebulaDMG1 / 100f;
            }

            if (type == BuffID.NebulaUpDmg2)
            {
                player.GetDamage(DamageClass.Generic) += -0.3f + NebulaDMG2 / 100f;
            }

            if (type == BuffID.NebulaUpDmg3)
            {
                player.GetDamage(DamageClass.Generic) += -0.45f + NebulaDMG3 / 100f;
            }
        }

        public override void ModifyBuffText(int type, ref string buffName, ref string tip, ref int rare)
        {
            if (type == BuffID.ManaSickness)
            {
                tip = LangUtils.GetTextValue("Buffs.VanillaBuffs.ManaSickness", (int)(100 * Main.LocalPlayer.manaSickReduction));
            }

            if (type == BuffID.AmmoReservation)
            {
                tip += "\n" + LangUtils.GetTextValue("CommonItemTooltip.Ranged.CritDmg", tsorcRevampPlayer.AmmoReservationRangedCritDamage);
            }

            if (type == BuffID.Tipsy)
            {
                tip += "\n" + LangUtils.GetTextValue("CommonItemTooltip.Summon.WhipDmg", 10);
            }

            if (type == BuffID.Sharpened)
            {
                tip = LangUtils.GetTextValue("Items.VanillaItems.SharpeningStation", tsorcRevampPlayer.SharpenedMeleeArmorPen);
            }

            if (type == BuffID.AmmoBox)
            {
                tip += "\n" + LangUtils.GetTextValue("Items.VanillaItems.AmmoBox");
            }

            if (type == BuffID.SolarShield1)
            {
                tip = LangUtils.GetTextValue("Buffs.VanillaBuffs.SolarShield", SolarDR1);
            }

            if (type == BuffID.SolarShield2)
            {
                tip = LangUtils.GetTextValue("Buffs.VanillaBuffs.SolarShield", SolarDR2);
            }

            if (type == BuffID.SolarShield3)
            {
                tip = LangUtils.GetTextValue("Buffs.VanillaBuffs.SolarShield", SolarDR3);
            }

            if (type == BuffID.NebulaUpDmg1)
            {
                tip = Language.GetTextValue("CommonItemTooltip.PercentIncreasedDamage", NebulaDMG1);
            }

            if (type == BuffID.NebulaUpDmg2)
            {
                tip = Language.GetTextValue("CommonItemTooltip.PercentIncreasedDamage", NebulaDMG2);
            }

            if (type == BuffID.NebulaUpDmg3)
            {
                tip = Language.GetTextValue("CommonItemTooltip.PercentIncreasedDamage", NebulaDMG3);
            }
        }

    }
}
