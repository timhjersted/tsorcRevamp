using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Systems.OverCrit;

public class OverCritPlayer : ModPlayer
{
    public int CritColorTier = 0;
        public void OverCrit(in int CritChance, DamageClass damageType, ref NPC.HitModifiers modifiers, out int critColorTier)
        {
            int critLevel = (int)(Math.Floor(CritChance / 100f));
            critColorTier = 0;
            if (critLevel != 0 && damageType != DamageClass.Summon && damageType != DamageClass.SummonMeleeSpeed)
            {
                if (critLevel > 1)
                {
                    for (int i = 1; i < critLevel; i++)
                    {
                        modifiers.CritDamage += 1;
                        modifiers.HideCombatText();
                        critColorTier++;
                    }
                }
                if (Main.rand.Next(1, 101) <= (float)CritChance - (100 * critLevel))
                {
                    modifiers.CritDamage += 1;
                    modifiers.HideCombatText();
                    critColorTier++;
                }
            }
            else if (critLevel != 0 && (damageType == DamageClass.Summon | damageType == DamageClass.SummonMeleeSpeed))
            {
                modifiers.SetCrit();
                if (critLevel > 1)
                {
                    for (int i = 1; i < critLevel; i++)
                    {
                        modifiers.CritDamage += 1;
                        modifiers.HideCombatText();
                        critColorTier++;
                    }
                }
                if (Main.rand.Next(1, 101) <= (float)CritChance - (100 * critLevel))
                {
                    modifiers.CritDamage += 1;
                    modifiers.HideCombatText();
                    critColorTier++;
                }
            }
            /*else if (IsWhip)
            {
                if (WhipTipCrit(projectile, projectile.WhipPointsForCollision, targetHitbox) || (Goredrinker && !Player.HasBuff(ModContent.BuffType<GoredrinkerCooldown>()) && GoredrinkerSwung))
                {
                    modifiers.SetCrit();
                    if (critLevel > 0)
                    {
                        for (int i = 0; i < critLevel; i++)
                        {
                            modifiers.CritDamage += 1;
                            modifiers.HideCombatText();
                            critColorTier++;
                        }
                    }
                    if (Main.rand.Next(1, 101) <= (float)CritChance - (100 * critLevel))
                    {
                        modifiers.CritDamage += 1;
                        modifiers.HideCombatText();
                        critColorTier++;
                    }
                }
            }*/
            else
            {
                if (Main.rand.Next(1, 101) <= (float)CritChance - (100 * critLevel))
                {
                    modifiers.SetCrit();
                }
            }
        }
    public void CustomCombatText(in Rectangle targetHitbox, in int damageDealt, in int critColorTier, in bool isCrit, bool isWhipTipCrit = false, bool sendPacket = true)
    {
        Color colorOfCrit = CombatText.DamagedHostile;

        switch (critColorTier)
        {
            case 1:
            {
                colorOfCrit = Color.Blue;
                break;
            }
            case 2:
            {
                colorOfCrit = Color.Purple;
                break;
            }
            case 3:
            {
                colorOfCrit = Color.White;
                break;
            }
            case 4:
            {
                colorOfCrit = Color.Black;
                break;
            }
            case 5:
            {
                colorOfCrit = Color.Red;
                break;
            }
            default:
            {
                if (isCrit)
                {
                    colorOfCrit = CombatText.DamagedHostileCrit;
                }
                break;
            }
        }
        if (!sendPacket) //if it's somebody else's dmg text
        {
            colorOfCrit = colorOfCrit.MultiplyRGBA(new Color(0.3f, 0.3f, 0.3f, 0.15f));
            //Main.NewText(CombatText.DamagedHostile);//r255, g160, b080, a255
            //Main.NewText(CombatText.OthersDamagedHostile);r102, g064, b032, a102
            //Main.NewText(CombatText.DamagedHostileCrit);//r255, g100, b30, a255
            //Main.NewText(CombatText.OthersDamagedHostileCrit);//r102, g040, b012, a102
        }
        CombatText.NewText(targetHitbox, colorOfCrit, damageDealt + (isWhipTipCrit ? "!" : ""), isCrit, false);
        if (Main.netMode == NetmodeID.MultiplayerClient && sendPacket)
        {
            ModPacket textPacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
            textPacket.Write(tsorcPacketID.CustomMultiplayerCombatText);
            textPacket.Write((byte)Player.whoAmI);
            textPacket.Write(targetHitbox.X);
            textPacket.Write(targetHitbox.Y);
            textPacket.Write(targetHitbox.Width);
            textPacket.Write(targetHitbox.Height);
            textPacket.Write(critColorTier);
            textPacket.Write(damageDealt);
            textPacket.Write(isWhipTipCrit);
            textPacket.Write(isCrit);
            
            textPacket.Send();
        }
    }

    public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
    {
        if (!proj.IsMinionOrSentryRelated)
        {
            OverCrit(proj.CritChance, proj.DamageType, ref modifiers, out CritColorTier);
        }
    }

    public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
    {
        OverCrit(Player.GetWeaponCrit(Player.HeldItem), item.DamageType, ref modifiers, out CritColorTier);
    }

    public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (item.DamageType != DamageClass.Default)
        {
            CustomCombatText(target.Hitbox, damageDone, CritColorTier, hit.Crit); 
        }
    }

    public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
    {
        Player owner = Main.player[proj.owner];
        var whipTipHitPlayer = owner.GetModPlayer<WhipTipHit>();
        if (ProjectileID.Sets.IsAWhip[proj.type])
        {
            CustomCombatText(target.Hitbox, damageDone, CritColorTier, hit.Crit, whipTipHitPlayer.Check(proj, proj.WhipPointsForCollision, target.Hitbox));
        }
        else if (!proj.IsMinionOrSentryRelated && proj.DamageType != DamageClass.Default)
        {
            CustomCombatText(target.Hitbox, damageDone, CritColorTier, hit.Crit);
        }
    }
}