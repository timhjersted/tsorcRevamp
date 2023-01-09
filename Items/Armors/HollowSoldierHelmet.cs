using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class HollowSoldierHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = true;
            Tooltip.SetDefault("Nullifies the negative stamina regen of the Dragon Crest Shield");
        }
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 20;
            Item.defense = 2;
            Item.rare = ItemRarityID.Blue;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            if (player.GetModPlayer<tsorcRevampPlayer>().DragonCrestShieldEquipped)
            {
                player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += 0.15f;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.IronHelmet);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 750);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }
    }
}
