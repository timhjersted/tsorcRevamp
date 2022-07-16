using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Mobility
{
    public class ImprovedBundleofBalloons : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Improved Bundle of Balloons");
            Tooltip.SetDefault("Allows the holder to quadruple jump" +
                                "\nIncreases jump height + 120% jump speed");
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 28;
            Item.accessory = true;
            Item.value = PriceByRarity.Green_2;
            Item.rare = ItemRarityID.Green;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.BundleofBalloons, 1);
            recipe.AddIngredient(ItemID.AdamantiteBar, 5);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Terraria.Recipe recipe2 = CreateRecipe();
            recipe2.AddRecipeGroup(tsorcRevampSystems.ImprovedBundleGroupBlue);
            recipe2.AddRecipeGroup(tsorcRevampSystems.ImprovedBundleGroupWhite);
            recipe2.AddRecipeGroup(tsorcRevampSystems.ImprovedBundleGroupYellow);
            recipe2.AddIngredient(ItemID.AdamantiteBar, 5);
            recipe2.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 3000);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            RasterizerState OverflowHiddenRasterizerState = new RasterizerState
            {
                CullMode = CullMode.None,
                ScissorTestEnable = true
            };

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, OverflowHiddenRasterizerState, null, Main.UIScaleMatrix);
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
            for (int i = 0; i < 4; i++)
            {
                Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.GreenYellow, 0, origin, scale, SpriteEffects.None, 0);
            }
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, OverflowHiddenRasterizerState, null, Main.UIScaleMatrix);

            return true;
        }

        public override void UpdateEquip(Player player)
        {
            player.jumpSpeedBoost += 1.2f;
            player.jumpBoost = true;
            player.hasJumpOption_Cloud = true;
            player.hasJumpOption_Blizzard = true;
            player.hasJumpOption_Sandstorm = true;
        }

    }
}