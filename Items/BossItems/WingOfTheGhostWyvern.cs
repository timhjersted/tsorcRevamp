using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems
{
    class WingOfTheGhostWyvern : ModItem
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wing of the Ghost Wyvern");
            Tooltip.SetDefault("Frees the Wyvern Mage's Shadow from its Glass Prison.\n" +
                "The Wyvern Mage once created a shadow form of himself, cursed by the powers of the Abyss\n" +
                "It was so hideous that the Mage imprisoned his shadow self in a massive glass cage, enchanted by dark magic\n");

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
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>()))
            {
                return false;
            }
            if (Main.dayTime)
            {
                UsefulFunctions.BroadcastText("The Ghost Wyvern is not present in this dimension... Retry at night.", 175, 75, 255);
                return false;
            }
            else
            {
                UsefulFunctions.BroadcastText("You think death is the end? You haven't begun to understand my powers, Red... ", 175, 75, 255);
                NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>());
            }
            return true;
        }

        public override void AddRecipes()
        {
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(Mod.Find<ModItem>("WingOfTheFallen").Type, 1);
                recipe.AddIngredient(Mod.Find<ModItem>("FlameOfTheAbyss").Type, 20);
                recipe.AddIngredient(Mod.Find<ModItem>("SoulOfAttraidies").Type, 1);
                recipe.AddTile(TileID.DemonAltar);

                recipe.Register();
            }
        }
    }
}
