using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    [AutoloadEquip(EquipType.Wings, EquipType.Shoes)]
    public class SupersonicWings : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("+20% supersonic movement speed, flight, rocket boots effect, " +
                                "\nfire-walk skill, knockback protection, and water-walking if moving fast enough." +
                                "\nDoes not work if Hermes Boots or Spectre Boots are equipped." +
                                "\nSpeed boost is multiplied by movement speed boosts." +
                                "\nCompatible with Dragoon Boots and Dragoon Gear.");
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 28;
            Item.accessory = true;
            Item.value = PriceByRarity.Pink_5;
            Item.rare = ItemRarityID.Pink;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.CloudinaBalloon, 1);
            recipe.AddIngredient(ItemID.AngelWings, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("SupersonicBoots").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 20000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Terraria.Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ModContent.ItemType<ImprovedCloudInABalloon>(), 1);
            recipe2.AddIngredient(ItemID.AngelWings, 1);
            recipe2.AddIngredient(ModContent.ItemType<SupersonicBoots>(), 1);
            recipe2.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 20000);
            recipe2.AddTile(TileID.DemonAltar);
            recipe2.Register();
        }
        public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling, ref float ascentWhenRising,
                            ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
        {
            ascentWhenFalling = 0.85f;
            ascentWhenRising = 0.15f;
            maxCanAscendMultiplier = 1f;
            maxAscentMultiplier = 1.25f;
            constantAscend = 0.135f;

        }

        public override void HorizontalWingSpeeds(Player player, ref float speed, ref float acceleration)
        {
            speed = 6f;
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.jumpBoost = true;
            player.doubleJumpCloud = true;
            player.fireWalk = true;
            player.noKnockback = true;
            player.canRocket = true;
            player.iceSkate = true;
            player.jumpSpeedBoost = 1.3f;
            player.wingTimeMax = 180;

            bool restricted = false;
            for (int i = 3; i <= 8; i++)
            {
                if (player.armor[i].type == ItemID.HermesBoots || player.armor[i].type == ItemID.SpectreBoots
                    || player.armor[i].type == ItemID.LightningBoots || player.armor[i].type == ItemID.FlurryBoots
                    || player.armor[i].type == ItemID.FrostsparkBoots || player.armor[i].type == ItemID.SailfishBoots)
                {
                    restricted = true;
                }
            }
            if (!restricted)
            {
                player.GetModPlayer<tsorcRevampPlayer>().supersonicLevel = 2;


                /** W1K's original code
                if (player.controlLeft) {
                    if (player.velocity.X > -3) player.velocity.X -= (float)(player.moveSpeed - 1f) / 10;
                    if (player.velocity.X < -3 && player.velocity.X > -6 * player.moveSpeed) {
                        if (player.velocity.Y != 0) player.velocity.X -= 0.1f;
                        else player.velocity.X -= 0.2f;
                        player.velocity.X -= 0.02f + ((player.moveSpeed - 1f) / 10);
                    }
                }
                if (player.controlRight) {
                    if (player.velocity.X < 3) player.velocity.X += (float)(player.moveSpeed - 1f) / 10;
                    if (player.velocity.X > 3 && player.velocity.X < 6 * player.moveSpeed) {
                        if (player.velocity.Y != 0) player.velocity.X += 0.1f;
                        else player.velocity.X += 0.2f;
                        player.velocity.X += 0.02f + ((player.moveSpeed - 1f) / 10);
                    }
                }**/

                if (player.velocity.X > 6 || player.velocity.X < -6)
                {
                    player.waterWalk = true;
                    int sonicDust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 16, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, default, 2f);
                    Main.dust[sonicDust].noGravity = true;
                    Main.dust[sonicDust].noLight = false;

                }
            }

        }

    }
}
