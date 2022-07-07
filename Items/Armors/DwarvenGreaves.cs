using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [LegacyName("AncientDwarvenGreaves")]
    [AutoloadEquip(EquipType.Legs)]
    class DwarvenGreaves : ModItem //To be reworked
    {

        public override void SetDefaults()
        {
            Item.height = Item.width = 18;
            Item.defense = 4;
            Item.value = 24000;
        }
    }
}
