using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Items.Materials.Titanite;

namespace tsorcRevamp.Content.Items.BossItems
{
    class GreatSerpentStone : ModItem
    {
        //Placeholder icon reusing HellkiteStone's art until this boss gets its own.
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(HellkiteStone));

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
            Item.consumable = false;
        }

        public override bool? UseItem(Player player)
        {
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.SuperHardMode.OolacileSerpent.GreatSerpentHead>());
            return true;
        }

        public override bool CanUseItem(Player player)
        {
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.OolacileSerpent.GreatSerpentHead>()))
            {
                return false;
            }
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<RedTitanite>(), 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 1000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddCondition(tsorcRevampWorld.AdventureModeDisabled);

            recipe.Register();
        }
    }
}
