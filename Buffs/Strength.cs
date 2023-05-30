using Terraria;
using Terraria.ModLoader;

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
            player.statDefense += 15;
            player.GetDamage(DamageClass.Generic) += 0.1f;
            player.GetAttackSpeed(DamageClass.Generic) += 0.15f;
            player.GetAttackSpeed(DamageClass.Melee) += 0.15f;
        }
    }
}
