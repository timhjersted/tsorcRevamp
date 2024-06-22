using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Accessories
{
    public class CosmicWatch : ModItem
    {
        public override void SetStaticDefaults()
        {
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
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SilverWatch, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override bool? UseItem(Player player)
        { // it doesn't, sorry :P
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.dayTime = !Main.dayTime;
                Main.time = 1;
                if (Main.dayTime)
                {
                    Main.NewText(LangUtils.GetTextValue("Items.CosmicWatch.Day"), Color.Orange);
                }
                else Main.NewText(LangUtils.GetTextValue("Items.CosmicWatch.Night"), 175, 75, 255);
            }

            if (Main.netMode == NetmodeID.MultiplayerClient && (player.whoAmI == Main.LocalPlayer.whoAmI))
            {
                ModPacket timePacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                timePacket.Write(tsorcPacketID.SyncTimeChange);
                timePacket.Write(!Main.dayTime);
                timePacket.Write(1);
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