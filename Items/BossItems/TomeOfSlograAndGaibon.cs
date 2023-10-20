using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.BossItems
{
    class TomeOfSlograAndGaibon : ModItem
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
            Item.maxStack = 1;
            Item.consumable = false;
            Item.rare = ItemRarityID.LightRed;
            Item.consumable = false;

        }


        public override bool? UseItem(Player player)
        {
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Slogra>()) || NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Gaibon>()))
            {
                return false;
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar);
            NPC.NewNPC(player.GetSource_ItemUse(Item), (int)player.position.X + 1000, (int)player.position.Y, ModContent.NPCType<NPCs.Bosses.Gaibon>(), 0);
            NPC.NewNPC(player.GetSource_ItemUse(Item), (int)player.position.X - 1000, (int)player.position.Y - 200, ModContent.NPCType<NPCs.Bosses.Slogra>(), 0);
            return true;
        }

        public override void AddRecipes()
        {
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.SpellTome, 1);
                recipe.AddIngredient(ItemID.MeteoriteBar, 3);
                recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1000);
                recipe.AddTile(TileID.DemonAltar);
                recipe.AddCondition(tsorcRevampWorld.AdventureModeDisabled);

                recipe.Register();
            }
        }
    }
}
