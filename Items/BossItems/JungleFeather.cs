using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.BossItems
{
    class JungleFeather : ModItem
    {

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.consumable = false;
            Item.rare = ItemRarityID.LightRed;
            Item.consumable = false;
        }


        public override bool? UseItem(Player player)
        {
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.JungleWyvern.JungleWyvernHead>()))
            {
                return false;
            }
            if (Main.dayTime)
            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("Items.JungleFeather.WrongTime"), 175, 75, 255);
            }
            else if (!player.ZoneRockLayerHeight)
            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("Items.JungleFeather.WrongLocation"), 175, 75, 255);
            }
            else
            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("Items.JungleFeather.Summon"), 175, 75, 255);
                NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.JungleWyvern.JungleWyvernHead>());
            }
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Feather);
            recipe.AddIngredient(ItemID.Bone, 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddCondition(tsorcRevampWorld.AdventureModeDisabled);

            recipe.Register();
        }
    }
}
