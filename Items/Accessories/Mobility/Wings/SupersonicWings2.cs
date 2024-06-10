using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Mobility.Wings
{
    [AutoloadEquip(EquipType.Wings, EquipType.Shoes)]
    public class SupersonicWings2 : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 28;
            Item.accessory = true;
            Item.value = PriceByRarity.Cyan_9;
            Item.rare = ItemRarityID.Cyan;
        }

        public override void AddRecipes()
        {
            Recipe recipe4 = CreateRecipe();
            recipe4.AddIngredient(ModContent.ItemType<SupersonicWings>());
            recipe4.AddIngredient(ModContent.ItemType<SoulOfAttraidies>());
            recipe4.AddIngredient(ItemID.EmpressFlightBooster);
            recipe4.AddIngredient(ModContent.ItemType<ImprovedBundleofBalloons>());
            recipe4.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe4.AddTile(TileID.DemonAltar);
            recipe4.Register();

            Recipe recipe3 = CreateRecipe();
            recipe3.AddIngredient(ModContent.ItemType<SupersonicWings>());
            recipe3.AddIngredient(ModContent.ItemType<SoulOfAttraidies>());
            recipe3.AddIngredient(ModContent.ItemType<ImprovedBundleofBalloons>());
            recipe3.AddIngredient(ModContent.ItemType<DarkSoul>(), 35000);
            recipe3.AddTile(TileID.DemonAltar);
            recipe3.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ModContent.ItemType<SupersonicWings>());
            recipe2.AddIngredient(ModContent.ItemType<SoulOfAttraidies>());
            recipe2.AddIngredient(ItemID.EmpressFlightBooster);
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), 50000);
            recipe2.AddTile(TileID.DemonAltar);
            recipe2.Register();

            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<SupersonicWings>());
            recipe.AddIngredient(ModContent.ItemType<SoulOfAttraidies>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 80000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }

        public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling, ref float ascentWhenRising,
                            ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
        {
            ascentWhenFalling = 0.85f;
            ascentWhenRising = 0.15f;
            maxCanAscendMultiplier = 1f;
            maxAscentMultiplier = 2.6f;
            constantAscend = 0.135f;

        }

        public override void HorizontalWingSpeeds(Player player, ref float speed, ref float acceleration)
        {
            speed = 6f;
            acceleration = 0.3f;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.jumpBoost = true;
            player.fireWalk = true;
            player.noKnockback = true;
            player.noFallDmg = true;
            player.canRocket = true;
            player.iceSkate = true;
            player.rocketTime = 1200;
            player.rocketBoots = 2;
            player.rocketTimeMax = 1200;
            player.jumpSpeedBoost = 3.2f;
            player.wingTimeMax = 1200;

            if (!ModContent.GetInstance<tsorcRevampConfig>().DisableSupersonicWings2ExtraJumps)
            {
                player.GetJumpState(ExtraJump.CloudInABottle).Enable()/* tModPorter Suggestion: Call Enable() if setting this to true, otherwise call Disable(). */;
                player.GetJumpState(ExtraJump.BlizzardInABottle).Enable()/* tModPorter Suggestion: Call Enable() if setting this to true, otherwise call Disable(). */;
                player.GetJumpState(ExtraJump.SandstormInABottle).Enable()/* tModPorter Suggestion: Call Enable() if setting this to true, otherwise call Disable(). */;
            }

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
                player.GetModPlayer<tsorcRevampPlayer>().supersonicLevel = 3;
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
                } **/

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
