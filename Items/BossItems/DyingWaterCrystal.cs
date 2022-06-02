using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems
{
    class DyingWaterCrystal : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("The fading Crystal of Water. \n" + "Will summon Kraken. \n" + "Item is non-consumable");
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
            //Main.NewText("Water Fiend Kraken emerges from the depths", Color.DeepSkyBlue);
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.Fiends.WaterFiendKraken>());
            return true;
        }
        public override bool CanUseItem(Player player)
        {
            return (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Fiends.WaterFiendKraken>()));
        }

        public override void AddRecipes()
        {
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ModContent.ItemType<BlueTitanite>(), 10);
                recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1000);
                recipe.AddTile(TileID.DemonAltar);
                
                recipe.Register();
            }
        }

    }
}
