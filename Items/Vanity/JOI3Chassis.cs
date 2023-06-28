using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Vanity
{
    [AutoloadEquip(EquipType.Body)]
    public class JOI3Chassis : ModItem
    {
        public override void SetStaticDefaults()
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }
            int equipslot = EquipLoader.GetEquipSlot(Mod, "JOI3Chassis_Legs", EquipType.Legs);
            ArmorIDs.Legs.Sets.OverridesLegs[equipslot] = true;
            ArmorIDs.Legs.Sets.HidesBottomSkin[equipslot] = true;
            ArmorIDs.Legs.Sets.HidesTopSkin[equipslot] = true;
            ArmorIDs.Legs.Sets.IncompatibleWithFrogLeg[equipslot] = true;
        }

        public override void SetDefaults()
        {
            Item.vanity = true;
            Item.width = 36;
            Item.height = 32;
            Item.rare = ItemRarityID.Master;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void Load()
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }
            EquipLoader.AddEquipTexture(Mod, "tsorcRevamp/Items/Vanity/JOI3Chassis_Legs", EquipType.Legs, this, "JOI3Chassis_Legs");
        }
        public override void SetMatch(bool male, ref int equipSlot, ref bool robes)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }
            robes = true;
            equipSlot = EquipLoader.GetEquipSlot(Mod, "JOI3Chassis_Legs", EquipType.Legs);
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.CopperBar, 5);
            recipe.AddIngredient(ItemID.IronBar, 5);
            recipe.AddIngredient(ItemID.SilverBar, 5);
            recipe.AddIngredient(ItemID.GoldBar, 5);
            recipe.AddTile(TileID.Anvils);

            recipe.Register();
        }
    }
}