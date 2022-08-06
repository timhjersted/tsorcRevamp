using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Magic
{
    public class HealingDuskCrownRing : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Accessories/Magic/DuskCrownRing";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("This magic crown-shaped ring was granted to Princess Dusk of Oolacile upon her birth." +
                                "\nThe ringstone doubles magic damage and boosts magic crit by 50%," +
                                "\nbut at the cost of 50% max HP. " +
                                "\nYour previous max HP is restored when the ring is removed. " +
                                "\nHealing enchantment provides +9 Life Regen.");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.lifeRegen = 9;
            Item.accessory = true;
            Item.value = PriceByRarity.Red_10;
            Item.rare = ItemRarityID.Red;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<DuskCrownRing>(), 1);
            recipe.AddIngredient(ModContent.ItemType<Humanity>(), 5);
            recipe.AddIngredient(ModContent.ItemType<BlueTitanite>(), 7);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 60000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.statLifeMax2 /= 2;
            player.GetDamage(DamageClass.Magic) *= 2;
            player.GetCritChance(DamageClass.Magic) += 50;
            player.lifeRegen += 9;
            player.GetModPlayer<tsorcRevampPlayer>().DuskCrownRing = true;

        }

        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            return !(Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().DuskCrownRing);
        }
    }
}