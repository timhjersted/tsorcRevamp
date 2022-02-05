using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    class EternalCrystal : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault($"[i:{item.type}][c/4949c2:A mysterious crystal][i:{item.type}]" +
                "\nLooking into it is like peering into the infinite expanse of [c/4949c2:space]");
            Main.RegisterItemAnimation(item.type, new DrawAnimationVertical(4, 25));

        }
        public override void SetDefaults()
        {
            item.width = 28;
            item.height = 50;
            item.maxStack = 999;
            item.value = 50000;
            item.rare = ItemRarityID.Yellow;
        }

        public int itemframe = 0;
        public int itemframeCounter = 0;

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Lighting.AddLight(item.Center, .5f, .35f, .35f);
            Texture2D texture = Main.itemTexture[item.type];
            Texture2D textureglow = mod.GetTexture("Items/EternalCrystal_Glow");
            var myrectangle = texture.Frame(1, 25, 0, itemframe);
            spriteBatch.Draw(texture, item.Center - Main.screenPosition, myrectangle, lightColor, 0f, new Vector2(14, 25), item.scale, SpriteEffects.None, 0.1f);
            spriteBatch.Draw(textureglow, item.Center - Main.screenPosition, myrectangle, Color.White, 0f, new Vector2(14, 25), item.scale, SpriteEffects.None, 0f);
			
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


            if (Main.rand.Next(50) == 0) //Yellow
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(item.position.X + 2, item.position.Y + 24), 24, 24, 170, item.velocity.X, item.velocity.Y, 100, default(Color), .4f)];
                dust.velocity *= 0f;
                dust.noGravity = true;
                dust.velocity += item.velocity;
                dust.fadeIn = 1f;
            }
            if (Main.rand.Next(50) == 0) //Pink
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(item.position.X + 2, item.position.Y + 24), 24, 24, 272, item.velocity.X, item.velocity.Y, 100, default(Color), .4f)]; //223, 255, 272
                dust.velocity *= 0f;
                dust.noGravity = true;
                dust.velocity += item.velocity;
                dust.fadeIn = 1f;
            }
            if (Main.rand.Next(50) == 0) //Blue
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(item.position.X + 2, item.position.Y + 24), 24, 24, 185, item.velocity.X, item.velocity.Y, 100, default(Color), .4f)];
                dust.velocity *= 0f;
                dust.noGravity = true;
                dust.velocity += item.velocity;
                dust.fadeIn = 1f;
            }
            if (Main.rand.Next(50) == 0) //Green
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(item.position.X + 2, item.position.Y + 24), 24, 24, 107, item.velocity.X, item.velocity.Y, 100, default(Color), .4f)];
                dust.velocity *= 0f;
                dust.noGravity = true;
                dust.velocity += item.velocity;
                dust.fadeIn = 1f;
            }

            return false;
        }
    }
}