using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems
{
    class GrassyEgg : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Summons The Hunter \n" + "You must sacrifice this at a Demon Altar in the Jungle far to the West");
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
            bool zoneJ = player.ZoneJungle;
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.TheHunter>()))
            {
                return false;
            }
            else if (!zoneJ)
            {
                Main.NewText("You can only use this in the Jungle.");
            }
            else
            {
                NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.TheHunter>());
            }
            return true;
        }

        public override void AddRecipes()
        {
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                Recipe recipe = new Recipe(Mod);
                recipe.AddIngredient(ItemID.AdamantiteOre, 30);
                recipe.AddIngredient(ItemID.ShadowScale, 1);
                recipe.AddTile(TileID.DemonAltar);
                recipe.SetResult(this, 1);
                recipe.AddRecipe();
            }
        }
    }
}
