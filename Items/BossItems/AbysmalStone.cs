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
            Item.width = 22;
            Item.height = 24;
            Item.consumable = false;
            Item.maxStack = 1;
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
                //Main.NewText("Nothing happens...", 175, 75, 255); //seems to write the text on every tick the item is being "used" when returning false
                return false;
            }
        }
        public override void AddRecipes()
        {
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                Terraria.Recipe recipe = CreateRecipe();
                recipe.AddIngredient(Mod.Find<ModItem>("RedTitanite").Type, 5);
                recipe.AddIngredient(Mod.Find<ModItem>("WhiteTitanite").Type, 5);
                recipe.AddIngredient(Mod.Find<ModItem>("CursedSoul").Type, 35);
                recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 1000);
                recipe.AddTile(TileID.DemonAltar);
                
                recipe.Register();
            }
        }
    }
}