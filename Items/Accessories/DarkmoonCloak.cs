using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    [AutoloadEquip(EquipType.Back)]

    public class DarkmoonCloak : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Rapid mana regen, +15% magic crit & +15% magic dmg when health falls below 150\n" +
                                "Provides Star Cloak, +5% magic crit & +5% magic damage boost normally");
        }

        public override void SetDefaults()
        {
            item.width = 28;
            item.height = 28;
            item.accessory = true;
            item.maxStack = 1;
            item.rare = ItemRarityID.Pink;
            item.value = PriceByRarity.Pink_5;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 20000);
            recipe.AddIngredient(ItemID.SoulofNight, 2);
            recipe.AddIngredient(ItemID.SoulofLight, 2);
            recipe.AddIngredient(ItemID.StarCloak, 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player)
        {

            player.GetModPlayer<tsorcRevampPlayer>().DarkmoonCloak = true;

            if (player.statLife <= 150)
            {
                player.manaRegenBuff = true;
                player.starCloak = true;
                player.magicCrit += 15;
                player.magicDamage += .15f;

            }
            else
            {
                player.starCloak = true;
                player.magicCrit += 5;
                player.magicDamage += .05f;
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode)
            {
                //only insert the tooltip if the last valid line is not the name, the "Equipped in social slot" line, or the "No stats will be gained" line (aka do not insert if in a vanity slot)
                int ttindex = tooltips.FindLastIndex(t => t.mod == "Terraria" && t.Name != "ItemName" && t.Name != "Social" && t.Name != "SocialDesc" && !t.Name.Contains("Prefix"));
                if (ttindex != -1)
                {// if we find one
                    //insert the extra tooltip line
                    tooltips.Insert(ttindex + 1, new TooltipLine(mod, "RevampDarkmoonCloak", "Magic Imbues no longer need to go on cooldown"));
                }
            }
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (!hideVisual)
            {
                if (player.statLife <= 150)
                {
                    Lighting.AddLight(player.Center, .250f, .250f, .650f);
                    if (player.direction == 1)
                    {
                        if (Main.rand.Next(2) == 0) //eye dusts
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
                            if (Main.rand.Next(2) == 0) //cape dusts
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

                        if (Main.rand.Next(2) == 0) //eye dusts
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
                            if (Main.rand.Next(2) == 0) //cape dusts
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
                        if (Main.rand.Next(8) == 0 && player.velocity.X > .5f)
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
                        if (Main.rand.Next(8) == 0 && player.velocity.X < -.5f)
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

        public override void UpdateVanity(Player player, EquipType type)
        {
            if (player.direction == 1)
            {
                if (Main.rand.Next(8) == 0 && player.velocity.X > .5f)
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
                if (Main.rand.Next(8) == 0 && player.velocity.X < -.5f)
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
