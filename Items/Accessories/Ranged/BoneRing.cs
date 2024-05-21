using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Accessories.Defensive.Rings;

namespace tsorcRevamp.Items.Accessories.Ranged
{
    [AutoloadEquip(EquipType.HandsOn)]

    public class BoneRing : ModItem
    {
        public static float RangedDmgCrit = 5f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(RangedDmgCrit);
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<RingOfTheBlueEye>();
        }
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 22;
            Item.defense = 2;
            Item.accessory = true;
            Item.value = PriceByRarity.Green_2;
            Item.rare = ItemRarityID.Green;
            ArmorIDs.Body.Sets.HidesHands[Item.handOnSlot] = true; //TODO maybe? something about "booleans in PlayerDrawSet" ?
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Bone, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 8000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Ranged) += RangedDmgCrit / 100f;
            player.GetCritChance(DamageClass.Ranged) += RangedDmgCrit;
            player.GetModPlayer<tsorcRevampPlayer>().BoneRing = true;
        }

    }
}
