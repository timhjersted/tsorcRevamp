using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Ranged
{
    [AutoloadEquip(EquipType.Body)]
    public class TriceratopsBody : ModItem
    {
        public static int FlatDmg = 3;
        public static float SpecialistDmgMult = 20f;
        public static int AmmoChance = 25; //changing this number has no effect since an ammo consumption chance stat doesn't exist
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(FlatDmg, SpecialistDmgMult, AmmoChance);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 7;
            Item.rare = ItemRarityID.Blue;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Ranged).Flat += FlatDmg;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<TriceratopsHead>() && legs.type == ModContent.ItemType<TriceratopsLegs>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.specialistDamage *= 1f + SpecialistDmgMult / 100f;
            player.ammoCost75 = true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.FossilShirt);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
