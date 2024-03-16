using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Spears;

//using tsorcRevamp.Dusts;

namespace tsorcRevamp.Projectiles.Melee.Spears
{
    class HiRyuuSpearProj : ModdedSpearProjectile
    {
        public override float HoldoutRangeMin => 81f;
        public override float HoldoutRangeMax => 243f;
        public override float HitboxSize => 1;
        public override float Scale => 1;
        public override int dustID => DustID.Blood;
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            //if(target.noGravity || !Collision.SolidCollision(target.position, target.width, target.height))
            if (target.velocity.Y != 0)
            {
                modifiers.FinalDamage += HiRyuuSpear.HiRyuuSpearDamageBoost / 100f;
            }
        }
    }

}
