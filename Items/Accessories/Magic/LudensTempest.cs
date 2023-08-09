using System;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Weapons.Magic;

namespace tsorcRevamp.Items.Accessories.Magic
{
    public class LudensTempest : ModItem
    {
        public static float Dmg = 4f;
        public static float ArmorPen = 3f;
        public static int Mana = 20;
        public static int Cooldown = 13;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, ArmorPen, Mana, Cooldown);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.value = PriceByRarity.Orange_3;
            Item.rare = ItemRarityID.Orange;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpellTome);
            recipe.AddIngredient(ItemID.ManaCrystal);
            recipe.AddIngredient(ModContent.ItemType<WandOfDarkness>());
            recipe.AddIngredient(ModContent.ItemType<WorldRune>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3200);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<tsorcRevampPlayer>().LudensTempest = true;
            player.GetDamage(DamageClass.Magic) += Dmg / 100f;
            player.GetArmorPenetration(DamageClass.Magic) += ArmorPen / 100f;
            player.statManaMax2 += Mana;
        }
    }
}
