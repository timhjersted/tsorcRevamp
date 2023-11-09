using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Buffs
{
    public class tsorcGlobalBuff : GlobalBuff
    {
        public const float EnduranceResistBonus = 1f;
        public const float IceBarrierResistBonus = 8f;
        public const float SolarDR = 25f;
        public const float BeetleDR = 17f;
        public const float NebulaDMG = 12f;
        public override void Update(int type, Player player, ref int buffIndex)
        {
            if (type == BuffID.ManaSickness)
            {
                player.GetDamage(DamageClass.Melee) *= 1f - player.manaSickReduction;
                player.GetDamage(DamageClass.Ranged) *= 1f - player.manaSickReduction;
                player.GetDamage(DamageClass.Summon) *= 1f - player.manaSickReduction;
                player.GetDamage(DamageClass.Throwing) *= 1f - player.manaSickReduction;
            }

            if (type == BuffID.Endurance)
            {
                player.endurance += EnduranceResistBonus / 100f;
            }

            if (type == BuffID.IceBarrier)
            {
                player.endurance += IceBarrierResistBonus / 100f;
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

            if (type == BuffID.SolarShield1 || type == BuffID.SolarShield2 || type == BuffID.SolarShield3)
            {
                player.endurance += SolarDR / 100f;
            }

            if (type == BuffID.BeetleEndurance1)
            {
                player.endurance += BeetleDR / 100f;
            }

            if (type == BuffID.BeetleEndurance2)
            {
                player.endurance += BeetleDR * 2f / 100f;
            }

            if (type == BuffID.BeetleEndurance3)
            {
                player.endurance += BeetleDR * 3f / 100f;
            }

            if (type == BuffID.NebulaUpDmg1)
            {
                player.GetDamage(DamageClass.Generic) += -0.15f + NebulaDMG / 100f;
            }

            if (type == BuffID.NebulaUpDmg2)
            {
                player.GetDamage(DamageClass.Generic) += -0.3f + NebulaDMG / 100f * 2f;
            }

            if (type == BuffID.NebulaUpDmg3)
            {
                player.GetDamage(DamageClass.Generic) += -0.45f + NebulaDMG / 100f * 3f;
            }
        }

        public override void ModifyBuffText(int type, ref string buffName, ref string tip, ref int rare)
        {
            if (type == BuffID.Endurance)
            {
                tip = LangUtils.GetTextValue("CommonItemTooltip.DRStat", 10 + EnduranceResistBonus);
            }

            if (type == BuffID.IceBarrier)
            {
                tip = LangUtils.GetTextValue("CommonItemTooltip.DRStat", 25 + IceBarrierResistBonus);
            }

            if (type == BuffID.Warmth)
            {
                tip = LangUtils.GetTextValue("Buffs.VanillaBuffs.Warmth", 30);
            }

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

            if (type == BuffID.SolarShield1 || type == BuffID.SolarShield2 || type == BuffID.SolarShield3)
            {
                tip = LangUtils.GetTextValue("Buffs.VanillaBuffs.SolarShield", SolarDR);
            }

            if (type == BuffID.BeetleEndurance1)
            {
                tip = LangUtils.GetTextValue("CommonItemTooltip.DRStat", BeetleDR);
            }

            if (type == BuffID.BeetleEndurance2)
            {
                tip = LangUtils.GetTextValue("CommonItemTooltip.DRStat", BeetleDR * 2);
            }

            if (type == BuffID.BeetleEndurance3)
            {
                tip = LangUtils.GetTextValue("CommonItemTooltip.DRStat", BeetleDR * 3);
            }

            if (type == BuffID.NebulaUpDmg1)
            {
                tip = Language.GetTextValue("CommonItemTooltip.PercentIncreasedDamage", NebulaDMG);
            }

            if (type == BuffID.NebulaUpDmg2)
            {
                tip = Language.GetTextValue("CommonItemTooltip.PercentIncreasedDamage", NebulaDMG * 2f);
            }

            if (type == BuffID.NebulaUpDmg3)
            {
                tip = Language.GetTextValue("CommonItemTooltip.PercentIncreasedDamage", NebulaDMG * 3f);
            }
        }

    }
}
