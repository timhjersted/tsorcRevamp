using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.Enemy.FireLurkerSet
{
    // NPC outfit converted to player equipment. Vanity only; no recipe or drop.
    [AutoloadEquip(EquipType.Head)]
    public class FireLurkerHeadpiece : ModItem
    {
        public override string Texture => "tsorcRevamp/Content/Items/Armor/Enemy/FireLurkerSet/FireLurkerHeadpiece";

        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
            ArmorIDs.Head.Sets.DrawHead[equipSlot] = false;
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 16;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Body)]
    public class FireLurkerHide : ModItem
    {
        public override string Texture => "tsorcRevamp/Content/Items/Armor/Enemy/FireLurkerSet/FireLurkerHide";

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
            Item.width = 22;
            Item.height = 18;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Legs)]
    public class FireLurkerLegs : ModItem
    {
        public override string Texture => "tsorcRevamp/Content/Items/Armor/Enemy/FireLurkerSet/FireLurkerLegs";

        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
            ArmorIDs.Legs.Sets.HidesTopSkin[equipSlot] = true;
            ArmorIDs.Legs.Sets.HidesBottomSkin[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 20;
            Item.vanity = true;
        }
    }
}




