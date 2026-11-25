using Terraria;
using Terraria.ID;
using tsorcRevamp.Content.Projectiles.Melee;

//using tsorcRevamp.Dusts;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Spears.DragonLance
{
    class DragonLanceProj : ModdedSpearProjectile
    {
        public override float HoldoutRangeMin => 53f;
        public override float HoldoutRangeMax => 160f;
        public override float HitboxSize => 1;
        public override float Scale => 1;
        public override int dustID => DustID.UnusedWhiteBluePurple;
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ArmorPenetration += DragonLanceItem.ArmorPen;
        }
    }

}
