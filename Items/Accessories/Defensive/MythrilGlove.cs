using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    [AutoloadEquip(EquipType.HandsOn)]
    public class MythrilGlove : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 30; 
            Item.height = 34; 
            Item.rare = ItemRarityID.Cyan;
            Item.value = PriceByRarity.Cyan_9;
            Item.accessory = true; 
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.statDefense += 4; 
            player.endurance += 0.06f; 

            if (player.statLife < player.statLifeMax2 * 0.66f)
            {
                player.statDefense += 4; 
                player.endurance += 0.06f; 
            }

            if (player.statLife < player.statLifeMax2 * 0.33f)
            {
                player.statDefense += 8; 
                player.endurance += 0.12f; 
            }

            if (!hideVisual)
            {

                Vector2 value10 = Main.OffsetsPlayerOnhand[player.bodyFrame.Y / 56] * 2f;

                if (player.direction != 1)
                {
                    value10.X = (float)player.bodyFrame.Width - value10.X;
                }

                if (player.gravDir != 1f)
                {
                    value10.Y = (float)player.bodyFrame.Height - value10.Y;
                }

                value10 -= new Vector2(player.bodyFrame.Width - player.width, player.bodyFrame.Height - 42) / 2f;
                Vector2 position = player.RotatedRelativePoint(player.position + value10) - player.velocity;
                if (Main.rand.NextBool(80))
                {
                    for (int num183 = 0; num183 < 2; num183++)
                    {
                        Dust dust = Main.dust[Dust.NewDust(player.Center, 0, 0, 57, player.direction * 2, 0f, 150, default(Color), .4f)]; //gold dust
                        dust.position = position;
                        dust.velocity *= 0f;
                        dust.noGravity = true;
                        dust.fadeIn = 1f;
                        dust.velocity += player.velocity;
                        dust.noLight = true; //this is being ignored oh well

                        if (Main.rand.NextBool(2))
                        {
                            dust.position += Utils.RandomVector2(Main.rand, -4f, 4f);
                            dust.scale += Main.rand.NextFloat();

                            if (Main.rand.NextBool(2))
                            {
                                dust.customData = player;
                            }
                        }
                    }
                }
                if (Main.rand.NextBool(80))
                {
                    for (int num183 = 0; num183 < 2; num183++)
                    {
                        Dust dust = Main.dust[Dust.NewDust(player.Center, 0, 0, 180, player.direction * 2, 0f, 150, default(Color), .4f)]; //blue dust
                        dust.position = position;
                        dust.velocity *= 0f;
                        dust.noGravity = true;
                        dust.fadeIn = 1f;
                        dust.velocity += player.velocity;
                        dust.noLight = true;

                        if (Main.rand.NextBool(2))
                        {
                            dust.position += Utils.RandomVector2(Main.rand, -4f, 4f);
                            dust.scale += Main.rand.NextFloat();

                            if (Main.rand.NextBool(2))
                            {
                                dust.customData = player;
                            }
                        }
                    }
                }
                if (player.statLife <= (player.statLifeMax2 * 0.33f))
                {
                    for (int num183 = 0; num183 < 2; num183++)
                    {
                        Dust dust = Main.dust[Dust.NewDust(player.Center, 0, 0, 57, player.direction * 2, 0f, 150, default(Color), 1f)]; //gold dust when barrier active
                        dust.position = position;
                        dust.velocity *= 0f;
                        dust.noGravity = true;
                        dust.fadeIn = 1f;
                        dust.velocity += player.velocity;

                        if (Main.rand.NextBool(2))
                        {
                            dust.position += Utils.RandomVector2(Main.rand, -4f, 4f);
                            dust.scale += Main.rand.NextFloat();

                            if (Main.rand.NextBool(2))
                            {
                                dust.customData = player;
                            }
                        }
                    }
                }
            }
        }
        public override void UpdateVanity(Player player)
        {

            Vector2 value10 = Main.OffsetsPlayerOnhand[player.bodyFrame.Y / 56] * 2f;

            if (player.direction != 1)
            {
                value10.X = (float)player.bodyFrame.Width - value10.X;
            }

            if (player.gravDir != 1f)
            {
                value10.Y = (float)player.bodyFrame.Height - value10.Y;
            }

            value10 -= new Vector2(player.bodyFrame.Width - player.width, player.bodyFrame.Height - 42) / 2f;
            Vector2 position = player.RotatedRelativePoint(player.position + value10) - player.velocity;
            if (Main.rand.NextBool(80))
            {
                for (int num183 = 0; num183 < 2; num183++)
                {
                    Dust dust = Main.dust[Dust.NewDust(player.Center, 0, 0, 57, player.direction * 2, 0f, 150, default(Color), .4f)]; //gold dust
                    dust.position = position;
                    dust.velocity *= 0f;
                    dust.noGravity = true;
                    dust.fadeIn = 1f;
                    dust.velocity += player.velocity;
                    dust.noLight = true;

                    if (Main.rand.NextBool(2))
                    {
                        dust.position += Utils.RandomVector2(Main.rand, -4f, 4f);
                        dust.scale += Main.rand.NextFloat();

                        if (Main.rand.NextBool(2))
                        {
                            dust.customData = player;
                        }
                    }
                }
            }
            if (Main.rand.NextBool(80))
            {
                for (int num183 = 0; num183 < 2; num183++)
                {
                    Dust dust = Main.dust[Dust.NewDust(player.Center, 0, 0, 180, player.direction * 2, 0f, 150, default(Color), .4f)]; //blue dust
                    dust.position = position;
                    dust.velocity *= 0f;
                    dust.noGravity = true;
                    dust.fadeIn = 1f;
                    dust.velocity += player.velocity;
                    dust.noLight = true;

                    if (Main.rand.NextBool(2))
                    {
                        dust.position += Utils.RandomVector2(Main.rand, -4f, 4f);
                        dust.scale += Main.rand.NextFloat();

                        if (Main.rand.NextBool(2))
                        {
                            dust.customData = player;
                        }
                    }
                }
            }
        }

        

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.TitanGlove);
            recipe.AddIngredient(ItemID.MythrilBar, 3);
            recipe.AddIngredient(ModContent.ItemType<GuardianSoul>());
            recipe.AddIngredient(ModContent.ItemType<SoulOfAttraidies>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 50000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
