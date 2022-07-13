using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Melee
{
    [AutoloadEquip(EquipType.Head)]
    public class FighterHairStyle : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Adept at close combat");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.defense = 2;
            Item.rare = ItemRarityID.Lime;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<FighterBreastplate>() && legs.type == ModContent.ItemType<FighterGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.GetDamage(DamageClass.Melee) += 0.25f;
            player.GetCritChance(DamageClass.Melee) += 17;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.AdamantiteHelmet, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 4000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
