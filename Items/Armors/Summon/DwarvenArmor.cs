using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Body)]
    class DwarvenArmor : ModItem
    {
        public static int MinionSlots = 1;
        public static float Dmg = 10f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MinionSlots, Dmg);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.height = Item.width = 18;
            Item.defense = 15;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.maxMinions += MinionSlots;

            player.GetDamage(DamageClass.Summon) += Dmg / 100f;

            if (player.HasBuff(BuffID.ShadowDodge))
            {
                player.GetDamage(DamageClass.Summon) += Dmg / 100f;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HallowedPlateMail, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();


            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.AncientHallowedPlateMail, 1);
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}
