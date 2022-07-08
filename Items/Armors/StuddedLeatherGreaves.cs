using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class StuddedLeatherGreaves : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Increases movement speed by 15%");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 1;
            Item.value = 650;
            Item.rare = ItemRarityID.White;
        }
        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += 0.15f;
        }
    }
}

