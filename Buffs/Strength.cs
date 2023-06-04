using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Items.Potions;

namespace tsorcRevamp.Buffs
{
    public class Strength : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense += StrengthPotion.Defense;
            player.GetDamage(DamageClass.Generic) += StrengthPotion.DamageBoost / 100f;
            player.GetAttackSpeed(DamageClass.Generic) += StrengthPotion.AttackSpeedBoost / 100f;
            player.GetAttackSpeed(DamageClass.Melee) += StrengthPotion.AttackSpeedBoost / 100f;
        }
    }
}
