using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    public class DragoonBoots : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Steel Boots made for Dragoons.\n" +
                                "No damage from falling.\n" +
                                "Faster Jump, which also results in a higher jump.\n" +
                                "Press the Dragoon Boots key to toggle high jump (default Z)");

        }

        public override void SetDefaults()
        {
            Item.accessory = true;
            Item.width = 32;
            Item.height = 26;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Red;
            Item.value = PriceByRarity.Red_10;
        }
        public override void UpdateEquip(Player player)
        {
            player.noFallDmg = true;
            Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().DragoonBoots = true;
        }

    }
}
