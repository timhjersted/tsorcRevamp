using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Defensive.Shields
{

    [AutoloadEquip(EquipType.Shield)]
    public class SpikedIronShield : ModItem
    {
        public static float Thorns = 100f;
        public static float DR = 5f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Thorns, DR);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.defense = 3;
            Item.rare = ItemRarityID.Green;
            Item.accessory = true;
            Item.value = PriceByRarity.Green_2;
        }        
        public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player)
        {
            if (incomingItem.type == ModContent.ItemType<IronShield>() || incomingItem.type == ModContent.ItemType<AncientDemonShield>())
            {
                return false;
            }
            return base.CanAccessoryBeEquippedWith(equippedItem, incomingItem, player);
        }

        public override void UpdateEquip(Player player)
        {
            // Under Active Shields Revamp the passive thorns + % DR become an on-block reflect instead.
            if (!tsorcRevampActiveShieldPlayer.ActiveFor(player))
            {
                player.thorns += Thorns / 100f;
                player.endurance += DR / 100f;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<IronShield>());
            recipe.AddIngredient(ItemID.Stinger, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }

}
