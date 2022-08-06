using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Melee
{
    [AutoloadEquip(EquipType.Legs)]
    public class PowerArmorNUGreaves : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("+20% Melee Speed, +15% move speed\nSkills: Water Walk, Fire Walk, No knockback, Double Jump, Jump Boost, Longer Breath");
            DisplayName.SetDefault("Power Armor NU Greaves");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 4;
            Item.rare = ItemRarityID.Lime;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += 0.15f;
            player.GetAttackSpeed(DamageClass.Melee) += 0.2f;
            player.waterWalk = true;
            player.breath = 10800;
            player.noKnockback = true;
            player.hasJumpOption_Santank = true;
            player.jumpBoost = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ShadowGreaves, 1);
            recipe.AddIngredient(ItemID.SoulofMight, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

