using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Expert
{
    public class CovetousSilverSerpentRing : ModItem
    {
        public static float SoulAmplifier = 20f;
        public static int DefenseDecrease = 15;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(SoulAmplifier, DefenseDecrease);

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 22;
            Item.accessory = true;
            Item.defense = -DefenseDecrease;
            Item.value = PriceByRarity.LightRed_4; //prohibitively expensive soul cost
            Item.expert = true;
        }
        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().SilverSerpentRing = true;
            int posX = (int)(player.position.X + (float)(player.width / 2) + (float)(8 * player.direction)) / 16;
            int posY = (int)(player.position.Y + 2f) / 16;
            Lighting.AddLight(posX, posY, 0.9f, 0.8f, 0.7f);
        }

        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            foreach (Item i in player.armor)
            {
                if (i.ModItem is CovetousSoulSerpentRing)
                {
                    return false;
                }
            }

            return base.CanEquipAccessory(player, slot, modded);
        }

    }
}
