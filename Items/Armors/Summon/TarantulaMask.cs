using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Head)]
    public class TarantulaMask : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Increases minion damage by 10%" +
                "\nSet Bonus: Increases your max number of minions and turrets by 1" +
                "\nIncreases critical strike damage to 250%");
        }
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.defense = 6;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
                player.GetDamage(DamageClass.Summon) += 0.1f;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<TarantulaCarapace>() && legs.type == ModContent.ItemType<TarantulaLegs>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.maxMinions += 1;
            player.maxTurrets += 1;
            player.GetModPlayer<tsorcRevampPlayer>().CritDamage250 = true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpiderMask);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 8000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.SpiderMask);
            recipe2.AddIngredient(ItemID.OrichalcumHeadgear);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}