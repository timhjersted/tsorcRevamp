using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Materials
{
    public abstract class Titanite : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 16;
            Item.rare = ItemRarityID.LightRed;
            Item.maxStack = 9999;
        }
    }

    public class BlueTitanite : Titanite
    {
        public override string Texture => "tsorcRevamp/Items/Materials/TitaniteBlue";
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
        }
        float rotation = 0;
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
            for (int i = 0; i < 4; i++)
            {
                rotation += 0.01f;
                Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i + rotation) * 3;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.DeepPink * 0.3f, 0, origin, scale, SpriteEffects.None, 0);

                offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i - rotation) * 3;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.DeepPink * 0.3f, 0, origin, scale, SpriteEffects.None, 0);
            }
            return true;
        }
    }

    public class RedTitanite : Titanite
    {
        public override string Texture => "tsorcRevamp/Items/Materials/TitaniteRed";
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
        }
        float rotation = 0;
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
            for (int i = 0; i < 4; i++)
            {
                rotation += 0.01f;
                Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i + rotation) * 3;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.Black * 0.5f, 0, origin, scale, SpriteEffects.None, 0);

                offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i - rotation) * 3;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.Black * 0.5f, 0, origin, scale, SpriteEffects.None, 0);
            }
            return true;
        }
    }

    public class WhiteTitanite : Titanite
    {
        public override string Texture => "tsorcRevamp/Items/Materials/TitaniteWhite";
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
        }
        float rotation = 0;
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
            for (int i = 0; i < 4; i++)
            {
                rotation += 0.01f;
                Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i + rotation) * 3;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.Purple * 0.5f, 0, origin, scale, SpriteEffects.None, 0);

                offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i - rotation) * 3;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.Purple * 0.5f, 0, origin, scale, SpriteEffects.None, 0);
            }
            return true;
        }
    }
}
