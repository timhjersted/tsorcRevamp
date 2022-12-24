using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems
{
    class SerrisBait : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Summons Serris");
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
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>()) || NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Serris.SerrisX>()))
            {
                return false;
            }
            else
            {
                NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>());
                NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>());
            }
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.AdamantiteBar, 3);
            recipe.AddIngredient(ItemID.SharkFin, 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddCondition(tsorcRevampWorld.AdventureModeDisabled);
            recipe.Register();
        }
    }
}
