using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Buffs
{
    class StoryTime : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Story Time");
            /* Description.SetDefault("Stay a moment... gain knowledge. \n" +
                                   "Enemy spawns disabled"); */
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = true;
        }

        int storyEffectTimer = 0;

        public override void Update(Player player, ref int buffIndex)
        {
            if (player == Main.LocalPlayer)
            {
                //clears incombat debuff near sign
                if (player.HasBuff(ModContent.BuffType<InCombat>()))
                {
                    player.ClearBuff(ModContent.BuffType<InCombat>());
                    
                }

                player.GetModPlayer<tsorcRevampPlayer>().BossZenBuff = true;
                storyEffectTimer++;

                if (player.velocity.X != 0 || player.velocity.Y != 0) //reset if player moves
                {
                    storyEffectTimer = 0;
                }
                /*
                #region Regen & Dusts

                if (!Main.npc.Any(n => n?.active == true && n.boss && n != Main.npc[200]) && player.statLife < player.statLifeMax) //only heal when no bosses are alive and when hp isn't full
                {
                    if (storyEffectTimer > 0 && storyEffectTimer <= 60 && (player.velocity.X == 0 && player.velocity.Y == 0)) //wind up 1
                    {
                        player.lifeRegen = player.statLifeMax2 / 80;

                        if (Main.rand.NextBool(8))
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


                    if (storyEffectTimer > 60 && storyEffectTimer <= 100 && (player.velocity.X == 0 && player.velocity.Y == 0)) //wind up 2
                    {
                        player.lifeRegen = player.statLifeMax2 / 60;

                        if (Main.rand.NextBool(4))
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


                    if (storyEffectTimer > 100 && storyEffectTimer <= 140 && (player.velocity.X == 0 && player.velocity.Y == 0)) //wind up 3
                    {
                        player.lifeRegen = player.statLifeMax2 / 30;

                        if (Main.rand.NextBool(2))
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


                    if (storyEffectTimer > 140 && (player.velocity.X == 0 && player.velocity.Y == 0))//full effect
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
                */
            }
        }
    }
}
