using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Magic
{
    public class HealingDuskCrownRing : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Accessories/DuskCrownRing";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("This magic crown-shaped ring was granted to Princess Dusk of Oolacile upon her birth." +
                                "\nThe ringstone doubles magic damage, reduces mana use by 50% and boosts magic crit by 50%" +
                                "\nbut at the cost of one-half Max HP. Your previous max HP is restored" +
                                "\nwhen the ring is removed. Healing enchantment provides +9 Life Regen.");
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
            recipe.AddIngredient(Mod.Find<ModItem>("DuskCrownRing").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("Humanity").Type, 5);
            recipe.AddIngredient(Mod.Find<ModItem>("BlueTitanite").Type, 7);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 60000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.statLifeMax2 /= 2;
            player.manaCost -= 0.5f;
            player.GetDamage(DamageClass.Magic) *= 2;
            player.GetCritChance(DamageClass.Magic) += 50;
            player.GetModPlayer<tsorcRevampPlayer>().DuskCrownRing = true;

        }

        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            return !(Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().DuskCrownRing);
        }
    }
}