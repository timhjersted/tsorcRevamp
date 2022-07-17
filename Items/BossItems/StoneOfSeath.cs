using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems
{
    class StoneOfSeath : ModItem
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Stone of Seath");
            Tooltip.SetDefault("Summons Seath the Scaleless, a great dragon granted the title of Duke by Lord Gwyn for his \n" +
                "assistance in defeating the Everlasting Dragons and given a fragment of a Lord Soul. Seath \n" +
                "was driven insane during his research on the Scales of Immortality, which he could never \n" +
                "obtain. Ironically, he is now an immortal himself, a true Undead by means of his research \n" +
                "into the Primordial Crystal, which he stole from the dragons when he defected.");
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

        public override bool CanUseItem(Player player)
        {
            bool canUse = true;
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessHead>()))
            {
                canUse = false;
            }
            return canUse;
        }

        public override bool? UseItem(Player player)
        {

            UsefulFunctions.BroadcastText("Thy death will only fuel my immortality, Red... ", 175, 75, 255);
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessHead>());
            return true;
        }


        public override void AddRecipes()
        {
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ModContent.ItemType<BlueTitanite>(), 10);
                recipe.AddIngredient(ModContent.ItemType<DragonEssence>(), 15);
                recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1000);
                recipe.AddTile(TileID.DemonAltar);
                
                recipe.Register();
            }
        }

    }
}
