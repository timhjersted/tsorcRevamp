using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [LegacyName("RedMagePants")]
    [AutoloadEquip(EquipType.Legs)]
    public class RedClothPants : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Increases movement speed by 10%");
        }
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 18;
            Item.defense = 3;
            Item.value = 9000;
            Item.rare = ItemRarityID.Blue;
        }
        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += 0.1f;
        }
        //Can be dropped by many enemies
    }
}

