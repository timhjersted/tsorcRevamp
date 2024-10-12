﻿using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Magic
{
    [AutoloadEquip(EquipType.Face)]
    [LegacyName("GrandWizardsHat")]
    public class EnchantedWizardsHat : ModItem
    {
        public const float Dmg = 14f;
        public const float CastSpeed = 10f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, CastSpeed);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 88;
            Item.height = 70;
            Item.accessory = true;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SorcererEmblem);
            recipe.AddIngredient(ItemID.WizardHat);
            recipe.AddIngredient(ItemID.HallowedBar, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 8500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Magic) += Dmg / 100f;
            player.GetAttackSpeed(DamageClass.Magic) += CastSpeed / 100f;
        }

    }
}