using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.Enemy.ManhunterSet
{
    [AutoloadEquip(EquipType.Head)]
    public class ManhunterHat : ModItem
    {
        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
            ArmorIDs.Head.Sets.DrawHead[equipSlot] = false;
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 20;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Body)]
    public class ManhunterJerkin : ModItem
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
            Item.width = 26;
            Item.height = 20;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Legs)]
    public class ManhunterBoots : ModItem
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
            Item.height = 16;
            Item.vanity = true;
        }
    }
}
