using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    class EnchantedMoonCharm : ModItem
    {

        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("Changes the phases of the Moon \n" +
                "Can trigger blood moons. \n" +
                "Provides 10 defense when equipped."); */
        }

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.LightRed_4;
            Item.width = 24;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.accessory = true;
            Item.defense = 10;
        }


        public override bool? UseItem(Player player)
        {
            Main.moonPhase++;
            if (Main.moonPhase >= 8) Main.moonPhase = 0;
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                if (Main.moonPhase == 0)
                {
                    Main.NewText("Moon Phase is now Full.", 50, 255, 130);
                }
                else if (Main.moonPhase == 1)
                {
                    Main.NewText("Moon Phase is now Last Gibbous.", 50, 255, 130);
                }
                else if (Main.moonPhase == 2)
                {
                    Main.NewText("Moon Phase is now Last Quarter.", 50, 255, 130);
                }
                else if (Main.moonPhase == 3)
                {
                    Main.NewText("Moon Phase is now Last Crescent.", 50, 255, 130);
                }
                else if (Main.moonPhase == 4)
                {
                    Main.NewText("Moon Phase is now New.", 50, 255, 130);
                }
                else if (Main.moonPhase == 5)
                {
                    Main.NewText("Moon Phase is now First Crescent.", 50, 255, 130);
                }
                else if (Main.moonPhase == 6)
                {
                    Main.NewText("Moon Phase is now First Quarter.", 50, 255, 130);
                }
                else if (Main.moonPhase == 7)
                {
                    Main.NewText("Moon Phase is now First Gibbous.", 50, 255, 130);
                }
            }
            else if (Main.netMode == NetmodeID.Server)
            {
                if (Main.moonPhase == 0)
                {
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("Moon phase is now Full"), new Color(50, 255, 130));
                }
                if (Main.moonPhase == 1)
                {
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("Moon phase is now Last Gibbous"), new Color(50, 255, 130));
                }
                if (Main.moonPhase == 2)
                {
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("Moon phase is now Last Quarter"), new Color(50, 255, 130));
                }
                if (Main.moonPhase == 3)
                {
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("Moon phase is now Last Crescent"), new Color(50, 255, 130));
                }
                if (Main.moonPhase == 4)
                {
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("Moon phase is now New"), new Color(50, 255, 130));
                }
                if (Main.moonPhase == 5)
                {
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("Moon phase is now First Crescent"), new Color(50, 255, 130));
                }
                if (Main.moonPhase == 6)
                {
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("Moon phase is now First Quarter"), new Color(50, 255, 130));
                }
                if (Main.moonPhase == 7)
                {
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("Moon phase is now First Gibbous"), new Color(50, 255, 130));
                }
                NetMessage.SendData(MessageID.WorldData);
            }

            if (Main.rand.NextBool(14) && !Main.dayTime && !Main.bloodMoon)
            {
                Main.bloodMoon = true;
                if (Main.netMode == NetmodeID.Server)
                {
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("The Blood moon has risen..."), new Color(50, 255, 130));
                    NetMessage.SendData(MessageID.WorldData);
                }
                else if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    Main.NewText("The Blood Moon has risen...", 50, 255, 130);
                }
            }
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SoulofLight, 1);
            recipe.AddIngredient(ItemID.MoonCharm, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
