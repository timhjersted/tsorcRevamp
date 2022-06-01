using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class GenjiHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Amor from the East\n+25% magic damage");
        }
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 20;
            Item.defense = 9;
            Item.value = 5000000;
            Item.rare = ItemRarityID.Orange;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<GenjiArmor>() && legs.type == ModContent.ItemType<GenjiGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.GetCritChance(DamageClass.Magic) += 20;
            player.statManaMax2 += 100;
            player.manaRegen += 3;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Magic) += 0.25f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.AdamantiteHeadgear, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 4000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
