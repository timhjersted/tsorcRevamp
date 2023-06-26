using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.BossItems
{
    class DyingWindCrystal : ModItem
    {

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.LightRed;
            Item.width = 12;
            Item.height = 12;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 5;
            Item.useTime = 5;
            Item.maxStack = 1;
            Item.consumable = false;
        }

        public override bool? UseItem(Player player)
        {
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>());
            return true;
        }
        public override bool CanUseItem(Player player)
        {
            return (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>()));
        }


        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<DyingWindShard>(), 5);
            recipe.AddIngredient(ModContent.ItemType<RedTitanite>(), 5);
            recipe.AddIngredient(ModContent.ItemType<WhiteTitanite>(), 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddCondition(tsorcRevampWorld.AdventureModeDisabled);
            recipe.Register();
        }
    }
}
