using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using tsorcRevamp.UI;
using Terraria.DataStructures;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;


namespace tsorcRevamp.Items {
    class PotionBag : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Can store up to 28 potions" +
                               "\nSupports Quick Buff/Heal/Mana hotkeys, as well as permanent potions!" +
                               "\n\"Favorite\" valuable potions in the pouch with Alt+Click" +
                               "\nFavorited potions are not consumed by Quick Buff!");
            Main.RegisterItemAnimation(item.type, new DrawAnimationVertical(10, 9));

        }

        public override void SetDefaults() {
            item.width = 22;
            item.height = 30;
            item.rare = ItemRarityID.Purple;
            item.value = 0;
            item.useAnimation = 10;
            item.useTime = 10;
            item.noUseGraphic = true;
            item.useStyle = ItemUseStyleID.SwingThrow;
            //item.UseSound = SoundID.Item4;
            item.maxStack = 1;
        }

        public override bool CanUseItem(Player player) {
			return true;
        }

        public override bool UseItem(Player player) {
            if (player.whoAmI == Main.myPlayer)
            {
                if (!PotionBagUIState.Visible)
                {
                    Main.playerInventory = true;
                    PotionBagUIState.Visible = true;
                    Main.PlaySound(SoundID.MenuOpen);
                }
                else
                {
                    PotionBagUIState.Visible = false;
                    Main.PlaySound(SoundID.MenuClose);
                }
            }
            return true;
        }

        public override void PostUpdate()
        {
            Lighting.AddLight(item.Center, 0.3f, 0.2f, 0.4f);

            if (Main.rand.Next(10) == 0)
            {
                Dust dust = Main.dust[Dust.NewDust(item.position, item.width, item.height, 27, 0f, 0f, 50, default, Main.rand.NextFloat(.8f, 1.2f))];
                dust.noGravity = true;
                dust.velocity *= 0;
            }
        }

        public int itemframe = 0;
        public int itemframeCounter = 0;

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D texture = Main.itemTexture[item.type];
            Texture2D textureGlow = mod.GetTexture("Items/PotionBag_Glow");
            var myrectangle = texture.Frame(1, 9, 0, itemframe);
            spriteBatch.Draw(texture, item.Center - Main.screenPosition, myrectangle, lightColor, 0f, new Vector2(12, 16), item.scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture, item.Center - Main.screenPosition, myrectangle, Color.White, 0f, new Vector2(12, 16), item.scale, SpriteEffects.None, 0f);


            itemframeCounter++;

            if (itemframeCounter < 10)
            {
                itemframe = 0;
            }
            else if (itemframeCounter < 20)
            {
                itemframe = 1;
            }
            else if (itemframeCounter < 30)
            {
                itemframe = 2;
            }
            else if (itemframeCounter < 40)
            {
                itemframe = 3;
            }
            else if (itemframeCounter < 50)
            {
                itemframe = 4;
            }
            else if (itemframeCounter < 60)
            {
                itemframe = 5;
            }
            else if (itemframeCounter < 70)
            {
                itemframe = 6;
            }
            else if (itemframeCounter < 80)
            {
                itemframe = 7;
            }
            else if (itemframeCounter < 90)
            {
                itemframe = 8;
            }
            else
            {
                itemframeCounter = 0;
            }

            return false;
        }

    }
}
