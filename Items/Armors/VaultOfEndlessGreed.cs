using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]

    public class VaultOfEndlessGreed : ModItem
    {
        public static float SoulAmplifier = 50f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(SoulAmplifier);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 28;
            Item.defense = 18;
            Item.rare = ItemRarityID.Purple;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().VOEGDrain = true;
            player.GetModPlayer<tsorcRevampPlayer>().MidasGreedEffect = true;
        }

        float rotation = 0;
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
            for (int i = 0; i < 4; i++)
            {
                rotation += 0.005f;
                Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i + rotation) * 2;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.Yellow * 0.3f, 0, origin, scale, SpriteEffects.None, 0);

                offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i - rotation) * 2;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.Yellow * 0.3f, 0, origin, scale, SpriteEffects.None, 0);
            }
            return true;
        }
    }
}
