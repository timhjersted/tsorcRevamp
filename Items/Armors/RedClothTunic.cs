using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [LegacyName("RedMageTunic")]
    [AutoloadEquip(EquipType.Body)]
    public class RedClothTunic : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Increases magic damage by 1 flat\nSet bonus: Increases max mana by 50, decreases mana costs by 10%");
        }
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 36;
            Item.defense = 4;
            Item.value = 27000;
            Item.rare = ItemRarityID.Blue;
            ArmorIDs.Body.Sets.HidesHands[Item.bodySlot] = true; //TODO maybe? 
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Magic).Flat += 1;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Silk, 5);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 250);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
