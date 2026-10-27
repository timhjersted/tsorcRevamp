using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Armor.Melee.AncientGoldenSet
{
    [AutoloadEquip(EquipType.Head)]
    public class AncientGoldenHelmet : ModItem
    {
        public const float CritChance = 6f;
        public const int FlatDmg = 3;
        public const float Resistance = 4;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CritChance);
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
            player.GetCritChance(DamageClass.Melee) += CritChance;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return (body.type == ModContent.ItemType<AncientGoldenArmor>() || body.type == ItemID.Gi) && legs.type == ModContent.ItemType<AncientGoldenGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = Language.GetTextValue(Tooltip.Key + "SetBonus", Resistance, FlatDmg);
            player.GetDamage(DamageClass.Melee).Flat += FlatDmg;
            player.endurance += Resistance / 100f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.GoldHelmet, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 250);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
