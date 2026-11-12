using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.Enemy.ElementalSet
{
    // Elemental NPC converted to player equipment. Vanity only; no recipe or drop.
    // The sheets are built by tsorcDocs/.agent scratchpad/hydris-elemental-armor/build_hydris_elemental_armor.py.
    [AutoloadEquip(EquipType.Head)]
    public class ElementalHeadpiece : ModItem
    {
        public override void SetStaticDefaults()
        {
            // The blue dome replaces the whole head, face included.
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
    public class ElementalBreastplate : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Blue-skinned arms and hands are painted on the sheet; the green front pad lives in the shoulder cap.
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
            ArmorIDs.Body.Sets.HidesTopSkin[equipSlot] = true;
            ArmorIDs.Body.Sets.HidesArms[equipSlot] = true;
            ArmorIDs.Body.Sets.HidesHands[equipSlot] = true;
            ArmorIDs.Body.Sets.showsShouldersWhileJumping[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 18;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Legs)]
    public class ElementalGreaves : ModItem
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
            Item.height = 14;
            Item.vanity = true;
        }
    }
}
