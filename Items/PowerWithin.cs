using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    public class PowerWithin : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Harness the power of flame to actualize the inner-self" +
                                "\nIncreases damage by 20% and stamina regeneration by 30%" +
                                "\nExcessive power eats away the life-force of its caster" +
                                "\nLasts 30 seconds, can't be cancelled" +
                                "\nEffect potency doubled for the [c/6d8827:Bearer of the Curse]");
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Orange;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 60;
            Item.useAnimation = 60;
            Item.value = 15000;

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
                player.AddBuff(ModContent.BuffType<Buffs.Crippled>(), 60);
                player.AddBuff(ModContent.BuffType<Buffs.GrappleMalfunction>(), 60);
            }

            if (player.itemTime < (int)(Item.useTime / PlayerLoader.UseTimeMultiplier(player, Item)) / 2)
            {
                Item.useStyle = ItemUseStyleID.HoldUp;

                if (Main.rand.Next(2) == 0)
                {
                    if (player.direction == 1)
                    {
                        Dust dust = Main.dust[Dust.NewDust(new Vector2(player.position.X + 10, player.position.Y - 4), 10, 10, 6, player.velocity.X, player.velocity.Y, 100, default, Main.rand.NextFloat(1f, 1.5f))];
                        dust.noGravity = true;
                    }
                    else
                    {
                        Dust dust = Main.dust[Dust.NewDust(new Vector2(player.position.X - 2, player.position.Y - 4), 10, 10, 6, player.velocity.X, player.velocity.Y, 100, default, Main.rand.NextFloat(1f, 1.5f))];
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
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item100 with { Volume = 1f, PitchVariance = 0.3f }, player.Center);

                player.AddBuff(ModContent.BuffType<Buffs.PowerWithin>(), 1800); //30s

                for (int q = 0; q < 30; q++)
                {
                    int z = Dust.NewDust(player.Center, 40, 56, 270, 0f, 0f, 120, default(Color), 1f);
                    Main.dust[z].noGravity = true;
                    Main.dust[z].velocity *= 2.75f;
                    Main.dust[z].fadeIn = 1.3f;
                    Vector2 vectorother = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                    vectorother.Normalize();
                    vectorother *= (float)Main.rand.Next(50, 100) * 0.2f;
                    Main.dust[z].velocity = vectorother;
                    vectorother.Normalize();
                    vectorother *= 10f;
                    Main.dust[z].position = player.Center - vectorother;
                }

                for (int q = 0; q < 30; q++)
                {
                    int z = Dust.NewDust(player.Center, 40, 56, 270, 0f, 0f, 120, default(Color), 1f);
                    Main.dust[z].noGravity = true;
                    Main.dust[z].velocity *= 2.75f;
                    Main.dust[z].fadeIn = 1.3f;
                    Vector2 vectorother = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                    vectorother.Normalize();
                    vectorother *= (float)Main.rand.Next(50, 100) * 0.12f;
                    Main.dust[z].velocity = vectorother;
                    vectorother.Normalize();
                    vectorother *= 30f;
                    Main.dust[z].position = player.Center - vectorother;
                }

            }
        }
    }
}