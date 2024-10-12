using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Buffs
{
    public class Flasks : GlobalBuff
    {
        public static float PoisonFlaskDMG = 10f;
        public static float IchorFlaskDMG = 6f;
        public static float FireFlaskDMG = 10f;
        public static float GoldFlaskDMG = 15f;
        public static float ConfettiFlaskDMG = 16f;
        public static float CursedFlaskDMG = 10f;
        public static float VenomFlaskCrit = 12f;
        public static float NanitesFlaskCrit = 17f;
        public override void Update(int type, Player player, ref int buffIndex)
        {
            if (type == BuffID.WeaponImbuePoison)
            {
                player.GetDamage(DamageClass.Melee) += PoisonFlaskDMG / 100f;
                player.GetDamage(DamageClass.SummonMeleeSpeed) += PoisonFlaskDMG / 100f;
            }

            if (type == BuffID.WeaponImbueIchor)
            {
                player.GetDamage(DamageClass.Melee) += IchorFlaskDMG / 100f;
                player.GetDamage(DamageClass.SummonMeleeSpeed) += IchorFlaskDMG / 100f;
            }

            if (type == BuffID.WeaponImbueFire)
            {
                player.GetDamage(DamageClass.Melee) += FireFlaskDMG / 100f;
                player.GetDamage(DamageClass.SummonMeleeSpeed) += FireFlaskDMG / 100f;
            }

            if (type == BuffID.WeaponImbueGold)
            {
                player.GetDamage(DamageClass.Melee) += GoldFlaskDMG / 100f;
                player.GetDamage(DamageClass.SummonMeleeSpeed) += GoldFlaskDMG / 100f;
            }

            if (type == BuffID.WeaponImbueConfetti)
            {
                player.GetDamage(DamageClass.Melee) += ConfettiFlaskDMG / 100f;
                player.GetDamage(DamageClass.SummonMeleeSpeed) += ConfettiFlaskDMG / 100f;
            }

            if (type == BuffID.WeaponImbueCursedFlames)
            {
                player.GetDamage(DamageClass.Melee) += CursedFlaskDMG / 100f;
                player.GetDamage(DamageClass.SummonMeleeSpeed) += CursedFlaskDMG / 100f;
            }

            if (type == BuffID.WeaponImbueVenom)
            {
                player.GetCritChance(DamageClass.Melee) += VenomFlaskCrit;
                player.GetCritChance(DamageClass.SummonMeleeSpeed) += VenomFlaskCrit;
            }

            if (type == BuffID.WeaponImbueNanites)
            {
                player.GetCritChance(DamageClass.Melee) += NanitesFlaskCrit;
                player.GetCritChance(DamageClass.SummonMeleeSpeed) += NanitesFlaskCrit;
            }
        }

        public override void ModifyBuffText(int type, ref string buffName, ref string tip, ref int rare)
        {
            if (type == BuffID.WeaponImbuePoison)
            {
                tip += "\n" + LangUtils.GetTextValue("CommonItemTooltip.MeleeWhipDmg", PoisonFlaskDMG);
            }

            if (type == BuffID.WeaponImbueFire)
            {
                tip += "\n" + LangUtils.GetTextValue("CommonItemTooltip.MeleeWhipDmg", FireFlaskDMG);
            }

            if (type == BuffID.WeaponImbueGold)
            {
                tip += "\n" + LangUtils.GetTextValue("CommonItemTooltip.MeleeWhipDmg", GoldFlaskDMG);
            }

            if (type == BuffID.WeaponImbueConfetti)
            {
                tip += "\n" + LangUtils.GetTextValue("CommonItemTooltip.MeleeWhipDmg", ConfettiFlaskDMG);
            }

            if (type == BuffID.WeaponImbueCursedFlames)
            {
                tip += "\n" + LangUtils.GetTextValue("CommonItemTooltip.MeleeWhipDmg", CursedFlaskDMG);
            }

            if (type == BuffID.WeaponImbueIchor)
            {
                tip += "\n" + LangUtils.GetTextValue("CommonItemTooltip.MeleeWhipDmg", IchorFlaskDMG);
            }

            if (type == BuffID.WeaponImbueVenom)
            {
                tip += "\n" + LangUtils.GetTextValue("Buffs.VanillaBuffs.WeaponImbue", VenomFlaskCrit, VenomFlaskCrit);
            }

            if (type == BuffID.WeaponImbueNanites)
            {
                tip += "\n" + LangUtils.GetTextValue("Buffs.VanillaBuffs.WeaponImbue", NanitesFlaskCrit, NanitesFlaskCrit);
            }
        }

    }
}
