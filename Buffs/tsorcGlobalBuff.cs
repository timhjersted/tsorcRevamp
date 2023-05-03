using Terraria;
using Terraria.ID;
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
            if (type == BuffID.SolarShield1)
            {
                tip = "Damage taken reduced by 5%, repel enemies when taking damage";
            }
            if (type == BuffID.SolarShield2)
            {
                tip = "Damage taken reduced by 10%, repel enemies when taking damage";
            }
            if (type == BuffID.SolarShield3)
            {
                tip = "Damage taken reduced by 15%, repel enemies when taking damage";
            }
            if (type == BuffID.NebulaUpDmg1)
            {
                tip = "+8% damage";
            }
            if (type == BuffID.NebulaUpDmg2)
            {
                tip = "+16% damage";
            }
            if (type == BuffID.NebulaUpDmg3)
            {
                tip = "+24% damage";
            }
            if (type == BuffID.NebulaUpLife1)
            {
                tip = "+2 Life Regeneration per second";
            }
            if (type == BuffID.NebulaUpLife2)
            {
                tip = "+4 Life Regeneration per second";
            }
            if (type == BuffID.NebulaUpLife3)
            {
                tip = "+6 Life Regeneration per second";
            }
        }

    }
}
