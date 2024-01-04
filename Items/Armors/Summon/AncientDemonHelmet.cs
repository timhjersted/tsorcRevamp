using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Head)]
    public class AncientDemonHelmet : ModItem
    {
        public static float Dmg = 22f;
        public static float CritChance = 8f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, CritChance);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.defense = 6;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon) += Dmg / 100f;
            player.GetCritChance(DamageClass.Summon) += CritChance;
        }


        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ObsidianHelm);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
