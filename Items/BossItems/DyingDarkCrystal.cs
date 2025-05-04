using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.BossItems
{
    class DyingDarkCrystal : ModItem
    {

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.LightRed;
            Item.width = 12;
            Item.height = 12;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 5;
            Item.useTime = 5;
            Item.consumable = false;
        }

        public override bool? UseItem(Player player)
        {
            UsefulFunctions.BroadcastText(LangUtils.GetTextValue("Items.DyingDarkCrystal.Summon"), Color.Violet);
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>());
            return true;
        }
        public override bool CanUseItem(Player player)
        {
            return (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>()));
        }


        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<CursedSoul>(), 4);
            recipe.AddIngredient(ModContent.ItemType<RedTitanite>(), 4);
            recipe.AddIngredient(ModContent.ItemType<WhiteTitanite>(), 4);
            recipe.AddIngredient(ModContent.ItemType<BlueTitanite>(), 4);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddCondition(tsorcRevampWorld.AdventureModeDisabled);
            recipe.Register();
        }
    }
}
