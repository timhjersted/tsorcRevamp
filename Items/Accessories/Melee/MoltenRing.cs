using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Melee
{
    [AutoloadEquip(EquipType.HandsOn)]

    public class MoltenRing : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Enchanted Molten Ring grants fire-walking ability and negates knockback" +
                                "\n+10% Melee Damage" +
                                "\nThe enchanted ring's power is fueled by a +5% mana cost");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 22;
            Item.accessory = true;
            Item.value = PriceByRarity.Orange_3;
            Item.rare = ItemRarityID.Orange;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HellstoneBar, 10);
            recipe.AddIngredient(Mod.Find<ModItem>("EphemeralDust").Type, 6);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 5000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.fireWalk = true;
            player.GetDamage(DamageClass.Melee) += 0.1f;
            player.noKnockback = true;
            player.manaCost += 0.05f;
        }

    }
}