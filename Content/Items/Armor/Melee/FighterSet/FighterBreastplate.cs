using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Armor;
using tsorcRevamp.Content.Items.Armor.SetBonuses;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Armor.Melee.FighterSet
{
    [AutoloadEquip(EquipType.Body)]
    public class FighterBreastplate : ModItem
    {
        public const float MeleeSpeed = 21f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MeleeSpeed, ShunpoBlink.Cooldown);
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 24;
            Item.rare = ItemRarityID.Lime;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetAttackSpeed(DamageClass.Melee) += MeleeSpeed / 100f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.AdamantiteBreastplate);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 4000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.AdamantiteBreastplate);
            recipe2.AddIngredient(ItemID.TitaniumBreastplate);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}
