using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    public class PowerWithin : ModItem
    {
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Harness the power of flame to actualize the inner-self" +
                                "\nIncreases damage and stamina regeneration by 20%" +
                                "\nExcessive power eats away the life-force of its caster" +
                                "\nLasts 30 seconds, can't be cancelled" +
                                "\nEffect potency doubled for the [c/6d8827:Bearer of the Curse]");
        }

        public override void SetDefaults()
        {
            item.width = 28;
            item.height = 30;
            item.maxStack = 1;
            item.rare = ItemRarityID.Orange;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 60;
            item.useAnimation = 60;
            item.value = 15000;

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
                player.AddBuff(ModContent.BuffType<Buffs.Crippled>(), 60);
                player.AddBuff(ModContent.BuffType<Buffs.GrappleMalfunction>(), 60);
            }

            if (player.itemTime < (int)(item.useTime / PlayerHooks.TotalUseTimeMultiplier(player, item)) / 2)
            {
                item.useStyle = ItemUseStyleID.HoldingUp;

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

            if (player.itemTime >= (int)(item.useTime / PlayerHooks.TotalUseTimeMultiplier(player, item)) / 2)
            {
                item.useStyle = ItemUseStyleID.HoldingOut;
            }

            if (player.itemTime == 1)
            {
                Main.PlaySound(SoundID.Item100.WithVolume(1f), player.position); // Plays sound.

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