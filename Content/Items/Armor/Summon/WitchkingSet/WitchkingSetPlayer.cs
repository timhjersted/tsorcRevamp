using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Armor;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Projectiles.VFX;

namespace tsorcRevamp.Content.Items.Armor.Summon.WitchkingSet;

public class WitchkingSetPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects()
    {
        Equipped = false;
    }

    public override void ArmorSetBonusActivated()
    {
        if (!Player.HasBuff(ModContent.BuffType<WitchkingScreamCooldown>()) && Equipped)
        {
            DoWitchkingScream();
        }
    }

    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (tsorcRevamp.WitchScream.JustReleased && !Player.HasBuff(ModContent.BuffType<WitchkingScreamCooldown>()) && Equipped)
        {
            DoWitchkingScream();
        }
    }

    public void DoWitchkingScream()
        {
                        Player.AddBuff(ModContent.BuffType<WitchkingScreamCooldown>(), 20 * 60);
                        
                        if (Main.myPlayer == Player.whoAmI)
                        {
                            SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Lotr/WitchkingScream"), Player.Center);

                            Projectile.NewProjectile(
                                Player.GetSource_Misc("WitchScream"),
                                Player.Center,
                                Vector2.Zero,
                                ModContent.ProjectileType<ExplosionFlash>(),
                                0,
                                0,
                                Player.whoAmI,
                                550, // ai0
                                20  // ai1
                            );
                            Projectile.NewProjectile(
                                Player.GetSource_Misc("WitchScream"),
                                Player.Center,
                                Vector2.Zero,
                                ModContent.ProjectileType<ShockwaveEffect>(),
                                0,
                                0,
                                Player.whoAmI,
                                520, // ai0
                                60   // ai1
                            );
                            for (int i = 0; i < 30; i++) 
                            {
                                Vector2 dustSpeed = Main.rand.NextVector2Circular(30f, 30f);
                                int dustIndex = Dust.NewDust(Player.Center, 0, 0, 114, dustSpeed.X, dustSpeed.Y, 0, default(Color), 1.9f);
                                Main.dust[dustIndex].noGravity = true; 
                            }
                            for (int i = 0; i < 30; i++) 
                            {
                                Vector2 dustSpeed = Main.rand.NextVector2Circular(30f, 30f);
                                int dustIndex = Dust.NewDust(Player.Center, 0, 0, 130, dustSpeed.X, dustSpeed.Y, 0, default(Color), 1.9f);
                                Main.dust[dustIndex].noGravity = true; 
                            }

                            float radius = 30 * 16;
                            foreach (var npc in Main.ActiveNPCs)
                            {
                                bool killable = !npc.dontTakeDamage && !npc.immortal && !tsorcRevamp.UntargetableNPCs.Contains(npc.type);
                                if (!npc.friendly && killable && npc.Distance(Player.Center) <= radius)
                                {
                                    npc.AddBuff(ModContent.BuffType<WitchkingCurse>(), 6 * 60); // 6 seconds
                                    npc.AddBuff(BuffID.Confused, 4 * 60);
                                    int baseDamage = (int)Player.GetTotalDamage(DamageClass.Summon).ApplyTo(800);
                                    int finalDamage = Main.DamageVar(baseDamage);
                                    
                                    npc.StrikeNPC(new NPC.HitInfo
                                    {
                                        Damage = finalDamage,
                                        Knockback = 0,
                                        HitDirection = 0,
                                        Crit = false,
                                        DamageType = DamageClass.Summon
                                    }, false, false); // noPlayerInteraction = false, dontTriggerSound = false
                                }
                            }
                        }
        }
}