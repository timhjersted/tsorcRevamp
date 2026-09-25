using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Systems;

namespace tsorcRevamp.Content.Items.Armor.Summon.AncientDemon;

public class AncientDemonPlayer : ModPlayer
{
    public bool EquippedSet;
    public override void ResetEffects()
    {
        EquippedSet = false;
    }

    public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
    {
        Player owner = Main.player[proj.owner];
        var whipTipHitPlayer = owner.GetModPlayer<WhipTipHit>();
        if (ProjectileID.Sets.IsAWhip[proj.type])
        {
            if (EquippedSet && whipTipHitPlayer.Check(proj, proj.WhipPointsForCollision, target.Hitbox) && Main.myPlayer == Player.whoAmI)
            {
                Projectile WhipTipBoom = Projectile.NewProjectileDirect(Projectile.GetSource_None(), target.Bottom, 
                    Vector2.Zero, ProjectileID.DD2ExplosiveTrapT1Explosion, 
                    (int)Player.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(AncientDemonArmor.ExplosionBaseDmg), 0, Player.whoAmI, 1);
                WhipTipBoom.position -= new Vector2(0, WhipTipBoom.height / 2f);
                WhipTipBoom.netUpdate = true;
            }
        }
    }

    public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (EquippedSet && hit.DamageType == DamageClass.SummonMeleeSpeed && Main.myPlayer == Player.whoAmI)
        {
            Projectile SummonMeleeBoom = Projectile.NewProjectileDirect(Projectile.GetSource_None(), target.Bottom, 
                Vector2.Zero, ProjectileID.DD2ExplosiveTrapT1Explosion, 
                (int)Player.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(AncientDemonArmor.ExplosionBaseDmg), 0, Player.whoAmI, 1);
            SummonMeleeBoom.position -= new Vector2(0, SummonMeleeBoom.height / 2f);
            SummonMeleeBoom.netUpdate = true;
        }
    }
}