using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items;

class EternalCrystal : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault($"[i:{Item.type}][c/4949c2:A mysterious crystal][i:{Item.type}]" +
            "\nLooking into it is like peering into the infinite expanse of [c/4949c2:space]");
        Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(4, 25));

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
        Texture2D textureglow = (Texture2D)Mod.Assets.Request<Texture2D>("Items/EternalCrystal_Glow");
        var myrectangle = texture.Frame(1, 25, 0, itemframe);
        spriteBatch.Draw(texture, Item.Center - Main.screenPosition, myrectangle, lightColor, 0f, new Vector2(14, 25), Item.scale, SpriteEffects.None, 0.1f);
        spriteBatch.Draw(textureglow, Item.Center - Main.screenPosition, myrectangle, Color.White, 0f, new Vector2(14, 25), Item.scale, SpriteEffects.None, 0);

        itemframeCounter++;

        if (itemframeCounter < 4)
        {
            itemframe = 0;
        }
        else if (itemframeCounter < 8)
        {
            itemframe = 1;
        }
        else if (itemframeCounter < 12)
        {
            itemframe = 2;
        }
        else if (itemframeCounter < 16)
        {
            itemframe = 3;
        }
        else if (itemframeCounter < 20)
        {
            itemframe = 4;
        }
        else if (itemframeCounter < 24)
        {
            itemframe = 5;
        }
        else if (itemframeCounter < 28)
        {
            itemframe = 6;
        }
        else if (itemframeCounter < 32)
        {
            itemframe = 7;
        }
        else if (itemframeCounter < 36)
        {
            itemframe = 8;
        }
        else if (itemframeCounter < 40)
        {
            itemframe = 9;
        }
        else if (itemframeCounter < 44)
        {
            itemframe = 10;
        }
        else if (itemframeCounter < 48)
        {
            itemframe = 11;
        }
        else if (itemframeCounter < 52)
        {
            itemframe = 12;
        }
        else if (itemframeCounter < 56)
        {
            itemframe = 13;
        }
        else if (itemframeCounter < 60)
        {
            itemframe = 14;
        }
        else if (itemframeCounter < 64)
        {
            itemframe = 15;
        }
        else if (itemframeCounter < 68)
        {
            itemframe = 16;
        }
        else if (itemframeCounter < 72)
        {
            itemframe = 17;
        }
        else if (itemframeCounter < 76)
        {
            itemframe = 18;
        }
        else if (itemframeCounter < 80)
        {
            itemframe = 19;
        }
        else if (itemframeCounter < 84)
        {
            itemframe = 20;
        }
        else if (itemframeCounter < 88)
        {
            itemframe = 21;
        }
        else if (itemframeCounter < 92)
        {
            itemframe = 22;
        }
        else if (itemframeCounter < 96)
        {
            itemframe = 23;
        }
        else if (itemframeCounter < 100)
        {
            itemframe = 24;
        }
        else
        {
            itemframeCounter = 0;
        }


        if (Main.rand.NextBool(50)) //Yellow
        {
            Dust dust = Main.dust[Dust.NewDust(new Vector2(Item.position.X + 2, Item.position.Y + 24), 24, 24, 170, Item.velocity.X, Item.velocity.Y, 100, default(Color), .4f)];
            dust.velocity *= 0f;
            dust.noGravity = true;
            dust.velocity += Item.velocity;
            dust.fadeIn = 1f;
        }
        if (Main.rand.NextBool(50)) //Pink
        {
            Dust dust = Main.dust[Dust.NewDust(new Vector2(Item.position.X + 2, Item.position.Y + 24), 24, 24, 272, Item.velocity.X, Item.velocity.Y, 100, default(Color), .4f)]; //223, 255, 272
            dust.velocity *= 0f;
            dust.noGravity = true;
            dust.velocity += Item.velocity;
            dust.fadeIn = 1f;
        }
        if (Main.rand.NextBool(50)) //Blue
        {
            Dust dust = Main.dust[Dust.NewDust(new Vector2(Item.position.X + 2, Item.position.Y + 24), 24, 24, 185, Item.velocity.X, Item.velocity.Y, 100, default(Color), .4f)];
            dust.velocity *= 0f;
            dust.noGravity = true;
            dust.velocity += Item.velocity;
            dust.fadeIn = 1f;
        }
        if (Main.rand.NextBool(50)) //Green
        {
            Dust dust = Main.dust[Dust.NewDust(new Vector2(Item.position.X + 2, Item.position.Y + 24), 24, 24, 107, Item.velocity.X, Item.velocity.Y, 100, default(Color), .4f)];
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
    }
}