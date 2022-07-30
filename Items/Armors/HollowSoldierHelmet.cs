using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class HollowSoldierHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = true;
        }
        public override void SetDefaults()
        {
            Item.vanity = true;
            Item.width = 26;
            Item.height = 20;
            Item.rare = ItemRarityID.Blue;
            Item.value = PriceByRarity.fromItem(Item);
        }

        /*public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<AncientBrassArmor>() && legs.type == ModContent.ItemType<AncientBrassGreaves>();
        }*/

        public override void UpdateArmorSet(Player player)
        {
            //
        }

        /*public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.IronHelmet);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }*/
    }
}
