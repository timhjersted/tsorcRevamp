using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems
{
    class JungleFeather : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Summons the Jungle Wyvern \n" + "An ancient beast that once guarded an advanced civilization, \n" + "long since forgotten. To this day, it watches over the lost \n" + "city, ripping to shreds any traveler who should discover it.");
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.maxStack = 1;
            Item.consumable = false;
            Item.rare = ItemRarityID.LightRed;
            Item.consumable = false;
        }


        public override bool? UseItem(Player player)
        {
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.JungleWyvern.JungleWyvernHead>()))
            {
                return false;
            }
            if (Main.dayTime)
            {
                UsefulFunctions.BroadcastText("The ancient Jungle Wyvern remains deep in slumber... Retry at night.", 175, 75, 255);
            }
            else if (!player.ZoneRockLayerHeight)
            {
                UsefulFunctions.BroadcastText("The ancient Jungle Wyvern must be summoned underground.", 175, 75, 255);
            }
            else
            {
                UsefulFunctions.BroadcastText("A rumbling thunder shakes the ground below you... ", 175, 75, 255);
                NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.JungleWyvern.JungleWyvernHead>());
            }
            return true;
        }

        public override void AddRecipes()
        {
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.Feather);
                //recipe.AddIngredient(ItemID.ShadowScale, 1);
                recipe.AddIngredient(ItemID.Bone, 1);
                recipe.AddTile(TileID.DemonAltar);
                 
                recipe.Register();
            }
        }
    }
}
