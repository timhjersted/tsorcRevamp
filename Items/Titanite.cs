using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    public abstract class Titanite : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("A rare and valuable ore");
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
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Insert(tooltips.Count, new TooltipLine(Mod, "", "Ice cold to the touch, it glows with enchanted power"));
        }

        float rotation = 0;
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
            for (int i = 0; i < 4; i++)
            {
                rotation += 0.01f;
                Vector2 offsetPositon = Vector2.UnitY.RotatedBy((MathHelper.PiOver2 * i) + rotation) * 3;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.DeepPink * 0.3f, 0, origin, scale, SpriteEffects.None, 0);

                offsetPositon = Vector2.UnitY.RotatedBy((MathHelper.PiOver2 * i) - rotation) * 3;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.DeepPink * 0.3f, 0, origin, scale, SpriteEffects.None, 0);
            }
            return true;
        }
    }

    public class RedTitanite : Titanite
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Insert(tooltips.Count, new TooltipLine(Mod, "", "Radiating heat, it shimmers with forbidden magic"));
        }

        float rotation = 0;
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
            for (int i = 0; i < 4; i++)
            {
                rotation += 0.01f;
                Vector2 offsetPositon = Vector2.UnitY.RotatedBy((MathHelper.PiOver2 * i) + rotation) * 3;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.Black * 0.5f, 0, origin, scale, SpriteEffects.None, 0);

                offsetPositon = Vector2.UnitY.RotatedBy((MathHelper.PiOver2 * i) - rotation) * 3;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.Black * 0.5f, 0, origin, scale, SpriteEffects.None, 0);
            }
            return true;
        }
    }

    public class WhiteTitanite : Titanite
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Insert(tooltips.Count, new TooltipLine(Mod, "", "A solid white surface, it casts an eerie purple shadow"));
        }
        float rotation = 0;
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
            for (int i = 0; i < 4; i++)
            {
                rotation += 0.01f;
                Vector2 offsetPositon = Vector2.UnitY.RotatedBy((MathHelper.PiOver2 * i) + rotation) * 3;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.Purple * 0.5f, 0, origin, scale, SpriteEffects.None, 0);

                offsetPositon = Vector2.UnitY.RotatedBy((MathHelper.PiOver2 * i) - rotation) * 3;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.Purple * 0.5f, 0, origin, scale, SpriteEffects.None, 0);
            }
            return true;
        }
    }
}
