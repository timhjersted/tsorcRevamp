using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.BossItems
{
    class LostScrollOfGwyn : ModItem
    {

        public override void SetStaticDefaults()
        {
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
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Gwyn>()))
            {
                return false;
            }
            //if (!Main.bloodMoon) {
            //    Main.NewText("Lord Gwyn ignores your call... you must enter The Abyss to summon the Lord of Cinder!", 175, 75, 255);
            //}
            else
            {
                UsefulFunctions.BroadcastText(LaUtils.GetTextValue("Items.LostScrollOfGwyn.Summon"), 175, 75, 255);
                NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Gwyn>());
            }
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<SoulOfBlight>(), 1);
            recipe.AddIngredient(ModContent.ItemType<SoulOfChaos>(), 1);
            recipe.AddIngredient(ModContent.ItemType<GhostWyvernSoul>(), 1);
            recipe.AddIngredient(ModContent.ItemType<SoulOfArtorias>(), 1);
            recipe.AddIngredient(ModContent.ItemType<BequeathedSoul>(), 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddCondition(tsorcRevampWorld.AdventureModeDisabled);

            recipe.Register();
        }
    }
}
