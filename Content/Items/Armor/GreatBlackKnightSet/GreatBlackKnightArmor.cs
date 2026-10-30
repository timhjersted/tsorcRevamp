using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.GreatBlackKnightSet
{
    // Planned outfit for the Great Black Knight puppet conversion. Vanity only and not obtainable yet (no drop or recipe).
    // Helm pixels are lifted straight from the GBK NPC sheet; body and greaves are the Black Knight rig re-skinned in GBK colours.
    [AutoloadEquip(EquipType.Head)]
    public class GreatBlackKnightHelm : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Body)]
    public class GreatBlackKnightArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            // The sheet paints its own arms and gauntlets, so the player's skin must not draw underneath.
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
            ArmorIDs.Body.Sets.HidesTopSkin[equipSlot] = true;
            ArmorIDs.Body.Sets.HidesArms[equipSlot] = true;
            ArmorIDs.Body.Sets.HidesHands[equipSlot] = true;

            // The pauldron lives only in the front shoulder-cap cell (0,1), which vanilla hides in the jump pose
            // for modded armour. Without this it would disappear mid-air.
            ArmorIDs.Body.Sets.showsShouldersWhileJumping[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 26;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Legs)]
    public class GreatBlackKnightGreaves : ModItem
    {
        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
            ArmorIDs.Legs.Sets.HidesTopSkin[equipSlot] = true;
            ArmorIDs.Legs.Sets.HidesBottomSkin[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 16;
            Item.vanity = true;
        }
    }
}
