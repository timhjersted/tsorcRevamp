using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.TibianAmazonSet
{
    // The NPC's complete hair/face and olive outfit, fitted to player armor. Vanity only; no drop or recipe.
    [AutoloadEquip(EquipType.Head)]
    public class TibianAmazonHeadpiece : ModItem
    {
        public override string Texture => "tsorcRevamp/Content/Items/Armor/Enemy/TibianAmazonSet/TibianAmazonHeadpiece";

        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
            ArmorIDs.Head.Sets.DrawHead[equipSlot] = false;
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Body)]
    public class TibianAmazonTunic : ModItem
    {
        public override string Texture => "tsorcRevamp/Content/Items/Armor/Enemy/TibianAmazonSet/TibianAmazonTunic";

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
            Item.width = 20;
            Item.height = 16;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Legs)]
    public class TibianAmazonBoots : ModItem
    {
        public override string Texture => "tsorcRevamp/Content/Items/Armor/Enemy/TibianAmazonSet/TibianAmazonBoots";

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
