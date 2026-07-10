using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    ///<summary>The Lord of Cinder's greaves. Phase 1: dresses the LordGwyn invader puppet.</summary>
    [AutoloadEquip(EquipType.Legs)]
    public class LordGwynLeggings : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 22;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.sellPrice(gold: 25);
        }
    }
}
