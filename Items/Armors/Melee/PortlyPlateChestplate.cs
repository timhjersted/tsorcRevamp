using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Melee
{
    [AutoloadEquip(EquipType.Body)]
    public class PortlyPlateChestplate : ModItem
    {
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 10;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Melee) += 0.1f;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<PortlyPlateHelmet>() && legs.type == ModContent.ItemType<PortlyPlateGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.lifeRegen += 2;
            player.noKnockback = true;

            if (player.statLife <= (player.statLifeMax2 / 4))
            {
                player.lifeRegen += 3;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.GladiatorBreastplate);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1800);
            recipe.AddTile(TileID.DemonAltar);

            //recipe.Register();
        }
    }
}
