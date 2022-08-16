using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Body)]
    public class WaspArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Increases minion damage by 2 flat + 8%\nIncreases your max number of minions by 1\nSet bonus: 9% increased minion damage,\nincreases your max number of minions and turrets by 1");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 6;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon).Flat += 2f;
            player.GetDamage(DamageClass.Summon) += 0.08f;
            player.maxMinions += 1;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<WaspHelmet>() && legs.type == ModContent.ItemType<WaspGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.GetDamage(DamageClass.Summon) += 0.09f;
            player.maxMinions += 1;
            player.maxTurrets += 1;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.BeeBreastplate, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3300);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
