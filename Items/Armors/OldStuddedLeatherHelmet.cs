using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class OldStuddedLeatherHelmet : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.defense = 3;
            Item.value = 900;
            Item.rare = ItemRarityID.White;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<OldStuddedLeatherArmor>() && legs.type == ModContent.ItemType<OldStuddedLeatherGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.GetDamage(DamageClass.Ranged) += 0.07f;
            player.GetCritChance(DamageClass.Ranged) += 5;
            player.moveSpeed += 0.1f;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("OldLeatherHelmet").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
