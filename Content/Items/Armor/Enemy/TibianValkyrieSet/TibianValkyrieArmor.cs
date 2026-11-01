using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.TibianValkyrieSet
{
    // The NPC's complete hair/face and silver outfit, fitted to player armor. Vanity only; no drop or recipe.
    [AutoloadEquip(EquipType.Head)]
    public class TibianValkyrieHeadpiece : ModItem
    {
        public override string Texture => "tsorcRevamp/Content/Items/Armor/Enemy/TibianValkyrieSet/TibianValkyrieHeadpiece";

        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
            ArmorIDs.Head.Sets.DrawHead[equipSlot] = false;
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 26;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Body)]
    public class TibianValkyrieBreastplate : ModItem
    {
        public override string Texture => "tsorcRevamp/Content/Items/Armor/Enemy/TibianValkyrieSet/TibianValkyrieBreastplate";

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
            Item.width = 24;
            Item.height = 20;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Legs)]
    public class TibianValkyrieGreaves : ModItem
    {
        public override string Texture => "tsorcRevamp/Content/Items/Armor/Enemy/TibianValkyrieSet/TibianValkyrieGreaves";

        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
            ArmorIDs.Legs.Sets.HidesTopSkin[equipSlot] = true;
            ArmorIDs.Legs.Sets.HidesBottomSkin[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 10;
            Item.vanity = true;
        }
    }
}
