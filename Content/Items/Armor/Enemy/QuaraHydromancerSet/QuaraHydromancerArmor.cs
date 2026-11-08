using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.Enemy.QuaraHydromancerSet
{
    // Converted from the QuaraHydromancer NPC sheet. Vanity only and not obtainable yet (no drop or recipe).
    [AutoloadEquip(EquipType.Head)]
    public class QuaraHydromancerHeadpiece : ModItem
    {
        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
            ArmorIDs.Head.Sets.DrawHead[equipSlot] = false;
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 24;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Body)]
    public class QuaraHydromancerBreastplate : ModItem
    {
        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
            ArmorIDs.Body.Sets.HidesTopSkin[equipSlot] = true;
            ArmorIDs.Body.Sets.HidesArms[equipSlot] = true;
            ArmorIDs.Body.Sets.HidesHands[equipSlot] = true;
            ArmorIDs.Body.Sets.showsShouldersWhileJumping[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 20;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Legs)]
    public class QuaraHydromancerGreaves : ModItem
    {
        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
            ArmorIDs.Legs.Sets.HidesTopSkin[equipSlot] = true;
            ArmorIDs.Legs.Sets.HidesBottomSkin[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 18;
            Item.vanity = true;
        }
    }
}
