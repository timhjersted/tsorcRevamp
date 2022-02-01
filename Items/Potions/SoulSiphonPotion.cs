using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions
{
    public class SoulSiphonPotion : ModItem
    {
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Enemies drop 20% more Dark souls\n"
                                + "Consumable souls' drop chance is increased by 50%\n"
                                + "Soul pickup range greatly increased");

            ItemID.Sets.ItemIconPulse[item.type] = true; // Makes item pulsate in world.
        }

        public override void SetDefaults()
        {
            item.width = 20;
            item.height = 30;
            item.useStyle = ItemUseStyleID.EatingUsing;
            item.useAnimation = 17;
            item.useTime = 17;
            item.useTurn = true;
            item.UseSound = SoundID.Item3;
            item.maxStack = 99;
            item.consumable = true;
            item.rare = ItemRarityID.Lime;
            item.value = 5000;
            item.buffType = ModContent.BuffType<Buffs.SoulSiphon>();
            item.buffTime = 25200;
        }
        public override void PostUpdate()
        {
            Lighting.AddLight(item.Center, 0.3f, 0.7f, 0.12f);
        }
        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.BottledWater);
            recipe.AddIngredient(ItemID.Bone, 5);
            recipe.AddIngredient(mod.ItemType("EphemeralDust"), 20);
            recipe.AddIngredient(mod.ItemType("LostUndeadSoul"));
            recipe.AddTile(TileID.Bottles);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}