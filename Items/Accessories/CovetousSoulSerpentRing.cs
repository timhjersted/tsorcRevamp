using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    public class CovetousSoulSerpentRing : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("An ancient relic forged and lost many centuries ago" +
                                "\nIncreases the number of Dark Souls dropped by fallen creatures by 50%. Defense reduced by 40." +
                                "\nAll souls are drawn to the wearer from a large distance" +
                                "\nThe ring glows with a bright white light");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 22;
            Item.accessory = true;
            Item.defense = -40;
            Item.value = PriceByRarity.Pink_5;
            Item.rare = ItemRarityID.Pink;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("CovetousSilverSerpentRing").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("SoulReaper2").Type, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().SilverSerpentRing = true;
            Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SoulReaper += 13;
            player.GetModPlayer<tsorcRevampPlayer>().ConsSoulChanceMult += 10; //50% increase
            int posX = (int)(player.position.X + (float)(player.width / 2) + (float)(8 * player.direction)) / 16;
            int posY = (int)(player.position.Y + 2f) / 16;
            Lighting.AddLight(posX, posY, 0.9f, 0.8f, 0.7f);
        }

        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            foreach (Item i in player.armor)
            {
                if (i.ModItem is CovetousSilverSerpentRing)
                {
                    return false;
                }
            }

            return base.CanEquipAccessory(player, slot, modded);
        }

    }
}
