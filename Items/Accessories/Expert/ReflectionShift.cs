using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace tsorcRevamp.Items.Accessories.Expert
{
    [AutoloadEquip(EquipType.Shoes)]
    public class ReflectionShift : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 40;
            Item.accessory = true;
            Item.value = PriceByRarity.Purple_11;
            Item.expert = true;
        }

        public override void UpdateVanity(Player player)
        {
            Lighting.AddLight(player.Center, Color.Blue.ToVector3());
            UsefulFunctions.DustRing(player.Center, 64, DustID.ShadowbeamStaff);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<tsorcRevampPlayer>().CanUseItemsWhileDodging = true;
            player.GetModPlayer<tsorcRevampPlayer>().ReflectionShiftEnabled = true;
            if (!hideVisual)
            {
                Lighting.AddLight(player.Center, Color.Blue.ToVector3());
                UsefulFunctions.DustRing(player.Center, 64, DustID.ShadowbeamStaff);
            }
        }
        public override int ChoosePrefix(UnifiedRandom rand)
        {
            // When the item is given a prefix, only roll the best modifiers for accessories
            return rand.Next(new int[] { PrefixID.Arcane, PrefixID.Lucky, PrefixID.Menacing, PrefixID.Quick, PrefixID.Violent, PrefixID.Warding });
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.ReflectionShift];
            spriteBatch.Draw(texture, position, frame, drawColor, 0f, origin, scale, SpriteEffects.None, 0);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {

            Lighting.AddLight(Item.Center, Color.Blue.ToVector3());
            UsefulFunctions.DustRing(Item.Center, 32, DustID.ShadowbeamStaff);
            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.ReflectionShift];
            spriteBatch.Draw(texture, new Vector2(Item.position.X - Main.screenPosition.X + Item.width * 0.5f, Item.position.Y - Main.screenPosition.Y + Item.height - texture.Height * 0.5f + 2f),
                new Rectangle(0, 0, texture.Width, texture.Height), Color.White, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
