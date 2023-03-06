using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Magic
{
    [LegacyName("RedMageTunic")]
    [AutoloadEquip(EquipType.Body)]
    public class RedClothTunic : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Increases magic damage by 2 flat\nSet bonus: Increases max mana by 50, decreases mana costs by 5%");
        }
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 36;
            Item.defense = 4;
            Item.rare = ItemRarityID.Green;
            Item.value = PriceByRarity.fromItem(Item);
            ArmorIDs.Body.Sets.HidesHands[Item.bodySlot] = true; //TODO maybe? 
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Magic).Flat += 2;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<RedClothHat>() && legs.type == ModContent.ItemType<RedClothPants>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.statManaMax2 += 50;
            player.manaCost -= 0.05f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Silk, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 250);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
