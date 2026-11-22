using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.DarkmoonKnightSet
{
    // Darkmoon NPC helmet on the Black Knight animation rig. Vanity only; no drop or recipe.
    [AutoloadEquip(EquipType.Head)]
    public class DarkmoonKnightHelmet : ModItem
    {
        public override string Texture => "tsorcRevamp/Content/Items/Armor/Enemy/DarkmoonKnightSet/DarkmoonKnightHelmet";

        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
            ArmorIDs.Head.Sets.DrawHead[equipSlot] = false;
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Body)]
    public class DarkmoonKnightBreastplate : ModItem
    {
        public override string Texture => "tsorcRevamp/Content/Items/Armor/Enemy/DarkmoonKnightSet/DarkmoonKnightBreastplate";

        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
            ArmorIDs.Body.Sets.HidesTopSkin[equipSlot] = true;
            ArmorIDs.Body.Sets.HidesArms[equipSlot] = true;
            ArmorIDs.Body.Sets.HidesHands[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 22;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Legs)]
    public class DarkmoonKnightGreaves : ModItem
    {
        public override string Texture => "tsorcRevamp/Content/Items/Armor/Enemy/DarkmoonKnightSet/DarkmoonKnightGreaves";

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
