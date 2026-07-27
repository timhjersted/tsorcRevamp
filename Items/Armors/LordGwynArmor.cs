using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    ///<summary>The Lord of Cinder's cuirass. Phase 1: dresses the LordGwyn invader puppet.</summary>
    [AutoloadEquip(EquipType.Body)]
    public class LordGwynArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            // LordGwynArmor_Body uses the same composite construction as Studded Leather: the
            // normal player arm is the substrate, then the sheet layers shoulder/sleeve/wrist and
            // its authored hand over it. Hiding top skin removes that substrate and leaves the
            // shoulder cropped with a detached hand during continuous composite-arm swings.
            ArmorIDs.Body.Sets.HidesTopSkin[Item.bodySlot] = false;
            ArmorIDs.Body.Sets.HidesArms[Item.bodySlot] = false;
            ArmorIDs.Body.Sets.HidesHands[Item.bodySlot] = true;
        }

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
