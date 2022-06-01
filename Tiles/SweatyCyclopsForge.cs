using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace tsorcRevamp.Tiles
{
    public class SweatyCyclopsForge : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
            TileObjectData.newTile.CoordinateHeights = new[] { 16, 16 };
            animationFrameHeight = 56;
            TileObjectData.addTile(Type);
            ModTranslation name = CreateMapEntryName();
            name.SetDefault("Sweaty Cyclops Forge");
            AddMapEntry(new Color(215, 60, 0), name);
            dustType = 30;
            disableSmartCursor = true;
            Main.tileLighted[Type] = true;
            Main.tileFrameImportant[Type] = true;
            Main.tileBlockLight[Type] = false;
            Main.tileNoAttach[Type] = true;
            Main.tileWaterDeath[Type] = false;
            Main.tileLavaDeath[Type] = false;

        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            r = 0.5f;
            g = 0.35f;
            b = 0.1f;
        }
        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            Item.NewItem(i * 16, j * 16, 16, 32, ModContent.ItemType<SweatyCyclopsForgeItem>());
        }

        public class SweatyCyclopsForgeItem : ModItem
        {
            public override void SetStaticDefaults()
            {
                DisplayName.SetDefault("Sweaty Cyclops Forge");
                Tooltip.SetDefault("Allows items to be reforged to remove random damage.");
            }

            public override void SetDefaults()
            {
                Item.CloneDefaults(ItemID.Sawmill);
                Item.createTile = ModContent.TileType<SweatyCyclopsForge>();
                Item.placeStyle = 0;
            }
            public override void AddRecipes()
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.StoneBlock, 50);
                recipe.AddIngredient(ItemID.Torch, 5);
                recipe.AddTile(TileID.WorkBenches);

                recipe.Register();
            }
        }
    }
}