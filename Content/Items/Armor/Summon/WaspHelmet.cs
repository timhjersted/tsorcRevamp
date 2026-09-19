using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Armor.Summon
{
    [AutoloadEquip(EquipType.Head)]
    public class WaspHelmet : ModItem
    {
        public static float TagStrength = 12f;
        public static float CritChance = 10f;
        public static int MinionSlot = 1;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(TagStrength, MinionSlot, CritChance);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 20;
            Item.defense = 4;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().SummonTagStrength += TagStrength / 100f;
            player.GetCritChance(DamageClass.Summon) += CritChance;
            player.maxMinions += MinionSlot;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.BeeHeadgear, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
