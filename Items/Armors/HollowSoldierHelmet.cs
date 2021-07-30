using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class HollowSoldierHelmet : ModItem
    {

        public override void SetDefaults()
        {
            item.vanity = true;
            item.width = 26;
            item.height = 20;
            //item.defense = 2;
            item.value = 10000;
            item.rare = ItemRarityID.Green;
        }

        /*public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<AncientBrassArmor>() && legs.type == ModContent.ItemType<AncientBrassGreaves>();
        }*/

        public override void DrawHair(ref bool drawHair, ref bool drawAltHair)
        {
            drawAltHair = true;
        }

        public override void UpdateArmorSet(Player player)
        {
            //
        }

        public override void AddRecipes()
        {
            /*ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.IronHelmet);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 100);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();*/
        }
    }
}
