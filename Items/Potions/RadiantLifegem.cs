using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions
{
    class RadiantLifegem : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("\nStone made up of crystallized souls" +
                "\nGradually restores HP" +
                "\nThe dull glimmer of these mysterious" +
                "\nstones brightens with the passage of time" +
                "\nRestores 200 HP over the course of 13 seconds");


        }

        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.width = 24;
            Item.height = 30;
            Item.maxStack = 99;
            Item.value = 10000;
            Item.useAnimation = 90;
            Item.useTime = 90;
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
                float slowdownY = player.velocity.Y * .9f;

                player.velocity.X = slowdownX;
                player.velocity.Y = slowdownY;
            }
        }

        public override void UseStyle(Player player, Rectangle rectangle)
        {
            if (player.itemTime == 0)
            {
                player.itemTime = (int)(Item.useTime / PlayerLoader.UseTimeMultiplier(player, Item));
                player.AddBuff(ModContent.BuffType<Buffs.Crippled>(), 90);
                player.AddBuff(ModContent.BuffType<Buffs.GrappleMalfunction>(), 90);
            }

            if (player.itemTime < (int)(Item.useTime / PlayerLoader.UseTimeMultiplier(player, Item)) / 2)
            {
                Item.useStyle = ItemUseStyleID.HoldUp;

                if (Main.rand.Next(4) == 0)
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

                player.AddBuff(ModContent.BuffType<Buffs.RadiantLifegemHealing>(), 800); //just over 13 seconds
                player.AddBuff(BuffID.PotionSickness, player.pStone ? 1800 : 3600);

                if (Item.stack == 1) Item.TurnToAir();
                else Item.stack--;

                if (Main.mouseItem.stack == 1) Main.mouseItem.TurnToAir();
                else Main.mouseItem.stack--;
            }
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Lighting.AddLight(Item.Center, 0.25f, 0.25f, 0.15f);

            if (Main.rand.Next(25) == 0)
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(Item.position.X, Item.position.Y), 20, 26, 43, Item.velocity.X, Item.velocity.Y, 100, Color.White, Main.rand.NextFloat(.4f, .6f))];
                dust.velocity *= 0f;
                dust.noGravity = true;
                dust.fadeIn = 1.3f;
            }

            Color color = Color.White * 0.6f;
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
            spriteBatch.Draw(texture, new Vector2(Item.position.X - Main.screenPosition.X + Item.width * 0.5f, Item.position.Y - Main.screenPosition.Y + Item.height - texture.Height * 0.5f + 2f),
                new Rectangle(0, 0, texture.Width, texture.Height), color, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }
    }
}
