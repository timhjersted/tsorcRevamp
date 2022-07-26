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
            Tooltip.SetDefault("Increases Stamina recovery speed by 20%" +
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
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += 0.2f;
            player.GetModPlayer<tsorcRevampPlayer>().StaminaReaper = 4;
            player.statDefense += 2;
        }

    }
}
