using System.Collections.Generic;
using Humanizer;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Projectiles.Melee;

namespace tsorcRevamp.Content.Items.Armor.Melee.PortlyPlateSet
{
    [AutoloadEquip(EquipType.Body)]
    public class PortlyPlateChestplate : ModItem
    {
        public const float DamageIncrease = 12f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(DamageIncrease);
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 10;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Melee) += DamageIncrease / 100f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.GladiatorBreastplate);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 1800);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
