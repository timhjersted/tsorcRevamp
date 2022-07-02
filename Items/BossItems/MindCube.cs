using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.Okiku.FirstForm;
using tsorcRevamp.NPCs.Bosses.Okiku.SecondForm;
using tsorcRevamp.NPCs.Bosses.Okiku.ThirdForm;


namespace tsorcRevamp.Items.BossItems
{
    class MindCube : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Summons Attraidies, the Mindflayer King \n" +
                "This is it. The final battle. \n" +
                "Item is not consumed on use. To find the light and dark shards needed to craft this in adventure mode... Seek the Dark Tower of Attraidies.");
        }

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.LightRed;
            Item.width = 38;
            Item.height = 34;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.maxStack = 1;
            Item.consumable = false;
        }


        public override bool? UseItem(Player player)
        {
            NPC.NewNPC(NPC.GetBossSpawnSource(player.whoAmI), (int)player.position.X, (int)player.position.Y - 64, ModContent.NPCType<DarkShogunMask>());            
            return true;
        }
        public override bool CanUseItem(Player player)
        {
            if (NPC.AnyNPCs(ModContent.NPCType<DarkShogunMask>())
                || NPC.AnyNPCs(ModContent.NPCType<DarkDragonMask>())
                || NPC.AnyNPCs(ModContent.NPCType<Okiku>())
                || NPC.AnyNPCs(ModContent.NPCType<BrokenOkiku>())
                )
            {
                return false;
            }
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                recipe.AddIngredient(ItemID.LightShard, 99);
                recipe.AddIngredient(ItemID.DarkShard, 99);
            }
            recipe.AddIngredient(ModContent.ItemType<CrestOfFire>(), 1);
            recipe.AddIngredient(ModContent.ItemType<CrestOfWater>(), 1);
            recipe.AddIngredient(ModContent.ItemType<CrestOfEarth>(), 1);
            recipe.AddIngredient(ModContent.ItemType<CrestOfSky>(), 1);
            recipe.AddIngredient(ModContent.ItemType<CrestOfCorruption>(), 1);
            recipe.AddIngredient(ModContent.ItemType<CrestOfSteel>(), 1);
            recipe.AddIngredient(ModContent.ItemType<CrestOfLife>(), 1);
            recipe.AddIngredient(ModContent.ItemType<CrestOfStone>(), 1);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }
    }
}
