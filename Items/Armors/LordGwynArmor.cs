using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    ///<summary>The Lord of Cinder's cuirass. Phase 1: dresses the LordGwyn invader puppet.</summary>
    [AutoloadEquip(EquipType.Body)]
    public class LordGwynArmor : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 28;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.sellPrice(gold: 25);
        }
    }
}
