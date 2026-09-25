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
    public void CustomCombatText(in Rectangle targetHitbox, in int damageDealt, in int CritColorTier, in bool isCrit, bool isWhipTipCrit = false, bool SendPacket = true)
    {
        Color ColorOfCrit = Color.Orange;
        switch (CritColorTier)
        {
            case 1:
            {
                ColorOfCrit = Color.Blue;
                break;
            }
            case 2:
            {
                ColorOfCrit = Color.Purple;
                break;
            }
            case 3:
            {
                ColorOfCrit = Color.White;
                break;
            }
            case 4:
            {
                ColorOfCrit = Color.Black;
                break;
            }
            case 5:
            {
                ColorOfCrit = Color.Red;
                break;
            }
            default:
            {
                if (isCrit)
                {
                    ColorOfCrit = Color.OrangeRed;
                }
                break;
            }
        }
        CombatText.NewText(targetHitbox, ColorOfCrit, damageDealt + (isWhipTipCrit ? "!" : ""), isCrit, false);
        if (Main.netMode == NetmodeID.MultiplayerClient && SendPacket)
        {
            ModPacket textPacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
            textPacket.Write(tsorcPacketID.CustomMultiplayerCombatText);
            textPacket.Write((byte)Player.whoAmI);
            textPacket.Write(targetHitbox.X);
            textPacket.Write(targetHitbox.Y);
            textPacket.Write(targetHitbox.Width);
            textPacket.Write(targetHitbox.Height);
            textPacket.Write(CritColorTier);
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