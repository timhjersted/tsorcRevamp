using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Magic;

[AutoloadEquip(EquipType.Body)]
public class GenjiArmor : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("Armor from the East, enchanted with magic forces" +
            "\n-12% mana cost\nSet bonus: +20% Magic Crit, +100 mana, +3 Mana Regen");
    }
    public override void SetDefaults()
    {
        Item.width = 18;
        Item.height = 18;
        Item.defense = 17;
        Item.rare = ItemRarityID.Lime;
        Item.value = PriceByRarity.fromItem(Item);
    }
    public override void UpdateEquip(Player player)
    {
        player.manaCost -= 0.12f;
    }
    public override bool IsArmorSet(Item head, Item body, Item legs)
    {
        return head.type == ModContent.ItemType<GenjiHelmet>() && legs.type == ModContent.ItemType<GenjiGreaves>();
    }
    public override void UpdateArmorSet(Player player)
    {
        player.GetCritChance(DamageClass.Magic) += 20;
        player.statManaMax2 += 100;
        player.manaRegen += 3;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.AdamantiteBreastplate, 1);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 4000);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }
}
