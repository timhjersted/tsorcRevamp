using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems
{
    class StrangeMagicRing : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A strange magic ring that Miakoda tells you will summon Artorias, the Abysswalker." +
                               "\nOne of Gwyn's Four Knights, Artorias is a holy warrior with an unbendable will of steel.");
            // "and unmatched skills with both melee combat and sorceries.\n" +
            // "Miakoda: \"Take heart Red, he will be like nothing you have ever faced before, but if you are successful in defeating him\n" +
            // "it will surely make the rest of our journey to close the seal of the Abyss more easy.\n" +
            // "Indeed, without the powerful ring he possesses, defeating the other 5 guardians I fear will not be possible...\"");
        }

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.LightRed;
            Item.width = 38;
            Item.height = 34;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 5;
            Item.useTime = 5;
            Item.maxStack = 1;
            Item.consumable = false;
        }


        public override bool? UseItem(Player player)
        {
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Artorias>());
            return true;
        }

        public override bool CanUseItem(Player player)
        {
            bool canUse = true;
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Artorias>()))
            {
                canUse = false;
            }
            return canUse;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<BrokenStrangeMagicRing>(), 1);
            recipe.AddIngredient(ModContent.ItemType<WhiteTitanite>(), 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddCondition(tsorcRevampWorld.AdventureModeDisabled);

            recipe.Register();
        }
    }
}
