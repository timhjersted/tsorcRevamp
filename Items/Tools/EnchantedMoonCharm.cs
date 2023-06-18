using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Tools
{
    class EnchantedMoonCharm : ModItem
    {

        public override void SetStaticDefaults()
        {
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
        }


        public override bool? UseItem(Player player)
        {
            Main.moonPhase++;
            if (Main.moonPhase >= 8) Main.moonPhase = 0;
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                if (Main.moonPhase == 0)
                {
                    Main.NewText(LanguageUtils.GetTextValue("Items.EnchantedMoonCharm.P1"), 50, 255, 130);
                }
                else if (Main.moonPhase == 1)
                {
                    Main.NewText(LanguageUtils.GetTextValue("Items.EnchantedMoonCharm.P2"), 50, 255, 130);
                }
                else if (Main.moonPhase == 2)
                {
                    Main.NewText(LanguageUtils.GetTextValue("Items.EnchantedMoonCharm.P3"), 50, 255, 130);
                }
                else if (Main.moonPhase == 3)
                {
                    Main.NewText(LanguageUtils.GetTextValue("Items.EnchantedMoonCharm.P4"), 50, 255, 130);
                }
                else if (Main.moonPhase == 4)
                {
                    Main.NewText(LanguageUtils.GetTextValue("Items.EnchantedMoonCharm.P5"), 50, 255, 130);
                }
                else if (Main.moonPhase == 5)
                {
                    Main.NewText(LanguageUtils.GetTextValue("Items.EnchantedMoonCharm.P6"), 50, 255, 130);
                }
                else if (Main.moonPhase == 6)
                {
                    Main.NewText(LanguageUtils.GetTextValue("Items.EnchantedMoonCharm.P7"), 50, 255, 130);
                }
                else if (Main.moonPhase == 7)
                {
                    Main.NewText(LanguageUtils.GetTextValue("Items.EnchantedMoonCharm.P8"), 50, 255, 130);
                }
            }
            else if (Main.netMode == NetmodeID.Server)
            {
                if (Main.moonPhase == 0)
                {
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(LanguageUtils.GetTextValue("Items.EnchantedMoonCharm.P1")), new Color(50, 255, 130));
                }
                if (Main.moonPhase == 1)
                {
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(LanguageUtils.GetTextValue("Items.EnchantedMoonCharm.P2")), new Color(50, 255, 130));
                }
                if (Main.moonPhase == 2)
                {
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(LanguageUtils.GetTextValue("Items.EnchantedMoonCharm.P3")), new Color(50, 255, 130));
                }
                if (Main.moonPhase == 3)
                {
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(LanguageUtils.GetTextValue("Items.EnchantedMoonCharm.P4")), new Color(50, 255, 130));
                }
                if (Main.moonPhase == 4)
                {
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(LanguageUtils.GetTextValue("Items.EnchantedMoonCharm.P5")), new Color(50, 255, 130));
                }
                if (Main.moonPhase == 5)
                {
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(LanguageUtils.GetTextValue("Items.EnchantedMoonCharm.P6")), new Color(50, 255, 130));
                }
                if (Main.moonPhase == 6)
                {
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(LanguageUtils.GetTextValue("Items.EnchantedMoonCharm.P7")), new Color(50, 255, 130));
                }
                if (Main.moonPhase == 7)
                {
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(LanguageUtils.GetTextValue("Items.EnchantedMoonCharm.P8")), new Color(50, 255, 130));
                }
                NetMessage.SendData(MessageID.WorldData);
            }

            if (Main.rand.NextBool(14) && !Main.dayTime && !Main.bloodMoon)
            {
                Main.bloodMoon = true;
                if (Main.netMode == NetmodeID.Server)
                {
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(LanguageUtils.GetTextValue("Items.EnchantedMoonCharm.BloodMoon")), new Color(50, 255, 130));
                    NetMessage.SendData(MessageID.WorldData);
                }
                else if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    Main.NewText(LanguageUtils.GetTextValue("Items.EnchantedMoonCharm.BloodMoon"), 50, 255, 130);
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
