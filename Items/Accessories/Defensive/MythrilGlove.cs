using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    [AutoloadEquip(EquipType.HandsOn)]
    public class MythrilGlove : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Casts and sustains Wall when wearer is critically wounded" +
                               "\nWall gives +50 defense" +
                               "\nDoes not stack with Fog, Barrier or Shield spells");
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 34;
            Item.accessory = true;
            Item.rare = ItemRarityID.Cyan;
            Item.value = PriceByRarity.Cyan_9;
        }

        public override void UpdateEquip(Player player)
        {
            if ((player.statLife <= (player.statLifeMax * 0.30f)) && !(player.HasBuff(ModContent.BuffType<Buffs.MagicShield>()) || player.HasBuff(ModContent.BuffType<Buffs.MagicBarrier>()) || player.HasBuff(ModContent.BuffType<Buffs.GreatMagicBarrier>())))
            {
                player.AddBuff(ModContent.BuffType<Buffs.GreatMagicShield>(), 1, false);
            }

        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
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
                if (player.statLife <= (player.statLifeMax * 0.30f))
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
            recipe.AddIngredient(ItemID.MythrilBar, 10);
            recipe.AddIngredient(Mod.Find<ModItem>("GuardianSoul").Type);
            recipe.AddIngredient(Mod.Find<ModItem>("SoulOfAttraidies").Type);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 50000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
