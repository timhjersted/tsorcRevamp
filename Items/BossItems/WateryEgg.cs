using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems
{
    class WateryEgg : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Summons The Sorrow \n" + "Must be used near the ocean");
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
            bool zoneJ = (player.position.X < 250 * 16 || player.position.X > (Main.maxTilesX - 250) * 16);
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.TheSorrow>()))
            {
                return false;
            }
            else if (!zoneJ)
            {
                UsefulFunctions.BroadcastText("You can only use this in the Ocean.");
            }
            else
            {
                NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.TheSorrow>());
            }
            return true;
        }

        public override void AddRecipes()
        {
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                Recipe recipe = CreateRecipe();

                recipe.AddIngredient(ItemID.MythrilOre, 30);
                recipe.AddIngredient(ItemID.Coral, 1);
                recipe.AddIngredient(ItemID.ShadowScale, 1);
                recipe.AddTile(TileID.DemonAltar);

                recipe.Register();
            }
        }
    }
}
