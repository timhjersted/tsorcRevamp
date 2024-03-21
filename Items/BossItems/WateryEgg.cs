using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.BossItems
{
    class WateryEgg : ModItem
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
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("Items.WateryEgg.WrongLocation"));
            }
            else
            {
                NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.TheSorrow>());
            }
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MythrilBar, 3);
            recipe.AddIngredient(ItemID.Coral, 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddCondition(tsorcRevampWorld.AdventureModeDisabled);

            recipe.Register();
        }
    }
}
