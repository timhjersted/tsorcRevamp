using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.Enemy.SkeletonSet
{
    // Register the supplied shared-name sheets directly, preserving their filenames.
    public class SkeletonHeadpiece : ModItem
    {
        internal static int HeadSlot { get; private set; } = -1;

        public override void Load()
        {
            if (!Main.dedServ)
                EquipLoader.AddEquipTexture(Mod, "tsorcRevamp/Content/Items/Armor/Enemy/SkeletonSet/Skeleton_Head", EquipType.Head, this);
        }

        public override void SetStaticDefaults()
        {
            if (Main.dedServ) return;
            int slot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
            HeadSlot = slot;
            ArmorIDs.Head.Sets.DrawHead[slot] = false;
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 22;
            Item.vanity = true;

            // AddEquipTexture runs in Load(), before this item has a Type, so tModLoader files the slot under item 0 and
            // never applies it on SetDefaults. Look the slot up by name instead (-1 on a dedicated server: no textures).
            Item.headSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
        }
    }

    public class SkeletonRibcage : ModItem
    {
        public override void Load()
        {
            if (!Main.dedServ)
                EquipLoader.AddEquipTexture(Mod, "tsorcRevamp/Content/Items/Armor/Enemy/SkeletonSet/Skeleton_Body", EquipType.Body, this);
        }

        public override void SetStaticDefaults()
        {
            if (Main.dedServ) return;
            int slot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
            ArmorIDs.Body.Sets.HidesTopSkin[slot] = true;
            ArmorIDs.Body.Sets.HidesArms[slot] = true;
            ArmorIDs.Body.Sets.HidesHands[slot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.vanity = true;

            // AddEquipTexture runs in Load(), before this item has a Type, so tModLoader files the slot under item 0 and
            // never applies it on SetDefaults. Look the slot up by name instead (-1 on a dedicated server: no textures).
            Item.bodySlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
        }
    }

    public class SkeletonLegBones : ModItem
    {
        public override void Load()
        {
            if (!Main.dedServ)
                EquipLoader.AddEquipTexture(Mod, "tsorcRevamp/Content/Items/Armor/Enemy/SkeletonSet/Skeleton_Legs", EquipType.Legs, this);
        }

        public override void SetStaticDefaults()
        {
            if (Main.dedServ) return;
            int slot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
            ArmorIDs.Legs.Sets.HidesTopSkin[slot] = true;
            ArmorIDs.Legs.Sets.HidesBottomSkin[slot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 16;
            Item.vanity = true;

            // AddEquipTexture runs in Load(), before this item has a Type, so tModLoader files the slot under item 0 and
            // never applies it on SetDefaults. Look the slot up by name instead (-1 on a dedicated server: no textures).
            Item.legSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
        }
    }
}



