using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Ranged
{
    [LegacyName("OldStuddedLeatherArmor")]
    [LegacyName("OldLeatherArmor")]
    [AutoloadEquip(EquipType.Body)]
    public class LeatherArmor : ModItem
    {
        public static int FlatDmg = 2;
        public static int AmmoChance = 20;  //changing this number has no effect since an ammo consumption chance stat doesn't exist
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(FlatDmg, AmmoChance);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 4;
            Item.rare = ItemRarityID.Green;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Ranged).Flat += FlatDmg;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<LeatherHelmet>() && legs.type == ModContent.ItemType<LeatherGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.ammoCost80 = true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Leather, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 150);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
