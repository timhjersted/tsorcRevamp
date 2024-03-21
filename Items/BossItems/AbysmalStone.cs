using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.BossItems
{
    class AbysmalStone : ModItem
    {

        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 24;
            Item.consumable = false;
            Item.value = 700000;
            Item.rare = ItemRarityID.Pink;
            Item.useTime = 45;
            Item.useAnimation = 45;
            Item.scale = 1f;
            Item.useStyle = ItemUseStyleID.HoldUp;
        }
        public override bool? UseItem(Player player)
        {
            //if (!Main.dayTime && !NPC.AnyNPCs(mod.NPCType("Blight"))
            if (!Main.dayTime && !NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.AbysmalOolacileSorcerer>())) //placeholder, use above
            {
                NPC.SpawnOnPlayer(Main.myPlayer, ModContent.NPCType<NPCs.Bosses.SuperHardMode.AbysmalOolacileSorcerer>()); //placeholder
                return true;
            }
            else
            {
                //UsefulFunctions.BroadcastText("Nothing happens...", 175, 75, 255); //seems to write the text on every tick the item is being "used" when returning false
                return false;//don't forget to put the text into the lang files if you re-enable it
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<RedTitanite>(), 5);
            recipe.AddIngredient(ModContent.ItemType<WhiteTitanite>(), 5);
            recipe.AddIngredient(ModContent.ItemType<CursedSoul>(), 30);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1000);
            recipe.AddCondition(tsorcRevampWorld.AdventureModeDisabled);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}