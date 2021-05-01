using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class Bonfire : ModBuff
    {
        public override void SetDefaults()
        {
            DisplayName.SetDefault("Bonfire");
            Description.SetDefault("Stay a little while... Let your soul heal");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = true;
        }

        int bonfireEffectTimer = 0;

        public override void Update(Player player, ref int buffIndex)
        {
            player.calmed = true;
            bonfireEffectTimer++;

            if (player.velocity.X != 0 || player.velocity.Y != 0) //reset if player moves
            {
                bonfireEffectTimer = 0;
            }

            #region Regen & Dusts

            if (!Main.npc.Any(n => n?.active == true && n.boss && n != Main.npc[200]) && player.statLife < player.statLifeMax2) //only heal when no bosses are alive and when hp isn't full
            {
                if (bonfireEffectTimer > 0 && bonfireEffectTimer <= 60 && (player.velocity.X == 0 && player.velocity.Y == 0)) //wind up 1
                {
                    player.lifeRegen = player.statLifeMax2 / 80;

                    if (Main.rand.Next(8) == 0)
                    {
                        int z = Dust.NewDust(player.position, player.width, player.height, 270, 0f, 0f, 120, default(Color), 1f);
                        Main.dust[z].noGravity = true;
                        Main.dust[z].velocity *= 2.75f;
                        Main.dust[z].fadeIn = 1.3f;
                        Vector2 vectorother = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                        vectorother.Normalize();
                        vectorother *= (float)Main.rand.Next(50, 100) * 0.015f;
                        Main.dust[z].velocity = vectorother;
                        vectorother.Normalize();
                        vectorother *= 10f;
                        Main.dust[z].position = player.Center - vectorother;
                    }
                }


                if (bonfireEffectTimer > 60 && bonfireEffectTimer <= 100 && (player.velocity.X == 0 && player.velocity.Y == 0)) //wind up 2
                {
                    player.lifeRegen = player.statLifeMax2 / 60;

                    if (Main.rand.Next(4) == 0)
                    {
                        int z = Dust.NewDust(player.position, player.width, player.height, 270, 0f, 0f, 120, default(Color), 1f);
                        Main.dust[z].noGravity = true;
                        Main.dust[z].velocity *= 2.75f;
                        Main.dust[z].fadeIn = 1.3f;
                        Vector2 vectorother = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                        vectorother.Normalize();
                        vectorother *= (float)Main.rand.Next(50, 100) * 0.025f;
                        Main.dust[z].velocity = vectorother;
                        vectorother.Normalize();
                        vectorother *= 15f;
                        Main.dust[z].position = player.Center - vectorother;
                    }
                }


                if (bonfireEffectTimer > 100 && bonfireEffectTimer <= 140 && (player.velocity.X == 0 && player.velocity.Y == 0)) //wind up 3
                {
                    player.lifeRegen = player.statLifeMax2 / 30;

                    if (Main.rand.Next(2) == 0)
                    {
                        int z = Dust.NewDust(player.position, player.width, player.height, 270, 0f, 0f, 120, default(Color), 1f);
                        Main.dust[z].noGravity = true;
                        Main.dust[z].velocity *= 2.75f;
                        Main.dust[z].fadeIn = 1.3f;
                        Vector2 vectorother = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                        vectorother.Normalize();
                        vectorother *= (float)Main.rand.Next(50, 100) * 0.035f;
                        Main.dust[z].velocity = vectorother;
                        vectorother.Normalize();
                        vectorother *= 20f;
                        Main.dust[z].position = player.Center - vectorother;
                    }
                }


                if (bonfireEffectTimer > 140 && (player.velocity.X == 0 && player.velocity.Y == 0))//full effect
                {
                    player.lifeRegen = player.statLifeMax2 / 15;

                    int z = Dust.NewDust(player.position, player.width, player.height, 270, 0f, 0f, 120, default(Color), 1f);
                    Main.dust[z].noGravity = true;
                    Main.dust[z].velocity *= 2.75f;
                    Main.dust[z].fadeIn = 1.3f;
                    Vector2 vectorother = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                    vectorother.Normalize();
                    vectorother *= (float)Main.rand.Next(80, 95) * 0.043f;
                    Main.dust[z].velocity = vectorother;
                    vectorother.Normalize();
                    vectorother *= 25f;
                    Main.dust[z].position = player.Center - vectorother;
                }
            }
            #endregion
        }
    }
}
