using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Buffs
{
    public class Flasks : GlobalBuff
    {
        public static float PoisonFlaskDMG = 10f;
        public static float IchorFlaskDMG = 10f;
        public static float FireFlaskDMG = 12f;
        public static float GoldFlaskDMG = 15f;
        public static float ConfettiFlaskDMG = 17f;
        public static float CursedFlaskDMG = 16f;
        public static float VenomFlaskDMGCrit = 8f;
        public static float NanitesFlaskDMGCrit = 11f;
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
                player.GetCritChance(DamageClass.Melee) += VenomFlaskDMGCrit;
                player.GetDamage(DamageClass.SummonMeleeSpeed) *= 1f + VenomFlaskDMGCrit / 100f;
            }

            if (type == BuffID.WeaponImbueNanites)
            {
                player.GetCritChance(DamageClass.Melee) += NanitesFlaskDMGCrit;
                player.GetDamage(DamageClass.SummonMeleeSpeed) *= 1f + NanitesFlaskDMGCrit / 100f;
            }
        }

        public override void ModifyBuffText(int type, ref string buffName, ref string tip, ref int rare)
        {
            if (type == BuffID.WeaponImbuePoison)
            {
                tip += "\n" + LaUtils.GetTextValue("CommonItemTooltip.Generic.MeleeWhipDmg", PoisonFlaskDMG);
            }

            if (type == BuffID.WeaponImbueFire)
            {
                tip += "\n" + LaUtils.GetTextValue("CommonItemTooltip.Generic.MeleeWhipDmg", FireFlaskDMG);
            }

            if (type == BuffID.WeaponImbueGold)
            {
                tip += "\n" + LaUtils.GetTextValue("CommonItemTooltip.Generic.MeleeWhipDmg", GoldFlaskDMG);
            }

            if (type == BuffID.WeaponImbueConfetti)
            {
                tip += "\n" + LaUtils.GetTextValue("CommonItemTooltip.Generic.MeleeWhipDmg", ConfettiFlaskDMG);
            }

            if (type == BuffID.WeaponImbueCursedFlames)
            {
                tip += "\n" + LaUtils.GetTextValue("CommonItemTooltip.Generic.MeleeWhipDmg", CursedFlaskDMG);
            }

            if (type == BuffID.WeaponImbueIchor)
            {
                tip += "\n" + LaUtils.GetTextValue("CommonItemTooltip.Generic.MeleeWhipDmg", IchorFlaskDMG);
            }

            if (type == BuffID.WeaponImbueVenom)
            {
                tip += "\n" + LaUtils.GetTextValue("Buffs.VanillaBuffs.WeaponImbue", VenomFlaskDMGCrit, VenomFlaskDMGCrit);
            }

            if (type == BuffID.WeaponImbueNanites)
            {
                tip += "\n" + LaUtils.GetTextValue("Buffs.VanillaBuffs.WeaponImbue", NanitesFlaskDMGCrit, NanitesFlaskDMGCrit);
            }
        }

    }
}
