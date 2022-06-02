using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    class SoulShekel : BaseRarityItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A mysterious coin formed out of dark souls" +
                "\nUsed as a currency among certain merchants");
            ItemID.Sets.ItemNoGravity[Item.type] = true;
            //Main.RegisterItemAnimation(item.type, new DrawAnimationVertical(4, 8));

        }

        public override void SetDefaults()
        {
            Item.width = 14;
            Item.height = 22;
            Item.maxStack = 99999;
            Item.value = 1;
            Item.rare = ItemRarityID.Lime;
            DarkSoulRarity = 12;
        }

        public override bool GrabStyle(Player player)
        {
            Vector2 vectorItemToPlayer = player.Center - Item.Center;
            Vector2 movement = vectorItemToPlayer.SafeNormalize(default) * 0.75f;
            Item.velocity = Item.velocity + movement;
            return true;
        }

        public override void GrabRange(Player player, ref int grabRange)
        {
            grabRange *= (2 + Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SoulReaper / 2);
        }

        public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> list)
        {
            foreach (TooltipLine line2 in list)
            {
                if (line2.mod == "Terraria" && line2.Name == "ItemName")
                {
                    line2.OverrideColor = BaseColor.RarityExample;
                }
            }
        }

        public override bool OnPickup(Player player)
        {
            bool openSlot = false;
            for (int i = 0; i < /*Main.maxInventory*/ 50; i++) //Main.maxInventory == 58 would include coin and ammo slots, we don't want to take those into account in this case
            {
                if (player.inventory[i].IsAir || player.HasItem(ModContent.ItemType<SoulShekel>()))
                {
                    openSlot = true;
                }
            }
            if (openSlot)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.CoinPickup, (int)player.position.X, (int)player.position.Y, 0, 0.8f);
            }
            return true;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 5);

            recipe.Register();

            Terraria.Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 50);
            recipe2.SetResult(this, 10);
            recipe2.Register();

            Recipe recipe3 = CreateRecipe();
            recipe3.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 500);
            recipe3.SetResult(this, 100);
            recipe3.Register();
        }

        int itemframe = 0;
        int itemframeCounter = 0;

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {

            Lighting.AddLight(Item.Center, 0.1f, 0.45f, 0.21f);
            Texture2D texture = Mod.GetTexture("Items/SoulShekel_InWorld");
            var myrectangle = texture.Frame(1, 8, 0, itemframe);
            spriteBatch.Draw(texture, Item.Center - Main.screenPosition, myrectangle, lightColor, 0f, new Vector2(7, 11), Item.scale, SpriteEffects.None, 0.1f);

            itemframeCounter += Main.rand.Next(1, 3);

            if (itemframeCounter >= 15)
            {
                itemframeCounter = 0;
                if (++itemframe >= 7)
                {
                    itemframe = 0;
                }
            }

            return false;
        }
    }
}