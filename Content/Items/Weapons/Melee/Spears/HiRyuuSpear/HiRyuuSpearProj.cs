using Terraria;
using Terraria.ID;
using tsorcRevamp.Content.Projectiles.Melee;

//using tsorcRevamp.Dusts;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Spears.HiRyuuSpear
{
    class HiRyuuSpearProj : ModdedSpearProjectile
    {
        public override float HoldoutRangeMin => 81f;
        public override float HoldoutRangeMax => 243f;
        public override float HitboxSize => 1;
        public override float Scale => 1;
        public override int dustID => DustID.Blood;
        bool hasHealed = false;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            if (Main.rand.Next(100) < HiRyuuSpearItem.HealOnHitChance && !hasHealed)
            {
                player.statLife += HiRyuuSpearItem.HealOnHit;
                player.HealEffect(HiRyuuSpearItem.HealOnHit, true);
                hasHealed = true;
            }
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            //if(target.noGravity || !Collision.SolidCollision(target.position, target.width, target.height))
            if (target.velocity.Y != 0)
            {
                modifiers.FinalDamage += HiRyuuSpearItem.HiRyuuSpearDamageBoost / 100f;
            }
        }
    }

}
