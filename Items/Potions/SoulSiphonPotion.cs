using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions
{
    public class SoulSiphonPotion : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Enemies drop 20% more Dark souls\n"
                                + "Consumable souls' drop chance is increased by 50%\n"
                                + "Soul pickup range greatly increased");

            ItemID.Sets.ItemIconPulse[Item.type] = true; // Makes item pulsate in world.
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 9999;
            Item.consumable = true;
            Item.rare = ItemRarityID.Lime;
            Item.value = 5000;
            Item.buffType = ModContent.BuffType<Buffs.SoulSiphon>();
            Item.buffTime = 25200;
        }
        public override void PostUpdate()
        {
            Lighting.AddLight(Item.Center, 0.3f, 0.7f, 0.12f);
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.BottledWater);
            //recipe.AddIngredient(ItemID.Bone, 5);
            recipe.AddIngredient(ModContent.ItemType<EphemeralDust>(), 25);
            recipe.AddIngredient(ModContent.ItemType<LostUndeadSoul>());
            recipe.AddTile(TileID.Bottles);

            recipe.Register();
        }
    }
}