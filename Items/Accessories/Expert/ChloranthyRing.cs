using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Expert
{
    [AutoloadEquip(EquipType.HandsOn)]

    public class ChloranthyRing : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Chloranthy Ring I");
            Tooltip.SetDefault("Increases Stamina recovery speed by 15%," +
                                "\nEnhances your agility and evasiveness when dodge rolling" +
                               "\nIncreases Stamina Droplet pickup range" +  
                               "\nThis old ring is named for its decorative green" +
                               "\nblossom, but its luster is long since faded" +
                               "\n+2 defense");
        }

        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 28;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.Expert;
            Item.expert = true;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += 0.15f;
            player.GetModPlayer<tsorcRevampPlayer>().StaminaReaper = 4;
            player.statDefense += 2;
        }

        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            foreach (Item i in player.armor)
            {
                if (i.ModItem is ChloranthyRing2)
                {
                    return false;
                }
            }

            return base.CanEquipAccessory(player, slot, modded);
        }

    }
}
