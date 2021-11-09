using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace tsorcRevamp.Items.Accessories
{
    [AutoloadEquip(EquipType.Shoes)]
    public class ReflectionShift : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Your full potential trancends even space and time" +
                                "\nDouble tap any direction or press the hotkey to [c/9803fc:shift]" +
                                "\nWhile shifting you are [c/9803fc:completely intangable] for a brief moment");
        }

        public override void SetDefaults() {
            item.width = 40;
            item.height = 40;
            item.accessory = true;
            item.value = PriceByRarity.Purple_11;
            item.rare = ItemRarityID.Purple;
        }

        public override void UpdateVanity(Player player, EquipType type)
        {
            Lighting.AddLight(player.Center, Color.Blue.ToVector3());
            UsefulFunctions.DustRing(player.Center, 64, DustID.ShadowbeamStaff);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
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
            spriteBatch.Draw(texture, position, frame, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {

            Lighting.AddLight(item.Center, Color.Blue.ToVector3());
            UsefulFunctions.DustRing(item.Center, 32, DustID.ShadowbeamStaff);
            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.ReflectionShift];
            spriteBatch.Draw(texture, new Vector2(item.position.X - Main.screenPosition.X + item.width * 0.5f, item.position.Y - Main.screenPosition.Y + item.height - texture.Height * 0.5f + 2f),
                new Rectangle(0, 0, texture.Width, texture.Height), Color.White, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
