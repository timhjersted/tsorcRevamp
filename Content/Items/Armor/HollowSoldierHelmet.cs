using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Armor
{
    [AutoloadEquip(EquipType.Head)]
    public class HollowSoldierHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 20;
            Item.defense = 3;
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
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 750);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
