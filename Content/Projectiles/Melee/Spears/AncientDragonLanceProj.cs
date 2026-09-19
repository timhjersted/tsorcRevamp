using Terraria;
using Terraria.ID;
using tsorcRevamp.Content.Items.Weapons.Melee.Spears;

//using tsorcRevamp.Dusts;

namespace tsorcRevamp.Content.Projectiles.Melee.Spears
{
    class AncientDragonLanceProj : ModdedSpearProjectile
    {
        public override float HoldoutRangeMin => 38f;
        public override float HoldoutRangeMax => 114f;
        public override float HitboxSize => 1;
        public override float Scale => 1;
        public override int dustID => DustID.UnusedWhiteBluePurple;
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ArmorPenetration += AncientDragonLance.ArmorPen;
        }
    }

}
