using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Melee
{
    [AutoloadEquip(EquipType.Legs)]
    public class DarkKnightGreaves : ModItem
    {
        public static float MoveSpeed = 17f;
        public static float MeleeSpeed = 10f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MoveSpeed, MeleeSpeed);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 15;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += MoveSpeed / 100f;
            player.GetAttackSpeed(DamageClass.Melee) += MeleeSpeed / 100f;

            if (player.HasBuff(BuffID.ShadowDodge))
            {
                player.moveSpeed += MoveSpeed / 100f;
                player.GetAttackSpeed(DamageClass.Melee) += MeleeSpeed / 100f;
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HallowedGreaves, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

