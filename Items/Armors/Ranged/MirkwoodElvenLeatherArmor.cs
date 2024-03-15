using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Ranged
{
    [AutoloadEquip(EquipType.Body)]
    public class MirkwoodElvenLeatherArmor : ModItem
    {
        public static float Dmg = 19f;
        public static int LifeRegen = 4;
        public static int AmmoChance = 25;  //changing this number has no effect since an ammo consumption chance stat doesn't exist
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, LifeRegen, tsorcRevampPlayer.MythrilOcrichalcumCritDmg, AmmoChance);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 13;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Ranged) += 0.19f;
            player.lifeRegen += 4;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<MirkwoodElvenBlondeHairStyle>() && legs.type == ModContent.ItemType<MirkwoodElvenLeggings>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().MythrilOrichalcumCritDamage = true;
            player.ammoCost75 = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MythrilChainmail);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 6000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.MythrilChainmail);
            recipe2.AddIngredient(ItemID.OrichalcumBreastplate);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}
