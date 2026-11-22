using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Damage.ShadowmoonCloak;

public class ShadowmoonCloakPlayer : ModPlayer
{
    public bool Equipped;
    public bool Visual;
    public override void ResetEffects()
    {
        Equipped = false;
        Visual = false;
    }
    public void ApplyShadowMoonCloak(in int currentHealth, in int threshold, float efficiency = 1f)
    {
        Player.GetCritChance(DamageClass.Generic) += ShadowmoonCloakItem.DamageAndCritIncrease1 * efficiency;
        Player.GetDamage(DamageClass.Generic) += (ShadowmoonCloakItem.DamageAndCritIncrease1 / 100f) * efficiency;
        Player.starCloakItem = new Item(ItemID.StarCloak);
        
        if (currentHealth <= threshold)
        {
            Player.manaRegenBonus += (int)(ShadowmoonCloakItem.ManaRegenBonus * efficiency);
            Player.GetCritChance(DamageClass.Generic) += ShadowmoonCloakItem.DamageAndCritIncrease2 * efficiency;
            Player.GetDamage(DamageClass.Generic) += (ShadowmoonCloakItem.DamageAndCritIncrease2 / 100f) * efficiency;

            if (Visual)
            {
                    Lighting.AddLight(Player.Center, .250f, .250f, .650f);
                    if (Player.direction == 1)
                    {
                        if (Main.rand.NextBool(2)) //eye dusts
                        {
                            Dust dust2 = Main.dust[Dust.NewDust(new Vector2(Player.position.X + 8, Player.position.Y + 8), 4, 4, 172, Player.velocity.X, Player.velocity.Y, 30, default(Color), 1f)];
                            dust2.velocity *= 0f;
                            dust2.noGravity = true;
                            dust2.fadeIn = 1f;
                            dust2.velocity += Player.velocity;
                        }

                        if (Player.velocity.X > 2f)
                        {
                            Lighting.AddLight(Player.Center, .325f, .325f, .8f);
                            if (Main.rand.NextBool(2)) //cape dusts
                            {
                                Dust dust = Main.dust[Dust.NewDust(new Vector2(Player.position.X - 12, Player.position.Y + 6), 18, 34, 68, 0f, 0f, 30, default(Color), .8f)];
                                dust.velocity *= 0f;
                                dust.noGravity = true;
                                dust.fadeIn = 1f;
                                dust.velocity += Player.velocity;

                                Dust dust2 = Main.dust[Dust.NewDust(new Vector2(Player.position.X - 12, Player.position.Y + 6), 18, 34, 172, 0f, 0f, 30, default(Color), 1f)];
                                dust2.velocity *= 0f;
                                dust2.noGravity = true;
                                dust2.fadeIn = 1f;
                                dust2.velocity += Player.velocity;
                            }
                        }
                    }

                    if (Player.direction == -1)
                    {

                        if (Main.rand.NextBool(2)) //eye dusts
                        {
                            Dust dust2 = Main.dust[Dust.NewDust(new Vector2(Player.position.X + 4, Player.position.Y + 8), 4, 4, 172, 0f, 0f, 30, default(Color), 1f)];
                            dust2.velocity *= 0f;
                            dust2.noGravity = true;
                            dust2.fadeIn = 1f;
                            dust2.velocity += Player.velocity;
                        }
                        if (Player.velocity.X < -2f)
                        {
                            Lighting.AddLight(Player.Center, .325f, .325f, .8f);
                            if (Main.rand.NextBool(2)) //cape dusts
                            {
                                Dust dust = Main.dust[Dust.NewDust(new Vector2(Player.position.X + 14, Player.position.Y + 6), 18, 34, 68, 0f, 0f, 30, default(Color), .8f)];
                                dust.velocity *= 0f;
                                dust.noGravity = true;
                                dust.fadeIn = 1f;
                                dust.velocity += Player.velocity;

                                Dust dust2 = Main.dust[Dust.NewDust(new Vector2(Player.position.X + 14, Player.position.Y + 6), 18, 34, 172, 0f, 0f, 30, default(Color), 1f)];
                                dust2.velocity *= 0f;
                                dust2.noGravity = true;
                                dust2.fadeIn = 1f;
                                dust2.velocity += Player.velocity;
                            }
                        }
                    }
            }
        }
        else if (Visual)
        {
            if (Player.direction == 1)
            {
                if (Main.rand.NextBool(8) && Player.velocity.X > .5f)
                {
                    Dust dust = Main.dust[Dust.NewDust(new Vector2(Player.position.X - 6, Player.position.Y + 16), 4, 24, 68, 0f, 0f, 30, default(Color), .5f)];
                    dust.velocity *= 0f;
                    dust.noGravity = true;
                    dust.fadeIn = 1f;
                    dust.velocity += Player.velocity;
                }
            }
            if (Player.direction == -1)
            {
                if (Main.rand.NextBool(8) && Player.velocity.X < -.5f)
                {
                    Dust dust = Main.dust[Dust.NewDust(new Vector2(Player.position.X + 18, Player.position.Y + 16), 4, 24, 68, 0f, 0f, 30, default(Color), .5f)];
                    dust.velocity *= 0f;
                    dust.noGravity = true;
                    dust.fadeIn = 1f;
                    dust.velocity += Player.velocity;
                }
            }
        }
    }
}