using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    class EstusFlaskShard : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("The shard of an Estus Flask" +
                             "\nTake it to the Emerald Herald to increase flask uses");
        }

        public override void SetDefaults()
        {
            Item.width = 8;
            Item.height = 12;
            Item.rare = ItemRarityID.Yellow;
            Item.value = 0;
            Item.maxStack = 10;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
            spriteBatch.Draw(texture, new Vector2(Item.position.X - Main.screenPosition.X + Item.width * 0.5f, Item.position.Y - Main.screenPosition.Y + Item.height - texture.Height * 0.5f + 2f),
                new Rectangle(0, 0, texture.Width, texture.Height), Color.White, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0);

            if (Main.rand.Next(4) == 0)
            {
                int z = Dust.NewDust(Item.Center, 40, 50, 270, 0f, 0f, 120, default(Color), 1f);
                Main.dust[z].noGravity = true;
                Main.dust[z].velocity *= 2.75f;
                Main.dust[z].fadeIn = 1.3f;
                Vector2 vectorother = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                vectorother.Normalize();
                vectorother *= (float)Main.rand.Next(50, 100) * 0.025f; //speed
                Main.dust[z].velocity = vectorother;
                vectorother.Normalize();
                vectorother *= 16f; //spawn distance
                Main.dust[z].position = Item.Center + new Vector2(0, 2) - vectorother;
            }


            return false;
        }


    }
}
