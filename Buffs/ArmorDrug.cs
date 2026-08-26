using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Items.Potions;

namespace tsorcRevamp.Buffs
{
    public class ArmorDrug : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense += ArmorDrugPotion.Defense;
            player.endurance += ArmorDrugPotion.ResistanceIncrease / 100f;
            player.GetDamage(DamageClass.Generic) *= 1f - (ArmorDrugPotion.BadDmgMult / 100f);
        }
    }
}
