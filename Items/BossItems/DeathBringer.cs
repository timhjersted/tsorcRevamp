using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.BossItems
{
    [LegacyName("BloodySkull")]
    class DeathBringer : ModItem
    {

        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.consumable = false;
            Item.maxStack = 1;
            Item.value = 100000;
            Item.rare = ItemRarityID.Pink;
            Item.useTime = 45;
            Item.useAnimation = 45;
            Item.scale = 1f;
            Item.useStyle = ItemUseStyleID.HoldUp;
        }
        public override bool? UseItem(Player player)
        {
            if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Death>()))
            {
                NPC.SpawnOnPlayer(Main.myPlayer, ModContent.NPCType<NPCs.Bosses.Death>()); //placeholder
                return true;
            }
            else
            {
                return false;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SoulofNight, 1);
            recipe.AddIngredient(ItemID.Bone, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1000);
            recipe.AddTile(TileID.LunarCraftingStation);
            recipe.AddCondition(tsorcRevampWorld.AdventureModeEnabled);
            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.SoulofNight, 1);
            recipe2.AddIngredient(ItemID.Bone, 1);
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), 1000);
            recipe2.AddTile(TileID.DemonAltar);
            recipe2.AddCondition(tsorcRevampWorld.AdventureModeDisabled);
            recipe2.Register();
        }
    }
}