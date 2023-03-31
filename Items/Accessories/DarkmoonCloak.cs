using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    [AutoloadEquip(EquipType.Back)]

    public class DarkmoonCloak : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Inherits the Star Cloaks effect and grants 5% increased damage and critical strike chance" +
                               "\nWhen life falls below 40%, also increases mana regeneration by 5, damage and critical strike chance by 10%" +
                                "\nMagic Imbues no longer need to go on cooldown");
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.accessory = true;
            Item.maxStack = 1;
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

            player.GetModPlayer<tsorcRevampPlayer>().DarkmoonCloak = true;

            if (player.statLife <= (player.statLifeMax / 5 * 2))
            {
                player.manaRegenBonus += 5;
                player.GetCritChance(DamageClass.Generic) += 10;
                player.GetDamage(DamageClass.Generic) += 0.1f;

            }
                player.starCloakItem = new Item(ItemID.StarCloak);
                player.GetCritChance(DamageClass.Generic) += 5;
                player.GetDamage(DamageClass.Generic) += 0.05f;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (!hideVisual)
            {
                if (player.statLife <= (player.statLifeMax / 5 * 2))
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
