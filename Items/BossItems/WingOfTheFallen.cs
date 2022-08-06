using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems
{
    class WingOfTheFallen : ModItem
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wing of the Fallen");
            Tooltip.SetDefault("Summons the Wyvern Mage, a powerful demon adept with the forces of magic and lightning\n" +
                "The Wyvern Mage is known to command the loyalty of a fiery Wyvern with white scales as strong as steel\n" +
                "If you cannot defeat the Wyvern Mage after several tries, it would be wise\n" +
                "to return later when you've become stronger and possess greater abilities.");
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
            if (Main.dayTime)
            {
                UsefulFunctions.BroadcastText("The Wyvern Mage is not present in this dimension... Retry at night.", 175, 75, 255);
            }
            else
            {
                UsefulFunctions.BroadcastText("It was a mistake to summon me... ", 175, 75, 255);
                NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.WyvernMage.WyvernMage>());
            }
            return true;
        }

        public override void AddRecipes()
        {
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.SoulofFlight, 15);
                recipe.AddIngredient(ItemID.Feather, 13);
                recipe.AddIngredient(ItemID.ShadowScale, 1);
                recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100);
                recipe.AddTile(TileID.DemonAltar);

                recipe.Register();
            }
        }
    }
}
