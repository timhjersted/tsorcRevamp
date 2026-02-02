using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories
{
    public class Trinity : ModItem
    {
        public static float LifeThreshold = 50f;
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 26;
            Item.accessory = true;
            Item.rare = ItemRarityID.Cyan;
            Item.value = PriceByRarity.Cyan_9;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().Trinity = true;

            // Gain trinity buff if hp is below 50%
            if (player.statLife <= (int)(player.statLifeMax2 * (LifeThreshold / 100f)))
            {
                player.AddBuff(ModContent.BuffType<Buffs.Accessories.TrinityBuff>(), 2 * 60); // 2 seconds
                tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
                modPlayer.SetAuraState(tsorcAuraState.TripleThreat);
                modPlayer.effectRadius = 250f;

            }
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
