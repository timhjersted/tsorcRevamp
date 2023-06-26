using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.BossItems
{
    class WingOfTheFallen : ModItem
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Wing of the Fallen");
            /* Tooltip.SetDefault("Summons the Wyvern Mage, a powerful demon adept with the forces of magic and lightning\n" +
                "The Wyvern Mage is known to command the loyalty of a fiery Wyvern with white scales as strong as steel\n" +
                "If you cannot defeat the Wyvern Mage after several tries, it would be wise\n" +
                "to return later when you've become stronger and possess greater abilities."); */
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
                UsefulFunctions.BroadcastText(LaUtils.GetTextValue("Items.WingOfTheFallen.WrongTime"), 175, 75, 255);
            }
            else
            {
                UsefulFunctions.BroadcastText(LaUtils.GetTextValue("Items.WingOfTheFallen.Summon"), 175, 75, 255);
                NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.WyvernMage.WyvernMage>());
                NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.WyvernMage.MechaDragonHead>());
            }
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SoulofFlight, 3);
            recipe.AddIngredient(ItemID.Feather, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddCondition(tsorcRevampWorld.AdventureModeDisabled);

            recipe.Register();
        }
    }
}
