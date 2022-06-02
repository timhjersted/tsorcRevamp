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
            player.GetCritChance(DamageClass.Magic) += 9;
            player.statManaMax2 += 20;
        }

        public override void UpdateArmorSet(Player player)
        {
            player.manaCost -= 0.3f;
            player.GetDamage(DamageClass.Magic) += 0.15f;
            player.GetCritChance(DamageClass.Magic) += 6;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.JungleHat, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
