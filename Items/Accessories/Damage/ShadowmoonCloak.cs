using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Damage
{
    [AutoloadEquip(EquipType.Back)]

    public class ShadowmoonCloak : ModItem
    {
        public static float DamageAndCritIncrease1 = 5f;
        public static float LifeThreshold = 40f;
        public static int ManaRegenBonus = 5;
        public static float DamageAndCritIncrease2 = 10f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(DamageAndCritIncrease1, LifeThreshold, ManaRegenBonus, DamageAndCritIncrease2);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.accessory = true;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.Pink_5;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 25000);
            recipe.AddIngredient(ItemID.SoulofNight, 1);
            recipe.AddIngredient(ItemID.StarCloak, 1);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().ShadowmoonCloak = true;
            player.GetCritChance(DamageClass.Generic) += DamageAndCritIncrease1;
            player.GetDamage(DamageClass.Generic) += DamageAndCritIncrease1 / 100f;
            player.starCloakItem = new Item(ItemID.StarCloak);

            if (player.statLife <= (int)(player.statLifeMax2 * (LifeThreshold / 100f)))
            {
                player.manaRegenBonus += ManaRegenBonus;
                player.GetCritChance(DamageClass.Generic) += DamageAndCritIncrease2;
                player.GetDamage(DamageClass.Generic) += DamageAndCritIncrease2 / 100f;

            }
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (!hideVisual)
            {
                if (player.statLife <= (int)(player.statLifeMax2 * (LifeThreshold / 100f)))
                {
                    Lighting.AddLight(player.Center, .250f, .250f, .650f);
                    if (player.direction == 1)
                    {
                        if (Main.rand.NextBool(2)) //eye dusts
                        {
                            Dust dust2 = Main.dust[Dust.NewDust(new Vector2(player.position.X + 8, player.position.Y + 8), 4, 4, 172, player.velocity.X, player.velocity.Y, 30, default(Color), 1f)];
                            dust2.velocity *= 0f;
                            dust2.noGravity = true;
                            dust2.fadeIn = 1f;
                            dust2.velocity += player.velocity;
                        }

                        if (player.velocity.X > 2f)
                        {
                            Lighting.AddLight(player.Center, .325f, .325f, .8f);
                            if (Main.rand.NextBool(2)) //cape dusts
                            {
                                Dust dust = Main.dust[Dust.NewDust(new Vector2(player.position.X - 12, player.position.Y + 6), 18, 34, 68, 0f, 0f, 30, default(Color), .8f)];
                                dust.velocity *= 0f;
                                dust.noGravity = true;
                                dust.fadeIn = 1f;
                                dust.velocity += player.velocity;

                                Dust dust2 = Main.dust[Dust.NewDust(new Vector2(player.position.X - 12, player.position.Y + 6), 18, 34, 172, 0f, 0f, 30, default(Color), 1f)];
                                dust2.velocity *= 0f;
                                dust2.noGravity = true;
                                dust2.fadeIn = 1f;
                                dust2.velocity += player.velocity;
                            }
                        }
                    }

                    if (player.direction == -1)
                    {

                        if (Main.rand.NextBool(2)) //eye dusts
                        {
                            Dust dust2 = Main.dust[Dust.NewDust(new Vector2(player.position.X + 4, player.position.Y + 8), 4, 4, 172, 0f, 0f, 30, default(Color), 1f)];
                            dust2.velocity *= 0f;
                            dust2.noGravity = true;
                            dust2.fadeIn = 1f;
                            dust2.velocity += player.velocity;
                        }
                        if (player.velocity.X < -2f)
                        {
                            Lighting.AddLight(player.Center, .325f, .325f, .8f);
                            if (Main.rand.NextBool(2)) //cape dusts
                            {
                                Dust dust = Main.dust[Dust.NewDust(new Vector2(player.position.X + 14, player.position.Y + 6), 18, 34, 68, 0f, 0f, 30, default(Color), .8f)];
                                dust.velocity *= 0f;
                                dust.noGravity = true;
                                dust.fadeIn = 1f;
                                dust.velocity += player.velocity;

                                Dust dust2 = Main.dust[Dust.NewDust(new Vector2(player.position.X + 14, player.position.Y + 6), 18, 34, 172, 0f, 0f, 30, default(Color), 1f)];
                                dust2.velocity *= 0f;
                                dust2.noGravity = true;
                                dust2.fadeIn = 1f;
                                dust2.velocity += player.velocity;
                            }
                        }
                    }
                }

                else
                {
                    if (player.direction == 1)
                    {
                        if (Main.rand.NextBool(8) && player.velocity.X > .5f)
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(player.position.X - 6, player.position.Y + 16), 4, 24, 68, 0f, 0f, 30, default(Color), .5f)];
                            dust.velocity *= 0f;
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                            dust.velocity += player.velocity;
                        }
                    }
                    if (player.direction == -1)
                    {
                        if (Main.rand.NextBool(8) && player.velocity.X < -.5f)
                        {
                            Dust dust = Main.dust[Dust.NewDust(new Vector2(player.position.X + 18, player.position.Y + 16), 4, 24, 68, 0f, 0f, 30, default(Color), .5f)];
                            dust.velocity *= 0f;
                            dust.noGravity = true;
                            dust.fadeIn = 1f;
                            dust.velocity += player.velocity;
                        }
                    }
                }
            }
        }

        public override void UpdateVanity(Player player)
        {
            if (player.direction == 1)
            {
                if (Main.rand.NextBool(8) && player.velocity.X > .5f)
                {
                    Dust dust = Main.dust[Dust.NewDust(new Vector2(player.position.X - 6, player.position.Y + 16), 4, 24, 68, 0f, 0f, 30, default(Color), .5f)];
                    dust.velocity *= 0f;
                    dust.noGravity = true;
                    dust.fadeIn = 1f;
                    dust.velocity += player.velocity;
                }
            }
            if (player.direction == -1)
            {
                if (Main.rand.NextBool(8) && player.velocity.X < -.5f)
                {
                    Dust dust = Main.dust[Dust.NewDust(new Vector2(player.position.X + 18, player.position.Y + 16), 4, 24, 68, 0f, 0f, 30, default(Color), .5f)];
                    dust.velocity *= 0f;
                    dust.noGravity = true;
                    dust.fadeIn = 1f;
                    dust.velocity += player.velocity;
                }
            }
        }
    }
}
