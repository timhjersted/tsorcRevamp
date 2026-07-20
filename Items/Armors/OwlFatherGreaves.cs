using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class OwlFatherGreaves : ModItem
    {
        public static float MeleeDmg = 6f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MeleeDmg);

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.value = 100000;
            Item.rare = ItemRarityID.Yellow;
            Item.defense = 21;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Melee) += MeleeDmg / 100f;
        }
    }
}
