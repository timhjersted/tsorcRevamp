using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Systems;

namespace tsorcRevamp.Content.Items.Armor.Summon.AncientDemon;

public class AncientDemonSetPlayer : ModPlayer
{
    public bool Equipped;
    public bool LowHealth;
    public bool Visual;
    public override void ResetEffects()
    {
        Equipped = false;
        LowHealth = false;
        Visual = false;
    }
    public void ApplyDemonSetBonus(in int currentHealth, in int threshold)
    {
        Player.whipRangeMultiplier += AncientDemonArmor.WhipRange / 100f;
        Player.GetAttackSpeed(DamageClass.Summon) += AncientDemonArmor.AtkSpeed / 100f;
        
        if (currentHealth <= threshold)
        {
            LowHealth = true;

            if (Visual)
            {
                int dust = Dust.NewDust(new Vector2((float)Player.position.X, (float)Player.position.Y), Player.width, Player.height, DustID.Torch,
                    (Player.velocity.X) + (Player.direction * 1), Player.velocity.Y, 100, Color.Green, 1.0f);
                Main.dust[dust].noGravity = true;
            }
        }
    }

    public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
    {
        Player owner = Main.player[proj.owner];
        var whipTipHitPlayer = owner.GetModPlayer<WhipTipHit>();
        if (ProjectileID.Sets.IsAWhip[proj.type])
        {
            if (LowHealth && whipTipHitPlayer.Check(proj, proj.WhipPointsForCollision, target.Hitbox) && Main.myPlayer == Player.whoAmI)
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
        if (LowHealth && hit.DamageType == DamageClass.SummonMeleeSpeed && Main.myPlayer == Player.whoAmI)
        {
            Projectile SummonMeleeBoom = Projectile.NewProjectileDirect(Projectile.GetSource_None(), target.Bottom, 
                Vector2.Zero, ProjectileID.DD2ExplosiveTrapT1Explosion, 
                (int)Player.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(AncientDemonArmor.ExplosionBaseDmg), 0, Player.whoAmI, 1);
            SummonMeleeBoom.position -= new Vector2(0, SummonMeleeBoom.height / 2f);
            SummonMeleeBoom.netUpdate = true;
        }
    }
}