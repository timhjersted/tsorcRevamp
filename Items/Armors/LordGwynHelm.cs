using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    ///<summary>
    ///The crown of the Lord of Cinder. Phase 1: exists to dress the LordGwyn invader puppet (the
    ///equip sheet is the boss's body art); stats/set bonus get their real pass when this becomes a
    ///player drop. Inventory icon is a placeholder crop of the equip sheet.
    ///</summary>
    [AutoloadEquip(EquipType.Head)]
    public class LordGwynHelm : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 20;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.sellPrice(gold: 25);
        }
    }
}
