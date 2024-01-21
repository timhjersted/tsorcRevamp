using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories
{
    public class CovetousSoulSerpentRing : ModItem
    {
        public static float SoulAmplifier = 30f;
        public static int DefenseDecrease = 40;
        public static int ConsSoulChanceAmplifier = 100;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(SoulAmplifier, DefenseDecrease, ConsSoulChanceAmplifier);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 22;
            Item.accessory = true;
            Item.defense = -DefenseDecrease;
            Item.value = PriceByRarity.Pink_5;
            Item.expert = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<CovetousSilverSerpentRing>(), 1);
            recipe.AddIngredient(ModContent.ItemType<SoulReaper2>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().SoulSerpentRing = true;
            player.GetModPlayer<tsorcRevampPlayer>().SoulSickle = true;
            player.GetModPlayer<tsorcRevampPlayer>().SoulReaper += 13;
            player.GetModPlayer<tsorcRevampPlayer>().ConsSoulChanceMult += ConsSoulChanceAmplifier / 5;
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
