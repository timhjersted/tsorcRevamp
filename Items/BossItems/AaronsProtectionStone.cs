using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems
{
    class AaronsProtectionStone : ModItem
    {

        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 14;
            Item.height = 26;
            Item.consumable = false;
            Item.maxStack = 1;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 5;
            Item.useTime = 5;
            //item.UseSound = SoundID.Item21;
            Item.value = 1000;
            Item.rare = ItemRarityID.Blue;
        }

        public override bool CanUseItem(Player player)
        {
            return player.ZoneUnderworldHeight && !NPC.AnyNPCs(NPCID.WallofFlesh);
        }

        public override bool? UseItem(Player player)
        {
            UsefulFunctions.BroadcastText("A Gate has been opened. The Great Wall has passed into this dimension!... ", 175, 75, 255);
            NPC.NewNPC(player.GetSource_ItemUse(Item), (int)Main.player[Main.myPlayer].position.X - (1070), (int)Main.player[Main.myPlayer].position.Y - 150, NPCID.WallofFlesh, 1);
            return true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.GuideVoodooDoll, 1);
            //recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
