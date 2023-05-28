using Humanizer;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class tsorcGlobalBuff : GlobalBuff
    {

        public override void Update(int type, Player player, ref int buffIndex)
        {
            if (type == BuffID.ManaSickness)
            {
                player.GetDamage(DamageClass.Melee) *= 1f - player.manaSickReduction;
                player.GetDamage(DamageClass.Ranged) *= 1f - player.manaSickReduction;
                player.GetDamage(DamageClass.Summon) *= 1f - player.manaSickReduction;
                player.GetDamage(DamageClass.Throwing) *= 1f - player.manaSickReduction;
            }
            if (type == BuffID.SolarShield1)
            {
                player.endurance -= 0.15f;
            }
            if (type == BuffID.SolarShield2)
            {
                player.endurance -= 0.1f;
            }
            if (type == BuffID.SolarShield3)
            {
                player.endurance -= 0.05f;
            }
            if (type == BuffID.NebulaUpDmg1)
            {
                player.GetDamage(DamageClass.Generic) -= 0.07f;
            }
            if (type == BuffID.NebulaUpDmg2)
            {
                player.GetDamage(DamageClass.Generic) -= 0.14f;
            }
            if (type == BuffID.NebulaUpDmg3)
            {
                player.GetDamage(DamageClass.Generic) -= 0.21f;
            }
        }

        public override void ModifyBuffText(int type, ref string buffName, ref string tip, ref int rare)
        {
            if (type == BuffID.ManaSickness)
            {
                tip = Language.GetTextValue("Mods.tsorcRevamp.Buffs.VanillaBuffs.ManaSickness").FormatWith((int)(100 * Main.LocalPlayer.manaSickReduction));
            }
            if (type == BuffID.SolarShield1)
            {
                tip = Language.GetTextValue("Mods.tsorcRevamp.Buffs.VanillaBuffs.SolarShield").FormatWith(5) + Language.GetTextValue("Mods.tsorcRevamp.Buffs.VanillaBuffs.SolarShieldRepel");
            }
            if (type == BuffID.SolarShield2)
            {
                tip = Language.GetTextValue("Mods.tsorcRevamp.Buffs.VanillaBuffs.SolarShield").FormatWith(10) + Language.GetTextValue("Mods.tsorcRevamp.Buffs.VanillaBuffs.SolarShieldRepel");
            }
            if (type == BuffID.SolarShield3)
            {
                tip = Language.GetTextValue("Mods.tsorcRevamp.Buffs.VanillaBuffs.SolarShield").FormatWith(15) + Language.GetTextValue("Mods.tsorcRevamp.Buffs.VanillaBuffs.SolarShieldRepel");
            }
            if (type == BuffID.NebulaUpDmg1)
            {
                tip = Language.GetTextValue("Mods.tsorcRevamp.Buffs.VanillaBuffs.NebulaDamageBoost").FormatWith(8);
            }
            if (type == BuffID.NebulaUpDmg2)
            {
                tip = Language.GetTextValue("Mods.tsorcRevamp.Buffs.VanillaBuffs.NebulaDamageBoost").FormatWith(16);
            }
            if (type == BuffID.NebulaUpDmg3)
            {
                tip = Language.GetTextValue("Mods.tsorcRevamp.Buffs.VanillaBuffs.NebulaDamageBoost").FormatWith(24);
            }
        }

    }
}
