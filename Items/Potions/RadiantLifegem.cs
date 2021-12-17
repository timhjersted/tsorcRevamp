using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions
{
    class RadiantLifegem : ModItem
    {
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("\nStone made up of crystallized souls" +
                "\nGradually restores HP" +
                "\nThe dull glimmer of these mysterious" +
                "\nstones brightens with the passage of time" +
                "\nDue to the gradual healing effect, potion sickness is low" +
                "\nRestores 200 HP over the course of 13 seconds"); 


        }

        public override void SetDefaults()
        {
            item.consumable = true;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.width = 24;
            item.height = 30;
            item.maxStack = 99;
            item.value = 10000;
            item.useAnimation = 120;
            item.useTime = 120;
            item.useTurn = true;
            item.rare = ItemRarityID.Orange;

        }


        public override bool CanUseItem(Player player)
        {
            if (player.HasBuff(BuffID.PotionSickness) || player.HasBuff(ModContent.BuffType<Buffs.LifegemHealing>()) || player.HasBuff(ModContent.BuffType<Buffs.RadiantLifegemHealing>()) || !player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse) // If you have the buff or are not BOTC, you can't use
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

        public override void UseStyle(Player player)
        {
            if (player.itemTime == 0)
            {
                player.itemTime = (int)(item.useTime / PlayerHooks.TotalUseTimeMultiplier(player, item));
                player.AddBuff(ModContent.BuffType<Buffs.Crippled>(), 120);
            }

            if (player.itemTime < (int)(item.useTime / PlayerHooks.TotalUseTimeMultiplier(player, item)) / 2)
            {
                item.useStyle = ItemUseStyleID.HoldingUp;

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

            if (player.itemTime >= (int)(item.useTime / PlayerHooks.TotalUseTimeMultiplier(player, item)) / 2)
            {
                item.useStyle = ItemUseStyleID.HoldingOut;
            }

            if (player.itemTime == 1)
            {
                Main.PlaySound(SoundID.Item27.WithVolume(.9f).WithPitchVariance(.3f), player.position); // Plays sound.
                Main.PlaySound(SoundID.Item29.WithVolume(.9f).WithPitchVariance(.3f), player.position); // Plays sound.

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
                player.AddBuff(BuffID.PotionSickness, player.pStone ? 1200 : 2400);

                if (item.stack == 1) item.TurnToAir();
                else item.stack--;
            }
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Lighting.AddLight(item.Center, 0.25f, 0.25f, 0.15f);

            if (Main.rand.Next(25) == 0)
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(item.position.X, item.position.Y), 20, 26, 43, item.velocity.X, item.velocity.Y, 100, Color.White, Main.rand.NextFloat(.4f, .6f))];
                dust.velocity *= 0f;
                dust.noGravity = true;
                dust.fadeIn = 1.3f;
            }

            Color color = Color.White * 0.6f;
            Texture2D texture = Main.itemTexture[item.type];
            spriteBatch.Draw(texture, new Vector2(item.position.X - Main.screenPosition.X + item.width * 0.5f, item.position.Y - Main.screenPosition.Y + item.height - texture.Height * 0.5f + 2f),
                new Rectangle(0, 0, texture.Width, texture.Height), color, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }
    }
}
