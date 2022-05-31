using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class MimeticHat : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("9% increased Magic critical strike\n+20 Mana");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 6;
            Item.value = 15000;
            Item.rare = ItemRarityID.White;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<MimeticSuit>() && legs.type == ModContent.ItemType<MimeticPants>();
        }

        public override void UpdateEquip(Player player)
        {
            player.magicCrit += 9;
            player.statManaMax2 += 20;
        }

        public override void UpdateArmorSet(Player player)
        {
            player.manaCost -= 0.3f;
            player.magicDamage += 0.15f;
            player.magicCrit += 6;
        }

        public override void AddRecipes()
        {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.JungleHat, 1);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
