using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Accessories.Runeterra.Goredrinker;
using tsorcRevamp.Content.Items.Armor.Summon;
using tsorcRevamp.Content.Items.Armor.Summon.AncientDemon;
using tsorcRevamp.Content.Projectiles.Summon.Whips;

namespace tsorcRevamp.Systems;

public class WhipTipHit : ModPlayer
{
    public float WhipTipHitboxSize = 1f;
    public const float WhipTipHitBonusDamage = 35f;
    public override void ResetEffects()
    {
        WhipTipHitboxSize = 1f;
    }
    public bool Check(in Projectile projectile, in List<Vector2> points, in Rectangle targetHitbox)
    {
        Player player = Main.player[projectile.owner];
        var cursePlayer = player.GetModPlayer<tsorcRevampPlayer>();
        var goredrinkerPlayer = player.GetModPlayer<GoredrinkerPlayer>();
        if (goredrinkerPlayer.Equipped && !Player.HasBuff(ModContent.BuffType<GoredrinkerCooldown>()) && goredrinkerPlayer.Swung)
        {
            return true;
        }
        Vector2 TipBase = tsorcRevamp.WhipTipBases[projectile.type];
        if ((Utils.CenteredRectangle(projectile.WhipPointsForCollision[points.Count - 2], TipBase * player.whipRangeMultiplier * projectile.WhipSettings.RangeMultiplier * WhipTipHitboxSize).Intersects(targetHitbox) || 
            Utils.CenteredRectangle(projectile.WhipPointsForCollision[points.Count - 1], TipBase * player.whipRangeMultiplier * projectile.WhipSettings.RangeMultiplier * WhipTipHitboxSize).Intersects(targetHitbox)) 
            && (cursePlayer.BearerOfTheCurse || projectile.type == ModContent.ProjectileType<UrumiProjectile>()))
        {
            return true;
        }
        return false;
    }

    public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
    {
        if (ProjectileID.Sets.IsAWhip[proj.type] && Check(proj, proj.WhipPointsForCollision, target.Hitbox))
        {
            modifiers.SourceDamage += WhipTipHitBonusDamage / 100f;
        }
    }
}