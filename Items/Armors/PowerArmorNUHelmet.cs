using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class PowerArmorNUHelmet : ModItem
    {
        public static float MeleeDmg = 20f;
        public static float RangedDmg = 24f;
        public static float MagicDmg = 11f;
        public static float CritChance = 17f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MeleeDmg, RangedDmg, MagicDmg, CritChance);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 9;
            Item.rare = ItemRarityID.Lime;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Melee) += MeleeDmg / 100f;
            player.GetDamage(DamageClass.Ranged) += RangedDmg / 100f;
            player.GetDamage(DamageClass.Magic) += MagicDmg / 100f;
            player.GetCritChance(DamageClass.Generic) += CritChance;
            player.breath = 10800;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SoulofMight, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
