using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class Strength : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Strength");
            Description.SetDefault("You feel much stronger");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense += 15;
            player.GetDamage(DamageClass.Generic) += 0.15f;
            player.GetAttackSpeed(DamageClass.Melee) += 0.15f;
            player.pickSpeed += 0.15f;
            player.GetCritChance(DamageClass.Magic) += 2;
            player.GetCritChance(DamageClass.Melee) += 2;
            player.GetCritChance(DamageClass.Ranged) += 2;
        }
    }
}
