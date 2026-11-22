using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.DarkrootSentrySet
{
    // Original hornless cyclops armor. Vanity only, ready for a future sentry puppet.
    [AutoloadEquip(EquipType.Head)]
    public class DarkrootSentryHelmet : ModItem
    {
        public override string Texture => "tsorcRevamp/Content/Items/Armor/Enemy/DarkrootSentrySet/DarkrootSentryHelmet";

        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
            ArmorIDs.Head.Sets.DrawHead[equipSlot] = false;
        }

        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 16;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Body)]
    public class DarkrootSentryBreastplate : ModItem
    {
        public override string Texture => "tsorcRevamp/Content/Items/Armor/Enemy/DarkrootSentrySet/DarkrootSentryBreastplate";

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
            Item.width = 18;
            Item.height = 24;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Legs)]
    public class DarkrootSentryGreaves : ModItem
    {
        public override string Texture => "tsorcRevamp/Content/Items/Armor/Enemy/DarkrootSentrySet/DarkrootSentryGreaves";

        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
            ArmorIDs.Legs.Sets.HidesTopSkin[equipSlot] = true;
            ArmorIDs.Legs.Sets.HidesBottomSkin[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 18;
            Item.vanity = true;
        }
    }
}
