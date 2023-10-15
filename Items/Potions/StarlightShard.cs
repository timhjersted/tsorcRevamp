using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Potions
{
    class StarlightShard : ModItem
    {
        public static float BaseDuration = 12f;
        public static float BaseRestorationPercent = 1f;
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.width = 48;
            Item.height = 48;
            Item.maxStack = 9999;
            Item.value = 1000;
            Item.useTime = 90;
            Item.useAnimation = Item.useTime;
            Item.useTurn = true;
            Item.rare = ItemRarityID.Cyan;
        }


        public override bool CanUseItem(Player player)
        {
            if (player.HasBuff(BuffID.Frozen) || player.HasBuff(BuffID.Stoned))
            {
                return false;
            }
            return player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse;
        }

        public override void HoldItem(Player player)
        {
            if (player.itemAnimation != 0)
            {
                float slowdownX = player.velocity.X * .9f;

                player.velocity.X = slowdownX;
            }
        }

        public override void UseStyle(Player player, Rectangle rectangle)
        {
            if (player.itemTime == 0)
            {
                player.itemTime = (int)(Item.useTime / PlayerLoader.UseTimeMultiplier(player, Item));
                player.AddBuff(ModContent.BuffType<Crippled>(), Item.useTime);
                player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), Item.useTime);
            }

            if (player.itemTime < (int)(Item.useTime / PlayerLoader.UseTimeMultiplier(player, Item)) / 2)
            {
                Item.useStyle = ItemUseStyleID.HoldUp;

                if (Main.rand.NextBool(4))
                {
                    if (player.direction == 1)
                    {
                        Dust dust = Main.dust[Dust.NewDust(new Vector2(player.position.X + 10, player.position.Y - 4), 10, 10, 43, player.velocity.X, player.velocity.Y, 100, Color.White, Main.rand.NextFloat(1f, 1.5f))];
                        dust.noGravity = true;
                    }
                    else
                    {
                        Dust dust = Main.dust[Dust.NewDust(new Vector2(player.position.X - 2, player.position.Y - 4), 10, 10, 43, player.velocity.X, player.velocity.Y, 100, Color.White, Main.rand.NextFloat(1f, 1.5f))];
                        dust.noGravity = true;
                    }
                }
            }

            if (player.itemTime >= (int)(Item.useTime / PlayerLoader.UseTimeMultiplier(player, Item)) / 2)
            {
                Item.useStyle = ItemUseStyleID.Shoot;
            }

            if (player.itemTime == 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.9f, PitchVariance = 0.3f }, player.Center);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.9f, PitchVariance = 0.3f }, player.Center);

                for (int i = 0; i < 30; i++)
                {
                    if (player.direction == 1)
                    {
                        Dust dust = Main.dust[Dust.NewDust(new Vector2(player.position.X + 10, player.position.Y - 4), DustID.Silver, 10, 43, Main.rand.NextFloat(-1.2f, 1.2f), Main.rand.NextFloat(-1.2f, 1.2f), 100, Color.White, Main.rand.NextFloat(1.5f, 3f))];
                        dust.noGravity = true;
                    }
                    else
                    {
                        Dust dust = Main.dust[Dust.NewDust(new Vector2(player.position.X - 2, player.position.Y - 4), DustID.Silver, 10, 43, Main.rand.NextFloat(-1.2f, 1.2f), Main.rand.NextFloat(-1.2f, 1.2f), 100, Color.White, Main.rand.NextFloat(1.5f, 3f))];
                        dust.noGravity = true;
                    }
                }
                if (player.manaRegenBuff)
                {
                    player.AddBuff(ModContent.BuffType<Buffs.StarlightShardRestoration>(), (int)(BaseDuration * 60f * (1.5f + player.manaRegenDelayBonus)));
                }
                else
                {
                    player.AddBuff(ModContent.BuffType<Buffs.StarlightShardRestoration>(), (int)(BaseDuration * 60f * (1f + player.manaRegenDelayBonus)));
                }

                //if (Main.mouseItem == null) // Not sure why but seems like it's not null if you're using something
                //{
                if (Item.stack == 1) Item.TurnToAir();
                else Item.stack--;
                //}
                //else
                //{
                if (Main.mouseItem.stack == 1) Main.mouseItem.TurnToAir();
                else Main.mouseItem.stack--;
                //}
            }
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;
            float RestorationPercentPerSecond = BaseRestorationPercent * (1f + ((float)player.manaRegenBonus / 10f));
            int RestorationDuration = (int)(BaseDuration * ((player.manaRegenBuff ? 1.5f : 1f) + player.manaRegenDelayBonus));
            int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
            if (ttindex != -1)
            {
                tooltips.Insert(ttindex, new TooltipLine(Mod, "Formatting", Language.GetTextValue("Mods.tsorcRevamp.Items.StarlightShard.Restoration").FormatWith(RestorationPercentPerSecond, RestorationDuration)));
            }
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Lighting.AddLight(Item.Center, 0.15f, 0.15f, 0.1f);

            if (Main.rand.NextBool(35))
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(Item.position.X, Item.position.Y), 14, 14, 43, Item.velocity.X, Item.velocity.Y, 100, Color.White, Main.rand.NextFloat(.3f, .5f))];
                dust.velocity *= 0f;
                dust.noGravity = true;
                dust.fadeIn = 1.3f;
            }

            Color color = Color.White * 0.4f;
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
            spriteBatch.Draw(texture, new Vector2(Item.position.X - Main.screenPosition.X + Item.width * 0.5f, Item.position.Y - Main.screenPosition.Y + Item.height - texture.Height * 0.5f + 2f),
                new Rectangle(0, 0, texture.Width, texture.Height), color, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0);
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.LesserManaPotion, 50);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>());
            recipe.AddCondition(tsorcRevampWorld.BearerOfTheCurseEnabled);
            recipe.AddTile(TileID.Bottles);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.ManaPotion, 40);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>());
            recipe2.AddCondition(tsorcRevampWorld.BearerOfTheCurseEnabled);
            recipe2.AddTile(TileID.Bottles);

            recipe2.Register();

            Recipe recipe3 = CreateRecipe();
            recipe3.AddIngredient(ItemID.GreaterManaPotion, 30);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>());
            recipe3.AddCondition(tsorcRevampWorld.BearerOfTheCurseEnabled);
            recipe3.AddTile(TileID.Bottles);

            recipe3.Register();

            Recipe recipe4 = CreateRecipe();
            recipe4.AddIngredient(ItemID.SuperManaPotion, 24);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>());
            recipe4.AddCondition(tsorcRevampWorld.BearerOfTheCurseEnabled);
            recipe4.AddTile(TileID.Bottles);

            recipe4.Register();

            Recipe recipe5 = CreateRecipe();
            recipe5.AddIngredient(ModContent.ItemType<SupremeManaPotion>(), 7);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>());
            recipe5.AddCondition(tsorcRevampWorld.BearerOfTheCurseEnabled);
            recipe5.AddTile(TileID.Bottles);

            recipe5.Register();

            Recipe recipe6 = CreateRecipe();
            recipe6.AddIngredient(ItemID.ManaCrystal, 4);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>());
            recipe6.AddCondition(tsorcRevampWorld.BearerOfTheCurseEnabled);
            recipe6.AddTile(TileID.Bottles);

            recipe6.Register();
        }
    }
}
