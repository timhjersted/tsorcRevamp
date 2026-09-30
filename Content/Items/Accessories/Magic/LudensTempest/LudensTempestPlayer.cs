using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Buffs;

namespace tsorcRevamp.Content.Items.Accessories.Magic.LudensTempest;

public class LudensTempestPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects()
    {
        Equipped = false;
    }

    public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
    {
            if (Equipped && hit.DamageType == DamageClass.Magic && !Player.HasBuff(ModContent.BuffType<LudensTempestCooldown>()) && !Player.DeadOrGhost)
            {
                int? closest = UsefulFunctions.GetClosestEnemyNPC(target.Center);
                if (closest.HasValue && (Main.npc[closest.Value].type != NPCID.TargetDummy || Main.npc[closest.Value].Distance(target.Center) < 2000))
                {
                    Vector2 velocity = UsefulFunctions.Aim(target.Bottom, Main.npc[closest.Value].Top, 3);
                    if (Main.myPlayer == Player.whoAmI)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_None(), target.Center, velocity + new Vector2(-1, -2), ModContent.ProjectileType<LudensTempestFire>(), (int)(hit.SourceDamage * LudensTempestItem.ProcDmg), 0, Main.myPlayer, 0);
                        Projectile.NewProjectile(Projectile.GetSource_None(), target.Center, velocity + new Vector2(0, -3), ModContent.ProjectileType<LudensTempestFire>(), (int)(hit.SourceDamage * LudensTempestItem.ProcDmg), 0, Main.myPlayer, 0);
                        Projectile.NewProjectile(Projectile.GetSource_None(), target.Center, velocity + new Vector2(1, -2), ModContent.ProjectileType<LudensTempestFire>(), (int)(hit.SourceDamage * LudensTempestItem.ProcDmg), 0, Main.myPlayer, 0);
                    }
                    Player.AddBuff(ModContent.BuffType<LudensTempestCooldown>(), LudensTempestItem.Cooldown * 60);
                }
                SoundEngine.PlaySound(new SoundStyle(UsefulFunctions.RefactorableFilepath(typeof(LudensTempestFire)) + "_Cast") with { Volume = 0.25f }, target.Center);
            }
            else if (Equipped && hit.DamageType == DamageClass.Magic && Player.HasBuff(ModContent.BuffType<LudensTempestCooldown>()) && proj.type != ModContent.ProjectileType<LudensTempestFire>() && proj.type != ModContent.ProjectileType<LudensTempestFirelet>())
            {
                UsefulFunctions.AddPlayerBuffDuration(Player, ModContent.BuffType<LudensTempestCooldown>(), -20);
            }
    }
}