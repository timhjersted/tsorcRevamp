using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems
{
    class FieryEgg : ModItem
    {

        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Summons The Rage \n" + "You must sacrifice this at the Demon Altar \n" + "within the Cathedral in the Underworld");
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
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.TheRage>()))
            {
                return false;
            }
            else if (!player.ZoneUnderworldHeight)
            {
                UsefulFunctions.BroadcastText("You can only use this in the Underworld.");
            }
            else
            {
                NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.TheRage>());
            }
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.CobaltBar, 10);
            recipe.AddIngredient(ItemID.ShadowScale, 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddCondition(tsorcRevampWorld.AdventureModeDisabled);

            recipe.Register();
        }
    }
}
