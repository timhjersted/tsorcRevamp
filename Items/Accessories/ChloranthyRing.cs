using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
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
            item.width = 34;
            item.height = 28;
            item.accessory = true;
            item.value = PriceByRarity.LightRed_4;
            item.rare = ItemRarityID.Lime;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += 0.2f;
            player.GetModPlayer<tsorcRevampPlayer>().StaminaReaper = 4;
            player.statDefense += 2;
        }

    }
}
