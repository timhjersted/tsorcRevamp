using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Defensive.MythrilGlove;

public class MythrilGlovePlayer : ModPlayer
{
    public bool Equipped;
    public bool Visual;
    public override void ResetEffects()
    {
        Equipped = false;
        Visual = false;
    }

    public void ApplyMythrilGlove(in int currentHealth, in int threshold1, in int threshold2)
    {
        Player.statDefense += MythrilGloveItem.Def1; 
        Player.endurance += MythrilGloveItem.Res1 / 100f; 

        if (currentHealth < threshold1)
        {
            Player.statDefense += MythrilGloveItem.Def2; 
            Player.endurance += MythrilGloveItem.Res2 / 100f; 
        }

        if (Player.statLife < threshold2)
        {
            Player.statDefense += MythrilGloveItem.Def3; 
            Player.endurance += MythrilGloveItem.Res3 / 100f; 
        }

        if (Visual)
        {
                Vector2 value10 = Main.OffsetsPlayerOnhand[Player.bodyFrame.Y / 56] * 2f;

                if (Player.direction != 1)
                {
                    value10.X = (float)Player.bodyFrame.Width - value10.X;
                }

                if (Player.gravDir != 1f)
                {
                    value10.Y = (float)Player.bodyFrame.Height - value10.Y;
                }

                value10 -= new Vector2(Player.bodyFrame.Width - Player.width, Player.bodyFrame.Height - 42) / 2f;
                Vector2 position = Player.RotatedRelativePoint(Player.position + value10) - Player.velocity;
                if (Main.rand.NextBool(80))
                {
                    for (int num183 = 0; num183 < 2; num183++)
                    {
                        Dust dust = Main.dust[Dust.NewDust(Player.Center, 0, 0, 57, Player.direction * 2, 0f, 150, default(Color), .4f)]; //gold dust
                        dust.position = position;
                        dust.velocity *= 0f;
                        dust.noGravity = true;
                        dust.fadeIn = 1f;
                        dust.velocity += Player.velocity;
                        dust.noLight = true; //this is being ignored oh well

                        if (Main.rand.NextBool(2))
                        {
                            dust.position += Utils.RandomVector2(Main.rand, -4f, 4f);
                            dust.scale += Main.rand.NextFloat();

                            if (Main.rand.NextBool(2))
                            {
                                dust.customData = Player;
                            }
                        }
                    }
                }
                if (Main.rand.NextBool(80))
                {
                    for (int num183 = 0; num183 < 2; num183++)
                    {
                        Dust dust = Main.dust[Dust.NewDust(Player.Center, 0, 0, 180, Player.direction * 2, 0f, 150, default(Color), .4f)]; //blue dust
                        dust.position = position;
                        dust.velocity *= 0f;
                        dust.noGravity = true;
                        dust.fadeIn = 1f;
                        dust.velocity += Player.velocity;
                        dust.noLight = true;

                        if (Main.rand.NextBool(2))
                        {
                            dust.position += Utils.RandomVector2(Main.rand, -4f, 4f);
                            dust.scale += Main.rand.NextFloat();

                            if (Main.rand.NextBool(2))
                            {
                                dust.customData = Player;
                            }
                        }
                    }
                }
                
                if (currentHealth <= (threshold2))
                {
                    for (int num183 = 0; num183 < 2; num183++)
                    {
                        Dust dust = Main.dust[Dust.NewDust(Player.Center, 0, 0, 57, Player.direction * 2, 0f, 150, default(Color), 1f)]; //gold dust when barrier active
                        dust.position = position;
                        dust.velocity *= 0f;
                        dust.noGravity = true;
                        dust.fadeIn = 1f;
                        dust.velocity += Player.velocity;

                        if (Main.rand.NextBool(2))
                        {
                            dust.position += Utils.RandomVector2(Main.rand, -4f, 4f);
                            dust.scale += Main.rand.NextFloat();

                            if (Main.rand.NextBool(2))
                            {
                                dust.customData = Player;
                            }
                        }
                    }
                }
        }
    }
}