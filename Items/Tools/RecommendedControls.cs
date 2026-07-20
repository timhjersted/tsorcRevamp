using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Tools
{
    public class RecommendedControls : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.UseSound = SoundID.MenuOpen;
            Item.rare = ItemRarityID.Blue;
            Item.value = 0;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer || Main.netMode == NetmodeID.Server)
            {
                return false;
            }

            if (!tsorcRevamp.TryToggleRecommendedControlsFromItem(out bool enabled))
            {
                Main.NewText(Language.GetTextValue("Mods.tsorcRevamp.Items.RecommendedControls.ToggleFailed"), Color.OrangeRed);
                return false;
            }

            string stateKey = enabled ? "Enabled" : "Disabled";
            Main.NewText(Language.GetTextValue("Mods.tsorcRevamp.Items.RecommendedControls." + stateKey),
                enabled ? Color.LightGreen : Color.LightGray);
            return true;
        }
    }
}
