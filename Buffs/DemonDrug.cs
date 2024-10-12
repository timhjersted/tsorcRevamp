using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Items.Potions;

namespace tsorcRevamp.Buffs
{
    public class DemonDrug : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetDamage(DamageClass.Generic) += DemonDrugPotion.Dmg / 100f;
            player.statDefense -= DemonDrugPotion.BadDefense;
        }
    }
}
