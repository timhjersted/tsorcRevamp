using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Items
{
    class PotionBag : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Can store up to 28 potions" +
                               "\nSupports Quick Buff/Heal/Mana hotkeys as well as permanent potions!" +
                               "\n\"[c/C92CD1:Favorite]\" valuable potions in the pouch with Alt+Click" +
                               "\n[c/C92CD1:Favorited potions] are not consumed by Quick Buff" +
                               "\nPotions are stored per character instead of per-bag, similar to a piggy bank");
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(10, 9));

        }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 30;
            Item.rare = ItemRarityID.Purple;
            Item.value = 0;
            Item.noUseGraphic = true;
            Item.useAnimation = 10;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.maxStack = 1;
        }

        public override bool CanUseItem(Player player)
        {

            return true;
        }

        public override bool? UseItem(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().potionBagCountdown = 12;
            return true;
        }

        //You can't pick up or move items in your inventory while an item is being used
        //If autopause is on, then the item stays frozen in-use unless you unpause... which closes the bag again :/
        //Doing it like this means the bag opens *after* the item is finished being used
        public override void UpdateInventory(Player player)
        {

            base.UpdateInventory(player);
        }

        public override void PostUpdate()
        {
            Lighting.AddLight(Item.Center, 0.3f, 0.2f, 0.4f);

            if (Main.rand.Next(10) == 0)
            {
                Dust dust = Main.dust[Dust.NewDust(Item.position, Item.width, Item.height, 27, 0f, 0f, 50, default, Main.rand.NextFloat(.8f, 1.2f))];
                dust.noGravity = true;
                dust.velocity *= 0;
            }
        }

        public int itemframe = 0;
        public int itemframeCounter = 0;

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
            Texture2D textureGlow = Mod.GetTexture("Items/PotionBag_Glow");
            var myrectangle = texture.Frame(1, 9, 0, itemframe);
            spriteBatch.Draw(texture, Item.Center - Main.screenPosition, myrectangle, lightColor, 0f, new Vector2(12, 16), Item.scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture, Item.Center - Main.screenPosition, myrectangle, Color.White, 0f, new Vector2(12, 16), Item.scale, SpriteEffects.None, 0f);


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

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Silk, 5);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 75);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
