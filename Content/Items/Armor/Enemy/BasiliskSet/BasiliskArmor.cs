using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.Enemy.BasiliskSet
{
    // Cosmetic NPC conversion; no recipe, drop or combat-stat changes.
    [AutoloadEquip(EquipType.Head)]
    public class BasiliskHeadpiece : ModItem
    {
        internal static int HeadSlot = -1;
        public const int MouthFrames = 12;
        public const int MouthFrameTicks = 8;

        public override void SetStaticDefaults()
        {
            HeadSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
            ArmorIDs.Head.Sets.DrawHead[HeadSlot] = false;
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(MouthFrameTicks, MouthFrames));
        }

        public override void SetDefaults()
        {
            Item.width = 44;
            Item.height = 24;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Body)]
    public class BasiliskCarapace : ModItem
    {
        public override void SetStaticDefaults()
        {
            int slot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
            ArmorIDs.Body.Sets.HidesTopSkin[slot] = true;
            ArmorIDs.Body.Sets.HidesArms[slot] = true;
            ArmorIDs.Body.Sets.HidesHands[slot] = true;
            ArmorIDs.Body.Sets.showsShouldersWhileJumping[slot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 16;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Legs)]
    public class BasiliskGreaves : ModItem
    {
        public override void SetStaticDefaults()
        {
            int slot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
            ArmorIDs.Legs.Sets.HidesTopSkin[slot] = true;
            ArmorIDs.Legs.Sets.HidesBottomSkin[slot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 16;
            Item.vanity = true;
        }
    }
}
