using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Items.BossItems
{
    [LegacyName("BloodySkull")]
    class DeathBringer : ModItem
    {

        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("A powerful weapon which brings death to all.");
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
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}