using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Items.BossItems
{
    class AbysmalStone : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Summons the Abysmal Oolacile Sorcerer." +
                                "\nCan only be used at night.");
        }
        public override void SetDefaults()
        {
            item.width = 22;
            item.height = 24;
            item.consumable = false;
            item.maxStack = 1;
            item.value = 700000;
            item.rare = ItemRarityID.Pink;
            item.useTime = 45;
            item.useAnimation = 45;
            item.scale = 1f;
            item.useStyle = ItemUseStyleID.HoldingUp;
        }
        public override bool UseItem(Player player)
        {
            //if (!Main.dayTime && !NPC.AnyNPCs(mod.NPCType("Blight"))
            if (!Main.dayTime && !NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.AbysmalOolacileSorcerer>())) //placeholder, use above
            {
                NPC.SpawnOnPlayer(Main.myPlayer, ModContent.NPCType<NPCs.Bosses.SuperHardMode.AbysmalOolacileSorcerer>()); //placeholder
                return true;
            }
            else
            {
                //Main.NewText("Nothing happens...", 175, 75, 255); //seems to write the text on every tick the item is being "used" when returning false
                return false;
            }
        }
        public override void AddRecipes()
        {
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                ModRecipe recipe = new ModRecipe(mod);
                recipe.AddIngredient(mod.GetItem("RedTitanite"), 5);
                recipe.AddIngredient(mod.GetItem("WhiteTitanite"), 5);
                recipe.AddIngredient(mod.GetItem("CursedSoul"), 35);
                recipe.AddIngredient(mod.GetItem("DarkSoul"), 1000);
                recipe.AddTile(TileID.DemonAltar);
                recipe.SetResult(this);
                recipe.AddRecipe();
            }
        }
    }
}