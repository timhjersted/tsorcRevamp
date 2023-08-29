using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors;

[AutoloadEquip(EquipType.Legs)]
public class DragoonGreaves : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("Grants an extra double jump and Shiny Red Balloon effect\nIncreases your max number of minions by 2");
    }
    public override void SetDefaults()
    {
        Item.width = 18;
        Item.height = 18;
        Item.defense = 15;
        Item.rare = ItemRarityID.Cyan;
        Item.value = PriceByRarity.fromItem(Item);
    }
    public override void UpdateEquip(Player player)
    {
        player.hasJumpOption_Unicorn = true;
        player.jumpBoost = true;
        player.maxMinions += 2;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<RedHerosPants>());
        recipe.AddIngredient(ModContent.ItemType<SoulOfLife>(), 1);
        recipe.AddIngredient(ItemID.SoulofMight, 10);
        recipe.AddIngredient(ItemID.SoulofSight, 10);
        recipe.AddIngredient(ItemID.SoulofFright, 10);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }
}

