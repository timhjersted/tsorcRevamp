using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Items.Materials.Titanite;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Content.Items.BossItems
{
    class StoneOfSeath : ModItem
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

            UsefulFunctions.BroadcastText(LangUtils.GetTextValue("Items.StoneOfSeath.Summon"), 175, 75, 255);
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessHead>());
            return true;
        }


        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<BlueTitanite>(), 5);
            recipe.AddIngredient(ModContent.ItemType<DragonEssence>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 1000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddCondition(tsorcRevampWorld.AdventureModeDisabled);

            recipe.Register();
        }

    }
}
