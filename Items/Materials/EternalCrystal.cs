using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Materials
{
    class EternalCrystal : ModItem
    {
        public override void SetStaticDefaults()
        {
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(4, 25));
            ItemID.Sets.AnimatesAsSoul[Item.type] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 50;
            Item.maxStack = 999;
            Item.value = 50000;
            Item.rare = ItemRarityID.Yellow;
        }

        public int itemframe = 0;
        public int itemframeCounter = 0;

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Lighting.AddLight(Item.Center, .5f, .35f, .35f);
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
            Texture2D textureglow = (Texture2D)Mod.Assets.Request<Texture2D>("Items/Materials/EternalCrystal_Glow");
            var myrectangle = texture.Frame(1, 25, 0, itemframe);
            spriteBatch.Draw(texture, Item.Center - Main.screenPosition, myrectangle, lightColor, 0f, new Vector2(14, 25), Item.scale, SpriteEffects.None, 0.1f);
            spriteBatch.Draw(textureglow, Item.Center - Main.screenPosition, myrectangle, Color.White, 0f, new Vector2(14, 25), Item.scale, SpriteEffects.None, 0);

            itemframeCounter++;

            if (Main.rand.NextBool(50)) //Yellow
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(Item.position.X + 2, Item.position.Y + 24), 24, 24, 170, Item.velocity.X, Item.velocity.Y, 100, default, .4f)];
                dust.velocity *= 0f;
                dust.noGravity = true;
                dust.velocity += Item.velocity;
                dust.fadeIn = 1f;
            }
            if (Main.rand.NextBool(50)) //Pink
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(Item.position.X + 2, Item.position.Y + 24), 24, 24, 272, Item.velocity.X, Item.velocity.Y, 100, default, .4f)]; //223, 255, 272
                dust.velocity *= 0f;
                dust.noGravity = true;
                dust.velocity += Item.velocity;
                dust.fadeIn = 1f;
            }
            if (Main.rand.NextBool(50)) //Blue
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(Item.position.X + 2, Item.position.Y + 24), 24, 24, 185, Item.velocity.X, Item.velocity.Y, 100, default, .4f)];
                dust.velocity *= 0f;
                dust.noGravity = true;
                dust.velocity += Item.velocity;
                dust.fadeIn = 1f;
            }
            if (Main.rand.NextBool(50)) //Green
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(Item.position.X + 2, Item.position.Y + 24), 24, 24, 107, Item.velocity.X, Item.velocity.Y, 100, default, .4f)];
                dust.velocity *= 0f;
                dust.noGravity = true;
                dust.velocity += Item.velocity;
                dust.fadeIn = 1f;
            }

            return false;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.CrystalShard, 55);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.LifeCrystal, 13);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();

            Recipe recipe3 = CreateRecipe();
            recipe3.AddIngredient(ItemID.LifeFruit, 7);
            recipe3.AddTile(TileID.DemonAltar);

            recipe3.Register();
        }
    }
}