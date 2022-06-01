using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems
{
    class DyingEarthCrystal : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("The fading Crystal of Earth. \n" + "Will summon Lich. \n" + "Item is non-consumable");
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
            //Main.NewText("Earth Fiend Lich ascends from the ground", Color.GreenYellow);
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.Fiends.EarthFiendLich>());
            return true;
        }
        public override bool CanUseItem(Player player)
        {

            return (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Fiends.EarthFiendLich>()));
        }


        public override void AddRecipes()
        {
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                Recipe recipe = new Recipe(Mod);
                recipe.AddIngredient(Mod.GetItem("WhiteTitanite"), 10);
                recipe.AddIngredient(Mod.GetItem("DarkSoul"), 1000);
                recipe.AddTile(TileID.DemonAltar);
                recipe.SetResult(this, 1);
                recipe.AddRecipe();
            }
        }

    }
}
