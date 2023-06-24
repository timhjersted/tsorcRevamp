using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Magic
{
    [LegacyName("RedMageTunic")]
    [AutoloadEquip(EquipType.Body)]
    public class RedClothTunic : ModItem
    {
        public static int FlatDmg = 2;
        public static int MaxMana = 40;
        public static float ManaCost = 5f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(FlatDmg, MaxMana, ManaCost);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 36;
            Item.defense = 4;
            Item.rare = ItemRarityID.Green;
            Item.value = PriceByRarity.fromItem(Item);
            ArmorIDs.Body.Sets.HidesHands[Item.bodySlot] = true; //TODO maybe? 
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Magic).Flat += FlatDmg;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<RedClothHat>() && legs.type == ModContent.ItemType<RedClothPants>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.statManaMax2 += MaxMana;
            player.manaCost -= ManaCost / 100f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Silk, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 250);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
