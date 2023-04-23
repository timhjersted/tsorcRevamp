using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class Flasks : GlobalBuff
    {

        public override void Update(int type, Player player, ref int buffIndex)
        {
            if (type == BuffID.WeaponImbueIchor
                || type == BuffID.WeaponImbuePoison
                )
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
                tip = "Increases melee and whip damage by 10%, melee and whip attacks poison enemies";
            }
            if (type == BuffID.WeaponImbueFire)
            {
                tip = "Increases melee and whip damage by 10%, melee and whip attacks set enemies on fire";
            }
            if (type == BuffID.WeaponImbueGold)
            {
                tip = "Increases melee and whip damage by 15%, melee and whip attacks make enemies drop more gold";
            }
            if (type == BuffID.WeaponImbueConfetti)
            {
                tip = "Increases melee and whip damage by 17%, melee and whip attacks cause confetti to appear";
            }
            if (type == BuffID.WeaponImbueCursedFlames)
            {
                tip = "Increases melee and whip damage by 16%, melee and whip attacks inflict enemies with cursed flames";
            }
            if (type == BuffID.WeaponImbueIchor)
            {
                tip = "Increases melee and whip damage by 10%, melee and whip attacks decrease enemies' defense";
            }
            if (type == BuffID.WeaponImbueVenom)
            {
                tip = "Increases melee crit by 8%, increases whip damage by 8% multiplicatively and melee and whip attacks inflict Venom on enemies";
            }
            if (type == BuffID.WeaponImbueNanites)
            {
                tip = "Increases melee crit by 14%, increases whip damage by 14% multiplicatively and melee and whip attacks confuse enemies";
            }
        }

    }
}
