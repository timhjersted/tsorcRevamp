using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Buffs
{
    public class Flasks : GlobalBuff
    {
        public override void Update(int type, Player player, ref int buffIndex)
        {
            if (type == BuffID.WeaponImbueIchor || type == BuffID.WeaponImbuePoison)
            {
                player.GetDamage(DamageClass.Melee) += 0.1f;
                player.GetDamage(DamageClass.SummonMeleeSpeed) += 0.1f;
            }

            if (type == BuffID.WeaponImbueFire)
            {
                player.GetDamage(DamageClass.Melee) += 0.12f;
                player.GetDamage(DamageClass.SummonMeleeSpeed) += 0.12f;
            }

            if (type == BuffID.WeaponImbueGold)
            {
                player.GetDamage(DamageClass.Melee) += 0.15f;
                player.GetDamage(DamageClass.SummonMeleeSpeed) += 0.15f;
            }

            if (type == BuffID.WeaponImbueConfetti)
            {
                player.GetDamage(DamageClass.Melee) += 0.17f;
                player.GetDamage(DamageClass.SummonMeleeSpeed) += 0.17f;
            }

            if (type == BuffID.WeaponImbueCursedFlames)
            {
                player.GetDamage(DamageClass.Melee) += 0.16f;
                player.GetDamage(DamageClass.SummonMeleeSpeed) += 0.16f;
            }

            if (type == BuffID.WeaponImbueVenom)
            {
                player.GetCritChance(DamageClass.Melee) += 8f;
                player.GetDamage(DamageClass.SummonMeleeSpeed) *= 1.08f;
            }

            if (type == BuffID.WeaponImbueNanites)
            {
                player.GetCritChance(DamageClass.Melee) += 14f;
                player.GetDamage(DamageClass.SummonMeleeSpeed) *= 1.14f;
            }
        }

        public override void ModifyBuffText(int type, ref string buffName, ref string tip, ref int rare)
        {
            if (type == BuffID.WeaponImbuePoison)
            {
                tip += "\n" + LanguageUtils.GetTextValue("CommonItemTooltip.IncreasedMeleeAndWhipDamage", 10);
            }

            if (type == BuffID.WeaponImbueFire)
            {
                tip += "\n" + LanguageUtils.GetTextValue("CommonItemTooltip.IncreasedMeleeAndWhipDamage", 12);
            }

            if (type == BuffID.WeaponImbueGold)
            {
                tip += "\n" + LanguageUtils.GetTextValue("CommonItemTooltip.IncreasedMeleeAndWhipDamage", 15);
            }

            if (type == BuffID.WeaponImbueConfetti)
            {
                tip += "\n" + LanguageUtils.GetTextValue("CommonItemTooltip.IncreasedMeleeAndWhipDamage", 17);
            }

            if (type == BuffID.WeaponImbueCursedFlames)
            {
                tip += "\n" + LanguageUtils.GetTextValue("CommonItemTooltip.IncreasedMeleeAndWhipDamage", 16);
            }

            if (type == BuffID.WeaponImbueIchor)
            {
                tip += "\n" + LanguageUtils.GetTextValue("CommonItemTooltip.IncreasedMeleeAndWhipDamage", 10);
            }

            if (type == BuffID.WeaponImbueVenom)
            {
                tip += "\n" + LanguageUtils.GetTextValue("Buffs.VanillaBuffs.WeaponImbue", 8, 8);
            }

            if (type == BuffID.WeaponImbueNanites)
            {
                tip += "\n" + LanguageUtils.GetTextValue("Buffs.VanillaBuffs.WeaponImbue", 14, 14);
            }
        }

    }
}
