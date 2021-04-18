using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]

    public class SymbolOfAvarice : ModItem
    {
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Increases Dark soul absorption from defeated enemies by 40%"
                                + "\nbut the curse of the branded also [c/FF0000:drains HP].");
        }
        public override void SetDefaults()
        {
            item.width = 26;
            item.height = 28;
            item.defense = 2;
            item.value = 3000000;
            item.rare = ItemRarityID.Lime;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().SOADrain = true;
        }
    }
}
