using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;


namespace tsorcRevamp.Items
{
    class GlintstonePebble : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("\nCrush in your hand to send out Glintstone Specks" +
                "\nGlintstone Specks hover a moment before firing off toward the closest enemy" +
                "\nGreat for setting up ambushes on unsuspecting foes. Scales with magic damage"); */
        }

        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.width = 16;
            Item.height = 18;
            Item.maxStack = 9999;
            Item.value = 1000;
            Item.useAnimation = 90;
            Item.useTime = 90;
            Item.useTurn = true;
            Item.rare = ItemRarityID.Blue;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(8);
            recipe.AddIngredient(ItemID.ManaCrystal);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }


        public override bool CanUseItem(Player player)
        {
            if (player.HasBuff(BuffID.Frozen) || player.HasBuff(BuffID.Stoned))
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
                player.AddBuff(ModContent.BuffType<Crippled>(), 90);
                player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 90);
            }

            if (player.itemTime < (int)(Item.useTime / PlayerLoader.UseTimeMultiplier(player, Item)) / 2)
            {
                Item.useStyle = ItemUseStyleID.HoldUp;

                if (Main.rand.NextBool(4))
                {
                    if (player.direction == 1)
                    {
                        Dust dust = Main.dust[Dust.NewDust(new Vector2(player.position.X + 10, player.position.Y - 4), 10, 10, 68, player.velocity.X, player.velocity.Y, 100, Color.White, Main.rand.NextFloat(.8f, 1.2f))];
                        dust.noGravity = true;
                    }
                    else
                    {
                        Dust dust = Main.dust[Dust.NewDust(new Vector2(player.position.X - 2, player.position.Y - 4), 10, 10, 68, player.velocity.X, player.velocity.Y, 100, Color.White, Main.rand.NextFloat(.8f, 1.2f))];
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
                        Dust dust = Main.dust[Dust.NewDust(new Vector2(player.position.X + 10, player.position.Y - 4), 10, 10, 68, Main.rand.NextFloat(-1.2f, 1.2f), Main.rand.NextFloat(-1.2f, 1.2f), 100, Color.White, Main.rand.NextFloat(.8f, 1.2f))];
                        dust.noGravity = true;
                    }
                    else
                    {
                        Dust dust = Main.dust[Dust.NewDust(new Vector2(player.position.X - 2, player.position.Y - 4), 10, 10, 68, Main.rand.NextFloat(-1.2f, 1.2f), Main.rand.NextFloat(-1.2f, 1.2f), 100, Color.White, Main.rand.NextFloat(.8f, 1.2f))];
                        dust.noGravity = true;
                    }
                }

                int damage = (int)(player.GetTotalDamage(DamageClass.Magic).ApplyTo(48));

                if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
                {
                    damage = (int)(damage * 1.2f); //because player.GetDamage(DamageClass.Generic)Mult isnt taking it into accound for some reason :/
                    player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= 20;
                }

                for (int i = 0; i < 7; i++)
                {
                    Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center + new Vector2(4 * player.direction, -20), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.GlintstoneSpeck>(), damage, 2.5f, Main.myPlayer);
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
                Item.stack--;
            }
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Lighting.AddLight(Item.Center, 0.2f, 0.2f, 0.35f);

            if (Main.rand.NextBool(35))
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(Item.position.X, Item.position.Y), 14, 14, 68, Item.velocity.X, Item.velocity.Y, 100, default, Main.rand.NextFloat(.4f, .8f))];
                dust.velocity *= 0f;
                dust.noGravity = true;
                dust.fadeIn = 1.3f;
            }

            Color color = Color.White * 0.8f;

            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
            spriteBatch.Draw(texture, new Vector2(Item.position.X - Main.screenPosition.X + Item.width * 0.5f, Item.position.Y - Main.screenPosition.Y + Item.height - texture.Height * 0.5f + 2f),
                new Rectangle(0, 0, texture.Width, texture.Height), color, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0);
        }
    }
}
