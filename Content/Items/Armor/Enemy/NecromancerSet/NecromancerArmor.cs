using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.Enemy.NecromancerSet
{
    // Converted from the Necromancer NPC sheet. Vanity only and not obtainable yet (no drop or recipe).
    [AutoloadEquip(EquipType.Head)]
    public class NecromancerHood : ModItem
    {
        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
            ArmorIDs.Head.Sets.DrawHead[equipSlot] = false;
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 16;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Body)]
    public class NecromancerRobe : ModItem
    {
        public override void SetStaticDefaults()
        {
            // The sheet paints its own sleeves and bone gauntlets, so the player's skin must not draw underneath.
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
            ArmorIDs.Body.Sets.HidesTopSkin[equipSlot] = true;
            ArmorIDs.Body.Sets.HidesArms[equipSlot] = true;
            ArmorIDs.Body.Sets.HidesHands[equipSlot] = true;

            // The bone pauldron lives only in the front shoulder-cap cell (0,1), which vanilla hides mid-jump for modded armour.
            ArmorIDs.Body.Sets.showsShouldersWhileJumping[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 16;
            Item.vanity = true;
        }
    }

    // The lower robe hem and feet; the hem sways through the NPC's walk frames.
    [AutoloadEquip(EquipType.Legs)]
    public class NecromancerBoots : ModItem
    {
        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
            ArmorIDs.Legs.Sets.HidesTopSkin[equipSlot] = true;
            ArmorIDs.Legs.Sets.HidesBottomSkin[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 16;
            Item.vanity = true;
        }
    }
}
