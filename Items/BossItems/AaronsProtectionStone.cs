using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems
{
    class AaronsProtectionStone : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("The volcanic stoned etched in Aaron's image" +
                                "\nSaid to protect the one who carries it in times of despair" +
                                "\nUse this at the top of The Temple Shrine of The Wall if your first attempt" +
                                "\ndoes not succeed. (Use it don't drop it in lava.)"); //+
                                //"\nBut first: save, quit and reload before each time you resummon him.");
        }
        public override void SetDefaults()
        {
            item.width = 14;
            item.height = 26;
            item.consumable = false;
            item.maxStack = 1;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 5;
            item.useTime = 5;
            //item.UseSound = SoundID.Item21;
            item.value = 1000;
            item.rare = ItemRarityID.Blue;
        }

        public override bool CanUseItem(Player player)
        {
            return player.ZoneUnderworldHeight && !NPC.AnyNPCs(NPCID.WallofFlesh);
        }

        public override bool UseItem(Player player)
        {
            Main.NewText("A Gate has been opened. The Wall of Flesh has passed into this dimension!... ", 175, 75, 255);
            NPC.NewNPC((int)Main.player[Main.myPlayer].position.X - (1070), (int)Main.player[Main.myPlayer].position.Y - 150, NPCID.WallofFlesh, 1);
            return true;
        }
        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.GuideVoodooDoll, 3);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
