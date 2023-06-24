using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Magic
{
    [AutoloadEquip(EquipType.Head)]
    public class AnkorWatHelmet : ModItem
    {
        public static int MaxMana = 100;
        public static float ManaCost = 6f;
        public static int ManaRegen = 5;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxMana, ManaCost, ManaRegen);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.defense = 14;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.statManaMax2 += MaxMana;

            player.manaCost -= ManaCost / 100f;
            player.manaRegenBonus += ManaRegen;

            if (player.HasBuff(BuffID.ShadowDodge))
            {
                player.manaCost -= ManaCost / 100f;
                player.manaRegen += ManaRegen;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HallowedHeadgear, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
