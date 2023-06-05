using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Melee
{
    [AutoloadEquip(EquipType.Body)]
    public class PortlyPlateChestplate : ModItem
    {
        public static float DamageIncrease = 10f;
        public static int LifeRegen1 = 2;
        public static float LifeThreshold = 25f;
        public static int LifeRegen2 = 3;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(DamageIncrease, LifeRegen1, LifeThreshold, LifeRegen2);
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
            player.GetDamage(DamageClass.Melee) += DamageIncrease / 100f;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<PortlyPlateHelmet>() && legs.type == ModContent.ItemType<PortlyPlateGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.lifeRegen += LifeRegen1;
            player.noKnockback = true;

            if (player.statLife <= (int)(player.statLifeMax2 * (LifeThreshold / 100f)))
            {
                player.lifeRegen += LifeRegen2;
                Dust.NewDust(player.Center, 10, 10, DustID.AmberBolt);
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
