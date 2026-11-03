using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.Enemy.HydrisNecromancerSet
{
    // NPC outfit converted to player equipment. Vanity only; no recipe or drop.
    [AutoloadEquip(EquipType.Head)]
    public class HydrisNecromancerHeadpiece : ModItem
    {
        public override string Texture => "tsorcRevamp/Content/Items/Armor/Enemy/HydrisNecromancerSet/HydrisNecromancerHeadpiece";

        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
            ArmorIDs.Head.Sets.DrawHead[equipSlot] = false;
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 22;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Body)]
    public class HydrisNecromancerRobe : ModItem
    {
        public override string Texture => "tsorcRevamp/Content/Items/Armor/Enemy/HydrisNecromancerSet/HydrisNecromancerRobe";

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
            Item.height = 10;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Legs)]
    public class HydrisNecromancerBoots : ModItem
    {
        public override string Texture => "tsorcRevamp/Content/Items/Armor/Enemy/HydrisNecromancerSet/HydrisNecromancerBoots";

        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
            ArmorIDs.Legs.Sets.HidesTopSkin[equipSlot] = true;
            ArmorIDs.Legs.Sets.HidesBottomSkin[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 20;
            Item.vanity = true;
        }
    }
}



