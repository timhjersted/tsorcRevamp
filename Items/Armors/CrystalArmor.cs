using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class CrystalArmor : ModItem //To be reworked
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Crystal armor vibrates with a mysterious energy\nthat attracts enemies to you in greater numbers\nSet bonus:+40% Melee Speed, +15% Melee Damage,\n-30% Ranged and Magic Damage");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 20;
            Item.value = 7000000;
            Item.rare = ItemRarityID.Pink;
        }
    }
}
