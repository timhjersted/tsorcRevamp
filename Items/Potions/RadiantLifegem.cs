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
    class RadiantLifegem : ModItem
    {
        public static int DurationInSeconds = 14;
        public static int HealingDivisor = 6;
        public static int TotalHPRestoration = DurationInSeconds * 60 / HealingDivisor;
        public static int SicknessBaseDuration = 60;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(TotalHPRestoration, DurationInSeconds);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.width = 24;
            Item.height = 30;
            Item.maxStack = 9999;
            Item.value = 10000;
            Item.useTime = 90;
            Item.useAnimation = Item.useTime;
            Item.useTurn = true;
            Item.rare = ItemRarityID.Orange;

        }


        public override bool CanUseItem(Player player)
        {
            if (player.HasBuff(BuffID.PotionSickness) || player.HasBuff(BuffID.Frozen) || player.HasBuff(BuffID.Stoned))
            {
                return false;
            }
            return true;
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
                        Dust dust = Main.dust[Dust.NewDust(new Vector2(player.position.X + 14, player.position.Y - 5), 14, 10, 43, player.velocity.X, player.velocity.Y, 100, Color.White, Main.rand.NextFloat(1f, 1.5f))];
                        dust.noGravity = true;
                    }
                    else
                    {
                        Dust dust = Main.dust[Dust.NewDust(new Vector2(player.position.X - 10, player.position.Y - 5), 14, 10, 43, player.velocity.X, player.velocity.Y, 100, Color.White, Main.rand.NextFloat(1f, 1.5f))];
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

                for (int i = 0; i < 50; i++)
                {
                    if (player.direction == 1)
                    {
                        Dust dust = Main.dust[Dust.NewDust(new Vector2(player.position.X + 14, player.position.Y - 5), 16, 16, 43, Main.rand.NextFloat(-1.4f, 1.4f), Main.rand.NextFloat(-1.4f, 1.4f), 100, Color.White, Main.rand.NextFloat(1.5f, 3f))];
                        dust.noGravity = true;
                    }
                    else
                    {
                        Dust dust = Main.dust[Dust.NewDust(new Vector2(player.position.X - 10, player.position.Y - 5), 16, 16, 43, Main.rand.NextFloat(-1.4f, 1.4f), Main.rand.NextFloat(-1.4f, 1.4f), 100, Color.White, Main.rand.NextFloat(1.5f, 3f))];
                        dust.noGravity = true;
                    }
                }

                player.AddBuff(ModContent.BuffType<Buffs.RadiantLifegemHealing>(), DurationInSeconds * 60);
                player.AddBuff(BuffID.PotionSickness, player.pStone ? (SicknessBaseDuration / 4 * 3 * 60) : (SicknessBaseDuration * 60));

                if (Item.stack == 1) Item.TurnToAir();
                else Item.stack--;

                if (Main.mouseItem.stack == 1) Main.mouseItem.TurnToAir();
                else Main.mouseItem.stack--;
            }
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Lighting.AddLight(Item.Center, 0.25f, 0.25f, 0.15f);

            if (Main.rand.NextBool(25))
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(Item.position.X, Item.position.Y), 20, 26, 43, Item.velocity.X, Item.velocity.Y, 100, Color.White, Main.rand.NextFloat(.4f, .6f))];
                dust.velocity *= 0f;
                dust.noGravity = true;
                dust.fadeIn = 1.3f;
            }

            Color color = Color.White * 0.6f;
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
            spriteBatch.Draw(texture, new Vector2(Item.position.X - Main.screenPosition.X + Item.width * 0.5f, Item.position.Y - Main.screenPosition.Y + Item.height - texture.Height * 0.5f + 2f),
                new Rectangle(0, 0, texture.Width, texture.Height), color, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0);
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int Sickness = SicknessBaseDuration;
            if (Main.LocalPlayer.pStone)
            {
                Sickness = SicknessBaseDuration / 4 * 3;
            }
            int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip4");
            if (ttindex != -1)
            {
                tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Formatting", Language.GetTextValue("Mods.tsorcRevamp.Items.RadiantLifegem.Sickness").FormatWith(Sickness)));
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.GreaterHealingPotion, 33);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>());
            recipe.AddCondition(tsorcRevampWorld.BearerOfTheCurseEnabled);
            recipe.AddTile(TileID.Bottles);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.SuperHealingPotion, 24);
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>());
            recipe2.AddCondition(tsorcRevampWorld.BearerOfTheCurseEnabled);
            recipe2.AddTile(TileID.Bottles);

            recipe2.Register();

            Recipe recipe3 = CreateRecipe();
            recipe3.AddIngredient(ItemID.RestorationPotion, 40);
            recipe3.AddIngredient(ModContent.ItemType<DarkSoul>());
            recipe3.AddCondition(tsorcRevampWorld.BearerOfTheCurseEnabled);
            recipe3.AddTile(TileID.Bottles);

            recipe3.Register();

            Recipe recipe4 = CreateRecipe();
            recipe4.AddIngredient(ItemID.LifeCrystal, 5);
            recipe4.AddIngredient(ModContent.ItemType<DarkSoul>());
            recipe4.AddCondition(tsorcRevampWorld.BearerOfTheCurseEnabled);
            recipe4.AddTile(TileID.Bottles);

            recipe4.Register();
        }
    }
}
