using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    public class CosmicWatch : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Use this item to push time foward" +
                                "\nto the beginning of night or to the beginning of day");
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.accessory = true;
            Item.value = PriceByRarity.Blue_1;
            Item.rare = ItemRarityID.Blue;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SilverWatch, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 50);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override bool? UseItem(Player player)
        { // it doesn't, sorry :P
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.dayTime = !Main.dayTime;
                Main.time = 0;
                if (Main.dayTime)
                {
                    Main.NewText("You shift time forward and a new day begins...", 175, 75, 255);
                }
                else Main.NewText("You shift time forward and a new night begins...", 175, 75, 255);
            }

            if (Main.netMode != NetmodeID.SinglePlayer && (player.whoAmI == Main.LocalPlayer.whoAmI))
            {
                ModPacket timePacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                timePacket.Write(tsorcPacketID.SyncTimeChange);
                timePacket.Send();
            }
            return true;
        }

        public override void UpdateInventory(Player player)
        {
            player.accWatch = 3;
        }
    }
}
