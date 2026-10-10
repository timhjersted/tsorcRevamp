using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Items.Weapons.Magic.Wands;

namespace tsorcRevamp.Content.Items.Accessories.Runeterra.LudensTempest
{
    public class LudensTempestItem : ModItem
    {
        public static float Dmg = 4f;
        public static float ArmorPen = 3;
        public static int Mana = 20;
        public static int Cooldown = 13;
        public static float ProcDmg = 1f;
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
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 2950);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<LudensTempestPlayer>().Equipped = true;
            player.GetDamage(DamageClass.Magic) += Dmg / 100f;
            player.GetArmorPenetration(DamageClass.Magic) += ArmorPen;
            player.statManaMax2 += Mana;
        }
    }
}
